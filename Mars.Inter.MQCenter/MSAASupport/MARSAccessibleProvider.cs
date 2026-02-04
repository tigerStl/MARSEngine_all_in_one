using Accessibility;
using Mars.message.windowsWrapper.SystemUtil;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MSAASupport
{

    public struct TraversalNode
    {
        public IAccessible Accessible;
        public int Indent;
        public IntPtr Hwnd;
        public string ParentPath; // 父对象路径
        public int ChildIndex;   // 在父对象中的索引
    }

    public class MARSAccessibleProvider : IAccessibleProvider
    {
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwId, ref Guid riid, out IntPtr ppvObject);

        private static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

        /// <summary>
        /// 获取窗口句柄对应的 IAccessible 实例
        /// </summary>
        public object GetAccessibleObject(IntPtr hwnd)
        {
            IntPtr pAcc = IntPtr.Zero;

            int result = AccessibleObjectFromWindow(hwnd, 0x00000000, ref IID_IAccessible, out pAcc);
            if (result >= 0 && pAcc != IntPtr.Zero)
            {
                // 返回 COM 对象实例
                return Marshal.GetObjectForIUnknown(pAcc);
            }
            return null;
        }
        // MSAA AccessibleChildren API
        [DllImport("oleacc.dll")]
        public static extern int AccessibleChildren(
            IAccessible paccContainer,
            int iChildStart,
            int cChildren,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
            out int pcObtained);

        /// <summary>
        /// 获取表格的所有列名
        /// </summary>
        public List<string> GetTableColumns(IntPtr hwnd)
        {
            var acc = GetAccessibleObject(hwnd) as IAccessible;
            var columns = new List<string>();
            if (acc == null) return columns;

            int childCount = acc.accChildCount;
            if (childCount > 0)
            {
                object[] children = new object[childCount];
                int obtained = AccessibleChildren(acc, 0, childCount, children, out int nObtained);
                for (int i = 0; i < nObtained; i++)
                {
                    if (children[i] is IAccessible childAcc)
                    {
                        int role = GetRole(childAcc);
                        if (role == 0x19) // ROLE_SYSTEM_COLUMNHEADER
                        {
                            string colName = childAcc.get_accName(0);
                            if (!string.IsNullOrEmpty(colName))
                                columns.Add(colName);
                        }
                    }
                }
            }
            return columns;
        }

        /// <summary>
        /// 获取表格的行数
        /// </summary>
        public int GetTableRowCount(IntPtr hwnd)
        {
            var acc = GetAccessibleObject(hwnd) as IAccessible;
            if (acc == null) return 0;

            int childCount = acc.accChildCount;
            object[] children = new object[childCount];
            int obtained = AccessibleChildren(acc, 0, childCount, children, out int nObtained);

            int rowCount = 0;
            for (int i = 0; i < nObtained; i++)
            {
                if (children[i] is IAccessible childAcc)
                {
                    int role = GetRole(childAcc);
                    if (role == 0x28) // ROLE_SYSTEM_ROW
                    {
                        rowCount++;
                    }
                }
            }
            return rowCount;
        }

        /// <summary>
        /// 获取指定单元格的值（row和col均从0开始）
        /// </summary>
        public string GetTableCellValue(IntPtr hwnd, int row, int col)
        {
            var acc = GetAccessibleObject(hwnd) as IAccessible;
            if (acc == null) return null;

            // 查找第row行
            int childCount = acc.accChildCount;
            int rowIdx = -1;
            for (int i = 1; i <= childCount; i++)
            {
                object childObj = acc.get_accChild(i);
                if (childObj is IAccessible childAcc && GetRole(childAcc) == 0x28) // ROLE_SYSTEM_ROW
                {
                    rowIdx++;
                    if (rowIdx == row)
                    {
                        // 查找第col列的cell
                        int cellCount = childAcc.accChildCount;
                        int colIdx = -1;
                        for (int j = 1; j <= cellCount; j++)
                        {
                            object cellObj = childAcc.get_accChild(j);
                            if (cellObj is IAccessible cellAcc && GetRole(cellAcc) == 0x1D) // ROLE_SYSTEM_CELL
                            {
                                colIdx++;
                                if (colIdx == col)
                                {
                                    return cellAcc.get_accValue(0) ?? cellAcc.get_accName(0);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 在指定单元格点击鼠标（row和col均从0开始），isRightClick为true时右键，否则左键
        /// </summary>
        public bool ClickTableCell(IntPtr hwnd, int row, int col, bool isRightClick)
        {
            var acc = GetAccessibleObject(hwnd) as IAccessible;
            if (acc == null) return false;

            // 查找第row行
            int childCount = acc.accChildCount;
            int rowIdx = -1;
            for (int i = 1; i <= childCount; i++)
            {
                object childObj = acc.get_accChild(i);
                if (childObj is IAccessible childAcc && GetRole(childAcc) == 0x28) // ROLE_SYSTEM_ROW
                {
                    rowIdx++;
                    if (rowIdx == row)
                    {
                        // 查找第col列的cell
                        int cellCount = childAcc.accChildCount;
                        int colIdx = -1;
                        for (int j = 1; j <= cellCount; j++)
                        {
                            object cellObj = childAcc.get_accChild(j);
                            if (cellObj is IAccessible cellAcc && GetRole(cellAcc) == 0x1D) // ROLE_SYSTEM_CELL
                            {
                                colIdx++;
                                if (colIdx == col)
                                {
                                    // 获取cell的屏幕坐标
                                    int left, top, width, height;
                                    cellAcc.accLocation(out left, out top, out width, out height, 0);
                                    int x = left + (width / 2);
                                    int y = top + (height / 2);

                                    // 模拟鼠标点击
                                    if (!isRightClick)
                                    {
                                        MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                                    }
                                    else
                                    {
                                        MarsWindowsAPIsExtend.RightMouseClick(x, y);
                                    }
                                    //MouseClick(x, y, isRightClick);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取IAccessible对象的Role
        /// </summary>
        public int GetRole(IAccessible acc)
        {
            object roleObj = acc.get_accRole(0);
            return (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
        }

        public static int Get_Role(IAccessible acc)
        {
            object roleObj = acc.get_accRole(0);
            return (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
        }

        public static string GetRoleName(IAccessible acc)
        {
            int role = Get_Role(acc);
            return GetRoleName(role);
        }

        /// <summary>
        /// 获取Role的名称 from role id
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static string GetRoleName(int role)
        {
            switch (role)
            {
                case MARSAccessibleConstans.ROLE_SYSTEM_TITLEBAR: return "TitleBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_MENUBAR: return "MenuBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_SCROLLBAR: return "ScrollBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_GRIP: return "Grip";
                case MARSAccessibleConstans.ROLE_SYSTEM_SOUND: return "Sound";
                case MARSAccessibleConstans.ROLE_SYSTEM_CURSOR: return "Cursor";
                case MARSAccessibleConstans.ROLE_SYSTEM_CARET: return "Caret";
                case MARSAccessibleConstans.ROLE_SYSTEM_ALERT: return "Alert";
                case MARSAccessibleConstans.ROLE_SYSTEM_WINDOW: return "Window";
                case MARSAccessibleConstans.ROLE_SYSTEM_CLIENT: return "Client";
                case MARSAccessibleConstans.ROLE_SYSTEM_MENUPOPUP: return "MenuPopup";
                case MARSAccessibleConstans.ROLE_SYSTEM_MENUITEM: return "MenuItem";
                case MARSAccessibleConstans.ROLE_SYSTEM_TOOLTIP: return "ToolTip";
                case MARSAccessibleConstans.ROLE_SYSTEM_APPLICATION: return "Application";
                case MARSAccessibleConstans.ROLE_SYSTEM_DOCUMENT: return "Document";
                case MARSAccessibleConstans.ROLE_SYSTEM_PANE: return "Pane";
                case MARSAccessibleConstans.ROLE_SYSTEM_CHART: return "Chart";
                case MARSAccessibleConstans.ROLE_SYSTEM_DIALOG: return "Dialog";
                case MARSAccessibleConstans.ROLE_SYSTEM_BORDER: return "Border";
                case MARSAccessibleConstans.ROLE_SYSTEM_GROUPING: return "Grouping";
                case MARSAccessibleConstans.ROLE_SYSTEM_SEPARATOR: return "Separator";
                case MARSAccessibleConstans.ROLE_SYSTEM_TOOLBAR: return "ToolBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_STATUSBAR: return "StatusBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_TABLE: return "Table";
                case MARSAccessibleConstans.ROLE_SYSTEM_COLUMNHEADER: return "ColumnHeader";
                case MARSAccessibleConstans.ROLE_SYSTEM_ROWHEADER: return "RowHeader";
                case MARSAccessibleConstans.ROLE_SYSTEM_COLUMN: return "Column";
                case MARSAccessibleConstans.ROLE_SYSTEM_ROW: return "Row";
                case MARSAccessibleConstans.ROLE_SYSTEM_CELL: return "Cell";
                case MARSAccessibleConstans.ROLE_SYSTEM_LINK: return "Link";
                case MARSAccessibleConstans.ROLE_SYSTEM_HELPBALLOON: return "HelpBalloon";
                case MARSAccessibleConstans.ROLE_SYSTEM_CHARACTER: return "Character";
                case MARSAccessibleConstans.ROLE_SYSTEM_LIST: return "List";
                case MARSAccessibleConstans.ROLE_SYSTEM_LISTITEM: return "ListItem";
                case MARSAccessibleConstans.ROLE_SYSTEM_OUTLINE: return "Outline";
                case MARSAccessibleConstans.ROLE_SYSTEM_OUTLINEITEM: return "OutlineItem";
                case MARSAccessibleConstans.ROLE_SYSTEM_PAGETAB: return "PageTab";
                case MARSAccessibleConstans.ROLE_SYSTEM_PROPERTYPAGE: return "PropertyPage";
                case MARSAccessibleConstans.ROLE_SYSTEM_INDICATOR: return "Indicator";
                case MARSAccessibleConstans.ROLE_SYSTEM_GRAPHIC: return "Graphic";
                case MARSAccessibleConstans.ROLE_SYSTEM_STATICTEXT: return "StaticText";
                case MARSAccessibleConstans.ROLE_SYSTEM_TEXT: return "Text";
                case MARSAccessibleConstans.ROLE_SYSTEM_PUSHBUTTON: return "PushButton";
                case MARSAccessibleConstans.ROLE_SYSTEM_CHECKBUTTON: return "CheckButton";
                case MARSAccessibleConstans.ROLE_SYSTEM_RADIOBUTTON: return "RadioButton";
                case MARSAccessibleConstans.ROLE_SYSTEM_COMBOBOX: return "ComboBox";
                case MARSAccessibleConstans.ROLE_SYSTEM_DROPLIST: return "DropList";
                case MARSAccessibleConstans.ROLE_SYSTEM_PROGRESSBAR: return "ProgressBar";
                case MARSAccessibleConstans.ROLE_SYSTEM_DIAL: return "Dial";
                case MARSAccessibleConstans.ROLE_SYSTEM_HOTKEYFIELD: return "HotkeyField";
                case MARSAccessibleConstans.ROLE_SYSTEM_SLIDER: return "Slider";
                case MARSAccessibleConstans.ROLE_SYSTEM_SPINBUTTON: return "SpinButton";
                case MARSAccessibleConstans.ROLE_SYSTEM_DIAGRAM: return "Diagram";
                case MARSAccessibleConstans.ROLE_SYSTEM_ANIMATION: return "Animation";
                case MARSAccessibleConstans.ROLE_SYSTEM_EQUATION: return "Equation";
                case MARSAccessibleConstans.ROLE_SYSTEM_BUTTONDROPDOWN: return "ButtonDropDown";
                case MARSAccessibleConstans.ROLE_SYSTEM_BUTTONMENU: return "ButtonMenu";
                case MARSAccessibleConstans.ROLE_SYSTEM_BUTTONDROPDOWNGRID: return "ButtonDropDownGrid";
                case MARSAccessibleConstans.ROLE_SYSTEM_WHITESPACE: return "WhiteSpace";
                case MARSAccessibleConstans.ROLE_SYSTEM_PAGETABLIST: return "PageTabList";
                case MARSAccessibleConstans.ROLE_SYSTEM_CLOCK: return "Clock";
                case MARSAccessibleConstans.ROLE_SYSTEM_SPLITBUTTON: return "SplitButton";
                case MARSAccessibleConstans.ROLE_SYSTEM_IPADDRESS: return "IPAddress";
                case MARSAccessibleConstans.ROLE_SYSTEM_OUTLINEBUTTON: return "OutlineButton";
                default: return $"UnknownRole_0x{role:X}";
            }
        }

        public void SaveAccessibleTreeIfRoleMatch(IntPtr hwnd, string targetRoleName = null)
        {
            var accObj = GetAccessibleObject(hwnd) as IAccessible;
            if (accObj == null) return;

            int role = GetRole(accObj);
            string roleName = GetRoleName(role);

            if ((!string.IsNullOrEmpty(targetRoleName)) && (!string.Equals(roleName, targetRoleName, StringComparison.OrdinalIgnoreCase)))
                return;

            StringBuilder sb = new StringBuilder();
            TraverseAccessibleTree(accObj, sb, 0);

            string filePath = System.IO.Path.Combine("c:\\temp\\", $"AccessibleTree_Test_{hwnd.ToInt64()}.txt");
            System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // 辅助递归方法
        private void TraverseAccessibleTree(IAccessible acc, StringBuilder sb, int indent)
        {
            if (acc == null) return;

            string indentStr = new string(' ', indent * 2);
            int role = GetRole(acc);
            string roleName = GetRoleName(role);
            string text = null;
            try { text = acc.get_accName(0); } catch { }
            if (string.IsNullOrEmpty(text))
            {
                try { text = acc.get_accValue(0); } catch { }
            }
            sb.AppendLine($"{indentStr}- [{roleName}] \"{text}\"");

            int childCount = acc.accChildCount;
            if (childCount > 0)
            {
                object[] children = new object[childCount];
                int obtained = AccessibleChildren(acc, 0, childCount, children, out int nObtained);
                for (int i = 0; i < nObtained; i++)
                {
                    if (children[i] is IAccessible childAcc)
                    {
                        TraverseAccessibleTree(childAcc, sb, indent + 1);
                    }
                    else if (Marshal.IsComObject(children[i]))
                    {
                        IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                        try
                        {
                            var accChild = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                            if (accChild != null)
                                TraverseAccessibleTree(accChild, sb, indent + 1);
                        }
                        catch { }
                        finally
                        {
                            Marshal.Release(unk);
                        }
                    }
                }
            }
        }

        public static void TraverseAccessibleTreeNonRecursive(IAccessible acc, MARSAccessibleProvider provider, StringBuilder sb, int indent, IntPtr hwnd)
        {
            if (acc == null) return;

            // 使用栈来替代递归
            Stack<TraversalNode> stack = new Stack<TraversalNode>();
            stack.Push(new TraversalNode { Accessible = acc, Indent = indent, Hwnd = hwnd, ParentPath = "", ChildIndex = -1 });

            while (stack.Count > 0)
            {
                TraversalNode currentNode = stack.Pop();
                IAccessible currentAcc = currentNode.Accessible;
                int currentIndent = currentNode.Indent;
                IntPtr currentHwnd = currentNode.Hwnd;
                string currentParentPath = currentNode.ParentPath;
                int currentChildIndex = currentNode.ChildIndex;

                if (currentAcc == null) continue;

                string indentStr = new string(' ', currentIndent * 2);
                object roleObj = provider.GetRole(currentAcc);
                int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                string roleName = MARSAccessibleProvider.GetRoleName(role);
                string text = null;
                try { text = currentAcc.get_accName(0); } catch { }
                if (string.IsNullOrEmpty(text))
                {
                    try { text = currentAcc.get_accValue(0); } catch { }
                }
                string id = currentAcc.GetHashCode().ToString();
                int childCount = currentAcc.accChildCount;

                // 生成UFT AttachText和AttachPath
                string attachText = GenerateUFTAttachText(currentAcc, provider, currentHwnd);
                string attachPath = GenerateUFTAttachPath(currentAcc, provider, currentHwnd, currentParentPath, currentChildIndex);

                sb.AppendLine($"{indentStr}- |P:|[{roleName}] \"{text}\" (ID:{id})");
                if (!string.IsNullOrEmpty(attachText))
                {
                    sb.AppendLine($"{indentStr}  AttachText: {attachText}");
                }
                if (!string.IsNullOrEmpty(attachPath))
                {
                    sb.AppendLine($"{indentStr}  AttachPath: {attachPath}");
                }

                // 统一处理子对象 - 合并了ButtonDropDownGrid特殊处理和通用处理
                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = AccessibleChildren(currentAcc, 0, childCount, children, out int nObtained);
                    sb.AppendLine($"{indentStr}  子对象总数: {childCount}, 实际获取: {nObtained}");

                    // 如果是ButtonDropDownGrid，显示特殊标识
                    if (role == 24) // ROLE_SYSTEM_BUTTONDROPDOWNGRID
                    {
                        sb.AppendLine($"{indentStr}  *** ButtonDropDownGrid 详细信息 ***");
                        // FlashRedBorder(currentAcc); // 已屏蔽flash功能
                    }

                    List<string> columns = new List<string>();
                    List<string> rowCounts = new List<string>();

                    // 将子对象按相反顺序添加到栈中，以保持正确的遍历顺序
                    for (int i = nObtained - 1; i >= 0; i--)
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

                            // 处理列头
                            if (childRole == 0x19) // ROLE_SYSTEM_COLUMNHEADER
                            {
                                if (!string.IsNullOrEmpty(childText))
                                    columns.Add(childText);
                            }
                            // 处理行对象 - 详细处理单元格
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
                                            MARSAccessibleHelper.HandleAccessibleCell(cellAcc, provider, currentHwnd, currentParentPath, j, indentStr, sb);
                                        }
                                        else if (rowChildren[j] is int childIndex)
                                        {
                                            // 使用Helper类处理整数索引的单元格获取操作
                                            MARSAccessibleHelper.HandleIntegerIndexCellAccess(
                                                childAcc,
                                                childIndex,
                                                provider,
                                                currentHwnd,
                                                currentParentPath,
                                                indentStr,
                                                sb,
                                                i);
                                        }
                                        else if (Marshal.IsComObject(rowChildren[j]))
                                        {
                                            // 使用Helper类处理COM对象转换
                                            MARSAccessibleHelper.HandleComObjectConversion(
                                                rowChildren[j],
                                                provider,
                                                indentStr,
                                                sb);
                                        }
                                        else
                                        {
                                            sb.AppendLine($"{indentStr}          其他类型对象: {childType} = {childValue}");
                                        }
                                    }
                                }
                            }

                            // 尝试转换为 Accessible2
                            IAccessible2 accessible2 = TryConvertToAccessible2(childAcc);
                            if (accessible2 != null)
                            {
                                string indentStr2 = new string(' ', (currentIndent + 1) * 2);
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
                                string indentStr2 = new string(' ', (currentIndent + 1) * 2);
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

                            // 将子对象添加到栈中（但跳过已经在ButtonDropDownGrid中详细处理过的row对象）
                            // 检查是否已经在ButtonDropDownGrid中处理过
                            bool skipAddingToStack = false;
                            if (role == 24) // ROLE_SYSTEM_BUTTONDROPDOWNGRID
                            {
                                if (childRole == 0x1C) // ROLE_SYSTEM_ROW
                                {
                                    skipAddingToStack = true; // 跳过已经在ButtonDropDownGrid中详细处理过的row
                                }
                            }

                            if (!skipAddingToStack)
                            {
                                // 生成当前对象的路径组件，用于子对象的父路径
                                string currentPathComponent = GeneratePathComponent(currentAcc, provider, currentHwnd, currentChildIndex);
                                string childParentPath = string.IsNullOrEmpty(currentParentPath) ? currentPathComponent : $"{currentParentPath}/{currentPathComponent}";

                                stack.Push(new TraversalNode
                                {
                                    Accessible = childAcc,
                                    Indent = currentIndent + 1,
                                    Hwnd = currentHwnd,
                                    ParentPath = childParentPath,
                                    ChildIndex = i
                                });
                            }
                        }
                        else if (children[i] is int childIndex)
                        {
                            // 使用Helper类处理整数索引的子对象获取操作
                            MARSAccessibleHelper.HandleIntegerIndexCellAccess(
                                currentAcc,
                                childIndex,
                                provider,
                                currentHwnd,
                                currentParentPath,
                                indentStr,
                                sb,
                                i);
                        }
                        else if (Marshal.IsComObject(children[i]))
                        {
                            MARSAccessibleHelper.HandleComObjectConversion(
                                children[i],
                                provider,
                                indentStr,
                                sb);
                        }
                    }

                    // 显示汇总信息
                    if (role == 24) // ROLE_SYSTEM_BUTTONDROPDOWNGRID
                    {
                        if (columns.Count > 0)
                            sb.AppendLine($"{indentStr}  检测到的列: {string.Join(", ", columns)}");
                        if (rowCounts.Count > 0)
                            sb.AppendLine($"{indentStr}  检测到的行数: {rowCounts.Count}");
                    }
                }
            }
        }



        /// <summary>
        /// 新的非递归遍历方法 - 可读性更好的版本
        /// </summary>
        /// <param name="acc">要遍历的可访问性对象</param>
        /// <param name="provider">可访问性提供者</param>
        /// <param name="sb">字符串构建器</param>
        /// <param name="indent">初始缩进级别</param>
        /// <param name="hwnd">窗口句柄</param>
        public static void TraverseAccessibleTreeNonRecursiveNew(IAccessible acc, MARSAccessibleProvider provider,
            StringBuilder sb, int indent, IntPtr hwnd)
        {
            // 步骤1：检查输入参数
            if (acc == null)
            {
                sb.AppendLine("错误：输入的可访问性对象为空");
                return;
            }

            // 初始化遍历栈
            Stack<TraversalNode> stack = new Stack<TraversalNode>();
            List<TraversalNode> treeNodes = new List<TraversalNode>(); // 用于存储树状结构

            // 将根节点添加到栈中
            stack.Push(new TraversalNode
            {
                Accessible = acc,
                Indent = indent,
                Hwnd = hwnd,
                ParentPath = "",
                ChildIndex = -1
            });


            bool isAFX = MfcAccessibleHelper.IsMfcWindow(hwnd);

            // 主循环：处理栈中的对象
            while (stack.Count > 0)
            {
                // 步骤1：堆栈中是否有新的对象？acc是否为空？
                TraversalNode currentNode = stack.Pop();
                IAccessible currentAcc = currentNode.Accessible;

                if (currentAcc == null)
                {
                    sb.AppendLine($"{new string(' ', currentNode.Indent * 2)}警告：遇到空的可访问性对象，跳过");
                    continue;
                }

                // 步骤2：获得基本信息：TraversalNode，将对象放到树中
                try
                {
                    object roleObj = provider.GetRole(currentAcc);
                    int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                    string roleName = MARSAccessibleProvider.GetRoleName(role);
                    string text = "", strValue = "";
                    // 输出基本信息
                    string indentStr = new string(' ', currentNode.Indent * 2);

                    try { text = currentAcc.get_accName(0); } catch { }
                    try { strValue = currentAcc.get_accValue(0); } catch { }

                    IntPtr currentHwnd = IntPtr.Zero;
                    MfcAccessibleHelper.WindowFromAccessibleObject(currentAcc, out currentHwnd);
                    sb.AppendLine($"{indentStr}\t处理前，基本数据: |roleid|{role}|roleobj|{roleObj}|accName|{text}|value|{strValue}");
                    isAFX = MfcAccessibleHelper.IsMfcWindow(currentHwnd);
                    if (isAFX && (!currentHwnd.Equals(hwnd)))
                    {
                        List<IntPtr> directChildWindows = MfcAccessibleHelper.GetDirectChildWindows(currentHwnd);
                        sb.AppendLine($"{indentStr}\t检测到MFC窗口，直接子窗口数量: {directChildWindows.Count}|roleid|{role}|roleobj|{roleObj}");
                        foreach (var childHwnd in directChildWindows)
                        {
                            var childAccess = provider.GetAccessibleObject(hwnd) as IAccessible;
                            if (childAccess != null)
                            {
                                stack.Push(new TraversalNode
                                {
                                    Accessible = childAccess,
                                    Indent = indent + 1,
                                    Hwnd = childHwnd,
                                    ParentPath = "",
                                    ChildIndex = -1
                                });
                            }
                            else
                            {
                                sb.AppendLine($"{indentStr}\t警告：无法获取子窗口 {childHwnd} 的可访问性对象");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(text))
                    {
                        try { text = currentAcc.get_accValue(0); } catch { }
                    }
                    string id = "";
                    try { id = currentAcc.get_accDescription(0); } catch { }

                    // 将当前节点添加到树中
                    treeNodes.Add(currentNode);
                    sb.AppendLine($"{indentStr}- |P:|[{roleName}] \"{text}\" (ID:{id})|winHwnd|{currentHwnd}");

                    // 步骤3：是否有子对象？没有，返回
                    int childCount = currentAcc.accChildCount;
                    if (childCount <= 0)
                    {
                        sb.AppendLine($"{indentStr}  无子对象");
                        continue;
                    }

                    sb.AppendLine($"{indentStr}  子对象总数: {childCount}");

                    // 步骤4：遍历子对象
                    try
                    {
                        object[] children = new object[childCount];
                        int obtained = AccessibleChildren(currentAcc, 0, childCount, children, out int nObtained);
                        sb.AppendLine($"{indentStr}  实际获取到: {nObtained} 个子对象");

                        // 将子对象按相反顺序添加到栈中，以保持正确的遍历顺序
                        for (int i = nObtained - 1; i >= 0; i--)
                        {
                            string childType = children[i]?.GetType()?.Name ?? "null";
                            string childValue = children[i]?.ToString() ?? "null";
                            sb.AppendLine($"{indentStr}        子对象[{i}]: 类型={childType}, 值={childValue}");

                            // 步骤4.1：子对象是IAccessible？将子对象放到堆栈中
                            if (children[i] is IAccessible childAcc)
                            {
                                // 生成路径组件
                                string currentPathComponent = GeneratePathComponent(currentAcc, provider, currentNode.Hwnd, i);
                                string childParentPath = string.IsNullOrEmpty(currentNode.ParentPath) ? currentPathComponent : $"{currentNode.ParentPath}/{currentPathComponent}";
                                int roleid = MARSAccessibleProvider.Get_Role(childAcc);
                                string rolename = MARSAccessibleProvider.GetRoleName(roleid);
                                sb.AppendLine($"{indentStr}\t{roleid}|{rolename}");
                                stack.Push(new TraversalNode
                                {
                                    Accessible = childAcc,
                                    Indent = currentNode.Indent + 1,
                                    Hwnd = currentNode.Hwnd,
                                    ParentPath = childParentPath,
                                    ChildIndex = i
                                });
                            }
                            // 步骤4.2：子对象是整数，按照index的方法获得真正的子对象，放到堆栈中
                            else if (children[i] is int childIndex)
                            {
                                sb.AppendLine($"{indentStr}    子对象[{i}]: 整数索引 {childIndex}，尝试获取真实对象...");

                                try
                                {
                                    object realChildObj = currentAcc.get_accChild(childIndex);
                                    string strName = currentAcc.get_accName(childIndex);
                                    strValue = currentAcc.get_accValue(childIndex);
                                    object? tmpRole = currentAcc.get_accRole(childIndex);
                                    string tmpRoleName = tmpRole == null ? "NO_ROLE" : tmpRole is int ? MARSAccessibleProvider.GetRoleName((int)tmpRole) : "Not_a_role";
                                    object? tmpState = currentAcc.get_accState(childIndex);
                                    Rectangle tmpRct = default(Rectangle);
                                    try
                                    {
                                        currentAcc.accLocation(out int left, out int top, out int width, out int height, childIndex);
                                        tmpRct = new Rectangle(left, top, width, height);
                                    }
                                    catch
                                    {
                                        tmpRct = Rectangle.Empty;
                                    }

                                    if (realChildObj is IAccessible realChildAcc)
                                    {
                                        // 生成路径组件
                                        string currentPathComponent = GeneratePathComponent(currentAcc, provider, currentNode.Hwnd, i);
                                        string childParentPath = string.IsNullOrEmpty(currentNode.ParentPath) ? currentPathComponent : $"{currentNode.ParentPath}/{currentPathComponent}";

                                        stack.Push(new TraversalNode
                                        {
                                            Accessible = realChildAcc,
                                            Indent = currentNode.Indent + 1,
                                            Hwnd = currentNode.Hwnd,
                                            ParentPath = childParentPath,
                                            ChildIndex = i
                                        });

                                        sb.AppendLine($"{indentStr}      成功获取真实对象并添加到栈中");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"{indentStr}      获取到的对象不是IAccessible: {realChildObj?.GetType()?.Name ?? "null"}" +
                                            $"{indentStr}      \t|name|{strName}|value|{strValue}|role|{tmpRole}|-{tmpRoleName}|rect|{tmpRct}|state|{tmpState}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"{indentStr}      获取真实对象失败: {ex.Message}");
                                }
                            }
                            // 步骤4.3：子对象是其他com对象，按照com的方法获得
                            else if (Marshal.IsComObject(children[i]))
                            {
                                sb.AppendLine($"{indentStr}    子对象[{i}]: COM对象，尝试转换...");

                                try
                                {
                                    IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                                    try
                                    {
                                        var convertedAcc = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                                        if (convertedAcc != null)
                                        {
                                            // 生成路径组件
                                            string currentPathComponent = GeneratePathComponent(currentAcc, provider, currentNode.Hwnd, i);
                                            string childParentPath = string.IsNullOrEmpty(currentNode.ParentPath) ? currentPathComponent : $"{currentNode.ParentPath}/{currentPathComponent}";

                                            stack.Push(new TraversalNode
                                            {
                                                Accessible = convertedAcc,
                                                Indent = currentNode.Indent + 1,
                                                Hwnd = currentNode.Hwnd,
                                                ParentPath = childParentPath,
                                                ChildIndex = i
                                            });

                                            sb.AppendLine($"{indentStr}      COM对象转换成功并添加到栈中");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"{indentStr}      COM对象转换失败：结果为null");
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.Release(unk);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"{indentStr}      COM对象转换异常: {ex.Message}");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"{indentStr}    子对象[{i}]: 其他类型 {children[i]?.GetType()?.Name ?? "null"}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indentStr}  获取子对象时发生异常: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{new string(' ', currentNode.Indent * 2)}处理节点时发生异常: {ex.Message}");
                }
            }

            // 步骤6：打印树状TraversalNode
            sb.AppendLine();
            sb.AppendLine("=== 树状结构汇总 ===");
            foreach (var node in treeNodes)
            {
                string indentStr = new string(' ', node.Indent * 2);
                try
                {
                    object roleObj = provider.GetRole(node.Accessible);
                    int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                    string roleName = MARSAccessibleProvider.GetRoleName(role);
                    string text = "";
                    try { text = node.Accessible.get_accName(0); } catch { }
                    if (string.IsNullOrEmpty(text))
                    {
                        try { text = node.Accessible.get_accValue(0); } catch { }
                    }

                    sb.AppendLine($"{indentStr}[{node.Indent}] {roleName} - \"{text}\" (Path: {node.ParentPath})");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{indentStr}[{node.Indent}] 错误: {ex.Message}");
                }
            }
        }

        // 尝试将 IAccessible 转换为 IAccessible2
        public static IAccessible2 TryConvertToAccessible2(IAccessible accessible)
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
        public static IAccessibleTable TryConvertToAccessibleTable(IAccessible accessible)
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


        // UFT AttachPath生成相关方法
        private static Dictionary<string, string> _pathComponentCache = new Dictionary<string, string>();
        public static string GeneratePathComponent(IAccessible acc, MARSAccessibleProvider provider,
            IntPtr hwnd, int childIndex)
        {
            if (acc == null) return "";

            // 创建缓存键
            string cacheKey = $"{acc.GetHashCode()}_{hwnd}_{childIndex}";
            if (_pathComponentCache.ContainsKey(cacheKey))
            {
                return _pathComponentCache[cacheKey];
            }

            StringBuilder component = new StringBuilder();
            List<string> properties = new List<string>();

            try
            {
                // 获取基本信息
                object roleObj = provider.GetRole(acc);
                int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                string roleName = MARSAccessibleProvider.GetRoleName(role);

                string text = "";
                try { text = acc.get_accName(0); } catch { }
                if (string.IsNullOrEmpty(text))
                {
                    try { text = acc.get_accValue(0); } catch { }
                }

                // 获取窗口类名
                string windowClass = GetWindowClassName(hwnd);

                // 获取控制ID
                int controlId = MarsWindowsAPIs.GetDlgCtrlID(hwnd);

                // 优先使用最稳定的属性
                if (!string.IsNullOrEmpty(text) && IsStableText(text))
                {
                    properties.Add($"text={text}");
                }
                else if (!string.IsNullOrEmpty(windowClass) && IsStableWindowClass(windowClass))
                {
                    properties.Add($"class={windowClass}");
                }
                else if (controlId > 0 && IsStableControlId(controlId))
                {
                    properties.Add($"id={controlId}");
                }
                else
                {
                    // 使用角色和索引作为后备
                    properties.Add($"role={roleName}");
                    if (childIndex >= 0)
                    {
                        properties.Add($"index={childIndex}");
                    }
                }

                // 组合属性
                if (properties.Count > 0)
                {
                    component.Append(string.Join(";", properties));
                }
                else
                {
                    component.Append($"role={roleName}");
                }

                // 缓存结果
                string result = component.ToString();
                _pathComponentCache[cacheKey] = result;
                return result;
            }
            catch (Exception ex)
            {
                return $"error:{ex.Message}";
            }
        }



        public static bool IsStableText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // 排除包含动态内容的文本
            string[] unstablePatterns = {
                "time", "date", "timestamp", "id:", "index:", "count:", "number:",
                "session", "user", "login", "temp", "cache", "random"
            };

            string lowerText = text.ToLower();
            foreach (string pattern in unstablePatterns)
            {
                if (lowerText.Contains(pattern))
                {
                    return false;
                }
            }

            // 检查是否包含数字（可能是动态的）
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\d{4,}")) // 4位以上数字
            {
                return false;
            }

            return true;
        }

        public static bool IsStableWindowClass(string windowClass)
        {
            if (string.IsNullOrEmpty(windowClass)) return false;

            // 排除动态生成的类名
            string[] unstableClasses = {
                "temp", "dynamic", "generated", "random"
            };

            string lowerClass = windowClass.ToLower();
            foreach (string pattern in unstableClasses)
            {
                if (lowerClass.Contains(pattern))
                {
                    return false;
                }
            }

            return true;
        }


        public static string GenerateUFTAttachPath(IAccessible acc, MARSAccessibleProvider provider, IntPtr hwnd, string parentPath, int childIndex)
        {
            if (acc == null) return "";

            try
            {
                // 生成当前对象的路径组件
                string currentComponent = GeneratePathComponent(acc, provider, hwnd, childIndex);

                // 构建完整路径
                if (string.IsNullOrEmpty(parentPath))
                {
                    return currentComponent;
                }
                else
                {
                    return $"{parentPath}/{currentComponent}";
                }
            }
            catch (Exception ex)
            {
                return $"error:{ex.Message}";
            }
        }


        // 获取对象的屏幕位置信息
        public static string GetLocationInfo(IAccessible accessible, object childIndex = null)
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

        // UFT AttachText生成相关方法
        public static string GenerateUFTAttachText(IAccessible acc, MARSAccessibleProvider provider, IntPtr hwnd)
        {
            if (acc == null) return "";

            StringBuilder attachText = new StringBuilder();
            List<string> properties = new List<string>();

            try
            {
                // 获取基本信息
                object roleObj = provider.GetRole(acc);
                int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                string roleName = MARSAccessibleProvider.GetRoleName(role);

                string text = "";
                try { text = acc.get_accName(0); } catch { }
                if (string.IsNullOrEmpty(text))
                {
                    try { text = acc.get_accValue(0); } catch { }
                }

                // 获取窗口类名
                string windowClass = GetWindowClassName(hwnd);

                // 获取控制ID
                int controlId = MarsWindowsAPIs.GetDlgCtrlID(hwnd);

                // 生成gen:属性（基于稳定属性）
                string genValue = GenerateStableLocatorStrategy(acc, provider, roleName, text, windowClass, controlId);
                if (!string.IsNullOrEmpty(genValue))
                {
                    properties.Add($"gen:{genValue}");
                }

                // 添加稳定的属性
                if (!string.IsNullOrEmpty(text) && IsStableText(text))
                {
                    properties.Add($"text:{text}");
                }

                if (!string.IsNullOrEmpty(windowClass) && IsStableWindowClass(windowClass))
                {
                    properties.Add($"class:{windowClass}");
                }

                if (controlId > 0 && IsStableControlId(controlId))
                {
                    properties.Add($"id:{controlId}");
                }

                // 添加角色信息
                properties.Add($"role:{roleName}");

                // 组合所有属性
                if (properties.Count > 0)
                {
                    attachText.Append(string.Join(";", properties));
                }
            }
            catch (Exception ex)
            {
                attachText.Append($"error:{ex.Message}");
            }

            return attachText.ToString();
        }


        private static string GenerateStableLocatorStrategy(IAccessible acc, MARSAccessibleProvider provider, string roleName, string text, string windowClass, int controlId)
        {
            // 基于稳定属性生成定位策略
            List<string> strategies = new List<string>();

            // 优先使用稳定的文本内容
            if (!string.IsNullOrEmpty(text) && IsStableText(text))
            {
                strategies.Add($"text={text}");
            }

            // 使用窗口类名
            if (!string.IsNullOrEmpty(windowClass) && IsStableWindowClass(windowClass))
            {
                strategies.Add($"class={windowClass}");
            }

            // 使用控制ID
            if (controlId > 0 && IsStableControlId(controlId))
            {
                strategies.Add($"id={controlId}");
            }

            // 使用角色
            if (!string.IsNullOrEmpty(roleName))
            {
                strategies.Add($"role={roleName}");
            }

            return strategies.Count > 0 ? string.Join(";", strategies) : "";
        }



        private static bool IsStableControlId(int controlId)
        {
            // 控制ID通常比较稳定，但排除一些特殊值
            return controlId > 0 && controlId < 1000000; // 排除过大的ID
        }

        private static string GetWindowClassName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return "";

            StringBuilder className = new StringBuilder(256);
            int result = MarsWindowsAPIs.GetClassName(hwnd, className, className.Capacity);
            return result > 0 ? className.ToString() : "";
        }

        public bool TryGetIAccessibleTable(object accObj, out object accessibleTable)
        {
            accessibleTable = null;
            if (accObj == null)
                return false;

            // IAccessibleTable IID: {35AD8070-C20C-4fb4-B094-F4F7275DD469}
            Guid IID_IAccessibleTable = new Guid("35AD8070-C20C-4fb4-B094-F4F7275DD469");
            IntPtr pUnk = Marshal.GetIUnknownForObject(accObj);
            IntPtr pTable = IntPtr.Zero;
            try
            {
                int hr = Marshal.QueryInterface(pUnk, ref IID_IAccessibleTable, out pTable);
                if (hr == 0 && pTable != IntPtr.Zero)
                {
                    accessibleTable = Marshal.GetObjectForIUnknown(pTable);
                    return true;
                }
            }
            finally
            {
                if (pTable != IntPtr.Zero) Marshal.Release(pTable);
                if (pUnk != IntPtr.Zero) Marshal.Release(pUnk);
            }
            return false;
        }

        public bool TryGetIAccessible2(object accObj, out object accessible2)
        {
            accessible2 = null;
            if (accObj == null)
                return false;

            // IAccessible2 IID: {E89F726E-C4F4-4c19-BB19-B647D7FA8478}
            Guid IID_IAccessible2 = new Guid("E89F726E-C4F4-4c19-BB19-B647D7FA8478");
            IntPtr pUnk = Marshal.GetIUnknownForObject(accObj);
            IntPtr pAcc2 = IntPtr.Zero;
            try
            {
                int hr = Marshal.QueryInterface(pUnk, ref IID_IAccessible2, out pAcc2);
                if (hr == 0 && pAcc2 != IntPtr.Zero)
                {
                    accessible2 = Marshal.GetObjectForIUnknown(pAcc2);
                    return true;
                }
            }
            finally
            {
                if (pAcc2 != IntPtr.Zero) Marshal.Release(pAcc2);
                if (pUnk != IntPtr.Zero) Marshal.Release(pUnk);
            }
            return false;
        }

        /// <summary>
        /// 获取分页控件的子项矩形位置,针对pagetab， pagelist
        /// </summary>
        /// <param name="accessible"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal static Rectangle getPageSubItemRect(IAccessible accessible, int idx, ref bool isOk, ref string strError)
        {
            if (accessible == null)
            {
                strError = "输入的IAccessible对象为空";
                isOk = false;
                return Rectangle.Empty;
            }
            Rectangle rect = new Rectangle();
            try
            {
                int left, top, width, height;
                accessible.accLocation(out left, out top, out width, out height, idx);
                rect.X = left;
                rect.Y = top;
                rect.Width = width;
                rect.Height = height;
                isOk = true;
                return rect;
            }
            catch(Exception e)
            {
                strError = e.Message;
                isOk = false;
                return Rectangle.Empty;
            }

        }
    }
}
