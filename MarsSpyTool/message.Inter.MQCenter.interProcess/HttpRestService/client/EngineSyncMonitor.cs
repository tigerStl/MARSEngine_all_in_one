using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client
{
    public class EngineSyncMonitor
    {
        public static ManualResetEvent portfileNotifyEvent = new ManualResetEvent(false);

    }
}
