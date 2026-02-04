using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Data.OracleClient;
using System.IO;
using log4net;
using Mars.SimpleLogger;
using System.Diagnostics;
using System.Reflection;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.MarsConfig
{
    public class MarsConfig
    {
        public const string MARS_HOME = "MARS_HOME";

        private string marsCommonConfigFile;
        private string marsUserConfigFile;
        private string marsEnvironment;
       
        private static MarsConfig marsConfigInstance;

        private static XDocument marsUserConfig;
        private static XDocument marsCommonConfig;

        private static ILog logger=MLogger.GetLogger("MarsConfig").mobjLog;

        public static bool isUsingCommand { get; set; } = false;

        public  Dictionary<string, string> AppSettings { get; set; }

        public static DatabaseConnectionDetails currentDBInfo = null;

        public MarsConfig()
        {

        }

        public MarsConfig(string marsCommonConfigFile, string marsUserConfigFile, string marsEnvironment)
        {
            this.marsCommonConfigFile = marsCommonConfigFile;
            this.marsUserConfigFile = marsUserConfigFile;
            this.marsEnvironment = marsEnvironment;

           
            //AppConfig.Change(marsSystemConfigFile);
        }

        public static MarsConfig Configure(string marsCommonConfigFile, string marsUserConfigFile, string marsEnvironment)
        {
            if (marsConfigInstance == null)
                marsConfigInstance = new MarsConfig(marsCommonConfigFile, marsUserConfigFile, marsEnvironment);

            marsUserConfig = XDocument.Load(marsUserConfigFile);
            marsCommonConfig = XDocument.Load(marsCommonConfigFile);

            marsConfigInstance.AppSettings = GetAppSettings();
            return marsConfigInstance;
        }

        public static void EventLogWrite(string msg)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(msg, EventLogEntryType.Information, 101, 1);
            }
        }

        public static MarsConfig Configure(string marsEnvironment)
        {
            string userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
            Console.WriteLine($"path|{userProfilePath}");
            logger.Info($"Configure\tGoto path|{userProfilePath}|env is|{marsEnvironment}");

            if (logger == null)
                logger = new Mars.SimpleLogger.SimpleLogger().Setup(userProfilePath + @"\AppData\Local\MarsAutomation\Log\MarsConfig.log", "MarsConfigLogger_" + System.AppDomain.CurrentDomain.FriendlyName);
            logger.Info("MarsConfigLogger Started");

            string marsHomeFolder = MarsConfig.GetMarsHome(true);
            if (marsHomeFolder == null)
            {
                logger.Error("Environment variable MARS_HOME is not set, program can not start");
                throw new System.ArgumentException("Environment variable MARS_HOME is not set, program can not start", System.AppDomain.CurrentDomain.FriendlyName);
            }

            string marsUserConfigFile = marsHomeFolder + @"\Config\MarsUser.config";
            string marsCommonConfigFile = marsHomeFolder + @"\Config\MarsCommon.config";
            Console.WriteLine($"marsUserConfigFile|{marsUserConfigFile}");

            if ((!System.IO.File.Exists(marsUserConfigFile))
                || (!System.IO.File.Exists(marsCommonConfigFile)))
                throw new Exception($"{marsUserConfig} or {marsCommonConfigFile} is not exists.");

            //EventLogWrite("marsUserConfigFile: " + marsUserConfigFile);
            //EventLogWrite("marsCommonConfigFile: " + marsCommonConfigFile);

            return Configure(marsCommonConfigFile, marsUserConfigFile, marsEnvironment);
        }

        private static Dictionary<string, string> GetAppSettings()
        {
            logger.Info("GetAppSettings");
            Dictionary<string, string> appSettings = new Dictionary<string, string>();
            appSettings = GetAppSettingsImp(marsCommonConfig, appSettings);
            appSettings = GetAppSettingsImp(marsUserConfig, appSettings);

            return appSettings;
        }


        private static Dictionary<string, string> GetAppSettingsImp(XDocument config, Dictionary<string, string> appSettings)
        {
            logger.Info("GetAppSettingsImp");
            var appsettingsSection = GetMarsAppSettingsSection(config);

            foreach (var appSetting in appsettingsSection)
            {
                string key = appSetting.Attribute("key").Value;
                string value = appSetting.Attribute("value").Value;

                if (appSettings.Keys.Contains(key))
                    appSettings[key] = value;
                else
                 appSettings.Add(key, value);
            }

            return appSettings;
        }




        /*
        public string getAppSetting(string key)
        {
            string value = "";

            // First try getting it from MarsUser.config

            try
            {
                value = value = getXMLSetting(key);
            }
            catch (Exception )
            {
                try
                {
                    var currentExeCfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    value = currentExeCfg.AppSettings.Settings[key].Value;
                }

                catch (Exception)
                {
                    throw new System.ArgumentException("Setting not found", "key");
                }

            }
                      
        
            return value;
        }

             */


        public string GetDefaultErrorFile()
        {
            string folder = "";
            logger.Info("GetDefaultErrorFile");
            folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\MarsAutomation\log\";

            Directory.CreateDirectory(folder);
            string logFile = folder + @"ERROLOG.log";
            logger.Info("File:" + logFile);
            return logFile;
        }


        /*
        private IEnumerable<XElement> GetMarsEnvironment()
        {
            XDocument xdoc = XDocument.Load(marsUserConfigFile);

            var marsEnvironments = xdoc.Element("configuration").Element("MarsEnvironments").Elements();
            var env = from e in marsEnvironments where e.Attribute("name").Value.Equals(marsEnvironment) select e;

            return env;
        }
        */

        private IEnumerable<XElement> GetMarsEnvironment(XDocument config)
        {
            XDocument xdoc = config;

            var marsEnvironments = xdoc.Element("configuration").Element("MarsEnvironments").Elements();
            //var env = from e in marsEnvironments where e.Attribute("name").Value.Equals(marsEnvironment) select e;
            var currentElement = xdoc.Element("configuration").Element("MarsEnvironments").Attribute("name").Value;
            var env = from e in marsEnvironments where e.Attribute("name").Value.Equals(currentElement) select e;


            return env;
        }

        private static IEnumerable<XElement> GetMarsAppSettingsSection(XDocument config)
        {
            XDocument xdoc = config;

            var marsAppSection = xdoc.Element("configuration").Element("appSettings").Elements();

            return marsAppSection;
        }

        public string GetApiUrl()
        {
            logger.Info("GetApiUrl");
            string url = "";

            try
            {
                url = GetApiUrlImp(marsUserConfig);
            }
            catch (Exception)
            {
                url = GetApiUrlImp(marsCommonConfig);
            }

            logger.Info("Url:" + url);
            return url;
        }

        private string GetApiUrlImp(XDocument config)
        {
            string url = "";

            var env = GetMarsEnvironment(config);

            var urlElement = env.Elements("WebApi").FirstOrDefault();

            url = urlElement.Attribute("url").Value;

            return url;
        }

        /*********************/
        // New configuration code
        public string GetApiOption()
        {
            logger.Info("GetApiOption");
            string option = "";

            try
            {
                option = GetApiOptionImp(marsUserConfig);
            }
            catch (Exception)
            {
                option = GetApiOptionImp(marsCommonConfig);
            }

            logger.Info("Url:" + option);
            return option;
        }

        private string GetApiOptionImp(XDocument config)
        {
            string option = "";

            var env = GetMarsEnvironment(config);

            var urlElement = env.Elements("WebApiOption").FirstOrDefault();

            option = urlElement.Attribute("option").Value;

            return option;
        }

        public string GetApiSchema()
        {
            logger.Info("GetApiSchema");
            string schema = "";

            try
            {
                schema = GetApiSchemaImp(marsUserConfig);
            }
            catch (Exception)
            {
                schema = GetApiSchemaImp(marsCommonConfig);
            }

            logger.Info("schema:" + schema);
            return schema;
        }

        private string GetApiSchemaImp(XDocument config)
        {
            string schema = "";

            var env = GetMarsEnvironment(config);

            var urlElement = env.Elements("WebApiSchema").FirstOrDefault();

            schema = urlElement.Attribute("schema").Value;

            return schema;
        }
        /*********************/



        public string GetLoggerPath()
        {
            string path = "";
            logger.Info("GetLoggerPath");
            try
            {
                path = GetLoggerPathImp(marsUserConfig);
            }
            catch (Exception)
            {
                path = GetLoggerPathImp(marsCommonConfig);
            }

            if (path.Contains("%"))
            {
                var str = path.Split('%');
                string envVarValue = Environment.GetEnvironmentVariable(str[1]);
                path = str[0] + envVarValue + str[2];
            }

            logger.Info("path:" + path);
            return path;
        }

        public string GetLoggerPathImp(XDocument config)
        {
            string url = "";
            var env = GetMarsEnvironment(config);

            var pathElement = env.Elements("LogPath").FirstOrDefault();

            url = pathElement.Attribute("path").Value;

            return url;
        }

        public static string GetMarsHome(bool isUseDefault = false)
        {
            string path = "";
            logger.Info("GetMarsHome");

            path = Environment.GetEnvironmentVariable("MARS_HOME");
            if (((string.IsNullOrEmpty(path))
                ||(!System.IO.Directory.Exists(path)))
                && isUseDefault
                )
            {
                string tmpPth = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(tmpPth);
                tmpPth = Uri.UnescapeDataString(uri.Path);
                path = System.IO.Path.GetDirectoryName(tmpPth);
            }
            
            logger.Info("GetMarsHome path:" + path);
            return path;
        }

        /*
        public string getXMLSetting(string key)
        {
            string value = "";

            //Load xml
            var env = GetMarsEnvironment();
            value = env.Elements(key).FirstOrDefault().Value;
          
            return value;
        }
        */


        public List<RegApplication>  GetRegApplications()
        {
            logger.Info("GetRegApplications");
            List<RegApplication> regAppList = new List<RegApplication>();
            try
            {
                regAppList = GetRegApplicationsImp(marsUserConfig);
            }
            catch (Exception)
            {
                regAppList = GetRegApplicationsImp(marsCommonConfig);
            }

            logger.Info("count:" + regAppList.Count);

            return regAppList;
        }

        private  List<RegApplication> GetRegApplicationsImp(XDocument config)
        {
            List<RegApplication> regAppList = new List<RegApplication>();
            var env = GetMarsEnvironment(config);
            var regAppications = env.Elements("RegistedApplications").FirstOrDefault().Elements();

            foreach (var regApp in regAppications)
            {
                RegApplication regApplication = new RegApplication();
                regApplication.AppName = regApp.Attribute("AppName").Value;
                regApplication.command = regApp.Attribute("command").Value;
                regApplication.path = regApp.Attribute("path").Value;
                regApplication.identifier = regApp.Attribute("identifier").Value;
                regApplication.ApplicationType = regApp.Attribute("ApplicationType").Value;
                regApplication.ExtraRequirement = regApp.Attribute("ExtraRequirement").Value;
                regApplication.ExtraPopupMenu = regApp.Attribute("ExtraPopupMenu").Value;
                regApplication.Mode = regApp.Attribute("Mode").Value;

                regAppList.Add(regApplication);
            }
            return regAppList;
        }

        /*
        public string GetLoggerPath(string loggerName)
        {
            string loggerPath = "";
            var env = GetMarsEnvironment();

            var loggers = env.Elements("Loggers").FirstOrDefault().Elements();
            var logger = from l in loggers where l.Attribute("name").Value.Equals(loggerName) select l;
            loggerPath = logger.Attributes("path").FirstOrDefault().Value;

            return loggerPath;
        }
        */


        public DatabaseConnectionDetails GetDatabaseConnectionDetails(string modifiedPassword = null, string dbIdx=null)
        {
            DatabaseConnectionDetails databaseConnectionDetails  = null;

            try
            {
                databaseConnectionDetails = GetDatabaseConnectionDetailsImp(marsUserConfig, modifiedPassword, dbIdx);
            }
            catch (Exception)
            {
                //databaseConnectionDetails = GetDatabaseConnectionDetailsImp(marsCommonConfig, modifiedPassword);
                throw new Exception($"Please check the DB conntion information from {this.marsUserConfigFile} is right and connectable.");
            }
            currentDBInfo = databaseConnectionDetails;
            return databaseConnectionDetails;
        }

        private DatabaseConnectionDetails GetDatabaseConnectionDetailsImp(XDocument config, string modifiedPassword, string dbNameIdx="default")
        {
            DatabaseConnectionDetails databaseConnectionDetails = null;
            var env = GetMarsEnvironment(config);

            //var details = env.Elements("DatabaseConnectionDetails").FirstOrDefault();
            var details = env.Elements("DatabaseConnectionDetails").FirstOrDefault(p => (!string.IsNullOrEmpty(p.Attribute("Name").Value))
                && (p.Attribute("Name").Value.Equals(dbNameIdx ?? "default", StringComparison.OrdinalIgnoreCase)));
            string ConnString = details.Attributes("ConnString").FirstOrDefault().Value;
            string Schema = details.Attributes("Schema").FirstOrDefault().Value;
            string UserName = details.Attributes("UserName").FirstOrDefault().Value;

            string Password = null;
            if (modifiedPassword != null)
                Password = modifiedPassword;
            else
                Password = details.Attributes("Password").FirstOrDefault().Value;

            string Host = details.Attributes("Host").FirstOrDefault().Value;
            string Port = details.Attributes("Port").FirstOrDefault().Value;
            string Type = details.Attributes("Type").FirstOrDefault().Value;
            string ServiceName = details.Attributes("ServiceName").FirstOrDefault().Value;
            string dbIdx = details.Attributes("DBIdx").FirstOrDefault().Value;

            if (ConnString.Trim().Length == 0)
                ConnString = new OracleConnectionStringBuilder(Host, int.Parse(Port), ServiceName, UserName, Password, "", true).Create();
            var entityInfo = details.Attributes("EntityConnString").FirstOrDefault();
            string EntityConnString = "";
            if (entityInfo!=null)
                EntityConnString = details.Attributes("EntityConnString").FirstOrDefault().Value;

            //if (EntityConnString.Trim().Length == 0)
            if (string.IsNullOrEmpty(EntityConnString))
                EntityConnString = BuildEntityConnString(ConnString);

            databaseConnectionDetails = new DatabaseConnectionDetails(Host, Port, ServiceName, Type, ConnString, EntityConnString, Schema, UserName, Password,
                dbIdx);

            return databaseConnectionDetails;
        }

        public ReportConfig GetReportConfig()
        {
            ReportConfig reportConfig = null;

            try
            {
                reportConfig = GetReportConfigImp(marsUserConfig);
               
            }
            catch (Exception)
            {
                reportConfig = GetReportConfigImp(marsCommonConfig);
            }

            return reportConfig;
        }

        private ReportConfig GetReportConfigImp(XDocument config)
        {
            ReportConfig reportConfig = null;
            var env = GetMarsEnvironment(config);

            var details = env.Elements("ReportConfig").FirstOrDefault();
            string ReportPath = "";
            string ReportTemplatePath = "";
            string ReportImagePath = "";
            string ReportTableWord = "";
            try
            {
                ReportPath = details.Attributes("REPORT_PATH").FirstOrDefault().Value;
                ReportTemplatePath = details.Attributes("REPORT_TEMPLATE_PATH").FirstOrDefault().Value;
                ReportImagePath = details.Attributes("REPORT_IMAGE_PATH").FirstOrDefault().Value;
                ReportTableWord = details.Attributes("REPORT_TABLE_WORD").FirstOrDefault().Value;
            }
            catch (Exception e)
            {
                throw new Exception($"Please check settings of REPORT_PATH,REPORT_TEMPLATE_PATH,REPORT_IMAGE_PATH or REPORT_TABLE_WORD");
            }
            reportConfig = new ReportConfig(ReportPath, ReportTemplatePath, ReportImagePath, ReportTableWord);            

            return reportConfig;
        }

        private string BuildEntityConnString(string connString)
        {
            string entityConnString = "";
            entityConnString = "metadata=res://*/Model.MarsModel.csdl|res://*/Model.MarsModel.ssdl|res://*/Model.MarsModel.msl;" +
                "provider=Oracle.ManagedDataAccess.Client;" +
                "provider connection string=" + "\"" +  connString + "\"";


            return entityConnString;
        }



        public DataCompareConfig GetDataCompareConfig()
        {
            DataCompareConfig dataCompareConfig = null;

            try
            {
                dataCompareConfig = GtDataCompareConfigImp(marsUserConfig);
            }
            catch (Exception)
            {
                dataCompareConfig = GtDataCompareConfigImp(marsCommonConfig);
            }

            return dataCompareConfig;
        }

        private DataCompareConfig GtDataCompareConfigImp(XDocument config)
        {
            DataCompareConfig dataCompareConfig = null;

            var env = GetMarsEnvironment(config);

            var dataCompare = env.Elements("DataCompare").FirstOrDefault();

            string TemplatePath = dataCompare.Attributes("TemplatePath").FirstOrDefault().Value;
            string OutputPath = dataCompare.Attributes("OutputPath").FirstOrDefault().Value;

            dataCompareConfig = new DataCompareConfig(TemplatePath, OutputPath);
            return dataCompareConfig;
        } 
       
    }
}
