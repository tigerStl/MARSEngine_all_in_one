# 🚀 将 License 嵌入编译文件 - 5分钟快速指南

## 📖 概述

这是最简单的方法，让您在 **5分钟内** 完成 License 的嵌入。

---

## ✅ 方法1: 嵌入资源（最推荐，最简单）

### 步骤1: 准备 License 文件（2分钟）

```bash
# 1. 运行您的应用程序
MarsSpyTool.exe

# 2. 使用 License 生成器生成一个 Key
# (参考 EXAMPLE_LicenseGenerator.cs)

# 3. 在应用中激活该 Key
# 这会在应用目录生成 mars.lic 文件

# 4. 复制该文件
copy mars.lic Resources\embedded_license.lic
```

### 步骤2: 添加到项目（1分钟）

在 Visual Studio 中：

1. 右键点击项目 → **Add** → **Existing Item**
2. 选择 `Resources\embedded_license.lic`
3. 在 **Properties** 窗口中设置：
   - **Build Action**: `Embedded Resource` ⚠️ **重要！**
   - **Copy to Output Directory**: `Do not copy`

或者直接编辑 `MarsSpyTool.csproj`：

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\embedded_license.lic" />
</ItemGroup>
```

### 步骤3: 修改加载代码（2分钟）

在 `Utility/License/MarsLicenseManager.cs` 的 `LoadLicense()` 方法开头添加：

```csharp
private void LoadLicense()
{
    logger.Info("LoadLicense\tBegin");
    try
    {
        // ========== 添加以下代码 ==========
        // 优先从嵌入资源加载
        var embeddedLicense = EmbeddedLicenseManager.LoadFromEmbeddedResource(
            "MarsSpyTool.Resources.embedded_license.lic");
        
        if (embeddedLicense != null && embeddedLicense.IsValid())
        {
            logger.Info("LoadLicense\tLoaded from embedded resource");
            _currentLicense = embeddedLicense;
            return;
        }
        // ========== 添加结束 ==========

        // 原有代码继续...
        if (!File.Exists(_licenseFilePath))
        {
            // ...
        }
```

### 步骤4: 编译测试

```bash
# 编译
msbuild MarsSpyTool.csproj /p:Configuration=Release

# 测试
cd bin\Release
MarsSpyTool.exe

# 应该自动使用嵌入的 License，无需手动激活
```

---

## ✅ 完成！

现在您的程序已经包含了嵌入的 License！

### ✨ 验证是否成功

1. 删除 `bin\Release\mars.lic` 文件（如果有）
2. 运行 `MarsSpyTool.exe`
3. 检查是否：
   - ✅ 不需要激活即可使用
   - ✅ 功能权限正确
   - ✅ 显示正确的 License 类型

---

## 🎯 不同场景的使用方法

### 场景1: 为单个客户创建定制版本

```powershell
# 1. 生成该客户的 License
LicenseGenerator.exe --customer "ABC公司" --type Enterprise --years 1

# 2. 复制生成的 mars.lic
copy mars.lic Resources\embedded_license.lic

# 3. 编译
msbuild /p:Configuration=Release

# 4. 打包发送给客户
```

### 场景2: 批量生成多个客户版本

使用提供的 PowerShell 脚本：

```powershell
# 为客户 A 构建
.\build_with_embedded_license.ps1 -CustomerName "客户A" -LicenseType "Professional"

# 为客户 B 构建
.\build_with_embedded_license.ps1 -CustomerName "客户B" -LicenseType "Enterprise"

# 输出在 Releases\ 目录
```

### 场景3: 预装试用版

```csharp
// 使用试用版 License
string trialKey = LicenseKeyGenerator.GenerateTrialKey("试用用户", 30);
// 激活并保存为 embedded_license.lic
// 编译后分发
// 用户可以使用 30 天，之后需要购买正式版
```

---

## 🔐 安全性说明

### ⚠️ 注意事项

1. **嵌入的 License 可以被提取**
   - 任何人都可以从 exe 文件中提取嵌入的资源
   - 建议配合代码混淆使用

2. **不要在嵌入 License 中绑定硬件ID**
   - 编译时无法知道目标机器的硬件ID
   - 硬件绑定应该在首次运行时进行

3. **建议的安全措施**
   - 使用代码混淆（Dotfuscator / ConfuserEx）
   - 使用强名称签名
   - 添加在线验证（可选）

---

## 🛠️ 高级选项

### 方案A: 混合模式（推荐）

同时支持嵌入 License 和外部文件：

```csharp
private void LoadLicense()
{
    // 1. 优先使用嵌入的 License
    var embedded = EmbeddedLicenseManager.LoadFromEmbeddedResource();
    if (embedded != null && embedded.IsValid())
    {
        _currentLicense = embedded;
        
        // 可选：保存到本地，方便用户查看
        if (!File.Exists(_licenseFilePath))
        {
            SaveLicense(embedded);
        }
        return;
    }

    // 2. 如果嵌入的 License 无效或不存在，使用外部文件
    if (File.Exists(_licenseFilePath))
    {
        // 加载外部 License...
    }
    
    // 3. 都没有，创建试用版
    _currentLicense = CreateTrialLicense();
}
```

**优点**：
- ✅ 预装客户直接使用（嵌入的 License）
- ✅ 其他客户可以手动激活（外部文件）
- ✅ 嵌入的 License 过期后可以更新（新的外部文件）

### 方案B: 只使用嵌入 License

如果您想强制使用嵌入的 License：

```csharp
private void LoadLicense()
{
    var embedded = EmbeddedLicenseManager.LoadFromEmbeddedResource();
    if (embedded == null || !embedded.IsValid())
    {
        throw new Exception("此版本需要有效的嵌入 License");
    }
    _currentLicense = embedded;
}
```

---

## 📊 与外部文件方式对比

| 特性 | 外部文件 | 嵌入资源 |
|------|----------|----------|
| 用户体验 | 需要激活 | 开箱即用 ✅ |
| 安全性 | 中等 | 中等（需配合混淆）|
| 灵活性 | 高 ✅ | 低（需重新编译）|
| 分发复杂度 | 简单 ✅ | 简单 ✅ |
| 适用场景 | 通用分发 | OEM/预授权 |

---

## ❓ 常见问题

### Q1: 嵌入的 License 会被提取吗？
**A:** 会的。任何人都可以使用工具提取嵌入的资源。建议：
- 使用代码混淆工具
- 每个客户使用不同的 License（即使被提取也只能该客户使用）
- 添加在线验证

### Q2: 如何为多个客户编译不同版本？
**A:** 使用自动化脚本：
```powershell
$customers = @("客户A", "客户B", "客户C")
foreach ($customer in $customers) {
    .\build_with_embedded_license.ps1 -CustomerName $customer
}
```

### Q3: 嵌入的 License 过期怎么办？
**A:** 有两种选择：
1. 重新编译新版本发送给客户
2. 让客户手动激活新的 License（如果支持混合模式）

### Q4: 可以同时嵌入多个 License 吗？
**A:** 可以，但不推荐。建议使用一个 License 包含所有权限。

### Q5: 如何验证 License 是否真的嵌入了？
**A:** 方法1 - 删除外部 License 文件后运行程序
```bash
del mars.lic
MarsSpyTool.exe
# 如果能正常使用，说明嵌入成功
```

方法2 - 查看嵌入的资源
```bash
# 使用 .NET Reflector 或 ILSpy 查看程序集资源
```

---

## 🎓 下一步学习

- 📘 [BUILD_WITH_EMBEDDED_LICENSE.md](BUILD_WITH_EMBEDDED_LICENSE.md) - 详细的所有方案
- 📗 [LICENSE_INTEGRATION_GUIDE.md](LICENSE_INTEGRATION_GUIDE.md) - 完整集成指南
- 📙 [README.md](README.md) - API 文档

---

## ✅ 检查清单

使用前请确认：

- [ ] 已创建 `EmbeddedLicenseManager.cs` 文件
- [ ] 已生成 License 文件 (`embedded_license.lic`)
- [ ] 已将 License 文件添加为嵌入资源
- [ ] 已修改 `LoadLicense()` 方法
- [ ] 已编译测试
- [ ] 已验证嵌入的 License 正常工作

---

**就是这么简单！5分钟完成 License 嵌入。** 🎉

如有问题，请参考详细文档或联系技术支持。

---

*最后更新：2025-01-09*

