# TigerClawdEntry

`TigerClawdEntry` is a unified AI Runtime Entry and Installer Assistant for VS Code / Cursor.

## English

### Features

- Environment overview for AI stacks: `LLM`, `Agent`, `Code`, `Vector DB`, `Tools`
- Scenario-based recommendations and setup flow
- Dashboard, Setup Wizard, Scenario Center, Health Center, and Agent Console

### Install

Install from Marketplace, or install from VSIX:

1. Open `Extensions: Install from VSIX...`
2. Select the `.vsix` package

### Commands

Open Command Palette (`Ctrl+Shift+P`) and run:

- `TigerClawdEntry: Open Dashboard`
- `TigerClawdEntry: Open Agent Console`
- `TigerClawdEntry: Reveal Agent Console`
- `TigerClawdEntry: Refresh Environment Status`
- `TigerClawdEntry: Open Health Center`
- `TigerClawdEntry: Open Setup Wizard`
- `TigerClawdEntry: Open Scenario Center`

### Quick Start

1. Run `TigerClawdEntry: Open Dashboard`
2. Run `Refresh Environment Status`
3. Open `Setup Wizard` and choose a scenario
4. Run `Health Center` checks
5. Use `Open Agent Console` for prompt-driven actions

### Notes

- Some installer/validator flows are mock behavior in current V1.

### Troubleshooting

- If a command does not respond, reload the window and retry.
- If Agent Console looks stale or blank, close it and open again.
- `Found unexpected service worker controller` is usually a host webview reuse warning.

---

## 中文

### 功能简介

- 提供 AI 技术栈环境总览：`LLM`、`Agent`、`Code`、`Vector DB`、`Tools`
- 提供场景化推荐与安装流程
- 包含 Dashboard、Setup Wizard、Scenario Center、Health Center、Agent Console

### 安装方式

可通过 Marketplace 安装，或通过 VSIX 安装：

1. 执行 `Extensions: Install from VSIX...`
2. 选择 `.vsix` 安装包

### 主要命令

打开命令面板（`Ctrl+Shift+P`）后执行：

- `TigerClawdEntry: Open Dashboard`
- `TigerClawdEntry: Open Agent Console`
- `TigerClawdEntry: Reveal Agent Console`
- `TigerClawdEntry: Refresh Environment Status`
- `TigerClawdEntry: Open Health Center`
- `TigerClawdEntry: Open Setup Wizard`
- `TigerClawdEntry: Open Scenario Center`

### 快速上手

1. 执行 `Open Dashboard`
2. 执行 `Refresh Environment Status`
3. 打开 `Setup Wizard` 选择场景模板
4. 打开 `Health Center` 进行检查
5. 打开 `Agent Console` 执行提示词任务

### 说明

- 当前 V1 版本中，部分安装/校验流程仍为 mock 行为。

### 常见问题

- 命令无响应：重载窗口后重试。
- Agent Console 显示异常或空白：关闭后重新打开。
- 出现 `Found unexpected service worker controller`：通常为宿主 webview 复用提示。
