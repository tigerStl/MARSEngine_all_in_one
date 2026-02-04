extern alias clientWCF;
using com.Mars.Constants;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.com.Mars.DB;
using Mars.Model;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Business;

namespace MarsTestFrame.SourceCode.com.Mars.Excels.DB
{
    public class DashBoardFromDB : BatchXls
    {
#if _Datafrom_Database
        private static MLogger Logger = MLogger.GetLogger(typeof(DashBoardFromDB));
        private long currentApplicationID;
        internal string CurrentApplicationID

        {
            get { return currentApplicationID+""; }
            set {
                if (!long.TryParse(value, out currentApplicationID))
                    currentApplicationID = -1;
                
            }
        }
        public override ERROR_CODE loadTestCase()
        {
            Logger.logBegin("loadXlsFile");
            BeforeLoadTestCase();
            ERROR_CODE eError = mAlystTestCase();
            Logger.Info("loadXlsFile", string.Format("mAlystExcleFile return ERROR_CODE [{0:X}], INFO:[{1}]", eError, ERROR_INFO.GET_ERROR_STR(eError)));
            Logger.logEnd("loadXlsFile");

            //this.mobjCurrentConnection.Close();
            return eError;
        }

        public override ERROR_CODE OnLoadCurrentTestSuiteEventImpl()
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.mobjCurrentTestSuite == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_CALL_BEGIN_NAVIGATEFIRST_PARA_0;
                return eCde;
            }

            eCde = mobjCurrentTestSuite.loadTestCase();
            /** compile the objTestSuit **/
            //List<ConfigObjectBase> lstAllSteps = objTestSuit.
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("OnLoadcurrentTestSuiteEventImpl", string.Format("Error Code ocurrs after call objTestSuit.loadXlsFile() ERROR_CODE:[{0:X}]，ERROR_INFO:[{1}]", eCde, ERROR_INFO.GET_ERROR_STR(eCde)));
                return eCde;
            }
            return eCde;
        }

        

        protected override ERROR_CODE mAlystTestCase()
        {
            Logger.logBegin("mAlystExcleFile");
            mlistTestSuite.Clear();

            ERROR_CODE error = this.GetSpecialTableDataToList(this.CurrentTestProjectNameID,this.mlistTestSuite);
            return error;
        }

        /// <summary>
        /// Get Runn-able test suite from Database
        /// </summary>
        /// <param name="strProjID"></param>
        /// <param name="lstDes"></param>
        /// <returns></returns>
        protected override ERROR_CODE GetSpecialTableDataToList(string strProjID, List<ConfigObjectBase> lstDes)
        {
            List<ConfigObjectBase> lstRslt = DashBoardFactory.GetProjectsAppViewsByProjID(strProjID,new int?[] { (int)ENUM_TEST_SUITE_RUNTYPE._EXECUTE , (int)ENUM_TEST_SUITE_RUNTYPE._RUN },this.currentApplicationID);
            foreach(ConfigObjectBase objItem in lstRslt)
            {
                lstDes.Add(objItem) ;
            }
            return ERROR_CODE._NO_ERROR; 
        }

        public override ERROR_CODE OnNavigateHandlerImpl()
        {
            return base.OnNavigateHandlerImpl();
        }

        public override ERROR_CODE OnNavigateWithRelyIdAndLoopIdHandlerImpl(string strRelyId, int iLoop)
        {
            return base.OnNavigateWithRelyIdAndLoopIdHandlerImpl(strRelyId, iLoop);
        }

        protected override TCObjects CreateTCObjectByTS(BatchConfigObject objTestSuite)
        {
            if (objTestSuite is BatchConfigObjectFromDB)
            {
                TCObjects4DB objDBObj = new TCObjects4DB();
                objDBObj.XlsFileNameWithPath = objTestSuite.TCFilePath;
                objDBObj.CurrentRunName = objTestSuite.TCSheetName;
                objDBObj.Id4Project = objTestSuite.TestSuiteID;
                objDBObj.Action4Project = objTestSuite.Action;
                objDBObj.TestSuiteKeyID = ((BatchConfigObjectFromDB)objTestSuite).TestSuiteKeyID;
                objDBObj.TestCaseKeyId = ((BatchConfigObjectFromDB)objTestSuite).TestCaseKeyId;
                objDBObj.AssignedStoryBoardInfo = objTestSuite;
#if v_useNameId
                objDBObj.CurrentApplicationId = this.currentApplicationID;
#endif
#if v_16AndUp
                objDBObj.CurrentDatasetName = ((BatchConfigObjectFromDB)objTestSuite).DataSetName;
#endif
                return objDBObj;
            }
            else
                return base.CreateTCObjectByTS(objTestSuite);
        }

        //protected override ERROR_CODE BeginRunTestSuit()
#endif
    }

    public class TCObjects4DB: TCObjects
    {

#if v_useNameId
        protected long currentApplicationId;
        public long CurrentApplicationId
        {
            get { return currentApplicationId; }
            set { currentApplicationId = value; }
        }
#endif
#if _Datafrom_Database
        private static MLogger logger = MLogger.GetLogger(typeof(TCObjects4DB));

        protected long? testSuiteKeyId;
        protected long? testCaseKeyId;
        protected long? storyBoardId;
        protected long runOrder;

        protected T_PROJ_TC_MGR m_storyBoard;

        public long? TestSuiteKeyID
        {
            get { return this.testSuiteKeyId; }
            set { this.testSuiteKeyId = value; }
        }
        public long? TestCaseKeyId
        {
            get { return this.testCaseKeyId; }
            set { this.testCaseKeyId = value; }
        }

        public long? StoryBoardId
        {
            get { return this.storyBoardId; }
            set { this.storyBoardId = value; }
        }

        public long RunOrd
        {
            get { return this.runOrder; }
            set { this.runOrder=value; }
        }

        public BatchConfigObject AssignedStoryBoardInfo { get; internal set; }

        protected override ERROR_CODE mAlystTestCase()
        {
            /// Load data from database
            /// 
            logger.logBegin("mAlystExcleFile");
            LoadTestSteps();
            return LoadTCData();
        }

        protected void LoadTestSteps()
        {
            logger.logBegin("LoadTestSteps");
            this.mlstSteps.Clear();


            if (AssignedStoryBoardInfo== null)
            {
                logger.Warnning("LoadTestSteps", "No StoryBoard object information is assigned. [AssignedStoryBoardInfo== null]");
                return;
            }
            if (!(AssignedStoryBoardInfo is BatchConfigObjectFromDB))
            {
                logger.Error("LoadTestSteps", string.Format("AssignedStoryBoardInfo should be BatchConfigObjectFromDB,but it is :[{0}]", AssignedStoryBoardInfo.GetType().ToString()));
                return;
            }
#if !v_useNameId
            IList<V_TEST_STEPS_FULLVISIONDTO>  lstDTO = BoHelper.GetTestStepsByTestCaseID(((BatchConfigObjectFromDB)AssignedStoryBoardInfo).TestCaseKeyId??-1);
#else
            List<V_TEST_STEPS_FULLVISIONDTO> lstDTO = (List<V_TEST_STEPS_FULLVISIONDTO>)BoHelper.GetTestStepsByTestCaseID(((BatchConfigObjectFromDB)AssignedStoryBoardInfo).TestCaseKeyId ?? -1, this.currentApplicationId);
            logger.Info("LoadTestSteps",string.Format("returned test steps :[{0}]", lstDTO.Count));
#endif
            // create Test stepobjects
            TestStepsFromDB objTestStep;
            foreach (V_TEST_STEPS_FULLVISIONDTO objDto in lstDTO)
            {
                objTestStep = null;
                if ((objTestStep = TestStepsFromDB.CreateObjectFromDBStepInfo(objDto)) == null) continue;
                this.mlstSteps.Add(objTestStep);                
            }
            // Load Data, key of hash table is Data Set Id and StoryBoard id
            TestCaseData objTestCaseData = new TestCaseData(((BatchConfigObjectFromDB)AssignedStoryBoardInfo).AssignedStoryObject);
            objTestCaseData.loadTestCase();

            string strDataKey = ConcatDataHashKey();
            if (!mlstDataFile.ContainsKey(strDataKey))
            {
                mlstDataFile.Add(strDataKey,objTestCaseData);
            }
            else
            {
                mlstDataFile[strDataKey] = objTestCaseData;
            }
        }

       
        protected virtual ERROR_CODE LoadTCData()
        {
            logger.logBegin("LoadTCData");
            //mlstSteps.Clear();
            //ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            //if (testCaseKeyId == null)
            //{
            //    logger.Error("LoadTCData", ERROR_INFO.GET_ERROR_STR(eCde = ERROR_CODE._TEST_CASE_DB_NO_TEST_CASE_ID_0));
            //    return eCde;
            //}
            //if (testSuiteKeyId == null)
            //{
            //    logger.Error("LoadTCData", ERROR_INFO.GET_ERROR_STR(eCde = ERROR_CODE._TEST_CASE_DB_NO_TEST_SUITE_ID_0));
            //    return eCde;
            //}
            ///// Load data from database
            //eCde = DashBoardFactory.GetTestStepsByTestSuiteIdAndTestCaseId(this.testSuiteKeyId, this.testCaseKeyId, mlstDataFile);

            logger.logEnd("LoadTCData");
            //throw new NotImplementedException();
            return ERROR_CODE._NO_ERROR;
        }

        private string ConcatDataHashKey()
        {
            return string.Format("{0}_{1}", ((BatchConfigObjectFromDB)AssignedStoryBoardInfo).AssignedStoryObject.STORYBOARD_ID, ((BatchConfigObjectFromDB)AssignedStoryBoardInfo).AssignedStoryObject.DATA_SETTING_ID); 
        }
        internal override int GetTestLoopCount()
        {
            //XlsFileNameWithPath
            logger.logBegin("GetTestLoopCount");
            /** get default data file name */
            try
            {
                /// 1,get data list from mlstDataFile
                string strDataKey = ConcatDataHashKey();
                if (!mlstDataFile.ContainsKey(strDataKey)) return -1;

                /// 2,get count from object
                /// 
                TestCaseData objTestCaseData = (TestCaseData)mlstDataFile[strDataKey];
                return objTestCaseData.GetColomnCount();

                //string strKey = XlsFileNameWithPath;

                //int iPos = XlsFileNameWithPath.LastIndexOf(cnst_data_file_tail);
                //if (iPos >= 0) strKey = XlsFileNameWithPath.Substring(0, iPos);
                //iPos = XlsFileNameWithPath.LastIndexOf(".");
                //if (iPos >= 0) strKey = XlsFileNameWithPath.Substring(0, iPos);

                //if (mlstDataFile.ContainsKey(strKey))
                //{
                //    TCDataFile objDataFile = (TCDataFile)mlstDataFile[strKey];
                //    int iLoop = objDataFile.GetColomnCount();
                //    return iLoop;
                //}
                //else
                //return -1;
            }
            finally
            {
                logger.logEnd("GetTestLoopCount");
            }


        }
        internal override string GetDataStringFromDataFile(string strObjectName, int iLoopId, ref ERROR_CODE eCde,int iStepId=-1)
        {
            logger.Info("GetDataStringFromDataFile",string.Format("ObjectName:[{0}],iLoopId:[{1}], stepId:[{2}]",strObjectName,iLoopId,iStepId));
            string strKey = ConcatDataHashKey();
            if (!this.mlstDataFile.ContainsKey(strKey))
            {
                eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                logger.Error("GetDataStringFromDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrCurrentDataFileName));
                return null;
            }

            TestCaseData objDataFile = (TestCaseData)this.mlstDataFile[strKey];
            string strResult = "";
            eCde = objDataFile.GetOneCellValueFromData(iLoopId, strObjectName, ref strResult,iStepId);
            logger.logEnd("GetDataStringFromDataFile");
            return strResult;
        }

        internal override ERROR_CODE SaveDataToSpecialCellComparisonMode(string strObjNameIndex, string strValueWithSetting, string strValue, int iLoop, string strConvertedValue = null)
        {
            logger.Info("SaveDataToSpecialCellComparisonMode",string.Format("strObjNameIndex:[{0}], strValueWithSetting:[{1}], strValue:[{2}], iLoop:[{3}], strConvertedValue:[{4}]",
                strObjNameIndex, strValueWithSetting, strValue, iLoop, strConvertedValue));
            ///算法：
            /// 1，如果只有一个值，则回写单一记录
            /// 2，如果多个值，第一个值写到单一记录中，然后复制该记录（id除外），将其他值依次写入其他记录
            /// notice：
            /// 如果 采用新的ObjectName，那么，从INPUT_VALUE_SETTING获得，同时将。如果没有，则将ObjectName复制过来
            /// 
            string strCurrentObjName = strObjNameIndex;
            if (string.IsNullOrEmpty(strValue))
            {
                logger.Info("SaveDataToSpecialCellComparisonMode", "NoData Need save to Database, as captured data is [null or empty]");
                return ERROR_CODE._NO_ERROR;
            }
            string[] arrValue = strValue.Split('\n');
            // deal with the first Value
            string strCurValue = arrValue[0];
            //B_TEST_REPORT_STEPS

            return base.SaveDataToSpecialCellComparisonMode(strObjNameIndex, strValueWithSetting, strValue, iLoop, strConvertedValue);
        }
#endif
        }

}
