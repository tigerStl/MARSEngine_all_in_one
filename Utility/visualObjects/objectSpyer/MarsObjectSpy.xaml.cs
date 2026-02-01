using Mars.windowsWrapper.SystemUtil;
using MarsTestFrame.SourceCode.xmlConfig;
using Route2NSEx.src.Marquis.systemUtil;
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
using System.Windows.Shapes;
using Pen = System.Drawing.Pen;

namespace Mars.Utility.visualObjects.objectSpyer
{
    


    

    /// <summary>
    /// Interaction logic for MarsObjectSpy.xaml
    /// </summary>
    public partial class MarsObjectSpy : Window
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsObjectSpy));

        private static MarsObjectSpy SpyInstance = null;
        private IntPtr HookHandler = IntPtr.Zero;
        private MarsObjectSpy()
        {
            InitializeComponent();

            FinderCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Mars.Resources.CrosshairsCursor.cur"));

            FinderButton.PreviewMouseLeftButtonDown += Button_PreviewMouseLeftButtonDown;
            //FinderButton.MouseMove += Button_MouseMove;
            //FinderButton.MouseLeftButtonUp += Button_MouseUp;
        }

        
        private Mouse_ObjectPosManagement CurrentObjectStays = new Mouse_ObjectPosManagement();
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
            if (CurrentObjectStays.PreviousTimeTickStopped==0)
            {
                CurrentObjectStays.PreviousTimeTickStopped = DateTime.Now.Ticks;
                return ;
            }
            CurrentObjectStays.HighlightObjectAtMousePosition(x, y,false);
            /// 判断停留时间
            /// 
            long lCurDis = DateTime.Now.Ticks - CurrentObjectStays.PreviousTimeTickStopped;
            if ((lCurDis/ 10000) > 500) // greater than 500 ms, then get current 
            {
                
                CurrentObjectStays.PreviousTimeTickStopped = lCurDis;
                
                //绘制当前的window
            }
          
        }

        public void mouseLeftButtonPressImp(int x, int y)
        {

        }

        private bool IsMarsAgentInjected(List<MarsWindowsAPIsExtend.Module> lstModule)
        {
            if (lstModule==null) 
                return false;
            var agnt = (from a in lstModule
                       where (!string.IsNullOrEmpty(a.ModuleName))
                       && (a.ModuleName.ToUpper().IndexOf("MANAGEDINJECTOR") >= 0
                       || a.ModuleName.ToUpper().IndexOf("MARS.MSMQHOST")>=0 
                       || a.ModuleName.ToUpper().IndexOf("MARS.INTER.MARCENTER") >= 0)
                        select a).FirstOrDefault();
            
            return agnt != null;

        }

        public void mouseLeftButtonUpImp(int x, int y)
        {
            if (!IsDragging) return;

            HighlightWindow.HideAndDestroy();

            StopSnoopTargetsSearch();
            IsDragging = false;
            //算法：
            //获得当前位置的windows
            IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
            //获得进程
            int pid;
            bool is64=false;
            if (MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out pid) == 0) return;
            Process p = Process.GetProcessById(pid);
            is64 = !MarsWindowsAPIsExtend.IsProcess32(p.Handle);
            //MarsWindowsAPIs.IsWow64Process(p.Handle, out is64);
            logger.Info("\t", $"It is 64? {is64}");

            bool is64_2 = MarsWindowsAPIsExtend.IsWin64Emulator(p.Handle);
            if (Process.GetCurrentProcess().Id == pid) return;
            // 通过列举所有的模块，判断是否是.net 或者java等程序，QT等获得qtcore的版本
            var itms = MarsWindowsAPIsExtend.CollectModules(pid);
            foreach(var itm in itms)
            {
                if (itm == null) continue;
                Console.WriteLine($"modual1 name:{itm.ModuleName}, path:{itm.ModulePath} processIs64?:{is64} - {is64_2}");
                logger.Info("\t", ($"modual1 name:{itm.ModuleName}, path:{itm.ModulePath} processIs64?:{is64} - {is64_2}"));
            }
            string strVer = "";
            MARSSupportedProcessType supportType = MarsProcessModule.GetTargetTypeFromProcessModule(itms, ref strVer);
            if (supportType == MARSSupportedProcessType.Mars_noneSupport)
            {
                MessageBox.Show($"该目标程序[{p.ProcessName}] is not one of the application:Java/.net/QT");
                return;
            }
            
            // 判断是否存在Mars的injector，如果存在，发送一个mqmessage，激活，否则injector
            bool isMarsAgentInjected = IsMarsAgentInjected(itms);
            
            logger.Info("object spy all modules", string.Join(",", itms.Select(z => z.ModuleName).ToArray()));
            //直接通过engne发送启动spy++ 
            string strError = "";
            //先关闭
            KillAgent();
            StartEngineAgent(is64, p.Id, hwnd, isMarsAgentInjected, supportType, ref strError);
            

            //IEnumerable<MarsWindowsAPIs.MODULEENTRY32> lstOfModuals = MarsWindowsAPIsExtend.GetModules(pid);
            //foreach (var itm in lstOfModuals)
            //{
            //    if (itm.Equals(default(MarsWindowsAPIs.MODULEENTRY32))) continue;
            //    Console.WriteLine($"modual name:{itm.szModule}, path:{itm.szExePath} processIs64?:{is64}");
            //}
            //MarsWindowsAPIsExtend.FlashWindowByHandle(hwnd);
            //CurrentObjectStays.HighlightObjectAtMousePosition(x, y);

        }
        private void KillAgent()
        {
            Process[] arrp = Process.GetProcessesByName("Mars.AutoTestingDriver");
            foreach(var itm in arrp)
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
        }

        private bool StartEngineAgent(bool isStart64, int targetPId, IntPtr hwnd, bool isMarsAgentInjected,
            MARSSupportedProcessType supportType,
            ref string strError)
        {
            string strFile = Process.GetCurrentProcess().MainModule.FileName;
            string strPth = System.IO.Path.GetDirectoryName(strFile);
            Assembly currentModuleAssm = Assembly.GetAssembly(typeof(MarsObjectSpy));
            if (currentModuleAssm == null) {
                strError = "Can't find Assembly path, MarsObjectSpy";
                return false;
            }
            UriBuilder uri = new UriBuilder(currentModuleAssm.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            path = System.IO.Path.GetDirectoryName(path);
            string engineFile = System.IO.Path.Combine(path, isStart64? "Mars.AutoTestingDriver.exe": "Mars.AutoTestingDriver32.exe");
            if (!System.IO.File.Exists(engineFile))
            {
                strError = $"can't find engine from [{engineFile}]";
                return false;
            }
            ProcessStartInfo pstart = new ProcessStartInfo();
            pstart.FileName = engineFile;
            pstart.Arguments = $"-spy {targetPId} -targetWnd {hwnd} -user {WCFXmlCfgMgr.CurrentLoginUser} -isRecall {!isMarsAgentInjected} -appType {supportType} ";
            Process.Start(pstart);
            return true;
        }

        public static MarsObjectSpy GetSpyInstance()
        {
            if (SpyInstance==null)
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
        private Cursor FinderCursor = null;
        private Cursor OldCursor = null;

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StartSnoopTargetsSearch();
                e.Handled = true;
            }
        }

        private void StartSnoopTargetsSearch()
        {
            CaptureMouse();
            IsDragging = true;
            Cursor = FinderCursor;
            MarsCrosshairsImage.Visibility = Visibility.Hidden;
            //_windowUnderCursor = null;
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
            if (( e.LeftButton == MouseButtonState.Released) && IsDragging)
            {
                StopSnoopTargetsSearch();
            }
            ///算法
            ///1，获得当前位置的进程

        }

        private void StopSnoopTargetsSearch()
        {
            ReleaseMouseCapture();
            IsDragging = false;
            Cursor = Cursors.Arrow;
            MarsCrosshairsImage.Visibility = Visibility.Visible;
            RemoveVisualFeedback();
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
            }catch(Exception e)
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


        /*
        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            Console.WriteLine($"mouse move:{IsDragging}");
            if (!IsDragging) return;

            // 獲得当前位置的window handle            
            IntPtr tmpCurHdl = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(
                System.Windows.Forms.Cursor.Position.X,
                System.Windows.Forms.Cursor.Position.Y
                ));
            currentWndHandleLbl.Content = $"{tmpCurHdl}/{currentWndHandle}";

            // 獲得windows的size和位置
            MarsWindowsAPIs.RECT lpRect;
            bool isChangeHighlightWnd = MarsWindowsAPIs.GetWindowRect(tmpCurHdl, out lpRect);

            if ((tmpCurHdl != currentWndHandle)
                &&(isChangeHighlightWnd))
            {
                //消除旧的highlight的窗口
                EreaseOldRect();
                drawingRect = new System.Drawing.Rectangle(lpRect.Left-1, lpRect.Top-1, lpRect.Right - lpRect.Left + 2, lpRect.Bottom - lpRect.Top+2);
                DrawCurrentHighlighted();

                //HighlightWindow highLightFrm = HighlightWindow.getInstance();
                //highLightFrm.Hide();
                //highLightFrm.Top = lpRect.Top - 1;
                //highLightFrm.Left = lpRect.Left - 1;
                //highLightFrm.Width = lpRect.Right - lpRect.Left + 1;
                //highLightFrm.Height = lpRect.Bottom - lpRect.Top + 1;
                ////画新的highlight窗口， 使用一个transparent的window
                //highLightFrm.Show();
                //highLightFrm.Refresh();

                //修改当前的handle
                currentWndHandle = tmpCurHdl;
            }

            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                StopSnoopTargetsSearch();
                return;
            }
            UpdateFeedbackWindowPosition();

        }
        */
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            Mousecapture.UnHookMouse();
            HookHandler = IntPtr.Zero;
            e.Cancel = true;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //Create hook
            if (HookHandler==IntPtr.Zero)
                HookHandler = Mousecapture.SetHook(mouseMoveHookImple,
                    mouseLeftButtonPressImp,
                    mouseLeftButtonUpImp);
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {

        }
    }

    internal class Mouse_ObjectPosManagement
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(Mouse_ObjectPosManagement));

        internal IntPtr CurrentHandler=IntPtr.Zero;
        internal IntPtr MainWindowHandleFromPos = IntPtr.Zero;
        internal long PreviousTimeTickStopped=0;

        internal void HighlightObjectAtMousePosition(int x, int y, bool refreshDeskTop = false)
        {
            IntPtr handleFromPos = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));

            if (handleFromPos == IntPtr.Zero) return;
            if (MainWindowHandleFromPos!=handleFromPos)
            {
                /// draw hightlight Frame for the window
                /// 
                MarsWindowsAPIs.RECT lpRect;
                MainWindowHandleFromPos = handleFromPos;
                //if (refreshDeskTop)
                {
                    Console.WriteLine("invalidate");
                    //MarsWindowsAPIs.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);

                }
                bool isOk = MarsWindowsAPIs.GetWindowRect(handleFromPos, out lpRect);
                if (!isOk) return;
                string strError = "";
#if _NOQTP
                HighlightWindow frm = HighlightWindow.getInstance();
                frm.Hide();
                frm.Left = lpRect.Left - 1;
                frm.Top = lpRect.Top - 1;
                frm.Width = lpRect.Right - lpRect.Left + 2;
                frm.Height = lpRect.Bottom - lpRect.Top + 2;
                frm.Show();

                //isOk = XorDrawing.DrawXorRectangleOnDeskTop(lpRect, ref strError,iTimes: refreshDeskTop?3:1, isErease:refreshDeskTop);
                //if (!isOk)
                //{
                //    Logger.Error("\tHighlightObjectAtMousePosition", strError);
                //    return;
                //}
#endif
            }
        }
    }
}
