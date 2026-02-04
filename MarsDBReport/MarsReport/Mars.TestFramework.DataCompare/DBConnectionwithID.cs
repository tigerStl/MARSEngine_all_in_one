
/*A class for constructing a new DB Connection with an ID and to build a connection string */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mars.Securities;

namespace Mars.TestFramework.DataCompare
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
        public string Sid { get; set; }
        public string ConnString { get; set; }

        public string BuildConnectionString()
        {
            string ConnectionString = "";
            // For Oracle we try building connection string 3 different ways
            // 1. Using Connection string provided
            // 2. Using SID
            // 3. Using SERVICE_NAME
            // Only one of the 3 ways will be used

            if (DatabaseType.Equals("Oracle"))
            {
                if (ConnString != null && ConnString.Trim().Length > 40)
                    ConnectionString = "Data Source = " + ConnString + "; User Id = " + UserID + "; Password = " + MarsEncodePwd.DecodeString(Password) + ";";
                else if (Sid != null && Sid.Trim().Length > 0)
                    ConnectionString = "Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = " + Protocol + ")(HOST = " + Host + ")(PORT = " + Port + "))(CONNECT_DATA = (SID = " + Sid + "))); User Id = " + UserID + "; Password = " + MarsEncodePwd.DecodeString(Password) + ";";
                else
                    ConnectionString = "Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = " + Protocol + ")(HOST = " + Host + ")(PORT = " + Port + "))(CONNECT_DATA = (SERVICE_NAME = " + ServiceName + "))); User Id = " + UserID + "; Password = " + MarsEncodePwd.DecodeString(Password) + ";";
            }
            else if (DatabaseType.Equals("SQL Server"))
                ConnectionString = "Data Source=" + Host + ";Initial Catalog=" + ServiceName + ";User ID=" + UserID + ";Password=" + MarsEncodePwd.DecodeString(Password) + ";";
            return ConnectionString;
        }   
        
        //<!--<add name="AcctPost53DB" type="Oracle" connectionString="Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)
        //(HOST = 192.168.2.99)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = orcl.internal.marquis.nyc))); User Id = AF; Password =AF;"/>-->
    }
}
