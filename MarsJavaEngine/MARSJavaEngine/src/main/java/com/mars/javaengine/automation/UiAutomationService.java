package com.mars.javaengine.automation;

import java.awt.AWTException;
import java.awt.Component;
import java.awt.Container;
import java.awt.Dialog;
import java.awt.KeyboardFocusManager;
import java.awt.MouseInfo;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.Robot;
import java.awt.Toolkit;
import java.awt.Window;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.awt.image.BufferedImage;
import java.io.File;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.atomic.AtomicReference;
import javax.imageio.ImageIO;
import javax.swing.AbstractButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JDialog;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JPopupMenu;
import javax.swing.JRadioButton;
import javax.swing.JSpinner;
import javax.swing.JTabbedPane;
import javax.swing.JTable;
import javax.swing.JTree;
import javax.swing.JWindow;
import javax.swing.SwingUtilities;
import javax.swing.border.Border;
import javax.swing.border.TitledBorder;
import javax.swing.text.JTextComponent;
import javax.swing.tree.TreeModel;
import javax.swing.tree.TreePath;

public class UiAutomationService {
    private final ControlRegistry registry = new ControlRegistry();
    private final ActionEvidence lastEvidence = new ActionEvidence();
    private final Path swapDirectory;

    public UiAutomationService(Path swapDirectory) {
        this.swapDirectory = swapDirectory;
    }

    public ActionEvidence getLastEvidence() {
        return lastEvidence;
    }

    public Map<String, Object> getApplicationState() {
        return onEdt(() -> {
            refreshRegistry();
            Window active = activeWindow();
            JDialog dialog = activeDialog();
            Map<String, Object> state = new LinkedHashMap<>();
            state.put("application", windowTitle(rootFrame()));
            state.put("activeWindow", windowTitle(active));
            state.put("activeTab", activeTabTitle());
            state.put("modalDialog", dialog == null ? null : dialog.getTitle());
            state.put("busy", false);
            state.put("statusText", findStatusText());
            return state;
        });
    }

    public Map<String, Object> listWindows() {
        return onEdt(() -> {
            List<Map<String, Object>> windows = new ArrayList<>();
            for (Window window : Window.getWindows()) {
                if (!window.isShowing()) {
                    continue;
                }
                Map<String, Object> item = new LinkedHashMap<>();
                item.put("title", windowTitle(window));
                item.put("className", window.getClass().getName());
                item.put("modal", window instanceof Dialog && ((Dialog) window).isModal());
                item.put("active", window.isActive());
                item.put("type", window instanceof Dialog ? "DIALOG" : "WINDOW");
                windows.add(item);
            }
            return Map.of("windows", windows);
        });
    }

    public Map<String, Object> getActiveWindow() {
        return onEdt(() -> {
            Window window = activeWindow();
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("title", windowTitle(window));
            result.put("className", window == null ? "" : window.getClass().getName());
            return result;
        });
    }

    public Map<String, Object> inspectScreen() {
        return onEdt(() -> {
            refreshRegistry();
            Map<String, List<Map<String, Object>>> sectionMap = new LinkedHashMap<>();
            for (ControlHandle handle : registry.all()) {
                if (!ControlHandle.KIND_COMPONENT.equals(handle.getKind())) {
                    continue;
                }
                Component component = handle.getComponent();
                if (!isInteractive(component)) {
                    continue;
                }
                String section = sectionName(component);
                sectionMap.computeIfAbsent(section, key -> new ArrayList<>()).add(controlSummary(handle, false));
            }
            List<Map<String, Object>> sections = new ArrayList<>();
            for (Map.Entry<String, List<Map<String, Object>>> entry : sectionMap.entrySet()) {
                Map<String, Object> section = new LinkedHashMap<>();
                section.put("name", entry.getKey());
                section.put("controls", entry.getValue());
                sections.add(section);
            }
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("window", windowTitle(activeWindow()));
            result.put("sections", sections);
            return result;
        });
    }

    public Map<String, Object> getObjectTree() {
        return onEdt(() -> {
            refreshRegistry();
            List<Map<String, Object>> roots = new ArrayList<>();
            for (Window window : Window.getWindows()) {
                if (window.isShowing()) {
                    roots.add(toTreeNode(window, 0));
                }
            }
            return Map.of("roots", roots);
        });
    }

    public Map<String, Object> findControls(Map<String, Object> info) {
        return onEdt(() -> {
            refreshRegistry();
            String query = str(info.get("query"));
            String expectedType = str(info.get("expectedType"));
            String window = contextWindow(info);
            int limit = info.get("limit") instanceof Number ? ((Number) info.get("limit")).intValue() : 10;
            List<Scored> scored = scoreControls(query, expectedType, window, contextSection(info));
            List<Map<String, Object>> matches = new ArrayList<>();
            for (int i = 0; i < scored.size() && i < limit; i++) {
                matches.add(controlSummary(scored.get(i).handle, true, scored.get(i).score));
            }
            return Map.of("matches", matches);
        });
    }

    public Map<String, Object> resolveControl(Map<String, Object> info) {
        return onEdt(() -> resolveInternal(firstNonBlank(str(info.get("description")), str(info.get("query"))),
                firstNonBlank(str(info.get("expectedRole")), str(info.get("expectedType"))),
                contextWindow(info), contextSection(info)));
    }

    public Map<String, Object> resolveField(Map<String, Object> info) {
        return onEdt(() -> {
            String field = firstNonBlank(str(info.get("field")), str(info.get("description")));
            Map<String, Object> resolved = resolveInternal(field, "editable field", contextWindow(info), contextSection(info));
            if (!"RESOLVED".equals(resolved.get("status"))) {
                return resolved;
            }
            @SuppressWarnings("unchecked")
            Map<String, Object> control = (Map<String, Object>) resolved.get("control");
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("status", "RESOLVED");
            result.put("field", field);
            result.put("controlId", control.get("controlId"));
            result.put("controlType", control.get("type"));
            result.put("confidence", resolved.get("confidence"));
            return result;
        });
    }

    public Map<String, Object> getControlProperties(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            return properties(handle);
        });
    }

    public Map<String, Object> getControlRelations(Map<String, Object> info) {
        return onEdt(() -> {
            refreshRegistry();
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            Rectangle self = handle.boundsOnScreen();
            List<Map<String, Object>> relations = new ArrayList<>();
            if (self != null) {
                for (ControlHandle other : registry.all()) {
                    if (other.getId().equals(handle.getId())) {
                        continue;
                    }
                    Rectangle bounds = other.boundsOnScreen();
                    if (bounds == null) {
                        continue;
                    }
                    String relation = relationOf(self, bounds, handle.getComponent(), other.getComponent());
                    if (relation != null) {
                        Map<String, Object> item = controlSummary(other, false);
                        item.put("relation", relation);
                        relations.add(item);
                    }
                }
            }
            return Map.of("controlId", handle.getId(), "relations", relations);
        });
    }

    public Map<String, Object> highlightControl(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            Rectangle bounds = handle.boundsOnScreen();
            if (bounds == null) {
                return McpResponses.error("CONTROL_NOT_VISIBLE", "Control is not visible.", true, "Call inspect_screen.");
            }
            flash(bounds);
            return Map.of("success", true, "controlId", handle.getId());
        });
    }

    public Map<String, Object> clickControl(Map<String, Object> info) {
        return clickInternal(info, 1, false);
    }

    public Map<String, Object> doubleClickControl(Map<String, Object> info) {
        return clickInternal(info, 2, false);
    }

    public Map<String, Object> rightClickControl(Map<String, Object> info) {
        return clickInternal(info, 1, true);
    }

    public Map<String, Object> fillText(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            String value = str(info.get("value"));
            boolean clearFirst = info.get("clearFirst") == null || Boolean.TRUE.equals(info.get("clearFirst"));
            Object before = readValue(handle);
            boolean ok = writeText(handle, value, clearFirst);
            Object after = readValue(handle);
            lastEvidence.record("fill_text", handle.getLabel(), handle.getId(), before, value, after, ok);
            if (!ok) {
                return McpResponses.error("CONTROL_NOT_EDITABLE", "Control is not editable.", true, "Use set_field or select_value.");
            }
            return Map.of("success", true, "actualValue", after);
        });
    }

    public Map<String, Object> setField(Map<String, Object> info) {
        return onEdt(() -> {
            refreshRegistry();
            String field = str(info.get("field"));
            String value = str(info.get("value"));
            Map<String, Object> resolved = resolveInternal(field, null, contextWindow(info), contextSection(info));
            if (!"RESOLVED".equals(resolved.get("status"))) {
                return resolved;
            }
            @SuppressWarnings("unchecked")
            Map<String, Object> control = (Map<String, Object>) resolved.get("control");
            String controlId = str(control.get("controlId"));
            ControlHandle handle = requireHandle(controlId);
            Object before = readValue(handle);
            boolean ok = applyValue(handle, value);
            Object after = readValue(handle);
            lastEvidence.record("set_field", field, controlId, before, value, after, ok);
            if (!ok) {
                return McpResponses.error("ACTION_FAILED", "Unable to set field '" + field + "'.", true, "Call get_control_properties.");
            }
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("success", true);
            result.put("field", field);
            result.put("controlId", controlId);
            result.put("actualValue", after);
            return result;
        });
    }

    public Map<String, Object> selectValue(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            String value = firstNonBlank(str(info.get("value")), str(info.get("item")));
            Object before = readValue(handle);
            boolean ok = selectByValue(handle, value);
            Object after = readValue(handle);
            lastEvidence.record("select_value", handle.getLabel(), handle.getId(), before, value, after, ok);
            if (!ok) {
                return McpResponses.error("VALUE_NOT_AVAILABLE", "Value not found: " + value, true, "Call get_control_properties.");
            }
            return Map.of("success", true, "actualValue", after);
        });
    }

    public Map<String, Object> selectIndex(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            int index = info.get("index") instanceof Number ? ((Number) info.get("index")).intValue() : -1;
            Component component = handle.getComponent();
            if (component instanceof JComboBox) {
                JComboBox<?> combo = (JComboBox<?>) component;
                if (index >= 0 && index < combo.getItemCount()) {
                    combo.setSelectedIndex(index);
                    return Map.of("success", true, "actualValue", readValue(handle));
                }
            }
            if (component instanceof JTabbedPane) {
                JTabbedPane tabs = (JTabbedPane) component;
                if (index >= 0 && index < tabs.getTabCount()) {
                    tabs.setSelectedIndex(index);
                    return Map.of("success", true, "actualValue", tabs.getTitleAt(index));
                }
            }
            return McpResponses.error("VALUE_NOT_AVAILABLE", "Index out of range.", true, null);
        });
    }

    public Map<String, Object> setCheckbox(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            boolean checked = Boolean.TRUE.equals(info.get("checked")) || "true".equalsIgnoreCase(str(info.get("checked")));
            if (handle.getComponent() instanceof JCheckBox) {
                JCheckBox box = (JCheckBox) handle.getComponent();
                box.setSelected(checked);
                lastEvidence.record("set_checkbox", handle.getLabel(), handle.getId(), !checked, checked, box.isSelected(), true);
                return Map.of("success", true, "checked", box.isSelected());
            }
            return McpResponses.error("UNSUPPORTED_CONTROL", "Control is not a checkbox.", true, null);
        });
    }

    public Map<String, Object> setRadioButton(Map<String, Object> info) {
        return onEdt(() -> {
            String controlId = str(info.get("controlId"));
            String value = firstNonBlank(str(info.get("value")), str(info.get("label")));
            ControlHandle handle = controlId != null ? requireHandle(controlId) : null;
            if (handle == null && value != null) {
                refreshRegistry();
                Map<String, Object> resolved = resolveInternal(value, "RADIO", contextWindow(info), contextSection(info));
                if ("RESOLVED".equals(resolved.get("status"))) {
                    @SuppressWarnings("unchecked")
                    Map<String, Object> control = (Map<String, Object>) resolved.get("control");
                    handle = requireHandle(str(control.get("controlId")));
                }
            }
            if (handle == null) {
                return staleOrMissing(controlId);
            }
            if (handle.getComponent() instanceof JRadioButton) {
                JRadioButton radio = (JRadioButton) handle.getComponent();
                radio.setSelected(true);
                radio.doClick();
                return Map.of("success", true, "selected", true, "controlId", handle.getId());
            }
            return McpResponses.error("UNSUPPORTED_CONTROL", "Control is not a radio button.", true, null);
        });
    }

    public Map<String, Object> selectTab(Map<String, Object> info) {
        return onEdt(() -> {
            String tab = firstNonBlank(str(info.get("tab")), str(info.get("title")));
            for (JTabbedPane pane : findOfType(JTabbedPane.class)) {
                for (int i = 0; i < pane.getTabCount(); i++) {
                    if (tab != null && tab.equalsIgnoreCase(pane.getTitleAt(i))) {
                        pane.setSelectedIndex(i);
                        return Map.of("success", true, "tab", pane.getTitleAt(i));
                    }
                }
            }
            return McpResponses.error("CONTROL_NOT_FOUND", "Tab not found: " + tab, true, "Call inspect_screen.");
        });
    }

    public Map<String, Object> selectTreeNode(Map<String, Object> info) {
        return onEdt(() -> {
            List<?> path = listOf(info.get("path"));
            if (path.isEmpty()) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Tree path is empty.", true, null);
            }
            for (JTree tree : findOfType(JTree.class)) {
                TreePath match = findTreePath(tree, path);
                if (match != null) {
                    tree.expandPath(match.getParentPath() == null ? match : match.getParentPath());
                    tree.setSelectionPath(match);
                    tree.scrollPathToVisible(match);
                    return Map.of("success", true, "path", path);
                }
            }
            return McpResponses.error("CONTROL_NOT_FOUND", "Tree node not found.", true, "Call get_object_tree.");
        });
    }

    public Map<String, Object> selectMenuItem(Map<String, Object> info) {
        return onEdt(() -> {
            List<?> path = listOf(info.get("path"));
            JFrame frame = rootFrame();
            if (frame == null || frame.getJMenuBar() == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "No menu bar is available.", true, null);
            }
            JMenuItem item = findMenuItem(frame.getJMenuBar(), path, 0);
            if (item == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Menu item not found.", true, null);
            }
            item.doClick();
            return Map.of("success", true, "path", path);
        });
    }

    public Map<String, Object> pressKey(Map<String, Object> info) {
        String key = firstNonBlank(str(info.get("key")), str(info.get("keys")));
        try {
            press(key);
            lastEvidence.record("press_key", null, null, null, key, key, true);
            return Map.of("success", true, "key", key);
        } catch (Exception ex) {
            return McpResponses.error("ACTION_FAILED", "Unable to press key: " + key, true, null);
        }
    }

    public Map<String, Object> focusControl(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null || handle.getComponent() == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            handle.getComponent().requestFocusInWindow();
            return Map.of("success", true, "controlId", handle.getId());
        });
    }

    public Map<String, Object> clearControl(Map<String, Object> info) {
        Map<String, Object> copy = new LinkedHashMap<>(info);
        copy.put("value", "");
        copy.put("clearFirst", true);
        return fillText(copy);
    }

    public Map<String, Object> getTableData(Map<String, Object> info) {
        return onEdt(() -> {
            JTable table = resolveTable(info);
            if (table == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Table not found.", true, "Call find_controls for the table.");
            }
            return Map.of("columns", tableColumns(table), "rows", tableRows(table, null, null, null));
        });
    }

    public Map<String, Object> findTableRows(Map<String, Object> info) {
        return onEdt(() -> {
            JTable table = resolveTable(info);
            if (table == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Table not found.", true, null);
            }
            Map<String, Object> filters = mapOf(info.get("filters"));
            String sortBy = str(info.get("sortBy"));
            String sortDirection = str(info.get("sortDirection"));
            List<Map<String, Object>> rows = tableRows(table, filters, sortBy, sortDirection);
            return Map.of("rows", rows, "count", rows.size());
        });
    }

    public Map<String, Object> selectTableRow(Map<String, Object> info) {
        return onEdt(() -> {
            JTable table = resolveTable(info);
            if (table == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Table not found.", true, null);
            }
            int modelRow = findRow(table, mapOf(firstNonNull(info.get("match"), info.get("filters"))));
            if (modelRow < 0) {
                return McpResponses.error("TABLE_ROW_NOT_FOUND", "No table row matched the filter.", true, null);
            }
            int viewRow = table.convertRowIndexToView(modelRow);
            table.getSelectionModel().setSelectionInterval(viewRow, viewRow);
            table.scrollRectToVisible(table.getCellRect(viewRow, 0, true));
            return Map.of("success", true, "rowIndex", modelRow);
        });
    }

    public Map<String, Object> getTableCell(Map<String, Object> info) {
        return onEdt(() -> {
            JTable table = resolveTable(info);
            if (table == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Table not found.", true, null);
            }
            int modelRow = findRow(table, mapOf(info.get("match")));
            int column = columnIndex(table, str(info.get("column")));
            if (modelRow < 0 || column < 0) {
                return McpResponses.error("TABLE_ROW_NOT_FOUND", "Cell not found.", true, null);
            }
            Object value = table.getModel().getValueAt(modelRow, column);
            return Map.of("value", value == null ? "" : String.valueOf(value), "rowIndex", modelRow, "column", table.getColumnName(column));
        });
    }

    public Map<String, Object> setTableCell(Map<String, Object> info) {
        return onEdt(() -> {
            JTable table = resolveTable(info);
            if (table == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Table not found.", true, null);
            }
            int modelRow = findRow(table, mapOf(info.get("match")));
            int column = columnIndex(table, str(info.get("column")));
            if (modelRow < 0 || column < 0) {
                return McpResponses.error("TABLE_ROW_NOT_FOUND", "Cell not found.", true, null);
            }
            if (!table.isCellEditable(table.convertRowIndexToView(modelRow), column)) {
                return McpResponses.error("CONTROL_NOT_EDITABLE", "Table cell is not editable.", false, null);
            }
            table.getModel().setValueAt(info.get("value"), modelRow, column);
            return Map.of("success", true, "value", table.getModel().getValueAt(modelRow, column));
        });
    }

    public Map<String, Object> openTableRow(Map<String, Object> info) {
        Map<String, Object> selected = selectTableRow(info);
        if (!Boolean.TRUE.equals(selected.get("success"))) {
            return selected;
        }
        return onEdt(() -> {
            JTable table = resolveTable(info);
            Point p = table.getLocationOnScreen();
            int viewRow = table.getSelectedRow();
            Rectangle cell = table.getCellRect(viewRow, 0, true);
            clickScreen(p.x + cell.x + 8, p.y + cell.y + 8, 2, false);
            return Map.of("success", true);
        });
    }

    public Map<String, Object> openTableContextMenu(Map<String, Object> info) {
        Map<String, Object> selected = selectTableRow(info);
        if (!Boolean.TRUE.equals(selected.get("success"))) {
            return selected;
        }
        return onEdt(() -> {
            JTable table = resolveTable(info);
            Point p = table.getLocationOnScreen();
            int viewRow = table.getSelectedRow();
            Rectangle cell = table.getCellRect(viewRow, 0, true);
            clickScreen(p.x + cell.x + 8, p.y + cell.y + 8, 1, true);
            return Map.of("success", true);
        });
    }

    public Map<String, Object> readValue(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            return Map.of("value", readValue(handle), "controlId", handle.getId());
        });
    }

    public Map<String, Object> readText(Map<String, Object> info) {
        return onEdt(() -> {
            if (info.get("controlId") != null) {
                return readValue(info);
            }
            String query = str(info.get("query"));
            refreshRegistry();
            List<String> texts = new ArrayList<>();
            for (ControlHandle handle : registry.all()) {
                String text = firstNonBlank(handle.getLabel(), stringValue(readValue(handle)));
                if (query == null || query.isBlank() || containsIgnoreCase(text, query)) {
                    texts.add(text);
                }
            }
            return Map.of("texts", texts);
        });
    }

    public Map<String, Object> isVisible(Map<String, Object> info) {
        return stateFlag(info, "visible");
    }

    public Map<String, Object> isEnabled(Map<String, Object> info) {
        return stateFlag(info, "enabled");
    }

    public Map<String, Object> isSelected(Map<String, Object> info) {
        return stateFlag(info, "selected");
    }

    public Map<String, Object> verifyValue(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            String expected = str(info.get("expected"));
            String actual = stringValue(readValue(handle));
            boolean passed = normalize(expected).equals(normalize(actual));
            return Map.of("passed", passed, "expected", expected, "actual", actual);
        });
    }

    public Map<String, Object> verifyText(Map<String, Object> info) {
        return onEdt(() -> {
            String expected = firstNonBlank(str(info.get("expected")), str(info.get("text")));
            String mode = firstNonBlank(str(info.get("mode")), "CONTAINS").toUpperCase(Locale.ROOT);
            String actual;
            if (info.get("controlId") != null) {
                ControlHandle handle = requireHandle(str(info.get("controlId")));
                if (handle == null) {
                    return staleOrMissing(str(info.get("controlId")));
                }
                actual = stringValue(readValue(handle));
            } else {
                actual = firstNonBlank(findStatusText(), windowTitle(activeWindow()));
            }
            boolean passed = matchText(actual, expected, mode);
            return Map.of("passed", passed, "expected", expected, "actual", actual, "mode", mode);
        });
    }

    public Map<String, Object> verifyControlState(Map<String, Object> info) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            Map<String, Object> actual = properties(handle);
            Map<String, Object> expected = mapOf(info.get("expected"));
            if (expected.isEmpty()) {
                expected.put("enabled", info.get("enabled"));
                expected.put("visible", info.get("visible"));
                expected.put("selected", info.get("selected"));
                expected.put("editable", info.get("editable"));
                expected.put("focused", info.get("focused"));
            }
            boolean passed = true;
            for (Map.Entry<String, Object> entry : expected.entrySet()) {
                if (entry.getValue() == null) {
                    continue;
                }
                if (!String.valueOf(actual.get(entry.getKey())).equalsIgnoreCase(String.valueOf(entry.getValue()))) {
                    passed = false;
                }
            }
            return Map.of("passed", passed, "expected", expected, "actual", actual);
        });
    }

    public Map<String, Object> waitForControl(Map<String, Object> info) {
        long timeout = info.get("timeoutMs") instanceof Number ? ((Number) info.get("timeoutMs")).longValue() : 5000L;
        long deadline = System.currentTimeMillis() + timeout;
        Map<String, Object> last = null;
        while (System.currentTimeMillis() < deadline) {
            last = resolveControl(info);
            if ("RESOLVED".equals(last.get("status"))) {
                return last;
            }
            sleep(150);
        }
        return last == null ? McpResponses.error("TIMEOUT", "Timed out waiting for control.", true, null) : last;
    }

    public Map<String, Object> waitForState(Map<String, Object> info) {
        long timeout = info.get("timeoutMs") instanceof Number ? ((Number) info.get("timeoutMs")).longValue() : 5000L;
        long deadline = System.currentTimeMillis() + timeout;
        Map<String, Object> last = Map.of();
        while (System.currentTimeMillis() < deadline) {
            last = verifyValue(info);
            if (Boolean.TRUE.equals(last.get("passed")) || Boolean.TRUE.equals(last.get("success"))) {
                return last;
            }
            if (info.get("expected") == null && info.get("text") != null) {
                last = verifyText(info);
                if (Boolean.TRUE.equals(last.get("passed"))) {
                    return last;
                }
            }
            sleep(150);
        }
        return McpResponses.error("TIMEOUT", "Timed out waiting for state.", true, null);
    }

    public Map<String, Object> getActiveDialog() {
        return onEdt(() -> {
            JDialog dialog = activeDialog();
            if (dialog == null) {
                Map<String, Object> empty = new LinkedHashMap<>();
                empty.put("dialog", null);
                return empty;
            }
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("title", dialog.getTitle());
            result.put("modal", dialog.isModal());
            List<String> buttons = new ArrayList<>();
            List<Map<String, Object>> controls = new ArrayList<>();
            collectDialog(dialog, buttons, controls);
            result.put("buttons", buttons);
            result.put("message", findDialogMessage(dialog));
            result.put("controls", controls);
            return result;
        });
    }

    public Map<String, Object> clickDialogButton(Map<String, Object> info) {
        return onEdt(() -> {
            String button = firstNonBlank(str(info.get("button")), str(info.get("text")), "OK");
            JDialog dialog = activeDialog();
            if (dialog == null) {
                return McpResponses.error("DIALOG_BLOCKING", "No active dialog.", true, "Call get_active_dialog.");
            }
            AbstractButton match = findButton(dialog, button);
            if (match == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "Dialog button not found: " + button, true, null);
            }
            match.doClick();
            return Map.of("success", true, "button", match.getText());
        });
    }

    public Map<String, Object> listPopupItems(Map<String, Object> info) {
        return onEdt(() -> {
            List<String> items = new ArrayList<>();
            JPopupMenu popup = visiblePopup();
            if (popup != null) {
                for (int i = 0; i < popup.getComponentCount(); i++) {
                    Component component = popup.getComponent(i);
                    if (component instanceof JMenuItem) {
                        items.add(((JMenuItem) component).getText());
                    }
                }
            }
            if (info.get("controlId") != null) {
                ControlHandle handle = requireHandle(str(info.get("controlId")));
                if (handle != null && handle.getComponent() instanceof JComboBox) {
                    JComboBox<?> combo = (JComboBox<?>) handle.getComponent();
                    for (int i = 0; i < combo.getItemCount(); i++) {
                        items.add(String.valueOf(combo.getItemAt(i)));
                    }
                }
            }
            return Map.of("items", items);
        });
    }

    public Map<String, Object> selectPopupItem(Map<String, Object> info) {
        return onEdt(() -> {
            String item = firstNonBlank(str(info.get("item")), str(info.get("value")));
            JPopupMenu popup = visiblePopup();
            if (popup != null) {
                for (int i = 0; i < popup.getComponentCount(); i++) {
                    Component component = popup.getComponent(i);
                    if (component instanceof JMenuItem && item.equalsIgnoreCase(((JMenuItem) component).getText())) {
                        ((JMenuItem) component).doClick();
                        return Map.of("success", true, "item", item);
                    }
                }
                return McpResponses.error("POPUP_NOT_FOUND", "Popup item not found: " + item, true, "Call list_popup_items.");
            }
            if (info.get("controlId") != null) {
                Map<String, Object> copy = new LinkedHashMap<>(info);
                copy.put("value", item);
                return selectValue(copy);
            }
            return McpResponses.error("POPUP_NOT_FOUND", "No popup is visible.", true, "Right-click a control first.");
        });
    }

    public Map<String, Object> captureScreenshot(Map<String, Object> info) {
        String scope = firstNonBlank(str(info.get("scope")), "ACTIVE_WINDOW");
        Rectangle bounds;
        if ("SCREEN".equalsIgnoreCase(scope)) {
            bounds = new Rectangle(Toolkit.getDefaultToolkit().getScreenSize());
        } else if ("CONTROL".equalsIgnoreCase(scope) && info.get("controlId") != null) {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            bounds = handle == null ? null : handle.boundsOnScreen();
        } else {
            Window window = onEdt(this::activeWindow);
            bounds = window == null || !window.isShowing() ? null : new Rectangle(window.getLocationOnScreen(), window.getSize());
        }
        if (bounds == null) {
            return McpResponses.error("ACTION_FAILED", "Unable to determine screenshot bounds.", true, null);
        }
        try {
            BufferedImage image = new Robot().createScreenCapture(bounds);
            File dir = swapDirectory.resolve("evidence").toFile();
            dir.mkdirs();
            File file = new File(dir, "screenshot-" + System.currentTimeMillis() + ".png");
            ImageIO.write(image, "png", file);
            return Map.of("success", true, "path", file.getAbsolutePath(), "scope", scope);
        } catch (Exception ex) {
            return McpResponses.error("INTERNAL_ERROR", ex.getMessage(), false, null);
        }
    }

    public Map<String, Object> captureControlScreenshot(Map<String, Object> info) {
        Map<String, Object> copy = new LinkedHashMap<>(info);
        copy.put("scope", "CONTROL");
        return captureScreenshot(copy);
    }

    public Map<String, Object> executeActions(Map<String, Object> info, CommandDispatcher dispatcher) {
        List<?> steps = listOf(info.get("steps"));
        boolean stopOnFailure = info.get("stopOnFailure") == null || Boolean.TRUE.equals(info.get("stopOnFailure"));
        List<Map<String, Object>> results = new ArrayList<>();
        for (int i = 0; i < steps.size(); i++) {
            Map<String, Object> step = mapOf(steps.get(i));
            String action = firstNonBlank(str(step.get("action")), str(step.get("tool")));
            Map<String, Object> result = dispatcher.dispatch(action, step);
            Map<String, Object> item = new LinkedHashMap<>();
            item.put("index", i);
            item.put("action", action);
            item.put("result", result);
            results.add(item);
            if (stopOnFailure && isFailure(result)) {
                return Map.of("success", false, "failedIndex", i, "results", results);
            }
        }
        return Map.of("success", true, "results", results);
    }

    public Map<String, Object> runTestCase(Map<String, Object> info, CommandDispatcher dispatcher) {
        Map<String, Object> executed = executeActions(info, dispatcher);
        Map<String, Object> result = new LinkedHashMap<>(executed);
        result.put("name", info.get("name"));
        return result;
    }

    public Map<String, Object> getObjectAt(int x, int y) {
        return onEdt(() -> {
            refreshRegistry();
            ControlHandle best = null;
            int bestArea = Integer.MAX_VALUE;
            for (ControlHandle handle : registry.all()) {
                Rectangle bounds = handle.boundsOnScreen();
                if (bounds != null && bounds.contains(x, y) && bounds.width * bounds.height < bestArea) {
                    best = handle;
                    bestArea = bounds.width * bounds.height;
                }
            }
            if (best == null) {
                return McpResponses.error("CONTROL_NOT_FOUND", "No control at " + x + "," + y, true, null);
            }
            return properties(best);
        });
    }

    public Map<String, Object> getObjectAtMouse() {
        Point point = MouseInfo.getPointerInfo().getLocation();
        return getObjectAt(point.x, point.y);
    }

    public void refreshRegistry() {
        List<ControlHandle> current = new ArrayList<>();
        for (Window window : Window.getWindows()) {
            if (window.isShowing()) {
                collect(window, current);
            }
        }
        registry.retain(current);
    }

    public ControlRegistry getRegistry() {
        return registry;
    }

    private void collect(Component component, List<ControlHandle> current) {
        if (component == null || !component.isShowing()) {
            return;
        }
        ControlHandle handle = registry.registerComponent(component, component.getClass().getName(),
                component.getName(), labelOf(component), classify(component));
        current.add(handle);
        if (component instanceof JTree) {
            collectTree((JTree) component, current);
        }
        if (component instanceof JTabbedPane) {
            JTabbedPane tabs = (JTabbedPane) component;
            for (int i = 0; i < tabs.getTabCount(); i++) {
                ControlHandle tab = registry.registerSynthetic(ControlHandle.KIND_TAB, tabs,
                        "javax.swing.JTabbedPane$Tab", tabs.getName(), tabs.getTitleAt(i), "Tab");
                tab.setTabbedPane(tabs, i);
                current.add(tab);
            }
        }
        if (component instanceof JTable) {
            ControlHandle table = registry.registerComponent(component, component.getClass().getName(),
                    component.getName(), firstNonBlank(component.getName(), "Table"), "Table");
            table.setTable((JTable) component, -1, -1);
            current.add(table);
        }
        if (component instanceof Container) {
            Component[] children = ((Container) component).getComponents();
            for (Component child : children) {
                collect(child, current);
            }
        }
    }

    private void collectTree(JTree tree, List<ControlHandle> current) {
        TreeModel model = tree.getModel();
        if (model == null || model.getRoot() == null) {
            return;
        }
        collectTreeNode(tree, model, new TreePath(model.getRoot()), current);
    }

    private void collectTreeNode(JTree tree, TreeModel model, TreePath path, List<ControlHandle> current) {
        String text = tree.convertValueToText(path.getLastPathComponent(), false, tree.isExpanded(path),
                model.isLeaf(path.getLastPathComponent()), tree.getRowForPath(path), false);
        ControlHandle handle = registry.registerSynthetic(ControlHandle.KIND_TREE_NODE, tree,
                path.getLastPathComponent().getClass().getName(), path.toString(), text, "TreeNode");
        handle.setTree(tree, path);
        current.add(handle);
        Object node = path.getLastPathComponent();
        for (int i = 0; i < model.getChildCount(node); i++) {
            collectTreeNode(tree, model, path.pathByAddingChild(model.getChild(node, i)), current);
        }
    }

    private Map<String, Object> resolveInternal(String query, String expected, String window, String section) {
        if (query == null || query.isBlank()) {
            return McpResponses.error("CONTROL_NOT_FOUND", "Query is empty.", true, null);
        }
        refreshRegistry();
        List<Scored> scored = scoreControls(query, expected, window, section);
        if (scored.isEmpty()) {
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("status", "NOT_FOUND");
            result.put("success", false);
            result.put("error", McpResponses.error("CONTROL_NOT_FOUND", "Unable to resolve '" + query + "'.", true,
                    "Call find_controls with query '" + query + "'.").get("error"));
            return result;
        }
        Scored top = scored.get(0);
        if (top.handle.getComponent() != null && !top.handle.getComponent().isEnabled() && isActionRole(expected)) {
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("status", "NOT_INTERACTABLE");
            result.put("control", controlSummary(top.handle, true, top.score));
            return result;
        }
        if (top.score >= 0.90 || (top.score >= 0.70 && (scored.size() == 1 || top.score - scored.get(1).score >= 0.08))) {
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("status", "RESOLVED");
            result.put("control", controlSummary(top.handle, true, top.score));
            result.put("confidence", round(top.score));
            return result;
        }
        List<Map<String, Object>> candidates = new ArrayList<>();
        for (int i = 0; i < scored.size() && i < 5; i++) {
            candidates.add(controlSummary(scored.get(i).handle, true, scored.get(i).score));
        }
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("status", "AMBIGUOUS");
        result.put("success", false);
        result.put("candidates", candidates);
        result.put("error", McpResponses.error("CONTROL_AMBIGUOUS", "Multiple controls match '" + query + "'.", true,
                "Add window/section context or pick a candidate controlId.").get("error"));
        return result;
    }

    private List<Scored> scoreControls(String query, String expected, String window, String section) {
        List<Scored> scored = new ArrayList<>();
        if (query == null) {
            return scored;
        }
        String needle = query.trim();
        for (ControlHandle handle : registry.all()) {
            if (!matchesWindow(handle, window) || !matchesSection(handle, section)) {
                continue;
            }
            if (!matchesExpected(handle, expected)) {
                continue;
            }
            double score = score(handle, needle);
            if (score >= 0.55) {
                scored.add(new Scored(handle, score));
            }
        }
        scored.sort(Comparator.comparingDouble((Scored item) -> item.score).reversed());
        return scored;
    }

    private double score(ControlHandle handle, String query) {
        String q = query.toLowerCase(Locale.ROOT);
        String name = safe(handle.getName()).toLowerCase(Locale.ROOT);
        String label = safe(handle.getLabel()).toLowerCase(Locale.ROOT);
        String type = safe(handle.getType()).toLowerCase(Locale.ROOT);
        String value = safe(stringValue(readValue(handle))).toLowerCase(Locale.ROOT);
        if (name.equals(q) || name.endsWith("." + q.replace(' ', '.'))) {
            return 1.0;
        }
        if (label.equalsIgnoreCase(query)) {
            return 0.98;
        }
        if (name.contains(q.replace(' ', '.')) || name.contains(q.replace(" ", ""))) {
            return 0.90;
        }
        if (label.contains(q) || q.contains(label) && !label.isBlank()) {
            return 0.86;
        }
        if (value.equals(q)) {
            return 0.80;
        }
        if (value.contains(q) || type.contains(q)) {
            return 0.68;
        }
        return 0;
    }

    private boolean matchesExpected(ControlHandle handle, String expected) {
        if (expected == null || expected.isBlank()) {
            return true;
        }
        String role = expected.toLowerCase(Locale.ROOT);
        String type = safe(handle.getType()).toLowerCase(Locale.ROOT);
        Component component = handle.getComponent();
        if (role.contains("edit") || "EDITABLE".equalsIgnoreCase(expected)) {
            return component instanceof JTextComponent || component instanceof JSpinner
                    || (component instanceof JComboBox && ((JComboBox<?>) component).isEditable())
                    || "TextField".equals(handle.getType()) || "ComboBox".equals(handle.getType());
        }
        if (role.contains("button") && !role.contains("radio")) {
            return "Button".equals(handle.getType());
        }
        if (role.contains("check")) {
            return component instanceof JCheckBox;
        }
        if (role.contains("radio")) {
            return component instanceof JRadioButton;
        }
        if (role.contains("combo") || role.contains("select")) {
            return component instanceof JComboBox;
        }
        if (role.contains("table")) {
            return component instanceof JTable;
        }
        if (role.contains("tree")) {
            return component instanceof JTree || ControlHandle.KIND_TREE_NODE.equals(handle.getKind());
        }
        if (role.contains("tab")) {
            return component instanceof JTabbedPane || ControlHandle.KIND_TAB.equals(handle.getKind());
        }
        return type.contains(role);
    }

    private boolean applyValue(ControlHandle handle, String value) {
        Component component = handle.getComponent();
        if (component instanceof JComboBox) {
            return selectByValue(handle, value);
        }
        if (component instanceof JCheckBox) {
            boolean checked = "true".equalsIgnoreCase(value) || "checked".equalsIgnoreCase(value) || "yes".equalsIgnoreCase(value);
            ((JCheckBox) component).setSelected(checked);
            return true;
        }
        if (component instanceof JRadioButton) {
            ((JRadioButton) component).setSelected(true);
            ((JRadioButton) component).doClick();
            return true;
        }
        if (component instanceof JSpinner) {
            ((JSpinner) component).setValue(parseSpinner(value));
            return true;
        }
        return writeText(handle, value, true);
    }

    private boolean writeText(ControlHandle handle, String value, boolean clearFirst) {
        Component component = handle.getComponent();
        if (component instanceof JTextComponent) {
            JTextComponent text = (JTextComponent) component;
            if (!text.isEditable() || !text.isEnabled()) {
                return false;
            }
            text.requestFocusInWindow();
            text.setText(clearFirst ? value : text.getText() + value);
            return true;
        }
        if (component instanceof JComboBox) {
            JComboBox<Object> combo = unsafeCombo(component);
            if (combo.isEditable()) {
                combo.setSelectedItem(value);
                return true;
            }
            return selectByValue(handle, value);
        }
        return false;
    }

    @SuppressWarnings("unchecked")
    private JComboBox<Object> unsafeCombo(Component component) {
        return (JComboBox<Object>) component;
    }

    private boolean selectByValue(ControlHandle handle, String value) {
        Component component = handle.getComponent();
        if (component instanceof JComboBox) {
            JComboBox<?> combo = (JComboBox<?>) component;
            for (int i = 0; i < combo.getItemCount(); i++) {
                if (String.valueOf(combo.getItemAt(i)).equalsIgnoreCase(value)) {
                    combo.setSelectedIndex(i);
                    return true;
                }
            }
        }
        if (component instanceof JList) {
            JList<?> list = (JList<?>) component;
            for (int i = 0; i < list.getModel().getSize(); i++) {
                if (String.valueOf(list.getModel().getElementAt(i)).equalsIgnoreCase(value)) {
                    list.setSelectedIndex(i);
                    return true;
                }
            }
        }
        return false;
    }

    private Object readValue(ControlHandle handle) {
        if (handle == null) {
            return null;
        }
        if (ControlHandle.KIND_TAB.equals(handle.getKind()) && handle.getTabbedPane() != null && handle.getTabIndex() >= 0) {
            return handle.getTabbedPane().getTitleAt(handle.getTabIndex());
        }
        if (ControlHandle.KIND_TREE_NODE.equals(handle.getKind())) {
            return handle.getLabel();
        }
        Component component = handle.getComponent();
        if (component instanceof JTextComponent) {
            return ((JTextComponent) component).getText();
        }
        if (component instanceof JComboBox) {
            return ((JComboBox<?>) component).getSelectedItem();
        }
        if (component instanceof JCheckBox) {
            return ((JCheckBox) component).isSelected();
        }
        if (component instanceof JRadioButton) {
            return ((JRadioButton) component).isSelected();
        }
        if (component instanceof AbstractButton) {
            return ((AbstractButton) component).getText();
        }
        if (component instanceof JLabel) {
            return ((JLabel) component).getText();
        }
        if (component instanceof JSpinner) {
            return ((JSpinner) component).getValue();
        }
        if (component instanceof JTabbedPane) {
            JTabbedPane tabs = (JTabbedPane) component;
            return tabs.getSelectedIndex() >= 0 ? tabs.getTitleAt(tabs.getSelectedIndex()) : "";
        }
        return component == null ? handle.getLabel() : component.getName();
    }

    private Map<String, Object> properties(ControlHandle handle) {
        Component component = handle.getComponent();
        Map<String, Object> map = controlSummary(handle, true, null);
        map.put("className", handle.getClassName());
        map.put("accessibleName", accessibleName(component));
        map.put("enabled", component == null || component.isEnabled());
        map.put("visible", component == null || component.isShowing());
        map.put("selected", isSelectedValue(handle));
        map.put("editable", component instanceof JTextComponent && ((JTextComponent) component).isEditable());
        map.put("focused", component != null && component.isFocusOwner());
        Rectangle bounds = handle.boundsOnScreen();
        if (bounds != null) {
            map.put("bounds", Map.of("x", bounds.x, "y", bounds.y, "width", bounds.width, "height", bounds.height));
        }
        return map;
    }

    private Map<String, Object> controlSummary(ControlHandle handle, boolean includeId) {
        return controlSummary(handle, includeId, null);
    }

    private Map<String, Object> controlSummary(ControlHandle handle, boolean includeId, Double confidence) {
        Map<String, Object> map = new LinkedHashMap<>();
        if (includeId) {
            map.put("controlId", handle.getId());
            map.put("objectId", handle.getId());
        }
        map.put("type", handle.getType());
        map.put("label", handle.getLabel());
        map.put("name", handle.getName());
        map.put("value", readValue(handle));
        if (handle.getComponent() != null) {
            map.put("enabled", handle.getComponent().isEnabled());
        }
        if (confidence != null) {
            map.put("confidence", round(confidence));
        }
        return map;
    }

    private Map<String, Object> clickInternal(Map<String, Object> info, int clicks, boolean right) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                Map<String, Object> resolved = resolveInternal(str(info.get("description")), "button",
                        contextWindow(info), contextSection(info));
                if ("RESOLVED".equals(resolved.get("status"))) {
                    @SuppressWarnings("unchecked")
                    Map<String, Object> control = (Map<String, Object>) resolved.get("control");
                    return clickById(str(control.get("controlId")), clicks, right);
                }
                return resolved;
            }
            return clickById(handle.getId(), clicks, right);
        });
    }

    private Map<String, Object> clickById(String id, int clicks, boolean right) {
        ControlHandle handle = requireHandle(id);
        if (handle == null) {
            return staleOrMissing(id);
        }
        Component component = handle.getComponent();
        if (!right && clicks == 1 && component instanceof AbstractButton) {
            ((AbstractButton) component).doClick();
            lastEvidence.record("click_control", handle.getLabel(), id, null, "click", "clicked", true);
            return Map.of("success", true, "controlId", id);
        }
        Rectangle bounds = handle.boundsOnScreen();
        if (bounds == null) {
            return McpResponses.error("CONTROL_NOT_VISIBLE", "Control is not visible.", true, null);
        }
        clickScreen(bounds.x + Math.max(2, bounds.width / 2), bounds.y + Math.max(2, bounds.height / 2), clicks, right);
        lastEvidence.record(right ? "right_click_control" : "click_control", handle.getLabel(), id, null, "click", "clicked", true);
        return Map.of("success", true, "controlId", id);
    }

    private void clickScreen(int x, int y, int clicks, boolean right) {
        try {
            Robot robot = new Robot();
            robot.mouseMove(x, y);
            int button = right ? InputEvent.BUTTON3_DOWN_MASK : InputEvent.BUTTON1_DOWN_MASK;
            for (int i = 0; i < clicks; i++) {
                robot.mousePress(button);
                robot.mouseRelease(button);
            }
        } catch (AWTException ex) {
            throw new RuntimeException(ex);
        }
    }

    private void flash(Rectangle bounds) {
        Thread thread = new Thread(() -> {
            try {
                JWindow window = new JWindow();
                window.setAlwaysOnTop(true);
                window.setBounds(bounds);
                window.setBackground(new java.awt.Color(255, 0, 0, 50));
                for (int i = 0; i < 3; i++) {
                    SwingUtilities.invokeAndWait(() -> window.setVisible(true));
                    Thread.sleep(160);
                    SwingUtilities.invokeAndWait(() -> window.setVisible(false));
                    Thread.sleep(160);
                }
                SwingUtilities.invokeAndWait(window::dispose);
            } catch (Exception ignored) {
            }
        }, "mars-highlight");
        thread.setDaemon(true);
        thread.start();
    }

    private JTable resolveTable(Map<String, Object> info) {
        refreshRegistry();
        ControlHandle handle = requireHandle(str(info.get("controlId")));
        if (handle != null) {
            if (handle.getTable() != null) {
                return handle.getTable();
            }
            if (handle.getComponent() instanceof JTable) {
                return (JTable) handle.getComponent();
            }
        }
        List<JTable> tables = findOfType(JTable.class);
        return tables.isEmpty() ? null : tables.get(0);
    }

    private List<String> tableColumns(JTable table) {
        List<String> columns = new ArrayList<>();
        for (int i = 0; i < table.getColumnCount(); i++) {
            columns.add(table.getColumnName(i));
        }
        return columns;
    }

    private List<Map<String, Object>> tableRows(JTable table, Map<String, Object> filters, String sortBy, String sortDirection) {
        List<Map<String, Object>> rows = new ArrayList<>();
        for (int r = 0; r < table.getModel().getRowCount(); r++) {
            Map<String, Object> row = new LinkedHashMap<>();
            row.put("rowIndex", r);
            boolean match = true;
            for (int c = 0; c < table.getModel().getColumnCount(); c++) {
                String column = table.getModel().getColumnName(c);
                Object value = table.getModel().getValueAt(r, c);
                row.put(column, value == null ? "" : String.valueOf(value));
                if (filters != null && filters.containsKey(column)
                        && !containsIgnoreCase(String.valueOf(value), String.valueOf(filters.get(column)))) {
                    match = false;
                }
            }
            if (match) {
                rows.add(row);
            }
        }
        if (sortBy != null && !sortBy.isBlank()) {
            boolean desc = "DESC".equalsIgnoreCase(sortDirection);
            rows.sort((a, b) -> {
                int cmp = String.valueOf(a.get(sortBy)).compareToIgnoreCase(String.valueOf(b.get(sortBy)));
                return desc ? -cmp : cmp;
            });
        }
        return rows;
    }

    private int findRow(JTable table, Map<String, Object> match) {
        if (match == null || match.isEmpty()) {
            return table.getSelectedRow() >= 0 ? table.convertRowIndexToModel(table.getSelectedRow()) : -1;
        }
        for (int r = 0; r < table.getModel().getRowCount(); r++) {
            boolean ok = true;
            for (Map.Entry<String, Object> entry : match.entrySet()) {
                int column = columnIndex(table, entry.getKey());
                if (column < 0 || !containsIgnoreCase(String.valueOf(table.getModel().getValueAt(r, column)),
                        String.valueOf(entry.getValue()))) {
                    ok = false;
                    break;
                }
            }
            if (ok) {
                return r;
            }
        }
        return -1;
    }

    private int columnIndex(JTable table, String name) {
        if (name == null) {
            return -1;
        }
        for (int i = 0; i < table.getModel().getColumnCount(); i++) {
            if (name.equalsIgnoreCase(table.getModel().getColumnName(i))) {
                return i;
            }
        }
        return -1;
    }

    private TreePath findTreePath(JTree tree, List<?> expected) {
        TreeModel model = tree.getModel();
        if (model == null) {
            return null;
        }
        return findTreePath(tree, model, new TreePath(model.getRoot()), expected, 0);
    }

    private TreePath findTreePath(JTree tree, TreeModel model, TreePath current, List<?> expected, int index) {
        String text = tree.convertValueToText(current.getLastPathComponent(), false, true,
                model.isLeaf(current.getLastPathComponent()), tree.getRowForPath(current), false);
        boolean match = text != null && text.equalsIgnoreCase(String.valueOf(expected.get(Math.min(index, expected.size() - 1))));
        if (index == 0 && !match) {
            Object node = current.getLastPathComponent();
            for (int i = 0; i < model.getChildCount(node); i++) {
                TreePath found = findTreePath(tree, model, current.pathByAddingChild(model.getChild(node, i)), expected, 0);
                if (found != null) {
                    return found;
                }
            }
            return null;
        }
        if (!match && index > 0) {
            return null;
        }
        int next = match ? index + 1 : index;
        if (next >= expected.size()) {
            return current;
        }
        Object node = current.getLastPathComponent();
        for (int i = 0; i < model.getChildCount(node); i++) {
            TreePath found = findTreePath(tree, model, current.pathByAddingChild(model.getChild(node, i)), expected, next);
            if (found != null) {
                return found;
            }
        }
        return null;
    }

    private JMenuItem findMenuItem(Object parent, List<?> path, int index) {
        if (index >= path.size()) {
            return parent instanceof JMenuItem ? (JMenuItem) parent : null;
        }
        String wanted = String.valueOf(path.get(index));
        if (parent instanceof JMenuBar) {
            JMenuBar bar = (JMenuBar) parent;
            for (int i = 0; i < bar.getMenuCount(); i++) {
                JMenu menu = bar.getMenu(i);
                if (menu != null && wanted.equalsIgnoreCase(menu.getText())) {
                    return findMenuItem(menu, path, index + 1);
                }
            }
        }
        if (parent instanceof JMenu) {
            JMenu menu = (JMenu) parent;
            for (int i = 0; i < menu.getItemCount(); i++) {
                JMenuItem item = menu.getItem(i);
                if (item != null && wanted.equalsIgnoreCase(item.getText())) {
                    if (index == path.size() - 1) {
                        return item;
                    }
                    return findMenuItem(item, path, index + 1);
                }
            }
        }
        return null;
    }

    private void press(String combo) throws Exception {
        Robot robot = new Robot();
        String[] parts = combo.toUpperCase(Locale.ROOT).split("\\+");
        List<Integer> mods = new ArrayList<>();
        int key = KeyEvent.VK_ENTER;
        for (String part : parts) {
            switch (part.trim()) {
                case "CTRL":
                case "CONTROL":
                    mods.add(KeyEvent.VK_CONTROL);
                    break;
                case "SHIFT":
                    mods.add(KeyEvent.VK_SHIFT);
                    break;
                case "ALT":
                    mods.add(KeyEvent.VK_ALT);
                    break;
                case "ENTER":
                    key = KeyEvent.VK_ENTER;
                    break;
                case "TAB":
                    key = KeyEvent.VK_TAB;
                    break;
                case "ESCAPE":
                case "ESC":
                    key = KeyEvent.VK_ESCAPE;
                    break;
                case "SPACE":
                    key = KeyEvent.VK_SPACE;
                    break;
                case "F5":
                    key = KeyEvent.VK_F5;
                    break;
                default:
                    if (part.length() == 1) {
                        key = KeyEvent.getExtendedKeyCodeForChar(part.charAt(0));
                    }
                    break;
            }
        }
        for (Integer mod : mods) {
            robot.keyPress(mod);
        }
        robot.keyPress(key);
        robot.keyRelease(key);
        for (int i = mods.size() - 1; i >= 0; i--) {
            robot.keyRelease(mods.get(i));
        }
    }

    private Map<String, Object> stateFlag(Map<String, Object> info, String flag) {
        return onEdt(() -> {
            ControlHandle handle = requireHandle(str(info.get("controlId")));
            if (handle == null) {
                return staleOrMissing(str(info.get("controlId")));
            }
            Map<String, Object> props = properties(handle);
            return Map.of(flag, props.get(flag), "controlId", handle.getId());
        });
    }

    private Map<String, Object> toTreeNode(Component component, int depth) {
        Map<String, Object> node = new LinkedHashMap<>();
        ControlHandle handle = registry.registerComponent(component, component.getClass().getName(),
                component.getName(), labelOf(component), classify(component));
        node.put("objectId", handle.getId());
        node.put("className", handle.getClassName());
        node.put("name", handle.getName());
        node.put("text", readValue(handle));
        node.put("accessibleName", accessibleName(component));
        node.put("enabled", component.isEnabled());
        node.put("visible", component.isShowing());
        Rectangle bounds = handle.boundsOnScreen();
        if (bounds != null) {
            node.put("bounds", Map.of("x", bounds.x, "y", bounds.y, "width", bounds.width, "height", bounds.height));
        }
        List<Map<String, Object>> children = new ArrayList<>();
        if (depth < 12 && component instanceof Container) {
            for (Component child : ((Container) component).getComponents()) {
                if (child.isShowing()) {
                    children.add(toTreeNode(child, depth + 1));
                }
            }
        }
        node.put("children", children);
        return node;
    }

    private String classify(Component component) {
        if (component instanceof JTextComponent) {
            return "TextField";
        }
        if (component instanceof JComboBox) {
            return "ComboBox";
        }
        if (component instanceof JCheckBox) {
            return "CheckBox";
        }
        if (component instanceof JRadioButton) {
            return "RadioButton";
        }
        if (component instanceof JMenuItem) {
            return "MenuItem";
        }
        if (component instanceof AbstractButton) {
            return "Button";
        }
        if (component instanceof JTable) {
            return "Table";
        }
        if (component instanceof JTree) {
            return "Tree";
        }
        if (component instanceof JTabbedPane) {
            return "TabbedPane";
        }
        if (component instanceof JLabel) {
            return "Label";
        }
        if (component instanceof JSpinner) {
            return "Spinner";
        }
        if (component instanceof JList) {
            return "List";
        }
        return component.getClass().getSimpleName();
    }

    private boolean isInteractive(Component component) {
        return component instanceof JTextComponent
                || component instanceof JComboBox
                || component instanceof AbstractButton
                || component instanceof JTable
                || component instanceof JTree
                || component instanceof JTabbedPane
                || component instanceof JSpinner
                || component instanceof JList;
    }

    private String labelOf(Component component) {
        String accessible = accessibleName(component);
        if (accessible != null && !accessible.isBlank()) {
            return accessible;
        }
        if (component instanceof AbstractButton) {
            return ((AbstractButton) component).getText();
        }
        if (component instanceof JLabel) {
            return ((JLabel) component).getText();
        }
        if (component instanceof JComponent) {
            JLabel label = findLabelFor((JComponent) component);
            if (label != null) {
                return label.getText();
            }
        }
        return component.getName();
    }

    private JLabel findLabelFor(JComponent component) {
        Container parent = component.getParent();
        if (parent == null) {
            return null;
        }
        for (Component child : parent.getComponents()) {
            if (child instanceof JLabel && ((JLabel) child).getLabelFor() == component) {
                return (JLabel) child;
            }
        }
        Rectangle self = component.getBounds();
        JLabel best = null;
        int bestDx = Integer.MAX_VALUE;
        for (Component child : parent.getComponents()) {
            if (!(child instanceof JLabel)) {
                continue;
            }
            JLabel label = (JLabel) child;
            if (label.getText() == null || label.getText().isBlank()) {
                continue;
            }
            Rectangle lb = label.getBounds();
            boolean sameRow = Math.abs((lb.y + lb.height / 2) - (self.y + self.height / 2)) <= 12;
            boolean left = lb.x + lb.width <= self.x + 4;
            if (sameRow && left && self.x - lb.x < bestDx) {
                best = label;
                bestDx = self.x - lb.x;
            }
        }
        return best;
    }

    private String sectionName(Component component) {
        Component current = component;
        while (current != null) {
            if (current instanceof JComponent) {
                Border border = ((JComponent) current).getBorder();
                if (border instanceof TitledBorder) {
                    String title = ((TitledBorder) border).getTitle();
                    if (title != null && !title.isBlank()) {
                        return title;
                    }
                }
            }
            current = current.getParent();
        }
        return windowTitle(SwingUtilities.getWindowAncestor(component));
    }

    private String accessibleName(Component component) {
        if (component == null) {
            return null;
        }
        try {
            return component.getAccessibleContext() == null ? null : component.getAccessibleContext().getAccessibleName();
        } catch (Exception ex) {
            return null;
        }
    }

    private String relationOf(Rectangle self, Rectangle other, Component a, Component b) {
        if (a instanceof Container && b != null && ((Container) a).isAncestorOf(b)) {
            return "CONTAINS";
        }
        if (b instanceof Container && a != null && ((Container) b).isAncestorOf(a)) {
            return "INSIDE";
        }
        if (Math.abs(self.y - other.y) <= 10) {
            return other.x >= self.x + self.width ? "RIGHT_OF" : other.x + other.width <= self.x ? "LEFT_OF" : "SAME_ROW";
        }
        if (Math.abs(self.x - other.x) <= 10) {
            return other.y >= self.y + self.height ? "BELOW" : "ABOVE";
        }
        return null;
    }

    private boolean matchesWindow(ControlHandle handle, String window) {
        if (window == null || window.isBlank() || handle.getComponent() == null) {
            return true;
        }
        return containsIgnoreCase(windowTitle(SwingUtilities.getWindowAncestor(handle.getComponent())), window);
    }

    private boolean matchesSection(ControlHandle handle, String section) {
        if (section == null || section.isBlank() || handle.getComponent() == null) {
            return true;
        }
        return containsIgnoreCase(sectionName(handle.getComponent()), section);
    }

    private Window activeWindow() {
        Window focused = KeyboardFocusManager.getCurrentKeyboardFocusManager().getActiveWindow();
        if (focused != null && focused.isShowing()) {
            return focused;
        }
        for (Window window : Window.getWindows()) {
            if (window.isShowing() && window.isActive()) {
                return window;
            }
        }
        return rootFrame();
    }

    private JFrame rootFrame() {
        for (Window window : Window.getWindows()) {
            if (window instanceof JFrame && window.isShowing()) {
                return (JFrame) window;
            }
        }
        return null;
    }

    private JDialog activeDialog() {
        for (Window window : Window.getWindows()) {
            if (window instanceof JDialog && window.isShowing()) {
                return (JDialog) window;
            }
        }
        return null;
    }

    private String activeTabTitle() {
        for (JTabbedPane pane : findOfType(JTabbedPane.class)) {
            int index = pane.getSelectedIndex();
            if (index >= 0) {
                return pane.getTitleAt(index);
            }
        }
        return null;
    }

    private String findStatusText() {
        for (ControlHandle handle : registry.all()) {
            if ("status.message".equals(handle.getName()) || "statusBar".equals(handle.getName())) {
                return stringValue(readValue(handle));
            }
        }
        return "Ready";
    }

    private String findDialogMessage(JDialog dialog) {
        AtomicReference<String> text = new AtomicReference<>("");
        walk(dialog, component -> {
            if (component instanceof JLabel && ((JLabel) component).getText() != null
                    && ((JLabel) component).getText().length() > text.get().length()) {
                text.set(((JLabel) component).getText());
            }
        });
        return text.get();
    }

    private void collectDialog(Container root, List<String> buttons, List<Map<String, Object>> controls) {
        walk(root, component -> {
            if (component instanceof AbstractButton) {
                buttons.add(((AbstractButton) component).getText());
            }
            if (isInteractive(component)) {
                controls.add(Map.of(
                        "type", classify(component),
                        "name", String.valueOf(component.getName()),
                        "text", component instanceof AbstractButton ? ((AbstractButton) component).getText() : ""
                ));
            }
        });
    }

    private AbstractButton findButton(Container root, String text) {
        AtomicReference<AbstractButton> found = new AtomicReference<>();
        walk(root, component -> {
            if (component instanceof AbstractButton && text.equalsIgnoreCase(((AbstractButton) component).getText())) {
                found.set((AbstractButton) component);
            }
        });
        return found.get();
    }

    private JPopupMenu visiblePopup() {
        for (Window window : Window.getWindows()) {
            if (window instanceof JWindow && window.isShowing()) {
                JPopupMenu popup = findPopup(window);
                if (popup != null) {
                    return popup;
                }
            }
        }
        for (ControlHandle handle : registry.all()) {
            if (handle.getComponent() instanceof JComponent) {
                JPopupMenu popup = ((JComponent) handle.getComponent()).getComponentPopupMenu();
                if (popup != null && popup.isShowing()) {
                    return popup;
                }
            }
        }
        return null;
    }

    private JPopupMenu findPopup(Container root) {
        AtomicReference<JPopupMenu> found = new AtomicReference<>();
        walk(root, component -> {
            if (component instanceof JPopupMenu && component.isShowing()) {
                found.set((JPopupMenu) component);
            }
        });
        return found.get();
    }

    private void walk(Container root, java.util.function.Consumer<Component> visitor) {
        visitor.accept(root);
        for (Component child : root.getComponents()) {
            visitor.accept(child);
            if (child instanceof Container) {
                walk((Container) child, visitor);
            }
        }
    }

    @SuppressWarnings("unchecked")
    private <T extends Component> List<T> findOfType(Class<T> type) {
        List<T> result = new ArrayList<>();
        for (Window window : Window.getWindows()) {
            if (!window.isShowing()) {
                continue;
            }
            walk(window, component -> {
                if (type.isInstance(component) && component.isShowing()) {
                    result.add((T) component);
                }
            });
        }
        return result;
    }

    private ControlHandle requireHandle(String id) {
        if (id == null || id.isBlank()) {
            return null;
        }
        ControlHandle handle = registry.get(id);
        if (handle == null || handle.isStale()) {
            refreshRegistry();
            handle = registry.get(id);
        }
        if (handle != null && handle.isStale()) {
            return null;
        }
        return handle;
    }

    private Map<String, Object> staleOrMissing(String id) {
        return McpResponses.error(id == null ? "CONTROL_NOT_FOUND" : "STALE_CONTROL_REFERENCE",
                "Control reference is missing or stale: " + id, true, "Call resolve_field or find_controls again.");
    }

    private String windowTitle(Window window) {
        if (window instanceof JFrame) {
            return ((JFrame) window).getTitle();
        }
        if (window instanceof Dialog) {
            return ((Dialog) window).getTitle();
        }
        return window == null ? "" : window.getName();
    }

    private boolean isSelectedValue(ControlHandle handle) {
        Component component = handle.getComponent();
        if (component instanceof AbstractButton) {
            return ((AbstractButton) component).isSelected();
        }
        return component != null && component.isFocusOwner();
    }

    private boolean isActionRole(String expected) {
        return expected != null && expected.toLowerCase(Locale.ROOT).contains("button");
    }

    private Object parseSpinner(String value) {
        try {
            return Integer.valueOf(value);
        } catch (Exception ex) {
            return value;
        }
    }

    private boolean matchText(String actual, String expected, String mode) {
        if (actual == null) {
            actual = "";
        }
        if (expected == null) {
            expected = "";
        }
        switch (mode) {
            case "EQUALS":
                return actual.equals(expected);
            case "STARTS_WITH":
                return actual.startsWith(expected);
            case "ENDS_WITH":
                return actual.endsWith(expected);
            case "REGEX":
                return actual.matches(expected);
            case "CONTAINS":
            default:
                return actual.contains(expected);
        }
    }

    private String contextWindow(Map<String, Object> info) {
        Map<String, Object> context = mapOf(info.get("context"));
        return firstNonBlank(str(info.get("window")), str(context.get("window")));
    }

    private String contextSection(Map<String, Object> info) {
        Map<String, Object> context = mapOf(info.get("context"));
        return firstNonBlank(str(info.get("section")), str(context.get("section")));
    }

    private boolean isFailure(Map<String, Object> result) {
        if (result == null) {
            return true;
        }
        if (Boolean.FALSE.equals(result.get("success")) || Boolean.FALSE.equals(result.get("passed"))) {
            return true;
        }
        Object status = result.get("status");
        return "NOT_FOUND".equals(status) || "AMBIGUOUS".equals(status) || "NOT_INTERACTABLE".equals(status);
    }

    private <T> T onEdt(java.util.concurrent.Callable<T> task) {
        try {
            if (SwingUtilities.isEventDispatchThread()) {
                return task.call();
            }
            AtomicReference<T> result = new AtomicReference<>();
            AtomicReference<Exception> error = new AtomicReference<>();
            SwingUtilities.invokeAndWait(() -> {
                try {
                    result.set(task.call());
                } catch (Exception ex) {
                    error.set(ex);
                }
            });
            if (error.get() != null) {
                throw error.get();
            }
            return result.get();
        } catch (Exception ex) {
            throw new RuntimeException(ex);
        }
    }

    private static void sleep(long ms) {
        try {
            Thread.sleep(ms);
        } catch (InterruptedException ignored) {
            Thread.currentThread().interrupt();
        }
    }

    private static String str(Object value) {
        return value == null ? null : String.valueOf(value);
    }

    private static String safe(String value) {
        return value == null ? "" : value;
    }

    private static String stringValue(Object value) {
        return value == null ? "" : String.valueOf(value);
    }

    private static String firstNonBlank(String... values) {
        for (String value : values) {
            if (value != null && !value.isBlank()) {
                return value;
            }
        }
        return null;
    }

    private static Object firstNonNull(Object a, Object b) {
        return a != null ? a : b;
    }

    private static boolean containsIgnoreCase(String haystack, String needle) {
        return haystack != null && needle != null && haystack.toLowerCase(Locale.ROOT).contains(needle.toLowerCase(Locale.ROOT));
    }

    private static String normalize(String value) {
        return value == null ? "" : value.trim();
    }

    private static double round(double value) {
        return Math.round(value * 100.0) / 100.0;
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> mapOf(Object value) {
        if (value instanceof Map) {
            return (Map<String, Object>) value;
        }
        return new LinkedHashMap<>();
    }

    @SuppressWarnings("unchecked")
    private static List<?> listOf(Object value) {
        if (value instanceof List) {
            return (List<?>) value;
        }
        return List.of();
    }

    private static final class Scored {
        private final ControlHandle handle;
        private final double score;

        private Scored(ControlHandle handle, double score) {
            this.handle = handle;
            this.score = score;
        }
    }
}
