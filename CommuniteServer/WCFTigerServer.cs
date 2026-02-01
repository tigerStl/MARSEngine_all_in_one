using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;

using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using MarsTestFrame.SourceCode.com.Mars.TCDataSource;
using MarsTestFrame.SourceCode.com.Mars.KeyWords;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject;
using com.Mars.TestFrame.Application;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.com.Mars.Compiler;
using System.Data.OleDb;
using System.ServiceModel.Dispatcher;
using MarsTestFrame.SourceCode.com.Mars.BusinessLogic;
using MarsTestFrame.SourceCode.com.Mars.Excels.DB;
using System.Threading;
using MarsTestFrame.systemUtil;
using Mars.DataLayer;
using Mars.plugins.standards;
using MarsTestFrame.SourceCode.com.Mars.KeyWords.loop;
using Mars.Business;
using Mars.Dto;
using com.BasicData;
using System.Xml;
using com.Mars.AutocheckError;
using MarsTestFrame.SourceCode.xmlConfig;
#if _Datafrom_Database
using MarsTestFrame.plugins;
#endif

namespace MarsTestFrame.CommuniteServer
{
    #region events begin
    public delegate int CompilerCurrentTestCaseEvent(string strCurrentTestSuiteName, string strCurrentTestCaseName);
    public delegate bool OnRequireAutoGen_CurrentPegInfoEvent(ref string strPegWindowInfo, ref string strErrorInfo);
    public delegate bool OnRequireAutoGen_OneTestStepEvent(string strSwfName, string strType, string strTxt, ref string strErrorInfo);

    public delegate void OnTestCaseFinishedCallBack(long lStoryBoardDetailId);

    public delegate void OnShutdownServer();

    public delegate bool OnIsSkipStepWord(string word);

    public delegate bool OnIsVariable(string word);

    public delegate string OnGetVariableValue(string strDBIdx,string variable);


    #endregion //events begin
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class MarsTestFrameCommuniteServer : IMarsTigerFrameWorkService
    {

        #region Logger
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTestFrameCommuniteServer));
        #endregion
        private static long miServerInstanceId = 0;
        #region Member
        private int miCurrentStepId = 0;
        private List<ConfigObjectBase> mlstSteps = null;
#if _Datafrom_Database
        private TestStep4Services mobjCurrentTestStep = null;
        private PluginsMgr mobjPlugins = new PluginsMgr();
#endif
        private ERROR_CODE meCde_LastError;
        private ServiceHost mobjHost = null;
        private int miCurrentLoopCnt = -1;
        /** Get current Data File **/
        private TCObjects mobjCurrentTestSuite = null;
        private int miCurrentTestCaseId = -1;
        private string mstrCurrrentApplicationShortName = "";
        #endregion //Member

        #region current testcase compiler error information
        private string mstrCurrentErrorInformation;
        private TestStep mobjCurrentErrorStep;
        private int miCurrentErrorId;
        #endregion //current testcase compiler error information

        #region properties
        public string CurrentTestCaseName { get { return mobjCurrentTestSuite == null ? "" : (mobjCurrentTestSuite.CurrentRunName == null ? "" : mobjCurrentTestSuite.CurrentRunName); } }
        public string CurrentTestSuiteName { get { return mobjCurrentTestSuite == null ? "" : (mobjCurrentTestSuite.XlsFileNameWithPath == null ? "" : mobjCurrentTestSuite.XlsFileNameWithPath); } }
#if v_16AndUp
        public string CurrentDataSetAlias { get { return mobjCurrentTestSuite == null ? "" : (mobjCurrentTestSuite.CurrentDatasetName ?? ""); } }
#endif
        public int CurrentLoopCount { get { return this.miCurrentLoopCnt; } set { this.miCurrentLoopCnt = value; } }
        public int CurrentTestCaseId { get { return this.miCurrentTestCaseId; } set { this.miCurrentTestCaseId = value; } }
        public string CurrentTestApplicationShortName { get { return this.mstrCurrrentApplicationShortName; } set { this.mstrCurrrentApplicationShortName = value; } }
#if v_useNameId
        private long currentApplicationId;
        public long CurrentApplicationId
        {
            get { return currentApplicationId; }
            set { currentApplicationId = value; }
        }
#endif

        #region Loop block
        private LoopKeywordMgr mobjLoopMgr = new LoopKeywordMgr();
        #endregion

        private bool _isRunningInBatchMode = false;
        public bool IsRunningInBatchMode
        {
            get
            {
                return _isRunningInBatchMode;
            }

            set
            {
                _isRunningInBatchMode = value;
            }
        }

        private static List<string> KEYWORDS_REQUIRE_AUTOERROR_CHECK = null;
        public List<string> GetKeywordsCanAutoCheckError()
        {
            try
            {
                if (KEYWORDS_REQUIRE_AUTOERROR_CHECK != null) return KEYWORDS_REQUIRE_AUTOERROR_CHECK;

                string strKeywordsWithComma = AppConfigReader.GetAutoCheckKeyword();
                if (!string.IsNullOrEmpty(strKeywordsWithComma))
                {
                    string[] arrKeywords = strKeywordsWithComma.Split(';');
                    if (arrKeywords == null) return null;
                    return KEYWORDS_REQUIRE_AUTOERROR_CHECK = new List<string>(arrKeywords);
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Logger.Error("GetKeywordsCanAutoCheckError",string.Format("Exception:{0}",e.Message),e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetKeywordsCanAutoCheckError");
            }
        }

        #endregion


        public static string currentDBIdx = null;

        public bool IsTestFrameworkRunning
        {
            get
            {
                if (this.mobjHost == null) return false;
                if ((this.mobjHost.State == CommunicationState.Closed)
                    || (this.mobjHost.State == CommunicationState.Closing)
                    || (this.mobjHost.State == CommunicationState.Faulted)
                    || (this.mobjHost.State == CommunicationState.Created)
                    )
                    return false;
                return true;
            }
        }

        #region auto_checkError

        private void LoadOpicsAutoCheckErrorMessage()
        {
            string strPath = typeof(MarsTestFrameCommuniteServer).Assembly.Location;
            string strCurDir = System.IO.Path.GetDirectoryName(strPath);
            string strMessageDir = System.IO.Path.Combine(strCurDir, "..\\autocheckError\\Opics\\", SystemConstant.CNST_AUTOCHECK_MESSAGE_FILE_NAME);
            if (!System.IO.File.Exists(strMessageDir))
            {
                Logger.Error("LoadOpicsAutoCheckErrorMessage", string.Format("No such message file exists:[{0}]", strMessageDir));
                return;
            }

            AutoCheckError4Opics objAutoCheck = new AutoCheckError4Opics(strMessageDir);
            string strError = "";
            bool isOk = objAutoCheck.InitObject(ref strError);
            if (isOk)
                OpicsMessageAndType = objAutoCheck.CombineMessageToHash();
            else
                OpicsMessageAndType = null;
        }

        private List<KeyValuePair<string, int>> OpicsMessageAndType = null; 
        public List<KeyValuePair<string,int>> GetOpicsMessageAndTypeList()
        {
            if (OpicsMessageAndType==null)
            {
                LoadOpicsAutoCheckErrorMessage();
            }
            return OpicsMessageAndType;
            //return new List<KeyValuePair<string, int>>() {
            //    new KeyValuePair<string, int>(@"Message \d{1,}:.* record not found.*",1),
            //    new KeyValuePair<string, int>(@"Message \d{1,}:.*User not allowed to access application.*", 1),
            //    new KeyValuePair<string, int>(@"Message \d{1,}; Invalid .* path.*", 1)
            //};
        }
        #endregion

#if _TestStepUnit
        #region Servsc 

        public MarsFrameWorkServicesMode CurrerntServiceMode = MarsFrameWorkServicesMode._svcmode_Default;
        /// <summary>
        /// 该对象是线程安全的
        /// </summary>
        private MarsStackManagement<MarsTestStepInfoForTestStepUnitTest> TestStepUnitStack = MarsStackManagement<MarsTestStepInfoForTestStepUnitTest>.GetInstance();
        public OnTestResultMessageArrivedEvent OnTestResultMessageArrivedHandler = null;
        //public OnAddTestStepUnitObjEvent onAddTestStepUnitObjHandler = null;

        public MarsTestStepInfoForTestStepUnitTest GetTestStepUnitFromStack(ref string strError, ref bool isOk)
        {
            isOk = true;
            try
            {
                if (TestStepUnitStack == null)
                {
                    strError = "Stack is empty.";
                    return null;
                }
                MarsTestStepInfoForTestStepUnitTest objRslt = TestStepUnitStack.Pop();
                return objRslt;
            }
            catch (Exception e)
            {
                Logger.Error("GetTestStepUnitFromStack", strError = string.Format("Exception:[{0}] stacktrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }


        }

        public int CreateAutoGenTestStepsCmd()
        {
            Logger.logBegin("CreateAutoGenTestStepsCmd");
            MarsTestStepInfoForTestStepUnitTest objStpCmd = new MarsTestStepInfoForTestStepUnitTest()
            {
                Command = MARSTigerSystemConstantsWCF.CNST_COMMAND_TEST_AUTO_GENTEST,
                TestStepDetail = new TestStep4Services()
                {
                    RunID = -1
                }
            };
            TestStepUnitStack.clean();
            TestStepUnitStack.Push(objStpCmd);
            return 1;
        }

        public int CreateTestStorybardCmd()
        {
            Logger.logBegin("CreateTestStorybardCmd");
            MarsTestStepInfoForTestStepUnitTest objStpCmd = new MarsTestStepInfoForTestStepUnitTest()
            {
                Command = MARSTigerSystemConstantsWCF.CNST_COMMAND_TEST_STORYBOARD,
                TestStepDetail = new TestStep4Services()
                {
                    RunID = -1
                }
            };
            TestStepUnitStack.clean();
            TestStepUnitStack.Push(objStpCmd);
            return 1;
        }

        /// <summary>
        /// 供其他模块调用.将一条TestStep的信息添加到堆栈中
        /// </summary>
        /// <param name="obj"></param>
        public void onAddTestStepUnitObjImpl(IList<TestStep4Services> obj)
        {
            Logger.logBegin("onAddTestStepUnitObjImpl", string.Format("Obj is trying to added:[{0}]", obj == null ? "" : obj.Count + ""));
            try
            {
                if (obj == null) return;
                List<MarsTestStepInfoForTestStepUnitTest> objStpCmd = new List<MarsTestStepInfoForTestStepUnitTest>();
                foreach (var itm in obj)
                {
                    MarsTestStepInfoForTestStepUnitTest objWcf = new MarsTestStepInfoForTestStepUnitTest()
                    {
                        Command = MARSTigerSystemConstantsWCF.CNST_COMMAND_TEST_UNIT,
                        TestStepDetail = itm
                    };
                    objStpCmd.Add(objWcf);
                }
                TestStepUnitStack.Push(objStpCmd);
            }
            finally
            {
                Logger.logEnd("onAddTestStepUnitObjImpl");
            }
        }

        public void NotifiResultForTestStep(MarsTestStepInfoForTestStepUnitTest currentTeststepUnitInfoFromStack, bool isTestOK, string strError)
        {
            Logger.logBegin("NotifiResultForTestStep", string.Format("objectFromClient:[{0}], TestResult:[{1}] ,Callback Message:[{2}]", currentTeststepUnitInfoFromStack == null ? "NULL" : currentTeststepUnitInfoFromStack.Command,
                isTestOK, strError));
            try
            {
                if (OnTestResultMessageArrivedHandler != null)
                {
                    OnTestResultMessageArrivedHandler(string.Format("Message from Client test:\r\n{0}", strError), isTestOK ? "Success" : "Error");
                }
            }
            finally
            {
                Logger.logEnd("NotifiResultForTestStep");
            }
        }
        #endregion
#endif

        #region version 1.1
        //private string mstrCurrentTestSuiteAction;        
        #endregion //version 1.1

        #region event

        protected CompilerCurrentTestCaseEvent CompilerTestCaseEventHandler = null;
        protected int AgentCompilerTestCaseEventHandler(string strTestSuite, string strTestCase)
        {
            if (CompilerTestCaseEventHandler == null)
            {
                Logger.Error("AgentCompilerTestCaseEventHandler", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_COMPILER_ASSIGNED_PARA_2), strTestSuite, strTestCase));
                return (int)ERROR_CODE._SERVICE_ERROR_NO_COMPILER_ASSIGNED_PARA_2;
            }
            return this.CompilerTestCaseEventHandler(strTestSuite, strTestCase);
        }
        public void AddCompilerTestCaseEventHandler(CompilerCurrentTestCaseEvent funcPointer, bool isAdd)
        {
            if (isAdd) this.CompilerTestCaseEventHandler += funcPointer;
            else this.CompilerTestCaseEventHandler -= funcPointer;
        }

        protected OnGetCurrentTestCaseByTestSuiteAndTestCaseNameEvent GetCurrentTestCaseByTestSuiteAndTestCaseNameEventHandler = null;
        public void AddGetCurrentTestCaseByTSAndTCEventHandler(OnGetCurrentTestCaseByTestSuiteAndTestCaseNameEvent funcGet, bool isAdd)
        {
            if (isAdd)
                this.GetCurrentTestCaseByTestSuiteAndTestCaseNameEventHandler = funcGet;
            else
                this.GetCurrentTestCaseByTestSuiteAndTestCaseNameEventHandler = null;
        }
        protected ERROR_CODE AgentGetCurrentTCByTSAndTCNameEventHandlerImpl(string strTestSuiteName, string strTestCaseName, TCObjects objTarget)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.GetCurrentTestCaseByTestSuiteAndTestCaseNameEventHandler == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_NO_TESTCASE_GETTER_ASSIGNED_PARA_0;
                Logger.Error("AgentGetCurrentTCByTSAndTCNameEventHandlerImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }

            return eCde = this.GetCurrentTestCaseByTestSuiteAndTestCaseNameEventHandler(strTestSuiteName, strTestCaseName, objTarget);
        }

        protected OnCompileListEvent CompileListEventHandler = null;
        public void AddOnCompileListEventHandler(OnCompileListEvent funcCompile, bool isAdd)
        {
            if (isAdd)
                this.CompileListEventHandler = funcCompile;
            else
                this.CompileListEventHandler = null;
        }
        //(List<ConfigObjectBase> lstSteps, ref int iErrorStep, ref TestStep objErrorStep, ref string strErrorInfo,string strCurrentAppShortName = null, string strCurrentPegObj = null , bool isSubMode = false,
        //OnCompileOneStepEvent funcCompileOneEventImpl=null, AfterCompileOneStepEvent funcAfterCompileEventImpl=null) ;
        protected ERROR_CODE AgentOnCompileListEventImpl(List<ConfigObjectBase> lstSteps, ref int iErrorStep, ref TestStep objErrorStep, ref string strErrorInfo, string strCurrentAppShortName = null, string strCurrentPegObj = null, bool isSubMode = false,
            OnCompileOneStepEvent funcCompileOneEventImpl = null, AfterCompileOneStepEvent funcAfterCompileEventImpl = null)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.CompileListEventHandler == null)
            {
                return eCde;
            }
            eCde = this.CompileListEventHandler(
                currentDBIdx,
                lstSteps, ref iErrorStep, ref objErrorStep, ref strErrorInfo, strCurrentAppShortName, strCurrentPegObj, isSubMode, funcCompileOneEventImpl, funcAfterCompileEventImpl);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentOnCompileListEventImpl", string.Format("Can't Compile Steps, Error Step Info:\r\n\tId is:{0}, \r\n\tKeyword:[{1}], \r\n\tObject:{2}",
                    objErrorStep.RunID, objErrorStep.Keyword, objErrorStep.ObjectName));
                return eCde;
            }
            return eCde;
        }

#if _Datafrom_Database
        private StoryboardDBRecordMgr currentStoryHis = new StoryboardDBRecordMgr();
        internal void NewTestStoryBoardBegins(string strDBIdx,string strStoryBoardId, bool isBaseLine)
        {
            string strTmp = null;
            Logger.Info("NewTestStoryBoardBegins", strTmp = string.Format("strStoryBoardId:[{0}], baseLine is:[{1}]", strStoryBoardId, isBaseLine));
            Console.WriteLine(strTmp);
            currentStoryHis.Initialization(strDBIdx,strStoryBoardId, isBaseLine, this.IsContinueToTest);
        }

        public int DBRecord_CreateNewTestMarkID(ref string strError)
        {
            Logger.logBegin("DBRecord_CreateNewTestMarkID", string.Format("return:[{0}]", currentStoryHis.curentTestMarkGroupId));
            try
            {
                if (currentStoryHis.curentTestMarkGroupId < 0)
                {
                    currentStoryHis.CreateNewTestMarkGroupId(currentDBIdx);
                }
                strError = "";
                return (int)currentStoryHis.curentTestMarkGroupId;
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_CreateNewTestMarkID", strError = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CANT_CREATE_LASTMARK_ID_PARA_1), e.Message), e);
                return (int)ERROR_CODE._SERVICE_ERROR_CANT_CREATE_LASTMARK_ID_PARA_1;
            }
            finally
            {
                Logger.logEnd("DBRecord_CreateNewTestMarkID");
            }    

        }

        /// <summary>
        /// Write Data To table T_PROJ_TEST_RESULT
        /// </summary>
        /// <param name="strCurrentCase"></param>
        /// <returns></returns>
        public int DBRecord_currentTestStoryBoardStart(string strCurrentCase)
        {
            Logger.Info("DBRecord_currentTestStoryBoardStart", string.Format("strCurrentCase:[{0}]", strCurrentCase));
            //this.mobjCurrentTestSuite.
            currentStoryHis.InitializeTestReportResult(((TCObjects4DB)mobjCurrentTestSuite).TestCaseKeyId,
                ((BatchConfigObjectFromDB)((TCObjects4DB)this.mobjCurrentTestSuite).AssignedStoryBoardInfo).AssignedStoryObject.STORYBOARD_DETAIL_ID
                );
            return (int)ERROR_CODE._NO_ERROR;
        }

        public int DBRecord_currentTestCaseStart(string strCurrentCase, int iLoop)
        {
            Logger.logBegin("DBRecord_currentTestCaseStart");
            try
            {
                Logger.Info("DBRecord_currentTestCaseStart", string.Format("strCurrentCase:[{0}]", strCurrentCase));
                currentStoryHis.LogTestCaseStart(currentDBIdx, ((TCObjects4DB)mobjCurrentTestSuite).TestCaseKeyId, iLoop);
                return (int)ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("DBRecord_currentTestCaseStart");
            }
            
        }

        public int DBRecord_currentTestCaseLoopEnd(int iResultId, int iLoopId, string strEndInfo)
        {
            Logger.logBegin("DBRecord_currentTestCaseLoopEnd");
            try
            {
                Logger.Info("DBRecord_currentTestCaseLoopEnd", string.Format("ResultId:[{0}], iLoopId:[{1}], EndInfo:[{2}]", iResultId, iLoopId, strEndInfo));
                return currentStoryHis.UpdateTestCaseLoopResult(((TCObjects4DB)mobjCurrentTestSuite).TestCaseKeyId, iResultId, iLoopId, strEndInfo);
            }catch(Exception e)
            {
                Logger.Error("DBRecord_currentTestCaseLoopEnd" , string.Format("Exception:[{0}]", e.Message),e);
                return -1;
            }
            finally
            {
                Logger.logEnd("DBRecord_currentTestCaseLoopEnd");
            }
            
        }

        public int DBRecord_currentTestStoryBoardEnd(int idSuccess, string strErrorInfo)
        {
            Logger.logBegin("DBRecord_currentTestStoryBoardEnd");
            try
            {
                Logger.Info("DBRecord_currentTestStoryBoardEnd", string.Format("Test case End,idSucess:[{0}],ErrorInfo:[{1}]", idSuccess, strErrorInfo));
                currentStoryHis.UpdateTestReportResult(idSuccess, strErrorInfo);
                this.mobjLoopMgr.Init();
                new Thread(new ThreadStart(new Action(delegate ()
                {
                    Logger.Info("----Test-----", "Enter Thread.....");
                    //if (this.TestCaseFinishedCallbackHandler != null)
                    //{

                    //    this.TestCaseFinishedCallbackHandler(-1);
                    //}
                    Logger.Info("----Test-----", "Exit Thread.....");
                }))).Start();
                return (int)ERROR_CODE._NO_ERROR;
            }catch(Exception e)
            {
                Logger.Error("DBRecord_currentTestStoryBoardEnd", string.Format("Exception:[{0}]", e.Message), e);
                return (int)ERROR_CODE._ERROR_UNKNOWN;
            }
            finally
            {
                Logger.logEnd("DBRecord_currentTestStoryBoardEnd"); 
            }
            
        }

        public int DBRecord_currentTestStepEnd(int idSuccess, int iCurrentLoopId, string strError)
        {
            Logger.logBegin("DBRecord_currentTestStepEnd");
            try
            {
                Logger.Info("DBRecord_currentTestStepEnd", string.Format("idSucess:[{0}], strError:[{1}], dataLen:[{2}]", idSuccess, strError, this.mobjCurrentTestStep == null ? 0 : this.mobjCurrentTestStep.PicInfo == null ? -1 : this.mobjCurrentTestStep.PicInfo.Length));
                ///Logger.Info("Object address", string.Format("mobjCurrentTestStep:[{0}]", (int)(&mobjCurrentTestStep.PicInfo)));
                //string arrPicInfo = this.mobjCurrentTestStep.PicInfo;
                return currentStoryHis.UpdateCurrentTestStepResult(iCurrentLoopId, this.mobjCurrentTestStep.AssignedTestStepId, idSuccess, strError, this.mobjCurrentTestStep.PicInfo);
            }catch(Exception e)
            {
                Logger.Error("DBRecord_currentTestStepEnd",string.Format("Exception", e.Message),e);
                return -1;
            }
            finally
            {
                Logger.logEnd("DBRecord_currentTestStepEnd");
            }
            
        }


        public int DBRecord_OnOneLoopIsDone()
        {
            Logger.logBegin("DBRecord_OnOneLoopIsDone");
            Logger.Info("DBRecord_OnOneLoopIsDone", string.Format("currentLoop:[{0}]", this.miCurrentStepId));
            Logger.logEnd("DBRecord_OnOneLoopIsDone");
            return (int)ERROR_CODE._NO_ERROR;
        }
        public int CreateStepLogInfo(TestStepRunningRecorder objCurrentStepLog, int iCurrentLoopId, long stepId)
        {
            Logger.logBegin("CreateStepLogInfo");
            try
            {
                Logger.Info("CreateStepLogInfo", string.Format("TeststepId:[{0}], LoopId:[{1}]", stepId, iCurrentLoopId));
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                if (objCurrentStepLog == null)
                {
                    Logger.Error("CreateStepLogInfo", ERROR_INFO.GET_ERROR_STR(eCde = ERROR_CODE._SERVICE_ERROR_NO_STEP_OBJECT_PARA_O));
                    return (int)eCde;
                }
                return currentStoryHis.CreateStepLog(stepId, objCurrentStepLog);
            } catch(Exception e)
            {
                Logger.Error("CreateStepLogInfo", string.Format("Exception:[{0}]", e.Message), e);
                return (int)ERROR_CODE._ERROR_UNKNOWN;
            }
            finally
            {
                Logger.logEnd("CreateStepLogInfo");
            }
            
        }

        public int DBRecord_UpdateCurrentStepData(int iLoopId, string strData, TestStepRunningRecorder lastRecorder)
        {
            Logger.logBegin("DBRecord_UpdateCurrentStepData");
            Logger.Info("DBRecord_UpdateCurrentStepData", string.Format("stepId:[{0}], iLoopId:[{1}], strData:[{2}]", lastRecorder == null ? -1 : lastRecorder.assignedStepId, iLoopId, strData));
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            try
            {
                if (lastRecorder == null)
                {
                    Logger.Error("CreateStepLogInfo", ERROR_INFO.GET_ERROR_STR(eCde = ERROR_CODE._SERVICE_ERROR_NO_STEP_OBJECT_PARA_O));
                    return (int)eCde;
                }
                return (int)currentStoryHis.updateDataField(strData, iLoopId, lastRecorder.assignedStepId);
            }catch(Exception e)
            {
                Logger.Error("DBRecord_UpdateCurrentStepData",string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("DBRecord_UpdateCurrentStepData");
            }            
        }
#endif
        protected OnBeginNavigateTestSuite onBeginNavigateTestSuiteHandler = null;
        public void AddOnNavigateHandler(OnBeginNavigateTestSuite funcOnNavigate, bool isAdd)
        {
            if (isAdd)
                onBeginNavigateTestSuiteHandler = funcOnNavigate;
            else
                onBeginNavigateTestSuiteHandler = null;
        }
        protected ERROR_CODE AgentOnNavigateTestSuiteEventImpl()
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.onBeginNavigateTestSuiteHandler == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_NO_TESTSUITE_NAVIGATE_ASSIGNED_PARA_0;
                Logger.Error("AgentOnNavigateTestSuiteEventImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }

            eCde = this.onBeginNavigateTestSuiteHandler();
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentOnNavigateTestSuiteEventImpl", string.Format("Can't Begin Navigate Test suite, error number is {0:x}", eCde));
                return eCde;
            }
            return eCde;
        }

        protected OnBeginNavigateTestSuiteWithRelyIdAndLoopId onBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler = null;
        public void AddOnBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler(OnBeginNavigateTestSuiteWithRelyIdAndLoopId funcOnNavigate, bool isAdd)
        {
            if (isAdd)
                onBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler = funcOnNavigate;
            else
                onBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler = null;
        }
        protected ERROR_CODE AgentonBeginNavigateTestSuiteWithRelyIdAndLoopIdImpl(string strRelyId, int iLoop)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.onBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_NO_TESTSUITE_NAVIGATE_ASSIGNED_PARA_0;
                Logger.Error("AgentonBeginNavigateTestSuiteWithRelyIdAndLoopIdImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }

            eCde = this.onBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler(strRelyId, iLoop);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentonBeginNavigateTestSuiteWithRelyIdAndLoopIdImpl", string.Format("Can't Begin Navigate Test suite, error number is {0:x}", eCde));
                return eCde;
            }
            return eCde;
        }

        protected OnGetNextTestSuiteEvent onGetNextTestSuiteEventHandler = null;
        public void AddOnGetNextTestSuiteEvent(OnGetNextTestSuiteEvent funcOnNextTS, bool isAdd)
        {
            if (isAdd)
                this.onGetNextTestSuiteEventHandler = funcOnNextTS;
            else
                this.onGetNextTestSuiteEventHandler = null;
        }
        protected ERROR_CODE AgentOnGetNextTestSuiteEventImpl()
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.onGetNextTestSuiteEventHandler == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_NO_GETNEXTTESTSUITE_ASSIGNED_PARA_0;
                Logger.Error("AgentOnGetNextTestSuiteEventImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }
            this.mobjCurrentTestSuite = new TCObjects();
#if v_useNameId
            eCde = this.onGetNextTestSuiteEventHandler(ref this.mobjCurrentTestSuite, this.currentApplicationId);
#else
            eCde = this.onGetNextTestSuiteEventHandler(ref this.mobjCurrentTestSuite);
#endif
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentOnGetNextTestSuiteEventImpl", string.Format("Can't Get Next test suite with error number is [{0:x}]", eCde));
                return eCde;
            }
            /*** to load all information of the current testsuite ***/
            eCde = this.AgentOnAfterLoadCurrentTestSuiteEventImpl();

            return eCde;
        }

        protected OnLoadCurrrentTestSuitEvent onAfterGetTestSuiteEventHandler = null;
        public void AddOnLoadcurrentTestSuiteEvent(OnLoadCurrrentTestSuitEvent funcM, bool isAdd)
        {
            if (isAdd)
                this.onAfterGetTestSuiteEventHandler = funcM;
            else
                this.onAfterGetTestSuiteEventHandler = null;
        }

        protected ERROR_CODE AgentOnAfterLoadCurrentTestSuiteEventImpl()
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.onAfterGetTestSuiteEventHandler == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_NO_GETNEXTTESTSUITE_ASSIGNED_PARA_0;
                Logger.Error("AgentOnAfterLoadCurrentTestSuiteEventImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }
            eCde = this.onAfterGetTestSuiteEventHandler();
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentOnAfterLoadCurrentTestSuiteEventImpl", string.Format("Can't initialize Test suite with error number:[{0}]", eCde));
                return eCde;
            }
            return eCde;
        }

        protected OnTestsuiteIsDoneEvent onTestSuiteIsDoneEventHandler = null;
        public void AddOnTestSuiteIsDoneEventHandler(OnTestsuiteIsDoneEvent funcHandler, bool isAdd)
        {
            if (isAdd)
                this.onTestSuiteIsDoneEventHandler = funcHandler;
            else
                this.onTestSuiteIsDoneEventHandler = null;
        }
        protected ERROR_CODE AgentOnTestSuiteIsDoneEventImpl(TestSuiteRunStatusInfo objStatus, bool isContinueWhenFalse)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.onTestSuiteIsDoneEventHandler == null)
            {
                eCde = ERROR_CODE._SERVCIE_ERROR_NO_TESTSUITE_IS_DONE_STATUS_ASSIGNED_PARA_0;
                Logger.Error("AgentOnTestSuiteIsDoneEventImpl", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }

            eCde = this.onTestSuiteIsDoneEventHandler(objStatus, this.mobjCurrentTestSuite.Action4Project, isContinueWhenFalse);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("AgentOnTestSuiteIsDoneEventImpl", string.Format(string.Format("Can't write status to Project running file:[{0}], error Number:[{1:x}]", objStatus, eCde)));
                return eCde;
            }

            /// Notify Gui if assigned
            /// 
            Thread thrdTmp = new Thread(new ThreadStart(new Action(delegate ()
            {
                Logger.Info("AgentOnTestSuiteIsDoneEventImpl", "Begin Thread....");
                if (this.TestCaseFinishedCallbackHandler != null)
                {
                    TestCaseFinishedCallbackHandler(-1);
                }
                Logger.Info("AgentOnTestSuiteIsDoneEventImpl", "End Thread....");
            })));
            thrdTmp.Start();

            thrdTmp.Join();

            return eCde;
        }

        #endregion //event


        #region service functions
        public string GetCurrentTestSuiteId4Project()
        {
            return this.mobjCurrentTestSuite == null ? null : this.mobjCurrentTestSuite.Id4Project;
        }

        public string GetApplicationExtraInfo(string strApplicationShortName, ref int iErrorId)
        {
            TargetApplicationInfo objData = TargetApplicationsManagement.GetRegApplicationByStepValue(strApplicationShortName);
            if (objData == null)
            {
                iErrorId = (int)ERROR_CODE._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1;
                return SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEFAULT;
            }
            return string.IsNullOrEmpty(objData.ExtraRequirement) ? SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEFAULT : objData.ExtraRequirement;
        }

        public string GetCurrentApplicationIdentifier()
        {
            int iError = (int)ERROR_CODE._NO_ERROR;
            string strAppId = "",strPara="";            
            string strResult = GetApplicationFullCmdByShortName(this.mstrCurrrentApplicationShortName, ref strAppId, ref iError,ref strPara);
            if (iError != (int)ERROR_CODE._NO_ERROR)
            {
                return null;
            }
            if (string.IsNullOrEmpty(strResult))
            {
                Logger.Error("GetCurrentApplicationCmd", string.Format("Can't get current Application:[{0}]", this.CurrentTestApplicationShortName));
                return null;
            }
            return strAppId;
        }

        public string GetCurrentApplicationCmdWithPara(ref string strPara)
        {
            int iError = (int)ERROR_CODE._NO_ERROR;
            string strAppId = "";
            string strResult = GetApplicationFullCmdByShortName(this.mstrCurrrentApplicationShortName, ref strAppId, ref iError,ref strPara);
            if (iError != (int)ERROR_CODE._NO_ERROR)
            {
                return null;
            }
            if (string.IsNullOrEmpty(strResult))
            {
                Logger.Error("GetCurrentApplicationCmd", string.Format("Can't get current Application:[{0}]", this.CurrentTestApplicationShortName));
                return null;
            }
            return strResult;
        }

        public string GetCurrentApplicationCmd()
        {
            int iError = (int)ERROR_CODE._NO_ERROR;
            string strAppId = "",strPara="";
            string strResult = GetApplicationFullCmdByShortName(this.mstrCurrrentApplicationShortName, ref strAppId, ref iError,ref strPara);
            if (iError != (int)ERROR_CODE._NO_ERROR)
            {
                return null;
            }
            if (string.IsNullOrEmpty(strResult))
            {
                Logger.Error("GetCurrentApplicationCmd", string.Format("Can't get current Application:[{0}]", this.CurrentTestApplicationShortName));
                return null;
            }
            return strResult;
        }

        public int GetExtraPopupMenuCount()
        {
            try
            {
                TargetApplicationInfo objData = TargetApplicationsManagement.GetRegApplicationByStepValue(CurrentTestApplicationShortName);
                if (objData == null) return 1;

                return int.Parse(objData.ExtraPopupMenuCount);
            }
            catch
            {
                return 1;//default value 
            }
        }

        public string GetCurrentDefaultTestApplication()
        {
            return AppConfigReader.GetDefaultApplicationName();
        }

        /// <summary>
        /// Used only for non-database mode
        /// </summary>
        /// <returns></returns>
        public string GetBaseLineMode()
        {
            string strMode = AppConfigReader.GetBaseLineMode(WCFXmlCfgMgr.CurrentLoginUser);
            strMode = strMode ?? "";
            if (!((SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD.ToUpper().CompareTo(strMode.ToUpper()) == 0)
            || (SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE.ToUpper().CompareTo(strMode.ToUpper()) == 0)))
            {
                // default value setting
                Logger.Warnning("GetBaseLineMode",
                    string.Format("Value of BaseLine is not a value expected, default value will be used. \r\nOnly [{0}/{1}] can be accepted."
                    , SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE));
                strMode = SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD;
            }
            return strMode;
        }

        public int GetNextTestSuite()
        {
            ERROR_CODE eCde = this.AgentOnGetNextTestSuiteEventImpl();
            return (int)eCde;
        }

        public int NotifyCurrentTestSuiteRunStatus(TestSuiteRunStatusInfo objStatus, bool isContinueWhenFalse)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            eCde = this.AgentOnTestSuiteIsDoneEventImpl(objStatus, isContinueWhenFalse);
            return (int)eCde;
        }

        public string GetCurrentTestCaseName()
        {
            return CurrentTestCaseName;
        }

        public string GetCurrentTestSuiteName()
        {
            return CurrentTestSuiteName;
        }

        public int GetCurrentStepRunType(string strKeyWordName)
        {
            Logger.logBegin("GetCurrentStepRunType");
            try
            {
                Keyword_RunType eRunType = KeyWordObjectInfo.GetKeywordRunType(strKeyWordName);
                return (int)eRunType;
            }catch(Exception e)
            {
                Logger.Error("GetCurrentStepRunType",string.Format("Exception:[{0}]",e.Message),e);
                return -1;
            }
            finally
            {
                Logger.logEnd("GetCurrentStepRunType");
            }
            
        }

        public int GetTestLoopCount()
        {
            Logger.logBegin("GetTestLoopCount");
            try
            {
                if (miCurrentLoopCnt < 0)
                {
                    if (mobjCurrentTestSuite == null)
                    {
                        Logger.Info("GetTestLoopCount", "Test suite object is null. ");
                        return -1;
                    }
                    miCurrentLoopCnt = mobjCurrentTestSuite.GetTestLoopCount();
                }
                return miCurrentLoopCnt;
            }catch(Exception e)
            {
                Logger.Error("GetTestLoopCount",string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("GetTestLoopCount");
            }
            
        }

        public bool GetBatchMode()
        {
            Logger.Info("GetBatchMode", "IsRunningInBatchMode =  " + IsRunningInBatchMode);
            return IsRunningInBatchMode;
        }


        public MarsTestFrameCommuniteServer()
        {
            Logger.Info("MarsTestFrameCommuniteServer", string.Format("Created an instance with instanceID:[{0}]", miServerInstanceId++));

            ///load plugins
            /// 
            LoadMarsPlugins();
            Logger.logEnd("MarsTestFrameCommuniteServer");
        }

        private void InitializeData()
        {
            miCurrentStepId = 0;
            meCde_LastError = ERROR_CODE._NO_ERROR;

        }
        #endregion

        #region Test Suite Navigate
        public int BeginNavigateTestSuite()
        {
            Logger.logBegin("BeginNavigateTestSuite");
            try
            {
                int iError = (int)AgentOnNavigateTestSuiteEventImpl();                
                return iError;
            }catch(Exception e)
            {
                Logger.Error("BeginNavigateTestSuite",string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("BeginNavigateTestSuite");
            }
            
        }

        public int BeginNavigateTestSuiteWithRelyIdAndLoop(string strRelyId, int iLoop)
        {
            
            Logger.logBegin("BeginNavigateTestSuiteWithRelyIdAndLoop", string.Format("Parameters- strRelyId:[{0}], iLoop:[{1}]", strRelyId, iLoop));
            try
            {
                int iError = (int)AgentonBeginNavigateTestSuiteWithRelyIdAndLoopIdImpl(strRelyId, iLoop);                
                return iError;
            }
            catch (Exception e)
            {
                Logger.Error("BeginNavigateTestSuiteWithRelyIdAndLoop", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("BeginNavigateTestSuiteWithRelyIdAndLoop");
            }

        }
        #endregion //Test Suite Navigate

        #region Test Step Navigate
        public bool StartTestStepNavigate()
        {
            try
            {
                Logger.Info("StartTestStepNavigate", "[]");
                InitializeData();
                InitializeLoopInfo();
                return mlstSteps != null;
            }catch(Exception e)
            {
                Logger.Error("StartTestStepNavigate",string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("StartTestStepNavigate");
            }
        }

        public bool GetNextTestCase()
        {
            Logger.logBegin("GetNextTestCase");
            Logger.logEnd("GetNextTestCase");
            return true;
        }

        public List<SubTestInfo4Services> GetCurrentSubActionsInfo(ref int iCde)
        {
            Logger.logBegin("GetCurrentSubActionsInfo");
            //ERROR_CODE eCde = ERROR_CODE._NO_ERROR;

            //if (this.)

            Logger.logEnd("GetCurrentSubActionsInfo");
            return null;
        }
        /// <summary>
        /// the function should be call only for pegwindow in vbs part,so that the current object is pegwindow
        /// to call GetCurrentErrorCheckObjectsInfo requiring an pegwindow object name
        /// </summary>
        /// <returns></returns>
        public TestStepObject GetDefaultErrorCheckingObj4CurrentPeg(string strDBIdx)
        {
            try
            {
                Logger.logBegin("GetDefaultErrorCheckingObj4CurrentPeg");
                ///从t-registered-object 中获得标记为check对象的对象
                /// 
                if (this.mobjCurrentTestStep == null) return null;
                if (string.IsNullOrEmpty(this.mobjCurrentTestStep.ObjectName)) return null;
                //B_REGISTED_OBJECT objObj = new B_REGISTED_OBJECT();
                bool isOk = false;
                string strError = "";
                ///该函数只能够在在pegwindow时期调用
                ///  获得peg的信息
                V_OBJECT_SNAPSHOTDTO objPeg = GetCurrentPegInfoByAppIdAndPegName(strDBIdx,this.mobjCurrentTestStep.ObjectName, this.currentApplicationId, ref strError, ref isOk);
                if (!isOk)
                {
                    Logger.Error("GetDefaultErrorCheckingObj4CurrentPeg", strError);
                    return null;
                }

                string strPegQuickAceessFull = TestObject.BuildPegQuickAcessStringByPegQuickAndType(objPeg.QUICK_ACCESS, objPeg.TYPE_NAME);

                V_OBJECT_SNAPSHOTDTO objObj = GetCurrentErrorCheckObjectsInfo(this.mobjCurrentTestStep.ObjectName, this.currentApplicationId, ref strError, ref isOk);
                //T_REGISTED_OBJECTDTO objAutoCheckErrorObj = objObj.getDefaultCheckErrorObjByTestStepId(this.mobjCurrentTestStep.AssignedTestStepId,ref isOk,  ref strError);
                if (!isOk)
                {
                    Logger.Error("GetDefaultErrorCheckingObj4CurrentPeg", string.Format("Error when get AutoCheckError object from db:[{0}] for test step id:[{1}]", strError, this.mobjCurrentTestStep.AssignedTestStepId));
                    return null;
                }
                if (objObj == null) return null;
                /// convert to TestStepObject
                TestStepObject objTarget = new TestStepObject()
                {
                    OBJECT_HAPPY_NAME = objObj.OBJECT_HAPPY_NAME,
                    APPLICATION_ID = objObj.APPLICATION_ID,
                    COMMENT = objObj.COMMENT,
                    ENUM_TYPE = objObj.ENUM_TYPE,
                    OBJECT_ID = objObj.OBJECT_ID,
                    OBJECT_NAME_ID = objObj.OBJECT_NAME_ID,
                    OBJECT_TYPE = objObj.TYPE_NAME,
                    PEG_ID = -1, //not assigned for now
                    PEG_NAME = this.mobjCurrentTestStep.ObjectName,
                    PEG_QUICK_ACCESS = strPegQuickAceessFull, //this.mobjCurrentTestStep.QuickAccess,
                    QUICK_ACCESS = objObj.QUICK_ACCESS,
                    TYPE_ID = (short)(objObj.TYPE_ID ?? -1)
                };
                /// enhance the quick_access, so that client side can use easily
                /// 
                objTarget.QUICK_ACCESS = QuickAccessMgr.ConvertQuickAccessIdToQtpObjectFormat(objTarget.QUICK_ACCESS, objTarget.OBJECT_TYPE);

                return objTarget;
            }catch(Exception e)
            {
                Logger.Error("GetDefaultErrorCheckingObj4CurrentPeg", string.Format("Exception:[{0}]", e.Message),e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetDefaultErrorCheckingObj4CurrentPeg");    

            }
        }

        public TestStep4Services GetNextTestStep(int iStepIdAsked = -1)
        {
            Logger.logBegin("GetNextTestStep", string.Format("StepdId Asked:[{0}]", iStepIdAsked));
            try
            {
                if (iStepIdAsked >= 0)
                    miCurrentStepId = iStepIdAsked >= mlstSteps.Count ? mlstSteps.Count - 1 : iStepIdAsked;

                if (miCurrentStepId >= mlstSteps.Count)
                {

                    #region loop mgr
                    if (!this.mobjLoopMgr.isLoopFinished())
                    {
                        Logger.Info("GetNextTestStep", "Loop is not done");
                        // 如果loop变量没有结束，继续重头开始现有test case,并且将loop的当前指针move到下一个
                        miCurrentStepId = 0;
                        this.mobjLoopMgr.MoveCurrentLoopNext();
                    }
                    else
                    #endregion //loop mgr
                    {
                        Logger.Info("GetNextTestStep", string.Format("Can't continue to get a new step as step reached to end . require id :[{0}], length:[{1}]", miCurrentStepId, mlstSteps.Count));
                        this.meCde_LastError = ERROR_CODE._NO_ERROR;
                        return null;
                    }
                }


                //if (miCurrentStepId < mlstSteps.Count)
                //{
                ConfigObjectBase objBase = mlstSteps[miCurrentStepId];
                if (!(objBase is TestStep))
                {
                    this.meCde_LastError = ERROR_CODE._SERVICE_ERROR_OBJECT_TYPE_IS_NOT_TEST_STEP;
                    Logger.Error("GetNextTestStep", string.Format(ERROR_INFO.GET_ERROR_STR(this.meCde_LastError), objBase.GetType().ToString()));
                    return null;
                }
                miCurrentStepId++;
                Logger.Info("GetNextTestStep", string.Format("Current StepId:[{0}]", miCurrentStepId));
                /** convert to from TestStep to  **/
                TestStep4Services objStep4Service = new TestStep4Services();
                TestStep objStep = (TestStep)objBase;

                //if (string.Compare("Pegwindow",objStep.Keyword,true)==0)
                //{
                //    this.CurrentPegwindowIdntifier = objStep.ObjectFullpath;
                //    string strError = "";
                //    bool isOk = GetCurrentErrorCheckObjectsInfo(objStep.ObjectName, this.currentApplicationId,ref strError);
                //}

                objStep.CloneToService(objStep4Service);
                //objStep4Service.CloneFromStep(objStep);
                this.meCde_LastError = ERROR_CODE._NO_ERROR;
                TestStep4Services.Normalization(objStep4Service);
#if _Datafrom_Database
                objStep4Service.AssignedTestStepId = ((TestStepsFromDB)objBase).TestStepsFullVisionDTO.STEPS_ID;
                mobjCurrentTestStep = objStep4Service;
#endif
                return objStep4Service;
                //}

            }
            finally
            {
                Logger.logEnd("GetNextTestStep");
            }
        }

        private V_OBJECT_SNAPSHOTDTO GetCurrentPegInfoByAppIdAndPegName(string strDBIdx,string strPegwindowName, long lAppId, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetCurrentPegInfoByAppIdAndPegName");
            try
            {
                B_V_OBJECT_SNAPSHOT objSnapPeg = new B_V_OBJECT_SNAPSHOT();
                isOk = false;
                V_OBJECT_SNAPSHOTDTO objPeg = objSnapPeg.GetCurrentPegInfoByAppIdAndPegName(strDBIdx, strPegwindowName, lAppId, ref isOk, ref strError);
                if (!isOk) return null;
                return objPeg;
            }
            finally
            {
                Logger.logEnd("GetCurrentPegInfoByAppIdAndPegName");
            }

        }

        private V_OBJECT_SNAPSHOTDTO GetCurrentErrorCheckObjectsInfo(string strPegwindowName, long lAppId, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetCurrentErrorCheckObjectsInfo", string.Format("Peg:[{0}] applicationId:[{1}]", strPegwindowName, lAppId));
            B_V_OBJECT_SNAPSHOT objSnap = new B_V_OBJECT_SNAPSHOT();
            isOk = false;
            V_OBJECT_SNAPSHOTDTO objErrorObj = objSnap.getDefaultErrorObjectForPegByAppId(strPegwindowName, lAppId, ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error("GetCurrentErrorCheckObjectsInfo", string.Format("Error when call getDefaultErrorObjectForPegByAppId:\r\n:[{0}]", strError));
                return null;
            }
            string strErrorIdx = objErrorObj.QUICK_ACCESS;
            if (string.IsNullOrEmpty(strErrorIdx))
            {
                Logger.Warnning("GetCurrentErrorCheckObjectsInfo", strError = "No Error object identifier information is set for default error checking objects");
                return null;
            }

            return objErrorObj;
        }
        #endregion //Test step navigate

        public string GetApplicationFullCmdByShortName(string strShortName, ref string strAPPID, ref int eCde)
        {
            Logger.logBegin("GetApplicationFullCmdByShortName");
           
            try
            {
                string strPara = "";
                return GetApplicationFullCmdByShortName(strShortName,ref strAPPID,ref eCde, ref strPara);
            }
            catch (Exception e)
            {
                if (e is MarsExceptions)
                {
                    eCde = ((MarsExceptions)e).ErrorId;
                }
                else
                {
                    eCde = (int)ERROR_CODE._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1;
                    Logger.Error("GetApplicationFullCmdByShortName", string.Format(ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde), strShortName));
                }
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationFullCmdByShortName");
            }


        }

        public string GetApplicationFullCmdByShortName(string strShortName, ref string strAPPID, ref int eCde,ref string strPara)
        {
            Logger.logBegin("GetApplicationFullCmdByShortName");
            try
            {
                TargetApplicationInfo objData = TargetApplicationsManagement.GetRegApplicationByStepValue(strShortName);
                if (objData == null)
                {
                    eCde = (int)ERROR_CODE._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1;
                    return null;
                }
                strPara = objData.Command;
                eCde = (int)ERROR_CODE._NO_ERROR;
                //strAPPID = objData.Indentifier;
                Logger.Info("GetApplicationFullCmdByShortName",string.Format("strAppId:[{0}]", strAPPID=objData.Indentifier));
                return objData.Path;
            }
            catch (Exception e)
            {
                if (e is MarsExceptions)
                {
                    eCde = ((MarsExceptions)e).ErrorId;
                }
                else
                {
                    eCde = (int)ERROR_CODE._SERVICE_ERROR_NO_APPLICATION_INFO_PARA_1;
                    Logger.Error("GetApplicationFullCmdByShortName", string.Format(ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde), strShortName));
                }
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationFullCmdByShortName");
            }
            
            
        }

        public void EndTestStepNavigate()
        {
            Logger.logBegin("EndTestStepNavigate");
            try
            {
                InitializeData();
                
            }catch(Exception e)
            {
                Logger.Error("EndTestStepNavigate",string.Format("Exception:{0}", e.Message),e);
            }
            finally
            {
                Logger.logEnd("EndTestStepNavigate");
            }
            
        }

        public ERROR_CODE GetLastError()
        {
            return meCde_LastError;
        }

        public void InitListTestSteps(List<ConfigObjectBase> lstData)
        {
            Logger.logBegin("SetListTestSteps");
            InitializeData();
            mlstSteps = this.mobjCurrentTestSuite.CurrentStepsList;
            Logger.logEnd("SetListTestSteps");
        }

        public List<TestStep4Services> GetCurrentCompiledList(ref int eCde)
        {
            Logger.logBegin("GetCurrentCompiledList");
            eCde = (int)ERROR_CODE._NO_ERROR;
            List<TestStep4Services> lstResult = new List<TestStep4Services>();
            try
            {
                foreach (ConfigObjectBase objBase in mlstSteps)
                {
                    if (!(objBase is TestStep))
                    {
                        eCde = (int)ERROR_CODE._SERVICE_ERROR_OBJECT_TYPE_IS_NOT_TEST_STEP;
                        Logger.Error("GetNextTestStep", string.Format(ERROR_INFO.GET_ERROR_STR(this.meCde_LastError), objBase.GetType().ToString()));
                        return null;
                    }
                    TestStep objStp = (TestStep)objBase;
                    TestStep4Services objTarget = new TestStep4Services();
                    objStp.CloneToService(objTarget);
                    lstResult.Add(objTarget);
                }
                Logger.Info("GetCurrentCompiledList", string.Format("Total Compiled list item count:[{0}]", lstResult.Count));

                return lstResult;
            }catch(Exception e)
            {
                Logger.Error("GetCurrentCompiledList",string.Format("Exception:[{0}]", e.Message),e);
                return lstResult;
            }
            finally
            {
                Logger.logEnd("GetCurrentCompiledList");
            }
            
        }

#if _Datafrom_Database
        /// <summary>
        /// Test Case ID is necessary for Database mode, as it is the database key for 
        /// </summary>
        /// <param name="iTestCaseIde"></param>
        /// <param name="objErrorObj"></param>
        /// <param name="iErrorId"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int CompilerCurrentTestCaseById(int iTestCaseIde, ref TestStep4Services objErrorObj, ref int iErrorId, ref string strError)
        {
            Logger.logBegin("CompilerCurrentTestCaseById");
            Logger.logEnd("CompilerCurrentTestCaseById");
            return (int)ERROR_CODE._NO_ERROR;
        }
#endif

        public int CompilerCurrentTestCase(string strSuiteName, string strCaseName, ref TestStep4Services objErrorObj, ref int iErrorRunId, ref string strError)
        {
            /** 
             * NOT Finished perfect yet, 
             * The perfect flow is listed below:
             * 1, check the currrent suitename or caseName equals strSuiteName or strCaseName
             *      1.1 return error when either of them desn't match
             * 2, Get the lst from test project
             * 3, compiler and return ErrorInformation
             * **/
            Logger.logBegin("CompilerCurrentTestCase");
            try
            {

                TCObjects objTC = mobjCurrentTestSuite;//new TCObjects();
                ERROR_CODE eCde = this.AgentGetCurrentTCByTSAndTCNameEventHandlerImpl(strSuiteName, strCaseName, objTC);
                if (eCde != ERROR_CODE._NO_ERROR)
                {
                    Logger.logBegin("CompilerCurrentTestCase No Agent....");
                    return (int)eCde;
                }
                //mobjCurrentTestSuite = objTC;
                eCde = objTC.loadTestCase();
                if (eCde != ERROR_CODE._NO_ERROR)
                {
                    Logger.Error("CompilerCurrentTestCase", string.Format("Error Code ocurrs after call objTestSuit.loadXlsFile() ERROR_CODE:[{0:X}]，ERROR_INFO:[{1}]", eCde, ERROR_INFO.GET_ERROR_STR(eCde)));
                    return (int)eCde;
                }

                if (objTC.GetType() == typeof(TCObjects))
                {
                    objTC.InitDefaultDataFileName();
                    eCde = objTC.loadData();
                }
                int iErrorStep = -1;
                TestStep objErrorTestStep = new TestStep();
                string strErrorInfo = "";
                eCde = this.AgentOnCompileListEventImpl(objTC.CurrentStepsList, ref iErrorStep, ref objErrorTestStep, ref strErrorInfo, null, null, false, null, null);
                if (eCde != ERROR_CODE._NO_ERROR)
                {
                    if (iErrorStep < 0)
                    {
                        /** Error Code with minus **/
                        iErrorRunId = iErrorStep;
                        objErrorObj = null;
                        strError = strErrorInfo;
                    }
                    else
                    {
                        iErrorRunId = iErrorStep;
                        if (objErrorTestStep != null)
                            objErrorTestStep.CloneToService(objErrorObj);
                        strError = strErrorInfo;
                    }
                    return (int)eCde;
                }
                else
                {
                    this.mlstSteps = objTC.CurrentStepsList;
                }
                return (int)ERROR_CODE._NO_ERROR;

            } catch(Exception e)
            {
                Logger.Error("CompilerCurrentTestCase",string.Format("Exception:[{0}]", e.Message), e);
                return (int)ERROR_CODE._ERROR_UNKNOWN;
            }
            finally
            {
                Logger.logEnd("CompilerCurrentTestCase");
            }
        }
        /*
        public ERROR_CODE StopService()
        {
            Logger.logBegin("StopService");
            try
            {
                if (this.mobjHost == null) return ERROR_CODE._NO_ERROR;
                this.mobjHost.Close();
            }
            catch (Exception e)
            {
                ERROR_CODE eCde = ERROR_CODE._SERVICE_ERROR_NO_SERVICE_STOP_UNKNOW;
                Logger.Error("StopService", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message));
                return eCde;

            }
            Logger.logEnd("StopService");
            return ERROR_CODE._NO_ERROR;
        }
         * */

        public CommunicationState? CurrentSvcStatus
        {
            get
            {
                if (this.mobjHost == null) return null;
                return this.mobjHost.State;
            }
        }

        public void StopService(ref string strErrorINfo)
        {
            Logger.Warnning("StopService", "Trying to stop Framework services");
            strErrorINfo = null;

            if (this.mobjHost == null) return;
            if (this.mobjHost.State != CommunicationState.Opened) return;
            try
            {
                //this.mobjHost.Close();
                this.mobjHost.Abort();
            }
            catch (Exception e)
            {
                Logger.Error("StopService", strErrorINfo = string.Format("Can't stop the services, Exception:[{0}]", e.Message), e);
                return;
            }


        }

        private void HostClosingEventImpl(object Sender, EventArgs e)
        {
            Logger.Info("HostClosingEventImpl", string.Format("Sender:[{0}] EventArgs:[{1}]", Sender, e));

        }

        private void HostClosedEventImpl(object sender, EventArgs e)
        {
            Logger.Info("HostClosedEventImpl", string.Format("Sender:[{0}] EventArgs:[{1}]", sender, e));
        }

        public ERROR_CODE StartService(string strURL)
        {
            Logger.logBegin("StartService");

            try
            {
                Uri objURL = new Uri(strURL);
                this.mobjHost = new ServiceHost(this, objURL);
                var behaviour = mobjHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();

                this.mobjHost.Closing += HostClosingEventImpl;
                this.mobjHost.Closed += HostClosedEventImpl;

                behaviour.InstanceContextMode = InstanceContextMode.Single;
                behaviour.ConcurrencyMode = ConcurrencyMode.Multiple;


                ServiceMetadataBehavior smb = mobjHost.Description.Behaviors.Find<ServiceMetadataBehavior>();

                if (smb == null)
                    smb = new ServiceMetadataBehavior();
                //smb.HttpGetEnabled = true;
                //smb.HttpGetUrl = new Uri("localhost:9888/MARSTIGFrame");

                //smb.HttpGetBinding 
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                this.mobjHost.Description.Behaviors.Add(smb);
#if TigerBuggerTrack
                mobjHost.AddServiceEndpoint(
                    ServiceMetadataBehavior.MexContractName,
                    MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

#endif
                NetTcpBinding objTCPBind = null;

                objTCPBind = new NetTcpBinding();
                if (objTCPBind.ReliableSession != null)
                {
                    objTCPBind.ReliableSession.InactivityTimeout = TimeSpan.MaxValue;
                }
                else
                {
                    Logger.Info("StartService", "ReliableSession is null");
                }
                objTCPBind.ReceiveTimeout = new TimeSpan(0, 30, 59);
                objTCPBind.SendTimeout = new TimeSpan(0, 10, 0);
                objTCPBind.CloseTimeout = new TimeSpan(0, 10, 0);
                objTCPBind.OpenTimeout = new TimeSpan(0, 10, 0);
                objTCPBind.MaxReceivedMessageSize = 10 * 1024 * 1024;
                objTCPBind.MaxBufferPoolSize = 10 * 1024 * 1024;
                objTCPBind.ReaderQuotas.MaxArrayLength = 20 * 1024 * 1024;
                objTCPBind.ReaderQuotas.MaxDepth = 20 * 1024 * 1024;
                objTCPBind.ReaderQuotas.MaxBytesPerRead = 20 * 1024 * 1024;
                objTCPBind.MaxBufferSize = 10 * 1024 * 1024;
                objTCPBind.TransferMode = TransferMode.Buffered;



                objTCPBind.Security = new NetTcpSecurity();
                objTCPBind.Security.Mode = SecurityMode.None;

                this.mobjHost.AddServiceEndpoint(typeof(IMarsTigerFrameWorkService), objTCPBind, "");

                //#if TigerBuggerTrack
                //                var dispatcher = this.mobjHost.ChannelDispatchers[0] as ChannelDispatcher;
                //                dispatcher.ChannelInitializers.Add(new TigerMarsHook());
                //#endif
                this.mobjHost.Open();

                //OperationContext.Current.Channel.Faulted += new EventHandler(WCFClientFaulted);
                //OperationContext.Current.Channel.Closed += new EventHandler(WCFClientFaulted);
                return ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                ERROR_CODE eCde = ERROR_CODE._SERVICE_ERROR_NO_SERVICE_START_UNKNOW;
                Logger.Error("StartService", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message), e);
                return eCde;
            }
            finally
            {
                Logger.logEnd("StartService");
            }
        }



        private void WCFClientFaulted(object Sender, EventArgs e)
        {
            if (Sender != null)
                Logger.Info("WCFClientFaulted", string.Format("Faulted from Client:[{0}]", ((IContextChannel)Sender).SessionId));
            else
                Logger.Info("WCFClientFaulted", "Sender is null");

        }

        public int OnGetNextTestSuite()
        {
            Logger.logBegin("OnGetNextTestSuite");
            try
            {
                miCurrentLoopCnt = -1;
                int iResult = (int)AgentOnGetNextTestSuiteEventImpl();
                if (iResult != (int)ERROR_CODE._NO_ERROR)
                {
                    return iResult;
                }
                if (this.mobjCurrentTestSuite == null)
                    return -1;
                this.mobjLoopMgr.Init();
                InitListTestSteps(this.mobjCurrentTestSuite.CurrentStepsList);
                //SetCurrentErrorInfo(iErrorStep, strCurrentError, objErrorStep);
                return iResult;
            }
            catch (Exception e)
            {
                Logger.Error("OnGetNextTestSuite", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("OnGetNextTestSuite");
            }

        }

        #region Data File service
#if _Datafrom_Database
        public OnRequireAutoGen_CurrentPegInfoEvent AutoGen_CurrentPegInfoHandler = null;
        public OnRequireAutoGen_OneTestStepEvent AutoGen_OneTestStepHandler = null;
        public bool IsContinueToTest = false;
        public bool IsIgnoreError = false;

        public OnShutdownServer OnShutdownServerHandler = null;

        public OnIsSkipStepWord OnIsSkipStepWordHandler = null;

        public OnIsVariable OnIsVariableHandler = null;

        public OnGetVariableValue OnGetVariableValueHandler = null;


        public bool GetCurrentGenerationPegQuickInfo(ref string strPegFromServer, ref string strErrorFromServer)
        {
            Logger.logBegin("GetCurrentGenerationPegQuickInfo");
            try
            {
                if (AutoGen_CurrentPegInfoHandler == null)
                {
                    strErrorFromServer = "No Deletegate attached for AutoGen_CurrentPegInfoHandler";
                    Logger.Error("GetCurrentGenerationPegQuickInfo", strErrorFromServer);
                    return false;
                }
                return AutoGen_CurrentPegInfoHandler(ref strPegFromServer, ref strErrorFromServer);
            }catch(Exception e)
            {
                Logger.Error("GetCurrentGenerationPegQuickInfo", strErrorFromServer = string.Format("Exception:[{0}]",e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("GetCurrentGenerationPegQuickInfo");
            }
            
        }

        public OnTestCaseFinishedCallBack TestCaseFinishedCallbackHandler = null;
        private void DealwithTestCaseFinishedCallBack(long lCurrentStoryDetailId)
        {
            Logger.Info("DealwithTestCaseFinishedCallBack", string.Format("time:{0}, CurrentStoryDetailId:[{1}]", DateTime.Now, lCurrentStoryDetailId));
            if (TestCaseFinishedCallbackHandler == null) return;
            try
            {
                TestCaseFinishedCallbackHandler(lCurrentStoryDetailId);
            }
            catch (Exception e)
            {
                Logger.Error("DealwithTestCaseFinishedCallBack", string.Format("Exception:[{0}]", e.Message), e);
            }
        }


        public bool AutoGen_GenStep(string strSwfName, string strType, string strTxt, ref string strError)
        {
            Logger.logBegin("AutoGen_GenStep", string.Format("strSwfName:[{0}] strType:[{1}] strTxt:[{2}]", strSwfName, strType, strTxt));
            try
            {
                if (AutoGen_OneTestStepHandler == null)
                {
                    strError = "No Delegate attached for AutoGen_OneTestStepHandler";
                    Logger.Error("AutoGen_GenStep", strError);
                    return false;
                }
                return AutoGen_OneTestStepHandler(strSwfName, strType, strTxt, ref strError);
            }catch(Exception e)
            {
                Logger.Error("AutoGen_GenStep",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("AutoGen_GenStep");
            }
            
        }

        public bool IsLoopVarApplied()
        {
            return this.mobjLoopMgr.isLoopUsedInTestCase();
        }
#if v_16AndUp
        public string GetCurrentTestDatasetName()
        {
            return this.CurrentDataSetAlias;
        }
#endif

        public int FetchDataByStepIdAndLoopId(int loop, long assignedTestStepId, ref string strError, ref string strDataResult)
        {
            Logger.logBegin("FetchDataByStepIdAndLoopId", string.Format("Loop:[{0}], stepId:[{1}]", loop, assignedTestStepId));
            try
            {
                strDataResult = "";
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                if (this.mobjCurrentTestSuite == null)
                {
                    eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                    Logger.Error("FetchDataByStepIdAndLoopId", strError = ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde));
                    return (int)eCde;
                }
                /// data from client side, loop is starting from 0
                strDataResult = this.mobjCurrentTestSuite.GetDataStringFromDataFile(string.Format("{0}_{1}", assignedTestStepId, loop + 1), loop + 1, ref eCde);
                //strDataResult = this.mobjCurrentTestSuite.GetDataStringFromDataFile(string.Format("{0}_{1}", assignedTestStepId, loop ), loop , ref eCde);
                if (eCde != ERROR_CODE._NO_ERROR)
                {
                    strDataResult = "";
                    Logger.Error("FetchDataByStepIdAndLoopId", strError = string.Format("Can't GetDataStringFromDataFile,with error code returns:[{0}]", eCde));
                    return (int)eCde;
                }

                bool isVar = this.IsVariable(strDataResult), isOk = false;
                int iVarMode = -1;
                string strObjectNameIdx = "";

                ///增加是否是设置了skip

                if (isVar)
                {
                    strObjectNameIdx = AbstractObjectNameFromVariable(strDataResult, ref iVarMode);

                    if (this.mobjLoopMgr.isLoopUsedInTestCase())
                    {
                        if (iVarMode == 6)
                        {
                            /// then data will be get from Loop vars
                            /// 
                            strDataResult = this.mobjLoopMgr.CurrentLoopData;
                        }
                    }
                    else
                    {
                        switch (iVarMode)
                        {
                            case 1:
                                strDataResult = GetReferenceDataFromDB(strObjectNameIdx, ref isOk);
                                break;
                            case 2:
                                // important!!!!
                                if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                                {
                                    Logger.Info("FetchDataByStepIdAndLoopId", string.Format("keyword is captureValues, return :[{0}]", strObjectNameIdx));
                                    break;
                                }
                                strDataResult = GetGlobeDataFromDB(strObjectNameIdx, ref isOk);
                                break;
                            case 4:
                                ///Modal var
                                /// 
                                strDataResult = GetModalVarDataFromDB(strObjectNameIdx, this.GetBaseLineMode(), ref isOk);
                                break;
                            case 8:
                                ///If var
                                /// 
                                strDataResult = GetIfVarDataFromDB(strObjectNameIdx, this.GetBaseLineMode(), ref isOk);
                                break;
                            default:
                                break;
                        }
                        if (string.Compare(this.mobjCurrentTestStep.Keyword, "Loop", true) == 0)
                        {
                            /// 说明 需要将数据导入到 loop 的管理对象中
                            /// 
                            if (iVarMode == 6)
                            {
                                //获得data
                                /// GetSystemLookup(strTableName = iVariableType == 1 ? SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL : 
                                //(iVariableType == 2 ? SystemConstant.CNST_RESERVED_VARIABLE_LOCAL : SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP), 
                                //    strObjectNameIdx) ;
                                B_SYSTEM_LOOKUP objB = new B_SYSTEM_LOOKUP();
                                List<B_SYSTEM_LOOKUP> lstLoopvarInfo = objB.GetSystemLookup(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP, strObjectNameIdx);
                                string strDataForLoop = lstLoopvarInfo == null ? "" : lstLoopvarInfo.Count <= 0 ? "" : lstLoopvarInfo[0].DISPLAY_NAME;
                                this.mobjLoopMgr.SourceData = strDataForLoop;
                            }
                        }
                    }
                }
                ///check addins
                ///                                                                   
                strDataResult = InvokeDataEventAddins(assignedTestStepId, strDataResult, ref strError);

                return (int)ERROR_CODE._NO_ERROR;

            } catch(Exception e)
            {
                Logger.Error("FetchDataByStepIdAndLoopId",string.Format("Exception:[{0}]",e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("FetchDataByStepIdAndLoopId");
            }
        }

        private bool IsTheFirstLoopRound()
        {
            if (!this.mobjLoopMgr.isInRunning())
                return true;
            return false;
        }

        private string InvokeDataEventAddins(long lStepId, string strDataSrc, ref string strError)
        {
            Logger.Info("InvokeDataEventAddins", string.Format(@"try to convert data by addins if exists, stepId:[{0}],strDataSrc:[{1}]
Keyword:[{2}] ObjectHappyName:[{3}], RC:[{4}]", lStepId, strDataSrc, this.mobjCurrentTestStep.Keyword, this.mobjCurrentTestStep.ObjectName, this.mobjCurrentTestStep.Row_Column));
            string strTargetData = strDataSrc;
            if (!mobjPlugins.DealWithEvent(EMars_PluginSensitive.E_SensitiveFor_AfterGetDataSet, this.mobjCurrentTestStep.Keyword, this.mobjCurrentTestStep.ObjectName, this.mobjCurrentTestStep.Row_Column,
                strDataSrc, ref strError, ref strTargetData))
            {
                Logger.Error("InvokeDataEventAddins", strError = string.Format("Error comes from DealWithEvent:[{0}]", strError));
                return "";
            }
            Logger.Info("InvokeDataEventAddins", string.Format("converted from [{0}] to [{1}]", strDataSrc, strTargetData));
            return strTargetData;
        }

#endif
        public bool isWriteBackKeyword(string strKeywordName)
        {
            foreach (string strItm in SystemConstant.CNST_ARR_FEEDBACKFUNCTIONS)
            {
                if (string.Compare(strKeywordName, strItm, true) == 0) return true;
            }
            return false;
        }

        public string GetDataStringFromDataFile(string strObjectName, int iLoopId, ref int eCde, int iStepId = -1)
        {
            Logger.Info("GetDataStringFromDataFile", string.Format("strObjectName:[{0}],iLoopId:[{1}], iStepId:[{2}]", strObjectName, iLoopId, iStepId));

            /// =====================================================
            /// create a reference mode check here
            /// and then create if clause and return 
            try
            {
                if (this.mobjCurrentTestSuite == null)
                {
                    eCde = (int)ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                    Logger.Error("GetDataStringFromDataFile", ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde));
                    return null;
                }
                ERROR_CODE eCdeTmp = ERROR_CODE._NO_ERROR;
#if !_Datafrom_Database
            string strData = this.mobjCurrentTestSuite.GetDataStringFromDataFile(strObjectName, iLoopId, ref eCdeTmp);
#else
                string strData = "";
                if (iStepId == -1)
                    strData = this.mobjCurrentTestSuite.GetDataStringFromDataFile(strObjectName, iLoopId + 1, ref eCdeTmp);
                else
                    strData = this.mobjCurrentTestSuite.GetDataStringFromDataFile(strObjectName, iLoopId + 1, ref eCdeTmp, iStepId);
                /// 
                /// 
                #region  //Loop part
                LoopInfoCheck(strData);
                #endregion

                //Logger.Info("GetDataStringFromDataFile",string.Format("Get Data for test step:[{0}]\r\n--------", strData));
                int iRefMode = 0;
                string strDataSrc = strData;
                if (((iStepId != -1) && checkReferenceDataMode(strData, ref strData, iStepId, ref iRefMode))
                    ||(((iStepId == -1) && checkReferenceDataMode(strData, ref strData, ref iRefMode))))
                {

                    //((BatchConfigObjectFromDB)((TCObjects4DB)mobjCurrentTestSuite).AssignedStoryBoardInfo)
                    Logger.Info("GetDataStringFromDataFile", string.Format("reference mode:[{0}] refMode:[{1}]", strData, iRefMode));
                    bool isOk = false;
                    switch (iRefMode)
                    {
                        case 1:
                            strData = GetReferenceDataFromDB(strData, ref isOk);
                            break;
                        case 2:
                            // important!!!!
                            if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                            {
                                Logger.Info("GetDataStringFromDataFile", string.Format("keyword is captureValues, return :[{0}]", strDataSrc));
                                return strDataSrc;
                            }
                            strData = GetGlobeDataFromDB(strData, ref isOk);
                            break;
                        case 3:
                            Logger.Warnning("GetDataStringFromDataFile", "Not implement!");
                            break;
                        case 4:
                            Logger.Info("GetDataStringFromDataFile", string.Format("Modal variable, keyword[{0}]", this.mobjCurrentTestStep.Keyword));
                            // important!!!!
                            if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                            {
                                Logger.Info("GetDataStringFromDataFile", string.Format("keyword is captureValues, return :[{0}], MODE:[4]", strDataSrc));
                                return strDataSrc;
                            }
                            strData = GetModalVarDataFromDB(strData, this.GetBaseLineMode(), ref isOk);
                            break;
                        case 5: //prefix from sequence
                            Logger.Info("GetDataStringFromDataFile", string.Format("Pre fix from SEQ, keyword:[{0}]", this.mobjCurrentTestStep.Keyword));
                            if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                            {
                                Logger.Info("GetDataStringFromDataFile", string.Format("keyword is captureValues, return :[{0}], MODE:[4]", strDataSrc));
                                return strDataSrc;
                            }
                            strData = GetSeqVarDataFromDB(strData, ref isOk);
                            break;
                        case 6:
                            Logger.Info("GetDataStringFromDataFile", string.Format("Loop var, keyword:[{0}]", this.mobjCurrentTestStep.Keyword));
                            if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                            {
                                Logger.Info("GetDataStringFromDataFile", string.Format("keyword is captureValues, return :[{0}], MODE:[6]", strDataSrc));
                                return strDataSrc;
                            }
                            strData = GetLoopVarData(strData, ref isOk);
                            break;
                        case 8:
                            Logger.Info("GetDataStringFromDataFile", string.Format("IF var, keyword:[{0}]", this.mobjCurrentTestStep.Keyword));
                            if (isWriteBackKeyword(this.mobjCurrentTestStep.Keyword))
                            {
                                Logger.Info("GetDataStringFromDataFile", string.Format("keyword is captureValues, return :[{0}], MODE:[8]", strDataSrc));
                                return strDataSrc;
                            }
                            strData = GetIfVarDataFromDB(strData, this.GetBaseLineMode(), ref isOk);
                            break;
                    }
                }
                
                //Logger.Info("GetDataStringFromDataFile",string.Format("Get data(GetReferenceDataFromDB/GetGlobeDataFromDB):[{0}] iRefMode:[{1}]", strData, iRef));
                string strError = "";
                strData = InvokeDataEventAddins(iStepId, strData, ref strError);
#endif
                eCde = (int)eCdeTmp;                
                return strData;
            } catch (Exception e)
            {
                Logger.Error("GetDataStringFromDataFile",string.Format("Exception:[{0}]",e.Message),e);
                eCde = (int)ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                return null;
            }
            finally
            {
                Logger.logEnd("GetDataStringFromDataFile");
            }
        }
        private string GetLoopVarData(string strData, ref bool isOk)
        {
            isOk = true;
            Logger.Info("GetLoopVarData", string.Format("Data:[{0}]", strData));
            if (this.mobjLoopMgr == null) return null;
            return this.mobjLoopMgr.CurrentLoopData;
        }

#if _Datafrom_Database
        private string GetGlobeDataFromDB(string strData, ref bool isOk)
        {
            Logger.Info("GetGlobeDataFromDB", string.Format("Data to get from GlobaVar :{0}", strData));
            string strError = "";
            string strResult = "";
            isOk = BoHelper.GetGlobalVariableInfo(strData, ref strError, ref strResult);
            if (!isOk)
            {
                Logger.Error("GetGlobeDataFromDB", string.Format("Error:[{0}]", strError));
            }
            return strResult;
        }



        private string GetSeqVarDataFromDB(string strData, ref bool isOk)
        {
            Logger.Info("GetSeqVarDataFromDB", string.Format("Data:[{0}]", strData));
            try
            {
                string strError = "";
                string strResult = "";
                int iN = -1;
                /// format abc_$ then $ will be replaced by new number
                isOk = BoHelper.GetBussinessSeq(ref iN, ref strError);
                if (strData.Contains("$"))
                {
                    strResult = strData.Replace("$", iN + "");
                }
                else strResult = string.Format("{0}{1}", strData, iN);
                return strResult;
            }
            catch (Exception e)
            {
                Logger.Error("GetSeqVarDataFromDB", string.Format("exception:[{0}]", e.Message));
                isOk = false;
                return strData;
            }
        }

        private string GetIfVarDataFromDB(string strData, string strTstMode, ref bool isOk)
        {
            Logger.Info("GetIfVarDataFromDB", string.Format("Data:[{0}] TestMode:[{1}]", strData, strTstMode));
            string strResult = "", strError = "";
            short sStatues = 1; // base line mode

            isOk = BoHelper.GetIFVariableInfo(currentDBIdx, strData, sStatues, ref strError, ref strResult);
            if (!isOk)
            {
                Logger.Error("GetModalVarDataFromDB", string.Format("Error:[{0}]", strError));
            }
            return strResult;
        }
        private string GetModalVarDataFromDB(string strData, string strTstMode, ref bool isOk)
        {
            Logger.Info("GetModalVarDataFromDB", string.Format("Data:[{0}] TestMode:[{1}]", strData, strTstMode));
            string strResult = "", strError = "";
            short sStatues = 1; // base line mode
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, strTstMode, true) == 0)
            {
                sStatues = 1;
            }
            else
            {
                if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE, strTstMode, true) == 0)
                {
                    sStatues = 2;
                }
                else
                {
                    Logger.Error("GetModalVarDataFromDB", string.Format("Test mode should be in [{0} or {1}],but the value is:[{2}]",
                        SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE,
                        SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE,
                        strTstMode));
                    return "";
                }
            }

            isOk = BoHelper.GetModalVariableInfo(strData, sStatues, ref strError, ref strResult);
            if (!isOk)
            {
                Logger.Error("GetModalVarDataFromDB", string.Format("Error:[{0}]", strError));
            }
            return strResult;
        }


        protected string GetReferenceDataFromDB(string strObjectName, ref bool isRight)
        {
            Logger.Info("GetReferenceDataFromDB", string.Format("ObjectName Index:[{0}]", strObjectName));
            if (!(mobjCurrentTestSuite is TCObjects4DB))
            {
                Logger.Error("object type is not or desn't deveried from TCObjects4DB:[{0}]", mobjCurrentTestSuite.GetType().ToString());
                isRight = false;
                return "";
            }
            TCObjects4DB obj4DB = (TCObjects4DB)this.mobjCurrentTestSuite;
            if (obj4DB.AssignedStoryBoardInfo == null)
            {
                Logger.Error("GetReferenceDataFromDB", "assigned storyboard object is null");
                isRight = false;
                return "";
            }
            if (!(obj4DB.AssignedStoryBoardInfo is BatchConfigObjectFromDB))
            {
                Logger.Error("GetReferenceDataFromDB", string.Format("assigned storyboard object is not BatchConfigObjectFromDB:[{0}]", obj4DB.AssignedStoryBoardInfo.GetType().ToString()));
                isRight = false;
                return "";
            }
            BatchConfigObjectFromDB objStoryInfo = (BatchConfigObjectFromDB)obj4DB.AssignedStoryBoardInfo;
            if (objStoryInfo.AssignedStoryObject == null)
            {
                Logger.Error("GetReferenceDataFromDB", "AssignedStoryObject is null");
                isRight = false;
                return "";
            }
            string strData = BoHelper.GetDynamicDataByStoryBoardInfoAndObjectName(
                currentDBIdx,
                objStoryInfo.AssignedStoryObject.STORYBOARD_DETAIL_ID,
                objStoryInfo.AssignedStoryObject.DATA_SETTING_ID,
                strObjectName,
                this.currentStoryHis.BaseLineTestId,
                this.currentStoryHis.CurrentLoop,
                ref isRight);
            return strData;
        }

        protected bool checkReferenceDataMode(string strData, ref string strResult, ref int iRefModeId)
        {
            Logger.Info("checkReferenceDataMode", string.Format("Data:[{0}] stepId:[{1}]", strData, strResult));
            strResult = strData;
            if (string.IsNullOrEmpty(strData)) return false;            
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX, strData.Trim()))
            {
                strResult = strData.Replace(SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX, "");
                iRefModeId = 1;
                return true;
            }
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL, strData.Trim()))
            {
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL + ":", "");
                iRefModeId = 2;
                return true;
            }
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_LOCAL, strData.Trim()))
            {
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL + ":", "");
                iRefModeId = 3;
                return true;
            }
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strData.Trim()))
            {
                iRefModeId = 4;
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_MODAL + ":", "");
                return true;
            }

            if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ, strData.Trim())))
            {
                iRefModeId = 5;
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ + ":", "");
                return true;
            }

            if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP, strData.Trim())))
            {
                iRefModeId = 6;
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP + ":", "");
                return true;
            }
            if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_IFVAR, strData.Trim())))
            {
                iRefModeId = 8;
                strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_IFVAR + ":", "");
                return true;
            }
            return false;
        }
        /// <summary>
        /// modified: 
        ///     2016-11-14
        ///     Tiger
        /// Reason:
        ///     reference mode is required. 
        /// </summary>
        /// <param name="strData"></param>
        /// <param name="strResult"></param>
        /// <param name="iStepId"></param>
        /// <param name="iRefModeId">1, means normal reference, 2, global variable, 3, local variable</param>
        /// <returns></returns>
        protected bool checkReferenceDataMode(string strData, ref string strResult, int iStepId, ref int iRefModeId)
        {
            Logger.Info("checkReferenceDataMode", string.Format("Data:[{0}] stepId:[{1}]", strData, strResult));
            strResult = strData;
            if (string.IsNullOrEmpty(strData)) return false;
            if (iStepId < 0) return false;
            return checkReferenceDataMode(strData,ref strResult, ref iRefModeId);
            #region old code replaced by function above
            //if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX, strData.Trim()))
            //{
            //    strResult = strData.Replace(SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX, "");
            //    iRefModeId = 1;
            //    return true;
            //}
            //if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL, strData.Trim()))
            //{
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL + ":", "");
            //    iRefModeId = 2;
            //    return true;
            //}
            //if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_LOCAL, strData.Trim()))
            //{
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL + ":", "");
            //    iRefModeId = 3;
            //    return true;
            //}
            //if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strData.Trim()))
            //{
            //    iRefModeId = 4;
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_MODAL + ":", "");
            //    return true;
            //}

            //if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ, strData.Trim())))
            //{
            //    iRefModeId = 5;
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ + ":", "");
            //    return true;
            //}

            //if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP, strData.Trim())))
            //{
            //    iRefModeId = 6;
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP + ":", "");
            //    return true;
            //}
            //if ((TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_IFVAR, strData.Trim())))
            //{
            //    iRefModeId = 8;
            //    strResult = strData.Replace(SystemConstant.CNST_RESERVED_VARIABLE_IFVAR + ":", "");
            //    return true;
            //}
            //return false;
            #endregion
        }
#endif

        public bool IsDataSetSet2Skipped(int iLoopID)
        {
            Logger.logBegin("IsDataSetSet2Skipped");
            int eCde;
            if (this.mobjCurrentTestSuite == null)
            {
                eCde = (int)ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                Logger.Error("GetDataStringFromDataFile", ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde));
                return false;
            }

            bool isSkip = this.mobjCurrentTestSuite.IsDataSetSet2Skipped(iLoopID);
            Logger.Info("IsDataSetSet2Skipped", string.Format("returns [{0}]", isSkip));
            Logger.logEnd("IsDataSetSet2Skipped");
            return isSkip;
        }

#if _Datafrom_Database
        public bool GetIgnoreErrorStatus()
        {
            return this.IsIgnoreError;
        }
#endif

        private string StoreDataLock = "";

        public int StoreDataBack(string strDBIdx,string strObjectNameIdx, string strData2Store, int iLoop)
        {
            Logger.Info("StoreDataBack", string.Format("objNameIdx:[{0}] Data2Store:[{1}], iLoop:[{2}]", strObjectNameIdx, strData2Store, iLoop));
            int eCde;
            Monitor.Enter(StoreDataLock);
            try
            {
                if (this.mobjCurrentTestSuite == null)
                {
                    eCde = (int)ERROR_CODE._SERVICE_ERROR_SERVER_NO_TESTSUITE_SETTING;
                    Logger.Error("StoreDataBack", ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde));
                    return eCde;
                }
                ERROR_CODE eCdeTmp = ERROR_CODE._NO_ERROR;
#if _Datafrom_Database
                if (mobjCurrentTestSuite is TCObjects4DB)
                {
                    #region Variable deal
                    bool isVar = this.IsVariable(strObjectNameIdx);
                    bool isUpdateVar = false;
                    int iVarMode = -1;
                    //string strObjectNameAdjusted = strObjectNameIdx;
                    if (isVar)
                    {
                        strObjectNameIdx = AbstractObjectNameFromVariable(strObjectNameIdx, ref iVarMode);
                        /// update globe values
                        /// 
                        string strError = "";
                        // for modal variable
                        string strBaseLineMode = AppConfigReader.GetBaseLineMode(WCFXmlCfgMgr.CurrentLoginUser);
                        isUpdateVar = currentStoryHis.updateVariableValue(strDBIdx,strObjectNameIdx, strData2Store, iLoop, iVarMode, ref strError, strBaseLineMode);
                        if (!isUpdateVar)
                        {
                            eCde = (int)ERROR_CODE._SERVICE_ERROR_CANT_CREATE_GLOBEVAR_FOR_SYSLOOKUP_2;
                            Logger.Error("StoreDataBack", string.Format(ERROR_INFO.GET_ERROR_STR((ERROR_CODE)eCde), strObjectNameIdx, strError));
                        }
                    }
                    #endregion
                    currentStoryHis.StoreData4ForTestReport_Steps(strObjectNameIdx, strData2Store, iLoop, ((TCObjects4DB)mobjCurrentTestSuite).TestCaseKeyId,
                        this.mobjCurrentTestStep.AssignedTestStepId,currentDBIdx);
                    //((TCObjects4DB)mobjCurrentTestSuite).SaveDataTo(strObjectNameIdx,);
                }
                else
                {
#endif
                    eCdeTmp = this.mobjCurrentTestSuite.SaveDataToSpecialCell(strObjectNameIdx, iLoop, strData2Store);
                    if (eCdeTmp == ERROR_CODE._NO_ERROR)
                    {
                        /**Create externel infor storing orignal data**/
                        string strBaseInfo = this.GetBaseLineMode();
                        if (SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD.CompareTo(strBaseInfo) == 0)
                        {
                        }

                    }
#if _Datafrom_Database
                }
#endif
                
                return (int)eCdeTmp;
            }
            catch (Exception e)
            {
                Logger.Error("StoreDataBack", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Monitor.Exit(StoreDataLock);
                Logger.logEnd("StoreDataBack");
            }
        }


        public int CompareGuiDataByLoopId(string strLoopId)
        {
            return (int)ERROR_CODE._NO_ERROR;
        }

        #endregion //Data File service


        internal void SetCurrentTestSuiteObject(TCObjects objTestSuit)
        {
            mobjCurrentTestSuite = objTestSuit;
        }

        public int SwitchDataFile(string strDataFileName)
        {
            Logger.logBegin("SwitchDataFile");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                eCde = this.mobjCurrentTestSuite.SwitchCurrentDataFile(strDataFileName);                
                return (int)eCde;
            }catch(Exception e)
            {
                Logger.Error("SwitchDataFile",string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("SwitchDataFile");
            }
        }

        public int StoreDataBackComparisonMode(string strTestCaseName, string strValueWithSetting, string strValue, int iLoop, string strBaseLineMode, bool isComparison = false)
        {
            Logger.logBegin("StoreDataBackComparisonMode");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                string strObjectNameIdx = "";
                strObjectNameIdx = this.mobjCurrentTestSuite.GetObjectNameFromComparisonMode(strValueWithSetting, ref eCde);
                if (eCde != ERROR_CODE._NO_ERROR) return (int)eCde;
                eCde = this.mobjCurrentTestSuite.SaveDataToSpecialCellComparisonMode(strObjectNameIdx, strValueWithSetting, strValue, iLoop);
                if (eCde != ERROR_CODE._NO_ERROR) return (int)eCde;
                
                return (int)eCde;
            }catch(Exception e)
            {
                string.Format("StoreDataBackComparisonMode",string.Format("Exception:[{0}]", e.Message),e);
                return -1;
            }
            finally
            {
                Logger.logEnd("StoreDataBackComparisonMode");
            }
            
        }


        private void SetCurrentErrorInfo(int iErrorStep, string strCurrentError, TestStep objErrorStep)
        {
            this.miCurrentErrorId = iErrorStep;
            this.mstrCurrentErrorInformation = strCurrentError;
            this.mobjCurrentErrorStep = objErrorStep;
        }

        internal static void ChangeBaseLineMode(string strBaseLineMode)
        {
            AppConfigReader.SetAppSetting(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, strBaseLineMode);
        }

        internal static bool GetBaseLineConfigValue()
        {
            return AppConfigReader.GetBaseLineConfigValue();
        }

        internal void ChangeDefaultApplication(string strDefaultApplication)
        {
            AppConfigReader.SetAppSetting(SystemConstant.CNST_APPCONFIG_APPSETTING_DEFAULTAPP, strDefaultApplication);
        }


        public bool ShutdownServer()
        {
            bool rc = true;
            Logger.logBegin("ShutdownServer");
            OnShutdownServerHandler();
            Logger.logEnd("ShutdownServer");

            return rc;
        }

        public bool IsSkipStepWord(string word)
        {
            return OnIsSkipStepWordHandler(word);
        }

#if v_16AndUp
        private const string KEY_WORD_RESUMENEXT = "ResumeNext";
        public bool hasResumInfoFromCurrent()
        {
            Logger.logBegin("hasResumInfoFromCurrent");

            return getNextResumeBlockStepIdx() > 0;
        }

        private int getNextResumeBlockStepIdx()
        {
            for (int i = this.miCurrentStepId; i < mlstSteps.Count; i++)
            {
                TestStep objItm = (TestStep)mlstSteps[i];
                if (string.Compare(objItm.Keyword, KEY_WORD_RESUMENEXT, true) == 0)
                {
                    Logger.Info("hasResumInfoFromCurrent", string.Format("Find test step with test step id:[{0}]", objItm.RunID));
                    return i;
                }
            }
            return -1;
        }
        public bool jumpToNextResumeBlock()
        {
            Logger.logBegin("jumpToNextResumeBlock");
            int iResumeBlckId = getNextResumeBlockStepIdx() + 1;
            if (iResumeBlckId <= 0)
            {
                Logger.Error("jumpToNextResumeBlock", string.Format("the next resume block index :[{0}]", iResumeBlckId));
                return false;
            }
            miCurrentStepId = iResumeBlckId;
            return true;
        }
#endif
        /// <summary>
        /// To abstract object Name from variable setting
        /// </summary>
        /// <param name="strObj">data should be start with Globe_var or local_var </param>
        /// <returns></returns>
        private string AbstractObjectNameFromVariable(string strObj, ref int iVarMode)
        {
            Logger.Info("ObjectNameDealForVariable", string.Format("strObj To be Abstract:[{0}]", strObj));
            int iPos = strObj == null ? -1 : strObj.IndexOf(":");

            if (iPos <= -1)
            {
                Logger.Info("ObjectNameDealForVariable", string.Format("Can't find : from [{0}]", strObj));
                return strObj;
            }

            string strVarInfo = strObj.Substring(0, iPos);
            if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL, strVarInfo, true) == 0)
            {
                iVarMode = 1;
            }
            else
            {
                if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strVarInfo, true) == 0)
                {
                    iVarMode = 4;
                }
                else
                {
                    if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP, strVarInfo, true) == 0)
                    {
                        iVarMode = 6;
                    }
                    else
                        if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_IFVAR, strVarInfo, true) == 0)
                        iVarMode = 8;
                    else
                        iVarMode = 1;
                }
            }
            return strObj.Substring(iPos + 1);
        }

        public bool IsVariable(string word)
        {
            bool rc = false;
            if (string.IsNullOrEmpty(word)) return false;
            rc = OnIsVariableHandler(word);
            Logger.Info("IsVariable", "return " + rc);
            return rc;
        }

        public string GetVariableValue(string variable)
        {
            string value = "";

            value = OnGetVariableValueHandler(currentDBIdx, variable);
            Logger.Info("GetVariableValue", "return " + value);
            return value;
        }

        public void UploadPicInfo4CurrentTestStep(byte[] arrData)
        {
            Logger.Info("UploadPicInfo4CurrentTestStep", string.Format("try to store data to current step, len:[{0}]", arrData == null ? 0 : arrData.Length));
            if (this.mobjCurrentTestStep == null)
            {
                Logger.Warnning("UploadPicqauserInfo4CurrentTestStep", "current step is null");
                return;
            }
            try
            {
                //byte[] arrD = new byte[arrData.Length];

                //System.IO.FileStream fs = new System.IO.FileStream()
                Logger.Info("UploadPicInfo4CurrentTestStep", string.Format("Try to copy and assign bytes [{0}]", arrData.Length));

                this.mobjCurrentTestStep.PicInfo = new byte[arrData.Length];
                arrData.CopyTo(this.mobjCurrentTestStep.PicInfo, 0);
                Logger.Info("UploadPicInfo4CurrentTestStep", string.Format("end copy bytes:[{0}]", this.mobjCurrentTestStep.PicInfo.Length));
                ////Buffer.BlockCopy(arrData, 0, this.mobjCurrentTestStep.PicInfo, 0, this.mobjCurrentTestStep.PicInfo.Length);
                return;
            }
            catch (Exception e)
            {
                Logger.Error("UploadPicInfo4CurrentTestStep", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                return;
            }

        }

        #region Pluginss
        protected void LoadMarsPlugins()
        {
            Logger.logBegin("LoadMarsPlugins");

            mobjPlugins.LoadPluginsConfig();
        }
        #endregion

        #region Loop Mgr
        private void LoopInfoCheck(string strDataForLoop)
        {

        }

        private void InitializeLoopInfo()
        {
            Logger.Info("InitializeLoopInfo", "Begin");
            this.mobjLoopMgr.Init();
        }
        #endregion


        public bool IsAutoCheckErrorEnable()
        {
            return AppConfigReader.IsAutoCheckErrorEnable();
        }

    }

}

