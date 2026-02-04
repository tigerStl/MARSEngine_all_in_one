using MARSEncoderApps.encoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MARSEncoderApps.data
{
    internal class MarsKeyMgr
    {

        public static string[] GenerateKeyAndIv()
        {
            string password = "Jackliew@@@752381"; // 你的输入字符串
            byte[] salt = Encoding.UTF8.GetBytes("###Jack1981liew"); // 你可以改成随机值
            List<string> rslt = new List<string>();
            // 生成 Key 和 IV
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] key = deriveBytes.GetBytes(32); // 32 字节 = 256 位
                byte[] iv = deriveBytes.GetBytes(16);  // 16 字节 = 128 位
                rslt.Add(Convert.ToBase64String(key));
                rslt.Add(Convert.ToBase64String(iv));
                Console.WriteLine($"Key: {Convert.ToBase64String(key)}");
                Console.WriteLine($"IV:  {Convert.ToBase64String(iv)}");
            }
            return rslt.ToArray();
        }

        public static bool WriteKeyToFile(ref string strError)
        {
            ///get file path
            ///
            try
            {
                string dir = System.IO.Path.GetDirectoryName(typeof(MarsKeyMgr).Assembly.Location);
                var dataPath = System.IO.Path.Combine(dir, "data");
                if (!System.IO.Directory.Exists(dataPath))
                {
                    System.IO.Directory.CreateDirectory(dataPath);
                }
                var targetFileName = System.IO.Path.Combine(dataPath, EnCodeConstant.cnst_pwd_file);
                if (File.Exists(targetFileName))
                {
                    File.Delete(targetFileName);
                }
                var keyandIv = GenerateKeyAndIv();
                System.IO.File.WriteAllText(targetFileName, string.Join("\r\n", keyandIv));
                return true;
            }
            catch (Exception e) {
                strError = e.Message;
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }


        public static bool ReadKeyFile(ref string strKey,ref string strIv)
        {
            string dir = System.IO.Path.GetDirectoryName(typeof(MarsKeyMgr).Assembly.Location);
            var dataPath = System.IO.Path.Combine(dir, "data");
            var targetFileName = System.IO.Path.Combine(dataPath, EnCodeConstant.cnst_pwd_file);
            if (!File.Exists(targetFileName))
            {
                return false;
            }
            var allTxt = System.IO.File.ReadAllText(targetFileName);
            var kndIv = string.IsNullOrEmpty(allTxt)?null:allTxt.Split(new string[] {"\r\n", "\r", "\n" },StringSplitOptions.RemoveEmptyEntries);
            if (kndIv == null) { return false; }
            if (kndIv.Length != 2) { return false; }
            strKey = kndIv[0];
            strIv = kndIv[1];
            return true;
        }
    }
}
