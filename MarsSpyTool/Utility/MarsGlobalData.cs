using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.Utility
{
    public enum Mars_spy_tool_function
    {
        _none, 
        _find_obj,
        _auto_gen_test_step,
        _record_replay
    }

    public class MarsGlobalData
    {
        public static string currentUUIDFromWeb { get; set; }
        public static string currentMode { get; set; }
        public static string currentRemoteServerWithAddress { get;set; }
    }
}
