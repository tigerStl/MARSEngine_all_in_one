using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// WPF可视元素图像捕获辅助类
    /// 用于通过VisualTreeHelper获取WPF对象的界面图像
    /// </summary>
    public class WpfVisualCaptureHelper
    {
        /// <summary>
        /// 为WPF对象捕获界面图像
        /// </summary>
        /// <param name="wpfObject">WPF对象信息</param>
        /// <returns>图像文件路径，如果捕获失败返回null</returns>
        public static string CaptureWpfObjectImage(WpfVisualTreeInspector.WpfVisualObjectInfo wpfObject)
        {
            if (wpfObject == null)
            {
                MarsLoggerSimple.Warnning("CaptureWpfObjectImage", "WpfObject is null");
                return null;
            }

            try
            {
                MarsLoggerSimple.logBegin("CaptureWpfObjectImage");

                // 确保在UI线程中执行
                if (Application.Current?.Dispatcher != null)
                {
                    string result = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = CaptureWpfObjectImageOnUIThread(wpfObject);
                    });
                    return result;
                }
                else
                {
                    // 如果没有UI线程，尝试直接执行（可能失败）
                    return CaptureWpfObjectImageOnUIThread(wpfObject);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CaptureWpfObjectImage", 
                    $"Error capturing WPF object image: {ex.Message}", ex);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("CaptureWpfObjectImage");
            }
        }

        /// <summary>
        /// 在UI线程中执行WPF对象图像捕获
        /// </summary>
        /// <param name="wpfObject">WPF对象信息</param>
        /// <returns>图像文件路径，如果捕获失败返回null</returns>
        private static string CaptureWpfObjectImageOnUIThread(WpfVisualTreeInspector.WpfVisualObjectInfo wpfObject)
        {
            try
            {
                // 获取WPF元素引用
                var wpfElement = GetWpfElementFromInfo(wpfObject);
                if (wpfElement == null)
                {
                    MarsLoggerSimple.Warnning("CaptureWpfObjectImageOnUIThread", 
                        $"Cannot get WPF element for object: {wpfObject.Name}");
                    return null;
                }

                // 检查元素是否可见
                if (wpfElement is UIElement uiElement && !uiElement.IsVisible)
                {
                    MarsLoggerSimple.Info("CaptureWpfObjectImageOnUIThread", 
                        $"Element is not visible: {wpfObject.Name}");
                    return null;
                }

                // 获取元素的边界
                var bounds = GetElementBounds(wpfElement);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    MarsLoggerSimple.Warnning("CaptureWpfObjectImageOnUIThread", 
                        $"Element has invalid bounds: {bounds}");
                    return null;
                }

                // 创建图像文件名
                var fileName = GenerateImageFileName(wpfObject);
                var filePath = Path.Combine(GetTempImageDirectory(), fileName);

                // 捕获图像
                var success = CaptureElementImage(wpfElement, bounds, filePath);
                if (success)
                {
                    MarsLoggerSimple.Info("CaptureWpfObjectImageOnUIThread", 
                        $"Successfully captured image: {filePath}");
                    return filePath;
                }
                else
                {
                    MarsLoggerSimple.Error("CaptureWpfObjectImageOnUIThread", 
                        "Failed to capture element image");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CaptureWpfObjectImageOnUIThread", 
                    $"Error capturing WPF object image on UI thread: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 为MarsSpiedObjectInfo对象捕获WPF图像
        /// </summary>
        /// <param name="marsObject">Mars对象信息</param>
        /// <returns>图像文件路径，如果捕获失败返回null</returns>
        public static string CaptureMarsObjectImage(MarsSpiedObjectInfo marsObject)
        {
            if (marsObject == null)
            {
                MarsLoggerSimple.Info("CaptureMarsObjectImage", "MarsObject is null");
                return null;
            }

            try
            {
                // 确保在UI线程中执行
                if (Application.Current?.Dispatcher != null)
                {
                    string result = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = CaptureMarsObjectImageOnUIThread(marsObject);
                    });
                    return result;
                }
                else
                {
                    // 如果没有UI线程，尝试直接执行（可能失败）
                    return CaptureMarsObjectImageOnUIThread(marsObject);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CaptureMarsObjectImage", 
                    $"Error capturing Mars object image: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 在UI线程中执行Mars对象图像捕获
        /// </summary>
        /// <param name="marsObject">Mars对象信息</param>
        /// <returns>图像文件路径，如果捕获失败返回null</returns>
        private static string CaptureMarsObjectImageOnUIThread(MarsSpiedObjectInfo marsObject)
        {
            try
            {
                // 尝试从referenceToObj获取WPF元素
                var wpfElement = GetWpfElementFromMarsObject(marsObject);
                if (wpfElement == null)
                {
                    MarsLoggerSimple.Warnning("CaptureMarsObjectImageOnUIThread", 
                        $"Cannot get WPF element from MarsObject: {marsObject.objectName}");
                    return null;
                }

                // 创建WpfVisualObjectInfo用于图像捕获
                var wpfInfo = CreateWpfVisualObjectInfoFromElement(wpfElement, marsObject);
                if (wpfInfo == null)
                {
                    MarsLoggerSimple.Warnning("CaptureMarsObjectImageOnUIThread", 
                        "Failed to create WpfVisualObjectInfo from element");
                    return null;
                }

                return CaptureWpfObjectImageOnUIThread(wpfInfo);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CaptureMarsObjectImageOnUIThread", 
                    $"Error capturing Mars object image on UI thread: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从WpfVisualObjectInfo获取WPF元素
        /// </summary>
        /// <param name="wpfObject">WPF对象信息</param>
        /// <returns>WPF元素，如果获取失败返回null</returns>
        private static DependencyObject GetWpfElementFromInfo(WpfVisualTreeInspector.WpfVisualObjectInfo wpfObject)
        {
            try
            {
                // 方法1：优先从RefObject获取WPF元素引用
                if (wpfObject.RefObject is DependencyObject refElement)
                {
                    MarsLoggerSimple.Info("GetWpfElementFromInfo", 
                        $"Successfully got WPF element from RefObject: {wpfObject.Name}");
                    return refElement;
                }

                // 方法2：通过窗口句柄获取
                if (wpfObject.Position.Width > 0 && wpfObject.Position.Height > 0)
                {
                    var hwnd = GetHwndFromPosition(wpfObject.Position);
                    if (hwnd != IntPtr.Zero)
                    {
                        var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                        if (hwndSource?.RootVisual != null)
                        {
                            return FindElementByBounds(hwndSource.RootVisual, wpfObject.Position);
                        }
                    }
                }

                // 方法3：通过Application.Current.Windows查找
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        var element = FindElementByBounds(window, wpfObject.Position);
                        if (element != null)
                        {
                            return element;
                        }
                    }
                }

                MarsLoggerSimple.Warnning("GetWpfElementFromInfo", 
                    $"Could not find WPF element for: {wpfObject.Name}");
                return null;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromInfo", 
                    $"Error getting WPF element: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从MarsSpiedObjectInfo获取WPF元素
        /// </summary>
        /// <param name="marsObject">Mars对象信息</param>
        /// <returns>WPF元素，如果获取失败返回null</returns>
        private static DependencyObject GetWpfElementFromMarsObject(MarsSpiedObjectInfo marsObject)
        {
            try
            {
                // 如果referenceToObj是WPF元素，直接返回
                if (marsObject.referenceToObj is DependencyObject wpfElement)
                {
                    return wpfElement;
                }

                // 否则通过位置信息查找
                var position = new Rectangle(marsObject.x, marsObject.y, marsObject.w, marsObject.h);
                return FindElementByPosition(position);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementFromMarsObject", 
                    $"Error getting WPF element from Mars object: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 通过位置查找WPF元素
        /// </summary>
        /// <param name="position">位置信息</param>
        /// <returns>找到的WPF元素</returns>
        private static DependencyObject FindElementByPosition(Rectangle position)
        {
            try
            {
                // 确保在UI线程中执行
                if (Application.Current?.Dispatcher != null)
                {
                    DependencyObject result = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = FindElementByPositionOnUIThread(position);
                    });
                    return result;
                }
                else
                {
                    // 如果没有UI线程，尝试直接执行（可能失败）
                    return FindElementByPositionOnUIThread(position);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindElementByPosition", 
                    $"Error finding element by position: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 在UI线程中通过位置查找WPF元素
        /// </summary>
        /// <param name="position">位置信息</param>
        /// <returns>找到的WPF元素</returns>
        private static DependencyObject FindElementByPositionOnUIThread(Rectangle position)
        {
            try
            {
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        var element = FindElementByBounds(window, position);
                        if (element != null)
                        {
                            return element;
                        }
                    }
                }

                // 尝试从PresentationSource获取
                var sources = PresentationSource.CurrentSources;
                foreach (PresentationSource source in sources)
                {
                    if (source is System.Windows.Interop.HwndSource hwndSource && hwndSource.RootVisual != null)
                    {
                        var element = FindElementByBounds(hwndSource.RootVisual, position);
                        if (element != null)
                        {
                            return element;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindElementByPositionOnUIThread", 
                    $"Error finding element by position on UI thread: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 在指定容器中通过边界查找元素
        /// </summary>
        /// <param name="container">容器元素</param>
        /// <param name="targetBounds">目标边界</param>
        /// <returns>找到的元素</returns>
        private static DependencyObject FindElementByBounds(DependencyObject container, Rectangle targetBounds)
        {
            try
            {
                return FindElementByBounds(container, new System.Windows.Rect(
                    targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height));
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindElementByBounds", 
                    $"Error finding element by bounds: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 在指定容器中通过边界查找元素
        /// </summary>
        /// <param name="container">容器元素</param>
        /// <param name="targetBounds">目标边界</param>
        /// <returns>找到的元素</returns>
        private static DependencyObject FindElementByBounds(DependencyObject container, System.Windows.Rect targetBounds)
        {
            try
            {
                if (container == null) return null;

                // 检查当前元素是否匹配
                var elementBounds = GetElementBounds(container);
                if (elementBounds.Width > 0 && elementBounds.Height > 0 &&
                    Math.Abs(elementBounds.X - targetBounds.X) < 5 &&
                    Math.Abs(elementBounds.Y - targetBounds.Y) < 5 &&
                    Math.Abs(elementBounds.Width - targetBounds.Width) < 5 &&
                    Math.Abs(elementBounds.Height - targetBounds.Height) < 5)
                {
                    return container;
                }

                // 递归查找子元素
                var childCount = VisualTreeHelper.GetChildrenCount(container);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(container, i);
                    var found = FindElementByBounds(child, targetBounds);
                    if (found != null)
                    {
                        return found;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindElementByBounds", 
                    $"Error finding element by bounds: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取元素的边界
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <returns>元素边界</returns>
        private static System.Windows.Rect GetElementBounds(DependencyObject element)
        {
            try
            {
                if (element is FrameworkElement fe)
                {
                    // 获取元素在屏幕上的位置
                    var point = fe.PointToScreen(new System.Windows.Point(0, 0));
                    return new System.Windows.Rect(point, fe.RenderSize);
                }
                else if (element is Visual visual)
                {
                    // 对于非FrameworkElement的Visual对象
                    var bounds = VisualTreeHelper.GetDescendantBounds(visual);
                    var point = visual.PointToScreen(new System.Windows.Point(0, 0));
                    return new System.Windows.Rect(point, bounds.Size);
                }

                return new System.Windows.Rect(0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetElementBounds", 
                    $"Error getting element bounds: {ex.Message}", ex);
                return new System.Windows.Rect(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// 从位置获取窗口句柄
        /// </summary>
        /// <param name="position">位置信息</param>
        /// <returns>窗口句柄</returns>
        private static IntPtr GetHwndFromPosition(Rectangle position)
        {
            try
            {
                // 使用Windows API获取指定位置的窗口句柄
                var point = new System.Drawing.Point(position.X + position.Width / 2, 
                                                   position.Y + position.Height / 2);
                
                // 使用Windows API的WindowFromPoint函数
                var hwnd = WindowFromPoint(point);
                return hwnd;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetHwndFromPosition", 
                    $"Error getting HWND from position: {ex.Message}", ex);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Windows API: 获取指定点处的窗口句柄
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>窗口句柄</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point point);

        /// <summary>
        /// 创建WpfVisualObjectInfo从WPF元素
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="marsObject">Mars对象信息</param>
        /// <returns>WpfVisualObjectInfo对象</returns>
        private static WpfVisualTreeInspector.WpfVisualObjectInfo CreateWpfVisualObjectInfoFromElement(DependencyObject element, MarsSpiedObjectInfo marsObject)
        {
            try
            {
                var bounds = GetElementBounds(element);
                return new WpfVisualTreeInspector.WpfVisualObjectInfo
                {
                    Name = marsObject.objectName,
                    Type = marsObject.objectType,
                    Position = new Rectangle(
                        (int)bounds.X, (int)bounds.Y, 
                        (int)bounds.Width, (int)bounds.Height),
                    Text = marsObject.Text,
                    IsVisible = marsObject.isVisible,
                    IsEnabled = marsObject.isEnabled,
                    RefObject = element  // 保存WPF元素引用
                };
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateWpfVisualObjectInfoFromElement", 
                    $"Error creating WpfVisualObjectInfo: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 捕获元素图像
        /// </summary>
        /// <param name="element">WPF元素</param>
        /// <param name="bounds">元素边界</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否成功</returns>
        private static bool CaptureElementImage(DependencyObject element, System.Windows.Rect bounds, string filePath)
        {
            try
            {
                // 创建RenderTargetBitmap
                var dpiX = 96.0; // 默认DPI
                var dpiY = 96.0;
                
                var renderTarget = new RenderTargetBitmap(
                    (int)bounds.Width, (int)bounds.Height, dpiX, dpiY, PixelFormats.Pbgra32);

                // 渲染元素
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var brush = new VisualBrush(element as Visual);
                    drawingContext.DrawRectangle(brush, null, bounds);
                }

                renderTarget.Render(drawingVisual);

                // 转换为Bitmap并保存
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                return true;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CaptureElementImage", 
                    $"Error capturing element image: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 生成图像文件名
        /// </summary>
        /// <param name="wpfObject">WPF对象信息</param>
        /// <returns>文件名</returns>
        private static string GenerateImageFileName(WpfVisualTreeInspector.WpfVisualObjectInfo wpfObject)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var name = string.IsNullOrEmpty(wpfObject.Name) ? "Unknown" : wpfObject.Name;
                var type = string.IsNullOrEmpty(wpfObject.Type) ? "Unknown" : wpfObject.Type.Split('.').LastOrDefault();
                
                return $"WpfCapture_{type}_{name}_{timestamp}.png";
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GenerateImageFileName", 
                    $"Error generating image file name: {ex.Message}", ex);
                return $"WpfCapture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            }
        }

        /// <summary>
        /// 获取临时图像目录
        /// </summary>
        /// <returns>临时目录路径</returns>
        private static string GetTempImageDirectory()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "MarsWpfCapture");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                return tempDir;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetTempImageDirectory", 
                    $"Error getting temp image directory: {ex.Message}", ex);
                return Path.GetTempPath();
            }
        }
    }
}