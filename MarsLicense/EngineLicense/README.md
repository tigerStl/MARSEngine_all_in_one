# MARS License管理系统

## 项目简介

MARS License管理系统是一个基于C# Windows Forms的License管理工具，用于生成和验证加密的License文件。系统支持MAC地址绑定、时限控制、应用程序限制等功能。

## 主要功能

### 1. License信息管理
- **License数量设置**：支持1-100个License
- **有效期设置**：支持1-3650天（默认365天/一年）
- **自动计算到期日期**：根据有效期自动计算
- **客户信息**：可选填写客户名称和备注信息

### 2. MAC地址管理
- **多MAC地址支持**：根据License数量添加对应数量的MAC地址
- **格式验证**：支持多种MAC地址格式
  - `00-11-22-33-44-55`
  - `00:11:22:33:44:55`
  - `001122334455`
- **重复检测**：自动检测并防止添加重复的MAC地址
- **数量限制**：MAC地址数量必须等于License数量

### 3. 应用程序限制
- **可选功能**：通过复选框启用/禁用应用程序限制
- **应用信息采集**：自动提取应用程序信息
  - 应用程序名称
  - 文件路径
  - 版本信息
  - 文件大小
  - MD5哈希值（用于完整性验证）
- **多应用支持**：可以添加多个应用程序

### 4. License文件生成
- **加密存储**：使用AES-256加密算法
- **数字签名**：使用HMAC-SHA256进行签名验证
- **防篡改**：任何修改都会导致验证失败
- **文件格式**：`.mlic`格式

### 5. License验证
- **完整性验证**：验证文件是否被篡改
- **有效期检查**：自动检查License是否过期
- **详细信息显示**：显示所有License信息
  - 基本信息（数量、有效期、到期日期等）
  - MAC地址列表
  - 应用程序限制信息

## 技术架构

### 项目结构

```
MarsLicenseManager/
├── Models/
│   └── LicenseInfo.cs          # License数据模型
├── Services/
│   └── LicenseEncryptionService.cs  # 加密和签名服务
├── Form1.cs                     # 主窗体逻辑
├── Form1.Designer.cs            # 主窗体设计器
├── Program.cs                   # 程序入口
└── MarsLicenseManager.csproj    # 项目配置
```

### 核心技术

- **.NET 9.0**: 使用最新的.NET框架
- **Windows Forms**: 桌面应用程序界面
- **AES加密**: 256位AES加密算法保护License数据
- **HMAC签名**: SHA256哈希消息认证码确保数据完整性
- **JSON序列化**: 使用System.Text.Json进行数据序列化

### 安全特性

1. **加密存储**
   - AES-256-CBC模式加密
   - 所有License数据加密存储
   
2. **完整性保护**
   - HMAC-SHA256数字签名
   - 防止License文件被篡改
   - 包含时间戳防止重放攻击

3. **应用程序验证**
   - MD5文件哈希验证
   - 文件大小验证
   - 版本信息记录

## 使用说明

### 编译项目

```bash
# 恢复依赖
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

### 生成License

1. 设置License数量（1-100）
2. 设置有效期（天数，默认365天）
3. 可选：填写客户名称和备注
4. 添加MAC地址（数量必须等于License数量）
5. 可选：启用应用程序限制并添加应用程序
6. 点击"生成License文件"按钮
7. 选择保存位置，文件将以`.mlic`格式保存

### 验证License

1. 点击"验证License文件"按钮
2. 选择要验证的`.mlic`文件
3. 系统将显示：
   - 验证结果（成功/失败）
   - License有效性（有效/已过期）
   - 所有License详细信息

## License文件格式

生成的`.mlic`文件包含以下加密信息：

```json
{
  "Data": "{加密的License JSON数据}",
  "Signature": "{HMAC签名}",
  "Timestamp": "{生成时间戳}"
}
```

内部License数据结构：

```json
{
  "LicenseCount": 1,
  "ValidityDays": 365,
  "ExpirationDate": "2025-10-15T00:00:00",
  "MacAddresses": ["00-11-22-33-44-55"],
  "RestrictApplication": true,
  "Applications": [
    {
      "Name": "app.exe",
      "ExePath": "C:\\path\\to\\app.exe",
      "Version": "1.0.0.0",
      "FileHash": "abc123...",
      "FileSize": 1024000
    }
  ],
  "CreatedDate": "2024-10-15T10:30:00",
  "CustomerName": "客户名称",
  "Notes": "备注信息"
}
```

## 注意事项

1. **密钥安全**：生产环境中应将加密密钥和签名密钥存储在安全位置（如配置文件、环境变量或密钥管理服务）
2. **MAC地址格式**：确保输入正确的MAC地址格式
3. **License数量**：MAC地址数量必须与License数量完全匹配
4. **应用程序限制**：启用后必须添加至少一个应用程序
5. **文件备份**：建议备份生成的License文件

## 扩展功能建议

1. **数据库存储**：将生成的License信息存储到数据库
2. **批量生成**：支持批量生成多个License文件
3. **在线激活**：支持在线激活和远程验证
4. **硬件指纹**：除MAC地址外，支持更多硬件指纹（CPU ID、硬盘序列号等）
5. **自动更新**：支持License自动更新和续期
6. **使用统计**：记录License使用情况和统计信息

## 系统要求

- **操作系统**：Windows 10/11
- **.NET运行时**：.NET 9.0或更高版本
- **开发工具**：Visual Studio 2022 或 Visual Studio Code + .NET SDK

## 许可证

本项目仅供MARS内部使用。

## 版本历史

- **v1.0.0** (2024-10-15)
  - 初始版本
  - 支持基本License生成和验证
  - MAC地址管理
  - 应用程序限制功能
  - AES加密和HMAC签名

## 联系方式

如有问题或建议，请联系MARS开发团队。


