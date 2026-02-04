using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mars.message.Inter.MQCenter.simpleLog;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// WpfVisualTreeInspector使用示例
    /// 展示如何使用WpfVisualTreeInspector获取WPF应用的完整可视对象层级结构
    /// </summary>
    public class WpfVisualTreeInspectorExample
    {
        /// <summary>
        /// 示例：获取并显示当前WPF应用的所有可视对象层级结构
        /// </summary>
        public static void DemonstrateVisualTreeInspection()
        {
            MarsLoggerSimple.logBegin("DemonstrateVisualTreeInspection");
            
            try
            {
                // 获取所有顶级窗口及其完整的可视对象层级结构
                var topLevelWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                
                MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                    $"Found {topLevelWindows.Count} top level windows");
                
                // 遍历每个顶级窗口
                foreach (var window in topLevelWindows)
                {
                    MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                        $"Window: {window.Name} [{window.Type}] - {window.Text}");
                    MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                        $"  Position: {window.Position}");
                    MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                        $"  NamePath: {window.NamePath}");
                    MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                        $"  TypePath: {window.TypePath}");
                    MarsLoggerSimple.Info("DemonstrateVisualTreeInspection", 
                        $"  Children Count: {window.Children.Count}");
                    
                    // 递归显示子对象（限制深度避免输出过多）
                    DisplayVisualObjectHierarchy(window, 0, 3);
                }
                
                // 演示查找功能
                DemonstrateSearchFunctions(topLevelWindows);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("DemonstrateVisualTreeInspection", 
                    $"Error during visual tree inspection: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("DemonstrateVisualTreeInspection");
        }
        
        /// <summary>
        /// 递归显示可视对象层级结构
        /// </summary>
        /// <param name="info">可视对象信息</param>
        /// <param name="depth">当前深度</param>
        /// <param name="maxDepth">最大显示深度</param>
        private static void DisplayVisualObjectHierarchy(
            WpfVisualTreeInspector.WpfVisualObjectInfo info, 
            int depth, 
            int maxDepth)
        {
            if (depth > maxDepth) return;
            
            var indent = new string(' ', (depth + 1) * 2);
            
            MarsLoggerSimple.Info("DisplayVisualObjectHierarchy", 
                $"{indent}{info.Name}[{info.Type}] - {info.Text}");
            MarsLoggerSimple.Info("DisplayVisualObjectHierarchy", 
                $"{indent}  Position: {info.Position}");
            MarsLoggerSimple.Info("DisplayVisualObjectHierarchy", 
                $"{indent}  UniqueId: {info.GetUniqueIdentifier()}");
            
            foreach (var child in info.Children)
            {
                DisplayVisualObjectHierarchy(child, depth + 1, maxDepth);
            }
        }
        
        /// <summary>
        /// 演示搜索功能
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        private static void DemonstrateSearchFunctions(
            List<WpfVisualTreeInspector.WpfVisualObjectInfo> windows)
        {
            MarsLoggerSimple.logBegin("DemonstrateSearchFunctions");
            
            try
            {
                // 查找所有Button控件
                var buttons = WpfVisualTreeInspector.FindObjectsByType(windows, "Button");
                MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                    $"Found {buttons.Count} Button controls");
                
                foreach (var button in buttons)
                {
                    MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                        $"  Button: {button.Name} - {button.Text} - {button.GetFullPath()}");
                }
                
                // 查找所有TextBox控件
                var textBoxes = WpfVisualTreeInspector.FindObjectsByType(windows, "TextBox");
                MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                    $"Found {textBoxes.Count} TextBox controls");
                
                foreach (var textBox in textBoxes)
                {
                    MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                        $"  TextBox: {textBox.Name} - {textBox.Text} - {textBox.GetFullPath()}");
                }
                
                // 查找所有具有特定名称的对象
                var namedObjects = WpfVisualTreeInspector.FindObjectsByName(windows, "MainWindow");
                MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                    $"Found {namedObjects.Count} objects named 'MainWindow'");
                
                foreach (var obj in namedObjects)
                {
                    MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                        $"  Named Object: {obj.Name} - {obj.Type} - {obj.GetFullPath()}");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("DemonstrateSearchFunctions", 
                    $"Error during search demonstration: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("DemonstrateSearchFunctions");
        }
        
        /// <summary>
        /// 示例：获取特定窗口的可视对象层级结构
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>窗口的可视对象信息，如果未找到则返回null</returns>
        public static WpfVisualTreeInspector.WpfVisualObjectInfo GetWindowVisualTree(string windowTitle)
        {
            MarsLoggerSimple.logBegin($"GetWindowVisualTree({windowTitle})");
            
            try
            {
                var topLevelWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                
                var targetWindow = topLevelWindows.FirstOrDefault(w => 
                    w.Text.Contains(windowTitle) || w.Name.Contains(windowTitle));
                
                if (targetWindow != null)
                {
                    MarsLoggerSimple.Info("GetWindowVisualTree", 
                        $"Found window: {targetWindow.Name} - {targetWindow.Text}");
                    MarsLoggerSimple.Info("GetWindowVisualTree", 
                        $"Total children: {CountAllChildren(targetWindow)}");
                }
                else
                {
                    MarsLoggerSimple.Info("GetWindowVisualTree", 
                        $"Window '{windowTitle}' not found");
                }
                
                MarsLoggerSimple.logEnd($"GetWindowVisualTree({windowTitle})");
                return targetWindow;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWindowVisualTree", 
                    $"Error getting window visual tree: {ex.Message}", ex);
                MarsLoggerSimple.logEnd($"GetWindowVisualTree({windowTitle})");
                return null;
            }
        }
        
        /// <summary>
        /// 递归计算所有子对象数量
        /// </summary>
        /// <param name="info">可视对象信息</param>
        /// <returns>子对象总数</returns>
        private static int CountAllChildren(WpfVisualTreeInspector.WpfVisualObjectInfo info)
        {
            int count = info.Children.Count;
            foreach (var child in info.Children)
            {
                count += CountAllChildren(child);
            }
            return count;
        }
        
        /// <summary>
        /// 示例：导出可视树结构为文本格式
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        /// <returns>可视树结构的文本表示</returns>
        public static string ExportVisualTreeAsText(
            List<WpfVisualTreeInspector.WpfVisualObjectInfo> windows)
        {
            var sb = new StringBuilder();
            
            foreach (var window in windows)
            {
                sb.AppendLine($"Window: {window.Name} [{window.Type}]");
                sb.AppendLine($"  Title: {window.Text}");
                sb.AppendLine($"  Position: {window.Position}");
                sb.AppendLine($"  NamePath: {window.NamePath}");
                sb.AppendLine($"  TypePath: {window.TypePath}");
                sb.AppendLine($"  UniqueId: {window.GetUniqueIdentifier()}");
                sb.AppendLine($"  FullPath: {window.GetFullPath()}");
                sb.AppendLine();
                
                ExportVisualObjectAsText(window, sb, 1);
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 递归导出可视对象为文本格式
        /// </summary>
        private static void ExportVisualObjectAsText(
            WpfVisualTreeInspector.WpfVisualObjectInfo info, 
            StringBuilder sb, 
            int depth)
        {
            var indent = new string(' ', depth * 2);
            
            sb.AppendLine($"{indent}{info.Name}[{info.Type}]");
            sb.AppendLine($"{indent}  Text: {info.Text}");
            sb.AppendLine($"{indent}  Position: {info.Position}");
            sb.AppendLine($"{indent}  Visible: {info.IsVisible}");
            sb.AppendLine($"{indent}  Enabled: {info.IsEnabled}");
            sb.AppendLine($"{indent}  AutomationId: {info.AutomationId}");
            sb.AppendLine($"{indent}  Uid: {info.Uid}");
            sb.AppendLine($"{indent}  Index: {info.Index}");
            sb.AppendLine($"{indent}  UniqueId: {info.GetUniqueIdentifier()}");
            sb.AppendLine($"{indent}  FullPath: {info.GetFullPath()}");
            
            foreach (var child in info.Children)
            {
                ExportVisualObjectAsText(child, sb, depth + 1);
            }
        }
        
        /// <summary>
        /// 示例：查找具有特定属性的对象
        /// </summary>
        /// <param name="windows">顶级窗口列表</param>
        /// <param name="predicate">查找条件</param>
        /// <returns>匹配的对象列表</returns>
        public static List<WpfVisualTreeInspector.WpfVisualObjectInfo> FindObjectsByCondition(
            List<WpfVisualTreeInspector.WpfVisualObjectInfo> windows,
            Func<WpfVisualTreeInspector.WpfVisualObjectInfo, bool> predicate)
        {
            var results = new List<WpfVisualTreeInspector.WpfVisualObjectInfo>();
            
            foreach (var window in windows)
            {
                FindObjectsByConditionRecursive(window, predicate, results);
            }
            
            return results;
        }
        
        /// <summary>
        /// 递归查找满足条件的对象
        /// </summary>
        private static void FindObjectsByConditionRecursive(
            WpfVisualTreeInspector.WpfVisualObjectInfo info,
            Func<WpfVisualTreeInspector.WpfVisualObjectInfo, bool> predicate,
            List<WpfVisualTreeInspector.WpfVisualObjectInfo> results)
        {
            if (predicate(info))
            {
                results.Add(info);
            }
            
            foreach (var child in info.Children)
            {
                FindObjectsByConditionRecursive(child, predicate, results);
            }
        }
    }
}