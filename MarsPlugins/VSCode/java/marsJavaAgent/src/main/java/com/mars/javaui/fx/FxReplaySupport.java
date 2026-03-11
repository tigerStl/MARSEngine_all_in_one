package com.mars.javaui.fx;

import java.awt.Robot;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

/**
 * JavaFX replay: resolve parent/object from identifier, then simulate with Robot (mouse/keyboard).
 * Resolution is in FxReplayResolver; operations use screen bounds and Robot like AWT/Swing.
 */
public final class FxReplaySupport extends FxReflectionSupport {

    private FxReplaySupport() {}

    /** Callbacks for typing/clearing text and step data. Implemented by RecordAgent. */
    public interface FxReplayCallbacks {
        String getStepData(Object step);
        void typeText(Robot robot, String text);
        void clearFocusedText(Robot robot, int deleteCount);
        String sanitizeFillEditInput(String data);
    }

    public static boolean isJavaFxObjectKey(Map<String, Object> key) {
        if (key == null) return false;
        Object jt = key.get("javaType");
        if (jt == null) return false;
        String t = String.valueOf(jt);
        return t.startsWith("javafx.");
    }

    /** Whether this step is JavaFX Table SearchAndUpdate/SearchAndClick (object = TableView). */
    public static boolean isJavaFxTableStep(Map<String, Object> objectKey, String keyword) {
        if (!"SearchAndUpdate".equals(keyword) && !"SearchAndClick".equals(keyword)) return false;
        if (objectKey == null) return false;
        Object cat = objectKey.get("objectCategory");
        if ("javaFxTable".equals(cat)) return true;
        Object jt = objectKey.get("javaType");
        if (jt == null) return false;
        String t = String.valueOf(jt);
        return t.contains("TableView");
    }

    /**
     * Resolve JavaFX parent then object by identifier, then replay with Robot (mouse/keyboard).
     * Same flow as AWT/Swing: parent = top window/dialog, object = node under scene graph.
     * Locators: javaType, javaTypePath, javaName/Name, index, title, text.
     *
     * @param parentKey  parent identifier (top window); null/empty = first JavaFX window
     * @param objectKey  object identifier
     * @return null on success, error message on failure
     */
    public static String resolveAndReplayJavaFx(
            Map<String, Object> parentKey,
            Map<String, Object> objectKey,
            String keyword,
            Object step,
            Robot robot,
            FxReplayCallbacks callbacks) {
        return resolveAndReplayJavaFxWithWait(parentKey, objectKey, keyword, step, robot, callbacks, 0L);
    }

    /**
     * JavaFX resolve+replay with optional wait:
     * - When waitMillis > 0, repeatedly tries resolveParent/resolveObject until timeout.
     * - If multiple nodes match and index is not specified, treats as error and reports count.
     */
    public static String resolveAndReplayJavaFxWithWait(
            Map<String, Object> parentKey,
            Map<String, Object> objectKey,
            String keyword,
            Object step,
            Robot robot,
            FxReplayCallbacks callbacks,
            long waitMillis) {
        long deadline = waitMillis > 0 ? System.currentTimeMillis() + waitMillis : 0L;
        Object parent = null;
        List<Object> matches = null;
        do {
            parent = FxReplayResolver.resolveParent(parentKey);
            if (parent != null) {
                matches = FxReplayResolver.resolveObjects(parent, objectKey);
                if (matches != null && !matches.isEmpty()) break;
            }
            if (waitMillis <= 0) break;
            try {
                Thread.sleep(200L);
            } catch (InterruptedException ie) {
                Thread.currentThread().interrupt();
                break;
            }
        } while (System.currentTimeMillis() < deadline);

        if (parent == null) {
            String err = FxReplayResolver.getLastParentError();
            return err != null ? err : "JavaFX parent not found";
        }
        if (matches == null || matches.isEmpty()) {
            return "JavaFX object not found under parent";
        }
        Integer index = FxReplayResolver.parseIndex(objectKey.get("index"));
        Object node;
        if (index != null && index >= 0 && index < matches.size()) {
            node = matches.get(index);
        } else if (matches.size() > 1) {
            return "JavaFX object locator is ambiguous: " + matches.size()
                    + " nodes matched. Please refine locator (e.g. add index or more specific name/text).";
        } else {
            node = matches.get(0);
        }
        int[] bounds = FxReplayResolver.getNodeScreenBounds(node);
        if (bounds == null || bounds[2] <= 0 || bounds[3] <= 0) {
            return "JavaFX object has no screen bounds";
        }
        Map<String, Object> keyWithBounds = new LinkedHashMap<>(objectKey != null ? objectKey : Map.of());
        Map<String, Object> sb = new LinkedHashMap<>();
        sb.put("x", bounds[0]);
        sb.put("y", bounds[1]);
        sb.put("width", bounds[2]);
        sb.put("height", bounds[3]);
        keyWithBounds.put("screenBounds", sb);
        return replayJavaFxByBounds(keyWithBounds, keyword, step, robot, callbacks);
    }

    @SuppressWarnings("unchecked")
    public static int[] getScreenBoundsFromObjectKey(Map<String, Object> key) {
        if (key == null) return null;
        Object sb = key.get("screenBounds");
        if (!(sb instanceof Map)) {
            sb = key.get("bounds");
        }
        if (!(sb instanceof Map)) return null;
        Map<String, Object> m = (Map<String, Object>) sb;
        Integer x = parseAnyInt(m.get("x"));
        Integer y = parseAnyInt(m.get("y"));
        Integer w = parseAnyInt(m.containsKey("width") ? m.get("width") : m.get("w"));
        Integer h = parseAnyInt(m.containsKey("height") ? m.get("height") : m.get("h"));
        if (x == null || y == null || w == null || h == null) return null;
        return new int[]{x, y, w, h};
    }

    private static Integer parseAnyInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(v)));
        } catch (NumberFormatException e) {
            return null;
        }
    }

    /**
     * Replay one JavaFX step by Robot at screen bounds center.
     *
     * @param objectKey step object identifier (must contain screenBounds)
     * @param keyword   step keyword
     * @param step      full step (for data); pass-through to callbacks
     * @param robot     AWT Robot
     * @param callbacks typing/clear/sanitize (from RecordAgent)
     * @return null on success, error message on failure
     */
    public static String replayJavaFxByBounds(
            Map<String, Object> objectKey,
            String keyword,
            Object step,
            Robot robot,
            FxReplayCallbacks callbacks) {
        int[] b = getScreenBoundsFromObjectKey(objectKey);
        if (b == null || b[2] <= 0 || b[3] <= 0) {
            return "JavaFX object has no screenBounds";
        }
        int cx = b[0] + b[2] / 2;
        int cy = b[1] + b[3] / 2;
        String data = callbacks != null ? callbacks.getStepData(step) : null;
        try {
            if ("FillEdit".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.delay(120);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                String text = callbacks != null ? callbacks.sanitizeFillEditInput(data) : (data != null ? data : "");
                if (callbacks != null) callbacks.clearFocusedText(robot, 30);
                if (text != null && !text.isEmpty() && callbacks != null) callbacks.typeText(robot, text);
                robot.keyPress(KeyEvent.VK_ENTER);
                robot.keyRelease(KeyEvent.VK_ENTER);
                robot.delay(120);
                return null;
            }
            if ("DoubleClickButton".equals(keyword) || "DoubleClick".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.delay(120);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(60);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                return null;
            }
            if ("SelectDropList".equals(keyword) || "SelectDropDown".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.delay(120);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                String text = callbacks != null ? callbacks.sanitizeFillEditInput(data) : (data != null ? data : "");
                if (text != null && !text.isEmpty() && callbacks != null) {
                    callbacks.typeText(robot, text);
                    robot.keyPress(KeyEvent.VK_ENTER);
                    robot.keyRelease(KeyEvent.VK_ENTER);
                }
                robot.delay(120);
                return null;
            }
            if ("SelectMenuItem".equals(keyword) || "SelectPopupMenu".equals(keyword) || "SelectListItem".equals(keyword)
                    || "SelectTreeList".equals(keyword) || "SetRadioBox".equals(keyword) || "SetCheckBox".equals(keyword)
                    || "ClickButton".equals(keyword) || "SelectTab".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.delay(120);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(150);
                return null;
            }
            robot.mouseMove(cx, cy);
            robot.delay(120);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(120);
            return null;
        } catch (Exception e) {
            return "JavaFX replay failed: " + e.getMessage();
        }
    }

    // ---------- JavaFX TableView SearchAndUpdate / SearchAndClick ----------

    /** Replay SearchAndUpdate on a JavaFX TableView: find row by condition, scroll, edit cell to targetValue. */
    public static String replayJavaFxTableViewSearchAndUpdate(
            Object tableView,
            String para,
            String data,
            Robot robot,
            FxReplayCallbacks callbacks) {
        if (tableView == null) return "TableView is null";
        if (para == null || para.trim().isEmpty()) return "SearchAndUpdate parameter is empty";
        FxTableReplaySpec spec = parseFxTableSpec(para, data, true);
        if (spec == null) return "SearchAndUpdate para/data format is invalid";
        int targetCol = findFxTableColumnIndex(tableView, spec.targetColumn);
        if (targetCol < 0) return "Target column not found: " + spec.targetColumn;
        List<Integer> condColIndexes = resolveConditionColumnIndexes(tableView, spec.conditionColumns);
        if (condColIndexes.size() != spec.conditionColumns.size()) return "Condition column(s) not found";
        int matchedRow = findMatchingRow(tableView, condColIndexes, spec.conditionValues, targetCol, spec.sourceValue, true);
        if (matchedRow < 0) return "Unable to locate row by condition values";
        scrollFxTableViewToRow(tableView, matchedRow);
        robot.delay(200);
        int[] cellBounds = getFxTableCellScreenBounds(tableView, matchedRow, targetCol);
        if (cellBounds == null || cellBounds[2] <= 0 || cellBounds[3] <= 0) return "Target cell bounds unavailable";
        int cx = cellBounds[0] + cellBounds[2] / 2;
        int cy = cellBounds[1] + cellBounds[3] / 2;
        try {
            robot.mouseMove(cx, cy);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(80);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(120);
            String targetVal = callbacks != null ? callbacks.sanitizeFillEditInput(spec.targetValue) : (spec.targetValue != null ? spec.targetValue : "");
            if (callbacks != null) callbacks.clearFocusedText(robot, 30);
            if (targetVal != null && !targetVal.isEmpty() && callbacks != null) callbacks.typeText(robot, targetVal);
            robot.keyPress(KeyEvent.VK_ENTER);
            robot.keyRelease(KeyEvent.VK_ENTER);
            robot.delay(120);
            return null;
        } catch (Exception e) {
            return "SearchAndUpdate replay failed: " + e.getMessage();
        }
    }

    /** Replay SearchAndClick on a JavaFX TableView: find row by condition, scroll, click/double-click/right-click. */
    public static String replayJavaFxTableViewSearchAndClick(
            Object tableView,
            String para,
            String data,
            Robot robot) {
        if (tableView == null) return "TableView is null";
        if (para == null || para.trim().isEmpty()) return "SearchAndClick parameter is empty";
        FxTableReplaySpec spec = parseFxTableSpec(para, data, false);
        if (spec == null) return "SearchAndClick para/data format is invalid";
        int targetCol = findFxTableColumnIndex(tableView, spec.targetColumn);
        if (targetCol < 0) return "Target column not found: " + spec.targetColumn;
        List<Integer> condColIndexes = resolveConditionColumnIndexes(tableView, spec.conditionColumns);
        if (condColIndexes.size() != spec.conditionColumns.size()) return "Condition column(s) not found";
        int matchedRow = findMatchingRow(tableView, condColIndexes, spec.conditionValues, targetCol, spec.sourceValue, false);
        if (matchedRow < 0) return "Unable to locate row by condition values";
        scrollFxTableViewToRow(tableView, matchedRow);
        robot.delay(200);
        int[] cellBounds = getFxTableCellScreenBounds(tableView, matchedRow, targetCol);
        if (cellBounds == null || cellBounds[2] <= 0 || cellBounds[3] <= 0) return "Target cell bounds unavailable";
        int cx = cellBounds[0] + cellBounds[2] / 2;
        int cy = cellBounds[1] + cellBounds[3] / 2;
        try {
            robot.mouseMove(cx, cy);
            boolean rightClick = "Action:RightClick".equalsIgnoreCase(spec.clickAction);
            boolean doubleClick = "Action:DoubleClick".equalsIgnoreCase(spec.clickAction);
            if (rightClick) {
                robot.mousePress(InputEvent.BUTTON3_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON3_DOWN_MASK);
            } else {
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                if (doubleClick) {
                    robot.delay(60);
                    robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                    robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                }
            }
            robot.delay(150);
            return null;
        } catch (Exception e) {
            return "SearchAndClick replay failed: " + e.getMessage();
        }
    }

    private static final class FxTableReplaySpec {
        String targetColumn;
        List<String> conditionColumns = new ArrayList<>();
        List<String> conditionValues = new ArrayList<>();
        String sourceValue;
        String targetValue;
        String clickAction; // Action:RightClick, Action:DoubleClick
    }

    /** Parse parameter "[condCol1;condCol2...];TargetColumn" and data "[val1;val2...];TargetValue" or "...];Action:RightClick". */
    private static FxTableReplaySpec parseFxTableSpec(String para, String data, boolean forUpdate) {
        if (para == null || data == null) return null;
        String p = para.trim();
        if (!p.contains("[") || p.indexOf(']') < 0) return null;
        int bracketStart = p.indexOf('[');
        int bracketEnd = p.indexOf(']');
        if (bracketStart < 0 || bracketEnd <= bracketStart) return null;
        String targetCol = p.substring(bracketEnd + 1).trim();
        if (targetCol.startsWith(";")) targetCol = targetCol.substring(1).trim();
        if (targetCol.isEmpty()) return null;
        String inside = p.substring(bracketStart + 1, bracketEnd).trim();
        String[] colParts = inside.isEmpty() ? new String[0] : inside.split(";");
        FxTableReplaySpec spec = new FxTableReplaySpec();
        spec.targetColumn = targetCol;
        for (String c : colParts) {
            String t = c != null ? c.trim() : "";
            if (!t.isEmpty()) spec.conditionColumns.add(t);
        }
        String d = data.trim();
        if (!d.contains("]")) return null;
        int dataBracket = d.indexOf(']');
        String valsPart = d.substring(1, dataBracket).trim();
        String rest = d.substring(dataBracket + 1).trim();
        if (rest.startsWith(";")) rest = rest.substring(1).trim();
        String[] valParts = valsPart.isEmpty() ? new String[0] : valsPart.split(";");
        for (String v : valParts) spec.conditionValues.add(v != null ? v.trim() : "");
        if (rest.startsWith("Action:")) {
            spec.clickAction = rest;
            spec.targetValue = "";
        } else {
            spec.clickAction = null;
            spec.targetValue = rest;
            if (forUpdate && rest.contains(":") && !rest.startsWith("Action:")) {
                int colon = rest.indexOf(':');
                spec.sourceValue = rest.substring(0, colon).trim();
                spec.targetValue = rest.substring(colon + 1).trim();
            }
        }
        return spec;
    }

    private static int findFxTableColumnIndex(Object tableView, String columnNameOrRegex) {
        if (tableView == null || columnNameOrRegex == null) return -1;
        try {
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return -1;
            int n = listSize(columns);
            for (int i = 0; i < n; i++) {
                Object col = listGet(columns, i);
                String header = getColumnHeaderText(col);
                if (header != null && (header.equals(columnNameOrRegex) || matchesRegex(header, columnNameOrRegex)))
                    return i;
            }
        } catch (Exception ignored) { }
        return -1;
    }

    private static String getColumnHeaderText(Object tableColumn) {
        if (tableColumn == null) return null;
        Object t = invokeNoArg(tableColumn, "getText");
        if (t != null && !String.valueOf(t).trim().isEmpty()) return String.valueOf(t).trim();
        Object g = invokeNoArg(tableColumn, "getGraphic");
        if (g != null) {
            Object gt = invokeNoArg(g, "getText");
            if (gt != null && !String.valueOf(gt).trim().isEmpty()) return String.valueOf(gt).trim();
        }
        Object id = invokeNoArg(tableColumn, "getId");
        if (id != null && !String.valueOf(id).trim().isEmpty()) return String.valueOf(id).trim();
        return null;
    }

    private static List<Integer> resolveConditionColumnIndexes(Object tableView, List<String> conditionColumns) {
        List<Integer> out = new ArrayList<>();
        if (tableView == null || conditionColumns == null) return out;
        for (String name : conditionColumns) {
            int idx = findFxTableColumnIndex(tableView, name);
            out.add(idx);
        }
        return out;
    }

    private static int findMatchingRow(Object tableView, List<Integer> condColIndexes, List<String> conditionValues,
                                      int targetCol, String sourceValue, boolean checkSource) {
        if (tableView == null) return -1;
        try {
            Object items = invokeNoArg(tableView, "getItems");
            if (items == null) return -1;
            int rows = listSize(items);
            for (int r = 0; r < rows; r++) {
                Object rowItem = listGet(items, r);
                boolean match = true;
                for (int i = 0; i < condColIndexes.size(); i++) {
                    int cIdx = condColIndexes.get(i);
                    if (cIdx < 0) { match = false; break; }
                    String cellVal = getFxTableCellValueAt(tableView, rowItem, cIdx);
                    String expected = i < conditionValues.size() ? conditionValues.get(i) : "";
                    if (!matchesRegex(cellVal != null ? cellVal : "", expected != null ? expected : "")) {
                        match = false;
                        break;
                    }
                }
                if (match && checkSource && sourceValue != null && !sourceValue.isEmpty()) {
                    String srcCell = getFxTableCellValueAt(tableView, rowItem, targetCol);
                    if (!matchesRegex(srcCell != null ? srcCell : "", sourceValue)) match = false;
                }
                if (match) return r;
            }
        } catch (Exception ignored) { }
        return -1;
    }

    private static String getFxTableCellValueAt(Object tableView, Object rowItem, int colIndex) {
        if (tableView == null || rowItem == null || colIndex < 0) return "";
        try {
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return "";
            Object col = listGet(columns, colIndex);
            if (col == null) return "";
            try {
                Method getCellObs = col.getClass().getMethod("getCellObservableValue", Object.class);
                Object obs = getCellObs.invoke(col, rowItem);
                if (obs != null) {
                    Object v = invokeNoArg(obs, "getValue");
                    return v != null ? String.valueOf(v) : "";
                }
            } catch (Exception e) {
                Method getCellVal = col.getClass().getMethod("getCellValue", Object.class);
                Object v = getCellVal.invoke(col, rowItem);
                return v != null ? String.valueOf(v) : "";
            }
        } catch (Exception ignored) { }
        return "";
    }

    private static boolean matchesRegex(String actual, String pattern) {
        if (actual == null) actual = "";
        if (pattern == null || pattern.isEmpty()) return actual.isEmpty();
        if (actual.equals(pattern)) return true;
        try {
            return Pattern.compile(pattern).matcher(actual).matches();
        } catch (PatternSyntaxException e) {
            return false;
        }
    }

    private static void scrollFxTableViewToRow(Object tableView, int rowIndex) {
        if (tableView == null || rowIndex < 0) return;
        runOnFxThread(() -> {
            try {
                Method scrollTo = tableView.getClass().getMethod("scrollTo", int.class);
                scrollTo.invoke(tableView, rowIndex);
            } catch (Exception e) {
                try {
                    Object items = invokeNoArg(tableView, "getItems");
                    if (items != null) {
                        Object rowItem = listGet(items, rowIndex);
                        if (rowItem != null) {
                            Method scrollToObj = tableView.getClass().getMethod("scrollTo", Object.class);
                            scrollToObj.invoke(tableView, rowItem);
                        }
                    }
                } catch (Exception e2) {
                    // ignore
                }
            }
        });
    }

    /** Get screen bounds [x, y, width, height] of the table cell at (row, col). Runs on FX thread. */
    private static int[] getFxTableCellScreenBounds(Object tableView, int row, int col) {
        final int[][] result = new int[1][];
        final CountDownLatch latch = new CountDownLatch(1);
        runOnFxThread(() -> {
            try {
                Object selectionModel = invokeNoArg(tableView, "getSelectionModel");
                Object columns = invokeNoArg(tableView, "getColumns");
                Object tableColumn = columns != null ? listGet(columns, col) : null;
                if (selectionModel != null && tableColumn != null) {
                    try {
                        for (Method m : selectionModel.getClass().getMethods()) {
                            if ("clearAndSelect".equals(m.getName()) && m.getParameterCount() == 2
                                    && m.getParameterTypes()[0] == int.class) {
                                m.invoke(selectionModel, row, tableColumn);
                                break;
                            }
                        }
                    } catch (Exception e) {
                        try {
                            Method select = selectionModel.getClass().getMethod("select", int.class);
                            select.invoke(selectionModel, row);
                        } catch (Exception e2) {
                            // ignore
                        }
                    }
                }
                scrollFxTableViewToRow(tableView, row);
                try {
                    Thread.sleep(150);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                }
                Object cellNode = findTableCellNode(tableView, row, col);
                if (cellNode != null) {
                    result[0] = FxReplayResolver.getNodeScreenBounds(cellNode);
                }
            } catch (Exception e) {
                result[0] = null;
            } finally {
                latch.countDown();
            }
        });
        try {
            latch.await(3, TimeUnit.SECONDS);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
        return result[0];
    }

    private static Object findTableCellNode(Object tableView, int row, int col) {
        if (tableView == null) return null;
        try {
            Object skin = invokeNoArg(tableView, "getSkin");
            if (skin == null) return null;
            Object root = invokeNoArg(skin, "getRoot");
            if (root == null) return null;
            List<Object> children = getNodeChildren(root);
            for (Object ch : children) {
                Object cell = findTableRowCellWithIndex(ch, row, col, tableView);
                if (cell != null) return cell;
            }
            Method getVirtualFlow = null;
            for (Method m : skin.getClass().getMethods()) {
                if ("getVirtualFlow".equals(m.getName()) && m.getParameterCount() == 0) {
                    getVirtualFlow = m;
                    break;
                }
            }
            if (getVirtualFlow != null) {
                Object flow = getVirtualFlow.invoke(skin);
                if (flow != null) {
                    Object flowChildren = invokeNoArg(flow, "getChildren");
                    if (flowChildren != null) {
                        int n = listSize(flowChildren);
                        for (int i = 0; i < n; i++) {
                            Object rowNode = listGet(flowChildren, i);
                            Object cell = findTableRowCellWithIndex(rowNode, row, col, tableView);
                            if (cell != null) return cell;
                        }
                    }
                }
            }
        } catch (Exception ignored) { }
        return null;
    }

    private static Object findTableRowCellWithIndex(Object rowNode, int wantRow, int wantCol, Object tableView) {
        if (rowNode == null) return null;
        try {
            Object rowIndex = invokeNoArg(rowNode, "getIndex");
            int r = rowIndex instanceof Number ? ((Number) rowIndex).intValue() : -1;
            if (r != wantRow) return null;
            List<Object> rowChildren = getNodeChildren(rowNode);
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return null;
            Object wantColumn = listGet(columns, wantCol);
            for (Object cell : rowChildren) {
                if (cell == null) continue;
                Object cellCol = invokeNoArg(cell, "getTableColumn");
                if (cellCol == wantColumn) return cell;
            }
        } catch (Exception ignored) { }
        return null;
    }

    private static List<Object> getNodeChildren(Object node) {
        if (node == null) return new ArrayList<>();
        try {
            Method m = node.getClass().getMethod("getChildrenUnmodifiable");
            Object listObj = m.invoke(node);
            return listToList(listObj);
        } catch (Exception e) {
            try {
                Method m = node.getClass().getMethod("getChildren");
                Object listObj = m.invoke(node);
                return listToList(listObj);
            } catch (Exception e2) {
                return new ArrayList<>();
            }
        }
    }

    private static List<Object> listToList(Object observableList) {
        if (observableList == null) return new ArrayList<>();
        try {
            Method getM = java.util.List.class.getMethod("get", int.class);
            Method sizeM = java.util.List.class.getMethod("size");
            int n = ((Number) sizeM.invoke(observableList)).intValue();
            List<Object> out = new ArrayList<>(n);
            for (int i = 0; i < n; i++) out.add(getM.invoke(observableList, i));
            return out;
        } catch (Exception e) {
            return new ArrayList<>();
        }
    }

    private static int listSize(Object list) {
        if (list == null) return 0;
        try {
            Method sizeM = java.util.List.class.getMethod("size");
            Object sz = sizeM.invoke(list);
            return (sz instanceof Number) ? ((Number) sz).intValue() : 0;
        } catch (Exception e) {
            return 0;
        }
    }

    private static Object listGet(Object list, int index) {
        if (list == null) return null;
        try {
            Method getM = java.util.List.class.getMethod("get", int.class);
            return getM.invoke(list, index);
        } catch (Exception e) {
            return null;
        }
    }

    private static void runOnFxThread(Runnable r) {
        try {
            Class<?> platform = Class.forName("javafx.application.Platform");
            Method runLater = platform.getMethod("runLater", Runnable.class);
            Method isFx = platform.getMethod("isFxApplicationThread");
            if (Boolean.TRUE.equals(isFx.invoke(null))) {
                r.run();
            } else {
                CountDownLatch latch = new CountDownLatch(1);
                runLater.invoke(null, (Runnable) () -> {
                    try {
                        r.run();
                    } finally {
                        latch.countDown();
                    }
                });
                latch.await(5, TimeUnit.SECONDS);
            }
        } catch (Exception e) {
            throw new RuntimeException("FX runLater failed", e);
        }
    }

    // invokeNoArg now comes from FxReflectionSupport
}
