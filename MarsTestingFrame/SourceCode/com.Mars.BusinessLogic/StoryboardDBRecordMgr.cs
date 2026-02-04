using Mars.Business;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarsTestFrame.CommuniteServer;
using com.Mars.Constants;

namespace MarsTestFrame.SourceCode.com.Mars.BusinessLogic
{

    public enum MARSE_TESETRESULT
    {
        E_INITIALIZATION = 0x00,
        E_SUCCESS = 0x01,
        E_FAILURE = 0x02
    }
    internal class StoryboardDBRecordMgr
    {
#if _Datafrom_Database
        private const string cnst_initialiazation_msg = "Begin to Test";

        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardDBRecordMgr));
        private string _currentTestStoryboardId;
        private bool _isBaseLineTest;
        private bool _isContinueToTest = false;
        private B_PROJ_TEST_RESULT storyBoardTestResult = new B_PROJ_TEST_RESULT();
        private long _currentTestMarkGroupId = -1;
        private int miCurrentLoop = -1;
        public int CurrentLoop
        {
            get { return miCurrentLoop; }
        }
        public int BaseLineTestId
        {
            get { return (_isBaseLineTest ? 1 : 0); }
        }
        public static string currentDBIdx = null;
        /// <summary>
        /// int -- loop
        /// </summary>
        private Dictionary<int, B_TEST_REPORT> currentTestCaseLog = new Dictionary<int, B_TEST_REPORT>();

        public long curentTestMarkGroupId
        {
            get
            {
                try
                {
                    if (_currentTestMarkGroupId==-1)
                    {
                        CreateNewTestMarkGroupId(currentDBIdx);
                    }
                    return _currentTestMarkGroupId;
                }
                catch (Exception e)
                {
                    Logger.Error("curentTestMarkGroupId_get", string.Format("Exceptions:[{0}]", e.Message), e);
                    return _currentTestMarkGroupId = -1;
                }
            }
            set { _currentTestMarkGroupId = value; }
        }

        public string currentTestStoryboardId
        {
            get { return _currentTestStoryboardId; }
            set { _currentTestStoryboardId = value; }
        }

        internal void Initialization(string strDBIdx, string strStoryBoardId, bool isBaseLine,bool isContinue2Test)
        {
            Logger.Info("Initialization", string.Format("StoryBoardId:[{0}] isBaseLine:[{1}] isContinue2Test:[{2}]", strStoryBoardId, isBaseLine, isContinue2Test));
            _currentTestStoryboardId = strStoryBoardId;
            _isBaseLineTest = isBaseLine;
            _isContinueToTest = isContinue2Test;
            ///set the latest_test_mark_ID
            /// 
            CreateNewTestMarkGroupId(strDBIdx);
        }

        internal void CreateNewTestMarkGroupId(string strDBIdx)
        {
            if (!_isContinueToTest)
                _currentTestMarkGroupId = BoHelper.GetLastestTestMarkID(strDBIdx:strDBIdx);
            else
            {
                long l_currentStoryBoardId = -1;
                if (long.TryParse(this._currentTestStoryboardId, out l_currentStoryBoardId))
                {
                    _currentTestMarkGroupId = BoHelper.GetLastestTestMarkIDByStoryBoardId(l_currentStoryBoardId,strDBIdx);
                }
                else
                {
                    Logger.Warnning("CreateNewTestMarkGroupId", string.Format("Can't convert storyboard id [{0}] to long, new LasterTestMarkId is fetched", _currentTestStoryboardId));
                    _currentTestMarkGroupId = BoHelper.GetLastestTestMarkID(strDBIdx:strDBIdx);
                }
                
            }
        }

        internal void InitializeTestReportResult(long? testCaseKeyId,long currentStoryBoardDetail_id)
        {
            Logger.Info("InitializeTestReportResult", string.Format("TestCaseID:[{0}]", testCaseKeyId ?? -1));

            storyBoardTestResult.TEST_CASE_ID = testCaseKeyId;
            storyBoardTestResult.TEST_BEGIN_TIME = DateTime.Now;
            storyBoardTestResult.CREATE_TIME = DateTime.Now;
            // missing 
            // storyBoardTestResult.RELY_TEST_CASE_ID 
            storyBoardTestResult.TEST_MODE = (short)(_isBaseLineTest ? 1 : 0);
            storyBoardTestResult.TEST_RESULT_IN_TEXT = cnst_initialiazation_msg;
            storyBoardTestResult.TEST_RESULT = (short)MARSE_TESETRESULT.E_INITIALIZATION;
            storyBoardTestResult.LATEST_TEST_MARK_ID = _currentTestMarkGroupId;
            
            /// clear TC Log information
            currentTestCaseLog.Clear();

            string strError = "";
            try
            {
                storyBoardTestResult.STORYBOARD_DETAIL_ID = currentStoryBoardDetail_id;
                //storyBoardTestResult.STORYBOARD_DETAIL_ID = long.Parse(this._currentTestStoryboardId);
                int iError = BoHelper.SaveStoryBoardTestResult(storyBoardTestResult, ref strError);
                if (iError != 1)
                {
                    Logger.Error("InitializeTestReportResult", string.Format("Exceceptions or error occurs when call BoHelper.SaveStoryBoardTestResult, Error:\r\n[{0}]", strError));
                    return;
                }
                
            }
            catch (Exception e)
            {
                Logger.Error("InitializeTestReportResult", string.Format("Exception:[{0}]", e.Message), e);
            }

        }

        internal void UpdateTestReportResult(int idSuccess, string strErrorInfo)
        {
            Logger.Info("UpdateTestReportResult", string.Format("idSuccess:[{0}], ErrorInfo:[{1}]", idSuccess, strErrorInfo));
            if (storyBoardTestResult == null)
            {
                Logger.Error("UpdateTestReportResult", string.Format("No Database agent instance, storyBoardTestResult==null"));
                return;
            }
            storyBoardTestResult.TEST_RESULT_IN_TEXT = strErrorInfo;
            storyBoardTestResult.TEST_RESULT = (short)idSuccess;
            storyBoardTestResult.TEST_END_TIME = DateTime.Now;

            ///update database 
            string strDBError = "";
            try
            {
                
                int iError = BoHelper.UpdateStoryBoardTestResult(storyBoardTestResult, ref strDBError, currentDBIdx);
                if (iError != 1)
                {
                    Logger.Error("UpdateTestReportResult", string.Format("Exceceptions or error occurs when call BoHelper.UpdateStoryBoardTestResult, Error:\r\n[{0}]", strDBError));
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateTestReportResult",string.Format("Exceptions:[{0}]",e.Message),e );
            }
        }

        internal int CreateStepLog(long stepId, TestStepRunningRecorder objCurrentStepLog)
        {
            Logger.Info("CreateStepLog", string.Format("StepId:[{0}]",stepId));
            try
            {
                if (objCurrentStepLog == null) return (int)ERROR_CODE._NO_ERROR;
                B_TEST_REPORT_STEPS objStepRpt = null;

                // find t_test_report obj by loop id
                B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(objCurrentStepLog.LoopId+1);
                objStepRpt = objTestReport.GetTestRptStepsByTestStepId(stepId);
                if (objStepRpt==null)
                {
                    objStepRpt = new B_TEST_REPORT_STEPS();
                    objStepRpt.STEPS_ID = stepId;
                    objTestReport.AddOrUpdateTestSteptsRpt(objStepRpt);
                }
                //objStepRpt.STEPS_ID = stepId;
                objStepRpt.BEGIN_TIME = objCurrentStepLog.StartTime;

                if (objTestReport == null )
                {
                     Logger.Error("CreateStepLog", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), objCurrentStepLog.LoopId));
                    return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1 ;
                }
                objStepRpt.TEST_REPORT_ID = objTestReport.TEST_REPORT_ID;
                objStepRpt.RUNNING_RESULT = (int)MARSE_TESETRESULT.E_INITIALIZATION;
                string strErrror = "",strAdv ="";
                int  iError = objStepRpt.CreateIdAndSave(ref strErrror,ref strAdv, currentDBIdx);
                if (iError<0)
                {
                    Logger.Error("CreateStepLog", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_REPORT_STEPS_CANT_SAVE_PARA_1),strErrror));
                    return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_REPORT_STEPS_CANT_SAVE_PARA_1;
                }
                
                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("CreateStepLog",string.Format("Exception:[{0}]",e.Message),e);
                return -1;
            }            
        }

        /// <summary>
        /// update Datafield of table cache of the memory. 
        /// </summary>
        /// <param name="strData">Data to be cached</param>
        /// <param name="iLoopId">Which Loop, parameter for test step record</param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public ERROR_CODE updateDataField(string strData, int iLoopId, long stepId=-1)
        {
            Logger.Info("updateDataField", string.Format("strData:[{0}], iLoopId:[{1}] stepId:[2]", strData, iLoopId, stepId));
            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport==null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("updateDataField",string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return eCde;
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(stepId);
            if (objTestStepLog==null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("updateDataField",string.Format(ERROR_INFO.GET_ERROR_STR(eCde), stepId));
                return eCde;
            }
            objTestStepLog.INPUT_VALUE_SETTING = strData;

            return ERROR_CODE._NO_ERROR;
        }
        internal int UpdateCurrentTestStepResult(int iLoopId,long assignedTestStepId, int idSuccess, string strError,byte[] arrPicInfo)
        {
            Logger.Info("UpdateCurrentTestStepResult", string.Format("TestStepId:[{0}], idSuccess:[{1}], strError:[{2}], LoopId:[{3}] picInfo len:[{4}]",
                assignedTestStepId, idSuccess, strError, iLoopId,arrPicInfo==null?0:arrPicInfo.Length));
            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("UpdateCurrentTestStepResult", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return (int)eCde;
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);            
            if (objTestStepLog == null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("UpdateCurrentTestStepResult", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), assignedTestStepId));
                return (int)eCde;
            }

            objTestStepLog.RUNNING_RESULT = (short)idSuccess;
            objTestStepLog.END_TIME = DateTime.Now;
            objTestStepLog.RUNNING_RESULT_INFO = strError;
            objTestStepLog.INFO_PIC = arrPicInfo;
            Logger.Info("----test byte[] Len----",string.Format("copied data lenth:[{0}]", objTestStepLog.INFO_PIC==null?-1: objTestStepLog.INFO_PIC.Length));
            try
            {
                return objTestStepLog.updateRecord(currentDBIdx);
            }
            catch (Exception e)
            {
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Exception:[{0}]",e.Message),e);
                return -1;
            }
            finally
            {
                Logger.logEnd("UpdateCurrentTestStepResult");
            }
            
        }

        internal int StoreData4ForTestReport_Steps(string strObjectNameIdx, string strData2Store, int iLoopId, 
            long? testCaseKeyId, long assignedTestStepId, string strDBIdx)
        {
            Logger.Info("StoreData4ForTestReport_Steps",string.Format("Trying to save data, objName:[{0}], data2Store:[{1}], Loop:[{2}], testcaseId:[{3}], TestStepId:[{4}]",
                strObjectNameIdx,strData2Store, iLoopId, testCaseKeyId,assignedTestStepId));

            ///算法：
            /// 1，如果只有一个值，则回写单一记录
            /// 2，如果多个值，第一个值写到单一记录中，然后复制该记录（id除外），将其他值依次写入其他记录
            /// 
            /// notice：
            /// 如果 采用新的ObjectName，那么，从INPUT_VALUE_SETTING获得，同时将。如果没有，则将ObjectName复制过来
            /// RUNNING_RESULT set to 3 that means data
            /// 
            
            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return (int)eCde;
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);
            if (objTestStepLog == null)
            {
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), assignedTestStepId));
                return (int)eCde;
            }

            string strCurrentObjName = strObjectNameIdx;
            if (string.IsNullOrEmpty(strData2Store))
            {
                Logger.Info("StoreData4ForTestReport_Steps", "NoData Need save to Database, as captured data is [null or empty]");
                return (int)ERROR_CODE._NO_ERROR;
            }
            string[] arrValue = strData2Store.Split('\n');
            // deal with the first Value
            string strCurValue = arrValue[0];
            //B_TEST_REPORT_STEPS
            objTestStepLog.RETURN_VALUES = strCurValue;
            List<B_TEST_REPORT_STEPS> lstTestStepReport = new List<B_TEST_REPORT_STEPS>();
            //lstTestStepReport.Add(objTestStepLog);
            /// start from the second 
            for (int i=1;i<arrValue.Length;i++)
            {
                B_TEST_REPORT_STEPS objStps = new B_TEST_REPORT_STEPS();
                objStps = objTestStepLog.CloneSelf();
                objStps.RETURN_VALUES = arrValue[i];
                objStps.RUNNING_RESULT = 3;
                lstTestStepReport.Add(objStps);
            }
            int iResult = B_TEST_REPORT_STEPS.UpdateAndInsertList(objTestStepLog,lstTestStepReport, strCurrentObjName, currentDBIdx);
            return iResult;
        }



        private B_TEST_REPORT GetTestReportObjByLoopId(int loopId)
        {
            Logger.Info("GetTestReportObjByLoopId",string.Format("loopId:[{0}]", loopId));
            B_TEST_REPORT objRpt = null;
            if (!this.currentTestCaseLog.TryGetValue(loopId, out objRpt))
            {
                return null;
            }
            
            return objRpt;
        }

        internal int LogTestCaseStart(string strDBIdx, long? testCaseKeyId, int iLoop)
        {
            Logger.Info("LogTestCaseStart",string.Format("TestCaseId :[{0}], iLoop:[{1}]", testCaseKeyId, iLoop));
            this.miCurrentLoop = iLoop;
            try
            {
                string strError = "";
                B_TEST_REPORT objTestCaseLog = new B_TEST_REPORT();
                objTestCaseLog.HIST_ID = this.storyBoardTestResult.HIST_ID;
                objTestCaseLog.BEGIN_TIME = DateTime.Now;
                objTestCaseLog.LOOP_ID = iLoop;
                objTestCaseLog.TEST_CASE_ID = testCaseKeyId;
                objTestCaseLog.TEST_MODE = this.storyBoardTestResult.TEST_MODE;
                int iErrorId = objTestCaseLog.Create2Entities(ref strError, strDBIdx);
                if (iErrorId<0)
                {
                    Logger.Error("LogTestCaseStart",string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1), strError));
                    return (int)ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1;
                }
                else
                {
                    currentTestCaseLog.Add(iLoop, objTestCaseLog);
                }
                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("LogTestCaseStart",string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1),e.Message),e );
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
            }
        }
        internal int UpdateTestCaseLoopResult(long? testCaseKeyId, int iResultId, int iLoopId, string strEndInfo)
        {
            Logger.Info("UpdateTestCaseLoopResult",string.Format("testCaseKeyId:[{0}], resultId:[{1}], LoopId:[{2}], EndInfo:[{3}]",
                testCaseKeyId, iResultId, iLoopId, strEndInfo));
            /// get t-test-report info
            /// Usually, current TestCaseLog is the current Testcase report with different Loops Info
            B_TEST_REPORT objCurrentLoopRecord=null;
            string strError = "";
            if (!currentTestCaseLog.TryGetValue(iLoopId, out objCurrentLoopRecord))
            {
                Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), iLoopId));
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1;
            }
            try
            {
                objCurrentLoopRecord.END_TIME = DateTime.Now;
                objCurrentLoopRecord.RUNNING_RESULT = (short)iResultId;
                objCurrentLoopRecord.RUNNING_RESULT_INFO = strEndInfo;

                int iError = objCurrentLoopRecord.updateById(ref strError, currentDBIdx);
                if (iError<0)
                {
                    Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1), strError));
                    return (int)ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1;
                }
                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), e.Message), e);
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
            }
        }

        internal int UpdateTestCaseLoopId(int iPos, int iLoopId)
        {
            Logger.Info("UpdateTestCaseLoopId", string.Format("iPos:[{0}], iLoopId:[{1}]", iPos, iLoopId));
            if (!currentTestCaseLog.ContainsKey(iPos))
            {
                Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), iLoopId));
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1;
            }
            B_TEST_REPORT objTarget;
            try
            {
                if (currentTestCaseLog.TryGetValue(iPos, out objTarget))
                {
                    objTarget.LOOP_ID = iLoopId;
                    int iError= objTarget.update(currentDBIdx);
                    if (iError < 0)
                    {
                        Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), "UpdateTestCaseLoopId"));
                        return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
                    }
                    return (int)ERROR_CODE._NO_ERROR;
                }
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), e.Message), e);
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
            }
        }

        internal bool updateVariableValue(string strDBIdx,string strObjectNameIdx, string strData2Store, int iLoop,int iVariableType,ref string strError,string strBaseLineMode= "Build")
        {
            Logger.Info("updateVariableValue",string.Format("Try to update varible [{0}],value:[{1}], iLoop:[{2}], iVarType:[{3}], strBaseLineMode:[{4}]",
                strObjectNameIdx, strData2Store, iLoop, iVariableType,strBaseLineMode));
            /// Steps:
            /// 1, check wether the variable is exist
            /// 2, if exists, then update,else create a new object
            /// 
            bool isRight = true;
            try
            {
                B_SYSTEM_LOOKUP objLook = new B_SYSTEM_LOOKUP();
                string strTableName = "";
                List<B_SYSTEM_LOOKUP> lstObj = null;
                bool iscreateNew;
                //string strTableNameIdx = SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL;
                switch (iVariableType)
                {
                    case 1://globe var
                    case 2://local var
                    case 6:
                    case 8://IF Var
                        lstObj = objLook.GetSystemLookup(strTableName = iVariableType == 1 ? SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL : 
                            (iVariableType==2?SystemConstant.CNST_RESERVED_VARIABLE_LOCAL : SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP), 
                            strObjectNameIdx) ;
                        iscreateNew  = lstObj == null ? true : lstObj.Count > 0 ? false : true;
                        if (iscreateNew)
                        {
                            objLook.DISPLAY_NAME = strData2Store;
                            objLook.FIELD_NAME = strObjectNameIdx;
                            objLook.VALUE = 1;
                            objLook.STATUS = 1;
                            objLook.TABLE_NAME = strTableName;
                            isRight = objLook.InsertSelf(strDBIdx, ref strError);
                        }
                        else
                        {
                            lstObj = ((lstObj.OrderBy(p => p.DISPLAY_NAME)).ToList());
                            isRight = lstObj[0].updateSelf(strData2Store, ref strError,strDBIdx);
                        }
                        return isRight;
                    case 4://modal var
                        int iStatus = string.Compare(strBaseLineMode,SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, true) == 0 ? 1 : 2;                        
                        lstObj = objLook.GetSystemLookup(SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strObjectNameIdx, iStatus, ref strError,strDBIdx);
                        iscreateNew = lstObj == null ? true : lstObj.Count > 0 ? false : true;
                        if (iscreateNew)
                        {
                            objLook.DISPLAY_NAME = strData2Store;
                            objLook.FIELD_NAME = strObjectNameIdx;
                            objLook.VALUE = 1;
                            objLook.STATUS =(short)iStatus;
                            objLook.TABLE_NAME = SystemConstant.CNST_RESERVED_VARIABLE_MODAL;
                            isRight = objLook.InsertSelfWithStatus(ref strError);
                        }
                        else
                        {
                            lstObj = ((lstObj.OrderBy(p => p.DISPLAY_NAME).ThenBy(p=>p.STATUS)).ToList());
                            isRight = lstObj[0].updateSelf(strData2Store, ref strError,strDBIdx);
                        }
                        return isRight;
                    
                        
                }
                                
                return isRight;
            }
            catch (Exception e)
            {
                Logger.Error("updateVariableValue",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
                       
        }


#endif
    }
}
