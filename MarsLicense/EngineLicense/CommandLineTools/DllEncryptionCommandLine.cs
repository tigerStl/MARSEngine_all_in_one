using System;
using System.IO;
using MarsLicenseManager.Services;

namespace MarsLicenseManager.CommandLineTools
{
    /// <summary>
    /// DLL加密命令行工具
    /// </summary>
    public static class DllEncryptionCommandLine
    {
        /// <summary>
        /// 处理命令行参数
        /// </summary>
        /// <param name="args">命令行参数</param>
        public static void ProcessCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "encrypt-dll":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Error: Missing required parameters for encrypt-dll command.");
                        ShowUsage();
                        return;
                    }
                    EncryptDllCommand(args[1], args[2]);
                    break;

                case "decrypt-dll":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Error: Missing required parameters for decrypt-dll command.");
                        ShowUsage();
                        return;
                    }
                    DecryptDllCommand(args[1], args[2]);
                    break;

                case "decrypt-to-stream":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Missing required parameters for decrypt-to-stream command.");
                        ShowUsage();
                        return;
                    }
                    DecryptToStreamCommand(args[1]);
                    break;

                case "help":
                case "-h":
                case "--help":
                    ShowUsage();
                    break;

                default:
                    Console.WriteLine($"Error: Unknown command '{command}'.");
                    ShowUsage();
                    break;
            }
        }

        /// <summary>
        /// 加密DLL命令
        /// </summary>
        /// <param name="dllPath">DLL文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        private static void EncryptDllCommand(string dllPath, string outputPath)
        {
            try
            {
                Console.WriteLine("DLL Encryption Tool");
                Console.WriteLine("==================");
                Console.WriteLine();

                // 验证输入文件
                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"Error: DLL file not found: {dllPath}");
                    return;
                }

                Console.WriteLine($"Input DLL: {dllPath}");
                Console.WriteLine($"Output file: {outputPath}");
                Console.WriteLine($"Encryption method: AES with fixed key 'MARsAutomationTest'");
                Console.WriteLine();

                // 创建加密服务
                var dllEncryptionService = new DllEncryptionService();

                // 显示文件信息
                FileInfo dllInfo = new FileInfo(dllPath);
                Console.WriteLine($"Original file size: {dllInfo.Length:N0} bytes");

                // 执行加密
                Console.WriteLine("Encrypting DLL file...");
                bool success = dllEncryptionService.EncryptDllFile(dllPath, outputPath);

                if (success)
                {
                    FileInfo outputInfo = new FileInfo(outputPath);
                    Console.WriteLine($"✓ Encryption completed successfully!");
                    Console.WriteLine($"  Output file size: {outputInfo.Length:N0} bytes");
                    Console.WriteLine($"  Output file: {outputPath}");
                }
                else
                {
                    Console.WriteLine("✗ Encryption failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 解密DLL命令
        /// </summary>
        /// <param name="encryptedPath">加密文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        private static void DecryptDllCommand(string encryptedPath, string outputPath)
        {
            try
            {
                Console.WriteLine("DLL Decryption Tool");
                Console.WriteLine("==================");
                Console.WriteLine();

                // 验证输入文件
                if (!File.Exists(encryptedPath))
                {
                    Console.WriteLine($"Error: Encrypted file not found: {encryptedPath}");
                    return;
                }

                Console.WriteLine($"Input encrypted file: {encryptedPath}");
                Console.WriteLine($"Output DLL: {outputPath}");
                Console.WriteLine($"Decryption method: AES with fixed key 'MARsAutomationTest'");
                Console.WriteLine();

                // 创建解密服务
                var dllDecryptionService = new DllEncryptionService();

                // 显示文件信息
                FileInfo encryptedInfo = new FileInfo(encryptedPath);
                Console.WriteLine($"Encrypted file size: {encryptedInfo.Length:N0} bytes");

                // 执行解密
                Console.WriteLine("Decrypting DLL file...");
                bool success = dllDecryptionService.DecryptDllFile(encryptedPath, outputPath);

                if (success)
                {
                    FileInfo outputInfo = new FileInfo(outputPath);
                    Console.WriteLine($"✓ Decryption completed successfully!");
                    Console.WriteLine($"  Output file size: {outputInfo.Length:N0} bytes");
                    Console.WriteLine($"  Output file: {outputPath}");
                }
                else
                {
                    Console.WriteLine("✗ Decryption failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 解密到流命令
        /// </summary>
        /// <param name="encryptedPath">加密文件路径</param>
        private static void DecryptToStreamCommand(string encryptedPath)
        {
            try
            {
                Console.WriteLine("DLL Decryption to Stream Tool");
                Console.WriteLine("=============================");
                Console.WriteLine();

                // 验证输入文件
                if (!File.Exists(encryptedPath))
                {
                    Console.WriteLine($"Error: Encrypted file not found: {encryptedPath}");
                    return;
                }

                Console.WriteLine($"Input encrypted file: {encryptedPath}");
                Console.WriteLine($"Decryption method: AES with fixed key 'MARsAutomationTest'");
                Console.WriteLine();

                // 创建解密服务
                var dllDecryptionService = new DllEncryptionService();

                // 显示文件信息
                FileInfo encryptedInfo = new FileInfo(encryptedPath);
                Console.WriteLine($"Encrypted file size: {encryptedInfo.Length:N0} bytes");

                // 执行解密到流
                Console.WriteLine("Decrypting DLL to stream...");
                byte[] decryptedDllBytes = dllDecryptionService.DecryptDllToStream(encryptedPath);

                Console.WriteLine($"✓ Decryption to stream completed successfully!");
                Console.WriteLine($"  Decrypted DLL size: {decryptedDllBytes.Length:N0} bytes");
                Console.WriteLine($"  The decrypted DLL data is ready for use in memory.");
                Console.WriteLine();
                Console.WriteLine("Note: The decrypted DLL data is not saved to disk. Use this for in-memory operations.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示使用说明
        /// </summary>
        private static void ShowUsage()
        {
            Console.WriteLine("MARS DLL Encryption/Decryption Command Line Tool");
            Console.WriteLine("================================================");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  MarsLicenseManager.exe <command> [parameters]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  encrypt-dll <dll_path> <output_path>");
            Console.WriteLine("    Encrypt a DLL file and save to output path");
            Console.WriteLine("    Example: MarsLicenseManager.exe encrypt-dll MyLibrary.dll MyLibrary.marsbin");
            Console.WriteLine();
            Console.WriteLine("  decrypt-dll <encrypted_path> <output_path>");
            Console.WriteLine("    Decrypt an encrypted file and save as DLL");
            Console.WriteLine("    Example: MarsLicenseManager.exe decrypt-dll MyLibrary.marsbin MyLibrary_decrypted.dll");
            Console.WriteLine();
            Console.WriteLine("  decrypt-to-stream <encrypted_path>");
            Console.WriteLine("    Decrypt an encrypted file to memory stream (no file output)");
            Console.WriteLine("    Example: MarsLicenseManager.exe decrypt-to-stream MyLibrary.marsbin");
            Console.WriteLine();
            Console.WriteLine("  help, -h, --help");
            Console.WriteLine("    Show this help message");
            Console.WriteLine();
            Console.WriteLine("Encryption Details:");
            Console.WriteLine("  - Method: AES-256 encryption");
            Console.WriteLine("  - Key: 'MARsAutomationTest' (fixed)");
            Console.WriteLine("  - Padding: 888 random chars prefix + encrypted data + 520 random chars suffix");
            Console.WriteLine();
        }
    }
}