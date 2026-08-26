namespace MarsTestFrame.CommuniteServer
{
    public sealed class ServicebasicInformation
    {
        #region member
        private string mstrHost = string.Empty;
        private string mstrProtocol = string.Empty;
        private string mstrPort = string.Empty;
        private string mstrServiceName = string.Empty;
        #endregion

        #region properties
        public string Host { get { return this.mstrHost; } }
        public string Potocol { get { return this.mstrProtocol; } }
        public string Port { get { return this.mstrPort; } }
        public string ServiceName { get { return this.mstrServiceName; } }
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
