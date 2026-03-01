# javafxagent.md — JavaFX Agent（JVMTI/Instrumentation）语义树采集与 VS Code/Cursor Extension 集成指南

> 目标：在你现有的 **Swing/AWT JVM Agent 注入体系**基础上，把 **JavaFX** 纳入同一套“语义对象模型 + 录制/回放 + 高亮”的自动化能力中，并把能力暴露给 **VS Code/Cursor Extension**（含 MCP）。
>
> 现状假设：你已经能注入 Java 进程，并且“已经拿到了 JavaFX 的树（Scene Graph）”。本指南把这一步之后的工作拆成可执行的 Cursor 任务清单、目录结构、关键接口与最小可商用（MVP）实现路径。

---

## 0. 你要让 Cursor 做什么（一次性任务说明）

把下面这段直接贴给 Cursor（建议作为本仓库的 `Agent.md` 或者 Cursor 的系统提示）：

**Cursor Task Prompt（复制给 Cursor）：**

1. 在现有 `java-agent` 模块中新增 `javafx` 子模块，实现 JavaFX Scene Graph 的 **采集 / 语义化 / 高亮 / 录制事件映射**。  
2. 语义层按 `UiElement / UiRole / UiLocator / UiAction / UiSnapshot` 模型实现（见本文第 4 章）。  
3. 新增 JavaFX recognizers：`TreeView/TreeCell/TreeItem` 与 `TableView/TableCell` 优先实现，确保能生成稳定的 `SelectTreeNode(path=...)`、`SelectTableCell(row,col)` 等 step。  
4. 在 extension 侧新增命令：`mars.javafx.snapshot`, `mars.javafx.highlight`, `mars.javafx.record`, `mars.javafx.replay`。  
5. MCP：暴露工具 `snapshot`, `highlight`, `recordStart`, `recordStop`, `replay`（仅针对当前 workspace 的 extension）。  
6. 输出：完整代码 + 本地可运行 demo（最小 JavaFX App）+ 单元测试（至少 recognizer 的 golden snapshot 测试）。

---

## 1. 总体架构（与 Swing/AWT 共存）

### 1.1 分层原则（Raw → Semantic → Steps）

- **Raw Layer（JavaFX 原始层）**：对 Scene Graph 的 `Node` 做镜像快照（类型、id、styleClass、bounds、可见性、可访问属性…）。  
- **Semantic Layer（语义层）**：把多个 Node 合并成用户理解的组件（TreeNode、TableCell、FormField…），并生成稳定 Locator。  
- **Step Layer（步骤层）**：把语义动作（select/expand/setText）编译成你现有 keyword 引擎的 step（如 `SelectTreeNode`、`FillEdit`、`ClickButton`）。

### 1.2 与 Swing/AWT Agent 的边界

- Swing/AWT：通常可以走 Accessibility/组件树 + 事件监听。  
- JavaFX：必须面对 **虚拟化（VirtualFlow）**、**自定义 cellFactory**、**graphic 组合**等特性；因此语义层必须独立实现 recognizer 链与多锚点 locator。

---

## 2. 工程目录建议（Agent 侧）

假设你现在有：`agent/`（JVMTI 或 javaagent + instrumentation），以及 `extension/`（VS Code/Cursor）。

在 `agent/` 中新增：

```
agent/
  src/main/java/...
    mars/agent/
      core/                 # 已有
      swing/                # 已有
      awt/                  # 已有
      javafx/
        fxscan/
          FxThreadBridge.java
          FxSnapshotBuilder.java
          FxNodeWalker.java
          FxBoundsUtil.java
          FxVirtualFlowSupport.java
        model/
          raw/
            FxRawNodeRef.java
            FxRawSnapshot.java
          semantic/
            UiElement.java
            UiRole.java
            UiState.java
            UiLocator.java
            UiAnchor.java
            UiAction.java
            UiActionType.java
            UiSnapshot.java
        recognize/
          SemanticRecognizer.java
          RecognizerPipeline.java
          RecognizerContext.java
          extractors/
            TextExtractor.java
            LabeledTextExtractor.java
            AccessibleTextExtractor.java
            TooltipTextExtractor.java
            GraphicTextExtractor.java
            TextExtractorChain.java
          locators/
            LocatorBuilder.java
            PathAnchorBuilder.java
            FxIdAnchorBuilder.java
            StyleClassAnchorBuilder.java
            NeighborAnchorBuilder.java
          recognizers/
            TreeViewRecognizer.java
            TreeNodeRecognizer.java
            TableViewRecognizer.java
            TableCellRecognizer.java
            ButtonRecognizer.java
            TextInputRecognizer.java
        execute/
          FxHighlighter.java
          FxActionExecutor.java
          ViewportController.java
        record/
          FxEventTap.java
          FxRecordMapper.java
```

> 说明：你可以把 `model/semantic` 抽到 `core`，让 Swing/AWT/JavaFX 共用一套语义对象模型（推荐），但为了 Cursor 好实现，这里先按 JavaFX 内聚写法给出。

---

## 3. JavaFX 采集（Snapshot）关键点

### 3.1 JavaFX Thread（必须）

所有 JavaFX UI 访问必须在 **JavaFX Application Thread** 执行。

实现一个统一桥接：

- `FxThreadBridge.runOnFxThread(Callable<T>)`：如果当前线程不是 FX Thread，则 `Platform.runLater` + `CountDownLatch` 等待结果返回。

**Cursor 实现要求：**
- 所有 snapshot、highlight、获取 bounds、获取文本，都必须通过该桥接执行。

### 3.2 Scene Root 获取

常见路径：

- 若能拿到 `Stage`：`stage.getScene().getRoot()`  
- 若无法直接拿 `Stage`：从 `Window.getWindows()` 枚举 `Window`，过滤 `Stage` 且 `isShowing()`。

### 3.3 Node 遍历（注意可见性与虚拟化）

- 遍历 `Parent.getChildrenUnmodifiable()`  
- 对 `Control` 类（TreeView/TableView/ListView）需要特殊处理：
  - 其可见 cell 来自 `VirtualFlow`，并不等于全部数据项。
  - **语义定位**要优先绑定到数据模型（TreeItem / ObservableList）而不是只靠可见 Node。

### 3.4 必采字段（RawNodeRef）

对每个 Node 采集：

- `fxType`：`node.getClass().getName()`  
- `fxId`：`node.getId()`  
- `styleClasses`：`node.getStyleClass()`  
- `boundsInScene`：`node.localToScene(node.getBoundsInLocal())`  
- `visible/managed/disabled`  
- `accessibleRole` / `accessibleText` / `accessibleHelp`  
- 若是 `Labeled`：`getText()`  
- 若有 `Tooltip`：`Tooltip.getText()`  
- 父子关系：`parentId` + children ids

输出为：`FxRawSnapshot(sceneSignature, nodes[])`

---

## 4. 语义对象模型（Semantic Model）— 必须实现

> 你要让用户看到的是 “TreeNode / TableCell / FormField”，而不是 Label/HBox/Arrow。

### 4.1 UiElement

字段建议：

- `elementId`（内部 uuid）
- `role: UiRole`
- `name: String`（用户可见名称）
- `state: UiState`（selected/expanded/enabled/visible）
- `locator: UiLocator`（anchors + confidence）
- `actions: List<UiAction>`
- `rawNodes: List<FxRawNodeRef>`（内部调试用，不暴露给最终用户）

### 4.2 UiLocator（多锚点）

`anchors[]`（按优先级）：

1. `path`（TreeItem 路径 / Menu path）⭐最稳  
2. `text`（Labeled/AccessibleText）  
3. `neighbor`（相邻 label、容器标题）  
4. `fxId` / `styleClass`（如果应用方配合）  
5. `index`（仅 fallback）

同时维护：`confidence: 0..1`

### 4.3 UiAction（行为归一）

统一动作集：

- `Select`
- `Expand`
- `Collapse`
- `SetText`
- `Click`
- `OpenContextMenu`
- `ScrollIntoView`

> 注意：Expand/Collapse 不应暴露“点击箭头”，应是 `node.expand()`，由 executor 决定点哪。

---

## 5. Recognizer Pipeline（应对 JavaFX 组合灵活性）

### 5.1 插件链模式

每个 recognizer 实现：

- `match(ctx, rawNode) -> score`
- `build(ctx, rawNode) -> UiElement`

Pipeline：
- 先对 rawNodes 跑 match，挑高分 recognizer
- 构建 UiElement
- 对复杂控件（Tree/Table）允许“聚合构建”（一个 raw node 构建多个语义元素，如 Table -> rows -> cells）

### 5.2 TextExtractorChain（不要写死 Label）

按优先级提取 name：

1. AccessibleText / AccessibleHelp  
2. Labeled.text  
3. Tooltip text  
4. graphic 内部文本（递归寻找可见 Labeled）  
5. 最后 fallback：fxType + index

输出 `name + sources + confidence`

---

## 6. JavaFX TreeView 的正确语义化（MVP 必做）

### 6.1 语义边界：TreeCell = TreeNode

识别策略（建议）：

- 识别 `TreeView`（role=Tree）  
- 识别 `TreeCell`（role=TreeNode）
- 从 `TreeCell.getTreeItem()` 获取数据模型
- name 优先 `TreeItem.getValue().toString()` 或 `TreeCell.getText()`
- locator 主锚点：**TreeItem 路径**（root → ... → current）

### 6.2 TreeItem Path 生成

`path = ["Root", "Operations", "Trade Management"]`

策略：
- 从 `TreeItem` 向上追溯 parent，收集 value/text
- 处理重复名：必要时在 anchor 增加 siblingIndex 或 stableKey

### 6.3 Expand/Collapse 动作

判断 expanded：
- `TreeItem.isExpanded()`

执行 expand：
- 优先调用：`treeItem.setExpanded(true)`（若可行且不会破坏应用逻辑）
- fallback：点击 disclosure node（从 TreeCell skin 中找箭头区域）

> 建议：先尝试 setExpanded（稳定），如有副作用再降级到点击。

---

## 7. 高亮（Highlight）实现（JavaFX Overlay）

目标：extension 发来 element locator，agent 在应用上高亮边框/半透明遮罩。

推荐做法：

- 在 `Scene` 上创建一个 overlay `Pane`（透明鼠标穿透：`setMouseTransparent(true)`）
- 高亮时在 overlay 上画 `Rectangle`（stroke + fill alpha）
- 持续 N ms 或直到下一次 highlight

关键：
- overlay 只创建一次（缓存），避免闪烁与性能问题
- bounds 使用 `boundsInScene` 转换到 overlay 坐标系

---

## 8. 录制与回放：从 FX 事件到语义 Step

### 8.1 事件捕获（FxEventTap）

在 root scene 添加 EventFilter：

- MouseEvent.MOUSE_PRESSED / RELEASED
- KeyEvent.KEY_PRESSED / TYPED

捕获后：
- 获取 `event.getTarget()`（通常是 Node）
- 用 `RecognizerPipeline` 把 target 映射到 UiElement（语义对象）
- 生成中间动作：`Select/Click/SetText/Expand`

### 8.2 录制的“去噪”策略（必须）

你提到的“一次点击产生大量 mouse 事件”需要过滤：

- 合并同一 element 的 press+release 为一个 Click
- 拒绝 hover/move
- 对连续 KeyTyped 合并成一次 SetText（基于 time window，如 300ms）
- 对 TreeNode：点击 arrow 区域识别成 Expand/Collapse；点击主体识别成 Select

### 8.3 StepCompiler 输出 keyword steps

示例：

- TreeNode select：`SelectTreeNode(path=..., name=...)`
- TextInput：`FillEdit(name=..., value=...)`
- Button：`ClickButton(name=...)`

---

## 9. Extension 集成（VS Code/Cursor）

### 9.1 命令与 UI

新增命令：

- `mars.javafx.snapshot`：获取当前语义树（JSON）
- `mars.javafx.highlight`：对选中元素高亮
- `mars.javafx.record.start` / `mars.javafx.record.stop`
- `mars.javafx.replay`：执行 step 列表

建议 UI：
- TreeView 面板展示语义树（role + name + locator confidence）
- 右键：Highlight / Copy Locator / Add Step

### 9.2 IPC 通道

你已有 Swing/AWT 通道就复用（本地 socket/NamedPipe/gRPC 均可），新增 JavaFX 消息类型：

- `GetFxSnapshot`
- `HighlightFx(locator)`
- `StartFxRecord`
- `StopFxRecord`
- `ReplayFx(steps[])`

---

## 10. MCP（可选但推荐）

如果你要让 Chat 通过 MCP 调用：

工具列表（tool schema）：

- `snapshot(appPid)` → returns semantic tree
- `highlight(appPid, locator)`
- `recordStart(appPid)`
- `recordStop(appPid)` → returns steps
- `replay(appPid, steps)`

> 注意：MCP 工具应当是“幂等/可审计”的。每次执行要回传 evidence id（截图/快照 hash）。

---

## 11. 最小可运行 Demo（Cursor 必须创建）

在 `demo-javafx/` 创建一个最简单但覆盖 Tree/Table/Form 的 JavaFX App：

- 左侧 TreeView（带展开/收起）
- 右侧 TableView（带可编辑 cell）
- 顶部 Form（TextField + ComboBox + Button）
- 点 Button 弹 Dialog

目的：
- 验证 snapshot/recognizer/highlight/record/replay 全链路

---

## 12. 测试（最少要有）

### 12.1 Golden Snapshot 测试

- 启动 demo
- 拿 `UiSnapshot` JSON
- 与 `src/test/resources/golden/*.json` 比对（允许忽略 bounds 这种浮动字段）

### 12.2 Recognizer 单测

对 `TreeNodeRecognizer`：
- 输入 raw snapshot（mock）
- 输出 role/name/path anchors 正确

---

## 13. 交付检查清单（Definition of Done）

- [ ] `FxThreadBridge` 正确封装 FX thread 调用  
- [ ] Snapshot 含 RawNode 必要字段 + sceneSignature  
- [ ] Recognizer Pipeline 支持 TreeView/TreeNode、TableView/TableCell  
- [ ] Locator 多锚点 + confidence 输出  
- [ ] Highlight overlay 稳定、不抢焦点、不阻塞鼠标  
- [ ] Record 去噪：click 合并、key 合并、expand 识别  
- [ ] Replay：可滚动到节点（VirtualFlow）并执行动作  
- [ ] Extension 命令可用 + UI 面板能展示语义树  
- [ ] MCP tools 可用（如开启）  
- [ ] Demo app + golden tests 通过

---

## 14. Cursor 执行顺序建议（按迭代）

**Iteration 1（2–3 天）**
- FxThreadBridge + SnapshotBuilder + 输出 raw snapshot
- TreeViewRecognizer + TreeNodeRecognizer（只做 select）
- Highlight（只框 TreeCell bounds）

**Iteration 2**
- Expand/Collapse
- Record click → SelectTreeNode
- Replay SelectTreeNode（含 scroll into view）

**Iteration 3**
- TableView/TableCell recognizer + SetText
- Dialog + Button recognizer
- MCP tools

---

## 15. 附：常见坑（务必规避）

1. **JavaFX 虚拟化**：不要指望遍历 node 就拿到全部行/节点；必须绑到 TreeItem/Items 模型。  
2. **cellFactory 自定义**：name 提取要走 extractor chain，不要写死 Label。  
3. **bounds 坐标系**：`localToScene` 后还需转换到 overlay pane 坐标。  
4. **线程**：任何 UI 访问都必须在 FX thread。  
5. **事件风暴**：录制必须合并/去噪，否则 step 不可用。  
6. **高亮不应影响用户操作**：overlay mouseTransparent。  

---

## 16. 你可以直接复制的接口签名（给 Cursor 的硬约束）

> （Cursor 实现时可按你代码风格调整，但对外契约保持一致）

- `FxRawSnapshot buildRawSnapshot()`  
- `UiSnapshot buildSemanticSnapshot(FxRawSnapshot raw)`  
- `ResolutionResult resolve(UiLocator locator, UiSnapshot snapshot)`  
- `ExecuteResult execute(UiAction action, ResolutionResult resolved)`  
- `void highlight(UiElement element, int durationMs)`  
- `List<KeywordStep> stopRecording()` / `void startRecording()`  

---

### 结束语

JavaFX 自动化的关键不是“能不能拿到树”，而是：

- **把灵活组合的 UI 结构，稳定地映射为用户认知的语义控件**
- **用多锚点 locator + recognizer 插件链 + 录制自学习**抵抗差异与演进
- **在 extension/MCP 层输出可控、可审计的 actions 与 evidence**

按本指南的 MVP 路线走，你可以在 1–2 周内把 JavaFX 纳入你现有的 Swing/AWT 自动化体系，并形成可产品化的“语义对象模型”能力。

