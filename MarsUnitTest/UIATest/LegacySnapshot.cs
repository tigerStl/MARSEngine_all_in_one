using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace MarsUnitTest.UIATest
{
    public sealed class LegacySnapshot
    {
        public int? ChildId;
        public string Name, Value, Description, Help, KeyboardShortcut, DefaultAction;
        public int? Role, State;
        public object Selection; // 有些提供者返回数组/字符串，保持原样

        public static LegacySnapshot ReadLegacy(AutomationElement e)
        {
            var s = new LegacySnapshot
            {
                Name = GetProp(e, LegacyIds.Name) as string ?? "",
                Value = GetProp(e, LegacyIds.Value) as string ?? "",
                Description = GetProp(e, LegacyIds.Description) as string ?? "",
                Help = GetProp(e, LegacyIds.Help) as string ?? "",
                KeyboardShortcut = GetProp(e, LegacyIds.KeyboardShortcut) as string ?? "",
                DefaultAction = GetProp(e, LegacyIds.DefaultAction) as string ?? "",
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

        public static object GetProp(AutomationElement e, AutomationProperty p, bool cachedFirst = true)
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
                return e.GetCurrentPropertyValue(p, true);
            }
            catch
            {
                return AutomationElement.NotSupported;
            }
        }

        public static bool HasLegacy(AutomationElement e)
        {
            // 用一个“核心属性”探测是否支持（Role 最合适）
            var v = GetProp(e, LegacyIds.Role);
            return !ReferenceEquals(v, AutomationElement.NotSupported);
        }
    }
}
