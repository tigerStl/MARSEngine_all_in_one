using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.MarsConfig
{
    public class RegApplication
    {
        public string AppName { get; internal set; }
        public string command { get; internal set; }
        public string path { get; internal set; }
        public string identifier { get; internal set; }
        public string ApplicationType { get; internal set; }
        public string ExtraRequirement { get; internal set; }
        public string ExtraPopupMenu { get; internal set; }
        public string Mode { get; internal set; }
    }
}
