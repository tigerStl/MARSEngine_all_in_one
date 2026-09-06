# 安装 MARS Java Automation MCP

MCP（Model Context Protocol）把 Chat（Cursor / VS Code / Codex）和 Java UI 自动化工具分开：模型理解业务指令，MCP tool 去发现应用、解析控件、执行操作并回读结果。

本仓库自带 **Java MCP Server**（`MARSJavaMcp`），工具名与 `doc/mars_java_automation_mcp_methods_spec.md` 一致，例如 `list_java_applications`、`attach_application`、`resolve_field`、`set_field`。

```text
Cursor / VS Code Chat
        │  MCP tools（list_java_applications / set_field / ...）
        ▼
MARSJavaMcp（stdio: java -jar MARSJavaMcp-1.0.0.jar）
        │  attach + HTTP /command
        ▼
目标 JVM 中的 MARSJavaEngine
```

没有 MCP 时，仍可按 [usage.md](usage.md) 用 Agent + WebSocket / HTTP 手工发命令。

## 0. 推荐：安装本仓库 Java MCP

### 0.1 构建

在 `MarsJavaEngine` 根目录：

```
mvn -q -DskipTests package
```

产物：

```
MARSJavaMcp/target/MARSJavaMcp-1.0.0.jar
MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar
MARSJavaEngine/target/MARSJavaEngine-1.0.0.jar
```

三个 JAR 需能互相找到。默认 MCP 会在自身同目录或模块 `target/` 下查找 Agent / Engine。也可设置：

```
MARS_AGENT_JAR
MARS_ENGINE_JAR
```

### 0.2 写入 Cursor MCP 配置

项目文件 `.cursor/mcp.json` 或 Cursor Settings → MCP：

```json
{
  "mcpServers": {
    "mars-java": {
      "command": "java",
      "args": [
        "-jar",
        "C:/work/MARS/MarsJavaEngine/MARSJavaMcp/target/MARSJavaMcp-1.0.0.jar"
      ]
    }
  }
}
```

打开 **Cursor Settings → MCP**，启用 `mars-java`，然后新开 Agent 会话。

### 0.3 验证

先启动 Northstar Demo，再在 Chat 中：

```
列出当前可调用的工具名
```

应看到 `list_java_applications`、`attach_application`、`resolve_field`、`set_field` 等。

然后：

```
列出 Java 应用，附加 Northstar，把 Notional 设为 100000000，Currency 设为 USD，然后读取这两个字段。
```

工具一览见规格文档第 17 节（V1）以及本仓库已实现的 V2 方法（双击、右键、对象树、表格编辑、截图证据等）。

---

以下为可选路径：通过 `MarsPlugins/VSCode` 扩展注册另一套 `mars-*` 工具。两套 MCP 不要同时开给同一个 Chat，以免 Agent 选错工具名。

## 1. 前置条件

- 已安装 [Cursor](https://cursor.com) 或 VS Code（1.85+，需带 MCP server definition API）
- Node.js 18+
- 本机可构建 `MarsPlugins/VSCode`（`npm`、JDK、Maven）
- 被测 Java 窗口已打开，例如 Northstar Demo

扩展工程路径（按本机调整）：

```
C:\work\MARS\MarsPlugins\VSCode
```

## 2. 推荐：安装扩展，让 Cursor 自动注册 MCP

扩展激活时会：

1. 在本机启动 MCP HTTP bridge
2. 通过 `mcpServerDefinitionProviders` 注册 stdio 服务器 `mars-local`
3. 用 `node out/mcp-server.js` 把 Chat 的 tool 调用转到扩展

### 2.1 开发模式（先验证）

```
cd C:\work\MARS\MarsPlugins\VSCode
npm install
npm run compile
```

需要完整 Java / ProcessInfo 能力时再构建：

```
npm run build:all
```

在 Cursor 或 VS Code 中打开 `MarsPlugins/VSCode`，按 **F5** 启动「扩展开发宿主」。

在新窗口中：

1. 命令面板执行 `Java UI Automation: Show Panel`
2. 命令面板执行 `Java UI Automation: MCP Show Status`
3. 确认输出里有 `Registered MCP server definition provider` 且 `out/mcp-server.js` 存在

### 2.2 安装 .vsix（本机长期使用）

```
cd C:\work\MARS\MarsPlugins\VSCode
npm install
npm run compile
npm install -g @vscode/vsce
vsce package
```

在 Cursor / VS Code：

1. 命令面板 → `Extensions: Install from VSIX...`
2. 选择生成的 `java-ui-automation-*.vsix`
3. 重新加载窗口

### 2.3 在 Cursor 里启用 MCP

1. 打开 **Cursor Settings → MCP**
2. 列表中应出现 **mars-local**（或 MARS Local MCP Servers）
3. 打开开关，等待状态变为已连接
4. 新开一个 Agent / Chat 会话（旧会话可能看不到新 tool）

若列表没有 `mars-local`：

- 确认当前窗口已加载 `java-ui-automation` 扩展
- 执行 `Java UI Automation: MCP Probe`
- 查看输出通道 **Java UI Automation**

## 3. 备选：手工写入 Cursor MCP 配置

扩展自动注册失败时，可在项目或用户级 MCP 配置里手动加服务器。

Cursor 项目文件：

```
<工作区>/.cursor/mcp.json
```

或 Cursor 用户 MCP 设置中的等价 JSON。

```json
{
  "mcpServers": {
    "mars-local": {
      "command": "node",
      "args": [
        "C:/work/MARS/MarsPlugins/VSCode/out/mcp-server.js"
      ],
      "env": {
        "MARS_WORKSPACE": "C:/work/MARS/MarsPlugins/VSCode",
        "MARS_MCP_TRACE": "C:/work/MARS/MarsPlugins/VSCode/scanedfiles/mcp-server.trace.log"
      }
    }
  }
}
```

注意：`mcp-server.js` 默认还要通过环境变量连接扩展 bridge：

- `MARS_MCP_BRIDGE_PORT`
- `MARS_MCP_BRIDGE_TOKEN`

这两个值由扩展宿主在注册 provider 时注入。手工配置时必须先启动扩展（F5 或已安装的扩展），从 `MCP Show Status` / 输出日志读取 port 与 token，再填进 `env`。只启动 `node mcp-server.js`、不启动扩展，tool 会报 `MCP bridge is unavailable`。

因此**优先用第 2 节的扩展自动注册**。

## 4. 验证 MCP 已装好

在新的 Chat 里发送：

```
请先列出你当前可调用的工具名（仅名称列表）
```

应看到类似：

```
mars-list-processes
mars-select-process
mars-get-object-tree
mars-highlight-object
mars-start-record
mars-stop-record
mars-get-steps
mars-update-step
mars-execute-step
mars-run-replay
mars-export-objects
mars-export-diagnostics
mars-get-last-errors
```

命令面板兜底（不依赖 Chat 发现）：

- `Java UI Automation: MCP Call Tool (Interactive)`
- 选择 `mars-list-processes`，确认返回 JSON

Chat 如果去跑 PowerShell 而不是 tool，用：

```
请不要调用 powershell/terminal。仅调用 MCP tool：mars-list-processes。返回原始 JSON 结果。
```

## 5. 和本仓库 Engine 一起用

完整演示顺序：

1. 启动 Demo

```
cd C:\work\MARS\MarsJavaEngine\MarsJavaDemo
mvn exec:java
```

2. 确认 Cursor 已启用 `mars-local`
3. 在 Chat 中：

```
列出当前 Java 进程，选中 Northstar Capital Markets Workstation，扫描对象树，确认能看到 Bond Trade Entry。
```

4. 业务指令示例：

```
Open the Northstar application and create a USD bond trade for 100m notional with FHLB at 98.25. Validate the trade, save it, and tell me the resulting Trade ID.
```

需要单独验证注入通道时，再按 [usage.md](usage.md) 运行 `MARSJavaEngineAgent`。MCP 路径通常由扩展完成进程选择与 attach；本仓库 Agent 用于直接调试 Engine 协议。

## 6. 工具一览

| Tool | 作用 |
| --- | --- |
| `mars-list-processes` | 列出本机 Java 进程 |
| `mars-select-process` | 按 PID 选中目标 |
| `mars-get-object-tree` | 扫描对象树 |
| `mars-highlight-object` | 高亮控件 |
| `mars-start-record` / `mars-stop-record` | 录制 |
| `mars-get-steps` / `mars-update-step` | 查看 / 改步骤 |
| `mars-execute-step` / `mars-run-replay` | 单步 / 回放 |
| `mars-export-objects` / `mars-export-diagnostics` | 导出证据 |
| `mars-get-last-errors` | 最近错误 |

## 7. 排障

| 现象 | 处理 |
| --- | --- |
| Settings → MCP 没有 mars-local | 扩展未激活；打开面板或执行 MCP Show Status |
| `MCP server definition API is unavailable` | Cursor / VS Code 版本过旧 |
| `server script not found` | 先 `npm run compile`，确认 `out/mcp-server.js` |
| `MCP bridge is unavailable` | 只起了 node server，没有扩展宿主；改用自动注册 |
| Chat 只有 `functions.*` 没有 `mars-*` | 新开 Agent 会话；确认 MCP 开关已开 |
| `Agent JARs not found` | 在 `MarsPlugins/VSCode` 执行 `npm run build:java` |
| `Failed to get Java processes` | 构建 `ProcessInfo`：`npm run build:processinfo` |
| 扫描结果空 | Demo 窗口必须可见 |

更细的 Chat 脚本见 `MarsPlugins/VSCode/doc/mcp-demo-chat-test.zh.md`。
