# MARS Message Agent

C# COM+ 本地服务器，可在任务栏右下角显示托盘图标，并启动从 10010 开始的 WebSocket 服务。支持通过网页 JS 使用 `CreateObject`（或 `new ActiveXObject`）激活，并可传入 SessionId（GUID）。

## 功能概览

1. **COM 激活**：可注册为 COM 组件，由 Web 端 JS 通过 `CreateObject("MARSMessageAgent.Agent")` 创建，支持传入 SessionId 参数。
2. **系统托盘**：激活后在任务栏右下角创建图标。
3. **WebSocket 服务**：从端口 10010 起寻找空闲端口并绑定。
4. **握手协议**：支持 `ShakeHandle_request` / `ShakeHandle_response`。
5. **启动 MARS Engine**：支持 `Start_MARSEngine_request`，在 COM 与 MARS 同目录下查找 ClickOnce 安装的 MARS Engine 并启动。

## 数据包与工厂

- 所有功能包继承基类 `PacketBase`（含 `packageType`、`sessionId`、`dateTime`）。
- 使用 **Newtonsoft.Json** 序列化/反序列化，通过 `PacketFactory.FromJson` 将 JSON 转为对应类型。

### 握手

- **请求**：`packageType: "ShakeHandle_request"`, `sessionId`, `dateTime`
- **响应**：`packageType: "ShakeHandle_response"`, `sessionId`, `message: "OK"`, `dateTime`

### 启动 MARS Engine

- **请求**：`packageType: "Start_MARSEngine_request"`, `sessionId`, `dateTime`
- **响应**：`packageType: "Start_MARSEngine_response"`, `sessionId`, `dateTime`, `result`（true/false 或 "Success"/"FAILED"）, `message`

## 如何注册 COM+

### 前提

1. 已成功生成项目（生成 `MARSMessageAgent\bin\Release\MARSMessageAgent.exe` 或 Debug 目录下的 exe）。
2. 必须以**管理员身份**运行注册命令（修改注册表需要提升权限）。

### 方法一：使用脚本（推荐）

1. 在解决方案根目录（与 `MARSMessageAgent.sln` 同级）找到：
   - **RegisterCOM.bat** — 注册
   - **UnregisterCOM.bat** — 取消注册
2. **右键** `RegisterCOM.bat` → **以管理员身份运行**。
3. 若提示“MARSMessageAgent.exe not found”，请先编译项目（Release 或 Debug 均可）。
4. 注册成功后会提示 “Done.”；取消注册时运行 `UnregisterCOM.bat`（同样需管理员）。

### 方法二：手动使用 RegAsm

在**以管理员身份打开**的命令提示符或 PowerShell 中执行：

```bat
:: 64 位系统（常见）
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe "完整路径\MARSMessageAgent.exe" /codebase

:: 32 位系统或需要给 32 位进程用
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe "完整路径\MARSMessageAgent.exe" /codebase
```

- 将 `"完整路径\MARSMessageAgent.exe"` 换成实际 exe 路径，例如：  
  `C:\work\MARS\MARSMessageAgent\MARSMessageAgent\bin\Release\MARSMessageAgent.exe`
- `/codebase` 表示把 exe 的完整路径写入注册表，便于 COM 按路径启动本地服务器。

### 取消注册

```bat
:: 64 位
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe "完整路径\MARSMessageAgent.exe" /unregister

:: 或直接运行项目根目录下的 UnregisterCOM.bat（以管理员身份）
```

### 验证

- 注册后，在注册表中可看到 ProgId `MARSMessageAgent.Agent` 与对应 CLSID。
- 在支持 ActiveX 的环境（如 IE）中可用：  
  `var agent = new ActiveXObject("MARSMessageAgent.Agent");`

---

## 构建与 COM 注册（简要）

1. 用 Visual Studio 或 `msbuild` 打开 `MARSMessageAgent.sln` 并生成（目标 .NET Framework 4.8）。
2. 以**管理员身份**运行根目录下：
   - `RegisterCOM.bat` — 注册 COM（RegAsm /codebase）
   - `UnregisterCOM.bat` — 取消注册

## 网页 JS 使用示例

```javascript
// 创建 Agent 并传入 SessionId（GUID）
var sessionId = "12345678-1234-1234-1234-123456789012";
var agent = new ActiveXObject("MARSMessageAgent.Agent");
agent.SessionId = sessionId;
agent.Activate(sessionId);

// 或直接带参激活
agent.Activate("12345678-1234-1234-1234-123456789012");

// 获取 WebSocket 端口（握手后用于连接）
var port = agent.WebSocketPort;
```

**说明**：`CreateObject`/`ActiveXObject` 仅在支持 ActiveX 的环境（如 IE 或配置了相应策略的宿主）中可用。

## Web 界面调用本地应用的常见方式

网页无法像 IE+ActiveX 那样“在脚本里直接创建本地进程”，但**并非无法实现**与本地已安装应用的通信，常见做法如下。

| 方式 | 浏览器支持 | 说明 |
|------|------------|------|
| **ActiveX / COM** | 仅 IE、Edge IE 模式 | 网页中 `new ActiveXObject("ProgId")` 由系统启动并绑定本地 exe。本项目已支持。 |
| **本地 HTTP/WebSocket 服务** | 任意浏览器 | 本地应用先运行并监听 `127.0.0.1:端口`，网页用 `fetch` / `WebSocket` 连接。用户需先启动应用（或开机自启）。本项目非 IE 模式即采用此方式：exe 提供 HTTP 端口发现（10005）和 WebSocket（10010+）。 |
| **自定义 URL 协议** | 任意浏览器 | 注册协议（如 `myapp://`），网页跳转 `myapp://action?param=value` 时由系统启动本地程序并传入 URL。只能“唤起”并传简单参数，不适合长连接。 |
| **浏览器扩展 + Native Messaging** | Chrome/Edge/Firefox（需安装扩展） | 扩展通过 Native Messaging 与本地宿主程序通信，网页通过扩展间接调用本地能力。需开发并分发扩展。 |

**结论**：  
- 若必须由**网页脚本直接创建**本地进程，目前只有 **IE 或 Edge IE 模式 + COM/ActiveX** 可行。  
- 若接受**用户先启动本地应用**，则 **本地应用开 HTTP/WebSocket，网页连接 127.0.0.1** 即可在任意浏览器中实现调用，本项目已支持该模式。

## 项目结构

- `MARSMessageAgent/` — 主工程（WinExe，隐藏主窗体以保持消息循环）
- `Packets/` — 数据包基类、握手与 StartMARSEngine 请求/响应、`PacketFactory`
- `ComAgent.cs` — COM 可见类，ProgId: `MARSMessageAgent.Agent`
- `TrayIconManager.cs` — 托盘图标
- `WebSocketServerManager.cs` — WebSocket 服务与消息分发
- `MarsEngineLauncher.cs` — 查找并启动 MARS Engine（与 COM 同目录）

## 依赖

- .NET Framework 4.8
- Newtonsoft.Json
- Fleck（WebSocket 服务端）
