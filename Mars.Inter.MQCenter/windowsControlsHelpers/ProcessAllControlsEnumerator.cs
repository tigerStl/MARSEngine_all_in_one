using Mars.Inter.MQCenter.MSAASupport;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Mars.Inter.MQCenter.windowsControlsHelpers.ProcessAllControlsEnumerator;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public class ProcessAllControlsEnumerator
    {
        /// <summary>
        /// 枚举当前进程所有 WinForms 控件（树状结构），通过 EnumChildWindows
        /// </summary>
        public static List<MarsSpiedObjectInfo> GetAllWinFormsControls()
        {
            var result = new List<MarsSpiedObjectInfo>();
            var processId = Process.GetCurrentProcess().Id;
            var topWindows = MarsWindowsAPIsExtend.GetWindows(processId);

            foreach (var hwnd in topWindows)
            {
                // 递归枚举所有子窗口
                MarsWindowsAPIs.EnumChildWindows(hwnd, (childHandle, lParam) =>
                {
                    var className = new StringBuilder(256);
                    MarsWindowsAPIs.GetClassName(childHandle, className, 255);
                    string cls = className.ToString();
                    if (cls.StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase))
                    {
                        var ctrl = System.Windows.Forms.Control.FromHandle(childHandle);
                        if (ctrl != null)
                        {
                            var info = CreateWinFormInfo(ctrl, null);
                            result.Add(info);
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            return result;
        }

        private static MarsSpiedObjectInfo CreateWinFormInfo(System.Windows.Forms.Control ctrl, MarsSpiedObjectInfo parent)
        {
            var info = new MarsSpiedObjectInfo
            {
                objectName = ctrl.Name,
                objectType = ctrl.GetType().FullName,
                Text = ctrl.Text,
                x = ctrl.Left,
                y = ctrl.Top,
                w = ctrl.Width,
                h = ctrl.Height,
                hwnd = ctrl.Handle.ToInt64(),
                referenceToObj = ctrl,
                children = new List<MarsSpiedObjectInfo>()
            };
            if (parent != null)
            {
                info.Pegwindow = parent;
                info.PegWindUUID = parent.obj_uuid;
            }
            foreach (System.Windows.Forms.Control child in ctrl.Controls)
            {
                info.children.Add(CreateWinFormInfo(child, info));
            }
            return info;
        }

        /// <summary>
        /// 枚举当前进程所有 WPF 控件（树状结构）
        /// </summary>
        public static List<MarsSpiedObjectInfo> GetAllWpfControls()
        {
            return Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support.WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();
        }

        /// <summary>
        /// 枚举当前进程所有标准控件（包括标准对话框控件，带 control id），通过 EnumChildWindows
        /// </summary>
        public static List<MarsSpiedObjectInfo> GetAllStandardControls()
        {
            var result = new List<MarsSpiedObjectInfo>();
            var processId = Process.GetCurrentProcess().Id;
            var topWindows = MarsWindowsAPIsExtend.GetWindows(processId);

            foreach (var hwnd in topWindows)
            {
                var tree = StandardWindowsEnumerator.BuildStandardObjectsTree(hwnd);
                if (tree != null)
                    result.AddRange(tree);
            }
            return result;
        }

        /// <summary>
        /// 检查当前进程的顶层窗口类名是否以"afx:"开头
        /// </summary>
        /// <returns>如果是MFC应用程序（afx:开头）返回true，否则返回false</returns>
        public static bool IsMfcApplication()
        {
            try
            {
                // 获取当前进程的顶层窗口
                var processId = Process.GetCurrentProcess().Id;
                var topWindows = MarsWindowsAPIsExtend.GetWindows(processId);
                
                if (topWindows == null || topWindows.Count == 0)
                {
                    return false;
                }

                // 检查第一个顶层窗口的类名
                var firstTopWindow = topWindows.FirstOrDefault();
                if (firstTopWindow != IntPtr.Zero)
                {
                    var className = new StringBuilder(256);
                    MarsWindowsAPIs.GetClassName(firstTopWindow, className, 255);
                    string windowClass = className.ToString();

                    // 检查类名是否以"afx:"开头
                    return windowClass.StartsWith("afx:", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，返回false
                System.Diagnostics.Debug.WriteLine($"Error in IsMfcApplication: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 根据顶层窗口类名智能获取控件信息
        /// 如果顶层窗口类名以"afx:"开头，则获取WinForms和标准控件
        /// 否则获取所有类型的控件
        /// </summary>
        /// <returns>控件信息列表</returns>
        public static List<MarsSpiedObjectInfo> GetControlsByTopWindowClass()
        {
            var result = new List<MarsSpiedObjectInfo>();            
            try
            {
                // 使用IsMfcApplication方法判断是否为MFC应用程序
                if (IsMfcApplication())
                {
                    // 如果是MFC应用程序，只获取WinForms和标准控件
                    result.AddRange(GetAllWinFormsControls());
                    result.AddRange(GetAllStandardControls());
                }
                else
                {
                    // 否则获取所有类型的控件
                    result.AddRange(GetAllWinFormsControls());
                    result.AddRange(GetAllWpfControls());
                    result.AddRange(GetAllStandardControls());
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，返回空列表
                System.Diagnostics.Debug.WriteLine($"Error in GetControlsByTopWindowClass: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 综合获取所有控件信息
        /// </summary>
        public static List<MarsSpiedObjectInfo> GetAllControls()
        {
            var all = new List<MarsSpiedObjectInfo>();
            all.AddRange(GetAllWinFormsControls());
            all.AddRange(GetAllWpfControls());
            all.AddRange(GetAllStandardControls());
            return all;
        }


        public static List<MarsSpiedObjectInfo> GetSpyWindowTree(int pid)
        {
            var result = new List<MarsSpiedObjectInfo>();
            MarsWindowsAPIs.EnumWindows((hWnd, lParam) =>
            {
                MarsWindowsAPIs.GetWindowThreadProcessId(hWnd, out int windowPid);
                if (windowPid == pid && MarsWindowsAPIs.GetParent(hWnd) == IntPtr.Zero)
                {
                    var className = new StringBuilder(256);
                    MarsWindowsAPIs.GetClassName(hWnd, className, 255);
                    string cls = className.ToString();
                    if (!cls.Equals("sysshadow", StringComparison.OrdinalIgnoreCase))
                    {
                        var node = BuildWindowNode(hWnd, null);
                        if (node != null)
                            result.Add(node);
                    }
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }

        private static MarsSpiedObjectInfo BuildWindowNode(IntPtr hWnd, MarsSpiedObjectInfo parent)
        {
            var className = new StringBuilder(256);
            MarsWindowsAPIs.GetClassName(hWnd, className, 255);
            var windowText = new StringBuilder(256);
            MarsWindowsAPIs.GetWindowText(hWnd, windowText, 255);

            var info = new MarsSpiedObjectInfo
            {
                hwnd = hWnd.ToInt64(),
                objectType = className.ToString(),
                objectName = windowText.ToString(),
                Text = windowText.ToString(),
                Pegwindow = parent,
                children = new List<MarsSpiedObjectInfo>()
            };

            // 判断类型
            if (className.ToString().StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase))
            {
                info.controlClassTypeFromAPI = "WinForms";
                // 尝试获取 .NET 控件引用
                try
                {
                    var ctrl = System.Windows.Forms.Control.FromHandle(hWnd);
                    if (ctrl != null)
                        info.referenceToObj = ctrl;
                }
                catch { }
            }
            else if (className.ToString().StartsWith("afx:", StringComparison.OrdinalIgnoreCase))
            {
                info.controlClassTypeFromAPI = "afx";
            }
            else
            {
                info.controlClassTypeFromAPI = "Standard";
            }

            // 递归枚举子窗口
            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.EnumChildWindows(hWnd, (childHwnd, lParam) =>
            {
                var childNode = BuildWindowNode(childHwnd, info);
                if (childNode != null)
                    info.children.Add(childNode);
                return true;
            }, IntPtr.Zero);

            info.allChildrenCount = info.children?.Count ?? 0;
            return info;
        }

        /// <summary>
        /// 获取指定进程ID下的所有窗口和子窗口的基本信息（平铺列表）
        /// 特别处理以"WindowsForms10."开头的类名，使用Control.FromHandle获取详细信息
        /// 每个结构包含parentHwnd字段，以便后期构建树状结构
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>窗口信息平铺列表</returns>
        public static List<MarsSpiedObjectInfo> GetSpyWindowControlList(int processId)
        {
            var result = new List<MarsSpiedObjectInfo>();
            
            try
            {
                // 获取指定进程的所有顶层窗口
                var topWindows = MarsWindowsAPIsExtend.GetWindows(processId);
                
                if (topWindows == null || topWindows.Count == 0)
                {
                    return result;
                }

                // 使用迭代方式遍历所有窗口，返回平铺列表
                var windowQueue = new Queue<(IntPtr hWnd, IntPtr parentHwnd)>();
                
                // 将顶层窗口加入队列（顶层窗口的parentHwnd为0）
                foreach (var topWindow in topWindows)
                {
                    MarsLoggerSimple.Info("GetSpyWindowControlList", $"Enqueue top window: {topWindow}");
                    windowQueue.Enqueue((topWindow, IntPtr.Zero));

                    /// 增加MSAA的校验：
                    /// 
                    MARSAccessibleProvider mARSAccessibleProvider = new MARSAccessibleProvider();
                    try
                    {
                        mARSAccessibleProvider.SaveAccessibleTreeIfRoleMatch(topWindow);
                            //"PushButton");
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("BuildDetailedWindowInfoForList", $"Error getting MSAA object: {ex.Message}", ex);
                    }

                }

                // 使用集合记录已处理的窗口，避免重复处理
                var processedWindows = new HashSet<IntPtr>();

                while (windowQueue.Count > 0)
                {
                    var (hWnd, parentHwnd) = windowQueue.Dequeue();
                    
                    // 避免重复处理
                    if (processedWindows.Contains(hWnd))
                        continue;

                    var windowInfo = BuildDetailedWindowInfoForList(hWnd, parentHwnd);
                    if (windowInfo != null)
                    {
                        //if (!windowInfo.isVisible) continue;
                        processedWindows.Add(hWnd);
                        result.Add(windowInfo);

                        // 将子窗口加入队列
                        EnqueueChildWindowsForList(hWnd, windowQueue);
                    }
                }

                /// 统一处理.net forms控件的名称和名称路径
                /// 
                MarsLoggerSimple.Info("GetSpyWindowControlList", "begin to pharse .net framework objects");
                int iCnt = 0;
                foreach (var win in result)
                {
                    if (win == null) continue;
                    if (win.controlClassTypeFromAPI.Equals("winforms", StringComparison.OrdinalIgnoreCase)) {
                        System.Windows.Forms.Control c = System.Windows.Forms.Control.FromHandle(new IntPtr(win.hwnd));
                        if (c==null) { continue; }
                        win.objectName = c.Name;
                        win.objectType = c.GetType().FullName;
                        win.objectNamePath = BuildNamePath(c);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                MarsLoggerSimple.Error("GetSpyWindowControlList", $"Error in GetSpyWindowControlList: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// 将子窗口加入队列（用于列表模式）
        /// </summary>
        private static void EnqueueChildWindowsForList(IntPtr parentHwnd, Queue<(IntPtr hWnd, IntPtr parentHwnd)> queue)
        {
            try
            {
                MarsWindowsAPIs.EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
                {
                    MarsLoggerSimple.Info("EnqueueChildWindowsForList", $"Enqueue child window: 0x{childHwnd:X} of parent {parentHwnd}");
                    queue.Enqueue((childHwnd, parentHwnd));
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("EnqueueChildWindowsForList", $"Error enqueuing child windows: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将子窗口加入队列（用于树模式）
        /// </summary>
        private static void EnqueueChildWindows(IntPtr parentHwnd, MarsSpiedObjectInfo parentInfo, Queue<(IntPtr hWnd, MarsSpiedObjectInfo parent)> queue)
        {
            try
            {
                MarsWindowsAPIs.EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
                {
                    queue.Enqueue((childHwnd, parentInfo));
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("EnqueueChildWindows", $"Error enqueuing child windows: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建窗口信息（用于列表模式）
        /// </summary>
        private static MarsSpiedObjectInfo BuildDetailedWindowInfoForList(IntPtr hWnd, IntPtr parentHwnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            try
            {
                // 批量获取窗口信息，减少API调用次数
                var windowInfo = GetWindowInfoBatch(hWnd);
                if (windowInfo == null)
                    return null;
                //windowInfo.parentHwnd = parentHwnd.ToInt64(); 
                var info = new MarsSpiedObjectInfo
                {
                    hwnd = hWnd.ToInt64(),
                    parentHwnd = parentHwnd.ToInt64(),
                    objectType = windowInfo.Value.ClassName,
                    objectName = windowInfo.Value.WindowText,
                    Text = windowInfo.Value.WindowText,
                    x = windowInfo.Value.Rect.Left,
                    y = windowInfo.Value.Rect.Top,
                    w = windowInfo.Value.Rect.Right - windowInfo.Value.Rect.Left,
                    h = windowInfo.Value.Rect.Bottom - windowInfo.Value.Rect.Top,
                    controlId = (int)windowInfo.Value.controID,
                    children = new List<MarsSpiedObjectInfo>() // 列表模式不构建children，但保留字段
                };

                // 特别处理WindowsForms控件
                if (windowInfo.Value.ClassName.StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 使用Control.FromHandle获取.NET控件引用
                        var ctrl = System.Windows.Forms.Control.FromHandle(hWnd);
                        if (ctrl != null)
                        {
                            // 更新为.NET控件的详细信息
                            info.objectName = ctrl.Name;
                            info.objectType = ctrl.GetType().FullName;
                            MarsLoggerSimple.Info("BuildDetailedWindowInfoForList", $"ctrl.GetType().FullName find|{info.objectType}|");
                            info.Text = ctrl.Text;
                            info.x = ctrl.Left;
                            info.y = ctrl.Top;
                            info.w = ctrl.Width;
                            info.h = ctrl.Height;
                            info.referenceToObj = ctrl;
                            info.controlClassTypeFromAPI = "WinForms";
                            
                            // 构建名称路径
                            info.objectNamePath = BuildNamePath(ctrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果无法获取.NET控件引用，使用基本窗口信息
                        info.controlClassTypeFromAPI = "WinForms";
                        MarsLoggerSimple.Error("BuildDetailedWindowInfoForList", $"Error getting WinForms control: {ex.Message}", ex);
                    }
                }
                else if (windowInfo.Value.ClassName.StartsWith("afx:", StringComparison.OrdinalIgnoreCase))
                {
                    info.controlClassTypeFromAPI = "afx";
                }
                else
                {
                    info.controlClassTypeFromAPI = "Standard";                    
                }

                // 检查宽度和高度是否为0
                if (info.w <= 0 || info.h <= 0)
                {
                    MarsLoggerSimple.Info("BuildDetailedWindowInfoForList", $"Control with zero size (w={info.w}, h={info.h}) ignored");
                    return null;
                }

                // 检查是否为Mars内部控件
                if ((!string.IsNullOrEmpty(info.objectType)) && (info.objectType.StartsWith("Mars.", StringComparison.OrdinalIgnoreCase)))
                {
                    /// 说明是Mars内部控件，直接忽略
                    /// 
                    MarsLoggerSimple.Info("BuildDetailedWindowInfoForList", $"find|{info.objectType}|ignored");
                    return null;
                }

                
                // --- 新增HitTest可见性判断 ---
                //try
                //{
                //    info.isVisible = MarsWindowsAPIs.IsWindowVisible(hWnd);
                //}
                //catch
                //{
                //    info.isVisible = false;
                //}

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildDetailedWindowInfoForList", $"Error building window info for {hWnd}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 快速构建窗口信息（优化版本）
        /// </summary>
        private static MarsSpiedObjectInfo BuildDetailedWindowInfoFast(IntPtr hWnd, MarsSpiedObjectInfo parent)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            try
            {
                // 批量获取窗口信息，减少API调用次数
                var windowInfo = GetWindowInfoBatch(hWnd);
                if (windowInfo == null)
                    return null;

                var info = new MarsSpiedObjectInfo
                {
                    hwnd = hWnd.ToInt64(),
                    objectType = windowInfo.Value.ClassName,
                    objectName = windowInfo.Value.WindowText,
                    Text = windowInfo.Value.WindowText,
                    x = windowInfo.Value.Rect.Left,
                    y = windowInfo.Value.Rect.Top,
                    w = windowInfo.Value.Rect.Right - windowInfo.Value.Rect.Left,
                    h = windowInfo.Value.Rect.Bottom - windowInfo.Value.Rect.Top,
                    Pegwindow = parent,
                    children = new List<MarsSpiedObjectInfo>()
                };

                // 特别处理WindowsForms控件
                if (windowInfo.Value.ClassName.StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 使用Control.FromHandle获取.NET控件引用
                        var ctrl = System.Windows.Forms.Control.FromHandle(hWnd);
                        if (ctrl != null)
                        {
                            // 更新为.NET控件的详细信息
                            info.objectName = ctrl.Name;
                            info.objectType = ctrl.GetType().FullName;
                            MarsLoggerSimple.Info("BuildDetailedWindowInfoFast", $"ctrl.GetType().FullName find|{info.objectType}|");
                            info.Text = ctrl.Text;
                            info.x = ctrl.Left;
                            info.y = ctrl.Top;
                            info.w = ctrl.Width;
                            info.h = ctrl.Height;
                            info.referenceToObj = ctrl;
                            info.controlClassTypeFromAPI = "WinForms";
                            
                            // 构建名称路径
                            info.objectNamePath = BuildNamePath(ctrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果无法获取.NET控件引用，使用基本窗口信息
                        info.controlClassTypeFromAPI = "WinForms";
                        MarsLoggerSimple.Error("BuildDetailedWindowInfoFast", $"Error getting WinForms control: {ex.Message}", ex);
                    }
                }
                else if (windowInfo.Value.ClassName.StartsWith("afx:", StringComparison.OrdinalIgnoreCase))
                {
                    info.controlClassTypeFromAPI = "afx";
                }
                else
                {
                    info.controlClassTypeFromAPI = "Standard";
                }

                // 检查是否为Mars内部控件
                if ((!string.IsNullOrEmpty(info.objectType)) && (info.objectType.StartsWith("Mars.", StringComparison.OrdinalIgnoreCase)))
                {
                    /// 说明是Mars内部控件，直接忽略
                    /// 
                    MarsLoggerSimple.Info("BuildDetailedWindowInfoFast", $"find|{info.objectType}|ignored");
                    return null;
                }

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildDetailedWindowInfoFast", $"Error building window info for {hWnd}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 窗口信息结构体，用于批量获取窗口信息
        /// </summary>
        public struct WindowInfo
        {
            public long hwnd { get; set; }
            public string ClassName { get; set; }
            public string WindowText { get; set; }
            public Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT Rect { get; set; }
            public long controID { get; set; }
            public long parentHwnd { get; set; }
        }

        /// <summary>
        /// 批量获取窗口信息，减少API调用次数
        /// </summary>
        public static WindowInfo? GetWindowInfoBatch(IntPtr hWnd)
        {
            try
            {
                var className = new StringBuilder(256);
                var windowText = new StringBuilder(256);
                var rect = new Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT();

                // 批量调用API
                string lastErrorStr = "";
                int classNameResult = MarsWindowsAPIs.GetClassName(hWnd, className, 255);
                uint lastError = MarsWindowsAPIs.GetLastError();
                lastErrorStr = $"{lastError}";
                int windowTextResult = MarsWindowsAPIs.GetWindowText(hWnd, windowText, 255);
                 lastError = MarsWindowsAPIs.GetLastError();
                lastErrorStr = $"{lastErrorStr}|{lastError}";
                bool rectResult = MarsWindowsAPIs.GetWindowRect(hWnd, out rect);
                 lastError = MarsWindowsAPIs.GetLastError();
                lastErrorStr = $"{lastErrorStr}|{lastError}";
                long controlId = MarsWindowsAPIs.GetDlgCtrlID(hWnd);
                 lastError = MarsWindowsAPIs.GetLastError();
                lastErrorStr = $"{lastErrorStr}|{lastError}";
                MarsLoggerSimple.Info("GetWindowInfoBatch", $"Getting info for hWnd: {hWnd}|{windowText}|{rect}|{controlId}|lastError|{lastErrorStr}");
                if (classNameResult == 0 && windowTextResult == 0 && !rectResult)
                {
                    MarsLoggerSimple.Info("GetWindowInfoBatch", $"Failed to get window info for {hWnd}, all API calls failed");
                    return null; // 窗口可能无效
                }

                return new WindowInfo
                {
                    hwnd = hWnd.ToInt64(),
                    ClassName = className.ToString(),
                    WindowText = windowText.ToString(),
                    Rect = rect,
                    controID = controlId
                };
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWindowInfoBatch", $"Error getting window info for {hWnd}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 构建详细的窗口信息，特别处理WindowsForms控件
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="parent">父窗口信息</param>
        /// <returns>窗口信息</returns>
        private static MarsSpiedObjectInfo BuildDetailedWindowInfo(IntPtr hWnd, MarsSpiedObjectInfo parent)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            try
            {
                // 获取窗口基本信息
                var className = new StringBuilder(256);
                MarsWindowsAPIs.GetClassName(hWnd, className, 255);
                var windowText = new StringBuilder(256);
                MarsWindowsAPIs.GetWindowText(hWnd, windowText, 255);
                
                // 获取窗口位置和大小
                var rect = new Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT();
                MarsWindowsAPIs.GetWindowRect(hWnd, out rect);

                var info = new MarsSpiedObjectInfo
                {
                    hwnd = hWnd.ToInt64(),
                    objectType = className.ToString(),
                    objectName = windowText.ToString(),
                    Text = windowText.ToString(),
                    x = rect.Left,
                    y = rect.Top,
                    w = rect.Right - rect.Left,
                    h = rect.Bottom - rect.Top,
                    Pegwindow = parent,
                    children = new List<MarsSpiedObjectInfo>()
                };

                // 特别处理WindowsForms控件
                if (className.ToString().StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 使用Control.FromHandle获取.NET控件引用
                        var ctrl = System.Windows.Forms.Control.FromHandle(hWnd);
                        if (ctrl != null)
                        {
                            // 更新为.NET控件的详细信息
                            info.objectName = ctrl.Name;
                            info.objectType = ctrl.GetType().FullName;
                            MarsLoggerSimple.Info("BuildDetailedWindowInfo", $"ctrl.GetType().FullName find|{info.objectType}|");
                            info.Text = ctrl.Text;
                            info.x = ctrl.Left;
                            info.y = ctrl.Top;
                            info.w = ctrl.Width;
                            info.h = ctrl.Height;
                            info.referenceToObj = ctrl;
                            info.controlClassTypeFromAPI = "WinForms";
                            
                            // 构建名称路径
                            info.objectNamePath = BuildNamePath(ctrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果无法获取.NET控件引用，使用基本窗口信息
                        info.controlClassTypeFromAPI = "WinForms";
                        System.Diagnostics.Debug.WriteLine($"Error getting WinForms control: {ex.Message}");
                    }
                }
                else if (className.ToString().StartsWith("afx:", StringComparison.OrdinalIgnoreCase))
                {
                    info.controlClassTypeFromAPI = "afx";
                }
                else
                {
                    info.controlClassTypeFromAPI = "Standard";
                }
                if ((!string.IsNullOrEmpty(info.objectType)) && (info.objectType.StartsWith("Mars.", StringComparison.OrdinalIgnoreCase)))
                {
                    /// 说明是Mars内部控件，直接忽略
                    /// 
                    MarsLoggerSimple.Info("BuildDetailedWindowInfo", $"find|{info.objectType}|ignored");
                    return null;
                }
                
                // 递归枚举子窗口
                MarsWindowsAPIs.EnumChildWindows(hWnd, (childHwnd, lParam) =>
                {
                    var childInfo = BuildDetailedWindowInfo(childHwnd, info);
                    if (childInfo != null)
                    {
                        info.children.Add(childInfo);
                    }
                    return true;
                }, IntPtr.Zero);

                // 计算所有子节点数量
                info.allChildrenCount = CountAllChildren(info);
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building window info for {hWnd}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 构建控件的名称路径
        /// </summary>
        /// <param name="ctrl">控件</param>
        /// <returns>名称路径</returns>
        private static string BuildNamePath(System.Windows.Forms.Control ctrl)
        {
            var path = new List<string>();
            var current = ctrl;
            
            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Name))
                {
                    path.Insert(0, current.Name);
                }
                else
                {
                    path.Insert(0, current.GetType().Name);
                }
                current = current.Parent;
            }
            
            return string.Join(";", path);
        }

        /// <summary>
        /// 按位置排序窗口列表（先按x坐标，再按y坐标）
        /// </summary>
        /// <param name="windowList">窗口信息列表</param>
        /// <returns>排序后的窗口信息列表</returns>
        public static List<MarsSpiedObjectInfo> SortWindowsByPosition(List<MarsSpiedObjectInfo> windowList)
        {
            if (windowList == null || windowList.Count == 0)
                return windowList;

            return windowList.OrderBy(w => w.x).ThenBy(w => w.y).ToList();
        }

        /// <summary>
        /// 从平铺列表构建树状结构
        /// </summary>
        /// <param name="flatList">平铺的窗口信息列表</param>
        /// <returns>树状结构的窗口信息列表</returns>
        public static List<MarsSpiedObjectInfo> BuildTreeFromList(List<MarsSpiedObjectInfo> flatList)
        {
            var result = new List<MarsSpiedObjectInfo>();
            var windowDict = new Dictionary<long, MarsSpiedObjectInfo>();

            try
            {
                // 首先将所有窗口添加到字典中
                foreach (var window in flatList)
                {
                    if (window != null)
                    {
                        windowDict[window.hwnd] = window;
                    }
                }

                // 然后构建父子关系
                foreach (var window in flatList)
                {
                    if (window == null) continue;

                    if (window.parentHwnd == 0)
                    {
                        // 顶层窗口
                        result.Add(window);
                    }
                    else if (windowDict.ContainsKey(window.parentHwnd))
                    {
                        // 子窗口
                        var parent = windowDict[window.parentHwnd];
                        if (parent.children == null)
                            parent.children = new List<MarsSpiedObjectInfo>();
                        parent.children.Add(window);
                    }
                }

                // 计算所有子节点数量
                foreach (var window in flatList)
                {
                    if (window != null)
                    {
                        window.allChildrenCount = CountAllChildren(window);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildTreeFromList", $"Error building tree from list: {ex.Message}", ex);
            }

            return result;
        }


        /// <summary>
        /// 递归计算所有子节点数量
        /// </summary>
        /// <param name="info">窗口信息</param>
        /// <returns>子节点总数</returns>
        private static int CountAllChildren(MarsSpiedObjectInfo info)
        {
            if (info?.children == null || info.children.Count == 0)
                return 0;

            int count = info.children.Count;
            foreach (var child in info.children)
            {
                count += CountAllChildren(child);
            }
            return count;
        }


        public static List<MarsSpiedObjectInfo> BuildTreeFromListByParentHwnd(List<MarsSpiedObjectInfo> flatList)
        {
            if (flatList == null || flatList.Count == 0)
                return new List<MarsSpiedObjectInfo>();

            var hwndDict = new Dictionary<long, MarsSpiedObjectInfo>();
            var roots = new List<MarsSpiedObjectInfo>();

            // 建立句柄到对象的映射
            foreach (var obj in flatList)
            {
                if (obj == null) continue;
                hwndDict[obj.hwnd] = obj;
                obj.children = new List<MarsSpiedObjectInfo>();
            }

            // 组织树结构
            foreach (var obj in flatList)
            {
                if (obj == null) continue;
                if (obj.parentHwnd != 0 && hwndDict.TryGetValue(obj.parentHwnd, out var parent))
                {
                    parent.children.Add(obj);
                }
                else
                {
                    roots.Add(obj);
                }
            }

            // 可选：递归统计所有子节点数量
            foreach (var root in roots)
            {
                root.allChildrenCount = CountAllChildren(root);
            }

            return roots;
        }

        

    }
}
