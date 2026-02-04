extern alias clientWCF;
using com.Mars.Config;
using com.Mars.Constants;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace com.Mars.TestFrame.Application
{
    public class TargetApplicationsManagement
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplicationsManagement));
        private static List<TargetApplicationInfo> mlstApplications = null;
        private static TargetApplicationsFactory mobjApplicationsFactory = TargetApplicationsFactory.GetInstance() ;

        public static void InitRegApplications()
        {
            Logger.logBegin("InitRegApplications");
            string strModeApp = AppConfigReader.GetRegApplicationsMode();
            if (strModeApp == null)
            {
                Logger.Error("GetRegApplicationByStepValue", MarsTestFrame.Properties.Resources.ERR_CONFIG_NO_REGAPPLICATONS_SETTING_IN_CONFIG_FILE);
                throw new Exception(MarsTestFrame.Properties.Resources.ERR_CONFIG_NO_REGAPPLICATONS_SETTING_IN_CONFIG_FILE);
            }
            TargetApplicationAdapter objAppAdpt = TargetApplicationsFactory.GetRegApplicationAdpt(strModeApp);
            mlstApplications = objAppAdpt.getRegApplications();
            Logger.logEnd("InitRegApplications");
        }

        public static TargetApplicationInfo GetDefaultApplication()
        {
            Logger.logBegin("GetDefaultApplication");
            /*** get default shortname from appsettings ***/
            string strDefaultAppName = AppConfigReader.GetDefaultApplicationName();
            if (strDefaultAppName == null) {
                Logger.Info("GetDefaultApplication", "Default application Name is null, no setting for DefaultApplication of config file.");
                return null;
            }
            TargetApplicationInfo objResult = GetRegApplicationByStepValue(strDefaultAppName);
            Logger.logEnd("GetDefaultApplication");
            return objResult;
        }

        public static TargetApplicationInfo GetRegApplicationByStepValue(string strValue) 
        {
            Logger.logBegin("GetRegApplicationByStepValue");
            /*** 
             * 
             * ***/
            string strModeApp = AppConfigReader.GetRegApplicationsMode();
            if (strModeApp == null)
            {
                Logger.Error("GetRegApplicationByStepValue", MarsTestFrame.Properties.Resources.ERR_CONFIG_NO_REGAPPLICATONS_SETTING_IN_CONFIG_FILE);
                throw new Exception(MarsTestFrame.Properties.Resources.ERR_CONFIG_NO_REGAPPLICATONS_SETTING_IN_CONFIG_FILE);
            }

            if (mlstApplications==null)
            {
                InitRegApplications();
            }

            /*** check value Format from strValue ***/
            string strAppName="" ;
            MARS_ADAPTER iValueMode = AlystApplicationSettingMode(strValue,ref strAppName);
            if (iValueMode == MARS_ADAPTER._ADPTR_APP_SETTING_NONE)
            {
                Logger.logEnd("GetRegApplicationByStepValue");
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_SETCURRENT_APPLICATION_VALUEFORMAT_ERROR, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_SETCURRENT_APPLICATION_VALUEFORMAT_ERROR));
            }
            TargetApplicationInfo query = mlstApplications.Where(st => iValueMode == MARS_ADAPTER._ADPTR_APP_SETTING_SHORTNAME?string.Compare(st.ApplicationShortName, strAppName, true) == 0
            : string.Compare(st.Path, strAppName, true) == 0).FirstOrDefault();
            if (query!=null)
            {
                Logger.logEnd("GetRegApplicationByStepValue");
                return query;
            }
            //foreach (TargetApplicationInfo objApp in mlstApplications)
            //{
            //    if (objApp == null) continue;
            //    if (iValueMode == MARS_ADAPTER._ADPTR_APP_SETTING_SHORTNAME)
            //    {
            //        if (string.Compare(objApp.ApplicationShortName, strAppName, true) == 0)
            //        {
            //            Logger.logEnd("GetRegApplicationByStepValue");
            //            return objApp;
            //        }
            //    }
            //    else
            //    {
            //        if (string.Compare(objApp.Path, strAppName, true) == 0)
            //        {
            //            Logger.logEnd("GetRegApplicationByStepValue");
            //            return objApp;
            //        }
            //    }
            //}
            Logger.Error("GetRegApplicationByStepValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED), strAppName));
            Logger.logEnd("GetRegApplicationByStepValue");
            throw new MarsExceptions((int)ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED),strAppName));
        }

        private static MARS_ADAPTER AlystApplicationSettingMode(string strValue,ref string strAppName)
        {
            const string cnst_shortname=@"^ShortName:" ;
            Logger.logBegin("AlystApplicationSettingMode");
            /*** start with short name? ***/            
            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace ;
            if (Regex.IsMatch(strValue, cnst_shortname,options)){
                strAppName = Regex.Replace(strValue, cnst_shortname, "", options);
                return MARS_ADAPTER._ADPTR_APP_SETTING_SHORTNAME ;
            }
            strAppName = strValue;
            /*** is a regular URL? ***/
            Uri objDesUri = null ;
            bool bUri = Uri.TryCreate(strValue,UriKind.RelativeOrAbsolute,out objDesUri) ;
            try
            {
                bUri = (bUri && objDesUri == null ? false : (objDesUri.Scheme == Uri.UriSchemeFile || objDesUri.Scheme == Uri.UriSchemeHttp || objDesUri.Scheme == Uri.UriSchemeHttps));
            }
            catch (Exception)
            {
                bUri = false;
                return MARS_ADAPTER._ADPTR_APP_SETTING_SHORTNAME;
            }
            
            if (bUri)
            {
                Logger.Info("AlystApplicationSettingMode", string.Format(MarsTestFrame.Properties.Resources.FUNTION_RETURN_VALUE, "AlystApplicationSettingMode", "MARS_ADAPTER._ADPTR_APP_SETTING_FULLPATH"));
                return MARS_ADAPTER._ADPTR_APP_SETTING_FULLPATH;
            }
            Logger.Info("AlystApplicationSettingMode", string.Format(MarsTestFrame.Properties.Resources.FUNTION_RETURN_VALUE, "AlystApplicationSettingMode", "MARS_ADAPTER._ADPTR_APP_SETTING_NONE"));
            return MARS_ADAPTER._ADPTR_APP_SETTING_NONE; 
        }

        public static TargetApplicationInfo GetApplicationByShortName(string strAppShortName)
        {
            Logger.logBegin("GetApplicationByShortName",strAppShortName);
            if (mlstApplications==null)
            {
                InitRegApplications();
            }
            try
            {
                foreach (TargetApplicationInfo objApplication in mlstApplications)
                {
                    Logger.Info("GetApplicationByShortName", string.Format("current to cmpare:[{0}]",objApplication==null?"NULL-N/A":objApplication.ApplicationShortName));
                    if (objApplication == null) continue;

                    if (string.Compare(strAppShortName, objApplication.ApplicationShortName, true) == 0)
                    {
                        return objApplication;
                    }
                }
                return null;
            }catch(Exception e)
            {
                Logger.Error("GetApplicationByShortName", e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationByShortName");
            }
            
        }

        public static TargetApplicationInfo GetApplicationByPath(string strAppPath)
        {
            Logger.logBegin("GetApplicationByPath");
            if (mlstApplications == null)
            {
                InitRegApplications();
            }
            try
            {
                foreach (TargetApplicationInfo objApplication in mlstApplications)
                {
                    if (objApplication == null) continue;
                    if (string.Compare(strAppPath, objApplication.Path, true) == 0)
                    {
                        return objApplication;
                    }
                }
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationByPath");
            }
            
        }

        public static List<TargetApplicationInfo> GetAllApplications()
        {
            return mlstApplications;
        }

        public static int SwitchAddinsFilesByShortName(string strShortName, ref string strError)
        {
            ConfigTestApplicationCollection lstApps = AppConfigReader.GetRegApplications();
            
            foreach(ConfigTestApplication itm in lstApps)
            {
                if (string.Compare(strShortName, itm.AppName,true)==0)
                {
                    ///如果是java程序， 无须插件的切换
                    if (string.Compare(itm.AppliationType, "Java", true) == 0) return 1;
                    return SwitchAddinsFiles(itm.ExtraRequirement, ref strError);
                }
            }
            strError = string.Format("Can't find the configuration for {0}", strShortName);
            return -1;
        }

        public static int SwitchAddinsFiles(string strRequirement, ref string strError)
        {
#if _NO_C_DRIVER_WRITE
            string strRoot = TigerMarsUtil.GetAppRootDir();
            Logger.Info("SwitchAddinsFiles", string.Format("Get Current working directory: [{0}]", strRoot));
            string strTargetPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            Logger.Info("SwitchAddinsFiles", string.Format("Get strTargetPath working directory: [{0}]", strTargetPath));
            if (string.IsNullOrEmpty(strTargetPath))
                strTargetPath = @"H:\MarsAutomation";
            try
            {


                string strCurrentPath = Path.Combine(strTargetPath, ".\\qtpAddins", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT);//  string.Format("{1}\\qtpAddins\\{0}\\", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT, strTargetPath);
                string strCurrentPath1 = Path.Combine(strTargetPath, ".\\qtpAddins", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT + "_tmp");//  string.Format("{1}\\qtpAddins\\{0}\\", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT, strTargetPath);
                                                                                                                                            //string strCurrentPath1 = string.Format("{1}\\qtpAddins\\{0}_tmp\\", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT, strTargetPath);
#else
                string strRoot = TigerMarsUtil.GetAppRootDir();
                Logger.Info("SwitchAddinsFiles",string.Format("Get Current working directory: [{0}]", strRoot)) ;
                string strTargetPath = "";
                string strCurrentPath = string.Format("{1}\\qtpAddins\\{0}\\", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT, strRoot);
                string strCurrentPath1 = string.Format("{1}\\qtpAddins\\{0}_tmp\\", SystemConstant.CNST_APPCONFIG_ADDINS_CURRENT, strRoot);
#endif
#if _ver1_5
                string[] arrRequirement = strRequirement.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < arrRequirement.Length; i++)
                {
                    switch (arrRequirement[i].ToUpper())
#else
                {
                     switch (strRequirement)
#endif
                    {
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V11:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR4", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V12:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR4V12", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V122:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR4V122", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V7:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR2", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V162:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLRV162", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V14:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR14", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V152:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR4V152", strRoot);
                            break;
#if _ver1_5
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEVEXPRESS_V11:
                            strTargetPath = string.Format("{0}\\qtpAddins\\SophisAddins", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEVEXPRESS_V18:
                            strTargetPath = string.Format("{0}\\qtpAddins\\devexpress18", strRoot);
                            break;
                        case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEVEXPRESS_V922:
                            strTargetPath = string.Format("{1}\\qtpAddins\\{0}\\", "CLR4V92", strRoot);
                            break;
#endif

                        default:
                            {
                                Logger.Error("SwitchSwfConfigFile", strError = string.Format("No such Addins version [{0}] is supported", strRequirement));
                                return -1;
                            }
                    }

                    if (!Directory.Exists(strTargetPath))
                    {
                        strError = string.Format("No addins Fold :[{0}], exists", strTargetPath);
                        return -1;
                    }


                    if (Directory.Exists(strCurrentPath1))
                    {
                        Directory.Delete(strCurrentPath1, true);
                    }
                    else
                    {
                        Directory.CreateDirectory(strCurrentPath1);
                    }

                    /** delete all files on CurrentPath **/
                    string strCurrentFileName = "";
                    try
                    {
#if _ver1_5
                        if (i == 0)
                        {
#endif
                            if (Directory.Exists(strCurrentPath))
                                Directory.Move(strCurrentPath, strCurrentPath1);
                            if (Directory.Exists(strCurrentPath))
                                Directory.Delete(strCurrentPath, true);
#if _ver1_5
                        }
#endif
                        if (!Directory.Exists(strCurrentPath))
                        {
                            Directory.CreateDirectory(strCurrentPath);
                        }
                        string[] arrFiles = Directory.GetFiles(strTargetPath);
                        foreach (string strFileName in arrFiles)
                        {
                            File.Copy(strFileName, strCurrentFileName = Path.Combine(strCurrentPath, Path.GetFileName(strFileName)), true);
                        }
#if !_ver1_5
                        return 1;
#endif
                    }
                    catch (Exception e)
                    {
                        Logger.Error("SwitchAddinsFiles", strError = string.Format("Can't copy all files from [{0}] to [{1}]--current copied fileName:[{3}], with Exception:[{2}]", strTargetPath, strCurrentPath, e.Message, strCurrentFileName), e);
                        return -1;
                    }
                }
#if _NO_C_DRIVER_WRITE
            }catch(Exception ex)
            {
                Logger.Error("SwitchAddinsFiles", strError = string.Format("Exception:{0}", ex.Message), ex);
                return -1;
            }
#endif
                return 1;
            
        }

        public static int SwitchSwfConfigFile(string strRequirement)
        {
            string strTargetPath = "";
            switch (strRequirement)
            {
                case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V11:
                    strTargetPath = string.Format("\\{0}\\SwfConfig.Xml", "CLR4");
                    break;
                case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V12:
                    strTargetPath = string.Format("\\{0}\\SwfConfig.Xml", "CLR4V12");
                    break;
                case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V7:
                    strTargetPath = string.Format("\\{0}\\SwfConfig.Xml", "CLR2");
                    break;
                case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_INFORA_V14:
                    strTargetPath = string.Format("\\{0}\\SwfConfig.Xml", "CLR14");
                    break;
                default:
                    {
                        Logger.Error("SwitchSwfConfigFile", string.Format("No such SwfConfig [{0}] file is supported", strRequirement));
                        return -1;
                    }
            }
            Logger.Info("SwitchSwfConfigFile", string.Format("Try to copy file:[{0}]", strTargetPath));

            /** copy file and override the Original **/
            string strQtpRoot = AppConfigReader.GetQtpRoot();
            if (strQtpRoot==null)
            {
                Logger.Error("SwitchSwfConfigFile", "No QtpRoot Setting is found, Can't switch SwfConfig File.");
                return -1;
            }
            string strDesFileName = string.Format(@"{0}\dat\SwfConfig.Xml", strQtpRoot);
            string strRoot = TigerMarsUtil.GetAppRootDir();
            string strSrcFileName = string.Format(@"{0}\qtpAddins{1}", strRoot, strTargetPath);
            try
            {
                File.Copy(strSrcFileName, strDesFileName, true);
                
            }
            catch (Exception e)
            {
                Logger.Error("SwitchSwfConfigFile", string.Format("Can't Copy [{0}] and override [{1}] file, Exceptions:[2]", strSrcFileName,strDesFileName, e.Message));
                return -1;
            }            

            return (int)ERROR_CODE._NO_ERROR;
        }
#if _Datafrom_Database
        public static List<ConfigTestApplication> CheckInstalledApps(List<MarsKeyValues<string, string>> lstSource, ref List<MarsKeyValues<string, string>> lstUnInstalled)
        {
            if (lstSource == null) return null;
            List<ConfigTestApplication> lstResult = new List<ConfigTestApplication> ();
            ConfigTestApplicationCollection lstLocalApps = AppConfigReader.GetRegApplications();
            bool isRegistedLocal;
            foreach (MarsKeyValues<string, string> objApp in lstSource)
            {
                isRegistedLocal = false; 
                if (objApp.MValue == null) continue;
                foreach (ConfigTestApplication objLocalApp in lstLocalApps)
                {
                    if (string.Compare(objLocalApp.AppName, objApp.MValue, true) == 0)
                    {
                        isRegistedLocal = true;
                        lstResult.Add(objLocalApp);
                        objLocalApp.Tag = objApp;
                        break;
                    }
                }
                if (!isRegistedLocal) 
                    lstUnInstalled.Add(objApp);
            }
            return lstResult;
        }
#endif

    }

    internal class TargetApplicationsFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplicationsFactory));
        private static TargetApplicationsFactory gobjInstance = null;
        
        private TargetApplicationsFactory(){

        }
        public static TargetApplicationsFactory GetInstance()
        {
            Logger.logBegin("GetInstance");
            if (gobjInstance==null)
            {
                Logger.Info("GetInstance",string.Format(MarsTestFrame.Properties.Resources.HINT_CREATE_NEW_SINGLE_INSTANCE, "TargetApplicationsFactory"));
                gobjInstance = new TargetApplicationsFactory();
            }
            Logger.logEnd("GetInstance");
            return gobjInstance;
        }



        internal static TargetApplicationAdapter GetRegApplicationAdpt(string strModeApp)
        {
            Logger.logBegin("GetRegApplicationAdpt");
            try
            {
                if (string.Compare(SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE_VALUE_CONFIG, strModeApp, true) == 0)
                {
                    Logger.Info("GetRegApplicationAdpt", MarsTestFrame.Properties.Resources.HINT_FIND_APPLICATION_SETTINGMODE_CONFIG);
                    return new TargetApplicationConfigAdapter();
                }
                if (string.Compare(SystemConstant.CNST_APPSETTING_APPLICATION_REG_MODE_VALUE_DB, strModeApp, true) ==0)
                {
                    Logger.Info("GetRegApplicationAdpt", MarsTestFrame.Properties.Resources.HINT_FIND_APPLICATION_SETTINGMODE_CONFIG);
                    return new TargetApplicatoinDBAdapter();
                }
                return null;
            }
            finally
            {
                Logger.logEnd("GetRegApplicationAdpt");
            }
            
            
            
        }
    }


    public class TargetApplicationInfo
    {
#region Member Vars
        private string mstrApplicationName;
        private string mstrPath;
        private string mstrStarter;
        private string mstrIdentifier;
        private string mstrApplicationType;
        private string mstrObjectPath;
#endregion

#region properties
        public string ApplicationShortName
        {
            get
            {
                return this.mstrApplicationName;
            }
            set { this.mstrApplicationName = value; }
        }
        public string Path
        {
            get { return this.mstrPath; }
            set { this.mstrPath = value; }
        }
        /// <summary>
        /// Now, the command used for parameters of path
        /// tiger addressed on 4-23-2019
        /// </summary>
        public string Command
        {
            get { return this.mstrStarter; }
            set { this.mstrStarter = value; }
        }
        public string Indentifier
        {
            get { return this.mstrIdentifier; }
            set { this.mstrIdentifier = value; }
        }
        public string ApplicationType
        {
            get { return this.mstrApplicationType; }
            set { this.mstrApplicationType = value; }
        }
        public string ObjectFilePath
        {
            get { return this.mstrObjectPath; }
            set { this.mstrObjectPath = value; }
        }

        public string ExtraRequirement { get; set; }
        public string ExtraPopupMenuCount { get ;set ;} 
#endregion

    }

    class TargetApplicationAdapter
    {
        public virtual List<TargetApplicationInfo> getRegApplications()
        {
            return null;
        }
        protected virtual TargetApplicationInfo convertObjectToApplicationInfo(object objSrc)
        {
            return null;
        }
    }

    class TargetApplicationConfigAdapter:TargetApplicationAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplicationConfigAdapter));
        public TargetApplicationConfigAdapter():base()
        {
            Logger.logBegin("TargetApplicationConfigAdapter constructor");
            Logger.logEnd("TargetApplicationConfigAdapter constructor");
        }
        public override List<TargetApplicationInfo> getRegApplications()
        {
            Logger.logBegin("getRegApplications");
            List<TargetApplicationInfo> lstResult = new List<TargetApplicationInfo>();
            try
            {
                ConfigTestApplicationCollection lstApplication = AppConfigReader.GetRegApplications();
                foreach (ConfigTestApplication objTestApplication in lstApplication)
                {
                    if (objTestApplication == null) continue;
                    lstResult.Add(convertObjectToApplicationInfo(objTestApplication));
                }
                return lstResult;
            }
            finally
            {
                Logger.logEnd("getRegApplications");
            }
            
        }
        protected override TargetApplicationInfo convertObjectToApplicationInfo(object objSrc)
        {
            Logger.logBegin("convertObjectToApplicationInfo");
            if (objSrc == null) return null;
            if (objSrc.GetType() != typeof(ConfigTestApplication))
            {
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_OBJECT_TYPE_MISMATCH_CONFIGTEST_REQUIRED, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_OBJECT_TYPE_MISMATCH_CONFIGTEST_REQUIRED), objSrc.GetType().ToString()));
            }
            ConfigTestApplication objCast = (ConfigTestApplication)objSrc;
            TargetApplicationInfo objResult = new TargetApplicationInfo();
            objResult.ApplicationShortName = objCast.AppName;
            objResult.Command = objCast.Command;
            objResult.Indentifier = objCast.identifier;
            objResult.Path = objCast.path;
            objResult.ApplicationType = objCast.AppliationType;
            objResult.ObjectFilePath = objCast.ObjectPath;
            objResult.ExtraRequirement = objCast.ExtraRequirement;
            objResult.ExtraPopupMenuCount = objCast.ExtraPopupMenu;
            Logger.logEnd("convertObjectToApplicationInfo");
            return objResult;
        }
    }

    class TargetApplicatoinDBAdapter:TargetApplicationAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplicatoinDBAdapter));
        public TargetApplicatoinDBAdapter()
            : base()
        {
            Logger.logBegin("TargetApplicatoinDBAdapter constructor");
            Logger.logEnd("TargetApplicatoinDBAdapter constructor");
        }
    }
}
