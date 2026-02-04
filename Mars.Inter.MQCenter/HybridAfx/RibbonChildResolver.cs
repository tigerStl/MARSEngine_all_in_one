using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows;
using Accessibility;
using Mars.Inter.MQCenter.MSAASupport;
using System.IO;
using System.Runtime.Remoting;
using Mars.message.Inter.MQCenter.simpleLog;

namespace MarsUnitTest.HybridAfx
{
    /// <summary>
    /// Ribbon子对象解析器 - 智能枚举AFX Ribbon控件的子元素
    /// 支持UIA和MSAA双重技术栈，确保最大兼容性
    /// </summary>
    public static class RibbonChildResolver
    {
        #region 常量定义

        /// <summary>
        /// UIA Patterns（老托管缺静态字段时用 LookupById 兜底）
        /// </summary>
        private static readonly AutomationPattern P_Invoke = InvokePattern.Pattern ?? AutomationPattern.LookupById(10000);
        private static readonly AutomationPattern P_SelItem = SelectionItemPattern.Pattern ?? AutomationPattern.LookupById(10010);
        private static readonly AutomationPattern P_ExpColl = ExpandCollapsePattern.Pattern ?? AutomationPattern.LookupById(10005);
        private static readonly AutomationPattern P_ScrollItem = ScrollItemPattern.Pattern ?? AutomationPattern.LookupById(10017);

        /// <summary>
        /// MSAA相关常量
        /// </summary>
        private static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
        private const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);

        #endregion

        #region 数据结构

        /// <summary>
        /// 节点信息 - 统一的UI元素描述
        /// </summary>
        public sealed class NodeInfo
        {
            /// <summary>数据来源：UIA-Control, UIA-Raw, MSAA</summary>
            public string Source { get; set; }

            /// <summary>元素名称</summary>
            public string Name { get; set; }

            public string Value { get; set; }

            /// <summary>控件类型</summary>
            public string ControlType { get; set; }

            /// <summary>类名</summary>
            public string ClassName { get; set; }

            /// <summary>自动化ID</summary>
            public string AutomationId { get; set; }

            /// <summary>边界矩形</summary>
            public Rect Rect { get; set; }

            /// <summary>窗口句柄</summary>
            public int Hwnd { get; set; }

            /// <summary>角色名称（MSAA时使用，UIA时留空）</summary>
            public string RoleName { get; set; }

            /// <summary>子节点列表</summary>
            public List<NodeInfo> Children { get; set; }

            /// <summary>
            /// 构造函数
            /// </summary>
            public NodeInfo()
            {
                Children = new List<NodeInfo>();
            }

            /// <summary>
            /// 转换为字符串表示
            /// </summary>
            public override string ToString()
            {
                return $"[{Source}] CT='{ControlType}' Name='{Name}' Class='{ClassName}' " +
                       $"AutoId='{AutomationId}' Hwnd=0x{Hwnd:X} " +
                       $"Rect=[{Rect.Left:0},{Rect.Top:0},{Rect.Width:0}x{Rect.Height:0}] " +
                       $"Role='{RoleName}'";
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 从窗口句柄智能枚举Ribbon子对象
        /// </summary>
        /// <param name="hwndRibbon">Ribbon窗口句柄</param>
        /// <param name="includeOffscreen">是否包含屏幕外元素</param>
        /// <returns>子对象列表</returns>
        public static List<NodeInfo> EnumRibbonChildrenSmart(IntPtr hwndRibbon, bool includeOffscreen = true)
        {
            var list = new List<NodeInfo>();

            AutomationElement ribbon;
            try
            {
                ribbon = AutomationElement.FromHandle(hwndRibbon);
            }
            catch
            {
                return list;
            }

            // 尝试先"激活/展开"一下Ribbon，很多Afx需要这样才能暴露子项
            TryMakeRibbonShowChildren(ribbon);

            // 1) UIA Control View
            var kids = GetChildrenOnce(ribbon, TreeWalker.ControlViewWalker, includeOffscreen);
            foreach (var k in kids)
            {
                list.Add(ToNodeInfo(k, "UIA-Control"));
            }

            // 2) 若为空，再Raw View
            if (list.Count == 0)
            {
                kids = GetChildrenOnce(ribbon, TreeWalker.RawViewWalker, includeOffscreen);
                foreach (var k in kids)
                {
                    list.Add(ToNodeInfo(k, "UIA-Raw"));
                }
            }

            // 3) 若仍为空，用MSAA兜底（很多Afx Ribbon只在MSAA暴露虚拟子项）
            if (list.Count == 0)
            {
                list.AddRange(EnumMsaaChildren(hwndRibbon));
            }

            return list;
        }

        /// <summary>
        /// 从屏幕位置智能枚举Ribbon子对象
        /// </summary>
        /// <param name="x">屏幕X坐标</param>
        /// <param name="y">屏幕Y坐标</param>
        /// <param name="includeOffscreen">是否包含屏幕外元素</param>
        /// <returns>子对象列表</returns>
        public static List<NodeInfo> EnumRibbonChildrenSmartFromPoint(int x, int y, bool includeOffscreen = true)
        {
            var list = new List<NodeInfo>();
            AutomationElement hit = null;

            try
            {
                hit = AutomationElement.FromPoint(new Point(x, y));
            }
            catch { }

            if (hit == null)
            {
                return list;
            }

            // 找到最近的Afx Ribbon容器（往上爬，直到ClassName以"Afx:RibbonBar"或"Afx"开头）
            var parent = hit;
            for (var p = TreeWalker.RawViewWalker.GetParent(hit); p != null; p = TreeWalker.RawViewWalker.GetParent(p))
            {
                var cls = Safe(() => p.Current.ClassName) ?? "";
                if (cls.StartsWith("Afx:RibbonBar", StringComparison.OrdinalIgnoreCase) ||
                    cls.StartsWith("Afx", StringComparison.OrdinalIgnoreCase))
                {
                    parent = p;
                    break;
                }
                parent = p;
            }

            int hwnd = Safe(() => parent.Current.NativeWindowHandle);
            if (hwnd != 0)
            {
                return EnumRibbonChildrenSmart(new IntPtr(hwnd), includeOffscreen);
            }

            // 没拿到hwnd，就直接对parent做同样逻辑
            TryMakeRibbonShowChildren(parent);

            var kids = GetChildrenOnce(parent, TreeWalker.ControlViewWalker, includeOffscreen);
            foreach (var k in kids)
            {
                list.Add(ToNodeInfo(k, "UIA-Control"));
            }

            if (list.Count == 0)
            {
                kids = GetChildrenOnce(parent, TreeWalker.RawViewWalker, includeOffscreen);
                foreach (var k in kids)
                {
                    list.Add(ToNodeInfo(k, "UIA-Raw"));
                }
            }

            return list;
        }

        #endregion

        #region 文件输出方法

        /// <summary>
        /// 将NodeInfo列表保存到文件（树状结构）
        /// </summary>
        /// <param name="nodes">NodeInfo列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">文件编码，默认为UTF8</param>
        public static void SaveToFile(List<NodeInfo> nodes, string filePath, Encoding encoding = null)
        {
            if (nodes == null || nodes.Count == 0)
            {
                File.WriteAllText(filePath, "没有找到任何节点。", encoding ?? Encoding.UTF8);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Ribbon子对象树状结构 - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            sb.AppendLine();

            foreach (var node in nodes)
            {
                WriteNodeToFile(sb, node, 0);
            }

            sb.AppendLine();
            sb.AppendLine("=== 遍历完成 ===");

            File.WriteAllText(filePath, sb.ToString(), encoding ?? Encoding.UTF8);
        }

        /// <summary>
        /// 将单个NodeInfo及其子节点写入StringBuilder（递归）
        /// </summary>
        /// <param name="sb">StringBuilder对象</param>
        /// <param name="node">节点</param>
        /// <param name="depth">深度（用于缩进）</param>
        private static void WriteNodeToFile(StringBuilder sb, NodeInfo node, int depth)
        {
            string indent = new string(' ', depth * 2);
            string connector = depth == 0 ? "└─ " : "├─ ";
            
            sb.AppendLine($"{indent}{connector}[{node.Source}] {node.Name}");
            sb.AppendLine($"{indent}    ├─ 控件类型: {node.ControlType}");
            sb.AppendLine($"{indent}    ├─ 类名: {node.ClassName}");
            sb.AppendLine($"{indent}    ├─ 自动化ID: {node.AutomationId}");
            sb.AppendLine($"{indent}    ├─ 值: {node.Value}");
            sb.AppendLine($"{indent}    ├─ 窗口句柄: 0x{node.Hwnd:X}");
            sb.AppendLine($"{indent}    ├─ 位置: [{node.Rect.Left:0},{node.Rect.Top:0},{node.Rect.Width:0}x{node.Rect.Height:0}]");
            sb.AppendLine($"{indent}    ├─ 角色: {node.RoleName}");
            sb.AppendLine($"{indent}    └─ 子节点数量: {node.Children.Count}");

            // 递归写入子节点
            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                bool isLast = (i == node.Children.Count - 1);
                string childIndent = indent + (isLast ? "    " : "│   ");
                
                sb.AppendLine($"{childIndent}└─ [{child.Source}] {child.Name}");
                sb.AppendLine($"{childIndent}    ├─ 控件类型: {child.ControlType}");
                sb.AppendLine($"{childIndent}    ├─ 类名: {child.ClassName}");
                sb.AppendLine($"{childIndent}    ├─ 自动化ID: {child.AutomationId}");
                sb.AppendLine($"{childIndent}    ├─ 值: {child.Value}");
                sb.AppendLine($"{childIndent}    ├─ 窗口句柄: 0x{child.Hwnd:X}");
                sb.AppendLine($"{childIndent}    ├─ 位置: [{child.Rect.Left:0},{child.Rect.Top:0},{child.Rect.Width:0}x{child.Rect.Height:0}]");
                sb.AppendLine($"{childIndent}    ├─ 角色: {child.RoleName}");
                sb.AppendLine($"{childIndent}    └─ 子节点数量: {child.Children.Count}");

                // 递归处理子节点的子节点
                WriteChildrenToFile(sb, child, depth + 2);
            }
        }

        /// <summary>
        /// 递归写入子节点到StringBuilder
        /// </summary>
        /// <param name="sb">StringBuilder对象</param>
        /// <param name="parentNode">父节点</param>
        /// <param name="depth">深度</param>
        private static void WriteChildrenToFile(StringBuilder sb, NodeInfo parentNode, int depth)
        {
            for (int i = 0; i < parentNode.Children.Count; i++)
            {
                var child = parentNode.Children[i];
                bool isLast = (i == parentNode.Children.Count - 1);
                string indent = new string(' ', depth * 2);
                string connector = isLast ? "└─ " : "├─ ";
                
                sb.AppendLine($"{indent}{connector}[{child.Source}] {child.Name}");
                sb.AppendLine($"{indent}    ├─ 控件类型: {child.ControlType}");
                sb.AppendLine($"{indent}    ├─ 类名: {child.ClassName}");
                sb.AppendLine($"{indent}    ├─ 自动化ID: {child.AutomationId}");
                sb.AppendLine($"{indent}    ├─ 值: {child.Value}");
                sb.AppendLine($"{indent}    ├─ 窗口句柄: 0x{child.Hwnd:X}");
                sb.AppendLine($"{indent}    ├─ 位置: [{child.Rect.Left:0},{child.Rect.Top:0},{child.Rect.Width:0}x{child.Rect.Height:0}]");
                sb.AppendLine($"{indent}    ├─ 角色: {child.RoleName}");
                sb.AppendLine($"{indent}    └─ 子节点数量: {child.Children.Count}");

                // 递归处理更深层的子节点
                if (child.Children.Count > 0)
                {
                    WriteChildrenToFile(sb, child, depth + 1);
                }
            }
        }

        /// <summary>
        /// 将NodeInfo列表保存到文件（简化版树状结构）
        /// </summary>
        /// <param name="nodes">NodeInfo列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">文件编码，默认为UTF8</param>
        public static void SaveToFileSimple(List<NodeInfo> nodes, string filePath, Encoding encoding = null)
        {
            if (nodes == null || nodes.Count == 0)
            {
                File.WriteAllText(filePath, "没有找到任何节点。", encoding ?? Encoding.UTF8);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== Ribbon子对象树状结构 - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            sb.AppendLine();

            foreach (var node in nodes)
            {
                WriteNodeSimple(sb, node, 0);
            }

            sb.AppendLine();
            sb.AppendLine("=== 遍历完成 ===");

            File.WriteAllText(filePath, sb.ToString(), encoding ?? Encoding.UTF8);
        }

        /// <summary>
        /// 简化版节点写入（递归）
        /// </summary>
        /// <param name="sb">StringBuilder对象</param>
        /// <param name="node">节点</param>
        /// <param name="depth">深度</param>
        private static void WriteNodeSimple(StringBuilder sb, NodeInfo node, int depth)
        {
            string indent = new string(' ', depth * 2);
            string connector = depth == 0 ? "└─ " : "├─ ";
            
            sb.AppendLine($"{indent}{connector}[{node.Source}]nodeName| {node.Name}|type|({node.ControlType})|role|{node.RoleName}|value|{node.Value}");
            
            // 如果有值，显示值
            if (!string.IsNullOrEmpty(node.Value))
            {
                sb.AppendLine($"{indent}    └─ 值: {node.Value}");
            }

            // 递归写入子节点
            foreach (var child in node.Children)
            {
                WriteNodeSimple(sb, child, depth + 1);
            }
        }

        /// <summary>
        /// 使用示例：扫描Ribbon并保存到文件
        /// </summary>
        /// <param name="hwnd">Ribbon窗口句柄</param>
        /// <param name="outputPath">输出文件路径，如果为null则使用默认路径</param>
        /// <returns>是否成功</returns>
        public static bool ScanAndSaveToFile(IntPtr hwnd, string outputPath = null)
        {
            try
            {
                // 扫描Ribbon子对象
                var nodes = EnumRibbonChildrenSmart(hwnd, true);
                
                if (nodes == null || nodes.Count == 0)
                {
                    Console.WriteLine("未找到任何Ribbon子对象。");
                    return false;
                }

                // 生成默认文件路径
                if (string.IsNullOrEmpty(outputPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    outputPath = Path.Combine("c:\\temp\\", $"RibbonTree_{timestamp}.txt");
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 保存详细版
                SaveToFile(nodes, outputPath);
                Console.WriteLine($"详细版树状结构已保存到: {outputPath}");

                // 保存简化版
                string simplePath = outputPath.Replace(".txt", "_Simple.txt");
                SaveToFileSimple(nodes, simplePath);
                Console.WriteLine($"简化版树状结构已保存到: {simplePath}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描和保存过程中发生错误: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 尝试让Ribbon"显露"子节点：选中Tab、展开分组、滚动到可见
        /// </summary>
        /// <param name="ribbonRoot">Ribbon根元素</param>
        private static void TryMakeRibbonShowChildren(AutomationElement ribbonRoot)
        {
            // 选中第一个Tab（ControlType.TabItem或MenuItem；有些实现把页签做成MenuItem）
            var tab = FindDescendant(ribbonRoot, e =>
            {
                var ct = Safe(() => e.Current.ControlType);
                if (ct == null) return false;
                return ct == ControlType.TabItem || ct == ControlType.MenuItem;
            }, preferControl: true) ?? FindDescendant(ribbonRoot, e =>
            {
                var ct = Safe(() => e.Current.ControlType);
                return ct == ControlType.TabItem || ct == ControlType.MenuItem;
            }, preferControl: false);

            if (tab != null)
            {
                if (P_SelItem != null && tab.TryGetCurrentPattern(P_SelItem, out var p) && p is SelectionItemPattern si)
                {
                    Safe(() => { si.Select(); return true; });
                }

                if (P_ExpColl != null && tab.TryGetCurrentPattern(P_ExpColl, out p) && p is ExpandCollapsePattern ec)
                {
                    var st = Safe(() => ec.Current.ExpandCollapseState);
                    if (st == ExpandCollapseState.Collapsed || st == ExpandCollapseState.PartiallyExpanded)
                    {
                        Safe(() => { ec.Expand(); return true; });
                    }
                }
            }

            // 有的Ribbon区域要滚动才出现项
            if (P_ScrollItem != null)
            {
                var anyItem = FindDescendant(ribbonRoot, e => e.TryGetCurrentPattern(P_ScrollItem, out _), preferControl: true)
                              ?? FindDescendant(ribbonRoot, e => e.TryGetCurrentPattern(P_ScrollItem, out _), preferControl: false);

                if (anyItem != null && anyItem.TryGetCurrentPattern(P_ScrollItem, out var sp) && sp is ScrollItemPattern sip)
                {
                    Safe(() => { sip.ScrollIntoView(); return true; });
                }
            }

            Thread.Sleep(50); // 给UIA一点时间刷新结构
        }

        /// <summary>
        /// 获取UIA一层子项
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="walker">树遍历器</param>
        /// <param name="includeOffscreen">是否包含屏幕外元素</param>
        /// <returns>子元素列表</returns>
        private static List<AutomationElement> GetChildrenOnce(AutomationElement parent, TreeWalker walker, bool includeOffscreen)
        {
            var list = new List<AutomationElement>();
            for (var c = Safe(() => walker.GetFirstChild(parent)); c != null; c = Safe(() => walker.GetNextSibling(c)))
            {
                if (!includeOffscreen && Safe(() => c.Current.IsOffscreen))
                {
                    continue;
                }
                list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// UIA广度优先查找
        /// </summary>
        /// <param name="root">根元素</param>
        /// <param name="match">匹配条件</param>
        /// <param name="preferControl">是否优先使用ControlView</param>
        /// <returns>找到的元素，未找到返回null</returns>
        private static AutomationElement FindDescendant(AutomationElement root, Func<AutomationElement, bool> match, bool preferControl)
        {
            var q = new Queue<AutomationElement>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var e = q.Dequeue();
                if (match(e))
                {
                    return e;
                }

                var kids = GetChildrenOnce(e, preferControl ? TreeWalker.ControlViewWalker : TreeWalker.RawViewWalker, includeOffscreen: true);
                if (kids.Count == 0)
                {
                    kids = GetChildrenOnce(e, preferControl ? TreeWalker.RawViewWalker : TreeWalker.ControlViewWalker, includeOffscreen: true);
                }

                foreach (var k in kids)
                {
                    q.Enqueue(k);
                }
            }
            return null;
        }

        /// <summary>
        /// 把UIA元素转成NodeInfo
        /// </summary>
        /// <param name="e">UIA元素</param>
        /// <param name="sourceTag">来源标签</param>
        /// <returns>NodeInfo对象</returns>
        private static NodeInfo ToNodeInfo(AutomationElement e, string sourceTag)
        {
            return new NodeInfo
            {
                Source = sourceTag,
                ControlType = Safe(() => e.Current.ControlType?.ProgrammaticName) ?? "ControlType.?",
                Name = Safe(() => e.Current.Name) ?? "",
                ClassName = Safe(() => e.Current.ClassName) ?? "",
                AutomationId = Safe(() => e.Current.AutomationId) ?? "",
                Rect = Safe(() => e.Current.BoundingRectangle),
                Hwnd = Safe(() => e.Current.NativeWindowHandle),
                RoleName = "" // UIA路径下留空
                // Children列表已在构造函数中初始化
            };
        }

        /// <summary>
        /// 安全执行函数，捕获所有异常
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="f">要执行的函数</param>
        /// <returns>执行结果或默认值</returns>
        private static T Safe<T>(Func<T> f)
        {
            try
            {
                return f();
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region MSAA支持

        /// <summary>
        /// Windows API声明
        /// </summary>
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, int dwId, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);


        [DllImport("oleacc.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetRoleTextW(int role, StringBuilder sb, uint cch);

        /// <summary>
        /// 使用MSAA枚举子对象（兜底方案）- 支持多层遍历（非递归）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>子对象列表</returns>
        private static List<NodeInfo> EnumMsaaChildren(IntPtr hwnd)
        {
            var rootList = new List<NodeInfo>();

            try
            {
                if (AccessibleObjectFromWindow(hwnd, OBJID_CLIENT, ref IID_IAccessible, out var obj) < 0 || obj is not IAccessible acc)
                {
                    return rootList;
                }

                // 创建根节点
                var rootNode = CreateNodeInfoFromMsaa(acc, null, hwnd.ToInt32());
                //rootList.Add(rootNode);

                // 使用队列进行广度优先遍历（非递归）
                var queue = new Queue<(IAccessible accessible, object childId, NodeInfo parentNode)>();
                queue.Enqueue((acc, null, null));
                bool isFirstNode = true;
                #region 测试
                /* 
                // 将根节点的直接子节点加入队列
                int count = acc.accChildCount;
                if (count > 0)
                {
                    var buf = new object[count];
                    int obtained = MARSAccessibleProvider.AccessibleChildren(acc, 0, count, buf, out int got);
                    if (got>0)
                    {
                        for (int i = 0; i < got; i++)
                        {
                            IAccessible childAcc = buf[i] as IAccessible;
                            object childId = null;
                            
                            if (childAcc == null && buf[i] is int childIndex)
                            {
                                // 如果是int，需要从parent调用get_accChild
                                childId = childIndex;
                                childAcc = acc; // 使用根对象作为父对象
                                object childByIndex = acc.get_accChild(childIndex);
                                if (childByIndex is IAccessible realAcc)
                                {
                                    childAcc = realAcc;
                                    childId = null; // 已经拿到真实对象，ID不再需要
                                    queue.Enqueue((childAcc, childId, rootNode));
                                }                               
                            }else if (childAcc != null)
                            {
                                queue.Enqueue((childAcc, childId, rootNode));
                            }
                    }
                }
                */

                #endregion
                // 处理队列中的所有节点
                while (queue.Count > 0)
                {
                    var (currentAcc, currentChildId, parentNode) = queue.Dequeue();

                    try
                    {
                        // 创建当前节点
                        var currentNode = CreateNodeInfoFromMsaa(currentAcc, currentChildId, hwnd.ToInt32());
                        if (parentNode != null)
                            parentNode.Children.Add(currentNode);
                        else if (isFirstNode)
                        {
                            rootList.Add(currentNode);
                            isFirstNode = false;
                        }

                        // 检查当前节点是否有子节点
                        int childCount = 0;
                        try
                        {
                            childCount = currentAcc.accChildCount;
                        }
                        catch(Exception e) {
                            MarsLoggerSimple.Error("EnumMsaaChildren", $"exception from currentAcc.accChildCount|{e.Message}|{e.StackTrace}");
                        }

                        if (childCount > 0)
                        {
                            var childBuf = new object[childCount];
                            int nGet = MARSAccessibleProvider.AccessibleChildren(currentAcc, 0, childCount, childBuf, out int childGot);
                            if (childGot>0)
                            {
                                // 将子节点加入队列
                                for (int i = 0; i < childGot; i++)
                                {
                                    IAccessible grandChildAcc = childBuf[i] as IAccessible;
                                    object grandChildId = null;
                                    
                                    if (grandChildAcc == null && childBuf[i] is int childIndex)
                                    {
                                        // 如果是int，需要从parent调用get_accChild
                                        grandChildId = childIndex;
                                        var subChildByIndex = currentAcc.get_accChild(childIndex);
                                        grandChildAcc = subChildByIndex as IAccessible;
                                        if (grandChildAcc == null)
                                        {
                                            // 仍然不是IAccessible，跳过
                                            MarsLoggerSimple.Warning("EnumMsaaChildren", $"index|{childIndex}| is not an accessibile object");
                                            continue;
                                        }
                                    }
                                    else if (childBuf[i] is not null)
                                    {
                                        MarsLoggerSimple.Info("EnumMsaaChildren", $"Not int, IAccessible|{childBuf[i]}|"); 
                                    }
                                    queue.Enqueue((grandChildAcc, 0, currentNode));
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        MarsLoggerSimple.Warning("EnumMsaaChildren", $"Error when pharse the node|{e.Message}|{e.StackTrace}");
                        // 忽略单个节点的错误，继续处理其他节点
                        continue;
                    }
                }
            }
            catch  (Exception e)
            {
                MarsLoggerSimple.Warning("EnumMsaaChildren", $"Outter Exception, when enum msaa children|{e.Message}|{e.StackTrace}");
            }

            return rootList;
        }

        /// <summary>
        /// 从MSAA对象创建NodeInfo
        /// </summary>
        /// <param name="acc">IAccessible对象</param>
        /// <param name="childId">子对象ID， 其实没用</param>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>NodeInfo对象</returns>
        private static NodeInfo CreateNodeInfoFromMsaa(IAccessible acc, object childId, int hwnd)
        {
            string name = "";
            try
            {
                name = acc.get_accName(0);
            }
            catch
            {
                name = "";
            }
            string v = "";
            try
            {
                v = acc.get_accValue(0);
            }
            catch
            {
                
            }
            object roleObj = null;
            try
            {
                roleObj = acc.get_accRole(0);
            }
            catch
            {
                roleObj = null;
            }

            int roleVal = 0;
            string roleText = "";
            if (roleObj is int ri)
            {
                roleText = MARSAccessibleProvider.GetRoleName(ri);
            }

            int x = 0, y = 0, w = 0, h = 0;
            try
            {
                acc.accLocation(out x, out y, out w, out h, 0);
            }
            catch { }

            return new NodeInfo
            {
                Source = "MSAA",
                ControlType = "MSAA",
                Name = name ?? "",
                Value = v ?? "",
                ClassName = "",   // MSAA不提供
                AutomationId = "",   // MSAA不提供
                Rect = (w > 0 && h > 0) ? new Rect(x, y, w, h) : Rect.Empty,
                Hwnd = hwnd,
                RoleName = roleText
            };
        }

        #endregion
    }
}