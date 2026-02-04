//using Axe.Windows.Desktop.UIAutomation.Patterns;
//using Axe.Windows.Desktop.UIAutomation.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;

namespace MarsUnitTest.HybridAfx
{
    public static class HybridAfxInteractor
    {
        // ---- UIA Pattern（带 Lookup 兜底，兼容旧托管）----
        static readonly AutomationPattern P_Invoke = InvokePattern.Pattern ?? AutomationPattern.LookupById(10000);
        static readonly AutomationPattern P_SelItem = SelectionItemPattern.Pattern ?? AutomationPattern.LookupById(10010);
        static readonly AutomationPattern P_ExpColl = ExpandCollapsePattern.Pattern ?? AutomationPattern.LookupById(10005);
        static readonly AutomationPattern P_Toggle = TogglePattern.Pattern ?? AutomationPattern.LookupById(10015);
        static readonly AutomationPattern P_Value = ValuePattern.Pattern ?? AutomationPattern.LookupById(10002);
        static readonly AutomationPattern P_ScrollItem = ScrollItemPattern.Pattern ?? AutomationPattern.LookupById(10017);
        static readonly AutomationPattern P_Legacy = AutomationPattern.LookupById(10018); // 旧托管可能拿不到

        // Legacy 属性 ID（托管旧版也能用 LookupById；若返回 null，后面会判 NotSupported）
        static readonly AutomationProperty Legacy_Name = AutomationProperty.LookupById(30092);
        static readonly AutomationProperty Legacy_Value = AutomationProperty.LookupById(30093);
        static readonly AutomationProperty Legacy_Role = AutomationProperty.LookupById(30095);
        static readonly AutomationProperty Legacy_Default = AutomationProperty.LookupById(30100);

        // ---- MSAA 基础 ----
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("618736E0-3C3D-11CF-810C-00AA00389B71")]
        interface IAccessible
        {
            // 只声明用到的方法（省略其它）
            [PreserveSig] int get_accName(object child, [MarshalAs(UnmanagedType.BStr)] out string name);
            [PreserveSig] int get_accRole(object child, out object role);
            [PreserveSig] int get_accState(object child, out object state);
            [PreserveSig] int get_accValue(object child, [MarshalAs(UnmanagedType.BStr)] out string value);
            [PreserveSig] int accDoDefaultAction(object child);
            [PreserveSig] int accSelect(int flagsSelect, object child);
            [PreserveSig] int accHitTest(int xLeft, int yTop, out object pvarChild);
        }

        [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr hwnd, int dwId, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
        const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);

        // ---- SendInput 兜底点击 ----
        [StructLayout(LayoutKind.Sequential)] struct INPUT { public int type; public MOUSEINPUT mi; }
        [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [DllImport("user32.dll")] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        const int INPUT_MOUSE = 0; const int MOUSEEVENTF_MOVE = 0x0001; const int MOUSEEVENTF_ABSOLUTE = 0x8000; const int MOUSEEVENTF_LEFTDOWN = 0x0002; const int MOUSEEVENTF_LEFTUP = 0x0004;

        // ---- 公共：遍历 & 信息 ----
        public static IEnumerable<AutomationElement> EnumChildren(AutomationElement parent, bool preferControlView = true, bool includeOffscreen = true)
        {
            var list = new List<AutomationElement>();
            bool Try(TreeWalker w)
            {
                for (var c = w.GetFirstChild(parent); 
                    c != null;
                    c = w.GetNextSibling(c))
                {
                    if (!includeOffscreen && Safe(() => c.Current.IsOffscreen)) 
                        continue;
                    list.Add(c);
                }
                return list.Count > 0;
            }
            if (preferControlView)
            {
                if (!Try(TreeWalker.ControlViewWalker)) { 
                    list.Clear(); 
                    Try(TreeWalker.RawViewWalker); 
                }
            }
            else
            {
                if (!Try(TreeWalker.RawViewWalker)) { 
                    list.Clear(); 
                    Try(TreeWalker.ControlViewWalker); 
                }
            }
            return list;
        }

        public static string Describe(AutomationElement e)
        {
            var ct = Safe(() => e.Current.ControlType?.ProgrammaticName) ?? "ControlType.?";
            var name = Safe(() => e.Current.Name) ?? "";
            var cls = Safe(() => e.Current.ClassName) ?? "";
            var aid = Safe(() => e.Current.AutomationId) ?? "";
            var fwk = Safe(() => e.Current.FrameworkId) ?? "";
            var hwnd = Safe(() => e.Current.NativeWindowHandle);
            var off = Safe(() => e.Current.IsOffscreen);
            var r = Safe(() => e.Current.BoundingRectangle);
            bool hasLegacy = P_Legacy != null && e.TryGetCurrentPattern(P_Legacy, out _);
            return $"{ct} Name='{name}' Class='{cls}' AutoId='{aid}' Fwk='{fwk}' Hwnd=0x{hwnd:X} Off={off} Rect=[{r.Left:0},{r.Top:0},{r.Right:0},{r.Bottom:0}] Legacy={(hasLegacy ? "Yes" : "No")}";
        }

        // ---- 公共：对目标元素做“智能点击/激活” ----
        public static bool Activate(AutomationElement e)
        {
            // 0) 能滚到可见先滚一下
            if (P_ScrollItem != null && e.TryGetCurrentPattern(P_ScrollItem, out var spi) && spi is ScrollItemPattern sip)
            { Safe(() => { sip.ScrollIntoView(); return true; }); }

            // 1) Invoke
            if (P_Invoke != null && e.TryGetCurrentPattern(P_Invoke, out var p) && p is InvokePattern inv)
                return Safe(() => { inv.Invoke(); return true; });

            // 2) SelectionItem
            if (P_SelItem != null && e.TryGetCurrentPattern(P_SelItem, out p) && p is SelectionItemPattern si)
                return Safe(() => { si.Select(); return true; });

            // 3) ExpandCollapse（折叠就展开）
            if (P_ExpColl != null && e.TryGetCurrentPattern(P_ExpColl, out p) && p is ExpandCollapsePattern ec)
            {
                var s = Safe(() => ec.Current.ExpandCollapseState);
                if (s == ExpandCollapseState.Collapsed || s == ExpandCollapseState.PartiallyExpanded)
                    return Safe(() => { ec.Expand(); return true; });
            }

            // 4) Toggle
            if (P_Toggle != null && e.TryGetCurrentPattern(P_Toggle, out p) && p is TogglePattern tg)
                return Safe(() => { tg.Toggle(); return true; });

            // 5) Legacy 默认动作
            if (P_Legacy != null && e.TryGetCurrentPattern(P_Legacy, out p) && p is Axe.Windows.Desktop.UIAutomation.Patterns.LegacyIAccessiblePattern lp)
                return Safe(() => { lp.DoDefaultAction(); return true; });

            // 6) MSAA：从宿主 HWND 做一次命中并默认动作
            var host = Safe(() => e.Current.NativeWindowHandle);
            var rect = Safe(() => e.Current.BoundingRectangle);
            if (host != 0 && rect != Rect.Empty)
            {
                var center = new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                if (TryMsaaDefaultFromPoint(new IntPtr(host), (int)center.X, (int)center.Y))
                    return true;
            }

            // 7) 兜底：SendInput 点击中心点
            return ClickCenter(rect, new IntPtr(host));
        }

        // ---- 读取 Legacy 快照（Afx 常用：无 UIA Pattern 时至少能拿到文本/角色） ----
        public static (string name, string value, int? role, string defaultAction) ReadLegacySnapshot(AutomationElement e)
        {
            string name = PropString(e, Legacy_Name);
            string val = PropString(e, Legacy_Value);
            int? role = PropInt(e, Legacy_Role);
            string def = PropString(e, Legacy_Default);
            return (name, val, role, def);
        }

        // ---- 内部：MSAA 命中并默认动作 ----
        static bool TryMsaaDefaultFromPoint(IntPtr hwnd, int x, int y)
        {
            try
            {
                if (AccessibleObjectFromWindow(hwnd, OBJID_CLIENT, ref IID_IAccessible, out var obj) >= 0 && obj is IAccessible acc)
                {
                    object v;
                    if (acc.accHitTest(x, y, out v) >= 0)
                    {
                        // 命中可能返回 IAccessible 或 childId(int)
                        if (v is IAccessible childAcc) return acc.accDoDefaultAction(0) >= 0 || childAcc.accDoDefaultAction(0) >= 0;
                        if (v is int cid) return acc.accDoDefaultAction(cid) >= 0;
                    }
                }
            }
            catch { }
            return false;
        }

        // ---- 内部：输入点击 ----
        static bool ClickCenter(Rect rect, IntPtr hwnd)
        {
            if (rect == Rect.Empty) return false;
            if (hwnd != IntPtr.Zero) try { SetForegroundWindow(hwnd); } catch { }

            // 把屏幕坐标换成 SendInput 的绝对坐标（0..65535）
            double cx = (rect.Left + rect.Right) / 2.0;
            double cy = (rect.Top + rect.Bottom) / 2.0;
            int sx = (int)(cx * 65535.0 / SystemParameters.PrimaryScreenWidth);
            int sy = (int)(cy * 65535.0 / SystemParameters.PrimaryScreenHeight);

            var inputs = new INPUT[]
            {
            new INPUT{ type=INPUT_MOUSE, mi=new MOUSEINPUT{ dx=sx, dy=sy, dwFlags=MOUSEEVENTF_MOVE|MOUSEEVENTF_ABSOLUTE }},
            new INPUT{ type=INPUT_MOUSE, mi=new MOUSEINPUT{ dwFlags=MOUSEEVENTF_LEFTDOWN }},
            new INPUT{ type=INPUT_MOUSE, mi=new MOUSEINPUT{ dwFlags=MOUSEEVENTF_LEFTUP }},
            };
            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == inputs.Length;
        }

        // ---- 小工具 ----
        static string PropString(AutomationElement e, AutomationProperty p)
        {
            try
            {
                var v = e.GetCurrentPropertyValue(p, true);
                return !ReferenceEquals(v, AutomationElement.NotSupported) ? (v as string) ?? "" : "";
            }
            catch { return ""; }
        }
        static int? PropInt(AutomationElement e, AutomationProperty p)
        {
            try
            {
                var v = e.GetCurrentPropertyValue(p, true);
                return !ReferenceEquals(v, AutomationElement.NotSupported) && v is int i ? i : (int?)null;
            }
            catch { return null; }
        }
        static T Safe<T>(Func<T> f) { try { return f(); } catch { return default(T); } 
        }

        public static AutomationElement FindByNameContains(AutomationElement root, string key)
        {
            var q = new Queue<AutomationElement>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var e = q.Dequeue();
                var name = e.Current.Name ?? "";
                if (name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return e;
                foreach (var c in HybridAfxInteractor.EnumChildren(e, true)) q.Enqueue(c);
                foreach (var c in HybridAfxInteractor.EnumChildren(e, false)) q.Enqueue(c); // Raw 兜底
            }
            return null;
        }
    }

    public static class Test_hybridAfx
    {
        // 仅测试能否调用
        public static void Test(int iHwnd)
        {
            // 1) 从句柄拿到元素（例如 Afx:RibbonBar）
            IntPtr hwnd = new IntPtr(iHwnd)/* 你的 Afx 窗口/控件句柄 */;
            var elem = AutomationElement.FromHandle(hwnd);

            // 2) 先遍历看子项（ControlView→RawView）
            foreach (var child in HybridAfxInteractor.EnumChildren(elem, preferControlView: true))
                Console.WriteLine("  " + HybridAfxInteractor.Describe(child));

            // 3) 尝试对某个子项执行动作（优先 UIA，再落 Legacy、MSAA、输入）
            // 这里示意：找到名字包含“保存”的按钮
            var target = HybridAfxInteractor.FindByNameContains(elem, "保存");
            if (target != null)
            {
                bool ok = HybridAfxInteractor.Activate(target);
                Console.WriteLine("Activate result: " + ok);
            }

            // 读取 Legacy 快照（即使无 Pattern，Afx 常能给出这些字段）
            var snap = HybridAfxInteractor.ReadLegacySnapshot(elem);
            Console.WriteLine($"Legacy: Name='{snap.name}', Value='{snap.value}', Role={snap.role}, Def='{snap.defaultAction}'");
        }


    }
}
