package com.mars.javaengine.automation;

import com.mars.javaengine.ui.UiObjectInfo;
import com.mars.javaengine.ui.UiObjectScanner;
import com.mars.javaengine.util.JsonUtil;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.function.Function;
import java.util.logging.Logger;

public class CommandDispatcher {
    private final UiAutomationService automation;
    private final Path swapDirectory;
    private final Logger logger;
    private final int highlightLimit;
    private final Map<String, Function<Map<String, Object>, Map<String, Object>>> handlers = new HashMap<>();

    public CommandDispatcher(UiAutomationService automation, Path swapDirectory, Logger logger, int highlightLimit) {
        this.automation = automation;
        this.swapDirectory = swapDirectory;
        this.logger = logger;
        this.highlightLimit = highlightLimit;
        register();
    }

    public String handleRaw(String json) {
        try {
            @SuppressWarnings("unchecked")
            Map<String, Object> payload = JsonUtil.fromJson(json, Map.class);
            String type = String.valueOf(payload.get("MessageType"));
            Object requestId = payload.get("RequestId");
            @SuppressWarnings("unchecked")
            Map<String, Object> info = payload.get("MessageInfo") instanceof Map
                    ? (Map<String, Object>) payload.get("MessageInfo")
                    : payload;
            Map<String, Object> data = dispatch(type, info == null ? Map.of() : info);
            Map<String, Object> response = new LinkedHashMap<>();
            response.put("MessageType", type + (String.valueOf(type).endsWith("_RESULT") ? "" : "_RESULT"));
            if (requestId != null) {
                response.put("RequestId", requestId);
            }
            response.putAll(data);
            if (!response.containsKey("success") && !response.containsKey("status") && !response.containsKey("error")) {
                response.put("success", true);
            }
            return JsonUtil.toJson(response);
        } catch (Exception ex) {
            logger.warning("Command failed: " + ex.getMessage());
            Map<String, Object> error = McpResponses.error("INTERNAL_ERROR",
                    ex.getMessage() == null ? "Internal error" : ex.getMessage(), false, null);
            return JsonUtil.toJson(error);
        }
    }

    public Map<String, Object> dispatch(String type, Map<String, Object> info) {
        String key = normalize(type);
        if ("get_uiobjects_all".equals(key)) {
            return scanAll();
        }
        if ("unload_engine".equals(key)) {
            return Map.of("success", true, "action", "UNLOAD_ENGINE");
        }
        Function<Map<String, Object>, Map<String, Object>> handler = handlers.get(key);
        if (handler == null) {
            return McpResponses.error("UNSUPPORTED_ACTION", "Unsupported action: " + type, false, null);
        }
        return handler.apply(info == null ? Map.of() : info);
    }

    private void register() {
        handlers.put("get_application_state", info -> automation.getApplicationState());
        handlers.put("list_windows", info -> automation.listWindows());
        handlers.put("get_active_window", info -> automation.getActiveWindow());
        handlers.put("inspect_screen", info -> automation.inspectScreen());
        handlers.put("get_object_tree", info -> automation.getObjectTree());
        handlers.put("find_controls", automation::findControls);
        handlers.put("resolve_control", automation::resolveControl);
        handlers.put("resolve_field", automation::resolveField);
        handlers.put("get_control_properties", automation::getControlProperties);
        handlers.put("get_control_relations", automation::getControlRelations);
        handlers.put("highlight_control", automation::highlightControl);
        handlers.put("click_control", automation::clickControl);
        handlers.put("double_click_control", automation::doubleClickControl);
        handlers.put("right_click_control", automation::rightClickControl);
        handlers.put("fill_text", automation::fillText);
        handlers.put("set_field", automation::setField);
        handlers.put("select_value", automation::selectValue);
        handlers.put("select_index", automation::selectIndex);
        handlers.put("set_checkbox", automation::setCheckbox);
        handlers.put("set_radio_button", automation::setRadioButton);
        handlers.put("select_tab", automation::selectTab);
        handlers.put("select_tree_node", automation::selectTreeNode);
        handlers.put("select_menu_item", automation::selectMenuItem);
        handlers.put("press_key", automation::pressKey);
        handlers.put("focus_control", automation::focusControl);
        handlers.put("clear_control", automation::clearControl);
        handlers.put("get_table_data", automation::getTableData);
        handlers.put("find_table_rows", automation::findTableRows);
        handlers.put("select_table_row", automation::selectTableRow);
        handlers.put("get_table_cell", automation::getTableCell);
        handlers.put("set_table_cell", automation::setTableCell);
        handlers.put("open_table_row", automation::openTableRow);
        handlers.put("open_table_context_menu", automation::openTableContextMenu);
        handlers.put("read_value", automation::readValue);
        handlers.put("read_text", automation::readText);
        handlers.put("is_visible", automation::isVisible);
        handlers.put("is_enabled", automation::isEnabled);
        handlers.put("is_selected", automation::isSelected);
        handlers.put("verify_value", automation::verifyValue);
        handlers.put("verify_text", automation::verifyText);
        handlers.put("verify_control_state", automation::verifyControlState);
        handlers.put("wait_for_control", automation::waitForControl);
        handlers.put("wait_for_state", automation::waitForState);
        handlers.put("get_active_dialog", info -> automation.getActiveDialog());
        handlers.put("click_dialog_button", automation::clickDialogButton);
        handlers.put("list_popup_items", automation::listPopupItems);
        handlers.put("select_popup_item", automation::selectPopupItem);
        handlers.put("capture_screenshot", automation::captureScreenshot);
        handlers.put("capture_control_screenshot", automation::captureControlScreenshot);
        handlers.put("get_last_action_evidence", info -> automation.getLastEvidence().toMap());
        handlers.put("execute_actions", info -> automation.executeActions(info, this));
        handlers.put("run_test_case", info -> automation.runTestCase(info, this));
        handlers.put("get_uiobject_by_mouse", info -> automation.getObjectAtMouse());
        handlers.put("get_uiobject_by_xy", info -> {
            int x = info.get("x") instanceof Number ? ((Number) info.get("x")).intValue() : 0;
            int y = info.get("y") instanceof Number ? ((Number) info.get("y")).intValue() : 0;
            return automation.getObjectAt(x, y);
        });
    }

    private Map<String, Object> scanAll() {
        Instant startTime = Instant.now();
        UiObjectScanner scanner = new UiObjectScanner(logger);
        List<UiObjectInfo> infos = scanner.scanAndHighlight(highlightLimit);
        automation.refreshRegistry();
        Instant endTime = Instant.now();
        try {
            Files.createDirectories(swapDirectory);
            Map<String, Object> payload = new HashMap<>();
            payload.put("StartTime", startTime.toString());
            payload.put("EndTime", endTime.toString());
            payload.put("TotalCount", infos.size());
            payload.put("Items", infos);
            Files.writeString(swapDirectory.resolve("MarsJavaEngineUiObjects.json"), JsonUtil.toJson(payload));
            logger.info("UI objects saved: " + infos.size());
            return Map.of("success", true, "totalCount", infos.size());
        } catch (Exception ex) {
            return McpResponses.error("INTERNAL_ERROR", ex.getMessage(), false, null);
        }
    }

    private static String normalize(String type) {
        if (type == null) {
            return "";
        }
        return type.trim().toLowerCase(Locale.ROOT).replace('-', '_');
    }
}
