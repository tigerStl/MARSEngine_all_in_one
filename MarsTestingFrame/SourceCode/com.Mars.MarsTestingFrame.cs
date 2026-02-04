using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using com.Mars.Constants;
using Route2NSEx.src.Marquis.systemUtil;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using MarsTestFrame.SourceCode.com.Mars.Compiler;
using MarsTestFrame.SourceCode.systemUtil;
using com.Mars.Config;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.com.Mars.TCDataSource;
using MarsTestFrame.CommuniteServer;
using System.Threading;
using System.Diagnostics;
using System.Data.OleDb;
using MarsTestFrame.SourceCode.com.Mars.Excels;
using MarsTestFrame.SourceCode.com.Mars.Excels.DB;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Configuration;
using System.Collections.Specialized;
using MarsTestFrame.SourceCode.xmlConfig;
#if _Datafrom_Database
using MarsTestFrame.systemUtil;
using Mars.DataLayer;
#endif

namespace com.Mars.MarsTestingFrame
{
    public class MarsTestFrameMain
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTestFrameMain));
#if _Datafrom_Database
        private BatchXls mobjXlsBatch = DashBoardFactory.GetDashBoardViaCfg();
#else
        private BatchXls mobjXlsBatch = new BatchXls();
#endif
        private TestCaseCompilerMainEntry mobjCompiler = null;
        private MarsTestFrameCommuniteServer mobjFrameworkService = new MarsTestFrameCommuniteServer();

        private string mstrCurrentBatchFileName = null;
        private ERROR_CODE meCdeCurrentServerError =ERROR_CODE._NO_ERROR ;

        public string CurrentTestProjectName { get;set;}
        private string currentTestApplicationShortName;
        public string CurrentTestApplicationShortName { get { return currentTestApplicationShortName; }
            set {
                currentTestApplicationShortName = value;
                AppConfigReader.WriteCurrentApplicationShortName(currentTestApplicationShortName);
            } }
        #region datafrom DAtabase
#if _Datafrom_Database
        public MarsKeyValues<string,string> CurrentTestApplicationInfo { get; set; }

        public delegate void OnCommServerShutdown();

        public OnCommServerShutdown 
            CommServerShutdownHandler = null;

        public bool isContinueToTestMode ;
        private bool isIgnoreError;
        public bool IsIgnoreError
        {
            get { return isIgnoreError; }
            set { isIgnoreError = value; }
        }

       

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
        private string currentDBIdx = "MarsEntities";
        public MarsTestFrameMain(string strDBIdx)
        {
            currentDBIdx = strDBIdx;
            mobjCompiler = new TestCaseCompilerMainEntry(currentDBIdx);
            mobjFrameworkService.OnShutdownServerHandler = ShutdownServer;
            mobjFrameworkService.OnIsSkipStepWordHandler = IsSkipStepWord;

            mobjFrameworkService.OnGetVariableValueHandler = GetVariableValue;
            mobjFrameworkService.OnIsVariableHandler = IsVariable;


            //ShutdownServerHandler = ShutdownServer;
        }

#endif
        #endregion//Data From Database


        #region Debug information
        private string mstrDebugModeInfo = SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE_NONE; //default value
        #endregion //Debug


        #region TestStep 
#if _TestStepUnit
        public void CreateAutoGenTestStepsCmd()
        {
            try
            {
                Logger.logBegin("CreateAutoGenTestStepsCmd");
                if (mobjFrameworkService==null) return;
                mobjFrameworkService.CreateAutoGenTestStepsCmd();
            }
            finally
            {
                Logger.logEnd("CreateAutoGenTestStepsCmd");
            }
        }


        public ERROR_CODE CreateTestStoryboardCmd()
        {
            Logger.logBegin("CreateTestStoryboardCmd");
            int iErrorId = mobjFrameworkService == null ? -1 : mobjFrameworkService.CreateTestStorybardCmd();
            return ERROR_CODE._NO_ERROR;
        }

        public MarsFrameWorkServicesMode current_FrameworkSvsMode
        {
            get
            {
                if (mobjFrameworkService == null) return MarsFrameWorkServicesMode._svcmode_Default;
                return mobjFrameworkService.CurrerntServiceMode;
            }
            set
            {
                if (mobjFrameworkService == null) return;
                mobjFrameworkService.CurrerntServiceMode = value;
            }
        }
        
#endif
        public OnAddTestStepUnitObjEvent onAddTeststepUnitObjHandler
        {
            get { return mobjFrameworkService == null ? null : (OnAddTestStepUnitObjEvent)mobjFrameworkService.onAddTestStepUnitObjImpl; }
        }

        public OnTestResultMessageArrivedEvent onTestResultMessageArrivedHandler
        {
            get { return mobjFrameworkService == null ? null : (OnTestResultMessageArrivedEvent)mobjFrameworkService.OnTestResultMessageArrivedHandler; }
            set {
                if (mobjFrameworkService != null)
                {
                    mobjFrameworkService.OnTestResultMessageArrivedHandler = value;
                }
            }
        }


        #endregion //TestStep
        public ERROR_CODE RunTestBatchFileByThread(string strFileNameWithPath, FrameWorkStartMode startMode = FrameWorkStartMode.FWSM_Normal ,
            string strProjectID=null,string strApplicationID=null, bool isBaseLineTest = false)
        {

            Logger.Info("----", string.Format("CurrentContextId:[{0}]\r\n", Thread.CurrentContext.ContextID, Thread.CurrentContext.ToString()));

            AppConfigReader.WriteCurrentApplicationShortName(CurrentTestApplicationShortName);

#if _Datafrom_Database
            if (!DashBoardFactory.IsDashBoardFromDB())
            {
                mstrCurrentBatchFileName = strFileNameWithPath;
                this.StartBatchFile(startMode);
                return meCdeCurrentServerError;
            }
            /// Database mode
            /// start Test by DashBoardID
            /// 

            meCdeCurrentServerError = RunTestBatchFile(startMode,strProjectID,strApplicationID,isBaseLineTest);
            return meCdeCurrentServerError;
#else
            mstrCurrentBatchFileName = strFileNameWithPath;
            this.StartBatchFile(startMode);
            return meCdeCurrentServerError;
#endif

        }

        private void StartBatchFile(FrameWorkStartMode startMode = FrameWorkStartMode.FWSM_Normal)
        {
            meCdeCurrentServerError = RunTestBatchFile(startMode);
        }


#if _Datafrom_Database
        public string OnRequireCurrentApplicationIDImpl()
        {
            if (mobjXlsBatch == null) return null;
            if (mobjXlsBatch is DashBoardFromDB)
            {
                return ((DashBoardFromDB)mobjXlsBatch).CurrentApplicationID;
            }
            return null;
        }
        public OnTestCaseFinishedCallBack Gui_TestCaseFinishedCallBackHandler = null;
        private void OnTestCaseIsDoneImpl(long lStoryboardDetailId)
        {
            if (Gui_TestCaseFinishedCallBackHandler == null) return;
            Gui_TestCaseFinishedCallBackHandler(lStoryboardDetailId);
        }

#endif

        public bool IsFrameworkRunning
        {
            get
            {
                return this.mobjFrameworkService == null ? false : this.mobjFrameworkService.IsTestFrameworkRunning;
            }
        }

        public void StartServiceWithMode(FrameWorkStartMode fsm,string strApplicationId,ref string strError)
        {
            ERROR_CODE ecde = RunTestBatchFile(fsm, null, strApplicationId, false);
            if (ecde!=ERROR_CODE._NO_ERROR)
            {

            }
        }

        /***
         * This is the main entry for testframe work
         * */
        private ERROR_CODE RunTestBatchFile(            
            FrameWorkStartMode startMode = FrameWorkStartMode.FWSM_Normal,
            string strProjectID="45", 
            string strApplicationID="1",
            bool isBaseLineTest=false)
        {

            Logger.logBegin("RunTestBatchFile");
            string strFileNameWithPath = this.mstrCurrentBatchFileName;
            string strError = "";
            if (this.mobjFrameworkService.IsTestFrameworkRunning)
            {
                this.mobjFrameworkService.StopService(ref strError);
            }
            /**
             * Steps:
             * 1, Open BatchQtp.xls
             * 2, Read Runnable items
             */
            mobjXlsBatch.XlsFileNameWithPath = strFileNameWithPath;
            
#if _Datafrom_Database
            if (mobjXlsBatch is DashBoardFromDB)
            {
                mobjXlsBatch.CurrentTestProjectNameID = strProjectID;
                ((DashBoardFromDB)mobjXlsBatch).CurrentApplicationID = strApplicationID;
            }
            else
            {
                mobjXlsBatch.CurrentTestProjectNameID = this.CurrentTestProjectName;
            }
            this.mobjFrameworkService.AutoGen_CurrentPegInfoHandler = this.AutoGen_CurrentPegInfoHandler;
            this.mobjFrameworkService.AutoGen_OneTestStepHandler = this.AutoGen_GenStepHandler;
            this.mobjFrameworkService.IsContinueToTest = this.isContinueToTestMode;
            this.mobjFrameworkService.IsIgnoreError = this.isIgnoreError;
            

#endif
            ERROR_CODE eCode = ERROR_CODE._NO_ERROR;
            
            /// None auto Generation Script Mode            
            if ((startMode != FrameWorkStartMode.FWSM_AUTOGEN_SCRIPTS)&&(startMode!=FrameWorkStartMode.FWSM_STEPMODE))
            {
                eCode = mobjXlsBatch.loadTestCase();
                if (eCode != ERROR_CODE._NO_ERROR)
                {
                    Logger.Error("RunTestBatchFile", string.Format("Finished Load Test suites, but Errors come, error code[{0:X}], error:[{1}]", eCode, ERROR_INFO.GET_ERROR_STR(eCode)));
                    return eCode;
                }

                this.mstrDebugModeInfo = AppConfigReader.GetDebugModeInfo();
                this.mstrDebugModeInfo = this.mstrDebugModeInfo ?? SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE_NONE;

                /*** get each test suite and run it ***/
                /*mobjXlsBatch.BeginRunTestSuit();
                TCObjects objTestSuit = mobjXlsBatch.getNextTCObject() ;
                int iErrorStep = 0;
                TestStep objErrorStep=null ;
                string strCurrentError = "";
                */
                this.mobjFrameworkService.CurrentTestApplicationShortName = this.CurrentTestApplicationShortName;
                /*** Init all delegate ***/
                this.mobjFrameworkService.AddGetCurrentTestCaseByTSAndTCEventHandler(mobjXlsBatch.GetCurrentTestCaseByTestSuiteNameAndTestCase, true);
                this.mobjFrameworkService.AddOnNavigateHandler(mobjXlsBatch.OnNavigateHandlerImpl, true);
                this.mobjFrameworkService.AddOnBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler(mobjXlsBatch.OnNavigateWithRelyIdAndLoopIdHandlerImpl, true);
                this.mobjFrameworkService.AddOnGetNextTestSuiteEvent(mobjXlsBatch.OnGetNextTestSuiteEventImpl, true);
                this.mobjFrameworkService.AddOnLoadcurrentTestSuiteEvent(mobjXlsBatch.OnLoadCurrentTestSuiteEventImpl, true);
                this.mobjFrameworkService.AddOnTestSuiteIsDoneEventHandler(mobjXlsBatch.OnTestSuiteIsDoneEventImpl, true);
#if v_useNameId
                long lAppId = -1;
                long.TryParse(strApplicationID,out lAppId);
                this.mobjFrameworkService.CurrentApplicationId = lAppId;
#endif
#if _Datafrom_Database
                /// set the current application ID to Compiler
                /// 
                mobjCompiler.onRequireCurrentApplicationIDHandler -= this.OnRequireCurrentApplicationIDImpl;
                mobjCompiler.onRequireCurrentApplicationIDHandler += this.OnRequireCurrentApplicationIDImpl;

                this.mobjFrameworkService.NewTestStoryBoardBegins(currentDBIdx, strProjectID, isBaseLineTest);
                this.mobjFrameworkService.TestCaseFinishedCallbackHandler = OnTestCaseIsDoneImpl;
#endif
                this.mobjFrameworkService.AddOnCompileListEventHandler(mobjCompiler.preComplierTestSteps, true);
            }

            /*** start service ***/
            try
            {
                string strServiceURL = GetConfigServerURLInfo();

                eCode = this.mobjFrameworkService.StartService(strServiceURL);
                if (eCode != ERROR_CODE._NO_ERROR)
                {
                    Logger.Error("RunTestBatchFile", MarsTestFrame.Properties.Resources.HINT_CANT_START_FRAMEWORK_SERVICE);
                    MessageBox.Show(MarsTestFrame.Properties.Resources.HINT_CANT_START_FRAMEWORK_SERVICE, "ERROR");
                    return eCode;
                }
            }
            catch (Exception e)
            {
                Logger.Error("RunTestBatchFile",string.Format("Exception:[{0}], stackTrace:[{1}]",e.Message,e.StackTrace));
                return ERROR_CODE._SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE;
            }
#if !_tigerQTPHost
            if (((startMode & FrameWorkStartMode.FWSM_Slience) != FrameWorkStartMode.FWSM_Slience)&&(startMode!=FrameWorkStartMode.FWSM_STEPMODE))
            {
                try
                {
                    //Process objNewMonitor = new Process { StartInfo = new ProcessStartInfo { FileName = @".\TestFrameMonitor.exe", Arguments=WCFXmlCfgMgr.CurrentLoginUser } };                    
                    //objNewMonitor.Start();
                }
                catch (Exception e)
                {
                    Logger.Error("RunTestBatchFile",string.Format("Exception when system wants to start Monitor:[{0}]",e.Message),e);
                }

#if !_SlienceDebug
#if !_START_QTP_FROM_APP
                (new Thread(new ThreadStart(new Action(delegate () {
                    try {
                        ProcessStartInfo StartInfo = new ProcessStartInfo();
                        StartInfo.FileName = @".\QtpStarter.exe";
                        StartInfo.Domain = "NewMarsDomain";
                        StartInfo.Arguments = "-AUTOGEN";
                        //Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe" } };
                        Process objNewProce = new Process();
                        objNewProce.StartInfo = StartInfo;
                        objNewProce.Start();
                    }catch (Exception e)
                    {
                        Logger.Error("RunTestBatchFile", string.Format("Exception when system start Framework Services:[{0}]",e.Message), e);
                    }
                })))).Start();
#endif
#endif
            }
#endif
#region trash codes
            /*

            while (objTestSuit != null)
            {
                eCode = objTestSuit.loadXlsFile();
                /** compile the objTestSuit ** /
                //List<ConfigObjectBase> lstAllSteps = objTestSuit.
                if (eCode != ERROR_CODE._NO_ERROR)
                {
                    Logger.Error("RunTestBatchFile", string.Format("Error Code ocurrs after call objTestSuit.loadXlsFile() ERROR_CODE:[{0:X}]，ERROR_INFO:[{1}]",eCode,ERROR_INFO.GET_ERROR_STR(eCode)));
                    return eCode;
                }
                /*** 加载数据 *** /
                objTestSuit.InitDefaultDataFileName();
                eCode = objTestSuit.loadData();           

                eCode = mobjCompiler.preComplierTestSteps(objTestSuit.CurrentStepsList,ref iErrorStep, ref objErrorStep,ref strCurrentError);
                if (eCode!=ERROR_CODE._NO_ERROR)
                {
                    MessageBox.Show("Compiler Error, please check LogFile", "Error");
                    break;
                }            

                /*** put list to WCF server *** /
                this.mobjFrameworkService.SetCurrentTestSuiteObject(objTestSuit);
                this.mobjFrameworkService.InitListTestSteps(objTestSuit.CurrentStepsList);
                this.mobjFrameworkService.SetCurrentErrorInfo(iErrorStep, strCurrentError,objErrorStep);
#if _tigerDebugSaveData
                this.mobjFrameworkService.StoreDataBackComparisonMode("TC_FO_LOGON.xls", "[StorageMode:Comparing;ColIndx:2;ConvertMethod:NONE];TRADE_PAY_NTL", "10M", 1);
#endif
                /*** start service as Thread*** /
#if _tigerQTPHost

#endif
#if !_tigerQTPHost

                if (string.Compare(this.mstrDebugModeInfo, SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE_VBS, true) == 0)
                {
                    MessageBox.Show("Debug Mode, Wait....\r\nClose the dialog, and Services will stop.");
                    objTestSuit = mobjXlsBatch.getNextTCObject();
                }
                else
                {
                    //create a process and wait 
                    Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe" } };                
                    objNewProce.Start();
                    objNewProce.WaitForExit();
                    objTestSuit = mobjXlsBatch.getNextTCObject();
                }                
#endif
            }
            mobjXlsBatch.EndRuneTestSuit();        

            if (eCode!=ERROR_CODE._NO_ERROR)
            {
                return eCode;
            }

            /** get data parameter and concat all script files for qtp running ** /            

             */
#endregion //trash codes
            return eCode;
        }

        internal static string GetConfigServerURLInfo()
        {
            Logger.logBegin("GetConfigServerURLInfo");

            //ServicebasicInformation objServiceInfo = new ServicebasicInformation();
            Configuration objConfig = AppConfigReader.GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            NameValueCollection lstKeyValues = AppConfigReader.ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            string[] strProtocol = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocol == null) || (strProtocol.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING));
            }
            string[] strHost = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHost == null) || (strHost.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
            }
            string[] strPort = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
            if ((strPort == null) || (strPort.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
            }
            string[] strServiceName = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceName == null) || (strServiceName.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
            }

            /// for multiple user mode
            /// 
            bool isOk = false;
            string strError = "";
            WCFServiceNode portInfo = WCFXmlCfgMgr.GetCurrerntWcfNodeInfo(ref isOk, ref strError);
            if (!isOk)
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_MULTIPLE_USER_NOCURRENT_ACCOUNTSETTING, strError);
            }

            //string strResult = ServicebasicInformation.GetURL(strHost[0], strProtocol[0], strPort[0], strServiceName[0]);
            string strResult = ServicebasicInformation.GetURL(strHost[0], strProtocol[0], portInfo.ServerPort+"", strServiceName[0]);

            Logger.logEnd("GetConfigServerURLInfo");
            return strResult;
        }

        public OnRequireAutoGen_CurrentPegInfoEvent AutoGen_CurrentPegInfoHandler = null;
        public OnRequireAutoGen_OneTestStepEvent AutoGen_GenStepHandler = null;
        public OnShutdownServer ShutdownServerHandler = null;


#if _tigerQTPHost
        private ERROR_CODE BuildRunningScript(List<ConfigObjectBase> lstSteps, ref int iErrorStep, ref TestStep objStep)
        {
            Logger.logBegin("BuildRunningScript");
            StringBuilder strRunnableScript = new StringBuilder();
            ERROR_CODE eCde = GenerateAllRequiredKeywordsDefs(lstSteps,ref strRunnableScript, ref iErrorStep, ref objStep);
            strRunnableScript.Clear();
            strRunnableScript.Append("\r\n ");
            //strRunnableScript.Append(MarsTestFrame.Properties.Resources.HINT_RUNABLESCRIPT_STARTER_MARK);

            strRunnableScript.Append("call RunTestFrameWork()\r\n");
            strRunnableScript.Append("msgbox \"ok\"\r\n"); 

            eCde = RunGeneratedScript(strRunnableScript);
            Logger.logEnd("BuildRunningScript");
            return ERROR_CODE._NO_ERROR;
        }

        private ERROR_CODE RunGeneratedScript(StringBuilder strRunnableScript)
        {
            Logger.logBegin("RunGeneratedScript");
            /** get QTP instance ***/
            ERROR_CODE eCde = mobjCompiler.InsertRunnableTestScript(strRunnableScript);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
#if _tigerDebug
            MessageBox.Show("Wait for setting Debug points action of QTP");
#endif
            eCde = mobjCompiler.RunTest();
            Logger.logEnd("RunGeneratedScript");
            return eCde;
        }
#endif

        private ERROR_CODE GenerateAllRequiredKeywordsDefs(List<ConfigObjectBase> lstSteps, ref StringBuilder strRunnableScript, ref int iErrorStep, ref TestStep objErrStep)
        {
            Logger.logBegin("GenerateAllRequiredKeywordsDefs");
            if (strRunnableScript == null) strRunnableScript = new StringBuilder();
            iErrorStep = 0;
            foreach(ConfigObjectBase objStepNav in lstSteps)
            {
                iErrorStep++;
                if (!(objStepNav is TestStep)) continue;
                strRunnableScript.Append("\r\n");
                strRunnableScript.Append(((TestStep)objStepNav).GetKeyWordsDef());
            }            
            Logger.logEnd("GenerateAllRequiredKeywordsDefs");
            return ERROR_CODE._NO_ERROR;
        }
        
        public void TestConfigFile_Applications()
        {
            ConfigTestApplicationCollection lstApps = AppConfigReader.GetRegApplications();
        }

        private void Stopsubprocess(string strUserName)
        {
#if _killUFT
            string[] cnst_arrApps = {"UFT", "TestFrameMonitor", "QtpStarter" };
#else
            /** Kill Monitor **/
            /**
             * on 20170829 需要保留UFT以节省时间
            **/

            string[] cnst_arrApps = { "TestFrameMonitor", "QtpStarter" };
#endif
            foreach (string strAppName in cnst_arrApps)
            {
                TigerMarsUtil.KillProcessByName(strAppName, strUserName);
                Thread.Sleep(50);
            }
            ///** Stop services**/
            //if (objMarsTestingFrame == null) return;
            //objMarsTestingFrame.StopService();
        }

        public CommunicationState? CurrentSvcStatus
        {
            get { return this.mobjFrameworkService == null ? null : this.mobjFrameworkService.CurrentSvcStatus; }
        }

        public void StopService(int iMode=1)
        {
            //try to kill all sub processes
            Logger.Info("StopService", "Trying to stop services.....");
            Stopsubprocess(WCFXmlCfgMgr.CurrentLoginUser);
            if (mobjFrameworkService == null) return;
            /** remove all Events handlers **/
            this.mobjFrameworkService.AddGetCurrentTestCaseByTSAndTCEventHandler(mobjXlsBatch.GetCurrentTestCaseByTestSuiteNameAndTestCase, false);
            this.mobjFrameworkService.AddOnNavigateHandler(mobjXlsBatch.OnNavigateHandlerImpl, false);
            this.mobjFrameworkService.AddOnBeginNavigateTestSuiteWithRelyIdAndLoopIdHandler(mobjXlsBatch.OnNavigateWithRelyIdAndLoopIdHandlerImpl, false);
            this.mobjFrameworkService.AddOnGetNextTestSuiteEvent(mobjXlsBatch.OnGetNextTestSuiteEventImpl, false);
            this.mobjFrameworkService.AddOnLoadcurrentTestSuiteEvent(mobjXlsBatch.OnLoadCurrentTestSuiteEventImpl, false);
            this.mobjFrameworkService.AddOnTestSuiteIsDoneEventHandler(mobjXlsBatch.OnTestSuiteIsDoneEventImpl, false);
            this.mobjFrameworkService.AddOnCompileListEventHandler(mobjCompiler.preComplierTestSteps, false);
#if _Datafrom_Database
            this.mobjFrameworkService.AutoGen_CurrentPegInfoHandler = null;
            this.mobjFrameworkService.AutoGen_OneTestStepHandler = null;
#endif
            string strErrorInfo="" ;
            try
            {
                if (iMode != 2)
                {
                    this.mobjFrameworkService.StopService(ref strErrorInfo);
                }
            }
            catch (Exception e)
            {
                Logger.Error("StopService",string.Format("Exceptions:[{0}]",e.Message),e);
            }
            
        }

        public static bool GetBaseLineValue()
        {
            return MarsTestFrameCommuniteServer.GetBaseLineConfigValue();
        }

        public static void ChangeBaseLineValue(bool isBaseLineMode)
        {
            //if (this.mobjFrameworkService == null)
            //    return;
            string strBaseLineMode = isBaseLineMode?SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD:SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE ;
            MarsTestFrameCommuniteServer.ChangeBaseLineMode(strBaseLineMode);
            //this.mobjFrameworkService.ChangeBaseLineMode(strBaseLineMode);            
        }

        public void ChangeDefaultApplication(string strCurrentApplicationShortName)
        {
            if (this.mobjFrameworkService == null)
                return;
            string strDefaultApplication = string.Format("ShortName:{0}", strCurrentApplicationShortName);
            this.mobjFrameworkService.ChangeDefaultApplication(strDefaultApplication);
        }

        public void SetBatchMode(bool isRunningInBatchMode)
        {
            Logger.logBegin("SetBatchMode");
            IsRunningInBatchMode = isRunningInBatchMode;
            if (mobjFrameworkService != null)
            {
                mobjFrameworkService.IsRunningInBatchMode = isRunningInBatchMode;
            }
            Logger.logEnd("SetBatchMode");
        }

        public void ShutdownServer()
        {
            Logger.logBegin("ShutdownServer");
            StopService();
            CommServerShutdownHandler();
            Logger.logEnd("ShutdownServer");
        }

        public bool IsSkipStepWord(string word)
        {
            bool rc = false;
            List <string> skipWordList = AppConfigReader.ReadSkipStepWords();

            if (skipWordList != null && skipWordList.Contains(word))
                rc = true;

            Logger.Info("IsSkipStepWord", "IsSkipStepWord returned " + rc);
            return rc;
        }


        public bool IsVariable(string word)
        {
            bool rc = false;
            if (word.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL+":") 
                || word.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL + ":")
                || word.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_MODAL+":")
                || word.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP+":")
                || word.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_IFVAR+":"))
                rc = true;

            Logger.Info("IsVariable", "word = " + word);
            Logger.Info("IsVariable", "IsVariable returned " + rc);
            return rc;
        }

        public string GetVariableValue(string strDBIdx,string variable)
        {
            Logger.Info("GetVariableValue", string.Format("variable:[{0}]",variable));
            string value = variable;
            string tempValue = null; ;

            string[] words = variable.Split(':');

            string variableLocalOrGlobal = words[0];
            string variableName = words[1];

            if ((variableLocalOrGlobal.Equals(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL))
                ||(variableLocalOrGlobal.Equals(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP)))
                tempValue = BoHelper.GetVariableValue(strDBIdx,variableLocalOrGlobal, variableName);
            else
            {
                if (String.Compare(SystemConstant.CNST_RESERVED_VARIABLE_MODAL, variableLocalOrGlobal,true)==0)
                {
                    //modal var
                    string strMode = AppConfigReader.GetBaseLineMode(WCFXmlCfgMgr.CurrentLoginUser), strError = "";
                    tempValue ="";
                    short sStatus = 2;
                    if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, strMode, true)==0)
                    {
                        sStatus = 1;
                    }
                    if (!BoHelper.GetModalVariableInfo(variableName, sStatus, ref strError, ref tempValue))
                        tempValue = "";
                }
                else
                {
                    if (string.Compare(SystemConstant.CNST_RESERVED_VARIABLE_IFVAR, variableLocalOrGlobal, true) == 0)
                    {
                        short sStatus = 1;
                        string strError = "";
                        if (!BoHelper.GetIFVariableInfo(strDBIdx, variableName, sStatus, ref strError, ref tempValue))
                            tempValue = "";
                    }
                    else
                        tempValue = "";
                }
            }

            if (tempValue != null)
                value = tempValue;
            else
                if ((variableLocalOrGlobal.Equals(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP)))
                    value = "";

            Logger.Info("GetVariableValue", "GetVariableValue returned " + value);
            return value;
        }



        //[DllImport("kernel32.dll")]
        //static extern IntPtr WinExec(string strPara, int cCmd);

    }
}
