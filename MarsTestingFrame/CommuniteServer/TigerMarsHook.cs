using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace MarsTestFrame.CommuniteServer
{
#if TigerBuggerTrack
    internal class TigerMarsHook : IChannelInitializer
    {

        private MLogger Logger = MLogger.GetLogger(typeof(TigerMarsHook));
        public void Initialize(IClientChannel channel)
        {
            if (channel == null) return;
            Logger.Info("Initialize","One connection coming...");
            //throw new NotImplementedException();
        }


}
#endif
}