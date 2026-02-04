using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.CommuniteServer
{
    public sealed class ServicebasicInformation
    {
        #region member
        private string mstrHost;
        private string mstrProtocol;
        private string mstrPort;
        private string mstrServiceName;        
        #endregion

        #region properties
        public string Host { get { return this.mstrHost; } set { this.mstrHost = value; } }
        public string Potocol { get { return this.mstrProtocol; } set { this.mstrProtocol = value; } }
        public string Port { get { return this.Port; } set { this.mstrPort = value; } }
        public string ServiceName { get { return this.ServiceName; } set { this.mstrServiceName = value; } } 
        #endregion

        public string GetURL()
        {
            return string.Format("{0}://{1}:{2}/{3}", mstrProtocol, mstrHost, mstrPort, mstrServiceName);
        }

        public static string GetURL(string strHost, string strProtocol, string strPort, string strServiceName)
        {
            return string.Format("{0}://{1}:{2}/{3}", strProtocol, strHost, strPort, strServiceName);
        }
    }
}
