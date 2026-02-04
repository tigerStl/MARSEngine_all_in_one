using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using LicenseEnCodeAndDecode.Models;

namespace LicenseEnCodeAndDecode.Services
{
    /// <summary>
    /// License加密和签名服务 - 使用RSA非对称加密
    /// </summary>
    public class LicenseEncryptionService
    {
        // 对称加密密钥（用于加密License数据内容，可选）
        private const string EncryptionKey = "MARS-LICENSE-2024-SECRET-KEY-32"; // 32字节密钥

        // RSA密钥（非对称加密）
        private RSA? _privateKey;  // 私钥用于签名（生成License）
        private RSA? _publicKey;   // 公钥用于验证（验证License）

        /// <summary>
        /// 默认构造函数 - 使用内置密钥（仅用于向后兼容）
        /// </summary>
        public LicenseEncryptionService()
        {
        }

        /// <summary>
        /// 使用私钥构造 - 用于生成License
        /// </summary>
        /// <param name="privateKeyPath">私钥文件路径</param>
        public LicenseEncryptionService(string privateKeyPath)
        {
            LoadPrivateKey(privateKeyPath);
        }

        /// <summary>
        /// 加载私钥文件（用于生成License）
        /// </summary>
        public void LoadPrivateKey(string privateKeyPath)
        {
            _privateKey = RsaKeyGenerator.LoadPrivateKeyFromFile(privateKeyPath);
        }

        /// <summary>
        /// 加载公钥文件（用于验证License）
        /// </summary>
        public void LoadPublicKey(string publicKeyPath)
        {
            _publicKey = RsaKeyGenerator.LoadPublicKeyFromFile(publicKeyPath);
        }

        /// <summary>
        /// 从PEM字符串加载私钥
        /// </summary>
        public void LoadPrivateKeyFromPem(string pemString)
        {
            _privateKey = RsaKeyGenerator.LoadPrivateKeyFromPem(pemString);
        }

        /// <summary>
        /// 从PEM字符串加载公钥
        /// </summary>
        public void LoadPublicKeyFromPem(string pemString)
        {
            _publicKey = RsaKeyGenerator.LoadPublicKeyFromPem(pemString);
        }

        /// <summary>
        /// 生成加密的License文件（使用RSA签名）
        /// </summary>
        public byte[] GenerateEncryptedLicense(LicenseInfo licenseInfo)
        {
            // 序列化License信息
            string jsonData = JsonConvert.SerializeObject(licenseInfo, Formatting.None);

            // 生成RSA数字签名
            string signature;
            if (_privateKey != null)
            {
                // 使用RSA私钥签名（推荐）
                signature = GenerateRsaSignature(jsonData, _privateKey);
            }
            else
            {
                // 向后兼容：使用HMAC签名
                signature = GenerateHmacSignature(jsonData);
            }

            // 组合数据和签名
            var dataWithSignature = new
            {
                Data = jsonData,
                Signature = signature,
                Timestamp = DateTime.UtcNow,
                SignatureType = _privateKey != null ? "RSA" : "HMAC" // 标记签名类型
            };

            string finalJson = JsonConvert.SerializeObject(dataWithSignature);

            // 加密数据（可选，默认不加密以提高性能）
            // 因为RSA签名已经保证了数据完整性
            byte[] encryptedData = EncryptData(finalJson);

            return encryptedData;
        }

        /// <summary>
        /// 验证和解密License文件（使用RSA或HMAC验证）
        /// </summary>
        public LicenseInfo? DecryptAndValidateLicense(byte[] encryptedData)
        {
            try
            {
                // 解密数据
                string decryptedJson = DecryptData(encryptedData);

                // 反序列化
                var dataWithSignature = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedJson);
                if (dataWithSignature == null)
                    return null;

                string jsonData = dataWithSignature["Data"]?.ToString() ?? string.Empty;
                string signature = dataWithSignature["Signature"]?.ToString() ?? string.Empty;

                // 检查签名类型
                string signatureType = "HMAC"; // 默认HMAC（向后兼容）
                if (dataWithSignature.ContainsKey("SignatureType"))
                {
                    signatureType = dataWithSignature["SignatureType"]?.ToString() ?? "HMAC";
                }

                // 验证签名
                bool isValid = false;
                if (signatureType == "RSA" && _publicKey != null)
                {
                    // 使用RSA公钥验证
                    isValid = VerifyRsaSignature(jsonData, signature, _publicKey);
                }
                else
                {
                    // 使用HMAC验证（向后兼容）
                    isValid = VerifyHmacSignature(jsonData, signature);
                }

                if (!isValid)
                    return null;

                // 反序列化License信息
                var licenseInfo = JsonConvert.DeserializeObject<LicenseInfo>(jsonData);

                return licenseInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 加密数据
        /// </summary>
        private byte[] EncryptData(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // 使用固定IV（实际应用中建议使用随机IV并存储）
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// 解密数据
        /// </summary>
        private string DecryptData(byte[] cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (MemoryStream ms = new MemoryStream(cipherText))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 生成RSA数字签名
        /// </summary>
        private string GenerateRsaSignature(string data, RSA privateKey)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = privateKey.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// 验证RSA数字签名
        /// </summary>
        private bool VerifyRsaSignature(string data, string signature, RSA publicKey)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return publicKey.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成HMAC签名（向后兼容）
        /// </summary>
        private string GenerateHmacSignature(string data)
        {
            const string SignatureKey = "MARS-SIGNATURE-KEY";
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SignatureKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// 验证HMAC签名（向后兼容）
        /// </summary>
        private bool VerifyHmacSignature(string data, string signature)
        {
            string computedSignature = GenerateHmacSignature(data);
            return computedSignature == signature;
        }

        /// <summary>
        /// 计算文件MD5哈希
        /// </summary>
        public static string CalculateFileMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
