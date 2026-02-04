using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// 嵌入式 License 管理器
    /// 支持将 License 信息编译到程序集中
    /// </summary>
    public class EmbeddedLicenseManager
    {
        private static readonly Logger logger = LogManager.GetLogger("MarsSpyLog");

        /// <summary>
        /// 方案1: 从嵌入资源加载 License
        /// 将 license 文件作为嵌入资源编译到程序集中
        /// </summary>
        public static MarsLicenseInfo LoadFromEmbeddedResource(string resourceName = "MarsSpyTool.Resources.embedded_license.lic")
        {
            logger.Info($"LoadFromEmbeddedResource\tBegin|{resourceName}");
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // 读取嵌入的资源
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        logger.Warn($"LoadFromEmbeddedResource\tResource not found: {resourceName}");
                        return null;
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string encryptedContent = reader.ReadToEnd();
                        
                        // 使用现有的解密方法
                        string decryptedContent = MarsLicenseManager.Instance.DecryptLicenseContent(encryptedContent);
                        MarsLicenseInfo license = JsonConvert.DeserializeObject<MarsLicenseInfo>(decryptedContent);
                        
                        logger.Info($"LoadFromEmbeddedResource\tSuccess|Type: {license.Type}");
                        return license;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"LoadFromEmbeddedResource\tException: {ex.Message}");
                return null;
            }
            finally
            {
                logger.Info("LoadFromEmbeddedResource\tEnd");
            }
        }

        /// <summary>
        /// 方案2: 从 Assembly 属性加载 License
        /// 将 license key 存储在程序集的自定义属性中
        /// </summary>
        public static MarsLicenseInfo LoadFromAssemblyAttribute()
        {
            logger.Info("LoadFromAssemblyAttribute\tBegin");
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // 获取自定义属性
                var attributes = assembly.GetCustomAttributes(typeof(EmbeddedLicenseAttribute), false);
                if (attributes.Length == 0)
                {
                    logger.Warn("LoadFromAssemblyAttribute\tNo EmbeddedLicenseAttribute found");
                    return null;
                }

                var licenseAttribute = (EmbeddedLicenseAttribute)attributes[0];
                string licenseKey = licenseAttribute.LicenseKey;
                
                // 解析 License Key
                string errorMessage = "";
                MarsLicenseInfo license = ParseEmbeddedLicenseKey(licenseKey, ref errorMessage);
                
                if (license != null)
                {
                    logger.Info($"LoadFromAssemblyAttribute\tSuccess|Type: {license.Type}");
                }
                else
                {
                    logger.Error($"LoadFromAssemblyAttribute\tFailed: {errorMessage}");
                }
                
                return license;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"LoadFromAssemblyAttribute\tException: {ex.Message}");
                return null;
            }
            finally
            {
                logger.Info("LoadFromAssemblyAttribute\tEnd");
            }
        }

        /// <summary>
        /// 方案3: 使用编译时常量（最安全）
        /// 需要在编译前将 license 信息写入源代码
        /// </summary>
        public static MarsLicenseInfo LoadFromCompiledConstants()
        {
            logger.Info("LoadFromCompiledConstants\tBegin");
            try
            {
                // 这些常量在编译时被替换
                // 可以通过 T4 模板或构建脚本自动生成
                #if EMBEDDED_LICENSE
                string licensedTo = EMBEDDED_LICENSE_TO;
                LicenseType type = (LicenseType)EMBEDDED_LICENSE_TYPE;
                DateTime expirationDate = new DateTime(EMBEDDED_EXPIRATION_TICKS);
                LicenseFeatures features = (LicenseFeatures)EMBEDDED_FEATURES;
                
                var license = new MarsLicenseInfo
                {
                    LicenseKey = EMBEDDED_LICENSE_KEY,
                    LicensedTo = licensedTo,
                    Type = type,
                    ExpirationDate = expirationDate,
                    Features = features,
                    IsActivated = true,
                    ActivationDate = DateTime.Now,
                    MaxActivations = 1,
                    HardwareId = "", // 编译时不绑定硬件
                    SupportedVersions = EMBEDDED_SUPPORTED_VERSIONS
                };
                
                logger.Info($"LoadFromCompiledConstants\tSuccess|Type: {type}");
                return license;
                #else
                logger.Warn("LoadFromCompiledConstants\tEMBEDDED_LICENSE not defined");
                return null;
                #endif
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"LoadFromCompiledConstants\tException: {ex.Message}");
                return null;
            }
            finally
            {
                logger.Info("LoadFromCompiledConstants\tEnd");
            }
        }

        /// <summary>
        /// 方案4: 混合模式 - 优先使用嵌入式，失败则使用文件
        /// </summary>
        public static MarsLicenseInfo LoadLicenseWithFallback()
        {
            logger.Info("LoadLicenseWithFallback\tBegin");
            
            // 1. 首先尝试从编译时常量加载（最安全）
            var license = LoadFromCompiledConstants();
            if (license != null && license.IsValid())
            {
                logger.Info("LoadLicenseWithFallback\tLoaded from compiled constants");
                return license;
            }

            // 2. 尝试从程序集属性加载
            license = LoadFromAssemblyAttribute();
            if (license != null && license.IsValid())
            {
                logger.Info("LoadLicenseWithFallback\tLoaded from assembly attribute");
                return license;
            }

            // 3. 尝试从嵌入资源加载
            license = LoadFromEmbeddedResource();
            if (license != null && license.IsValid())
            {
                logger.Info("LoadLicenseWithFallback\tLoaded from embedded resource");
                return license;
            }

            // 4. 最后尝试从外部文件加载
            license = MarsLicenseManager.Instance.CurrentLicense;
            if (license != null && license.IsValid())
            {
                logger.Info("LoadLicenseWithFallback\tLoaded from external file");
                return license;
            }

            logger.Warn("LoadLicenseWithFallback\tAll methods failed");
            return null;
        }

        /// <summary>
        /// 解析嵌入的 License Key
        /// </summary>
        private static MarsLicenseInfo ParseEmbeddedLicenseKey(string licenseKey, ref string errorMessage)
        {
            try
            {
                // 这里复用 MarsLicenseManager 的解析逻辑
                // 实际实现中，您可能需要访问 MarsLicenseManager 的私有方法
                // 或者将解析逻辑提取为公共方法
                
                // 简化示例：假设 License Key 是 Base64 编码的 JSON
                byte[] data = Convert.FromBase64String(licenseKey);
                string json = System.Text.Encoding.UTF8.GetString(data);
                MarsLicenseInfo license = JsonConvert.DeserializeObject<MarsLicenseInfo>(json);
                
                return license;
            }
            catch (Exception ex)
            {
                errorMessage = $"解析 License Key 失败: {ex.Message}";
                logger.Error(ex, errorMessage);
                return null;
            }
        }

        /// <summary>
        /// 验证嵌入式 License 的完整性
        /// </summary>
        public static bool ValidateEmbeddedLicense(MarsLicenseInfo license)
        {
            if (license == null) return false;

            // 1. 检查基本有效性
            if (!license.IsValid()) return false;

            // 2. 验证签名（防止被提取后修改）
            // 这里需要实现签名验证逻辑

            // 3. 检查程序集完整性（可选）
            var assembly = Assembly.GetExecutingAssembly();
            byte[] publicKey = assembly.GetName().GetPublicKey();
            if (publicKey == null || publicKey.Length == 0)
            {
                logger.Warn("ValidateEmbeddedLicense\tAssembly not signed with strong name");
                // 可以选择是否允许未签名的程序集
            }

            return true;
        }
    }

    /// <summary>
    /// 自定义程序集属性 - 用于存储 License Key
    /// 使用方法：在 AssemblyInfo.cs 中添加
    /// [assembly: EmbeddedLicense("YOUR_LICENSE_KEY_HERE")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class EmbeddedLicenseAttribute : Attribute
    {
        public string LicenseKey { get; private set; }

        public EmbeddedLicenseAttribute(string licenseKey)
        {
            LicenseKey = licenseKey;
        }
    }
}

