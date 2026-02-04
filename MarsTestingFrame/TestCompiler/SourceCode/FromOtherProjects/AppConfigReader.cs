using com.Mars.Constants;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;



namespace MarsTestFrame.SourceCode.systemUtil
{
    public sealed class AppConfigReader
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(AppConfigReader));
        private static Configuration gCurrentConfiguration = null;
        private static Configuration GetConfigurationInstance()
        {
            if (gCurrentConfiguration == null)
            {
                string exeConfigPath = typeof(AppConfigReader).Assembly.Location ;
                gCurrentConfiguration = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
                
            }
            return gCurrentConfiguration;
        }

        protected static string GetSpecialNodeValueFromNameValue(ConfigurationSection objSection,string strSection,string strKey)
        {
            Logger.logBegin("GetSpecialNodeValueFromNameValue");
            try
            {
                if (objSection == null)
                {
                    Logger.Error("GetSpecialNodeValueFromNameValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SECTION_SPECIAL) + ":[{0}]", strSection));
                    return null;
                }
                string strParamsSectionRawXml = objSection.SectionInformation.GetRawXml();
                XmlDocument objSectionXmlDoc = new XmlDocument();
                objSectionXmlDoc.Load(new StringReader(strParamsSectionRawXml));
                NameValueSectionHandler objHandler = new NameValueSectionHandler();

                NameValueCollection handlerCreatedCollection =
                    objHandler.Create(null, null, objSectionXmlDoc.DocumentElement) as NameValueCollection;
                string strResult = handlerCreatedCollection.Get(strKey);
                if (strResult == null)
                {

                    Logger.Error("GetSpecialNodeValueFromNameValue", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_NO_SECTION_SPECIAL)+" Key:[{0}]-Section:[{1}]",strKey,strSection));
                    return null;
                }
                return strResult;
            }
            finally
            {
                Logger.logEnd("GetSpecialNodeValueFromNameValue");
            }           
            
        }
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
    }
}
