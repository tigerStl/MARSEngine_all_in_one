#if _forWebClient
using Mars.message.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
//extern alias clientWCF;
//using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#else
using Route2NSEx.src.Marquis.systemUtil;
#endif

using System;
using System.Collections.Generic;
#if !(_forWebClient || _forWebSvc)
using MarsTestFrame.SourceCode.systemUtil;
#endif
using System.Configuration;
using System.Data.EntityClient;

namespace Mars.AutoTestingDriver.db
{
    public class MarsDBEntitiesBuilder
    {

    }

    public class MarsConnectionStrings
    {
        private string connectName;
        private string connectString;
        private string defaultSchema;

        private MarsDBEntitiesBuilder entitiesBuilder;

        public string ConnectName
        {
            get { return connectName; }
            set
            {
                connectName = value;
            }
        }

        public string ConnectionString
        {
            get => connectString;
            set => connectString = value;
        }

        public string DefaultSchema
        {
            get => defaultSchema;
            set => defaultSchema = value;
        }

        public EntityConnection getDBEntitiesConnection;

        internal void InitDBConnectionBuilder()
        {
            throw new NotImplementedException();
        }
    }

#if _forWebSvc

#endif
    public class DatabaseEnvironment
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DatabaseEnvironment));

        private static Dictionary<string, MarsConnectionStrings> marsConnections = null;
        public static Dictionary<string, MarsConnectionStrings> MarsConnections
        {
            get {
                if (marsConnections == null)
                {
                    marsConnections = new Dictionary<string, MarsConnectionStrings>();
                    foreach (ConnectionStringSettings c in System.Configuration.ConfigurationManager.ConnectionStrings)
                    {
                        try
                        {
                            MarsConnectionStrings tmpCnnStrs = null;
                            marsConnections.Add(c.Name, tmpCnnStrs = new MarsConnectionStrings() {
                                ConnectName = c.Name ,
                                ConnectionString = c.ConnectionString,
                                DefaultSchema = string.Compare("Oracle_dbConnection", c.Name,true) ==0 ? System.Configuration.ConfigurationManager.AppSettings["Oracle_default_schema"] 
                                    : string.Compare("MarsEntities", c.Name, true) ==0 ?  System.Configuration.ConfigurationManager.AppSettings[ "defaultSchema"]
                                    : System.Configuration.ConfigurationManager.AppSettings[$"{c.Name}.defaultSchema"] 
                            });
                            // 初始化连接和connection string的model +
                            tmpCnnStrs.InitDBConnectionBuilder();
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                        
                    }
                }
                return marsConnections;
            }
        }

        public const string db_connectionName = "MarsEntities";

#if !(_forWebClient || _forWebSvc)
        private static Configuration currentExeCfg = AppConfigReader.GetConfigurationInstance();
        public static string MarsEntiesConnString
        {
            get
            {
                if (currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"] == null) return null;
                return currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();
            }

        }
#else
#if _forWebClient
        public static string MarsEntiesConnString = null;
#endif
        private static Configuration currentExeCfg = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
        private class MarsConstants
        {
            internal const string CNST_DATABASE_PASSWORD = "DB_PASSWORD";
            internal const string CNST_DATABASE_USERID = "DB_USERID";
            internal const string CNST_DATABASE_HOST = "DB_HOST";
            internal const string CNST_DATABASE_SERVICE_ID = "DATABASE_NAMEORID";
            internal const string CNST_DATABASE_PORT = "DB_PORT";

        }
#endif
        public static string DbPasswordDecoded
        {
            get
            {
#if !_forWebSvc
                if (currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD] == null)
                {
                    currentExeCfg.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    currentExeCfg.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("AppSetting");
                    return "";
                }
                string strEncoded = currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value.ToString();
                string strDecoded = Mars.message.Securities.MarsEncodePwd.DecodeString(strEncoded);
                return strDecoded;
#else
                if (System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_PASSWORD] == null)
                {
                    System.Web.Configuration.WebConfigurationManager.AppSettings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    return "";
                }
                string strEncoded = System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_PASSWORD].ToString();
                string strDecoded = Mars.message.Securities.MarsEncodePwd.DecodeString(strEncoded);
                return strDecoded;
#endif

            }
        }
#if _forWebSvc
//#if _forWebClient
        public static string WebMarsEntitiesOracleConnection(string idx,ref string strError)
        {
            for(int i=0;i<System.Web.Configuration.WebConfigurationManager.ConnectionStrings.Count;i++)
            {
                var itm = System.Web.Configuration.WebConfigurationManager.ConnectionStrings[i];
                if (itm == null)
                {
                    continue;
                }
                
                if (string.Compare(idx, itm.Name, true) == 0)
                {
                    return itm.ConnectionString;
                }
            }
            strError = $"no such connection index {idx}";
            return null;
                //if (System.Web.Configuration.WebConfigurationManager.ConnectionStrings["Oracle_dbConnection"] == null) return null;
                //return System.Web.Configuration.WebConfigurationManager.ConnectionStrings["Oracle_dbConnection"].ToString();
            //if (System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MarsEntities"] == null) return null;                
            //    return System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MarsEntities"].ToString();
            
        }

        /// <summary>
        /// 多数据库链接管理
        /// </summary>
        /// 


        public static string WebMarsEntitiesDBUserID
        {
            get
            {
                if (System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_USERID] == null) return null;
                return System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_USERID].ToString();
            }
        }

        public static string WebMarsEntitiesDBHost
        {
            get
            {
                if (System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_HOST] == null) return null;
                return System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_HOST].ToString();
            }
        }
        public static string WebMarsEntitiesDBServiceID
        {
            get
            {
                if (System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_SERVICE_ID] == null) return null;
                return System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_SERVICE_ID].ToString();
            }
        }

        public static string WebMarsEntitiesDBPort
        {
            get
            {
                if (System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_PORT] == null) return null;
                return System.Web.Configuration.WebConfigurationManager.AppSettings[MarsConstants.CNST_DATABASE_PORT].ToString();
            }
        }

#endif
        public static bool InitSchemaChangingAndDBConnection(string strDBConnectionIdx = "MarsEntities")
        {
            //EntityConnectionStringBuilder dd = new EntityConnectionStringBuilder("metadata=res://*/Model.MarsModel.csdl|res://*/Model.MarsModel.ssdl|res://*/Model.MarsModel.msl;provider=Oracle.ManagedDataAccess.Client;provider connection string=';DATA SOURCE=TESTIDELOCAL;PASSWORD=TESTMARS;USER ID=TESTMARS';");
            //EntityConnection objCnn = MarsEntitiesExtends.createConnection("TESTMARS", dd, "Model.MarsModel");
            //MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(objCnn);
            //try
            //{
            //    var user = (from q in objDBCntx.V_STORYBOARD_TEST_FULLVISION
            //                where q.PROJECT_NAME == "FHLBC Debt Bonds"
            //                select q).FirstOrDefault();
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error("OnStartUp",string.Format("Exceptions:[{0}]",ex.Message),ex);
            //}
#if _forWebSvc
            Logger.logBegin("InitSchemaChangingAndDBConnection");
            MarsEntitiesExtends.NewSchemaName = System.Web.Configuration.WebConfigurationManager.AppSettings["Oracle_default_schema"];
            MarsEntitiesExtends.InitDBInfo(System.Web.Configuration.WebConfigurationManager.AppSettings,
                System.Web.Configuration.WebConfigurationManager.ConnectionStrings);                       
#else
#if _forWebClient
            //MarsEntitiesExtends.NewSchemaName = System.Configuration.ConfigurationManager.AppSettings
#else
            if (string.Compare("MarsEntities", strDBConnectionIdx, true) == 0)
            {
                MarsEntitiesExtends.NewSchemaName = AppConfigReader.GetDefaultSchemaForOracle();
            }
            else
            {
                //多数据库连接
                MarsEntitiesExtends.NewSchemaName = AppConfigReader.GetDefaultSchemaForOracle(strDBConnectionIdx+".");
            }
#endif

#endif
            ///get Connection string from configuration file
            /// 
            string strPassword = DbPasswordDecoded;
#if _forWebSvc
            string strError = "";
            string strConnString = WebMarsEntitiesOracleConnection(strDBConnectionIdx,ref strError);
#else
            string strConnString = MarsEntiesConnString;
#endif
            if (string.IsNullOrEmpty(strConnString)) return false;
#if _forWebSvc
            // (DESCRIPTION = (ADDRESS_LIST=(ADDRESS = (PROTOCOL = TCP)(HOST = {4})(PORT = {3}))) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = {2}) )  );Connection Timeout=360;PASSWORD={0};USER ID={1}
            strConnString = string.Format(strConnString, strPassword, WebMarsEntitiesDBUserID,
                WebMarsEntitiesDBServiceID, WebMarsEntitiesDBPort, WebMarsEntitiesDBHost);            
            MarsEntitiesExtends.connectionBuilder = new EntityConnectionStringBuilder(strConnString);
#else
            MarsEntitiesExtends.connectionBuilder = new EntityConnectionStringBuilder(strConnString);
#endif


            return true;
        }
    }
}
