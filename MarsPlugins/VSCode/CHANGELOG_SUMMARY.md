# Java UI Automation - 修改摘要 (Summary of Changes)

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
