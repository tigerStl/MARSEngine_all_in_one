using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace MarsUnitTest.UIATest
{
    public static class LegacyIds
    {
        // 模式 ID
        public static readonly AutomationPattern LegacyPattern =
            AutomationPattern.LookupById(10018); // 可能在旧版为 null

        // 属性 ID（托管旧版也能拿到，若 Lookup 失败则为 null，下面会兜底处理）
        public static readonly AutomationProperty ChildId = AutomationProperty.LookupById(30091);
        public static readonly AutomationProperty Name = AutomationProperty.LookupById(30092);
        public static readonly AutomationProperty Value = AutomationProperty.LookupById(30093);
        public static readonly AutomationProperty Description = AutomationProperty.LookupById(30094);
        public static readonly AutomationProperty Role = AutomationProperty.LookupById(30095);
        public static readonly AutomationProperty State = AutomationProperty.LookupById(30096);
        public static readonly AutomationProperty Help = AutomationProperty.LookupById(30097);
        public static readonly AutomationProperty KeyboardShortcut = AutomationProperty.LookupById(30098);
        public static readonly AutomationProperty Selection = AutomationProperty.LookupById(30099);
        public static readonly AutomationProperty DefaultAction = AutomationProperty.LookupById(30100);
    }

    // MSAA 角色转本地化文本（可选）
    public static class MsaaText
    {
        [DllImport("oleacc.dll", CharSet = CharSet.Unicode)]
        static extern uint GetRoleTextW(int role, StringBuilder sb, uint cch);

        public static string RoleToText(int role)
        {
            var sb = new StringBuilder(64);
            try { GetRoleTextW(role, sb, (uint)sb.Capacity); } catch { }
            return sb.ToString();
        }
    }

}
