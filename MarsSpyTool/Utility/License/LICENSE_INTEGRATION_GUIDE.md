# MARS Spy Tool License 管理系统集成指南

## 📖 概述

这是一个完整的软件授权管理系统，提供以下功能：
- ✅ 多种 License 类型（试用版、标准版、专业版、企业版、永久版）
- ✅ 硬件绑定（防止 License 转移）
- ✅ 功能权限控制
- ✅ 过期检测
- ✅ 加密存储
- ✅ 防篡改签名验证

---

## 🏗️ 架构设计

```
┌─────────────────────────────────────────────┐
│         应用程序启动                         │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   MarsLicenseManager.Instance.ValidateLicense()   │
│   验证 License 是否有效                      │
└──────────────┬──────────────────────────────┘
               │
       ┌───────┴───────┐
       │ Valid?        │
       └───┬───────┬───┘
           │       │
         Yes       No
           │       │
           ▼       ▼
    ┌─────────┐ ┌──────────────────┐
    │ 正常运行 │ │ 显示激活窗口      │
    └─────────┘ │ 或功能受限模式    │
                └──────────────────┘
```

---

## 📦 文件说明

| 文件 | 说明 |
|------|------|
| `MarsLicenseInfo.cs` | License 信息模型类 |
| `MarsLicenseManager.cs` | License 管理核心类（单例） |
| `LicenseKeyGenerator.cs` | License Key 生成器（管理员工具） |
| `LicenseActivationWindow.xaml/cs` | License 激活窗口 |
| `LICENSE_INTEGRATION_GUIDE.md` | 本文档 |

---

## 🚀 快速集成

### 1️⃣ 在应用启动时验证 License

在 `App.xaml.cs` 的 `Application_Startup` 方法中添加：

```csharp
using MarsSpyTool.Utility.License;

private void Application_Startup(object sender, StartupEventArgs e)
{
    // ... 现有代码 ...
    
    // 验证 License
    string errorMessage = "";
    MarsLicenseManager licenseManager = MarsLicenseManager.Instance;
    
    if (!licenseManager.ValidateLicense(ref errorMessage))
    {
        logger.Warn($"License validation failed: {errorMessage}");
        
        // 显示激活窗口
        LicenseActivationWindow licenseWindow = new LicenseActivationWindow();
        bool? result = licenseWindow.ShowDialog();
        
        // 如果用户关闭窗口且仍未激活，则退出程序
        if (!licenseManager.ValidateLicense(ref errorMessage))
        {
            MessageBox.Show(
                "未检测到有效的 License，程序将退出。\n\n" +
                "请联系销售获取 License Key。",
                "License 验证失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }
    }
    else
    {
        // License 有效，检查是否即将过期
        var license = licenseManager.CurrentLicense;
        if (license.IsExpiringSoon())
        {
            MessageBox.Show(
                $"您的 License 即将在 {license.GetRemainingDays()} 天后过期。\n\n" +
                "请及时续费以确保正常使用。",
                "License 即将过期",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        
        logger.Info($"License valid. Type: {license.Type}, Remaining days: {license.GetRemainingDays()}");
    }
    
    // ... 现有代码继续 ...
    MarsObjectSpy wnd = new MarsObjectSpy();
    wnd.Show();
}
```

### 2️⃣ 在 MainWindow 添加 License 管理菜单

在 `MainWindow.xaml` 中添加菜单项：

```xml
<Window.Resources>
    <!-- ... -->
</Window.Resources>

<DockPanel>
    <!-- 添加菜单栏 -->
    <Menu DockPanel.Dock="Top">
        <MenuItem Header="帮助(_H)">
            <MenuItem Header="License 激活..." Click="ShowLicenseActivation_Click"/>
            <MenuItem Header="License 信息..." Click="ShowLicenseInfo_Click"/>
            <Separator/>
            <MenuItem Header="关于..." Click="ShowAbout_Click"/>
        </MenuItem>
    </Menu>
    
    <!-- 原有内容 -->
    <Grid>
        <!-- ... -->
    </Grid>
</DockPanel>
```

在 `MainWindow.xaml.cs` 中添加事件处理：

```csharp
using MarsSpyTool.Utility.License;

private void ShowLicenseActivation_Click(object sender, RoutedEventArgs e)
{
    LicenseActivationWindow licenseWindow = new LicenseActivationWindow();
    licenseWindow.ShowDialog();
}

private void ShowLicenseInfo_Click(object sender, RoutedEventArgs e)
{
    var license = MarsLicenseManager.Instance.CurrentLicense;
    if (license != null && license.IsValid())
    {
        string features = GetFeaturesList(license.Features);
        MessageBox.Show(
            $"License 信息\n\n" +
            $"授权给: {license.LicensedTo}\n" +
            $"类型: {license.Type}\n" +
            $"激活日期: {license.ActivationDate:yyyy-MM-dd}\n" +
            $"过期日期: {(license.Type == LicenseType.Perpetual ? "永久" : license.ExpirationDate.ToString("yyyy-MM-dd"))}\n" +
            $"剩余天数: {(license.Type == LicenseType.Perpetual ? "永久" : license.GetRemainingDays().ToString())}\n" +
            $"功能权限:\n{features}",
            "License 信息",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    else
    {
        MessageBox.Show("未找到有效的 License", "提示", 
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

private string GetFeaturesList(LicenseFeatures features)
{
    var list = new List<string>();
    if ((features & LicenseFeatures.BasicObjectSpy) != 0)
        list.Add("- 基础对象识别");
    if ((features & LicenseFeatures.SingleObjectMode) != 0)
        list.Add("- 单对象模式");
    if ((features & LicenseFeatures.AutoGenerateTestCase) != 0)
        list.Add("- 自动生成测试用例");
    if ((features & LicenseFeatures.RecordReplay) != 0)
        list.Add("- 录制回放");
    if ((features & LicenseFeatures.MultiDatabase) != 0)
        list.Add("- 多数据库支持");
    if ((features & LicenseFeatures.AdvancedObjectRecognition) != 0)
        list.Add("- 高级对象识别");
    return string.Join("\n", list);
}
```

### 3️⃣ 功能级别权限控制

在需要权限控制的功能处添加检查：

```csharp
// 示例：检查录制回放功能权限
private void Button_PreviewRecordAndReplayButtonDown(object sender, MouseButtonEventArgs e)
{
    // 检查 License 权限
    if (!MarsLicenseManager.Instance.HasFeature(LicenseFeatures.RecordReplay))
    {
        MessageBox.Show(
            "您的 License 不包含录制回放功能。\n\n" +
            "请升级到专业版或企业版以使用此功能。",
            "功能受限",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }
    
    // ... 原有代码 ...
}

// 示例：自动生成测试用例
private void Button_PreviewTestCaseMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!MarsLicenseManager.Instance.HasFeature(LicenseFeatures.AutoGenerateTestCase))
    {
        MessageBox.Show(
            "您的 License 不包含自动生成测试用例功能。\n\n" +
            "请升级到标准版或更高版本。",
            "功能受限",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }
    
    // ... 原有代码 ...
}
```

---

## 🔧 License Key 生成

### 创建独立的 License 生成工具

建议创建一个独立的 WPF 或控制台应用程序用于生成 License Key，**不要将生成逻辑包含在客户端程序中**。

```csharp
// 示例：生成不同类型的 License Key
using MarsSpyTool.Utility.License;

// 1. 生成试用版 (30天)
string trialKey = LicenseKeyGenerator.GenerateTrialKey("张三公司");

// 2. 生成标准版 (1年)
string standardKey = LicenseKeyGenerator.GenerateStandardKey(
    "李四公司", 
    DateTime.Now.AddYears(1), 
    maxActivations: 1);

// 3. 生成专业版 (1年，可激活3次)
string professionalKey = LicenseKeyGenerator.GenerateProfessionalKey(
    "王五公司", 
    DateTime.Now.AddYears(1), 
    maxActivations: 3);

// 4. 生成企业版 (1年，10并发，10次激活)
string enterpriseKey = LicenseKeyGenerator.GenerateEnterpriseKey(
    "赵六公司", 
    DateTime.Now.AddYears(1), 
    maxConcurrentUsers: 10, 
    maxActivations: 10);

// 5. 生成永久版
string perpetualKey = LicenseKeyGenerator.GeneratePerpetualKey(
    "钱七公司", 
    maxActivations: 1);

// 6. 生成自定义 License
string customKey = LicenseKeyGenerator.GenerateCustomKey(
    licensedTo: "孙八公司",
    type: LicenseType.Professional,
    expirationDate: DateTime.Now.AddMonths(6),
    features: LicenseFeatures.BasicObjectSpy 
        | LicenseFeatures.AutoGenerateTestCase 
        | LicenseFeatures.RecordReplay,
    maxConcurrentUsers: 5,
    maxActivations: 5,
    supportedVersions: "1.0,2.0");

Console.WriteLine($"Trial Key: {trialKey}");
```

---

## 🔐 安全建议

### 1. **加密密钥管理**

当前代码中使用的加密密钥是硬编码的：
```csharp
private const string ENCRYPTION_KEY = "MARS_SPY_TOOL_2025_ENCRYPTION_KEY_V1";
```

**生产环境建议**：
- 使用更复杂的随机生成密钥
- 使用代码混淆工具保护密钥
- 考虑使用 RSA 非对称加密

### 2. **License Key 格式**

当前使用 Base64 编码的 JSON 格式。**更安全的方案**：
- 使用 RSA 公钥加密 License 信息
- 客户端只保留公钥，无法生成 License
- 服务端使用私钥生成 License

### 3. **在线验证（可选）**

对于 SaaS 模式，建议添加在线验证：
```csharp
public bool ValidateLicenseOnline(ref string errorMessage)
{
    try
    {
        // 向服务器发送验证请求
        var client = new HttpClient();
        var response = await client.PostAsync(
            "https://your-license-server.com/api/validate",
            new StringContent(JsonConvert.SerializeObject(new
            {
                LicenseKey = _currentLicense.LicenseKey,
                HardwareId = GetHardwareId()
            })));
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            // 处理验证结果
            return true;
        }
        return false;
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        return false;
    }
}
```

### 4. **防调试措施**

添加反调试和完整性检查：
```csharp
public static bool IsDebuggerAttached()
{
    return System.Diagnostics.Debugger.IsAttached;
}

// 在关键代码处检查
if (IsDebuggerAttached())
{
    throw new Exception("检测到调试器");
}
```

---

## 📊 License 类型对比

| 功能 | 试用版 | 标准版 | 专业版 | 企业版 | 永久版 |
|------|--------|--------|--------|--------|--------|
| 有效期 | 30天 | 1年 | 1年 | 1年 | 永久 |
| 基础对象识别 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 单对象模式 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 自动生成测试用例 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 录制回放 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 多数据库支持 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 高级对象识别 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 批量操作 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 云端同步 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 并发用户数 | 1 | 1 | 3 | 10+ | 1 |
| 激活次数 | 1 | 1 | 3 | 10+ | 1 |

---

## 🐛 常见问题

### Q: License 文件存储在哪里？
A: 默认存储在应用程序根目录的 `mars.lic` 文件中。

### Q: 如何更换机器？
A: 需要联系管理员生成新的 License Key，或增加最大激活次数。

### Q: 如何批量生成 License？
A: 使用 `LicenseKeyGenerator.GenerateBatchKeys()` 方法。

### Q: 如何实现浮动 License（网络并发）？
A: 需要搭建 License 服务器，客户端启动时向服务器请求授权，关闭时释放。

---

## 📞 技术支持

如有问题，请联系：
- Email: support@marstest.com
- 技术支持热线: 400-xxx-xxxx

---

## 📝 更新日志

### v1.0.0 (2025-01-09)
- ✅ 初始版本
- ✅ 支持5种 License 类型
- ✅ 硬件绑定功能
- ✅ 加密存储和签名验证
- ✅ License 激活窗口

