
/*A class for constructing a new DB Connection with an ID and to build a connection string */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.CompareGUI
{
    public class DBConnectionwithID
    {
        public DBConnectionwithID()
        {
            ConnectionID = "";
            DatabaseType = "";
            Host = "";
            Port = "";
            Protocol = "";
            ServiceName = "";
            UserID = "";
            Password = "";
        }
        
        public string ConnectionID { get; set; }
        public string DatabaseType { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string Protocol { get; set; }
        public string ServiceName { get; set; }
        public string UserID { get; set; }
        public string Password { get; set; }

        public string BuildConnectionString()
        {
            string ConnectionString = "";
            if (DatabaseType.Equals("Oracle"))
                ConnectionString = "Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = " + Protocol + ")(HOST = " + Host + ")(PORT = " + Port + "))(CONNECT_DATA = (SERVICE_NAME = " + ServiceName + "))); User Id = "+UserID+"; Password = "+Password+";";
            else if (DatabaseType.Equals("SQL Server"))
                ConnectionString = "Data Source=" + Host + ";Initial Catalog=" + ServiceName + ";User ID=" + UserID + ";Password=" + Password + ";";
            return ConnectionString;
        }   
        
        //<!--<add name="AcctPost53DB" type="Oracle" connectionString="Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)
        //(HOST = 192.168.2.99)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = orcl.internal.marquis.nyc))); User Id = AF; Password =AF;"/>-->
    }
}
