using com.Mars.Constants;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Xml;

namespace TestFlowClient.Mars.TigerConfig
{
    internal class AppConfigReader
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(AppConfigReader));
        private static Configuration gCurrentConfiguration = null;
        private static Configuration GetConfigurationInstance()
        {
            if (gCurrentConfiguration == null)
            {
                string exeConfigPath = typeof(AppConfigReader).Assembly.Location;
                gCurrentConfiguration = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            }
            Logger.Info("GetConfigurationInstance--------------", string.Format("path:[{0}] execonfigPath:[{1}]", typeof(AppConfigReader).Assembly.Location,
                    gCurrentConfiguration == null ? "null" : gCurrentConfiguration.FilePath));
            return gCurrentConfiguration;
        }

        private static NameValueCollection ConvertNameValueSectionToCollection(ConfigurationSection objSection, string strSectionName)
        {
            Logger.logBegin("ConvertNameValueSectionToCollection");

            try
            {
                if (objSection == null)
                {
                    Logger.Error("GetSpecialNodeValueFromNameValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SECTION_SPECIAL) + ":[{0}]", strSectionName));
                    return null;
                }
                string strParamsSectionRawXml = objSection.SectionInformation.GetRawXml();
                XmlDocument objSectionXmlDoc = new XmlDocument();
                objSectionXmlDoc.Load(new StringReader(strParamsSectionRawXml));
                NameValueSectionHandler objHandler = new NameValueSectionHandler();

                NameValueCollection handlerCreatedCollection =
                    objHandler.Create(null, null, objSectionXmlDoc.DocumentElement) as NameValueCollection;
                return handlerCreatedCollection;
            }
            finally
            {
                Logger.logEnd("ConvertNameValueSectionToCollection");
            }


        }

        private static string GetAppSettingByKey(string strKey, string strDefault)
        {
            Logger.logBegin("GetAppSettingByKey");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetRegApplications", "No/Wrong Appsetting Node of application config file");
                return null;
            }
            string strValue = strDefault;
            try
            {
                strValue = objConfig.AppSettings.Settings[strKey].Value;
            }
            catch (Exception)
            {
                strValue = null;
            }

            Logger.logEnd("GetAppSettingByKey");
            return strValue ?? strDefault;
        }

        internal static string[] GetSkippableKeyWord()
        {
            string strSkp = GetAppSettingByKey(SystemConstant.CNST_APPSETTING_APPLICATION_SKIPPABLE, null);
            if (strSkp == null) return new string[] { };
            return strSkp.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static bool GetConfigServerMonitorURLInfo(ref string strProtocol, ref string strHost, ref string strServiceName, ref string strError)
        {
            Logger.logBegin("GetConfigServerMonitorURLInfo");
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_MONITORSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_MONITORSERVICE);
            string[] strProtocols = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocols == null) || (strProtocols.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING));
            }
            string[] strHosts = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHosts == null) || (strHosts.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
            }
            string[] strPorts = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
            if ((strPorts == null) || (strPorts.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
            }
            string[] strServiceNames = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceNames == null) || (strServiceNames.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
            }
            strProtocol = strProtocols[0];
            strHost = strHosts[0];
            strServiceName = strServiceNames[0];
            return true;
        }

        internal static string GetConfigServerMonitorURLInfo()
        {
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_MONITORSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_MONITORSERVICE);
            string[] strProtocol = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocol == null) || (strProtocol.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING));
            }
            string[] strHost = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHost == null) || (strHost.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
            }
            string[] strPort = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
            if ((strPort == null) || (strPort.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
            }
            string[] strServiceName = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceName == null) || (strServiceName.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
            }
            string strResult = string.Format("{0}://{1}:{2}/{3}", strProtocol[0], strHost[0], strPort[0], strServiceName[0]);

            Logger.logEnd("GetConfigServerURLInfo");
            return strResult;
        }


        internal static bool GetConfigServerURLInfo(ref string strProtocol, ref string strHost, ref string strServiceName, ref string strError)
        {
            Logger.logBegin("GetConfigServerURLInfo reference mode");
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            string[] strProtocols = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocols == null) || (strProtocols.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING);
                return false;
            }
            string[] strHosts = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHosts == null) || (strHosts.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING);
                return false;
            }

            string[] strServiceNames = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceNames == null) || (strServiceNames.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING);
                return false;
            }

            strProtocol = strProtocols[0];
            strServiceName = strServiceNames[0];
            strHost = strHosts[0];
            return true;
        }

        internal static string GetConfigServerURLInfo()
        {
            Logger.logBegin("GetConfigServerURLInfo");
            //ServicebasicInformation objServiceInfo = new ServicebasicInformation();
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            string[] strProtocol = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocol == null) || (strProtocol.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING));
            }
            string[] strHost = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHost == null) || (strHost.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
            }
            string[] strPort = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
            if ((strPort == null) || (strPort.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
            }
            string[] strServiceName = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceName == null) || (strServiceName.Length == 0))
            {
                throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
            }
            string strResult = string.Format("{0}://{1}:{2}/{3}", strProtocol[0], strHost[0], strPort[0], strServiceName[0]);

            Logger.logEnd("GetConfigServerURLInfo");
            return strResult;
        }

        internal static string[] GetKeywordsNotRequireData()
        {
            string strSetting = GetAppSettingByKey(SystemConstant.CNST_APPSETTING_APPLICATION_KEYWORDNOTREQUIREDATA, "");
            Logger.Info("GetKeywordsNotRequireData", string.Format("returns:[{0}]", strSetting));
            if (string.IsNullOrEmpty(strSetting))
                strSetting = "";

            return strSetting.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static System.Drawing.Color GetErrorCellColor()
        {
            string strErrorColor = GetAppSettingByKey("COLOR_FOR_ERROR_CELL", "#FFFF00");
            System.Windows.Media.Color errorcolor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(strErrorColor);
            if (errorcolor == null)
            {
                return default(System.Drawing.Color);
            }

            return System.Drawing.Color.FromArgb(errorcolor.A, errorcolor.R, errorcolor.G, errorcolor.B);
        }

        public static string GetFillTableAutoCheckError()
        {
            return GetAppSettingByKey("FillTableAutoCheckError", "false");
        }
    }
}
