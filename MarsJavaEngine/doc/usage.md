# MARS Java Engine / Agent 使用说明

## 1. 环境

- JDK 11+（构建 Engine / Agent）
- Maven 3.8+
- 目标桌面 Java 应用已启动且窗口可见（不要 headless）
- 本机可对目标进程做 `attach`（与目标进程同一用户）

验证 Northstar Demo 时另需 **JDK 17+**。

## 2. 构建

在仓库根目录：

```
mvn -q -DskipTests package
```

产物：

```
MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar
MARSJavaEngine/target/MARSJavaEngine-1.0.0.jar
```

`pom.xml` 还会按 `build-config.properties` 把 shade JAR 复制到：

```
C:/work/automationTest/Automation Workbooks/dlls/javaEngine
  MarsEngineJava.jar
  MarsEngineJavaAgent.jar
```

Agent 运行时默认在**自己所在目录**找 `MARSJavaEngine-*.jar`。若只复制了 Agent JAR，请同时放上 Engine JAR，或设置：

```
set MARS_ENGINE_JAR=C:\path\to\MARSJavaEngine-1.0.0.jar
```

## 3. 启动被测应用

以 Northstar Demo 为例：

```
cd MarsJavaDemo
mvn clean package
mvn exec:java
```

或：

```
java -jar MarsJavaDemo/target/northstar-financial-demo-1.0.0.jar
```

记下窗口标题 **Northstar Capital Markets Workstation**，并用任务管理器 / `jps` 取得 PID。主类为 `com.northstar.capital.Main`。

```
jps -l
```

## 4. 注入引擎

参数：

```
<processName> <processId> <swapDirectory> <serverIp> <serverPort> [debug-single|unload]
```

### 4.1 已知 PID

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar ^
  "com.northstar.capital.Main" 12345 "C:\temp\mars\javaengine" 127.0.0.1 8080
```

`processName` 会与 `VirtualMachineDescriptor.displayName` 做包含匹配。

`serverIp` / `serverPort` 是 MARS 控制服务地址。本机没有控制服务时仍可注入；握手失败只记日志，WebSocket 仍会起来。

### 4.2 按进程名注入并立即扫描

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar ^
  "com.northstar.capital.Main" 0 "C:\temp\mars\javaengine" 127.0.0.1 8080 debug-single
```

此模式会：

1. 按名称查找 PID（跳过 Agent 自身）
2. 注入
3. 等待 `MarsJavaEngineSwap.json`
4. 向 `http://{SvcIp}:{HttpPort}/command` 发送 `GET_UIOBJECTS_ALL`

### 4.3 卸载

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar ^
  "com.northstar.capital.Main" 0 "C:\temp\mars\javaengine" 127.0.0.1 8080 unload
```

卸载走 HTTP，因此目标进程需要曾经用 `debug-single` 注入（才会有 `HttpPort`）。普通注入后请改用 WebSocket 发送 `UNLOAD_ENGINE`。

## 5. 连接 WebSocket

读取：

```
swapDirectory/MarsJavaEngineSwap.json
```

示例：

```json
{
  "SvcIp": "192.168.1.10",
  "PortNumber": 52341
}
```

连接：

```
ws://{SvcIp}:{PortNumber}
```

### 全量扫描

```json
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECTS_ALL",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

扫描完成后查看：

```
swapDirectory/MarsJavaEngineUiObjects.json
```

目标窗口上会出现红色高亮框。限制数量：

```
set MARS_HIGHLIGHT_LIMIT=10
```

负值表示不限制（可能较慢）。

### 卸载

```json
{
  "MessageSource": "TestClient",
  "MessageType": "UNLOAD_ENGINE",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

## 6. 日志

```
swapDirectory/log/
```

常见记录：

- Agent：`Injected MARSJavaEngine into process …`
- Engine：`Swap file written`、`WebSocket server started`、`UI objects saved`
- 失败：`Target java process not found`、`MARSJavaEngine jar not found`、`Handshake` 失败

## 7. 推荐联调顺序

1. 启动 Northstar Demo
2. 构建并注入 Agent（`debug-single`）
3. 确认 `MarsJavaEngineSwap.json` 与 `MarsJavaEngineUiObjects.json`
4. 在结果里搜索 `bond.notional`、`bond.validate`、`navigationTree`
5. 需要 Chat 驱动时，按 [mcp-install.md](mcp-install.md) 安装 MCP
6. 用 `MarsJavaDemo/docs/agent-demo-scenarios.md` 中的业务指令验收

## 8. 排障

| 现象 | 处理 |
| --- | --- |
| `Target process is not a Java application` | PID 与进程名不匹配，或进程已退出。用 `jps -l` 重查 |
| `Target java process not found by name` | `debug-single` 的进程名要能匹配 displayName 子串 |
| `MARSJavaEngine jar not found` | 把 Engine JAR 放到 Agent 同目录，或设 `MARS_ENGINE_JAR` |
| Attach 失败 | 确认同一用户、目标不是受保护 JVM；Windows 上不要跨会话 attach |
| 扫描结果为空 | 窗口必须可见；最小化或 headless 扫不到 |
| 握手失败 | 控制服务未开时可忽略；用 swap 文件里的端口直连 WebSocket |
| `unload` 提示没有 HttpPort | 该次注入不是 `debug-single`，改走 WebSocket `UNLOAD_ENGINE` |
| 高亮太多导致卡顿 | 减小 `MARS_HIGHLIGHT_LIMIT` |
