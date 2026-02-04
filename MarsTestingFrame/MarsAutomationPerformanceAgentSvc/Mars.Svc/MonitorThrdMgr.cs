using Mars.LogCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarsAutomationPerformanceAgentSvc.Mars.Svc
{
    internal class MonitorThrdMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MonitorThrdMgr));
        private static Thread svcThread = null;

        internal static bool StartDeamonThread(ref string strError)
        {
            Logger.logBegin("StartDeamonThread");

            //try
            //{
            //    if (svcThread==null)
            //    {
            //        svcThread = new Thread()
            //    }
            //}
            //catch (Exception e)
            //{

            //    throw;
            //}

            return false;
            
        }
    }
}
