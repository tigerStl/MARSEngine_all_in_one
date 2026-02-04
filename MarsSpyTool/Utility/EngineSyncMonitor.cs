using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarsSpyTool.Utility
{
    public class EngineSyncMonitor
    {
        public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    }
}
