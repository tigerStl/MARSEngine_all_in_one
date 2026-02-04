using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Accessibility;
using Mars.Inter.MQCenter.MSAASupport;
using Mars.message.windowsWrapper.SystemUtil;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public static class MarsHwndAccBuilder
    {
        // ---------- Win32 ----------
        private const int OBJID_WINDOW = 0x00000000;
        private const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);

        //[DllImport("user32.dll")]
        //private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        //private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern int GetWindowTextLength(IntPtr hWnd);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetParent(IntPtr hWnd);

        //[DllImport("user32.dll")]
        //private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        //[StructLayout(LayoutKind.Sequential)]
        //private struct RECT { public int Left, Top, Right, Bottom; }

        // ---------- MSAA ----------
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(
            IntPtr hwnd, int dwObjectID, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

        [DllImport("oleacc.dll")]
        private static extern int WindowFromAccessibleObject(IAccessible pacc, out IntPtr phwnd);

        private static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

        // ---------- 数据结构 ----------
        public sealed class HwndNode
        {
            public IntPtr Hwnd { get; set; }
            public string ClassName { get; set; } = "";
            public IntPtr ParentHwnd { get; set; }
            public AccInfo Acc { get; set; } = new();
            public List<HwndNode> Children { get; set; } = new();

            public override string ToString()
                => $"HWND=0x{Hwnd.ToInt64():X} Class='{ClassName}' Parent=0x{ParentHwnd.ToInt64():X}";
        }

        public sealed class AccInfo
        {
            public string RoleName { get; set; } = "";
            public string Text { get; set; } = "";   // accName 或窗口文本
            public string Value { get; set; } = "";  // accValue
            public string AttachText { get; set; } = ""; // 类似 UFT 的"附属标签"
            public string AttachTextPath { get; set; } = ""; // 控件层次路径
            public IntPtr HostHwnd { get; set; }     // WindowFromAccessibleObject 结果
        }

        // ---------- 主入口 ----------
        /// <summary>
        /// 从 rootHwnd 出发非递归构建 HWND 树，并为每个 HWND 附带 IAccessible 概览和 AttachText。
        /// </summary>
        public static HwndNode BuildTreeWithAcc(IntPtr rootHwnd)
        {
            EnsureSta();

            // 先一次性构建所有节点（BFS），用字典做父子挂载
            var nodes = new Dictionary<IntPtr, HwndNode>();
            var order = new List<IntPtr>(); // 记录顺序，后续填 Children

            // 创建根
            var rootNode = CreateNodeSkeleton(rootHwnd);
            nodes[rootHwnd] = rootNode;
            order.Add(rootHwnd);

            // BFS 队列
            var q = new Queue<IntPtr>();
            q.Enqueue(rootHwnd);

            while (q.Count > 0)
            {
                var parent = q.Dequeue();

                foreach (var ch in EnumImmediateChildren(parent))
                {
                    if (!nodes.ContainsKey(ch))
                    {
                        var n = CreateNodeSkeleton(ch);
                        nodes[ch] = n;
                        order.Add(ch);
                    }
                    q.Enqueue(ch);
                }
            }

            // 填充 Acc 信息 + 生成 Children 列表
            foreach (var h in order)
            {
                var n = nodes[h];

                // 填充 Acc
                n.Acc = BuildAccInfoForHwnd(h);

                // Children
                foreach (var ch in EnumImmediateChildren(h))
                {
                    if (nodes.TryGetValue(ch, out var cn))
                        n.Children.Add(cn);
                }
            }

            return rootNode;
        }

        // ---------- 组装单节点 ----------
        private static HwndNode CreateNodeSkeleton(IntPtr hwnd)
        {
            return new HwndNode
            {
                Hwnd = hwnd,
                ClassName = GetWinClass(hwnd),
                ParentHwnd = MarsWindowsAPIs.GetParent(hwnd)
            };
        }

        // ---------- 取 IAccessible 概览 + AttachText ----------
        private static AccInfo BuildAccInfoForHwnd(IntPtr hwnd)
        {
            IAccessible? acc = GetIAccessible(hwnd, preferClient: true) ?? GetIAccessible(hwnd, preferClient: false);

            string roleName = "";
            string name = "";
            string value = "";
            IntPtr host = IntPtr.Zero;

            if (acc != null)
            {
                // childId = 0 表示"对象本身"
                roleName = RoleToString(Safe(() => acc.get_accRole(0)));
                name = Safe(() => acc.get_accName(0)) ?? "";
                value = Safe(() => acc.get_accValue(0)) ?? "";
                try { WindowFromAccessibleObject(acc, out host); } catch { }
            }
            var allTypes = ComIntrospection.GetSupportedManagedInterfaces(acc);
            Console.WriteLine($"[DEBUG] HWND=0x{hwnd.ToInt64():X} IAccessible Parent COM Types: {string.Join(", ", allTypes)}");
            // 若 accName 为空，尝试用窗口文本补齐
            if (string.IsNullOrEmpty(name))
                name = GetWindowTextSafe(hwnd) ?? "";

            // 计算 AttachText（同父级中离得最近的"Static/Label"）
            string attach = ComputeAttachText(hwnd);

            // 计算 AttachTextPath（控件层次路径）
            string attachPath = ComputeAttachTextPath(hwnd);

            return new AccInfo
            {
                RoleName = roleName,
                Text = name,
                Value = value,
                AttachText = attach,
                AttachTextPath = attachPath,
                HostHwnd = host
            };
        }

        // ---------- AttachText 近邻标签启发式 ----------
        // 规则：在同父级下找 ClassName = "Static"（或文本非空的其它候选），
        // 与目标控件垂直方向有较大重叠，且在其“左侧/上侧较近”的窗口文本。
        private static string ComputeAttachText(IntPtr hwnd)
        {
            var parent = MarsWindowsAPIs.GetParent(hwnd);
            if (parent == IntPtr.Zero) return "";

            if (!TryGetRect(hwnd, out var rcTarget)) return "";

            string best = "";
            int bestScore = int.MaxValue;

            foreach (var sib in EnumImmediateChildren(parent))
            {
                if (sib == hwnd) continue;

                string cls = GetWinClass(sib);
                string txt = GetWindowTextSafe(sib) ?? "";
                if (string.IsNullOrWhiteSpace(txt)) continue;

                // 仅考虑典型 Label
                bool likelyLabel = cls.Equals("Static", StringComparison.OrdinalIgnoreCase) ||
                                   cls.StartsWith("ThunderRT", StringComparison.OrdinalIgnoreCase) || // VB6
                                   cls.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!likelyLabel) continue;

                if (!TryGetRect(sib, out var rcLab)) continue;

                // 计算重叠与距离（优先左侧、次选上侧）
                int vOverlap = Overlap(rcTarget.Top, rcTarget.Bottom, rcLab.Top, rcLab.Bottom);
                if (vOverlap <= 0) continue;

                bool isLeft = rcLab.Right <= rcTarget.Left;
                bool isAbove = rcLab.Bottom <= rcTarget.Top;

                int dx = isLeft ? rcTarget.Left - rcLab.Right : Math.Abs(CenterX(rcLab) - CenterX(rcTarget));
                int dy = isAbove ? rcTarget.Top - rcLab.Bottom : Math.Abs(CenterY(rcLab) - CenterY(rcTarget));

                // 简单打分：越近越好，左侧优先、上侧次之、同侧最差
                int sidePenalty = isLeft ? 0 : (isAbove ? 50 : 80);
                int score = sidePenalty + dx + dy - vOverlap / 4;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = txt.Trim();
                }
            }

            return best;
        }

        // ---------- AttachTextPath 层次路径计算 ----------
        // 从根窗口到目标控件的层次路径，每层使用控件的文本或类名
        private static string ComputeAttachTextPath(IntPtr hwnd)
        {
            var pathParts = new List<string>();
            var current = hwnd;

            while (current != IntPtr.Zero)
            {
                string text = GetWindowTextSafe(current) ?? "";
                string className = GetWinClass(current);
                
                // 优先使用文本，如果文本为空则使用类名
                string part = !string.IsNullOrWhiteSpace(text) ? text.Trim() : className;
                
                if (!string.IsNullOrEmpty(part))
                {
                    pathParts.Insert(0, part); // 插入到开头，构建从根到叶的路径
                }

                // 移动到父窗口
                var parent = MarsWindowsAPIs.GetParent(current);
                if (parent == current || parent == IntPtr.Zero)
                    break;
                    
                current = parent;
            }

            return string.Join("/", pathParts);
        }

        private static IEnumerable<IntPtr> EnumImmediateChildren(IntPtr parent, bool deep = false, bool onlyVisible = false)
        {
            if (!deep)
            {
                // 仅一层
                for (var child = MarsWindowsAPIs.GetWindow(parent, MarsWindowsAPIs.GetWindowType.GW_CHILD);
                     child != IntPtr.Zero;
                     child = MarsWindowsAPIs.GetWindow(child, MarsWindowsAPIs.GetWindowType.GW_HWNDNEXT))
                {
                    if (onlyVisible && !MarsWindowsAPIs.IsWindowVisible(child)) continue;
                    yield return child;
                }
                yield break;
            }

            // 深度遍历（BFS，非递归）
            var q = new Queue<IntPtr>();
            q.Enqueue(parent);

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                for (var child = MarsWindowsAPIs.GetWindow(p, MarsWindowsAPIs.GetWindowType.GW_CHILD);
                     child != IntPtr.Zero;
                     child = MarsWindowsAPIs.GetWindow(child, MarsWindowsAPIs.GetWindowType.GW_HWNDNEXT))
                {
                    if (!onlyVisible || MarsWindowsAPIs.IsWindowVisible(child))
                        yield return child;

                    // 不论可见与否都继续向下（避免因为父隐藏漏掉后代）
                    q.Enqueue(child);
                }
            }
        }

        // ---------- IAccessible 获取 ----------
        private static IAccessible? GetIAccessible(IntPtr hwnd, bool preferClient)
        {
            int objId = preferClient ? OBJID_CLIENT : OBJID_WINDOW;
            try
            {
                if (AccessibleObjectFromWindow(hwnd, objId, ref IID_IAccessible, out var o) >= 0 && o is IAccessible ia)
                    return ia;
            }
            catch { }
            return null;
        }

        // ---------- 小工具 ----------
        private static string GetWinClass(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            try { if (MarsWindowsAPIs.GetClassName(hwnd, sb, sb.Capacity) > 0) return sb.ToString(); } catch { }
            return "";
        }

        private static string? GetWindowTextSafe(IntPtr hwnd)
        {
            try
            {
                int len = MarsWindowsAPIs.GetWindowTextLength(hwnd);
                if (len <= 0) return "";
                var sb = new StringBuilder(len + 1);
                if (MarsWindowsAPIs.GetWindowText(hwnd, sb, sb.Capacity) > 0) return sb.ToString();
            }
            catch { }
            return "";
        }

        private static bool TryGetRect(IntPtr hwnd, out Rectangle r)
        {
            r = Rectangle.Empty;
            try
            {
                if (MarsWindowsAPIs.GetWindowRect(hwnd, out var rc))
                {
                    r = Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Bottom);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static int Overlap(int a1, int a2, int b1, int b2)
            => Math.Max(0, Math.Min(a2, b2) - Math.Max(a1, b1));

        private static int CenterX(Rectangle r) => (r.Left + r.Right) / 2;
        private static int CenterY(Rectangle r) => (r.Top + r.Bottom) / 2;

        private static string RoleToString(object? roleObj)
        {
            if (roleObj == null) return "";
            if (roleObj is int i)
            {
                // 常见 ROLE 映射（可按需扩充）
                return MARSAccessibleProvider.GetRoleName(i);
            }
            return roleObj.ToString() ?? "";
        }

        private static T? Safe<T>(Func<T> f)
        {
            try { return f(); } catch { return default; }
        }

        private static void EnsureSta()
        {
            var ap = System.Threading.Thread.CurrentThread.GetApartmentState();
            if (ap != System.Threading.ApartmentState.STA)
                Console.Error.WriteLine("[WARN] Not STA thread. Consider running on STA to avoid COM/MSAA issues.");
        }

        public class ControlInfo
        {
            public IntPtr Hwnd { get; set; }
            public IntPtr Parent { get; set; }
            public string ClassName { get; set; }
            public int CtrlId { get; set; }     // GetDlgCtrlID（同父内唯一；IDC_STATIC=-1 可重复）
            public string Text { get; set; }
            public bool Visible { get; set; }
            public Rectangle Bounds { get; set; }
        }

        public static List<ControlInfo> GetAllDescendantControls(IntPtr root, bool onlyVisible = false)
        {
            var result = new List<ControlInfo>();
            var q = new Queue<IntPtr>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var parent = q.Dequeue();

                // 枚举 parent 的“直接子窗口”
                for (var child = MarsWindowsAPIs.GetWindow(parent, MarsWindowsAPIs.GetWindowType.GW_CHILD);
                     child != IntPtr.Zero;
                     child = MarsWindowsAPIs.GetWindow(child, MarsWindowsAPIs.GetWindowType.GW_HWNDNEXT))
                {
                    bool vis = MarsWindowsAPIs.IsWindowVisible(child);
                    if (onlyVisible && !vis) { q.Enqueue(child); continue; } // 仍然入队，保证遍历到底

                    var info = new ControlInfo
                    {
                        Hwnd = child,
                        Parent = MarsWindowsAPIs.GetParent(child),
                        ClassName = GetClass(child),
                        CtrlId = MarsWindowsAPIs.GetDlgCtrlID(child),
                        Text = GetText(child),
                        Visible = vis,
                        Bounds = GetRect(child)
                    };

                    result.Add(info);
                    q.Enqueue(child); // 继续向下层遍历
                }
            }
            // 移除根自身（如果需要仅“后代”）
            result.RemoveAll(ci => ci.Hwnd == root);
            return result;
        }

        static string GetClass(IntPtr h)
        {
            var sb = new StringBuilder(256);
            return MarsWindowsAPIs.GetClassName(h, sb, sb.Capacity) > 0 ? sb.ToString() : "";
        }

        static string GetText(IntPtr h)
        {
            try
            {
                int len = MarsWindowsAPIs.GetWindowTextLength(h);
                if (len <= 0) return "";
                var sb = new StringBuilder(len + 1);
                return MarsWindowsAPIs.GetWindowText(h, sb, sb.Capacity) > 0 ? sb.ToString() : "";
            }
            catch { return ""; }
        }

        static Rectangle GetRect(IntPtr h)
        {
            try
            {
                if (MarsWindowsAPIs.GetWindowRect(h, out var r))
                    return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            }
            catch { }
            return Rectangle.Empty;
        }

        // 读样式可判断是否 WS_CHILD（一般控件都会有）
        public static bool IsChildWindow(IntPtr h)
        {
            long style = MarsWindowsAPIs.GetWindowLong(h, MarsWindowsAPIs.GWL_STYLE);
            return (style & MarsWindowsAPIs.WindowStyles.WS_CHILD) != 0;
        }
    }
}
