using Axe.Windows.Desktop.UIAutomation.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace MarsUnitTest.UIATest
{
    public static class UiaLegacy
    {
        /// <summary>从元素读取一个属性（优先 Cached，失败读 Current）。</summary>
        public static object GetProp(AutomationElement e, AutomationProperty p, bool cachedFirst = true)
        {
            if (e == null || p == null) return AutomationElement.NotSupported;

            if (cachedFirst)
            {
                try
                {
                    var v = e.GetCachedPropertyValue(p, true);
                    if (!ReferenceEquals(v, AutomationElement.NotSupported)) return v;
                }
                catch { /* ignore */ }
            }
            try
            {
                return e.GetCurrentPropertyValue(p, true);
            }
            catch
            {
                return AutomationElement.NotSupported;
            }
        }

        /// <summary>元素是否“具备 Legacy”——用一个核心属性（Role）是否 NotSupported 来判断。</summary>
        public static bool HasLegacy(AutomationElement e)
        {
            var v = GetProp(e, LegacyIds.Role);
            return !ReferenceEquals(v, AutomationElement.NotSupported);
        }

        /// <summary>读取元素上的 Legacy 快照（即使拿不到 Pattern 也可用）。</summary>
        public static LegacySnapshot ReadLegacy(AutomationElement e)
        {
            var s = new LegacySnapshot
            {
                Name = (GetProp(e, LegacyIds.Name) as string) ?? "",
                Value = (GetProp(e, LegacyIds.Value) as string) ?? "",
                Description = (GetProp(e, LegacyIds.Description) as string) ?? "",
                Help = (GetProp(e, LegacyIds.Help) as string) ?? "",
                KeyboardShortcut = (GetProp(e, LegacyIds.KeyboardShortcut) as string) ?? "",
                DefaultAction = (GetProp(e, LegacyIds.DefaultAction) as string) ?? "",
                Selection = GetProp(e, LegacyIds.Selection)
            };

            var child = GetProp(e, LegacyIds.ChildId);
            if (!(ReferenceEquals(child, AutomationElement.NotSupported)) && child is int cid) s.ChildId = cid;

            var role = GetProp(e, LegacyIds.Role);
            if (!(ReferenceEquals(role, AutomationElement.NotSupported)) && role is int r) s.Role = r;

            var st = GetProp(e, LegacyIds.State);
            if (!(ReferenceEquals(st, AutomationElement.NotSupported)) && st is int stv) s.State = stv;

            return s;
        }

        /// <summary>尝试获取 Legacy Pattern 对象（托管 UIA 新版可用；旧版 `LegacyPattern` 可能为 null）。</summary>
        public static bool TryGetLegacyPattern(AutomationElement e, out LegacyIAccessiblePattern pat)
        {
            pat = null;
            if (LegacyIds.LegacyPattern == null) return false; // 旧 UIA：没有这个模式
            if (e.TryGetCurrentPattern(LegacyIds.LegacyPattern, out object obj))
            {
                pat = obj as LegacyIAccessiblePattern;
                return pat != null;
            }
            return false;
        }

        /// <summary>执行默认动作（需要 Pattern；在旧 UIA 可能返回 false）。</summary>
        public static bool DoDefaultAction(AutomationElement e)
        {
            return TryGetLegacyPattern(e, out var pat) && Safe(() => { pat.DoDefaultAction(); return true; });
        }

        /// <summary>设置值（针对此元素的 MSAA Value；需要 Pattern）。</summary>
        public static bool SetValue(AutomationElement e, string value)
        {
            return TryGetLegacyPattern(e, out var pat) && Safe(() => { pat.SetValue(value ?? ""); return true; });
        }

        /// <summary>选择（传入 MSAA SELFLAG_* 的组合；需要 Pattern）。</summary>
        public static bool Select(AutomationElement e, int selFlags)
        {
            return TryGetLegacyPattern(e, out var pat) && Safe(() => { pat.Select(selFlags); return true; });
        }

        public static class SelFlags
        {
            public const int TAKEFOCUS = 0x1;
            public const int TAKESELECTION = 0x2;
            public const int EXTENDSELECTION = 0x4;
            public const int ADDSELECTION = 0x8;
            public const int REMOVESELECTION = 0x10;
        }

        private static bool Safe(Func<bool> f) { try { return f(); } catch { return false; } }
    }
}
