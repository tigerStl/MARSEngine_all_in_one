# TigerClawdEntry 提交总结

## Agent Console 重构与修复

### 1. 布局调整
- **Sidebar**：仅保留导航与轻量操作；从 Dashboard 顶部 Tab 中移除「Agent」Tab。
- **Agent Console**：改为在主编辑区以 **WebviewPanel** 打开（`createWebviewPanel`），标题为 "TigerClawdEntry Agent Console"。
- **入口**：Sidebar Overview 英雄区「打开代理控制台」、命令面板 `Open Agent Console` / `Reveal Agent Console`。

### 2. 新增与修改文件
- `src/views/AgentConsolePanel.ts`：面板管理，消息处理 agentAction → runAgentTask → postMessage(agentResult)。
- `src/templates/agentConsoleHtml.ts`：Agent Console 全屏 HTML；初始数据经 **Base64** 传入，避免脚本内嵌 JSON 导致语法错误。
- `src/commands/openAgentConsole.ts`：注册 openAgentConsole、revealAgentConsole。
- `package.json`：新增上述命令及 activationEvents。
- `src/extension.ts`：创建 AgentConsolePanel，注入 logger，注册 Agent Console 命令。

### 3. 语法错误与安全修复（Unexpected identifier 'tce'）
- **tce**：TigerClawd Entry 缩写，用作所有 UI 的 class/id 前缀（如 `tce-ac-root`、`tce-ac-run`）。
- 报错原因：脚本中嵌入的字符串被提前结束，后续 `class="tce-..."` 被当代码解析。
- **处理**：初始数据改为 Base64 传入（`atob('${dataB64}')`）；HTML 中所有 `${lang.xxx}` 经 `escapeHtmlAttr` / `escapeHtmlText` 转义，避免 `"` 与 `</script>` 破坏页面。

### 4. 调试与日志
- 打开 Agent Console 时，将 Base64 解码后的 JSON 写入 `c:\temp\cursorDebugger.txt`（便于检查 payload）；写失败时在扩展控制台打印错误与 stack。
- AgentConsolePanel 收到 agentAction 时通过 LoggerService 打日志（输出 → TigerClawdEntry）。
- 按钮点击后结果区先显示 "Sending…"，收到 agentResult 后更新计划/日志/结果。

### 5. 多语言与文案
- locale 新增：openAgentConsole、revealAgentConsole、agentRuntimeStatus、agentRunning（中英文）。

### 6. 已知问题（README）
- "Found unexpected service worker controller" 为编辑器宿主行为，与扩展无关。
- Agent Console 在 **VS Code** 中正常；在 **Cursor** 中 Webview DevTools 可能空白、postMessage 可能异常，属 Cursor 对 Webview 支持差异。

### 7. Dashboard 修改
- `dashboardHtml.ts`：移除 Agent Tab 与 agent 面板；Overview 增加「打开代理控制台」按钮；处理 `openAgentConsole` 消息并执行命令。
- `TigerClawdSidebarProvider.ts`：处理 openAgentConsole；移除对 agentAction 的响应（改由 AgentConsolePanel 处理）。
