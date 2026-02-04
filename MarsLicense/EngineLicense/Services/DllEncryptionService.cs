using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MarsLicenseManager.Services
{
    /// <summary>
    /// DLL文件加密和解密服务
    /// </summary>
    public class DllEncryptionService
    {
        private const string AesKey = "MARsAutomationTest";
        private const int PrefixRandomLength = 888;  // 前缀随机字符串长度
        private const int SuffixRandomLength = 520;  // 后缀随机字符串长度

        /// <summary>
        /// 构造函数
        /// </summary>
        public DllEncryptionService()
        {
        }

        /// <summary>
        /// 加密DLL文件
        /// </summary>
        /// <param name="dllFilePath">DLL文件路径</param>
        /// <param name="outputPath">输出加密文件路径</param>
        /// <returns>是否成功</returns>
        public bool EncryptDllFile(string dllFilePath, string outputPath)
        {
            try
            {
                if (!File.Exists(dllFilePath))
                {
                    throw new FileNotFoundException($"DLL文件不存在：{dllFilePath}");
                }

                // 读取原始DLL文件
                byte[] originalDllBytes = File.ReadAllBytes(dllFilePath);

                // 使用AES加密DLL文件
                byte[] encryptedDllBytes = EncryptWithAes(originalDllBytes);

                // 生成前缀和后缀随机字符串
                string prefixRandom = GenerateRandomString(PrefixRandomLength);
                string suffixRandom = GenerateRandomString(SuffixRandomLength);

                // 组合：前缀 + 加密的DLL + 后缀
                byte[] prefixBytes = Encoding.UTF8.GetBytes(prefixRandom);
                byte[] suffixBytes = Encoding.UTF8.GetBytes(suffixRandom);

                byte[] combinedBytes = new byte[prefixBytes.Length + encryptedDllBytes.Length + suffixBytes.Length];
                Buffer.BlockCopy(prefixBytes, 0, combinedBytes, 0, prefixBytes.Length);
                Buffer.BlockCopy(encryptedDllBytes, 0, combinedBytes, prefixBytes.Length, encryptedDllBytes.Length);
                Buffer.BlockCopy(suffixBytes, 0, combinedBytes, prefixBytes.Length + encryptedDllBytes.Length, suffixBytes.Length);

                // 保存加密文件
                File.WriteAllBytes(outputPath, combinedBytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加密DLL文件失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解密加密的DLL文件并保存为原始DLL
        /// </summary>
        /// <param name="encryptedFilePath">加密文件路径</param>
        /// <param name="outputPath">输出DLL文件路径</param>
        /// <returns>是否成功</returns>
        public bool DecryptDllFile(string encryptedFilePath, string outputPath)
        {
            try
            {
                if (!File.Exists(encryptedFilePath))
                {
                    throw new FileNotFoundException($"加密文件不存在：{encryptedFilePath}");
                }

                byte[] decryptedBytes = DecryptDllToStream(encryptedFilePath);
                File.WriteAllBytes(outputPath, decryptedBytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解密DLL文件失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解密加密的DLL文件到内存流
        /// </summary>
        /// <param name="encryptedFilePath">加密文件路径</param>
        /// <returns>解密后的DLL字节数组</returns>
        public byte[] DecryptDllToStream(string encryptedFilePath)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException($"加密文件不存在：{encryptedFilePath}");
            }

            byte[] encryptedContent = File.ReadAllBytes(encryptedFilePath);

            // 移除前缀和后缀
            byte[] encryptedDllBytes = ExtractEncryptedDllBytes(encryptedContent);

            // 使用AES解密DLL文件
            return DecryptWithAes(encryptedDllBytes);
        }

        /// <summary>
        /// 使用AES加密数据
        /// </summary>
        /// <param name="data">要加密的数据</param>
        /// <returns>加密后的数据</returns>
        private byte[] EncryptWithAes(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                // 使用固定的密钥和IV
                aes.Key = GetAesKey();
                aes.IV = GetAesIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                    csEncrypt.FlushFinalBlock();
                    return msEncrypt.ToArray();
                }
            }
        }

        /// <summary>
        /// 使用AES解密数据
        /// </summary>
        /// <param name="encryptedData">加密的数据</param>
        /// <returns>解密后的数据</returns>
        private byte[] DecryptWithAes(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                // 使用固定的密钥和IV
                aes.Key = GetAesKey();
                aes.IV = GetAesIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(encryptedData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var msResult = new MemoryStream())
                {
                    csDecrypt.CopyTo(msResult);
                    return msResult.ToArray();
                }
            }
        }

        /// <summary>
        /// 获取AES密钥
        /// </summary>
        /// <returns>AES密钥字节数组</returns>
        private byte[] GetAesKey()
        {
            // 将字符串转换为32字节的密钥
            byte[] keyBytes = Encoding.UTF8.GetBytes(AesKey);
            byte[] key = new byte[32]; // AES-256需要32字节密钥
            
            // 如果密钥长度不足32字节，则重复填充
            for (int i = 0; i < 32; i++)
            {
                key[i] = keyBytes[i % keyBytes.Length];
            }
            
            return key;
        }

        /// <summary>
        /// 获取AES IV
        /// </summary>
        /// <returns>AES IV字节数组</returns>
        private byte[] GetAesIV()
        {
            // 使用密钥的哈希值作为IV
            using (var sha256 = SHA256.Create())
            {
                byte[] keyHash = sha256.ComputeHash(GetAesKey());
                byte[] iv = new byte[16]; // AES需要16字节IV
                Array.Copy(keyHash, 0, iv, 0, 16);
                return iv;
            }
        }

        /// <summary>
        /// 从加密文件中提取加密的DLL字节
        /// </summary>
        /// <param name="encryptedContent">加密文件内容</param>
        /// <returns>加密的DLL字节数组</returns>
        private byte[] ExtractEncryptedDllBytes(byte[] encryptedContent)
        {
            // 计算前缀和后缀的字节长度
            int prefixByteLength = PrefixRandomLength;
            int suffixByteLength = SuffixRandomLength;
            
            // 验证文件长度
            int expectedLength = prefixByteLength + suffixByteLength;
            if (encryptedContent.Length <= expectedLength)
            {
                throw new ArgumentException("加密文件长度不足，无法提取DLL数据");
            }

            // 提取中间的加密DLL数据
            int dllDataLength = encryptedContent.Length - prefixByteLength - suffixByteLength;
            byte[] encryptedDllBytes = new byte[dllDataLength];
            Array.Copy(encryptedContent, prefixByteLength, encryptedDllBytes, 0, dllDataLength);

            return encryptedDllBytes;
        }

        /// <summary>
        /// 生成指定长度的随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns>随机字符串</returns>
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}