# LicenseEnCodeAndDecode - License Encryption & Decryption Library

## Overview / 概述

**LicenseEnCodeAndDecode** is a .NET class library that provides license encryption, decryption, and validation functionality for the MARS License system.

**LicenseEnCodeAndDecode** 是一个 .NET 类库，为 MARS License 系统提供License加密、解密和验证功能。

## Features / 功能

### Core Capabilities / 核心功能

- **License Encryption / License加密**
  - AES-256-CBC encryption
  - HMAC-SHA256 digital signature
  - Tamper detection
  
- **License Decryption & Validation / License解密和验证**
  - Signature verification
  - Expiration checking
  - MAC address validation
  - Application integrity verification

- **Data Models / 数据模型**
  - `LicenseInfo` - License information
  - `ApplicationInfo` - Application restriction details

## Project Structure / 项目结构

```
LicenseEnCodeAndDecode/
├── Models/
│   └── LicenseInfo.cs          # Data models
├── Services/
│   ├── LicenseEncryptionService.cs   # Encryption & signing
│   └── LicenseValidator.cs           # Client validation
└── LicenseEnCodeAndDecode.csproj     # Project file
```

## Usage / 使用方法

### As a DLL Reference / 作为DLL引用

Add project reference in your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="..\LicenseEnCodeAndDecode\LicenseEnCodeAndDecode.csproj" />
</ItemGroup>
```

Or use the command line:

```bash
dotnet add reference ..\LicenseEnCodeAndDecode\LicenseEnCodeAndDecode.csproj
```

### Generate License / 生成License

```csharp
using LicenseEnCodeAndDecode.Models;
using LicenseEnCodeAndDecode.Services;

// Create license information
var licenseInfo = new LicenseInfo
{
    LicenseCount = 1,
    ValidityDays = 365,
    ExpirationDate = DateTime.Now.AddDays(365),
    MacAddresses = new List<string> { "00-11-22-33-44-55" },
    CustomerName = "Customer Name",
    RestrictApplication = false
};

// Generate encrypted license
var encryptionService = new LicenseEncryptionService();
byte[] encryptedLicense = encryptionService.GenerateEncryptedLicense(licenseInfo);

// Save to file
File.WriteAllBytes("license.mlic", encryptedLicense);
```

### Validate License / 验证License

```csharp
using LicenseEnCodeAndDecode.Services;

// Validate license file
var validator = new LicenseValidator();
var result = validator.ValidateLicense("license.mlic");

if (result.IsValid)
{
    Console.WriteLine($"License valid for {validator.GetRemainingDays()} days");
}
else
{
    Console.WriteLine($"License invalid: {result.ErrorMessage}");
    Application.Exit();
}
```

### Decrypt and Verify / 解密和验证

```csharp
using LicenseEnCodeAndDecode.Services;

// Read and decrypt license
byte[] encryptedData = File.ReadAllBytes("license.mlic");
var encryptionService = new LicenseEncryptionService();
var licenseInfo = encryptionService.DecryptAndValidateLicense(encryptedData);

if (licenseInfo != null)
{
    Console.WriteLine($"License Count: {licenseInfo.LicenseCount}");
    Console.WriteLine($"Expiration: {licenseInfo.ExpirationDate:yyyy-MM-dd}");
}
```

## API Reference / API参考

### LicenseEncryptionService

#### Methods / 方法

- **`GenerateEncryptedLicense(LicenseInfo licenseInfo)`**
  - Generates encrypted license file
  - Returns: `byte[]` - Encrypted license data
  
- **`DecryptAndValidateLicense(byte[] encryptedData)`**
  - Decrypts and validates license
  - Returns: `LicenseInfo?` - License info or null if invalid
  
- **`CalculateFileMD5(string filePath)` (static)**
  - Calculates MD5 hash of a file
  - Returns: `string` - MD5 hash in hex format

### LicenseValidator

#### Methods / 方法

- **`ValidateLicense(string licenseFilePath)`**
  - Validates license file comprehensively
  - Returns: `LicenseValidationResult`
  
- **`GetRemainingDays()`**
  - Gets remaining days before expiration
  - Returns: `int` - Number of days
  
- **`GetCurrentLicense()`**
  - Gets the current loaded license info
  - Returns: `LicenseInfo?`

### LicenseValidationResult

#### Properties / 属性

- `bool IsValid` - Whether license is valid
- `bool IsExpired` - Whether license has expired
- `string? Message` - Success message
- `string? ErrorMessage` - Error message
- `LicenseInfo? LicenseInfo` - License information

## Security Features / 安全特性

1. **Encryption / 加密**
   - AES-256 in CBC mode
   - Secure key management

2. **Digital Signature / 数字签名**
   - HMAC-SHA256
   - Tamper detection

3. **Hardware Binding / 硬件绑定**
   - MAC address verification
   - Multiple MAC support

4. **Application Verification / 应用验证**
   - MD5 file hash
   - File size check
   - Version tracking

## Building the DLL / 编译DLL

```bash
cd LicenseEnCodeAndDecode
dotnet build
```

Output DLL location:
```
bin/Debug/net9.0/LicenseEnCodeAndDecode.dll
```

## Integration Examples / 集成示例

### Windows Forms Application

```csharp
public partial class MainForm : Form
{
    private LicenseValidator _validator;
    
    public MainForm()
    {
        InitializeComponent();
        ValidateLicense();
    }
    
    private void ValidateLicense()
    {
        _validator = new LicenseValidator();
        var result = _validator.ValidateLicense("license.mlic");
        
        if (!result.IsValid)
        {
            MessageBox.Show(
                $"License Error: {result.ErrorMessage}",
                "License Invalid",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            Application.Exit();
        }
    }
}
```

### Console Application

```csharp
class Program
{
    static void Main(string[] args)
    {
        var validator = new LicenseValidator();
        var result = validator.ValidateLicense("license.mlic");
        
        if (!result.IsValid)
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            Environment.Exit(1);
        }
        
        Console.WriteLine("License validated successfully!");
        Console.WriteLine($"Days remaining: {validator.GetRemainingDays()}");
        
        // Continue with application logic...
    }
}
```

## Dependencies / 依赖项

- **.NET 9.0**
- **System.Security.Cryptography** - Built-in
- **System.Text.Json** - Built-in
- **System.Net.NetworkInformation** - Built-in

## Technical Details / 技术细节

### Encryption Algorithm / 加密算法
- **Algorithm**: AES (Advanced Encryption Standard)
- **Key Size**: 256 bits
- **Mode**: CBC (Cipher Block Chaining)
- **Padding**: PKCS7

### Signature Algorithm / 签名算法
- **Algorithm**: HMAC-SHA256
- **Purpose**: Data integrity and authenticity

### File Format / 文件格式
`.mlic` files contain:
```json
{
  "Data": "{encrypted JSON}",
  "Signature": "{HMAC signature}",
  "Timestamp": "{UTC timestamp}"
}
```

## Notes / 注意事项

1. **Security Keys / 安全密钥**
   - Keys are hardcoded for demonstration
   - In production, use secure key storage (Azure Key Vault, etc.)

2. **MAC Address / MAC地址**
   - Validates against active network interfaces
   - Supports multiple MAC addresses per license

3. **Expiration / 过期**
   - Checked against system time
   - Ensure system clock is accurate

## Version History / 版本历史

- **v1.0.0** - Initial release
  - License encryption and decryption
  - MAC address validation
  - Application restriction support
  - Multi-language ready

## License / 许可证

This library is for internal MARS use only.
本库仅供MARS内部使用。

---

**Build Status**: ✅ Compiled Successfully
**编译状态**: ✅ 编译成功

