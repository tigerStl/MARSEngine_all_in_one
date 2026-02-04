using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.subToolWindows.testStepEditor
{
    internal class MarsTestAPPDBInfo
    {
        public static MarsSpyApplication CurrentApplicationInfo { 
            get; 
            set; 
        }
        public static string currentDBIdx { get; set; }
        public static List<string> CurrentListOfProcess { get; set; }
    }
}
