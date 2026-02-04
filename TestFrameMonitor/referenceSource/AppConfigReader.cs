#if tigerServer
using com.Mars.Config;
using com.Mars.KeyWords.KeyWordObject;
#endif
using com.Mars.Constants;
using MarsTestFrame.CommuniteServer;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Xml;



namespace MarsTestFrame.SourceCode.systemUtil
{
    public enum TestObjectSource
    {
        TOS_Not_Init,
        TOS_From_XlsFile,
        TOS_From_Database,
    }

    public enum TestKeywSource
    {
        TKS_Not_Init,
        TKS_From_ConfigFile,
        TKS_From_Database,
    }

    public enum TestClassStatus
    {
        TCS_Not_Init,
        TCS_Initilized,
        TCS_Done,
    }

    public sealed class AppConfigReader
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
            return gCurrentConfiguration;
        }
        private static string GetSpecialNodeValueFromNameValue(ConfigurationSection objSection, string strSection, string strKey)
        {
            Logger.logBegin("GetSpecialNodeValueFromNameValue");
            try
            {
                if (objSection == null)
                {
                    Logger.Error("GetSpecialNodeValueFromNameValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SECTION_SPECIAL) + ":[{0}]", strSection));
                    return null;
                }
                NameValueCollection handlerCreatedCollection = ConvertNameValueSectionToCollection(objSection, strSection);
                string strResult = handlerCreatedCollection.Get(strKey);
                if (strResult == null)
                {

                    Logger.Error("GetSpecialNodeValueFromNameValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SECTION_SPECIAL) + " Key:[{0}]-Section:[{1}]", strKey, strSection));
                    return null;
                }
                return strResult;
            }
            finally
            {
                Logger.logEnd("GetSpecialNodeValueFromNameValue");
            }

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



        public static string GetXlsRootPath()
        {
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetXlsRootPath", TestFrameMonitor.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECITON_XLSCONFGMODE);
            string strResult = GetSpecialNodeValueFromNameValue(objSection, SystemConstant.CNST_APPCONFIG_SECITON_XLSCONFGMODE, SystemConstant.CNST_APPCONFIG_SECTION_XLSMODE_ROOT);
            Logger.Info("GetXlsRootPath", string.Format(TestFrameMonitor.Properties.Resources.FUNTION_RETURN_VALUE, "GetXlsRootPat", strResult ?? ""));
            return strResult;
        }


        internal static string GetRegApplicationsMode()
        {
            Logger.logBegin("GetRegApplicationsMode");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetRegApplications", TestFrameMonitor.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            string strMode = GetAppSettingByKey(SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE, null);
            //objConfig.AppSettings[SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE];
            Logger.logEnd("GetRegApplicationsMode");
            return strMode;
        }
#if tigerServer
        internal static string GetBaseLineMode()
        {
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetBaseLineMode", TestFrameMonitor.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            string strMode = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, null);
            //objConfig.AppSettings[SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE];
            Logger.logEnd("GetBaseLineMode");
            return strMode;
        }

        public static string GetDefaultApplicationName()
        {
            Logger.logBegin("GetDefaultApplicationName");
            string strResult = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DEFAULTAPP, null);
            Logger.logEnd("GetDefaultApplicationName");
            return strResult;
        }
#endif
        private static string GetAppSettingByKey(string strKey, string strDefault)
        {
            Logger.logBegin("GetAppSettingByKey");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetRegApplications", TestFrameMonitor.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
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

#if tigerServer
        public static TestObjectSource GetObjectSource()
        {
            Logger.logBegin("GetObjectSource");
            string strSourceFrom = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_OBJECTSOURCE, null);
            if (strSourceFrom == null)
            {
                throw new MarsExceptions((int)ERROR_CODE._APP_NO_SETTING_OBJECT_FROM, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SETTING_OBJECT_FROM));
            }
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_OBJECTSOURCE_XLS, strSourceFrom, true) == 0)
            {
                Logger.Info("GetObjectSource", TestFrameMonitor.Properties.Resources.HINT_FIND_APPLICATON_SETTING_OBJECTSOURCE_XLS);
                Logger.logEnd("GetObjectSource");
                return TestObjectSource.TOS_From_XlsFile;
            }
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_OBJECTSOURCE_DB, strSourceFrom, true) == 0)
            {
                Logger.Info("GetObjectSource", TestFrameMonitor.Properties.Resources.HINT_FIND_APPLICATON_SETTING_OBJECTSOURCE_DB);
                Logger.logEnd("GetObjectSource");
                return TestObjectSource.TOS_From_Database;
            }
            throw new MarsExceptions((int)ERROR_CODE._APP_WRONG_VALUE_SETTING_OBJECT_FROM, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_WRONG_VALUE_SETTING_OBJECT_FROM));
            
        }

        internal static TestKeywSource GetKeyWordsFrom()
        {
            Logger.logBegin("GetKeyWordsFrom");
            string strSourceFrom = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE, null);
            if (strSourceFrom == null)
            {
                throw new MarsExceptions((int)ERROR_CODE._APP_NO_SETTING_KEYWORD_FROM, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SETTING_KEYWORD_FROM));
            }
            if (string.Compare(strSourceFrom, SystemConstant.CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE_CONFIG,true)==0)
            {
                return TestKeywSource.TKS_From_ConfigFile;
            }
            if (string.Compare(strSourceFrom, SystemConstant.CNST_APPCONFIG_APPSETTING_KEYWORDSSOURCE_DB, true) == 0)
            {
                return TestKeywSource.TKS_From_Database;
            }

            Logger.logEnd("GetKeyWordsFrom");
            throw new MarsExceptions((int)ERROR_CODE._APP_WRONG_VALUE_SETTING_KEYWORD_FROM, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_WRONG_VALUE_SETTING_KEYWORD_FROM), strSourceFrom));
        }

        public static string GetTCDataSource()
        {
            Logger.logBegin("GetTCDataSource");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetTCDataSource", TestFrameMonitor.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            string strResult = objConfig.AppSettings.Settings[SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE].ToString() ;
            Logger.logEnd("GetTCDataSource");
            return strResult;
        }

        internal static List<string> GetConfigUFTInitScripts()
        {
            Logger.logBegin("GetConfigUFTInitScripts");
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_UFT_INIT_SCRIPTS);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_UFT_INIT_SCRIPTS);
            if (lstKeyValues == null)
            {
                Logger.Error("GetConfigUFTInitScripts", string.Format(TestFrameMonitor.Properties.Resources.HINT_CONFIGREADER_NO_SUCHSECTION_AS_NAMEVALUE, SystemConstant.CNST_APPCONFIG_SECTION_UFT_INIT_SCRIPTS));
                return null;
            }
            List<string> lstResult = new List<string>();
            foreach (string strKey in lstKeyValues.AllKeys)
            {
                string[] arrValue = lstKeyValues.GetValues(strKey);
                lstResult.Add(arrValue==null?"":arrValue[0]);
            }

            Logger.logEnd("GetConfigUFTInitScripts");
            return lstResult;
        }

        internal static List<string> GetConfigCurrentUFTAddins()
        {
            Logger.logBegin("GetConfigCurrentUFTAddins");
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_CURRENT_UFT_ADDINS);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_CURRENT_UFT_ADDINS);
            if (lstKeyValues == null)
            {
                Logger.Error("GetConfigCurrentUFTAddins", string.Format(TestFrameMonitor.Properties.Resources.HINT_CONFIGREADER_NO_SUCHSECTION_AS_NAMEVALUE, SystemConstant.CNST_APPCONFIG_SECTION_CURRENT_UFT_ADDINS));
                return null;
            }
            List<string> lstResult = new List<string>();
            foreach (string strKey in lstKeyValues.AllKeys)
            {
                string[] arrValue = lstKeyValues.GetValues(strKey);
                lstResult.Add(arrValue == null ? "" : arrValue[0]);
            }

            Logger.logEnd("GetConfigCurrentUFTAddins");
            return lstResult;
        }
#endif
        internal static bool GetConfigServerURLInfo(ref string strProtocol, ref string strHost, ref string strServiceName, ref string strError)
        {
            Logger.logBegin("GetConfigServerURLInfo");
            //ServicebasicInformation objServiceInfo = new ServicebasicInformation();
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            string[] strProtocols = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
            if ((strProtocols == null) || (strProtocols.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING);
                return false;
                //throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING));
            }
            string[] strHosts = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
            if ((strHosts == null) || (strHosts.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING);
                return false;
                //throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
            }
            string[] strPorts = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
            if ((strPorts == null) || (strPorts.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING);
                return false;
                //throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
            }
            string[] strServiceNames = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
            if ((strServiceNames == null) || (strServiceNames.Length == 0))
            {
                strError = ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING);
                return false;
                //throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
            }
            //string strResult = ServicebasicInformation.GetURL(strHost[0], strProtocol[0], strPort[0], strServiceName[0]);
            strProtocol = strProtocols[0];
            strHost = strHosts[0];
            strServiceName = strServiceNames[0];

            Logger.logEnd("GetConfigServerURLInfo");
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
            string strResult = ServicebasicInformation.GetURL(strHost[0], strProtocol[0], strPort[0], strServiceName[0]);

            Logger.logEnd("GetConfigServerURLInfo");

            return strResult;
        }
#if _VEDIO_TIGER_
        internal static bool GetVedioSetting()
        {
            Configuration objConfig = GetConfigurationInstance();
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
            string[] strValue = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_VEDIO);
            if ((strValue == null) || (strValue.Length == 0))
            {
                return false;
            }
            bool bRecord = false;
            if (bool.TryParse(strValue[0],out bRecord))
            {
                return bRecord;
            }
            return false;
        }
#endif
        #region Debug Mode inforamtion
        public static string GetDebugModeInfo()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE, SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE_NONE);
        }
        #endregion


    }
}
