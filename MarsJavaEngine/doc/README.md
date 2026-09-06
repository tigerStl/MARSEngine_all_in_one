# MARS Java Engine 文档

本目录是 `MarsJavaEngine` 仓库的需求与使用说明。

| 文档 | 内容 |
| --- | --- |
| [requirements.md](requirements.md) | Java Engine / Agent 架构、能力范围与协议需求 |
| [usage.md](usage.md) | 构建、注入、卸载、WebSocket 命令与排障 |
| [mcp-install.md](mcp-install.md) | 在 Cursor / VS Code 中安装 MARS MCP |
| [mars_java_automation_mcp_methods_spec.md](mars_java_automation_mcp_methods_spec.md) | MCP 工具规格 |

相关模块源码：

- `MARSJavaEngineAgent`：可执行注入器
- `MARSJavaEngine`：被注入到目标 JVM 后的引擎
- `MARSJavaMcp`：MCP stdio 服务
- `MarsJavaDemo`：Northstar 桌面 Demo，用于联调
