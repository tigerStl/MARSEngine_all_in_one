using MarsTestFrame.CommuniteServer;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.xmlConfig;
using MarsTestFrame.systemUtil;
using QtpStarter.Info;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows.Forms;
using TestFrameMonitor.Server.ServiceContracts;

namespace TestFrameMonitor.Server
{

    public delegate void OnBreakpointReachedEvent(TestStep4Services objTestInfo, SystemDebuggerMode breakMode);

    public delegate void OnNewInstanceofServiceCreated(MonitorService objService);
    public delegate TestFlowDebugInfo OnGetCurrentModeEvent();
    public delegate void OnTestSuiteId4ProjectChangedEvent(string strTestSuiteId4Project);

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MonitorService : IMonitorService
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MonitorService));

        public static OnNewInstanceofServiceCreated onNewServiceStarts = null;

        public delegate void OnShutdownServer();
        public static OnShutdownServer OnShutdownServerHandler = null;
        public delegate void OnSetBatchMode();
        public static OnSetBatchMode OnSetBatchModeHandler = null;
        public delegate void OnSkipCurrentStep();
        public static OnSkipCurrentStep OnSkipCurrentStepHandler = null;
        private bool _isBatchMode = false;
        public bool IsBatchMode
        {
            get
            {
                return _isBatchMode;
            }
            set
            {
                _isBatchMode = value;
            }
        }
        #region Interface Service
        private int ClientCount = 0;
        private IMonitorCallBack CallBackClient = null;
        private TestFlowDebugInfo currentDebuggerInfo = new TestFlowDebugInfo();


        public void GetCurrentDebugMode()
        {
            if (onGetCurrentModeHandler == null)
                return;
            this.CallBackClient.GetCurrentDebugModeCallBack(onGetCurrentModeHandler());
            //return onGetCurrentModeHandler();
        }

        public MonitorService()
        {
            Logger.logBegin("MonitorService");
            Logger.Info("MonitorService", "a new instance..... of service....");
            if (onNewServiceStarts != null)
                onNewServiceStarts(this);
            Logger.logEnd("MonitorService");
        }

        public void OnClientTestSuiteId4ProjectReadyEvent(string strCurrentTestSuiteId4Project)
        {
            this.currentDebuggerInfo.CurrentRelyId = strCurrentTestSuiteId4Project;
            /** notify monitor service **/
            if (this.onTestSuiteId4ProjectChangedHandler != null)
                this.onTestSuiteId4ProjectChangedHandler(this.currentDebuggerInfo.CurrentRelyId);
        }

        public void Subscribe()
        {
            Logger.logBegin("Subscribe");
            try
            {
                ClientCount++;
                CallBackClient = OperationContext.Current.GetCallbackChannel<IMonitorCallBack>();

                CallBackClient.SetMonitorTokenId(ClientCount);
                CallBackClient.SubScribCallBack();
            }
            catch (Exception e)
            {
                CallBackClient = null;
                Logger.Error("Subscribe", string.Format("Exceptions: [{0}]", e.Message), e);
                return;
            }

            Logger.logEnd("Subscribe");

            return;
        }

        public void OnClientTestSuiteTestCaseNamesChangeEvent(string strTestSuite, string strTestCase)
        {
            if (onTestSuiteTestCaseNamesChangeHandler != null)
            {
                this.onTestSuiteTestCaseNamesChangeHandler(strTestSuite, strTestCase);
            }
            return;
        }

        public void OnClientBreakpointReachedEvent(TestStep4Services objTestInfo, SystemDebuggerMode breakMode)
        {
            Logger.logBegin("OnClientBreakpointReachedEvent");
            if (objTestInfo == null) return;
            if (onBreakpointReachedHandler == null) return;
            this.onBreakpointReachedHandler(objTestInfo, breakMode);
            Logger.logEnd("OnClientBreakpointReachedEvent");
        }

        string lock_lstSteps = "Lock_ListSteps";
        public void OnClientTestCaseListChangeEvent(List<TestStep4Services> lstTestSteps)
        {
            List<TestStep4Services> lstStps = new List<TestStep4Services>(lstTestSteps);
            Monitor.Enter(lock_lstSteps);
            try
            {
                if (onTestCaseListChangeHandler != null)
                {
                    this.onTestCaseListChangeHandler(lstStps);
                }
                return;
            }
            finally
            {
                Monitor.Exit(lock_lstSteps);
            }

        }

        public void OnClientTestCompilerEndEvent(bool isError, string strErrorInfo, List<TestStep4Services> objErrorInfo)
        {
            if (onTestStepCompilerEndHandler != null)
            {
                this.onTestStepCompilerEndHandler(isError, strErrorInfo, objErrorInfo);
            }
            return;
        }

        public void OnClientWriteCurrentLog(string strMessage, int iErrorOrNormal)
        {
            Logger.Info("OnClientWriteCurrentLog", string.Format("Message:[{0}] ErrorOrNot:[{1}]", strMessage, iErrorOrNormal));
            try
            {
                if (this.onWriteCurrentLogHandler != null)
                {
                    this.onWriteCurrentLogHandler(strMessage, iErrorOrNormal);
                }
            }
            catch (Exception e)
            {
                Logger.Error("OnClientWriteCurrentLog", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
            }

            return;
        }

        public void OnClientCompilerOneTestStepEvent(TestStep4Services objCurrentCompilerTestStep)
        {
            if (onCompilerOneTestStepHandler != null)
                this.onCompilerOneTestStepHandler(objCurrentCompilerTestStep);
            return;
        }


        public void BeforeClientRunTestStepEvent(TestStep4Services objTestStepRunning)
        {
            if (this.beforeRunTestStepHandler == null) return;
            this.beforeRunTestStepHandler(objTestStepRunning);
        }

        public void AfterClientRunTestStepEvent(string strDataWriteBack, int iResult, string strMessage)
        {
            Logger.Info("AfterClientRunTestStepEvent", string.Format("strDataWriteBack:[{0}] iResult:[{1}] Message:[{2}]", strDataWriteBack, iResult, strMessage));
            if (this.afterRunTestStepHandler == null) return;
            this.afterRunTestStepHandler(strDataWriteBack, iResult, strMessage);
        }

        public void RefreshGridStyle()
        {
            if (this.ReFreshGridStyleHandler == null) return;
            this.ReFreshGridStyleHandler();
        }

        public void BeginAddLogHint()
        {
            if (OnLogModeChangedHandler == null) return;
            this.OnLogModeChangedHandler(1);
        }

        public void EndAddLogHing()
        {
            if (OnLogModeChangedHandler == null) return;
            this.OnLogModeChangedHandler(0);
        }

        public void OnCurrentLoopChangeEvent(int iLoopId)
        {
            if (onCurrentLoopChangeHandler == null) return;
            this.currentDebuggerInfo.TestCurrentLoopId = iLoopId;
            this.onCurrentLoopChangeHandler(iLoopId);
        }

        public void OnLoopCountChangeEvent(int iLoopCount)
        {
            if (onLoopCountChangeHandler == null) return;
            this.onLoopCountChangeHandler(iLoopCount);
        }

        #endregion Interface Service

        #region Delegate for form
        public OnTestSuiteTestCaseNamesChangeEvent onTestSuiteTestCaseNamesChangeHandler = null;
        public OnTestCaseListChangeEvent onTestCaseListChangeHandler = null;
        public OnTestCompilerEndEvent onTestStepCompilerEndHandler = null;
        public OnCompilerOneTestStepEvent onCompilerOneTestStepHandler = null;
        public BeforeRunTestStepEvent beforeRunTestStepHandler = null;
        public AfterRunTestStepEvent afterRunTestStepHandler = null;
        public OnWriteCurrentLogEvent onWriteCurrentLogHandler = null;
        public OnRefreshStepGridEvent ReFreshGridStyleHandler = null;
        public OnLogModeChangedEvent OnLogModeChangedHandler = null;
        public OnCurrentLoopChangeEvent onCurrentLoopChangeHandler = null;
        public OnClientLoopCountChangeEvent onLoopCountChangeHandler = null;
        public OnBreakpointReachedEvent onBreakpointReachedHandler = null;
        public OnGetCurrentModeEvent onGetCurrentModeHandler = null;
        public OnTestSuiteId4ProjectChangedEvent onTestSuiteId4ProjectChangedHandler = null;


        public OnOneLoopIsDoneEvent onOneLoopIsDoneHandler = null;
#if v_16AndUp
        public OnTestStoryBoardNameEvent onTestStoryboardHandler = null;
        public OnTestDataSetNameEvent onDataSetNameHandler = null;
        public OnTestSToryBoardTotalStepsChangeEvent onTestStoryboardTotalStepsHandler = null;
        public OnTestSToryBoardTotalStepsChangeEvent onTestStoryboardCurrentStepsNoHandler = null;
#endif

        #endregion Delegate for Form

        #region Client Data Cache
        private string TestSuiteName = "";
        private string TestCaseName = "";
        #endregion //Client Data Cache

        #region Delegate for Debug Mode

        /** this section could be called by other application **/
        /** OnSetDebuggerModeEvent **/
        public void onSetDebuggerModeImpl(SystemDebuggerMode iMode)
        {
            Logger.logBegin("onSetDebuggerModeImpl");
            /** to stop client running, i.e. force client side wait until another command **/
            try
            {
                /** ask to stop client **/
                if (CallBackClient == null)
                {
                    Logger.Error("onAddOrRemoveOneStepFromBreakpointsListImpl", "no callback object can get...");
                    return;
                }
                CallBackClient.SetCurrentDebuggerMode((int)iMode);
            }
            catch (Exception e)
            {
                Logger.Error("onSetDebuggerModeImpl", string.Format("Exceptions: [{0}]", e.Message), e);
            }
            Logger.logEnd("onSetDebuggerModeImpl");
        }

        public bool onAddOrRemoveOneStepFromBreakpointsListImpl(TestStep4Services objStepBreakPoint, bool isAdd, int debuggerMode)
        {
            Logger.logBegin("onAddOrRemoveOneStepFromBreakpointsListImpl");
            try
            {

                IMonitorCallBack callBackIF = this.CallBackClient;
                //IBreakPoint4TestSuite callBackIF = OperationContext.Current.GetCallbackChannel<IBreakPoint4TestSuite>();
                if (callBackIF == null)
                {
                    Logger.Error("onAddOrRemoveOneStepFromBreakpointsListImpl", "no callback object can get...");
                    return false;
                }

                //currentDebuggerInfo = new TestFlowDebugInfo();
                currentDebuggerInfo.CurrentFromId = objStepBreakPoint.RunID;
                currentDebuggerInfo.CurrentTestSuiteName = TestSuiteName;
                currentDebuggerInfo.CurrentTestCaseName = TestCaseName;
                currentDebuggerInfo.TestDebugMode = debuggerMode;
                //currentDebuggerInfo.TestCurrentLoopId = 

                if (isAdd)
                {
                    currentDebuggerInfo.RemoveOrAddId = 0;
                    //objBreakPoints.TestProjectName = this.
                    callBackIF.AddOneBreakPoint(currentDebuggerInfo);
                    /** **/
                    Logger.Info("onAddOrRemoveOneStepFromBreakpointsListImpl", string.Format("added a breakpoint to client. StepId:[{0}]", currentDebuggerInfo.CurrentFromId));
                }
                else
                {
                    currentDebuggerInfo.RemoveOrAddId = 1;
                    callBackIF.AddOneBreakPoint(currentDebuggerInfo);
                    Logger.Info("onAddOrRemoveOneStepFromBreakpointsListImpl", string.Format("added a breakpoint to client. StepId:[{0}]", currentDebuggerInfo.CurrentFromId));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("onAddOrRemoveOneStepFromBreakpointsListImpl", string.Format("Exceptions :[{0}]", e.Message));
                return false;
            }
            finally
            {
                Logger.logEnd("onAddOrRemoveOneStepFromBreakpointsListImpl");
            }
        }
        #endregion //Delegate for Debug Mode

        #region member
        protected static ServiceHost mobjHost = null;
        #endregion


        internal static int StartService(int iPort = -1)
        {
            string strProtocol = "", strHost = "", strServiceName = "", strError = "";
            bool isOk = false;
            //string strURL = AppConfigReader.GetConfigServerURLInfo();
            if (iPort > 0)
            {
                strHost = "localhost";
                strProtocol = "net.tcp";
                strServiceName = "MARSTIGFrameMonitor";
                isOk = true;
            }
            else
            {
                try
                {
                    isOk = AppConfigReader.GetConfigServerURLInfo(ref strProtocol, ref strHost, ref strServiceName, ref strError);
                }
                catch (Exception e)
                {
                    Logger.Error("StartService", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                    isOk = false;
                }
            }

            if (!isOk)
            {
                MessageBox.Show(string.Format("Can't get Monitor Service protocol or host information with error:\r\n\t{0}", strError), "ERROR");
                return -1;
            }
            //get port info from config xml file
            string strURL = "";
            if (iPort == -1)
            {
                WCFServiceNode objNode = WCFXmlCfgMgr.GetCurrerntWcfNodeInfo(ref isOk, ref strError);
                if ((!isOk) || (objNode == null) || (objNode.IsEmpty()))
                {
                    MessageBox.Show(string.Format("Can't get Port info from MarsService.xml with error:[{0}]", strError), "ERROR");
                    return -1;
                }
                strURL = string.Format("{0}://{1}:{2}/{3}", strProtocol, strHost, objNode.MonitorPort, strServiceName);
            }
            else
            {
                strURL = $"{strProtocol}://{strHost}:{iPort}/{strServiceName}";
            }
            //MessageBox.Show($"URL is:{strURL}");
            if (StartService(strURL) != 1)
            {
                MessageBox.Show("Can't start Monit Services.", "Error");
                return -1;
            }
            return 1;
        }

        private static int StartService(string strURL)
        {
            Logger.logBegin("StartService");
            Logger.Info("StartService", string.Format("URL"));
            try
            {
                Uri objURL = new Uri(strURL);
                //this.mobjHost = new ServiceHost(this, objURL);
                mobjHost = new ServiceHost(typeof(MonitorService), objURL);
                var behaviour = mobjHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();

                behaviour.InstanceContextMode = InstanceContextMode.Single;
                behaviour.ConcurrencyMode = ConcurrencyMode.Multiple;

                ServiceMetadataBehavior smb = mobjHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if (smb == null)
                    smb = new ServiceMetadataBehavior();


                //smb.HttpGetBinding 
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                mobjHost.Description.Behaviors.Add(smb);
                NetTcpBinding objTCP = new NetTcpBinding();
                objTCP.OpenTimeout = new TimeSpan(0, 10, 0);
                objTCP.CloseTimeout = new TimeSpan(0, 10, 0);
                objTCP.SendTimeout = new TimeSpan(0, 10, 0);
                objTCP.ReceiveTimeout = new TimeSpan(0, 10, 0);
                mobjHost.AddServiceEndpoint(typeof(IMonitorService), objTCP, "");
                mobjHost.AddServiceEndpoint(typeof(IMetadataExchange),
                                        MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

                mobjHost.Faulted += mobjHost_Faulted;
                mobjHost.Open();


                return 1;
            }
            catch (Exception e)
            {
                //ERROR_CODE eCde = ERROR_CODE._SERVICE_ERROR_NO_SERVICE_START_UNKNOW;
                Logger.Error("StartService", e.Message, e);
                return -1;
            }
            finally
            {
                Logger.logEnd("StartService");
            }
        }

        static void mobjHost_Faulted(object sender, EventArgs e)
        {
            Logger.Error("mobjHost_Faulted", string.Format("sender:[{0}], e:[{1}]", sender ?? "null", e));
            try
            {
                OperationContext.Current.Channel.Abort();
            }
            catch (Exception ex)
            {
                Logger.Error("mobjHost_Faulted", string.Format("Exception:[{0}]", ex.Message), ex);
            }

        }


        public void OnOneLoopIsDone()
        {
            if (this.onOneLoopIsDoneHandler == null) return;

            this.onOneLoopIsDoneHandler();
        }


        public bool ShutdownServer()
        {
            Logger.logBegin("ShutdownServer");
            OnShutdownServerHandler();
            Logger.logEnd("ShutdownServer");
            return true;
        }
        public bool SetBatchMode()
        {
            Logger.logBegin("SetBatchMode");
            IsBatchMode = true;
            Logger.logEnd("SetBatchMode");
            return true;
        }
        public void SkipCurrentStep()
        {
            OnSkipCurrentStepHandler();
        }
#if v_16AndUp
        public void SetTestStoryboardName(string strStoryBoardName)
        {
            Logger.Info("SetTestStoryboardName", string.Format("storyBoard Name:[{0}]", strStoryBoardName));
            if (onTestStoryboardHandler != null)
            {
                onTestStoryboardHandler(strStoryBoardName);
            }
        }

        public void SetTestStoryboardTotalSteps(int iCnt)
        {


        }

        public void ClickSpecialPos(int X, int Y)
        {
            MouseSimulator.SetCursorPos(X, Y);
            MouseSimulator.ClickLeftMouseButton();
        }

        public void SetTestDataSetName(string strDataSetName)
        {
            Logger.Info("SetTestDataSetName", string.Format("Dataset Name:[{0}]", strDataSetName));
            if (onDataSetNameHandler != null)
                onDataSetNameHandler(strDataSetName);
        }


#endif
    }
}
