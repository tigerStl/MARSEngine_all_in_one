using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support;

namespace Mars.Inter.MQCenter.interProcess
{
    //public class ControlInfo
    //{
    //    public string Name { get; set; }
    //    public string Type { get; set; }
    //    public string Size { get; set; }
    //    public string Position { get; set; }
    //    public string Text { get; set; }
    //    public List<string> ListItems { get; set; }
    //    public List<ColumnInfo> DataTableColumns { get; set; }
    //    public bool? IsChecked { get; set; }
    //}
    


    public class MarsWpfInspector
    {
        private static void LogPresentationSourceInfo(PresentationSource source)
        {
            //var log = new StringBuilder();

            // 记录 PresentationSource 的类型
            MarsLoggerSimple.Info("LogPresentationSourceInfo", $"PresentationSource Type: {source.GetType().Name}");

            // 检查是否是 HwndSource
            if (source is HwndSource hwndSource)
            {
                MarsLoggerSimple.Info("LogPresentationSourceInfo", $"- HwndSource Handle: {hwndSource.Handle}");
                MarsLoggerSimple.Info("LogPresentationSourceInfo", $"- HwndSource RootVisual Type: {hwndSource.RootVisual?.GetType().Name}");
                MarsLoggerSimple.Info("LogPresentationSourceInfo", $"- HwndSource RootVisual Name: {(hwndSource.RootVisual as FrameworkElement)?.Name}");
            }

            // 记录 CompositionTarget（渲染目标）
            if (source.CompositionTarget != null)
            {
                MarsLoggerSimple.Info("LogPresentationSourceInfo", $"- CompositionTarget Type: {source.CompositionTarget.GetType().Name}");
            }

            // 输出日志
            //Console.WriteLine(log.ToString());
        }

        public static void FindMainWindow()
        {
            MarsLoggerSimple.logBegin("FindMainWindow");
            // 获取当前进程
            var currentProcess = Process.GetCurrentProcess();

            // 获取当前进程的所有窗口
            var windows = AutomationElement.RootElement.FindAll(
                TreeScope.Subtree,
                new PropertyCondition(AutomationElement.ProcessIdProperty, currentProcess.Id)
            );

            var sources = PresentationSource.CurrentSources;
            MarsLoggerSimple.Info("FindMainWindow", "begin LogPresentationSourceInfo");
            // 遍历每个 PresentationSource
            foreach (PresentationSource source in sources)
            {
                LogPresentationSourceInfo(source);
            }
            MarsLoggerSimple.Info("FindMainWindow", "end LogPresentationSourceInfo");
            IntPtr hwnd = currentProcess.MainWindowHandle;
            MarsLoggerSimple.Info("FindMainWindow", $"currentProcess.MainWindowHandle|{hwnd}|{currentProcess.MainWindowTitle}|{currentProcess.MainModule.ModuleName}|{currentProcess.MainModule.FileName}|{currentProcess.MainModule.ModuleMemorySize}|{currentProcess.MainModule.FileVersionInfo.FileDescription}|{currentProcess.MainModule.FileVersionInfo.FileVersion}|{currentProcess.MainModule.FileVersionInfo.ProductVersion}|{currentProcess.MainModule.FileVersionInfo.OriginalFilename}|{currentProcess.MainModule.FileVersionInfo.Comments}|{currentProcess.MainModule.FileVersionInfo.CompanyName}|{currentProcess.MainModule.FileVersionInfo.InternalName}|{currentProcess.MainModule.FileVersionInfo.LegalCopyright}");
            HwndSource tmpMainSource = HwndSource.FromHwnd(hwnd);
            MarsLoggerSimple.Info("FindMainWindow", $"find tmpMainSource|{tmpMainSource}|");
            if (tmpMainSource != null)
            {
                var tmpW = tmpMainSource.RootVisual as System.Windows.Window;
                if (tmpMainSource.RootVisual != null)
                {
                    MarsLoggerSimple.Info("FindMainWindow", $"tmpMainSource|{tmpMainSource.RootVisual.GetType().FullName}|{tmpW.Title}");
                }
                else
                {
                    MarsLoggerSimple.Info("FindMainWindow", $"tmpMainSource|null|");
                }
                
            }
            // 遍历窗口，找到主窗口
            foreach (AutomationElement window in windows)
            {
                MarsLoggerSimple.Info("FindMainWindow", $"window|{window?.GetType().FullName}|{window?.ToString()}");
                if (!window.Current.NativeWindowHandle.Equals( IntPtr.Zero))
                {
                    // 假设第一个顶级窗口是主窗口
                    MarsLoggerSimple.Info("FindMainWindow", $"Found  Window: {window.Current.Name}|AutomationId|{window.Current.AutomationId}|{window.Current.NativeWindowHandle}");

                    HwndSource source = HwndSource.FromHwnd((IntPtr)window.Current.NativeWindowHandle);
                    MarsLoggerSimple.Info("FindMainWindow", $"find source|{source}|");
                    if (source == null) continue;
                    
                    var w = source.RootVisual as System.Windows.Window;
                    if (w != null)
                    {
                        MarsLoggerSimple.Info("FindMainWindow", $"Active Window Title: {w.Title}");
                    }
                    else
                    {
                        MarsLoggerSimple.Info("FindMainWindow", "No WPF window associated with the active window.");
                    }
                    //break;
                }
            }
            MarsLoggerSimple.logEnd("FindMainWindow");
        }

        public static void GetActiveWindow()
        {
            MarsLoggerSimple.logBegin("GetActiveWindow");
            System.Threading.Thread.Sleep(5000);
            // 获取当前活动窗口的句柄
            IntPtr hwnd = MarsWindowsAPIs.GetForegroundWindow();

            // 通过句柄获取 HwndSource
            HwndSource source = HwndSource.FromHwnd(hwnd);
            MarsLoggerSimple.Info("GetActiveWindow", $"find source|{source}");
            if (source != null)
            {
                // 获取关联的 WPF 窗口
                var window = source.RootVisual as System.Windows.Window;
                MarsLoggerSimple.Info("GetActiveWindow", $"find window rootVisual|{window?.Uid}|{window?.Title}|{window?.GetType().FullName}");
                if (window != null)
                {
                    MarsLoggerSimple.Info("GetActiveWindow", $"Active Window Title: {window.Title}");
                }
                else
                {
                    MarsLoggerSimple.Info("GetActiveWindow", "No WPF window associated with the active window.");
                }
            }
            else
            {
                MarsLoggerSimple.Info("GetActiveWindow", "No HwndSource found for the active window.");
            }
            MarsLoggerSimple.logEnd("GetActiveWindow");
        }

        public static Rect GetRelativeRectangle(UIElement ui)
        {
            // 获取控件相对于屏幕的位置
            Point absoluteTopLeft = ui.PointToScreen(new Point(0, 0));

            // 获取控件的大小
            Size elementSize = ui.RenderSize;

            // 创建一个绝对位置的矩形
            return new Rect(absoluteTopLeft, elementSize);
        }

        private static System.Drawing.Rectangle convertFromRect(Rect rect)
        {
            return new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }


        public Dictionary<MarsSpiedObjInfoAI, List<MarsSpiedObjInfoAI>> ExtractWindowInfo(System.Windows.Window window)
        {
            var windowInfo = new Dictionary<MarsSpiedObjInfoAI, List<MarsSpiedObjInfoAI>>();
            MarsSpiedObjInfoAI w = new MarsSpiedObjInfoAI();
            // 获取窗口信息
            w.Text = window.Title;
            w.objectType = window.GetType().ToString();
            w.objectRect = convertFromRect(GetRelativeRectangle((window)));           

            // 获取所有控件信息
            var controlsInfo = new List<MarsSpiedObjInfoAI>();
            TraverseControls(window.Content as DependencyObject, controlsInfo);
            windowInfo.Add(w,controlsInfo);
            return windowInfo;
        }

        /// <summary>
        /// 使用WpfVisualTreeInspector获取所有WPF窗口的树结构
        /// 这是新的推荐方法，提供更完整和准确的WPF对象信息
        /// </summary>
        /// <returns>WPF可视对象信息列表</returns>
        public static List<WpfVisualTreeInspector.WpfVisualObjectInfo> GetAllWpfVisualTree()
        {
            MarsLoggerSimple.logBegin("MarsWpfInspector.GetAllWpfVisualTree");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                MarsLoggerSimple.logEnd("MarsWpfInspector.GetAllWpfVisualTree", 
                    $"Found {wpfWindows.Count} WPF windows");
                return wpfWindows;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.GetAllWpfVisualTree", 
                    $"Error getting WPF visual tree: {ex.Message}", ex);
                return new List<WpfVisualTreeInspector.WpfVisualObjectInfo>();
            }
        }

        /// <summary>
        /// 使用WpfVisualTreeInspector获取WPF窗口的树结构并转换为MarsSpiedObjectInfo格式
        /// 用于与现有的MarsObjSpyForm兼容
        /// </summary>
        /// <returns>MarsSpiedObjectInfo列表</returns>
        public static List<MarsSpiedObjectInfo> GetAllWpfObjectsAsMarsObjects()
        {
            MarsLoggerSimple.logBegin("MarsWpfInspector.GetAllWpfObjectsAsMarsObjects");
            
            try
            {
                var marsObjects = WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();
                MarsLoggerSimple.logEnd("MarsWpfInspector.GetAllWpfObjectsAsMarsObjects", 
                    $"Converted {marsObjects.Count} WPF objects to Mars objects");
                return marsObjects;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.GetAllWpfObjectsAsMarsObjects", 
                    $"Error getting WPF objects as Mars objects: {ex.Message}", ex);
                return new List<MarsSpiedObjectInfo>();
            }
        }

        /// <summary>
        /// 获取特定窗口的WPF可视树结构
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>窗口的可视对象信息，如果未找到则返回null</returns>
        public static WpfVisualTreeInspector.WpfVisualObjectInfo GetWindowVisualTree(string windowTitle)
        {
            MarsLoggerSimple.logBegin($"MarsWpfInspector.GetWindowVisualTree({windowTitle})");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                var targetWindow = wpfWindows.FirstOrDefault(w => 
                    w.Text.Contains(windowTitle) || w.Name.Contains(windowTitle));
                
                if (targetWindow != null)
                {
                    MarsLoggerSimple.Info("MarsWpfInspector.GetWindowVisualTree", 
                        $"Found window: {targetWindow.Name} - {targetWindow.Text}");
                }
                else
                {
                    MarsLoggerSimple.Info("MarsWpfInspector.GetWindowVisualTree", 
                        $"Window '{windowTitle}' not found");
                }
                
                MarsLoggerSimple.logEnd($"MarsWpfInspector.GetWindowVisualTree({windowTitle})");
                return targetWindow;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.GetWindowVisualTree", 
                    $"Error getting window visual tree: {ex.Message}", ex);
                MarsLoggerSimple.logEnd($"MarsWpfInspector.GetWindowVisualTree({windowTitle})");
                return null;
            }
        }

        /// <summary>
        /// 查找具有指定名称的WPF对象
        /// </summary>
        /// <param name="name">要查找的名称</param>
        /// <returns>匹配的对象列表</returns>
        public static List<WpfVisualTreeInspector.WpfVisualObjectInfo> FindWpfObjectsByName(string name)
        {
            MarsLoggerSimple.logBegin($"MarsWpfInspector.FindWpfObjectsByName({name})");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                var results = WpfVisualTreeInspector.FindObjectsByName(wpfWindows, name);
                
                MarsLoggerSimple.logEnd($"MarsWpfInspector.FindWpfObjectsByName({name})", 
                    $"Found {results.Count} objects with name '{name}'");
                return results;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.FindWpfObjectsByName", 
                    $"Error finding WPF objects by name: {ex.Message}", ex);
                return new List<WpfVisualTreeInspector.WpfVisualObjectInfo>();
            }
        }

        /// <summary>
        /// 查找具有指定类型的WPF对象
        /// </summary>
        /// <param name="typeName">要查找的类型名称</param>
        /// <returns>匹配的对象列表</returns>
        public static List<WpfVisualTreeInspector.WpfVisualObjectInfo> FindWpfObjectsByType(string typeName)
        {
            MarsLoggerSimple.logBegin($"MarsWpfInspector.FindWpfObjectsByType({typeName})");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                var results = WpfVisualTreeInspector.FindObjectsByType(wpfWindows, typeName);
                
                MarsLoggerSimple.logEnd($"MarsWpfInspector.FindWpfObjectsByType({typeName})", 
                    $"Found {results.Count} objects of type '{typeName}'");
                return results;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.FindWpfObjectsByType", 
                    $"Error finding WPF objects by type: {ex.Message}", ex);
                return new List<WpfVisualTreeInspector.WpfVisualObjectInfo>();
            }
        }

        /// <summary>
        /// 打印WPF可视树结构到控制台（用于调试）
        /// </summary>
        /// <param name="maxDepth">最大打印深度</param>
        public static void PrintWpfVisualTree(int maxDepth = 30)
        {
            MarsLoggerSimple.logBegin("MarsWpfInspector.PrintWpfVisualTree");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                WpfVisualTreeInspector.PrintVisualTree(wpfWindows, maxDepth);
                
                MarsLoggerSimple.logEnd("MarsWpfInspector.PrintWpfVisualTree", 
                    $"Printed visual tree for {wpfWindows.Count} windows");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.PrintWpfVisualTree", 
                    $"Error printing WPF visual tree: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出WPF可视树结构为文本格式
        /// </summary>
        /// <returns>可视树结构的文本表示</returns>
        public static string ExportWpfVisualTreeAsText()
        {
            MarsLoggerSimple.logBegin("MarsWpfInspector.ExportWpfVisualTreeAsText");
            
            try
            {
                var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                var treeText = WpfVisualTreeInspectorExample.ExportVisualTreeAsText(wpfWindows);
                
                MarsLoggerSimple.logEnd("MarsWpfInspector.ExportWpfVisualTreeAsText", 
                    $"Exported visual tree for {wpfWindows.Count} windows");
                return treeText;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsWpfInspector.ExportWpfVisualTreeAsText", 
                    $"Error exporting WPF visual tree: {ex.Message}", ex);
                return "";
            }
        }

        private void TraverseControls(DependencyObject parent, List<MarsSpiedObjInfoAI> controlsInfo)
        {
            message.Inter.MQCenter.simpleLog.MarsLoggerSimple.logBegin("TraverseControls");
            if (parent == null)
            {
                return;
            }
            MarsSpiedObjInfoAI currentObj = new MarsSpiedObjInfoAI();
            bool isAddtoList = true;
            if (parent is System.Windows.Controls.Control cntrl)
            {
                currentObj.objectName = cntrl.Name;
                currentObj.objectType = cntrl.GetType().FullName;
                currentObj.objectRect = convertFromRect(GetRelativeRectangle((cntrl)));
                //currentObj.Text = cntrl.Text;
            }
            else
            {
                MarsLoggerSimple.Error("TraverseControls", $"object is not control|type is|{parent.GetType().FullName}");
                return;
            }
            
            // 判断控件类型并提取信息
            if (parent is System.Windows.Controls.TextBox textBox)
            {
                currentObj.Text = textBox.Text;                 
            }
            else if (parent is System.Windows.Controls.ComboBox comboBox)
            {
                var listItems = comboBox.Items.Cast<object>().Select(item => item.ToString()).ToList();
                currentObj.Text = comboBox.Text;
                currentObj.ListItems = listItems;                
            }
            else if (parent is System.Windows.Controls.CheckBox checkBox)
            {
                currentObj.Text = checkBox.Content?.ToString();
                //currentObj.IsChecked = checkBox.IsChecked                
            }
            else if (parent is System.Windows.Controls.DataGrid dataGrid)
            {
                var columns = dataGrid.Columns.Select(c => new MarsObjectColumnInfo { ColumnName = c.Header.ToString() }).ToList();
                currentObj.DataTableColumns = columns;               
            }
            else
            {
                MarsLoggerSimple.Error("TraverseControls",$"Unsupported objectType|{parent.GetType()}");
            }
            if (isAddtoList)
            {
                controlsInfo.Add(currentObj);
            }

            // 递归遍历子控件
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                TraverseControls(child, controlsInfo);
            }

        }

        /// <summary>
        /// 获取所有界面元素（包括可视树、逻辑树和自动化树）
        /// 这是一个更全面的方法，能够获取所有类型的界面元素
        /// </summary>
        /// <returns>所有界面元素列表</returns>
        public static List<WpfVisualTreeInspector.WpfVisualObjectInfo> GetAllUIElements()
        {
            return WpfVisualTreeInspector.GetAllUIElements();
        }

        /// <summary>
        /// 获取所有界面元素并转换为MarsSpiedObjectInfo格式
        /// </summary>
        /// <returns>MarsSpiedObjectInfo格式的界面元素列表</returns>
        public static List<MarsSpiedObjectInfo> GetAllUIElementsAsMarsObjects()
        {
            var uiElements = GetAllUIElements();
            return WpfVisualTreeAdapter.ConvertWpfTreeToMarsObjects(uiElements);
        }

        /// <summary>
        /// 获取所有界面元素并加载到TreeView
        /// </summary>
        /// <param name="treeView">目标TreeView</param>
        /// <param name="targetControlId">目标控件ID</param>
        public static void LoadAllUIElementsToTreeView(System.Windows.Forms.TreeView treeView, IntPtr targetControlId = default(IntPtr))
        {
            var uiElements = GetAllUIElements();
            var marsObjects = WpfVisualTreeAdapter.ConvertWpfTreeToMarsObjects(uiElements);
            WpfVisualTreeAdapter.LoadWpfTreeToTreeView(treeView, marsObjects, targetControlId);
        }

        /// <summary>
        /// 打印所有界面元素信息
        /// </summary>
        public static void PrintAllUIElements()
        {
            var uiElements = GetAllUIElements();
            WpfVisualTreeInspector.PrintVisualTree(uiElements);
            
        }

        /// <summary>
        /// 导出所有界面元素信息为文本
        /// </summary>
        /// <returns>界面元素信息文本</returns>
        public static string ExportAllUIElementsAsText()
        {
            var uiElements = GetAllUIElements();
            var result = new StringBuilder();
            
            foreach (var element in uiElements)
            {
                result.AppendLine(element.GetFullPath());
            }
            
            return result.ToString();
        }
    }
}
