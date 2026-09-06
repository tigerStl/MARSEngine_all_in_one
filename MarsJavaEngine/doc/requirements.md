# MARS Java Engine 与 Agent 需求说明

## 1. 目标

在**已经运行**的 Java 桌面进程中注入自动化引擎，使外部控制面（MARS 服务、测试客户端、Cursor / Codex + MCP）能够：

1. 发现目标 JVM
2. 注入引擎，不改目标应用启动参数
3. 扫描可见 UI 对象
4. 把对象属性与屏幕坐标写到交换目录
5. 通过 WebSocket / HTTP 接收后续命令
6. 按需卸载引擎服务

本仓库的引擎是 **in-process Java Agent**，不是独立 GUI，也不替代目标业务应用。

## 2. 模块职责

### 2.1 MARSJavaEngineAgent

可执行 JAR，主类：`com.mars.agent.MarsJavaEngineAgentMain`。

职责：

- 按进程名 / PID 定位目标 JVM
- 校验目标是 Java 进程
- 通过 `VirtualMachine.attach` + `loadAgent` 把 `MARSJavaEngine` 注入目标进程
- 把 `EngineConfig` 以 JSON 作为 agent 参数传入
- `debug-single` 模式下按进程名查找 PID，并在注入后通过 HTTP 触发全量扫描
- `unload` 模式下不注入，只向已运行引擎发送 `UNLOAD_ENGINE`

查找引擎 JAR 的顺序：

1. 环境变量 `MARS_ENGINE_JAR`
2. Agent JAR 同目录下的 `MARSJavaEngine-*.jar`

### 2.2 MARSJavaEngine

被注入后的引擎，入口：

- `agentmain` / `premain`：`com.mars.javaengine.MarsJavaEngineAgent`
- 实际启动：`com.mars.javaengine.EngineService`

职责：

- 在交换目录写入 `MarsJavaEngineSwap.json`（服务 IP、WebSocket 端口，debug 时还有 HTTP 端口）
- 向 MARS 控制服务发送 TCP 握手 `HAND_SHAKING`
- 启动 WebSocket 命令服务（始终）
- `debug-single` 时额外启动 HTTP `POST /command`
- 扫描 AWT/Swing、嵌入的 JavaFX、SWT 可见控件
- 将扫描结果写入 `MarsJavaEngineUiObjects.json`
- 对扫描到的区域做红色高亮（默认最多 30 个，可用 `MARS_HIGHLIGHT_LIMIT` 调整）
- 响应卸载命令并停止服务

## 3. 运行链路

```text
目标 Java 应用（例如 Northstar Demo）
        │
        │  java -jar MARSJavaEngineAgent-*.jar
        │  <processName> <pid> <swapDir> <serverIp> <serverPort>
        ▼
MARSJavaEngineAgent
        │  VirtualMachine.attach + loadAgent
        ▼
目标 JVM 内的 MARSJavaEngine
        │
        ├── 写 swapDirectory/MarsJavaEngineSwap.json
        ├── TCP HAND_SHAKING → serverIp:serverPort
        ├── WebSocket 监听动态端口
        └── 收到 GET_UIOBJECTS_ALL
                ├── 扫描 UI
                ├── 高亮
                └── 写 MarsJavaEngineUiObjects.json
```

与 Chat / MCP 的关系：

```text
用户业务指令
    → Cursor / VS Code Agent
    → MARS MCP tools
    → 扩展或控制面选择进程 / 注入
    → Java Engine 扫描或操作 UI
    → 交换文件 / 工具返回值作为证据
```

MCP 本身不在本仓库实现。本引擎提供进程内扫描与命令通道；MCP 安装见 [mcp-install.md](mcp-install.md)。

## 4. 功能需求（当前实现）

| 编号 | 需求 | 状态 |
| --- | --- | --- |
| FR-01 | 按 PID + 进程名注入运行中的 JVM | 已实现 |
| FR-02 | 按进程名查找并注入（`debug-single`） | 已实现 |
| FR-03 | 注入失败时向控制服务发送 `INJECT_JAVAENGINE_STATUS / Failed` | 已实现 |
| FR-04 | 引擎启动后写 `MarsJavaEngineSwap.json` | 已实现 |
| FR-05 | 引擎启动后发送 `HAND_SHAKING`，`ResultType` 为 WebSocket 端口 | 已实现 |
| FR-06 | WebSocket 接收 JSON 命令 | 已实现 |
| FR-07 | `GET_UIOBJECTS_ALL` 扫描并落盘 | 已实现 |
| FR-08 | `UNLOAD_ENGINE` 停止 HTTP / WebSocket / keepalive | 已实现 |
| FR-09 | 扫描 Swing/AWT 窗口、`JTree` 节点、`JTabbedPane` Tab | 已实现 |
| FR-10 | 扫描 `JFXPanel` 内 JavaFX 节点与 Tab | 已实现 |
| FR-11 | 扫描 SWT Shell / Tree / TabFolder（目标进程已加载 SWT 时） | 已实现 |
| FR-12 | 对象属性包含 class、name、text、javaTypePath、javaNamePath、屏幕矩形 | 已实现 |
| FR-13 | 扫描后红色边框高亮 | 已实现 |
| FR-14 | Agent 侧 `unload` 通过 HTTP 转发卸载命令 | 已实现（需 debug-single 已启动 HTTP） |

根 README 中的 `GET_UIOBJECT_BY_MOUSE`、`GET_UIOBJECT_BY_XY` 是客户端约定示例；`EngineService` **当前只处理** `GET_UIOBJECTS_ALL` 与 `UNLOAD_ENGINE`。

## 5. 非功能需求

| 编号 | 需求 |
| --- | --- |
| NFR-01 | 引擎与 Agent 使用 Java 11 构建 |
| NFR-02 | 目标应用可以是 Java 11+ 桌面进程；Northstar Demo 需要 Java 17+ |
| NFR-03 | 不修改目标应用源码即可注入 |
| NFR-04 | 交换目录与日志必须可配置，便于多会话并行 |
| NFR-05 | WebSocket 端口自动选取空闲端口，避免写死冲突 |
| NFR-06 | 高亮不得永久挡住目标窗口（闪烁后关闭） |
| NFR-07 | 注入器与引擎 JAR 需 shade，保证目标进程 classpath 不依赖本仓库 |

## 6. 命令协议

通用 JSON 字段：

```json
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECTS_ALL",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

| MessageType | 方向 | 作用 |
| --- | --- | --- |
| `HAND_SHAKING` | Engine → 控制服务 | 报告 `SvcIp`、WebSocket 端口、目标进程名 |
| `INJECT_JAVAENGINE_STATUS` | Agent → 控制服务 | 注入失败 |
| `GET_UIOBJECTS_ALL` | 客户端 → Engine | 全量扫描 |
| `UNLOAD_ENGINE` | 客户端 → Engine | 停止引擎 |

控制服务地址由注入参数 `serverIp` / `serverPort` 指定。即使握手失败，引擎仍会继续监听 WebSocket。

## 7. 交换文件

`swapDirectory` 下：

| 文件 | 内容 |
| --- | --- |
| `MarsJavaEngineSwap.json` | `SvcIp`、`PortNumber`；debug 时含 `HttpPort` |
| `MarsJavaEngineUiObjects.json` | `StartTime`、`EndTime`、`TotalCount`、`Items[]` |
| `log/` | Agent / Engine 日志 |

`Items` 中每个对象：

```text
className
name
text
javaTypePath
javaNamePath
x, y, width, height
```

这是 MCP / Agent 做对象解析的主要证据，不要只依赖屏幕坐标。

## 8. 配置

| 项 | 来源 | 默认 |
| --- | --- | --- |
| `swapDirectory` | Agent 命令行第 3 参 | 无 |
| `serverIp` / `serverPort` | Agent 命令行第 4、5 参 | 无 |
| `debug-single` / `unload` | Agent 可选第 6 参 | 关闭 |
| `MARS_ENGINE_JAR` | 环境变量 | Agent 同目录匹配 |
| `MARS_HIGHLIGHT_LIMIT` | 环境变量 | `30` |
| 构建输出目录 | `build-config.properties` | `C:/work/automationTest/Automation Workbooks/dlls/javaEngine` |

## 9. 与 Demo / MCP 的验收关系

Northstar Demo（`MarsJavaDemo`）用来验证：

- 引擎能 attach 到真实 Swing 进程
- 能扫到带稳定 `setName` 的业务控件（如 `bond.notional`）
- 也能扫到仅有标签关联的控件（如 `settlementCombo`）
- Chat 通过 MCP 用业务语言下指令，而不是让用户报坐标

完整 Chat 场景见 `MarsJavaDemo/docs/agent-demo-scenarios.md`。
