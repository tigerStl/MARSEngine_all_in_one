# Multi-Language Support Guide / 多语言支持指南

## English

### Overview

The MARS License Manager supports multiple languages with English as the default. Users can switch between English and Chinese (Simplified) through the interface.

### Supported Languages

- **English (en-US)** - Default
- **中文 (zh-CN)** - Chinese Simplified

### How to Change Language

1. Launch the application
2. In the "Basic Information" section, find the "Language" dropdown
3. Select your preferred language:
   - **English** for English
   - **中文** for Chinese
4. The interface will immediately update to show all text in the selected language
5. Your language preference is automatically saved and will be used when you restart the application

### Configuration File

The language setting is stored in `config.json` in the application directory:

```json
{
  "Language": "en-US"
}
```

You can manually edit this file to set the default language:
- `"en-US"` for English
- `"zh-CN"` for Chinese

### For Developers

#### Adding a New Language

To add support for a new language:

1. **Create a new resource file:**
   - Copy `Resources/Strings.resx` to `Resources/Strings.[culture-code].resx`
   - Example: `Strings.fr-FR.resx` for French

2. **Translate all string values:**
   - Open the new `.resx` file
   - Translate the `<value>` content for each `<data>` entry
   - Keep the `name` attributes unchanged

3. **Update the language selector:**
   - Edit `Form1.Designer.cs`
   - Add the new language to `comboBoxLanguage.Items`
   - Update `Form1.cs` to handle the new language in `ComboBoxLanguage_SelectedIndexChanged`

4. **Update configuration:**
   - Add the new culture code to `ConfigurationService.cs` if needed

#### Resource File Structure

All user-facing text is stored in resource files:

- `Resources/Strings.resx` - English (default)
- `Resources/Strings.zh-CN.resx` - Chinese
- `Resources/Strings.Designer.cs` - Auto-generated code (do not edit manually)

#### Using Localized Strings in Code

```csharp
using MarsLicenseManager.Resources;

// Display localized message
MessageBox.Show(Strings.MsgEnterMacAddress, Strings.TitleWarning);

// Format localized message with parameters
string message = string.Format(Strings.MsgMacCountExceeded, count);
```

---

## 中文

### 概述

MARS License管理器支持多语言，默认语言为英语。用户可以通过界面在英语和简体中文之间切换。

### 支持的语言

- **English (en-US)** - 默认语言
- **中文 (zh-CN)** - 简体中文

### 如何更改语言

1. 启动应用程序
2. 在"基本信息"部分，找到"语言"下拉框
3. 选择您偏好的语言：
   - **English** 表示英语
   - **中文** 表示中文
4. 界面将立即更新，显示所选语言的所有文本
5. 您的语言偏好会自动保存，重启应用程序时将使用该设置

### 配置文件

语言设置存储在应用程序目录下的 `config.json` 文件中：

```json
{
  "Language": "zh-CN"
}
```

您可以手动编辑此文件来设置默认语言：
- `"en-US"` 表示英语
- `"zh-CN"` 表示中文

### 开发者指南

#### 添加新语言

要添加对新语言的支持：

1. **创建新的资源文件：**
   - 将 `Resources/Strings.resx` 复制为 `Resources/Strings.[区域代码].resx`
   - 示例：`Strings.fr-FR.resx` 用于法语

2. **翻译所有字符串值：**
   - 打开新的 `.resx` 文件
   - 翻译每个 `<data>` 条目的 `<value>` 内容
   - 保持 `name` 属性不变

3. **更新语言选择器：**
   - 编辑 `Form1.Designer.cs`
   - 将新语言添加到 `comboBoxLanguage.Items`
   - 更新 `Form1.cs` 中的 `ComboBoxLanguage_SelectedIndexChanged` 以处理新语言

4. **更新配置：**
   - 如需要，在 `ConfigurationService.cs` 中添加新的区域代码

#### 资源文件结构

所有面向用户的文本都存储在资源文件中：

- `Resources/Strings.resx` - 英语（默认）
- `Resources/Strings.zh-CN.resx` - 中文
- `Resources/Strings.Designer.cs` - 自动生成的代码（请勿手动编辑）

#### 在代码中使用本地化字符串

```csharp
using MarsLicenseManager.Resources;

// 显示本地化消息
MessageBox.Show(Strings.MsgEnterMacAddress, Strings.TitleWarning);

// 使用参数格式化本地化消息
string message = string.Format(Strings.MsgMacCountExceeded, count);
```

---

## Technical Details / 技术细节

### Implementation / 实现方式

The multi-language support is implemented using .NET's built-in resource file system (`.resx` files) and the `CultureInfo` class.

多语言支持使用 .NET 内置的资源文件系统（`.resx` 文件）和 `CultureInfo` 类实现。

### Key Components / 核心组件

1. **Resource Files / 资源文件**
   - Store all translatable strings
   - 存储所有可翻译的字符串

2. **ConfigurationService / 配置服务**
   - Persist language preferences
   - 持久化语言偏好设置

3. **ApplyLanguage Method / ApplyLanguage 方法**
   - Updates all UI controls when language changes
   - 语言更改时更新所有 UI 控件

### Best Practices / 最佳实践

1. **Never hardcode user-facing text / 不要硬编码面向用户的文本**
   - Always use resource strings
   - 始终使用资源字符串

2. **Test all languages / 测试所有语言**
   - Verify translations in context
   - 在上下文中验证翻译

3. **Consider text length / 考虑文本长度**
   - UI controls should accommodate different text lengths
   - UI 控件应能容纳不同长度的文本

4. **Use format strings carefully / 谨慎使用格式字符串**
   - Ensure parameter order makes sense in all languages
   - 确保参数顺序在所有语言中都合理

