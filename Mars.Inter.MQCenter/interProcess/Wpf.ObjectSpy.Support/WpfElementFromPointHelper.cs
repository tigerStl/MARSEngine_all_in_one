using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Diagnostics;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.message.Inter.MQCenter.simpleLog;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// 从指定点坐标获取WPF对象的辅助类
    /// 用于在AFX控件中Host的WPF对象识别
    /// </summary>
    public class WpfElementFromPointHelper
    {
        /// <summary>
        /// 从POINT坐标获取WPF对象
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>WPF对象信息，如果未找到返回null</returns>
        public static WpfVisualTreeInspector.WpfVisualObjectInfo GetWpfElementFromPoint(POINT point)
        {
            return GetWpfElementFromPoint(new System.Drawing.Point(point.X, point.Y));
        }

        /// <summary>
        /// 从指定点坐标获取WPF对象
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WPF对象信息，如果未找到返回null</returns>
        public static WpfVisualTreeInspector.WpfVisualObjectInfo GetWpfElementFromPoint(System.Drawing.Point screenPoint)
        {
            MarsLoggerSimple.logBegin("GetWpfElementFromPoint");

            try
            {
                // 方法1：通过窗口句柄获取HwndSource，然后进行HitTest
                var wpfElement = GetWpfElementFromWindowHandle(screenPoint);
                if (wpfElement != null)
                {
                    var wpfObjectInfo = CreateWpfObjectInfoFromElement(wpfElement, screenPoint);
                    if (wpfObjectInfo != null)
                    {
                        MarsLoggerSimple.logEnd("GetWpfElementFromPoint", 
                            $"Successfully found WPF element: {wpfObjectInfo.Name}[{wpfObjectInfo.Type}]");
                        return wpfObjectInfo;
                    }
                }

                // 方法2：通过Application.Current.Windows遍历查找
                wpfElement = GetWpfElementFromApplicationWindows(screenPoint);
                if (wpfElement != null)
                {
                    var wpfObjectInfo = CreateWpfObjectInfoFromElement(wpfElement, screenPoint);
                    if (wpfObjectInfo != null)
                    {
                        MarsLoggerSimple.logEnd("GetWpfElementFromPoint", 
                            $"Successfully found WPF element from Application: {wpfObjectInfo.Name}[{wpfObjectInfo.Type}]");
                        return wpfObjectInfo;
                    }
                }

                // 方法3：通过PresentationSource.CurrentSources查找
                wpfElement = GetWpfElementFromPresentationSources(screenPoint);
                if (wpfElement != null)
                {
                    var wpfObjectInfo = CreateWpfObjectInfoFromElement(wpfElement, screenPoint);
                    if (wpfObjectInfo != null)
                    {
                        MarsLoggerSimple.logEnd("GetWpfElementFromPoint", 
                            $"Successfully found WPF element from PresentationSource: {wpfObjectInfo.Name}[{wpfObjectInfo.Type}]");
                        return wpfObjectInfo;
                    }
                }

                MarsLoggerSimple.Warnning("GetWpfElementFromPoint", 
                    $"Could not find WPF element at point: ({screenPoint.X}, {screenPoint.Y})");
                return null;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromPoint", 
                    $"Error getting WPF element from point: {ex.Message}", ex);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("GetWpfElementFromPoint");
            }
        }

        /// <summary>
        /// 通过窗口句柄获取WPF元素（使用HitTest）
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WPF元素，如果未找到返回null</returns>
        private static DependencyObject GetWpfElementFromWindowHandle(System.Drawing.Point screenPoint)
        {
            try
            {
                // 使用WindowFromPoint获取窗口句柄
                IntPtr hwnd = WindowFromPoint(screenPoint);
                if (hwnd == IntPtr.Zero)
                {
                    MarsLoggerSimple.Info("GetWpfElementFromWindowHandle", 
                        $"WindowFromPoint returned null for point: ({screenPoint.X}, {screenPoint.Y})");
                    return null;
                }
                StringBuilder sb = new StringBuilder();
                sb.Length = 256;
                MarsWindowsAPIs.GetClassName(hwnd, sb, 256);
                if (sb.ToString().StartsWith("WindowsForms10", StringComparison.OrdinalIgnoreCase))
                {
                    ///说明是winform通过System.Windows.Forms.Integration.WinFormsAdapter等技术
                    ///嵌入了wpf额应用
                    ///
                    var c = System.Windows.Forms.Control.FromHandle(hwnd);
                    if (c != null)
                    {
                        MarsLoggerSimple.Info("GetWpfElementFromWindowHandle", $"get .net Framework control from wpf container|objectname:{c.Name}\r\n{c.AccessibleName}\r\n{c.Controls.Count}");
                    }
                }
                // 通过窗口句柄获取HwndSource
                var hwndSource = HwndSource.FromHwnd(hwnd);
                if (hwndSource == null)
                {
                    MarsLoggerSimple.Info("GetWpfElementFromWindowHandle", 
                        "HwndSource is null, may not be a WPF window");
                    return null;
                }

                // 获取RootVisual
                var rootVisual = hwndSource.RootVisual as DependencyObject;
                if (rootVisual == null)
                {
                    MarsLoggerSimple.Warnning("GetWpfElementFromWindowHandle", 
                        "RootVisual is null");
                    return null;
                }

                // 调用HitTest进行元素查找
                DependencyObject hitElement = null;

                // 确保在UI线程中执行
                if (hwndSource.Dispatcher != null && !hwndSource.Dispatcher.CheckAccess())
                {
                    hwndSource.Dispatcher.Invoke(() =>
                    {
                        hitElement = HitTestFromPoint(rootVisual, screenPoint, hwndSource);
                    });
                }
                else
                {
                    hitElement = HitTestFromPoint(rootVisual, screenPoint, hwndSource);
                }

                return hitElement;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromWindowHandle", 
                    $"Error getting WPF element from window handle: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 通过HitTest从指定点获取WPF元素
        /// </summary>
        /// <param name="visual">可视树根节点</param>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <param name="hwndSource">HwndSource</param>
        /// <returns>WPF元素</returns>
        private static DependencyObject HitTestFromPoint(DependencyObject visual, System.Drawing.Point screenPoint, HwndSource hwndSource)
        {
            if (visual == null || !(visual is Visual))
                return null;

            try
            {
                DependencyObject hitElement = null;

                VisualTreeHelper.HitTest(
                    visual as Visual,
                    null,
                    result =>
                    {
                        hitElement = result.VisualHit as DependencyObject;
                        return HitTestResultBehavior.Stop;
                    },
                    new PointHitTestParameters(new System.Windows.Point(screenPoint.X, screenPoint.Y)));

                return hitElement;
            }
            catch (Exception ex)
            {
                // HitTest可能会失败，尝试使用FindElementsInHostCoordinates作为备选
                MarsLoggerSimple.Warnning("HitTestFromPoint", 
                    $"HitTest failed, trying alternative method: {ex.Message}");

                try
                {
                    if (visual is UIElement uiElement)
                    {
                        var wpPoint = uiElement.PointFromScreen(new System.Windows.Point(screenPoint.X, screenPoint.Y));
                        return GetElementFromPointInVisual(uiElement, wpPoint);
                    }
                }
                catch (Exception ex2)
                {
                    MarsLoggerSimple.Error("HitTestFromPoint", 
                        $"Alternative method also failed: {ex2.Message}", ex2);
                }

                return null;
            }
        }

        /// <summary>
        /// 在视觉树中从点获取元素（备选方法）
        /// </summary>
        /// <param name="visual">容器元素</param>
        /// <param name="point">容器相对坐标点</param>
        /// <returns>WPF元素</returns>
        private static DependencyObject GetElementFromPointInVisual(Visual visual, System.Windows.Point point)
        {
            try
            {
                DependencyObject foundElement = null;

                HitTestResultCallback callback = (result) =>
                {
                    // 返回第一个找到的可视元素
                    foundElement = result.VisualHit as DependencyObject;
                    return HitTestResultBehavior.Stop;
                };

                VisualTreeHelper.HitTest(
                    visual,
                    null,
                    callback,
                    new PointHitTestParameters(point));

                return foundElement;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetElementFromPointInVisual", 
                    $"Error getting element from point in visual: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 通过Application.Current.Windows查找WPF元素
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WPF元素，如果未找到返回null</returns>
        private static DependencyObject GetWpfElementFromApplicationWindows(System.Drawing.Point screenPoint)
        {
            try
            {
                if (Application.Current == null)
                {
                    MarsLoggerSimple.Info("GetWpfElementFromApplicationWindows", 
                        "Application.Current is null");
                    return null;
                }

                // 确保在UI线程中执行
                DependencyObject hitElement = null;

                if (Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window != null && IsPointInWindow(window, screenPoint))
                            {
                                hitElement = HitTestFromPointInWindow(window, screenPoint);
                                if (hitElement != null)
                                    break;
                            }
                        }
                    });
                }
                else
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null && IsPointInWindow(window, screenPoint))
                        {
                            hitElement = HitTestFromPointInWindow(window, screenPoint);
                            if (hitElement != null)
                                break;
                        }
                    }
                }

                return hitElement;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromApplicationWindows", 
                    $"Error getting WPF element from Application windows: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 通过PresentationSource.CurrentSources查找WPF元素
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WPF元素，如果未找到返回null</returns>
        private static DependencyObject GetWpfElementFromPresentationSources(System.Drawing.Point screenPoint)
        {
            try
            {
                var sources = PresentationSource.CurrentSources;
                DependencyObject hitElement = null;

                foreach (PresentationSource source in sources)
                {
                    if (source is HwndSource hwndSource && hwndSource.RootVisual != null)
                    {
                        try
                        {
                            // 在对应的Dispatcher中执行
                            if (hwndSource.Dispatcher != null && !hwndSource.Dispatcher.CheckAccess())
                            {
                                hwndSource.Dispatcher.Invoke(() =>
                                {
                                    hitElement = HitTestFromPoint(hwndSource.RootVisual, screenPoint, hwndSource);
                                });
                            }
                            else
                            {
                                hitElement = HitTestFromPoint(hwndSource.RootVisual, screenPoint, hwndSource);
                            }

                            if (hitElement != null)
                                break;
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Warnning("GetWpfElementFromPresentationSources", 
                                $"Error in source: {ex.Message}");
                        }
                    }
                }

                return hitElement;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromPresentationSources", 
                    $"Error getting WPF element from PresentationSource: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 检查点是否在窗口内
        /// </summary>
        /// <param name="window">WPF窗口</param>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>是否在窗口内</returns>
        private static bool IsPointInWindow(Window window, System.Drawing.Point screenPoint)
        {
            try
            {
                var windowPoint = window.PointFromScreen(new System.Windows.Point(screenPoint.X, screenPoint.Y));
                var windowRect = new System.Windows.Rect(0, 0, window.ActualWidth, window.ActualHeight);
                return windowRect.Contains(windowPoint);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 在窗口中从点进行HitTest
        /// </summary>
        /// <param name="window">WPF窗口</param>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WPF元素</returns>
        private static DependencyObject HitTestFromPointInWindow(Window window, System.Drawing.Point screenPoint)
        {
            try
            {
                var windowPoint = window.PointFromScreen(new System.Windows.Point(screenPoint.X, screenPoint.Y));
                return GetElementFromPointInVisual(window, windowPoint);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("HitTestFromPointInWindow", 
                    $"Error in hit test from point in window: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从WPF元素创建WpfVisualObjectInfo
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>WpfVisualObjectInfo对象</returns>
        private static WpfVisualTreeInspector.WpfVisualObjectInfo CreateWpfObjectInfoFromElement(DependencyObject element, System.Drawing.Point screenPoint)
        {
            if (element == null)
                return null;

            try
            {
                var info = new WpfVisualTreeInspector.WpfVisualObjectInfo
                {
                    Type = element.GetType().FullName,
                    RefObject = element
                };

                // 设置名称和其他属性
                if (element is FrameworkElement fe)
                {
                    info.Name = fe.Name ?? "";
                    info.Uid = fe.Uid ?? "";
                    info.Tag = fe.Tag;
                    info.IsVisible = fe.Visibility == Visibility.Visible;
                    info.IsEnabled = fe.IsEnabled;
                }

                // 设置AutomationId
                info.AutomationId = System.Windows.Automation.AutomationProperties.GetAutomationId(element) ?? "";

                // 设置文本内容
                info.Text = GetElementText(element);

                // 设置位置
                info.Position = GetElementBounds(element);

                // 设置Index
                if (element != null)
                {
                    var parent = VisualTreeHelper.GetParent(element);
                    if (parent != null)
                    {
                        var childCount = VisualTreeHelper.GetChildrenCount(parent);
                        for (int i = 0; i < childCount; i++)
                        {
                            if (VisualTreeHelper.GetChild(parent, i) == element)
                            {
                                info.Index = i;
                                break;
                            }
                        }
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateWpfObjectInfoFromElement", 
                    $"Error creating WpfObjectInfo: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取元素文本内容
        /// </summary>
        private static string GetElementText(DependencyObject element)
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
        /// Windows API: 获取指定点处的窗口句柄
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>窗口句柄</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point point);
    }
}

