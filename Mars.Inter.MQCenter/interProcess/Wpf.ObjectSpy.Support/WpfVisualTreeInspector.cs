using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Diagnostics;

using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// WPF可视树检查器，用于获取WPF应用中所有可视对象的层级结构
    /// 每个顶级窗口为树的根节点，包含完整的对象属性信息用于唯一定位GUI对象
    /// </summary>
    public class WpfVisualTreeInspector
    {
        /// <summary>
        /// WPF可视对象信息类，包含用于唯一定位GUI对象的所有属性
        /// </summary>
        public class WpfVisualObjectInfo
        {
            /// <summary>
            /// 对象名称
            /// </summary>
            public string Name { get; set; } = "";

            /// <summary>
            /// 对象名称路径（从根到当前对象的完整路径）
            /// </summary>
            public string NamePath { get; set; } = "";

            /// <summary>
            /// 对象类型
            /// </summary>
            public string Type { get; set; } = "";

            /// <summary>
            /// 对象类型路径（继承层次结构）
            /// </summary>
            public string TypePath { get; set; } = "";

            /// <summary>
            /// 对象位置和大小（相对于屏幕的绝对位置）
            /// </summary>
            public System.Drawing.Rectangle Position { get; set; }

            /// <summary>
            /// 对象文本内容
            /// </summary>
            public string Text { get; set; } = "";

            /// <summary>
            /// 对象是否可见
            /// </summary>
            public bool IsVisible { get; set; }

            /// <summary>
            /// 对象是否启用
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// 对象的AutomationId（如果存在）
            /// </summary>
            public string AutomationId { get; set; } = "";

            /// <summary>
            /// 对象的Uid（如果存在）
            /// </summary>
            public string Uid { get; set; } = "";

            /// <summary>
            /// 对象的Tag（如果存在）
            /// </summary>
            public object Tag { get; set; }

            /// <summary>
            /// 子对象列表
            /// </summary>
            public List<WpfVisualObjectInfo> Children { get; set; } = new List<WpfVisualObjectInfo>();

            /// <summary>
            /// 父对象引用
            /// </summary>
            public WpfVisualObjectInfo Parent { get; set; }

            /// <summary>
            /// 对象在父容器中的索引
            /// </summary>
            public int Index { get; set; } = -1;

            /// <summary>
            /// 对象的Z-Order
            /// </summary>
            public int ZOrder { get; set; } = -1;

            /// <summary>
            /// WPF UI对象引用（用于直接访问WPF元素）
            /// </summary>
            public object RefObject { get; set; }

            /// <summary>
            /// 所有子节点和孙节点的总数（包括直接子节点和所有后代节点）
            /// </summary>
            public int AllChildrenCount { get; set; } = 0;

            /// <summary>
            /// 对象的依赖属性列表（类似Snoop）
            /// </summary>
            public Dictionary<string, object> DependencyProperties { get; set; } = new Dictionary<string, object>();

            /// <summary>
            /// 对象的事件列表（类似Snoop）
            /// </summary>
            public List<string> Events { get; set; } = new List<string>();

            /// <summary>
            /// 对象的样式信息（类似Snoop）
            /// </summary>
            public string Style { get; set; } = "";

            /// <summary>
            /// 对象的模板信息（类似Snoop）
            /// </summary>
            public string Template { get; set; } = "";

            /// <summary>
            /// 对象的资源信息（类似Snoop）
            /// </summary>
            public Dictionary<string, object> Resources { get; set; } = new Dictionary<string, object>();

            /// <summary>
            /// 对象的绑定信息（类似Snoop）
            /// </summary>
            public List<string> Bindings { get; set; } = new List<string>();

            /// <summary>
            /// 对象的触发器信息（类似Snoop）
            /// </summary>
            public List<string> Triggers { get; set; } = new List<string>();

            /// <summary>
            /// 对象的渲染信息（类似Snoop）
            /// </summary>
            public string RenderInfo { get; set; } = "";

            /// <summary>
            /// 对象的布局信息（类似Snoop）
            /// </summary>
            public string LayoutInfo { get; set; } = "";

            /// <summary>
            /// 对象的输入信息（类似Snoop）
            /// </summary>
            public string InputInfo { get; set; } = "";

            /// <summary>
            /// 对象的焦点信息（类似Snoop）
            /// </summary>
            public string FocusInfo { get; set; } = "";

            /// <summary>
            /// 对象的可见性信息（类似Snoop）
            /// </summary>
            public string VisibilityInfo { get; set; } = "";

            /// <summary>
            /// 对象的变换信息（类似Snoop）
            /// </summary>
            public string TransformInfo { get; set; } = "";

            /// <summary>
            /// 对象的动画信息（类似Snoop）
            /// </summary>
            public string AnimationInfo { get; set; } = "";

            /// <summary>
            /// 对象的上下文信息（类似Snoop）
            /// </summary>
            public string ContextInfo { get; set; } = "";

            /// <summary>
            /// 对象的调试信息（类似Snoop）
            /// </summary>
            public string DebugInfo { get; set; } = "";

            /// <summary>
            /// 获取对象的唯一标识字符串
            /// </summary>
            public string GetUniqueIdentifier()
            {
                var identifier = new StringBuilder();

                if (!string.IsNullOrEmpty(Name))
                    identifier.Append($"Name:{Name}");

                if (!string.IsNullOrEmpty(AutomationId))
                    identifier.Append($"|AutomationId:{AutomationId}");

                if (!string.IsNullOrEmpty(Uid))
                    identifier.Append($"|Uid:{Uid}");

                if (Index >= 0)
                    identifier.Append($"|Index:{Index}");

                if (!string.IsNullOrEmpty(Type))
                    identifier.Append($"|Type:{Type}");

                return identifier.ToString();
            }

            /// <summary>
            /// 获取对象的完整路径描述
            /// </summary>
            public string GetFullPath()
            {
                var path = new List<string>();
                var current = this;

                while (current != null)
                {
                    var nodeDesc = string.IsNullOrEmpty(current.Name) ?
                        $"[{current.Type}]" :
                        $"{current.Name}[{current.Type}]";

                    if (current.Index >= 0)
                        nodeDesc += $"#{current.Index}";

                    path.Insert(0, nodeDesc);
                    current = current.Parent;
                }

                return string.Join(" -> ", path);
            }

            public override string ToString()
            {
                return $"{Name}[{Type}] - {Position} - {Text}";
            }
        }

        /// <summary>
        /// 获取当前WPF应用的所有顶级窗口及其完整的可视对象层级结构
        /// 从VisualRoot开始遍历，这是WPF可视树的真正根节点
        /// </summary>
        /// <returns>顶级窗口列表，每个窗口包含完整的子对象树</returns>
        public static List<WpfVisualObjectInfo> GetAllTopLevelWindows()
        {
            MarsLoggerSimple.logBegin("GetAllTopLevelWindows");
            var topLevelWindows = new List<WpfVisualObjectInfo>();

            try
            {
                // 方法1：从VisualRoot开始遍历（推荐方式）
                var visualRoots = GetVisualRoots();
                MarsLoggerSimple.Info("GetAllTopLevelWindows",
                    $"Found {visualRoots.Count} visual roots");

                foreach (var visualRoot in visualRoots)
                {
                    if (visualRoot != null)
                    {
                        var rootInfo = CreateVisualObjectInfo(visualRoot, null, 0);
                        if (rootInfo != null)
                        {
                            topLevelWindows.Add(rootInfo);
                            MarsLoggerSimple.Info("GetAllTopLevelWindows",
                                $"Added visual root: {rootInfo.Name} [{rootInfo.Type}]");
                        }
                    }
                }

                // 方法2：如果VisualRoot为空，尝试从Application.Current.Windows获取
                if (topLevelWindows.Count == 0 && Application.Current != null)
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindows",
                        "No visual roots found, trying Application.Current.Windows");

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null)
                        {
                            var windowInfo = CreateVisualObjectInfo(window, null, 0);
                            if (windowInfo != null)
                            {
                                topLevelWindows.Add(windowInfo);
                                MarsLoggerSimple.Info("GetAllTopLevelWindows",
                                    $"Added window: {window.Title} [{window.GetType().Name}]");
                            }
                        }
                    }
                }

                // 方法3：从PresentationSource.CurrentSources获取
                if (topLevelWindows.Count == 0)
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindows",
                        "No windows found in Application.Current.Windows, trying PresentationSource.CurrentSources");

                    var sources = PresentationSource.CurrentSources;
                    var sourcesList = new List<PresentationSource>();
                    foreach (PresentationSource source in sources)
                    {
                        sourcesList.Add(source);
                    }
                    MarsLoggerSimple.Info("GetAllTopLevelWindows",
                        $"PresentationSource.CurrentSources.Count: {sourcesList.Count}");

                    foreach (PresentationSource source in sources)
                    {
                        if (source is HwndSource hwndSource)
                        {
                            // 在Dispatcher中获取RootVisual
                            var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
                            if (rootVisual != null)
                            {
                                var wpfWindow = rootVisual as Window;
                                if (wpfWindow != null)
                                {
                                    var windowInfo = CreateVisualObjectInfo(wpfWindow, null, 0);
                                    if (windowInfo != null)
                                    {
                                        topLevelWindows.Add(windowInfo);
                                        MarsLoggerSimple.Info("GetAllTopLevelWindows",
                                            $"Added window from HwndSource: {wpfWindow.Title} [{wpfWindow.GetType().Name}]");
                                    }
                                }
                            }
                        }
                    }
                }

                // 方法4：如果仍然没有找到窗口，尝试从当前进程的窗口句柄获取
                if (topLevelWindows.Count == 0)
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindows",
                        "No windows found in PresentationSource, trying process window handles");

                    var currentProcess = Process.GetCurrentProcess();
                    var windows = System.Windows.Automation.AutomationElement.RootElement.FindAll(
                        System.Windows.Automation.TreeScope.Subtree,
                        new System.Windows.Automation.PropertyCondition(
                            System.Windows.Automation.AutomationElement.ProcessIdProperty,
                            currentProcess.Id)
                    );

                    MarsLoggerSimple.Info("GetAllTopLevelWindows",
                        $"Found {windows.Count} automation elements for current process");

                    foreach (System.Windows.Automation.AutomationElement window in windows)
                    {
                        if (!window.Current.NativeWindowHandle.Equals(IntPtr.Zero))
                        {
                            // 通过窗口句柄获取HwndSource
                            var hwndSource = HwndSource.FromHwnd((IntPtr)window.Current.NativeWindowHandle);
                            if (hwndSource != null)
                            {
                                // 在Dispatcher中获取RootVisual
                                var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
                                if (rootVisual != null)
                                {
                                    var wpfWindow = rootVisual as Window;
                                    if (wpfWindow != null)
                                    {
                                        var windowInfo = CreateVisualObjectInfo(wpfWindow, null, 0);
                                        if (windowInfo != null)
                                        {
                                            topLevelWindows.Add(windowInfo);
                                            MarsLoggerSimple.Info("GetAllTopLevelWindows",
                                                $"Added window from automation: {wpfWindow.Title} [{wpfWindow.GetType().Name}]");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllTopLevelWindows", $"Error getting top level windows: {ex.Message}", ex);
            }

            MarsLoggerSimple.logEnd("GetAllTopLevelWindows", $"Found {topLevelWindows.Count} top level windows");
            return topLevelWindows;
        }

        /// <summary>
        /// 获取所有VisualRoot（WPF可视树的根节点）
        /// </summary>
        /// <returns>VisualRoot列表</returns>
        private static List<DependencyObject> GetVisualRoots()
        {
            var visualRoots = new List<DependencyObject>();

            try
            {
                // 方法1：从Application.Current.Windows获取
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null)
                        {
                            visualRoots.Add(window);
                        }
                    }
                }

                // 方法2：从PresentationSource.CurrentSources获取
                var sources = PresentationSource.CurrentSources;
                foreach (PresentationSource presentationSource in sources)
                {
                    if (presentationSource==null) continue;
                    var presentationSourceDispatcher = presentationSource.Dispatcher;
                    var rootVisual = presentationSourceDispatcher.Invoke(() => presentationSource.RootVisual);

                    if (!(Application.Current is null)
                        && (Application.Current.Dispatcher == presentationSourceDispatcher))
                    {
                        // 如果Dispatcher相同，直接添加RootVisual
                        if (rootVisual != null)
                        {
                            visualRoots.Add(rootVisual);
                        }
                    }
                    
                    else
                    {
                        // 如果Dispatcher不同，使用Dispatcher.Invoke获取RootVisual
                        if (rootVisual != null)
                        {
                            visualRoots.Add(rootVisual);
                        }
                    }

                    
                }

                // 方法3：从HwndSource获取
                var hwndSources = GetHwndSources();
                foreach (var hwndSource in hwndSources)
                {
                    if (hwndSource != null)
                    {
                        // 在Dispatcher中获取RootVisual
                        var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
                        if (rootVisual != null)
                        {
                            visualRoots.Add(rootVisual);
                        }
                    }
                }

                // 去重
                var uniqueRoots = new List<DependencyObject>();
                var seenRoots = new HashSet<DependencyObject>();
                
                foreach (var root in visualRoots)
                {
                    if (root != null && !seenRoots.Contains(root))
                    {
                        seenRoots.Add(root);
                        uniqueRoots.Add(root);
                    }
                }

                return uniqueRoots;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetVisualRoots", 
                    $"Error getting visual roots: {ex.Message}", ex);
                return visualRoots;
            }
        }

        /// <summary>
        /// 获取所有HwndSource
        /// </summary>
        /// <returns>HwndSource列表</returns>
        private static List<HwndSource> GetHwndSources()
        {
            var hwndSources = new List<HwndSource>();

            try
            {
                // 通过PresentationSource.CurrentSources获取HwndSource
                var sources = PresentationSource.CurrentSources;
                foreach (var source in sources)
                {
                    if (source is HwndSource hwndSource)
                    {
                        hwndSources.Add(hwndSource);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetHwndSources", 
                    $"Error getting HwndSources: {ex.Message}", ex);
            }

            return hwndSources;
        }

        /// <summary>
        /// 创建可视对象信息
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="parent">父对象</param>
        /// <param name="index">在父容器中的索引</param>
        /// <returns>可视对象信息</returns>
        private static WpfVisualObjectInfo CreateVisualObjectInfo(DependencyObject element, WpfVisualObjectInfo parent, int index)
        {
            if (element == null) return null;

            try
            {
                var info = new WpfVisualObjectInfo
                {
                    Parent = parent,
                    Index = index,
                    Type = element.GetType().FullName,
                    TypePath = GetTypePath(element.GetType()),
                    RefObject = element  // 保存WPF元素引用
                };

                // 设置名称
                if (element is FrameworkElement fe)
                {
                    info.Name = fe.Name ?? "";
                    info.Uid = fe.Uid ?? "";
                    info.Tag = fe.Tag;
                    info.IsVisible = fe.Visibility == Visibility.Visible;
                    info.IsEnabled = fe.IsEnabled;
                }

                // 设置AutomationId
                if (element is DependencyObject)
                {
                    info.AutomationId = System.Windows.Automation.AutomationProperties.GetAutomationId(element) ?? "";
                }

                // 设置文本内容
                info.Text = GetElementText(element);

                // 设置位置
                info.Position = GetElementBounds(element);

                // 构建名称路径
                info.NamePath = BuildNamePath(info);

                // 收集Snoop风格的信息
                CollectSnoopStyleInfo(element, info);

                // 递归处理子元素
                ProcessChildren(element, info);

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateVisualObjectInfo", $"Error creating visual object info: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 处理子元素
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="parentInfo">父元素信息</param>
        /// <returns>所有子节点和孙节点的总数</returns>
        private static int ProcessChildren(DependencyObject parent, WpfVisualObjectInfo parentInfo)
        {
            try
            {
                var childCount = VisualTreeHelper.GetChildrenCount(parent);
                int totalChildrenCount = 0;

                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    var childInfo = CreateVisualObjectInfo(child, parentInfo, i);
                    if (childInfo != null)
                    {
                        parentInfo.Children.Add(childInfo);
                        totalChildrenCount += 1 + childInfo.AllChildrenCount; // 包括直接子节点和其所有后代节点
                    }
                }

                // 更新父节点的所有子节点总数
                parentInfo.AllChildrenCount = totalChildrenCount;
                return totalChildrenCount;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ProcessChildren", $"Error processing children: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取元素文本内容
        /// </summary>
        private static string GetElementText(DependencyObject element)
        {
            try
            {
                // 根据元素类型获取文本
                if (element is TextBlock textBlock)
                    return textBlock.Text ?? "";

                if (element is TextBox textBox)
                    return textBox.Text ?? "";

                if (element is Label label)
                    return label.Content?.ToString() ?? "";

                if (element is Button button)
                    return button.Content?.ToString() ?? "";

                if (element is CheckBox checkBox)
                    return checkBox.Content?.ToString() ?? "";

                if (element is RadioButton radioButton)
                    return radioButton.Content?.ToString() ?? "";

                if (element is ComboBox comboBox)
                    return comboBox.Text ?? "";

                if (element is Window window)
                    return window.Title ?? "";

                if (element is HeaderedContentControl headeredControl)
                    return headeredControl.Header?.ToString() ?? "";

                if (element is ContentControl contentControl)
                    return contentControl.Content?.ToString() ?? "";

                if (element is FrameworkElement fe && fe.ToolTip != null)
                    return fe.ToolTip.ToString();

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取元素边界
        /// </summary>
        private static System.Drawing.Rectangle GetElementBounds(DependencyObject element)
        {
            try
            {
                if (element is UIElement uiElement)
                {
                    // 获取相对于屏幕的位置
                    var point = uiElement.PointToScreen(new System.Windows.Point(0, 0));
                    var size = uiElement.RenderSize;

                    return new System.Drawing.Rectangle(
                        (int)point.X,
                        (int)point.Y,
                        (int)size.Width,
                        (int)size.Height
                    );
                }

                return new System.Drawing.Rectangle(0, 0, 0, 0);
            }
            catch
            {
                return new System.Drawing.Rectangle(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// 获取类型路径（继承层次结构）
        /// </summary>
        private static string GetTypePath(Type type)
        {
            if (type == null) return "";

            var path = new List<string>();
            var currentType = type;

            while (currentType != null)
            {
                path.Add(currentType.FullName);
                currentType = currentType.BaseType;

                // 限制继承层次深度，避免过长的路径
                if (path.Count > 10) break;
            }

            return string.Join(";", path);
        }

        /// <summary>
        /// 构建名称路径
        /// </summary>
        private static string BuildNamePath(WpfVisualObjectInfo info)
        {
            var path = new List<string>();
            var current = info;

            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Name))
                {
                    path.Insert(0, current.Name);
                }
                else
                {
                    path.Insert(0, $"[{current.Type}]");
                }

                current = current.Parent;
            }

            return string.Join(".", path);
        }

        /// <summary>
        /// 打印可视树结构到控制台（用于调试）
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        /// <param name="maxDepth">最大打印深度</param>
        public static void PrintVisualTree(List<WpfVisualObjectInfo> windows, int maxDepth = 30)
        {
            foreach (var window in windows)
            {
                PrintVisualObject(window, 0, maxDepth);
            }
        }

        /// <summary>
        /// 递归打印可视对象
        /// </summary>
        private static void PrintVisualObject(WpfVisualObjectInfo info, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                MarsLoggerSimple.Warnning("PrintVisualObject", $"Max depth reached, stopping recursion.|{depth}|{maxDepth}");
                return;
            }

            var indent = new string(' ', depth * 2);
            //Console.WriteLine($"{indent}{info}");
            MarsLoggerSimple.Info("PrintVisualObject", $"{indent}{info}");
            foreach (var child in info.Children)
            {
                PrintVisualObject(child, depth + 1, maxDepth);
            }
        }

        /// <summary>
        /// 查找具有指定名称的对象
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        /// <param name="name">要查找的名称</param>
        /// <returns>匹配的对象列表</returns>
        public static List<WpfVisualObjectInfo> FindObjectsByName(List<WpfVisualObjectInfo> windows, string name)
        {
            var results = new List<WpfVisualObjectInfo>();

            foreach (var window in windows)
            {
                FindObjectsByNameRecursive(window, name, results);
            }

            return results;
        }

        /// <summary>
        /// 递归查找具有指定名称的对象
        /// </summary>
        private static void FindObjectsByNameRecursive(WpfVisualObjectInfo info, string name, List<WpfVisualObjectInfo> results)
        {
            if (info.Name == name)
            {
                results.Add(info);
            }

            foreach (var child in info.Children)
            {
                FindObjectsByNameRecursive(child, name, results);
            }
        }

        /// <summary>
        /// 查找具有指定类型的对象
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        /// <param name="typeName">要查找的类型名称</param>
        /// <returns>匹配的对象列表</returns>
        public static List<WpfVisualObjectInfo> FindObjectsByType(List<WpfVisualObjectInfo> windows, string typeName)
        {
            var results = new List<WpfVisualObjectInfo>();

            foreach (var window in windows)
            {
                FindObjectsByTypeRecursive(window, typeName, results);
            }

            return results;
        }

        /// <summary>
        /// 递归查找具有指定类型的对象
        /// </summary>
        private static void FindObjectsByTypeRecursive(WpfVisualObjectInfo info, string typeName, List<WpfVisualObjectInfo> results)
        {
            if (info.Type.Contains(typeName))
            {
                results.Add(info);
            }

            foreach (var child in info.Children)
            {
                FindObjectsByTypeRecursive(child, typeName, results);
            }
        }

        /// <summary>
        /// 获取当前WPF应用的所有顶级窗口并转换为MarsSpiedObjectInfo格式
        /// 用于直接加载到Windows Forms的TreeView中
        /// </summary>
        /// <returns>转换后的MarsSpiedObjectInfo列表</returns>
        public static List<MarsSpiedObjectInfo> GetAllTopLevelWindowsAsMarsObjects()
        {
            MarsLoggerSimple.logBegin("GetAllTopLevelWindowsAsMarsObjects");

            try
            {
                // 获取WPF可视树
                var wpfWindows = GetAllTopLevelWindows();

                // 转换为MarsSpiedObjectInfo格式
                var marsObjects = WpfVisualTreeAdapter.ConvertWpfTreeToMarsObjects(wpfWindows);

                MarsLoggerSimple.logEnd("GetAllTopLevelWindowsAsMarsObjects",
                    $"Converted {wpfWindows.Count} WPF windows to {marsObjects.Count} Mars objects");

                return marsObjects;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllTopLevelWindowsAsMarsObjects",
                    $"Error getting WPF windows as Mars objects: {ex.Message}", ex);
                return new List<MarsSpiedObjectInfo>();
            }
        }

        /// <summary>
        /// 将WPF可视树直接加载到Windows Forms的TreeView中
        /// </summary>
        /// <param name="treeView">目标TreeView控件</param>
        /// <param name="targetControlId">目标控件ID（可选，用于高亮显示）</param>
        public static void LoadWpfTreeToTreeView(System.Windows.Forms.TreeView treeView,
            List<MarsSpiedObjectInfo> marsObjects,
            IntPtr targetControlId = default(IntPtr))
        {
            if (treeView == null) return;

            MarsLoggerSimple.logBegin("LoadWpfTreeToTreeView");

            try
            {
                // 直接加载到TreeView
                WpfVisualTreeAdapter.LoadWpfTreeToTreeView(treeView, marsObjects, targetControlId);

                MarsLoggerSimple.logEnd("LoadWpfTreeToTreeView",
                    $"Loaded {marsObjects.Count} WPF windows to TreeView");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadWpfTreeToTreeView",
                    $"Error loading WPF tree to TreeView: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建带WPF元素引用的可视对象信息
        /// 用于需要直接操作WPF元素的场景
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="parent">父对象</param>
        /// <param name="index">在父容器中的索引</param>
        /// <returns>带引用的可视对象信息</returns>
        private static WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference CreateVisualObjectInfoWithReference(
            DependencyObject element,
            WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference parent,
            int index)
        {
            if (element == null) return null;

            try
            {
                var info = new WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference
                {
                    Parent = parent,
                    Index = index,
                    Type = element.GetType().FullName,
                    TypePath = GetTypePath(element.GetType()),
                    WpfElement = element
                };

                // 设置名称
                if (element is FrameworkElement fe)
                {
                    info.Name = fe.Name ?? "";
                    info.Uid = fe.Uid ?? "";
                    info.Tag = fe.Tag;
                    info.IsVisible = fe.Visibility == Visibility.Visible;
                    info.IsEnabled = fe.IsEnabled;
                }

                // 设置AutomationId
                if (element is DependencyObject)
                {
                    info.AutomationId = System.Windows.Automation.AutomationProperties.GetAutomationId(element) ?? "";
                }

                // 设置文本内容
                info.Text = GetElementText(element);

                // 设置位置
                info.Position = GetElementBounds(element);

                // 构建名称路径
                info.NamePath = BuildNamePath(info);

                // 递归处理子元素
                ProcessChildrenWithReference(element, info);

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateVisualObjectInfoWithReference",
                    $"Error creating visual object info with reference: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 处理带引用的子元素
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="parentInfo">父元素信息</param>
        /// <returns>所有子节点和孙节点的总数</returns>
        private static int ProcessChildrenWithReference(DependencyObject parent, WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference parentInfo)
        {
            try
            {
                var childCount = VisualTreeHelper.GetChildrenCount(parent);
                int totalChildrenCount = 0;

                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    var childInfo = CreateVisualObjectInfoWithReference(child, parentInfo, i);
                    if (childInfo != null)
                    {
                        parentInfo.Children.Add(childInfo);
                        totalChildrenCount += 1 + childInfo.AllChildrenCount; // 包括直接子节点和其所有后代节点
                    }
                }

                // 更新父节点的所有子节点总数
                parentInfo.AllChildrenCount = totalChildrenCount;
                return totalChildrenCount;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ProcessChildrenWithReference",
                    $"Error processing children with reference: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取带WPF元素引用的顶级窗口列表
        /// 直接从Application的VisualTree中获取，不依赖AutomationElement
        /// </summary>
        /// <returns>带引用的顶级窗口列表</returns>
        public static List<WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference> GetAllTopLevelWindowsWithReference()
        {
            MarsLoggerSimple.logBegin("GetAllTopLevelWindowsWithReference");
            var topLevelWindows = new List<WpfVisualTreeAdapter.WpfVisualObjectInfoWithReference>();

            try
            {
                // 方法1：直接从Application.Current.Windows获取（主要方法）
                if (Application.Current != null)
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                        $"Application.Current.Windows.Count: {Application.Current.Windows.Count}");

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null)
                        {
                            var windowInfo = CreateVisualObjectInfoWithReference(window, null, 0);
                            if (windowInfo != null)
                            {
                                topLevelWindows.Add(windowInfo);
                                MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                                    $"Added window with reference: {window.Title} [{window.GetType().Name}]");
                            }
                        }
                    }
                }
                else
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference", "Application.Current is null");
                }

                // 方法2：如果Application.Current.Windows为空，尝试从PresentationSource获取
                if (topLevelWindows.Count == 0)
                {
                    MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                        "No windows found in Application.Current.Windows, trying PresentationSource.CurrentSources");

                    var sources = PresentationSource.CurrentSources;
                    var sourcesList = new List<PresentationSource>();
                    foreach (PresentationSource source in sources)
                    {
                        sourcesList.Add(source);
                    }
                    MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                        $"PresentationSource.CurrentSources.Count: {sourcesList.Count}");

                    foreach (PresentationSource source in sources)
                    {
                        if (source is HwndSource hwndSource)
                        {
                            // 在Dispatcher中获取RootVisual
                            var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
                            if (rootVisual != null)
                            {
                                var wpfWindow = rootVisual as Window;
                                if (wpfWindow != null)
                                {
                                    var windowInfo = CreateVisualObjectInfoWithReference(wpfWindow, null, 0);
                                    if (windowInfo != null)
                                    {
                                        topLevelWindows.Add(windowInfo);
                                        MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                                            $"Added window with reference from HwndSource: {wpfWindow.Title} [{wpfWindow.GetType().Name}]");
                                    }
                                }
                            }
                        }
                    }

                    // 方法3：如果仍然没有找到窗口，尝试从当前进程的窗口句柄获取
                    if (topLevelWindows.Count == 0)
                    {
                        MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                            "No windows found in PresentationSource, trying process window handles");

                        var currentProcess = Process.GetCurrentProcess();
                        var windows = System.Windows.Automation.AutomationElement.RootElement.FindAll(
                            System.Windows.Automation.TreeScope.Subtree,
                            new System.Windows.Automation.PropertyCondition(
                                System.Windows.Automation.AutomationElement.ProcessIdProperty,
                                currentProcess.Id)
                        );

                        MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                            $"Found {windows.Count} automation elements for current process");

                        foreach (System.Windows.Automation.AutomationElement window in windows)
                        {
                            if (!window.Current.NativeWindowHandle.Equals(IntPtr.Zero))
                            {
                                // 通过窗口句柄获取HwndSource
                                var hwndSource = HwndSource.FromHwnd((IntPtr)window.Current.NativeWindowHandle);
                                if (hwndSource != null)
                                {
                                    // 在Dispatcher中获取RootVisual
                                    var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
                                    if (rootVisual != null)
                                    {
                                        var wpfWindow = rootVisual as Window;
                                        if (wpfWindow != null)
                                        {
                                            var windowInfo = CreateVisualObjectInfoWithReference(wpfWindow, null, 0);
                                            if (windowInfo != null)
                                            {
                                                topLevelWindows.Add(windowInfo);
                                                MarsLoggerSimple.Info("GetAllTopLevelWindowsWithReference",
                                                    $"Added window with reference from automation: {wpfWindow.Title} [{wpfWindow.GetType().Name}]");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllTopLevelWindowsWithReference",
                    $"Error getting top level windows with reference: {ex.Message}", ex);
            }

            MarsLoggerSimple.logEnd("GetAllTopLevelWindowsWithReference",
                $"Found {topLevelWindows.Count} top level windows with reference");
            return topLevelWindows;
        }

        /// <summary>
        /// 获取所有界面元素（包括可视树、逻辑树和自动化树）
        /// 这是一个更全面的方法，能够获取所有类型的界面元素
        /// </summary>
        /// <returns>所有界面元素列表</returns>
        public static List<WpfVisualObjectInfo> GetAllUIElements()
        {
            MarsLoggerSimple.logBegin("GetAllUIElements");
            var allElements = new List<WpfVisualObjectInfo>();

            try
            {
                // 方法1：从可视树获取元素
                var visualElements = GetAllTopLevelWindows();
                allElements.AddRange(visualElements);

                // 方法2：从逻辑树获取元素
                var logicalElements = GetAllLogicalElements();
                allElements.AddRange(logicalElements);

                // 方法3：从自动化树获取元素
                var automationElements = GetAllAutomationElements();
                allElements.AddRange(automationElements);

                // 去重（基于元素引用）
                var uniqueElements = new List<WpfVisualObjectInfo>();
                var processedRefs = new HashSet<object>();

                foreach (var element in allElements)
                {
                    if (element.RefObject != null && !processedRefs.Contains(element.RefObject))
                    {
                        uniqueElements.Add(element);
                        processedRefs.Add(element.RefObject);
                    }
                    else if (element.RefObject == null)
                    {
                        // 对于没有引用的元素，使用位置和类型进行去重
                        var key = $"{element.Type}_{element.Position.X}_{element.Position.Y}_{element.Position.Width}_{element.Position.Height}";
                        if (!processedRefs.Contains(key))
                        {
                            uniqueElements.Add(element);
                            processedRefs.Add(key);
                        }
                    }
                }

                MarsLoggerSimple.Info("GetAllUIElements", 
                    $"Found {uniqueElements.Count} unique UI elements (Visual: {visualElements.Count}, Logical: {logicalElements.Count}, Automation: {automationElements.Count})");

                return uniqueElements;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllUIElements", 
                    $"Error getting all UI elements: {ex.Message}", ex);
                return allElements;
            }
            finally
            {
                MarsLoggerSimple.logEnd("GetAllUIElements");
            }
        }

        /// <summary>
        /// 获取所有逻辑树元素
        /// </summary>
        /// <returns>逻辑树元素列表</returns>
        private static List<WpfVisualObjectInfo> GetAllLogicalElements()
        {
            var logicalElements = new List<WpfVisualObjectInfo>();

            try
            {
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        var logicalInfo = CreateLogicalObjectInfo(window, null, 0);
                        if (logicalInfo != null)
                        {
                            logicalElements.Add(logicalInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllLogicalElements", 
                    $"Error getting logical elements: {ex.Message}", ex);
            }

            return logicalElements;
        }

        /// <summary>
        /// 获取所有自动化树元素
        /// </summary>
        /// <returns>自动化树元素列表</returns>
        private static List<WpfVisualObjectInfo> GetAllAutomationElements()
        {
            var automationElements = new List<WpfVisualObjectInfo>();

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var rootElement = System.Windows.Automation.AutomationElement.RootElement;
                
                var condition = new System.Windows.Automation.PropertyCondition(
                    System.Windows.Automation.AutomationElement.ProcessIdProperty,
                    currentProcess.Id);

                var elements = rootElement.FindAll(
                    System.Windows.Automation.TreeScope.Subtree,
                    condition);

                foreach (System.Windows.Automation.AutomationElement element in elements)
                {
                    var automationInfo = CreateAutomationObjectInfo(element, null, 0);
                    if (automationInfo != null)
                    {
                        automationElements.Add(automationInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllAutomationElements", 
                    $"Error getting automation elements: {ex.Message}", ex);
            }

            return automationElements;
        }

        /// <summary>
        /// 创建逻辑树对象信息
        /// </summary>
        /// <param name="element">逻辑元素</param>
        /// <param name="parent">父元素</param>
        /// <param name="index">索引</param>
        /// <returns>逻辑对象信息</returns>
        private static WpfVisualObjectInfo CreateLogicalObjectInfo(object element, WpfVisualObjectInfo parent, int index)
        {
            if (element == null) return null;

            try
            {
                var info = new WpfVisualObjectInfo
                {
                    Parent = parent,
                    Index = index,
                    Type = element.GetType().FullName,
                    TypePath = GetTypePath(element.GetType()),
                    RefObject = element
                };

                // 设置基本属性
                if (element is FrameworkElement fe)
                {
                    info.Name = fe.Name ?? "";
                    info.Uid = fe.Uid ?? "";
                    info.Tag = fe.Tag;
                    info.IsVisible = fe.Visibility == Visibility.Visible;
                    info.IsEnabled = fe.IsEnabled;
                }

                // 设置位置
                if (element is Visual visual)
                {
                    info.Position = GetElementBounds(visual);
                }

                // 设置文本内容
                info.Text = GetElementText(element as DependencyObject);

                // 构建名称路径
                info.NamePath = BuildNamePath(info);

                // 递归处理逻辑子元素
                ProcessLogicalChildren(element, info);

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateLogicalObjectInfo", 
                    $"Error creating logical object info: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 处理逻辑子元素
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="parentInfo">父元素信息</param>
        /// <returns>子元素总数</returns>
        private static int ProcessLogicalChildren(object parent, WpfVisualObjectInfo parentInfo)
        {
            try
            {
                int totalChildrenCount = 0;

                if (parent is FrameworkElement fe)
                {
                    // 获取逻辑子元素
                    var logicalChildren = LogicalTreeHelper.GetChildren(fe);
                    int index = 0;

                    foreach (var child in logicalChildren)
                    {
                        var childInfo = CreateLogicalObjectInfo(child, parentInfo, index);
                        if (childInfo != null)
                        {
                            parentInfo.Children.Add(childInfo);
                            totalChildrenCount += 1 + childInfo.AllChildrenCount;
                        }
                        index++;
                    }
                }

                parentInfo.AllChildrenCount = totalChildrenCount;
                return totalChildrenCount;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ProcessLogicalChildren", 
                    $"Error processing logical children: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 创建自动化对象信息
        /// </summary>
        /// <param name="element">自动化元素</param>
        /// <param name="parent">父元素</param>
        /// <param name="index">索引</param>
        /// <returns>自动化对象信息</returns>
        private static WpfVisualObjectInfo CreateAutomationObjectInfo(System.Windows.Automation.AutomationElement element, WpfVisualObjectInfo parent, int index)
        {
            if (element == null) return null;

            try
            {
                var info = new WpfVisualObjectInfo
                {
                    Parent = parent,
                    Index = index,
                    Type = element.Current.ClassName,
                    TypePath = element.Current.ClassName,
                    RefObject = element
                };

                // 设置基本属性
                info.Name = element.Current.Name ?? "";
                info.AutomationId = element.Current.AutomationId ?? "";
                info.Text = element.Current.Name ?? "";

                // 设置位置
                var rect = element.Current.BoundingRectangle;
                info.Position = new System.Drawing.Rectangle(
                    (int)rect.X, (int)rect.Y,
                    (int)rect.Width, (int)rect.Height);

                // 设置可见性和启用状态
                info.IsVisible = !element.Current.IsOffscreen;
                info.IsEnabled = element.Current.IsEnabled;

                // 构建名称路径
                info.NamePath = BuildNamePath(info);

                // 递归处理自动化子元素
                ProcessAutomationChildren(element, info);

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateAutomationObjectInfo", 
                    $"Error creating automation object info: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// 处理自动化子元素
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="parentInfo">父元素信息</param>
        /// <returns>子元素总数</returns>
        private static int ProcessAutomationChildren(System.Windows.Automation.AutomationElement parent, WpfVisualObjectInfo parentInfo)
        {
            try
            {
                int totalChildrenCount = 0;

                var children = parent.FindAll(
                    System.Windows.Automation.TreeScope.Children,
                    System.Windows.Automation.Condition.TrueCondition);

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    var childInfo = CreateAutomationObjectInfo(child, parentInfo, i);
                    if (childInfo != null)
                    {
                        parentInfo.Children.Add(childInfo);
                        totalChildrenCount += 1 + childInfo.AllChildrenCount;
                    }
                }

                parentInfo.AllChildrenCount = totalChildrenCount;
                return totalChildrenCount;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ProcessAutomationChildren", 
                    $"Error processing automation children: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 收集Snoop风格的信息（类似Snoop工具）
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="info">对象信息</param>
        private static void CollectSnoopStyleInfo(DependencyObject element, WpfVisualObjectInfo info)
        {
            try
            {
                // 使用专门的收集器收集所有Snoop风格的信息
                //WpfSnoopStyleInfoCollector.CollectAllSnoopStyleInfo(element, info);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CollectSnoopStyleInfo", 
                    $"Error collecting Snoop style info: {ex.Message}", ex);
            }
        }
    }
}