# RSA非对称加密实现指南

## 概述

MARS License系统已升级为使用RSA非对称加密，提供更高的安全性。

### 加密方案对比

| 特性 | 对称加密(HMAC) | 非对称加密(RSA) |
|------|---------------|----------------|
| 密钥类型 | 单一密钥 | 公钥+私钥对 |
| 安全性 | 中等 | 高 |
| 密钥泄露风险 | 可伪造License | 仅公钥泄露无法伪造 |
| 分发方式 | 密钥需保密 | 公钥可公开分发 |
| 使用场景 | 开发/测试 | 生产环境 |

## 系统架构

```
服务端（生成License）              客户端（验证License）
┌─────────────────────┐           ┌──────────────────────┐
│  私钥(mars_private.key)│           │  公钥(mars_public.key) │
│        ↓              │           │         ↓             │
│  签名License数据     │  -----→  │   验证签名            │
│  生成.mlic文件       │           │   检查有效性          │
└─────────────────────┘           └──────────────────────┘
```

## 密钥管理

### 1. 生成RSA密钥对

#### 方法A：使用命令行工具

```bash
cd LicenseEnCodeAndDecode
dotnet run -- generate-keys
```

输出：
```
- mars_public.key  (公钥，可分发)
- mars_private.key (私钥，严格保密)
```

#### 方法B：使用代码生成

```csharp
using LicenseEnCodeAndDecode.Services;

// 生成并保存密钥对
RsaKeyGenerator.GenerateKeyPair("public.key", "private.key", 2048);

// 或者生成为字符串
var (publicKey, privateKey) = RsaKeyGenerator.GenerateKeyPairString(2048);
```

### 2. 密钥文件格式

**公钥 (mars_public.key)**
```
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA2TQvTks2qTFbyN73U9pjGL2BxTs2oHqaIvuyxzK5m15S9xORaBRO
...
-----END RSA PUBLIC KEY-----
```

**私钥 (mars_private.key)**
```
-----BEGIN RSA PRIVATE KEY-----
MIIE...
-----END RSA PRIVATE KEY-----
```

### 3. 密钥存储建议

#### 服务端（私钥）
- ✅ 存储在安全目录，限制访问权限
- ✅ 使用环境变量或密钥管理服务
- ❌ 不要提交到Git仓库
- ❌ 不要硬编码在代码中

#### 客户端（公钥）
- ✅ 可以随应用程序分发
- ✅ 可以内嵌在代码中
- ✅ 即使泄露也无法伪造License

## 使用方法

### 服务端 - 生成License

```csharp
using LicenseEnCodeAndDecode.Models;
using LicenseEnCodeAndDecode.Services;

// 1. 创建服务并加载私钥
var encryptionService = new LicenseEncryptionService();
encryptionService.LoadPrivateKey("mars_private.key");

// 2. 创建License信息
var licenseInfo = new LicenseInfo
{
    LicenseCount = 1,
    ValidityDays = 365,
    ExpirationDate = DateTime.Now.AddDays(365),
    MacAddresses = new List<string> { "00-11-22-33-44-55" },
    CustomerName = "客户名称"
};

// 3. 生成加密License（使用RSA签名）
byte[] encryptedLicense = encryptionService.GenerateEncryptedLicense(licenseInfo);

// 4. 保存到文件
File.WriteAllBytes("customer_license.mlic", encryptedLicense);
```

### 客户端 - 验证License

```csharp
using LicenseEnCodeAndDecode.Services;

// 1. 创建服务并加载公钥
var encryptionService = new LicenseEncryptionService();
encryptionService.LoadPublicKey("mars_public.key");

// 2. 验证License
byte[] encryptedData = File.ReadAllBytes("customer_license.mlic");
var licenseInfo = encryptionService.DecryptAndValidateLicense(encryptedData);

// 3. 检查结果
if (licenseInfo == null)
{
    Console.WriteLine("License无效或已被篡改！");
    Application.Exit();
}
else if (licenseInfo.ExpirationDate < DateTime.Now)
{
    Console.WriteLine("License已过期！");
    Application.Exit();
}
else
{
    Console.WriteLine($"License验证成功！剩余{(licenseInfo.ExpirationDate - DateTime.Now).Days}天");
}
```

### 使用LicenseValidator（推荐）

```csharp
using LicenseEnCodeAndDecode.Services;

// 创建验证器
var validator = new LicenseValidator();

// 加载公钥
validator.LoadPublicKey("mars_public.key");

// 验证License
var result = validator.ValidateLicense("customer_license.mlic");

if (!result.IsValid)
{
    MessageBox.Show($"License错误：{result.ErrorMessage}", "错误");
    Application.Exit();
}
```

## EngineLicense应用程序配置

### 自动加载私钥

`EngineLicense`应用程序会自动从以下位置加载私钥：

1. 当前目录: `mars_private.key`
2. 应用程序目录: `{AppDir}/mars_private.key`
3. 开发环境: `../../LicenseEnCodeAndDecode/mars_private.key`

如果没有找到私钥，系统会回退到HMAC签名（兼容模式）。

### 生产环境部署

1. **复制私钥文件**
   ```
   EngineLicense/
   ├── MarsLicenseManager.exe
   ├── mars_private.key        ← 私钥文件
   └── ...
   ```

2. **设置文件权限**
   - Windows: 右键 → 属性 → 安全 → 仅管理员可读写
   - Linux: `chmod 600 mars_private.key`

## 客户端应用集成

### 方法1：公钥文件分发

```csharp
// 1. 将mars_public.key文件随应用程序分发
// 2. 在应用程序启动时加载

public class Program
{
    static void Main()
    {
        var validator = new LicenseValidator();
        
        // 从应用程序目录加载公钥
        string publicKeyPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "mars_public.key"
        );
        
        validator.LoadPublicKey(publicKeyPath);
        
        // 验证License
        var result = validator.ValidateLicense("license.mlic");
        if (!result.IsValid)
        {
            Console.WriteLine($"License无效：{result.ErrorMessage}");
            return;
        }
        
        // 继续应用程序逻辑...
    }
}
```

### 方法2：公钥内嵌代码

```csharp
public class Program
{
    // 公钥可以安全地内嵌在代码中
    private const string PUBLIC_KEY_PEM = @"
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA2TQvTks2qTFbyN73U9pjGL2BxTs2oHqaIvuyxzK5m15S9xORaBRO
...
-----END RSA PUBLIC KEY-----";

    static void Main()
    {
        var validator = new LicenseValidator();
        validator.LoadPublicKeyFromPem(PUBLIC_KEY_PEM);
        
        // 验证License...
    }
}
```

## 向后兼容

系统保持向后兼容，支持两种签名方式：

1. **RSA签名**（推荐）- 使用RSA私钥/公钥
2. **HMAC签名**（兼容）- 使用对称密钥

License文件会标记签名类型：
```json
{
  "Data": "...",
  "Signature": "...",
  "SignatureType": "RSA"  // 或 "HMAC"
}
```

验证时会自动检测并使用相应的验证方法。

## 安全最佳实践

### ✅ 推荐做法

1. **私钥管理**
   - 存储在安全位置
   - 使用环境变量或密钥管理服务
   - 定期轮换密钥
   - 备份到安全存储

2. **公钥分发**
   - 可以随应用程序分发
   - 可以内嵌在代码中
   - 可以通过HTTPS下载

3. **License文件**
   - 使用`.mlic`扩展名
   - 定期验证有效性
   - 检查MAC地址绑定
   - 检查应用程序哈希

### ❌ 避免做法

1. **不要**将私钥提交到Git
2. **不要**在日志中输出密钥
3. **不要**通过不安全渠道传输私钥
4. **不要**使用弱密钥（少于2048位）

## 密钥轮换

当需要更换密钥时：

1. 生成新的密钥对
2. 使用新私钥生成License
3. 更新客户端公钥
4. 保留旧公钥用于验证旧License（可选）

```csharp
// 支持多个公钥验证
public class MultiKeyValidator
{
    private List<RSA> _publicKeys = new();
    
    public void AddPublicKey(string keyPath)
    {
        var key = RsaKeyGenerator.LoadPublicKeyFromFile(keyPath);
        _publicKeys.Add(key);
    }
    
    public bool Validate(byte[] licenseData)
    {
        // 尝试用任一公钥验证
        foreach (var publicKey in _publicKeys)
        {
            if (TryValidate(licenseData, publicKey))
                return true;
        }
        return false;
    }
}
```

## 故障排除

### 问题1：找不到密钥文件

**错误**：`⚠️ 未找到RSA私钥文件`

**解决**：
1. 确认密钥文件位置
2. 检查文件名：`mars_private.key` 或 `mars_public.key`
3. 检查文件权限

### 问题2：签名验证失败

**错误**：`License验证失败！文件可能已损坏或被篡改`

**原因**：
1. License文件被修改
2. 使用错误的公钥验证
3. License使用不同的私钥生成

**解决**：
1. 重新生成License
2. 确认使用匹配的密钥对
3. 检查License文件完整性

### 问题3：MAC地址验证失败

**错误**：`当前机器的MAC地址未授权`

**解决**：
1. 检查License中的MAC地址列表
2. 确认网卡状态（必须是UP状态）
3. 重新生成License并添加正确的MAC地址

## 性能考虑

- **RSA签名**：约2-5ms（生成）
- **RSA验证**：约1-2ms（验证）
- **AES加密**：约<1ms

RSA仅用于签名，数据加密仍使用高效的AES算法。

## 总结

**RSA非对称加密优势：**
- ✅ 更高安全性
- ✅ 公钥可安全分发
- ✅ 即使客户端代码泄露也无法伪造License
- ✅ 支持密钥轮换
- ✅ 向后兼容HMAC签名

**适用场景：**
- ✅ 生产环境（强烈推荐）
- ✅ 需要分发License验证器
- ✅ 高安全要求的应用

---

**生成的密钥文件位置：**
- `LicenseEnCodeAndDecode/mars_public.key`
- `LicenseEnCodeAndDecode/mars_private.key`

**下次编译前请关闭所有运行中的MarsLicenseManager.exe实例**

