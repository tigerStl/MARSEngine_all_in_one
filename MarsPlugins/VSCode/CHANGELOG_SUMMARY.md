# Java UI Automation - 修改摘要 (Summary of Changes)

## 2026-02-23 Beta 分支与收费能力准备摘要

### Beta 准备

- 版本调整为 `0.9.0-beta.1`，用于 Beta 阶段验证与发布准备。
- 增加根脚本：`npm run start:license-server`。

### 新增 License Server（最简实现）

- 新增目录：`license-server/`
  - `server.js`：最小 HTTP 服务（签发/校验/吊销/健康检查）
  - `.env.example`：部署参数模板
  - `README.md`：接口与启动说明
  - `data/.gitkeep`：本地数据目录占位

### 隐私增强（默认启用）

- 数据最小化：默认不持久化原始邮箱等 PII。
- 伪匿名化：`customerEmail` 转换为带盐哈希 `customerRef`。
- 吊销安全：仅存储哈希化 `licenseId`。
- 审计最小化：仅记录 `ts/action/ok/requestId/subjectHash/reason`。
- 安全头与缓存控制：`no-store` + `nosniff` + `DENY` + `no-referrer`。
- 请求体限制：`64KB` 上限，降低滥用风险。

### 文档补充

- 新增：`doc/license-server-privacy_zh.md`（部署、接口、隐私与生产建议）
- README 增加收费准备说明与 license-server 启动入口。

## 2026-02-22 MCP 联调与文档交付摘要

### MCP 与扩展链路增强

- 扩展侧新增 MCP bridge 与 provider 注册能力，支持本地 `stdio` server 暴露给 Chat 客户端发现。
- 增加诊断命令：`javaUiAutomation.mcp.showStatus`、`javaUiAutomation.mcp.probe`。
- `mcp.callTool` 路由扩展到 13 个工具，新增录制工具：`mars.startRecord`、`mars.stopRecord`。

### MCP Server 协议与工具能力

- 新增 `src/mcp-server.ts`：实现 `initialize` / `tools/list` / `tools/call`。
- 工具名统一为合规格式 `mars-*`，并保留 `mars.*` / `mars_*` 别名兼容。
- 输出协议采用 JSON 行格式，同时兼容 Content-Length 帧输入解析。
- 新增协议自检脚本 `scripts/mcp-stdio-smoke.js`，覆盖初始化、工具列表与调用闭环。

### 录制状态一致性修复（MCP 路径）

- 修复 MCP 选进程后面板状态不同步：新增 `setSelectedProcess` 消息，确保下拉框选中与 change 等效逻辑执行。
- 修复 MCP 启动录制时 UI 状态不同步：后端新增 `mcpStartRecord` / `mcpStopRecord`，复用现有 `recordingStarted/Stopped` 状态流。
- 前端新增 `applySelectedProcess` 统一处理流程，保证按钮禁用/启用状态一致。

### 文档交付（中英）

- 新增英文文档：
  - `doc/mcp-demo-test_en.md`
  - `doc/mcp-demo-chat-test.en.md`
- 更新中文文档并统一为 13 工具口径，补齐录制开始/停止步骤与验收标准：
  - `doc/mcp-demo-test_zh.md`
  - `doc/mcp-demo-chat-test.zh.md`

### 构建与验证

- `npm run compile`：通过
- `npm run test:mcp-smoke`：通过
- `mvn clean package`（java）：通过
- `dotnet build -c release`（ProcessInfo）：通过

## 2026-02-20 Alpha 0.9 发布摘要

### 本版本定位

- 版本号：`0.9.0-alpha`
- 发布阶段：Alpha（功能收敛与产品化加固阶段）

### 关键能力完成

- 回放执行链路稳定化：`ClickButton`、`SelectTreeList`、`SearchAndUpdate` 等关键关键词可回放。
- `SelectTreeList` 严格失败策略：目标非 `JTree` 或路径无效时，立即失败并终止回放。
- `SearchAndUpdate` 可靠性增强：进入 cell 编辑态后增加稳定等待，再执行键盘输入。
- Replay 前高亮能力：支持配置开关 `IsHighlightObjectWhileReplay`，执行 keyword 前先高亮对象。

### 录制与可视化编辑增强

- 修复 object tree 点击误新增 visual 节点（仅录制中允许新增）。
- Visual 节点支持 `parameter` 字段：显示、编辑、保存、回放全链路生效。
- Test Steps 中 `Data` 列可直接编辑并同步执行数据。
- 表格编辑体验：`Enter` 提交、`Esc` 回滚。

### 配置与分发

- 新增可随 JAR 分发配置：`marsJavaAgent-config.json`。
- 配置值标准化为布尔值：
  - `"IsHighlightObjectWhileReplay": true`
- 扩展在复制临时 agent JAR 时，会同步复制配置文件，确保扫描/录制/回放一致生效。

### 产品化（P0）落实

- 文档口径统一为“支持回放执行”（中/英 README 同步）。
- 新增标准 `CHANGELOG.md`。
- 新增 CI 门禁：Node 编译、基础测试、Java 构建、.NET 构建。

### 构建验证

- `npm run compile`：通过
- `mvn -pl marsJavaAgent -am package -DskipTests`：通过
- `dotnet build -c Release`（ProcessInfo）：通过

## 2026-02-19 增量总结

### 核心能力增强

- JTable 录制增强：新增 `SearchAndClick`、`SearchAndUpdate`、`SelectPopupMenu` 录制链路，支持单元格级参数与条件列数据生成。
- JTable 回放增强：`SearchAndUpdate` 按 para/data 解析后执行定位、匹配、更新、提交（回车），并在失败时返回明确错误。
- 事件链路打通：Java Agent → Recorder → Adapter → Panel 全链路支持 `parameter` 字段，避免步骤参数丢失。

### 面板与可维护性

- 新增 Save/Load 按钮（独立配色），支持步骤导出/导入 JSON。
- Load 前增加“清空当前步骤”确认；加载后会清空并重绘可视化流程。
- 保存文件写入 MARS 元信息（marker/copyright/purpose/version/generatedAt/md5）；加载阶段按最新要求不再校验 md5。

### 诊断与稳定性

- 增加可配置录制步骤调试日志：`loaniq.recordingStepDebugLog`（默认 `false`），输出 keyword/parameter/data。
- 修复关键词降级风险：补齐 fallback 白名单，避免 `SearchAndUpdate`/`SearchAndClick`/`SelectPopupMenu` 被错误回退为 `Click`。

### 构建验证

- Extension：`npm run compile` 通过。
- Java：`mvn clean package` / `mvn -pl marsJavaAgent -DskipTests package` 通过。

## 1. 数据与输出路径

- **scanedfiles**：扫描与脚本统一存放在插件安装目录下的 `scanedfiles/`，不再依赖工作区文件夹
- **按进程缓存**：扫描结果同时写入 `objects.json` 与 `objects-<pid>.json`，便于按应用切换加载
- **extension.ts**：selectProcess、generateScript 等命令均使用 `getScanDir(context)`（scanedfiles）

## 2. 面板状态持久化

- 使用 `workspaceState` 保存进程列表、对象、步骤、日志
- 切换 tab 后再次打开面板时，通过 `restoreState` 消息恢复状态
- 日志内容用防抖（约 800ms）写入状态

## 3. 面板 UI 调整

- **工具栏**：Window Spy →「Java Applications」；增加进程下拉框（Combobox）；删除 Test/works，增加「Generate Test Steps」「Execute」图标按钮
- **左侧**：删除进程列表与搜索框；仅保留「--->Object List」标签与对象树；Combobox 选择变更时触发扫描（原双击进程行为）
- **Object Info 与 Test Steps**：并排布局，Object Info 约 1/3 宽，Test Steps 约 2/3，两区域均可滚动
- **Object Info**：增加 x,y,w,h 一行与 Visible 字段
- **对象树**：扫描后发送 `objectTree`，面板按层级递归渲染（缩进）；visible=false 的节点显示为红色

## 4. 高亮（Highlight）

- **坐标**：优先使用 **screenBounds**（屏幕绝对坐标）；无则用 bounds（相对），并打日志说明
- **实现**：改为 C# 程序 **HighlightOverlay**（WinForms），参数 `x y width height`（屏幕坐标），绘制红框并闪烁 3 次
- **扩展**：`runHighlightOverlay(extensionPath, x, y, w, h)` 替代原 Java `loadHighlightAgent`；仅 Windows 支持

## 5. Java 相关

- **JAVA_HOME**：扩展通过 `JAVA_HOME/bin/java`（Windows 下 `java.exe`）调用，避免把目录当可执行文件
- **Agent 加载**：加载 ui-scanner-agent 前复制 JAR 到临时文件再加载，避免同一进程重复加载同一路径报错
- **Java Agent 日志**：agent-loader、ui-scanner-agent 使用 `AgentLogUtil`，日志写入各 JAR 同目录下的 `javaagentLog/`（如 `agent-loader.log`、`ui-scanner-agent.log`）
- **ui-scanner-agent**：扫描输出增加 `screenBounds`（getLocationOnScreen + getSize）与 `visible`（isVisible）

## 6. 对象与树结构

- **types**：增加 `Bounds`、`UIObjectTree`（含 `children`）；ElementIdentifier 增加 bounds、screenBounds、visible
- **objectConverter**：ScannedNode 支持 screenBounds、visible；增加 `convertScanToUIObjectTree(scan)` 输出树形结构
- **panel**：支持 `objectTree` 消息；有树时按树渲染，否则按扁平列表渲染；restoreState 仅恢复扁平 objects

## 7. 新增/重要文件

- **HighlightOverlay/**：C# WinForms 项目，`HighlightOverlay.exe x y width height` 在屏幕坐标绘制红框闪烁
- **agentLoader.ts**：`loadAgentAndScan`（含临时 JAR 复制）、`runHighlightOverlay`；无 Java highlight-agent 调用
- **java/agent-loader**、**ui-scanner-agent**：各含 `AgentLogUtil.java`，日志写 javaagentLog
- **java/highlight-agent**：保留但扩展侧已不再使用，高亮由 C# HighlightOverlay 负责

## 8. 文档

- README.md、doc/README_zh.md、doc/README_en.md：补充面板、scanedfiles、javaagentLog、高亮、对象树、构建与运行说明

## 构建与运行

- **扩展**：`npm install`、`npm run compile`
- **Java**：`cd java && mvn package`（agent-loader、ui-scanner-agent、highlight-agent）
- **ProcessInfo**：`dotnet publish -c Release`
- **HighlightOverlay**：`cd HighlightOverlay && dotnet publish -c Release`
