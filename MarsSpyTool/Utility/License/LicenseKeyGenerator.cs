using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// License Key 生成器（供管理员使用）
    /// 这个类应该单独打包成一个独立的工具，不应包含在客户端程序中
    /// </summary>
    public class LicenseKeyGenerator
    {
        /// <summary>
        /// 生成试用版 License Key
        /// </summary>
        public static string GenerateTrialKey(string licensedTo, int trialDays = 30)
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = LicenseType.Trial,
                ActivationDate = DateTime.MinValue, // 未激活
                ExpirationDate = DateTime.Now.AddDays(trialDays),
                Features = LicenseFeatures.BasicObjectSpy | LicenseFeatures.SingleObjectMode,
                MaxConcurrentUsers = 1,
                MaxActivations = 1,
                IsActivated = false,
                SupportedVersions = "1.0"
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 生成标准版 License Key
        /// </summary>
        public static string GenerateStandardKey(string licensedTo, DateTime expirationDate, int maxActivations = 1)
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = LicenseType.Standard,
                ActivationDate = DateTime.MinValue,
                ExpirationDate = expirationDate,
                Features = LicenseFeatures.BasicObjectSpy 
                    | LicenseFeatures.SingleObjectMode 
                    | LicenseFeatures.AutoGenerateTestCase,
                MaxConcurrentUsers = 1,
                MaxActivations = maxActivations,
                IsActivated = false,
                SupportedVersions = "1.0"
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 生成专业版 License Key
        /// </summary>
        public static string GenerateProfessionalKey(string licensedTo, DateTime expirationDate, int maxActivations = 3)
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = LicenseType.Professional,
                ActivationDate = DateTime.MinValue,
                ExpirationDate = expirationDate,
                Features = LicenseFeatures.BasicObjectSpy 
                    | LicenseFeatures.SingleObjectMode 
                    | LicenseFeatures.AutoGenerateTestCase
                    | LicenseFeatures.RecordReplay
                    | LicenseFeatures.MultiDatabase
                    | LicenseFeatures.AdvancedObjectRecognition,
                MaxConcurrentUsers = 3,
                MaxActivations = maxActivations,
                IsActivated = false,
                SupportedVersions = "1.0,2.0"
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 生成企业版 License Key
        /// </summary>
        public static string GenerateEnterpriseKey(string licensedTo, DateTime expirationDate, int maxConcurrentUsers = 10, int maxActivations = 10)
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = LicenseType.Enterprise,
                ActivationDate = DateTime.MinValue,
                ExpirationDate = expirationDate,
                Features = LicenseFeatures.All,
                MaxConcurrentUsers = maxConcurrentUsers,
                MaxActivations = maxActivations,
                IsActivated = false,
                SupportedVersions = "1.0,2.0,3.0"
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 生成永久版 License Key
        /// </summary>
        public static string GeneratePerpetualKey(string licensedTo, int maxActivations = 1)
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = LicenseType.Perpetual,
                ActivationDate = DateTime.MinValue,
                ExpirationDate = DateTime.MaxValue,
                Features = LicenseFeatures.All,
                MaxConcurrentUsers = 1,
                MaxActivations = maxActivations,
                IsActivated = false,
                SupportedVersions = "1.0,2.0,3.0,4.0,5.0"
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 生成自定义 License Key
        /// </summary>
        public static string GenerateCustomKey(
            string licensedTo,
            LicenseType type,
            DateTime expirationDate,
            LicenseFeatures features,
            int maxConcurrentUsers = 1,
            int maxActivations = 1,
            string supportedVersions = "1.0")
        {
            var license = new MarsLicenseInfo
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(),
                LicensedTo = licensedTo,
                Type = type,
                ActivationDate = DateTime.MinValue,
                ExpirationDate = expirationDate,
                Features = features,
                MaxConcurrentUsers = maxConcurrentUsers,
                MaxActivations = maxActivations,
                IsActivated = false,
                SupportedVersions = supportedVersions
            };

            return MarsLicenseManager.GenerateLicenseKey(license);
        }

        /// <summary>
        /// 示例：批量生成 License Keys
        /// </summary>
        public static Dictionary<string, string> GenerateBatchKeys(
            List<string> licensedToList, 
            LicenseType type, 
            DateTime expirationDate)
        {
            var results = new Dictionary<string, string>();
            
            foreach (var licensedTo in licensedToList)
            {
                string key = "";
                switch (type)
                {
                    case LicenseType.Trial:
                        key = GenerateTrialKey(licensedTo);
                        break;
                    case LicenseType.Standard:
                        key = GenerateStandardKey(licensedTo, expirationDate);
                        break;
                    case LicenseType.Professional:
                        key = GenerateProfessionalKey(licensedTo, expirationDate);
                        break;
                    case LicenseType.Enterprise:
                        key = GenerateEnterpriseKey(licensedTo, expirationDate);
                        break;
                    case LicenseType.Perpetual:
                        key = GeneratePerpetualKey(licensedTo);
                        break;
                }
                
                results[licensedTo] = key;
            }
            
            return results;
        }
    }
}

