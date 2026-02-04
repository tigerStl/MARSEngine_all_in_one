# MARS License Management System / MARS License管理系统

## Project Overview / 项目概述

The MARS License Management System consists of two main components:

MARS License管理系统由两个主要组件组成：

1. **LicenseEnCodeAndDecode** - Core DLL library for license encryption/decryption
2. **EngineLicense** - Windows Forms UI application for license management

## Project Structure / 项目结构

```
MarsLicense/
├── LicenseEnCodeAndDecode/          # Core DLL Library
│   ├── Models/
│   │   └── LicenseInfo.cs           # Data models
│   ├── Services/
│   │   ├── LicenseEncryptionService.cs  # Encryption service
│   │   └── LicenseValidator.cs          # Validation service
│   ├── LicenseEnCodeAndDecode.csproj
│   └── README.md
│
├── EngineLicense/                   # Windows Forms Application
│   ├── Resources/                   # Multi-language resources
│   │   ├── Strings.resx            # English (default)
│   │   └── Strings.zh-CN.resx      # Chinese
│   ├── Services/
│   │   └── ConfigurationService.cs # App configuration
│   ├── Form1.cs                    # Main form logic
│   ├── Form1.Designer.cs           # UI design
│   ├── Program.cs                  # Entry point
│   ├── MarsLicenseManager.csproj
│   ├── README.md
│   ├── USAGE.md
│   ├── MULTILINGUAL.md
│   └── PROJECT_SUMMARY.md
│
├── .gitignore
└── README.md                       # This file
```

## Quick Start / 快速开始

### Prerequisites / 先决条件

- .NET 9.0 SDK
- Windows OS
- Visual Studio 2022 or VS Code (optional)

### Build & Run / 编译和运行

#### Build DLL / 编译DLL

```bash
cd LicenseEnCodeAndDecode
dotnet build
```

#### Build and Run Application / 编译和运行应用程序

```bash
cd EngineLicense
dotnet build
dotnet run
```

Or open the solution in Visual Studio and press F5.

### Build Both Projects / 编译两个项目

From the root directory:

```bash
dotnet build LicenseEnCodeAndDecode/LicenseEnCodeAndDecode.csproj
dotnet build EngineLicense/MarsLicenseManager.csproj
```

## Features / 功能特性

### LicenseEnCodeAndDecode (DLL)

✅ **Encryption & Decryption / 加密解密**
- AES-256-CBC encryption
- HMAC-SHA256 digital signature
- Tamper detection

✅ **Validation / 验证**
- MAC address validation
- Application integrity check
- Expiration checking

✅ **Models / 数据模型**
- License information structure
- Application restriction details

### EngineLicense (UI Application)

✅ **License Generation / License生成**
- Configure license count (1-100)
- Set validity period (1-3650 days)
- Manage MAC addresses
- Optional application restrictions
- Customer information tracking

✅ **License Validation / License验证**
- Read and decrypt license files
- Verify digital signatures
- Display detailed license information
- Check expiration status

✅ **Multi-Language Support / 多语言支持**
- English (default)
- Chinese (Simplified)
- Runtime language switching
- Persistent language preferences

✅ **User Interface / 用户界面**
- Intuitive Windows Forms interface
- Real-time validation
- Detailed error messages
- Professional layout

## Usage Examples / 使用示例

### Generate License / 生成License

1. Launch `EngineLicense` application
2. Set license count and validity period
3. Add MAC addresses
4. Optionally enable application restrictions
5. Click "Generate License File"
6. Save the `.mlic` file

### Validate License / 验证License

1. Click "Validate License File"
2. Select a `.mlic` file
3. View validation results

### Use DLL in Your Application / 在应用程序中使用DLL

```csharp
using LicenseEnCodeAndDecode.Services;

// Validate license at application startup
var validator = new LicenseValidator();
var result = validator.ValidateLicense("license.mlic");

if (!result.IsValid)
{
    MessageBox.Show($"License Error: {result.ErrorMessage}");
    Application.Exit();
}
```

For more examples, see:
- [EngineLicense/USAGE.md](EngineLicense/USAGE.md)
- [LicenseEnCodeAndDecode/README.md](LicenseEnCodeAndDecode/README.md)

## Architecture / 架构设计

### Separation of Concerns / 关注点分离

The system is designed with clear separation:

- **LicenseEnCodeAndDecode**: Core business logic (encryption, validation)
- **EngineLicense**: User interface and application-specific logic

This separation allows:
- Easy integration into other applications
- Reusability of core components
- Better maintainability
- Testing isolation

### Security / 安全性

1. **Encryption**
   - AES-256 encryption
   - Secure key management (configure in production)

2. **Integrity**
   - HMAC digital signatures
   - Tamper detection

3. **Hardware Binding**
   - MAC address verification
   - Multiple device support

4. **Application Control**
   - File hash verification (MD5)
   - Version tracking

## Configuration / 配置

### Language Settings / 语言设置

Configuration is stored in `EngineLicense/config.json`:

```json
{
  "Language": "en-US"
}
```

Change to `"zh-CN"` for Chinese.

### Security Keys / 安全密钥

⚠️ **Production Warning / 生产环境警告**: 

The encryption and signing keys are currently hardcoded in `LicenseEncryptionService.cs`. For production use, move these to secure storage:

- Environment variables
- Azure Key Vault
- Encrypted configuration files

## Development / 开发

### Adding Features / 添加功能

1. **Core Logic**: Add to `LicenseEnCodeAndDecode`
2. **UI Features**: Add to `EngineLicense`
3. **New Languages**: See [EngineLicense/MULTILINGUAL.md](EngineLicense/MULTILINGUAL.md)

### Testing / 测试

```bash
# Build both projects
dotnet build LicenseEnCodeAndDecode/LicenseEnCodeAndDecode.csproj
dotnet build EngineLicense/MarsLicenseManager.csproj

# Run the application
cd EngineLicense
dotnet run
```

## Documentation / 文档

- [EngineLicense/README.md](EngineLicense/README.md) - UI application guide
- [EngineLicense/USAGE.md](EngineLicense/USAGE.md) - Detailed usage instructions
- [EngineLicense/MULTILINGUAL.md](EngineLicense/MULTILINGUAL.md) - Multi-language guide
- [EngineLicense/PROJECT_SUMMARY.md](EngineLicense/PROJECT_SUMMARY.md) - Project summary
- [LicenseEnCodeAndDecode/README.md](LicenseEnCodeAndDecode/README.md) - DLL API reference

## Build Status / 编译状态

✅ **LicenseEnCodeAndDecode**: Compiled Successfully  
✅ **EngineLicense**: Compiled Successfully  
✅ **Integration**: Working  

## Dependencies / 依赖项

### LicenseEnCodeAndDecode
- .NET 9.0
- System.Security.Cryptography
- System.Text.Json
- System.Net.NetworkInformation

### EngineLicense
- .NET 9.0 Windows
- Windows Forms
- LicenseEnCodeAndDecode (project reference)

## License / 许可证

This project is for internal MARS use only.  
本项目仅供MARS内部使用。

## Version / 版本

**v1.0.0** - Complete System
- Core DLL library
- Windows Forms UI application
- Multi-language support (English, Chinese)
- Full encryption and validation features

## Contact / 联系方式

For technical support, contact the MARS development team.  
如需技术支持，请联系MARS开发团队。

---

**Last Updated**: 2024-10-15  
**Project Type**: Internal License Management System  
**Target Framework**: .NET 9.0

