using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Mars.message.Securities
{
    public sealed class MarsEncodePwd
    {
        private static byte[] cnst_key = {84,105,71,101,114,76,73,69,87,
            84, 105, 71, 101, 114, 76, 73};//87
        public static string EncodeString(string strSrc)
        {

            TripleDESCryptoServiceProvider objDesProvider = new TripleDESCryptoServiceProvider();
            objDesProvider.Key = UTF8Encoding.UTF8.GetBytes(System.Text.Encoding.Default.GetString(cnst_key));
            objDesProvider.Mode = CipherMode.ECB;
            objDesProvider.Padding = PaddingMode.PKCS7;
            ICryptoTransform icTransform = objDesProvider.CreateEncryptor();
            byte[] arrSrc = UTF8Encoding.UTF8.GetBytes(strSrc);
            byte[] byResult = icTransform.TransformFinalBlock(arrSrc, 0, arrSrc.Length);
            //objDesProvider.Clear();
            objDesProvider.Clear();
            return Convert.ToBase64String(byResult, 0, byResult.Length);
            //Convert.ToBase64String(byResult,0, byResult.Length);            
        }

        public static bool EncryptDllWithTripleDES(string dllPath, string password, ref string outPath)
        {
            if (!File.Exists(dllPath))
            {
                Console.WriteLine("DLL 文件不存在。");
                return false;
            }

            // 文件头
            byte[] headerBytes = Encoding.ASCII.GetBytes("MARRESOUCE"); // 10 字节

            // 生成密钥和 IV
            using (var md5 = MD5.Create())
            {
                byte[] key = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] iv = new byte[8]; // TripleDES 的 IV 固定为 8 字节
                Array.Copy(key, iv, 8);

                using (var tdes = TripleDES.Create())
                {
                    tdes.Key = key.Length == 16 ? key : PadKey(key);
                    tdes.IV = iv;
                    tdes.Mode = CipherMode.CBC;
                    tdes.Padding = PaddingMode.PKCS7;
                    if (string.IsNullOrEmpty(outPath))
                        outPath = Path.Combine(
                            Path.GetDirectoryName(dllPath),
                            Path.GetFileNameWithoutExtension(dllPath) + ".bin");

                    using (var inputFile = File.OpenRead(dllPath))
                    using (var outputFile = File.Create(outPath))
                    {
                        // 写入自定义文件头
                        outputFile.Write(headerBytes, 0, headerBytes.Length);

                        // 加密写入
                        using (var cryptoStream = new CryptoStream(outputFile, tdes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            inputFile.CopyTo(cryptoStream);
                        }
                    }

                    Console.WriteLine($"加密成功，文件包含头标记：{outPath}");

                    // 创建key文件
                    string strKeyFile = Path.Combine(
                        Path.GetDirectoryName(dllPath),
                        "MarsResource.key");
                    if (File.Exists(strKeyFile))
                    {
                        File.Delete(strKeyFile);
                    }
                    string encodeKey = EncodeString(password);
                    System.IO.File.WriteAllText(strKeyFile, encodeKey);

                    return true;
                }
            }
        }

        public static MemoryStream DecryptDllToMemory(string binPath, string password,string strFileName,ref string strError,ref bool isOk )
        {
            if (!File.Exists(binPath))
            {
                strError = $"加密文件不存在：{binPath}";
                isOk = false;
                return null;
                //throw new FileNotFoundException("加密文件不存在", binPath);
            }

            using (var fileStream = File.OpenRead(binPath))
            {
                // 1. 读取并验证文件头
                byte[] header = new byte[10];
                fileStream.Read(header, 0, 10);
                string headerStr = Encoding.ASCII.GetString(header);

                if (headerStr != "MARRESOUCE")
                    throw new InvalidDataException("无效的文件头，文件可能不是有效的加密文件。");

                // 2. 准备密钥和 IV
                using (var md5 = MD5.Create())
                {
                    byte[] key = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                    byte[] iv = new byte[8];
                    Array.Copy(key, iv, 8);

                    using (var tdes = TripleDES.Create())
                    {
                        tdes.Key = key.Length == 16 ? PadKey(key) : key;
                        tdes.IV = iv;
                        tdes.Mode = CipherMode.CBC;
                        tdes.Padding = PaddingMode.PKCS7;

                        // 3. 解密流
                        using (var cryptoStream = new CryptoStream(fileStream, tdes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            MemoryStream decryptedStream = new MemoryStream();
                            cryptoStream.CopyTo(decryptedStream);
                            decryptedStream.Position = 0;
                            File.WriteAllBytes(strFileName, decryptedStream.ToArray());
                            return decryptedStream;
                        }
                    }
                }
            }
        }



        private static byte[] PadKey(byte[] key)
        {
            // 将16字节扩展为24字节（TripleDES 兼容模式）
            byte[] padded = new byte[24];
            Array.Copy(key, padded, key.Length);
            Array.Copy(key, 0, padded, 16, 8); // 重复前8字节
            return padded;
        }

        public static string DecodeString(string strSrc)
        {
            try
            {
                TripleDESCryptoServiceProvider objDesProvider = new TripleDESCryptoServiceProvider();
                objDesProvider.Key = UTF8Encoding.UTF8.GetBytes(System.Text.Encoding.Default.GetString(cnst_key));
                objDesProvider.Mode = CipherMode.ECB;
                objDesProvider.Padding = PaddingMode.PKCS7;
                ICryptoTransform icTransform = objDesProvider.CreateDecryptor();
                byte[] arrSrc = //UTF8Encoding.UTF8.GetBytes(strSrc);
                    Convert.FromBase64String(strSrc);

                byte[] byResult = icTransform.TransformFinalBlock(arrSrc, 0, arrSrc.Length);
                //objDesProvider.Clear();
                objDesProvider.Clear();
                return UTF8Encoding.UTF8.GetString(byResult, 0, byResult.Length);
            }
            catch (Exception )
            {

            }
            return strSrc;
        }

        //        public static string Encrypt(string toEncrypt, bool useHashing=false)
        //        {
        //            byte[] keyArray;
        //            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

        ////            System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();
        //            // Get the key from config file
        //            string key = System.Text.Encoding.Default.GetString(cnst_key); 
        //            //System.Windows.Forms.MessageBox.Show(key);
        //            if (useHashing)
        //            {
        //                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
        //                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
        //                hashmd5.Clear();
        //            }
        //            else
        //                keyArray = UTF8Encoding.UTF8.GetBytes(key);

        //            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
        //            tdes.Key = keyArray;
        //            tdes.Mode = CipherMode.ECB;
        //            tdes.Padding = PaddingMode.PKCS7;

        //            ICryptoTransform cTransform = tdes.CreateEncryptor();
        //            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        //            tdes.Clear();
        //            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        //        }

        //        public static string Decrypt(string cipherString, bool useHashing=false)
        //        {
        //            byte[] keyArray;
        //            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

        //            //            System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();
        //            string key = System.Text.Encoding.Default.GetString(cnst_key);
        //            //Get your key from config file to open the lock!
        //            //string key = (string)settingsReader.GetValue("SecurityKey", typeof(String));

        //            if (useHashing)
        //            {
        //                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
        //                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
        //                hashmd5.Clear();
        //            }
        //            else
        //                keyArray = UTF8Encoding.UTF8.GetBytes(key);

        //            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
        //            tdes.Key = keyArray;
        //            tdes.Mode = CipherMode.ECB;
        //            tdes.Padding = PaddingMode.PKCS7;

        //            ICryptoTransform cTransform = tdes.CreateDecryptor();
        //            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        //            tdes.Clear();
        //            return UTF8Encoding.UTF8.GetString(resultArray);
        //        }
    }
}
