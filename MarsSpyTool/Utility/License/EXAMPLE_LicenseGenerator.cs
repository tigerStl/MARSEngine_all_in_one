/*
 * MARS License Key 生成器示例
 * 
 * 注意：这个文件仅供参考，实际使用时应该创建一个独立的工具项目
 * 不要将此文件包含在客户端发布版本中！
 * 
 * 使用方法：
 * 1. 创建一个新的控制台项目
 * 2. 引用必要的 License 类
 * 3. 运行生成工具
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MarsSpyTool.Utility.License;

namespace MarsLicenseGeneratorTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("    MARS Spy Tool - License Key 生成器 v1.0");
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine();

            while (true)
            {
                ShowMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        GenerateTrial();
                        break;
                    case "2":
                        GenerateStandard();
                        break;
                    case "3":
                        GenerateProfessional();
                        break;
                    case "4":
                        GenerateEnterprise();
                        break;
                    case "5":
                        GeneratePerpetual();
                        break;
                    case "6":
                        GenerateCustom();
                        break;
                    case "7":
                        GenerateBatch();
                        break;
                    case "8":
                        ShowHardwareId();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("❌ 无效选项，请重新选择。");
                        break;
                }

                Console.WriteLine("\n按任意键继续...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        static void ShowMenu()
        {
            Console.WriteLine("请选择要生成的 License 类型：");
            Console.WriteLine();
            Console.WriteLine("  1. 试用版 (30天)");
            Console.WriteLine("  2. 标准版 (自定义时长)");
            Console.WriteLine("  3. 专业版 (自定义时长)");
            Console.WriteLine("  4. 企业版 (自定义时长)");
            Console.WriteLine("  5. 永久版");
            Console.WriteLine("  6. 自定义 License");
            Console.WriteLine("  7. 批量生成");
            Console.WriteLine("  8. 查看机器硬件ID");
            Console.WriteLine("  0. 退出");
            Console.WriteLine();
            Console.Write("请输入选项 (0-8): ");
        }

        static void GenerateTrial()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成试用版 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.Write("试用天数 [默认30]: ");
            string daysInput = Console.ReadLine();
            int trialDays = string.IsNullOrWhiteSpace(daysInput) ? 30 : int.Parse(daysInput);

            string licenseKey = LicenseKeyGenerator.GenerateTrialKey(licensedTo, trialDays);

            DisplayLicenseKey(licenseKey, "试用版", licensedTo, DateTime.Now.AddDays(trialDays));
        }

        static void GenerateStandard()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成标准版 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.Write("有效期（年）[默认1]: ");
            string yearsInput = Console.ReadLine();
            int years = string.IsNullOrWhiteSpace(yearsInput) ? 1 : int.Parse(yearsInput);
            DateTime expirationDate = DateTime.Now.AddYears(years);

            Console.Write("最大激活次数 [默认1]: ");
            string activationsInput = Console.ReadLine();
            int maxActivations = string.IsNullOrWhiteSpace(activationsInput) ? 1 : int.Parse(activationsInput);

            string licenseKey = LicenseKeyGenerator.GenerateStandardKey(licensedTo, expirationDate, maxActivations);

            DisplayLicenseKey(licenseKey, "标准版", licensedTo, expirationDate);
        }

        static void GenerateProfessional()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成专业版 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.Write("有效期（年）[默认1]: ");
            string yearsInput = Console.ReadLine();
            int years = string.IsNullOrWhiteSpace(yearsInput) ? 1 : int.Parse(yearsInput);
            DateTime expirationDate = DateTime.Now.AddYears(years);

            Console.Write("最大激活次数 [默认3]: ");
            string activationsInput = Console.ReadLine();
            int maxActivations = string.IsNullOrWhiteSpace(activationsInput) ? 3 : int.Parse(activationsInput);

            string licenseKey = LicenseKeyGenerator.GenerateProfessionalKey(licensedTo, expirationDate, maxActivations);

            DisplayLicenseKey(licenseKey, "专业版", licensedTo, expirationDate);
        }

        static void GenerateEnterprise()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成企业版 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.Write("有效期（年）[默认1]: ");
            string yearsInput = Console.ReadLine();
            int years = string.IsNullOrWhiteSpace(yearsInput) ? 1 : int.Parse(yearsInput);
            DateTime expirationDate = DateTime.Now.AddYears(years);

            Console.Write("最大并发用户数 [默认10]: ");
            string concurrentInput = Console.ReadLine();
            int maxConcurrentUsers = string.IsNullOrWhiteSpace(concurrentInput) ? 10 : int.Parse(concurrentInput);

            Console.Write("最大激活次数 [默认10]: ");
            string activationsInput = Console.ReadLine();
            int maxActivations = string.IsNullOrWhiteSpace(activationsInput) ? 10 : int.Parse(activationsInput);

            string licenseKey = LicenseKeyGenerator.GenerateEnterpriseKey(
                licensedTo, expirationDate, maxConcurrentUsers, maxActivations);

            DisplayLicenseKey(licenseKey, "企业版", licensedTo, expirationDate);
        }

        static void GeneratePerpetual()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成永久版 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.Write("最大激活次数 [默认1]: ");
            string activationsInput = Console.ReadLine();
            int maxActivations = string.IsNullOrWhiteSpace(activationsInput) ? 1 : int.Parse(activationsInput);

            string licenseKey = LicenseKeyGenerator.GeneratePerpetualKey(licensedTo, maxActivations);

            DisplayLicenseKey(licenseKey, "永久版", licensedTo, DateTime.MaxValue);
        }

        static void GenerateCustom()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("生成自定义 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("授权给（公司/个人名称）: ");
            string licensedTo = Console.ReadLine();

            Console.WriteLine("\n选择 License 类型:");
            Console.WriteLine("  0. 试用版");
            Console.WriteLine("  1. 标准版");
            Console.WriteLine("  2. 专业版");
            Console.WriteLine("  3. 企业版");
            Console.WriteLine("  99. 永久版");
            Console.Write("请选择: ");
            int typeValue = int.Parse(Console.ReadLine());
            LicenseType type = (LicenseType)typeValue;

            DateTime expirationDate;
            if (type == LicenseType.Perpetual)
            {
                expirationDate = DateTime.MaxValue;
            }
            else
            {
                Console.Write("有效期（天数）: ");
                int days = int.Parse(Console.ReadLine());
                expirationDate = DateTime.Now.AddDays(days);
            }

            Console.WriteLine("\n选择功能权限（用空格分隔，例如: 1 2 4）:");
            Console.WriteLine("  1. 基础对象识别");
            Console.WriteLine("  2. 单对象模式");
            Console.WriteLine("  4. 自动生成测试用例");
            Console.WriteLine("  8. 录制回放");
            Console.WriteLine("  16. 多数据库支持");
            Console.WriteLine("  32. 高级对象识别");
            Console.WriteLine("  64. 批量操作");
            Console.WriteLine("  128. 云端同步");
            Console.Write("请选择: ");
            string[] featureValues = Console.ReadLine().Split(' ');
            LicenseFeatures features = LicenseFeatures.None;
            foreach (var val in featureValues)
            {
                if (int.TryParse(val, out int featureValue))
                {
                    features |= (LicenseFeatures)featureValue;
                }
            }

            Console.Write("最大并发用户数: ");
            int maxConcurrentUsers = int.Parse(Console.ReadLine());

            Console.Write("最大激活次数: ");
            int maxActivations = int.Parse(Console.ReadLine());

            Console.Write("支持的版本（逗号分隔，如: 1.0,2.0）: ");
            string supportedVersions = Console.ReadLine();

            string licenseKey = LicenseKeyGenerator.GenerateCustomKey(
                licensedTo, type, expirationDate, features, 
                maxConcurrentUsers, maxActivations, supportedVersions);

            DisplayLicenseKey(licenseKey, "自定义", licensedTo, expirationDate);
        }

        static void GenerateBatch()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("批量生成 License");
            Console.WriteLine("───────────────────────────────────────");

            Console.Write("输入客户名称列表（用逗号分隔）: ");
            string input = Console.ReadLine();
            var licensedToList = input.Split(',').Select(s => s.Trim()).ToList();

            Console.WriteLine("\n选择 License 类型:");
            Console.WriteLine("  0. 试用版");
            Console.WriteLine("  1. 标准版");
            Console.WriteLine("  2. 专业版");
            Console.WriteLine("  3. 企业版");
            Console.WriteLine("  99. 永久版");
            Console.Write("请选择: ");
            int typeValue = int.Parse(Console.ReadLine());
            LicenseType type = (LicenseType)typeValue;

            DateTime expirationDate = DateTime.Now.AddYears(1);
            if (type != LicenseType.Perpetual)
            {
                Console.Write("有效期（年）: ");
                int years = int.Parse(Console.ReadLine());
                expirationDate = DateTime.Now.AddYears(years);
            }

            var results = LicenseKeyGenerator.GenerateBatchKeys(licensedToList, type, expirationDate);

            Console.WriteLine("\n✅ 批量生成完成！\n");
            Console.WriteLine("═══════════════════════════════════════════════════");
            foreach (var kvp in results)
            {
                Console.WriteLine($"\n客户: {kvp.Key}");
                Console.WriteLine($"License Key:\n{kvp.Value}");
                Console.WriteLine("───────────────────────────────────────");
            }

            Console.Write("\n是否保存到文件? (Y/N): ");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                string fileName = $"LicenseKeys_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                System.IO.File.WriteAllLines(fileName, 
                    results.Select(kvp => $"{kvp.Key}\t{kvp.Value}"));
                Console.WriteLine($"✅ 已保存到: {fileName}");
            }
        }

        static void ShowHardwareId()
        {
            Console.WriteLine("\n───────────────────────────────────────");
            Console.WriteLine("当前机器硬件ID");
            Console.WriteLine("───────────────────────────────────────");
            
            string hardwareId = MarsLicenseManager.GetHardwareId();
            Console.WriteLine($"\n硬件ID: {hardwareId}");
            Console.WriteLine("\n说明：");
            Console.WriteLine("- 硬件ID 用于绑定 License 到特定机器");
            Console.WriteLine("- 更换硬件后硬件ID会改变，需要重新激活");
            Console.WriteLine("- 客户可在激活窗口中查看自己的硬件ID");
        }

        static void DisplayLicenseKey(string licenseKey, string type, string licensedTo, DateTime expirationDate)
        {
            Console.WriteLine("\n✅ License Key 生成成功！\n");
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine($"License 类型: {type}");
            Console.WriteLine($"授权给: {licensedTo}");
            Console.WriteLine($"过期日期: {(expirationDate == DateTime.MaxValue ? "永久" : expirationDate.ToString("yyyy-MM-dd"))}");
            Console.WriteLine("───────────────────────────────────────────────────");
            Console.WriteLine("License Key:");
            Console.WriteLine(licenseKey);
            Console.WriteLine("═══════════════════════════════════════════════════");
            
            Console.Write("\n是否复制到剪贴板? (Y/N): ");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetText(licenseKey);
                    Console.WriteLine("✅ 已复制到剪贴板");
                }
                catch
                {
                    Console.WriteLine("❌ 复制失败，请手动复制");
                }
            }
        }
    }
}

