# JavaFX Record & Semantic Model Spec (for Cursor)

Version: 1.0  
Date: 2026-03-01  
Scope: **Injected Java Agent** for JavaFX apps — recording user actions and generating MARS-style keyword test steps using a **fold/lift semantic model**.

---

## 1. Goals

1. Provide a **stable semantic model** for JavaFX UI recording that is resilient to Skin/virtualization/internal implementation changes.
2. Ensure **layout / rendering nodes** (e.g., `StackPane`, `Skin`, `VirtualFlow`, `Label` in many cases) **do not generate test steps**.
3. Generate test steps based on **semantic anchors**:
   - **SEMANTIC_CONTROL**: high-level controls (TableView, TreeView, TextField, Button, ComboBox, TabPane, etc.)
   - **SEMANTIC_PART**: parts of composite controls (TableCell/Row, TreeCell, ListCell, Tab, ColumnHeader)
4. Provide a configurable **tracking window** (ancestor hops) for semantic resolution; default `5`, adjustable via settings.
5. Provide a **keyword mapping catalog** and patterns/locators for reliable replay.
6. Special handling for **Button inside TableCell** (row action pattern).

---

## 2. Definitions

### 2.1 Categories

- **SEMANTIC_CONTROL**: a top-level control that represents user-meaningful UI entity.
- **SEMANTIC_PART**: a sub-entity inside composite controls where actions happen (cells, tabs, items).
- **STRUCTURAL_CONTAINER**: layout/skin/virtualization nodes; used for rendering; never a step target.
- **DECORATION**: non-interactive visual elements (Label/Image/Text); generally not a step target unless configured.
- **UNKNOWN**: fallback category.

### 2.2 Boundaries

- **ACTION_BOUNDARY**: a node/object that can be the target of a test step (e.g., Button, TextField, TableCell).
- **COMPOSITE_BOUNDARY**: a control that owns parts (TableView/TreeView/ListView/TabPane), typically not the direct click target.
- **NON_BOUNDARY**: never a target (structural/decoration).

### 2.3 Folding & Lifting

- **Fold**: ignore a node/object for step targeting (STRUCTURAL_CONTAINER, most DECORATION).
- **Lift**: when event target lands on a folded/detail node, climb up to the nearest SEMANTIC_PART; if none, then to SEMANTIC_CONTROL.

### 2.4 Terminal Semantic (可停止的语义)

Some **SEMANTIC_CONTROL** nodes are *terminal*: once we hit them during lift, we **stop immediately** and use them as the step target—we do **not** keep walking ancestors to look for a composite (SEMANTIC_PART or COMPOSITE_BOUNDARY).

**Why not treat them as composite?**  
Composite semantics (SEMANTIC_PART) mean “part of a composite control” (e.g. TreeCell belongs to TreeView, Tab belongs to TabPane). A **MenuBarButton**’s parent is **MenuBar**, which is STRUCTURAL_CONTAINER, not a composite we prefer. So MenuBarButton is not a “part” of a composite; marking it as SEMANTIC_PART would misuse the category.

**Why “terminal” instead of “just use simple semantic”?**  
For ordinary simple semantics (Button, TextField), we *do* keep walking up: we might find a SEMANTIC_PART (e.g. Tab, TableCell) and prefer that as the target. For MenuBarButton / MenuItem, there is no such higher semantic—they are already the top-level menu entry or item. So we define a separate concept: **terminal semantic** = “stop here, no need to look up.”

**Current terminal semantics**  
- `semanticType == "MENUITEM"` (covers MenuBarButton, MenuItemContainer, MenuItem in rules).  
Extensible later (e.g. more semanticTypes or a rule field like `stopsLift`).

---

## 3. High-level Architecture

### 3.1 Pipeline

1. **Event capture** (mouse/keyboard/action/selection/edit-commit).
2. **Normalize** event target (e.g., `LabeledText` → owning `Label`/`Button`).
3. **Classify** target into category/boundary via a table-driven classifier.
4. **Resolve semantic hit**:
   - Prefer **SEMANTIC_PART** (TableCell/TreeCell/ListCell/Tab/etc.)
   - Resolve **owner** (TableView/TreeView/ListView/TabPane/etc.) and **attributes** (column, row key, tree path, index).
5. **Map to keyword** using catalog + event context (click vs update vs right click).
6. **Emit step**: `keyword + object + parameters + data`.

### 3.2 JavaFX Thread Rule

All UI access must run on **JavaFX Application Thread**. Provide helper:

- `runOnFxThreadAndWait(Callable<T>)`

---

## 4. Semantic Catalogs

> Catalogs must be maintained in config (JSON recommended). The code should have a minimal built-in default set and allow project overrides.

### 4.1 SEMANTIC_CONTROL Anchors → Keywords

| Anchor base type | Examples | Keyword |
|---|---|---|
| `javafx.scene.control.ButtonBase` | Button, ToggleButton (non-radio/checkbox) | `ClickButton` |
| `javafx.scene.control.TextInputControl` | TextField, TextArea, PasswordField | `FillEdit` |
| `javafx.scene.control.ComboBoxBase` | ComboBox, DatePicker | `SelectDropDown` (DatePicker optional `SelectDate`) |
| `javafx.scene.control.ChoiceBox` | ChoiceBox | `SelectDropDown` |
| `javafx.scene.control.CheckBox` | CheckBox | `SetBoxCheckBox` |
| `javafx.scene.control.RadioButton` | RadioButton | `SetBoxRadio` |
| `javafx.scene.control.TabPane` (+ Tab as part) | TabPane | `SelectTab` (targets `Tab`) |
| `javafx.scene.control.MenuItem` | MenuItem, CheckMenuItem, RadioMenuItem | `SelectMenuItem` |
| `javafx.stage.Window` | Stage, PopupWindow | `PegWindow` |
| `javafx.scene.control.Dialog` / `DialogPane` | Alert, Dialog | `PegWindow` (or `HandleDialog`) |
| `javafx.scene.control.ListView` | ListView | `SelectListItem` (targets `ListCell`) |
| `javafx.scene.control.TreeView` | TreeView | `SelectTreeItem` (targets `TreeCell`) |
| `javafx.scene.control.TableView` | TableView | `SearchAndClick` / `SearchAndUpdate` (targets `TableCell`) |

**Notes**
- For Radio/Checkbox, do **not** map them to ClickButton even though they extend ButtonBase.
- Composite controls normally produce steps at **parts** (cells/tabs/items), with the control as owner.

### 4.2 SEMANTIC_PART Anchors → Owner & Attributes

| Part anchor | Owner control | Attributes to resolve |
|---|---|---|
| `javafx.scene.control.TableCell` | TableView | columnId/columnText + rowKey/rowIndex |
| `javafx.scene.control.TableRow` | TableView | rowKey/rowIndex |
| `javafx.scene.control.TreeCell` | TreeView | treePath (TreeItem value/text path) |
| `javafx.scene.control.ListCell` | ListView | itemKey/itemText/index |
| `javafx.scene.control.Tab` | TabPane | tabText/index |
| Column header (style `.column-header`) | TableView | columnId/columnText |

### 4.3 FOLD Catalog (Never Step Targets)

**Skin & Internal rendering**
- `javafx.scene.control.Skin`
- `javafx.scene.control.SkinBase`
- anything under package `javafx.scene.control.skin.*`
- anything under package `com.sun.javafx.*` (e.g., `LabeledText`)

**Layout containers**
- `javafx.scene.layout.Pane` (HBox/VBox/StackPane/BorderPane/GridPane/AnchorPane/FlowPane/TilePane/…)
- `javafx.scene.layout.Region`
- `javafx.scene.Group`

**Decorations (default fold)**
- `javafx.scene.control.Label` (unless configured as interactive)
- `javafx.scene.image.ImageView`
- `javafx.scene.shape.Shape`
- `javafx.scene.text.Text`

---

## 5. Config Schema (JSON)

```json
{
  "SemanticTracking": {
    "MaxAncestorHops": 5,
    "PreferPartOverControl": true,
    "PreferModelOverView": true,
    "UseCssClassAnchors": true,
    "FallbackToSelectionModel": true
  },
  "SemanticPolicy": {
    "ButtonInTableCell": "ROW_ACTION",
    "ActionNamePriority": ["text", "tooltip", "graphic"],
    "RowKeyStrategy": "ITEM_PRIMARY_KEY_OR_INDEX"
  },
  "SemanticExtensions": [
    { "class": "com.acme.controls.IconButton", "as": "SEMANTIC_CONTROL", "keyword": "ClickButton" }
  ]
}
```

Settings UI should allow editing:
- `MaxAncestorHops`
- `ButtonInTableCell` mode

---

## 6. Core Algorithms (Pseudo-code)

### 6.1 Classification (table-driven; avoid `$1$1` / skin-internals)

```text
classify(obj):
  if obj implements Skin or package startsWith javafx.scene.control.skin or package startsWith com.sun.javafx:
      return STRUCTURAL_CONTAINER

  if obj is Pane/Region/Group or (UseCssClassAnchors and hasStyleClass(obj,"virtual-flow")):
      return STRUCTURAL_CONTAINER

  if obj is TableCell/TreeCell/ListCell:
      return SEMANTIC_PART

  if obj is Tab (model) or (UseCssClassAnchors and hasStyleClass(obj,"tab")):
      return SEMANTIC_PART

  if obj is TableView/TreeView/ListView/TabPane/TextInputControl/ButtonBase/ComboBoxBase/ChoiceBox:
      return SEMANTIC_CONTROL

  if obj is Label/ImageView/Text/Shape:
      if isInteractable(obj): return SEMANTIC_CONTROL  (configurable exception)
      return DECORATION

  if isInteractable(obj): return SEMANTIC_CONTROL
  return UNKNOWN
```

### 6.2 Fold/Lift Resolution

```text
resolveSemantic(eventTarget, cfg):
  n = normalize(eventTarget)

  // Skin → skinnable (model) if possible
  if n implements Skin:
      n = n.getSkinnable()

  // Prefer SEMANTIC_PART
  part = liftToCategory(n, SEMANTIC_PART, cfg.MaxAncestorHops, cfg)
  if part != null:
      owner = resolveOwner(part)
      attrs = resolvePartAttributes(part)
      return SemanticHit(owner, part, attrs)

  // Else SEMANTIC_CONTROL (see 2.4 for terminal semantic)
  ctrl = liftToCategory(n, SEMANTIC_CONTROL, cfg.MaxAncestorHops, cfg)
  if ctrl != null:
      return SemanticHit(ctrl, ctrl, {})

  if cfg.FallbackToSelectionModel:
      return fallbackBySelectionModel(n)

  return null
```

**Lift loop (ancestor walk)**  
When walking ancestors from event target:

1. If current node is **SEMANTIC_PART** (composite part) → use it as target, **break**.
2. If current node is **SEMANTIC_CONTROL** and **terminal** (e.g. semanticType MENUITEM) → use it as target, **break** (see §2.4).
3. If current node is SEMANTIC_CONTROL but not terminal → record as candidate, **continue** walking up to see if a composite part exists above.
4. If no composite part found, use the candidate simple semantic as target.


### 6.3 Owner/Attribute Resolution

**TableCell**
- owner: `cell.getTableView()`
- column: `cell.getTableColumn()` → `getText()` or `getId()`
- row: use `cell.getIndex()` and resolve row item key if possible (RowKeyStrategy)

**TreeCell**
- owner: `cell.getTreeView()`
- tree item: `cell.getTreeItem()` → build path from root values/text.

**ListCell**
- owner: `cell.getListView()`
- item: `cell.getItem()` or `cell.getIndex()`

**TabHeaderSkin (injected agent)**
- `((Skin)headerSkin).getSkinnable()` → `Tab`
- owner: `tab.getTabPane()`
- caption: `tab.getText()`

### 6.4 Menu path (SelectMenuItem data)

Step data for `SelectMenuItem` is a path string from top-level menu to the clicked item, e.g. `"File;Edit;Copy"`. Menu/semantic object is usually found by **lifting up** (e.g. the hit node is **MenuBarButton** or a popup menu node), so path building and text resolution work as follows.

**Path chain (model)**  
- If the semantic control is a **Skin** (e.g. MenuBarButton), resolve to the model first: `getSkinnable(control)` → Menu/MenuItem.  
- Build the path by walking **up** from that model: `getParentMenu()` repeatedly to get the chain root → … → leaf.  
- So the chain is always **model** objects (Menu/MenuItem), and `getParentMenu()` is available on each.

**Segment text (look down when needed)**  
For each segment in the path we need the **display text**. The semantic object we have may be the model (MenuItem/Menu have `getText()`) or a visual (e.g. MenuBarButton); for the visual, the text is often on a **child** node. Therefore:

1. Try **model** `getText(node)` first.  
2. If the node is a Skin, try `getText(getSkinnable(node))`.  
3. If still no text, **look down**: from this node’s children (e.g. `getChildren()` on Parent), take the first non-empty `getText()` from any direct or nested child.

So: **semantic object is resolved by going up; its text is resolved by going down when the object is a visual.**

---

## 7. Keyword Mapping Rules (Event-aware)

### 7.1 Click vs Update for TableView

- **SearchAndClick**
  - Mouse PRIMARY click/double-click on TableCell/Row
  - Mouse SECONDARY click → `SearchAndClick(..., button="RIGHT")` (context menu)
- **SearchAndUpdate**
  - edit commit events: `TableCell.commitEdit(...)`, Enter confirmation, focus-lost commit, or value/text change in editor + commit.

### 7.2 MenuItem
- `SelectMenuItem(menuPath)` with menu path from root menu to leaf (e.g. `"File;Edit;Copy"`). Path building and segment text resolution: see **§6.4 Menu path (SelectMenuItem data)** (model chain via `getParentMenu`; text from model `getText` or by looking down into children when the semantic object is a visual).

### 7.3 Tab
- `SelectTab(TabPanePattern, TabTextOrIndex)`

---

## 8. Patterns / Locators

### 8.1 General
Prefer stable identifiers in order:
1. `node.getId()`
2. (for model objects) `Tab.getText()`, `TableColumn.getId()/getText()`
3. index as last resort (with owner context)

### 8.2 Examples

- Button:
  - `BUTTON[id="saveBtn"]` or `BUTTON[text="Save"]`
- TextField:
  - `TEXTFIELD[id="username"]`
- Tab:
  - `TABPANE[id="mainTabs"].TAB[text="Settings", index=2]`
- Table cell:
  - `TABLEVIEW[id="orders"].CELL[rowKey="ORD-1001", column="Status"]`
  - fallback: `TABLEVIEW[id="orders"].CELL[rowIndex=5, column="Status"]`
- Tree item:
  - `TREEVIEW[id="nav"].ITEM[path="/Root/Admin/Users"]`

---

## 9. Special Case: Button inside TableCell (Row Action)

Problem: a `Button` embedded in a `TableCell` represents a **row operation** (Edit/Delete/View), not an independent global Button.

### 9.1 Recommended default behavior
Config: `"ButtonInTableCell": "ROW_ACTION"`

Resolution:
1. Event target is ButtonBase.
2. Lift to nearest TableCell/TableRow to get row context.
3. Derive `ActionName` from Button (priority: text → tooltip → graphic).
4. Emit a **row action step** (preferred), or a constrained ClickButton with row context.

### 9.2 Step formats

**Option A (preferred, new keyword)**
- `RowAction(TableView, RowKey, ActionName)`

**Option B (reuse existing keyword)**
- `SearchAndClick(TableView, RowKey, Column="Actions", SubAction="Delete")`

**Option C (keep ClickButton but contextual locator)**
- `ClickButton(Button[text="Delete"] @ TABLEVIEW[id="orders"].ROW[rowKey="ORD-1001"])`

### 9.3 When to NOT use RowAction
If the button is not semantically tied to a row (e.g., floating toolbar button), do normal `ClickButton`.

Heuristic:
- If Button has an ancestor `TableCell` within `MaxAncestorHops + extraCellHops` (recommend +3), treat as in-cell action.

---

## 10. Implementation Notes (Injected Agent)

1. **Never use** internal class names like `TableColumn$1$1` or `TreeViewSkin$1` as anchors.
2. You may see objects like:
   - `TabPaneSkin$TabHeaderSkin`
   - `TabPaneSkin$TabHeaderSkin$4`
   - `Label`, `com.sun.javafx.scene.control.LabeledText`
   Always resolve via Skin → skinnable (Tab) or via lift to part (Cell).
3. Ensure all UI reads happen on FX thread.
4. Keep catalogs/configs decoupled from recorder so you can iterate quickly.

---

## 11. Deliverables (Cursor tasks)

1. Implement `SemanticClassifier` (table-driven + config extensions).
2. Implement `SemanticResolver`:
   - normalize
   - Skin→skinnable
   - lift to part/control with MaxAncestorHops
   - owner/attrs extraction for Table/Tree/List/Tab
3. Implement `KeywordMapper` (event-aware mapping; especially Table click/update/right-click).
4. Implement `PatternBuilder` for stable locators.
5. Implement Settings UI for:
   - `MaxAncestorHops` (default 5)
   - `ButtonInTableCell` mode
6. Add unit tests / integration tests for:
   - Tab header chain (TabHeaderSkin → Tab text)
   - Label inside TableCell resolves to Cell+Column
   - Button-in-cell resolves to RowAction
   - Right click yields SearchAndClick(button="RIGHT")

---

## 12. Acceptance Criteria

- No test steps are produced for: Skin, Pane/Region, VirtualFlow, LabeledText, decorative Label (unless configured).
- Tab selection step uses **Tab model text** (not header label implementation detail).
- Table steps use **row+column semantics**; right-click is represented explicitly.
- Button inside TableCell produces **RowAction** (default) and includes row identity.
- `MaxAncestorHops` (default 5) changes behavior deterministically and is persisted via settings.