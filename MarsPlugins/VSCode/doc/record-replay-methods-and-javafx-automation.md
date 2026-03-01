# Record/Replay 方法对比与 JavaFX 自动化方案

本文档说明：  
1）当前各对象类型（Swing/AWT vs JavaFX）的 Record/Replay 实现方式；  
2）JavaFX 可用的 UI 自动化手段；  
3）差异与缺口；  
4）可选后续方案，供一起决定如何继续。

---

## 一、当前 Record/Replay 实现总览

### 1.1 按 UI 技术分类

| 技术 | 扫描 (Scan) | 录制 (Record) | 回放 (Replay) |
|------|-------------|----------------|----------------|
| **Swing/AWT** | `AgentProtocol.scanComponent`：遍历 `Window.getWindows()` → `Container.getComponents()`，每节点 `getLocationOnScreen()`+`getSize()` 得到 screenBounds | AWT 事件监听（MouseListener/KeyListener 等）→ `MouseEventHandler` 等生成 keyword + parent/object identifier（Component 引用可解析） | 用 parent/object identifier **解析出 Component**，再按 keyword 调用专用逻辑（部分用 Robot 辅助） |
| **JavaFX** | `AgentProtocol.scanJavaFxRoots` → `scanJavaFxNode`：反射 `Window.getWindows()`、Scene、Node，`localToScreen(layoutBounds)` 得到 screenBounds | 反射挂载 Scene 级 `addEventFilter(MOUSE_CLICKED/KEY_RELEASED)` → `handleJavaFxRecordEvent` 按 target 类型映射 keyword，identifier 仅含 **javaType/text/value + screenBounds**（无 Node 引用） | **不解析 Node**：用 objectKey 中 **screenBounds** 判定为 JavaFX 步后，统一走 `replayJavaFxByBounds`：**仅用 Robot 在屏幕坐标 (cx,cy) 上模拟点击/输入** |

结论：Swing 是「解析到组件 + 语义操作（必要时配合 Robot）」；JavaFX 目前是「不解析节点，只按屏幕坐标 Robot 操作」。

---

### 1.2 按 Keyword 的 Record/Replay 支持矩阵

下表为「录制是否有对应逻辑」与「回放实现方式（Swing vs JavaFX）」。

| Keyword | Swing Record | Swing Replay | JavaFX Record | JavaFX Replay |
|---------|--------------|--------------|----------------|----------------|
| **ClickButton** | 有（按钮点击） | 解析 Component → Robot 点击中心 或 专用逻辑 | 有（MOUSE_CLICKED → ClickButton） | Robot 点击 screenBounds 中心 |
| **DoubleClickButton / DoubleClick** | 有 | 解析 → Robot 双击 | 无专门分支（可归入 ClickButton） | Robot 双击中心 |
| **FillEdit** | 有（键盘输入） | 解析 → 点击 → 清空 → typeText + Enter | 有（KEY_RELEASED ENTER/TAB → TextField/TextArea） | Robot 点击 → 清空 → 输入 → Enter |
| **SelectDropList / SelectDropDown** | 有 | 解析 JComboBox → 选值 或 Robot | 有（ComboBox/ChoiceBox → SelectDropList） | Robot 点击中心 → 输入 data → Enter（未用 setValue） |
| **SelectMenuItem / SelectPopupMenu** | 有 | 解析到 JMenuItem → **Robot 点击**（保证菜单关闭） | 有（MenuItem → SelectMenuItem） | Robot 点击中心 |
| **SelectListItem** | 有 | 解析到 List 项 → Robot 点击 | 无专门分支（可归入 ClickButton） | Robot 点击中心 |
| **SelectTreeList** | 有（JTree 路径） | **解析 JTree → 按 path 展开/选中 → Robot 点击** | 有（TreeCell/TreeView → SelectTreeList，data=text） | **仅 Robot 点击中心**（不展开、不按路径选节点） |
| **SetRadioBox** | 有（选中的 radio text） | 解析 AbstractButton → 按 text 选 → Robot | 有（RadioButton → SetRadioBox） | Robot 点击中心 |
| **SetCheckBox** | 有（true/false） | 解析 AbstractButton → 按状态设 → Robot | 有（CheckBox → SetCheckBox） | Robot 点击中心 |
| **SelectTab** | 有 | **解析 JTabbedPane → 按 index/标题选 tab** | 无（未识别 TabPane） | 无（未在 replayJavaFxByBounds 中实现） |
| **SearchAndClick / SearchAndUpdate** | 有（JTable） | **解析 JTable → 按行列/条件定位单元格 → Robot 或 API** | 无 | 无 |
| **VerifyObjectValue** | 有 | 解析 Component → 读 value/text 与 step 比较 | 无专门分支 | 无 |
| **ExpandTreeNode / CollapseTreeNode** | 有（JTree） | 解析 JTree → 展开/折叠路径 | 无 | 无 |
| **ClickAT** | 有（含 Rightclick） | 按 parameter/data 执行点击/右键 | 可录到 ClickButton 或未单独区分 | 与 ClickButton 同路径（Robot） |

要点：

- **Swing**：多数 keyword 都有「解析到具体组件 + 语义动作」；复杂控件（JTree、JTabbedPane、JTable）有专用 replay（路径、tab 索引、行列条件）。
- **JavaFX**：录制上已按控件类型映射了多种 keyword，但回放**全部**落在 `replayJavaFxByBounds`，仅「屏幕中心点 + Robot」，没有：
  - 解析到 `javafx.scene.Node`
  - `TabPane` 的 SelectTab
  - `TableView` 的 SearchAndClick / SearchAndUpdate
  - `TreeView` 的展开/折叠、按路径选中（SelectTreeList 只点中心）
  - VerifyObjectValue
  - 对 ComboBox 的 `setValue` 等 API 级操作

---

## 二、JavaFX 自动化的常见做法（行业/API）

以下均为「在目标 JVM 内」或「同机」可用的方式，与当前 agent 注入、跨进程控制的约束不完全一致，但可作为「可选方向」参考。

### 2.1 基于坐标的模拟（当前做法）

- **方式**：用 `java.awt.Robot` 在屏幕坐标上模拟鼠标/键盘。
- **优点**：与 UI 框架无关，Swing/JavaFX 通用；无需加载 JavaFX 类即可回放。
- **缺点**：依赖布局稳定、窗口不遮挡；无法做语义校验（如 ComboBox 当前值）；TreeView/TabPane 等需多次点击或无法精确表达。

### 2.2 TestFX（测试框架，同进程）

- **方式**：在**同一 JVM** 内用 TestFX 的 FxRobot：`clickOn("#id")`、`type()`、`lookup()` 等，基于 Node 查询再模拟。
- **优点**：API 友好、可查 Node、可断言状态。
- **缺点**：面向单元/集成测试，需在**应用进程内**启动测试；当前架构是「extension + 独立 agent 进程 attach 目标进程」，无法直接使用 TestFX 的 in-process robot。

### 2.3 Node.fireEvent（程序化触发事件）

- **方式**：在目标进程中通过反射拿到 `javafx.scene.Node`，构造 `MouseEvent`/`KeyEvent` 等，调用 `node.fireEvent(event)`。
- **优点**：不依赖坐标，可精确针对节点；部分逻辑可不用 Robot。
- **缺点**：需在**目标 JVM 内**解析「步骤 → Node」；事件是“合成”的，与真实硬件事件在传播/焦点上可能有差异；某些控件（如 ComboBox 下拉）可能仍依赖真实点击才弹出。

### 2.4 控件 API 直接调用（setValue / setSelected 等）

- **方式**：解析到 Node 后，若是 `ComboBoxBase` 则 `setValue()`，若是 `CheckBox` 则 `setSelected()`，若是 `TabPane` 则 `getSelectionModel().select(index)` 等。
- **优点**：最稳定、不依赖坐标与焦点，回放快。
- **缺点**：必须在目标进程内解析 identifier → Node；每种控件都要写一段分支；有些效果（如打开下拉再选）若依赖 UI 动画/异步，可能仍需一次 Robot 点击。

### 2.5 可访问性（Accessibility）

- **方式**：JavaFX 的 `Node` 有 `accessibleText`、`AccessibleRole` 等，可配合系统 a11y 或自写查询。
- **现状**：当前 agent 未使用 a11y 做定位或回放；若引入，可辅助「按角色/文本查 Node」，再结合 2.3/2.4。

---

## 三、当前 JavaFX 的缺口汇总

1. **回放仅靠 screenBounds + Robot**  
   - 无 Node 解析，无 `fireEvent`，无 `setValue`/`setSelected`/`select(index)` 等。
2. **SelectTab（TabPane）**  
   - 未录制 Tab 切换语义，未回放。
3. **SelectTreeList（TreeView）**  
   - 录制了节点 text，回放只点中心，不展开、不按路径选中。
4. **SearchAndClick / SearchAndUpdate（TableView）**  
   - 未实现录制与回放。
5. **ExpandTreeNode / CollapseTreeNode（TreeView）**  
   - 未实现。
6. **VerifyObjectValue**  
   - 未对 JavaFX 控件做值校验。
7. **ComboBox/ChoiceBox**  
   - 回放用「点击 + 输入 + Enter」，未用 `getSelectionModel().select(...)` 或 `setValue()`，在选项多或需精确匹配时不稳定。

---

## 四、可选后续方向（供一起决定）

在「保持 agent 注入、跨进程、现有 keyword 体系」的前提下，可以按优先级选一条或组合做。

### 方案 A：维持现状（仅 Robot + screenBounds）

- 只保证当前已支持的 JavaFX keyword 继续用 `replayJavaFxByBounds` 稳定工作（含 screenBounds 正确性、高亮）。
- **适用**：以「点击、简单输入、简单选择」为主、界面简单的 JavaFX 应用。
- **不解决**：Tab、Tree 路径、Table 行列、校验等。

### 方案 B：增加「JavaFX Node 解析 + 按 keyword 分支回放」（推荐方向）

- 在 agent 内增加「从 objectKey（javaType/text/value/parent + screenBounds）解析到 `javafx.scene.Node`」的逻辑（类似 Swing 的 `resolveComponentWithWait`，但遍历 JavaFX Window/Scene/Node）。
- 回放时：若解析到 Node，则按 keyword 分支：
  - **SelectTab**：解析到 TabPane → `getSelectionModel().select(index)` 或按 title 匹配。
  - **SelectTreeList**：解析到 TreeView → 按 path/text 展开并选中 → 再视情况 `fireEvent` 或 Robot 点击。
  - **SetCheckBox/SetRadioBox**：解析到 CheckBox/RadioButton → `setSelected()` / 选同组 radio。
  - **ComboBox**：解析到 ComboBoxBase → `setValue()` 或 `getSelectionModel().select(...)`。
  - **FillEdit**：解析到 TextInputControl → `setText()` + 可选 `fireEvent` 通知。
  - 其余可保留 Robot 兜底（如菜单、弹出层）。
- **优点**：与 Swing 的「解析 + 语义回放」一致，稳定性、可维护性更好。
- **成本**：需在 agent 中维护 JavaFX 树遍历、identifier 与 Node 的匹配规则，以及各控件的 replay 分支。

### 方案 C：JavaFX 仅做「Node.fireEvent」补充

- 不追求全覆盖，只对「点击类」keyword 在解析到 Node 后调用 `fireEvent(new MouseEvent(...))`，减少对坐标的依赖。
- **优点**：改动相对小。  
- **缺点**：fireEvent 行为与真实点击不完全一致（见 2.3），且不解决 Tab/Tree/Table/校验等问题。

### 方案 D：文档化 + 分阶段实现

- 将本文档作为「Record/Replay 与 JavaFX 自动化」的正式说明，并在开发计划中明确：
  - 第一阶段：确保现有 JavaFX Robot 回放 + screenBounds 无误（含高亮）。
  - 第二阶段：实现 JavaFX Node 解析与 SelectTab / SelectTreeList / SetCheckBox / SetRadioBox / ComboBox 的语义回放（方案 B 的子集）。
  - 第三阶段：视需求再考虑 TableView、VerifyObjectValue、Expand/Collapse 等。

---

## 五、建议的下一步

1. **确认目标**：以「与 Swing 行为对齐」为主，还是「先保证基本点击/输入可用」即可。  
2. **若选方案 B 或 D**：优先实现「JavaFX Node 解析」和 1～2 个高价值 keyword（例如 SelectTab + SelectTreeList，或 ComboBox setValue），再逐步补全。  
3. **文档**：把最终选定的方案和 keyword 支持矩阵写进 `doc/README_zh.md` 或 `doc/USER_GUIDE_*.md`，方便后续维护和用户预期。

以上为当前各对象类型的 Record/Replay 方法对比，以及 JavaFX 的可行路线；可根据你的优先级（开发量、稳定性、功能范围）一起定下一步实现顺序。

---

## 六、语义对象与解析（semanticRole）

### 6.1 语义角色来源

- **扫描阶段**：`FxScanner.scanJavaFxNode` 使用 `FxSemanticConfig.roleForTypeName(javaType)` 为每个节点写入 `semanticRole`（如 `TreeView` → `Tree`，可配置 `fx-semantic.properties` 或 `mars.fx.semantic.config`）。
- **录制阶段**：当前 JavaFX 录制 step 的 `objectIdentifier` 含 `javaType`、`text`、`value`、`screenBounds` 等；可按需扩展为根据 `javaType` 查 `FxSemanticConfig` 写入 `semanticRole`，便于前端统一按角色处理。

### 6.2 解析时如何处理语义对象

- **Extension 侧**：`stepAdapter.keyToElementId` 已将 `objectKey.semanticRole` 映射到 `ElementIdentifier.semanticRole`；`RecordedStep` → `TestScriptStep` 的转换会保留该字段。
- **用途**：
  - **展示**：用 `semanticRole` 显示为「Tree」「Edit」「ComboBox」等，而不是原始 `javaType` 类名。
  - **匹配/过滤**：按角色筛选对象列表或决定高亮/回放策略。
  - **脚本生成**：若按角色生成关键字或参数，可依赖 `semanticRole` 与 `keyword` 一致（如角色 Tree → SelectTreeList）。
- **高亮**：高亮仍以 `objectIdentifier.screenBounds` 为准；语义解析不改变坐标，只补充角色信息。

---

## 附录：JavaFX 录制规则与 SelectTab 配置

### A.1 规则表（FxNodeClassifier RULES）

规则按**顺序**匹配，类名 **contains** 即命中。用于 `classify(node)` 和 `foldAndLift` 中的语义类型与边界。

| 顺序 | pattern | category | boundary | semanticType | 说明 |
|------|---------|----------|----------|--------------|------|
| 1 | TabPaneSkin | SEMANTIC_CONTROL | COMPOSITE_BOUNDARY | TABPANE | Tab 区域，不折叠 |
| 2 | **TabHeaderSkin** | **SEMANTIC_PART** | **ACTION_BOUNDARY** | **TAB** | Tab 头（含 `TabHeaderSkin$4` 等内部类）→ SelectTab |
| 3 | Skin | STRUCTURAL_CONTAINER | NON_BOUNDARY | SKIN_OR_VFLOW | 其他 Skin 折叠 |
| 4 | VirtualFlow | STRUCTURAL_CONTAINER | NON_BOUNDARY | SKIN_OR_VFLOW | 折叠 |
| 5–… | Pane, Region, HBox, … | STRUCTURAL_CONTAINER | NON_BOUNDARY | LAYOUT_CONTAINER | 布局折叠 |
| … | TreeCell, TableCell, ListCell, … | SEMANTIC_PART | ACTION_BOUNDARY | TREECELL / TABLECELL / … | 列表/树/表单元格 |
| … | Button, CheckBox, TextField, … | SEMANTIC_CONTROL | ACTION_BOUNDARY | BUTTON / TEXT_INPUT / … | 可点击/可输入控件 |
| … | **Label** | **DECORATION** | **NON_BOUNDARY** | **DECORATION** | 不单独作为步目标，优先提升到 TabHeaderSkin |

### A.2 SelectTab 的 normalize 顺序（normalizeToMeaningfulNode）

点击 Tab 上的文字时，事件 target 常为 **LabeledText**（或 Label）。必须先提升到 **TabHeaderSkin**，再走 foldAndLift，否则会得到 Label → ClickButton。

1. **最先**：若存在祖先类名包含 **TabHeaderSkin**（含 `TabPaneSkin$TabHeaderSkin$4`），直接返回该祖先 → foldAndLift 从 Tab 头开始 → semanticType=TAB → **SelectTab**。
2. 否则再处理：LabeledText → 最近 Labeled 控件；图形节点 → 最近 Button；等。

代码位置：`java/marsJavaAgent/.../fx/FxNodeClassifier.java`  
- 规则表：`buildRules()`，**TabHeaderSkin** 在 **Skin** 之前。  
- normalize：`normalizeToMeaningfulNode()`，**先** `nearestAncestorWithClassNameContaining(node, "TabHeaderSkin")`，再 text/graphic 到 Labeled/Button。
