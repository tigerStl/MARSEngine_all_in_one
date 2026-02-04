using System;
using System.IO;
using Newtonsoft.Json;

namespace MarsLicenseManager.Services
{
    /// <summary>
    /// 配置服务 - 管理应用程序配置
    /// </summary>
    public class ConfigurationService
    {
        private const string ConfigFileName = "config.json";
        private static readonly string ConfigFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            ConfigFileName
        );

        private AppConfiguration? _configuration;

        public ConfigurationService()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public AppConfiguration Configuration => _configuration ?? new AppConfiguration();

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _configuration = JsonConvert.DeserializeObject<AppConfiguration>(json);
                }
            }
            catch
            {
                // 如果加载失败，使用默认配置
            }

            // 确保配置不为null
            _configuration = _configuration ?? new AppConfiguration();
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_configuration, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // 保存失败时忽略错误
            }
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        public void SetLanguage(string language)
        {
            if (_configuration != null)
            {
                _configuration.Language = language;
                SaveConfiguration();
            }
        }

        /// <summary>
        /// 获取语言
        /// </summary>
        public string GetLanguage()
        {
            return _configuration?.Language ?? "en-US";
        }
    }

    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// 语言设置 (默认为英语)
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// 上次保存License文件的路径
        /// </summary>
        public string? LastSavePath { get; set; }

        /// <summary>
        /// 窗口位置X
        /// </summary>
        public int? WindowX { get; set; }

        /// <summary>
        /// 窗口位置Y
        /// </summary>
        public int? WindowY { get; set; }
    }
}
