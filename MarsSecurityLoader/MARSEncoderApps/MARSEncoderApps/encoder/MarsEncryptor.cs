using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MARSEncoderApps.encoder
{
    class MarsEncryptionConfig
    {
        public MarsEncryptionConfigSub EncryptionSettings { get; set; }
    }
    class MarsEncryptionConfigSub
    {
        public string[] Directories { get; set; } = Array.Empty<string>();
        public string OutputFile { get; set; } = "MarsAgent.source";
        public string Key { get; set; } = "";
        public string IV { get; set; } = "";
    }

    internal class MarsEncryptor
    {
        private static void EncryptFiles(string[] inputFiles, string outputFile, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            using FileStream fsOut = new(outputFile, FileMode.Create);
            using CryptoStream cs = new(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write);
            int idx = 1;
            foreach (string file in inputFiles)
            {
                string fileName = Path.GetFileName(file);
                byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName + "\0");

                cs.Write(fileNameBytes, 0, fileNameBytes.Length);

                byte[] fileData = File.ReadAllBytes(file);
                cs.Write(fileData, 0, fileData.Length);

                Console.WriteLine($"|已经加密|{idx++}/{inputFiles.Length}|: {file}");
            }
        }

        public static void EncrypFilesEntry(string strKey,string strIv)
        {
            // 读取配置文件
            var config = LoadConfig();
            if (config == null)
            {
                Console.WriteLine("❌Can't load Config files");
                return;
            }

            byte[] key = Convert.FromBase64String(strKey);
            byte[] iv = Convert.FromBase64String(strIv);

            // 查找要加密的文件
            //var filesToEncrypt = config.EncryptionSettings.Directories
            //    .SelectMany(dir => Directory.Exists(dir) ? Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories) : Array.Empty<string>())
            //    .Where(f => f.EndsWith(".exe") || f.EndsWith(".dll"))
            //    .ToArray();
            var filesToEncrypt = Directory.Exists(config.EncryptionSettings.Directories[0])?
                Directory.GetFiles(config.EncryptionSettings.Directories[0], "*.*", SearchOption.AllDirectories):
                Array.Empty<string>()                
                .Where(f => f.EndsWith(".exe") || f.EndsWith(".dll"))
                .ToArray();

            if (filesToEncrypt.Length == 0)
            {
                Console.WriteLine("⚠️ 没有找到要加密的文件！");
                return;
            }
            string strOutFileName = Path.Combine(config.EncryptionSettings.Directories[1], config.EncryptionSettings.OutputFile);
            EncryptFiles(filesToEncrypt, strOutFileName , key, iv);
            Console.WriteLine("✅ 加密完成！");
        }

        static MarsEncryptionConfig? LoadConfig()
        {
            try
            {
                string json = File.ReadAllText("appsettings.json");
                return JsonSerializer.Deserialize<MarsEncryptionConfig>(json);
            }
            catch(Exception e)
            {
                Console.WriteLine($"[Exception]\tLoadConfig\t{e.Message}|{e.StackTrace}");
                return null;
            }
        }
    }
}
