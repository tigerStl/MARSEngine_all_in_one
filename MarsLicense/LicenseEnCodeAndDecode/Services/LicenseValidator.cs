using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using LicenseEnCodeAndDecode.Models;

namespace LicenseEnCodeAndDecode.Services
{
    /// <summary>
    /// License验证器 - 用于客户端应用程序集成
    /// </summary>
    public class LicenseValidator
    {
        private readonly LicenseEncryptionService _encryptionService;
        private LicenseInfo? _currentLicense;

        public LicenseValidator()
        {
            _encryptionService = new LicenseEncryptionService();
        }

        /// <summary>
        /// 加载并验证License文件
        /// </summary>
        /// <param name="licenseFilePath">License文件路径</param>
        /// <returns>验证结果</returns>
        public LicenseValidationResult ValidateLicense(string licenseFilePath)
        {
            var result = new LicenseValidationResult();

            try
            {
                // 检查文件是否存在
                if (!File.Exists(licenseFilePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "License文件不存在";
                    return result;
                }

                // 读取并解密License
                byte[] encryptedData = File.ReadAllBytes(licenseFilePath);
                _currentLicense = _encryptionService.DecryptAndValidateLicense(encryptedData);

                if (_currentLicense == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "License文件无效或已被篡改";
                    return result;
                }

                // 检查是否过期
                if (_currentLicense.ExpirationDate < DateTime.Now)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"License已过期（过期日期：{_currentLicense.ExpirationDate:yyyy-MM-dd}）";
                    result.IsExpired = true;
                    return result;
                }

                // 验证MAC地址
                if (!ValidateMacAddress())
                {
                    result.IsValid = false;
                    result.ErrorMessage = "当前机器的MAC地址未授权";
                    return result;
                }

                // 验证应用程序（如果启用了限制）
                if (_currentLicense.RestrictApplication)
                {
                    string? currentAppPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                    if (currentAppPath != null && !ValidateApplication(currentAppPath))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "当前应用程序未授权";
                        return result;
                    }
                }

                // 所有验证通过
                result.IsValid = true;
                result.LicenseInfo = _currentLicense;
                result.Message = "License验证成功";

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"验证过程发生错误：{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 验证MAC地址
        /// </summary>
        private bool ValidateMacAddress()
        {
            if (_currentLicense == null || _currentLicense.MacAddresses.Count == 0)
                return false;

            try
            {
                // 获取本机所有网络接口的MAC地址
                var localMacAddresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                               && nic.OperationalStatus == OperationalStatus.Up)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .Where(mac => !string.IsNullOrEmpty(mac))
                    .ToList();

                // 格式化MAC地址以便比较
                var formattedLocalMacs = localMacAddresses
                    .Select(mac => FormatMacAddress(mac))
                    .ToList();

                var formattedLicenseMacs = _currentLicense.MacAddresses
                    .Select(mac => FormatMacAddress(mac))
                    .ToList();

                // 检查是否有匹配的MAC地址
                return formattedLocalMacs.Any(localMac =>
                    formattedLicenseMacs.Contains(localMac, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 格式化MAC地址为统一格式（移除分隔符）
        /// </summary>
        private string FormatMacAddress(string macAddress)
        {
            return macAddress.Replace("-", "").Replace(":", "").ToUpperInvariant();
        }

        /// <summary>
        /// 验证应用程序
        /// </summary>
        private bool ValidateApplication(string appPath)
        {
            if (_currentLicense == null || _currentLicense.Applications.Count == 0)
                return true; // 如果没有应用限制，则允许

            try
            {
                string appFileName = Path.GetFileName(appPath);

                // 查找匹配的应用程序
                var matchedApp = _currentLicense.Applications
                    .FirstOrDefault(app => app.Name.Equals(appFileName, StringComparison.OrdinalIgnoreCase));

                if (matchedApp == null)
                    return false;

                // 验证文件哈希
                if (!string.IsNullOrEmpty(matchedApp.FileHash))
                {
                    string currentHash = LicenseEncryptionService.CalculateFileMD5(appPath);
                    if (!currentHash.Equals(matchedApp.FileHash, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // 验证文件大小
                FileInfo fileInfo = new FileInfo(appPath);
                if (fileInfo.Length != matchedApp.FileSize)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取License剩余天数
        /// </summary>
        public int GetRemainingDays()
        {
            if (_currentLicense == null)
                return 0;

            var remainingTime = _currentLicense.ExpirationDate - DateTime.Now;
            return Math.Max(0, (int)remainingTime.TotalDays);
        }

        /// <summary>
        /// 获取当前License信息
        /// </summary>
        public LicenseInfo? GetCurrentLicense()
        {
            return _currentLicense;
        }
    }

    /// <summary>
    /// License验证结果
    /// </summary>
    public class LicenseValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// 成功消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// License信息
        /// </summary>
        public LicenseInfo? LicenseInfo { get; set; }
    }
}
