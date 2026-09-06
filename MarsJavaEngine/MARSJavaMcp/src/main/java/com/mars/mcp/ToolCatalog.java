package com.mars.mcp;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public final class ToolCatalog {
    private ToolCatalog() {
    }

    public static List<Map<String, Object>> tools() {
        List<Map<String, Object>> tools = new ArrayList<>();
        tools.add(tool("list_java_applications",
                "List Java applications currently available for automation.",
                objectSchema()));
        tools.add(tool("attach_application",
                "Attach MARS to a running Java process by applicationId or processId.",
                objectSchema("applicationId", "string", "processId", "integer")));
        tools.add(tool("detach_application",
                "Detach from the current Java application and unload the engine.",
                objectSchema()));
        tools.add(tool("get_application_state",
                "Return high-level state of the attached application: active window, tab, dialog, status.",
                objectSchema()));
        tools.add(tool("list_windows", "List visible application windows and dialogs.", objectSchema()));
        tools.add(tool("get_active_window", "Return the currently active Java window.", objectSchema()));
        tools.add(tool("inspect_screen",
                "Return a concise structured view of the current visible screen grouped by section. Prefer this over dumping the raw object tree.",
                objectSchema("window", "string")));
        tools.add(tool("get_object_tree",
                "Return the Java UI object tree when deeper structural information is required.",
                objectSchema()));
        tools.add(tool("find_controls",
                "Search controls using business or UI criteria such as Notional or Currency.",
                objectSchema("query", "string", "expectedType", "string", "window", "string", "limit", "integer")));
        tools.add(tool("resolve_control",
                "Resolve a UI description into one specific Java control. Preferred high-level locator.",
                objectSchema("description", "string", "expectedRole", "string", "context", "object")));
        tools.add(tool("resolve_field",
                "Resolve a business-visible field name such as Notional, Currency, Counterparty, or Settlement Location into the actual Java Swing/AWT/JavaFX control in the active application. Use this when the user refers to a field by business name rather than by Java class or internal id.",
                objectSchema("field", "string", "context", "object")));
        tools.add(tool("get_control_properties", "Read properties of a resolved control.",
                objectSchema("controlId", "string")));
        tools.add(tool("get_control_relations", "Return nearby or logically related controls for ambiguity resolution.",
                objectSchema("controlId", "string")));
        tools.add(tool("highlight_control", "Visually highlight a resolved control.",
                objectSchema("controlId", "string")));
        tools.add(tool("click_control", "Click a control by controlId or description.",
                objectSchema("controlId", "string", "description", "string")));
        tools.add(tool("double_click_control", "Double-click a control, table row, or tree node.",
                objectSchema("controlId", "string")));
        tools.add(tool("right_click_control", "Right-click a control to open a context menu.",
                objectSchema("controlId", "string")));
        tools.add(tool("fill_text", "Type text into an editable control.",
                objectSchema("controlId", "string", "value", "string", "clearFirst", "boolean")));
        tools.add(tool("set_field",
                "Resolve a business field and set its value, then read the value back. Preferred over toolkit-specific setters.",
                objectSchema("field", "string", "value", "string", "context", "object")));
        tools.add(tool("select_value", "Select a value in a ComboBox, List, or equivalent selector.",
                objectSchema("controlId", "string", "value", "string")));
        tools.add(tool("select_index", "Select by index when no stable business value exists.",
                objectSchema("controlId", "string", "index", "integer")));
        tools.add(tool("set_checkbox", "Set a checkbox checked or unchecked.",
                objectSchema("controlId", "string", "checked", "boolean")));
        tools.add(tool("set_radio_button", "Select a radio button by controlId or label.",
                objectSchema("controlId", "string", "value", "string")));
        tools.add(tool("select_tab", "Select a tab by title, for example Trade Blotter.",
                objectSchema("tab", "string")));
        tools.add(tool("select_tree_node", "Select a navigation tree node by path.",
                objectSchema("path", "array")));
        tools.add(tool("select_menu_item", "Select a menu item by path such as Trade / New Bond Trade.",
                objectSchema("path", "array")));
        tools.add(tool("press_key", "Press a key or chord: ENTER, TAB, ESCAPE, SPACE, F5, CTRL+S.",
                objectSchema("key", "string")));
        tools.add(tool("focus_control", "Move focus to a control.", objectSchema("controlId", "string")));
        tools.add(tool("clear_control", "Clear an editable field.", objectSchema("controlId", "string")));
        tools.add(tool("get_table_data", "Return table columns and row data.", objectSchema("controlId", "string")));
        tools.add(tool("find_table_rows", "Find table rows using business filters.",
                objectSchema("controlId", "string", "filters", "object", "sortBy", "string", "sortDirection", "string")));
        tools.add(tool("select_table_row", "Select a table row by business values, not raw index.",
                objectSchema("controlId", "string", "match", "object")));
        tools.add(tool("get_table_cell", "Read one table cell by row match and column name.",
                objectSchema("controlId", "string", "match", "object", "column", "string")));
        tools.add(tool("set_table_cell", "Set an editable table cell.",
                objectSchema("controlId", "string", "match", "object", "column", "string", "value", "string")));
        tools.add(tool("open_table_row", "Open the matched table row, typically by double-click.",
                objectSchema("controlId", "string", "match", "object")));
        tools.add(tool("open_table_context_menu", "Open the context menu on a matched table row.",
                objectSchema("controlId", "string", "match", "object")));
        tools.add(tool("read_value", "Read the current value of a control.", objectSchema("controlId", "string")));
        tools.add(tool("read_text", "Read visible text from a control or matching labels.",
                objectSchema("controlId", "string", "query", "string")));
        tools.add(tool("is_visible", "Return whether a control is visible.", objectSchema("controlId", "string")));
        tools.add(tool("is_enabled", "Return whether a control is enabled.", objectSchema("controlId", "string")));
        tools.add(tool("is_selected", "Return whether a control is selected.", objectSchema("controlId", "string")));
        tools.add(tool("verify_value", "Verify a control value equals the expected value.",
                objectSchema("controlId", "string", "expected", "string")));
        tools.add(tool("verify_text", "Verify visible text using EQUALS, CONTAINS, STARTS_WITH, ENDS_WITH, or REGEX.",
                objectSchema("controlId", "string", "expected", "string", "mode", "string", "text", "string")));
        tools.add(tool("verify_control_state", "Verify enabled, visible, selected, editable, or focused state.",
                objectSchema("controlId", "string", "expected", "object")));
        tools.add(tool("wait_for_control", "Wait until a control can be resolved or becomes interactable.",
                objectSchema("description", "string", "query", "string", "timeoutMs", "integer")));
        tools.add(tool("wait_for_state", "Wait until a field or status reaches an expected value.",
                objectSchema("controlId", "string", "expected", "string", "timeoutMs", "integer")));
        tools.add(tool("get_active_dialog", "Return the active dialog title, message, buttons, and controls.",
                objectSchema()));
        tools.add(tool("click_dialog_button", "Click a button on the active dialog, such as OK.",
                objectSchema("button", "string")));
        tools.add(tool("list_popup_items", "List items in a visible popup, context menu, or combo popup.",
                objectSchema("controlId", "string")));
        tools.add(tool("select_popup_item", "Select an item from a visible popup or combo.",
                objectSchema("item", "string", "controlId", "string")));
        tools.add(tool("capture_screenshot", "Capture a screenshot. Scope: ACTIVE_WINDOW, APPLICATION, CONTROL, SCREEN.",
                objectSchema("scope", "string", "controlId", "string")));
        tools.add(tool("capture_control_screenshot", "Capture a screenshot of one control.",
                objectSchema("controlId", "string")));
        tools.add(tool("get_last_action_evidence", "Return evidence for the last mutating action.", objectSchema()));
        tools.add(tool("execute_actions",
                "Execute a batch of high-level actions such as set_field and click_control. Use stopOnFailure to halt on the first error.",
                objectSchema("steps", "array", "stopOnFailure", "boolean")));
        tools.add(tool("run_test_case", "Execute a named or inline test case composed of action steps.",
                objectSchema("name", "string", "steps", "array")));
        return tools;
    }

    private static Map<String, Object> tool(String name, String description, Map<String, Object> schema) {
        Map<String, Object> tool = new LinkedHashMap<>();
        tool.put("name", name);
        tool.put("description", description);
        tool.put("inputSchema", schema);
        return tool;
    }

    private static Map<String, Object> objectSchema(String... props) {
        Map<String, Object> properties = new LinkedHashMap<>();
        for (int i = 0; i < props.length; i += 2) {
            properties.put(props[i], Map.of("type", props[i + 1]));
        }
        Map<String, Object> schema = new LinkedHashMap<>();
        schema.put("type", "object");
        schema.put("properties", properties);
        return schema;
    }
}
