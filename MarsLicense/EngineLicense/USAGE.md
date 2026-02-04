# MARS License管理系统使用指南

## 目录

1. [快速开始](#快速开始)
2. [生成License](#生成license)
3. [验证License](#验证license)
4. [客户端集成](#客户端集成)
5. [常见问题](#常见问题)

## 快速开始

### 运行程序

```bash
cd c:\work\MARS\MarsLicense
dotnet run
```

或者直接运行编译后的exe文件。

### 界面说明

程序界面分为以下几个部分：

1. **基本信息区域**（顶部）
   - License数量
   - 有效期（天数）
   - 到期日期
   - 客户名称
   - 备注

2. **MAC地址管理**（左下）
   - MAC地址列表
   - 添加/删除MAC地址

3. **应用程序限制**（右下）
   - 启用/禁用应用限制
   - 应用程序列表
   - 添加/删除应用程序

4. **操作按钮**（底部）
   - 生成License文件
   - 验证License文件

## 生成License

### 步骤1：填写基本信息

1. **设置License数量**
   - 范围：1-100
   - 默认值：1
   - 说明：决定可以授权多少台机器

2. **设置有效期**
   - 范围：1-3650天
   - 默认值：365天（1年）
   - 说明：License的有效时长，到期日期会自动更新

3. **填写客户信息**（可选）
   - 客户名称：用于标识License归属
   - 备注：记录其他相关信息

### 步骤2：添加MAC地址

每个License需要绑定一个MAC地址，添加的MAC地址数量必须等于License数量。

#### 支持的MAC地址格式

```
00-11-22-33-44-55    (推荐格式)
00:11:22:33:44:55
001122334455
```

#### 如何获取MAC地址

**Windows系统：**
```cmd
# 方法1：使用ipconfig
ipconfig /all

# 方法2：使用getmac
getmac /v /fo list
```

在输出中找到"物理地址"或"Physical Address"。

**示例：**
```
物理地址. . . . . . . . . . . . . : 00-50-56-C0-00-08
```

#### 添加MAC地址步骤

1. 在MAC地址输入框中输入MAC地址
2. 点击"添加MAC地址"按钮
3. 重复以上步骤，直到达到License数量

### 步骤3：配置应用程序限制（可选）

如果需要限制License只能在特定应用程序中使用：

1. 勾选"启用应用程序限制"复选框
2. 点击"添加应用程序"按钮
3. 选择要授权的.exe文件
4. 系统会自动提取并保存以下信息：
   - 应用程序名称
   - 文件路径
   - 版本信息
   - 文件大小
   - MD5哈希值

**注意：** 启用应用程序限制后，必须至少添加一个应用程序。

### 步骤4：生成License文件

1. 确认所有信息填写正确
2. 点击"生成License文件"按钮
3. 选择保存位置
4. 文件将以`.mlic`扩展名保存
5. 默认文件名格式：`MARS_License_yyyyMMdd_HHmmss.mlic`

### 验证规则

生成前会进行以下验证：

- ✅ MAC地址数量必须等于License数量
- ✅ 至少添加一个MAC地址
- ✅ 如果启用应用限制，必须添加至少一个应用程序
- ✅ MAC地址格式必须正确
- ✅ 不允许重复的MAC地址

## 验证License

### 验证现有License文件

1. 点击"验证License文件"按钮
2. 选择要验证的`.mlic`文件
3. 系统会显示验证结果，包括：
   - ✅ **验证成功**：文件有效且未被篡改
   - ⚠️ **已过期**：License超过有效期
   - ❌ **验证失败**：文件损坏或被篡改

### 验证信息显示

验证成功后会显示以下信息：

```
License验证成功！ 【有效】

License数量：2
有效期：365 天
到期日期：2025-10-15 10:30:00
创建日期：2024-10-15 10:30:00
客户名称：示例客户
备注：测试License

MAC地址列表(2)：
00-11-22-33-44-55
AA-BB-CC-DD-EE-FF

应用程序限制(1)：
- MyApp.exe (v1.0.0.0)
  路径：C:\Program Files\MyApp\MyApp.exe
  MD5：abc123def456...
```

## 客户端集成

### 在应用程序中集成License验证

#### 方法1：引用项目

1. 将`Models`和`Services`文件夹复制到你的项目
2. 在应用程序启动时验证License

```csharp
using MarsLicenseManager.Services;

// 在应用程序启动时
var validator = new LicenseValidator();
var result = validator.ValidateLicense("path/to/license.mlic");

if (!result.IsValid)
{
    MessageBox.Show($"License验证失败：{result.ErrorMessage}", 
        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    Application.Exit();
    return;
}

// License验证成功，继续运行程序
MessageBox.Show($"License有效，剩余{validator.GetRemainingDays()}天", 
    "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
```

#### 方法2：集成到Program.cs

```csharp
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        
        // 验证License
        var validator = new LicenseValidator();
        string licensePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "license.mlic"
        );
        
        var result = validator.ValidateLicense(licensePath);
        
        if (!result.IsValid)
        {
            MessageBox.Show(
                $"License验证失败：{result.ErrorMessage}\n\n应用程序将退出。",
                "License错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return;
        }
        
        // 检查即将过期（少于30天）
        int remainingDays = validator.GetRemainingDays();
        if (remainingDays <= 30)
        {
            MessageBox.Show(
                $"警告：您的License将在{remainingDays}天后过期！\n请及时续期。",
                "License警告",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        
        // 继续运行应用程序
        Application.Run(new MainForm());
    }
}
```

#### 完整示例

```csharp
using MarsLicenseManager.Services;
using MarsLicenseManager.Models;

public class MyApplication
{
    private LicenseValidator _licenseValidator;
    private LicenseInfo _licenseInfo;
    
    public bool Initialize()
    {
        _licenseValidator = new LicenseValidator();
        
        // 从多个位置尝试加载License
        string[] possiblePaths = new[]
        {
            "license.mlic",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.mlic"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "MyApp", "license.mlic")
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                var result = _licenseValidator.ValidateLicense(path);
                
                if (result.IsValid)
                {
                    _licenseInfo = result.LicenseInfo!;
                    return true;
                }
                else
                {
                    LogError($"License验证失败 ({path}): {result.ErrorMessage}");
                }
            }
        }
        
        return false;
    }
    
    public void CheckLicenseStatus()
    {
        int remainingDays = _licenseValidator.GetRemainingDays();
        
        if (remainingDays <= 0)
        {
            throw new InvalidOperationException("License已过期");
        }
        
        if (remainingDays <= 7)
        {
            ShowWarning($"License将在{remainingDays}天后过期");
        }
    }
    
    private void LogError(string message)
    {
        // 实现日志记录
        Console.WriteLine($"[ERROR] {message}");
    }
    
    private void ShowWarning(string message)
    {
        // 显示警告信息
        MessageBox.Show(message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
```

### 定期验证

建议在应用程序运行期间定期验证License：

```csharp
// 创建定时器，每小时验证一次
private System.Windows.Forms.Timer _licenseCheckTimer;

private void InitializeLicenseCheck()
{
    _licenseCheckTimer = new System.Windows.Forms.Timer();
    _licenseCheckTimer.Interval = 3600000; // 1小时
    _licenseCheckTimer.Tick += (s, e) => CheckLicense();
    _licenseCheckTimer.Start();
}

private void CheckLicense()
{
    var result = _licenseValidator.ValidateLicense(_licensePath);
    
    if (!result.IsValid)
    {
        _licenseCheckTimer.Stop();
        MessageBox.Show("License验证失败，应用程序将退出。",
            "License错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Exit();
    }
}
```

## 常见问题

### Q1: MAC地址应该从哪里获取？

**A:** 在Windows中使用`ipconfig /all`命令，查看"物理地址"。选择主要网卡的MAC地址。

### Q2: 一个License可以用于多台机器吗？

**A:** 可以。通过设置License数量并添加对应数量的MAC地址，一个License文件可以授权多台机器。

### Q3: License文件可以修改吗？

**A:** 不可以。License文件使用加密和数字签名保护，任何修改都会导致验证失败。

### Q4: 如何延长License有效期？

**A:** 需要重新生成License文件，使用新的有效期设置。

### Q5: 应用程序限制是如何工作的？

**A:** 系统会验证运行程序的文件名、大小和MD5哈希值。如果这些信息与License中记录的不匹配，验证将失败。

### Q6: 更新应用程序后License会失效吗？

**A:** 如果启用了应用程序限制，更新会改变文件哈希和大小，需要重新生成License。

### Q7: License文件丢失了怎么办？

**A:** 需要重新生成License文件。建议保留原始的生成配置信息，以便重新生成相同的License。

### Q8: 可以在虚拟机中使用吗？

**A:** 可以，只要虚拟机的MAC地址在License授权列表中即可。

### Q9: 支持哪些.NET版本？

**A:** 当前使用.NET 9.0。如需兼容其他版本，可以修改`.csproj`文件中的`TargetFramework`。

### Q10: 如何保证License的安全性？

**A:** 
- 使用AES-256加密保护数据
- 使用HMAC-SHA256数字签名防止篡改
- 建议在生产环境中将密钥存储在安全位置
- 可以考虑添加硬件绑定等额外安全措施

## 技术支持

如需技术支持或有其他问题，请联系MARS开发团队。

## 更新日志

- **2024-10-15**: 初始版本发布


