#if _MarsCDriver
extern alias clientWCF;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;

#else
using Route2NSEx.src.Marquis.systemUtil;
#endif

using com.Mars.Config;
using com.Mars.Constants;
#if !_MarsCDriver
using com.Mars.KeyWords.KeyWordObject;
//using MarsTestFrame.CommuniteServer;
using MarsTestFrame.plugins;
#endif //_MarsCDriver
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml;
using MarsTestFrame.SourceCode.xmlConfig;


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

        #region constant or static
        internal const string CNST_SECTION_APPLICATION = "RegistedApplications";

        internal const string CNST_SECTION_MARSPLUGINS = "MarsPlugins";
        internal const string CNST_APPSETTING_TARGET_XML_EXPORTDIR = "ExportXmlDirectory";
        internal const string CNST_APPSETTING_TARGET_XML_IMPORTDIR = "ImportXmlDirectory";
        public const string CNST_APPSETTING_TARGET_XML_PROJ_EXP_DIR = "PROJ_EXP_DIR";

        #endregion constant or static

        private static Configuration gCurrentConfiguration = null;
#if !_MarsCDriver
        public static Configuration GetConfigurationInstance()
        {
            string exeConfigPath = typeof(AppConfigReader).Assembly.Location;
            Logger.Info("GetConfigurationInstance", exeConfigPath);
            gCurrentConfiguration = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            return gCurrentConfiguration;
        }
#else
        public static Configuration GetConfigurationInstance()
        {
            
            return gCurrentConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }
#endif //_MarsCDriver
        
        public static NameValueCollection GetAppsettings()
        {
            if (gCurrentConfiguration == null) return null;
            NameValueCollection rslt = new NameValueCollection();
            for (int i=0;i< gCurrentConfiguration.AppSettings.Settings.Count; i++)
            {
                string k = gCurrentConfiguration.AppSettings.Settings.AllKeys[i];
                if (string.IsNullOrEmpty(k)) continue;
                rslt.Add(k, gCurrentConfiguration.AppSettings.Settings[k].Value);
            }
            return rslt;
            //ConfigurationManager.GetSection()
        }

        public static void SaveConfiguration()
        {
            if (gCurrentConfiguration == null) return;
            gCurrentConfiguration.Save(ConfigurationSaveMode.Full);
        }

        public static string GetXmlExportDir()
        {
            return GetAppSettingByKey(CNST_APPSETTING_TARGET_XML_EXPORTDIR, null);
        }

        public static string GetProjExportDir()
        {
            return GetAppSettingByKey(CNST_APPSETTING_TARGET_XML_PROJ_EXP_DIR, null);
        }

        public static string GetXmlImportDir()
        {
            return GetAppSettingByKey(CNST_APPSETTING_TARGET_XML_IMPORTDIR, null);
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

        public static void WriteCurrentApplicationShortName(string currentTestApplicationShortName)
        {
            Logger.logBegin("WriteCurrentApplicationShortName", currentTestApplicationShortName);
            try
            {

                Configuration objConfig = GetConfigurationInstance();
                if (objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME] == null)
                    objConfig.AppSettings.Settings.Add(SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME, currentTestApplicationShortName);
                else
                    objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME].Value = currentTestApplicationShortName;
                objConfig.Save();
                objConfig = null;

            }
            catch (Exception)
            {

            }
            Logger.logEnd("WriteCurrentApplicationShortName");
        }

        public static NameValueCollection ConvertNameValueSectionToCollection(ConfigurationSection objSection, string strSectionName)
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

        internal static string GetAutoCheckKeyword()
        {
            Configuration cfg = GetConfigurationInstance();
            if (cfg == null) return null;
            try
            {
                return cfg.AppSettings.Settings[SystemConstant.CNST_AUTOCHECK_KEYWORD].Value;
            }
            catch (Exception e)
            {
                Logger.Error("GetAutoCheckKeyword", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public static string GetDefaultSchemaForOracle(string defaultSchemaPrefix="")
        {
            Configuration cfg = GetConfigurationInstance();
            if (cfg == null) return null;
            try
            {
                return cfg.AppSettings.Settings[$"{defaultSchemaPrefix}{SystemConstant.CNST_DEFAULT_DATABASE_SCHEMA_KEY}"].Value;
            }
            catch (Exception e)
            {
                Logger.Error("GetDefaultSchemaForOracle", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public static string GetShellDir()
        {
            Configuration cfg = GetConfigurationInstance();
            if (cfg == null) return null;
            try
            {
                if (cfg.AppSettings.Settings["ShellDir"] == null) return null;
                return cfg.AppSettings.Settings["ShellDir"].Value;
            }
            catch (Exception e)
            {
                Logger.Error("GetDefaultSchemaForOracle", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public static string GetCurrentPegwindowReplacement()
        {
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null) return null;
            try
            {
                return objConfig.AppSettings.Settings[SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW].Value;
            }
            catch (Exception e)
            {
                Logger.Error("GetCurrentPegwindowReplacement", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }

        }
#if !_MarsCDriver
        public static string GetXlsRootPath()
        {
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetXlsRootPath", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECITON_XLSCONFGMODE) ;
            string strResult = GetSpecialNodeValueFromNameValue(objSection, SystemConstant.CNST_APPCONFIG_SECITON_XLSCONFGMODE, SystemConstant.CNST_APPCONFIG_SECTION_XLSMODE_ROOT);
            Logger.Info("GetXlsRootPath", string.Format(MarsTestFrame.Properties.Resources.FUNTION_RETURN_VALUE,"GetXlsRootPat",strResult??""));
            return strResult;                  
        }
#endif


        public static ConfigTestApplicationCollection GetRegApplications()
        {
            Logger.logBegin("GetRegApplications");
#if !_noLocalApplications

            try
            {
                Configuration objConfig = GetConfigurationInstance();
                if (objConfig == null)
                {
#if !_MarsCDriver
                Logger.Error("GetRegApplications", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
#endif //_MarsCDriver
                    return null;
                }
                RegisterdApplictions objInst = null;
                if (objConfig.Sections[CNST_SECTION_APPLICATION] == null)
                {
                    objInst = new RegisterdApplictions();
                    objConfig.Sections.Add(CNST_SECTION_APPLICATION, objInst);

                    objConfig.Save(ConfigurationSaveMode.Full);

                }
                else
                    objInst = (RegisterdApplictions)objConfig.Sections[CNST_SECTION_APPLICATION];
                //ConfigTestApplication objFirst = objInst.RegApplications[0];
                Logger.logEnd("GetRegApplications");
                return objInst.RegApplications;
            }catch(Exception e)
            {
                Logger.Error("GetRegApplications", e.Message, e);
                return null ;
            }
            finally
            {
                Logger.logEnd("GetRegApplications");
            }
#else
            return null ;
#endif
        }
#if !_MarsCDriver
        public static ConfigTesMarsPluginsCollection GetPMarslugins()
        {
            Logger.logBegin("GetPMarslugins");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetPMarslugins", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            MarsPluginsLoader objInst = null;
            try
            {

            
            if (objConfig.Sections[CNST_SECTION_MARSPLUGINS] == null)
            {
                objInst = new MarsPluginsLoader();
                objConfig.Sections.Add(CNST_SECTION_MARSPLUGINS, objInst);

                objConfig.Save(ConfigurationSaveMode.Full);

            }
            else
                objInst = (MarsPluginsLoader)objConfig.Sections[CNST_SECTION_MARSPLUGINS];
            }
            catch (Exception e)
            {
                /// no such section exists
                /// 
                Logger.Error("GetPMarslugins",string.Format("Exception:[{0}]",e.Message));
                return null;
            }
            //ConfigTestApplication objFirst = objInst.RegApplications[0];
            Logger.logEnd("GetRegApplications");
            return objInst.MarsPlugins;
        }
#endif
        public static bool ClearAllAppSettings(ref string strError)
        {
            Logger.logBegin("ClearAllAppSettings");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
#if !_MarsCDriver
                Logger.Error("ClearAllAppSettings", strError = MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
#endif
                return false;
            }
            RegisterdApplictions objInst = null;
            if (objConfig.Sections[CNST_SECTION_APPLICATION] == null)
            {
                objInst = new RegisterdApplictions();
                objConfig.Sections.Add(CNST_SECTION_APPLICATION, objInst);

                objConfig.Save(ConfigurationSaveMode.Full);

            }
            else
                objInst = (RegisterdApplictions)objConfig.Sections[CNST_SECTION_APPLICATION];
            for (int i = objInst.RegApplications.Count - 1; i >= 0; i--)
            {
                objInst.RegApplications[i] = null;
            }
            objConfig.Save(ConfigurationSaveMode.Full);
            return true;
        }

#if _NOQTP
        public static Configuration GetConfigurationInstanceFromSpecialFile(string strFileName,ref string strError, ref bool isOk )
        {
            //if (gCurrentConfiguration == null)
            //{
            //    string exeConfigPath = typeof(AppConfigReader).Assembly.Location ;
            //    gCurrentConfiguration = ConfigurationManager.OpenExeConfiguration(exeConfigPath);

            //}
            string exeConfigPath = typeof(AppConfigReader).Assembly.Location;
            string strConfigFile = System.IO.Path.GetDirectoryName(exeConfigPath);
            strConfigFile = Path.Combine(strConfigFile,strFileName);
            if (!File.Exists(strConfigFile))
            {
                isOk = false;
                strError = string.Format("No such file exists[{0}]",strConfigFile);
                return null;
            }
            isOk = true;
            try
            {
                return ConfigurationManager.OpenExeConfiguration(strConfigFile);
            }
            catch (Exception e)
            {
                Logger.Error("GetConfigurationInstanceFromSpecialFile", strError = string.Format("Exception when tries to open :[{0}] {1}", strConfigFile, e.Message), e);
                isOk = false;
                return null;
            }
            
        }

        public static bool SaveConfigurationNoQTP(ConfigTestApplication objConfgApp2Save, ref string strError, bool bDelete = false, string strTargetConfigFileName = "Mars.AutoTestingDriver.exe")
        {
            if ((objConfgApp2Save == null) && (!bDelete))
            {
                strError = "Data to be saved is null.";
                return false;
            }
            Logger.Info("SaveConfigurationNoQTP", string.Format("data to save ConfigTestApplication:[0]", objConfgApp2Save.AppName));
            bool isOk = false;
            Configuration objConfig = GetConfigurationInstanceFromSpecialFile(strTargetConfigFileName,ref strError, ref isOk);
            if (!isOk)
            {
                Logger.Error("SaveConfigurationNoQTP",strError );
                return false;
            }
            RegisterdApplictions objInst = null;
            if (objConfig.Sections[CNST_SECTION_APPLICATION] == null)
            {
                objInst = new RegisterdApplictions();
                objConfig.Sections.Add(CNST_SECTION_APPLICATION, objInst);

                objConfig.Save(ConfigurationSaveMode.Full);

            }
            else
                objInst = (RegisterdApplictions)objConfig.Sections[CNST_SECTION_APPLICATION];

            bool bFound = false;
            for (int i = 0; i < objInst.RegApplications.Count; i++)
            {
                if (string.Compare(objConfgApp2Save.AppName, objInst.RegApplications[i].AppName, true) == 0)
                {
                    bFound = true;
                    if (!bDelete)
                        objInst.RegApplications[i] = objConfgApp2Save;
                    else
                        objInst.RegApplications[i] = null;
                    break;
                }
            }
            if (!bFound)
            {
                objInst.RegApplications[objInst.RegApplications.Count] = objConfgApp2Save;
            }
            objConfig.Save(ConfigurationSaveMode.Full);
            return true;
        }
#endif
        public static bool SaveConfiguration(ConfigTestApplication objConfgApp2Save, ref string strError, bool bDelete = false)
        {
            if ((objConfgApp2Save == null) && (!bDelete))
            {
                strError = "Data to be saved is null.";
                return false;
            }
            Logger.Info("SaveConfiguration", string.Format("data to save ConfigTestApplication:[0]", objConfgApp2Save.AppName));
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
#if !_MarsCDriver
                Logger.Error("GetRegApplications", strError = MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
#endif //_MarsCDriver
                return false;
            }
            RegisterdApplictions objInst = null;
            if (objConfig.Sections[CNST_SECTION_APPLICATION] == null)
            {
                objInst = new RegisterdApplictions();
                objConfig.Sections.Add(CNST_SECTION_APPLICATION, objInst);

                objConfig.Save(ConfigurationSaveMode.Full);

            }
            else
                objInst = (RegisterdApplictions)objConfig.Sections[CNST_SECTION_APPLICATION];

            bool bFound = false;
            for (int i = 0; i < objInst.RegApplications.Count; i++)
            {
                if (string.Compare(objConfgApp2Save.AppName, objInst.RegApplications[i].AppName, true) == 0)
                {
                    bFound = true;
                    if (!bDelete)
                        objInst.RegApplications[i] = objConfgApp2Save;
                    else
                        objInst.RegApplications[i] = null;
                    break;
                }
            }
            if (!bFound)
            {
                objInst.RegApplications[objInst.RegApplications.Count] = objConfgApp2Save;
            }
            objConfig.Save(ConfigurationSaveMode.Full);
            return true;
        }
#if !_MarsCDriver
        public static KeyWordsConfigCollection GetKeyWordsList()
        {
            Logger.logBegin("GetKeyWordsList");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
                Logger.Error("GetKeyWordsList", MarsTestFrame.Properties.Resources._APP_NO_SECTION_KEWWORDS);
                return null;
            }
            KeyWordsSection objInst = null;
            if (objConfig.Sections[SystemConstant.CNST_APPCONFIG_SECTION_KEYWORDS] == null)
            {
                objInst = new KeyWordsSection();
                objConfig.Sections.Add(SystemConstant.CNST_APPCONFIG_SECTION_KEYWORDS, objInst);
                objConfig.Save(ConfigurationSaveMode.Modified);
            }
            else
                objInst = (KeyWordsSection)objConfig.Sections[SystemConstant.CNST_APPCONFIG_SECTION_KEYWORDS];
            Logger.logEnd("GetKeyWordsList");
            return objInst.Keywords;
        }
#endif

        internal static string GetRegApplicationsMode()
        {
            Logger.logBegin("GetRegApplicationsMode");
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
#if !_MarsCDriver
                Logger.Error("GetRegApplications", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
#endif
                return null;
            }
            string strMode = GetAppSettingByKey(SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE, null);
            //objConfig.AppSettings[SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE];
            Logger.logEnd("GetRegApplicationsMode");
            return strMode;
        }

        public static string GetBaseLineMode(string strUserEnvName)
        {
            return UserCfgMgr.GetTestBaseModeInfo(strUserEnvName);
#region old code
            /// codes below aren't usable anymore, 
            /// as now,, user test baseline information are stored in an xml file
            //Configuration objConfig = GetConfigurationInstance();
            //            if (objConfig == null)
            //            {
            //#if !_MarsCDriver
            //                Logger.Error("GetBaseLineMode", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
            //#endif
            //                return null;
            //            }
            //            string strMode = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, null);
            //            //objConfig.AppSettings[SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE];
            //            Logger.logEnd("GetBaseLineMode");
            //            return strMode;
#endregion
        }


        public static string GetDefaultApplicationName()
        {
            Logger.logBegin("GetDefaultApplicationName");
            string strResult = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DEFAULTAPP, null);
            Logger.logEnd("GetDefaultApplicationName");
            return strResult;
        }

        public static string GetDefaultApplicationNameEx()
        {
            string strName = GetDefaultApplicationName();
            return strName == null ? "" : strName.Replace("ShortName:", "");
        }

        public static string GetQtpRoot()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTIGN_QTPROOT, null);
        }

        public static bool GetBaseLineConfigValue()
        {
            string strDefault = "N/A";
            string strResult = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, strDefault);
            if (strResult == null)
            {
                SetAppSetting(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD);
                return true;
            }
            if (string.Compare(strResult, strDefault) == 0)
            {
                SetAppSetting(SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD);
                return true;
            }
            return string.Compare(strResult, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD) == 0;
        }

        private static string GetAppSettingByKey(string strKey, string strDefault)
        {
            Logger.logBegin("GetAppSettingByKey");

            if (string.Compare(strKey, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, true) == 0)
            {
#if _MarsCDriver
                return UserCfgMgr.GetTestBaseModeInfo(clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser);
#else
                return UserCfgMgr.GetTestBaseModeInfo(WCFXmlCfgMgr.CurrentLoginUser);
#endif
            }

            Configuration objConfig = GetConfigurationInstance();
            if (objConfig == null)
            {
#if !_MarsCDriver
                Logger.Error("GetRegApplications", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
#endif
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

#if !_MarsCDriver
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
                Logger.Info("GetObjectSource", MarsTestFrame.Properties.Resources.HINT_FIND_APPLICATON_SETTING_OBJECTSOURCE_XLS);
                Logger.logEnd("GetObjectSource");
                return TestObjectSource.TOS_From_XlsFile;
            }
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_OBJECTSOURCE_DB, strSourceFrom, true) == 0)
            {
                Logger.Info("GetObjectSource", MarsTestFrame.Properties.Resources.HINT_FIND_APPLICATON_SETTING_OBJECTSOURCE_DB);
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
                Logger.Error("GetTCDataSource", MarsTestFrame.Properties.Resources.APP_CONFIG_ERROR_CONFIGOBJ_NULL);
                return null;
            }
            string strResult = objConfig.AppSettings.Settings[SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE].Value;
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
                Logger.Error("GetConfigUFTInitScripts", string.Format(MarsTestFrame.Properties.Resources.HINT_CONFIGREADER_NO_SUCHSECTION_AS_NAMEVALUE, SystemConstant.CNST_APPCONFIG_SECTION_UFT_INIT_SCRIPTS));
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
                Logger.Error("GetConfigCurrentUFTAddins", string.Format(MarsTestFrame.Properties.Resources.HINT_CONFIGREADER_NO_SUCHSECTION_AS_NAMEVALUE, SystemConstant.CNST_APPCONFIG_SECTION_CURRENT_UFT_ADDINS));
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

		//internal static string GetConfigServerURLInfo()
  //      {
  //          Logger.logBegin("GetConfigServerURLInfo") ;
  //          //ServicebasicInformation objServiceInfo = new ServicebasicInformation();
  //          Configuration objConfig = GetConfigurationInstance();
  //          ConfigurationSection objSection = objConfig.GetSection(SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE );
  //          NameValueCollection lstKeyValues = ConvertNameValueSectionToCollection(objSection, SystemConstant.CNST_APPCONFIG_SECTION_FRAMEWORKSERVICE);
  //          string[] strProtocol = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PROTOCOL);
  //          if ((strProtocol == null) || (strProtocol.Length==0))
  //          {
  //              throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PROTOCO_SETTING)) ;
  //          }
  //          string[] strHost = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_HOST);
  //          if ((strHost == null) || (strHost.Length == 0))
  //          {
  //              throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_HOST_SETTING));
  //          }
  //          string[] strPort = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_PORT);
  //          if ((strPort == null) || (strPort.Length == 0))
  //          {
  //              throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_PORT_SETTING));
  //          }
  //          string[] strServiceName = lstKeyValues.GetValues(SystemConstant.CNST_SERVICE_KEY_URL_SERVICENAME);
  //          if ((strServiceName == null) || (strServiceName.Length == 0))
  //          {
  //              throw new MarsExceptions((int)ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._SERVICE_ERROR_NO_SERVICENAME_SETTING));
  //          }
  //          string strResult = ServicebasicInformation.GetURL(strHost[0], strProtocol[0], strPort[0], strServiceName[0]);

  //          Logger.logEnd("GetConfigServerURLInfo");

  //          return strResult;
  //      }
#endif

#region Debug Mode inforamtion
        public static string GetDebugModeInfo()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE, SystemConstant.CNST_APPCONFIG_APPSETTING_DEBUGMODE_NONE);
        }
#endregion

        internal static void SetAppSetting(string appKeyToChange, string strValue)
        {
            try
            {
                if (string.Compare(appKeyToChange, SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE, true) == 0)
                {
#if _MarsCDriver
                    UserCfgMgr.ChangeUserBaseLineMode(clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser, strValue);
#else
                    UserCfgMgr.ChangeUserBaseLineMode(WCFXmlCfgMgr.CurrentLoginUser, strValue);
#endif
                }
                else
                {

                    Configuration objConfig = GetConfigurationInstance();

                    objConfig.AppSettings.Settings[appKeyToChange].Value = strValue;
                    objConfig.Save();
                    objConfig = null;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SetAppSetting", string.Format("Exception:[{0}] property:[{1}]-[{2}]", e.Message, appKeyToChange, strValue));
            }
        }

        public static bool IsBaseLineMode()
        {
#if _MarsCDriver
            return string.Compare(AppConfigReader.GetBaseLineMode(clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser), SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, true) == 0;
#else
            return string.Compare(AppConfigReader.GetBaseLineMode(WCFXmlCfgMgr.CurrentLoginUser), SystemConstant.CNST_APPCONFIG_APPSETTING_BASELINEMODE_BUILD, true) == 0;
#endif
        }

#if _Datafrom_Database
        public static string[] ReadPegWindowTypes()
        {
            /** all items are upper cased **/
            string strPegWindow = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_PEGWINDOW_TYPE, null);
            if (strPegWindow == null) return new string[] { SystemConstant.CNST_RESERVED_KEYWORD_PEGWINDOW.ToUpper() };
            string[] arrResult = strPegWindow.ToUpper().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrResult.Length == 0) return new string[] { SystemConstant.CNST_RESERVED_KEYWORD_PEGWINDOW.ToUpper() };
            return arrResult;
        }

        public static List<string> ReadSkipStepWords()
        {
            List<string> skipStepWordList = new List<string>();
            string strSkipStepWords = GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_SKIP_WORD_LIST, null);
            if (strSkipStepWords != null)
            {
                string[] arrResult = strSkipStepWords.ToUpper().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrResult.Length > 0)
                {
                    skipStepWordList = new List<string>(arrResult);
                }
            }

            return skipStepWordList;
        }

        private static string current_assembly_path = null;
        public static string CURRENT_ASSEMBLY_PATH
        {
            get
            {
                if (current_assembly_path == null)
                    current_assembly_path = GetCurrentAssemblyDirectory();
                return current_assembly_path;
            }
        }
        internal static string GetCurrentAssemblyDirectory()
        {
            UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
        internal static string GetObjectDataConnection()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_OBJECT_DATABASE_CONN, null);
        }
        internal static string GetTCFileDirectory()
        {
            string strTCDirDefault = Path.Combine(CURRENT_ASSEMBLY_PATH, "..\\TC");
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_TESTCASEDIRECTORY, strTCDirDefault);
        }
        internal static string GetDataFileDiretory()
        {
            string strDataDirDefault = Path.Combine(CURRENT_ASSEMBLY_PATH, "..\\DATA");
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DATADIRECTORY, strDataDirDefault);
        }

        internal static string GetTestProjectSource()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_PROJECTS_SOURCE, null);
        }

        public static string GetAppForGen()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_GENSCRIPTON_APPLICATIONNAME, "SummitFT.exe");
        }

        public static string GetDefaultCacheAppsObjs()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DEFAULT_CACHE_APP_OBJS, null);
        }

        public static string GetDataComprisonPath()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_APPSETTING_DATACOMPARE_STARTER, null);
        }
#endif
#if !_MarsCDriver
        public static string GetClientLogoPos()
        {
            string strClientLogoDirDefault = Path.Combine(current_assembly_path, "..\\resource\\ClientLogo");
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_CLIENTLOGO_PATH, strClientLogoDirDefault);
        }
#endif
        public static string GetCurrentClientName()
        {
            return GetAppSettingByKey(SystemConstant.CNST_APPCONFIG_CLIENTNAME, null);
        }
        private static ConfigTestApplicationCollection RegApplicationsCache = GetRegApplications();
        public static string GetCurrentRuntimeApplicationType()
        {
            Configuration objConfig = GetConfigurationInstance();
            if (objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME] == null) return null;
            string strCurrentApplicationShortName = objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME].Value;
            if (string.IsNullOrEmpty(strCurrentApplicationShortName)) return null;
            for (int i = 0; i < RegApplicationsCache.Count; i++)
            {
                if (string.Compare(RegApplicationsCache[i].AppName, strCurrentApplicationShortName, true) == 0)
                    return RegApplicationsCache[i].AppliationType;
            }
            return null;
        }

        public static ConfigTestApplication GetDefaultApplicationInfo()
        {
            Logger.logBegin("GetDefaultApplicationInfo");
            try
            {
                Configuration objConfig = GetConfigurationInstance();
                if (objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME] == null) return null;
                string strCurrentApplicationShortName = objConfig.AppSettings.Settings[SystemConstant.CNST_APP_CURRENT_APPLICATIONSHORTNAME].Value;
                if (string.IsNullOrEmpty(strCurrentApplicationShortName)) return null;
                for (int i = 0; i < RegApplicationsCache.Count; i++)
                {
                    if (string.Compare(RegApplicationsCache[i].AppName, strCurrentApplicationShortName, true) == 0)
                        return RegApplicationsCache[i];
                }
                return null;
            }
            finally
            {
                Logger.logEnd("GetDefaultApplicationInfo");
            }


        }


        public static bool IsAutoCheckErrorEnable()
        {
            try
            {
                Configuration objConfig = GetConfigurationInstance();
                if (objConfig.AppSettings.Settings[SystemConstant.CNST_AUTO_CHECK_ERROR_SETTING] == null)
                {
                    objConfig.AppSettings.Settings.Add(SystemConstant.CNST_AUTO_CHECK_ERROR_SETTING, "false");
                    objConfig.Save();
                }
                string strIsAutoCheck = objConfig.AppSettings.Settings[SystemConstant.CNST_AUTO_CHECK_ERROR_SETTING].Value;
                bool isAutoCheckError;
                if (bool.TryParse(strIsAutoCheck, out isAutoCheckError))
                    return isAutoCheckError;
                else
                    return false;
            }
            catch (Exception e) { return false; }
            finally
            {

            }

        }

    }
}
