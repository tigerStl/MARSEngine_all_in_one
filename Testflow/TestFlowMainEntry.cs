using com.Mars.Constants;
using MarsTestFrame.CommuniteServer;
using MarsTestFrame.SourceCode.xmlConfig;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using TestFlowClient.Mars.TigerConfig;
using TestFrameMonitor.Server.ServiceContracts;

namespace TestFlowClient
{

    public delegate void TestSuiteTestCaseReadyEvent(string strTestSuiteName, string strTestCaseName);
    public delegate void TestStepsAreReadyEvent(List<TestStep4Services> lstSteps);
    public delegate void OnBreakpointReachedEvent(TestStep4Services objTestInfo, SystemDebuggerMode breakMode);
    /** This is main entry for test flow
     * It will connect to WCF server 
     * and get Current Test Step.
     * Then, provides caller, usually is QTP/UFT, would ask for
     * VBScript type code, runnable codes.
     * **/
    enum TestFrameWorkRunMode
    {
        TFRM_Normal = 1,
        TFRM_StopAndBreakPoint,
        TFRM_Breaks,
        TFRM_RunFrom,
        TFRM_SkipRowsTempoary
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00020400-0000-0000-C000-000000000046")]
    public interface IDispatch
    {
        // Gets the number of Types that the object provides (0 or 1).
        [PreserveSig]
        int GetTypeInfoCount(out int typeInfoCount);

        // Gets the Type information for an object if GetTypeInfoCount returned 1.
        void GetTypeInfo(int typeInfoIndex, int lcid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef =
            typeof(System.Runtime.InteropServices.CustomMarshalers.TypeToTypeInfoMarshaler))]
        out ITypeInfo typeInfo);

        // Gets the DISPID of the specified member name.
        [PreserveSig]
        int GetDispId(ref Guid riid, ref string name, int nameCount, int lcid,
            out int dispId);

        // NOTE: The real IDispatch also has an Invoke method next, but we don't need it.
    }


    //public class TestFlowClientMainEntry : DuplexClientBase<IMonitorCallBack>,IBreakPoint4TestSuite
    [CallbackBehaviorAttribute(
   IncludeExceptionDetailInFaults = true,
    //UseSynchronizationContext = true,
    //ValidateMustUnderstand = true,
    ConcurrencyMode = ConcurrencyMode.Reentrant
  )]
    public class TestFlowClientMainEntry : IMonitorCallBack, INotifyPropertyChanged, IMarsTestNotificationCallback
    {

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformationW(
          IntPtr hServer,
          int SessionId,
          int WTSInfoClass,
          out IntPtr ppBuffer,
          out IntPtr pBytesReturned);

        private static MLogger Logger = null;// MLogger.GetLogger(typeof(TestFlowClientMainEntry));

        public static DuplexChannelFactory<IMonitorService> objDualFactory = null;

        public const string CNST_DATEFORMAT = "MM/dd/yyyy HH:mm:ss";
        public static string currentMarsUserAccount = "";
        public static int currentMarsMonitorPort = -1;
        public static string currentMarsMonitorSvr = null;

        private IMarsTigerFrameWorkService mobjClient2Server = null;
        private int MonitorTokenId = -1;
        private IMonitorService mobjClient2Monitor = null;
        //private DuplexChannelFactory<IMonitorService> objDualFactory = null ;
        public IMonitorService MonitorWcfClient
        {
            get
            {
                bool isOk = false;
                string strError = "";
                if (mobjClient2Monitor == null)
                {
                    ERROR_CODE eCode = this.ConnectToWCFMonitorServer(ref isOk, ref strError);
                    if (eCode != ERROR_CODE._NO_ERROR)
                    {
                        return mobjClient2Monitor = null;
                    }
                }
                return mobjClient2Monitor;
            }

            set
            {
                if (value == null)
                {
                    //ask for reset
                    string strError = "";
                    bool isOk = false;
                    ERROR_CODE eCode = this.ConnectToWCFMonitorServer(ref isOk, ref strError);
                    if (eCode != ERROR_CODE._NO_ERROR)
                    {
                        mobjClient2Monitor = null;
                    }
                }
            }
        }
        private TestStep4Services mobjCurrentTestStep = null;

        public TestFlowClientMainEntry()
        {
            try
            {
                string strFile = typeof(TestFlowClientMainEntry).Assembly.Location;
                strFile = Path.GetDirectoryName(strFile);
                strFile = Path.Combine(strFile, ".\\MarsTestClient.dll");
                MLogger.LogFileCofigName = strFile;
                Logger = MLogger.GetLogger(typeof(TestFlowClientMainEntry));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }

        CachedTestStepMgr TestStepsMgr = new CachedTestStepMgr();

        private ERROR_CODE e_CurrentErrorCode = ERROR_CODE._NO_ERROR;

        #region critical section management
        object DebugListCriticalSection = new object();
        #endregion //critical section management

        #region Debugger management
        public OnBreakpointReachedEvent onBreakPointReachedHandler = null;
        private TestFlowClient.Debug.DebugMgr debugMgrInfo = new TestFlowClient.Debug.DebugMgr();

        public void SetCurrentDebuggerMode(int iDebuggerMode)
        {
            /** provide methods to be invoked by server side, asking for stopping right now 
             * There are two ways to stop,
             * 1, is to call pause of qtp
             * 2, is to set a break varible of the local, so that client will wait until next value
             * As mode 1, it calls QTP active, therefore, mode 2 is applied.
             * **/
            Logger.logBegin("SetCurrentDebuggerMode");
            try
            {
                SystemDebuggerMode emDebugMode = ((SystemDebuggerMode)iDebuggerMode);
                switch (emDebugMode)
                {
                    case SystemDebuggerMode.SDM_BREAKAT:
                        TestFrameUtility.ChangeBreakPointNow(true);
                        CleanDeubgerInfoForDebuggerModeChange();
                        break;
                    case SystemDebuggerMode.SDM_BREAKAT | SystemDebuggerMode.SDM_REUSME:
                        TestFrameUtility.ChangeBreakPointNow(false);
                        break;
                    case SystemDebuggerMode.SDM_REPLAY_THESAME_TEST:
                    case SystemDebuggerMode.SDM_REPLAY_THESAME_TEST | SystemDebuggerMode.SDM_REUSME:
                        /** tell qtp to stop and exit then restart **/
                        /** call qtp starter to exit and restart **/
                        TestFrameUtility.ChangeBreakPointNow(false);
                        CallQtpStarter2RestartcurrentTest();
                        break;
                    case SystemDebuggerMode.SDM_SKIP:
                        TestFrameUtility.ChangeBreakPointNow(true);
                        CleanDeubgerInfoForDebuggerModeChange();
                        break;
                    case SystemDebuggerMode.SDM_SKIP | SystemDebuggerMode.SDM_REUSME:
                        /** skip row **/
                        TestFrameUtility.ChangeBreakPointNow(false);
                        break;
                    default:
                        /** do nothing **/
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SetCurrentDebuggerMode", string.Format("Exception :[{0}]", e.Message), e);
            }
            Logger.logEnd("SetCurrentDebuggerMode");
        }

        private void CallQtpStarter2RestartcurrentTest()
        {
            /** find qtp starter path **/
            Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe", Arguments = "-R" } };
            objNewProce.Start();
        }

        [MTAThread]
        public void AddOneBreakPoint(TestFlowDebugInfo objBreakPnt)
        {
            int iIdx = -1;
            iIdx = GetBreakPointInfoFromList(objBreakPnt);
            if (objBreakPnt.RemoveOrAddId == 0)
            {
                if (iIdx != -1) return;
                debugMgrInfo.getBreakPointList().Add(objBreakPnt);
                debugMgrInfo.SortDebuggerList();
                Logger.Info("AddOneBreakPoint", string.Format("One breakpoint [#{0}] is added.", objBreakPnt.CurrentFromId));
            }
            else
                if ((iIdx >= 0) || (iIdx < debugMgrInfo.getBreakPointList().Count))
            {
                debugMgrInfo.getBreakPointList().RemoveAt(iIdx);
                Logger.Info("AddOneBreakPoint", string.Format("One breakpoint [#{0}] is remmoved.", objBreakPnt.CurrentFromId));
            }
        }


        public void DebugStepOver(TestFlowDebugInfo objBreakPnt)
        {

        }

        #region Message Error auto check
        private List<KeyValuePair<string, int>> OpicsMessageAndType = null;
        public bool CheckErrorMessageType(string strTextMessage, string ApplicationFriendlyName, ref int iMsgType, ref string strError)
        {
            Logger.logBegin("CheckErrorMessageType");
            Logger.Info("CheckErrorMessageType", string.Format("Message to check:[{0}] for appName:[{1}]", strTextMessage, ApplicationFriendlyName));
            try
            {
                //思路:
                // 就opics而言，数据存在于服务器端，因此 可以有两种方式，一是将数据从服务器端取出来 客户端比较 或者在后端比较 目前采用客户端模式
                if (OpicsMessageAndType == null)
                    OpicsMessageAndType = this.mobjClient2Server.GetOpicsMessageAndTypeList();

                if (OpicsMessageAndType == null)
                {
                    iMsgType = -1;// unknow
                    strError = "No Message data is returned from remote.";
                    return false;
                }
                foreach (var itm in OpicsMessageAndType)
                {
                    if (itm.Equals(default(KeyValuePair<string, int>))) continue;
                    if (TigerMarsUtil.RegularTest(itm.Key, strTextMessage))
                    {
                        iMsgType = itm.Value;
                        return true;
                    }
                }
                strError = string.Format("No such information [{0}] matched from list.", strTextMessage);
                return false;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception [{0}] when comparason from data.", e.Message);
                Logger.Error("CheckErrorMessageType", strError, e);
                iMsgType = -1;
                return false;
            }
            finally
            {
                Logger.logEnd("CheckErrorMessageType");
            }
        }
        #endregion


        private int GetBreakPointInfoFromList(TestFlowDebugInfo objBreakPnt)
        {

            for (int i = 0; i < debugMgrInfo.getBreakPointList().Count; i++)
            {
                if (debugMgrInfo.getBreakPointList()[i].EqualTo(objBreakPnt)) return i;
            }
            return -1;
        }

        #endregion

        #region DuplexClientBase methods
        /*
        public TestFlowClientMainEntry(System.ServiceModel.InstanceContext callbackInstance)
            : base(callbackInstance)
        {
            Logger.logBegin("TestFlowClientMainEntry");
            Logger.Info("TestFlowClientMainEntry", string.Format("parameters:"callbackInstance.ToString());
        }
         * */
        #endregion //DuplexClientBase methods

        #region Data from Serverside
        private int miCurrentLoopId;//begin from 1
        #endregion //Data from Serverside

        #region MonitorForm and Monitor control

        protected TestSuiteTestCaseReadyEvent TestSuiteInformationHandler = null;
        protected void AddTestSuiteTestcaseEventHandle(TestSuiteTestCaseReadyEvent objEventHandle)
        {
            TestSuiteInformationHandler += objEventHandle;
        }
        protected TestStepsAreReadyEvent TestStepsReadyEventHandler = null;
        protected void AddTestStepsAreReadyEventHander(TestStepsAreReadyEvent objNewEventHandler)
        {
            TestStepsReadyEventHandler += objNewEventHandler;
        }

        protected void MonitorFormCloseEventHandler()
        {
            TestSuiteInformationHandler = null;
            TestStepsReadyEventHandler = null;

        }

        private const int CNST_NORMALSTATUS = 1;
        private const int CNST_ERRORSTATUS = -1;
        protected void WriteToMonitorStatus(string strHint, int iNormalOrError = CNST_NORMALSTATUS)
        {
            try
            {
                if (this.mobjClient2Monitor == null) return;
                this.mobjClient2Monitor.OnClientWriteCurrentLog(strHint, iNormalOrError);
            }
            catch (Exception e)
            {
                Logger.Error("WriteToMonitorStatus", string.Format("Exception:[{0}]", e.Message), e);
            }

        }
        #endregion//MonitorForm

        #region common ApIs
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr procid);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hwnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hwnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref MarsShellLib.ShellApi.RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        public bool SetFocusByCreateCaret(object oHwnd, ref string strError)
        {
            if (oHwnd == null)
            {
                strError = "Null handle is passed.";
                return false;
            }
            string sHandle = oHwnd.ToString();
            int hwnd = -1;
            if (!int.TryParse(sHandle, out hwnd))
            {
                strError = string.Format("Not a number for handle:[{0}]", sHandle);
                return false;
            }
            bool isOk = CreateCaret(new IntPtr(hwnd), IntPtr.Zero, 0, 0);
            if (!isOk)
            {
                strError = string.Format("CreateCaret Error code:[{0}] ", GetLastError());
                return false;
            }
            return true;
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        public bool BringWindowToFront(object oHwnd, ref string strError)
        {
            if (oHwnd == null)
            {
                strError = "Null handle is passed.";
                return false;
            }
            string sHandle = oHwnd.ToString();
            int hwnd = -1;
            if (!int.TryParse(sHandle, out hwnd))
            {
                strError = string.Format("Not a number for handle:[{0}]", sHandle);
                return false;
            }
            bool isOk = SetForegroundWindow(new IntPtr(hwnd));
            if (!isOk)
            {
                strError = string.Format("SetForegroundWindow Error code:[{0}] ", GetLastError());
                return false;
            }
            return true;
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MOVE = 0x0001;




        /*** ClickAtSpecialPosition("100","200","LEFT/RIGHT","FALSE") ***/
        public ERROR_CODE ClickAtSpecialPosition(object objX, object objY, object objButtonId = null, object isDouble = null)
        {
            int x, y;
            try
            {
                x = Int32.Parse(objX.ToString());
                y = Int32.Parse(objY.ToString());
            }
            catch (Exception e)
            {
                Logger.Error("ClickAtSpecialPosition", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_PARAMETER_SHOULDBE_INT_PARAP_1), "x | y"), e);
                return ERROR_CODE._SERVICE_ERROR_PARAMETER_SHOULDBE_INT_PARAP_1;
            }

            int iLeftOrRightDown = (objButtonId == null ? MOUSEEVENTF_LEFTDOWN : (string.Compare("LEFT", objButtonId.ToString(), true) == 0 ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN)) & 0x8000;
            int iLeftOrRightUp = (objButtonId == null ? MOUSEEVENTF_LEFTUP : (string.Compare("LEFT", objButtonId.ToString(), true) == 0 ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP)) & 0x8000;
            SetCursorPos(x - 10, y - 10);
            Logger.Info("ClickAtSpecialPosition", string.Format("move mouse to :[{0}-{1}]", x - 10, y - 10));
            Thread.Sleep(100);
            SetCursorPos(x, y);
            Thread.Sleep(50);

            Logger.Info("ClickAtSpecialPosition", string.Format("move mouse to :[{0}-{1}]", x, y));
            mouse_event(iLeftOrRightDown, x, y, 0, 0);
            Thread.Sleep(100);

            mouse_event(iLeftOrRightUp, x, y, 0, 0);
            if (isDouble != null)
            {
                mouse_event(iLeftOrRightDown, x, y, 0, 0);
                Thread.Sleep(5);
                mouse_event(iLeftOrRightUp, x, y, 0, 0);
            }

            return ERROR_CODE._NO_ERROR;
        }

        #endregion //common ApIs

        #region status information
        private TestSuiteRunStatusInfo mobjRunStatus = new TestSuiteRunStatusInfo();
        #endregion

        #region process monitor
        private Dictionary<string, string> ProcessOldListCurrent = new Dictionary<string, string>();
        private Dictionary<string, string> ProcessNewListCurrent = new Dictionary<string, string>();
        private void ReadProcessInfoToDictionary(Dictionary<string, string> objDesDictionary)
        {
            Process[] arrCurrentPrcs = Process.GetProcesses();
            foreach (Process objPrc in arrCurrentPrcs)
            {
                try
                {
                    objDesDictionary.Add(string.Format("{0}", objPrc.Id), objPrc.ProcessName);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            Logger.Info("ReadProcessInfoToDictionary", string.Format("Get total [{0}] processes", objDesDictionary.Keys.Count));
        }
        private string GetNewProcessId()
        {
            foreach (string strPId in ProcessNewListCurrent.Keys)
            {
                if (!ProcessOldListCurrent.ContainsKey(strPId)) return strPId;
            }
            return null;
        }
        #endregion //process monitor

        #region Methods for QTP

        public void OneLoopIsDone()
        {

            #region New Loop Part,2017-1
            cleanLoopInfoStack();
            #endregion //New Loop Part,2017-1

#if (_NORMAL_FORM_ && _VEDIO_TIGER_)
            /** 通知monitor存贮视频文件 **/
            if (this.mobjClient2Monitor != null)
                this.mobjClient2Monitor.OnOneLoopIsDone();
#endif

        }

        #region Methods for objects selector
        private string GetTypeName(object comObj)
        {

            if (comObj == null)
                return String.Empty;

            if (!Marshal.IsComObject(comObj))
                //The specified object is not a COM object
                return String.Empty;

            IDispatch dispatch = comObj as IDispatch;
            if (dispatch == null)
                //The specified COM object doesn't support getting type information
                return String.Empty;

            System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo = null;
            try
            {
                try
                {
                    // obtain the ITypeInfo interface from the object
                    dispatch.GetTypeInfo(0, 0, out typeInfo);
                    Logger.Info("GetTypeName", string.Format("Type getted:[{0}]", typeInfo.ToString()));
                }
                catch (Exception ex)
                {
                    Logger.Error("GetTypeName", string.Format("Excetpions:[{0}]", ex.Message), ex);
                    //Cannot get the ITypeInfo interface for the specified COM object
                    return String.Empty;
                }

                string typeName = "";
                string documentation, helpFile;
                int helpContext = -1;

                try
                {
                    //retrieves the documentation string for the specified type description 
                    typeInfo.GetDocumentation(-1, out typeName, out documentation,
                        out helpContext, out helpFile);
                    Logger.Info("GetDocumentation", string.Format("Type getted:[{0}], doc:[{1}]", typeName.ToString(), documentation.ToString()));
                }
                catch (Exception ex)
                {
                    // Cannot extract ITypeInfo information
                    Logger.Error("GetTypeName", string.Format("Excetpions:[{0}]", ex.Message), ex);
                    return String.Empty;
                }
                return typeName;
            }
            catch (Exception ex)
            {
                // Unexpected error
                Logger.Error("GetTypeName", string.Format("Excetpions:[{0}]", ex.Message), ex);
                return String.Empty;
            }
            finally
            {
                if (typeInfo != null) Marshal.ReleaseComObject(typeInfo);
            }
        }

        public string CheckObjectType(object objFromQtp)
        {
            Logger.Info("CheckObjectType", string.Format("type:[{0}],toString:[{1}]", objFromQtp.GetType().ToString(), objFromQtp.ToString()));
            GetTypeName(objFromQtp);
            return "";
        }
        #endregion

        #region Process Monitor methods
        public void BeginProcessMonitor()
        {
            Logger.logBegin("BeginProcessMonitor");
            ProcessOldListCurrent.Clear();
            ReadProcessInfoToDictionary(ProcessOldListCurrent);

            Logger.logEnd("BeginProcessMonitor");
        }
        public string EndProcessMonitor()
        {
            Logger.logBegin("EndProcessMonitor");
            ProcessNewListCurrent.Clear();
            ReadProcessInfoToDictionary(ProcessNewListCurrent);
            string strNewProcessId = GetNewProcessId();
            Logger.Info("EndProcessMonitor", string.Format("Find new Process Id :[{0}]", strNewProcessId ?? "NULL"));
            Logger.logEnd("EndProcessMonitor");
            return strNewProcessId;
        }

        public int SmartWait(object objProcessId, object objWaitSeconds = null)
        {
            Logger.logBegin("SmartWait");
            Logger.Info("SmartWait", string.Format("parameters:[{0}], [{1}]", objProcessId ?? "", objWaitSeconds ?? ""));
            string strProcessId = objProcessId == null ? null : objProcessId.ToString();
            if (strProcessId == null)
            {
                Logger.Error("SmartWait", "No monitored Application path is Passed");
                return 1;
            }

            int iProcessId = -1;
            try
            {
                iProcessId = int.Parse(strProcessId);
                int iWaitSeconds = objWaitSeconds == null ? 300 : string.IsNullOrEmpty(objWaitSeconds.ToString()) ? 300 : int.Parse(objWaitSeconds.ToString().Trim());
                int iWaited = 0;
                /** get process informaton **/
                Process objPs = Process.GetProcessById(iProcessId);

                //int iHitTime = 0;
                bool isContinueToWait = true;
                //int iSmartWaitMode;//1--main window is not visible
                bool isBrought2To = false;
                string strHintStatus = "";
                while (isContinueToWait && (iWaited <= iWaitSeconds))
                {
                    objPs.Refresh();
                    int iGuiThreadId = GetWindowThreadProcessId(objPs.MainWindowHandle, IntPtr.Zero);
                    Logger.Info("SmartWait", string.Format("get the current GUI Thread Id is [{0}]", iGuiThreadId));

                    if (!IsWindowVisible(objPs.MainWindowHandle))
                    {
                        Logger.Info("SmartWait", strHintStatus = string.Format("Main window is not visible, wait...It has [{0}] seconds waiting", iWaited));
                        Thread.Sleep(1000);
                        /** Write to Monitor **/

                        iWaited++;
                        continue;
                    }
                    if (!isBrought2To)
                    {
                        BringWindowToTop(objPs.MainWindowHandle);
                        Thread.Sleep(200);
                        // set cursor to the center of the window
                        MarsShellLib.ShellApi.RECT lpRect = new MarsShellLib.ShellApi.RECT();
                        if (GetWindowRect(objPs.MainWindowHandle, ref lpRect))
                        {
                            mouse_event(MOUSEEVENTF_MOVE, (lpRect.left + lpRect.right) / 2, (lpRect.top + lpRect.bottom) / 2, 0, 0);
                        }
                        isBrought2To = true;
                        Thread.Sleep(50);
                    }

                    //iSmartWaitMode = 2;//cursor mode



                    foreach (ProcessThread objT in objPs.Threads)
                    {
                        if (objT.Id == iGuiThreadId)
                        {
                            Logger.Info("SmartWait", string.Format("Find Thread with Id [{0}],name:[{1}], current Status is :[{2}]", objT.Id, objT.GetType().ToString(), objT.ThreadState));
                            Thread.Sleep(1000);
                        }
                    }

                    iWaited++;
                    // very 500 millionseconds call thread status once                
                }

                return (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("SmartWait", string.Format("Can't get the process inforamtion with exceptions [{1}],process Id :[{0}]", strProcessId, e.Message), e);
                return -1;
            }
        }
        #endregion //Process Monitor methods

        /** get Loop from framework service ,
         *  default value should be 1
         * 
         * **/

        public int GetStartLoop()
        {
            /** data should from monitor service **/
            if (this.mobjClient2Monitor == null) return 1;
            try
            {
                objDebugInfoFromCurrentModeMethod = GetDebugInfoFromMonitor();
                /** no debugInfo, perhaps, monitor service is not available **/
                if (objDebugInfoFromCurrentModeMethod == null) return 1;
                if (objDebugInfoFromCurrentModeMethod.TestDebugMode != (int)SystemDebuggerMode.SDM_REPLAY_THESAME_TEST) return 1;
                int iStartLoop = objDebugInfoFromCurrentModeMethod.TestCurrentLoopId <= 0 ? 1 : objDebugInfoFromCurrentModeMethod.TestCurrentLoopId;
                Logger.Info("GetStartLoop", string.Format("Start Loop:[{0}] CurrentLoopId:[{1}]", iStartLoop, objDebugInfoFromCurrentModeMethod.TestCurrentLoopId));
                return iStartLoop;
            }
            catch (Exception e)
            {
                Logger.Error("GetStartLoop", string.Format("Exception when getting current debbuger info, default value [1] returns. Exceptions:[{0}]", e.Message, e));
                return 1;
            }
        }



        private TestFlowDebugInfo GetDebugInfoFromMonitor()
        {
            if (this.mobjClient2Monitor == null) return null;
            try
            {
                objDebugInfoFromCurrentModeMethod = null;
                GetCurrentDebugModeProxy();
                return objDebugInfoFromCurrentModeMethod;
            }
            catch (Exception e)
            {
                Logger.Error("GetDebugInfoFromMonitor", string.Format("Exceptions [{0}]", e.Message), e);
                return objDebugInfoFromCurrentModeMethod = null;
            }
        }

        public void SetCurrentLoopId(object currerntLoopId)
        {
            int iLoopId = -1;
            try
            {
                iLoopId = int.Parse(currerntLoopId.ToString());
                if (this.mobjClient2Monitor == null) return;
                this.mobjClient2Monitor.OnCurrentLoopChangeEvent(iLoopId);
            }
            catch (Exception e)
            {
                Logger.Error("SetCurrentLoopId", string.Format("Can't setCurrent Loop Id to monitor, the value is: [{0}],exception:[{1}]", currerntLoopId ?? "NULL", e.Message), e);
            }
        }

        public string GetCurrentMonitorMode()
        {
            string strCurrentMonitorMode = "";
            Logger.logBegin("GetCurrentMonitorMode");

            Logger.logEnd("GetCurrentMonitorMode");
            return strCurrentMonitorMode;
        }

        public int GetExtraPopupMenuCount()
        {
            if (this.mobjClient2Server != null)
                return this.mobjClient2Server.GetExtraPopupMenuCount();
            return 1;
        }

        public int UpdateTestStepResultStatus(object objId, object objStatus, object objErrorInfo, object objBeginTimeStr, object objEndTimeStr)
        {

            return (int)ERROR_CODE._NO_ERROR;
        }

        public string GetErrorBitmapDir()
        {
#if _NO_C_DRIVER_WRITE
            string strPrntDir = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
#else
            string strFullPath = Assembly.GetExecutingAssembly().Location;
            string strPrntDir = Directory.GetParent(Directory.GetParent(strFullPath).FullName).FullName;
#endif
            string strDes = string.Format("{0}\\BitMap\\", strPrntDir);
            if (!Directory.Exists(strDes))
            {
                Directory.CreateDirectory(strDes);
            }
            return strDes;
        }

        public int LogCurrentStatusOfTesting(object strInfomation, object iNormalOrError)
        {
            if (this.mobjClient2Monitor == null) return (int)ERROR_CODE._NO_ERROR;
            string strMsg = strInfomation == null ? null : strInfomation.ToString();
            if (string.IsNullOrEmpty(strMsg)) return (int)ERROR_CODE._NO_ERROR;
            int iErrorOrNormal = -1;
            try
            {
                iErrorOrNormal = Int16.Parse(iNormalOrError.ToString());
                iErrorOrNormal = iErrorOrNormal >= 0 ? 1 : -1;
            }
            catch
            {
                iErrorOrNormal = -1;
            }
            this.mobjClient2Monitor.OnClientWriteCurrentLog(strMsg, iErrorOrNormal);
            return 1;
        }

        public int GetWriteBackModeByKeyWord(object strCurrentKeyWord)
        {
            if (strCurrentKeyWord == null)
            {
                Logger.Error("GetWriteBackModeByKeyWord", "Parameter is Null");
            }
            if (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE.ToUpper(), strCurrentKeyWord.ToString().ToUpper(), true) == 0)
            {
                return 2;
            }
            if (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREVALUE.ToUpper(), strCurrentKeyWord.ToString().ToUpper(), true) == 0)
            {
                return 1;
            }
            if (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY.ToUpper(), strCurrentKeyWord.ToString().ToUpper(), true) == 0)
            {
                return 3;
            }


            return 0;
        }

        public string GetBaseLineMode()
        {
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetBaseLineMode", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return null;
            }
            string strBaseLineInfo = this.mobjClient2Server.GetBaseLineMode();
            Logger.Info("GetBaseLineMode", string.Format("Get baseLine :[{0}]", strBaseLineInfo));
            return strBaseLineInfo;
        }

        public string GetCurrentApplcationCmd(ref string strAPPId)
        {
            Logger.logBegin("GetCurrentApplcationCmd");
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetCurrentApplcationCmd", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return "";
            }
            string strCurrentAppCmd = this.mobjClient2Server.GetCurrentApplicationCmd();
            strAPPId = this.mobjClient2Server.GetCurrentApplicationIdentifier();
            Logger.Info("GetCurrentApplcationCmd", string.Format("Data Return:[{0}]", strAPPId));
            return strCurrentAppCmd == null ? "" : strCurrentAppCmd;
        }

        public string GetCurrentApplcationCmd(ref string strAPPId, ref string strCmmdPara)
        {
            Logger.logBegin("GetCurrentApplcationCmd");
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetCurrentApplcationCmd", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return "";
            }
            string strCurrentAppCmd = this.mobjClient2Server.GetCurrentApplicationCmdWithPara(ref strCmmdPara);
            //string strCurrentAppCmd = this.mobjClient2Server.GetCurrentApplicationCmd();
            strAPPId = this.mobjClient2Server.GetCurrentApplicationIdentifier();
            Logger.Info("GetCurrentApplcationCmd", string.Format("Data Return:[{0}]", strAPPId));
            return strCurrentAppCmd == null ? "" : strCurrentAppCmd;
        }

#if _ver1_5
        public string GetCurrentApplicationCmdWithPara(ref object objCurrentDir, ref object objKeyCmd, ref object strPara)
        {
            Logger.logBegin("GetCurrentApplicationCmdWithPara");
            try
            {
                string strAppId = "", strCmmdPara = "";
                string strCurrrentApplication = GetCurrentApplcationCmd(ref strAppId, ref strCmmdPara);
                objCurrentDir = TigerMarsUtil.GetPathWithoutFileName(strCurrrentApplication);
                objKeyCmd = strAppId;// Path.GetFileName(strCurrrentApplication);
                strPara = strCmmdPara;
                Logger.logEnd("GetCurrentApplicationCmd15");
                return strCurrrentApplication;
            }
            finally
            {
                Logger.logEnd("GetCurrentApplicationCmdWithPara");
            }
        }
        public string GetCurrentApplicationCmd15(ref object objCurrentDir, ref object objKeyCmd)
        {
            Logger.logBegin("GetCurrentApplicationCmd15");

            string strAppId = "";
            string strCurrrentApplication = GetCurrentApplcationCmd(ref strAppId);
            objCurrentDir = TigerMarsUtil.GetPathWithoutFileName(strCurrrentApplication);
            objKeyCmd = strAppId;// Path.GetFileName(strCurrrentApplication);
            Logger.logEnd("GetCurrentApplicationCmd15");
            return strCurrrentApplication;
        }
#endif

        /***
         * Write Test suite status back
         * ***/
        public bool NotifyCurrentTestSuiteRunStatus(object strStatus = null, object strCauseReason = null, object objIsSkipWrongTestSuite = null)
        {
            /** **/
            const string cnst_status_T = "true";
            const string cnst_status_F = "false";
            const string cnst_SUCCESS = "SUCCESS";
            const string cnst_FAILURE = "FAILURE";
            if (this.mobjClient2Server == null)
            {
                Logger.Error("NotifyCurrentTestSuiteRunStatus", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return false;
            }
            /** Write back to Server **/
            string strOKOrNOT = strStatus == null ? "true" : strStatus.ToString();
            if (!((string.Compare(cnst_status_T, strOKOrNOT, true) == 0) || (string.Compare(cnst_status_F, strOKOrNOT, true) == 0)))
            {
                Logger.Error("NotifyCurrentTestSuiteRunStatus", "value should be true/false");
                return false;
            }
            mobjRunStatus.RunResult = string.Compare(cnst_status_T, strOKOrNOT, true) == 0 ? cnst_SUCCESS : cnst_FAILURE;
            mobjRunStatus.EndTime = DateTime.Now.ToString(CNST_DATEFORMAT);
            mobjRunStatus.CauseReason = strCauseReason == null ? "" : strCauseReason.ToString();
            bool isContinueWhenFalse = true;
            if (objIsSkipWrongTestSuite != null)
            {
                if (!bool.TryParse(objIsSkipWrongTestSuite.ToString(), out isContinueWhenFalse))
                    isContinueWhenFalse = false;
            }
            int iResult = this.mobjClient2Server.NotifyCurrentTestSuiteRunStatus(this.mobjRunStatus, isContinueWhenFalse);

            /** write back to Monitor **/
            string strMsg = string.Format("Currrent Test suite Run status is   with {0}", (string.Compare(cnst_status_T, strOKOrNOT, true) == 0) ? "PASSED" : "FAULT");
            this.LogCurrentStatusOfTesting(strMsg, (string.Compare(cnst_status_T, strOKOrNOT, true) == 0) ? "-1" : "1");
            return iResult == (int)ERROR_CODE._NO_ERROR;
        }

        public void TestTool_NotificationTSTCChangeEvent(object objTestSuiteName, object objTestCaseName)
        {
            /** called by QTP or other TestTool **/
            if ((objTestSuiteName == null) || (objTestCaseName == null))
            {
                Logger.Error("TestTool_NotificationTSTCChangeEvent", "Parameters are null.");
                return;
            }
            if (this.mobjClient2Monitor == null) return;
            this.mobjClient2Monitor.OnClientTestSuiteTestCaseNamesChangeEvent(objTestSuiteName.ToString(), objTestCaseName.ToString());
        }

        public void ClickPositionByMonitor(object ox, object oy)
        {
            Logger.logBegin("ClickPositionByMonitor", string.Format("ox-{0}, oy-{1}", ox, oy));
            if (this.mobjClient2Monitor == null) return;
            int ix = 0, iy = 0;
            if (ox == null || oy == null) return;
            if ((!int.TryParse(ox.ToString(), out ix)) || (!int.TryParse(oy.ToString(), out iy)))
            {
                Logger.Error("ClickPositionByMonitor", string.Format("ox, or oy is not number -[ix {0}],[iy {1}]", ix, iy));
                return;
            }
            SetCursorPos(ix, iy);
            MouseSimulator.ClickLeftMouseButton();
            this.mobjClient2Monitor.ClickSpecialPos(ix, iy);

        }


        public void SetDataSetName()
        {
#if v_16AndUp
            if (this.mobjClient2Server == null) return;
            if (this.mobjClient2Monitor == null) return;
            string strDataSetName = this.mobjClient2Server.GetCurrentTestDatasetName();
            Logger.Info("TestTool_NotificationTSTCChangeEvent", string.Format("Dataset Name:[{0}]", strDataSetName));
            this.mobjClient2Monitor.SetTestDataSetName(strDataSetName);
#endif

        }

        /**
         * 由于可以重新执行当前的脚本，因此，需要首先从monitor获得当前运行模式。
         * 有两种情况：
         * 1，qtp重启过
         * 2，qtp没有重启过
         * qtp重启过，那么需要重新连接monitorservice。而wcf会创建一个新的对象实例。运行模式对象应该保存在form对象中。
         * 或者，使用singleton模式， monitorservice只是用一个实例。那么，任何时候，这里连接到monitor时都是同一个对象实例
         * 
         * */
        public bool BeginNavigateTestSuite()
        {
            try
            {


                /** get the current mode from monitor **/
                TestFlowDebugInfo objCurrentDebugInfo = null;
                if (this.mobjClient2Monitor != null)
                {
                    objCurrentDebugInfo = GetDebugInfoFromMonitor(); ;
                    if (objCurrentDebugInfo != null)
                    {
                        if ((SystemDebuggerMode)objCurrentDebugInfo.TestDebugMode == SystemDebuggerMode.SDM_REPLAY_THESAME_TEST)
                        {

                        }
                    }
                }
                /** **/
                if (this.mobjClient2Server == null)
                {
                    Logger.Error("BeginNavigateTestSuite", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                    return false;
                }
                if ((objCurrentDebugInfo == null) || ((SystemDebuggerMode)objCurrentDebugInfo.TestDebugMode != SystemDebuggerMode.SDM_REPLAY_THESAME_TEST))
                {
                    int iResult = this.mobjClient2Server.BeginNavigateTestSuite();
                    return iResult == (int)ERROR_CODE._NO_ERROR;
                }
                else
                {
                    /** need tell test framework which one to run **/
                    int iResult = this.mobjClient2Server.BeginNavigateTestSuiteWithRelyIdAndLoop(objCurrentDebugInfo.CurrentRelyId, objCurrentDebugInfo.TestCurrentLoopId);
                    if (iResult != (int)ERROR_CODE._NO_ERROR)
                    {
                        /** notify user that can't run from the current test case and ask whether system can run from beginning **/
                        if (MessageBox.Show(string.Format("Can't run the current test script [Testsuite:{0}, TestCase:{1}, Loop:{2}, RelyId:{3}]. Error Code:[{4}]. \r\n Click Ok/Yes, system will run from beginning. Otherwise, please change settings from TestStep Viewer.",
                            objCurrentDebugInfo.CurrentTestSuiteName, objCurrentDebugInfo.CurrentTestCaseName, objCurrentDebugInfo.TestCurrentLoopId,
                            objCurrentDebugInfo.CurrentRelyId, iResult), "Hint", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            return this.mobjClient2Server.BeginNavigateTestSuite() == (int)ERROR_CODE._NO_ERROR;
                        }
                        else
                        {
                            return false;
                        }

                    }

#if v_16AndUp
                    /// Added by tiger
                    /// Get test storyboard information and Total test Storyboard test cases count from serverside, then 
                    /// push to monitor
                    /// 
                    string strError = "";
                    bool isGetAndSetStoryboardInfo = GetAndSetStoryboardInfo(ref strError);
                    if (!isGetAndSetStoryboardInfo)
                    {
                        Logger.Error("BeginNavigateTestSuite", string.Format("Error:[{0}]", strError));
                        return false;
                    }
#endif
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("BeginNavigateTestSuite", string.Format("Exception generated:[{0}]", e.Message), e);
                return false;
            }
        }

        public bool GetNextTestSuite(object objCurrentTestSuiteID = null)
        {
            /** **/
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetNextTestSuite", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return false;
            }
            /** clear obj status **/
            ClearStatusObject();

            #region New Loop Part,2017-1
            cleanLoopInfoStack();
            #endregion //New Loop Part,2017-1

            this.mobjRunStatus.StartTime = DateTime.Now.ToString(CNST_DATEFORMAT);
            try
            {
                int iResult = this.mobjClient2Server.OnGetNextTestSuite();
                if (iResult == (int)ERROR_CODE._NO_ERROR)
                {
                    string strCurrentTestSuiteId4Project = this.mobjClient2Server.GetCurrentTestSuiteId4Project();
                    if ((strCurrentTestSuiteId4Project != null) && (this.mobjClient2Monitor != null))
                    {
                        this.mobjClient2Monitor.OnClientTestSuiteId4ProjectReadyEvent(strCurrentTestSuiteId4Project);
                    }
                }

                /** clean debugger Info **/
                CleanDebugerInfoForTSChange();

                return iResult == (int)ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("GetNextTestSuite", string.Format("Exception:[{0}], \r\ntrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }

        }



        private void ClearStatusObject()
        {
            this.mobjRunStatus.CauseReason = "";
            this.mobjRunStatus.EndTime = "";
            this.mobjRunStatus.RunResult = "";
            this.mobjRunStatus.StartTime = "";
        }
        public int beforeRunTestStepImpl(object objCurrentTestStep)
        {
            dataFromClientToWriteBack = "";
            if (this.mobjClient2Monitor != null)
            {
                this.mobjClient2Monitor.BeforeClientRunTestStepEvent(this.mobjCurrentTestStep);
            }
            return (int)ERROR_CODE._NO_ERROR;
        }
        private string dataFromClientToWriteBack = "";
        public int afterRunTestStepImpl(object objMessage, object isRightOrNot, object objOutData)
        {
            if (this.mobjClient2Monitor != null)
            {
                string strResult = isRightOrNot == null ? "true" : isRightOrNot.ToString();
                int iResult;
                if ((strResult.ToUpper().CompareTo("TRUE") == 0) || (strResult.CompareTo("1") == 0))
                {
                    iResult = 1;
                }
                else
                {
                    iResult = 0;
                }
                this.mobjClient2Monitor.AfterClientRunTestStepEvent(objOutData == null ? "" : objOutData.ToString(), iResult, objMessage == null ? "" : objMessage.ToString());
            }
            return (int)ERROR_CODE._NO_ERROR;

        }

        /** get current step list to monitors **/
        public int LoadTestStepForCompiler()
        {
            if (this.mobjClient2Server == null)
            {
                Logger.Error("LoadTestStepForCompiler", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
            }
            int iErrorId = (int)ERROR_CODE._NO_ERROR;
            List<TestStep4Services> lstResult = TestStepsMgr.CachedTestSteps = this.mobjClient2Server.GetCurrentCompiledList(ref iErrorId);

            Logger.Info("LoadTestStepForCompiler", "Testing");
            if (iErrorId != (int)ERROR_CODE._NO_ERROR)
            {
                Logger.Error("LoadTestStepForCompiler", string.Format("Can't get TestSteps from Server side. Error Id is :{0}\r\n\tCheck Log inforamtion on server to get more details.", iErrorId));
                return iErrorId;
            }
            /** load to Monitor **/
            if (this.mobjClient2Monitor != null)
            {
                try
                {
                    this.mobjClient2Monitor.OnClientTestCaseListChangeEvent(lstResult);
                }
                catch (Exception e)
                {
                    Logger.Error("LoadTestStepForCompiler", string.Format("Exceptions:[{0}]", e.Message), e);
                }

            }
            Logger.Info("LoadTestStepForCompiler", string.Format("After load compiled info to Monitor:[{0}]", iErrorId));
            return (int)ERROR_CODE._NO_ERROR;
        }

        public int CompileCurrentTestCase(object objCurrentSuite, object objCurrentCase, out object objErrorInfo)
        {
            Logger.logBegin("CompileCurrentTestCase");
            /** tell server to compile the current Testcase **/
            if (this.mobjClient2Server == null)
            {
                Logger.Error("CompileCurrentTestCase", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                objErrorInfo = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST);
                return (int)ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;

            }
            TestStep4Services objService = new TestStep4Services();
            int iErrorStepId = -1;
            string strErrorInfo = "";
            int iError = (int)this.mobjClient2Server.CompilerCurrentTestCase(objCurrentSuite.ToString(), objCurrentCase.ToString(), ref objService, ref iErrorStepId, ref strErrorInfo);
            /**notify monitor**/
            if (this.mobjClient2Monitor != null)
            {
                List<TestStep4Services> lstErrorObj = new List<TestStep4Services>();
                if ((iErrorStepId >= 0) && (iError != (int)ERROR_CODE._NO_ERROR)) lstErrorObj.Add(objService);
                this.mobjClient2Monitor.OnClientTestCompilerEndEvent(iError != (int)ERROR_CODE._NO_ERROR, strErrorInfo, lstErrorObj);
            }
            objErrorInfo = strErrorInfo;
            Logger.Info("CompileCurrentTestCase", string.Format("CompileCurrentTestCase END WITH ERROR INFO:[{0}]", strErrorInfo));
            return iError;
        }

        public ERROR_CODE ConnectToWCFMonitorServer(ref bool isOk, ref string strError)
        {
            Logger.Info("ConnectToWCFMonitorServer", "Begin....");

            string strURL = GetAccessMonitorURL(ref isOk, ref strError);
            this.mobjClient2Monitor = null;
            if (strURL == null)
            {
                return ERROR_CODE._SERVICE_ERROR_NO_MONITOR_SEVICE_PARA_0;
            }

            try
            {
                //BasicHttpBinding objHttpBasicBinding = new BasicHttpBinding();
                NetTcpBinding objHttpBasicBinding = new NetTcpBinding();
                EndpointAddress objEndpoint = new EndpointAddress(strURL);
#if !_CallBackMode
                //ChannelFactory<IMonitorService> objFactory = new ChannelFactory<IMonitorService>(objHttpBasicBinding, objEndpoint);
                //mobjClient2Monitor = objFactory.CreateChannel();
                objHttpBasicBinding.OpenTimeout = new TimeSpan(0, 10, 0);
                objHttpBasicBinding.CloseTimeout = new TimeSpan(0, 10, 0);
                objHttpBasicBinding.SendTimeout = new TimeSpan(0, 10, 0);
                objHttpBasicBinding.ReceiveTimeout = new TimeSpan(0, 10, 0);


                if (objDualFactory != null)
                {
                    try
                    {
                        objDualFactory.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ConnectToWCFMonitorServer", string.Format("Exception:[{0}]", ex.Message, ex));
                    }

                    objDualFactory = null;
                }
                objDualFactory =
                    new DuplexChannelFactory<IMonitorService>(this, objHttpBasicBinding, objEndpoint);
                try
                {
                    mobjClient2Monitor = objDualFactory.CreateChannel();
                    
                }
                catch (Exception ex)
                {
                    Logger.Error("ConnectToWCFMonitorServer", string.Format("Exception:[{0}]", ex.Message, ex));
                    /** try again **/
                    mobjClient2Monitor = objDualFactory.CreateChannel();
                }

                this.SubscribProxy();
                //mobjClient2Monitor.Subscribe();
                Logger.Info("ConnectToWCFMonitorServer", string.Format("Connect to Monitor service and get token:[{0}]", MonitorTokenId));

#else

#endif

                //objFactory.                
                return ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                ERROR_CODE eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("ConnectToWCFMonitorServer", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strURL, e.Message), e);
                return eCde;
            }
            finally
            {
                Logger.logEnd("ConnectToWCFMonitorServer");
            }

        }



        public void SetMonitorTokenId(int iMonitorToken)
        {
            MonitorTokenId = iMonitorToken;
        }
        #region Teststep Mode
        //public bool StartTestStepMode(int iMonitorPort,int iSvcPort, ref string strError,string strUserName)
        //{
        //    Logger.logBegin("StartTestStepMode");
        //    try
        //    {
        //        WCFXmlCfgMgr.CurrentLoginUser = strUserName;
        //        ERROR_CODE eCde = StartWCFClientWithMode(iMonitorPort, iSvcPort, ref strError,1);
        //        return eCde==ERROR_CODE._NO_ERROR;
        //    }
        //    finally
        //    {
        //        Logger.logEnd("StartTestStepMode");
        //    }
        //}

        private Thread mThrdDaemon = null;
        private Stack<MarsTestStepInfoForTestStepUnitTest> mTestStepsWithCmdStack = new Stack<MarsTestStepInfoForTestStepUnitTest>();
        private MarsTestStepInfoForTestStepUnitTest mcurrentTeststepUnitInfoFromStack = null;

        private string TestStepStackMonitorString = "TestStepStackMonitorString";

        public bool StartServices(ref string strError)
        {
            Logger.logBegin("StartServices");
            try
            {
                if (mThrdDaemon == null)
                {
                    mThrdDaemon = new Thread(FetchCommandThread);
                }
                else
                {
                    Logger.Info("StartServices", string.Format("Thread status:[{0}]", mThrdDaemon.ThreadState));
                    if (mThrdDaemon.ThreadState == System.Threading.ThreadState.Running)
                    {
                        return true;
                    }
                    else
                    {
                        mThrdDaemon.Abort();
                        mThrdDaemon = new Thread(FetchCommandThread);
                    }

                }
                mThrdDaemon.Priority = ThreadPriority.Normal;
                mThrdDaemon.Start();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("StartServices", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("StartServices");
            }
        }

        private static AutoResetEvent _eventFromService = new AutoResetEvent(false);

        private void FetchCommandThread()
        {
            int iCount = 1;
            while (true)
            {
                Thread.Sleep(2000);
                try
                {
                    ///每循环1000次，休息5秒钟
                    /// 
                    if ((iCount++) % 1000 == 0)
                    {
                        iCount = 1;
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                    string strError = "";
                    bool isOk = false;

                    MarsTestStepInfoForTestStepUnitTest objStp = this.mobjClient2Server.GetTestStepUnitFromStack(ref strError, ref isOk);
                    if ((objStp == null) || (!isOk))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        mTestStepsWithCmdStack.Push(objStp);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("FetchCommandThread", string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace));
                    break;
                }
            }
        }
        /// <summary>
        /// 该函数供vbs代码调用
        /// </summary>
        /// <param name="isTestOK"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool NotifyResultForTestStep(bool isTestOK, string strError)
        {
            Logger.logBegin("NotifyResultForTestStep");
            Logger.Info("NotifyResultForTestStep", string.Format("isTestOk:[{0}] strError:[{1}]", isTestOK, strError ?? "N/A"));
            try
            {
                if (this.mobjClient2Server == null)
                {
                    Logger.Error("NotifyResultForTestStep", "No server connections.");
                    return false;
                }
                this.mobjClient2Server.NotifiResultForTestStep(this.mcurrentTeststepUnitInfoFromStack, isTestOK, strError);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("NotifyResultForTestStep", string.Format("Exception:[{0}] StackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("NotifyResultForTestStep");
            }
        }

        /// <summary>
        /// 该函数供vbs调用，判断是daemon thread状态。如果该线程处于运行状态，则返回true
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool isTestStepServiceEnd(ref string strError)
        {
            Logger.logBegin("isTestStepServiceEnd");
            try
            {
                if (this.mThrdDaemon == null)
                {
                    Logger.Error("isTestStepServiceEnd", strError = "No Thread instance is created");
                    return false;
                }

                return ((this.mThrdDaemon.ThreadState == System.Threading.ThreadState.Running)
                    || (this.mThrdDaemon.ThreadState == System.Threading.ThreadState.WaitSleepJoin));
            }
            finally
            {
                Logger.logEnd("isTestStepServiceEnd");
            }
        }
        /// <summary>
        /// 从命令堆栈中获得数据，一旦处理完，清除堆栈内容
        /// </summary>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public string GetCurrentCommand(ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetCurrentCommand");
            try
            {
                System.Threading.Monitor.Enter(this.TestStepStackMonitorString);

                isOk = true;
                if (this.mTestStepsWithCmdStack == null) return "";
                if (this.mTestStepsWithCmdStack.Count == 0) return "";
                Logger.Info("GetCurrentCommand", string.Format("Stack count:[{0}]", this.mTestStepsWithCmdStack.Count));
                mcurrentTeststepUnitInfoFromStack = this.mTestStepsWithCmdStack.Pop();
                Logger.Info("GetCurrentCommand", string.Format("Stack count,after pop:[{0}],command:[{1}]", this.mTestStepsWithCmdStack.Count, mcurrentTeststepUnitInfoFromStack == null ? "N/A" : mcurrentTeststepUnitInfoFromStack.Command));
                return mcurrentTeststepUnitInfoFromStack.Command;
            }
            catch (Exception e)
            {
                Logger.Error("GetCurrentCommand", strError = string.Format("Exception:[{0}],stackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return "";
            }
            finally
            {
                System.Threading.Monitor.Exit(this.TestStepStackMonitorString);
            }
        }
        /// <summary>
        /// 供vbs调用，产生vbs可以执行的代码片段
        /// </summary>
        /// <param name="isOk">是否执行成功</param>
        /// <param name="strError">错误信息</param>
        /// <returns></returns>
        public MarsTestStepInfoForTestStepUnitTest GetCurrentRunnableTestStepString(ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetCurrentRunnableTestStepString");
            try
            {
                System.Threading.Monitor.Enter(this.TestStepStackMonitorString);
                if (mcurrentTeststepUnitInfoFromStack == null)
                {
                    isOk = false;
                    Logger.Info("GetCurrentRunnableTestStepString", strError = "No Teststep object information. [object = null]");
                    return null;
                }
                ///依据teststep 数据从service端获得可以执行的命令字
                /// 如，filledit("javawindow(""title:=some title"", ""index:=0"").javaEdit(""attached text:=some strings"")","Data to fill","some parameters", "Data to fill for extend")
                /// 
                //mcurrentTeststepUnitInfoFromStack.TestStepDetail.
                return mcurrentTeststepUnitInfoFromStack;
                //this.mcurrentTeststepUnitInfoFromStack.TestStepDetail.
            }
            finally
            {
                System.Threading.Monitor.Exit(this.TestStepStackMonitorString);
                Logger.logEnd("GetCurrentRunnableTestStepString");
            }
        }

        #endregion //Test Step mode
        private ERROR_CODE StartWCFClientWithMode(int iMonitorPort, int iSvrPort, ref string strError, int iMode = 0)
        {
            Logger.logBegin("StartWCFClient");
            Logger.Info("StartWCFClient", string.Format("iMode:[{0}]", iMode));
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            string strURL = "";
            try
            {
                bool isOk = false;

                /** get the URL  **/
                strURL = GetAccessURL(ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("StartWCFClientWithMode", strError);
                    return ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING;
                }
                //BasicHttpBinding objHttpBasicBinding = new BasicHttpBinding();
                NetTcpBinding objTCPBind = new NetTcpBinding();
                EndpointAddress objEndpoint = new EndpointAddress(strURL);
                objTCPBind.Security = new NetTcpSecurity();
                objTCPBind.Security.Mode = SecurityMode.None;

                objTCPBind.ReceiveTimeout = TimeSpan.FromMinutes(10);
                objTCPBind.SendTimeout = TimeSpan.FromMinutes(10);
                objTCPBind.CloseTimeout = TimeSpan.FromMinutes(10);
                objTCPBind.OpenTimeout = TimeSpan.FromMinutes(10);
                /*
                objTCPBind.MaxReceivedMessageSize = 10 * 1024 * 1024;
                objTCPBind.MaxBufferPoolSize = 10 * 1024 * 1024;
                objTCPBind.MaxBufferSize = 10 * 1024 * 1024;
                objTCPBind.TransferMode = TransferMode.Buffered;
*/
                DuplexChannelFactory<IMarsTigerFrameWorkService> objFactory = new DuplexChannelFactory<IMarsTigerFrameWorkService>(this, objTCPBind, objEndpoint);
                mobjClient2Server = objFactory.CreateChannel();

                this.isAutoCheckError = mobjClient2Server.IsAutoCheckErrorEnable();
                if (iMode == 0)
                    ConnectToWCFMonitorServer(ref isOk, ref strError);
                else
                {
                    ///如果是mode==1,那么是线程查询模式
                    ///启动线程 进行svc查询 

                }
                if (!isOk)
                {
                    return ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING;
                }
                return eCde;
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
                Logger.Error("StartWCFClient", strError = string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strURL, e.Message), e);
                return eCde;
            }
            finally
            {
                Logger.logEnd("StartWCFClient");
            }
        }

        public ERROR_CODE StartWCFClient(int iMonitorPort, int iSvcPort, string strCurrentUserInfo)
        {
            #region license info
            //ILicenseMgr gLicenseMgr = TestFrameLicense.LoadLicense() ;
            //bool isShowLicenseInfo = false;
            //if (gLicenseMgr==null)
            //{
            //    isShowLicenseInfo = true;
            //}
            //else
            //{
            //    isShowLicenseInfo = !gLicenseMgr.isAvailable() ;
            //}
            //if (isShowLicenseInfo)
            //{
            //    MessageBox.Show("Mars Automation Testing framework is expired already.\r\nPlease contact Marquis Business Tech solution LLC renewwing.","WARNING") ;
            //    return ERROR_CODE._COMPILER_NO_SUCH_OBJECT_FILE;
            //}
            #endregion //license Info
            if (string.Compare(WCFXmlCfgMgr.CurrentLoginUser, strCurrentUserInfo ?? "", true) != 0)
                WCFXmlCfgMgr.CurrentLoginUser = strCurrentUserInfo;
            string strError = "";
            return StartWCFClientWithMode(iMonitorPort, iSvcPort, ref strError, 0);
        }

        public bool IsAutoCheckErrorRequired()
        {
            return this.isAutoCheckError ?? false;
        }

        private TestStepObject currentAutoCheckObject = null;
        public string GetCurrentCheckObjectIdentifier()
        {
            Logger.logBegin("GetCurrentCheckObjectId");
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetCurrentCheckObjectId", "Server connection object is null");
                return "";
            }
            try
            {
                if (!(this.isAutoCheckError ?? false)) return "";

                currentAutoCheckObject = this.mobjClient2Server.GetDefaultErrorCheckingObj4CurrentPeg("MarsEntities");
                if (currentAutoCheckObject == null)
                {
                    Logger.Info("GetCurrentCheckObjectId", "AutocheckObject from Server is null");
                    return "";
                }
                return string.Format("{0}.{1}", currentAutoCheckObject.PEG_QUICK_ACCESS, currentAutoCheckObject.QUICK_ACCESS);
            }
            catch (Exception e)
            {
                Logger.Error("GetCurrentCheckObjectId", string.Format("Exception:[{0}] ", e.Message), e);
                return "";
            }
        }

        public string GetApplicationFullCmdByShortName(object strShortName)
        {
            int iErrorCde = (int)ERROR_CODE._NO_ERROR;
            string strResult = "";
            if (strShortName == null)
            {
                Logger.Error("GetApplicationFullCmdByShortName", "strShortName parameter is null");
                return "";
            }
            string strAppId = "";
            strResult = mobjClient2Server == null ? null : mobjClient2Server.GetApplicationFullCmdByShortName(strShortName.ToString(), ref strAppId, ref iErrorCde);
            if ((strResult == null) || ((ERROR_CODE)iErrorCde != ERROR_CODE._NO_ERROR))
            {
                Logger.Error("GetApplicationFullCmdByShortName", string.Format("NO such Application Name found-[{0}]", strShortName));
                return "";
            }
            return strResult;
        }

        public void StartMonitor()
        {
            /**
             * monitor contains all information about current running mode, for example run-current test case 
             * when connect to Monitor service, this inforamtion should get first and then connect to Framework service
             * all details about current test script, test suite and loop information should get and set to framework service
             * */
            if (this.mobjClient2Monitor == null) return;

            TestFlowDebugInfo objDebugInfo = GetDebugInfoFromMonitor();
            if (objDebugInfo != null)
            {
                if ((SystemDebuggerMode)objDebugInfo.TestDebugMode == SystemDebuggerMode.SDM_REPLAY_THESAME_TEST)
                {
                    /** set value to framework with sepcified testcase,testsuite and loopid **/
                    if (this.mobjClient2Server == null) return;

                }
            }
        }


        public string GetScriptLogPath()
        {
#if _NO_C_DRIVER_WRITE
            string strFullPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            try
            {
                if (!Directory.Exists(Path.Combine(strFullPath, "MARS")))
                {
                    Directory.CreateDirectory(Path.Combine(strFullPath, "MARS"));
                }
                strFullPath = Path.Combine(strFullPath, "MARS");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Exception when create folders under Application Director:[{0}]\r\n{1}", strFullPath, e.Message));
                //return ;
                strFullPath = Assembly.GetExecutingAssembly().Location;
            }
#else
            /** to set QTP Log file path **/
            string strFullPath = Assembly.GetExecutingAssembly().Location;
#endif
            return Path.Combine(TigerMarsUtil.GetPathWithoutFileName(strFullPath), "log");
        }

        public bool SwitchData(object strStepValue)
        {
            Logger.logBegin("SwitchData");
            Logger.Info("INFO", TigerMarsUtil.GetParameter("StepValue", strStepValue == null ? "" : strStepValue.ToString()));
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            /** change current Data file to stetpValue **/
            if (strStepValue == null)
            {
                eCde = ERROR_CODE._SERVICE_ERROR_CLIENT_SWITCH_DATA_NO_FILENAME_PARA_0;
                Logger.Error("SwitchData", ERROR_INFO.GET_ERROR_STR(eCde));
                return false;
            }
            eCde = (ERROR_CODE)this.mobjClient2Server.SwitchDataFile(strStepValue.ToString());
            Logger.logEnd("SwitchData");
            return eCde == ERROR_CODE._NO_ERROR;
        }

        /**
         * Used for QTP to tell client agent, that a new test loop is beginning
         * 
         * **/
        public ERROR_CODE BeginNavigate(object iLoop)
        {
            Logger.Info("BeginNavigate", string.Format("iLoop:[{0}]", iLoop == null ? "N/A" : iLoop.ToString()));
#if v_16AndUp
            this.lastResumeNextError = null;
#endif
            if (this.mobjClient2Server == null)
            {
                Logger.Error("BeginNavigate", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
            }
            miCurrentLoopId = -1;
            try
            {
                if (!this.mobjClient2Server.StartTestStepNavigate())
                {
                    Logger.Error("BeginNavigate", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE));
                    return e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CANT_START_NAVIGATE;
                }
            }
            catch (Exception e)
            {
                Logger.Error("BeginNavigate", string.Format("Exception:[{0}]", e.Message), e);
                return ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
            }

            miCurrentLoopId = Int16.Parse(iLoop.ToString());

            /** tell monitor, the current loop is **/
            if (this.mobjClient2Monitor != null)
            {
                this.mobjClient2Monitor.OnCurrentLoopChangeEvent(miCurrentLoopId);
            }

            /** clean break points and other debugger information **/
            CleanDebugerInfoForLoopChange();

            Logger.logEnd("BeginNavigate");
            return ERROR_CODE._NO_ERROR;
        }

        public bool CurrentLoopSkip()
        {
            Logger.logBegin("CurrentLoopSkip");
            if (this.mobjClient2Server == null)
            {
                Logger.Error("CurrentLoopSkip", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return false;
            }

            bool isDataSetSet2Skip = this.mobjClient2Server.IsDataSetSet2Skipped(this.miCurrentLoopId);
            Logger.Info("CurrentLoopSkip", string.Format("Returns :[{0}]", isDataSetSet2Skip));
            Logger.logEnd("CurrentLoopSkip");
            return isDataSetSet2Skip;
        }

        private void CleanDeubgerInfoForDebuggerModeChange()
        {
            if (debugMgrInfo == null) return;
            System.Threading.Monitor.Enter(this.DebugListCriticalSection);
            try
            {
                debugMgrInfo.CleanDebugerInfo4DeubgerModeChange();
            }
            finally
            {
                System.Threading.Monitor.Exit(this.DebugListCriticalSection);
            }
        }

        private void CleanDebugerInfoForLoopChange()
        {
            if (debugMgrInfo == null) return;
            debugMgrInfo.CleanDebuggerInfo4LoopChange();
        }
        private void CleanDebugerInfoForTSChange()
        {
            if (debugMgrInfo == null) return;
            debugMgrInfo.CleanDebugerInfo4TSChange();
        }

        public ERROR_CODE EndNavigate(object iLoop)
        {
            Logger.logBegin("EndNavigate");
            if (this.mobjClient2Server == null)
            {
                Logger.Error("StartTestStepNavigate", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
            }
            this.mobjClient2Server.EndTestStepNavigate();
            miCurrentLoopId = -1;
            Logger.logEnd("EndNavigate");
            return ERROR_CODE._NO_ERROR;
        }



        public bool TestStepExists()
        {
            Logger.logBegin("TestStepExists");
            try
            {
                bool bContinue = true;
                do
                {


                    mobjCurrentTestStep = this.mobjClient2Server.GetNextTestStep();
                    //mobjCurrentTestStep = this.TestStepsMgr.CurrentTestSteps;
                    //this.mobjClient2Server.GetNextTestStep(this.TestStepsMgr.CurrentStepId);
                    this.TestStepsMgr.CurrentStepId++;
                    //Logger.Info("=====", string.Format("objectValue:[{0}]", mobjCurrentTestStep.Value));

                    if (mobjCurrentTestStep == null) return false;
                    //if (miCurrentLoopId == 0) return true; // the first loop , run all steps
                    if (miCurrentLoopId == 0) break;
#if _Datafrom_Database
                    /// 在数据库模式下 无须使用loop来控制
                    bContinue = false;
#else
                    bContinue = mobjCurrentTestStep.Loop <= 0;
#endif
                } while (bContinue);

                /** check break points **/
                if (this.mobjCurrentTestStep != null)
                {
                    //进入临界区
                    System.Threading.Monitor.Enter(DebugListCriticalSection);
                    try
                    {
                        bool isExitLoop = false;
                        for (int i = 0; i < debugMgrInfo.getBreakPointList().Count; i++)
                        {
                            isExitLoop = false;
                            if (debugMgrInfo.getBreakPointList()[i].TestCurrentLoopId != this.miCurrentLoopId) continue;
                            if (debugMgrInfo.getBreakPointList()[i].CurrentFromId != this.mobjCurrentTestStep.RunID) continue;

                            switch ((SystemDebuggerMode)debugMgrInfo.getBreakPointList()[i].TestDebugMode)
                            {
                                case SystemDebuggerMode.SDM_BREAKAT:
                                case SystemDebuggerMode.SDM_BREAKAT | SystemDebuggerMode.SDM_REUSME:
                                    Logger.Info("TestStepExists", string.Format("Come across a break point with information:[{0}], \r\n\tID{1}\r\n\tTSRelyID:{2}",
                                        this.mobjCurrentTestStep.ToString(), debugMgrInfo.getBreakPointList()[i].CurrentFromId, debugMgrInfo.getBreakPointList()[i].CurrentRelyId));

                                    /** Break point **/
                                    beforeRunTestStepImpl(null);
                                    TestFrameUtility.ChangeBreakPointNow(true);
                                    /** call Monitor to start Hint Message **/
                                    /** missing                            **/
                                    isExitLoop = true;
                                    break;
                                case SystemDebuggerMode.SDM_SKIP:
                                case SystemDebuggerMode.SDM_SKIP | SystemDebuggerMode.SDM_REUSME:
                                    Logger.Info("TestStepExists", string.Format("Come across a skip step with information:[{0}], \r\n\tID{1}\r\n\tTSRelyID:{2}",
                                        this.mobjCurrentTestStep.ToString(), debugMgrInfo.getBreakPointList()[i].CurrentFromId, debugMgrInfo.getBreakPointList()[i].CurrentRelyId));
                                    Logger.Info("TestStepExists", "try to find next available step...");
                                    return TestStepExists();

                            }
                            if (isExitLoop) break;

                            //if ((SystemDebuggerMode)debugMgrInfo.getBreakPointList()[i].TestDebugMode != SystemDebuggerMode.SDM_BREAKAT)
                            //{
                            //    continue;
                            //}
                            //Logger.Info("TestStepExists", string.Format("Come across a break point with information:[{0}], \r\n\tID{1}\r\n\tTSRelyID:{2}", 
                            //    this.mobjCurrentTestStep.ToString(), debugMgrInfo.getBreakPointList()[i].CurrentFromId,debugMgrInfo.getBreakPointList()[i].CurrentRelyId));
                            //if (debugMgrInfo.getBreakPointList()[i].TestCurrentLoopId != this.miCurrentLoopId) continue;
                            //if (debugMgrInfo.getBreakPointList()[i].CurrentFromId != this.mobjCurrentTestStep.RunID) continue;
                            ///** Break point **/
                            //beforeRunTestStepImpl(null);
                            //TestFrameUtility.ChangeBreakPointNow(true);
                        }
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(DebugListCriticalSection);
                    }
                }

                //Logger.Info("=====", string.Format("2 objectValue:[{0}]", mobjCurrentTestStep.Value));

                return this.mobjCurrentTestStep != null;
            }
            finally
            {
                Logger.logEnd("TestStepExists");
            }
        }

        public TestStep4Services GetCurrentTestStep()
        {
            Logger.Info("GetCurrentTestStep", string.Format("Current objects' quick:[{0}]\r\nObjectName:[{1}]\r\nValue:[{2}]", this.mobjCurrentTestStep.QuickAccess, this.mobjCurrentTestStep.ObjectName ?? "", this.mobjCurrentTestStep.Value));

            return this.mobjCurrentTestStep;
        }

        public int GetCurrentStepRunType()
        {
            int iResult = this.mobjClient2Server.GetCurrentStepRunType(this.mobjCurrentTestStep.Keyword);
            return iResult;
        }

        private string[] KeywordsNotRequireData = null;
        private void LoadKeywordsNotRequireData()
        {
            if (KeywordsNotRequireData == null)
            {
                KeywordsNotRequireData = AppConfigReader.GetKeywordsNotRequireData();
            }
        }
        private bool isKeywordsRequireDataFromServer(string strKeyword)
        {
            LoadKeywordsNotRequireData();
            foreach (string strKey in KeywordsNotRequireData)
            {
                if (strKey == null) continue;
                if (string.Compare(strKeyword, strKey, true) == 0) return false;
            }
            return true;
        }
        /**
         * Enhancement of Keywords like:
         * VerifyValue, 
         * EqualTo:
         * [StorageMode:Comparing;ColIndx:2;ConvertMethod:NONE];OBJECTHAPPYNAME
        */
        public string GetCurrentTestStepData(out object isOK)
        {
            Logger.logBegin("GetCurrentTestStepData AF");
            if (mobjClient2Server == null)
            {
                Logger.Error("GetCurrentTestStepData", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                isOK = new ERROR_CODE();
                isOK = e_CurrentErrorCode;
                return null;
            }

            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            int iError = 1;
            string strResult = "", strError = "";
            string strReferenceObjectName = "";
            bool isReferenceMode = false;

            //Logger.Info("=====",string.Format(" a objectValue:[{0}]", mobjCurrentTestStep.Value));

            if (string.IsNullOrEmpty(this.mobjCurrentTestStep.ObjectName))
            {
#if _Datafrom_Database
                /// Try to get data by stepid, and loop
                /// check whether the keywword need data from server
                /// 
                if (!isKeywordsRequireDataFromServer(mobjCurrentTestStep.Keyword))
                {
                    Logger.Info("GetCurrentTestStepData", string.Format("Not neccessary to get data for keyword:[{0}] ", mobjCurrentTestStep.Keyword));
                    isOK = true;
                    return strResult = "";
                }
                Logger.Info("INFO", string.Format("Before get data from from server, objectName is NULL or Empty,Keyword:[{0}], LoopId:[{1}],stepId:[{2}]",
                    mobjCurrentTestStep.Keyword, mobjCurrentTestStep.Loop, mobjCurrentTestStep.AssignedTestStepId));
                iError = this.mobjClient2Server.FetchDataByStepIdAndLoopId(this.mobjCurrentTestStep.Loop, this.mobjCurrentTestStep.AssignedTestStepId, ref strError, ref strResult);
                if (iError != (int)ERROR_CODE._NO_ERROR)
                {
                    Logger.Error("GetCurrentTestStepData", string.Format("Error from server side:[{0}], no Data Returned", strError));
                    isOK = false;
                    return "";
                }

                Logger.Info("GetCurrentTestStepData", string.Format("Get data from Server:[{0}] for stepId:[{1}]", strResult, this.mobjCurrentTestStep.AssignedTestStepId));
                isOK = true;
                return strResult;
#else
                Logger.Info("INFO", "No Object name is setting, just return ");
                isOK = true;
                return "";
#endif
            }

            Logger.Info("INFO", string.Format("Before get data from from server, objectName isnot NULL or Empty,Keyword:[{0}], object:[{3}],LoopId:[{1}],stepId:[{2}]",
                    mobjCurrentTestStep.Keyword, mobjCurrentTestStep.Loop, mobjCurrentTestStep.AssignedTestStepId, mobjCurrentTestStep.ObjectName));
            /*** ***/

            isReferenceMode = CheckCurrentStepDataReferenceMode(ref strReferenceObjectName);
#if _Datafrom_Database
#if v_16AndUp
            string strVarName = "";
            int ivarType = !isReferenceMode ? CheckCurrentStepDataVarMode(this.mobjCurrentTestStep.Value, ref strVarName) : 0;

            //Logger.Info("=====", string.Format("b objectValue:[{0}]", mobjCurrentTestStep.Value));

            switch (ivarType)
            {
                case 0:
                case 1:
                case 2:
                    strResult = this.mobjClient2Server.GetDataStringFromDataFile(isReferenceMode ? GetReferenceObjectNameFromValue() : this.mobjCurrentTestStep.ObjectName, this.miCurrentLoopId, ref iError, (int)this.mobjCurrentTestStep.AssignedTestStepId);

                    //Logger.Info("=====", string.Format("c objectValue:[{1}] return :[{0}]", strResult, mobjCurrentTestStep.Value));
                    break;
                    //case 1:
                    //case 2:
                    //    strResult = this.mobjCurrentTestStep.Value;
                    //    break;
            }

#else
            strResult = this.mobjClient2Server.GetDataStringFromDataFile(isReferenceMode ? GetReferenceObjectNameFromValue() : this.mobjCurrentTestStep.ObjectName, this.miCurrentLoopId, ref iError,(int)this.mobjCurrentTestStep.AssignedTestStepId);                     
#endif

#else
            strResult = this.mobjClient2Server.GetDataStringFromDataFile(isReferenceMode ? GetReferenceObjectNameFromValue() : this.mobjCurrentTestStep.ObjectName, this.miCurrentLoopId, ref iError);
#endif
            /** should update cell **/
            if (this.mobjClient2Monitor != null)
            {
                this.mobjClient2Monitor.BeginAddLogHint();
                this.mobjClient2Monitor.OnClientWriteCurrentLog(string.Format("Data:[{0}]", strResult), 0);
                this.mobjClient2Monitor.EndAddLogHing();
            }

            eCde = (ERROR_CODE)iError;

            Logger.logEnd("GetCurrentTestStepData");
            isOK = eCde == ERROR_CODE._NO_ERROR;
            return strResult;
        }
#if v_16AndUp

        private int CheckCurrentStepDataVarMode(string strSrc, ref string strVarName)
        {
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL, strSrc))
            {
                Logger.Info("CheckCurrentStepDataReferenceMode", "CNST_RESERVED_VARIABLE_GLOBAL");
                return 1;
            }
            if (TigerMarsUtil.RegularTest("^" + SystemConstant.CNST_RESERVED_VARIABLE_MODAL, strSrc))
            {
                Logger.Info("CheckCurrentStepDataReferenceMode", "CNST_RESERVED_VARIABLE_MODAL");
                return 2;
            }
            return 0;
        }
#endif

        private bool CheckCurrentStepDataReferenceMode(ref string strReferenceObjectName)
        {
            Logger.Info("CheckCurrentStepDataReferenceMode", string.Format("Begin, this.mobjCurrentTestStep.Value:[{0}]", this.mobjCurrentTestStep.Value));

            string strPart = string.Format("^{0}.*", SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX);
            bool isReference = TigerMarsUtil.RegularTest(strPart, this.mobjCurrentTestStep.Value);
            Logger.logEnd("CheckCurrentStepDataReferenceMode");
            return isReference;
        }

        private string GetReferenceObjectNameFromValue()
        {
            Logger.logBegin("GetReferenceObjectNameFromValue");
            if (this.mobjCurrentTestStep == null)
            {
                return "";
            }
            string strNewObj = this.mobjCurrentTestStep.Value == null ? "" : this.mobjCurrentTestStep.Value.Replace(SystemConstant.CNST_ENHANCE_VALUE_EQUALTO_PREFIX, "");
            Logger.logEnd("GetReferenceObjectNameFromValue");
            return strNewObj;
        }



        public string GetCurrentRunnableScript()
        {
            Logger.logBegin("GetCurrentRunnableScript");
            try
            {
                if (this.mobjCurrentTestStep == null) return "";

                return "";
            }
            finally
            {
                Logger.logEnd("GetCurrentRunnableScript");
            }

        }

        public int GetRunnableLoopCount()
        {
            Logger.logBegin("GetRunnableLoopCount");
            if (mobjClient2Server == null)
            {
                Logger.Error("GetRunnableLoopCount", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                return -1;
            }
            //Logger.logEnd("GetRunnableLoopCount");
            int iLoopCnt = mobjClient2Server.GetTestLoopCount();
            if (this.mobjClient2Monitor != null)
            {
                try
                {
                    /** Write Loop Info to Monitor **/
                    this.mobjClient2Monitor.OnClientWriteCurrentLog(string.Format("Get test case Loop:[{0}]--0 means Error.", iLoopCnt), iLoopCnt == 0 ? -1 : 1);
                    /** tell the monitor that a loop count is change **/
                    this.mobjClient2Monitor.OnLoopCountChangeEvent(iLoopCnt);
                }
                catch (Exception e)
                {
                    Logger.Error("GetRunnableLoopCount", string.Format("Exception:[{0}]", e.Message), e);
                }

            }
            Logger.Info("GetRunnableLoopCount", string.Format("Loop Count:[{0}]", iLoopCnt));

            return iLoopCnt;
        }

        public string GetCurrentTestCaseName()
        {
            Logger.logBegin("GetCurrentTestCaseName");
            if (mobjClient2Server == null)
            {
                Logger.Error("GetCurrentTestCaseName", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                return "";
            }
            string strCurrentCaseName = mobjClient2Server.GetCurrentTestCaseName();
            Logger.logEnd("GetCurrentTestCaseName");
            return strCurrentCaseName;
        }

        public string GetCurrentTestSuiteName()
        {
            Logger.logBegin("GetCurrentTestSuiteName");
            if (mobjClient2Server == null)
            {
                Logger.Error("GetCurrentTestSuiteName", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                return "";
            }
            string strCurrentCaseName = mobjClient2Server.GetCurrentTestSuiteName();
            Logger.logEnd("GetCurrentTestSuiteName");
            return strCurrentCaseName;
        }

        #endregion //Methods for QTP

        private bool? isAutoCheckError = false;

        private string GetAccessURL(ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetAccessURL");
            /// for multiple user using, information should be get from marstestframe.dll.config
            /// string strResult = AppConfigReader.GetConfigServerURLInfo();
            /// 
            WCFServiceNode objNodeInfo = WCFXmlCfgMgr.GetCurrerntWcfNodeInfo(ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error("GetAccessURL", strError);
                return null;
            }
            string strProtocol = "", strHost = "", strServiceName = "";
            isOk = AppConfigReader.GetConfigServerURLInfo(ref strProtocol, ref strHost, ref strServiceName, ref strError);
            string strResult = string.Format("{0}://{1}:{2}/{3}", strProtocol, strHost, objNodeInfo.ServerPort, strServiceName);
            Logger.logEnd("GetAccessURL");
            return strResult;
        }

        private string GetAccessMonitorURL(ref bool isOk, ref string strError)
        {
            /// for multiple users
            /// 

            string strProtocol = "", strHost = "", strServiceName = "";
            string strResult = "";
            if (currentMarsMonitorPort <= 0)
            {
                isOk = AppConfigReader.GetConfigServerMonitorURLInfo(ref strProtocol, ref strHost, ref strServiceName, ref strError);
                if (!isOk) return null;
                WCFServiceNode portInfo = WCFXmlCfgMgr.GetCurrerntWcfNodeInfo(ref isOk, ref strError);
                if (!isOk) return null;
                /// get port
                /// 


                //string strResult = AppConfigReader.GetConfigServerMonitorURLInfo();
                //if (strResult == null)
                //{
                //    isOk = false;
                //    Logger.Error("GetAccessMonitorURL", strError = "No monitor Service setting Or settings of Monitor service are wrong.");
                //}
                if (string.IsNullOrEmpty(strProtocol) || string.IsNullOrEmpty(strHost) || portInfo.MonitorPort < 1000 || string.IsNullOrEmpty(strServiceName))
                {
                    isOk = false;
                    Logger.Error("GetAccessMonitorURL", strError = string.Format("Protocol, HostName, serviceName should not be null or empty, and port number should greater 1000, but they are:[{0}]", strResult));
                    return null;
                }
                strResult = string.Format("{0}://{1}:{2}/{3}", strProtocol, strHost, portInfo.MonitorPort + "", strServiceName);
            }
            else
            {
                strServiceName = "MARSTIGFrameMonitor";
                strHost = "localhost";
                strProtocol = "net.tcp";
                strResult = string.Format("{0}://{1}:{2}/{3}", strProtocol, strHost, currentMarsMonitorPort + "", strServiceName);
            }

            isOk = true;
            return strResult;
        }

        public bool GetBatchMode()
        {
            if (mobjClient2Server == null)
            {
                Logger.Error("GetBatchMode", " mobjClient2Server == null");
                return false;
            }
            else
            {
                try
                {
                    bool rc = mobjClient2Server.GetBatchMode();
                    Logger.Info("GetBatchMode", " rc = " + rc);
                    return rc;
                }
                catch (Exception e)
                {
                    Logger.Error("GetBatchMode", string.Format("Exception:[{0}]", e.Message), e);
                    return false;
                }
            }
        }
        public void SetBatchMode()
        {
            mobjClient2Monitor.SetBatchMode();
        }
        public bool ShutdownServer()
        {
            bool rc = true;
            if (mobjClient2Server == null)
            {
                Logger.Error("ShutdownServer", " mobjClient2Server == null");
                rc = false;
            }
            else
                mobjClient2Server.ShutdownServer();
            if (mobjClient2Monitor == null)
            {
                Logger.Error("ShutdownServer", " mobjClient2Monitor == null");
                rc = false;
            }
            else
                mobjClient2Monitor.ShutdownServer();
            return rc;
        }
        public bool IsSkipStepWord(string word)
        {

            if (mobjClient2Server == null)
            {
                Logger.Error("IsSkipStepWord", " mobjClient2Server == null");
                return false;
            }
            return mobjClient2Server.IsSkipStepWord(word);
        }
#if v_16AndUp
        public bool hasResumInfoFromCurrent()
        {
            Logger.logBegin("hasResumInfoFromCurrent");
            if (mobjClient2Server == null)
            {
                Logger.Error("hasResumInfoFromCurrent", " mobjClient2Server == null");
                return false;
            }
            return mobjClient2Server.hasResumInfoFromCurrent();
        }

        public bool isLoopVarApplied()
        {
            Logger.logBegin("isLoopVarApplied");
            if (mobjClient2Server == null)
            {
                Logger.Error("isLoopVarApplied", " mobjClient2Server == null");
                return false;
            }
            return mobjClient2Server.IsLoopVarApplied();
        }
        public bool jumpToNextResumeBlock()
        {
            Logger.logBegin("jumpToNextResumeBlock");
            if (mobjClient2Server == null)
            {
                Logger.Error("hasResumInfoFromCurrent", " mobjClient2Server == null");
                return false;
            }
            return mobjClient2Server.jumpToNextResumeBlock();
        }
        private string lastResumeNextError = null;
        public void StoreErrorForResume(string strLastResumeError)
        {
            lastResumeNextError = strLastResumeError;
        }
        public bool GetLatestResumeError(ref string strErrorInfo)
        {
            bool isNull = false;
            strErrorInfo = (isNull = string.IsNullOrEmpty(lastResumeNextError)) ? "" : lastResumeNextError;
            return isNull;
        }

        private bool GetAndSetStoryboardInfo(ref string strError)
        {
            if (mobjClient2Server == null)
            {
                Logger.Error("GetAndSetStoryboardInfo", strError = " mobjClient2Server == null");
                return false;
            }
            if (mobjClient2Monitor == null)
            {
                Logger.Error("GetAndSetStoryboardInfo", strError = "mobjClient2Monitor==null");
                return false;
            }

            //string strStoryBoardName;
            return true;
        }
#endif
        public void SkipCurrentStep()
        {
            Logger.Info("SkipCurrentStep", "to all SkipCurrentStep--tiger test");
            if (mobjClient2Monitor == null)
            {
                Logger.Error("SkipCurrentStep", " mobjClient2Monitor == null");
            }
            else
                mobjClient2Monitor.SkipCurrentStep();
        }
        public bool IsVariable(string word)
        {
            bool rc = true;
            if (mobjClient2Server == null)
            {
                Logger.Error("IsVariable", " mobjClient2Server == null");
                rc = false;
            }
            else
                rc = mobjClient2Server.IsVariable(word);
            return rc;
        }
        public string GetVariableValue(string variable)
        {
            string value = "";
            if (mobjClient2Server == null)
            {
                Logger.Error("GetVariableValue", " mobjClient2Server == null");
            }
            else
            {
                value = mobjClient2Server.GetVariableValue(variable);
                Logger.Info("GetVariableValue", " value:" + value);
            }
            return value;
        }

        public bool isWriteDataBackKeyWord(object strCurrentKeyWord)
        {
            Logger.logBegin("isWriteDataBackKeyWord");
            if (strCurrentKeyWord == null) return false;

            foreach (string strFuncName in SystemConstant.CNST_ARR_FEEDBACKFUNCTIONS)
            {
                if (string.Compare(strFuncName, strCurrentKeyWord.ToString(), true) == 0)
                {
                    Logger.Info("INFO", "FIND FEED BACK DATA KEYWORD:" + strFuncName);
                    return true;
                }
            }

            Logger.logEnd("isWriteDataBackKeyWord");
            return false;
        }

        public int CompareGuiDataByLoopId(object objLoopId)
        {
            Logger.Info("CompareGuiDataByLoopId", string.Format("Parameters:[{0}]",
                TigerMarsUtil.GetParameter("objLoopId", objLoopId == null ? "" : objLoopId.ToString())));

            if (objLoopId == null)
            {
                e_CurrentErrorCode = ERROR_CODE._CLIENT_ERROR_PARAMETERISNULL_PARA_1;
                Logger.Error("CompareGuiDataByLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode), "objLoopId"));
                return (int)e_CurrentErrorCode;
            }
            int iLoopId = -1;
            try
            {
                iLoopId = int.Parse(objLoopId.ToString());
            }
            catch (Exception e)
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_LOOP_ISNOT_A_NUMBER_PARA_1; ;
                Logger.Error("CompareGUIDAtaByLoopId", string.Format(ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode), objLoopId.ToString()), e);
                return (int)e_CurrentErrorCode;
            }
            e_CurrentErrorCode = ERROR_CODE._NO_ERROR;
            if (mobjClient2Server == null)
            {
                Logger.Error("CompareGuiDataByLoopId", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                return (int)e_CurrentErrorCode;
            }

            e_CurrentErrorCode = (ERROR_CODE)mobjClient2Server.CompareGuiDataByLoopId(objLoopId.ToString());
            if (e_CurrentErrorCode != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("CompareGuiDataByLoopId", string.Format("Can't get the comparison result. LoopId:[{0}],ErrorCode:[{1:x}]", objLoopId.ToString(), e_CurrentErrorCode));
            }
            return (int)e_CurrentErrorCode;
        }

        /**
         * Parameters:
         * objKeywordName: to check whether the function is nessaary to store data
         * objObjectName : which row to write
         * objData2Store : Data to store to Excel file
         * iLoopId       : which column to write
         */
        /* 看起来该函数没用 */
        /*
        public int WriteDataBack(object objKeywordName, object objObjectName, object strData_RC, object objData2Store, object iLoopId, object objBaseLineMode = null)
        {
            Logger.logBegin("WriteDataBack");
            Logger.Info("WriteDataBack", string.Format("\tParameters:{0}\r\n\t{1}\r\n\t{2}\r\n\t{3}\r\n\t{4}\r\n\t{5}",
                TigerMarsUtil.GetParameter("objKeywordName", objKeywordName == null ? "" : objKeywordName.ToString()),
                TigerMarsUtil.GetParameter("objObjectName", objObjectName == null ? "" : objObjectName.ToString()),
                TigerMarsUtil.GetParameter("objData2Store", objData2Store == null ? "" : objData2Store.ToString()),
                TigerMarsUtil.GetParameter("iLoopId", iLoopId == null ? "" : iLoopId.ToString()),
                TigerMarsUtil.GetParameter("objBaseLineMode", objBaseLineMode == null ? "" : objBaseLineMode.ToString()),
                TigerMarsUtil.GetParameter("objValue", strData_RC == null ? "" : strData_RC.ToString())));

            this.dataFromClientToWriteBack = objData2Store == null ? "" : objData2Store.ToString();
            string strBaseLineMode = objBaseLineMode == null ? null : objBaseLineMode.ToString();

            e_CurrentErrorCode = ERROR_CODE._NO_ERROR;
            if (mobjClient2Server == null)
            {
                Logger.Error("WriteDataBack", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST;
                return (int)e_CurrentErrorCode;
            }

            // /** Check parameters 
            if (string.IsNullOrEmpty(objKeywordName == null ? null : objKeywordName.ToString()))
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_KEYWORDNAME_ISNULL_PARA_0;
                Logger.Error("WriteDataBack", ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode));
                return (int)e_CurrentErrorCode;
            }
            if (string.IsNullOrEmpty(objObjectName == null ? null : objObjectName.ToString()))
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_OBJECTNAME_ISNULL_PARA_0;
                Logger.Error("WriteDataBack", ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode));
                return (int)e_CurrentErrorCode;
            }
            if (objData2Store == null)
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_DATA_ISNULL_PARA_0;
                Logger.Error("WriteDataBack", ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode));
                return (int)e_CurrentErrorCode;
            }
            if (string.IsNullOrEmpty(iLoopId == null ? null : iLoopId.ToString()))
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_LOOP_ISNULL_PARA_0;
                Logger.Error("WriteDataBack", ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode));
                return (int)e_CurrentErrorCode;
            }
            int iLoop = -1;
            try
            {
                iLoop = int.Parse(iLoopId.ToString());
            }
            catch (Exception)
            {
                e_CurrentErrorCode = ERROR_CODE._SERVICE_ERROR_CLIENT_LOOP_ISNOT_A_NUMBER_PARA_1;
                Logger.Error("WriteDataBack", string.Format(ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode), iLoopId.ToString()));
                return (int)e_CurrentErrorCode;
            }
            
            // for Compare Mode, it is more complicated 
#if _Datafrom_Database
            bool isComparison = false;
#else
            bool isComparison = (GetKeyComparisonType(this.mobjCurrentTestStep.Keyword) == 1);
#endif
            bool isRegularStorageMode = true;
            string strTargetObject = strData_RC == null ? objObjectName.ToString() : (string.IsNullOrEmpty(strData_RC.ToString().Trim()) ? objObjectName.ToString() : strData_RC.ToString());
            if (isComparison)
            {
                Logger.Info("WriteDataBack", "come to Comparison Mode");
                try
                {
                    if (!((string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD.ToUpper(), objBaseLineMode.ToString().ToUpper(), true) == 0)
                            || (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE.ToUpper(), objBaseLineMode.ToString().ToUpper(), true) == 0)))
                    {
                        e_CurrentErrorCode = ERROR_CODE._TEST_STEP_COMPARISON_MODE_BASELINE_PARAMETER_NOTSUPPORT_PARA_1;
                        Logger.Error("WriteDataBack", string.Format(ERROR_INFO.GET_ERROR_STR(e_CurrentErrorCode), objBaseLineMode.ToString()));
                        return (int)e_CurrentErrorCode;
                    }
                    string strValue = string.Format("[StorageMode:Comparing;ColIndx:{0};ConvertMethod:NONE];{1}", GetColIndex4BaseLineCapture(objBaseLineMode == null ? null : objBaseLineMode.ToString()), strTargetObject);// /*this.mobjCurrentTestStep.ObjectName
                    Logger.Info("WriteDataBack", string.Format("Build comparison Info:[{0}]", strValue));

                    e_CurrentErrorCode = (ERROR_CODE)mobjClient2Server.StoreDataBackComparisonMode(this.GetCurrentTestCaseName(), strValue, objData2Store.ToString(), iLoop, objBaseLineMode == null ? null : objBaseLineMode.ToString(), isComparison);
                }
                catch (Exception e)
                {
                    Logger.Error("WriteDataBack", string.Format("call mobjClient2Server.StoreDataBackComparisonMode return Exceptions:[{0}]", e.Message), e);
                }

            }
            else
            {
                isRegularStorageMode = CheckRegularStorageMode(this.mobjCurrentTestStep.Value);
                if (isRegularStorageMode)
                {
                    e_CurrentErrorCode = (ERROR_CODE)mobjClient2Server.StoreDataBackComparisonMode(this.GetCurrentTestCaseName(), this.mobjCurrentTestStep.Value, objData2Store.ToString(), iLoop, objBaseLineMode == null ? null : objBaseLineMode.ToString(), isComparison);
                }
                else
                {
                    e_CurrentErrorCode = (ERROR_CODE)mobjClient2Server.StoreDataBack(strTargetObject, objData2Store.ToString(), iLoop);
                }
            }

            Logger.logEnd("WriteDataBack");
            return (int)ERROR_CODE._NO_ERROR;
        }
        */
        private int GetColIndex4BaseLineCapture(string strBaseLineCaptureMode)
        {
            if (string.IsNullOrEmpty(strBaseLineCaptureMode)) return 0;
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_COMPARE.ToUpper(), strBaseLineCaptureMode.ToUpper(), true) == 0) return 1;
            return 0;
        }

        private int GetKeyComparisonType(string strKeyWord)
        {
            if (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE, strKeyWord ?? "", true) == 0) return 1;
            return -1;
        }

        private bool CheckRegularStorageMode(string strValueToCheck)
        {
            Logger.logBegin("CheckRegualStorageMode");
            try
            {
                return TigerMarsUtil.RegularTest(SystemConstant.CNST_ENHANCE_STORAGEMODE_COMPARISON_REGULAR, strValueToCheck);
            }
            finally
            {
                Logger.logEnd("CheckRegularStorageMode");
            }
        }

        public object GetButtonMode(object objRCInfo, ref object ValueToReturn)
        {
            const string cnst_mode_1 = @"ImgButton:Until:Text=";
            const string cnst_mode_2 = @"ImgButton:Until:Text Not=";

            Logger.logBegin("GetButtonMode");
            Logger.Info("GetButtonMode", string.Format("Parameters : {0}", TigerMarsUtil.GetParameter("objRCInfo", objRCInfo == null ? "" : objRCInfo.ToString())));
            if (objRCInfo == null)
            {
                return "0";
            }

            string strObjRC = objRCInfo.ToString();
            if (TigerMarsUtil.RegularTest(string.Format("{0}.*", cnst_mode_1), strObjRC))
            {
                ValueToReturn = strObjRC.Replace(cnst_mode_1, "");
                return "1";
            }

            if (TigerMarsUtil.RegularTest(string.Format("{0}.*", cnst_mode_2), strObjRC))
            {
                ValueToReturn = strObjRC.Replace(cnst_mode_2, "");
                return "2";
            }

            Logger.logEnd("GetButtonMode");
            return "0";
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessageTimeout(
            HandleRef hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            int flags,
            int timeout,
            out IntPtr pdwResult);

        const int SMTO_ABORTIFHUNG = 2;
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();


        public bool FindApplicationByName(string strAppName, object tryTimes, out object errorInfo)
        {
            /**
             * steps:
             * 1, check try Times is a int
             * 2, loop to find applications name
             * */
            Logger.Info("FindApplicationByName", string.Format("Parameters: AppName:[{0}], tryTimes:[{1}]", strAppName, tryTimes.ToString()));
            try
            {
                int iTimes = int.Parse(tryTimes.ToString());
                for (int i = 0; i < iTimes; i++)
                {
                    /** sleep 30  when no such application is available 
                     *  5秒检查1次
                     * **/
                    //Process[] arrP = Process.GetProcessesByName(strAppName);
                    for (int zz = 0; zz < 12; zz++)
                    {
                        Process[] arrP = Process.GetProcessesByName(strAppName);
                        if ((arrP == null) || (arrP.Length <= 0))
                        {
                            Thread.Sleep(5000);
                            continue;
                        }
                        else
                        {
                            errorInfo = string.Format("find application with ID: [{0}]", arrP[0].Id);
                            return true;
                        }
                    }

                }
                Logger.Error("FindApplicationByName", (string)(errorInfo = string.Format("Tried [{0}] times, each time takes 1 minutes, but no such application [{1}] exists in memory", iTimes, strAppName)));
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("FindApplicationByName", (string)(errorInfo = string.Format("Exceptions [{0}]", e.Message)), e);
                return false;
            }
        }

        public bool ExtraMouseAction(string extraAction)
        {
            if (string.IsNullOrEmpty(extraAction)) return true;
            if (!extraAction.ToUpper().StartsWith("MOVE:"))
            {
                return false;
            }

            string strMove = extraAction.Substring("MOVE:".Length);
            string[] arrPos = strMove.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (arrPos.Length < 2) return false;
            int x = 10, y = 10;
            int.TryParse(arrPos[0], out x);
            int.TryParse(arrPos[1], out y);
            for (int i = 0; i < 10; i++)
            {
                Cursor.Position = new System.Drawing.Point(x + (9 - i) * 5, y + (9 - i) * 5);
                Thread.Sleep(10);
            }
            return true;
        }

        internal static int WTS_UserName = 5;

        public bool IsResponding(string strPrcessName, object objTime = null)
        {
            /** for tsting  **/
            //strPrcessName = "SummitFT.exe";
            Process[] arrP = Process.GetProcessesByName(strPrcessName);
            Process p = null;
            string strAccount = Environment.UserName, strPAccountName = "";
            Logger.Info("IsResponding", string.Format("Current Account:[{0}]", strAccount));
            long n = DateTime.Now.Ticks;
            long pt = n;
            int iWaitSeconds = -1;
            try
            {
                iWaitSeconds = int.Parse(objTime == null ? "-1" : objTime.ToString());
            }
            catch (Exception)
            {
                iWaitSeconds = -1;
            }
            iWaitSeconds = iWaitSeconds < 0 ? 60 : iWaitSeconds;

            while (TimeSpan.FromTicks(pt - n).TotalSeconds < iWaitSeconds)
            {
                p = arrP == null ? null : (arrP.Length <= 0 ? null : arrP[0]);
                IntPtr AnswerBytes;
                IntPtr AnswerCount;

                for (int i = 0; i < arrP.Length; i++)
                {
                    if (WTSQuerySessionInformationW(IntPtr.Zero,
                                                   arrP[i].SessionId,
                                                   WTS_UserName,
                                                   out AnswerBytes,
                                                   out AnswerCount))
                    {
                        strPAccountName = Marshal.PtrToStringUni(AnswerBytes);
                    }
                    else
                    {
                        Logger.Info("IsResponding", ("WTSQuerySessionInformationW is wrong, empty string is used"));
                        strPAccountName = "";
                    }

                    Logger.Info("IsResponding", string.Format("process Account:[{0}]", strPAccountName));
                    if (string.Compare(strPAccountName, strAccount, true) == 0)
                    {
                        Logger.Info("IsResponding", string.Format("Find application for account:[{0}]", strAccount));
                        p = arrP[i];
                        break;
                    }
                }


                if (p == null)
                {
                    if (iWaitSeconds <= 0)
                        Thread.Sleep(1000);
                    else
                        Thread.Sleep(iWaitSeconds * 1000);
                    arrP = Process.GetProcessesByName(strPrcessName);

                    pt = DateTime.Now.Ticks;
                    continue;
                }
                pt = DateTime.Now.Ticks;
                break;
            }

            //一共2分钟
            n = DateTime.Now.Ticks;
            pt = n;
            while (TimeSpan.FromTicks(pt - n).TotalSeconds < iWaitSeconds)
            {
                bool isOk = IsResponding(p, 1);
                if (isOk) return true;
                Thread.Sleep(1000);
                pt = DateTime.Now.Ticks;
            }
            Logger.Warnning("IsResponding", string.Format("Waited about 2 minutes but process [{0}] is stil busy", strPrcessName));
            return true;
        }

        bool IsResponding(Process process, int iWaitSeconds = -1)
        {
            HandleRef handleRef = new HandleRef(process, process.MainWindowHandle);
            int timeout = iWaitSeconds < 0 ? 2000 * 60 : iWaitSeconds * 1000; //three minutes for default
            IntPtr lpdwResult;

            IntPtr lResult = SendMessageTimeout(
                handleRef,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                SMTO_ABORTIFHUNG,
                timeout,
                out lpdwResult);
            uint iLastError = GetLastError();
            Logger.Info("IsResponding", string.Format("SendMessageTimeout returns:[{0}], result:[{1}], lasterror Code:[{2}]", lResult, lpdwResult, iLastError));
            if (iLastError == 1400)
            {
                Logger.Info("IsResponding", "return invalidate handle, just return true");
                return true;
            }
            return lResult != IntPtr.Zero;
        }



        #region property change notification
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        #endregion //property change notification

        #region proxy for monitor service call
        /****************************************************************************         
         * Subscribe part
         *****************************************************************************/
        private bool isSubscribeDone = false;
        public bool IsSubscribeDone
        {
            get { return this.isSubscribeDone; }
            set { if (this.isSubscribeDone != value) { isSubscribeDone = value; OnPropertyChanged("IsSubscribeDone"); } }
        }
        private void SubscribProxy()
        {
            if (this.mobjClient2Monitor == null) return;
            isSubscribeDone = false;
            this.mobjClient2Monitor.Subscribe();
            int iCount = 0;
            while ((!IsSubscribeDone) && (iCount++ < 1000))
            {
                Thread.Sleep(50);
            }
            if (iCount >= 999)
            {
                Logger.Info("SubscribProxy", string.Format("WARNNING, no return from Monitor service after [{0}]*50 milliSeconds", iCount));
            }
        }
        public void SubScribCallBack()
        {
            IsSubscribeDone = true;
        }


        /** **********************************************************************
         * GetCurrentDebugMode
         * ***********************************************************************/
        private bool isDebugModeFromMonitorReady = false;
        public bool IsDebugModeFromMonitorReady
        {
            get { return isDebugModeFromMonitorReady; }
            set
            {
                if (isDebugModeFromMonitorReady != value)
                {
                    isDebugModeFromMonitorReady = value;
                    OnPropertyChanged("IsDebugModeFromMonitorReady");
                }
            }
        }
        private TestFlowDebugInfo objDebugInfoFromCurrentModeMethod = null;
        public void GetCurrentDebugModeCallBack(TestFlowDebugInfo objDebugInfo)
        {
            objDebugInfoFromCurrentModeMethod = objDebugInfo;
            isDebugModeFromMonitorReady = true;
        }
        private void GetCurrentDebugModeProxy()
        {
            if (this.mobjClient2Monitor == null) return;
            isDebugModeFromMonitorReady = false;
            int iCount = 0;
            this.mobjClient2Monitor.GetCurrentDebugMode();
            while ((!IsDebugModeFromMonitorReady) && (iCount++ < 1000))
            {
                Thread.Sleep(50);
            }
            if (iCount >= 999)
            {
                Logger.Info("GetCurrentDebugModeProxy", string.Format("WARNNING, no return from Monitor service after [{0}]*50 milliSeconds", iCount));
            }
        }

        public void OnPreCompile(string strTestSuite, string strTestCase)
        {
            //throw new NotImplementedException();
        }

        public void OnConnected()
        {
            //throw new NotImplementedException();
        }

        public void OnGetData(string strObjectName, string strData, string strError)
        {
            //throw new NotImplementedException();
        }

        public void OnPreCompileTestSteps(List<TestStep4Services> lstSteps)
        {
            //throw new NotImplementedException();
        }

        private static List<string> Keywords_requireAutoChck = null;
        /// <summary>
        /// 判断是否需要进行自动error check
        /// </summary>
        /// <param name="strKeyword">
        /// Keyword Name.Peginwindow is not required to check auto. Other keywords are:
        ///     waitforSeconds, killapplication等不操作当前界面的
        /// </param>
        /// <param name="strObjectAutoID"></param>
        /// <returns></returns>
        public bool IsCheckErrorRequired(string strKeyword, string strObjectAutoID)
        {
            if (string.IsNullOrEmpty(strObjectAutoID)) return false;
            if (string.IsNullOrEmpty(strKeyword)) return false;
            if (this.mobjClient2Server == null) return false;
            try
            {
                if (Keywords_requireAutoChck == null)
                {
                    Keywords_requireAutoChck = mobjClient2Server.GetKeywordsCanAutoCheckError();
                }
                if (Keywords_requireAutoChck == null) return false;

                return Keywords_requireAutoChck.FirstOrDefault(p => string.Compare(p, strKeyword, true) == 0) != null;

            }
            catch (Exception e)
            {
                Logger.Error("IsCheckErrorRequired", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }


        }

        #endregion //proxy for monitor service call

        public void DBRecord_currentTestStoryBoardEnd(int iErrorId, string strError)
        {
#if _Datafrom_Database
            try
            {
                this.mobjClient2Server.DBRecord_currentTestStoryBoardEnd(iErrorId, strError);

            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_currentTestStoryBoardEnd", string.Format("Exception:[{0}]", e.Message), e);

            }

#endif
        }

        public int DBRecord_CreateNewTestMarkID()
        {
#if _Datafrom_Database
            string strError = "";
            try
            {
                return this.mobjClient2Server.DBRecord_CreateNewTestMarkID(ref strError);
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_CreateNewTestMarkID", string.Format("Exceptions:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                if (!string.IsNullOrEmpty(strError))
                {
                    Logger.Error("DBRecord_CreateNewTestMarkID", string.Format("Error from Server:[{0}]", strError));
                }
            }

#else
            return -1 ;
#endif
        }

        public void DBRecord_currentTestStoryBoardStart(string strTestCaseName)
        {
#if _Datafrom_Database
            try
            {
                this.mobjClient2Server.DBRecord_currentTestStoryBoardStart(strTestCaseName);
            }
            catch (Exception e)
            {

                Logger.Error("DBRecord_currentTestStoryBoardStart", string.Format("Exception:[{0}]", e.Message), e);
            }
#endif
        }

        public void DBRecord_currentTestCaseStart(string strCurrentCase, int iLoop)
        {
#if _Datafrom_Database
            Logger.Info("DBRecord_currentTestCaseStart", string.Format("strCurrentCase:[{0}], iLoop:[{1}]", strCurrentCase, iLoop));
            try
            {

                this.mobjClient2Server.DBRecord_currentTestCaseStart(strCurrentCase, iLoop);
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_currentTestCaseStart", string.Format("CurrentCase:[{0}], exception:[{1}]", strCurrentCase, e.Message), e);
            }
#endif
        }

        public void DBRecord_currentTestCaseLoopEnd(int iResultId, int iLoopId, string strEndInfo)
        {
#if _Datafrom_Database
            Logger.Info("DBRecord_currentTestCaseLoopEnd", string.Format("ResultId:[{0}], LoopId:[{1}], EndInfo:[{2}]", iResultId, iLoopId, strEndInfo));
            try
            {
                this.mobjClient2Server.DBRecord_currentTestCaseLoopEnd(iResultId, iLoopId, strEndInfo);
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_currentTestCaseLoopEnd", string.Format("Exception:[{0}]", e.Message), e);
            }
#endif
        }



        public void DBRecord_currentTestStepEnd(int iResultId, int iCurrentLoopId, string strError)
        {
#if _Datafrom_Database
            Logger.Info("DBRecord_currentTestStepEnd", string.Format("iResultId:[{0}], strError:[{1}]", iResultId, strError));
            try
            {
                this.mobjClient2Server.DBRecord_currentTestStepEnd(iResultId, iCurrentLoopId, strError);
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_currentTestStepEnd", string.Format("Exceptions from server:[{0}]", e.Message), e);
            }
#endif
        }

        public void LogCurrentTestStepStart(int iLoop)
        {
#if _Datafrom_Database
            try
            {
                /// Recorders Steps:
                /// 1, check the latest record of step, whether is uploaded to Server, if it were not uploaded, then goto 2, else goto 3
                /// 2, upload the Latest Recorder to Server
                /// 3, Create a new record and initialze members

                /// 1, check the latest record of step, whether is uploaded to Server, if it were not uploaded, then goto 2, else goto 3
                /// 
                TestStepRunningRecorder objCurrentStepLog;

                Logger.Info("=====", string.Format("3 objectValue:[{0}], Loop:[{1}]", mobjCurrentTestStep.Value, iLoop));

                if (this.mobjCurrentTestStep.StepRunningRecorders.Count <= 0)
                {

                    ///3, Create a new record and initialze members
                    /// 
                    objCurrentStepLog = new TestStepRunningRecorder();
                    this.mobjCurrentTestStep.StepRunningRecorders.Add(objCurrentStepLog);
                    objCurrentStepLog.StartTime = DateTime.Now;
                    objCurrentStepLog.LoopId = this.miCurrentLoopId;
                    /// this field is not using now, as server side cached a copy, or server side can query and update database
                    /// but this is not good for performance. 
                    objCurrentStepLog.SaveToServerId = 0;//Initialization                                    
                    objCurrentStepLog.assignedStepId = this.mobjCurrentTestStep.AssignedTestStepId;
                }
                objCurrentStepLog = this.mobjCurrentTestStep.StepRunningRecorders.Last();
                this.mobjClient2Server.CreateStepLogInfo(objCurrentStepLog, this.miCurrentLoopId, mobjCurrentTestStep.AssignedTestStepId);
                Logger.Info("=====", string.Format("4 objectValue:[{0}]", mobjCurrentTestStep.Value));
            }
            catch (Exception e)
            {
                Logger.Error("LogCurrentTestStepStart", string.Format("Exception:[{0}]", e.Message), e);
            }
#endif
        }


        public void DBRecord_UpdateCurrentStepData(int iLoopId, string strData)
        {
#if _Datafrom_Database
            Logger.Info("DBRecord_UpdateCurrentStepData", string.Format("LoopId:[{0}], strData:[{1}]:", iLoopId, strData));
            try
            {
                if (this.mobjCurrentTestStep.StepRunningRecorders.Count > 0)
                {
                    TestStepRunningRecorder lastRecorder = this.mobjCurrentTestStep.StepRunningRecorders[this.mobjCurrentTestStep.StepRunningRecorders.Count - 1];

                    this.mobjClient2Server.DBRecord_UpdateCurrentStepData(iLoopId, strData, lastRecorder);
                }
            }
            catch (Exception e)
            {
                Logger.Error("DBRecord_UpdateCurrentStepData", string.Format("Exception:[{0}]", e.Message), e);
            }
#endif
        }

#if _Datafrom_Database
        public bool GetIgnoreErrorStatus()
        {
            if (this.mobjClient2Server == null)
            {
                Logger.Error("GetIgnoreErrorStatus", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                return false;
            }

            return mobjClient2Server.GetIgnoreErrorStatus();
        }
#endif
        #region upload picture to server
        public bool uploadPicInfo2Db(string strFileNameWithPath, ref string strError)
        {
            Logger.Info("uploadPicInfo2Db", string.Format("try to update data to server"));
            try
            {
                if (this.mobjClient2Server == null)
                {
                    Logger.Error("uploadPicInfo2Db", strError = "no Connections to Server");
                    return false;
                }
                /// read file
                /// 
                if (!File.Exists(strFileNameWithPath))
                {
                    Logger.Error("uploadPicInfo2Db", strError = string.Format("No such file:[{0}]", strFileNameWithPath));
                    return false;
                }
                FileStream objR = null;
                try
                {
                    objR = new FileStream(strFileNameWithPath, FileMode.Open, FileAccess.Read);
                    byte[] arrD = new byte[objR.Length];
                    int iLen = objR.Read(arrD, 0, (int)objR.Length);
                    Logger.Info("uploadPicInfo2Db", string.Format("read picture data length:[{0}], objR.len:[{1}]", iLen, arrD.Length));
                    //Write back server
                    this.mobjClient2Server.UploadPicInfo4CurrentTestStep(arrD);
                    arrD = null;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("uploadPicInfo2Db", strError = string.Format("Exception when read piction file:[{0}], Exception:[{1}], stack:[{2}]", strFileNameWithPath, ex.Message, ex.StackTrace));
                    return false;
                }
                finally
                {
                    if (objR != null)
                    {
                        try
                        {
                            objR.Close();
                        }
                        catch (Exception ee)
                        {
                            Logger.Error("uploadPicInfo2Db", strError = string.Format("{0} Exception when close file stream:[{1}], stack:{2}", string.IsNullOrEmpty(strError) ? "" : strError + "\r\n", ee.Message, ee.StackTrace));
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Logger.Error("uploadPicInfo2Db", string.Format("Excption:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }
        }
        #endregion

        #region auto generate Test Script
#if _Generate_Scripts
        public bool AutoGen_GenStep(string strSwfName, string strType, string strTxt)
        {
            Logger.Info("AutoGen_GenStep", string.Format("strSwfName:[{0}] strType:[{1}] strTxt:[{2}]", strSwfName, strType, strTxt));
            /// tell server to create a new row
            /// 
            try
            {
                if (this.mobjClient2Server == null)
                {
                    Logger.Error("AutoGen_GenStep", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                    return false;
                }
                string strError = "";
                bool isCreated = this.mobjClient2Server.AutoGen_GenStep(strSwfName, strType, strTxt, ref strError);
                if (!isCreated)
                {
                    Logger.Error("AutoGen_GenStep", string.Format("Can't create a test step, with error returns:[{0}]", strError));
                }
                return isCreated;
            }
            catch (Exception e)
            {
                Logger.Error("AutoGen_GenStep", string.Format("Exceptions:[{0}]", e.Message), e);
                return false;
            }
        }

        public bool GetCurrentGenerationPegAccess(out string strPegInfo, out string strErrorInfo)
        {
            Logger.logBegin("GetCurrentGenerationPegAccess");

            strPegInfo = null;
            strErrorInfo = "";

            try
            {
                if (this.mobjClient2Server == null)
                {
                    Logger.Error("GetBaseLineMode", strErrorInfo = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_CLIENT_CONNECT_2_SERVICE_FIRST));
                    return false;
                }
                string strErrorFromServer = "";
                string strPegFromServer = "";
                bool isTestCaseDataReady = this.mobjClient2Server.GetCurrentGenerationPegQuickInfo(ref strPegFromServer, ref strErrorFromServer);
                Logger.Info("GetCurrentGenerationPegAccess", string.Format("Quick_access:[{0}], errorInfo:[{1}]", strPegFromServer, strErrorFromServer));
                strPegInfo = strPegFromServer;
                strErrorInfo = strErrorFromServer;
                return isTestCaseDataReady;
            }
            catch (Exception e)
            {
                Logger.Error("GetCurrentGenerationPegAccess", string.Format("Exceptions:[{0}]", e.Message), e);
                return false;
            }


        }
#endif
        #endregion //auto generate Test Script

        #region New Loop Part,2017-1

        private Stack<MarLoopKeywordClientInfo> mstackLoopKeywordInfo = new Stack<MarLoopKeywordClientInfo>();
        public void LoopInit(object objCurrentDataAsStr)
        {
            string strObjectCurrentDataAsStr;
            Logger.Info("LoopInit", string.Format("objCurrentDataAsStr:[{0}]", strObjectCurrentDataAsStr = objCurrentDataAsStr == null ? "" : objCurrentDataAsStr.ToString()));
            if (string.IsNullOrEmpty(strObjectCurrentDataAsStr))
                return;
            if (mobjCurrentTestStep == null) return;
            /// currently, only one object exists in stack
            /// 
            MarLoopKeywordClientInfo objCurrentLoopInfo = mstackLoopKeywordInfo.Peek();
            if (objCurrentLoopInfo == null) return;
            objCurrentLoopInfo.InitLoopItemsFromStr(strObjectCurrentDataAsStr);
        }
        private void cleanLoopInfoStack()
        {
            if (mstackLoopKeywordInfo != null)
                mstackLoopKeywordInfo.Clear();
        }
        #endregion //New Loop Part,2017-1

    }

    internal class MarLoopKeywordClientInfo
    {
        int miCurrentItmIdx;
        string[] marrAllItems;
        internal bool InitLoopItemsFromStr(string strItemsSrc)
        {
            miCurrentItmIdx = -1;
            string strTmpSrc = strItemsSrc.Replace("\n", "");
            marrAllItems = strTmpSrc.Split(new string[] { "\r" }, StringSplitOptions.None);
            if (marrAllItems == null) return false;
            if (marrAllItems.Length == 0) return false;
            miCurrentItmIdx = 0;
            return true;
        }

        internal string CurrentLoopVar
        {
            get
            {
                if (miCurrentItmIdx < 0) miCurrentItmIdx = 0;
                if (miCurrentItmIdx >= marrAllItems.Length) return "";
                return marrAllItems[miCurrentItmIdx];
            }
        }
    }


    internal class CachedTestStepMgr
    {
        private List<TestStep4Services> _CachedTestSteps = null;
        internal List<TestStep4Services> CachedTestSteps
        {
            get { return _CachedTestSteps; }
            set
            {
                _CachedTestSteps = value;
                CurrentStepId = 0;
            }
        }
        internal int CurrentStepId
        {
            get; set;
        }

        internal TestStep4Services CurrentTestSteps
        {
            get
            {
                CurrentStepId = CurrentStepId < 0 ? 0 : CurrentStepId;
                if (CurrentStepId >= CachedTestSteps.Count) return null;
                return CachedTestSteps[CurrentStepId];
            }
        }

    }


}
