using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Accessibility;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;

namespace Mars.Inter.MQCenter.MSAASupport
{
    /// <summary>
    /// MSAA辅助类，用于处理可访问性对象的复杂操作
    /// </summary>
    public class MARSAccessibleHelper
    {
        /// <summary>
        /// 静态开关：是否显示高度和宽度都为0的IAccessible对象
        /// </summary>
        public static bool ShowZeroSizeObjects { get; set; } = false;
        
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromPoint(MSAA_POINT pt, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible ppacc, [Out, MarshalAs(UnmanagedType.Struct)] out object pvarChild);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSAA_POINT
        {
            public int x;
            public int y;
        }
        
        /// <summary>
        /// 判断某个 IAccessible 是否属于指定根窗口（topHwnd）
        /// 通过 accParent 逐级向上查找其拥有的窗口句柄是否与 topHwnd 一致
        /// </summary>
        private static bool BelongsToRootWindow(IAccessible acc, IntPtr topHwnd)
        {
            if (acc == null || topHwnd == IntPtr.Zero) return false;
            int guard = 0;
            IAccessible cur = acc;
            while (cur != null && guard++ < 128)
            {
                try
                {
                    // 尝试从该层对象获取其关联的窗口句柄（通过 accLocation 获取一个点，然后 WindowFromPoint 找 hwnd）
                    cur.accLocation(out int l, out int t, out int w, out int h, 0);
                    var pt = new System.Drawing.Point(Math.Max(l, 0), Math.Max(t, 0));
                    var hwnd = MarsWindowsAPIs.WindowFromPoint(pt);
                    if (hwnd != IntPtr.Zero)
                    {
                        var root = MarsWindowsAPIs.GetAncestor(hwnd, MarsWindowsAPIs.GetAncestorFlags.GetRoot);
                        if (root == IntPtr.Zero) root = hwnd;
                        if (root == topHwnd)
                            return true;
                        // 如果已经到达另一个根窗口，说明不属于
                        return false;
                    }
                }
                catch { }

                try
                {
                    object p = cur.accParent;
                    cur = p as IAccessible;
                }
                catch { break; }
            }
            return false;
        }
        /// <summary>
        /// 处理整数索引的单元格获取操作
        /// </summary>
        /// <param name="childAcc">子可访问性对象</param>
        /// <param name="childIndex">子索引</param>
        /// <param name="provider">可访问性提供者</param>
        /// <param name="currentHwnd">当前窗口句柄</param>
        /// <param name="currentParentPath">当前父路径</param>
        /// <param name="indentStr">缩进字符串</param>
        /// <param name="sb">字符串构建器</param>
        /// <param name="i">行索引</param>
        public static void HandleIntegerIndexCellAccess(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            IntPtr currentHwnd, 
            string currentParentPath, 
            string indentStr, 
            StringBuilder sb, 
            int i)
        {
            sb.AppendLine($"{indentStr}          整数索引: {childIndex}，尝试多种方法获取单元格...");

            // 方法1: 通过索引获取子对象
            TryMethod1_GetChildByIndex(childAcc, childIndex, provider, currentHwnd, currentParentPath, indentStr, sb);

            // 方法2: 尝试直接通过父对象获取
            TryMethod2_GetThroughParent(childAcc, childIndex, provider, currentHwnd, currentParentPath, indentStr, sb, i);

            // 方法3: 尝试通过索引作为简单元素处理
            TryMethod3_GetAsSimpleElement(childAcc, childIndex, provider, indentStr, sb);

            // 方法4: 记录索引信息，可能用于后续分析
            sb.AppendLine($"{indentStr}            方法4-索引信息: 行索引={i}, 单元格索引={childIndex}");
        }

        /// <summary>
        /// 处理整数索引的单元格获取操作（带遍历栈支持）
        /// </summary>
        /// <param name="childAcc">子可访问性对象</param>
        /// <param name="childIndex">子索引</param>
        /// <param name="provider">可访问性提供者</param>
        /// <param name="currentHwnd">当前窗口句柄</param>
        /// <param name="currentParentPath">当前父路径</param>
        /// <param name="indentStr">缩进字符串</param>
        /// <param name="sb">字符串构建器</param>
        /// <param name="i">行索引</param>
        /// <param name="stack">遍历栈</param>
        /// <param name="currentIndent">当前缩进级别</param>
        public static void HandleIntegerIndexCellAccess(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            IntPtr currentHwnd, 
            string currentParentPath, 
            string indentStr, 
            StringBuilder sb, 
            int i,
            Stack<TraversalNode> stack,
            int currentIndent)
        {
            sb.AppendLine($"{indentStr}          整数索引: {childIndex}，尝试多种方法获取单元格...");

            // 方法1: 通过索引获取子对象
            TryMethod1_GetChildByIndex(childAcc, childIndex, provider, currentHwnd, currentParentPath, indentStr, sb);

            // 方法2: 尝试直接通过父对象获取
            TryMethod2_GetThroughParent(childAcc, childIndex, provider, currentHwnd, currentParentPath, indentStr, sb, i);

            // 方法3: 尝试通过索引作为简单元素处理（带栈支持）
            TryMethod3_GetAsSimpleElement(childAcc, childIndex, provider, indentStr, sb, stack, currentIndent, currentHwnd, currentParentPath);

            // 方法4: 记录索引信息，可能用于后续分析
            sb.AppendLine($"{indentStr}            方法4-索引信息: 行索引={i}, 单元格索引={childIndex}");
        }

        /// <summary>
        /// 方法1: 通过索引获取子对象
        /// </summary>
        private static void TryMethod1_GetChildByIndex(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            IntPtr currentHwnd, 
            string currentParentPath, 
            string indentStr, 
            StringBuilder sb)
        {
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
                        string locationInfo1 = MARSAccessibleProvider.GetLocationInfo(indexCellAcc);
                        string method1AttachText = MARSAccessibleProvider.GenerateUFTAttachText(indexCellAcc, provider, currentHwnd);
                        string method1AttachPath = MARSAccessibleProvider.GenerateUFTAttachPath(indexCellAcc, provider, currentHwnd, currentParentPath, childIndex);
                        sb.AppendLine($"{indentStr}            方法1-通过索引获取: {cellRoleName} - \"{cellText}\"|位置|{locationInfo1}");
                        if (!string.IsNullOrEmpty(method1AttachText))
                        {
                            sb.AppendLine($"{indentStr}              方法1 AttachText: {method1AttachText}");
                        }
                        if (!string.IsNullOrEmpty(method1AttachPath))
                        {
                            sb.AppendLine($"{indentStr}              方法1 AttachPath: {method1AttachPath}");
                        }
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
        }

        /// <summary>
        /// 方法2: 尝试直接通过父对象获取
        /// </summary>
        private static void TryMethod2_GetThroughParent(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            IntPtr currentHwnd, 
            string currentParentPath, 
            string indentStr, 
            StringBuilder sb, 
            int i)
        {
            try
            {
                // 获取父对象（ButtonDropDownGrid）
                IAccessible parentGrid = childAcc.accParent as IAccessible;
                if (parentGrid != null)
                {
                    // 尝试转换为IAccessibleTable
                    IAccessibleTable table = MARSAccessibleProvider.TryConvertToAccessibleTable(parentGrid);
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
                                string locationInfo2 = MARSAccessibleProvider.GetLocationInfo(tableCell);
                                string method2AttachText = MARSAccessibleProvider.GenerateUFTAttachText(tableCell, provider, currentHwnd);
                                string method2AttachPath = MARSAccessibleProvider.GenerateUFTAttachPath(tableCell, provider, currentHwnd, currentParentPath, childIndex);
                                sb.AppendLine($"{indentStr}              方法2-通过IAccessibleTable获取: {cellRoleName} - \"{cellText}\"|位置|{locationInfo2}");
                                if (!string.IsNullOrEmpty(method2AttachText))
                                {
                                    sb.AppendLine($"{indentStr}                方法2 AttachText: {method2AttachText}");
                                }
                                if (!string.IsNullOrEmpty(method2AttachPath))
                                {
                                    sb.AppendLine($"{indentStr}                方法2 AttachPath: {method2AttachPath}");
                                }
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
        }

        /// <summary>
        /// 方法3: 尝试通过索引作为简单元素处理
        /// </summary>
        private static void TryMethod3_GetAsSimpleElement(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            string indentStr, 
            StringBuilder sb)
        {
            try
            {
                // 尝试将索引作为简单元素处理
                string simpleText = "";
                string txt = "";
                string tmpRoleName = string.Empty;
                try
                {
                    simpleText = childAcc.get_accName(childIndex);
                    txt = childAcc.get_accValue(childIndex);
                    object tmproleObj = provider.GetRole(childAcc);
                    int tmprole = (tmproleObj is int) ? (int)tmproleObj : Convert.ToInt32(tmproleObj);

                    tmpRoleName = MARSAccessibleProvider.GetRoleName(tmprole);
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
                    string locationInfo = MARSAccessibleProvider.GetLocationInfo(childAcc, childIndex);
                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取: |{tmpRoleName}|\"{simpleText}\"|Txt|{txt}|位置|{locationInfo}");
                    //sb.AppendLine($"{indentStr}  开始绘制红色闪烁框...");
                    // FlashRedBorder(childAcc, null, 3); // 已屏蔽flash功能
                    //sb.AppendLine($"{indentStr}  红色闪烁框绘制完成");
                }
                else
                {
                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取失败");
                }

                // 检查childAcc是否有子对象需要处理
                if (childAcc.accChildCount > 0)
                {
                    sb.AppendLine($"{indentStr}            方法3-检测到childAcc有{childAcc.accChildCount}个子对象，尝试获取...");
                    
                    try
                    {
                        // 尝试获取子对象
                        object[] children = new object[childAcc.accChildCount];
                        int obtained = MARSAccessibleProvider.AccessibleChildren(childAcc, 0, childAcc.accChildCount, children, out int nObtained);
                        
                        if (nObtained > 0)
                        {
                            sb.AppendLine($"{indentStr}            方法3-成功获取到{nObtained}个子对象");
                            
                            for (int i = 0; i < nObtained && i < 3; i++) // 限制最多显示3个子对象
                            {
                                if (children[i] is IAccessible subChildAcc)
                                {
                                    object subRoleObj = provider.GetRole(subChildAcc);
                                    int subRole = (subRoleObj is int) ? (int)subRoleObj : Convert.ToInt32(subRoleObj);
                                    string subRoleName = MARSAccessibleProvider.GetRoleName(subRole);
                                    string subText = "";
                                    try { subText = subChildAcc.get_accName(0); } catch { }
                                    if (string.IsNullOrEmpty(subText))
                                    {
                                        try { subText = subChildAcc.get_accValue(0); } catch { }
                                    }
                                    
                                    sb.AppendLine($"{indentStr}              子对象[{i}]: {subRoleName} - \"{subText}\"");
                                }
                                else if (children[i] is int subIndex)
                                {
                                    sb.AppendLine($"{indentStr}              子对象[{i}]: 整数索引 {subIndex}");
                                }
                                else
                                {
                                    sb.AppendLine($"{indentStr}              子对象[{i}]: 其他类型 {children[i]?.GetType()?.Name ?? "null"}");
                                }
                            }
                            
                            if (nObtained > 3)
                            {
                                sb.AppendLine($"{indentStr}              还有{nObtained - 3}个子对象未显示...");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"{indentStr}            方法3-无法通过AccessibleChildren获取子对象，尝试通过索引获取...");
                            
                            // 尝试通过索引逐个获取子对象
                            for (int idx = 0; idx < childAcc.accChildCount && idx < 3; idx++)
                            {
                                try
                                {
                                    object subChildObj = childAcc.get_accChild(idx);
                                    if (subChildObj != null)
                                    {
                                        sb.AppendLine($"{indentStr}              通过索引[{idx}]获取到子对象: {subChildObj.GetType().Name}");
                                        
                                        if (subChildObj is IAccessible subChildAcc)
                                        {
                                            object subRoleObj = provider.GetRole(subChildAcc);
                                            int subRole = (subRoleObj is int) ? (int)subRoleObj : Convert.ToInt32(subRoleObj);
                                            string subRoleName = MARSAccessibleProvider.GetRoleName(subRole);
                                            string subText = "";
                                            try { subText = subChildAcc.get_accName(0); } catch { }
                                            if (string.IsNullOrEmpty(subText))
                                            {
                                                try { subText = subChildAcc.get_accValue(0); } catch { }
                                            }
                                            
                                            sb.AppendLine($"{indentStr}                子对象[{idx}]: {subRoleName} - \"{subText}\"");
                                        }
                                        else if (subChildObj is int subIndex)
                                        {
                                            sb.AppendLine($"{indentStr}                子对象[{idx}]: 整数索引 {subIndex}");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"{indentStr}                子对象[{idx}]: 其他类型 {subChildObj.GetType().Name}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"{indentStr}              索引[{idx}]获取失败: {ex.Message}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indentStr}            方法3-处理子对象时发生异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{indentStr}            方法3-作为简单元素获取异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 方法3: 尝试通过索引作为简单元素处理（带遍历栈支持）
        /// </summary>
        private static void TryMethod3_GetAsSimpleElement(
            IAccessible childAcc, 
            int childIndex, 
            MARSAccessibleProvider provider, 
            string indentStr, 
            StringBuilder sb,
            Stack<TraversalNode> stack,
            int currentIndent,
            IntPtr currentHwnd,
            string currentParentPath)
        {
            try
            {
                // 尝试将索引作为简单元素处理
                string simpleText = "";
                string txt = "";
                string tmpRoleName = string.Empty;
                try
                {
                    simpleText = childAcc.get_accName(childIndex);
                    txt = childAcc.get_accValue(childIndex);
                    object tmproleObj = provider.GetRole(childAcc);
                    int tmprole = (tmproleObj is int) ? (int)tmproleObj : Convert.ToInt32(tmproleObj);

                    tmpRoleName = MARSAccessibleProvider.GetRoleName(tmprole);
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
                    string locationInfo = MARSAccessibleProvider.GetLocationInfo(childAcc, childIndex);
                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取: |{tmpRoleName}|\"{simpleText}\"|Txt|{txt}|位置|{locationInfo}");
                }
                else
                {
                    sb.AppendLine($"{indentStr}            方法3-作为简单元素获取失败");
                }

                // 检查childAcc是否有子对象需要处理
                if (childAcc.accChildCount > 0)
                {
                    sb.AppendLine($"{indentStr}            方法3-检测到childAcc有{childAcc.accChildCount}个子对象，添加到遍历栈...");
                    
                    try
                    {
                        // 尝试获取子对象
                        object[] children = new object[childAcc.accChildCount];
                        int obtained = MARSAccessibleProvider.AccessibleChildren(childAcc, 0, childAcc.accChildCount, children, out int nObtained);
                        
                        if (nObtained > 0)
                        {
                            sb.AppendLine($"{indentStr}            方法3-成功获取到{nObtained}个子对象，添加到栈中...");
                            
                            // 将子对象按相反顺序添加到栈中，以保持正确的遍历顺序
                            for (int i = nObtained - 1; i >= 0; i--)
                            {
                                if (children[i] is IAccessible subChildAcc)
                                {
                                    // 生成路径组件
                                    string currentPathComponent = MARSAccessibleProvider.GeneratePathComponent(childAcc, provider, currentHwnd, i);
                                    string subParentPath = string.IsNullOrEmpty(currentParentPath) ? currentPathComponent : $"{currentParentPath}/{currentPathComponent}";
                                    
                                    stack.Push(new TraversalNode
                                    {
                                        Accessible = subChildAcc,
                                        Indent = currentIndent + 1,
                                        Hwnd = currentHwnd,
                                        ParentPath = subParentPath,
                                        ChildIndex = i
                                    });
                                    
                                    sb.AppendLine($"{indentStr}              子对象[{i}]已添加到遍历栈");
                                }
                                else if (children[i] is int subIndex)
                                {
                                    sb.AppendLine($"{indentStr}              子对象[{i}]: 整数索引 {subIndex}");
                                }
                                else
                                {
                                    sb.AppendLine($"{indentStr}              子对象[{i}]: 其他类型 {children[i]?.GetType()?.Name ?? "null"}");
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine($"{indentStr}            方法3-无法通过AccessibleChildren获取子对象，尝试通过索引获取...");
                            
                            // 尝试通过索引逐个获取子对象并添加到栈中
                            for (int idx = childAcc.accChildCount - 1; idx >= 0 && idx >= childAcc.accChildCount - 3; idx--) // 限制最多3个
                            {
                                try
                                {
                                    object subChildObj = childAcc.get_accChild(idx);
                                    if (subChildObj != null && subChildObj is IAccessible subChildAcc)
                                    {
                                        // 生成路径组件
                                        string currentPathComponent = MARSAccessibleProvider.GeneratePathComponent(childAcc, provider, 
                                            currentHwnd, idx);
                                        string subParentPath = string.IsNullOrEmpty(currentParentPath) ? currentPathComponent : $"{currentParentPath}/{currentPathComponent}";
                                        
                                        stack.Push(new TraversalNode
                                        {
                                            Accessible = subChildAcc,
                                            Indent = currentIndent + 1,
                                            Hwnd = currentHwnd,
                                            ParentPath = subParentPath,
                                            ChildIndex = idx
                                        });
                                        
                                        sb.AppendLine($"{indentStr}              通过索引[{idx}]获取的子对象已添加到遍历栈");
                                    }
                                    else if (subChildObj is int subIndex)
                                    {
                                        sb.AppendLine($"{indentStr}              子对象[{idx}]: 整数索引 {subIndex}");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"{indentStr}              子对象[{idx}]: 其他类型 {subChildObj?.GetType()?.Name ?? "null"}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"{indentStr}              索引[{idx}]获取失败: {ex.Message}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indentStr}            方法3-处理子对象时发生异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{indentStr}            方法3-作为简单元素获取异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理COM对象的转换
        /// </summary>
        /// <param name="comObject">COM对象</param>
        /// <param name="provider">可访问性提供者</param>
        /// <param name="indentStr">缩进字符串</param>
        /// <param name="sb">字符串构建器</param>
        public static void HandleComObjectConversion(
            object comObject, 
            MARSAccessibleProvider provider, 
            string indentStr, 
            StringBuilder sb)
        {
            sb.AppendLine($"{indentStr}          COM对象，尝试转换...");
            IntPtr unk = Marshal.GetIUnknownForObject(comObject);
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
                    string locationInfo3 = MARSAccessibleProvider.GetLocationInfo(convertedCellAcc);
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

        /// <summary>
        /// 处理IAccessible单元格的详细信息
        /// </summary>
        /// <param name="cellAcc">单元格可访问性对象</param>
        /// <param name="provider">可访问性提供者</param>
        /// <param name="currentHwnd">当前窗口句柄</param>
        /// <param name="currentParentPath">当前父路径</param>
        /// <param name="cellIndex">单元格索引</param>
        /// <param name="indentStr">缩进字符串</param>
        /// <param name="sb">字符串构建器</param>
        public static void HandleAccessibleCell(
            IAccessible cellAcc, 
            MARSAccessibleProvider provider, 
            IntPtr currentHwnd, 
            string currentParentPath, 
            int cellIndex, 
            string indentStr, 
            StringBuilder sb)
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
            string cellAttachText = GenerateUFTAttachText(cellAcc, provider, currentHwnd);
            string cellAttachPath = GenerateUFTAttachPath(cellAcc, provider, currentHwnd, currentParentPath, cellIndex);
            sb.AppendLine($"{indentStr}          IAccessible单元格: {cellRoleName} - \"{cellText}\"|位置|{locationInfo}");
            if (!string.IsNullOrEmpty(cellAttachText))
            {
                sb.AppendLine($"{indentStr}            Cell AttachText: {cellAttachText}");
            }
            if (!string.IsNullOrEmpty(cellAttachPath))
            {
                sb.AppendLine($"{indentStr}            Cell AttachPath: {cellAttachPath}");
            }
        }

        /// <summary>
        /// 获取位置信息
        /// </summary>
        private static string GetLocationInfo(IAccessible acc)
        {
            try
            {
                acc.accLocation(out int left, out int top, out int width, out int height, 0);
                return $"({left},{top},{width},{height})";
            }
            catch
            {
                return "(位置获取失败)";
            }
        }

        /// <summary>
        /// 生成UFT AttachText
        /// </summary>
        private static string GenerateUFTAttachText(IAccessible acc, MARSAccessibleProvider provider, IntPtr hwnd)
        {
            try
            {
                object roleObj = provider.GetRole(acc);
                int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                string roleName = MARSAccessibleProvider.GetRoleName(role);
                string text = "";
                try { text = acc.get_accName(0); } catch { }
                if (string.IsNullOrEmpty(text))
                {
                    try { text = acc.get_accValue(0); } catch { }
                }
                return $"{roleName}[\"{text}\"]";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 生成UFT AttachPath
        /// </summary>
        private static string GenerateUFTAttachPath(IAccessible acc, MARSAccessibleProvider provider, IntPtr hwnd, string parentPath, int index)
        {
            try
            {
                object roleObj = provider.GetRole(acc);
                int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                string roleName = MARSAccessibleProvider.GetRoleName(role);
                string text = "";
                try { text = acc.get_accName(0); } catch { }
                if (string.IsNullOrEmpty(text))
                {
                    try { text = acc.get_accValue(0); } catch { }
                }
                string pathComponent = $"{roleName}[\"{text}\"]";
                return string.IsNullOrEmpty(parentPath) ? pathComponent : $"{parentPath}/{pathComponent}";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 从指定进程名称和屏幕坐标获取初始的可访问性对象树
        /// </summary>
        /// <param name="strProcessName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static List<IAccessible> GetInitAccessibleObjectTreeFromPosition(string strProcessName,
            int x, int y, ref IAccessible targetObject, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetInitAccessibleObjectTreeFromPosition", 
                $"{iMark}|{strProcessName}|{x}-{y}");
            try
            {
                isOk = false;
                targetObject = null;

                // 1) 直接通过 AccessibleObjectFromPoint 获取 IAccessible
                IAccessible accFromPoint = null;
                object varChild;
                var pt = new MSAA_POINT { x = x, y = y };

                int hr = AccessibleObjectFromPoint(pt, out accFromPoint, out varChild);
                if (hr != 0 || accFromPoint == null)
                {
                    strError = $"AccessibleObjectFromPoint failed at ({x},{y}), hr={hr}";
                    MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|{strError}");
                    return null;
                }  

                // 2) 解析 child 返回值，定位最精确的 IAccessible
                IAccessible locatedAcc = accFromPoint;
                try
                {
                    if (varChild is IAccessible childAcc)
                    {
                        locatedAcc = childAcc;
                        MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Found IAccessible child directly");
                    }
                    else if (varChild is int childId && childId != 0)
                    {
                        try
                        {
                            object child = accFromPoint.get_accChild(childId);
                            if (child is IAccessible idxAcc)
                            {
                                locatedAcc = idxAcc;
                                MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Found IAccessible child by ID {childId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error getting child by ID {childId}: {ex.Message}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error processing child object: {ex.Message}", ex);
                }

                targetObject = locatedAcc;

                // 3) 计算本进程目标窗口的根 IAccessible 作为上溯边界（避免越过到系统 Desktop）
                IAccessible topWindowAccBoundary = null;
                IntPtr topHwnd = IntPtr.Zero;
                try
                {
                    // 用 WindowFromPoint 获取窗口，随后求其根窗口
                    var hwndAtPoint = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                    MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|WindowFromPoint result: 0x{hwndAtPoint.ToInt64():X}");
                    
                    if (hwndAtPoint != IntPtr.Zero)
                    {
                        topHwnd = MarsWindowsAPIs.GetAncestor(hwndAtPoint, MarsWindowsAPIs.GetAncestorFlags.GetRoot);
                        if (topHwnd == IntPtr.Zero) topHwnd = hwndAtPoint;
                        MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Top window HWND: 0x{topHwnd.ToInt64():X}");
                        
                        var providerForBoundary = new MARSAccessibleProvider();
                        topWindowAccBoundary = providerForBoundary.GetAccessibleObject(topHwnd) as IAccessible;
                        if (topWindowAccBoundary != null)
                        {
                            MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Successfully got top window accessible object");
                        }
                        else
                        {
                            MarsLoggerSimple.Warning("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Failed to get accessible object for top window 0x{topHwnd.ToInt64():X}");
                        }
                    }
                    else
                    {
                        MarsLoggerSimple.Warning("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|No window found at point ({x},{y})");
                    }
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error getting top window boundary: {ex.Message}", ex);
                }

                // 4) 逐层向上查找 parent，构建从本进程顶层窗口到 target 的链
                var ancestorStack = new Stack<IAccessible>();
                IAccessible cursor = locatedAcc;
                int guard = 0;
                while (cursor != null && guard++ < 256)
                {
                    // 若当前节点所属的根窗口不是 topHwnd，则停止（防止爬到 Desktop 或其它进程）
                    if (!BelongsToRootWindow(cursor, topHwnd))
                        break;

                    ancestorStack.Push(cursor);

                    // Desktop 防守
                    bool reachedBoundary = false;
                    try
                    {
                        object roleObj = cursor.get_accRole(0);
                        int role = (roleObj is int) ? (int)roleObj : Convert.ToInt32(roleObj);
                        string roleName = MARSAccessibleProvider.GetRoleName(role) ?? string.Empty;
                        string name = SafeGet(() => cursor.get_accName(0));
                        MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Checking boundary - Role: {roleName}, Name: {name}");
                        
                        if (roleName.Equals("desktop", StringComparison.OrdinalIgnoreCase) || name.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                        {
                            reachedBoundary = true;
                            MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Reached desktop boundary, stopping traversal");
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error checking desktop boundary: {ex.Message}", ex);
                    }
                    if (reachedBoundary) break;

                    try
                    {
                        object parentObj = cursor.accParent;
                        var parentAcc = parentObj as IAccessible;
                        if (parentAcc == null)
                        {
                            MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Parent is null, stopping traversal at level {guard}");
                            break;
                        }

                        // 如果父不属于同一个根窗口，则不再上溯
                        if (!BelongsToRootWindow(parentAcc, topHwnd))
                        {
                            MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Parent belongs to different root window, stopping traversal at level {guard}");
                            break;
                        }

                        cursor = parentAcc;
                        MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Moved to parent at level {guard}");
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error getting parent at level {guard}: {ex.Message}", ex);
                        break;
                    }
                }

                // 5) 将链条转换为列表（root -> ... -> target）
                var pathList = new List<IAccessible>(ancestorStack);

                // 6) 可选：构建 MarsSpiedObjectInfo 的链式树（root 作为起点，单链 children）
                try
                {
                    MarsSpiedObjectInfo rootInfo = null;
                    MarsSpiedObjectInfo prev = null;
                    int nodeIndex = 0;
                    foreach (var acc in pathList)
                    {
                        // 获取位置信息用于零尺寸检查
                        int left = 0, top = 0, width = 0, height = 0;
                        try { acc.accLocation(out left, out top, out width, out height, 0); } catch { }
                        
                        // 检查是否应该跳过零尺寸对象
                        if (!ShowZeroSizeObjects && width == 0 && height == 0)
                        {
                            MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Skipping zero-size object at node {nodeIndex}: Name={SafeGet(() => acc.get_accName(0))}");
                            continue;
                        }
                        
                        var node = new MarsSpiedObjectInfo
                        {
                            referenceToObj = acc,
                            objectName = SafeGet(() => acc.get_accName(0)),
                            Text = SafeGet(() => acc.get_accValue(0)),
                            x = left,
                            y = top,
                            w = width,
                            h = height,
                            isVisible = width > 0 && height > 0,
                            children = new List<MarsSpiedObjectInfo>()
                        };
                        MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Building node {nodeIndex}: Name={node.objectName}, Text={node.Text}, Size={width}x{height}");
                        
                        if (rootInfo == null) rootInfo = node;
                        if (prev != null)
                        {
                            prev.children.Add(node);
                        }
                        prev = node;
                        nodeIndex++;
                    }
                    MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Built {nodeIndex} MarsSpiedObjectInfo nodes");
                    // 这里不返回 rootInfo，因为方法签名要求返回 List<IAccessible>
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Error building MarsSpiedObjectInfo tree: {ex.Message}", ex);
                }

                isOk = pathList.Count > 0;
                if (!isOk)
                {
                    strError = "No accessible path constructed";
                    MarsLoggerSimple.Warning("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|No accessible path constructed");
                    return null;
                }
                
                MarsLoggerSimple.Info("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Successfully constructed path with {pathList.Count} elements");
                return pathList;
            }
            catch (Exception ex)
            {
                isOk = false;
                strError = $"Exception in GetInitAccessibleObjectTreeFromPosition: {ex.Message}";
                MarsLoggerSimple.Error("GetInitAccessibleObjectTreeFromPosition", $"{iMark}|Main exception: {ex.Message}", ex);
                return null;
            }
        }

        private static string SafeGet(Func<string> getter)
        {
            try { return getter() ?? string.Empty; } 
            catch (Exception ex) 
            { 
                MarsLoggerSimple.Error("SafeGet", $"Error in SafeGet: {ex.Message}", ex);
                return string.Empty; 
            }
        }

        /// <summary>
        /// 测试Helper类的功能
        /// </summary>
        /// <param name="provider">可访问性提供者</param>
        /// <returns>测试结果字符串</returns>
        public static string TestHelperFunctionality(MARSAccessibleProvider provider)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== MARSAccessibleHelper 功能测试 ===");
            sb.AppendLine("Helper类已成功创建并包含以下功能：");
            sb.AppendLine("1. HandleIntegerIndexCellAccess - 处理整数索引的单元格获取");
            sb.AppendLine("2. HandleComObjectConversion - 处理COM对象转换");
            sb.AppendLine("3. HandleAccessibleCell - 处理IAccessible单元格的详细信息");
            sb.AppendLine("4. TryMethod1_GetChildByIndex - 方法1：通过索引获取子对象");
            sb.AppendLine("5. TryMethod2_GetThroughParent - 方法2：通过父对象获取");
            sb.AppendLine("6. TryMethod3_GetAsSimpleElement - 方法3：作为简单元素处理");
            sb.AppendLine();
            sb.AppendLine("Helper类已成功从MARSAccessibleProvider中提取复杂逻辑，");
            sb.AppendLine("提高了代码的可维护性和可读性。");
            
            return sb.ToString();
        }
    }
}

