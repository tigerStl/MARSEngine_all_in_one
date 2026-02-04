using System;
using System.IO;
using LicenseEnCodeAndDecode.Services;

namespace LicenseEnCodeAndDecode
{
    /// <summary>
    /// RSA密钥对生成工具
    /// 使用方法：dotnet run --project LicenseEnCodeAndDecode -- generate-keys
    /// </summary>
    public class KeyGeneratorTool
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("MARS License - RSA密钥对生成工具");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "generate-keys")
            {
                GenerateKeys();
            }
            else
            {
                ShowUsage();
            }
        }

        private static void GenerateKeys()
        {
            Console.WriteLine("正在生成RSA密钥对...");
            Console.WriteLine();

            // 生成密钥对
            var keyPair = RsaKeyGenerator.GenerateKeyPairString(2048);
            string publicKey = keyPair.PublicKey;
            string privateKey = keyPair.PrivateKey;

            // 保存到文件
            string publicKeyFile = "mars_public.key";
            string privateKeyFile = "mars_private.key";

            File.WriteAllText(publicKeyFile, publicKey);
            File.WriteAllText(privateKeyFile, privateKey);

            Console.WriteLine("✓ 密钥对生成成功！");
            Console.WriteLine();
            Console.WriteLine($"公钥文件: {Path.GetFullPath(publicKeyFile)}");
            Console.WriteLine($"私钥文件: {Path.GetFullPath(privateKeyFile)}");
            Console.WriteLine();
            Console.WriteLine("⚠️  重要提示：");
            Console.WriteLine("1. 私钥文件(mars_private.key)请妥善保管，切勿泄露！");
            Console.WriteLine("2. 私钥用于生成License（仅服务端使用）");
            Console.WriteLine("3. 公钥文件(mars_public.key)用于验证License（可分发给客户端）");
            Console.WriteLine("4. 公钥即使泄露也无法伪造License，可以安全分发");
            Console.WriteLine();

            // 显示公钥内容预览
            Console.WriteLine("公钥内容预览：");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine(publicKey);
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();
        }

        private static void ShowUsage()
        {
            Console.WriteLine("用法：");
            Console.WriteLine("  dotnet run --project LicenseEnCodeAndDecode -- generate-keys");
            Console.WriteLine();
            Console.WriteLine("或者在已编译的程序中：");
            Console.WriteLine("  LicenseEnCodeAndDecode.exe generate-keys");
            Console.WriteLine();
            Console.WriteLine("这将生成：");
            Console.WriteLine("  - mars_public.key  (公钥，用于验证License)");
            Console.WriteLine("  - mars_private.key (私钥，用于生成License)");
            Console.WriteLine();
        }
    }
}
