extern alias clientWCF;
using clientWCF::TestFlowClient;
using clientWCF::TestFrameMonitor.Server.ServiceContracts;

namespace Mars.AutoTestingDriver.wcfClient
{
    class WcfClientAgent
    {
        private static TestFlowClientMainEntry clientInstance = new TestFlowClientMainEntry();
        internal static IMonitorService MonitorWcfClient
        {
            get
            {
                return clientInstance.MonitorWcfClient;
            }

        }

        public static bool IsWcfOffLine()
        {
            if (TestFlowClientMainEntry.objDualFactory == null) return true;
            if (TestFlowClientMainEntry.objDualFactory.State != System.ServiceModel.CommunicationState.Opened)
            {
                return true;
            }
            return false;
        }

        internal static void ReconnectTo()
        {
            string strError = "";
            bool isOk = true;
            clientInstance.ConnectToWCFMonitorServer(ref isOk, ref strError);
        }
    }

}
