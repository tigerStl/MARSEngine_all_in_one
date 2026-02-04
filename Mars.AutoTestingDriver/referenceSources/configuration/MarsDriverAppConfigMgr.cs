using com.Mars.Config;
using System;
using System.Configuration;

namespace Mars.AutoTestingDriver.referenceSources.configuration
{
    public class MarsDriverAppConfigMgr
    {
        private const string CNST_SECTION_APPLICATION = "RegistedApplications";
        private const string CNST_APP_VERIFYVALUESKIPPER = "VerifyValueSkipper";
        private static ConfigTestApplicationCollection currentApplications = null;
        public static ConfigTestApplicationCollection CurrentApplications
        {
            get
            {
                return currentApplications;
            }
        }

        public static bool GetVerifyValueSkipper()
        {
            try
            {
                Configuration objConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                string strVerifyValue = objConfig.AppSettings.Settings[CNST_APP_VERIFYVALUESKIPPER].Value as string;
                if (string.IsNullOrEmpty(strVerifyValue))
                {
                    return false;
                }
                Boolean isSkipper;
                if (bool.TryParse(strVerifyValue, out isSkipper))
                {
                    return isSkipper;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static bool GetConfigurationApps(ref string strError)
        {
            try
            {
                Configuration objConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                RegisterdApplictions objInst = null;
                if (objConfig.Sections[CNST_SECTION_APPLICATION] == null)
                {
                    objInst = new RegisterdApplictions();
                    objConfig.Sections.Add(CNST_SECTION_APPLICATION, objInst);
                    objConfig.Save(ConfigurationSaveMode.Full);
                }
                else
                    objInst = (RegisterdApplictions)objConfig.Sections[CNST_SECTION_APPLICATION];

                currentApplications = objInst.RegApplications;
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception [{0}],\r\n\t{1}", e.Message, e.StackTrace);
                return false;
            }
        }

        internal static string GetApplicationIdByShortName(string aPP_SHORT_NAME, ref string strError, ref bool isOk)
        {
            if (currentApplications == null)
            {
                strError = "Not initialized. Read applications info first.";
                isOk = false;
                return "";
            }
            ConfigTestApplication marsApp = currentApplications.GetSingle(aPP_SHORT_NAME);
            if (marsApp == null)
            {
                strError = string.Format("Can't find such application:[{0}]", aPP_SHORT_NAME);
                isOk = false;
                return null;
            }
            isOk = true;
            if (!string.IsNullOrEmpty(marsApp.identifier))
            {
                return System.IO.Path.GetFileNameWithoutExtension(marsApp.identifier);
            }
            return "";
        }

        internal static string GetApplciationStartCommandByShortName(string aPP_SHORT_NAME, ref string strError, ref bool isOk)
        {
            if (currentApplications == null)
            {
                strError = "Not initialized. Read applications info first.";
                isOk = false;
                return "";
            }
            ConfigTestApplication marsApp = currentApplications.GetSingle(aPP_SHORT_NAME);
            if (marsApp == null)
            {
                strError = string.Format("Can't find such application:[{0}] configged in local", aPP_SHORT_NAME);
                isOk = false;
                return null;
            }
            isOk = true;
            return marsApp.path;
        }        
    }
}
