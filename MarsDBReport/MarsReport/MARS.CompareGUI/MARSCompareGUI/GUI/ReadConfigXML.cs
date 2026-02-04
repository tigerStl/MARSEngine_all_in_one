
/*Class to read the configuration file.
  Uses ID to read and return a specific compare, dbconnection and query config*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Configuration;
using Mars.DataLayer;
using Mars.TestFramework.DataCompare;
using Mars.Securities;
using DomUtil;
using DocumentFormat.OpenXml.EMMA;

namespace MARS.CompareGUI
{
    class ReadConfigXML
    {
        // Reading a Query config
        public static string GetQueryFromID (string QID, string filename)
        {
            string Query = "";
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);

            xmlDoc = DomHelper.ReadXmlDoc();

            string searchstring = "//configuration/Queries/Query[@ID='"+QID+"']";
            var nodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (nodeRegion!=null)
                Query = nodeRegion.Attributes["Query"] == null ? null : nodeRegion.Attributes["Query"].Value;
            return Query;
        }

        // Reading a DBConnection config
        public static DBConnectionwithID GetConnectionFromID (string CID, string filename)
        {
            DBConnectionwithID DBConnFromConfig = new DBConnectionwithID();
            var xmlDoc = new XmlDocument();
            xmlDoc = DomHelper.ReadXmlDoc();
            //xmlDoc.Load(@filename);

            string searchstring = "//configuration/Connections/DBConn[@ID='"+CID+"']";
            var nodeRegion = xmlDoc.SelectSingleNode(searchstring);
            DBConnFromConfig.ConnectionID = CID;
            DBConnFromConfig.DatabaseType = nodeRegion.Attributes["Type"].Value;
            DBConnFromConfig.Host = nodeRegion.Attributes["Host"].Value;
            DBConnFromConfig.Port = nodeRegion.Attributes["Port"].Value;
            DBConnFromConfig.Protocol = nodeRegion.Attributes["Protocol"]==null?null: nodeRegion.Attributes["Protocol"].Value;
            DBConnFromConfig.ServiceName = nodeRegion.Attributes["ServiceName"].Value;
            DBConnFromConfig.UserID = nodeRegion.Attributes["UserID"].Value;
            
            DBConnFromConfig.Password = MarsEncodePwd.DecodeString(nodeRegion.Attributes["Password"].Value);
            
            if (nodeRegion.Attributes["Sid"] != null)
                DBConnFromConfig.Sid = nodeRegion.Attributes["Sid"].Value;
            if (nodeRegion.Attributes["ConnString"] != null)
                DBConnFromConfig.ConnString = nodeRegion.Attributes["ConnString"].Value;

            return DBConnFromConfig;
        }



        // Reading a Profile config
        public static ProfileWithID GetProfileFromID(string PID, string filename)
        {
            ProfileWithID ProfileFromConfig = new ProfileWithID();
            var xmlDoc = new XmlDocument();
            xmlDoc = DomHelper.ReadXmlDoc();
            //xmlDoc.Load(@filename);

            string searchstring = "//configuration/Profiles/Profile[@ID='" + PID + "']";
            var nodeRegion = xmlDoc.SelectSingleNode(searchstring);
            ProfileFromConfig.ProfileNameID = PID;
            ProfileFromConfig.outDir = nodeRegion.Attributes["outDir"].Value;
            ProfileFromConfig.BaselineFmt = nodeRegion.Attributes["BaselineFmt"].Value;
            ProfileFromConfig.BaselineRpt = nodeRegion.Attributes["BaselineRpt"].Value;
            ProfileFromConfig.CompareFmt = nodeRegion.Attributes["CompareFmt"].Value;
            ProfileFromConfig.CompareRpt = nodeRegion.Attributes["CompareRpt"].Value;
            return ProfileFromConfig;
        }



        // Reading a Compare config
        public static ComparewithID GetCompareFromID(string CompID, string filename)
        {
            ComparewithID CompareFromConfig = new ComparewithID();
            
            var xmlDoc = DomHelper.ReadXmlDoc();
            //xmlDoc.Load(@filename);

            string searchstring = "//configuration/Compares/Compare[@ID='"+CompID+"']";
            var nodeRegion = xmlDoc.SelectSingleNode(searchstring);
            CompareFromConfig.CompareID = CompID;
            CompareFromConfig.S1Type = nodeRegion.Attributes["S1Type"].Value;
            CompareFromConfig.S1DBConn = nodeRegion.Attributes["S1DBConn"]==null?null: nodeRegion.Attributes["S1DBConn"].Value;
            CompareFromConfig.S1QueryID = nodeRegion.Attributes["S1QueryID"]==null?null: nodeRegion.Attributes["S1QueryID"].Value;
            //CompareFromConfig.S1FileLocation = nodeRegion.Attributes["S1FileLoc"] == null ? null : nodeRegion.Attributes["S1FileLoc"].Value;
            if (nodeRegion.Attributes["S1OpicsRepFileLoc"] != null)
                CompareFromConfig.S1OpicsRepFileLoc = nodeRegion.Attributes["S1OpicsRepFileLoc"].Value;
            if (nodeRegion.Attributes["S1CSVDelim"] != null)
                CompareFromConfig.S1CSVDelim = nodeRegion.Attributes["S1CSVDelim"].Value;


            CompareFromConfig.S2Type = nodeRegion.Attributes["S2Type"] == null ? null : nodeRegion.Attributes["S2Type"].Value;
            CompareFromConfig.S2DBConn = nodeRegion.Attributes["S2DBConn"] == null ? null : nodeRegion.Attributes["S2DBConn"].Value;
            CompareFromConfig.S2QueryID = nodeRegion.Attributes["S2QueryID"] == null ? null : nodeRegion.Attributes["S2QueryID"].Value;
            //CompareFromConfig.S2FileLocation = nodeRegion.Attributes["S2FileLoc"] == null ? null : nodeRegion.Attributes["S2FileLoc"].Value;
            if (nodeRegion.Attributes["S2OpicsRepFileLoc"] != null)
                CompareFromConfig.S2OpicsRepFileLoc = nodeRegion.Attributes["S2OpicsRepFileLoc"].Value;
            if (nodeRegion.Attributes["S2CSVDelim"] != null)
                CompareFromConfig.S2CSVDelim = nodeRegion.Attributes["S2CSVDelim"].Value;

            CompareFromConfig.OFileLocation = nodeRegion.Attributes["OFileLoc"].Value;
            CompareFromConfig.KeyFields = nodeRegion.Attributes["KeyFields"] == null ? null : nodeRegion.Attributes["KeyFields"].Value;
            CompareFromConfig.ShowFields = nodeRegion.Attributes["ShowFields"] == null ? null : nodeRegion.Attributes["ShowFields"].Value;
            CompareFromConfig.CompareFields = nodeRegion.Attributes["CompareFields"] == null ? null : nodeRegion.Attributes["CompareFields"].Value;
            CompareFromConfig.RowFields = nodeRegion.Attributes["RowFields"] == null ? null : nodeRegion.Attributes["RowFields"].Value;
            CompareFromConfig.ColumnFields = nodeRegion.Attributes["ColumnFields"] == null ? null : nodeRegion.Attributes["ColumnFields"].Value;


            if (nodeRegion.Attributes["OutputFilter"] != null)
                CompareFromConfig.OutputFilter = nodeRegion.Attributes["OutputFilter"].Value;

            if (nodeRegion.Attributes["OutputOrderBy"] != null)
                CompareFromConfig.OutputOrderBy = nodeRegion.Attributes["OutputOrderBy"].Value;

            if (nodeRegion.Attributes["OutputFilterApply"] != null)
                CompareFromConfig.OutputFilterApply = bool.Parse(nodeRegion.Attributes["OutputFilterApply"].Value);

            CompareFromConfig.S1XMlIndex = nodeRegion.Attributes["S1XMlIndex"]?.Value;
            CompareFromConfig.S2XMlIndex = nodeRegion.Attributes["S2XMlIndex"]?.Value;

            if (nodeRegion.Attributes["S1ActualPath"] != null)
            {
                CompareFromConfig.S1FileLocation = nodeRegion.Attributes["S1ActualPath"].Value;
            }
            else
            {
                CompareFromConfig.S1FileLocation = nodeRegion.Attributes["S1FileLoc"]?.Value;
            }
            if (nodeRegion.Attributes["S2ActualPath"] != null)
            {
                CompareFromConfig.S2FileLocation = nodeRegion.Attributes["S2ActualPath"].Value;
            }
            else
            {
                CompareFromConfig.S2FileLocation = nodeRegion.Attributes["S2FileLoc"]?.Value;
            }

            return CompareFromConfig; 
        }
    }
}
