using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using Newtonsoft.Json;
using NLog;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// License 管理器
    /// </summary>
    public class MarsLicenseManager
    {
        private static readonly Logger logger = LogManager.GetLogger("MarsSpyLog");
        private static MarsLicenseManager _instance;
        private static readonly object _lock = new object();
        
        private const string LICENSE_FILE_NAME = "mars.lic";
        private const string ENCRYPTION_KEY = "MARS_SPY_TOOL_2025_ENCRYPTION_KEY_V1"; // 建议使用更复杂的密钥
        private const string SIGNATURE_SALT = "MARS_LICENSE_SIGNATURE_SALT_2025"; // 签名盐值

        private MarsLicenseInfo _currentLicense;
        private string _licenseFilePath;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static MarsLicenseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MarsLicenseManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前 License 信息
        /// </summary>
        public MarsLicenseInfo CurrentLicense => _currentLicense;

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private MarsLicenseManager()
        {
            _licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LICENSE_FILE_NAME);
            LoadLicense();
        }

        /// <summary>
        /// 加载 License
        /// </summary>
        private void LoadLicense()
        {
            logger.Info("LoadLicense\tBegin");
            try
            {
                if (!File.Exists(_licenseFilePath))
                {
                    logger.Warn($"LoadLicense\tLicense file not found: {_licenseFilePath}");
                    _currentLicense = CreateTrialLicense();
                    SaveLicense(_currentLicense);
                    return;
                }

                string encryptedContent = File.ReadAllText(_licenseFilePath);
                string decryptedContent = DecryptString(encryptedContent);
                _currentLicense = JsonConvert.DeserializeObject<MarsLicenseInfo>(decryptedContent);

                // 验证签名
                if (!VerifySignature(_currentLicense))
                {
                    logger.Error("LoadLicense\tLicense signature verification failed");
                    throw new Exception("License 文件已被篡改，签名验证失败");
                }

                // 验证硬件绑定
                if (!string.IsNullOrEmpty(_currentLicense.HardwareId))
                {
                    string currentHardwareId = GetHardwareId();
                    if (_currentLicense.HardwareId != currentHardwareId)
                    {
                        logger.Error($"LoadLicense\tHardware ID mismatch. Expected: {_currentLicense.HardwareId}, Current: {currentHardwareId}");
                        throw new Exception("License 与当前机器不匹配");
                    }
                }

                logger.Info($"LoadLicense\tLicense loaded successfully. Type: {_currentLicense.Type}, Valid: {_currentLicense.IsValid()}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"LoadLicense\tException: {ex.Message}");
                _currentLicense = CreateTrialLicense();
            }
            finally
            {
                logger.Info("LoadLicense\tEnd");
            }
        }

        /// <summary>
        /// 保存 License
        /// </summary>
        public bool SaveLicense(MarsLicenseInfo license)
        {
            logger.Info("SaveLicense\tBegin");
            try
            {
                // 生成签名
                license.Signature = GenerateSignature(license);

                string jsonContent = JsonConvert.SerializeObject(license, Formatting.Indented);
                string encryptedContent = EncryptString(jsonContent);
                
                File.WriteAllText(_licenseFilePath, encryptedContent);
                _currentLicense = license;
                
                logger.Info("SaveLicense\tLicense saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"SaveLicense\tException: {ex.Message}");
                return false;
            }
            finally
            {
                logger.Info("SaveLicense\tEnd");
            }
        }

        /// <summary>
        /// 激活 License
        /// </summary>
        public bool ActivateLicense(string licenseKey, ref string errorMessage)
        {
            logger.Info($"ActivateLicense\tBegin. LicenseKey: {MaskLicenseKey(licenseKey)}");
            try
            {
                // 解析 License Key
                MarsLicenseInfo license = ParseLicenseKey(licenseKey);
                if (license == null)
                {
                    errorMessage = "无效的 License Key";
                    logger.Error($"ActivateLicense\t{errorMessage}");
                    return false;
                }

                // 检查激活次数
                if (license.ActivationCount >= license.MaxActivations)
                {
                    errorMessage = $"License 激活次数已达上限（{license.MaxActivations}次）";
                    logger.Error($"ActivateLicense\t{errorMessage}");
                    return false;
                }

                // 绑定硬件ID
                license.HardwareId = GetHardwareId();
                license.IsActivated = true;
                license.ActivationDate = DateTime.Now;
                license.ActivationCount++;

                // 保存
                if (!SaveLicense(license))
                {
                    errorMessage = "保存 License 失败";
                    return false;
                }

                logger.Info($"ActivateLicense\tLicense activated successfully. Type: {license.Type}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"激活失败: {ex.Message}";
                logger.Error(ex, $"ActivateLicense\tException: {ex.Message}");
                return false;
            }
            finally
            {
                logger.Info("ActivateLicense\tEnd");
            }
        }

        /// <summary>
        /// 验证 License 有效性
        /// </summary>
        public bool ValidateLicense(ref string errorMessage)
        {
            logger.Info("ValidateLicense\tBegin");
            try
            {
                if (_currentLicense == null)
                {
                    errorMessage = "未找到 License 信息";
                    logger.Error($"ValidateLicense\t{errorMessage}");
                    return false;
                }

                if (!_currentLicense.IsActivated)
                {
                    errorMessage = "License 未激活";
                    logger.Warn($"ValidateLicense\t{errorMessage}");
                    return false;
                }

                if (!_currentLicense.IsValid())
                {
                    errorMessage = $"License 已过期（过期日期: {_currentLicense.ExpirationDate:yyyy-MM-dd}）";
                    logger.Error($"ValidateLicense\t{errorMessage}");
                    return false;
                }

                // 验证版本兼容性
                string currentVersion = typeof(MarsLicenseManager).Assembly.GetName().Version.ToString();
                if (!string.IsNullOrEmpty(_currentLicense.SupportedVersions))
                {
                    if (!_currentLicense.SupportedVersions.Contains(currentVersion.Substring(0, 3)))
                    {
                        errorMessage = $"License 不支持当前版本 ({currentVersion})";
                        logger.Error($"ValidateLicense\t{errorMessage}");
                        return false;
                    }
                }

                logger.Info($"ValidateLicense\tLicense is valid. Remaining days: {_currentLicense.GetRemainingDays()}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"验证失败: {ex.Message}";
                logger.Error(ex, $"ValidateLicense\tException: {ex.Message}");
                return false;
            }
            finally
            {
                logger.Info("ValidateLicense\tEnd");
            }
        }

        /// <summary>
        /// 检查功能权限
        /// </summary>
        public bool HasFeature(LicenseFeatures feature)
        {
            if (_currentLicense == null) return false;
            
            string errorMessage = "";
            if (!ValidateLicense(ref errorMessage))
            {
                logger.Warn($"HasFeature\tLicense invalid: {errorMessage}");
                return false;
            }

            bool hasFeature = _currentLicense.HasFeature(feature);
            logger.Info($"HasFeature\tFeature '{feature}': {hasFeature}");
            return hasFeature;
        }

        /// <summary>
        /// 创建试用版 License
        /// </summary>
        private MarsLicenseInfo CreateTrialLicense()
        {
            logger.Info("CreateTrialLicense\tCreating trial license");
            return new MarsLicenseInfo
            {
                Type = LicenseType.Trial,
                LicensedTo = "Trial User",
                ActivationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(30),
                Features = LicenseFeatures.BasicObjectSpy | LicenseFeatures.SingleObjectMode,
                MaxConcurrentUsers = 1,
                MaxActivations = 1,
                IsActivated = true,
                HardwareId = GetHardwareId(),
                SupportedVersions = "1.0"
            };
        }

        /// <summary>
        /// 解析 License Key（这里需要实现你自己的 License Key 格式）
        /// </summary>
        private MarsLicenseInfo ParseLicenseKey(string licenseKey)
        {
            logger.Info($"ParseLicenseKey\tParsing license key: {MaskLicenseKey(licenseKey)}");
            try
            {
                // License Key 格式示例: BASE64编码的JSON
                // 实际项目中应该使用更安全的格式，比如 RSA 加密
                byte[] data = Convert.FromBase64String(licenseKey);
                string json = Encoding.UTF8.GetString(data);
                MarsLicenseInfo license = JsonConvert.DeserializeObject<MarsLicenseInfo>(json);
                
                // 验证 License Key 的签名
                if (!VerifySignature(license))
                {
                    logger.Error("ParseLicenseKey\tSignature verification failed");
                    return null;
                }

                return license;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ParseLicenseKey\tException: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 生成 License Key（用于 License 生成工具）
        /// </summary>
        public static string GenerateLicenseKey(MarsLicenseInfo license)
        {
            license.Signature = GenerateSignature(license);
            string json = JsonConvert.SerializeObject(license);
            byte[] data = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(data);
        }

        #region 加密解密

        /// <summary>
        /// 加密字符串
        /// </summary>
        private string EncryptString(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = DeriveKeyFromPassword(ENCRYPTION_KEY, 32);
                byte[] iv = DeriveKeyFromPassword(ENCRYPTION_KEY, 16);
                
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        /// <summary>
        /// 解密 License 内容（公共方法，供 EmbeddedLicenseManager 使用）
        /// </summary>
        public string DecryptLicenseContent(string encryptedText)
        {
            return DecryptString(encryptedText);
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        private string DecryptString(string encryptedText)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = DeriveKeyFromPassword(ENCRYPTION_KEY, 32);
                byte[] iv = DeriveKeyFromPassword(ENCRYPTION_KEY, 16);
                
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        /// <summary>
        /// 从密码派生密钥
        /// </summary>
        private byte[] DeriveKeyFromPassword(string password, int keySize)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] key = new byte[keySize];
                Array.Copy(hash, key, Math.Min(hash.Length, keySize));
                return key;
            }
        }

        #endregion

        #region 签名验证

        /// <summary>
        /// 生成签名
        /// </summary>
        private static string GenerateSignature(MarsLicenseInfo license)
        {
            string data = $"{license.LicenseKey}|{license.LicensedTo}|{license.Type}|{license.ExpirationDate:yyyyMMdd}|{license.Features}|{SIGNATURE_SALT}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// 验证签名
        /// </summary>
        private bool VerifySignature(MarsLicenseInfo license)
        {
            if (string.IsNullOrEmpty(license.Signature))
            {
                logger.Warn("VerifySignature\tSignature is empty");
                return false;
            }

            string expectedSignature = GenerateSignature(license);
            bool isValid = expectedSignature == license.Signature;
            
            if (!isValid)
            {
                logger.Error($"VerifySignature\tSignature mismatch. Expected: {expectedSignature}, Actual: {license.Signature}");
            }
            
            return isValid;
        }

        #endregion

        #region 硬件信息

        /// <summary>
        /// 获取硬件ID（用于机器绑定）
        /// </summary>
        public static string GetHardwareId()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                
                // CPU ID
                string cpuId = GetCpuId();
                sb.Append(cpuId);

                // 主板序列号
                string motherboardId = GetMotherboardId();
                sb.Append(motherboardId);

                // MAC 地址
                string macAddress = GetMacAddress();
                sb.Append(macAddress);

                // 生成最终的硬件ID（SHA256哈希）
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                    return Convert.ToBase64String(hash).Substring(0, 32);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetHardwareId\tException: {ex.Message}");
                return "UNKNOWN_HARDWARE_ID";
            }
        }

        private static string GetCpuId()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private static string GetMotherboardId()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private static string GetMacAddress()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string mac = obj["MACAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(mac))
                        return mac;
                }
            }
            catch { }
            return "";
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 掩码显示 License Key（隐藏中间部分）
        /// </summary>
        private string MaskLicenseKey(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey) || licenseKey.Length < 10)
                return "****";
            
            return licenseKey.Substring(0, 4) + "****" + licenseKey.Substring(licenseKey.Length - 4);
        }

        #endregion
    }
}

