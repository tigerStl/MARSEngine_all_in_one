# MARS Java Engine

中文 | [English](README_en.md)

本仓库包含三个 Maven 模块和一个 Swing Demo：

- `MARSJavaEngineAgent`：可执行注入器（`java -jar`），用于附加到正在运行的 JVM。
- `MARSJavaEngine`：被注入后的引擎，负责启动服务、扫描并操作 UI 对象。
- `MARSJavaMcp`：Cursor / Codex 使用的 Java MCP Server，实现 `doc/mars_java_automation_mcp_methods_spec.md` 中的工具。
- `MarsJavaDemo`：Northstar Capital Markets Workstation，虚构的资本市场桌面应用，用于 MARS UI 自动化演示。

## 构建

需要 JDK 11+。若终端提示 `mvn` 无法识别，本机没有把 Maven 加入 PATH。Windows 可改用仓库自带包装脚本：

```
.\mvnw.cmd -q -DskipTests package
```

已安装 Maven 时，在仓库根目录执行：

```
mvn -q -DskipTests package
```

## 模块产物

- `MARSJavaEngineAgent` 生成 `MARSJavaEngineAgent-1.0.0-shaded.jar`
- `MARSJavaEngine` 生成 `MARSJavaEngine-1.0.0-shaded.jar`
- `MARSJavaMcp` 生成 `MARSJavaMcp-1.0.0.jar`

## 启动 Northstar Demo

`MarsJavaDemo` 是独立 Maven 工程（需要 Java 17+），不在父工程 reactor 中。

进入 `MarsJavaDemo`：

```
mvn clean package
mvn exec:java
```

或打包后：

```
java -jar target/northstar-financial-demo-1.0.0.jar
```

更多说明见 `MarsJavaDemo/README.md`。

## 文档

- `doc/README.md` — 需求、使用说明、MCP 安装索引
- `doc/requirements.md` — Java Engine / Agent 需求与协议
- `doc/usage.md` — 构建、注入、WebSocket 命令
- `doc/mcp-install.md` — 在 Cursor / VS Code 中安装 MARS MCP
- `MARSJavaEngineAgent/README.md`
- `MARSJavaEngineAgent/doc/USAGE.md`

## WebSocket 测试请求

从 `swapDirectory/MarsJavaEngineSwap.json` 读取 `SvcIp` 和 `PortNumber`，然后连接：

```
ws://{SvcIp}:{PortNumber}
```

当前引擎已处理的命令：

扫描全部 UI 对象：
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECTS_ALL",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

卸载引擎：
```
{
  "MessageSource": "TestClient",
  "MessageType": "UNLOAD_ENGINE",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

以下为客户端约定示例，引擎尚未实现：

按鼠标位置取对象：
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECT_BY_MOUSE",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

按坐标取对象：
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECT_BY_XY",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {
    "x": 100,
    "y": 200
  }
}
```
