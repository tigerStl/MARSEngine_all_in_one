/**
 * Record:
 * Date: 20150817
 * Creator:Tiger
 * Version:1.01
 * Description: 
 *   1, new status for RUN column:Exe, done, skip and run
 *   2, Done: when Sucessfully run a test case, when initialized status is run and result is sucessful
 *      Skip: as defined
 *      Run : Initialized value
 *      Exe : Forced to run even the result is failed
 * */

extern alias clientWCF;
using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.com.Mars.TestConfigObjects.Adatpers;
using clientWCF::MarsTestFrame.CommuniteServer;
using MarsTestFrame.SourceCode.com.Mars.Compiler;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls
{
    public delegate ERROR_CODE OnGetCurrentTestCaseByTestSuiteAndTestCaseNameEvent(string strTestSuiteName, string strTestCaseName, TCObjects objTarget);

    public delegate ERROR_CODE OnBeginNavigateTestSuite();
    public delegate ERROR_CODE OnBeginNavigateTestSuiteWithRelyIdAndLoopId(string strRelyId, int iLoop) ;
    public delegate ERROR_CODE OnGetNextTestSuiteEvent(ref TCObjects objTestSuite,long lAppId=-1);
    public delegate ERROR_CODE OnLoadCurrrentTestSuitEvent(); // same as AfterGetNextTestSuiteEvent
    //public delegate ERROR_CODE OnAfterGetNextTestSuiteEvent();
    public delegate ERROR_CODE OnTestsuiteIsDoneEvent(TestSuiteRunStatusInfo objStatus, string strAction = "Run", bool isContinueWhenFalse=false);


    public class BatchXls : MarsExcelFileBase
    {
        #region CONST
        protected const string CNST_EXECUTE_TABLENAME = "Batch";
        public const string cnst_SUCCESS = "SUCCESS";
        public const string cnst_FAILURE = "FAILURE";
        #endregion

#if _Datafrom_Database
        internal const string cnst_run_type_run = "RUN";
        internal const string cnst_run_type_exe = "EXEC";
        internal const string cnst_run_type_skip = "SKIP";
        internal const string cnst_run_type_done = "DONE";
#endif
        private static MLogger Logger = MLogger.GetLogger(typeof(BatchXls));

        protected List<ConfigObjectBase> mlistTestSuite = new List<ConfigObjectBase>();
        protected int miCurrentNavigateId = 0;

        public string CurrentTestProjectNameID { get; set; } 
        #region for Service
        protected TCObjects mobjCurrentTestSuite = null;
        public virtual ERROR_CODE OnGetNextTestSuiteEventImpl(ref TCObjects objTestSuite,long lAppId=-1)
        {
#if v_useNameId
            objTestSuite = getNextTCObject(lAppId);
#else
            objTestSuite = getNextTCObject();
#endif
            return ERROR_CODE._NO_ERROR;
        }
        public virtual ERROR_CODE OnLoadCurrentTestSuiteEventImpl()
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
            /*** 加载数据 ***/
            mobjCurrentTestSuite.InitDefaultDataFileName();
            eCde = mobjCurrentTestSuite.loadData();

            return eCde;
        }
#endregion

#region Event for Compiler
        private OnCompileListEvent CompileListEventHandler = null;
        public void AddOnCompileListEvent(OnCompileListEvent funcImpl, bool isAdd)
        {
            if (isAdd)
            {
                this.CompileListEventHandler += funcImpl;
            }
            else
            {
                this.CompileListEventHandler -= funcImpl;
            }
        }
#endregion //Event for Compiler
#if _Datafrom_Database
        private bool CheckDatasourceForTC()
        {
            Logger.logBegin("CheckDatasourceForTC");
            string strTCDataSource = AppConfigReader.GetTCDataSource(); 
            if (string.IsNullOrEmpty(strTCDataSource))
            {
                Logger.Warnning("CheckDatasourceForTC", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._OBJECT_IS_NULL));
                return false;
            }
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE_DB, strTCDataSource, true) == 0)
            {
                return true;
            }
            return false;
        }
#endif
        protected override ERROR_CODE mAlystTestCase()
        {
            Logger.logBegin("mAlystExcleFile");

            mlistTestSuite.Clear();
#if _Datafrom_Database
            /// To check whether data is from database
            /// 
            bool isDatafromDB = CheckDatasourceForTC();
            
                /// Get data from Excel, Orignal Version
#endif
            ERROR_CODE error = this.GetDataTableFromExcelFile(isDatafromDB);
            if (error != ERROR_CODE._NO_ERROR)
            {
                Logger.Info("mAlystExcleFile", string.Format("GetDataTableFromExcelFile return errorcode[{0:X}]", ERROR_INFO.GET_ERROR_STR(error)));
                return error;
            }

            int iItm = this.CheckTableExists(string.IsNullOrEmpty(CurrentTestProjectNameID) ? CNST_EXECUTE_TABLENAME : CurrentTestProjectNameID, isDatafromDB);
            if (iItm < 0)
            {
                Logger.Info("mAlystExcleFile", string.Format("CheckTableExists return errorcode[{0:X}], \r\n\terror:[{1}]", ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE)));
                return ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE;
            }
#if _Datafrom_Database
            if (!isDatafromDB)
            {
                error = this.GetSpecialTableDataToList(this.mlstCurrentTables[iItm], mlistTestSuite);
            }
            else
            {
                /// get all test cases based on test project id from datbase
                /// 
                //error = this.GetSpecialProjectItemsToListFromDd(CurrentTestProjectNameID, mlistTestSuite);
            }
#else
            error = this.GetSpecialTableDataToList(this.mlstCurrentTables[iItm], mlistTestSuite);
#endif
            if (error != ERROR_CODE._NO_ERROR)
            {
                Logger.Info("mAlystExcleFile", string.Format("GetSpecialTableDataToList return errorcode[{0:X}],\r\n\terror:[{1}]", ERROR_INFO.GET_ERROR_STR(error)));
                return error;
            }
#if _Datafrom_Database
            
#endif
            return ERROR_CODE._NO_ERROR;
        }

        
        public virtual ERROR_CODE GetCurrentTestCaseByTestSuiteNameAndTestCase(string strTestSuite, string strTestCase, TCObjects objTarget)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTarget == null)
            {
                eCde = ERROR_CODE._OBJECT_IS_NULL;
                Logger.Error("GetCurrentTestCaseByTestSuiteNameAndTestCase", "objTarget is Null");
                return eCde;
            }
            string strTestSuiteName = ((BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId-1)).TCFilePath,
                strTestCaseName = ((BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId-1)).TCSheetName;
            if (!(string.Compare(strTestSuite, strTestSuiteName, true) == 0) && (string.Compare(strTestCase, strTestCaseName, true) == 0))
            {
                eCde = ERROR_CODE._COMPILER_NOT_THE_CURRENT_TESTCASE_SERVIING_PARA_4;
                Logger.Error("CompilerTestCaseEventImplement", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strTestSuiteName, strTestCaseName, strTestSuite, strTestCase));
                return eCde;
            }
            objTarget.XlsFileNameWithPath = ((BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId-1)).TCFilePath;
            objTarget.CurrentRunName = ((BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId-1)).TCSheetName;
            objTarget.Action4Project = ((BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId - 1)).Action;
            return eCde;
        }

#if v_useNameId
        public TCObjects getNextTCObject(long lAppId)
        {
#else
        public TCObjects getNextTCObject()
        {
#endif       
            Logger.logBegin("getNextTCObject");
            while ((miCurrentNavigateId < mlistTestSuite.Count) && (miCurrentNavigateId>=0))
            {
                BatchConfigObject objTestSuit = (BatchConfigObject)mlistTestSuite.ElementAt(miCurrentNavigateId);
                if (objTestSuit == null)
                {
                    miCurrentNavigateId++;
                    continue;
                }
                
                if ((string.Compare(objTestSuit.Action, "Run", true) == 0)||(TigerMarsUtil.RegularTest("EXE.*", objTestSuit.Action.ToUpper())))
                {
                    Logger.Info("getNextTCObject", string.Format("trying to get [{0}] parent", objTestSuit.PreParentId??"null"));
                    /** check preconditions **/
                    if (!string.IsNullOrEmpty(objTestSuit.PreParentId))
                    {
                        string[] arrParentId = objTestSuit.PreParentId.Split(new string[]{","},StringSplitOptions.RemoveEmptyEntries);
                        bool isSucessResultOrNull = false ;
                        for (int i = 0; i < arrParentId.Length; i++)
                        {
                            if (string.IsNullOrEmpty(arrParentId[i]) || string.IsNullOrWhiteSpace(arrParentId[i])) continue;
                            /** get the status of the parent status **/
                            //bool isSucessResultOrNullItem = getTSStatusById(objTestSuit.PreParentId);
                            Logger.Info("getNextTCObject", string.Format("Begin to Get RelyId [{0}] status", arrParentId[i]));
                            bool isSucessResultOrNullItem = getTSStatusById(arrParentId[i]);
                            isSucessResultOrNull = isSucessResultOrNullItem;
                            if (!isSucessResultOrNull) break;
                        }
                        if (!isSucessResultOrNull)
                        {
                            miCurrentNavigateId++;
                            continue;
                        }
                    }
                    else
                    {
                        Logger.Info("getNextTCObject", "back to normal ");
                    }

#if _Datafrom_Database
                    TCObjects objTC = CreateTCObjectByTS(objTestSuit);
#else
                    TCObjects objTC = new TCObjects();
                    objTC.XlsFileNameWithPath = objTestSuit.TCFilePath;
                    objTC.CurrentRunName = objTestSuit.TCSheetName;
                    objTC.Id4Project = objTestSuit.TestSuiteID;
                    objTC.Action4Project = objTestSuit.Action;
#endif


                    miCurrentNavigateId++;
                    return mobjCurrentTestSuite = objTC;
                }
                else
                {
                    miCurrentNavigateId++;
                    continue;
                }
            }
            Logger.logEnd("getNextTCObject");
            return null;
        }

#if _Datafrom_Database
        protected virtual TCObjects CreateTCObjectByTS(BatchConfigObject objTestSuit)
        {
            TCObjects objTC = new TCObjects();
            objTC.XlsFileNameWithPath = objTestSuit.TCFilePath;
            objTC.CurrentRunName = objTestSuit.TCSheetName;
            objTC.Id4Project = objTestSuit.TestSuiteID;
            objTC.Action4Project = objTestSuit.Action;
#if v_16AndUp
            
#endif
            return objTC;
        }
#endif
        private bool getTSStatusById(string strId)
        {
            Logger.logBegin("getTSStatusById");
            Logger.Info("getTSStatusById", string.Format("[{0}] count items to be checked", mlistTestSuite.Count));
#if _Datafrom_Database
            int iStoryBoardDetailId2Compare;
            if (!int.TryParse(strId,out iStoryBoardDetailId2Compare) )
            { iStoryBoardDetailId2Compare = -1; }
#endif
            for (int i = 0; i < mlistTestSuite.Count;i++ )
            {
                BatchConfigObject objTestSuit = (BatchConfigObject)mlistTestSuite.ElementAt(i);
                Logger.Info("getTSStatusById", string.Format("Current to cmpare TestCase:[id-[{0}]], [test sheetname-{1}],[result -{2}]", objTestSuit.TestSuiteID, objTestSuit.TCSheetName, objTestSuit.RunResult));
#if _Datafrom_Database
                BatchConfigObjectFromDB objItmFromDB = (BatchConfigObjectFromDB)objTestSuit;
                
                if (objItmFromDB.AssignedStoryObject == null) continue;
                if (objItmFromDB.AssignedStoryObject.STORYBOARD_DETAIL_ID==iStoryBoardDetailId2Compare)
                {
                    Logger.Info("getTSStatusById", "Find pre-condition test case.");
                    /// check the status of test case 
                    if (string.IsNullOrEmpty(objTestSuit.RunResult))
                    {
                        /** the parent test case should be run with success result **/
                        Logger.Error("getTSStatusById", string.Format("RunResult,rely ID is [{0}], [{1}] is not \"Success\".", strId, objTestSuit.RunResult));
                        return false;
                    }
                    if (objTestSuit.RunResult.CompareTo(cnst_SUCCESS) == 0)
                    {
                        return true;
                    }
                    Logger.Error("getTSStatusById", string.Format("RunResult with rely id [{0}] is not \"Success\", value is [{1}]", strId, objTestSuit.RunResult));
                    return false;
                }
#else
                if (string.IsNullOrEmpty(objTestSuit.TestSuiteID)) continue;

                if (string.Compare(strId, objTestSuit.TestSuiteID, true) == 0)
                {
                    if (string.IsNullOrEmpty(objTestSuit.RunResult))
                    {
                        /** the parent test case should be run with success result **/
                        Logger.Error("getTSStatusById", string.Format("RunResult,rely ID is [{0}], [{1}] is not \"Success\".", strId,objTestSuit.RunResult));
                        return false;
                    }

                    if (objTestSuit.RunResult.CompareTo(cnst_SUCCESS) == 0)
                    {
                        return true; 
                    }
                    Logger.Error("getTSStatusById", string.Format("RunResult with rely id [{0}] is not \"Success\", value is [{1}]", strId, objTestSuit.RunResult));
                    return false;
                }
#endif
            }
            Logger.Error("getTSStatusById", string.Format("No such rely id [{0}] found", strId));
            Logger.logEnd("getTSStatusById");
            return false;
        }

        protected override ConfigObjectBase mLoadDataRow2ConfigObj(DataRow objRow, int iRowId = -1, long lAppId = -1)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            if (objRow == null) return null;
            TestSuiteAdapter objTestSuiteAdp = TestSuiteAdapterFactory.GetAdapterInstance(MARS_ADAPTER._ADPTR_XLSJET_2_TESTSUITE);
            ConfigObjectBase objResult = objTestSuiteAdp.LoadTestSuiteInfo(objRow);
            Logger.logEnd("mLoadDataRow2ConfigObj");
            return objResult;
        }

        protected void BeginRunTestSuit()
        {
            Logger.logBegin("BeginRunTestSuit");
            /*** Initial a Loop ***/
            miCurrentNavigateId = 0;
            
            Logger.logEnd("BeginRunTestSuit");
        }

        protected virtual ERROR_CODE BeginRunTestSuit(string strRelyId, int iLoopId)
        {
            Logger.logBegin("BeginRunTestSuit");
            miCurrentNavigateId = 0;
            int iLpId = 0;
            while (iLpId< mlistTestSuite.Count)
            {
                //BatchConfigObject objBthObj = (BatchConfigObject)mlistTestSuite.ElementAt(i);
                BatchConfigObject objBthObj = (BatchConfigObject)mlistTestSuite.ElementAt(iLpId);
                if (objBthObj.TestSuiteID.ToUpper().CompareTo(strRelyId==null?"":strRelyId.ToUpper())==0)
                {
                    Logger.Info("BeginRunTestSuit", string.Format("Find special Rely Id and assigned test suite:[{0}]", strRelyId));
                    miCurrentNavigateId = iLpId;
                    return ERROR_CODE._NO_ERROR;
                }
                iLpId++;
            }
            Logger.Info("BeginRunTestSuit", string.Format("can't Find special Rely Id and assigned test suite:[{0}]", strRelyId));
            miCurrentNavigateId = 0;            
            Logger.logEnd("BeginRunTestSuit");
            return ERROR_CODE._BATCH_ERROR_NO_SUCH_RELYID_TESTSUITE_PARA_1;
        }

        protected void EndRuneTestSuit()
        {
            Logger.logBegin("EndRuneTestSuit");
            miCurrentNavigateId = -1;
            Logger.logEnd("EndRuneTestSuit");
            //throw new NotImplementedException();
        }

        public virtual ERROR_CODE OnNavigateHandlerImpl()
        {
            this.BeginRunTestSuit();
            return ERROR_CODE._NO_ERROR; 
        }

        public virtual ERROR_CODE OnNavigateWithRelyIdAndLoopIdHandlerImpl(string strRelyId, int iLoop)
        {
            return this.BeginRunTestSuit(strRelyId, iLoop);
        }

        internal virtual ERROR_CODE OnTestSuiteIsDoneEventImpl(TestSuiteRunStatusInfo objStatus, string strAction = "Run",bool isContinueWhenFalse=true)
        {
#if !_Datafrom_Database
            /** write a statu string back to Batch file **/
            //const string cnst_update_singlecell = "update [{0}$] set RESULT='[{2}]', ERROR_CAUSE='{3}', SCRIPT_START='[{4}]', SCRIPT_END='[{5}]' where RELY='{1}'";
            const string cnst_update_singlecell = "update [{0}$] set F5='[{2}]', F6='{3}', F7='[{4}]', F8='[{5}]' where F2='{1}'";
            const string cnst_update_singlecell_ex = "update [{0}$] set F5='[{2}]', F6='{3}', F7='[{4}]', F8='[{5}]', F1='{6}' where F2='{1}'";
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (mobjCurrentTestSuite==null) return eCde ;

            string strTestSuiteName = mobjCurrentTestSuite.XlsFileNameWithPath, 
                strTestCaseName=mobjCurrentTestSuite.CurrentRunName ;

            string iCurrentId = ((BatchConfigObject)this.mlistTestSuite[(this.miCurrentNavigateId-1)>0?this.miCurrentNavigateId-1:0]).TestSuiteID;
            
            bool isSuccess = false ;
            if (!(isSuccess = !(string.Compare(cnst_SUCCESS, objStatus.RunResult, true) != 0)))
            {
                Logger.Error("OnTestSuiteIsDoneEventImpl", string.Format("Result from Client is [{0}], Test Ends.", objStatus.RunResult));
                if (!isContinueWhenFalse)
                    EndRuneTestSuit();
            }
                

            GetWritableConnectionWithoutHead();
            string strReason = string.IsNullOrEmpty(objStatus.CauseReason) ? "" : objStatus.CauseReason.Replace("'", "''");
            string strUpdate = "", strNewAction=null;
            /** comment for version 1.1 
            * 1, if the initialiaztion action is run, then action changes to  done if isSuccess = true, else, no change
            * 2, if the initialiaztion action is exe then, only change result 
            * **/
            if (string.Compare("Run", strAction, true) == 0)
            {
                if (isSuccess)
                {
                    strNewAction = "Done";
                    strUpdate = string.Format(cnst_update_singlecell_ex, string.IsNullOrEmpty(CurrentTestProjectNameID) ? CNST_EXECUTE_TABLENAME : CurrentTestProjectNameID, iCurrentId, objStatus.RunResult, strReason, objStatus.StartTime, objStatus.EndTime, strNewAction);
                }
                else
                    strUpdate = string.Format(cnst_update_singlecell, string.IsNullOrEmpty(CurrentTestProjectNameID) ? CNST_EXECUTE_TABLENAME : CurrentTestProjectNameID, iCurrentId, objStatus.RunResult, strReason, objStatus.StartTime, objStatus.EndTime);
            }else
            {
                strUpdate = string.Format(cnst_update_singlecell, string.IsNullOrEmpty(CurrentTestProjectNameID) ? CNST_EXECUTE_TABLENAME : CurrentTestProjectNameID, iCurrentId, objStatus.RunResult, strReason, objStatus.StartTime, objStatus.EndTime);
            }
                
            Logger.Info("OnTestSuiteIsDoneEventImpl", string.Format("UPdateSQL: [{0}]", strUpdate));
            eCde =this.RunNoneQuerySql(strUpdate);

            /** update catch **/
            if (eCde == ERROR_CODE._NO_ERROR)
            {
                Logger.Info("----updateTestSuiteStatusCatch----", string.Format("parameters:{0}, {1}", iCurrentId, objStatus.RunResult));
                updateTestSuiteStatusCatch(iCurrentId, objStatus.RunResult, strNewAction);
            }
            else
            {
                Logger.Error("OnTestSuiteIsDoneEventImpl", string.Format("Returned value after calling RunNoneQuerySql is [{0}]", eCde));
            }
            this.RecoveryReadConnection();

            return eCde;
#else
            return ERROR_CODE._NO_ERROR;
#endif
        }

        private void updateTestSuiteStatusCatch(string iCurrentId, string strResult, string strNewAction)
        {
            Logger.logBegin("updateTestSuiteStatusCatch");
            for (int i = 0; i < this.mlistTestSuite.Count; i++)
            {
                BatchConfigObject objBthObj = (BatchConfigObject)mlistTestSuite.ElementAt(i);
                if (string.Compare(objBthObj.TestSuiteID, iCurrentId, true) == 0)
                {
                    objBthObj.RunResult = strResult;
                    if (strNewAction!=null)
                    {
                        objBthObj.Action = strNewAction;
                    }
                    Logger.Info("updateTestSuiteStatusCatch" ,string.Format("Changed object relyid=[{0}] catch to [{1}] ", iCurrentId, strResult));
                    return;
                }
            }
            Logger.logEnd("updateTestSuiteStatusCatch");
        }


    }
}
