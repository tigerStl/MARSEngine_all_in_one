# TigerClawdEntry 提交总结

## 功能与修复概览

### 1. 配置与头部按钮
- **配置按钮**：修复模块「配置」点击无反应；通过延迟 `showInputBox`（100ms）并设置 title，确保在 webview 焦点下也能弹出。
- **头部按钮**：补全 `headerAction` 处理——刷新（刷新环境）、校验（运行全量健康检查）、向导（打开安装向导）。

### 2. 环境检查 (node/npm/git)
- **Windows**：使用 shell 执行 `node`/`npm`/`git`，解决 `npm.cmd` 在 `spawn(..., { shell: false })` 下 ENOENT 的问题。
- **macOS**：同样使用 shell 执行，与终端环境一致。
- **错误信息**：失败时分别输出 node/npm/git 的 stderr，便于排查。

### 3. code-cli / cursor-cli 检查
- **Windows**：使用 shell 执行；PATH 失败时回退到 app 的 `bin`，尝试 `../../bin` 与 `../bin` 下的 `code.cmd`/`cursor.cmd`。
- **macOS**：使用 shell 执行；回退时尝试 `appRoot/bin`、`../bin`、`../../bin` 下的 `code`/`cursor` 脚本。
- 确保已安装但未加入 PATH 时也能通过检查。

### 4. 工具执行服务 (ToolExecutionService)
- `runCommand` 增加可选参数 `useShell`；为 true 时在系统 shell（Windows cmd / Unix sh）中执行，正确解析 .cmd 与 PATH。
- 命令行参数带空格或引号时正确转义（Windows `""`，Unix `\"`）。

---

## 涉及文件（主要）
- `src/views/TigerClawdSidebarProvider.ts`：headerAction、moduleAction(configure)、runInstallCheck(env/code-cli/cursor-cli)、useShell 与 bin 回退路径。
- `src/services/agent/ToolExecutionService.ts`：runCommand(..., useShell) 与 shell 下引号转义。
