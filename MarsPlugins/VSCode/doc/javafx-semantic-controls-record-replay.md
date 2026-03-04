# JavaFX 语义控件 Record 与 Replay 说明

本文档说明 JavaFX 中语义控件的录制（record）与回放（replay）机制、当前已支持的控件，以及如何扩展其他语义控件。  
**说明**：**DateTimePicker**、**Splitter** 暂不实现。  
**规范**：详细语义模型与算法见 **java/doc/javafx_record_semantic_spec.md**（fold/lift、MaxAncestorHops、Skin→skinnable、SEMANTIC_PART 优先等）。

---

## 1. 指令与配置来源

### 1.1 语义类型（semanticType）

语义类型由 **规则表** 决定：

- **配置文件**：`java/marsJavaAgent/src/main/resources/fx-node-classifier-rules.json`  
  每条规则包含：`pattern`（类名包含匹配）、`category`、`boundary`、**`semanticType`**。  
  规则按顺序匹配，**先匹配到的生效**（例如 TabHeaderSkin 需在 TabPane 前，才能把 Tab 头识别为 TAB）。

- **回退**：若资源缺失或解析失败，使用 `FxNodeClassifier.buildRulesFallback()` 中的内置规则。

### 1.2 语义对象类别：组合语义与简单语义

语义对象分为两类，用于决定「以谁为步骤目标」：

| 类别 | 说明 | 示例 |
|------|------|------|
| **组合语义**（composite semantic） | 表/树/列表/标签页及其**部分**（cell、tab）。步骤目标为「部分」时表示在组合内的某一格/项/页。 | TableView + **TableCell**；TreeView + **TreeCell**；ListView + **ListCell**；TabPane + **Tab** |
| **简单语义**（simple semantic） | 单一控件，如输入框、按钮、复选框等。 | TextField、Button、CheckBox、ComboBox、RadioButton 等 |

**规则**：对**简单语义**均需在 **MaxAncestorHops** 内**向上查找**是否处于某**组合语义**（即是否在 TableCell/TreeCell/ListCell/Tab 内）。  
- **若找到**：以该组合语义的 part（如 TableCell）为步骤目标，生成 SearchAndUpdate、SearchAndClick、SelectTab 等。  
- **若未找到**：以该简单语义对象本身为步骤目标，生成 FillEdit、ClickButton、SetCheckBox 等。

代码中：`FxNodeCategory.COMPOSITE_SEMANTIC` / `SIMPLE_SEMANTIC` 为标记常量；`FxNodeClassifier.isCompositeSemanticPart(meta)`、`isSimpleSemanticControl(meta)` 用于判断类别。

### 1.3 语义追踪配置（SemanticTracking）

- **配置文件**：`marsJavaAgent-config.json` 中的 **SemanticTracking**、**SemanticPolicy**（见 spec §5）。
- **MaxAncestorHops**（默认 5）：从事件目标节点向上追溯的**最大层数**。例如点击 Table 单元格内的 Label 时，会在该层数内优先解析到 **TableCell**（列语义），而不是 Label；若超过层数仍未找到 SEMANTIC_PART/SEMANTIC_CONTROL 则不再继续向上。
- **PreferPartOverControl**：优先将目标解析为 SEMANTIC_PART（TableCell/TreeCell/ListCell/Tab 等），再解析 owner（TableView/TreeView 等）。
- **组合内的控件**：TextField、Button 等 SEMANTIC_CONTROL 也可能是组合语义对象的一部分（如 **TextFieldTableCell** 内的 TextField）。录制时会对这类控件同样按 MaxAncestorHops 向上追溯：若在层数内先找到 SEMANTIC_PART（如 TableCell），则**以 part 为语义目标**，生成 SearchAndUpdate/SearchAndClick 等表级 keyword，而不是 FillEdit/ClickButton。
- 可通过系统属性 `mars.fx.semantic.config` 指定外部 JSON 路径覆盖上述配置。

### 1.4 Record 时的指令

- **keyword**：由 `FxRecordSupport.keywordForMouseClick(semanticTarget, semanticType)` 根据 **semanticType** 决定（写死在代码中）。
- **data**：由 `FxRecordSupport.dataForMouseClick(control, semanticType, keyword)` 根据 **keyword** 通过反射取控件的 `getText()`、`getValue()`、`isSelected()` 等。

### 1.5 Replay 时的行为

- **FxReplaySupport.replayJavaFxByBounds**（以及 **resolveAndReplayJavaFx** 解析出 Node 后调用的同一套逻辑）根据 **keyword** 分支。
- 所有操作均通过 **Robot** 在控件 **screenBounds 中心** 进行鼠标点击、键盘输入等，与 AWT/Swing 一致，不直接调用 JavaFX API。

---

## 2. 当前已支持的语义控件

| semanticType（规则） | keyword（Record） | data（Record） | Replay |
|---------------------|-------------------|----------------|--------|
| CHECKBOX | SetCheckBox | isSelected (true/false) | 点击中心 |
| RADIOBUTTON | SetRadioBox | getText | 点击中心 |
| TREECELL / TREEVIEW | SelectTreeList | getText | 点击中心 |
| MENUITEM | SelectMenuItem | getText | 点击中心 |
| INPUT_CONTROL（ComboBox/ChoiceBox） | SelectDropList | getValue | 点击 + 输入 data + Enter |
| TAB / TABPANE | SelectTab | Tab 的 text 或 index | 点击中心 |
| TABLECELL / TABLEVIEW（表整体语义） | SearchAndUpdate / SearchAndClick | 见下文 Table 小节 | 按 condition 定位行/列后编辑或点击 |
| TABLECELL, TABLEVIEW（未命中表逻辑时）, LISTCELL, LISTVIEW, COLUMN_HEADER, BUTTON 等 | ClickButton | 空 | 点击中心 |
| 默认 | ClickButton | 空 | 点击中心 |

规则中还有 **DatePicker、Slider、ColorPicker**（均为 INPUT_CONTROL），但 Record 侧仅对 **ComboBox/ChoiceBox** 显式映射为 SelectDropList，其余 INPUT_CONTROL 目前落回 **ClickButton**，未单独实现 keyword/data。

**暂不实现**：**DateTimePicker**、**Splitter**。

### 2.1 Table（TableView）作为整体语义对象

Table 以 **整个 TableView** 为语义对象，不以单独 cell/header 为对象。主要 keyword：**SearchAndUpdate**、**SearchAndClick**。先通过 parent/object 定位到 TableView，再依据 parameter 与 data 定位行、列。

- **Parent**：一律为最顶层 **Window/Stage/Dialog**，原则上前端容器（Stack、Panel 等）不作为 parent。
- **Object**：TableView（MARS 对象类别 **javaFxTable**）。

**Record**

1. 事件发生在 JavaFX 的 **TableCell** 或其子节点（可由语义模型识别为 Button、EditField 等）时，先判断是否为 Table 及其扩展。
2. 确认为 Table 后，将 MARS 对象类别设为 **javaFxTable**。
3. **targetColumn**：当前激活的 cell 所在列；**conditionColumns**：所有列从左到右依次为 conditionColumn1, conditionColumn2, …（含 target 列本身）。
4. **conditionValueX**：该行在修改前、各列对应的值（与 conditionColumns 一一对应）。
5. **targetValue**：cell **失去输入焦点后** 的最终值（用于 SearchAndUpdate）。
6. 组成步骤：**SearchAndUpdate**，parentObject，object，parameter=`[conditionColumn1;conditionColumn2;...];targetColumn`，data=`[conditionValue1;conditionValue2;...];targetValue`。  
   若在 cell 上 **右键**（表示后续操作 popup menu），则 keyword 为 **SearchAndClick**，data 末尾改为 `Action:RightClick` 或 `Action:DoubleClick`，不用 targetValue。

**Replay**

1. **Parameter** 格式：`[conditionColumn1;conditionColumn2;...];TargetColumn`。`[]` 内为用于定位行的 condition 列；conditionColumn 支持与表头 **正则匹配**。
2. **Data** 格式：`[ConditionValue1;ConditionValue2;...];TargetValue`。与 parameter 中的 condition 列一一对应（ConditionColumn1↔ConditionValue1…），遍历行匹配 condition 即可定位到唯一行，再按 TargetColumn 定位到该行的目标 cell。
3. **SearchAndUpdate**：将目标行滚动到 viewport 内，用鼠标激活该 cell 进入编辑，清空后以键盘输入 targetValue。
4. **SearchAndClick**：定位到目标 cell 后，根据 data 中的 Action 执行左键/双击/右键点击。

---

## 3. 如何扩展其他语义控件

按以下三步即可增加新的语义控件支持（仍不实现 DateTimePicker、Splitter）：

### 3.1 规则中增加或调整 semanticType

在 **`fx-node-classifier-rules.json`** 中：

- 为新控件增加一条规则，设置合适的 `pattern`、`category`、`boundary`、**`semanticType`**。
- 若希望沿用现有类型（如 INPUT_CONTROL），可在 Record 代码中按 `control.getClass().getName()` 再细分 keyword。

### 3.2 Record 扩展（FxRecordSupport）

- **keywordForMouseClick**：为新的 `semanticType`（或 INPUT_CONTROL + 类名）返回对应的 **keyword**（如 SetDatePicker、SetSlider 等）。
- **dataForMouseClick**：为每个新 **keyword** 增加分支，通过反射从 control 上取 **data**（如 `getValue()`、`getText()` 等），与 SetCheckBox、SelectTab 等写法一致。

### 3.3 Replay 扩展（FxReplaySupport）

- 在 **replayJavaFxByBounds** 中为新 **keyword** 增加分支，使用 **Robot** 在 bounds 中心执行：点击、输入 data、回车等（可参考 SelectDropList、FillEdit）。

所有逻辑保持在 **fx** 包下，多使用方法复用，避免重复代码。

---

## 4. 可选：配置化扩展

若希望后续扩展更多控件时少改代码，可增加配置（例如在 fx 目录下新增 JSON）：

- **semanticType → keyword** 映射表；
- **keyword → data 来源**（如 `getValue`、`getText`、`isSelected`）。

Record 时根据配置查表得到 keyword 和 data 的取值方式；Replay 仍按 keyword 分支（或同样配置化）。当前实现为写死在 `keywordForMouseClick` / `dataForMouseClick` 中。

---

## 5. 相关文件

| 文件 | 作用 |
|------|------|
| `java/marsJavaAgent/src/main/resources/fx-node-classifier-rules.json` | 语义类型规则（pattern → semanticType 等） |
| `java/marsJavaAgent/src/main/java/com/mars/javaui/fx/FxNodeClassifier.java` | 加载规则、classify、foldAndLift |
| `java/marsJavaAgent/src/main/java/com/mars/javaui/fx/FxRecordSupport.java` | keywordForMouseClick、dataForMouseClick、buildJavaFxObjectIdentifier |
| `java/marsJavaAgent/src/main/java/com/mars/javaui/fx/FxReplaySupport.java` | resolveAndReplayJavaFx、replayJavaFxByBounds（按 keyword 执行 Robot 操作） |
| `java/marsJavaAgent/src/main/java/com/mars/javaui/fx/FxReplayResolver.java` | 按 parent/object 标识解析 Node、getNodeScreenBounds |

---

*文档中标注：DateTimePicker、Splitter 暂不实现。*
