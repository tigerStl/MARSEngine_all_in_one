using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mars.Securities;
using Mars.Inter.MQCenter.MSAASupport;
using Accessibility;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Mars.Inter.MQCenter.windowsControlsHelpers;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.Business;
using com.Mars.Constants;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using System.Diagnostics;
using Axe.Windows.Desktop.UIAutomation;
using Axe.Windows.Desktop.UIAutomation.CustomObjects;
using Axe.Windows.Automation;
using System.Linq;
using MarsUnitTest.UIATest;
using System.Windows.Automation;
using System.Threading;
using MarsUnitTest.HybridAfx;

namespace MarsUnitTest
{

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var pwd = Mars.message.Securities.MarsEncodePwd.EncodeString("admin");// marsEncodePwd = new Mars.message.Securities.MarsEncodePwd();
            Console.WriteLine($"Encoded pwd: {pwd}");

        }





        [TestMethod]
        public void TestMSAATableInfo()
        {
            Console.WriteLine("请输入窗口句柄 (hwnd)：");
            //string hwndStr = Console.ReadLine();

            //if (!long.TryParse(hwndStr, out long hwndLong))
            //{
            //    Console.WriteLine("输入无效。");
            //    return;
            //}
            IntPtr hwnd = new IntPtr(331032);

            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                Console.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            // 获取类型
            object roleObj = accObj.get_accRole(0);
            int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
            string typeName = MARSAccessibleProvider.GetRoleName(role);
            Console.WriteLine($"对象类型: {typeName}");

            if (role == 0x20) // ROLE_SYSTEM_TABLE
            {
                var columns = provider.GetTableColumns(hwnd);
                int rows = provider.GetTableRowCount(hwnd);
                Console.WriteLine($"Table Columns: {string.Join(", ", columns)}");
                Console.WriteLine($"Table Rows: {rows}");

                int printed = 0;
                for (int r = 0; r < rows && printed < 20; r++)
                {
                    for (int c = 0; c < columns.Count && printed < 20; c++)
                    {
                        string val = provider.GetTableCellValue(hwnd, r, c);
                        Console.WriteLine($"Cell[{r},{c}]: {val}");
                        printed++;
                    }
                }
            }
        }


        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMSAATableInfo2()
        {
            IntPtr hwnd = new IntPtr(331032);

            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                TestContext.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            object roleObj = accObj.get_accRole(0);
            int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
            string typeName = MARSAccessibleProvider.GetRoleName(role);
            TestContext.WriteLine($"对象类型: {typeName}");

            // 枚举所有子对象的 role name（推荐用 AccessibleChildren API）
            int childCount = accObj.accChildCount;
            if (childCount > 0)
            {
                object[] children = new object[childCount];
                int obtained = AccessibleChildren(accObj, 0, childCount, children, out int nObtained);
                for (int i = 0; i < nObtained; i++)
                {
                    if (children[i] is IAccessible childAcc)
                    {
                        int childRole = provider.GetRole(childAcc);
                        string childRoleName = MARSAccessibleProvider.GetRoleName(childRole);
                        TestContext.WriteLine($"子对象[{i}] Role: {childRoleName}");
                    }
                    else
                    {
                        TestContext.WriteLine($"子对象[{i}] Role: (非IAccessible或简单元素)");
                    }
                }
            }
            else
            {
                TestContext.WriteLine("无子对象。");
            }

            if (role == 0x20) // ROLE_SYSTEM_TABLE
            {
                var columns = provider.GetTableColumns(hwnd);
                int rows = provider.GetTableRowCount(hwnd);
                TestContext.WriteLine($"Table Columns: {string.Join(", ", columns)}");
                TestContext.WriteLine($"Table Rows: {rows}");

                int printed = 0;
                for (int r = 0; r < rows && printed < 20; r++)
                {
                    for (int c = 0; c < columns.Count && printed < 20; c++)
                    {
                        string val = provider.GetTableCellValue(hwnd, r, c);
                        TestContext.WriteLine($"Cell[{r},{c}]: {val}");
                        printed++;
                    }
                }
            }
        }

        // MSAA AccessibleChildren API
        [DllImport("oleacc.dll")]
        private static extern int AccessibleChildren(
            IAccessible paccContainer,
            int iChildStart,
            int cChildren,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
            out int pcObtained);

        // Windows API for drawing border
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);

        [DllImport("gdi32.dll")]
        private static extern bool LineTo(IntPtr hdc, int x, int y);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("kernel32.dll")]
        private static extern void Sleep(uint dwMilliseconds);

        // Windows API for UFT AttachText generation
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }





        //private int GetDlgCtrlID(IntPtr hwnd)
        //{
        //    if (hwnd == IntPtr.Zero) return 0;
        //    return GetDlgCtrlID(hwnd);
        //}



        [TestMethod]
        public void TestAccessibleTreeToFile()
        {
            IntPtr hwnd = new IntPtr(0x00000000003A04DC);

            //MfcAccessibleHelper.NavigateAllObjects(hwnd);

            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                TestContext.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 开始遍历可访问性树结构 ===");
            MARSAccessibleProvider.TraverseAccessibleTreeNonRecursiveNew(accObj, provider, sb, 0, hwnd);
            //MARSAccessibleProvider.TraverseAccessibleTreeNonRecursive(accObj, provider, sb, 0, hwnd);
            sb.AppendLine("=== 遍历完成 ===");

            string filePath = Path.Combine("c:\\temp\\", $"AccessibleTree_{DateTime.Now.ToString("yyyyMMddhhmm")}.txt");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            TestContext.WriteLine($"树状结构已保存到: {filePath}");
        }


        [TestMethod]
        public void TestButtonDropDownGridDataExtraction()
        {
            // 请替换为您实际的窗口句柄
            IntPtr hwnd = new IntPtr(0x00020FF2); // 请修改为您的窗口句柄

            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                TestContext.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            // 检查是否为ButtonDropDownGrid
            object roleObj = provider.GetRole(accObj);
            int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
            string roleName = MARSAccessibleProvider.GetRoleName(role);
            TestContext.WriteLine($"对象类型: {roleName} (Role: {role})");

            if (roleName.Equals("ButtonDropDownGrid", StringComparison.OrdinalIgnoreCase) || role == 24)
            {
                TestContext.WriteLine("检测到ButtonDropDownGrid，开始提取数据...");

                // 尝试转换为Accessible2和IAccessibleTable
                IAccessible2 accessible2 = MARSAccessibleProvider.TryConvertToAccessible2(accObj);
                IAccessibleTable accessibleTable = MARSAccessibleProvider.TryConvertToAccessibleTable(accObj);

                if (accessible2 != null)
                {
                    TestContext.WriteLine("✓ 成功转换为IAccessible2");
                    try
                    {
                        accessible2.get_attributes(out string attributes);
                        TestContext.WriteLine($"  Attributes: {attributes}");
                        accessible2.get_states(out long states);
                        TestContext.WriteLine($"  States: {states}");
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"  Accessible2信息获取失败: {ex.Message}");
                    }
                }
                else
                {
                    TestContext.WriteLine("✗ 无法转换为IAccessible2");
                }

                if (accessibleTable != null)
                {
                    TestContext.WriteLine("✓ 成功转换为IAccessibleTable");
                    try
                    {
                        accessibleTable.get_nColumns(out long columnCount);
                        accessibleTable.get_nRows(out long rowCount);
                        TestContext.WriteLine($"  列数: {columnCount}, 行数: {rowCount}");

                        // 尝试获取列信息
                        if (columnCount > 0)
                        {
                            TestContext.WriteLine("  列信息:");
                            for (int col = 0; col < columnCount && col < 20; col++)
                            {
                                try
                                {
                                    accessibleTable.get_columnDescription(col, out string colDescription);
                                    accessibleTable.get_columnHeader(out IAccessible colHeader, out long startingRowIndex);
                                    string headerName = "";
                                    if (colHeader != null)
                                    {
                                        try { headerName = colHeader.get_accName(0); } catch { }
                                    }
                                    TestContext.WriteLine($"    列[{col}]: \"{headerName}\" (描述: {colDescription})");
                                }
                                catch (Exception ex)
                                {
                                    TestContext.WriteLine($"    列[{col}]: 获取失败 - {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"  IAccessibleTable信息获取失败: {ex.Message}");
                    }
                }
                else
                {
                    TestContext.WriteLine("✗ 无法转换为IAccessibleTable");
                }

                // 使用传统方法遍历子对象获取数据
                TestContext.WriteLine("\n使用传统方法遍历子对象:");
                ExtractButtonDropDownGridData(accObj, provider);
            }
            else
            {
                TestContext.WriteLine($"对象类型不是ButtonDropDownGrid，而是: {roleName}");
            }
        }

        private void ExtractButtonDropDownGridData(IAccessible accObj, MARSAccessibleProvider provider)
        {
            try
            {
                int childCount = accObj.accChildCount;
                TestContext.WriteLine($"子对象总数: {childCount}");

                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = AccessibleChildren(accObj, 0, childCount, children, out int nObtained);
                    TestContext.WriteLine($"实际获取到的子对象数: {nObtained}");

                    List<string> columns = new List<string>();
                    List<List<string>> rows = new List<List<string>>();

                    for (int i = 0; i < nObtained; i++)
                    {
                        if (children[i] is IAccessible childAcc)
                        {
                            object childRoleObj = provider.GetRole(childAcc);
                            int childRole = (childRoleObj is int) ? (int)childRoleObj : Convert.ToInt32(childRoleObj);
                            string childRoleName = MARSAccessibleProvider.GetRoleName(childRole);
                            string childText = "";
                            try { childText = childAcc.get_accName(0); } catch { }
                            if (string.IsNullOrEmpty(childText))
                            {
                                try { childText = childAcc.get_accValue(0); } catch { }
                            }

                            TestContext.WriteLine($"  子对象[{i}]: {childRoleName} - \"{childText}\"");

                            // 如果是行对象
                            if (childRoleName.Equals("row", StringComparison.OrdinalIgnoreCase) || childRole == 0x1C)
                            {
                                TestContext.WriteLine($"    发现行对象，开始提取单元格...");
                                ExtractRowCells(childAcc, provider, rows, columns);
                            }
                            // 如果是列头对象
                            else if (childRoleName.Equals("columnheader", StringComparison.OrdinalIgnoreCase) || childRole == 0x19)
                            {
                                if (!string.IsNullOrEmpty(childText) && !columns.Contains(childText))
                                {
                                    columns.Add(childText);
                                    TestContext.WriteLine($"    发现列头: \"{childText}\"");
                                }
                            }
                        }
                        else if (Marshal.IsComObject(children[i]))
                        {
                            TestContext.WriteLine($"  子对象[{i}]: COM对象，尝试转换...");
                            IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                            try
                            {
                                var convertedAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                if (convertedAcc != null)
                                {
                                    object convertedRoleObj = provider.GetRole(convertedAcc);
                                    int convertedRole = (convertedRoleObj is int) ? (int)convertedRoleObj : Convert.ToInt32(convertedRoleObj);
                                    string convertedRoleName = MARSAccessibleProvider.GetRoleName(convertedRole);
                                    TestContext.WriteLine($"    转换后类型: {convertedRoleName}");

                                    if (convertedRoleName.Equals("row", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ExtractRowCells(convertedAcc, provider, rows, columns);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                TestContext.WriteLine($"    COM对象转换失败: {ex.Message}");
                            }
                            finally
                            {
                                Marshal.Release(unk);
                            }
                        }
                    }

                    // 输出提取的数据
                    TestContext.WriteLine($"\n提取结果:");
                    TestContext.WriteLine($"检测到的列数: {columns.Count}");
                    if (columns.Count > 0)
                    {
                        TestContext.WriteLine($"列名: {string.Join(", ", columns)}");
                    }

                    TestContext.WriteLine($"检测到的行数: {rows.Count}");
                    for (int i = 0; i < Math.Min(rows.Count, 10); i++) // 限制显示前10行
                    {
                        TestContext.WriteLine($"  行[{i}]: {string.Join(" | ", rows[i])}");
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"提取ButtonDropDownGrid数据时发生错误: {ex.Message}");
            }
        }

        private void ExtractRowCells(IAccessible rowAcc, MARSAccessibleProvider provider, List<List<string>> rows, List<string> columns)
        {
            try
            {
                int cellCount = rowAcc.accChildCount;
                TestContext.WriteLine($"    行包含 {cellCount} 个单元格");

                List<string> rowCells = new List<string>();

                if (cellCount > 0)
                {
                    object[] cellChildren = new object[cellCount];
                    int cellObtained = AccessibleChildren(rowAcc, 0, cellCount, cellChildren, out int nCellObtained);

                    for (int j = 0; j < nCellObtained; j++)
                    {
                        if (cellChildren[j] is IAccessible cellAcc)
                        {
                            object cellRoleObj = provider.GetRole(cellAcc);
                            int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                            string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                            string cellText = "";
                            try { cellText = cellAcc.get_accValue(0); } catch { }
                            if (string.IsNullOrEmpty(cellText))
                            {
                                try { cellText = cellAcc.get_accName(0); } catch { }
                            }

                            TestContext.WriteLine($"      单元格[{j}]: {cellRoleName} - \"{cellText}\"");
                            rowCells.Add(cellText);

                            // 如果是列头，添加到列列表
                            if (cellRoleName.Equals("columnheader", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cellText) && !columns.Contains(cellText))
                            {
                                columns.Add(cellText);
                            }
                        }
                        else
                        {
                            TestContext.WriteLine($"      单元格[{j}]: 非IAccessible对象");
                            rowCells.Add("");
                        }
                    }
                }

                rows.Add(rowCells);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"    提取行单元格时发生错误: {ex.Message}");
            }
        }

        // 绘制红色闪烁框
        private void FlashRedBorder(IAccessible accessible, object childIndex = null, int flashCount = 3)
        {
            try
            {
                // 获取对象位置
                accessible.accLocation(out int x, out int y, out int width, out int height, childIndex ?? 0);

                // 获取桌面DC
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero) return;

                // 创建红色画笔
                uint redColor = 0x0000FF; // RGB(255, 0, 0) - 红色
                IntPtr redPen = CreatePen(0, 3, redColor); // 0 = PS_SOLID, 3 = 宽度
                IntPtr oldPen = SelectObject(hdc, redPen);

                // 闪烁3次
                for (int i = 0; i < flashCount; i++)
                {
                    // 绘制红色边框
                    Rectangle(hdc, x, y, x + width, y + height);

                    // 等待200毫秒
                    Sleep(200);

                    // 再次绘制（覆盖之前的边框，产生闪烁效果）
                    Rectangle(hdc, x, y, x + width, y + height);

                    // 如果不是最后一次，等待200毫秒
                    if (i < flashCount - 1)
                    {
                        Sleep(200);
                    }
                }

                // 清理资源
                SelectObject(hdc, oldPen);
                DeleteObject(redPen);
                ReleaseDC(IntPtr.Zero, hdc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"绘制红色闪烁框失败: {ex.Message}");
            }
        }

        // 异步绘制红色闪烁框
        private async Task FlashRedBorderAsync(IAccessible accessible, object childIndex = null, int flashCount = 3)
        {
            await Task.Run(() => FlashRedBorder(accessible, childIndex, flashCount));
        }


        // 获取对象的详细位置信息（包括相对位置）
        private string GetDetailedLocationInfo(IAccessible accessible, object childIndex = null)
        {
            try
            {
                // 获取当前位置
                accessible.accLocation(out int x, out int y, out int width, out int height, childIndex ?? 0);

                // 尝试获取父对象位置进行相对位置计算
                try
                {
                    IAccessible parent = accessible.accParent as IAccessible;
                    if (parent != null)
                    {
                        parent.accLocation(out int parentX, out int parentY, out int parentWidth, out int parentHeight, 0);
                        int relativeX = x - parentX;
                        int relativeY = y - parentY;
                        return $"绝对位置:X:{x},Y:{y},W:{width},H:{height}|相对位置:X:{relativeX},Y:{relativeY}|父位置:X:{parentX},Y:{parentY},W:{parentWidth},H:{parentHeight}";
                    }
                }
                catch
                {
                    // 如果无法获取父对象位置，只返回绝对位置
                }

                return $"绝对位置:X:{x},Y:{y},W:{width},H:{height}";
            }
            catch (Exception ex)
            {
                return $"位置获取失败:{ex.Message}";
            }
        }

        [TestMethod]
        public void TestPushButtonWithDropDownGrid()
        {
            // 针对您的场景：pushbutton包含ButtonDropDownGrid
            IntPtr hwnd = new IntPtr(0x00111392); // 请修改为您的窗口句柄

            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                TestContext.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            object roleObj = provider.GetRole(accObj);
            int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
            string roleName = MARSAccessibleProvider.GetRoleName(role);
            TestContext.WriteLine($"根对象类型: {roleName} (Role: {role})");

            // 如果是pushbutton，查找其中的ButtonDropDownGrid
            if (roleName.Equals("pushbutton", StringComparison.OrdinalIgnoreCase) || role == 0x2B)
            {
                TestContext.WriteLine("检测到PushButton，开始查找ButtonDropDownGrid子对象...");
                FindAndExtractDropDownGrid(accObj, provider);
            }
            else
            {
                TestContext.WriteLine($"对象类型不是PushButton，而是: {roleName}");
                // 即使不是pushbutton，也尝试查找ButtonDropDownGrid
                FindAndExtractDropDownGrid(accObj, provider);
            }
        }

        private void FindAndExtractDropDownGrid(IAccessible parentAcc, MARSAccessibleProvider provider)
        {
            try
            {
                int childCount = parentAcc.accChildCount;
                TestContext.WriteLine($"父对象子元素总数: {childCount}");

                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = AccessibleChildren(parentAcc, 0, childCount, children, out int nObtained);
                    TestContext.WriteLine($"实际获取到的子对象数: {nObtained}");

                    for (int i = 0; i < nObtained; i++)
                    {
                        if (children[i] is IAccessible childAcc)
                        {
                            object childRoleObj = provider.GetRole(childAcc);
                            int childRole = (childRoleObj is int) ? (int)childRoleObj : Convert.ToInt32(childRoleObj);
                            string childRoleName = MARSAccessibleProvider.GetRoleName(childRole);
                            string childText = "";
                            try { childText = childAcc.get_accName(0); } catch { }
                            if (string.IsNullOrEmpty(childText))
                            {
                                try { childText = childAcc.get_accValue(0); } catch { }
                            }

                            TestContext.WriteLine($"  子对象[{i}]: {childRoleName} - \"{childText}\"");

                            // 如果是ButtonDropDownGrid
                            if (childRoleName.Equals("ButtonDropDownGrid", StringComparison.OrdinalIgnoreCase) || childRole == 24)
                            {
                                TestContext.WriteLine($"    *** 找到ButtonDropDownGrid，开始详细分析 ***");
                                AnalyzeButtonDropDownGrid(childAcc, provider);
                            }
                            else
                            {
                                // 递归查找更深层的ButtonDropDownGrid
                                FindAndExtractDropDownGrid(childAcc, provider);
                            }
                        }
                        else if (Marshal.IsComObject(children[i]))
                        {
                            TestContext.WriteLine($"  子对象[{i}]: COM对象，尝试转换...");
                            IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                            try
                            {
                                var convertedAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                if (convertedAcc != null)
                                {
                                    FindAndExtractDropDownGrid(convertedAcc, provider);
                                }
                            }
                            catch (Exception ex)
                            {
                                TestContext.WriteLine($"    COM对象转换失败: {ex.Message}");
                            }
                            finally
                            {
                                Marshal.Release(unk);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"查找ButtonDropDownGrid时发生错误: {ex.Message}");
            }
        }

        private void AnalyzeButtonDropDownGrid(IAccessible gridAcc, MARSAccessibleProvider provider)
        {
            try
            {
                TestContext.WriteLine("=== ButtonDropDownGrid 详细分析 ===");

                // 尝试转换为IAccessibleTable
                IAccessibleTable accessibleTable = MARSAccessibleProvider.TryConvertToAccessibleTable(gridAcc);
                if (accessibleTable != null)
                {
                    TestContext.WriteLine("✓ 成功转换为IAccessibleTable");
                    try
                    {
                        accessibleTable.get_nColumns(out long columnCount);
                        accessibleTable.get_nRows(out long rowCount);
                        TestContext.WriteLine($"  IAccessibleTable 报告: {columnCount} 列, {rowCount} 行");

                        // 尝试获取具体的单元格数据
                        if (columnCount > 0 && rowCount > 0)
                        {
                            TestContext.WriteLine("  尝试获取单元格数据:");
                            for (int row = 0; row < Math.Min(rowCount, 5); row++) // 限制显示前5行
                            {
                                for (int col = 0; col < Math.Min(columnCount, 5); col++) // 限制显示前5列
                                {
                                    try
                                    {
                                        accessibleTable.get_accessibleAt(row, col, out IAccessible cellAcc);
                                        if (cellAcc != null)
                                        {
                                            string cellText = "";
                                            try { cellText = cellAcc.get_accValue(0); } catch { }
                                            if (string.IsNullOrEmpty(cellText))
                                            {
                                                try { cellText = cellAcc.get_accName(0); } catch { }
                                            }
                                            TestContext.WriteLine($"    单元格[{row},{col}]: \"{cellText}\"");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        TestContext.WriteLine($"    单元格[{row},{col}]: 获取失败 - {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"  IAccessibleTable操作失败: {ex.Message}");
                    }
                }
                else
                {
                    TestContext.WriteLine("✗ 无法转换为IAccessibleTable");
                }

                // 使用传统方法分析
                TestContext.WriteLine("\n=== 使用传统方法分析 ===");
                int childCount = gridAcc.accChildCount;
                TestContext.WriteLine($"子对象总数: {childCount}");

                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = AccessibleChildren(gridAcc, 0, childCount, children, out int nObtained);
                    TestContext.WriteLine($"实际获取到的子对象数: {nObtained}");

                    List<string> columns = new List<string>();
                    List<List<string>> rows = new List<List<string>>();

                    for (int i = 0; i < nObtained; i++)
                    {
                        if (children[i] is IAccessible childAcc)
                        {
                            object childRoleObj = provider.GetRole(childAcc);
                            int childRole = (childRoleObj is int) ? (int)childRoleObj : Convert.ToInt32(childRoleObj);
                            string childRoleName = MARSAccessibleProvider.GetRoleName(childRole);
                            string childText = "";
                            try { childText = childAcc.get_accName(0); } catch { }
                            if (string.IsNullOrEmpty(childText))
                            {
                                try { childText = childAcc.get_accValue(0); } catch { }
                            }

                            TestContext.WriteLine($"  子对象[{i}]: {childRoleName} - \"{childText}\"");

                            // 如果是行对象
                            if (childRoleName.Equals("row", StringComparison.OrdinalIgnoreCase) || childRole == 0x1C)
                            {
                                TestContext.WriteLine($"    发现行对象，开始提取单元格...");
                                ExtractRowCellsDetailed(childAcc, provider, rows, columns, i);
                            }
                            // 如果是列头对象
                            else if (childRoleName.Equals("columnheader", StringComparison.OrdinalIgnoreCase) || childRole == 0x19)
                            {
                                if (!string.IsNullOrEmpty(childText) && !columns.Contains(childText))
                                {
                                    columns.Add(childText);
                                    TestContext.WriteLine($"    发现列头: \"{childText}\"");
                                }
                            }
                        }
                    }

                    // 输出提取的数据
                    TestContext.WriteLine($"\n=== 提取结果汇总 ===");
                    TestContext.WriteLine($"检测到的列数: {columns.Count}");
                    if (columns.Count > 0)
                    {
                        TestContext.WriteLine($"列名: {string.Join(", ", columns)}");
                    }

                    TestContext.WriteLine($"检测到的行数: {rows.Count}");
                    for (int i = 0; i < Math.Min(rows.Count, 10); i++) // 限制显示前10行
                    {
                        TestContext.WriteLine($"  行[{i}]: {string.Join(" | ", rows[i])}");
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"分析ButtonDropDownGrid时发生错误: {ex.Message}");
            }
        }

        private void ExtractRowCellsDetailed(IAccessible rowAcc, MARSAccessibleProvider provider, List<List<string>> rows, List<string> columns, int rowIndex)
        {
            try
            {
                int cellCount = rowAcc.accChildCount;
                TestContext.WriteLine($"    行[{rowIndex}]包含 {cellCount} 个单元格");

                List<string> rowCells = new List<string>();

                if (cellCount > 0)
                {
                    object[] cellChildren = new object[cellCount];
                    int cellObtained = AccessibleChildren(rowAcc, 0, cellCount, cellChildren, out int nCellObtained);
                    TestContext.WriteLine($"    实际获取到 {nCellObtained} 个单元格对象");

                    for (int j = 0; j < nCellObtained; j++)
                    {
                        if (cellChildren[j] is IAccessible cellAcc)
                        {
                            object cellRoleObj = provider.GetRole(cellAcc);
                            int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                            string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                            string cellText = "";
                            try { cellText = cellAcc.get_accValue(0); } catch { }
                            if (string.IsNullOrEmpty(cellText))
                            {
                                try { cellText = cellAcc.get_accName(0); } catch { }
                            }

                            TestContext.WriteLine($"      单元格[{j}]: {cellRoleName} - \"{cellText}\"");
                            rowCells.Add(cellText);

                            // 如果是列头，添加到列列表
                            if (cellRoleName.Equals("columnheader", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cellText) && !columns.Contains(cellText))
                            {
                                columns.Add(cellText);
                            }
                        }
                        else if (Marshal.IsComObject(cellChildren[j]))
                        {
                            TestContext.WriteLine($"      单元格[{j}]: COM对象，尝试转换...");
                            IntPtr unk = Marshal.GetIUnknownForObject(cellChildren[j]);
                            try
                            {
                                var convertedCellAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                if (convertedCellAcc != null)
                                {
                                    int cellRole = provider.GetRole(convertedCellAcc);
                                    string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                                    string cellText = "";
                                    try { cellText = convertedCellAcc.get_accValue(0); } catch { }
                                    if (string.IsNullOrEmpty(cellText))
                                    {
                                        try { cellText = convertedCellAcc.get_accName(0); } catch { }
                                    }

                                    TestContext.WriteLine($"        转换后: {cellRoleName} - \"{cellText}\"");
                                    rowCells.Add(cellText);

                                    // 如果是列头，添加到列列表
                                    if (cellRoleName.Equals("columnheader", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cellText) && !columns.Contains(cellText))
                                    {
                                        columns.Add(cellText);
                                    }
                                }
                                else
                                {
                                    TestContext.WriteLine($"        COM对象转换失败");
                                    rowCells.Add("");
                                }
                            }
                            catch (Exception ex)
                            {
                                TestContext.WriteLine($"        COM对象转换异常: {ex.Message}");
                                rowCells.Add("");
                            }
                            finally
                            {
                                Marshal.Release(unk);
                            }
                        }
                        else
                        {
                            TestContext.WriteLine($"      单元格[{j}]: 非IAccessible对象 - {cellChildren[j]?.GetType()?.Name ?? "null"}");
                            rowCells.Add("");
                        }
                    }
                }

                rows.Add(rowCells);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"    提取行[{rowIndex}]单元格时发生错误: {ex.Message}");
            }
        }

        public void PrintAllRowCells(IAccessible parent, MARSAccessibleProvider provider)
        {
            int childCount = parent.accChildCount;
            if (childCount <= 0) return;

            object[] children = new object[childCount];
            int obtained = AccessibleChildren(parent, 0, childCount, children, out int nObtained);

            for (int i = 0; i < nObtained; i++)
            {
                if (children[i] is IAccessible rowAcc)
                {
                    object rowRoleObj = provider.GetRole(rowAcc);
                    int rowRole = (rowRoleObj is int) ? (int)rowRoleObj : Convert.ToInt32(rowRoleObj);
                    if (rowRole == 0x1C) // ROLE_SYSTEM_ROW
                    {
                        Console.Write("Row: ");
                        int cellCount = rowAcc.accChildCount;
                        if (cellCount > 0)
                        {
                            object[] cellChildren = new object[cellCount];
                            int cellObtained = MARSAccessibleProvider.AccessibleChildren(rowAcc, 0, cellCount, cellChildren, out int nCellObtained);
                            for (int j = 0; j < nCellObtained; j++)
                            {
                                if (cellChildren[j] is IAccessible cellAcc)
                                {
                                    object cellRoleObj = provider.GetRole(cellAcc);
                                    int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                                    if (cellRole == 0x1D) // ROLE_SYSTEM_CELL
                                    {
                                        string cellText = null;
                                        try { cellText = cellAcc.get_accValue(0); } catch { }
                                        if (string.IsNullOrEmpty(cellText))
                                        {
                                            try { cellText = cellAcc.get_accName(0); } catch { }
                                        }
                                        Console.Write($"[{cellText}] ");
                                    }
                                }
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }
        }

        [TestMethod]
        public void TestObjectFromHwnds()
        {
            var root = MarsHwndAccBuilder.BuildTreeWithAcc(new IntPtr(0x004A0BE8));
            if (root != null)
            {
                HwndTreePrinter.PrintTreeToFile(root, $@"c:\temp\accNode_{DateTime.Now.ToString("yyyyMMddHHmmss")}", true);
            }
            else
            {
                Console.WriteLine("构建窗口树失败。");
            }
        }

        [TestMethod]
        public void Test_SearchAndClick()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
            }


            // 构造测试用的 B_V_OBJECT_SNAPSHOT
            var testSnapshot = new B_V_OBJECT_SNAPSHOT
            {
                OBJECT_NAME_ID = 123456,
                OBJECT_HAPPY_NAME = "SWAPS_LIST_TABLE",
                OBJECT_ID = 654321,
                OBJECT_TYPE = "winTable",
                PEG_QUICK_ACCESS = "winClass:=^Afx:\r\nTitle:=^Interest Rate Swaps",
                QUICK_ACCESS = "winClass:=CSCtrlGrille\r\nattachText:=DataGrid",
                TYPE_NAME ="winTable"
            };


            // 构造 MARSDealResult
            var dealResult = new MARSDealResult();

            // 其它参数
            string strPara = "MarsAddins;Reference;Action:LEFT_DBL_CLICK";
            string strSearchText = "Sanjay1";
            string strError = string.Empty;
            int stepIndex = 1;
            var appType = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP;
            string strDBIdx = "GEN_MARS_5";

            // 执行 SEARCHANDCLICK 关键字操作
            var result = KeywordOpForGUI.GUIKeyword["SEARCHANDCLICK"](
                stepIndex,
                strPara,
                strSearchText,
                testSnapshot,
                "",
                ref strError,
                ref dealResult,
                appType,
                strDBIdx,
                null // KeywordExecuteCallBack 可为 null 或自定义回调
            );

            // 断言结果
            Assert.IsTrue(result, $"SEARCHANDCLICK 执行失败: {strError}");
            Assert.IsNotNull(dealResult, "dealResult 不能为空");
            Console.WriteLine($"执行结果: {dealResult.ToString()}");
        }

        [TestMethod]
        public void Test_UIAWay_SearchAndClick()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
            }
            //using (var w = new StreamWriter($@"C:\temp\pid_uia_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt", false, Encoding.UTF8)) {
            //    MarsUIAHelper.DumpProcessUia(0,0,MARSTestProcess.CurrentTestProcessId,w);
            //}
        }

        [TestMethod]
        public void Test_PressKey()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
            }

            // 构造测试用的 B_V_OBJECT_SNAPSHOT
            var testSnapshot = new B_V_OBJECT_SNAPSHOT
            {
                OBJECT_NAME_ID = 123456,
                OBJECT_HAPPY_NAME = "SWAPS_LIST_TABLE",
                OBJECT_ID = 654321,
                OBJECT_TYPE = "winTable",
                PEG_QUICK_ACCESS = "winClass:=^Afx:\r\nTitle:=^Interest Rate Swaps",
                QUICK_ACCESS = "winClass:=CSCtrlGrille\r\nattachText:=DataGrid",
                TYPE_NAME = "winTable"
            };

            // 构造 MARSDealResult
            var dealResult = new MARSDealResult();

            // 其它参数 - 针对按键操作
            string strPara = "CURRENT_POS_CLI1CK;MarsAddins;Reference;Action:PRESSKEY";
            string strKeyText = "^N"; // 可以修改为其他按键，如 "ENTER", "TAB", "ESC" 等
            string strError = string.Empty;
            int stepIndex = 1;
            var appType = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP;
            string strDBIdx = "GEN_MARS_5";

            // 执行 PRESSKEY 关键字操作
            var result = KeywordOpForGUI.GUIKeyword["PRESSKEYS"](
                stepIndex,
                strPara,
                strKeyText,
                testSnapshot,
                "",
                ref strError,
                ref dealResult,
                appType,
                strDBIdx,
                null // KeywordExecuteCallBack 可为 null 或自定义回调
            );

            // 断言结果
            Assert.IsTrue(result, $"PRESSKEY 执行失败: {strError}");
            Assert.IsNotNull(dealResult, "dealResult 不能为空");
            Console.WriteLine($"执行结果: {dealResult.ToString()}");
        }

        [TestMethod]
        public void Test_AxeWinTree()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
                return;
            }

            try
            {
                // 使用Axe.Windows扫描界面对象
                var config = Config.Builder.ForProcessId(MARSTestProcess.CurrentTestProcessId).Build();
                var tree = A11yAutomation.ElementsFromProcessId(MARSTestProcess.CurrentTestProcessId, DesktopDataContext.DefaultContext);
                
                string fileName = $@"C:\temp\axe_windows_scan_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                Console.WriteLine($"开始扫描界面对象，结果将保存到: {fileName}");
                
                using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    writer.WriteLine($"=== Axe.Windows 界面对象扫描结果 ===");
                    writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"目标进程ID: {MARSTestProcess.CurrentTestProcessId}");
                    writer.WriteLine($"进程名称: sophisvalue.exe");
                    writer.WriteLine($"发现对象数量: {tree.ToList().Count}");
                    writer.WriteLine("=" + new string('=', 50));
                    writer.WriteLine();

                    int elementIndex = 0;
                    foreach (var element in tree)
                    {
                        elementIndex++;
                        writer.WriteLine($"--- 对象 #{elementIndex} ---");
                        writer.WriteLine($"类型: {element.GetType().Name}");
                        writer.WriteLine($"详细信息: {element}");
                        
                        // 尝试获取更多属性信息（基础信息，不在此方法统计子元素数量）
                        try
                        {
                            if (element is DesktopElement desktopElement)
                            {
                                writer.WriteLine($"自动化ID: {desktopElement.AutomationId}");
                                writer.WriteLine($"名称: {desktopElement.Name}");
                                writer.WriteLine($"控件类型: {desktopElement.LocalizedControlType}");
                                writer.WriteLine($"类名: {desktopElement.ClassName}");
                                writer.WriteLine($"是否启用: {desktopElement.IsEnabled}");
                                writer.WriteLine($"是否可见: {desktopElement.IsOffScreen}");
                                writer.WriteLine($"边界: {desktopElement.BoundingRectangle}");
                                writer.WriteLine($"子元素数量: (请使用 Test_AxeWinTreeDetailed 或 Test_AxeWinTreeByWindow 获取)");
                            }
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine($"获取详细属性时出错: {ex.Message}");
                        }
                        
                        writer.WriteLine();
                    }
                }
                
                Console.WriteLine($"扫描完成，共发现 {tree} 个界面对象");
                Console.WriteLine($"结果已保存到: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Axe.Windows扫描过程中出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        [TestMethod]
        public void Test_AxeWinTreeDetailed()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
                return;
            }

            try
            {
                // 使用Axe.Windows扫描界面对象
                var config = Config.Builder.ForProcessId(MARSTestProcess.CurrentTestProcessId).Build();
                var tree = A11yAutomation.ElementsFromProcessId(MARSTestProcess.CurrentTestProcessId, DesktopDataContext.DefaultContext);
                
                string fileName = $@"C:\temp\axe_windows_detailed_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                Console.WriteLine($"开始详细扫描界面对象，结果将保存到: {fileName}");
                
                using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    writer.WriteLine($"=== Axe.Windows 详细界面对象扫描结果 ===");
                    writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"目标进程ID: {MARSTestProcess.CurrentTestProcessId}");
                    writer.WriteLine($"进程名称: sophisvalue.exe");
                    writer.WriteLine("=" + new string('=', 60));
                    writer.WriteLine();

                    int totalElements = 0;
                    foreach (var rootElement in tree)
                    {
                        totalElements += WriteElementDetails(writer, rootElement, 0, ref totalElements);
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine($"=== 扫描总结 ===");
                    writer.WriteLine($"总共发现 {totalElements} 个界面对象");
                }
                
                Console.WriteLine($"详细扫描完成，结果已保存到: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Axe.Windows详细扫描过程中出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        private int WriteElementDetails(StreamWriter writer, DesktopElement element, int indentLevel, ref int elementCount)
        {
            int count = 1;
            string indent = new string(' ', indentLevel * 2);
            
            try
            {
                elementCount++;
                writer.WriteLine($"{indent}--- 对象 #{elementCount} (层级: {indentLevel}) ---");
                writer.WriteLine($"{indent}类型: {element.GetType().Name}");
                writer.WriteLine($"{indent}自动化ID: {element.AutomationId ?? "无"}");
                writer.WriteLine($"{indent}名称: {element.Name ?? "无"}");
                writer.WriteLine($"{indent}控件类型: {element.LocalizedControlType ?? "无"}");
                writer.WriteLine($"{indent}类名: {element.ClassName ?? "无"}");
                writer.WriteLine($"{indent}是否启用: {element.IsEnabled}");
                writer.WriteLine($"{indent}是否可见: {!element.IsOffScreen}");
                writer.WriteLine($"{indent}边界: {element.BoundingRectangle}");
                
                // 获取更多属性
                try
                {
                    var properties = element.Properties;
                    if (properties != null && properties.Any())
                    {
                        writer.WriteLine($"{indent}其他属性:");
                        foreach (var prop in properties)
                        {
                            writer.WriteLine($"{indent}  {prop.Key}: {prop.Value}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"{indent}获取属性时出错: {ex.Message}");
                }
                
                // 递归处理子元素
                var children = element.Children;
                if (children != null && children.Any())
                {
                    writer.WriteLine($"{indent}子元素数量: {children.Count()}");
                    writer.WriteLine($"{indent}子元素列表:");
                    
                    foreach (var child in children)
                    {
                        if (child is DesktopElement childElement)
                        {
                            count += WriteElementDetails(writer, childElement, indentLevel + 1, ref elementCount);
                        }
                    }
                }
                else
                {
                    writer.WriteLine($"{indent}无子元素");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}处理元素时出错: {ex.Message}");
            }
            
            writer.WriteLine();
            return count;
        }

        [TestMethod]
        public void Test_AxeWinTreeSimple()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
                return;
            }

            try
            {
                // 使用Axe.Windows扫描界面对象
                var tree = A11yAutomation.ElementsFromProcessId(MARSTestProcess.CurrentTestProcessId, DesktopDataContext.DefaultContext);
                
                string fileName = $@"C:\temp\axe_windows_simple_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                Console.WriteLine($"开始简单扫描界面对象，结果将保存到: {fileName}");
                
                using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    writer.WriteLine($"=== Axe.Windows 简单界面对象扫描 ===");
                    writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"目标进程ID: {MARSTestProcess.CurrentTestProcessId}");
                    writer.WriteLine($"发现对象数量: {tree.Count()}");
                    writer.WriteLine("=" + new string('=', 40));
                    writer.WriteLine();

                    int index = 0;
                    foreach (var element in tree)
                    {
                        index++;
                        writer.WriteLine($"{index:D3}. {element}");
                    }
                }
                
                Console.WriteLine($"简单扫描完成，共发现 {tree.Count()} 个界面对象");
                Console.WriteLine($"结果已保存到: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Axe.Windows简单扫描过程中出错: {ex.Message}");
            }
        }

        [TestMethod]
        public void Test_AxeWinTreeByWindow()
        {
            //Console.WriteLine("请输入窗口句柄 (hwnd)：");
            //string hwndStr = Console.ReadLine();
            long hwndLong = 0x000000000046168E;
            //if (!long.TryParse(hwndStr, out hwndLong))
            //{
            //    Console.WriteLine("输入无效。");
            //    return;
            //}

            IntPtr hwnd = new IntPtr(hwndLong);
            
            try
            {
                // 在STA线程中执行UIAutomation遍历，避免MTA导致的子树不可见问题
                System.Threading.Thread staThread = new System.Threading.Thread(() =>
                {
                    var automationElement = System.Windows.Automation.AutomationElement.FromHandle(hwnd);
                    if (automationElement == null)
                    {
                        Console.WriteLine("无法获取指定窗口的AutomationElement。");
                        return;
                    }

                    string fileName = $@"C:\temp\axe_windows_window_{hwndLong}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                    Console.WriteLine($"开始扫描窗口 {hwndLong} 的界面对象，结果将保存到: {fileName}");
                    
                    using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                    {
                        writer.WriteLine($"=== Axe.Windows 窗口界面对象扫描(UIAutomation) ===");
                        writer.WriteLine($"扫描时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine($"目标窗口句柄: {hwndLong} (0x{hwndLong:X})");
                        writer.WriteLine("=" + new string('=', 50));
                        writer.WriteLine();

                        int count = 0;
                        WriteAutomationElementDetails(writer, automationElement, 0, ref count);
                        writer.WriteLine();
                        writer.WriteLine($"总计元素: {count}");
                    }
                    
                    Console.WriteLine($"窗口扫描完成，结果已保存到: {fileName}");
                });
                staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Axe.Windows窗口扫描过程中出错: {ex.Message}");
            }
        }

        private void WriteAutomationElementDetails(StreamWriter writer, System.Windows.Automation.AutomationElement element, int indentLevel, ref int elementCount)
        {
            string indent = new string(' ', indentLevel * 2);
            try
            {
                elementCount++;
                var name = element.Current.Name;
                var automationId = element.Current.AutomationId;
                var className = element.Current.ClassName;
                var controlType = element.Current.ControlType?.ProgrammaticName;
                var boundingRect = element.Current.BoundingRectangle;
                bool isOffscreen = element.Current.IsOffscreen;

                writer.WriteLine($"{indent}--- 元素 #{elementCount} (层级: {indentLevel}) ---");
                writer.WriteLine($"{indent}名称: {name}");
                writer.WriteLine($"{indent}自动化ID: {automationId}");
                writer.WriteLine($"{indent}控件类型: {controlType}");
                writer.WriteLine($"{indent}类名: {className}");
                writer.WriteLine($"{indent}是否可见: {!isOffscreen}");
                writer.WriteLine($"{indent}边界: {boundingRect}");

                // Grid/Table/List/Tree 等容器，优先尝试按“行”输出
                if (IsGridLike(element))
                {
                    var rows = CollectGridRows(element, maxPages: 10);
                    if (rows.Count > 0)
                    {
                        writer.WriteLine($"{indent}检测为Grid/Table/List/Tree，行数(去重后): {rows.Count}");
                        int rowIndex = 0;
                        foreach (var row in rows)
                        {
                            rowIndex++;
                            // 虚拟化支持：尝试Realize
                            TryRealize(row);
                            WriteAutomationElementDetails(writer, row, indentLevel + 1, ref elementCount);
                            // 防止日志过大，默认最多写入前200行
                            if (rowIndex >= 200) { writer.WriteLine($"{indent}... 已截断输出，仅显示前200行"); break; }
                        }
                        return; // 已输出行信息，避免重复遍历通用子节点
                    }
                }

                // 遍历子元素：优先ControlView，其次RawView，最后FindAll
                var children = GetChildrenByAllStrategies(element);
                if (children.Count == 0)
                {
                    writer.WriteLine($"{indent}无子元素");
                }
                else
                {
                    writer.WriteLine($"{indent}子元素数量: {children.Count}");
                    writer.WriteLine($"{indent}子元素列表:");
                    foreach (var child in children)
                    {
                        WriteAutomationElementDetails(writer, child, indentLevel + 1, ref elementCount);
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{indent}处理AutomationElement时出错: {ex.Message}");
            }
        }

        private List<System.Windows.Automation.AutomationElement> GetChildrenByAllStrategies(System.Windows.Automation.AutomationElement parent)
        {
            var result = new List<System.Windows.Automation.AutomationElement>();

            void AddUnique(System.Windows.Automation.AutomationElement el)
            {
                if (el == null) return;
                // 避免重复
                if (!result.Contains(el)) result.Add(el);
            }

            // 1) ControlViewWalker
            try
            {
                var walker = System.Windows.Automation.TreeWalker.ControlViewWalker;
                var child = walker.GetFirstChild(parent);
                while (child != null)
                {
                    AddUnique(child);
                    child = walker.GetNextSibling(child);
                }
            }
            catch { }

            // 2) RawViewWalker（有些控件只在Raw视图显示）
            try
            {
                var walker = System.Windows.Automation.TreeWalker.RawViewWalker;
                var child = walker.GetFirstChild(parent);
                while (child != null)
                {
                    AddUnique(child);
                    child = walker.GetNextSibling(child);
                }
            }
            catch { }

            // 3) FindAll(TreeScope.Children)
            try
            {
                var collection = parent.FindAll(System.Windows.Automation.TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                for (int i = 0; i < collection.Count; i++)
                {
                    AddUnique(collection[i]);
                }
            }
            catch { }

            return result;
        }

        private bool IsGridLike(System.Windows.Automation.AutomationElement element)
        {
            try
            {
                var ct = element.Current.ControlType;
                if (ct == System.Windows.Automation.ControlType.Table ||
                    ct == System.Windows.Automation.ControlType.DataGrid ||
                    ct == System.Windows.Automation.ControlType.List ||
                    ct == System.Windows.Automation.ControlType.Tree)
                {
                    return true;
                }

                // 没有明确类型时，若支持GridPattern/TablePattern也作为Grid处理
                return element.TryGetCurrentPattern(System.Windows.Automation.GridPattern.Pattern, out _) ||
                       element.TryGetCurrentPattern(System.Windows.Automation.TablePattern.Pattern, out _);
            }
            catch { return false; }
        }

        private List<System.Windows.Automation.AutomationElement> CollectGridRows(System.Windows.Automation.AutomationElement grid, int maxPages)
        {
            var rows = new List<System.Windows.Automation.AutomationElement>();

            void AddUniqueByRuntimeId(System.Windows.Automation.AutomationElement el)
            {
                if (el == null) return;
                var id = SafeGetRuntimeId(el);
                if (!rows.Any(r => SequenceEqual(SafeGetRuntimeId(r), id)))
                {
                    rows.Add(el);
                }
            }

            // 单页收集（ControlView/RawView/FindAll）
            foreach (var row in FindRowsOnce(grid)) AddUniqueByRuntimeId(row);

            // 尝试滚动多页以加载更多虚拟化行
            if (TryGetScrollPattern(grid, out var scroll) && (scroll.Current.VerticallyScrollable || scroll.Current.HorizontallyScrollable))
            {
                for (int i = 0; i < maxPages; i++)
                {
                    try
                    {
                        if (scroll.Current.VerticallyScrollable) scroll.ScrollVertical(System.Windows.Automation.ScrollAmount.LargeIncrement);
                        System.Threading.Thread.Sleep(50);
                        foreach (var row in FindRowsOnce(grid)) AddUniqueByRuntimeId(row);
                    }
                    catch { break; }
                }
                // 复位到顶部（尽力而为）
                try { if (scroll.Current.VerticallyScrollable) scroll.SetScrollPercent(scroll.Current.HorizontalScrollPercent, 0); } catch { }
            }

            return rows;
        }

        private IEnumerable<System.Windows.Automation.AutomationElement> FindRowsOnce(System.Windows.Automation.AutomationElement grid)
        {
            var result = new List<System.Windows.Automation.AutomationElement>();
            try
            {
                // 常见行类型：DataItem、ListItem、TreeItem
                var rowTypes = new System.Windows.Automation.ControlType[]
                {
                    System.Windows.Automation.ControlType.DataItem,
                    System.Windows.Automation.ControlType.ListItem,
                    System.Windows.Automation.ControlType.TreeItem
                };

                var conds = rowTypes.Select(t => new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, t)).ToArray();
                var orCond = conds.Length == 1 ? (System.Windows.Automation.Condition)conds[0] : new System.Windows.Automation.OrCondition(conds);

                // Raw视图下的Descendants更容易找到虚拟化容器下的行
                var rows = grid.FindAll(System.Windows.Automation.TreeScope.Descendants, orCond);
                for (int i = 0; i < rows.Count; i++) result.Add(rows[i]);
            }
            catch { }
            return result;
        }

        private void TryRealize(System.Windows.Automation.AutomationElement element)
        {
            try
            {
                if (element.TryGetCurrentPattern(System.Windows.Automation.VirtualizedItemPattern.Pattern, out var p))
                {
                    ((System.Windows.Automation.VirtualizedItemPattern)p).Realize();
                }
            }
            catch { }
        }

        private bool TryGetScrollPattern(System.Windows.Automation.AutomationElement element, out System.Windows.Automation.ScrollPattern pattern)
        {
            pattern = null;
            try
            {
                if (element.TryGetCurrentPattern(System.Windows.Automation.ScrollPattern.Pattern, out var p))
                {
                    pattern = (System.Windows.Automation.ScrollPattern)p;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private int[] SafeGetRuntimeId(System.Windows.Automation.AutomationElement element)
        {
            try { return element.GetRuntimeId(); } catch { return Array.Empty<int>(); }
        }

        private bool SequenceEqual(int[] a, int[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        #region chatgpt
        [TestMethod]
        public void Test_classLegacy()
        {
            Console.WriteLine("选择数据来源：1=FromPoint(鼠标), 2=Focus, 3=FromHandle(hex)");
            Console.Write("> ");
            //var key = Console.ReadLine();
            StaRunner.Run(() => {
                ToGetLegacy();
            });
        }

        private void ToGetLegacy()
        {
            string key = "1";

            AutomationElement elem = null;
            StreamWriter sw = new StreamWriter($@"c:\temp\legacy_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.txt");
            try
            {
               
                Console.SetOut(sw);
                
                if (key == "1")
                {
                    // 鼠标当前位置（注意坐标在 Console 下是屏幕坐标）737,227
                    //System.Drawing.Point p = System.Windows.Forms.Control.MousePosition;
                    //elem = AutomationElement.FromPoint(new System.Windows.Point(p.X, p.Y));
                    elem = AutomationElement.FromPoint(new System.Windows.Point(737,227));
                }
                else if (key == "2")
                {
                    elem = AutomationElement.FocusedElement;
                }
                else if (key == "3")
                {
                    sw.WriteLine("HWND(hex, e.g. 0003059A): ");
                    //string s = Console.ReadLine();
                    IntPtr s = new IntPtr(0x009D0C1A) ;
                    elem = AutomationElement.FromHandle(s);
                    //if (IntPtr.Size == 8)
                    //    elem = AutomationElement.FromHandle((IntPtr)Convert.ToInt64(s, 16));
                    //else
                    //    elem = AutomationElement.FromHandle((IntPtr)Convert.ToInt32(s, 16));
                }
                else
                {
                    sw.WriteLine("无效选择");
                    return;
                }
            }
            catch (Exception ex)
            {
                sw.WriteLine("获取元素失败: " + ex.Message);
                return;
            }

            if (elem == null)
            {
                Console.WriteLine("未拿到元素。");
                return;
            }

            // 可选：启用 CacheRequest，减少跨进程调用
            var cr = new CacheRequest();
            cr.Add(AutomationElement.NameProperty);
            cr.Add(AutomationElement.AutomationIdProperty);
            cr.Add(AutomationElement.ClassNameProperty);
            cr.Add(AutomationElement.ControlTypeProperty);
            cr.Add(AutomationElement.FrameworkIdProperty);
            cr.TreeScope = TreeScope.Subtree;// TreeScope.Element; // 只缓存当前元素属性

            using (cr.Activate())
            {
                var eCached = elem.GetUpdatedCache(cr);

                // 打印基础信息
                var ct = eCached.Current.ControlType?.ProgrammaticName ?? "ControlType.?";
                sw.WriteLine($"Element: {ct}, Name='{eCached.Current.Name}', AutoId='{eCached.Current.AutomationId}', Class='{eCached.Current.ClassName}', Fwk='{eCached.Current.FrameworkId}'");

                // 打印 Legacy 快照（即使拿不到 Pattern 也能读）
                bool hasLegacy = UiaLegacy.HasLegacy(eCached);
                sw.WriteLine("Has LegacyIAccessible: " + hasLegacy);

                if (hasLegacy)
                {
                    var snap = UiaLegacy.ReadLegacy(eCached);
                    sw.WriteLine(snap.ToString());

                    // 如果托管 UIA 支持 Pattern，可尝试执行动作
                    if (UiaLegacy.DoDefaultAction(eCached))
                        sw.WriteLine("DoDefaultAction() 已尝试执行。");

                    // 例如：设置值（可能仅对可编辑文本生效）
                    // bool ok = UiaLegacy.SetValue(eCached, "Hello");
                    // Console.WriteLine("SetValue: " + ok);
                }
            }

            sw.WriteLine("\n按任意键退出…");
            sw.Flush();
            //Console.ReadKey(true);
        }

        [TestMethod]
        public void Test_HybridAfx()
        {
            StaRunner.Run(
                () =>{
                    // tab
                    //Test_hybridAfx.Test(0x001E0C3A);
                    // ribbon 
                    //Test_hybridAfx.Test(0x00281CD2);
                    IntPtr hwndRibbon = new IntPtr(0x00042316);
                    var items = RibbonChildResolver.EnumRibbonChildrenSmart(hwndRibbon, includeOffscreen: true);
                    RibbonChildResolver.SaveToFileSimple(items, $@"c:\temp\ribbon_children_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.txt");
                    //foreach (var it in items)
                    //    Console.WriteLine(it);
                }
             );
        }
        #endregion

    }

    public static class StaRunner
    {
        public static void Run(Action action)
        {
            Exception ex = null;
            var done = new ManualResetEvent(false);
            var t = new Thread(() =>
            {
                try { action(); }
                catch (Exception e) { ex = e; }
                finally { done.Set(); }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            done.WaitOne();
            if (ex != null) throw new AssertFailedException("STA run failed", ex);
        }
    }
}

