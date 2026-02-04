using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.MarsUISupport
{
    public class MARSUiaInfo
    {
        public string Name { get; set; } = "";
        public string AutomationId { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string FrameworkId { get; set; } = "";
        public string ControlTypeName { get; set; } = "";
        public int NativeWindowHandle { get; set; } = 0;
        public int ProcessId { get; set; } = 0;
        public System.Windows.Rect Bounds { get; set; }
        public string RuntimeId { get; set; } = "";
        public int SiblingIndex { get; set; } = -1;
    }
}
