extern alias clientWCF;
using client= clientWCF::Route2NSEx.src.Marquis.systemUtil;

using Mars.message.Business;
using Mars.message.DataLayer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.Mars.Constants;
using clientWCF::MarsTestFrame.CommuniteServer;
using System.IO;
using Mars.message.AutoTestingDriver.SystemUtil.DataStructure;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.AutoTestingDriver.ExecuteTestcase;

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

        private static client.MLogger Logger = client.MLogger.GetLogger(typeof(StoryboardDBRecordMgr));
        private string _currentTestStoryboardId;
        private bool _isBaseLineTest;
        private bool _isContinueToTest = false;
        private B_PROJ_TEST_RESULT storyBoardTestResult = new B_PROJ_TEST_RESULT();
        private long _currentTestMarkGroupId = -1;
        private int miCurrentLoop = -1;

        private string currrentDBIdx = MarsEntitiesExtends.cnst_default_dbName;
        public string getCurrentDBIdx()
        {
            return currrentDBIdx;
        }

        public long currentApplication_id { get; set; }

        public StoryboardDBRecordMgr(string strDBIdx)
        {
            currrentDBIdx = strDBIdx;
        }

        public int CurrentLoop
        {
            get { return miCurrentLoop; }
        }
        public int BaseLineTestId
        {
            get { return (_isBaseLineTest ? 1 : 0); }
        }

        public byte[] GetFileToBytes(string strFileName, ref bool isOk, ref string strError)
        {
            FileStream objR = null;
            try
            {
                objR = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
                byte[] arrD = new byte[objR.Length];
                int iLen = objR.Read(arrD, 0, (int)objR.Length);
                isOk = true;
                return arrD;
            }
            catch (Exception  e)
            {
                isOk = false;
                Logger.Error("GetFileToBytes",strError = string.Format("Exception :[{0}]", e.Message),e );
                return null;
            }
        }

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
                        CreateNewTestMarkGroupId(currrentDBIdx);
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

        internal int currentTestStoryboardIdAsInt {
            get {
                int lSbId;
                if (!int.TryParse(_currentTestStoryboardId, out lSbId))
                    lSbId = -1;
                return lSbId;
            }
        }
        public string currentTestStoryboardId
        {
            get { return _currentTestStoryboardId; }
            set {
                _currentTestStoryboardId = value;
                
            }
        }

        internal void Initialization(string strStoryBoardId, bool isBaseLine,
            bool isContinue2Test, 
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("Initialization", string.Format("StoryBoardId:[{0}] isBaseLine:[{1}] isContinue2Test:[{2}]", strStoryBoardId, isBaseLine, isContinue2Test));
            _currentTestStoryboardId = strStoryBoardId;
            _isBaseLineTest = isBaseLine;
            _isContinueToTest = isContinue2Test;
            ///set the latest_test_mark_ID
            /// 
            CreateNewTestMarkGroupId(strDBIdx);
        }

        internal void CreateNewTestMarkGroupId(string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            int iMark = new Random().Next(10000);
#if _forWebClient
            Logger.logBegin("CreateNewTestMarkGroupId", $"{iMark}|db|{strDBIdx}|{this._currentTestStoryboardId}");
            MarsRESTfulApiClient webClient = new MarsRESTfulApiClient(strDBIdx);
            string strError = "";
            bool isOk = false;
            if (!_isContinueToTest)
                _currentTestMarkGroupId = webClient.GetLastestTestMarkID(null,ref isOk, ref strError);
            else
            {
                long l_currentStoryBoardId = -1;
                if (long.TryParse(this._currentTestStoryboardId, out l_currentStoryBoardId))
                {
                    _currentTestMarkGroupId = webClient.GetLastestTestMarkID(l_currentStoryBoardId, ref isOk, ref strError);
                }
                else
                {
                    Logger.Warnning("CreateNewTestMarkGroupId", string.Format("Can't convert storyboard id [{0}] to long, new LasterTestMarkId is fetched", _currentTestStoryboardId));
                    _currentTestMarkGroupId = webClient.GetLastestTestMarkID(null, ref isOk, ref strError);
                }

            }
#else

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
#endif
            Logger.logEnd("InitializeTestReportResult", $"{iMark}|_currentTestStoryboardId|{_currentTestStoryboardId}|MarkGroundId|{_currentTestMarkGroupId}|{isOk}|{strError}|");
        }

        internal void InitializeTestReportResult(long? testCaseKeyId,long currentStoryBoardDetail_id, 
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
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
#if !_forWebClient
                int iError = BoHelper.SaveStoryBoardTestResult(storyBoardTestResult, ref strError, strDBIdx);
#else
                bool isOk = false;
                int iError = (new MarsRESTfulApiClient(strDBIdx)).SaveStoryBoardTestResult(storyBoardTestResult,ref isOk,  
                    ref strError);
#endif
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
            finally
            {
                Logger.logEnd("InitializeTestReportResult");
            }
        }

        internal void UpdateTestReportResult(int idSuccess, string strErrorInfo)
        {
            Logger.logBegin("UpdateTestReportResult", string.Format("idSuccess:[{0}], ErrorInfo:[{1}]", idSuccess, strErrorInfo));
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
                int iError = BoHelper.UpdateStoryBoardTestResult(storyBoardTestResult, ref strDBError, currrentDBIdx);
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
            finally
            {
                Logger.logEnd("UpdateTestReportResult");
            }
        }

        internal int CreateStepLog(long stepId, TestStepRunningRecorder objCurrentStepLog, string strInputValueSetting)
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
                objStepRpt.END_TIME = objCurrentStepLog.EndTime;
                objStepRpt.INPUT_VALUE_SETTING = strInputValueSetting;

                if (objTestReport == null )
                {
#if _MarsCDriver
                    Logger.Error("CreateStepLog", string.Format("Can't find Test_report id by loop[{0}]", objCurrentStepLog.LoopId));

                    return -1;
#else
                    Logger.Error("CreateStepLog", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), objCurrentStepLog.LoopId));

                    return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1 ;
#endif
                }
                objStepRpt.TEST_REPORT_ID = objTestReport.TEST_REPORT_ID;
                objStepRpt.RUNNING_RESULT = (int)MARSE_TESETRESULT.E_INITIALIZATION;
                string strErrror = "", strAdv = "";
                int  iError = objStepRpt.CreateIdAndSave(ref strErrror,ref strAdv,this.currrentDBIdx);
                if (iError<0)
                {
#if _MarsCDriver
                    Logger.Error("CreateStepLog", string.Format("Error when Try to save Test step log  :{0}", strErrror));
                    return iError;

#else
                    Logger.Error("CreateStepLog", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_REPORT_STEPS_CANT_SAVE_PARA_1),strErrror));
                    return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_REPORT_STEPS_CANT_SAVE_PARA_1;
#endif
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
#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("updateDataField", string.Format("Can't find report for loop:[{0}]", iLoopId));
                return eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("updateDataField",string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return eCde;
#endif
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(stepId);
            if (objTestStepLog==null)
            {
#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("updateDataField", string.Format("Can't get this test step information :[{0}]", stepId));
                return eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("updateDataField",string.Format(ERROR_INFO.GET_ERROR_STR(eCde), stepId));
                return eCde;
#endif
            }
            objTestStepLog.INPUT_VALUE_SETTING = strData;

            return ERROR_CODE._NO_ERROR;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iLoopId"></param>
        /// <param name="assignedTestStepId"></param>
        /// <param name="idSuccess"></param>
        /// <param name="resultInfo">SUCCESS OR ERROR</param>
        /// <param name="strActualInput"></param>
        /// <param name="strDataReturned"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStackInfo"></param>
        /// <returns></returns>
        internal int UpdateCurrentTestStepResultForAPI(int iLoopId, long assignedTestStepId,
            int idSuccess, string resultInfo,
            string strActualInput,
            string strDataReturned,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName,
            string strAdv = "",
            string strStackInfo = ""
            )
        {
            Logger.Info("UpdateCurrentTestStepResult", $"stepid:{assignedTestStepId}|idSuccess:{idSuccess}|dataReturned:{strDataReturned}|actualInput:{strActualInput}");
            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {

                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Can't find report for loop:[{0}]", iLoopId));
                return (int)eCde;
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);
            if (objTestStepLog == null)
            {

                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Can't find TEST_REPORT_STEPS for teststp id:[{0}]", assignedTestStepId));
                return (int)eCde;
            }

            objTestStepLog.RUNNING_RESULT = (short)idSuccess;
            objTestStepLog.END_TIME = DateTime.Now;
            objTestStepLog.RUNNING_RESULT_INFO = resultInfo;
            objTestStepLog.RETURN_VALUES = string.IsNullOrEmpty(strDataReturned)?"": strDataReturned.Length>256? strDataReturned.Substring(0,256):strDataReturned;
            objTestStepLog.ACTUAL_INPUT_DATA = string.IsNullOrEmpty(strActualInput) ? "" : strActualInput.Length > 120 ? strActualInput.Substring(0, 120) : strActualInput;
            objTestStepLog.ADVICE = strAdv;
            objTestStepLog.STACKINFO = strStackInfo;
            try
            {
                return objTestStepLog.updateRecord(strDBIdx);
            }
            catch (Exception e)
            {
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
        }

        internal int UpdateCurrentTestStepResult(int iLoopId,long assignedTestStepId, 
            int idSuccess, string strError,
            string strActualInput,
            byte[] arrPicInfo,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName,
            string strAdv = "",
            string strStackInfo =""
            )
        {
            Logger.Info("UpdateCurrentTestStepResult", string.Format("TestStepId:[{0}], idSuccess:[{1}], strError:[{2}], LoopId:[{3}] picInfo len:[{4}]",
                assignedTestStepId, idSuccess, strError, iLoopId,arrPicInfo==null?0:arrPicInfo.Length));
            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {
                
#if _MarsCDriver
                    eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                    Logger.Error("UpdateCurrentTestStepResult", string.Format("Can't find report for loop:[{0}]", iLoopId));
                    return (int)eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("UpdateCurrentTestStepResult", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return (int)eCde;
#endif
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);            
            if (objTestStepLog == null)
            {
                    
#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Can't find TEST_REPORT_STEPS for teststp id:[{0}]", assignedTestStepId));
                return (int)eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("UpdateCurrentTestStepResult", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), assignedTestStepId));
                return (int)eCde;
#endif
            }

            objTestStepLog.RUNNING_RESULT = (short)idSuccess;
            objTestStepLog.END_TIME = DateTime.Now;
            objTestStepLog.RUNNING_RESULT_INFO = strError;
            objTestStepLog.INFO_PIC = arrPicInfo;
            objTestStepLog.ACTUAL_INPUT_DATA = strActualInput;
            objTestStepLog.ADVICE = strAdv;
            objTestStepLog.STACKINFO = strStackInfo;

            Logger.Info("----test byte[] Len----",string.Format("copied data lenth:[{0}]", objTestStepLog.INFO_PIC==null?-1: objTestStepLog.INFO_PIC.Length));
            try
            {
                return objTestStepLog.updateRecord(strDBIdx);
            }
            catch (Exception e)
            {
                Logger.Error("UpdateCurrentTestStepResult", string.Format("Exception:[{0}]",e.Message),e);
                return -1;
            }            
        }

        internal bool StoreData4ForTestCompareByKeyReport_Steps(string strObjectNameIdx, 
            string strData2Store, int iLoopId, 
            long? testCaseKeyId, long assignedTestStepId,
            short iSuccessId,
            string strReturnResult, //"SUCCESS OR ERROR INFO"
            ref string strError,
            string strDBIdx
            )
        {
            Logger.Info("StoreData4ForTestCompareByKeyReport_Steps", string.Format("Trying to save data, objName:[{0}], data2Store:[{1}], Loop:[{2}], testcaseId:[{3}], TestStepId:[{4}]",
                strObjectNameIdx, strData2Store, iLoopId, testCaseKeyId, assignedTestStepId));
            /// ËùÒÔ¸ñÊ½ÊÇ£ºObjectName_[Key1]_[Key2]....[KeyN][::]value\r\n
            /// 
            string[] arrData = strData2Store.Split(new string[] { "\r\n"},StringSplitOptions.None);
            List<KeyValuePair<string, string>> lstObjectNameAndValues = new List<KeyValuePair<string, string>>();
            bool isOk = false ;
            foreach (var itm in arrData)
            {
                if (itm == null) continue;
                MarsKeyValues<string, string> capturedDataWithKey = MarsKeyValues<string,string>.ConvertFromStringBySplitter(itm, new string[] { "[::]" }, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("StoreData4ForTestCompareByKeyReport_Steps", strError);
                    return false;
                }
                lstObjectNameAndValues.Add(new KeyValuePair<string, string>(capturedDataWithKey.MKey, capturedDataWithKey.MValue));
            }

            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {

#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format("Can't find TEST_REPORT for loop id:[{0}]", iLoopId));
                return false;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return false;
#endif
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);
            if (objTestStepLog == null)
            {
#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format("Can't find B_TEST_REPORT_STEPS for step id:[{0}]", assignedTestStepId));
                return false;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), assignedTestStepId));
                return (int)eCde;
#endif
            }
            ///ÏÈÖðÌõ´´½¨½á¹û£¬È»ºóºÍ×îÐÂµÄÆäËûµÄ¼ÇÂ¼±È½Ï ÊÇ·ñÆ¥Åä£¬Èç¹ûÆ¥Åä£¬¾Í±£³Ötest storyboard result£¬·ñÔòÐÞ¸ÄÎªpartial
            ///
            if (objTestStepLog.END_TIME == null)
                objTestStepLog.END_TIME = DateTime.Now;
            isOk = B_TEST_REPORT_STEPS.InsertTestStepResultForKeyCompare(objTestStepLog.TEST_REPORT_ID, objTestStepLog.STEPS_ID,
                objTestStepLog.BEGIN_TIME,
                objTestStepLog.END_TIME,
                iSuccessId, lstObjectNameAndValues,
                objTestStepLog.DATA_SUMMARY_ID,
                strObjectNameIdx, strReturnResult,
                ref strError,
                strDBIdx);
            
            Logger.logEnd("StoreData4ForTestCompareByKeyReport_Steps");
            return false;

        }

        internal int StoreData4ForTestReport_Steps(string strObjectNameIdx, string strData2Store, int iLoopId, long? testCaseKeyId, 
            long assignedTestStepId, string strFuncOfDataDealing, 
            ref bool isOk, ref string strError,
            string strDBIdx)
        {
            Logger.Info("StoreData4ForTestReport_Steps",string.Format("Trying to save data, objName:[{0}], data2Store:[{1}], Loop:[{2}], testcaseId:[{3}], TestStepId:[{4}]",
                strObjectNameIdx,strData2Store, iLoopId, testCaseKeyId,assignedTestStepId));

            /// 算法（对captureValue和CaptureAndCompare而言）：
            /// 1、如果只有一个值，则只写单一记录
            /// 2、如果有多个值，第一值写到单一记录中，然后复制该记录（除id外），将其他值依次写入其他记录
            /// 
            /// notice：
            /// 如果采用新的ObjectName，那么从INPUT_VALUE_SETTING获取，同时赋值；如果没有，则将ObjectName复制过来
            /// RUNNING_RESULT set to 3 that means data
            /// 

            B_TEST_REPORT objTestReport = GetTestReportObjByLoopId(iLoopId);
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objTestReport == null)
            {

#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("StoreData4ForTestReport_Steps", strError = string.Format("Can't find TEST_REPORT for loop id:[{0}]", iLoopId));
                isOk = false;
                return (int)eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return (int)eCde;
#endif
            }
            B_TEST_REPORT_STEPS objTestStepLog = objTestReport.GetTestRptStepsByTestStepId(assignedTestStepId);
            if (objTestStepLog == null)
            {
#if _MarsCDriver
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("StoreData4ForTestReport_Steps", strError = string.Format("Can't find B_TEST_REPORT_STEPS for step id:[{0}]", assignedTestStepId));
                isOk = false;
                return (int)eCde;
#else
                eCde = ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_STEP_LOG_INFO_PARA_1;
                Logger.Error("StoreData4ForTestReport_Steps", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), assignedTestStepId));
                return (int)eCde;
#endif
            }

            string strCurrentObjName = strObjectNameIdx;
            if (string.IsNullOrEmpty(objTestStepLog.INPUT_VALUE_SETTING))
            {
                objTestStepLog.INPUT_VALUE_SETTING = strCurrentObjName;
            }

            if (string.IsNullOrEmpty(strData2Store))
            {
                Logger.Info("StoreData4ForTestReport_Steps", "NoData Need save to Database, as captured data is [null or empty]");
                isOk = true;
                return (int)ERROR_CODE._NO_ERROR;
            }
            string[] arrValue = strData2Store.Split('\n');
            MarsCompareDataDealingFunction instance = MarsCompareDataDealingFunction.GetInstance(strFuncOfDataDealing);
            // deal with the first Value
            string strCurValue = arrValue[0];
            //B_TEST_REPORT_STEPS
            if (instance != null)
            {
                objTestStepLog.RETURN_VALUES = instance.dealOneRow(strCurValue, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("StoreData4ForTestReport_Steps", strError);
                    return -1;
                }
            }
            else
            {
                objTestStepLog.RETURN_VALUES = strCurValue;
            }
            List<B_TEST_REPORT_STEPS> lstTestStepReport = new List<B_TEST_REPORT_STEPS>();
            //lstTestStepReport.Add(objTestStepLog);
            /// start from the second 
            /// 
            
            for (int i=1;i<arrValue.Length;i++)
            {
                B_TEST_REPORT_STEPS objStps = new B_TEST_REPORT_STEPS();
                objStps = objTestStepLog.CloneSelf();
                if (instance == null)
                {
                    objStps.RETURN_VALUES = arrValue[i];
                }
                else
                {
                    objStps.RETURN_VALUES = instance.dealOneRow(arrValue[i], ref isOk, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("StoreData4ForTestReport_Steps",strError);
                        return -1;
                    }
                }                
                
                objStps.RUNNING_RESULT = 3;
                lstTestStepReport.Add(objStps);
            }
            Logger.Info("\t", string.Format("len after split:[{0}] - list count:[{1}]", arrValue.Length, lstTestStepReport.Count));

            int iResult = B_TEST_REPORT_STEPS.UpdateAndInsertList(objTestStepLog,lstTestStepReport, strCurrentObjName, strDBIdx);
            return iResult;
        }



        private B_TEST_REPORT GetTestReportObjByLoopId(int loopId)
        {
            Logger.Info("GetTestReportObjByLoopId",string.Format("loopId:[{0}] this.currentTestCaseLog:[{1}]", loopId, 
                this.currentTestCaseLog==null?"null": this.currentTestCaseLog.ToString()));
            B_TEST_REPORT objRpt = null;
            if (!this.currentTestCaseLog.TryGetValue(loopId, out objRpt))
            {
                return null;
            }
            
            return objRpt;
        }

        internal int LogTestCaseStart(long? testCaseKeyId, int iLoop, //long application_id,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("LogTestCaseStart",string.Format("TestCaseId :[{0}], iLoop:[{1}], dbIdx:[{2}]", 
                testCaseKeyId, iLoop, strDBIdx));
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
                objTestCaseLog.APPLICATION_ID = this.currentApplication_id;
                int iErrorId = objTestCaseLog.Create2Entities(ref strError, strDBIdx);
                if (iErrorId<0)
                {
#if _MarsCDriver
                    //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                    Logger.Error("LogTestCaseStart", string.Format("Can't create entity TEst_report with error:[{0}]", strError));
                    return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                    Logger.Error("LogTestCaseStart",string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1), strError));
                    return (int)ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1;
#endif
                }
                else
                {
                    currentTestCaseLog.Add(iLoop, objTestCaseLog);
                }
                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
#if _MarsCDriver
                //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("LogTestCaseStart", string.Format("Exception :{0}\r\n stacktrace:{1}", e.Message,e.StackTrace));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                  
                Logger.Error("LogTestCaseStart",string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1),e.Message),e );
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
#endif
            }
            finally
            {
                Logger.logEnd("LogTestCaseStart");
            }
        }

        /// <summary>
        /// iLoopId Ä¿Ç°Ö»ÊÇ0
        /// </summary>
        /// <param name="iLoopId"></param>
        /// <param name="restoredFrom"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <returns></returns>
        internal int PrepareReRunFrom(int iLoopId, ExecutableTestCaseStep restoredFrom, string strDBIdx, 
            ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
        {
            Logger.logBegin("PrepareReRunFrom", $"step info :[{restoredFrom?.RunId}]");
            try
            {
                /// Ëã·¨£º
                /// 1£¬»ñµÃ test_report_id
                /// 2, µ÷ÓÃrestful api, 
                ///   2.1, »ñµÃrun order
                ///   2.2, É¾³ýt_test_reportËùÓÐµÄrunord´óÓÚµÈÓÚÕâ¸öµÄ¼ÇÂ¼£¬ÒÀ¾Ýtest_report_id
                ///   
                B_TEST_REPORT objCurrentLoopRecord = null;
                if (!currentTestCaseLog.TryGetValue(iLoopId, out objCurrentLoopRecord))
                {
                    Logger.Error("PrepareReRunFrom", string.Format("no test report record is found [{0}]", iLoopId));
                    return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                }
                MarsRESTfulApiClient webClient = new MarsRESTfulApiClient(strDBIdx);
                int updatedCnt = webClient.deleteTestStepRecordsForReRun(restoredFrom.TestStepId, objCurrentLoopRecord.TEST_REPORT_ID, ref strError,
                    ref strStack, ref strAdv);
                if (updatedCnt<0)
                {
                    isOk = false;
                    return updatedCnt;
                }
                isOk = true;
                return (int)ERROR_CODE._NO_ERROR;
            }catch(Exception e)
            {
                Logger.Error("PrepareReRunFrom", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return (int)ERROR_CODE._ERROR_UNKNOWN;
            }
            finally
            {
                Logger.logEnd("PrepareReRunFrom");
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
#if _MarsCDriver
                //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateTestCaseLoopResult", string.Format("iLoop is not a bool [{0}]", iLoopId));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), iLoopId));
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1;
#endif
            }
            try
            {
                objCurrentLoopRecord.END_TIME = DateTime.Now;
                objCurrentLoopRecord.RUNNING_RESULT = (short)iResultId;
                objCurrentLoopRecord.RUNNING_RESULT_INFO = strEndInfo;
                objCurrentLoopRecord.APPLICATION_ID = this.currentApplication_id;// app_id; 

                int iError = objCurrentLoopRecord.updateById( ref strError, currrentDBIdx);
                if (iError<0)
                {
#if _MarsCDriver
                    //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                    //Logger.Error("UpdateTestCaseLoopResult", string.Format("Exception :{0}\r\n stacktrace:{1}", e.Message, e.StackTrace));
                    return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                    Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1), strError));
                    return (int)ERROR_CODE._ENTITY_ERROR_SAVE_PARA_1;
#endif
                }
                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
#if _MarsCDriver
                //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateTestCaseLoopResult", string.Format("Exception :{0}\r\n stacktrace:{1}", e.Message, e.StackTrace));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else

                Logger.Error("UpdateTestCaseLoopResult", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), e.Message), e);
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
#endif
            }
            finally
            {
                Logger.logEnd("UpdateTestCaseLoopResult");
            }
        }

        internal int UpdateTestCaseLoopId(int iPos, int iLoopId)
        {
            Logger.Info("UpdateTestCaseLoopId", string.Format("iPos:[{0}], iLoopId:[{1}]", iPos, iLoopId));
            if (!currentTestCaseLog.ContainsKey(iPos))
            {
#if _MarsCDriver
                //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateTestCaseLoopId", string.Format("No such Test Case Exists [{0}]", iPos));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1), iLoopId));
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_NO_TEST_REPORT_PARA_1;
#endif
            }
            B_TEST_REPORT objTarget;
            try
            {
                if (currentTestCaseLog.TryGetValue(iPos, out objTarget))
                {
                    objTarget.LOOP_ID = iLoopId;
                    int iError= objTarget.update(currrentDBIdx);
                    if (iError < 0)
                    {
#if _MarsCDriver
                        //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                        Logger.Error("UpdateTestCaseLoopId", string.Format("No such Test Case Exists [{0}]", iPos));
                        return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                        Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), "UpdateTestCaseLoopId"));
                        return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
#endif
                    }
                    return (int)ERROR_CODE._NO_ERROR;
                }
#if _MarsCDriver
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                return (int)ERROR_CODE._STORYBOARD_ERROR_TEST_CANT_GET_FROM_DICTIONARY_PARA_1;
#endif
            }
            catch (Exception e)
            {
#if _MarsCDriver
                //eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("UpdateTestCaseLoopId", string.Format("Exception :{0}\r\n stacktrace:{1}", e.Message, e.StackTrace));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
#else
                Logger.Error("UpdateTestCaseLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1), e.Message), e);
                return (int)ERROR_CODE._SERVICE_ERROR_UNKOWN_EXCETPION_TEST_REPORT_1;
#endif
            }
        }

        internal int getVariableType(string strType)
        {
            if (string.Compare(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL , strType, true) == 0)
                return 4;
            if (string.Compare(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, strType, true) == 0)
                return 1;
            if (string.Compare(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOCAL , strType, true) == 0)
                return 2;
            if (string.Compare(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_IF    , strType, true) == 0)
                return 8;
            if (string.Compare(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP  , strType, true) == 0)
                return 16;
            if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_ITERATION, strType, true) == 0) 
                return 32;
            return -1;
        }
        
        internal bool updateVariableValue(string strObjectNameIdx, string strData2Store, int iLoop,int iVariableType,
            ref string strError,string strBaseLineMode= "Build",
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
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
                        /// 2019 1 30 ÐÂÔö
                        /// Èç¹û globe var²»´æÔÚ£¬ÐèÒª´´½¨
                        /// 
                        
                    case 2://local var
                    case 6:
                    case 8://IF Var
#if _MarsCDriver

                        lstObj = objLook.GetSystemLookup(strTableName = (iVariableType == 1 ? "GLOBAL_VAR" :
                                            (iVariableType == 2 ? "LOCAL_VAR" : "LOOP_VAR")),
                                            strObjectNameIdx,
                                            strDBIdx);
#else
                        lstObj = objLook.GetSystemLookup(strTableName = iVariableType == 1 ? SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL : 
                            (iVariableType==2?SystemConstant.CNST_RESERVED_VARIABLE_LOCAL : SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP), 
                            strObjectNameIdx,
                            strDBIdx) ;
#endif
                        iscreateNew = lstObj == null ? true : lstObj.Count > 0 ? false : true;
                        if (iscreateNew)
                        {
                            objLook.DISPLAY_NAME = strData2Store;
                            objLook.FIELD_NAME = strObjectNameIdx;
                            objLook.VALUE = 1;
                            objLook.STATUS = 1;
                            objLook.TABLE_NAME = strTableName;
                            isRight = objLook.InsertSelf(strDBIdx: strDBIdx,ref strError);
                        }
                        else
                        {
                            lstObj = ((lstObj.OrderBy(p => p.DISPLAY_NAME)).ToList());
                            isRight = lstObj[0].updateSelf( strData2Store, ref strError, strDBIdx);
                        }
                        return isRight;
                    case 4://modal var
#if _MarsCDriver

                        int iStatus = ((string.Compare(strBaseLineMode, "Build", true) == 0)||(string.Compare(strBaseLineMode, "Base", true) == 0)) ? 1 : 2;
                        lstObj = objLook.GetSystemLookup("MODAL_VAR", strObjectNameIdx, iStatus, ref strError, strDBIdx);
                        iscreateNew = lstObj == null ? true : lstObj.Count > 0 ? false : true;
                        //Logger.Info("updateVariableValue", string.Format("");
                        if (iscreateNew)
                        {
                            objLook.DISPLAY_NAME = strData2Store;
                            objLook.FIELD_NAME = strObjectNameIdx;
                            objLook.VALUE = 1;
                            objLook.STATUS = (short)iStatus;
                            objLook.TABLE_NAME = "MODAL_VAR";

                            isRight = objLook.InsertSelfWithStatus(ref strError, strDBIdx:this.currrentDBIdx);
                        }
                        else
                        {
                            lstObj = ((lstObj.OrderBy(p => p.DISPLAY_NAME).ThenBy(p => p.STATUS)).ToList());
                            isRight = lstObj[0].updateSelf(strData2Store, ref strError, strDBIdx);
                        }
#else
                        int iStatus = string.Compare(strBaseLineMode,SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, true) == 0 ? 1 : 2;                        
                        lstObj = objLook.GetSystemLookup(SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strObjectNameIdx, iStatus, ref strError);
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
                            isRight = lstObj[0].updateSelf(strData2Store, ref strError);
                        }
#endif
                        return isRight;

                    case 16://loop var
                        isRight = objLook.CreateOrUpdateLoopVar(strObjectNameIdx, strData2Store,ref strError, strDBIdx);
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
