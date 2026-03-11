# Java UI Automation 使用手册（中文）

## 1. 适用范围

本手册用于发布版用户，覆盖以下内容：

- 安装与环境准备
- 扫描、录制、回放完整流程
- 常见关键字说明
- License（测试版/收费版/受限版）规则
- 常见问题与诊断导出

---

## 2. 环境要求

- VS Code `1.85+`
- Node.js `18+`
- JDK `17+`（必须是 JDK，不是 JRE）
- .NET SDK `8+`（用于 `ProcessInfo`）
- Maven `3.6+`

建议提前确认：

- `JAVA_HOME` 已指向可用 JDK
- `JAVA_HOME/bin/jcmd(.exe)` 可执行（用于更准确显示 Java 进程）

---

## 3. 安装与构建

在扩展工程根目录执行：

```bash
npm install
npm run compile
cd java && mvn -q -DskipTests package
cd ..
cd ProcessInfo && dotnet publish -c Release
cd ..
```

---

## 4. 面板与基本操作

打开面板：`Java UI Automation`（底部 Panel）。

工具栏主要按钮：

- `Java Applications`：扫描 Java 进程列表
- `Record & Replay`：开始/停止录制
- `Execute`：回放当前 Test Steps
- `Save/Load`：保存/加载步骤文件
- `Diag`：导出诊断包
- `Refresh`：刷新对象列表

---

## 5. 扫描流程

1. 启动目标 Java 应用（Swing/AWT）
2. 点击 `Java Applications`
3. 在下拉中选择进程
4. 双击进程执行扫描
5. 左侧对象树出现后，可单击对象查看 `Object Info`
6. 双击对象可高亮（红框闪烁）

说明：进程下拉会显示来源标记：

- `[jcmd]`：来自 `jcmd -l`（推荐）
- `[fallback]`：回退识别

---

## 6. 录制与回放

### 6.1 录制

1. 选择目标进程
2. 点击 `Record & Replay` 开始
3. 在目标应用执行操作
4. 点击 `Record & Replay` 停止
5. 步骤自动进入 `Test Steps`

### 6.2 回放

1. 在 `Test Steps` 检查/编辑步骤
2. 点击 `Execute` 执行
3. 可单步执行（Action 列按钮）

---

## 7. 关键字说明（常用）

- `FillEdit`：输入文本
- `SelectDropList` / `SelectDropDown`：下拉选择
- `SelectTreeList`：树节点选择
- `SelectTab`：标签页选择
- `SelectMenuItem` / `SelectPopupMenu` / `SelectListItem`：菜单或列表项选择
- `SetRadioBox`：单选框
- `SetCheckBox`：复选框
- `ClickAT`：按当前位置点击（含右键）

右键规则：

- 右键触发业务步骤时，会额外追加一条 `ClickAT`
- `Select*` 相关步骤会在 `parameter` 中追加 `rightclick`
- 回放时优先移动到选中项位置再执行右键

菜单稳定性规则：

- `SelectMenuItem/SelectPopupMenu/SelectListItem` 回放后默认等待 `1s`

---

## 8. License 说明（发布）

### 8.1 类型

- `TEST`：测试版
- `PAID`：收费版
- `TRIAL_LIMITED`：下载后的受限版

### 8.2 受限版规则

- 前 `7` 天：回放不限步数
- 超过 `7` 天：录制可超过 `30` 步，但回放最多 `10` 步
- 超过 `10` 步时：提示升级付费

### 8.3 收费标准

- 美国：`$4.99`
- 中国：`5 元`

### 8.4 测试版配额

- 总计 `400`
- 美国 `200`、中国 `200`
- 由 License Server 统一控制

### 8.5 License 状态显示

面板顶部会显示：

- 类型（TEST/PAID/TRIAL）
- 区域（US/CN/GLOBAL）
- 价格提示
- Tooltip 中的试用限制与测试池余量

---

## 9. License Server（最简）

默认地址（可配置）：

- `loaniq.licenseServerUrl`（默认 `http://127.0.0.1:8787`）

关键接口：

- `GET /v1/license/client-state`
- `GET /v1/license/policy`
- `GET /v1/license/declaration?lang=zh|en`
- `POST /v1/license/test/claim`（管理员）

客户端会自动拉取：

- `scanedfiles/license.latest.json`
- `scanedfiles/license.declaration.latest.txt`

---

## 10. 输出文件与目录

常见输出（扩展目录下）：

- `scanedfiles/objects.json`
- `scanedfiles/script.json` / `script-*.json`
- `scanedfiles/processes-latest.json`
- `scanedfiles/license.latest.json`
- `scanedfiles/license.declaration.latest.txt`

### 10.1 MARS Java Agent 引擎日志（可选配置）

用于排查“某控件被忽略”“分类不符合预期”等问题时查看引擎内部日志。

- **默认位置**：系统临时目录下的 `javaUIAutomationLog`（Windows 一般为 `%TEMP%\javaUIAutomationLog\`），**默认文件名**：`MARSJavaEngineLog_yyyyMMdd.log`（按日一个文件，同一天多次运行会追加）。
- **配置方式**（在**被 attach 的目标 Java 进程**的 JVM 参数中设置，若由扩展通过 attach 加载则需在扩展/启动脚本侧配置传入）：
  - **mars.javaagent.log.dir**：日志目录的绝对路径。
  - **mars.javaagent.log.file**：日志文件名（不含路径）；不设则使用默认的 `MARSJavaEngineLog_yyyyMMdd.log`。
- 示例：`-Dmars.javaagent.log.dir=D:\Logs\JavaUI` 或 `-Dmars.javaagent.log.file=myagent.log`

---

## 11. 常见问题

### Q1: 进程下拉只显示 `java -jar`，看不到真实名称？

- 优先确认下拉条目是否带 `[jcmd]`
- 检查 `JAVA_HOME/bin/jcmd(.exe)` 是否可运行
- 若仍为 `[fallback]`，可先用 `Diag` 导出诊断包排查

### Q2: 菜单步骤回放后菜单未关闭？

- 当前版本已加入真实鼠标模拟 + 默认 1 秒等待
- 如仍复现，请提供诊断包

### Q3: 底部一直显示 `Java: Activating...`？

- 运行 `Java: Clean Java Language Server Workspace`
- 确认 JDK 版本与 Java 扩展状态

---

## 12. 诊断导出

点击 `Diag` 可导出：

- 面板日志
- 步骤文件
- 最近录制日志
- 运行配置摘要

用于提交问题时的最小复现材料。

