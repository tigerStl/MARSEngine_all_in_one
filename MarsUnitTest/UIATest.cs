using Axe.Windows.Core.Bases;
using Axe.Windows.Desktop.UIAutomation;
using Axe.Windows.Desktop.UIAutomation.CustomObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Mars.message.windowsWrapper.SystemUtil;

namespace MarsUnitTest
{
    [TestClass]
    public class AfxApplicationScannerTests
    {
        // Windows API declarations
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        [TestMethod]
        [TestCategory("Integration")]
        [Description("测试扫描特定 AFX 应用程序窗口的对象树")]
        public void TestScanAfxApplicationByProcessName()
        {
            // 安排 (Arrange)
            string processName = "notepad"; // 替换为你要测试的 AFX 应用程序进程名

            // 执行 (Act) & 断言 (Assert)
            using (var scanner = new AfxApplicationScanner())
            {
                scanner.ScanAfxApplicationByProcessName(processName);
            }

            Assert.IsTrue(true, "扫描完成，无异常抛出");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("测试通过窗口句柄扫描 AFX 应用程序")]
        public void TestScanAfxWindowByHandle()
        {
            // 安排
            IntPtr windowHandle = GetNotepadWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                Assert.Inconclusive("未找到记事本窗口，跳过测试");
                return;
            }

            // 执行 & 断言
            using (var scanner = new AfxApplicationScanner())
            {
                scanner.ScanAfxWindow(windowHandle);
            }

            Assert.IsTrue(true, "窗口扫描完成");
        }

        [TestMethod]
        [TestCategory("Unit")]
        [Description("测试元素描述生成功能")]
        public void TestGetElementDescription()
        {
            // 安排
            using (var scanner = new AfxApplicationScanner())
            {
                var element = scanner.GetDesktopElement();

                // 执行
                string description = scanner.GetElementDescription(element);

                // 断言
                Assert.IsNotNull(description);
                Assert.IsTrue(description.Contains(element.LocalizedControlType));
                Console.WriteLine($"元素描述: {description}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("通过窗口句柄扫描AFX应用程序，使用UIA技术获取所有子对象信息并保存到文件")]
        public void TestScanAfxWindowByHandleWithUIA()
        {
            // 安排 - 使用指定的窗口句柄
            IntPtr targetHwnd = new IntPtr(0x0015163A); // 可以修改为其他AFX窗口句柄
            
            // 验证窗口句柄是否有效且类名以Afx开头
            if (!IsValidAfxWindow(targetHwnd))
            {
                Assert.Inconclusive("指定的窗口句柄无效或不是AFX窗口，跳过测试");
                return;
            }

            // 执行 - 使用UIA技术扫描并保存到文件
            string outputFile = $@"C:\temp\uia_test_{DateTime.Now:yyyyMMddHHmmss}.txt";
            ScanAfxWindowWithUIA(targetHwnd, outputFile);

            // 断言 - 验证文件是否创建且包含内容
            Assert.IsTrue(File.Exists(outputFile), $"输出文件未创建: {outputFile}");
            
            string fileContent = File.ReadAllText(outputFile);
            Assert.IsFalse(string.IsNullOrEmpty(fileContent), "输出文件为空");
            Assert.IsTrue(fileContent.Contains("AFX"), "输出文件中应包含AFX相关信息");
            
            Console.WriteLine($"扫描完成，结果已保存到: {outputFile}");
        }

        [TestMethod]
        public void TestPostMessageLeftError()
        {
            Thread.Sleep(10 * 1000);
            ///等待10秒钟，确保有足够时间观察PostMessage的效果
            ///对指定窗口发送鼠标左键单击消息 keydown和keyup
            ///Left arrow

            IntPtr hwnd = (IntPtr)0x001603DC; // 替换为目标窗口的句柄
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;
            const int VK_LEFT = 0x27;

            // 发送 Left Arrow KeyDown
            MarsWindowsAPIs.PostMessage(hwnd, WM_KEYDOWN, VK_LEFT, 0);
            Thread.Sleep(50); // 小延时，模拟真实按键
                              // 发送 Left Arrow KeyUp
            MarsWindowsAPIs.PostMessage(hwnd, WM_KEYUP, VK_LEFT, 0);
        }

        [TestMethod]
        public void TestSendKeysForSophisGrid()
        {
            Thread.Sleep(10 * 1000);
            ///等待10秒钟，确保有足够时间观察PostMessage的效果
            ///对指定窗口发送鼠标左键单击消息 keydown和keyup
            ///Left arrow
            System.Windows.Forms.SendKeys.SendWait("{Right}");
            //IntPtr hwnd = (IntPtr)0x001603DC; // 替换为目标窗口的句柄
            //const int WM_KEYDOWN = 0x0100;
            //const int WM_KEYUP = 0x0101;
            //const int VK_LEFT = 0x27;

            //// 发送 Left Arrow KeyDown
            //MarsWindowsAPIs.PostMessage(hwnd, WM_KEYDOWN, VK_LEFT, 0);
            //Thread.Sleep(50); // 小延时，模拟真实按键
            //                  // 发送 Left Arrow KeyUp
            //MarsWindowsAPIs.PostMessage(hwnd, WM_KEYUP, VK_LEFT, 0);
        }

        //[DllImport("user32.dll")]
        //private static extern bool GetCursorPos(out POINT lpPoint);

        //[DllImport("user32.dll")]
        //private static extern IntPtr WindowFromPoint(POINT Point);

        [TestMethod]
        public void Test_findSophisvaluePopupMenusFromCursorPos()
        {
            Thread.Sleep(10 * 1000); // 等待弹出菜单出现

            // 获取当前鼠标位置
            Mars.message.windowsWrapper.SystemUtil.POINT pt=default;
            if (!MarsWindowsAPIs.GetCursorPos(ref pt))
            {
                Console.WriteLine("无法获取鼠标位置");
                return;
            }

            // 向右下偏移10像素
            pt.X += 10;
            pt.Y += 10;

            // 获取该位置的窗口句柄
            IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(pt.X,pt.Y));
            if (hwnd == IntPtr.Zero)
            {
                Console.WriteLine("未找到窗口句柄");
                return;
            }

            // 获取UIA对象
            AutomationElement element = AutomationElement.FromHandle(hwnd);
            if (element == null)
            {
                Console.WriteLine("无法获取AutomationElement");
                return;
            }

            // 判断是否为Menu
            if (element.Current.ControlType == ControlType.Menu)
            {
                Console.WriteLine("找到弹出菜单: " + element.Current.Name);

                // 绘制菜单边框
                var rect = element.Current.BoundingRectangle;
                if (!rect.IsEmpty)
                {
                    string strError = "";
                    MarsWindowsAPIs.RECT menuRect = new MarsWindowsAPIs.RECT()
                    {
                        Left = (int)rect.Left - 3,
                        Top = (int)rect.Top - 3,
                        Right = (int)rect.Right,
                        Bottom = (int)rect.Bottom
                    };
                    Mars.message.windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(menuRect, ref strError);
                    if (!string.IsNullOrEmpty(strError))
                        Console.WriteLine("绘制弹出菜单边框错误: " + strError);
                }
                TreeWalker walker = TreeWalker.ControlViewWalker;
                // 获取所有一级菜单项（不递归）
                Condition menuItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem);
                AutomationElementCollection menuItems = element.FindAll(TreeScope.Descendants, Condition.TrueCondition);

                foreach (AutomationElement menuItem in menuItems)
                {
                    Console.WriteLine($"菜单项: {menuItem.Current.Name}|{menuItem.Current.ItemType}|{menuItem.Current.ControlType.ProgrammaticName}|{menuItem.Current.Name}|{menuItem.Current.ClassName}");
                    var p = walker.GetParent(menuItem);
                    if (p != null)
                    {
                        Console.WriteLine($"  父元素: {p.Current.Name}|{p.Current.ItemType}|{p.Current.ControlType.ProgrammaticName}|{p.Current.Name}|{p.Current.ClassName}");
                    }
                    var itemRect = menuItem.Current.BoundingRectangle;
                    if (!itemRect.IsEmpty)
                    {
                        string strError = "";
                        MarsWindowsAPIs.RECT itemRectStruct = new MarsWindowsAPIs.RECT()
                        {
                            Left = (int)itemRect.Left - 3,
                            Top = (int)itemRect.Top - 3,
                            Right = (int)itemRect.Right,
                            Bottom = (int)itemRect.Bottom
                        };
                        Mars.message.windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(itemRectStruct, ref strError);
                        if (!string.IsNullOrEmpty(strError))
                            Console.WriteLine("绘制菜单项边框错误: " + strError);
                    }
                }
            }
            else
            {
                Console.WriteLine("该位置的窗口不是弹出菜单类型: " + element.Current.ControlType.ProgrammaticName);
            }
        }




        [TestMethod]
        public void Test_findSophisvaluePopupMenus()
        {
            Thread.Sleep(10 * 1000); // 等待弹出菜单出现
            string processName = "sophisvalue";
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Console.WriteLine("未找到进程: " + processName);
                Assert.Inconclusive("未找到进程: " + processName);
                return;
            }

            foreach (var proc in processes)
            {
                IntPtr mainHwnd = proc.MainWindowHandle;
                if (mainHwnd == IntPtr.Zero)
                {
                    Console.WriteLine($"进程 {proc.Id} 没有主窗口，尝试查找所有窗口。");
                    // 可选：遍历所有窗口，或用 Win32 API 枚举
                }

                // 推荐直接从桌面查找所有属于该进程的菜单
                AutomationElement desktop = AutomationElement.RootElement;
                Condition menuCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu);

                // 查找所有弹出菜单
                AutomationElementCollection popupMenus = desktop.FindAll(TreeScope.Descendants, menuCondition);

                foreach (AutomationElement popupMenu in popupMenus)
                {
                    // 过滤属于 sophisvalue.exe 的菜单
                    int menuProcessId = popupMenu.Current.ProcessId;
                    if (menuProcessId != proc.Id) continue;

                    Console.WriteLine($"找到弹出菜单: {popupMenu.Current.Name} (PID={menuProcessId})");

                    // 绘制弹出菜单边框
                    var rect = popupMenu.Current.BoundingRectangle;
                    if (!rect.IsEmpty)
                    {
                        string strError = "";
                        MarsWindowsAPIs.RECT menuRect = new MarsWindowsAPIs.RECT()
                        {
                            Left = (int)rect.Left - 3,
                            Top = (int)rect.Top - 3,
                            Right = (int)rect.Right,
                            Bottom = (int)rect.Bottom
                        };
                        Mars.message.windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(menuRect, ref strError);
                        if (!string.IsNullOrEmpty(strError))
                            Console.WriteLine("绘制弹出菜单边框错误: " + strError);
                    }

                    // 递归遍历菜单项，最多4层
                    DrawMenuItemsWithRectLimited(popupMenu, 1, 2);
                }
            }
        }

        /// <summary>
        /// 递归遍历并绘制菜单项边框，限制最大层级
        /// </summary>
        private void DrawMenuItemsWithRectLimited(AutomationElement parent, int currentLevel, int maxLevel)
        {
            if (currentLevel > maxLevel || parent == null) return;

            Condition menuItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem);
            AutomationElementCollection menuItems = parent.FindAll(TreeScope.Children, menuItemCondition);

            foreach (AutomationElement menuItem in menuItems)
            {
                Console.WriteLine(new string(' ', currentLevel * 2) + "菜单项: " + menuItem.Current.Name);

                var itemRect = menuItem.Current.BoundingRectangle;
                if (!itemRect.IsEmpty)
                {
                    string strError = "";
                    MarsWindowsAPIs.RECT itemRectStruct = new MarsWindowsAPIs.RECT()
                    {
                        Left = (int)itemRect.Left - 3,
                        Top = (int)itemRect.Top - 3,
                        Right = (int)itemRect.Right,
                        Bottom = (int)itemRect.Bottom
                    };
                    Mars.message.windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(itemRectStruct, ref strError);
                    if (!string.IsNullOrEmpty(strError))
                        Console.WriteLine("绘制菜单项边框错误: " + strError);
                }

                // 递归处理子菜单项
                DrawMenuItemsWithRectLimited(menuItem, currentLevel + 1, maxLevel);
            }
        }



        [TestMethod]
        [TestCategory("Integration")]
        [Description("增强版窗口扫描，不限制窗口类型，获取所有可能的子对象")]
        public void TestEnhancedWindowScanWithUIA()
        {
            // 安排 - 使用指定的窗口句柄
            IntPtr targetHwnd = new IntPtr(0x0015163A); // 可以修改为其他窗口句柄
            
            // 使用更宽松的窗口验证
            if (!IsValidWindow(targetHwnd))
            {
                Assert.Inconclusive("指定的窗口句柄无效，跳过测试");
                return;
            }

            // 执行 - 使用增强版UIA技术扫描并保存到文件
            string outputFile = $@"C:\temp\uia_enhanced_{DateTime.Now:yyyyMMddHHmmss}.txt";
            
            // 在STA线程中执行UIAutomation扫描
            System.Threading.Thread staThread = new System.Threading.Thread(() =>
            {
                ScanEnhancedWindowWithUIA(targetHwnd, outputFile);
            });
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            // 断言 - 验证文件是否创建且包含内容
            Assert.IsTrue(File.Exists(outputFile), $"输出文件未创建: {outputFile}");
            
            string fileContent = File.ReadAllText(outputFile);
            Assert.IsFalse(string.IsNullOrEmpty(fileContent), "输出文件为空");
            
            Console.WriteLine($"增强扫描完成，结果已保存到: {outputFile}");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("使用LegacyPattern扫描Ribbon内容")]
        public void TestRibbonScanWithLegacyPattern()
        {
            // 安排 - 使用指定的窗口句柄
            IntPtr targetHwnd = new IntPtr(0x0015163A); // 可以修改为其他窗口句柄
            
            // 使用更宽松的窗口验证
            if (!IsValidWindow(targetHwnd))
            {
                Assert.Inconclusive("指定的窗口句柄无效，跳过测试");
                return;
            }

            // 执行 - 使用LegacyPattern扫描Ribbon内容
            string outputFile = $@"C:\temp\uia_ribbon_legacy_{DateTime.Now:yyyyMMddHHmmss}.txt";
            
            // 在STA线程中执行UIAutomation扫描
            System.Threading.Thread staThread = new System.Threading.Thread(() =>
            {
                ScanRibbonWithLegacyPattern(targetHwnd, outputFile);
            });
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            // 断言 - 验证文件是否创建且包含内容
            Assert.IsTrue(File.Exists(outputFile), $"输出文件未创建: {outputFile}");
            
            string fileContent = File.ReadAllText(outputFile);
            Assert.IsFalse(string.IsNullOrEmpty(fileContent), "输出文件为空");
            
            Console.WriteLine($"Ribbon LegacyPattern扫描完成，结果已保存到: {outputFile}");
        }

        // 辅助方法
        private IntPtr GetNotepadWindowHandle()
        {
            var processes = Process.GetProcessesByName("notepad");
            return processes.Length > 0 ? processes[0].MainWindowHandle : IntPtr.Zero;
        }

        /// <summary>
        /// 验证窗口句柄是否有效且类名以Afx开头
        /// </summary>
        private bool IsValidAfxWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            StringBuilder className = new StringBuilder(256);
            if (GetClassName(hwnd, className, className.Capacity) > 0)
            {
                string classNameStr = className.ToString();
                Console.WriteLine($"窗口类名: {classNameStr}");
                // 放宽限制，允许更多类型的窗口
                return classNameStr.StartsWith("Afx", StringComparison.OrdinalIgnoreCase) ||
                       classNameStr.Contains("Afx:") ||
                       classNameStr.StartsWith("MFC", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// 验证窗口句柄是否有效（不限制类名）
        /// </summary>
        private bool IsValidWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            StringBuilder className = new StringBuilder(256);
            if (GetClassName(hwnd, className, className.Capacity) > 0)
            {
                string classNameStr = className.ToString();
                Console.WriteLine($"窗口类名: {classNameStr}");
                return true; // 接受所有有效的窗口
            }

            return false;
        }

        /// <summary>
        /// 使用LegacyPattern扫描Ribbon内容
        /// </summary>
        private void ScanRibbonWithLegacyPattern(IntPtr hwnd, string outputFile)
        {
            // 确保输出目录存在
            string directory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                writer.WriteLine($"Ribbon LegacyPattern扫描报告");
                writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"窗口句柄: 0x{hwnd.ToInt64():X}");
                writer.WriteLine($"窗口类名: {GetWindowClassName(hwnd)}");
                writer.WriteLine(new string('=', 80));

                try
                {
                    // 使用UIAutomation获取窗口元素
                    AutomationElement rootElement = AutomationElement.FromHandle(hwnd);
                    if (rootElement == null)
                    {
                        writer.WriteLine("错误: 无法获取窗口的AutomationElement");
                        return;
                    }

                    writer.WriteLine($"窗口标题: {rootElement.Current.Name}");
                    writer.WriteLine($"控件类型: {rootElement.Current.ControlType.ProgrammaticName}");
                    writer.WriteLine($"框架ID: {rootElement.Current.FrameworkId}");
                    writer.WriteLine();

                    // 查找Ribbon相关元素
                    FindAndScanRibbonElements(writer, rootElement, 0);

                    writer.WriteLine();
                    writer.WriteLine($"Ribbon LegacyPattern扫描完成");
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"扫描过程中发生错误: {ex.Message}");
                    writer.WriteLine($"错误详情: {ex}");
                }
            }
        }

        /// <summary>
        /// 查找并扫描Ribbon元素
        /// </summary>
        private void FindAndScanRibbonElements(StreamWriter writer, AutomationElement element, int indentLevel)
        {
            if (element == null) return;

            string indent = new string(' ', indentLevel * 2);

            try
            {
                // 检查是否为Ribbon相关元素
                bool isRibbonElement = IsRibbonElement(element);
                
                if (isRibbonElement)
                {
                    writer.WriteLine($"{indent}*** 发现Ribbon元素 ***");
                    WriteRibbonElementDetails(writer, element, indentLevel);
                    writer.WriteLine();
                }

                // 尝试使用LegacyPattern获取内容
                TryLegacyPatternAccess(writer, element, indentLevel);

                // 递归查找子元素
                var children = GetEnhancedChildren(element);
                foreach (var child in children)
                {
                    FindAndScanRibbonElements(writer, child, indentLevel + 1);
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否为Ribbon元素
        /// </summary>
        private bool IsRibbonElement(AutomationElement element)
        {
            try
            {
                string className = element.Current.ClassName?.ToUpper() ?? "";
                string name = element.Current.Name?.ToUpper() ?? "";
                string localizedControlType = element.Current.LocalizedControlType?.ToUpper() ?? "";

                // 检查类名
                if (className.Contains("RIBBON") || 
                    className.Contains("AFX:RIBBON") ||
                    className.Contains("RIBBONBAR"))
                {
                    return true;
                }

                // 检查名称
                if (name.Contains("RIBBON") || 
                    name.Contains("Ribbon"))
                {
                    return true;
                }

                // 检查控件类型
                if (localizedControlType.Contains("RIBBON") ||
                    localizedControlType.Contains("TOOLBAR"))
                {
                    return true;
                }

                // 检查AutomationId
                string automationId = element.Current.AutomationId?.ToUpper() ?? "";
                if (automationId.Contains("RIBBON"))
                {
                    return true;
                }
            }
            catch
            {
                // 忽略异常
            }

            return false;
        }

        /// <summary>
        /// 写入Ribbon元素详细信息
        /// </summary>
        private void WriteRibbonElementDetails(StreamWriter writer, AutomationElement element, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);

            writer.WriteLine($"{indent}名称: {element.Current.Name}");
            writer.WriteLine($"{indent}类型: {element.Current.ControlType.ProgrammaticName}");
            writer.WriteLine($"{indent}本地化类型: {element.Current.LocalizedControlType}");
            writer.WriteLine($"{indent}类名: {element.Current.ClassName}");
            writer.WriteLine($"{indent}AutomationId: {element.Current.AutomationId}");
            writer.WriteLine($"{indent}框架: {element.Current.FrameworkId}");

            var boundingRect = element.Current.BoundingRectangle;
            if (!boundingRect.IsEmpty)
            {
                writer.WriteLine($"{indent}位置: X={boundingRect.X:F0}, Y={boundingRect.Y:F0}, W={boundingRect.Width:F0}, H={boundingRect.Height:F0}");
            }

            writer.WriteLine($"{indent}启用状态: {element.Current.IsEnabled}");
            writer.WriteLine($"{indent}可见性: {element.Current.IsOffscreen}");
        }

        

        /// <summary>
        /// 尝试使用LegacyPattern访问元素
        /// </summary>
        private void TryLegacyPatternAccess(StreamWriter writer, AutomationElement element, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);

            try
            {
                // 定义LegacyPattern
                AutomationPattern LegacyPattern = AutomationPattern.LookupById(10018);

                // 尝试获取LegacyPattern
                if (element.TryGetCurrentPattern(LegacyPattern, out object legacyPatternObj))
                {
                    writer.WriteLine($"{indent}*** 成功获取LegacyPattern ***");
                    
                    if (legacyPatternObj != null)
                    {
                        try
                        {
                            // 使用反射获取LegacyPattern的属性或方法
                            Type legacyPatternType = legacyPatternObj.GetType();
                            writer.WriteLine($"{indent}LegacyPattern对象类型: {legacyPatternType.Name}");
                            
                            // 尝试获取一些常见的属性
                            var properties = legacyPatternType.GetProperties();
                            writer.WriteLine($"{indent}LegacyPattern可用属性数量: {properties.Length}");
                            
                            foreach (var prop in properties.Take(10)) // 只显示前10个属性
                            {
                                try
                                {
                                    var value = prop.GetValue(legacyPatternObj);
                                    writer.WriteLine($"{indent}  {prop.Name}: {value}");
                                }
                                catch (Exception ex)
                                {
                                    writer.WriteLine($"{indent}  {prop.Name}: [无法获取值: {ex.Message}]");
                                }
                            }

                            // 尝试获取方法
                            var methods = legacyPatternType.GetMethods()
                                .Where(m => m.IsPublic && !m.IsSpecialName)
                                .Take(5); // 只显示前5个公共方法
                            
                            writer.WriteLine($"{indent}LegacyPattern可用方法数量: {methods.Count()}");
                            foreach (var method in methods)
                            {
                                try
                                {
                                    writer.WriteLine($"{indent}  {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                                }
                                catch (Exception ex)
                                {
                                    writer.WriteLine($"{indent}  {method.Name}: [无法获取方法信息: {ex.Message}]");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine($"{indent}LegacyPattern操作错误: {ex.Message}");
                        }
                    }
                }
                else
                {
                    writer.WriteLine($"{indent}无法获取LegacyPattern");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}LegacyPattern访问错误: {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        static object GetProp(AutomationElement e, AutomationProperty p, bool cachedFirst = true)
        {
            if (p == null) return AutomationElement.NotSupported;
            if (cachedFirst)
            {
                try
                {
                    var v = e.GetCachedPropertyValue(p, true);
                    if (!ReferenceEquals(v, AutomationElement.NotSupported)) return v;
                }
                catch { }
            }
            try
            {
                var v = e.GetCurrentPropertyValue(p, true);
                return v;
            }
            catch
            {
                return AutomationElement.NotSupported;
            }
        }

        /// <summary>
        /// 增强版窗口扫描，使用UIA技术获取所有可能的子对象
        /// </summary>
        private void ScanEnhancedWindowWithUIA(IntPtr hwnd, string outputFile)
        {
            // 确保输出目录存在
            string directory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                writer.WriteLine($"增强版窗口扫描报告");
                writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"窗口句柄: 0x{hwnd.ToInt64():X}");
                writer.WriteLine($"窗口类名: {GetWindowClassName(hwnd)}");
                writer.WriteLine(new string('=', 80));

                try
                {
                    // 使用UIAutomation获取窗口元素
                    AutomationElement rootElement = AutomationElement.FromHandle(hwnd);
                    if (rootElement == null)
                    {
                        writer.WriteLine("错误: 无法获取窗口的AutomationElement");
                        return;
                    }

                    writer.WriteLine($"窗口标题: {rootElement.Current.Name}");
                    writer.WriteLine($"控件类型: {rootElement.Current.ControlType.ProgrammaticName}");
                    writer.WriteLine($"框架ID: {rootElement.Current.FrameworkId}");
                    writer.WriteLine();

                    // 遍历所有子对象，使用增强版方法
                    int elementCount = 0;
                    WriteEnhancedAutomationElementDetails(writer, rootElement, 0, ref elementCount);

                    writer.WriteLine();
                    writer.WriteLine($"增强扫描完成，共找到 {elementCount} 个UI元素");
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"扫描过程中发生错误: {ex.Message}");
                    writer.WriteLine($"错误详情: {ex}");
                }
            }
        }

        /// <summary>
        /// 使用UIA技术扫描AFX窗口的所有子对象并保存到文件
        /// </summary>
        private void ScanAfxWindowWithUIA(IntPtr hwnd, string outputFile)
        {
            // 确保输出目录存在
            string directory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                writer.WriteLine($"AFX应用程序窗口扫描报告");
                writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"窗口句柄: 0x{hwnd.ToInt64():X}");
                writer.WriteLine($"窗口类名: {GetWindowClassName(hwnd)}");
                writer.WriteLine(new string('=', 80));

                try
                {
                    // 使用UIAutomation获取窗口元素
                    AutomationElement rootElement = AutomationElement.FromHandle(hwnd);
                    if (rootElement == null)
                    {
                        writer.WriteLine("错误: 无法获取窗口的AutomationElement");
                        return;
                    }

                    writer.WriteLine($"窗口标题: {rootElement.Current.Name}");
                    writer.WriteLine($"控件类型: {rootElement.Current.ControlType.ProgrammaticName}");
                    writer.WriteLine($"框架ID: {rootElement.Current.FrameworkId}");
                    writer.WriteLine();

                    // 遍历所有子对象
                    int elementCount = 0;
                    WriteAutomationElementDetails(writer, rootElement, 0, ref elementCount);

                    writer.WriteLine();
                    writer.WriteLine($"扫描完成，共找到 {elementCount} 个UI元素");
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"扫描过程中发生错误: {ex.Message}");
                    writer.WriteLine($"错误详情: {ex}");
                }
            }
        }

        /// <summary>
        /// 获取窗口类名
        /// </summary>
        private string GetWindowClassName(IntPtr hwnd)
        {
            StringBuilder className = new StringBuilder(256);
            if (GetClassName(hwnd, className, className.Capacity) > 0)
            {
                return className.ToString();
            }
            return "Unknown";
        }

        /// <summary>
        /// 递归写入AutomationElement的详细信息
        /// </summary>
        private void WriteAutomationElementDetails(StreamWriter writer, AutomationElement element, int indentLevel, ref int elementCount)
        {
            if (element == null) return;

            elementCount++;
            string indent = new string(' ', indentLevel * 2);

            try
            {
                // 基本信息
                writer.WriteLine($"{indent}[{elementCount}] {element.Current.Name}");
                writer.WriteLine($"{indent}    类型: {element.Current.ControlType.ProgrammaticName}");
                writer.WriteLine($"{indent}    本地化类型: {element.Current.LocalizedControlType}");
                
                if (!string.IsNullOrEmpty(element.Current.AutomationId))
                    writer.WriteLine($"{indent}    AutomationId: {element.Current.AutomationId}");
                
                if (!string.IsNullOrEmpty(element.Current.ClassName))
                    writer.WriteLine($"{indent}    类名: {element.Current.ClassName}");
                
                if (!string.IsNullOrEmpty(element.Current.FrameworkId))
                    writer.WriteLine($"{indent}    框架: {element.Current.FrameworkId}");

                // 位置信息
                var boundingRect = element.Current.BoundingRectangle;
                if (!boundingRect.IsEmpty)
                {
                    writer.WriteLine($"{indent}    位置: X={boundingRect.X:F0}, Y={boundingRect.Y:F0}, W={boundingRect.Width:F0}, H={boundingRect.Height:F0}");
                }

                // 状态信息
                writer.WriteLine($"{indent}    启用状态: {element.Current.IsEnabled}");
                writer.WriteLine($"{indent}    可见性: {element.Current.IsOffscreen}");
                writer.WriteLine($"{indent}    可键盘聚焦: {element.Current.IsKeyboardFocusable}");
                
                if (element.Current.HasKeyboardFocus)
                    writer.WriteLine($"{indent}    键盘焦点: {element.Current.HasKeyboardFocus}");

                // 特别标记AFX相关控件
                if (IsAfxRelatedControl(element))
                {
                    writer.WriteLine($"{indent}    *** AFX相关控件 ***");
                }

                writer.WriteLine();

                // 处理子元素
                var children = GetChildren(element);
                if (children.Count > 0)
                {
                    writer.WriteLine($"{indent}    子元素数量: {children.Count}");
                    foreach (AutomationElement child in children)
                    {
                        WriteAutomationElementDetails(writer, child, indentLevel + 1, ref elementCount);
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}    错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 增强版递归写入AutomationElement的详细信息，尝试更多获取策略
        /// </summary>
        private void WriteEnhancedAutomationElementDetails(StreamWriter writer, AutomationElement element, int indentLevel, ref int elementCount)
        {
            if (element == null) return;

            elementCount++;
            string indent = new string(' ', indentLevel * 2);

            try
            {
                // 基本信息
                writer.WriteLine($"{indent}[{elementCount}] {element.Current.Name}");
                writer.WriteLine($"{indent}    类型: {element.Current.ControlType.ProgrammaticName}");
                writer.WriteLine($"{indent}    本地化类型: {element.Current.LocalizedControlType}");
                
                if (!string.IsNullOrEmpty(element.Current.AutomationId))
                    writer.WriteLine($"{indent}    AutomationId: {element.Current.AutomationId}");
                
                if (!string.IsNullOrEmpty(element.Current.ClassName))
                    writer.WriteLine($"{indent}    类名: {element.Current.ClassName}");
                
                if (!string.IsNullOrEmpty(element.Current.FrameworkId))
                    writer.WriteLine($"{indent}    框架: {element.Current.FrameworkId}");

                // 位置信息
                var boundingRect = element.Current.BoundingRectangle;
                if (!boundingRect.IsEmpty)
                {
                    writer.WriteLine($"{indent}    位置: X={boundingRect.X:F0}, Y={boundingRect.Y:F0}, W={boundingRect.Width:F0}, H={boundingRect.Height:F0}");
                }

                // 状态信息
                writer.WriteLine($"{indent}    启用状态: {element.Current.IsEnabled}");
                writer.WriteLine($"{indent}    可见性: {element.Current.IsOffscreen}");
                writer.WriteLine($"{indent}    可键盘聚焦: {element.Current.IsKeyboardFocusable}");
                
                if (element.Current.HasKeyboardFocus)
                    writer.WriteLine($"{indent}    键盘焦点: {element.Current.HasKeyboardFocus}");

                // 特别标记AFX相关控件
                if (IsAfxRelatedControl(element))
                {
                    writer.WriteLine($"{indent}    *** AFX相关控件 ***");
                }

                writer.WriteLine();

                // 使用增强版子元素获取策略
                var children = GetEnhancedChildren(element);
                if (children.Count > 0)
                {
                    writer.WriteLine($"{indent}    子元素数量: {children.Count}");
                    foreach (AutomationElement child in children)
                    {
                        WriteEnhancedAutomationElementDetails(writer, child, indentLevel + 1, ref elementCount);
                    }
                }
                else
                {
                    writer.WriteLine($"{indent}    无子元素或无法获取子元素");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}    错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 增强版获取元素的子元素，使用更多策略确保完整性
        /// </summary>
        private List<AutomationElement> GetEnhancedChildren(AutomationElement element)
        {
            var children = new List<AutomationElement>();

            try
            {
                // 策略1: 使用TreeWalker.ControlViewWalker
                var walker = TreeWalker.ControlViewWalker;
                AutomationElement child = walker.GetFirstChild(element);
                while (child != null)
                {
                    children.Add(child);
                    child = walker.GetNextSibling(child);
                }

                // 策略2: 使用TreeWalker.RawViewWalker（获取更多元素）
                var rawWalker = TreeWalker.RawViewWalker;
                AutomationElement rawChild = rawWalker.GetFirstChild(element);
                while (rawChild != null)
                {
                    // 避免重复添加
                    if (!children.Any(c => c.GetRuntimeId().SequenceEqual(rawChild.GetRuntimeId())))
                    {
                        children.Add(rawChild);
                    }
                    rawChild = rawWalker.GetNextSibling(rawChild);
                }

                // 策略3: 使用FindAll方法（TreeScope.Children）
                var foundChildren = element.FindAll(TreeScope.Children, Condition.TrueCondition);
                foreach (AutomationElement foundChild in foundChildren)
                {
                    if (!children.Any(c => c.GetRuntimeId().SequenceEqual(foundChild.GetRuntimeId())))
                    {
                        children.Add(foundChild);
                    }
                }

                // 策略4: 使用FindAll方法（TreeScope.Descendants，但限制深度）
                if (children.Count == 0)
                {
                    var descendants = element.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                    var directChildren = new List<AutomationElement>();
                    
                    foreach (AutomationElement descendant in descendants)
                    {
                        // 检查是否为直接子元素
                        var parent = TreeWalker.RawViewWalker.GetParent(descendant);
                        if (parent != null && parent.GetRuntimeId().SequenceEqual(element.GetRuntimeId()))
                        {
                            directChildren.Add(descendant);
                        }
                    }
                    
                    children.AddRange(directChildren);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取增强子元素时发生错误: {ex.Message}");
            }

            return children;
        }

        /// <summary>
        /// 获取元素的子元素，使用多种方法确保完整性
        /// </summary>
        private List<AutomationElement> GetChildren(AutomationElement element)
        {
            var children = new List<AutomationElement>();

            try
            {
                // 方法1: 使用TreeWalker.ControlViewWalker
                var walker = TreeWalker.ControlViewWalker;
                AutomationElement child = walker.GetFirstChild(element);
                while (child != null)
                {
                    children.Add(child);
                    child = walker.GetNextSibling(child);
                }

                // 方法2: 如果ControlViewWalker没有找到子元素，尝试RawViewWalker
                if (children.Count == 0)
                {
                    walker = TreeWalker.RawViewWalker;
                    child = walker.GetFirstChild(element);
                    while (child != null)
                    {
                        children.Add(child);
                        child = walker.GetNextSibling(child);
                    }
                }

                // 方法3: 使用FindAll作为最后的备选方案
                if (children.Count == 0)
                {
                    var foundElements = element.FindAll(TreeScope.Children, Condition.TrueCondition);
                    foreach (AutomationElement foundChild in foundElements)
                    {
                        children.Add(foundChild);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取子元素时发生错误: {ex.Message}");
            }

            return children;
        }

        /// <summary>
        /// 判断是否为AFX相关控件
        /// </summary>
        private bool IsAfxRelatedControl(AutomationElement element)
        {
            try
            {
                // 检查类名是否包含AFX相关关键字
                string className = element.Current.ClassName?.ToUpper() ?? "";
                if (className.Contains("AFX") || className.Contains("MFC"))
                {
                    return true;
                }

                // 检查框架ID
                string frameworkId = element.Current.FrameworkId?.ToUpper() ?? "";
                if (frameworkId.Contains("WIN32") && className.Contains("CONTROL"))
                {
                    return true;
                }

                // 检查特定的控件类型
                var controlType = element.Current.ControlType;
                if (controlType == ControlType.ToolBar ||
                    controlType == ControlType.StatusBar ||
                    controlType == ControlType.MenuBar ||
                    controlType == ControlType.Document ||
                    controlType == ControlType.Pane)
                {
                    return true;
                }
            }
            catch
            {
                // 忽略异常
            }

            return false;
        }
    }

    // 修复的扫描器类
    public class AfxApplicationScanner : IDisposable
    {
        private readonly DesktopDataContext _context;

        public AfxApplicationScanner(IntPtr pid)
        {
            // 使用默认的DesktopDataContext
            _context = DesktopDataContext.DefaultContext;
        }

        public AfxApplicationScanner()
        {
            // 使用默认的DesktopDataContext
            _context = DesktopDataContext.DefaultContext;
        }

        public void ScanAfxApplicationByProcessName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"未找到进程: {processName}");
                    return;
                }

                foreach (var process in processes)
                {
                    Console.WriteLine($"正在扫描进程: {process.ProcessName} (PID: {process.Id})");
                    ScanAfxWindow(process.MainWindowHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描错误: {ex.Message}");
                throw;
            }
        }

        public void ScanAfxWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                Console.WriteLine("无效的窗口句柄");
                return;
            }

            try
            {
                // 使用UIAutomation获取窗口元素
                AutomationElement windowElement = AutomationElement.FromHandle(windowHandle);

                if (windowElement != null)
                {
                    Console.WriteLine($"窗口标题: {windowElement.Current.Name}");
                    Console.WriteLine("开始遍历对象树...");
                    Console.WriteLine("==================================");

                    TraverseAfxElements(windowElement, 0);

                    Console.WriteLine("==================================");
                    Console.WriteLine("遍历完成。");
                }
                else
                {
                    Console.WriteLine("无法获取窗口元素");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"遍历错误: {ex.Message}");
                throw;
            }
        }

        public A11yElement GetDesktopElement()
        {
            try
            {
                var elements = A11yAutomation.ElementsFromProcessId(Process.GetCurrentProcess().Id, _context);
                return elements.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取桌面元素时发生错误: {ex.Message}");
                return null;
            }
        }

        private List<AutomationElement> GetChildren(AutomationElement element)
        {
            var children = new List<AutomationElement>();

            try
            {
                // 方法1: 使用TreeWalker.ControlViewWalker
                var walker = TreeWalker.ControlViewWalker;
                AutomationElement child = walker.GetFirstChild(element);
                while (child != null)
                {
                    children.Add(child);
                    child = walker.GetNextSibling(child);
                }

                // 方法2: 如果ControlViewWalker没有找到子元素，尝试RawViewWalker
                if (children.Count == 0)
                {
                    walker = TreeWalker.RawViewWalker;
                    child = walker.GetFirstChild(element);
                    while (child != null)
                    {
                        children.Add(child);
                        child = walker.GetNextSibling(child);
                    }
                }

                // 方法3: 使用FindAll作为最后的备选方案
                if (children.Count == 0)
                {
                    var foundElements = element.FindAll(TreeScope.Children, Condition.TrueCondition);
                    foreach (AutomationElement foundChild in foundElements)
                    {
                        children.Add(foundChild);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取子元素时发生错误: {ex.Message}");
            }

            return children;
        }

        public void TraverseAfxElements(AutomationElement element, int depth = 0)
        {
            if (element == null) return;

            string indent = new string(' ', depth * 2);
            string elementInfo = $"{indent}└── {GetElementDescription(element)}";

            Console.WriteLine(elementInfo);

            // 特别关注 AFX 控件类型
            if (IsAfxSpecificControl(element))
            {
                Console.WriteLine($"{indent}    [AFX 控件]");
                PrintAfxSpecificProperties(element, indent + "    ");
            }

            // 递归遍历子元素，但限制深度以避免栈溢出
            if (depth < 10)
            {
                var children = GetChildren(element);
                foreach (var child in children)
                {
                    TraverseAfxElements(child, depth + 1);
                }
            }
        }

        public void TraverseAfxElements(A11yElement element, int depth = 0)
        {
            if (element == null) return;

            string indent = new string(' ', depth * 2);
            string elementInfo = $"{indent}└── {GetElementDescription(element)}";

            Console.WriteLine(elementInfo);

            // 特别关注 AFX 控件类型
            if (IsAfxSpecificControl(element))
            {
                Console.WriteLine($"{indent}    [AFX 控件]");
                PrintAfxSpecificProperties(element, indent + "    ");
            }

            // 递归遍历子元素，但限制深度以避免栈溢出
            if (depth < 10 && element.Children != null && element.Children.Count > 0)
            {
                foreach (var child in element.Children)
                {
                    TraverseAfxElements(child, depth + 1);
                }
            }
        }

        public string GetElementDescription(AutomationElement element)
        {
            string description = $"{element.Current.Name} [{element.Current.ControlType.ProgrammaticName}]";

            if (!string.IsNullOrEmpty(element.Current.AutomationId))
                description += $", ID: {element.Current.AutomationId}";

            if (!string.IsNullOrEmpty(element.Current.ClassName))
                description += $", Class: {element.Current.ClassName}";

            description += $", Enabled: {element.Current.IsEnabled}";

            return description;
        }

        public string GetElementDescription(A11yElement element)
        {
            string description = $"{element.Name} [{element.LocalizedControlType}]";

            if (!string.IsNullOrEmpty(element.AutomationId))
                description += $", ID: {element.AutomationId}";

            if (!string.IsNullOrEmpty(element.ClassName))
                description += $", Class: {element.ClassName}";

            description += $", Enabled: {element.IsEnabled}";

            return description;
        }

        public bool IsAfxSpecificControl(AutomationElement element)
        {
            try
            {
                // 检查类名是否包含AFX相关关键字
                string className = element.Current.ClassName?.ToUpper() ?? "";
                if (className.Contains("AFX") || className.Contains("MFC"))
                {
                    return true;
                }

                // 检查特定的控件类型
                var controlType = element.Current.ControlType;
                if (controlType == ControlType.ToolBar ||
                    controlType == ControlType.StatusBar ||
                    controlType == ControlType.MenuBar ||
                    controlType == ControlType.Document ||
                    controlType == ControlType.Pane)
                {
                    return true;
                }
            }
            catch
            {
                // 忽略异常
            }

            return false;
        }

        public bool IsAfxSpecificControl(A11yElement element)
        {
            try
            {
                // 检查类名是否包含AFX相关关键字
                string className = element.ClassName?.ToUpper() ?? "";
                if (className.Contains("AFX") || className.Contains("MFC"))
                {
                    return true;
                }

                // 检查本地化控件类型
                string localizedControlType = element.LocalizedControlType?.ToUpper() ?? "";
                if (localizedControlType.Contains("TOOLBAR") ||
                    localizedControlType.Contains("STATUSBAR") ||
                    localizedControlType.Contains("MENUBAR") ||
                    localizedControlType.Contains("DOCUMENT") ||
                    localizedControlType.Contains("PANE"))
                {
                    return true;
                }
            }
            catch
            {
                // 忽略异常
            }

            return false;
        }

        public void PrintAfxSpecificProperties(AutomationElement element, string indent)
        {
            if (!string.IsNullOrEmpty(element.Current.ClassName))
                Console.WriteLine($"{indent}类名: {element.Current.ClassName}");

            if (!string.IsNullOrEmpty(element.Current.FrameworkId))
                Console.WriteLine($"{indent}框架: {element.Current.FrameworkId}");

            var boundingRect = element.Current.BoundingRectangle;
            if (!boundingRect.IsEmpty)
                Console.WriteLine($"{indent}位置: {boundingRect}");

            if (element.Current.IsKeyboardFocusable)
                Console.WriteLine($"{indent}可键盘聚焦: 是");
        }

        public void PrintAfxSpecificProperties(A11yElement element, string indent)
        {
            if (!string.IsNullOrEmpty(element.ClassName))
                Console.WriteLine($"{indent}类名: {element.ClassName}");

            if (!string.IsNullOrEmpty(element.Framework))
                Console.WriteLine($"{indent}框架: {element.Framework}");

            var boundingRect = element.BoundingRectangle;
            if (boundingRect != null && !boundingRect.IsEmpty)
                Console.WriteLine($"{indent}位置: {boundingRect}");

            if (element.IsKeyboardFocusable)
                Console.WriteLine($"{indent}可键盘聚焦: 是");
        }

        public void Dispose()
        {
            // DesktopDataContext doesn't implement IDisposable
            // No cleanup needed for this implementation
        }

        
    }
}
