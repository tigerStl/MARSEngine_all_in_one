# 🔐 MARS Spy Tool License 管理建议方案

## 📋 总体建议

针对 MARS Spy Tool 应用，我建议采用 **混合式 License 管理方案**，结合本地加密文件和可选的在线验证。

---

## 🎯 推荐方案架构

```
┌────────────────────────────────────────────────────────────┐
│                    客户端应用程序                           │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  1. 应用启动 → 验证 License                          │  │
│  │  2. 功能调用 → 检查权限                              │  │
│  │  3. 定期验证 → 防止篡改                              │  │
│  └──────────────────────────────────────────────────────┘  │
│           ↓                          ↓                      │
│  ┌──────────────────┐      ┌──────────────────────┐        │
│  │  本地 License     │      │  硬件绑定             │        │
│  │  加密存储         │      │  防止转移             │        │
│  └──────────────────┘      └──────────────────────┘        │
└────────────────────────────────────────────────────────────┘
                       ↓ (可选)
          ┌────────────────────────────┐
          │   License 验证服务器        │
          │   - 在线激活               │
          │   - 使用统计               │
          │   - 并发控制               │
          └────────────────────────────┘
```

---

## ✅ 方案优势

### 1️⃣ **已实现的功能**

- ✅ **多种 License 类型**：试用版、标准版、专业版、企业版、永久版
- ✅ **硬件绑定**：防止 License 在不同机器间转移
- ✅ **加密存储**：使用 AES 加密保护 License 文件
- ✅ **防篡改**：SHA256 签名验证
- ✅ **功能权限控制**：精细化的功能访问控制
- ✅ **过期检测**：自动检测 License 是否过期
- ✅ **友好界面**：提供 WPF 激活窗口

### 2️⃣ **安全特性**

| 特性 | 实现方式 | 安全级别 |
|------|----------|----------|
| License 加密 | AES-256 | ⭐⭐⭐⭐ |
| 防篡改 | SHA256 签名 | ⭐⭐⭐⭐ |
| 硬件绑定 | CPU ID + 主板 + MAC | ⭐⭐⭐⭐ |
| 密钥保护 | 代码混淆（推荐） | ⭐⭐⭐ |
| 在线验证 | 可选功能 | ⭐⭐⭐⭐⭐ |

---

## 🚀 实施建议

### 阶段 1：基础集成（1-2天）

**✅ 已提供的代码**：
- `MarsLicenseInfo.cs` - License 数据模型
- `MarsLicenseManager.cs` - 核心管理器
- `LicenseKeyGenerator.cs` - Key 生成器
- `LicenseActivationWindow.xaml/cs` - 激活界面

**需要做的**：
1. 在 `App.xaml.cs` 启动时验证 License
2. 在 `MainWindow.xaml` 添加 License 管理菜单
3. 在关键功能处添加权限检查

```csharp
// 示例：在 App.xaml.cs 中
private void Application_Startup(object sender, StartupEventArgs e)
{
    string errorMessage = "";
    if (!MarsLicenseManager.Instance.ValidateLicense(ref errorMessage))
    {
        var licenseWindow = new LicenseActivationWindow();
        licenseWindow.ShowDialog();
        
        if (!MarsLicenseManager.Instance.ValidateLicense(ref errorMessage))
        {
            MessageBox.Show("未找到有效的 License");
            Application.Current.Shutdown();
            return;
        }
    }
    
    // 继续启动应用
    var mainWindow = new MarsObjectSpy();
    mainWindow.Show();
}
```

### 阶段 2：功能权限控制（1天）

在现有功能中添加权限检查：

```csharp
// 录制回放功能
private void Button_PreviewRecordAndReplayButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!MarsLicenseManager.Instance.HasFeature(LicenseFeatures.RecordReplay))
    {
        MessageBox.Show("此功能需要专业版或企业版 License");
        return;
    }
    // ... 原有代码
}

// 自动生成测试用例
private void Button_PreviewTestCaseMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!MarsLicenseManager.Instance.HasFeature(LicenseFeatures.AutoGenerateTestCase))
    {
        MessageBox.Show("此功能需要标准版或更高版本");
        return;
    }
    // ... 原有代码
}
```

### 阶段 3：License 生成工具（0.5天）

创建独立的 License 生成器工具（控制台或 WPF）：

```csharp
// 简单的控制台工具
static void Main(string[] args)
{
    Console.WriteLine("MARS License Key Generator");
    Console.Write("客户名称: ");
    string name = Console.ReadLine();
    
    Console.Write("License 类型 (1=试用/2=标准/3=专业/4=企业/5=永久): ");
    int type = int.Parse(Console.ReadLine());
    
    string key = "";
    switch(type)
    {
        case 1: key = LicenseKeyGenerator.GenerateTrialKey(name); break;
        case 2: key = LicenseKeyGenerator.GenerateStandardKey(name, DateTime.Now.AddYears(1)); break;
        case 3: key = LicenseKeyGenerator.GenerateProfessionalKey(name, DateTime.Now.AddYears(1)); break;
        case 4: key = LicenseKeyGenerator.GenerateEnterpriseKey(name, DateTime.Now.AddYears(1)); break;
        case 5: key = LicenseKeyGenerator.GeneratePerpetualKey(name); break;
    }
    
    Console.WriteLine($"\nLicense Key:\n{key}");
}
```

### 阶段 4：增强安全性（可选，1-2天）

1. **代码混淆**
   - 使用 Dotfuscator 或 ConfuserEx
   - 保护加密密钥和算法

2. **在线验证**
   ```csharp
   public async Task<bool> ValidateOnlineAsync()
   {
       var client = new HttpClient();
       var response = await client.PostAsync(
           "https://license-api.mars.com/validate",
           new StringContent(JsonConvert.SerializeObject(new {
               LicenseKey = CurrentLicense.LicenseKey,
               HardwareId = GetHardwareId()
           })));
       return response.IsSuccessStatusCode;
   }
   ```

3. **反调试检测**
   ```csharp
   if (System.Diagnostics.Debugger.IsAttached)
   {
       throw new Exception("检测到调试器");
   }
   ```

---

## 📊 License 定价建议

| 版本 | 价格建议 | 适用场景 |
|------|----------|----------|
| 试用版 | 免费 | 新用户评估 |
| 标准版 | ¥1,999/年 | 小团队（1-3人） |
| 专业版 | ¥4,999/年 | 中型团队（3-10人） |
| 企业版 | ¥19,999/年 | 大型组织（10+人） |
| 永久版 | ¥29,999 | 长期使用 |

---

## 🔧 运维管理建议

### 1. License 发放流程

```
客户购买 → 销售获取硬件ID → 生成 License Key → 
发送给客户 → 客户激活 → 记录激活信息
```

### 2. License 数据库设计

```sql
CREATE TABLE Licenses (
    Id INT PRIMARY KEY,
    LicenseKey VARCHAR(500),
    CustomerId INT,
    CustomerName VARCHAR(200),
    LicenseType INT,
    CreatedDate DATETIME,
    ExpirationDate DATETIME,
    MaxActivations INT,
    ActivationCount INT,
    Status INT, -- 0=未激活, 1=已激活, 2=已过期, 3=已禁用
    Notes TEXT
);

CREATE TABLE LicenseActivations (
    Id INT PRIMARY KEY,
    LicenseId INT,
    HardwareId VARCHAR(100),
    MachineName VARCHAR(200),
    ActivationDate DATETIME,
    LastValidationDate DATETIME,
    IsActive BIT
);
```

### 3. 续费提醒

在应用中添加续费提醒：

```csharp
private void CheckLicenseExpiration()
{
    var license = MarsLicenseManager.Instance.CurrentLicense;
    if (license.IsExpiringSoon())
    {
        // 剩余 7 天内提醒
        int remainingDays = license.GetRemainingDays();
        MessageBox.Show(
            $"您的 License 将在 {remainingDays} 天后过期。\n\n" +
            "请及时联系销售续费：sales@mars.com",
            "续费提醒",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
```

---

## 🌐 可选的在线验证服务

如果需要更严格的控制，可以搭建 License 服务器：

### 服务器 API 设计

```csharp
// 激活 API
POST /api/license/activate
{
    "licenseKey": "xxxx",
    "hardwareId": "yyyy",
    "machineInfo": {...}
}

// 验证 API
POST /api/license/validate
{
    "licenseKey": "xxxx",
    "hardwareId": "yyyy"
}

// 心跳 API（定期验证）
POST /api/license/heartbeat
{
    "licenseKey": "xxxx",
    "hardwareId": "yyyy",
    "usageData": {...}
}
```

### 客户端集成

```csharp
// 定期验证（每小时）
private System.Timers.Timer _validationTimer;

private void StartLicenseValidation()
{
    _validationTimer = new System.Timers.Timer(3600000); // 1小时
    _validationTimer.Elapsed += async (s, e) =>
    {
        string error = "";
        bool isValid = await MarsLicenseManager.Instance.ValidateOnlineAsync(ref error);
        if (!isValid)
        {
            // 处理验证失败
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("License 验证失败，应用将退出");
                Application.Current.Shutdown();
            });
        }
    };
    _validationTimer.Start();
}
```

---

## 📈 统计分析建议

收集使用数据（需要用户同意）：

```csharp
public class UsageStatistics
{
    public DateTime SessionStart { get; set; }
    public DateTime SessionEnd { get; set; }
    public Dictionary<string, int> FeatureUsageCount { get; set; }
    public string Version { get; set; }
    public string MachineInfo { get; set; }
}

// 上报使用统计
private async Task ReportUsageStatistics()
{
    var stats = new UsageStatistics
    {
        SessionStart = _sessionStart,
        SessionEnd = DateTime.Now,
        FeatureUsageCount = _featureUsageCounter,
        Version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
    };
    
    await _httpClient.PostAsync(
        "https://api.mars.com/usage",
        new StringContent(JsonConvert.SerializeObject(stats)));
}
```

---

## 🛡️ 法律和合规建议

1. **用户许可协议（EULA）**
   - 明确使用条款
   - 禁止逆向工程
   - 限制责任条款

2. **隐私政策**
   - 说明收集的数据
   - 数据使用方式
   - 用户权利

3. **防盗版声明**
   ```
   本软件受版权法和国际条约保护。
   未经授权的复制、分发或使用将受到民事和刑事处罚。
   ```

---

## 📞 客户支持

### 常见问题处理

1. **License 丢失** → 查询数据库重新发送
2. **激活次数用完** → 根据情况增加激活次数
3. **硬件更换** → 重置硬件绑定
4. **过期续费** → 生成新的 License Key

### 支持渠道

- 📧 Email: license@mars.com
- 📞 电话: 400-xxx-xxxx
- 💬 在线客服: https://www.mars.com/support
- 📚 知识库: https://docs.mars.com/license

---

## 🎓 总结

### ✅ 核心优势

1. **灵活性**：支持多种 License 类型
2. **安全性**：多层加密和验证
3. **易用性**：友好的激活界面
4. **可扩展**：支持在线验证和统计
5. **低成本**：无需复杂基础设施

### 🚀 快速开始

1. 复制 `Utility/License/` 文件夹到项目
2. 在 `App.xaml.cs` 中集成验证逻辑
3. 创建独立的 License 生成工具
4. 测试完整流程
5. 发布应用

### 📝 后续改进方向

- [ ] 实现浮动 License（网络并发）
- [ ] 添加 License 转移功能
- [ ] 实现自动续费
- [ ] 集成支付系统
- [ ] 添加使用分析仪表板

---

**祝您的 MARS Spy Tool 销售顺利！** 🎉

