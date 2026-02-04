using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Securities
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
            byte[] byResult = icTransform.TransformFinalBlock(arrSrc, 0,arrSrc.Length);
            //objDesProvider.Clear();
            objDesProvider.Clear();
            return Convert.ToBase64String(byResult, 0, byResult.Length);
            //Convert.ToBase64String(byResult,0, byResult.Length);            
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
            catch (Exception e)
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
