using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.interProcess;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// WinForm嵌入WPF的混合对象信息
    /// </summary>
    public class HybridObjectInfo
    {
        /// <summary>
        /// WinForm控件对象
        /// </summary>
        public Control WinFormControl { get; set; }

        /// <summary>
        /// WPF元素对象
        /// </summary>
        public DependencyObject WpfElement { get; set; }

        /// <summary>
        /// 对象类型：WinForm或WPF
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// 对象名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对象类型全名
        /// </summary>
        public string TypeFullName { get; set; }

        /// <summary>
        /// 对象文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public System.Drawing.Rectangle Bounds { get; set; }

        /// <summary>
        /// 父对象
        /// </summary>
        public HybridObjectInfo Parent { get; set; }

        /// <summary>
        /// 子对象列表
        /// </summary>
        public List<HybridObjectInfo> Children { get; set; } = new List<HybridObjectInfo>();

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// 转换为MarsSpiedObjectInfo
        /// </summary>
        public MarsSpiedObjectInfo ToMarsSpiedObjectInfo()
        {
            var info = new MarsSpiedObjectInfo();
            
            if (WinFormControl != null)
            {
                info.objectName = WinFormControl.Name ?? "";
                info.objectType = WinFormControl.GetType().FullName;
                info.Text = WinFormControl.Text ?? "";
                info.x = WinFormControl.Left;
                info.y = WinFormControl.Top;
                info.w = WinFormControl.Width;
                info.h = WinFormControl.Height;
                info.isVisible = WinFormControl.Visible;
                info.referenceToObj = WinFormControl;
                info.hwnd = WinFormControl.Handle.ToInt64();
            }
            else if (WpfElement != null)
            {
                if (WpfElement is FrameworkElement fe)
                {
                    info.objectName = fe.Name ?? "";
                    info.objectType = fe.GetType().FullName;
                    info.isVisible = fe.Visibility == Visibility.Visible;
                    info.referenceToObj = fe;
                    
                    try
                    {
                        var point = fe.PointToScreen(new System.Windows.Point(0, 0));
                        var size = fe.RenderSize;
                        info.x = (int)point.X;
                        info.y = (int)point.Y;
                        info.w = (int)size.Width;
                        info.h = (int)size.Height;
                    }
                    catch { }
                }
            }
            else
            {
                info.objectName = Name ?? "";
                info.objectType = TypeFullName ?? "";
                info.Text = Text ?? "";
                info.x = Bounds.X;
                info.y = Bounds.Y;
                info.w = Bounds.Width;
                info.h = Bounds.Height;
            }

            // 转换子对象
            if (Children != null && Children.Count > 0)
            {
                info.children = new List<MarsSpiedObjectInfo>();
                foreach (var child in Children)
                {
                    var childInfo = child.ToMarsSpiedObjectInfo();
                    if (childInfo != null)
                    {
                        info.children.Add(childInfo);
                    }
                }
            }

            return info;
        }
    }

    /// <summary>
    /// WinForm嵌入WPF的混合对象处理辅助类
    /// </summary>
    public class WinFormWpfHybridHelper
    {
        /// <summary>
        /// 从WinForm控件构建混合对象树（包括所有父对象和子对象）
        /// </summary>
        /// <param name="control">WinForm控件</param>
        /// <param name="targetHwnd">目标窗口句柄</param>
        /// <returns>混合对象树根节点</returns>
        public static HybridObjectInfo BuildHybridObjectTree(System.Windows.Forms.Control control, IntPtr targetHwnd)
        {
            MarsLoggerSimple.logBegin("BuildHybridObjectTree");

            try
            {
                if (control == null)
                {
                    MarsLoggerSimple.Warnning("BuildHybridObjectTree", "Control is null");
                    return null;
                }

                // 1. 构建父对象链（向上到null）
                var parentChain = BuildParentChain(control);

                // 2. 从根节点开始，构建完整的子对象树
                var rootNode = parentChain.Count > 0 ? parentChain[0] : CreateHybridObjectInfo(control, targetHwnd);

                // 3. 递归构建所有子对象
                BuildChildrenTree(rootNode, targetHwnd);

                MarsLoggerSimple.logEnd("BuildHybridObjectTree", 
                    $"Built tree with root: {rootNode.Name}[{rootNode.TypeFullName}]");
                
                return rootNode;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildHybridObjectTree", 
                    $"Error building hybrid object tree: {ex.Message}", ex);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("BuildHybridObjectTree");
            }
        }

        /// <summary>
        /// 构建父对象链（向上到null）
        /// </summary>
        /// <param name="control">起始控件</param>
        /// <returns>父对象列表（从根到当前控件）</returns>
        private static List<HybridObjectInfo> BuildParentChain(Control control)
        {
            var parentChain = new List<HybridObjectInfo>();
            
            try
            {
                Control current = control;
                
                // 向上遍历到根
                while (current != null)
                {
                    var info = CreateHybridObjectInfo(current, current.Handle);
                    if (info != null)
                    {
                        parentChain.Insert(0, info); // 插入到开头，保证顺序从根到当前
                    }
                    
                    current = current.Parent;
                }

                // 设置父子关系
                for (int i = 0; i < parentChain.Count - 1; i++)
                {
                    parentChain[i + 1].Parent = parentChain[i];
                    parentChain[i].Children.Add(parentChain[i + 1]);
                }

                MarsLoggerSimple.Info("BuildParentChain", 
                    $"Built parent chain with {parentChain.Count} nodes");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildParentChain", 
                    $"Error building parent chain: {ex.Message}", ex);
            }

            return parentChain;
        }

        /// <summary>
        /// 递归构建所有子对象树
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="targetHwnd">目标窗口句柄</param>
        private static void BuildChildrenTree(HybridObjectInfo node, IntPtr targetHwnd)
        {
            if (node == null) return;

            try
            {
                // 如果是WinForm控件，获取其子控件
                if (node.WinFormControl != null)
                {
                    foreach (Control childControl in node.WinFormControl.Controls)
                    {
                        var childInfo = CreateHybridObjectInfo(childControl, targetHwnd);
                        if (childInfo != null)
                        {
                            childInfo.Parent = node;
                            node.Children.Add(childInfo);

                            // 检查是否是WindowsForms10控件，可能包含WPF
                            StringBuilder sb = new StringBuilder(256);
                            if (MarsWindowsAPIs.GetClassName(childControl.Handle, sb, 256) > 0)
                            {
                                string className = sb.ToString();
                                if (className.StartsWith("WindowsForms10", StringComparison.OrdinalIgnoreCase))
                                {
                                    // 尝试获取嵌入的WPF元素
                                    var wpfElements = GetWpfElementsFromWinFormControl(childControl);
                                    foreach (var wpfElement in wpfElements)
                                    {
                                        var wpfInfo = CreateHybridObjectInfoFromWpf(wpfElement);
                                        if (wpfInfo != null)
                                        {
                                            wpfInfo.Parent = childInfo;
                                            childInfo.Children.Add(wpfInfo);

                                            // 递归构建WPF元素的子对象
                                            BuildWpfChildrenTree(wpfInfo);
                                        }
                                    }
                                }
                            }

                            // 递归构建子控件的子对象
                            BuildChildrenTree(childInfo, targetHwnd);
                        }
                    }
                }
                // 如果是WPF元素，已经在BuildWpfChildrenTree中处理
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildChildrenTree", 
                    $"Error building children tree: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归构建WPF元素的子对象树
        /// </summary>
        /// <param name="node">当前WPF节点</param>
        private static void BuildWpfChildrenTree(HybridObjectInfo node)
        {
            if (node == null || node.WpfElement == null) return;

            try
            {
                // 确保在UI线程中执行
                // 使用WPF的Application，需要明确指定命名空间
                var wpfApp = System.Windows.Application.Current;
                if (wpfApp != null && wpfApp.Dispatcher != null && !wpfApp.Dispatcher.CheckAccess())
                {
                    wpfApp.Dispatcher.Invoke(() =>
                    {
                        BuildWpfChildrenTreeOnUIThread(node);
                    });
                }
                else
                {
                    BuildWpfChildrenTreeOnUIThread(node);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildWpfChildrenTree", 
                    $"Error building WPF children tree: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 在UI线程上构建WPF子对象树
        /// </summary>
        private static void BuildWpfChildrenTreeOnUIThread(HybridObjectInfo node)
        {
            if (node == null || node.WpfElement == null) return;

            try
            {
                var childCount = VisualTreeHelper.GetChildrenCount(node.WpfElement);
                for (int i = 0; i < childCount; i++)
                {
                    var childElement = VisualTreeHelper.GetChild(node.WpfElement, i);
                    var childInfo = CreateHybridObjectInfoFromWpf(childElement);
                    if (childInfo != null)
                    {
                        childInfo.Parent = node;
                        node.Children.Add(childInfo);

                        // 递归构建子元素
                        BuildWpfChildrenTree(childInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildWpfChildrenTreeOnUIThread", 
                    $"Error building WPF children tree on UI thread: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从WinForm控件创建HybridObjectInfo
        /// </summary>
        private static HybridObjectInfo CreateHybridObjectInfo(Control control, IntPtr targetHwnd)
        {
            if (control == null) return null;

            try
            {
                return new HybridObjectInfo
                {
                    WinFormControl = control,
                    ObjectType = "WinForm",
                    Name = control.Name ?? "",
                    TypeFullName = control.GetType().FullName,
                    Text = control.Text ?? "",
                    Bounds = new System.Drawing.Rectangle(control.Left, control.Top, control.Width, control.Height),
                    Handle = control.Handle
                };
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateHybridObjectInfo", 
                    $"Error creating HybridObjectInfo from control: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从WPF元素创建HybridObjectInfo
        /// </summary>
        private static HybridObjectInfo CreateHybridObjectInfoFromWpf(DependencyObject wpfElement)
        {
            if (wpfElement == null) return null;

            try
            {
                var info = new HybridObjectInfo
                {
                    WpfElement = wpfElement,
                    ObjectType = "WPF",
                    TypeFullName = wpfElement.GetType().FullName
                };

                if (wpfElement is FrameworkElement fe)
                {
                    info.Name = fe.Name ?? "";
                    info.Text = GetWpfElementText(fe);

                    try
                    {
                        var point = fe.PointToScreen(new System.Windows.Point(0, 0));
                        var size = fe.RenderSize;
                        info.Bounds = new System.Drawing.Rectangle(
                            (int)point.X, (int)point.Y,
                            (int)size.Width, (int)size.Height);
                    }
                    catch { }
                }

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateHybridObjectInfoFromWpf", 
                    $"Error creating HybridObjectInfo from WPF element: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取WPF元素文本
        /// </summary>
        private static string GetWpfElementText(FrameworkElement element)
        {
            try
            {
                if (element is TextBlock textBlock)
                    return textBlock.Text ?? "";
                if (element is TextBox textBox)
                    return textBox.Text ?? "";
                if (element is Label label)
                    return label.Content?.ToString() ?? "";
                if (element is Button button)
                    return button.Content?.ToString() ?? "";
                if (element is ContentControl contentControl)
                    return contentControl.Content?.ToString() ?? "";
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 从WinForm控件获取嵌入的WPF元素
        /// </summary>
        private static List<DependencyObject> GetWpfElementsFromWinFormControl(Control control)
        {
            var wpfElements = new List<DependencyObject>();

            try
            {
                // 尝试通过HwndSource获取WPF元素
                var hwndSource = HwndSource.FromHwnd(control.Handle);
                if (hwndSource != null && hwndSource.RootVisual != null)
                {
                    // 确保在UI线程中执行
                    if (hwndSource.Dispatcher != null && !hwndSource.Dispatcher.CheckAccess())
                    {
                        hwndSource.Dispatcher.Invoke(() =>
                        {
                            wpfElements.Add(hwndSource.RootVisual);
                        });
                    }
                    else
                    {
                        wpfElements.Add(hwndSource.RootVisual);
                    }
                }

                // 尝试通过反射查找ElementHost或其他WPF宿主控件
                var elementHost = control as System.Windows.Forms.Integration.ElementHost;
                if (elementHost != null && elementHost.Child != null)
                {
                    wpfElements.Add(elementHost.Child);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warnning("GetWpfElementsFromWinFormControl", 
                    $"Error getting WPF elements from WinForm control: {ex.Message}");
            }

            return wpfElements;
        }

        /// <summary>
        /// 将混合对象树转换为MarsSpiedObjectInfo列表
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <returns>MarsSpiedObjectInfo列表</returns>
        public static List<MarsSpiedObjectInfo> ConvertToMarsObjects(HybridObjectInfo rootNode)
        {
            var marsObjects = new List<MarsSpiedObjectInfo>();

            try
            {
                if (rootNode != null)
                {
                    var marsObj = rootNode.ToMarsSpiedObjectInfo();
                    if (marsObj != null)
                    {
                        marsObjects.Add(marsObj);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ConvertToMarsObjects", 
                    $"Error converting hybrid objects to Mars objects: {ex.Message}", ex);
            }

            return marsObjects;
        }
    }
}
