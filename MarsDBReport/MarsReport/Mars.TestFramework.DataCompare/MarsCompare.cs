using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Mars.DataLayer;
using System.Configuration;
using Mars.Utility;
using Mars.Model;
using System.Data.EntityClient;
using DomUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System.IO;
//using DomUtil;

namespace Mars.TestFramework.DataCompare
{
    public class MarsCompare
    {
        ComparewithID compareConfig;
        private static MLogger Logger = MLogger.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        
        public MarsCompare()
        {
            getDatabasePwd();
        }

        public static void getDatabasePwd()
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                if (cfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD] == null)
                {
                    cfg.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    cfg.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("AppSetting");
                }
                string strEncoded = cfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value.ToString();
                string strDecoded = Mars.Securities.MarsEncodePwd.DecodeString(strEncoded);
                MarsEntities.Database_Password = strDecoded;
                string strCnn = cfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();
                MarsEntities.Database_ConnectionString = strCnn;
                //Logger.Info("getDatabasePwd", string.Format("Cnn str:[{0}] EncodedPwd:[{1}]", strCnn, strEncoded));
            }
            catch (Exception e)
            {
                Logger.Error("getDatabasePwd", string.Format("Can't get database password setting from config file. \r\nException:[{0}]", e.Message));
                return;
            }


        }

        internal bool GetFileRequirements(string compareId, out bool file1IsRequired, out bool file2IsRequired)
        {
            compareConfig = new ComparewithID();
            file1IsRequired = true;
            file2IsRequired = true;
            compareConfig = new ComparewithID();
            bool rc = RetrieveCompareInfo(compareId, "DUMMY", "DUMMY", "DUMMY");
            if (rc == true)
            {
                if (compareConfig.S1Type.Equals("DATABASE"))
                    file1IsRequired = false;
                if (compareConfig.S2Type.Equals("DATABASE"))
                    file2IsRequired = false;
            }

            return rc;
        }

        public string  RunCompare(string compareId, string filePath1, string filePath2, string oFile, 
            ref DataCompareError error,
            XmlDocument sourceDoc =null,
            bool isWebCommandLine=false)
        {
            string resultFileName = "";
            compareConfig = new ComparewithID();

            bool rc = RetrieveCompareInfo(compareId, filePath1, filePath2, oFile, sourceDoc, isWebCommandLine);
            //if (isWebCommandLine)
            //    compareConfig.OFileLocation = filePath1;
            if (error == null)
                error = new DataCompareError();
            if (rc == false)
            {
                error.Status = false;
                error.Message = "Compare ID not found";
                return null;
            }
            try
            {
                resultFileName = ExcecuteCompareJob(ref error, isWebCommandLine);
                Logger.Info("RunCompare",$"generated file|{resultFileName}|Error|{error}");
                error.refFileNameWithPath = resultFileName;
            }
           catch(Exception ex)
            {
                Logger.Error("RunCompare", $"{ex.Message}|{ex.StackTrace}");
                error = new DataCompareError();
                error.Status = false;
                error.Message = ex.Message;
            }
            return resultFileName;
        }

        private string  ExcecuteCompareJob(ref DataCompareError error, bool isFromWebCommand = false)
        {
            return ExecuteCompare.ExecuteCompareProgram(compareConfig, ref error, isFromWebCommand);
        }
        #region tiger copied 
        private const string CNST_DEFAULT_DATABASE_SCHEMA_KEY = "DefaultSchema";
        public static string DbPasswordDecoded(Configuration currentExeCfg)
        {

            {
                if (currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD] == null)
                {
                    currentExeCfg.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    currentExeCfg.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("AppSetting");
                    return "";
                }
                string strEncoded = currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value.ToString();
                string strDecoded = Mars.Securities.MarsEncodePwd.DecodeString(strEncoded);
                return strDecoded;
            }
        }
        private static string MarsEntiesConnString(Configuration currentExeCfg)
        {

            {
                if (currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"] == null) return null;
                return currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();
            }
        }
        public static bool InitSchemaChangingAndDBConnection()
        {
            Configuration currentExeCfg = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                if (currentExeCfg.AppSettings.Settings[CNST_DEFAULT_DATABASE_SCHEMA_KEY] != null)
                {
                    MarsEntitiesExtends.NewSchemaName = currentExeCfg.AppSettings.Settings[CNST_DEFAULT_DATABASE_SCHEMA_KEY].Value;
                }
                else
                {
                    MarsEntitiesExtends.NewSchemaName = "TESTIDE2";
                }
                ///get Connection string from configuration file
                /// 
                string strPassword = DbPasswordDecoded(currentExeCfg);
                string strConnString = MarsEntiesConnString(currentExeCfg);
                if (string.IsNullOrEmpty(strConnString)) return false;

                strConnString = string.Format(strConnString, strPassword);
                MarsEntitiesExtends.connectionBuilder = new EntityConnectionStringBuilder(strConnString);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InitSchemaChangingAndDBConnection", string.Format("exception:[{0}]", e.Message));
                return false;
            }
        }
        #endregion //tiger copied 

        private bool RetrieveCompareInfo(string compareId, string filePath1, string filePath2, string oFile, XmlDocument sourceDoc = null, bool isWebCompare=false)
        {
            InitSchemaChangingAndDBConnection();
            //XmlDocument xmlDoc = DomUtil.DomHelper.ReadXmlDoc();
            //string xpath = string.Format("//Compare[@ID='{0}']", compareId);

            XmlDocument xmlDoc = sourceDoc == null ? DomHelper.ReadXmlDoc() : sourceDoc;
            string xpath = string.Format("//Compares//Compare[@ID='{0}']", compareId);

            XmlNodeList xnList = xmlDoc.SelectNodes(xpath);

            if (xnList.Count == 0)
                return false;

            XmlNode node = xnList[0];

            string ss = node.Attributes["ID"].Value;

            compareConfig.CompareID = node.Attributes["ID"].Value;

            compareConfig.S1DBConn = node.Attributes["S1DBConn"]?.Value;
            compareConfig.S1Type = node.Attributes["S1Type"].Value;
            compareConfig.S1QueryID = node.Attributes["S1QueryID"]?.Value;
            if (node.Attributes["S1ActualPath"] != null)
            {
                compareConfig.S1FileLocation = node.Attributes["S1ActualPath"]?.Value;
            }
            else
            {
                compareConfig.S1FileLocation = node.Attributes["S1FileLoc"]?.Value;
            }            
            if (compareConfig.S1Type.Equals("DATABASE"))
            {
                // Get node containing database info from connection node
                string dbConnXpath = string.Format("//DBConn[@ID='{0}']", compareConfig.S1DBConn);
                DBConnectionwithID dbConnConfig = new DBConnectionwithID();
                XmlNodeList dbList = xmlDoc.SelectNodes(dbConnXpath);
                XmlNode dbNode = dbList[0];

                // Fill DB structure
                dbConnConfig.ConnectionID = dbNode.Attributes["ID"].Value;
                dbConnConfig.DatabaseType = dbNode.Attributes["Type"].Value;
                dbConnConfig.Host = dbNode.Attributes["Host"].Value;
                dbConnConfig.Port = dbNode.Attributes["Port"].Value;
                dbConnConfig.Protocol = dbNode.Attributes["Protocol"].Value;
                dbConnConfig.ServiceName = dbNode.Attributes["ServiceName"].Value;
                dbConnConfig.UserID = dbNode.Attributes["UserID"].Value;
                dbConnConfig.Password = dbNode.Attributes["Password"].Value;

                compareConfig.S1ConnString = dbConnConfig.BuildConnectionString();
                compareConfig.S1DBType = dbConnConfig.DatabaseType;

                // Retrieve query from query node

                string qConnXpath = string.Format("//Query[@ID='{0}']", compareConfig.S1QueryID);
                XmlNodeList qList = xmlDoc.SelectNodes(qConnXpath);
                XmlNode qNode = qList[0];

                compareConfig.S1Query = qNode.Attributes["Query"].Value;
            }

            compareConfig.S2DBConn = node.Attributes["S2DBConn"]?.Value;
            compareConfig.S2Type = node.Attributes["S2Type"].Value;
            compareConfig.S2QueryID = node.Attributes["S2QueryID"]?.Value;
            if (node.Attributes["S2ActualPath"] != null)
            {
                compareConfig.S2FileLocation = node.Attributes["S2ActualPath"]?.Value;
            }
            else
            {
                compareConfig.S2FileLocation = node.Attributes["S2FileLoc"]?.Value;
            }
            compareConfig.S1XMlIndex = node.Attributes["S1XMlIndex"]?.Value;
            compareConfig.S2XMlIndex = node.Attributes["S2XMlIndex"]?.Value;
            
            //compareConfig.S2FileLocation = node.Attributes["S2FileLoc"].Value;

            if (compareConfig.S2Type.Equals("DATABASE"))
            {
                // Get node containing database info from connection node
                string dbConnXpath = string.Format("//DBConn[@ID='{0}']", compareConfig.S2DBConn);
                DBConnectionwithID dbConnConfig = new DBConnectionwithID();
                XmlNodeList dbList = xmlDoc.SelectNodes(dbConnXpath);
                XmlNode dbNode = dbList[0];

                // Fill DB structure
                dbConnConfig.ConnectionID = dbNode.Attributes["ID"].Value;
                dbConnConfig.DatabaseType = dbNode.Attributes["Type"].Value;
                dbConnConfig.Host = dbNode.Attributes["Host"].Value;
                dbConnConfig.Port = dbNode.Attributes["Port"].Value;
                dbConnConfig.Protocol = dbNode.Attributes["Protocol"].Value;
                dbConnConfig.ServiceName = dbNode.Attributes["ServiceName"].Value;
                dbConnConfig.UserID = dbNode.Attributes["UserID"].Value;
                dbConnConfig.Password = dbNode.Attributes["Password"].Value;

                compareConfig.S2ConnString = dbConnConfig.BuildConnectionString();
                compareConfig.S2DBType = dbConnConfig.DatabaseType;

                // Retrieve query from query node

                string qConnXpath = string.Format("//Query[@ID='{0}']", compareConfig.S2QueryID);
                XmlNodeList qList = xmlDoc.SelectNodes(qConnXpath);
                XmlNode qNode = qList[0];

                compareConfig.S2Query = qNode.Attributes["Query"].Value;
            }

            if (compareConfig.S1Type.Equals("REPORT") && node.Attributes["S1OpicsRepFileLoc"] != null)
            {
                compareConfig.S1OpicsRepFileLoc = node.Attributes["S1OpicsRepFileLoc"].Value;
            }

            if (compareConfig.S2Type.Equals("REPORT") && node.Attributes["S2OpicsRepFileLoc"] != null)
            {
                compareConfig.S2OpicsRepFileLoc = node.Attributes["S2OpicsRepFileLoc"].Value;
            }

            compareConfig.OFileLocation = node.Attributes["OFileLoc"]?.Value;
            compareConfig.KeyFields = node.Attributes["KeyFields"]?.Value;
            compareConfig.ShowFields = node.Attributes["ShowFields"]?.Value;
            compareConfig.CompareFields = node.Attributes["CompareFields"]?.Value;
            compareConfig.RowFields = node.Attributes["RowFields"]?.Value;
            compareConfig.ColumnFields = node.Attributes["ColumnFields"]?.Value;

            if (filePath1 != null && filePath1.Trim().Length > 5)
            {
                compareConfig.S1FileLocation = filePath1;
            }

            if (filePath2 != null && filePath2.Trim().Length > 5)
            {
                compareConfig.S2FileLocation = filePath2;
            }
            if (!isWebCompare)
            compareConfig.OFileLocation = @"c:\temp";

            string folder = ConfigurationManager.AppSettings["DataCompareResultFolder"];

            if (folder != null)
                compareConfig.OFileLocation = folder;

            if (oFile != null && oFile.Trim().Length > 5)
            {
                if(Path.HasExtension(oFile))
                    compareConfig.OfileName = oFile;
            }
            if (isWebCompare)
            {
                compareConfig.OFileLocation = oFile;
            }
            Logger.Info("RetrieveCompareInfo", compareConfig.ToString());
            return true;
        }
        private string ExtractFields(string compareFields)
        {
            string returnString = "";
            string workString = "";
            if (string.IsNullOrEmpty(compareFields)) return null;
            if (compareFields.StartsWith(V2_MARKER))
            {
                int index = compareFields.IndexOf(V2_MARKER);
                workString = (index < 0)
                    ? compareFields
                    : compareFields.Remove(index, V2_MARKER.Length);
                string[] rows = workString.Split(';');


                foreach (string row in rows)
                {
                    string[] values = row.Split('|');

                    string fieldName = values[0];

                    returnString += fieldName + ", ";
                }
                returnString = returnString.TrimEnd(' ').TrimEnd(',');
            }
            else
                returnString = compareFields;
            return returnString;
        }

        private static string V2_MARKER = "==";
    }
}
