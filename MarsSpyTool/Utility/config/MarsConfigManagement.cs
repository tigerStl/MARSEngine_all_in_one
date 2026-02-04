using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace MarsSpyTool.Utility.config
{
    internal class MarsConfigManagement
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");
        public const string cnst_engine_config_file = "Mars.AutoTestingDriver.exe.config";
        public const string cnst_engine_restful_svc = "MarsEngineSvc_url";
        public const string cnst_single_object_mode = "singleObjectMode";
        private Configuration engineConfig = null;
        public bool switch2EngineMainCfg(ref string strError)
        {
            logger.Info("switch2EngineMainCfg begin");
            try
            {
                string strConfigFile = AppDomain.CurrentDomain.BaseDirectory + cnst_engine_config_file;
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = strConfigFile;                
                engineConfig = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                return true;
            }
            catch(Exception ex)
            {
                logger.Error(ex, $"switch2EngineMainCfg\tException|{ex.Message}|");
                strError = $"can't load {cnst_engine_config_file}";
                engineConfig = null;
                return false;
            }
            finally
            {
                logger.Info("switch2EngineMainCfg\tEnd");
            }
            
        }

        public string getRESTfulServerBase()
        {
            if (engineConfig == null) return null;
            return engineConfig.AppSettings.Settings[cnst_engine_restful_svc].Value;
        }

        /// <summary>
        /// 保存 singleObjectMode 的状态到配置文件
        /// </summary>
        /// <param name="isSingleObjectMode">是否为单对象模式</param>
        /// <param name="strError">错误信息</param>
        /// <returns>是否保存成功</returns>
        public bool SaveSingleObjectMode(bool isSingleObjectMode, ref string strError)
        {
            logger.Info($"SaveSingleObjectMode begin|{isSingleObjectMode}");
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[cnst_single_object_mode] != null)
                {
                    config.AppSettings.Settings[cnst_single_object_mode].Value = isSingleObjectMode.ToString();
                }
                else
                {
                    config.AppSettings.Settings.Add(cnst_single_object_mode, isSingleObjectMode.ToString());
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                logger.Info($"SaveSingleObjectMode|Saved successfully|{isSingleObjectMode}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"SaveSingleObjectMode\tException|{ex.Message}");
                strError = $"保存配置失败: {ex.Message}";
                return false;
            }
            finally
            {
                logger.Info("SaveSingleObjectMode\tEnd");
            }
        }

        /// <summary>
        /// 从配置文件加载 singleObjectMode 的状态
        /// </summary>
        /// <param name="strError">错误信息</param>
        /// <returns>单对象模式的状态，默认为 true</returns>
        public bool LoadSingleObjectMode(ref string strError)
        {
            logger.Info("LoadSingleObjectMode begin");
            try
            {
                string value = ConfigurationManager.AppSettings[cnst_single_object_mode];
                if (string.IsNullOrEmpty(value))
                {
                    logger.Info("LoadSingleObjectMode|No config found, using default value true");
                    return true; // 默认值
                }
                bool result = bool.Parse(value);
                logger.Info($"LoadSingleObjectMode|Loaded value|{result}");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"LoadSingleObjectMode\tException|{ex.Message}");
                strError = $"加载配置失败: {ex.Message}";
                return true; // 出错时返回默认值
            }
            finally
            {
                logger.Info("LoadSingleObjectMode\tEnd");
            }
        }
    }
}
