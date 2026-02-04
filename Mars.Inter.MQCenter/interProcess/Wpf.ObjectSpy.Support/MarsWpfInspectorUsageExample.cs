using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// MarsWpfInspector使用示例
    /// 展示如何使用修改后的MarsWpfInspector类来获取WPF对象的树结构
    /// </summary>
    public class MarsWpfInspectorUsageExample
    {
        /// <summary>
        /// 示例：使用MarsWpfInspector获取WPF可视树
        /// </summary>
        public static void DemonstrateMarsWpfInspector()
        {
            MarsLoggerSimple.logBegin("DemonstrateMarsWpfInspector");
            
            try
            {
                // 方法1：获取WPF可视树（推荐）
                var wpfVisualTree = MarsWpfInspector.GetAllWpfVisualTree();
                MarsLoggerSimple.Info("DemonstrateMarsWpfInspector", 
                    $"Found {wpfVisualTree.Count} WPF windows using MarsWpfInspector");
                
                // 遍历每个窗口
                foreach (var window in wpfVisualTree)
                {
                    MarsLoggerSimple.Info("DemonstrateMarsWpfInspector", 
                        $"Window: {window.Name} [{window.Type}] - {window.Text}");
                    MarsLoggerSimple.Info("DemonstrateMarsWpfInspector", 
                        $"  Position: {window.Position}");
                    MarsLoggerSimple.Info("DemonstrateMarsWpfInspector", 
                        $"  Children Count: {window.Children.Count}");
                }
                
                // 方法2：获取MarsSpiedObjectInfo格式的对象（兼容现有代码）
                var marsObjects = MarsWpfInspector.GetAllWpfObjectsAsMarsObjects();
                MarsLoggerSimple.Info("DemonstrateMarsWpfInspector", 
                    $"Converted {marsObjects.Count} WPF objects to Mars objects");
                
                // 演示搜索功能
                DemonstrateSearchFunctions();
                
                // 演示调试功能
                DemonstrateDebugFunctions();
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("DemonstrateMarsWpfInspector", 
                    $"Error during MarsWpfInspector demonstration: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("DemonstrateMarsWpfInspector");
        }
        
        /// <summary>
        /// 演示搜索功能
        /// </summary>
        private static void DemonstrateSearchFunctions()
        {
            MarsLoggerSimple.logBegin("DemonstrateSearchFunctions");
            
            try
            {
                // 按名称查找WPF对象
                var buttons = MarsWpfInspector.FindWpfObjectsByName("MyButton");
                MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                    $"Found {buttons.Count} objects named 'MyButton'");
                
                // 按类型查找WPF对象
                var textBoxes = MarsWpfInspector.FindWpfObjectsByType("TextBox");
                MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                    $"Found {textBoxes.Count} TextBox objects");
                
                // 查找特定窗口
                var mainWindow = MarsWpfInspector.GetWindowVisualTree("MainWindow");
                if (mainWindow != null)
                {
                    MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                        $"Found MainWindow: {mainWindow.Name} - {mainWindow.Text}");
                }
                else
                {
                    MarsLoggerSimple.Info("DemonstrateSearchFunctions", 
                        "MainWindow not found");
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
        /// 演示调试功能
        /// </summary>
        private static void DemonstrateDebugFunctions()
        {
            MarsLoggerSimple.logBegin("DemonstrateDebugFunctions");
            
            try
            {
                // 打印可视树到控制台
                MarsLoggerSimple.Info("DemonstrateDebugFunctions", 
                    "Printing WPF visual tree to console...");
                MarsWpfInspector.PrintWpfVisualTree(maxDepth: 3);
                
                // 导出可视树为文本
                MarsLoggerSimple.Info("DemonstrateDebugFunctions", 
                    "Exporting WPF visual tree as text...");
                string treeText = MarsWpfInspector.ExportWpfVisualTreeAsText();
                MarsLoggerSimple.Info("DemonstrateDebugFunctions", 
                    $"Exported tree text length: {treeText.Length} characters");
                
                // 可以保存到文件或进一步处理
                // System.IO.File.WriteAllText("wpf_visual_tree.txt", treeText);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("DemonstrateDebugFunctions", 
                    $"Error during debug demonstration: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("DemonstrateDebugFunctions");
        }
        
        /// <summary>
        /// 示例：在MarsObjSpyForm中使用MarsWpfInspector
        /// </summary>
        public static void DemonstrateMarsObjSpyFormIntegration()
        {
            MarsLoggerSimple.logBegin("DemonstrateMarsObjSpyFormIntegration");
            
            try
            {
                // 获取MarsObjSpyForm实例
                var spyForm = MarsObjSpyForm.getInstance(null);
                
                // 方法1：使用MarsWpfInspector获取WPF对象并加载到TreeView
                var wpfObjects = MarsWpfInspector.GetAllWpfObjectsAsMarsObjects();
                if (wpfObjects != null && wpfObjects.Count > 0)
                {
                    // 使用扩展方法加载
                    spyForm.ReloadObjects(wpfObjects);
                    MarsLoggerSimple.Info("DemonstrateMarsObjSpyFormIntegration", 
                        $"Loaded {wpfObjects.Count} WPF objects to MarsObjSpyForm");
                }
                
                // 方法2：使用扩展方法直接加载WPF可视树
                spyForm.LoadWpfVisualTree();
                
                // 方法3：加载混合可视树（Windows Forms + WPF）
                spyForm.LoadMixedVisualTree();
                
                MarsLoggerSimple.Info("DemonstrateMarsObjSpyFormIntegration", 
                    "Successfully integrated MarsWpfInspector with MarsObjSpyForm");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("DemonstrateMarsObjSpyFormIntegration", 
                    $"Error during MarsObjSpyForm integration: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("DemonstrateMarsObjSpyFormIntegration");
        }
        
        /// <summary>
        /// 示例：比较新旧方法的差异
        /// </summary>
        public static void CompareOldAndNewMethods()
        {
            MarsLoggerSimple.logBegin("CompareOldAndNewMethods");
            
            try
            {
                // 旧方法：使用原有的ExtractWindowInfo
                var oldInspector = new MarsWpfInspector();
                // 注意：旧方法需要传入具体的Window对象
                // var oldResult = oldInspector.ExtractWindowInfo(someWindow);
                
                // 新方法：使用WpfVisualTreeInspector
                var newResult = MarsWpfInspector.GetAllWpfVisualTree();
                
                MarsLoggerSimple.Info("CompareOldAndNewMethods", 
                    $"New method found {newResult.Count} WPF windows");
                
                // 比较结果
                foreach (var window in newResult)
                {
                    MarsLoggerSimple.Info("CompareOldAndNewMethods", 
                        $"New method - Window: {window.Name} [{window.Type}]");
                    MarsLoggerSimple.Info("CompareOldAndNewMethods", 
                        $"  UniqueId: {window.GetUniqueIdentifier()}");
                    MarsLoggerSimple.Info("CompareOldAndNewMethods", 
                        $"  FullPath: {window.GetFullPath()}");
                }
                
                MarsLoggerSimple.Info("CompareOldAndNewMethods", 
                    "New method provides more comprehensive object information");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CompareOldAndNewMethods", 
                    $"Error during comparison: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("CompareOldAndNewMethods");
        }
        
        /// <summary>
        /// 示例：处理WPF对象的详细信息
        /// </summary>
        public static void ProcessWpfObjectDetails()
        {
            MarsLoggerSimple.logBegin("ProcessWpfObjectDetails");
            
            try
            {
                var wpfWindows = MarsWpfInspector.GetAllWpfVisualTree();
                
                foreach (var window in wpfWindows)
                {
                    MarsLoggerSimple.Info("ProcessWpfObjectDetails", 
                        $"Processing window: {window.Name}");
                    
                    // 处理窗口的详细信息
                    ProcessObjectDetails(window, 0);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ProcessWpfObjectDetails", 
                    $"Error processing WPF object details: {ex.Message}", ex);
            }
            
            MarsLoggerSimple.logEnd("ProcessWpfObjectDetails");
        }
        
        /// <summary>
        /// 递归处理对象的详细信息
        /// </summary>
        /// <param name="obj">WPF对象</param>
        /// <param name="depth">当前深度</param>
        private static void ProcessObjectDetails(WpfVisualTreeInspector.WpfVisualObjectInfo obj, int depth)
        {
            if (depth > 5) return; // 限制深度避免过多输出
            
            var indent = new string(' ', depth * 2);
            
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}Object: {obj.Name} [{obj.Type}]");
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}  Text: {obj.Text}");
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}  Position: {obj.Position}");
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}  Visible: {obj.IsVisible}, Enabled: {obj.IsEnabled}");
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}  AutomationId: {obj.AutomationId}");
            MarsLoggerSimple.Info("ProcessObjectDetails", 
                $"{indent}  UniqueId: {obj.GetUniqueIdentifier()}");
            
            // 递归处理子对象
            foreach (var child in obj.Children)
            {
                ProcessObjectDetails(child, depth + 1);
            }
        }
    }
}