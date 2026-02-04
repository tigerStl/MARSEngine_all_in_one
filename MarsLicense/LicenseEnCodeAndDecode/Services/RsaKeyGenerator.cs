using System;
using System.IO;
using System.Security.Cryptography;

namespace LicenseEnCodeAndDecode.Services
{
    /// <summary>
    /// RSA密钥生成器 - 用于生成公钥和私钥对
    /// </summary>
    public static class RsaKeyGenerator
    {
        /// <summary>
        /// 生成RSA密钥对并保存到文件
        /// </summary>
        /// <param name="publicKeyPath">公钥文件路径</param>
        /// <param name="privateKeyPath">私钥文件路径</param>
        /// <param name="keySize">密钥大小（位），默认2048</param>
        public static void GenerateKeyPair(string publicKeyPath, string privateKeyPath, int keySize = 2048)
        {
            using (var rsa = new RSACryptoServiceProvider(keySize))
            {
                // 导出公钥（XML格式，.NET Framework 兼容）
                string publicKeyXml = rsa.ToXmlString(false);
                File.WriteAllText(publicKeyPath, publicKeyXml);

                // 导出私钥（XML格式，.NET Framework 兼容）
                string privateKeyXml = rsa.ToXmlString(true);
                File.WriteAllText(privateKeyPath, privateKeyXml);

                Console.WriteLine($"密钥对生成成功！");
                Console.WriteLine($"公钥: {publicKeyPath}");
                Console.WriteLine($"私钥: {privateKeyPath} (请妥善保管，切勿泄露！)");
            }
        }

        /// <summary>
        /// 生成RSA密钥对并返回XML字符串
        /// </summary>
        /// <param name="keySize">密钥大小（位），默认2048</param>
        /// <returns>(公钥XML, 私钥XML)</returns>
        public static (string PublicKey, string PrivateKey) GenerateKeyPairString(int keySize = 2048)
        {
            using (var rsa = new RSACryptoServiceProvider(keySize))
            {
                string publicKeyXml = rsa.ToXmlString(false);
                string privateKeyXml = rsa.ToXmlString(true);
                return (publicKeyXml, privateKeyXml);
            }
        }

        /// <summary>
        /// 从XML字符串加载RSA公钥
        /// </summary>
        public static RSA LoadPublicKeyFromPem(string xmlString)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlString);
            return rsa;
        }

        /// <summary>
        /// 从XML字符串加载RSA私钥
        /// </summary>
        public static RSA LoadPrivateKeyFromPem(string xmlString)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlString);
            return rsa;
        }

        /// <summary>
        /// 从文件加载RSA公钥
        /// </summary>
        public static RSA LoadPublicKeyFromFile(string filePath)
        {
            string xmlString = File.ReadAllText(filePath);
            return LoadPublicKeyFromPem(xmlString);
        }

        /// <summary>
        /// 从文件加载RSA私钥
        /// </summary>
        public static RSA LoadPrivateKeyFromFile(string filePath)
        {
            string xmlString = File.ReadAllText(filePath);
            return LoadPrivateKeyFromPem(xmlString);
        }
    }
}
