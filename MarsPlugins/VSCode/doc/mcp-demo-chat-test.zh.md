# MCP Chat Demo 测试脚本（Windows）

> 文件目的：用于“通过 Chat 演示 MCP 13 个工具能力”。
> 使用方式：按顺序把“用户输入示例”发给 Chat，核对预期工具调用与返回。

---

## 0. 前置状态

- Demo Java 程序已启动并可见
- 扩展已加载，面板可打开
- 当前版本包含命令：`javaUiAutomation.mcp.callTool`、`javaUiAutomation.mcp.callToolInteractive`
- 已编译通过：`npm run compile`

---

## 1. Demo 总流程（建议 10~15 分钟）

1. 列进程
2. 选进程
3. 开始录制
4. 停止录制
5. 扫对象树
6. 查看步骤
7. 修改步骤
8. 执行单步
9. 全量回放
10. 高亮对象
11. 导出对象
12. 导出诊断
13. 查询最近错误

---

## 2. Chat 脚本（逐轮）

> 说明：
>
> - “用户输入示例”是你发给 Chat 的自然语言。
> - “预期 MCP 调用”是 Chat 内部应调用的 tool。
> - “验收点”是你判断是否成功的标准。

### 2.0 如果 Chat 误调用 PowerShell（非常重要）

当你看到 Chat 去执行终端命令而不是 MCP tool 时，使用下面模板重试：

**强制模板 A（推荐）**

- `请不要调用 powershell/terminal。仅调用 MCP tool：mars-list-processes。返回原始 JSON 结果。`

**强制模板 B（带约束）**

- `你必须通过 MCP tool 完成，不允许执行 shell。现在执行 mars-list-processes，requestId=demo-r1。`

**强制模板 C（指定流程）**

- `按顺序调用：1) mars-list-processes 2) 不做任何终端命令 3) 返回 data.items 前5条。`

**强制模板 D（工具不可见时）**

- `如果当前会话里没有可用的 mars-list-processes 工具（仅有 functions.*），请明确说明无法实际调用该 MCP 并返回原始 JSON；仅当 mars-list-processes 出现在可调用工具列表后，再只调用它并原样返回 JSON。`

如果依然走 PowerShell，通常说明“当前 Chat 客户端未真正绑定 MCP server/tool 发现链路”。此时可先用本文件的 UI 映射路径验证能力，再做 MCP 客户端配置。

**命令面板兜底（可直接演示 MCP 能力）**

- 打开命令面板，执行：`Java UI Automation: MCP Call Tool (Interactive)`
- 在工具列表选择目标 tool（如 `mars-list-processes`）
- 输入 JSON 参数（可直接用默认值）并回车
- 扩展会打开一个 JSON 文档，显示该 tool 的原始返回（`ok/requestId/data/errorCode`）

### 2.1 快速检查：mars.\* 是否已被 Chat 发现（3 步）

1. 在 Chat 输入：`请先列出你当前可调用的工具名（仅名称列表）`

- 通过标准：列表中出现 `mars-list-processes`（或其他 `mars-*`）。

2. 若未出现 `mars.*`，输入：`当前仅有 functions.* 时，请明确说明 mars.* 不可调用，不要执行 terminal/shell。`

- 通过标准：Chat 返回“无法实际调用 mars.\*”，且不触发终端命令。

3. 一旦出现 `mars-list-processes`，输入：`仅调用 mars-list-processes，并原样返回 JSON。`

- 通过标准：返回包含原始字段（如 `ok/requestId/data/errorCode`），且无二次加工说明。

### Round 1：列出 Java 进程

**用户输入示例**

- `请列出当前 Java 进程`

**预期 MCP 调用**

- `mars-list-processes`

**预期返回关键字段**

- `ok=true`
- `data.items` 为数组，至少包含 `pid/displayName`

**验收点**

- 能看到 demo 程序对应 PID。

---

### Round 2：选择目标进程

**用户输入示例**

- `选择进程 pid=12345`

**预期 MCP 调用**

- `mars-select-process`，input: `{ "pid": 12345 }`

**预期返回关键字段**

- `ok=true`
- `data.selectedPid=12345`

**验收点**

- 后续 `getObjectTree` 不再报 `MARS_E_NO_PROCESS_SELECTED`。

---

### Round 3：开始录制（新增）

**用户输入示例**

- `开始录制，pid=12345`

**预期 MCP 调用**

- `mars-start-record`，input: `{ "pid": 12345 }`

**预期返回关键字段**

- `ok=true`
- `data.status="recording"`
- `data.pid=12345`

**验收点**

- 面板进入录制状态：`Record&Replay` 按钮显示 Stop 语义，进程下拉被禁用，Execute/Spy 等按钮禁用。

---

### Round 4：停止录制（新增）

**用户输入示例**

- `停止录制`

**预期 MCP 调用**

- `mars-stop-record`

**预期返回关键字段**

- `ok=true`
- `data.status="stopped"`
- `data.stepCount` 为数字

**验收点**

- 面板退出录制状态：进程下拉恢复可用，步骤列表更新，日志出现 recording stopped 提示。

---

### Round 5：获取对象树

**用户输入示例**

- `扫描对象树`

**预期 MCP 调用**

- `mars-get-object-tree`，input: `{ "refresh": true }`

**预期返回关键字段**

- `ok=true`
- `data.roots` 数组（可为空但通常非空）

**验收点**

- 能从返回中看到窗口/组件层级。

---

### Round 6：读取测试步骤

**用户输入示例**

- `读取当前测试步骤`

**预期 MCP 调用**

- `mars-get-steps`

**预期返回关键字段**

- `ok=true`
- `data.steps` 数组

**验收点**

- 步骤数量与面板一致。

---

### Round 7：修改某一步（参数/期望）

**用户输入示例**

- `把第 3 步 expected 改成 Deal JSON`

**预期 MCP 调用**

- `mars-update-step`
- input 示例：

```json
{
  "index": 2,
  "patch": {
    "assertValue": "Deal JSON"
  }
}
```

**预期返回关键字段**

- `ok=true`
- `data.step` 返回更新后的步骤

**验收点**

- 面板中第 3 步 Expected 同步变化。

---

### Round 8：执行单步

**用户输入示例**

- `执行第 3 步`

**预期 MCP 调用**

- `mars-execute-step`，input: `{ "index": 2 }`

**预期返回关键字段**

- `ok=true`
- `data.status` = `success` 或 `failed`
- `data.durationMs` 为数字

**验收点**

- UI 有对应动作；失败时返回明确错误。

---

### Round 9：执行回放

**用户输入示例**

- `从第1步到第5步回放`

**预期 MCP 调用**

- `mars-run-replay`
- input 示例：

```json
{ "fromIndex": 0, "toIndex": 4, "strictParent": true }
```

**预期返回关键字段**

- 成功：`data.status=done`
- 失败：`data.status=failed` 且可能有 `failedIndex/error`

**验收点**

- 回放结果可解释（尤其失败位置）。

---

### Round 10：高亮对象

**用户输入示例**

- `高亮对象 javaType=javax.swing.JButton name=okButton`

**预期 MCP 调用**

- `mars-highlight-object`
- input 示例：

```json
{
  "objectKey": {
    "javaType": "javax.swing.JButton",
    "name": "okButton"
  }
}
```

**预期返回关键字段**

- `ok=true`
- `data.message` 包含高亮坐标信息

**验收点**

- 屏幕出现高亮框（Windows）。

---

### Round 11：导出对象

**用户输入示例**

- `导出对象为 json，包含 parent`

**预期 MCP 调用**

- `mars-export-objects`
- input: `{ "format": "json", "includeParents": true }`

**预期返回关键字段**

- `ok=true`
- `data.filePath` 有效路径

**验收点**

- 路径文件存在且可打开。

---

### Round 12：导出诊断

**用户输入示例**

- `导出诊断包，包含日志`

**预期 MCP 调用**

- `mars-export-diagnostics`
- input: `{ "includeLogs": true }`

**预期返回关键字段**

- `ok=true`
- `data.filePath` 目录存在

**验收点**

- 目录内至少有 `summary.json` 与日志文件。

---

### Round 13：查询最近错误

**用户输入示例**

- `给我最近 20 条错误`

**预期 MCP 调用**

- `mars-get-last-errors`
- input: `{ "limit": 20 }`

**预期返回关键字段**

- `ok=true`
- `data.items` 为数组，元素包含 `ts/scope/message`

**验收点**

- 至少能定位最近失败原因。

---

## 3. 失败用例脚本（建议至少跑 2 条）

### 失败用例 A：未选进程先执行

**用户输入示例**

- `执行第 1 步`

**预期**

- `ok=false`
- `errorCode=MARS_E_NO_PROCESS_SELECTED`

---

### 失败用例 B：非法下标

**用户输入示例**

- `执行第 9999 步`

**预期**

- `ok=false`
- `errorCode=MARS_E_STEP_INDEX_INVALID`

---

## 4. 演示通过标准

- 13 个工具至少各成功 1 次调用
- 至少 2 个失败用例返回正确错误码
- 用户可通过 chat 完成“选进程→扫描→改单步→执行→回放→导出”闭环

---

## 5. 演示记录模板（复制填写）

- 时间：
- Demo 程序：
- 成功工具：
- 失败工具：
- 典型错误码：
- 待优化点（最多3条）：

---

## 6. 如何在 VS Code 中注册 MCP（测试环境）

> 关键区分：
>
> - 本仓库已完成的是“扩展命令注册”（`javaUiAutomation.mcp.callTool` / `javaUiAutomation.mcp.callToolInteractive`）。
> - Chat 能直接调用 `mars.*`，还需要“Chat 客户端可发现的 MCP server 注册”。

### 6.1 扩展内注册（已完成）

- 命令路由实现：见 [src/extension.ts](src/extension.ts#L82) 与 [src/extension.ts](src/extension.ts#L196)
- 命令贡献/激活：见 [package.json](package.json#L1)
- 本地验证：命令面板执行 `Java UI Automation: MCP Call Tool (Interactive)`

### 6.2 Chat 可发现 MCP server（测试环境必须）

在你的 Chat/MCP 客户端里新增一个 `mars` server（`stdio`），并配置 server 启动命令。

概念模板（字段名按你的客户端实际 schema 调整）：

```json
{
  "servers": {
    "mars": {
      "transport": "stdio",
      "command": "node",
      "args": ["C:/work/MARS/MarsPlugins/VSCode/out/mcp-server.js"],
      "env": {
        "MARS_WORKSPACE": "C:/work/MARS/MarsPlugins/VSCode"
      }
    }
  }
}
```

说明：当前仓库已提供最小可运行入口 `out/mcp-server.js`（由 `src/mcp-server.ts` 编译生成），可先用于打通 `mars-list-processes`。其余工具可按同样模式逐步补齐。

### 6.2.1 扩展安装后“自动注册 MCP server”应怎么做（代码侧）

不是自注册；要在扩展 `activate()` 里主动注册 `McpServerDefinitionProvider`。核心流程：

1. 在 `package.json` 增加 provider 贡献（`contributes.mcpServerDefinitionProviders`）
2. 在 `activate()` 调用 `vscode.lm.registerMcpServerDefinitionProvider(...)`
3. 在 `provideMcpServerDefinitions()` 返回 `stdio/http` 的 server 定义

示例（放在 [src/extension.ts](src/extension.ts) 的 `activate()` 中）：

```ts
const vsAny = vscode as any;
if (
  vsAny.lm?.registerMcpServerDefinitionProvider &&
  vsAny.McpStdioServerDefinition
) {
  context.subscriptions.push(
    vsAny.lm.registerMcpServerDefinitionProvider("mars.mcp-provider", {
      provideMcpServerDefinitions: async () => {
        const server = new vsAny.McpStdioServerDefinition({
          label: "mars-local",
          command: "node",
          args: [path.join(context.extensionPath, "out", "mcp-server.js")],
          cwd: vscode.Uri.file(context.extensionPath),
          env: { MARS_WORKSPACE: context.extensionPath },
          version: "0.1.0",
        });
        return [server];
      },
    }),
  );
}
```

注意：

- 这段“注册代码”只负责把 server 暴露给 Chat；真正可调用还取决于 `out/mcp-server.js` 是否存在且可启动。
- 若用户 VS Code 版本不支持该 API，需保留你当前的 `javaUiAutomation.mcp.callToolInteractive` 作为 fallback。

### 6.2.2 是否需要“特殊注册程序”？

通常不需要独立注册程序；扩展激活时注册 provider 即可。

但需要一个可执行的 MCP server 入口（例如 `node out/mcp-server.js`）。

命令行自检：

```powershell
# 1) 编译扩展输出
npm run compile

# 2) 启动 MCP server
npm run start:mcp
```

### 6.3 测试环境最短验证链路

1. 重载窗口后，在 Chat 里先让模型列可调用工具名。

- 看到 `mars-list-processes` 才表示 MCP server 已被发现。

2. 发起最小调用：`仅调用 mars-list-processes，并原样返回 JSON。`
   - 期望：返回 `ok/requestId/data/errorCode` 结构。

3. 若仍不可见：执行本文件第 2.0 节的“模板 D + 命令面板兜底”。
