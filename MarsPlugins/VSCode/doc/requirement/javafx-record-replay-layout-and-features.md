## JavaFX 录制 / 回放布局结构与功能说明

### 1. 总体结构

- **Agent 端（JavaFX 应用内）**
  - `FxRecordSupport`：JavaFX 事件录制与语义步骤生成。
  - `FxReplaySupport` / `FxReplayResolver`：根据对象标识解析 JavaFX 控件并通过 `Robot` 执行回放。
  - `RecordAgent`：统一入口，负责配置加载、与 VS Code 扩展的 WebSocket 通信、步骤发送与回放控制。
- **VS Code 扩展端**
  - `panel.html`：展示对象树、可视化节点、测试步骤表格，以及各种交互按钮。
  - `panelProvider.ts`：连接 Webview 与扩展逻辑，处理消息、日志与回放调用。
  - `agentLoader.ts`：启动/附加 Java Agent，建立与 Agent 的 WebSocket 连接，发送回放请求。
  - `recording/stepAdapter.ts`：将录制得到的语义步骤转换为测试步骤（TestScriptStep）。

### 2. 录制相关布局与功能

- **对象树（Object Tree）**
  - 数据来源：
    - AWT/Swing：`AgentProtocol` 扫描组件树。
    - JavaFX：`FxScanner` 扫描 `Window.getWindows()` → `Scene.getRoot()` 下的节点树。
  - 节点标识（`ElementIdentifier`）包含：
    - `javaType`：控件实际 Java 类型。
    - `javaName`：控件 ID / 标题（JavaFX 中优先使用 `getId()` / `getTitle()`）。
    - `javaTypePath` / `javaNamePath`：从根到该节点的类型 / 名称路径（用于消歧）。
    - `text` / `caption` / `title` / `value`：用于展示和语义定位的文本。
    - `screenBounds`：用于可视化高亮和对象树展示的绝对坐标（x, y, width, height）。
  - 功能：
    - 在对象树上点击节点，可以在右侧 Visual 区域中高亮显示对应控件。
    - 支持搜索（基于 text / name / javaType 等）和上下导航。

- **录制步骤（Recorded Steps）**
  - JavaFX 录制由 `FxRecordSupport.attachJavaFxRecordHooks` 完成：
    - 在所有 `Window` 的 `Scene` 上挂载输入事件监听（鼠标、键盘）。
    - 监控 `Window.getWindows()` 列表变化，自动为新弹出的 Dialog/Popup/ContextMenu 注册钩子。
  - 事件处理逻辑：
    - 将低层次事件（例如 `MOUSE_PRESSED` / `MOUSE_CLICKED`）转换为语义关键字：
      - `ClickButton` / `FillEdit` / `SelectMenuItem` / `SelectTreeList` / `SelectTab` 等。
    - 使用 `FxNodeClassifier` 将 JavaFX 控件归类为语义控件（`SEMANTIC_CONTROL`、`MENUITEM` 等）。
    - 对于菜单类控件，通过 `buildFxMenuPathFromRootToLeaf` 构造菜单路径（例如“文件/打开/最近文件”）。
  - 父对象与目标对象：
    - 父对象（`parentIdentifier`）：
      - 代表顶层 JavaFX 窗口 / 对话框 / ContextMenu 所在的 `Window`：
        - `javaType = window.getClass().getName()`。
        - `javaName = window.getTitle()`（若非空）。
      - 不再包含 `screenBounds`，只用于逻辑定位。
    - 目标对象（`objectIdentifier`）：
      - 代表触发事件的 JavaFX 控件：
        - `javaType = node.getClass().getName()`。
        - `javaName = node.getId()`（若非空）。
        - `text = node.getText()`、`value = node.getValue()`（若存在）。
      - 不再包含 `screenBounds`，避免将屏幕坐标作为定位条件。
  - 步骤发送：
    - 录制步骤通过 WebSocket 发送给 VS Code 扩展，扩展端使用 `recording/stepAdapter.ts` 转换为 `TestScriptStep`：
      - `parentIdentifier` 与 `objectIdentifier` 会被转换为 `ElementIdentifier`，并写入测试步骤列表。
      - 为避免误用，转换时不会带入 `bounds` / `screenBounds` 字段；JavaFX 步骤目前主要依赖 `javaName` / 文本等语义字段定位。

### 3. 测试步骤编辑与布局

- **步骤表格（Steps Table）**
  - 显示字段：
    - 关键字（Keyword）：例如 `ClickButton`、`FillEdit`、`SelectMenuItem` 等。
    - 父对象标识（Parent Identifier）：顶层窗口或弹出层。
    - 对象标识（Object Identifier）：具体控件。
    - 参数（Parameter）、数据（Data）、断言值（AssertValue）、等待时间等。
  - 编辑行为：
    - 父对象与对象标识单元格支持多行文本编辑，语法为 locator 的多行描述（支持 javaType/javaName/javaTypePath/javaNamePath/text/title 等）。
    - 编辑完成（blur）时，内容会解析为 `ElementIdentifier` 对象：
      - 在解析过程中，`screenBounds` 仅用于 Visual 视图和高亮，不用于发送给 Agent 的回放步骤。
    - 修改后通过 `syncSteps` 消息同步到扩展主进程，并刷新 Visual 视图。

- **Visual 视图（Visual Tab）**
  - 将每个步骤与一个“可视化节点”关联，包含：
    - 文本（text/caption）、名称（name）、javaType 等。
    - `screenBounds`：用于点击 “Highlight” 或执行高亮前预览。
  - 功能：
    - 点击 Visual 节点可快速添加 / 编辑对应的测试步骤。
    - 支持右键菜单（上下文菜单）进行复制、删除、重新生成步骤等操作。

### 4. 回放流程与功能

- **回放入口**
  - 在 Steps 表格中点击 “Execute” 按钮，由 `panelProvider._handleExecute` 调用：
    - 校验已选择目标进程 PID。
    - 进行许可证检查和前台激活目标应用窗口。
    - 调用 `agentLoader.replaySteps` 启动 / 附加 Agent，并通过 WebSocket 发送步骤数据。
  - 在发送步骤前，对每个 `TestScriptStep` 做统一清洗：
    - 若 `parentIdentifier` / `objectIdentifier` 中的 `index` 为空字符串或 `null/undefined`，会被删除，不会传给 Agent。
    - 无论 FX 还是 AWT/Swing，`bounds` / `screenBounds` 都不会作为回放定位条件发送到 Agent，只保留在 Visual / 对象树中使用。

- **JavaFX 回放解析**
  - `FxReplaySupport.resolveAndReplayJavaFxWithWait` 负责整体流程：
    - 使用 `FxReplayResolver.resolveParent(parentKey)` 定位顶层 `Window/Stage`：
      - 支持键：`javaType`、`javaName`、`title`/`Title`、`isDialog`、`isShowing`、`index`。
      - `javaType/javaName/title` 支持正则表达式。
      - 若有多个匹配窗口且未指定 `index`，返回错误；若指定的 `index` 超出范围，也返回错误。
      - 匹配失败时，`windowMatchesKey` 会返回具体错误信息（例如哪个字段不匹配），供 `lastParentError` 使用。
    - 在 parent 下通过 `FxReplayResolver.resolveObjects(parent, objectKey)` 收集所有匹配的 JavaFX 节点：
      - 从 `Scene.getRoot()` 开始，递归使用 `getChildrenUnmodifiable` / `getChildren` 遍历场景图。
      - 根据 `javaType` / `javaTypePath` / `javaName` / `title` / `text` 等条件过滤节点。
      - 如果有 `index`，由解析器在最终匹配结果上应用；否则按第一个匹配对象处理。
      - 当出现多匹配且未指定 `index` 时，报告定位歧义，并可在后续扩展中返回候选对象列表。
    - 使用 `FxReplayResolver.getNodeScreenBounds(node)` 计算对象的屏幕绝对坐标，并调用 `replayJavaFxByBounds`。
  - 高亮与等待：
    - 在实际点击 /输入前，`replayJavaFxByBounds` 通过 `Robot.mouseMove(cx, cy)` 与 `delay` 在目标对象上短暂高亮。
    - 支持 `maxWaitTimeForObjectAvailable` 配置，在父窗口 / 目标对象暂未就绪时进行轮询等待。

### 5. JavaFX 对象定位策略（当前约定）

- **主定位字段**
  - `javaType`：控件类型（例如 `javafx.scene.control.Button`、`javafx.scene.control.MenuItem`）。
  - `javaName`：JavaFX 控件 ID 或窗口标题。
  - `title` / `text`：根据控件类型（按钮、菜单、标签等）获取的显示文本。
  - `javaTypePath` / `javaNamePath`：当存在多处相同的控件时，用路径来消歧。

- **index 字段**
  - 对于 JavaFX 目标对象，目前约定：
    - 如果 index 没有被明确用于解析（例如 FX 中尚未严格实现基于 index 的选择），则在 Test Step 的 objectIdentifier 中**不设置 index**。
    - 回放时，如果 index 为空或未设置，VS Code 扩展不会将 index 字段发送给 Agent，以避免产生与实际解析逻辑不一致的行为。
  - 对于 AWT/Swing 对象，index 仍然可以用来区分同名同类型的兄弟控件。

### 6. 对象树导出与诊断

- **对象树导出**
  - 支持将当前扫描到的 UI 对象树（包含 `screenBounds`）导出为 JSON，用于：
    - 人工分析控件结构。
    - 对照测试步骤中的 objectIdentifier，检查是否能唯一定位到某个节点。
  - 在导出对象树时，`screenBounds` 被保留下来，便于可视化与调试。

- **诊断与日志**
  - Agent 端：
    - 通过 `JavaLog` / `AgentLogger` 记录所有关键事件，包含时间戳和 `[INFO]` / `[ERROR]` 前缀。
    - JavaFX 专用日志包括：
      - 场景钩子注册 / 取消。
      - Window 列表监听（包括动态弹窗）。
      - 菜单路径解析与 SelectMenuItem 数据。
      - 解析失败时的具体原因（例如 `windowMatchesKey` 的字段级错误说明）。
  - 扩展端：
    - `panelProvider` 将重要操作和错误输出到 Output Channel，并在 Webview 状态栏中展示简要信息。
    - `agentLoader` 记录 Agent Loader 的启动 / 退出 / 连接错误等信息。

### 7. 后续扩展点（建议）

- 为 JavaFX 对象增加更丰富的 locator 组合策略（如 `javaName + text + typePath`）的可视化编辑支持。
- 在多匹配场景下，将所有候选对象的标识（包括路径和基本属性）以 JSON 形式返回前端，便于用户选择或生成更精确的 locator。
- 在 Steps 表格中，对 JavaFX 对象的 index 字段增加 UI 级别提示（例如 tooltip），说明当前版本 index 对 FX 的影响有限，推荐优先使用名称 / 文本 / 路径消歧。

