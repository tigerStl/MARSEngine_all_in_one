using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.MarsConfig
{
    public class OracleConnectionStringBuilder 
    {
        private string serviceName;
        private string otherOptions;
        private string password;
        private int port;
        private string hostName;

        public bool IsDirty { get; private set; }

        private string username;
        private bool pooling;
        private string statementCacheSize;



        public OracleConnectionStringBuilder(string host, int port, string service, string userName, string password, string cacheSize, bool pooling)
        {
            this.hostName = host;
            this.port = port;
            this.serviceName = service;
            this.username = userName;
            this.password = password;
            this.statementCacheSize = cacheSize;
            this.pooling = pooling;
        }

        public OracleConnectionStringBuilder()
        {
            // Port is pre-slugged as 1521 is the default Oracle port.
            port = 1521;
        }

        /// <summary>
        /// Specifies the hostName to connect. This can be either the DNS name of the
        /// hostName or the IP (as a string).
        /// </summary>
        /// <param name="server">The hostName.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Server(string server)
        {
            this.hostName = server;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Specifies the serviceName (database name) to use.  This can be the short name or the
        /// fully qualified name (Oracle service name).
        /// </summary>
        /// <param name="instance">The serviceName.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Instance(string instance)
        {
            this.serviceName = instance;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Specifies the name of the user account accessing the database.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Username(string username)
        {
            this.username = username;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Specifies the password of the user account accessing the database.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Password(string password)
        {
            this.password = password;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Optional. Ports the specified port the oracle database is running on.  This defaults to 1521.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Port(int port)
        {
            this.port = port;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Enable or disable pooling connections for this data configuration.
        /// </summary>
        /// <param name="pooling">if set to <c>true</c> enable pooling.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder Pooling(bool pooling)
        {
            this.pooling = pooling;
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Specifies the SQL statement cache size to use for this connection.
        /// </summary>
        /// <param name="cacheSize">Size of the cache.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder StatementCacheSize(int cacheSize)
        {
            this.statementCacheSize = string.Format("Statement Cache Size={0};", cacheSize);
            IsDirty = true;
            return this;
        }

        /// <summary>
        /// Specifies, as a string, other Oracle options to pass to the connection.
        /// </summary>
        /// <param name="otherOptions">The other options.</param>
        /// <returns></returns>
        public OracleConnectionStringBuilder OtherOptions(string otherOptions)
        {
            this.otherOptions = string.Format("{0};", otherOptions);
            IsDirty = true;
            return this;
        }

        protected internal  string Create()
        {
            /*
            string connectionString = string.Format(
                     "User Id={0};Password={1};Pooling={2};{3}{4}Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={5})(PORT={6})))(CONNECT_DATA=(SERVER = DEDICATED)(SERVICE_NAME={7})))",
                     username, password, pooling, statementCacheSize, otherOptions, hostName, port, serviceName);
            */

            string connectionString = string.Format(
                "DATA SOURCE=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={2})));PASSWORD={3};USER ID={4}", hostName, port, serviceName, password, username);

            return connectionString;
        }
    }
}
