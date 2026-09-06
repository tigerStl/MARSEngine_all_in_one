# MARS Java Automation MCP — Recommended Tool Specification

## 1. Purpose

This document defines the recommended MCP tool surface for **MARS Java Automation**.

The MCP is intended for Codex, Cursor Agent, GitHub Copilot Agent, Claude Code, and other MCP-compatible agents. It should allow an Agent to discover a running Java application, understand current state, resolve business-visible fields to actual Java controls, execute deterministic UI actions, verify results, and collect evidence.

Supported GUI technologies may include Swing, AWT, and JavaFX. The MCP should hide toolkit-specific implementation details whenever possible.

```text
AI Agent
   ↓
MARS Java MCP
   ↓
MARS Java Automation Abstraction
   ↓
Swing / AWT / JavaFX Executors
   ↓
Target Java Application
```

## 2. Design Principles

The Agent should not need to know whether a control is a `JTextField`, `JComboBox`, `JCheckBox`, JavaFX `TextField`, or JavaFX `ComboBox`. It should operate through higher-level actions such as `FillText`, `SelectValue`, `Click`, `Check`, `ReadValue`, and `VerifyText`.

The MCP should separate **reasoning** from **execution**. The Agent decides what needs to be done; MARS determines which actual Java object to operate and whether the operation succeeded.

## 3. Tool Categories

Recommended groups:

```text
1. Session / Application
2. Discovery
3. Control Resolution
4. Action
5. Verification
6. Workflow / Evidence
```

# 4. Session / Application Tools

## 4.1 `list_java_applications`

Lists Java applications currently available for automation.

Example output:

```json
{
  "applications": [
    {
      "applicationId": "northstar-001",
      "processId": 18244,
      "name": "Northstar Capital Markets Workstation",
      "technology": "Swing",
      "status": "CONNECTED"
    }
  ]
}
```

## 4.2 `attach_application`

Attach MARS to a Java process/application.

Input:

```json
{
  "applicationId": "northstar-001"
}
```

Alternative:

```json
{
  "processId": 18244
}
```

Output:

```json
{
  "success": true,
  "sessionId": "mars-session-0001",
  "application": {
    "name": "Northstar Capital Markets Workstation",
    "technology": "Swing"
  }
}
```

## 4.3 `detach_application`

Detach from the current Java application.

## 4.4 `get_application_state`

Return the high-level state of the attached application.

Example:

```json
{
  "application": "Northstar Capital Markets Workstation",
  "activeWindow": "Bond Trade Entry",
  "activeTab": "Trade Entry",
  "modalDialog": null,
  "busy": false,
  "statusText": "Ready"
}
```

# 5. Window / Screen Discovery Tools

## 5.1 `list_windows`

List relevant application windows, including main windows, dialogs, and popup windows where appropriate.

## 5.2 `get_active_window`

Return the currently active Java window.

## 5.3 `inspect_screen`

Return a concise structured representation of the current visible screen. Do not dump the complete raw object tree by default.

Example output:

```json
{
  "window": "Bond Trade Entry",
  "sections": [
    {
      "name": "Product",
      "controls": [
        {
          "type": "TextField",
          "label": "Notional",
          "value": "",
          "enabled": true
        },
        {
          "type": "ComboBox",
          "label": "Currency",
          "value": "USD"
        }
      ]
    }
  ]
}
```

## 5.4 `get_object_tree`

Return the Java UI object tree when deeper structural information is required.

Recommended node data:

```json
{
  "objectId": "obj-182",
  "className": "javax.swing.JTextField",
  "name": "bond.notional",
  "text": "",
  "accessibleName": "Notional",
  "enabled": true,
  "visible": true,
  "bounds": {
    "x": 412,
    "y": 288,
    "width": 160,
    "height": 24
  },
  "children": []
}
```

# 6. Control Discovery / Resolution Tools

This is one of the most important MCP layers. The Agent should normally call these tools rather than parse the entire object tree itself.

## 6.1 `find_controls`

Search controls using business/UI criteria.

Input:

```json
{
  "query": "Notional",
  "expectedType": "EDITABLE",
  "window": "Bond Trade Entry",
  "limit": 10
}
```

Output:

```json
{
  "matches": [
    {
      "controlId": "obj-182",
      "type": "TextField",
      "label": "Notional",
      "name": "bond.notional",
      "value": "",
      "confidence": 0.99
    }
  ]
}
```

## 6.2 `resolve_control`

Resolve a user's UI description into one specific Java control. This should be the preferred high-level locator.

Input:

```json
{
  "description": "Notional",
  "expectedRole": "editable field",
  "context": {
    "window": "Bond Trade Entry",
    "section": "Product"
  }
}
```

Output:

```json
{
  "status": "RESOLVED",
  "control": {
    "controlId": "obj-182",
    "type": "TextField",
    "label": "Notional",
    "value": ""
  },
  "confidence": 0.98
}
```

Possible status values:

```text
RESOLVED
AMBIGUOUS
NOT_FOUND
NOT_INTERACTABLE
```

## 6.3 `resolve_field`

Resolve a business field such as `Notional`, `Currency`, `Counterparty`, `Settlement Location`, or `Price` without requiring the Agent to know the Java control type.

Input:

```json
{
  "field": "Counterparty",
  "context": {
    "window": "Bond Trade Entry"
  }
}
```

Output:

```json
{
  "status": "RESOLVED",
  "field": "Counterparty",
  "controlId": "obj-203",
  "controlType": "TextField",
  "confidence": 0.97
}
```

This method is strongly recommended.

## 6.4 `get_control_properties`

Read relevant properties of a control.

## 6.5 `get_control_relations`

Return nearby or logically related controls. Useful for ambiguity resolution.

Recommended relations:

```text
LEFT_OF
RIGHT_OF
ABOVE
BELOW
NEAREST
INSIDE
CONTAINS
SAME_ROW
SAME_COLUMN
```

## 6.6 `highlight_control`

Visually highlight a resolved control for debugging or user confirmation.

# 7. Generic Action Tools

Prefer a compact, stable set of high-level actions.

## 7.1 `click_control`

Click a control.

## 7.2 `double_click_control`

Useful for table rows, list items, and tree nodes.

## 7.3 `right_click_control`

Open context menus.

## 7.4 `fill_text`

Input:

```json
{
  "controlId": "obj-182",
  "value": "100000000",
  "clearFirst": true
}
```

Output:

```json
{
  "success": true,
  "actualValue": "100000000"
}
```

## 7.5 `set_field`

Recommended higher-level wrapper.

Input:

```json
{
  "field": "Notional",
  "value": "100000000",
  "context": {
    "window": "Bond Trade Entry"
  }
}
```

MARS should:

```text
Resolve field
→ determine actual Java control
→ perform correct operation
→ read value back
```

## 7.6 `select_value`

Select a value in ComboBox, List, Choice, or equivalent selector.

## 7.7 `select_index`

Select by index when no stable business value exists. Use sparingly.

## 7.8 `set_checkbox`

Input:

```json
{
  "controlId": "obj-standard-ssi",
  "checked": true
}
```

## 7.9 `set_radio_button`

## 7.10 `select_tab`

Input:

```json
{
  "tab": "Trade Blotter"
}
```

## 7.11 `select_tree_node`

Input:

```json
{
  "path": [
    "Trading",
    "Fixed Income",
    "Bond Entry"
  ]
}
```

## 7.12 `select_menu_item`

Input:

```json
{
  "path": [
    "Trade",
    "New Bond Trade"
  ]
}
```

## 7.13 `press_key`

Examples:

```text
ENTER
TAB
ESCAPE
SPACE
F5
CTRL+S
```

## 7.14 `focus_control`

Move focus to a specific control.

## 7.15 `clear_control`

Clear editable field contents.

# 8. Table Tools

Financial applications are table-heavy. Table support should be first-class.

## 8.1 `get_table_data`

Return table columns and row data.

## 8.2 `find_table_rows`

Find rows using business filters.

Input:

```json
{
  "controlId": "trade-blotter",
  "filters": {
    "Counterparty": "FHLB",
    "Status": "Booked"
  },
  "sortBy": "Last Updated",
  "sortDirection": "DESC"
}
```

## 8.3 `select_table_row`

Prefer matching by business values instead of raw row index.

Example:

```json
{
  "controlId": "trade-blotter",
  "match": {
    "Trade ID": "FI-20260905-000041"
  }
}
```

## 8.4 `get_table_cell`

Input:

```json
{
  "controlId": "trade-blotter",
  "match": {
    "Trade ID": "FI-20260905-000041"
  },
  "column": "Status"
}
```

## 8.5 `set_table_cell`

For editable tables.

## 8.6 `open_table_row`

## 8.7 `open_table_context_menu`

# 9. Read / Verification Tools

Verification should be explicit rather than inferred only from screenshots.

## 9.1 `read_value`

## 9.2 `read_text`

Read visible text from labels, status bars, buttons, dialogs, and text controls.

## 9.3 `is_visible`

## 9.4 `is_enabled`

## 9.5 `is_selected`

## 9.6 `verify_value`

Input:

```json
{
  "controlId": "obj-currency",
  "expected": "USD"
}
```

Output:

```json
{
  "passed": true,
  "expected": "USD",
  "actual": "USD"
}
```

## 9.7 `verify_text`

Recommended match modes:

```text
EQUALS
CONTAINS
STARTS_WITH
ENDS_WITH
REGEX
```

## 9.8 `verify_control_state`

Verify enabled, visible, selected, editable, or focused state.

## 9.9 `wait_for_control`

Wait until a control appears or becomes interactable.

## 9.10 `wait_for_state`

Wait until a field/status reaches an expected value.

# 10. Dialog / Popup Tools

## 10.1 `get_active_dialog`

Return dialog title, message, buttons, and relevant controls.

## 10.2 `click_dialog_button`

## 10.3 `list_popup_items`

For context menus, ComboBox popup, and menu popup.

## 10.4 `select_popup_item`

# 11. Screenshot / Evidence Tools

## 11.1 `capture_screenshot`

Recommended scopes:

```text
ACTIVE_WINDOW
APPLICATION
CONTROL
SCREEN
```

## 11.2 `capture_control_screenshot`

## 11.3 `get_last_action_evidence`

Example output:

```json
{
  "action": "set_field",
  "field": "Currency",
  "before": "EUR",
  "requested": "USD",
  "after": "USD",
  "success": true,
  "timestamp": "2026-09-06T01:00:14-05:00"
}
```

# 12. Batch / Workflow Tools

Agents should not need dozens of round trips for common workflows.

## 12.1 `execute_actions`

Input:

```json
{
  "steps": [
    {
      "action": "set_field",
      "field": "Notional",
      "value": "100000000"
    },
    {
      "action": "set_field",
      "field": "Currency",
      "value": "USD"
    },
    {
      "action": "set_field",
      "field": "Counterparty",
      "value": "FHLB"
    }
  ],
  "stopOnFailure": true
}
```

## 12.2 `run_test_case`

Execute a pre-defined or Agent-generated test case.

## 12.3 `execute_business_task`

Optional higher-level interface. Add only after lower-level deterministic tools are stable.

Example:

```json
{
  "task": "Create Bond Trade",
  "parameters": {
    "Side": "Buy",
    "Notional": "100000000",
    "Currency": "USD",
    "Counterparty": "FHLB",
    "Price": "98.25",
    "Settlement Location": "NYC"
  }
}
```

# 13. Recording / Replay Tools

If MARS already supports recording/replay, expose it through MCP.

```text
start_recording
stop_recording
get_recorded_steps
replay_recording
save_workflow
run_workflow
```

# 14. Recommended Error Model

Every tool should return structured errors.

Example:

```json
{
  "success": false,
  "error": {
    "code": "CONTROL_NOT_FOUND",
    "message": "Unable to resolve field 'Counterparty'.",
    "recoverable": true,
    "suggestedAction": "Call find_controls with query 'Counterparty'."
  }
}
```

Recommended error codes:

```text
APPLICATION_NOT_FOUND
APPLICATION_NOT_ATTACHED
WINDOW_NOT_FOUND
CONTROL_NOT_FOUND
CONTROL_AMBIGUOUS
CONTROL_NOT_VISIBLE
CONTROL_DISABLED
CONTROL_NOT_EDITABLE
VALUE_NOT_AVAILABLE
ACTION_FAILED
VALIDATION_FAILED
TIMEOUT
DIALOG_BLOCKING
POPUP_NOT_FOUND
TABLE_ROW_NOT_FOUND
UNSUPPORTED_CONTROL
UNSUPPORTED_ACTION
STALE_CONTROL_REFERENCE
SESSION_EXPIRED
INTERNAL_ERROR
```

# 15. Confidence and Ambiguity

Resolution tools should return confidence and candidates rather than silently guessing.

Example:

```json
{
  "status": "AMBIGUOUS",
  "candidates": [
    {
      "controlId": "obj-101",
      "label": "Trade Currency",
      "confidence": 0.79
    },
    {
      "controlId": "obj-144",
      "label": "Settlement Currency",
      "confidence": 0.73
    }
  ]
}
```

Suggested configurable thresholds:

```text
>= 0.90     auto resolve
0.70–0.89   return candidate + evidence
< 0.70      ambiguous / not found
```

# 16. Stable Control References

When a control is resolved, return a runtime `controlId` so the Agent can reuse it without repeating resolution.

If the UI changes and the reference becomes stale, return:

```text
STALE_CONTROL_REFERENCE
```

and allow re-resolution.

# 17. Suggested Minimal V1 Tool Set

For a compact public MCP surface, expose these first:

```text
list_java_applications
attach_application
get_application_state
list_windows
inspect_screen
find_controls
resolve_control
resolve_field
get_control_properties
highlight_control
click_control
fill_text
set_field
select_value
set_checkbox
set_radio_button
select_tab
select_tree_node
select_menu_item
press_key
get_table_data
find_table_rows
get_table_cell
select_table_row
read_value
read_text
verify_value
verify_text
verify_control_state
wait_for_control
get_active_dialog
click_dialog_button
select_popup_item
capture_screenshot
execute_actions
run_test_case
```

# 18. Recommended V2 Tool Set

Add after V1 stabilizes:

```text
get_object_tree
get_control_relations
double_click_control
right_click_control
focus_control
clear_control
set_table_cell
open_table_row
open_table_context_menu
list_popup_items
capture_control_screenshot
get_last_action_evidence
start_recording
stop_recording
get_recorded_steps
replay_recording
save_workflow
run_workflow
```

# 19. Recommended V3 / Agent-Level Tools

Only after deterministic execution is reliable:

```text
execute_business_task
verify_business_task
run_regression
test_requirement
evaluate_release_readiness
```

These should not replace the deterministic execution tools underneath them.

# 20. Tool Naming Guidance

Prefer:

```text
set_field
resolve_field
select_value
verify_value
```

Avoid toolkit-specific names such as:

```text
setJTextField
clickJButton
selectJavaFxComboBox
```

Toolkit differences belong inside MARS.

# 21. Example Agent Interaction

User:

> Create a USD bond trade for 100m with FHLB at 98.25 and settle in NYC.

Recommended Agent sequence:

```text
1. get_application_state
2. resolve_field("Notional")
3. set_field("Notional", "100000000")
4. set_field("Currency", "USD")
5. set_field("Counterparty", "FHLB")
6. set_field("Price", "98.25")
7. set_field("Settlement Location", "NYC")
8. resolve_control("Validate Trade")
9. click_control(...)
10. verify_text("Validation", "PASSED")
11. click_control("Save Trade")
12. get_active_dialog()
13. read Trade ID
14. click_dialog_button("OK")
15. select_tab("Trade Blotter")
16. find_table_rows({"Trade ID": generatedTradeId})
17. get_table_cell(..., "Status")
18. verify Status == "Booked"
```

# 22. Example Tool Description for Agent Discovery

Example `resolve_field` description:

```text
Resolve a business-visible field name such as "Notional",
"Currency", "Counterparty", or "Settlement Location" into the
actual Java Swing/AWT/JavaFX control in the active application.
Use this tool when the user refers to a field by business name
rather than by Java control class or internal object identifier.
The method may use visible labels, accessible properties,
container relationships, geometry, and object metadata.
```

Strong MCP descriptions help Codex/Cursor choose the right tool.

# 23. Responsibility Boundary

Recommended boundary:

```text
User
  ↓
Agent
  ├─ intent understanding
  ├─ task planning
  ├─ business reasoning
  └─ ambiguity clarification
        ↓
MARS MCP
  ├─ application discovery
  ├─ control resolution
  ├─ deterministic operation
  ├─ state readback
  ├─ verification
  └─ evidence
```

# 24. Final Recommended Architecture

```text
                Codex / Cursor / Other Agent
                           │
                    Natural Language
                           │
                    Agent Planning
                           │
                           ▼
                    MARS Java MCP
                           │
       ┌───────────────────┼───────────────────┐
       │                   │                   │
   Discovery           Resolution          Verification
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                     Action Layer
                           │
          ┌────────────────┼────────────────┐
          │                │                │
        Swing             AWT             JavaFX
          │                │                │
          └────────────────┼────────────────┘
                           │
                  Java Application
```

The long-term value of MARS Java MCP is not merely exposing `click` and `fill_text`.

Its core role should be:

```text
Business/UI intent
      ↓
Real Java object
      ↓
Deterministic operation
      ↓
Verified application state
```

That is the capability AI Agents need in order to reliably operate enterprise Java applications.
