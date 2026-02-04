using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.httpSvc
{
    /// <summary>
    /// that is MARS http server, the object spy would send msg to this sever, the the server would transfer objects info to mars web
    /// there is two way to send back to mars web, one is client application's stub send message via spy application, 
    /// the other one is taht client application's stub connect to mars web directly.
    /// stub application connects to mars web directly could cause trouble. 
    /// so far, no http server is applied, instead of, FileSystemWatch is used. 
    /// </summary>
    internal class MarsHttpServer
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");
        private static MarsHttpServer HttpSvcInstance = null;

        public static MarsHttpServer getInstance()
        {
            return HttpSvcInstance;
        }

        public bool startSvc()
        {
            return true;
        }
    }
}
