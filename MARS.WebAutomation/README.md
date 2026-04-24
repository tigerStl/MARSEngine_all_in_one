# MARS.WebAutomation

基于 **.NET Framework 4.7.2**（SDK 风格 `net472` 类库）与 **Microsoft.Playwright** 的 Web 录制/回放工作台（WinForms，DLL 输出）。

## 功能概要

- 工作台：工具栏（Target / Recorder / Replay / Export / Import / Save）、多 Tab（目标页、对象树、录制回放、设置）。
- 录制：页面事件 → 语义 Keyword（如 `FillEdit`、`ClickButton` 等）+ Playwright Locator + 参数。
- 回放：按保存步骤驱动 Playwright。
- 网络：捕获 XHR/Fetch 的协议信息（头、Cookie 摘要等）供后续性能测试使用。
- 存储：`data\[Host]_key\test_[Url].json`（可配置根目录）。

## 环境要求

- Windows + .NET Framework 4.7.2 开发/运行环境  
- Visual Studio 2017 或更高（建议）  

## Playwright 浏览器

还原 NuGet 后在本仓库目录执行（默认输出为 `bin\Debug\net472\`）：

```powershell
dotnet build .\MARS.WebAutomation.csproj
pwsh .\bin\Debug\net472\playwright.ps1 install chromium
```

若 `playwright.ps1` 路径不同，可在用户目录 `.nuget\packages\microsoft.playwright\` 下查找 CLI 脚本，或使用全局 `playwright` CLI 执行 `playwright install chromium`。

## 对外调用示例

```csharp
MARS.WebAutomation.WebAutomationApp.ShowWorkbench();
```

## 文档

- [需求说明](doc/需求.md)  
- [详细设计](doc/详细设计.md)  
- [状态追踪 / WBS](doc/状态追踪.md)  

## 进度

见 [doc/状态追踪.md](doc/状态追踪.md)。
