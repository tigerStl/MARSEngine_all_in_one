# 🔐 MARS Spy Tool License 管理系统

## 📖 简介

这是一个为 MARS Spy Tool 设计的完整 License（软件授权）管理系统，提供从 License 生成、激活、验证到权限控制的全套解决方案。

---

## 🎯 核心特性

| 特性 | 说明 | 状态 |
|------|------|------|
| **多种 License 类型** | 试用版、标准版、专业版、企业版、永久版 | ✅ 已实现 |
| **硬件绑定** | 防止 License 在不同机器间转移使用 | ✅ 已实现 |
| **加密存储** | AES-256 加密保护 License 文件 | ✅ 已实现 |
| **防篡改** | SHA256 数字签名验证 | ✅ 已实现 |
| **功能权限控制** | 精细化的功能访问权限管理 | ✅ 已实现 |
| **过期检测** | 自动检测 License 是否过期并提醒 | ✅ 已实现 |
| **友好界面** | WPF 可视化激活窗口 | ✅ 已实现 |
| **在线验证** | 可选的在线激活和验证功能 | 📋 设计完成 |

---

## 📁 文件结构

```
Utility/License/
├── MarsLicenseInfo.cs                      # License 数据模型
├── MarsLicenseManager.cs                   # License 管理核心类
├── LicenseKeyGenerator.cs                  # License Key 生成器
├── LicenseActivationWindow.xaml            # 激活窗口界面
├── LicenseActivationWindow.xaml.cs         # 激活窗口逻辑
├── EXAMPLE_LicenseGenerator.cs             # 生成器示例代码
├── LICENSE_INTEGRATION_GUIDE.md            # 详细集成指南
├── LICENSE_MANAGEMENT_RECOMMENDATIONS.md   # 管理建议方案
├── INTEGRATION_EXAMPLE.txt                 # 快速集成示例
└── README.md                               # 本文档
```

---

## 🚀 快速开始

### 1️⃣ 最简单的集成（5分钟）

#### 步骤 1：在 App.xaml.cs 启动时验证

```csharp
using MarsSpyTool.Utility.License;

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
    
    // 继续启动应用...
}
```

#### 步骤 2：添加功能权限检查

```csharp
// 在功能按钮事件中
private void Button_PreviewRecordAndReplayButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!MarsLicenseManager.Instance.HasFeature(LicenseFeatures.RecordReplay))
    {
        MessageBox.Show("此功能需要专业版或企业版 License");
        return;
    }
    // ... 原有代码
}
```

#### 步骤 3：生成测试 License Key

```csharp
// 使用生成器（独立工具）
string testKey = LicenseKeyGenerator.GenerateTrialKey("测试用户");
Console.WriteLine(testKey);
```

### 2️⃣ 测试流程

1. ▶️ 编译并运行应用
2. 🔑 应该弹出激活窗口
3. 📋 复制生成的 License Key 并粘贴
4. ✅ 点击激活
5. 🎉 激活成功，应用正常运行

---

## 📊 License 类型对比

| 版本 | 有效期 | 功能 | 并发数 | 激活次数 | 适用场景 |
|------|--------|------|--------|----------|----------|
| **试用版** | 30天 | 基础功能 | 1 | 1 | 评估测试 |
| **标准版** | 1年 | 标准功能 | 1 | 1 | 个人开发者 |
| **专业版** | 1年 | 高级功能 | 3 | 3 | 小团队 |
| **企业版** | 1年 | 全部功能 | 10+ | 10+ | 大型组织 |
| **永久版** | 永久 | 全部功能 | 1 | 1 | 长期使用 |

### 功能权限明细

| 功能 | 试用版 | 标准版 | 专业版 | 企业版 | 永久版 |
|------|:------:|:------:|:------:|:------:|:------:|
| 基础对象识别 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 单对象模式 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 自动生成测试用例 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 录制回放 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 多数据库支持 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 高级对象识别 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 批量操作 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 云端同步 | ❌ | ❌ | ❌ | ✅ | ✅ |

---

## 🛠️ API 使用说明

### MarsLicenseManager（核心管理类）

```csharp
// 获取单例
var manager = MarsLicenseManager.Instance;

// 验证 License
string error = "";
bool isValid = manager.ValidateLicense(ref error);

// 激活 License
bool success = manager.ActivateLicense("LICENSE_KEY_HERE", ref error);

// 检查功能权限
bool hasFeature = manager.HasFeature(LicenseFeatures.RecordReplay);

// 获取当前 License 信息
var license = manager.CurrentLicense;
int remainingDays = license.GetRemainingDays();
bool expiringSoon = license.IsExpiringSoon();

// 获取硬件ID（用于 License 绑定）
string hwid = MarsLicenseManager.GetHardwareId();
```

### LicenseKeyGenerator（生成器）

```csharp
// 生成试用版 (30天)
string trialKey = LicenseKeyGenerator.GenerateTrialKey("客户名");

// 生成标准版 (1年)
string standardKey = LicenseKeyGenerator.GenerateStandardKey(
    "客户名", DateTime.Now.AddYears(1), maxActivations: 1);

// 生成专业版 (1年，可激活3次)
string proKey = LicenseKeyGenerator.GenerateProfessionalKey(
    "客户名", DateTime.Now.AddYears(1), maxActivations: 3);

// 生成企业版 (1年，10并发)
string enterpriseKey = LicenseKeyGenerator.GenerateEnterpriseKey(
    "客户名", DateTime.Now.AddYears(1), 
    maxConcurrentUsers: 10, maxActivations: 10);

// 生成永久版
string perpetualKey = LicenseKeyGenerator.GeneratePerpetualKey("客户名");

// 自定义生成
string customKey = LicenseKeyGenerator.GenerateCustomKey(
    licensedTo: "客户名",
    type: LicenseType.Professional,
    expirationDate: DateTime.Now.AddMonths(6),
    features: LicenseFeatures.BasicObjectSpy | LicenseFeatures.AutoGenerateTestCase,
    maxConcurrentUsers: 5,
    maxActivations: 5,
    supportedVersions: "1.0,2.0");
```

---

## 🔐 安全特性

### 1. 加密存储

- **算法**：AES-256
- **存储位置**：`{应用目录}/mars.lic`
- **密钥保护**：建议使用代码混淆工具

### 2. 硬件绑定

绑定以下硬件信息：
- CPU ID
- 主板序列号
- 网卡 MAC 地址

生成唯一的硬件指纹，防止 License 转移。

### 3. 数字签名

使用 SHA256 哈希算法生成签名，防止 License 文件被篡改。

### 4. 防止逆向工程

建议使用：
- **Dotfuscator**（Visual Studio 自带）
- **ConfuserEx**（开源免费）
- **Eziriz .NET Reactor**（商业版）

---

## 📖 详细文档

| 文档 | 说明 |
|------|------|
| 📘 [LICENSE_INTEGRATION_GUIDE.md](LICENSE_INTEGRATION_GUIDE.md) | 详细集成指南（推荐阅读） |
| 📗 [LICENSE_MANAGEMENT_RECOMMENDATIONS.md](LICENSE_MANAGEMENT_RECOMMENDATIONS.md) | 完整的管理建议方案 |
| 📙 [INTEGRATION_EXAMPLE.txt](INTEGRATION_EXAMPLE.txt) | 快速集成示例代码 |
| 📕 [EXAMPLE_LicenseGenerator.cs](EXAMPLE_LicenseGenerator.cs) | License 生成器示例 |

---

## ❓ 常见问题

### Q1: License 文件存储在哪里？
**A:** 默认在应用程序根目录的 `mars.lic` 文件中。

### Q2: 如何更换机器？
**A:** 需要生成新的 License Key，或增加原 License 的最大激活次数。

### Q3: 如何实现浮动 License（网络并发）？
**A:** 需要搭建 License 服务器，客户端启动时请求授权，关闭时释放。详见 [LICENSE_MANAGEMENT_RECOMMENDATIONS.md](LICENSE_MANAGEMENT_RECOMMENDATIONS.md)

### Q4: License 可以被破解吗？
**A:** 没有绝对安全的方案，但通过以下措施可以大大提高破解难度：
- 代码混淆
- 在线验证
- 定期心跳检测
- 关键算法服务器端执行

### Q5: 如何批量生成 License？
**A:** 使用 `LicenseKeyGenerator.GenerateBatchKeys()` 方法，详见 [EXAMPLE_LicenseGenerator.cs](EXAMPLE_LicenseGenerator.cs)

### Q6: 支持自动续费吗？
**A:** 当前版本不支持，可以扩展实现：
- 集成支付接口
- 添加自动续费 API
- 在 License 管理服务器中实现

---

## 🛣️ 后续改进方向

- [ ] **浮动 License**：支持网络并发控制
- [ ] **在线验证**：定期向服务器验证 License
- [ ] **使用统计**：收集功能使用数据
- [ ] **自动续费**：集成支付系统
- [ ] **License 转移**：支持合法转移到新机器
- [ ] **仪表板**：License 管理后台
- [ ] **API 集成**：RESTful API 供第三方系统调用

---

## 📞 技术支持

如有问题或建议，请联系：

- 📧 **Email**: license@mars.com
- 📞 **电话**: 400-xxx-xxxx
- 💬 **在线客服**: https://www.mars.com/support
- 📚 **文档中心**: https://docs.mars.com/license

---

## 📝 更新日志

### v1.0.0 (2025-01-09)

#### ✨ 新功能
- ✅ 实现 License 核心管理功能
- ✅ 支持 5 种 License 类型
- ✅ 硬件绑定功能
- ✅ AES-256 加密存储
- ✅ SHA256 数字签名
- ✅ 功能权限控制
- ✅ WPF 激活窗口
- ✅ License Key 生成器

#### 📖 文档
- ✅ 集成指南
- ✅ 管理建议
- ✅ 示例代码
- ✅ API 文档

---

## 📄 许可证

本 License 管理系统是 MARS Spy Tool 的一部分，版权归 MARS Team 所有。

---

## 🙏 致谢

感谢所有为 MARS Spy Tool 做出贡献的开发者和用户！

---

**祝您使用愉快！** 🎉

