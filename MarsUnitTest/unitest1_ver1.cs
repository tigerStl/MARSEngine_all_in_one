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

namespace MarsUnitTest
{
    // Accessible2 接口定义
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("E89F726E-C4F4-4C19-BB19-B647D7FA8478")]
    public interface IAccessible2
    {
        // IAccessible 基础方法
        void get_accParent([Out, MarshalAs(UnmanagedType.Interface)] out object ppdispParent);
        void get_accChildCount(out long pcountChildren);
        void get_accChild([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Interface)] out object ppdispChild);
        void get_accName([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void get_accValue([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszValue);
        void get_accDescription([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszDescription);
        void get_accRole([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Struct)] out object pvarRole);
        void get_accState([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Struct)] out object pvarState);
        void get_accHelp([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszHelp);
        void get_accHelpTopic([Out, MarshalAs(UnmanagedType.LPWStr)] string pszHelpFile, [In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out] out long pidTopic);
        void get_accKeyboardShortcut([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszKeyboardShortcut);
        void get_accFocus([Out, MarshalAs(UnmanagedType.Struct)] out object pvarChild);
        void get_accSelection([Out, MarshalAs(UnmanagedType.Struct)] out object pvarChildren);
        void get_accDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszDefaultAction);
        void accSelect(long flagsSelect, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void accLocation([Out] out long pxLeft, [Out] out long pyTop, [Out] out long pcxWidth, [Out] out long pcyHeight, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void accNavigate(long navDir, [In, MarshalAs(UnmanagedType.Struct)] object varStart, [Out, MarshalAs(UnmanagedType.Interface)] out object pvarEndUpAt);
        void accHitTest(long xLeft, long yTop, [Out, MarshalAs(UnmanagedType.Interface)] out object pvarChild);
        void accDoDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void put_accName([In, MarshalAs(UnmanagedType.Struct)] object varChild, [In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void put_accValue([In, MarshalAs(UnmanagedType.Struct)] object varChild, [In, MarshalAs(UnmanagedType.LPWStr)] string pszValue);
        
        // Accessible2 特有方法
        void get_nRelations(out long nRelations);
        void get_relation(long relationIndex, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible2 ppTarget);
        void get_relations(long maxRelations, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAccessible2[] ppTargets, out long nRelations);
        void role(out long role);
        void get_locale(out string locale);
        void get_attributes(out string attributes);
        void get_groupPosition(out long groupLevel, out long similarItemsInGroup, out long positionInGroup);
        void get_states(out long states);
        void get_extendedRole(out string role);
        void get_localizedExtendedRole(out string localizedRole);
        void get_nExtendedStates(out long nExtendedStates);
        void get_extendedStates(long maxExtendedStates, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] string[] extendedStates, out long nExtendedStates);
        void get_localizedExtendedStates(long maxLocalizedExtendedStates, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] string[] localizedExtendedStates, out long nLocalizedExtendedStates);
        void get_uniqueID(out long uniqueID);
        void get_windowHandle(out IntPtr windowHandle);
        void get_indexInParent(out long indexInParent);
        void get_relationTargetsOfType(string type, long maxTargets, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IAccessible2[] targets, out long nTargets);
        void get_selections(out IAccessible2 selections);
        void scrollTo(IAccessible2 scrollType);
        void scrollToPoint(IAccessible2 coordinateType, long x, long y);
    }

    // IAccessibleTable 接口定义
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("35AD8075-C20C-4fb4-B094-F4F7275DD469")]
    public interface IAccessibleTable
    {
        void get_accessibleAt(long row, long column, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_caption([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_columnDescription(long column, [Out, MarshalAs(UnmanagedType.LPWStr)] out string description);
        void get_columnExtentAt(long row, long column, out long nColumnsSpanned);
        void get_columnHeader([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long startingRowIndex);
        void get_columnIndex(long cellIndex, out long columnIndex);
        void get_nColumns(out long columnCount);
        void get_nRows(out long rowCount);
        void get_nSelectedCells(out long cellCount);
        void get_nSelectedChildren(out long childCount);
        void get_rowDescription(long row, [Out, MarshalAs(UnmanagedType.LPWStr)] out string description);
        void get_rowExtentAt(long row, long column, out long nRowsSpanned);
        void get_rowHeader([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long startingColumnIndex);
        void get_rowIndex(long cellIndex, out long rowIndex);
        void get_selectedCells([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long nSelectedCells);
        void get_selectedChildren(long maxChildren, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAccessible[] children, out long nChildren);
        void get_summary([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_isColumnSelected(long column, out bool isSelected);
        void get_isRowSelected(long row, out bool isSelected);
        void get_isSelected(long row, long column, out bool isSelected);
        void selectRow(long row);
        void selectColumn(long column);
        void unselectRow(long row);
        void unselectColumn(long column);
        void get_modelChange([Out, MarshalAs(UnmanagedType.Struct)] out object modelChange);
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var pwd = Mars.message.Securities.MarsEncodePwd.EncodeString("admin");// marsEncodePwd = new Mars.message.Securities.MarsEncodePwd();
            Console.WriteLine($"Encoded pwd: {pwd}");

        }

        // 尝试将 IAccessible 转换为 IAccessible2
        private IAccessible2 TryConvertToAccessible2(IAccessible accessible)
        {
            try
            {
                if (accessible == null) return null;
                
                // 获取 IUnknown 指针
                IntPtr unk = Marshal.GetIUnknownForObject(accessible);
                try
                {
                    // 尝试查询 IAccessible2 接口
                    Guid iid = typeof(IAccessible2).GUID;
                    int hr = Marshal.QueryInterface(unk, ref iid, out IntPtr accessible2Ptr);
                    if (hr == 0 && accessible2Ptr != IntPtr.Zero)
                    {
                        return Marshal.GetObjectForIUnknown(accessible2Ptr) as IAccessible2;
                    }
                }
                finally
                {
                    Marshal.Release(unk);
                }
            }
            catch (Exception ex)
            {
                // 转换失败，返回 null
                System.Diagnostics.Debug.WriteLine($"转换为 Accessible2 失败: {ex.Message}");
            }
            return null;
        }

        // 尝试将 IAccessible 转换为 IAccessibleTable
        private IAccessibleTable TryConvertToAccessibleTable(IAccessible accessible)
        {
            try
            {
                if (accessible == null) return null;
                
                // 获取 IUnknown 指针
                IntPtr unk = Marshal.GetIUnknownForObject(accessible);
                try
                {
                    // 尝试查询 IAccessibleTable 接口
                    Guid iid = typeof(IAccessibleTable).GUID;
                    int hr = Marshal.QueryInterface(unk, ref iid, out IntPtr tablePtr);
                    if (hr == 0 && tablePtr != IntPtr.Zero)
                    {
                        return Marshal.GetObjectForIUnknown(tablePtr) as IAccessibleTable;
                    }
                }
                finally
                {
                    Marshal.Release(unk);
                }
            }
            catch (Exception ex)
            {
                // 转换失败，返回 null
                System.Diagnostics.Debug.WriteLine($"转换为 IAccessibleTable 失败: {ex.Message}");
            }
            return null;
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
        // MSAA AccessibleChildren API
        //[DllImport("oleacc.dll")]
        //private static extern int AccessibleChildren(
        //    object paccContainer,
        //    int iChildStart,
        //    int cChildren,
        //    [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
        //    out int pcObtained);


        private void TraverseAccessibleTree(IAccessible acc, MARSAccessibleProvider provider, StringBuilder sb, int indent, IntPtr hwnd)
        {
            if (acc == null) return;

            string indentStr = new string(' ', indent * 2);
            object roleObj = provider.GetRole(acc);
            int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
            string roleName = MARSAccessibleProvider.GetRoleName(role);
            string text = null;
            try { text = acc.get_accName(0); } catch { }
            if (string.IsNullOrEmpty(text))
            {
                try { text = acc.get_accValue(0); } catch { }
            }
            string id = acc.GetHashCode().ToString();
            int childCount = acc.accChildCount;
            sb.AppendLine($"{indentStr}- [{roleName}] \"{text}\" (ID:{id})");

            // If ROLE_SYSTEM_BUTTONDROPDOWNGRID, get columns and detailed info
            if (role == 24) // ROLE_SYSTEM_BUTTONDROPDOWNGRID
            {
                sb.AppendLine($"{indentStr}  *** ButtonDropDownGrid 详细信息 ***");
                List<string> columns = new List<string>();
                List<string> rowCounts = new List<string>();
                
                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = AccessibleChildren(acc, 0, childCount, children, out int nObtained);
                    sb.AppendLine($"{indentStr}  子对象总数: {childCount}, 实际获取: {nObtained}");
                    
                    // 在ButtonDropDownGrid上绘制红色闪烁框，闪烁3次

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
                            
                            sb.AppendLine($"{indentStr}    子对象[{i}]: {childRoleName} - \"{childText}\"");
                            
                            // 如果是列头
                            if (childRole == 0x19) // ROLE_SYSTEM_COLUMNHEADER
                            {
                                if (!string.IsNullOrEmpty(childText))
                                    columns.Add(childText);
                            }
                            // 如果是行
                            else if (childRole == 0x1C) // ROLE_SYSTEM_ROW
                            {
                                rowCounts.Add($"行{i}|ROLE_SYSTEM_ROW");
                                sb.AppendLine($"{indentStr}      行对象，包含 {childAcc.accChildCount} 个单元格");
                                
                                // 尝试获取行的单元格详细信息
                                if (childAcc.accChildCount > 0)
                                {
                                    object[] rowChildren = new object[childAcc.accChildCount];
                                    int rowObtained = AccessibleChildren(childAcc, 0, childAcc.accChildCount, rowChildren, out int nRowObtained);
                                    sb.AppendLine($"{indentStr}      行包含 {childAcc.accChildCount} 个子对象，实际获取到 {nRowObtained} 个");
                                    
                                    for (int j = 0; j < nRowObtained; j++)
                                    {
                                        // 记录子对象的类型和值
                                        string childType = rowChildren[j]?.GetType()?.Name ?? "null";
                                        string childValue = rowChildren[j]?.ToString() ?? "null";
                                        sb.AppendLine($"{indentStr}        子对象[{j}]: 类型={childType}, 值={childValue}");
                                        
                                        if (rowChildren[j] is IAccessible cellAcc)
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
                                            string locationInfo = GetLocationInfo(cellAcc);
                                            sb.AppendLine($"{indentStr}          IAccessible单元格: {cellRoleName} - \"{cellText}\"|位置|{locationInfo}");
                                        }
                                        else if (rowChildren[j] is int childIndex)
                                        {
                                            sb.AppendLine($"{indentStr}          整数索引: {childIndex}，尝试多种方法获取单元格...");
                                            
                                            // 方法1: 通过索引获取子对象
                                            try
                                            {
                                                object childByIndex = childAcc.get_accChild(childIndex);
                                                if (childByIndex != null)
                                                {
                                                    if (childByIndex is IAccessible indexCellAcc)
                                                    {
                                                        object cellRoleObj = provider.GetRole(indexCellAcc);
                                                        int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                                                        string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                                                        string cellText = "";
                                                        try { cellText = indexCellAcc.get_accValue(0); } catch { }
                                                        if (string.IsNullOrEmpty(cellText))
                                                        {
                                                            try { cellText = indexCellAcc.get_accName(0); } catch { }
                                                        }
                                                        string locationInfo1 = GetLocationInfo(indexCellAcc);
                                                        sb.AppendLine($"{indentStr}            方法1-通过索引获取: {cellRoleName} - \"{cellText}\"|位置|{locationInfo1}");
                                                        

                                                    }
                                                    else
                                                    {
                                                        sb.AppendLine($"{indentStr}            方法1-通过索引获取的对象不是IAccessible: {childByIndex?.GetType()?.Name ?? "null"}");
                                                    }
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"{indentStr}            方法1-通过索引获取返回null");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                sb.AppendLine($"{indentStr}            方法1-通过索引获取失败: {ex.Message}");
                                            }
                                            
                                            // 方法2: 尝试直接通过父对象获取
                                            try
                                            {
                                                // 获取父对象（ButtonDropDownGrid）
                                                IAccessible parentGrid = childAcc.accParent as IAccessible;
                                                if (parentGrid != null)
                                                {
                                                    // 尝试转换为IAccessibleTable
                                                    IAccessibleTable table = TryConvertToAccessibleTable(parentGrid);
                                                    if (table != null)
                                                    {
                                                        sb.AppendLine($"{indentStr}            方法2-找到IAccessibleTable，尝试获取单元格...");
                                                        
                                                        // 假设这是第i行，第j列
                                                        try
                                                        {
                                                            table.get_accessibleAt(i, childIndex, out IAccessible tableCell);
                                                            if (tableCell != null)
                                                            {
                                                                object cellRoleObj = provider.GetRole(tableCell);
                                                                int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                                                                string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                                                                string cellText = "";
                                                                try { cellText = tableCell.get_accValue(0); } catch { }
                                                                if (string.IsNullOrEmpty(cellText))
                                                                {
                                                                    try { cellText = tableCell.get_accName(0); } catch { }
                                                                }
                                                                string locationInfo2 = GetLocationInfo(tableCell);
                                                                sb.AppendLine($"{indentStr}              方法2-通过IAccessibleTable获取: {cellRoleName} - \"{cellText}\"|位置|{locationInfo2}");
                                                            }
                                                            else
                                                            {
                                                                sb.AppendLine($"{indentStr}              方法2-IAccessibleTable.get_accessibleAt返回null");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            sb.AppendLine($"{indentStr}              方法2-IAccessibleTable.get_accessibleAt失败: {ex.Message}");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sb.AppendLine($"{indentStr}            方法2-父对象无法转换为IAccessibleTable");
                                                    }
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"{indentStr}            方法2-无法获取父对象");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                sb.AppendLine($"{indentStr}            方法2-获取父对象失败: {ex.Message}");
                                            }
                                            
                                            // 方法3: 尝试通过索引作为简单元素处理
                                            try
                                            {
                                                // 尝试将索引作为简单元素处理
                                                string simpleText = "";
                                                string txt = "";
                                                try 
                                                { 
                                                    simpleText = childAcc.get_accName(childIndex);
                                                    txt = childAcc.get_accValue(childIndex);
                                                } 
                                                catch { }
                                                if (string.IsNullOrEmpty(simpleText))
                                                {
                                                    try 
                                                    { 
                                                        simpleText = childAcc.get_accValue(childIndex); 
                                                    } 
                                                    catch { }
                                                }
                                                if (!string.IsNullOrEmpty(simpleText))
                                                {
                                                    // 获取屏幕位置信息
                                                    string locationInfo = GetLocationInfo(childAcc, childIndex);
                                                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取: \"{simpleText}\"|Txt|{txt}|位置|{locationInfo}");
                                                    sb.AppendLine($"{indentStr}  开始绘制红色闪烁框...");
                                                    FlashRedBorder(childAcc, null, 3);
                                                    sb.AppendLine($"{indentStr}  红色闪烁框绘制完成");
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取失败");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                sb.AppendLine($"{indentStr}            方法3-作为简单元素获取异常: {ex.Message}");
                                            }
                                            
                                            // 方法4: 记录索引信息，可能用于后续分析
                                            sb.AppendLine($"{indentStr}            方法4-索引信息: 行索引={i}, 单元格索引={childIndex}");
                                        }
                                        else if (Marshal.IsComObject(rowChildren[j]))
                                        {
                                            sb.AppendLine($"{indentStr}          COM对象，尝试转换...");
                                            IntPtr unk = Marshal.GetIUnknownForObject(rowChildren[j]);
                                            try
                                            {
                                                var convertedCellAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                                if (convertedCellAcc != null)
                                                {
                                                    object cellRoleObj = provider.GetRole(convertedCellAcc);
                                                    int cellRole = (cellRoleObj is int) ? (int)cellRoleObj : Convert.ToInt32(cellRoleObj);
                                                    string cellRoleName = MARSAccessibleProvider.GetRoleName(cellRole);
                                                    string cellText = "";
                                                    try { cellText = convertedCellAcc.get_accValue(0); } catch { }
                                                    if (string.IsNullOrEmpty(cellText))
                                                    {
                                                        try { cellText = convertedCellAcc.get_accName(0); } catch { }
                                                    }
                                                    string locationInfo3 = GetLocationInfo(convertedCellAcc);
                                                    sb.AppendLine($"{indentStr}            转换后: {cellRoleName} - \"{cellText}\"|位置|{locationInfo3}");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                sb.AppendLine($"{indentStr}            COM对象转换失败: {ex.Message}");
                                            }
                                            finally
                                            {
                                                Marshal.Release(unk);
                                            }
                                        }
                                        else
                                        {
                                            sb.AppendLine($"{indentStr}          其他类型对象: {childType} = {childValue}");
                                        }
                                    }
                                }
                            }
                        }
                        else if (Marshal.IsComObject(children[i]))
                        {
                            sb.AppendLine($"{indentStr}    子对象[{i}]: COM对象，尝试转换...");
                            IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                            try
                            {
                                var convertedAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                if (convertedAcc != null)
                                {
                            object convertedRoleObj = provider.GetRole(convertedAcc);
                            int convertedRole = (convertedRoleObj is int) ? (int)convertedRoleObj : Convert.ToInt32(convertedRoleObj);
                            string convertedRoleName = MARSAccessibleProvider.GetRoleName(convertedRole);
                                    sb.AppendLine($"{indentStr}      转换后类型: {convertedRoleName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"{indentStr}      COM对象转换失败: {ex.Message}");
                            }
                            finally
                            {
                                Marshal.Release(unk);
                            }
                        }
                    }
                }
                
                if (columns.Count > 0)
                    sb.AppendLine($"{indentStr}  检测到的列: {string.Join(", ", columns)}");
                if (rowCounts.Count > 0)
                    sb.AppendLine($"{indentStr}  检测到的行数: {rowCounts.Count}");
            }

            // If row, print cell count and traverse cells
            if (roleName.Equals("row", StringComparison.OrdinalIgnoreCase))
            {
                int cellCount = acc.accChildCount;
                sb.AppendLine($"{indentStr}  Cell count: {cellCount}");
                if (cellCount > 0)
                {
                    object[] cellChildren = new object[cellCount];
                    int cellObtained = AccessibleChildren(acc, 0, cellCount, cellChildren, out int nCellObtained);
                    for (int j = 0; j < nCellObtained; j++)
                    {
                        if (cellChildren[j] is IAccessible cellAcc) // ROLE_SYSTEM_CELL
                        {
                            object rowSubChildTypeObj = provider.GetRole(cellAcc);
                            int rowSubChildType = (rowSubChildTypeObj is int) ? (int)rowSubChildTypeObj : Convert.ToInt32(rowSubChildTypeObj);
                            sb.AppendLine($"{indentStr}  Cell Role: {MARSAccessibleProvider.GetRoleName(rowSubChildType)}");
                            string cellText = null;
                            try { cellText = cellAcc.get_accValue(0); } catch { }
                            if (string.IsNullOrEmpty(cellText))
                            {
                                try { cellText = cellAcc.get_accName(0); } catch { }
                            }
                            sb.AppendLine($"{indentStr}    [Cell] \"{cellText}\"");
                        }
                        else if (Marshal.IsComObject(cellChildren[j]))
                        {
                            IntPtr unk = Marshal.GetIUnknownForObject(cellChildren[j]);
                            try
                            {
                                var accObj = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                if (accObj != null)
                                {
                                    TraverseAccessibleTree(accObj, provider, sb, indent + 1, hwnd);
                                }
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"{indentStr}  [COM object, cannot cast to IAccessible]: {ex.Message}");
                            }
                            finally
                            {
                                Marshal.Release(unk);
                            }
                        }
                    }
                }
            }

            // Recursively traverse children
            if (childCount > 0)
            {
                object[] children = new object[childCount];
                int obtained = AccessibleChildren(acc, 0, childCount, children, out int nObtained);
                for (int i = 0; i < nObtained; i++)
                {
                    if (children[i] is IAccessible childAcc)
                    {
                        // 尝试转换为 Accessible2
                        IAccessible2 accessible2 = TryConvertToAccessible2(childAcc);
                        if (accessible2 != null)
                        {
                            string indentStr2 = new string(' ', (indent + 1) * 2);
                            sb.AppendLine($"{indentStr2}[Accessible2] 检测到 Accessible2 接口");
                            try
                            {
                                accessible2.get_attributes(out string attributes);
                                if (!string.IsNullOrEmpty(attributes))
                                {
                                    sb.AppendLine($"{indentStr2}  Attributes: {attributes}");
                                }
                                accessible2.get_states(out long states);
                                sb.AppendLine($"{indentStr2}  States: {states}");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"{indentStr2}  Accessible2 信息获取失败: {ex.Message}");
                            }
                        }

                        // 尝试转换为 IAccessibleTable
                        IAccessibleTable accessibleTable = TryConvertToAccessibleTable(childAcc);
                        if (accessibleTable != null)
                        {
                            string indentStr2 = new string(' ', (indent + 1) * 2);
                            sb.AppendLine($"{indentStr2}[IAccessibleTable] 检测到 IAccessibleTable 接口");
                            try
                            {
                                accessibleTable.get_nColumns(out long columnCount);
                                accessibleTable.get_nRows(out long rowCount);
                                sb.AppendLine($"{indentStr2}  Columns: {columnCount}, Rows: {rowCount}");
                                
                                // 获取列信息
                                if (columnCount > 0)
                                {
                                    sb.AppendLine($"{indentStr2}  Column Details:");
                                    for (int col = 0; col < columnCount && col < 10; col++) // 限制显示前10列
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
                                            sb.AppendLine($"{indentStr2}    Column[{col}]: \"{headerName}\" (Desc: {colDescription})");
                                        }
                                        catch (Exception ex)
                                        {
                                            sb.AppendLine($"{indentStr2}    Column[{col}]: 获取信息失败 - {ex.Message}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"{indentStr2}  IAccessibleTable 信息获取失败: {ex.Message}");
                            }
                        }

                        TraverseAccessibleTree(childAcc, provider, sb, indent + 1, hwnd);
                    }
                }
            }
        }



        [TestMethod]
        public void TestAccessibleTreeToFile()
        {
            IntPtr hwnd = new IntPtr(0x003C1576);
            var provider = new MARSAccessibleProvider();
            var accObj = provider.GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null)
            {
                TestContext.WriteLine("无法获取 IAccessible 对象。");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 开始遍历可访问性树结构 ===");
            TraverseAccessibleTree(accObj, provider, sb, 0, hwnd);
            sb.AppendLine("=== 遍历完成 ===");

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AccessibleTree.txt");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            TestContext.WriteLine($"树状结构已保存到: {filePath}");
        }

        [TestMethod]
        public void TestButtonDropDownGridDataExtraction()
        {
            // 请替换为您实际的窗口句柄
            IntPtr hwnd = new IntPtr(0x00111392); // 请修改为您的窗口句柄
            
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
                IAccessible2 accessible2 = TryConvertToAccessible2(accObj);
                IAccessibleTable accessibleTable = TryConvertToAccessibleTable(accObj);
                
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

        // 获取对象的屏幕位置信息
        private string GetLocationInfo(IAccessible accessible, object childIndex = null)
        {
            try
            {
                // 尝试获取位置信息
                accessible.accLocation(out int x, out int y, out int width, out int height, childIndex ?? 0);
                return $"X:{x},Y:{y},W:{width},H:{height}";
            }
            catch (Exception ex)
            {
                return $"位置获取失败:{ex.Message}";
            }
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
                IAccessibleTable accessibleTable = TryConvertToAccessibleTable(gridAcc);
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
    }
}

