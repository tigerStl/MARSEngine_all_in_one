using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.MarsConfig
{
    public class DatabaseConnectionDetails
    {
        public string ConnString { get; internal set; }
        public string Schema { get; internal set; }
        public string Login { get; internal set; }
        public string Password { get; internal set; }

        public string Host { get; internal set; }

        public string Port { get; internal set; }

        public string Type { get; internal set; }

        public string ServiceName { get; internal set; }

        public string EntityConnString { get; internal set; }

        /// <summary>
        /// Added by tiger for multiple db index 
        /// </summary>
        public string DBIdx { get; set; }
        public DatabaseConnectionDetails(string host, string port, string serviceName, string type, string connString, string entityConnString, string schema, string login, string password, 
            string dbIdx="MarsEntities")
        {
            Host = host;
            Port = port;
            Type = type;
            ConnString = connString;
            EntityConnString = entityConnString;
            Schema = schema;
            Login = login;
            Password = password;
            ServiceName = serviceName;
            DBIdx = dbIdx;
        }
    }
}
