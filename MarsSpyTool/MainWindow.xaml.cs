using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mars.message.windowsWrapper.SystemUtil;
using NLog;
using windowsWrapper = Mars.message.windowsWrapper;
using Pen = System.Drawing.Pen;
using Mars.message.Utility.visualObjects.objectSpyer;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Mars.message.Utility;
using MarsSpyTool.subToolWindows;
using Mars.message.Inter.MQCenter.interProcess;
using MarsSpyTool.httpSvc;
using System.IO;
using MarsSpyTool.Utility;
using MarsSpyTool.Utility.config;
using Mars.message.DataLayer;
using System.ComponentModel;
using NLog.Targets.Wrappers;
using MarsEngineSvc.basicReturnDataStructure;
using Mars.message.Business;
using System.Collections.ObjectModel;
using MarsSpyTool.subToolWindows.hintWindows;
using MarsSpyTool.subToolWindows.viewModal;
using MarsSpyTool.Utility.directoryMonitor;
using Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode;
using System.Threading;
using MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client;
using Mars.Inter.MQCenter.objectEngine;
using MarsSpyTool.subToolWindows.testStepEditor;
using Mars.Model;
using System.Net.NetworkInformation;
using Mars.message.Dto;
//using System.Windows.Interactivity;
using Mars.message.Inter.MQCenter.interProcess.HttpRestService;
using Mars.message.Inter.MQCenter.objectSpy;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace MarsSpyTool
{

    public class MarsSpyApplication: INotifyPropertyChanged
    {
        private string applicationName;
        public string ApplicationName
        {
            get { return applicationName; }
            set { 
                if (applicationName == value) return ; 
                applicationName = value;
                OnPropertyChanged(nameof(ApplicationName));
            }
        }
        private string applicationProcess;
        public string ApplicationProcess
        {
            get => applicationProcess;
            set
            {
                if (applicationProcess == value) return ;
                applicationProcess = value;
                OnPropertyChanged(nameof(ApplicationProcess));
            }
        }

        private long applicationId;
        public long ApplicationId
        {
            get => applicationId;
            set
            {
                if (applicationId == value) return ;
                applicationId = value;
                OnPropertyChanged(nameof(ApplicationId));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static List<MarsSpyApplication> CurrentMarsFilteredApplications
        {
            get;set;
        }

        public static string CurrentProcessExe { get; set; }
    }


    public sealed class SpyToolParaInfo
    {
        public static Dictionary<string, string> currentToolParaInfo = new Dictionary<string, string>();
    }
    internal class Mouse_ObjectPosManagement
    {
        private static Logger _Logger = LogManager.GetLogger("MarsSpyLog");

        internal IntPtr CurrentHandler = IntPtr.Zero;
        internal IntPtr MainWindowHandleFromPos = IntPtr.Zero;
        internal long PreviousTimeTickStopped = 0;

        internal void HighlightObjectAtMousePosition(int x, int y, bool refreshDeskTop = false)
        {
            IntPtr handleFromPos = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));

            if (handleFromPos == IntPtr.Zero) return;
            if (MainWindowHandleFromPos != handleFromPos)
            {                
                MarsWindowsAPIs.RECT lpRect;
                MainWindowHandleFromPos = handleFromPos;
                //if (refreshDeskTop)
                {
                    Console.WriteLine("invalidate");
                }
                bool isOk = MarsWindowsAPIs.GetWindowRect(handleFromPos, out lpRect);
                if (!isOk) return;
                string strError = "";
#if _NOQTP

                //windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(lpRect, ref strError);

                HighlightWindow frm = HighlightWindow.getInstance();
                //if (frm.InvokeRequired) {
                //    frm.Invoke(new Action(() => {
                //        frm.Hide();
                //        frm.Left = lpRect.Left - 1;
                //        frm.Top = lpRect.Top - 1;
                //        frm.Width = lpRect.Right - lpRect.Left + 2;
                //        frm.Height = lpRect.Bottom - lpRect.Top + 2;
                //        frm.Show();
                //    }));

                //}
                //else
                //{
                //frm.Hide();
                HighlightWindow.SafeInvoke(()=>frm.Left = lpRect.Left - 1);
                HighlightWindow.SafeInvoke(() => frm.Top = lpRect.Top - 1);
                HighlightWindow.SafeInvoke(() => frm.Width = lpRect.Right - lpRect.Left + 2);
                HighlightWindow.SafeInvoke(() => frm.Height = lpRect.Bottom - lpRect.Top + 2);
                HighlightWindow.SafeInvoke(() => frm.Show());
                //}
#endif
            }
        }
    }

    public class MarsObjectFileStatusMonitor
    {
        private string monitorFileNameWithPath = "";
        public MarsObjectFileStatusMonitor()
        {
            InitFileNameInfo();
            startMonitor();
        }
        private void InitFileNameInfo()
        {
            var pth = System.IO.Path.GetDirectoryName(typeof(MarsSpiedObjectInfo).Assembly.Location);
            var currentSystemUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            pth = System.IO.Path.Combine(pth, $"data\\obj\\{currentSystemUser}");
            if (!System.IO.Directory.Exists(pth))
            {
                System.IO.Directory.CreateDirectory(pth);
            }
            monitorFileNameWithPath = System.IO.Path.Combine(pth, MarsConstants.CNST_SYPTOOL_JSONOBJ_FILENAME); 
        }

        private void startMonitor()
        {
            //objectFileMonitor.MarsFileMonitor.InitMonitor(
            //   stepFileChangeImpl, stepFileCreateImpl, stepFileDeleteImpl, MarsConstants.CNST_SYPTOOL_STEPS_FILENAME
            //   );
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MarsObjectSpy : Window, INotifyPropertyChanged
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");

        private static MarsObjectSpy SpyInstance = null;
        private IntPtr HookHandler = IntPtr.Zero;
        private Cursor FinderCursor = null;
        private Cursor GenStepsCursor= null;
        private Cursor RecordAndReplayCursor = null;
        private Cursor OldCursor = null;
        private int    currentPID = -1;
        private bool IsRecordAndReplayMode {
            get => this.Current_tool_func == Mars_spy_tool_function._record_replay;            
        }
        
        private Mars_spy_tool_function current_tool_func = Mars_spy_tool_function._none;
        public Mars_spy_tool_function Current_tool_func
        {
            get => current_tool_func;
            set => current_tool_func = value;
        }


        private MarsObjectFileStatusMonitor marsObjFileStatusMonitor = new MarsObjectFileStatusMonitor();

        public MarsObjectSpy()
        {
            logger.Info($"MarsObjectSpy\tbegin");
            InitializeComponent();
            FinderCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("MarsSpyTool.Resources.CrosshairsCursor.cur"));
            GenStepsCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("MarsSpyTool.Resources.icon-generate-testcase.cur"));
            RecordAndReplayCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("MarsSpyTool.Resources.video-record-64.cur"));
            FinderButton.PreviewMouseLeftButtonDown += Button_PreviewMouseLeftButtonDown;
            DataContext = this;
            //FinderButton.MouseMove += Button_MouseMove;
            //FinderButton.MouseLeftButtonUp += Button_MouseUp;
        }
        // Global hotkey (Ctrl+F2) support
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_CTRL_F2 = 0xA102;
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_F2 = 0x71;
        private HwndSource hwndSource;

        private void RegisterGlobalHotkeys()
        {
            try
            {
                var source = (HwndSource)PresentationSource.FromVisual(this);
                if (source == null) return;
                hwndSource = source;
                hwndSource.AddHook(WndProc);
                RegisterHotKey(hwndSource.Handle, HOTKEY_ID_CTRL_F2, MOD_CONTROL, VK_F2);
                logger.Info("RegisterGlobalHotkeys\tCtrl+F2 registered");
            }
            catch (Exception ex)
            {
                logger.Warn($"RegisterGlobalHotkeys\tFailed: {ex.Message}");
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            try
            {
                if (hwndSource != null)
                {
                    UnregisterHotKey(hwndSource.Handle, HOTKEY_ID_CTRL_F2);
                    hwndSource.RemoveHook(WndProc);
                    hwndSource = null;
                }
                logger.Info("UnregisterGlobalHotkeys\tCtrl+F2 unregistered");
            }
            catch (Exception ex)
            {
                logger.Warn($"UnregisterGlobalHotkeys\tFailed: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_CTRL_F2)
                {
                    handled = true;
                    HandleCtrlF2Hotkey();
                    return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
        }

        private void HandleCtrlF2Hotkey()
        {
            try
            {
                if (uiaInfoWindow != null && uiaInfoWindow.IsVisible 
                    // && !uiaInfoWindow.IsActivated
                    )
                {
                    //uiaInfoWindow.ActivateWindow();
                    Clipboard.SetText(uiaInfoWindow.GetObjectInfo());
                    uiaInfoWindow.Show();
                    var helper = new WindowInteropHelper(uiaInfoWindow);
                    MarsWindowsAPIs.ShowWindow(helper.Handle, (int)ShowWindowCommands.SW_SHOWMINNOACTIVE);
                    return;
                }
                if (trackingCheckBox != null && trackingCheckBox.IsEnabled)
                {
                    trackingCheckBox.IsChecked = !(trackingCheckBox.IsChecked ?? false);
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"HandleCtrlF2Hotkey\tException: {ex.Message}");
            }
        }
        private Mouse_ObjectPosManagement CurrentObjectStays = new Mouse_ObjectPosManagement();
        
        // Tracking related fields
        private System.Windows.Threading.DispatcherTimer trackingTimer = null;
        private System.Drawing.Point lastMousePosition = new System.Drawing.Point(-1, -1);
        private DateTime lastMousePositionTime = DateTime.MinValue;
        private subToolWindows.hintWindows.UIAObjectInfoWindow uiaInfoWindow = null;
        private IntPtr trackingHookHandler = IntPtr.Zero;

        public void ObjectFileChangeImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin..");
            try
            {
                string strFileName = fileChangeEvent.FullPath;
                /// read to object, and send to back end
                /// 
                logger.Info($"{iMark}|{strFileName}");
                /// read file
                /// 
                var alllines = System.IO.File.ReadAllText(strFileName);
                if (string.IsNullOrEmpty(alllines))
                {
                    logger.Error($"{iMark}|no text after readLines|{strFileName}");
                    return;
                }
                int iMarkEnd = alllines.IndexOf(Mars.message.Utility.MarsConstants.CNST_SPYTOOL_OBJ_FILE_ENDMARK);
                if (iMarkEnd < 0)
                {
                    logger.Info($"{iMark}|no last row mark at the end of the file||{strFileName}");
                    return;
                }
                string strObjInfo2WebServer = alllines.Substring(0, iMarkEnd);
                RestClient2MarsServer clnt = new RestClient2MarsServer();
                isOk = clnt.sendRecgObjectsToServer(strObjInfo2WebServer, ref strError);
                if (!isOk)
                {
                    this.statusBarHint.Text = strError;
                    //this.statusBarHint.Foreground= System.Windows.Media.Brush.
                }
                else
                {
                    this.statusBarHint.Text = "Have send object info to MARS web end";
                }
            }
            finally
            {
                logger.Info($"{iMark}|end");
            }
        }
        public void mouseMoveHookImple(int x, int y)
        {
            //Console.WriteLine("mouseMoveHookImple");
            if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.IsLeftMousePressed())
            {
                StopSnoopTargetsSearch();
                return;
            }
            UpdateFeedbackWindowPosition();

            if (!IsDragging) return;
            
            // Stop tracking if dragging starts
            if (trackingCheckBox.IsChecked == true && trackingCheckBox.IsEnabled)
            {
                trackingCheckBox.IsChecked = false;
            }
            
            if (CurrentObjectStays.PreviousTimeTickStopped == 0)
            {
                CurrentObjectStays.PreviousTimeTickStopped = DateTime.Now.Ticks;
                return;
            }
            
            /// 判断停留时间
            /// 
            long lCurDis = DateTime.Now.Ticks - CurrentObjectStays.PreviousTimeTickStopped;
            if ((lCurDis / 10000) > 500) // greater than 500 ms, then get current 
            {

                CurrentObjectStays.PreviousTimeTickStopped = lCurDis;

                //绘制当前的window
                CurrentObjectStays.HighlightObjectAtMousePosition(x, y, false);
            }

        }

        public void mouseLeftButtonPressImp(int x, int y)
        {
            //CurrentObjectStays.PreviousTimeTickStopped = DateTime.Now.Ticks;
        }

        private bool IsMarsAgentInjected(List<MarsWindowsAPIsExtend.Module> lstModule)
        {
            if (lstModule == null)
                return false;
            var agnt = (from a in lstModule
                        where (!string.IsNullOrEmpty(a.ModuleName))
                        && (a.ModuleName.ToUpper().IndexOf("MANAGEDINJECTOR") >= 0
                        || a.ModuleName.ToUpper().IndexOf("MARS.MSMQHOST") >= 0
                        || a.ModuleName.ToUpper().IndexOf("MARS.INTER.MARCENTER") >= 0
                        || a.ModuleName.ToUpper().IndexOf("MARSENGINE.CORE") >=0 
                        || a.ModuleName.ToUpper().IndexOf("MARSCoreEngineWpfImpls") >= 0 )
                        select a).FirstOrDefault();

            return agnt != null;

        }

        private void FileChangeService_FileChanged(object sender, MarsFileChangedEventArgs e)
        {
            logger.Info($"FileChangeService_FileChanged\tBegin|File change detected|{e.ChangeType}|{e.FilePath}");
            
            string targetFileName = "";
            if (e.ChangeType == WatcherChangeTypes.Created)
                targetFileName = e.FilePath;
            else if (e.ChangeType == WatcherChangeTypes.Changed)
                targetFileName = e.OldFilePath;
            else
            {
                logger.Info($"FileChangeService_FileChanged\t|{e.ChangeType}|unsupported");
                return;
            }
            targetFileName = string.IsNullOrEmpty(targetFileName) ? " " : targetFileName;
            string strFileName = System.IO.Path.GetFileName(targetFileName);
            string strError = "";
            if (strFileName.Equals(MarsRestFulCnst.cnst_port_swapfile))
            {
                // open file and connect to the server
                var restSvcInfo = RestServiceInfo.Instance();
                if (!restSvcInfo.loadFromFile(targetFileName, ref strError))
                {
                    logger.Error($"FileChangeService_FileChanged\t|error|{strError}");
                    return;
                }
                /// set signal
                /// 
                EngineSyncMonitor.portfileNotifyEvent.Set();
            }
        }

        private bool IsSvcRead()
        {
            logger.Info($"IsSvcRead\tBegin");
            /// 算法：
            /// 1，是否存在端口文件，如果不存在，返回false
            /// 2，加载文件， 链接端口，发送
            /// 
            bool isOk = true;
            string strError = "";
            int iPort = -1;
            var rsltInfo = RestServiceInfo.Instance();
            string strPth = this.GetType().Assembly.Location;
            strPth = System.IO.Path.GetDirectoryName(strPth);
            strPth = System.IO.Path.Combine(strPth, $"{MarsRestFulCnst.cnst_SwapDir}\\{MarsRestFulCnst.cnst_port_swapfile}");
            if (!System.IO.File.Exists(strPth)) return false;

            if (!rsltInfo.loadFromFile(strPth, ref strError)) return false;
            var clnt = RESTClient2MessageCenter.getInstance();
            if (!clnt.testHeartBeat())
                return false;
            return true;
        }

        public void mouseLeftButtonUpImp(int x, int y)
        {
            logger.Info($"mouseLeftButtonUpImp\tbegin|{x}|{y}|");
            try
            {
                if (!IsDragging) return;

                if (this.WindowState == WindowState.Minimized)
                    this.WindowState = WindowState.Normal;

                HighlightWindow.HideAndDestroy();
                // if record&replay should still hold the mouse
                if (!this.IsRecordAndReplayMode)    
                    StopSnoopTargetsSearch();
                IsDragging = false;
                
                //算法：
                //获得当前位置的windows
                IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                //获得进程
                int pid;
                bool is64 = false;
                if (MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out pid) == 0) return;
                Process p = Process.GetProcessById(pid);
                if (p.Id == Process.GetCurrentProcess().Id) 
                    return;

                if (this.current_tool_func == Mars_spy_tool_function._find_obj
                    && WebSpyIntegration.TryHandleFinderWebDrop(p, hwnd, x, y))
                {
                    return;
                }

                var is64_2 = !MarsWindowsAPIsExtend.IsProcess32(p.Handle);
                //MarsWindowsAPIs.IsWow64Process(p.Handle, out is64);
                logger.Info("\t", $"It is 64? {is64_2}|{p.ProcessName}");

                /// 通过IAccessible接口获得对象树
                if (IsSingleObjectMode)
                {
                    //Mousecapture.UnHookMouse();
                    ExecuteObjectSingleMode(p.ProcessName, pid, hwnd, x, y);
                    return;
                }

#if debug_summitOnly
                if (p.ProcessName.IndexOf("summitFT", StringComparison.OrdinalIgnoreCase) < 0)
                    return;
#endif
                //bool is64_2 = MarsWindowsAPIsExtend.IsWin64Emulator(p.Handle);
                //if (Process.GetCurrentProcess().Id == pid) return;
                // 通过列举所有的模块，判断是否是.net 或者java等程序，QT等获得qtcore的版本
                var itms = MarsWindowsAPIsExtend.CollectModules(pid);
                foreach (var itm in itms)
                {
                    if (itm == null) continue;                    
                    logger.Info("\t", ($"modual1 name:{itm.ModuleName}, path:{itm.ModulePath} processIs64?:{is64} - {is64_2}"));
                }
                string strVer = "";
                MARSSupportedProcessType supportType = MarsProcessModule.GetTargetTypeFromProcessModule(itms, ref strVer);
                if (supportType == MARSSupportedProcessType.Mars_noneSupport)
                {
                    MessageBox.Show($"The process [{p.ProcessName}] is not one of the application:Java/.net/QT");
                    return;
                }

                /// 是否存在可以测试的系统
                /// 
                var lstOfMatchedProcess = this.applicationListInfo
                    .Where(xx => string.Compare(xx.ApplicationProcess, p.ProcessName, true) == 0)
                    .ToList();

                MarsHintWindowsModal appConfirmModal                = null;
                MarsSpyApplication.CurrentMarsFilteredApplications  = lstOfMatchedProcess;
                MarsSpyApplication.CurrentProcessExe                = p.ProcessName;

                if ((lstOfMatchedProcess.Count > 0)&&(
                    (this.current_tool_func == Mars_spy_tool_function._auto_gen_test_step)||
                    (this.current_tool_func == Mars_spy_tool_function._record_replay)
                    ))
                {
                    appConfirmModal = new MarsHintWindowsModal();
                    appConfirmModal.CreateApplicationsFromList(lstOfMatchedProcess);
                    MarsHintWindowSelectApplication appConfirm = new MarsHintWindowSelectApplication(appConfirmModal);
                    appConfirm.ShowDialog();
                    if ((bool)!appConfirm.DialogResult) return;
                }

                // 判断是否存在Mars的injector，如果存在，发送一个mqmessage，激活，否则injector
                bool isMarsAgentInjected = IsMarsAgentInjected(itms);
                logger.Info("object spy all modules|"+string.Join("\r\n\t", itms.Select(z => z.ModuleName).OrderBy(z=>z).ToList()));
                //直接通过engne发送启动spy++ 
                string strError = "";
                //先关闭
                KillAgent();
                
                bool isEngineSvcRead = false;
                if (this.current_tool_func == Mars_spy_tool_function._auto_gen_test_step)
                {
                    isEngineSvcRead = IsSvcRead();
                    if (!isEngineSvcRead)
                    {
                        StartEngineAgent(is64, p.Id, hwnd, isMarsAgentInjected,
                            this.IsRecordAndReplayMode, supportType, ref strError);
                    }
                } else     
                        StartEngineAgent(is64, p.Id, hwnd, isMarsAgentInjected,
                            this.IsRecordAndReplayMode, supportType, ref strError);

                if (this.IsRecordAndReplayMode)
                {
                    /// miniue the current window, and active the log window
                    //RecordReplayStepsWindow.showRecordStepList(this);
                    //this.WindowState = System.Windows.WindowState.Minimized;
                    Mousecapture.UnHookMouse();
                }
                else
                {
                    if (this.current_tool_func == Mars_spy_tool_function._auto_gen_test_step)
                    {
                        bool isOk = true;
                        /// check 是否服務已經起來
                        /// 
                        //isOk = IsSvcRead();
                        uint result = 1;
                        if (isEngineSvcRead)
                        {
                            result = 0;
                        }else
                            result = MarsWinAPIs.WaitForSingleObject(EngineSyncMonitor.portfileNotifyEvent.SafeWaitHandle.DangerousGetHandle(), 10000);
                        // Check the result
                        if (result == 0) // 已经获得portfile
                        {
                            logger.Info("StartEngineAgent", "Event signaled, that means service is ready, try to connect to service and send command");
                            QueryObjectRequst req = new QueryObjectRequst();
                            req.currentHandle = hwnd.ToInt64();
                            req.x = x;
                            req.y = y;
                            req.typeOfGenerateSteps = 0;

                            // invoke getting all objects command API
                            List<MarsSpiedObjectBasicInfo> objs = RESTClient2MessageCenter
                                .getInstance()
                                .QueryCurrentObjects(req, ref isOk, ref strError);
                            if (!isOk)
                            {
                                logger.Error("StartEngineAgent\t" + $"can't query objects|with Error|{strError}");
                                return;
                            }
                            
                            MARSTestStepEditorModel stepEditorModel = new MARSTestStepEditorModel();
                            if (appConfirmModal == null)
                            {
                                MessageBox.Show("Please select the right application!", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            stepEditorModel.SelectedApplication = appConfirmModal.SelectedApplication;
                            stepEditorModel.TargetDBIdx = this.CurrentMarsDBIdx;
                            stepEditorModel.HookedPId = p.Id;
                            // filter ignored types
                            List<MARSTestStepsModel> lstTestStep = stepEditorModel.FilterObjectsAndBuildTestSteps(objs, ref isOk, ref strError);
                            // 创建编辑区
                            TestStepEditorForSpyer stepEditor = TestStepEditorForSpyer.getInstance();                           
                            stepEditorModel.SetTestStep(lstTestStep);
                            stepEditorModel.scrollToLast += stepEditor.ScrollToTheBottom;
                            stepEditorModel.updateProcessBar += stepEditor.UpdateProcessBar;
                            //stepEditorModel.TestSteps.CollectionChanged += stepEditor.monitorColletionChanges;
                            stepEditorModel.KeywordList = KeywordsForSpyer.MarsKeywords;
                            stepEditor.DataContext = stepEditorModel;
                            
                            stepEditor.Show();
                        }
                        else if (result == 0x00000102) // WAIT_TIMEOUT
                        {
                            logger.Error("StartEngineAgent", "Timed out waiting for event.");
                            /// pop up message that
                            /// 
                            MessageBox.Show(strError = "Can't start or connect MARS engine services, \r\nplease try later ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            strError = "Wait failed with error code: " + Marshal.GetLastWin32Error();
                            logger.Error($"StartEngineAgent|{strError}" );
                            MessageBox.Show(strError = "Can't start or connect MARS engine services, \r\nplease try later ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }catch(Exception e)
            {
                logger.Error(e,$"mouseLeftButtonUpImp\t{ e.Message}");
            }
            finally
            {
                logger.Info("mouseLeftButtonUpImp\tend");
            }
        }
        /// <summary>
        /// 单节点模式。主要算法如下：
        /// 1，通过x,y获得IAccessible接口，如果没有，返回错误
        /// 2，通过IAccessible接口一直回溯parent IAccessible对象到顶层，
        /// 3，将树在Mars.Inter.MQCenter中的MarsObjSpyForm界面显示
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="hwnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ExecuteObjectSingleMode(string strProcessName, int pid, IntPtr hwnd, int x, int y)
        {
            MarsObjSpyForm.StartAccessibleModeFromXY(strProcessName,pid, hwnd, x, y);
        }

        private void KillMarsProcess(string strProcessName)
        {
            var arrp = Process.GetProcessesByName(strProcessName);
            foreach (var itm in arrp)
            {
                try
                {
                    if (itm != null) itm.Kill();
                }
                catch (Exception e)
                {
                    logger.Error("KillAgent", e.Message, e);
                }

            }
        }

        private void KillAgent()
        {
            Process[] arrp = Process.GetProcessesByName("Mars.AutoTestingDriver");
            foreach (var itm in arrp)
            {
                try
                {
                    if (itm != null) itm.Kill();
                }
                catch (Exception e)
                {
                    logger.Error("KillAgent", e.Message, e);
                }

            }
            arrp = Process.GetProcessesByName("Mars.AutoTestingDriver32");
            foreach (var itm in arrp)
            {
                try
                {
                    if (itm != null) itm.Kill();
                }
                catch (Exception e)
                {
                    logger.Error("KillAgent", e.Message, e);
                }

            }
            arrp = Process.GetProcessesByName("MarsEngineCore.HostLauncher");
            foreach (var itm in arrp)
            {
                try
                {
                    if (itm != null) itm.Kill();
                }
                catch (Exception e)
                {
                    logger.Error("KillAgent", e.Message, e);
                }

            }
            arrp = Process.GetProcessesByName("MARSCoreMessageCenter");
            foreach (var itm in arrp)
            {
                try
                {
                    if (itm != null) itm.Kill();
                }
                catch (Exception e)
                {
                    logger.Error("KillAgent", e.Message, e);
                }

            }
            
            //KillMarsProcess("MarsSpyTool"); 
        }

        private bool IsMouseInsideOfCurrentSpy(POINT p)
        {
            string strError = "";
            string strStack = "";
            System.Windows.Forms.Control currentObj = MarsWindowsAPIsExtend.FromScreenPoint(new System.Drawing.Point(p.X, p.Y), ref strError, ref strStack);
            if (currentObj == null) return false;
            int pid = -1;
            if (MarsWindowsAPIs.GetWindowThreadProcessId(currentObj.Handle, out pid) != 0)
            {
                return currentPID == pid;
            }
            //return IsMouseInsideOfCurrentSpy(currentObj.Handle);
            return false;
        }

        private bool IsMouseInsideOfCurrentSpy(IntPtr currentWnd)
        {
            if (currentWnd == IntPtr.Zero) return false;
            if (currentWnd == this.currentWndHandle) return true;

            IntPtr prntHwnd = MarsWindowsAPIs.GetAncestor(currentWnd, MarsWindowsAPIs.GetAncestorFlags.GetParent);
            if (prntHwnd == this.currentWndHandle) return true;
            if (prntHwnd != IntPtr.Zero) return IsMouseInsideOfCurrentSpy(prntHwnd);
            return false;
        }

        private void startMarsObjectTool32(int targetPid,bool isRecorderReplay)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            string app32ToolPath = "";
            try
            {
                app32ToolPath = typeof(MarsObjectSpy).Assembly.Location;
                app32ToolPath = System.IO.Path.GetDirectoryName(app32ToolPath);
                app32ToolPath = System.IO.Path.Combine(app32ToolPath, "MARSToolHostAgent32.exe");
                Process p = null;
                if (!isRecorderReplay)
                    p=Process.Start(app32ToolPath, $"startMARSObjectTool {targetPid}");
                else
                    p=Process.Start(app32ToolPath, $"startMARSRecordReplayTool {targetPid}");
                p.WaitForExit(5000);
                logger.Error($"{iMark}|{app32ToolPath}|returns|{p.ExitCode}");
            }
            catch (Exception e)
            {
                logger.Error($"{iMark}|Exception|{e.Message}|\r\n{e.StackTrace}");
                Console.WriteLine($"Can't start {app32ToolPath} with exception|{e.Message}|\r\n{e.StackTrace}");
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("Please press [enter] to quit");
                Console.ReadLine();
            }
            finally
            {
                logger.Info($"{iMark}|end");
            }
        }

        private bool StartEngineAgent(bool isStart64, int targetPId, IntPtr hwnd, bool isMarsAgentInjected,
            bool isRecordAndReplay, 
            MARSSupportedProcessType supportType,
            ref string strError)
        {
            int iMark = new Random().Next();
            logger.Trace($"{iMark}|begin.");
            bool isOk = false;
            try
            {
                // 1 is the other process?
                // 2 is 32 or 64?
                // 3 is .net or windows, qt, java, web?
                
                //POINT p = new POINT();
                int tmpPid = targetPId;
                bool isDialogMode = false;                
            
                logger.Info($"the same pid {tmpPid}|{this.currentPID}");
                if (this.currentPID == tmpPid)
                {
                    logger.Info($"\tsame process, do nothing");
                    return false;
                }

                /// 如果是wpf frame,需要启动.net framewok
                if ((supportType & MARSSupportedProcessType.Mars_dotNet_wpf_Core)!=0)
                {
                    isOk= StartWpfCoreEngine(supportType, targetPId, hwnd, ref strError);
                    if (!isOk)
                    {
                        logger.Error($"{iMark}|StartEngineAgent\t{strError}");
                    }
                    return isOk;
                }

                IntPtr hdl = MarsWindowsAPIs.OpenProcess((uint)MarsWindowsAPIs.ProcessAccessFlags.All, true, (uint)tmpPid);
                if (hdl == IntPtr.Zero)
                {
                    logger.Info($"{iMark}|get desktop");
                    return false;
                }
                System.Diagnostics.Process ps = System.Diagnostics.Process.GetProcessById(tmpPid);
                if (ps != null)
                {
                    logger.Info($"{iMark}|process is|{ps.ProcessName}");
                }
                string strMQCenterDllName = "MarsInterMQCenter.dll";// is32Bit ? "MarsInterMQCenter.Any.dll" : "MarsInterMQCenter.dll";
                string strPathOfMars = System.IO.Path.GetDirectoryName(typeof(MarsObjectSpy).Assembly.Location);
                string tmpRestSpace = "Mars.message.Inter.MQCenter.interProcess.HttpRestService.MarsSpyRESTfulServer";
                string strInjectType = "Normal";
                if ((supportType & MARSSupportedProcessType.Mars_dotNet_wpf_frame)!=0)
                {
                    strInjectType = "Wpf";
                }                
                                       
                if (MarsWindowsAPIsExtend.IsProcess32(hdl)) //
                {
                    logger.Info($"{iMark}|is 32 application");
                    //Load
                    startMarsObjectTool32(tmpPid,this.IsRecordAndReplayMode);
                }
                else
                {
                    // load inject 64
                    IntPtr dialogHdl = IntPtr.Zero;
                    if (ModalChecker.IsWaitingForUserInput(ps, ref dialogHdl))
                    {
                        isDialogMode = true;
                        if (!this.IsRecordAndReplayMode)
                        {
                            if (current_tool_func == Mars_spy_tool_function._auto_gen_test_step) {

                                /// start file notify service and send command
                                /// 
                                var objFileNotifySVC = MarsFileChangeNotificationService.GetInst();
                                objFileNotifySVC.FileChanged += FileChangeService_FileChanged;
                                objFileNotifySVC.startSVC();
                                if ((string.Compare(strInjectType, "wpf", true) == 0)
                                    &&((supportType & MARSSupportedProcessType.Mars_dotNet_wpf_Core)!=0))
                                {
                                    strMQCenterDllName = "MarsCoreAgentLibrary.dll";
                                }
                                //string tmpNameSpace = "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc";

                                /// start normal way, and send command
                                ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                                        strMQCenterDllName //"MarsInterMQCenter.dll"
                                        ), tmpRestSpace, "StartInternalSpyRestSvc", strInjectType);
                                logger.Info("StartEngineAgent\twait for port file is ready");

                                return true;
                            }
                            else  /// find object
                            {
                                if ((string.Compare(strInjectType, "wpf", true) == 0)
                                    && ((supportType & MARSSupportedProcessType.Mars_dotNet_wpf_Core) != 0))
                                {
                                    strMQCenterDllName = "MarsCoreAgentLibrary.dll";
                                }
                                /// 对于 wpf 在framework中的，采用不同的入口方法，或者参数
                                /// 引擎方面已经采用参数模式
                                ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                                        strMQCenterDllName //"MarsInterMQCenter.dll"
                                        ), "Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer", "StartInternalSpyRestSvc", strInjectType);
                                                          
                                logger.Info("StartEngineAgent\tMode|find object|wait for port file is ready");
                            }
                        }
                        else
                        {
                            tmpRestSpace = typeof(Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer).FullName;// "Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRecordReplayServer";
                            ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                                    strMQCenterDllName //"MarsInterMQCenter.dll"
                                    ), tmpRestSpace, "StartInternalRecordReplayRestSvc", strInjectType);
                        }
                    }
                    else
                    {
                        if (!MarsWindowsAPIsExtend.IsProcessMainWindowLoaded(ps, ref strError))
                        {
                            logger.Error($"{iMark}|{strError}|after IsProcessMainWindowLoaded");
                            return false;
                        }
                        HandleRef handleRef = new HandleRef(ps, ps.MainWindowHandle);
                        IntPtr lpdwResult = IntPtr.Zero;
                        if (!ps.MainWindowHandle.Equals(IntPtr.Zero))
                        {
                            IntPtr lResult = MarsWindowsAPIs.SendMessageTimeout(
                                    //handleRef,
                                    ps.MainWindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    MarsWindowsAPIs.SMTO_BLOCK,
                                    3000,
                                    out lpdwResult);
                        }
                        //ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                        //        strMQCenterDllName //"MarsInterMQCenter.dll"
                        //        ), "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc", "StartMonitorThread", "Normal");
                        if (!isRecordAndReplay)
                        {
                            ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                                    strMQCenterDllName //"MarsInterMQCenter.dll"
                                    ), "Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer", "StartInternalSpyRestSvc", "Normal");
                            /// wait for file notify service
                            /// 

                        }
                        else
                        {
                            ManagedInjector.Injector.Launch(isDialogMode ? dialogHdl : ps.MainWindowHandle, System.IO.Path.Combine(strPathOfMars,
                                    strMQCenterDllName //"MarsInterMQCenter.dll"
                                    ), "Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer", "StartInternalRecordReplayRestSvc", "Normal");
                        }
                    }
                }
            
                return true;
            }catch(Exception e)
            {
                logger.Error($"StartEngineAgent\tException|{e.Message}\r\n{e.StackTrace}");
                throw new Exception(e.Message);
            }
            finally
            {
                logger.Info($"{iMark}|end");
            }
        }

        private bool StartWpfCoreEngine(MARSSupportedProcessType supportType, int targetPId, IntPtr hwnd, ref string strError)
        {
            logger.Info($"StartWpfCoreEngine\tsupportType|{supportType}");
            string marscoreWpfLauncherDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(MarsObjectSpy).Assembly.Location),
                "MarsCore");
            string marcoreWpfLauncher = System.IO.Path.Combine(marscoreWpfLauncherDir, "MarsEngineCore.HostLauncher.exe");
            if (!System.IO.File.Exists(marcoreWpfLauncher))
            {
                logger.Error(strError = $"StartWpfCoreEngine\t|{marcoreWpfLauncher}|not exist");
                return false;
            }
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = marcoreWpfLauncher;
                startInfo.WorkingDirectory = marscoreWpfLauncherDir; // 工作目录
                startInfo.Arguments = $"--assembly MarsEngine.Core --attachConsoleToParent --className MarsEngine.Infrastructure.MarsCoreInnerManager --targetHwnd {hwnd} --methodName StartMarsCoreAutomation --targetPID {targetPId}";
                startInfo.UseShellExecute = false; // 是否使用操作系统外壳程序启动
                startInfo.RedirectStandardOutput = true; // 重定向标准输出
                startInfo.RedirectStandardError = true; // 重定向标准错误
#if DEBUG
                startInfo.CreateNoWindow = false;// 创建新窗口, for test
#else
                startInfo.CreateNoWindow = true; // 不创建新窗口
#endif
                using (Process process = new Process { StartInfo = startInfo })
                {
                    logger.Info($"StartWpfCoreEngine\t with para|{startInfo.Arguments}");
                    process.Start();
                    process.WaitForInputIdle(10000); // 等待进程进入空闲状态
                }

                /// wait until the process is ready
                /// 
                
                return true;
            }
            catch (Exception e)
            {
                strError = $"Can't start MARS core wpf engine starter, with error message|{e.Message}";
                logger.Error($"StartWpfCoreEngine\t|{strError}\r\n{e.StackTrace}", e);
                return false;
            }
        }

        public static MarsObjectSpy GetSpyInstance()
        {
            if (SpyInstance == null)
            {
                SpyInstance = new MarsObjectSpy();
            }
            return SpyInstance;
        }

        private bool g_isLeftMouseDown = false;
        private bool isDragging = false;
        public bool IsDragging
        {
            get => isDragging;
            set
            {
                if (isDragging != value)
                {
                    isDragging = value;
                    DragStatus.Content = isDragging ? "T" : "F";

                }
            }
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            logger.Info($"Button_PreviewMouseLeftButtonDown\t begin|{IsDragging}");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Current_tool_func = Mars_spy_tool_function._find_obj;
                if (HookHandler == IntPtr.Zero)
                    HookHandler = Mousecapture.SetHook(mouseMoveHookImple,
                        mouseLeftButtonPressImp,
                        mouseLeftButtonUpImp);
                StartSnoopTargetsSearch();
                e.Handled = true;
                IsDragging = true;

                /// start object file monitor
                /// 
                startObjectMonitor();
            }
            logger.Info($"Button_PreviewMouseLeftButtonDown\tEnd|{IsDragging}");
        }

        private void StartWebSocketServer()
        {
            //if (MarsSpyRESTfulNetServer.listener == null)
            //    MarsSpyRESTfulNetServer.StartInternalSpyRestSvc(1);
            //else
            //{
            //    logger.Info($"Button_PreviewMouseLeftButtonDown\tSvc has been started at |");
            //}
        }

        private void Button_PreviewRecordAndReplayButtonDown(object sender, MouseButtonEventArgs e)
        {
            logger.Info($"Button_PreviewRecordAndReplayButtonDown\t begin|{IsDragging}");
            /// 将appbar windows显示            
            RecordReplayStepsWindow.showRecordStepList(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Current_tool_func = Mars_spy_tool_function._record_replay;
                if (HookHandler == IntPtr.Zero)
                    HookHandler = Mousecapture.SetHook(mouseMoveHookImple,
                        mouseLeftButtonPressImp,
                        mouseLeftButtonUpImp);
                StartSnoopTargetsSearch();
                e.Handled = true;
                IsDragging = true;                
                /// start object file monitor
                /// 
                startObjectMonitor();
                /// 启动WebSocket服务
                /// 
                StartWebSocketServer();
            }
            logger.Info($"Button_PreviewRecordAndReplayButtonDown\tEnd|{IsDragging}");
        }

        private void Button_PreviewTestCaseMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            logger.Info($"Button_PreviewTestCaseMouseLeftButtonDown\t begin|{IsDragging}");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Current_tool_func = Mars_spy_tool_function._auto_gen_test_step;
                if (HookHandler == IntPtr.Zero)
                    HookHandler = Mousecapture.SetHook(mouseMoveHookImple,
                        mouseLeftButtonPressImp,
                        mouseLeftButtonUpImp);
                StartSnoopTargetsSearch();
                e.Handled = true;
                IsDragging = true;

                /// start object file monitor
                /// 
                startObjectMonitor();
            }
            logger.Info($"Button_PreviewTestCaseMouseLeftButtonDown\tEnd|{IsDragging}");
        }

        private void startObjectMonitor()
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin...");
            try
            {

            }
            catch (Exception e)
            {
                logger.Error($"{iMark}|{e.Message}\r\n{e.StackTrace}");
            }
            finally
            {
                logger.Info($"{iMark}|End.");
            }
        }

        private void StartSnoopTargetsSearch()
        {
            CaptureMouse();
            IsDragging = true;
            if (this.Current_tool_func == Mars_spy_tool_function._find_obj)
            {
                Cursor = FinderCursor;
                MarsCrosshairsImage.Visibility = Visibility.Hidden;
            }
            else if (this.Current_tool_func == Mars_spy_tool_function._auto_gen_test_step)
            {
                Cursor = this.GenStepsCursor;
                MARS_GENERATE_TESTCASE.Visibility = Visibility.Hidden;
            }else
            {
                Cursor = this.RecordAndReplayCursor;
                MARS_RECORD_REPLAY.Visibility = Visibility.Hidden;
            }
            
            
            //_windowUnderCursor = null;
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((e.LeftButton == MouseButtonState.Released) && IsDragging)
            {
                Mousecapture.UnHookMouse();
                StopSnoopTargetsSearch();
            }            
        }

        private void StopSnoopTargetsSearch()
        {
            //logger.Info("StopSnoopTargetsSearch\tbegin");
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
            MarsCrosshairsImage.Visibility = Visibility.Visible;
            MARS_GENERATE_TESTCASE.Visibility = Visibility.Visible;
            MARS_RECORD_REPLAY.Visibility = Visibility.Visible;
            RemoveVisualFeedback();
            //logger.Info("StopSnoopTargetsSearch\tend");
        }

        private void RemoveVisualFeedback()
        {
            //throw new NotImplementedException();
            XPos.Content = "";
            YPos.Content = "";
        }

        private void UpdateFeedbackWindowPosition()
        {
            //if (_feedbackWindow != null)
            //{
            var mouse = GetCursorPosition();
            this.XPos.Content = mouse.X - 34;//.Left;
            this.YPos.Content = mouse.Y + 10; // windowRect.Top;
            //}
        }

        public static System.Drawing.Point GetCursorPosition()
        {
            var pos = new System.Drawing.Point();
            var win32Point = new windowsWrapper.SystemUtil.POINT();
            if (windowsWrapper.SystemUtil.MarsWindowsAPIs.GetCursorPos(ref win32Point))
            {
                pos.X = win32Point.X;
                pos.Y = win32Point.Y;
            }
            return pos;
        }


        private IntPtr currentWndHandle = IntPtr.Zero;
        private IntPtr currentRgn = IntPtr.Zero;
        private System.Drawing.Rectangle drawingRect = default(System.Drawing.Rectangle);

        //private IntPtr GetTargetRegion(Rectangle rect)
        //{
        //    IntPtr hrgn, hdc;
        //    hdc = MarsWindowsAPIs.GetDC(IntPtr.Zero);
        //    hrgn = create
        //}
        private void EreaseOldRect()
        {
            //IntPtr hdc = MarsWindowsAPIs.GetDC(IntPtr.Zero);
            try
            {
                MarsWindowsAPIs.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            }
            catch (Exception e)
            {

            }
            finally
            {
                //MarsWindowsAPIs.ReleaseDC(IntPtr.Zero, hdc);
            }
        }
        private void DrawCurrentHighlighted()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);

            g.DrawRectangle(new Pen(System.Drawing.Brushes.Chocolate), drawingRect);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            
            // Stop tracking before closing
            StopTracking();
            
            Mousecapture.UnHookMouse();
            
            HookHandler = IntPtr.Zero;
            e.Cancel = false;
            
            // Unregister hotkeys
            UnregisterGlobalHotkeys();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //Create hook
            //if (HookHandler == IntPtr.Zero)
            //    HookHandler = Mousecapture.SetHook(mouseMoveHookImple,
            //        mouseLeftButtonPressImp,
            //        mouseLeftButtonUpImp);
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {

        }

        private void OnRecordReplayChecked(object sender, RoutedEventArgs e)
        {            
            //IsRecordAndReplayMode = this.IsRecordReplay.IsChecked??false;            
        }
        #region mars database info
        private string[] databaseNameList;
        public string[] DatabaseNameList {
            get=>databaseNameList; 
            set
            {
                databaseNameList = value;
                OnPropertyChanged(nameof(DatabaseNameList));
            }
        }

        private bool isSingleObjectMode = true;
        public bool IsSingleObjectMode
        {
            get => isSingleObjectMode;
            set
            {
                if (isSingleObjectMode == value) return;
                isSingleObjectMode = value;
                OnPropertyChanged(nameof(IsSingleObjectMode));
            }
        }

        //private string currentMarsDBIdx;
        public string CurrentMarsDBIdx
        {
            get => MarsTestAPPDBInfo.currentDBIdx;
            set
            {
                if (MarsTestAPPDBInfo.currentDBIdx == value) return;
                MarsTestAPPDBInfo.currentDBIdx = value;
                OnPropertyChanged(nameof(CurrentMarsDBIdx));
            }
        }
        #endregion

        #region status info
        private List<B_REGISTERED_APPS> currentApplications;
        public List<B_REGISTERED_APPS> CurrentApplications
        {
            get =>currentApplications;
            set
            {
                if (currentApplications==value) return;
                currentApplications = value;
                OnPropertyChanged(nameof(CurrentApplications));
            }
        }

        private string currentStatus = "Wait for steps coming....";
        public string CurrentStatus
        {
            get =>currentStatus;
            set
            {
                if (currentStatus==value) return;
                currentStatus = value;                
                OnPropertyChanged(nameof(CurrentStatus));
            }
        }

        private System.Windows.Media.Brush statusForeGroundColor;
        public System.Windows.Media.Brush StatusForeGroundColor
        {
            get => statusForeGroundColor;
            set
            {
                if (statusForeGroundColor==value) return;
                statusForeGroundColor = value;
                OnPropertyChanged(nameof(StatusForeGroundColor));
            }
        }

        private ObservableCollection<MarsSpyApplication> applicationListInfo=new ObservableCollection<MarsSpyApplication>();
        public ObservableCollection<MarsSpyApplication> ApplicationListInfo
        {
            get => applicationListInfo;
            set
            {
                if (applicationListInfo==value) return; 
                applicationListInfo = value;
                OnPropertyChanged(nameof(ApplicationListInfo));
            }
        }

        private MarsSpyApplication currentApplication;
        public MarsSpyApplication CurrentApplication
        {
            get { return currentApplication; }
            set
            {
                if (currentApplication==value) return;
                currentApplication = value;
                OnPropertyChanged(nameof(CurrentApplication));
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Info($"Window_Loaded begin");
            try
            {            
                /// switch config file
                /// then get restful api pos
                /// 
                string strError = "";
                MarsConfigManagement cfgMgr = new MarsConfigManagement();
                bool                   isOk = cfgMgr.switch2EngineMainCfg(ref strError);
                if (!isOk) return;
                logger.Info($"Window_Loaded\tgeLoadFromConfigFile(\"engineObjectsManager. ");
                if (ObjectEngineConfigFileManagement.LoadFromConfigFile("engineObjectsManager.json", ref strError, ref isOk) == null)
                {
                    logger.Error($"Window_Loaded\t{strError}");
                    MessageBox.Show("can't load Object control file, please make sure that file exists under config");
                }
                logger.Info($"Window_Loaded\tgeLoadFromConfigFile(\"engineObjectsManager. done");
                // create restful api 
                string strBaseURL   = cfgMgr.getRESTfulServerBase();
                SystemGlobalHelper.g_mars_webREST_baseURL = strBaseURL;
                logger.Info($"Window_Loaded\tget baseurl|{strBaseURL}");
                MarsRESTfulApiClient.setWebURLPrefix(strBaseURL);            
               
                // get all dbs
                DatabaseNameList = MarsRESTfulApiClient.GetAllDBIds(ref isOk, ref strError);
                if ((DatabaseNameList!=null) && (DatabaseNameList.Length > 0))
                {
                    this.CurrentMarsDBIdx = DatabaseNameList[0];
                    SystemGlobalHelper.g_currentDB_Idx = CurrentMarsDBIdx;
                }
                logger.Info($"Window_Loaded\tget dbIds|{DatabaseNameList}");

                KeywordsForSpyer.InitKeyword();

                // 加载 singleObjectMode 的配置
                logger.Info("Window_Loaded\tLoading singleObjectMode config");
                bool loadedSingleObjectMode = cfgMgr.LoadSingleObjectMode(ref strError);
                IsSingleObjectMode = loadedSingleObjectMode;
                logger.Info($"Window_Loaded\tLoaded singleObjectMode|{IsSingleObjectMode}");
                
                // Initialize tracking checkbox enabled state
                trackingCheckBox.IsEnabled = IsSingleObjectMode;

                // Register global hotkeys after handle is ready
                RegisterGlobalHotkeys();
            }
            finally
            {
                logger.Info("Window_Loaded\tend");
            }
        }
        private void OnMarsDBIdsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.CurrentMarsDBIdx)) return;

            MarsRESTfulApiClient client = new MarsRESTfulApiClient(this.CurrentMarsDBIdx);
            bool isOk = false;
            string strError = "", strStack="";
            RESTfullReturnApplicationObjects marsApps = client.navigateApplications(this.CurrentMarsDBIdx, ref isOk, ref strError, ref strStack);
            if ((!isOk)
                ||(marsApps==null)
                ||(marsApps.AssignedObjects==null)
                ||(marsApps.AssignedObjects.Count()==0))
            {
                // write to status
                logger.Error("OnMarsDBIdsSelectionChanged", CurrentStatus = $"can't fetch applications from MARS server|{strError}");
                StatusForeGroundColor = System.Windows.Media.Brushes.Red;
                return;
            }
            CurrentApplications = marsApps.AssignedObjects
                .OrderBy(p=>p.APP_SHORT_NAME)
                .ToList();
            ApplicationListInfo = new ObservableCollection<MarsSpyApplication>();
            foreach(var appItm in CurrentApplications)
            {
                if (appItm == null) continue;
                ApplicationListInfo.Add(new MarsSpyApplication()
                {
                    ApplicationName     = appItm.APP_SHORT_NAME,
                    ApplicationProcess  = appItm.PROCESS_IDENTIFIER,
                    ApplicationId       = appItm.APPLICATION_ID
                }) ;
                
            }
            if (ApplicationListInfo.Count > 0)
            {
                CurrentApplication = applicationListInfo[0];
            }
        }

        private void singleObjectMode_checked(object sender, RoutedEventArgs e)
        {
            logger.Info("singleObjectMode_checked\tbegin");
            try
            {
                string strError = "";
                MarsConfigManagement cfgMgr = new MarsConfigManagement();
                bool isChecked = singleObjectMode.IsChecked ?? false;
                bool isOk = cfgMgr.SaveSingleObjectMode(isChecked, ref strError);
                if (!isOk)
                {
                    logger.Error($"singleObjectMode_checked\tFailed to save config|{strError}");
                }
                else
                {
                    logger.Info($"singleObjectMode_checked\tSaved successfully|{isChecked}");
                }
                // Enable/disable tracking checkbox based on singleObjectMode
                trackingCheckBox.IsEnabled = isChecked;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"singleObjectMode_checked\tException|{ex.Message}");
            }
            finally
            {
                logger.Info("singleObjectMode_checked\tend");
            }
        }

        private void TopMostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            logger.Info("TopMostCheckBox_Unchecked\tbegin");
            try
            {
                string strError = "";
                MarsConfigManagement cfgMgr = new MarsConfigManagement();
                bool isChecked = singleObjectMode.IsChecked ?? false;
                bool isOk = cfgMgr.SaveSingleObjectMode(isChecked, ref strError);
                if (!isOk)
                {
                    logger.Error($"TopMostCheckBox_Unchecked\tFailed to save config|{strError}");
                }
                else
                {
                    logger.Info($"TopMostCheckBox_Unchecked\tSaved successfully|{isChecked}");
                }
                // Enable/disable tracking checkbox based on singleObjectMode
                trackingCheckBox.IsEnabled = isChecked;
                if (!isChecked)
                {
                    trackingCheckBox.IsChecked = false;
                    StopTracking();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"TopMostCheckBox_Unchecked\tException|{ex.Message}");
            }
            finally
            {
                logger.Info("TopMostCheckBox_Unchecked\tend");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+F2 to toggle tracking or activate info window
            if (e.Key == Key.F2 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (uiaInfoWindow != null && uiaInfoWindow.IsVisible 
                    // && !uiaInfoWindow.IsActivated
                    )
                {
                    // Activate the info window and copy to clipboard
                    uiaInfoWindow.ActivateWindow();
                    Clipboard.SetText(uiaInfoWindow.GetObjectInfo());
                    e.Handled = true;
                }
                else if (trackingCheckBox.IsEnabled)
                {
                    trackingCheckBox.IsChecked = !trackingCheckBox.IsChecked;
                    e.Handled = true;
                }
            }
        }

        private void TrackingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            logger.Info("TrackingCheckBox_Checked\tbegin");
            StartTracking();
        }

        private void TrackingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            logger.Info("TrackingCheckBox_Unchecked\tbegin");
            StopTracking();
        }

        private void StartTracking()
        {
            logger.Info("StartTracking\tbegin");
            // Only set hook if no other hook is active
            if (HookHandler == IntPtr.Zero && trackingHookHandler == IntPtr.Zero)
            {
                trackingHookHandler = Mousecapture.SetHook(
                    trackingMouseMoveHookImpl,
                    null,
                    null);
                logger.Info($"StartTracking\tHook set, handler={trackingHookHandler}");
            }
            else
            {
                logger.Info($"StartTracking\tHook already active, HookHandler={HookHandler}, trackingHookHandler={trackingHookHandler}");
            }
            
            if (trackingTimer == null)
            {
                trackingTimer = new System.Windows.Threading.DispatcherTimer();
                trackingTimer.Interval = TimeSpan.FromMilliseconds(1000);
                trackingTimer.Tick += TrackingTimer_Tick;
            }
            trackingTimer.Start();
            lastMousePosition = new System.Drawing.Point(-1, -1);
            lastMousePositionTime = DateTime.MinValue;
            logger.Info("StartTracking\tTracking started");
        }

        private void StopTracking()
        {
            logger.Info("StopTracking\tbegin");
            if (trackingTimer != null)
            {
                trackingTimer.Stop();
            }
            
            // Only unhook if we set the hook (and no other hook is active)
            if (trackingHookHandler != IntPtr.Zero && HookHandler == IntPtr.Zero)
            {
                Mousecapture.UnHookMouse();
                trackingHookHandler = IntPtr.Zero;
            }
            
            if (uiaInfoWindow != null)
            {
                uiaInfoWindow.Close();
                uiaInfoWindow = null;
            }
            
            lastMousePosition = new System.Drawing.Point(-1, -1);
            lastMousePositionTime = DateTime.MinValue;
        }

        private void trackingMouseMoveHookImpl(int x, int y)
        {
            // Ensure we're on UI thread for window operations
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(new Action<int, int>(trackingMouseMoveHookImpl), x, y);
                return;
            }
            
            // Only process if tracking is enabled
            if (trackingCheckBox == null || trackingCheckBox.IsChecked != true || !trackingCheckBox.IsEnabled)
            {
                return;
            }
            
            var currentPos = new System.Drawing.Point(x, y);
            var currentTime = DateTime.Now;
            
            // Check if mouse has moved significantly (use smaller threshold, e.g., 3 pixels)
            if (lastMousePosition.X >= 0 && lastMousePosition.Y >= 0)
            {
                int deltaX = Math.Abs(currentPos.X - lastMousePosition.X);
                int deltaY = Math.Abs(currentPos.Y - lastMousePosition.Y);
                
                if (deltaX > 3 || deltaY > 3)
                {
                    // Mouse moved significantly, reset timer
                    logger.Debug($"trackingMouseMoveHookImpl\tMouse moved from ({lastMousePosition.X}, {lastMousePosition.Y}) to ({x}, {y}), delta=({deltaX}, {deltaY})");
                    lastMousePosition = currentPos;
                    lastMousePositionTime = currentTime;
                    // Hide highlight window if shown
                    HighlightWindow.HideAndDestroy();
                    // Reset if info window already shown
                    if (uiaInfoWindow != null && uiaInfoWindow.IsVisible)
                    {
                        uiaInfoWindow.Close();
                        uiaInfoWindow = null;
                    }
                    return;
                }
                
                // Mouse hasn't moved much, check if it has stayed for 3 seconds
                var timeSinceStopped = (currentTime - lastMousePositionTime).TotalSeconds;
                logger.Debug($"trackingMouseMoveHookImpl\tMouse at ({x}, {y}), stayed for {timeSinceStopped:F2} seconds");
                
                if (timeSinceStopped >= 3.0 && (uiaInfoWindow == null || !uiaInfoWindow.IsVisible))
                {
                    logger.Info($"trackingMouseMoveHookImpl\tMouse stayed at ({x}, {y}) for {timeSinceStopped:F2} seconds, showing window and highlight");
                    // Show highlight window first
                    ShowHighlightWindow(x, y);
                    // Show UIA object info window
                    ShowUIAObjectInfo(x, y);
                    // Reset position to prevent repeated triggering
                    lastMousePosition = new System.Drawing.Point(-1, -1);
                    lastMousePositionTime = DateTime.MinValue;
                }
            }
            else
            {
                // First time or after reset, initialize position
                lastMousePosition = currentPos;
                lastMousePositionTime = currentTime;
                logger.Debug($"trackingMouseMoveHookImpl\tInitializing position at ({x}, {y})");
            }
        }

        private void TrackingTimer_Tick(object sender, EventArgs e)
        {
            // Timer is used to periodically check mouse position
            // The actual tracking is done in the mouse hook callback
            // Also use timer to verify hook is working and trigger check if needed
            if (trackingCheckBox.IsChecked == true && trackingHookHandler != IntPtr.Zero)
            {
                var currentMouse = GetCursorPosition();
                if (lastMousePosition.X >= 0 && lastMousePosition.Y >= 0)
                {
                    var timeSinceStopped = (DateTime.Now - lastMousePositionTime).TotalSeconds;
                    logger.Debug($"TrackingTimer_Tick\tMouse at ({currentMouse.X}, {currentMouse.Y}), Last tracked at ({lastMousePosition.X}, {lastMousePosition.Y}), stayed for {timeSinceStopped:F2}s");
                    
                    // If hook callback didn't trigger, manually check here as fallback
                    if (timeSinceStopped >= 3.0 && (uiaInfoWindow == null || !uiaInfoWindow.IsVisible))
                    {
                        logger.Info($"TrackingTimer_Tick\tFallback: Mouse stayed for {timeSinceStopped:F2} seconds, triggering show");
                        ShowHighlightWindow(currentMouse.X, currentMouse.Y);
                        ShowUIAObjectInfo(currentMouse.X, currentMouse.Y);
                        lastMousePosition = new System.Drawing.Point(-1, -1);
                        lastMousePositionTime = DateTime.MinValue;
                    }
                }
                else
                {
                    logger.Debug($"TrackingTimer_Tick\tMouse at ({currentMouse.X}, {currentMouse.Y}), waiting for position lock");
                }
            }
        }

        private void ShowHighlightWindow(int x, int y)
        {
            try
            {
                logger.Info($"ShowHighlightWindow\tbegin|x={x}, y={y}");
                
                // Get window handle at position
                IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                if (hwnd == IntPtr.Zero)
                {
                    logger.Warn("ShowHighlightWindow\thwnd is zero");
                    return;
                }
                
                logger.Info($"ShowHighlightWindow\tFound hwnd: 0x{hwnd.ToInt64():X}");
                
                // Get window rect - this returns screen coordinates
                MarsWindowsAPIs.RECT rect;
                if (!MarsWindowsAPIs.GetWindowRect(hwnd, out rect))
                {
                    logger.Warn("ShowHighlightWindow\tGetWindowRect failed");
                    return;
                }
                
                logger.Info($"ShowHighlightWindow\tWindow rect: Left={rect.Left}, Top={rect.Top}, Right={rect.Right}, Bottom={rect.Bottom}");
                
                // Calculate window dimensions
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                
                // Show highlight window - Form coordinates are in screen pixels
                var highlightWindow = Mars.message.Utility.visualObjects.objectSpyer.HighlightWindow.getInstance();
                highlightWindow.Hide();
                
                // Ensure window is set to manual positioning
                highlightWindow.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                highlightWindow.Left = rect.Left - 1;
                highlightWindow.Top = rect.Top - 1;
                highlightWindow.Width = width + 2;
                highlightWindow.Height = height + 2;
                highlightWindow.TopMost = true; // Ensure it's on top
                highlightWindow.Show();
                
                logger.Info($"ShowHighlightWindow\tHighlight shown at screen position ({rect.Left - 1}, {rect.Top - 1}), size ({width + 2}, {height + 2})");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ShowHighlightWindow\tException|{ex.Message}");
            }
        }

        private void ShowUIAObjectInfo(int x, int y)
        {
            try
            {
                logger.Info($"ShowUIAObjectInfo\tbegin|x={x}, y={y}");
                
                // Ensure we're on UI thread
                if (!this.Dispatcher.CheckAccess())
                {
                    this.Dispatcher.BeginInvoke(new Action<int, int>(ShowUIAObjectInfo), x, y);
                    return;
                }
                
                // Get window handle at position
                IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                if (hwnd == IntPtr.Zero)
                {
                    logger.Warn("ShowUIAObjectInfo\thwnd is zero");
                    return;
                }
                MarsSpiedObjectInfo spiedObject = MarsObjSpyForm.GetUIAInfoFromHwnd(hwnd);
                AutomationElement automationElement = null;

                if (spiedObject == null)
                {
                    logger.Warn("ShowUIAObjectInfo\tFailed to get UIA object info, attempting AutomationElement fallback");
                    automationElement = TryGetAutomationElement(x, y, hwnd);
                    if (automationElement == null)
                    {
                        logger.Warn("ShowUIAObjectInfo\tAutomationElement fallback failed");
                    }
                }
                else
                {
                    automationElement = TryGetAutomationElement(x, y, hwnd);
                }

                // Get process info
                int pid;
                if (MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out pid) == 0)
                {
                    logger.Warn("ShowUIAObjectInfo\tGetWindowThreadProcessId failed");
                    return;
                }
                
                Process p = Process.GetProcessById(pid);
                // Allow showing info even for our own process, but log it
                if (p.Id == Process.GetCurrentProcess().Id)
                {
                    logger.Debug("ShowUIAObjectInfo\tShowing info for our own process");
                }
                
                
                // Get UIA object info - pass the spied object if available
                int? automationLevel = null;
                if (automationElement != null)
                {
                    automationLevel = GetAutomationElementLevel(automationElement);
                    if (automationLevel.HasValue)
                    {
                        logger.Info($"ShowUIAObjectInfo\tAutomationElement UIA level={automationLevel.Value}");
                    }
                    else
                    {
                        logger.Debug("ShowUIAObjectInfo\tAutomationElement level unavailable");
                    }
                }

                string objectInfo = GetUIAObjectInfoString(p.ProcessName, pid, hwnd, x, y, spiedObject, automationElement, automationLevel);
                
                // Close existing window if any
                if (uiaInfoWindow != null)
                {
                    try
                    {
                        uiaInfoWindow.Close();
                    }
                    catch { }
                    uiaInfoWindow = null;
                }
                
                // Create new info window
                uiaInfoWindow = new subToolWindows.hintWindows.UIAObjectInfoWindow();
                uiaInfoWindow.SetObjectInfo(objectInfo);
                
                // Position window below main window
                uiaInfoWindow.Left = this.Left;
                uiaInfoWindow.Top = this.Top + this.Height + 5;
                
                // Make sure window is visible
                uiaInfoWindow.Show();
                uiaInfoWindow.Activate();
                logger.Info($"ShowUIAObjectInfo\tWindow shown at ({uiaInfoWindow.Left}, {uiaInfoWindow.Top})");
                
                // Automatically copy to clipboard when window is shown
                try
                {
                    Clipboard.SetText(objectInfo);
                    logger.Info("ShowUIAObjectInfo\tContent copied to clipboard");
                }
                catch (Exception clipEx)
                {
                    logger.Warn($"ShowUIAObjectInfo\tFailed to copy to clipboard: {clipEx.Message}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ShowUIAObjectInfo\tException|{ex.Message}");
            }
        }

        private string GetUIAObjectInfoString(string processName, int pid, IntPtr hwnd, int x, int y, MarsSpiedObjectInfo spiedObject = null, AutomationElement automationElement = null, int? automationLevel = null)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine($"Process: {processName}");
                sb.AppendLine($"PID: {pid}");
                sb.AppendLine($"HWND: 0x{hwnd.ToInt64():X}");
                sb.AppendLine($"Position: ({x}, {y})");
                sb.AppendLine();
                
                // Get window title
                StringBuilder windowTitle = new StringBuilder(256);
                int titleLength = GetWindowText(hwnd, windowTitle, windowTitle.Capacity);
                if (titleLength > 0)
                {
                    sb.AppendLine($"Window Title: {windowTitle}");
                }
                
                // Get window class
                StringBuilder className = new StringBuilder(256);
                int classLength = GetClassName(hwnd, className, className.Capacity);
                if (classLength > 0)
                {
                    sb.AppendLine($"Window Class: {className}");
                }
                
                // Get window rect
                MarsWindowsAPIs.RECT rect;
                if (MarsWindowsAPIs.GetWindowRect(hwnd, out rect))
                {
                    sb.AppendLine($"Window Rect: ({rect.Left}, {rect.Top}, {rect.Right}, {rect.Bottom})");
                    sb.AppendLine($"Window Size: {rect.Right - rect.Left} x {rect.Bottom - rect.Top}");
                }

                //sb.AppendLine();
                
                // Display MarsSpiedObjectInfo if available
                if (spiedObject != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== MarsSpiedObjectInfo ===");
                    FormatMarsSpiedObjectInfo(sb, spiedObject);
                }

                if (automationElement != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== AutomationElement Details ===");
                    FormatAutomationElementInfo(sb, automationElement, automationLevel);
                }
                else if (spiedObject == null)
                {
                    sb.AppendLine();
                    sb.AppendLine("No detailed UIA information available for this element.");
                }
                
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetUIAObjectInfoString\tException|{ex.Message}");
                sb.AppendLine($"Error getting object info: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private void FormatMarsSpiedObjectInfo(StringBuilder sb, MarsSpiedObjectInfo obj)
        {
            if (obj == null) return;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };
                // Serialize full object graph for readability
                string json = JsonSerializer.Serialize(obj, jsonOptions);
                sb.AppendLine(json);
            }
            catch (Exception ex)
            {
                // Fallback: reflect properties
                sb.AppendLine($"(Failed to serialize MarsSpiedObjectInfo: {ex.Message})");
                try
                {
                    var props = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    foreach (var p in props)
                    {
                        object val = null;
                        try { val = p.GetValue(obj); } catch {}
                        sb.AppendLine($"{p.Name}: {val}");
                    }
                }
                catch { }
            }
        }
        
        private AutomationElement TryGetAutomationElement(int x, int y, IntPtr hwnd)
        {
            try
            {
                var point = new System.Windows.Point(x, y);
                var element = AutomationElement.FromPoint(point);
                if (element != null)
                {
                    return element;
                }
            }
            catch (ElementNotAvailableException ex)
            {
                logger.Warn(ex, $"TryGetAutomationElement\tElement not available at point ({x}, {y})");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"TryGetAutomationElement\tException querying point ({x}, {y})|{ex.Message}");
            }

            if (hwnd != IntPtr.Zero)
            {
                try
                {
                    var element = AutomationElement.FromHandle(hwnd);
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch (ElementNotAvailableException ex)
                {
                    logger.Warn(ex, $"TryGetAutomationElement\tElement not available for hwnd 0x{hwnd.ToInt64():X}");
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"TryGetAutomationElement\tException for hwnd 0x{hwnd.ToInt64():X}|{ex.Message}");
                }
            }

            return null;
        }

        private void FormatAutomationElementInfo(StringBuilder sb, AutomationElement element, int? automationLevel)
        {
            if (element == null) return;
            try
            {
                var current = element.Current;
                if (automationLevel.HasValue)
                {
                    sb.AppendLine($"UIA Level: {automationLevel.Value}");
                }
                sb.AppendLine($"Name: {current.Name}");
                sb.AppendLine($"AutomationId: {current.AutomationId}");
                sb.AppendLine($"ControlType: {current.ControlType?.ProgrammaticName}");
                sb.AppendLine($"LocalizedControlType: {current.LocalizedControlType}");
                sb.AppendLine($"FrameworkId: {current.FrameworkId}");
                sb.AppendLine($"ClassName: {current.ClassName}");
                sb.AppendLine($"BoundingRectangle: {current.BoundingRectangle}");
                sb.AppendLine($"IsEnabled: {current.IsEnabled}");
                sb.AppendLine($"IsOffscreen: {current.IsOffscreen}");
                sb.AppendLine($"NativeWindowHandle: 0x{current.NativeWindowHandle:X}");

                sb.AppendLine();
                sb.AppendLine("Supported Patterns:");
                AutomationPattern[] patterns = null;
                try
                {
                    patterns = element.GetSupportedPatterns();
                }
                catch (ElementNotAvailableException ex)
                {
                    logger.Warn(ex, "FormatAutomationElementInfo\tSupported pattern query failed");
                }

                if (patterns != null && patterns.Length > 0)
                {
                    foreach (var pattern in patterns)
                    {
                        sb.AppendLine($"- {pattern.ProgrammaticName}");
                    }
                }
                else
                {
                    sb.AppendLine("- (none)");
                }
            }
            catch (ElementNotAvailableException ex)
            {
                logger.Warn(ex, "FormatAutomationElementInfo\tAutomationElement no longer available");
                sb.AppendLine("AutomationElement is no longer available.");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"FormatAutomationElementInfo\tException|{ex.Message}");
                sb.AppendLine($"Error retrieving AutomationElement info: {ex.Message}");
            }
        }

        private int? GetAutomationElementLevel(AutomationElement element, int maxDepth = 512)
        {
            if (element == null) return null;
            try
            {
                var walker = TreeWalker.RawViewWalker;
                var current = element;
                int level = 0;
                while (current != null && level < maxDepth)
                {
                    level++;
                    current = walker.GetParent(current);
                }

                if (level >= maxDepth)
                {
                    logger.Warn($"GetAutomationElementLevel\tExceeded maxDepth {maxDepth}, returning null");
                    return null;
                }

                return level;
            }
            catch (ElementNotAvailableException ex)
            {
                logger.Warn(ex, "GetAutomationElementLevel\tAutomationElement no longer available");
                return null;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"GetAutomationElementLevel\tException|{ex.Message}");
                return null;
            }
        }
        
        private MarsSpiedObjectBasicInfo GetMarsSpiedObjectInfo(string processName, int pid, IntPtr hwnd, int x, int y)
        {
            try
            {
                // Try to get object info from REST service if available
                bool isSvcRead = IsSvcRead();
                if (isSvcRead)
                {
                    logger.Info("GetMarsSpiedObjectInfo\tService is available, querying objects");
                    QueryObjectRequst req = new QueryObjectRequst();
                    req.currentHandle = hwnd.ToInt64();
                    req.x = x;
                    req.y = y;
                    req.typeOfGenerateSteps = 0;
                    
                    bool isOk = false;
                    string strError = "";
                    List<MarsSpiedObjectBasicInfo> objs = RESTClient2MessageCenter
                        .getInstance()
                        .QueryCurrentObjects(req, ref isOk, ref strError);
                    
                    if (isOk && objs != null && objs.Count > 0)
                    {
                        // Return the first object (usually the one at the specified position)
                        logger.Info($"GetMarsSpiedObjectInfo\tFound {objs.Count} objects, returning first one");
                        return objs[0];
                    }
                    else
                    {
                        logger.Debug($"GetMarsSpiedObjectInfo\tFailed to query objects: {strError}");
                    }
                }
                else
                {
                    logger.Debug("GetMarsSpiedObjectInfo\tService not available, creating basic object info");
                }
                
                // If service not available or query failed, create a basic object from window info
                MarsSpiedObjectBasicInfo basicObj = new MarsSpiedObjectBasicInfo();
                basicObj.hwnd = hwnd.ToInt64();
                basicObj.x = x;
                basicObj.y = y;
                
                // Get window rect for size
                MarsWindowsAPIs.RECT rect;
                if (MarsWindowsAPIs.GetWindowRect(hwnd, out rect))
                {
                    basicObj.w = rect.Right - rect.Left;
                    basicObj.h = rect.Bottom - rect.Top;
                }
                
                // Get window title as object name
                StringBuilder windowTitle = new StringBuilder(256);
                if (GetWindowText(hwnd, windowTitle, windowTitle.Capacity) > 0)
                {
                    basicObj.objectName = windowTitle.ToString();
                    basicObj.Text = windowTitle.ToString();
                }
                
                // Get window class as object type
                StringBuilder className = new StringBuilder(256);
                if (GetClassName(hwnd, className, className.Capacity) > 0)
                {
                    basicObj.objectType = className.ToString();
                }
                
                basicObj.isVisible = MarsWindowsAPIs.IsWindowVisible(hwnd);
                basicObj.index = 0;
                
                return basicObj;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetMarsSpiedObjectInfo\tException|{ex.Message}");
                return null;
            }
        }

        private string GetAccessibleObjectInfo(IntPtr hwnd, int x, int y)
        {
            // This is a simplified version - in a real implementation,
            // you would use UIAutomation API or IAccessible COM interface
            // to get detailed object properties
            StringBuilder sb = new StringBuilder();
            
            try
            {
                // For now, just return basic info
                // In a full implementation, you would:
                // 1. Use UIAutomationClient to get AutomationElement from point
                // 2. Get all properties like Name, ControlType, AutomationId, etc.
                // 3. Get the element's pattern information
                
                sb.AppendLine("(Accessible object details would be shown here)");
                sb.AppendLine("Note: Full UIA implementation requires UIAutomationClient references");
            }
            catch (Exception ex)
            {
                logger.Debug($"GetAccessibleObjectInfo\tException|{ex.Message}");
            }
            
            return sb.ToString();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    }
}
