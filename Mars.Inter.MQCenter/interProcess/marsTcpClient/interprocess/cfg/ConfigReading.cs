using System.Configuration;
using System.IO;

namespace Mars.message.Inter.MQCenter.cfg
{
    public class ConfigReading
    {
        static System.Configuration.Configuration gInstance = null;
        private static bool ChangeConfigFile(ref string strError)
        {
            string strMarsConfigFile = typeof(ConfigReading).Assembly.Location;
            string strPath = Directory.GetParent(strMarsConfigFile).ToString();

            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = Path.Combine(strPath, "Mars.EXE.config"); //

            //string strFileName = Path.Combine(strPath, "Mars.EXE");
            string strFileName = configMap.ExeConfigFilename;
            if (!File.Exists(strFileName))
            {
                strError = string.Format("No such file exists. [{0}]", strFileName);
                return false;
            }
            //
            gInstance = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            //gInstance = ConfigurationManager.OpenExeConfiguration(strFileName);
            return true;
        }
        const string cnst_DefaultWindowIds = "DefaultWindowIds";
        const string cnst_DefaultWaitTime = "DefaultWaitTime";
        private static double? defaultWaitTime = 120;

        public static string GetDefaultWindows()
        {
            
            string strError = "";
            if (gInstance == null)
            {
                if (!ChangeConfigFile(ref strError))
                {
                    simpleLog.MarsLoggerSimple.Error("GetDefaultWindows", strError);
                    return null;
                }
                simpleLog.MarsLoggerSimple.Info("\t", $"current config file:{ gInstance.FilePath}");
            }
            KeyValueConfigurationElement strValue = gInstance.AppSettings.Settings[cnst_DefaultWindowIds];
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("current default value:{0}", strValue == null ? "" : strValue.Value));
            return strValue == null ? "" : strValue.Value;
        }

        /// <summary>
        /// 获得默认的等待enable visible时间
        /// </summary>
        /// <returns></returns>
        public static double GetDefaultWaitTime()
        {
            string strError = "";
            if (gInstance == null)
                if (!ChangeConfigFile(ref strError)) return 120; //默认120秒
            if (defaultWaitTime != null) return defaultWaitTime ?? 120;

            KeyValueConfigurationElement strValue = gInstance.AppSettings.Settings[cnst_DefaultWindowIds];
            double d;
            if (double.TryParse(strValue.Value, out d))
            {
                return (defaultWaitTime = d) ?? 120;
            }
            else return 120;
        }

    }
}
