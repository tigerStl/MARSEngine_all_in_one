package com.mars.javaui.record.eventoperation;

import java.awt.Component;
import java.awt.Container;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.io.IOException;
import java.io.Writer;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.logging.Level;

import javax.accessibility.AccessibleContext;
import javax.accessibility.AccessibleRole;
import javax.swing.AbstractButton;
import javax.swing.JComboBox;
import javax.swing.JCheckBox;
import javax.swing.JMenu;
import javax.swing.JMenuItem;
import javax.swing.JPopupMenu;
import javax.swing.JRadioButton;
import javax.swing.JTabbedPane;
import javax.swing.JTable;
import javax.swing.JTree;
import javax.swing.SwingUtilities;

import org.java_websocket.WebSocket;

import com.mars.javaui.keyword.MarsKeyword;
import com.mars.javaui.record.AgentLogger;
import com.mars.javaui.record.RecordAgent;
import com.mars.javaui.record.config.EventFilterConfig;

/** Handles mouse events for recording: click/double-click merge, menu, tree, toolbar, ComboBox. */
public final class MouseEventHandler {

    private static final java.util.logging.Logger LOG = java.util.logging.Logger.getLogger(MouseEventHandler.class.getName());
    private static final boolean MOUSE_CLICK_TRACE_ENABLED = Boolean.parseBoolean(
            System.getProperty("mars.record.mouse.click.trace.enabled", "true"));

    public static void handle(MouseEvent me, RecordingContext ctx) {
        int id = me.getID();
        if (id == MouseEvent.MOUSE_CLICKED) return;

        Component rawComp = me.getComponent();
        JTable table = RecordAgent.resolveJTable(rawComp);
        if (table != null) {
            handleTableMouseEvent(me, ctx, table);
            return;
        }

        Component clickTarget = RecordAgent.resolveClickTarget(rawComp);
        if (clickTarget == null) return;
        if (EventFilterConfig.shouldSkipMouseKeyboard(clickTarget)) return;
        if (!clickTarget.isShowing() || !clickTarget.isEnabled()) return;

        if (!(clickTarget instanceof JMenuItem)) {
            ctx.pendingSelectMenuItemStepRef[0] = null;
        }

        long now = System.currentTimeMillis();
        int button = me.getButton();
        int x = me.getX();
        int y = me.getY();

        int screenX = 0, screenY = 0;
        try {
            Point onScreen = me.getLocationOnScreen();
            if (onScreen != null) {
                screenX = onScreen.x;
                screenY = onScreen.y;
            }
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.FINE, "getLocationOnScreen", e);
        }

        Rectangle rect = clickTarget.getBounds();
        String objectType = clickTarget.getClass().getName();
        String parentType = clickTarget.getParent() != null ? clickTarget.getParent().getClass().getName() : "";
        String eventKind = (id == MouseEvent.MOUSE_PRESSED) ? "Pressed" : (id == MouseEvent.MOUSE_RELEASED) ? "Released" : "Other";
        if (MOUSE_CLICK_TRACE_ENABLED) {
            if (id == MouseEvent.MOUSE_PRESSED || id == MouseEvent.MOUSE_RELEASED) {
                String objectName = RecordAgent.getComponentNameForLog(clickTarget);
                AgentLogger.info(LOG, "ClickEvent time=" + now + ", name=" + objectName + ", type=" + objectType
                    + ", parentType=" + parentType
                    + ", phase=" + eventKind + ", x=" + x + ", y=" + y + ", screenX=" + screenX + ", screenY=" + screenY);
            }
            RecordAgent.appendDebugLog(ctx.outputDir, "MouseEvent_[" + eventKind + "], " + now + ", " + objectType
                + ", parentType=" + parentType
                    + ", x=" + x + ",y=" + y + ", screenX=" + screenX + ",screenY=" + screenY
                    + ", rect=" + rect.x + "," + rect.y + "," + rect.width + "," + rect.height);
        }

        if (clickTarget instanceof JComboBox) {
            if (id == MouseEvent.MOUSE_PRESSED) {
                ctx.currentComboBoxRef[0] = (JComboBox<?>) clickTarget;
                ctx.currentComboInteractedRef[0] = true;
                if (ctx.currentComboInitialRef[0] == null) {
                    ctx.currentComboInitialRef[0] = RecordAgent.getComboSelectedText((JComboBox<?>) clickTarget);
                }
                ctx.currentComboSelectedRef[0] = RecordAgent.getComboSelectedText((JComboBox<?>) clickTarget);
            }
            return;
        }

        if (clickTarget instanceof JMenuItem) {
            if (id == MouseEvent.MOUSE_RELEASED) {
                JMenuItem mi = (JMenuItem) clickTarget;
                boolean isPopup = isPopupMenuItem(mi);
                if (isPopup && isRecentTableRightClick(ctx, now)) {
                    String data = mi.getText() != null ? mi.getText() : "";
                    Map<String, Object> step = MarsKeyword.buildScriptStep("SelectPopupMenu", mi, "", data, "");
                    step.put("event", "selectPopupMenu");
                    step.put("timestamp", now);
                    RecordAgent.putComponentInfo(step, mi);
                    step.put("content", data);
                    appendRightClickParameter(step, button);
                    emitStep(ctx, step);
                    if (button == MouseEvent.BUTTON3) {
                        emitRightClickAtStep(ctx, now);
                    }
                    ctx.lastTableRightClickRef[0] = null;
                    ctx.lastTableRightClickTimeRef[0] = 0L;
                } else {
                    handleMenuItemClick(ctx, mi, now, button);
                }
            }
            return;
        }

        JTree tree = RecordAgent.resolveJTree(clickTarget);
        if (tree != null) {
            if (id == MouseEvent.MOUSE_RELEASED) {
                String data = RecordAgent.buildTreePathString(tree);
                if (data != null && !data.isEmpty()) {
                    Map<String, Object> step = MarsKeyword.buildScriptStep("SelectTreeList", tree, "", data, "");
                    step.put("event", "selectTreeList");
                    step.put("timestamp", now);
                    RecordAgent.putComponentInfo(step, tree);
                    step.put("content", data);
                    appendRightClickParameter(step, button);
                    emitStep(ctx, step);
                }
            }
            return;
        }

        if (RecordAgent.isToolButtonLike(clickTarget)) {
            if (id == MouseEvent.MOUSE_RELEASED) {
                Component obj = RecordAgent.findToolbarParentOrSelf(clickTarget);
                String data = RecordAgent.getToolButtonSemanticText(clickTarget);
                Map<String, Object> step = MarsKeyword.buildScriptStep("SelectMenuIcon", obj, "", data, "");
                step.put("event", "selectMenuIcon");
                step.put("timestamp", now);
                RecordAgent.putComponentInfo(step, obj);
                step.put("content", data);
                emitStep(ctx, step);
            }
            return;
        }

        if (clickTarget instanceof JTabbedPane) {
            if (id == MouseEvent.MOUSE_RELEASED) {
                JTabbedPane tabbedPane = (JTabbedPane) clickTarget;
                Point tabPoint = SwingUtilities.convertPoint(me.getComponent(), me.getPoint(), tabbedPane);
                int tabIndex = tabbedPane.indexAtLocation(tabPoint.x, tabPoint.y);
                if (tabIndex < 0) {
                    tabIndex = tabbedPane.getSelectedIndex();
                }
                if (tabIndex >= 0 && tabIndex < tabbedPane.getTabCount()) {
                    String header = tabbedPane.getTitleAt(tabIndex);
                    if (header == null) header = "";
                    Map<String, Object> step = MarsKeyword.buildScriptStep("SelectTab", tabbedPane, "", header, "");
                    step.put("event", "selectTab");
                    step.put("timestamp", now);
                    step.put("index", tabIndex);
                    RecordAgent.putComponentInfo(step, tabbedPane);
                    step.put("content", header);
                    appendRightClickParameter(step, button);
                    emitStep(ctx, step);
                }
            }
            return;
        }

        if (clickTarget instanceof JRadioButton) {
            if (id == MouseEvent.MOUSE_RELEASED && button == MouseEvent.BUTTON1) {
                JRadioButton radio = (JRadioButton) clickTarget;
                String text = resolveSelectedRadioText(radio);
                Map<String, Object> step = MarsKeyword.buildScriptStep("SetRadioBox", radio, "", text, "");
                step.put("event", "clickButton");
                step.put("timestamp", now);
                RecordAgent.putComponentInfo(step, radio);
                step.put("content", text);
                emitStep(ctx, step);
            }
            return;
        }

        if (clickTarget instanceof JCheckBox) {
            if (id == MouseEvent.MOUSE_RELEASED && button == MouseEvent.BUTTON1) {
                JCheckBox checkBox = (JCheckBox) clickTarget;
                String checked = String.valueOf(checkBox.isSelected());
                Map<String, Object> step = MarsKeyword.buildScriptStep("SetCheckBox", checkBox, "", checked, "");
                step.put("event", "clickButton");
                step.put("timestamp", now);
                RecordAgent.putComponentInfo(step, checkBox);
                step.put("content", checked);
                emitStep(ctx, step);
            }
            return;
        }

        if (!RecordAgent.shouldRecordClickTarget(clickTarget)) return;

        if (id == MouseEvent.MOUSE_PRESSED && button != MouseEvent.NOBUTTON) {
            ctx.lastPressedTimeRef[0] = now;
            ctx.lastPressedComponentRef[0] = clickTarget;
            ctx.lastPressedXRef[0] = x;
            ctx.lastPressedYRef[0] = y;
            ctx.lastPressedScreenXRef[0] = screenX;
            ctx.lastPressedScreenYRef[0] = screenY;
            ctx.lastPressedButtonRef[0] = button;
            return;
        }
        if (id != MouseEvent.MOUSE_RELEASED || button == MouseEvent.NOBUTTON) return;

        long pressTime = ctx.lastPressedTimeRef[0];
        Component pressComp = ctx.lastPressedComponentRef[0];
        long dt = now - pressTime;
        boolean sameComponent = (pressComp == clickTarget);
        boolean sameScreenPos = (Math.abs(screenX - ctx.lastPressedScreenXRef[0]) <= ctx.SCREEN_POS_TOLERANCE
                && Math.abs(screenY - ctx.lastPressedScreenYRef[0]) <= ctx.SCREEN_POS_TOLERANCE);
        if (pressTime == 0L || pressComp == null) return;
        if (!sameComponent && !sameScreenPos) return;
        if (dt < ctx.PRESS_RELEASE_MIN_MS || dt > ctx.PRESS_RELEASE_MAX_MS) return;
        ctx.lastPressedTimeRef[0] = 0L;

        boolean sameButton = (button == ctx.lastPressedButtonRef[0]);
        boolean withinDblClickDistance = (Math.abs(screenX - ctx.lastPressedScreenXRef[0]) <= ctx.CLICK_MERGE_DISTANCE_PX
                && Math.abs(screenY - ctx.lastPressedScreenYRef[0]) <= ctx.CLICK_MERGE_DISTANCE_PX);
        boolean secondReleaseWithinDblClickMs = ctx.pendingClickComponentRef[0] != null && ctx.pendingClickComponentRef[0] == pressComp
                && sameButton && withinDblClickDistance && (now - ctx.pendingClickReleaseTimeRef[0]) <= ctx.DBLCLICK_MS;

        if (secondReleaseWithinDblClickMs) {
            javax.swing.Timer t = ctx.pendingClickTimerRef.getAndSet(null);
            if (t != null) t.stop();
            ctx.pendingClickComponentRef[0] = null;

            int clickCount = 2;
            String param = "button=" + ctx.pendingClickButtonRef[0] + ",clickCount=" + clickCount;
            String keyword = "ClickButton";
            String data = "";
            if (isRadioButtonLike(pressComp)) {
                keyword = "SetRadioBox";
                data = resolveSelectedRadioText(pressComp);
            } else if (isCheckBoxLike(pressComp)) {
                keyword = "SetCheckBox";
                data = String.valueOf(isButtonSelected(pressComp));
            }
            Map<String, Object> step = MarsKeyword.buildScriptStep(keyword, pressComp, param, data, "");
            step.put("event", "clickButton");
            step.put("timestamp", now);
            RecordAgent.putComponentInfo(step, pressComp);
            Map<String, Object> meta = new LinkedHashMap<>();
            meta.put("clickCount", clickCount);
            meta.put("button", ctx.pendingClickButtonRef[0]);
            meta.put("x", ctx.pendingClickXRef[0]);
            meta.put("y", ctx.pendingClickYRef[0]);
            step.put("meta", meta);
            emitStep(ctx, step);
            if (ctx.pendingClickButtonRef[0] == MouseEvent.BUTTON3) {
                emitRightClickAtStep(ctx, now);
            }
            ctx.lastReleasedTimeRef[0] = now;
            ctx.lastReleasedComponentRef[0] = pressComp;
            return;
        }

        ctx.pendingClickComponentRef[0] = pressComp;
        ctx.pendingClickButtonRef[0] = button;
        ctx.pendingClickXRef[0] = ctx.lastPressedXRef[0];
        ctx.pendingClickYRef[0] = ctx.lastPressedYRef[0];
        ctx.pendingClickReleaseTimeRef[0] = now;
        ctx.lastReleasedTimeRef[0] = now;
        ctx.lastReleasedComponentRef[0] = pressComp;

        javax.swing.Timer oldTimer = ctx.pendingClickTimerRef.getAndSet(null);
        if (oldTimer != null) oldTimer.stop();
        final Component emitComp = pressComp;
        final int emitButton = button;
        final int emitX = ctx.lastPressedXRef[0];
        final int emitY = ctx.lastPressedYRef[0];
        final Map<String, Object> emitParentIdentifier = MarsKeyword.buildParentIdentifier(pressComp);

        javax.swing.Timer timer = new javax.swing.Timer(ctx.PENDING_CLICK_DELAY_MS, ev -> {
            ctx.pendingClickTimerRef.set(null);
            if (ctx.pendingClickComponentRef[0] != emitComp) return;
            ctx.pendingClickComponentRef[0] = null;
            int clickCount = 1;
            String param = "button=" + emitButton + ",clickCount=" + clickCount;
            String keyword = "ClickButton";
            String data = "";
            if (isRadioButtonLike(emitComp)) {
                keyword = "SetRadioBox";
                data = resolveSelectedRadioText(emitComp);
            } else if (isCheckBoxLike(emitComp)) {
                keyword = "SetCheckBox";
                data = String.valueOf(isButtonSelected(emitComp));
            }
            Map<String, Object> step = MarsKeyword.buildScriptStep(keyword, emitComp, param, data, "");
            if (emitParentIdentifier != null && !emitParentIdentifier.isEmpty()) {
                step.put("parentIdentifier", emitParentIdentifier);
            }
            step.put("event", "clickButton");
            step.put("timestamp", System.currentTimeMillis());
            RecordAgent.putComponentInfo(step, emitComp);
            Map<String, Object> meta = new LinkedHashMap<>();
            meta.put("clickCount", clickCount);
            meta.put("button", emitButton);
            meta.put("x", emitX);
            meta.put("y", emitY);
            step.put("meta", meta);
            try {
                Writer w = ctx.writerRef.get();
                if (w != null) RecordAgent.writeLine(w, step);
            } catch (IOException e) {
                AgentLogger.logException(LOG, Level.WARNING, "writeLine failed", e);
            }
            WebSocket c = ctx.clientConn.get();
            if (c != null && c.isOpen()) c.send(RecordAgent.toJson(step));
            if (emitButton == MouseEvent.BUTTON3) {
                emitRightClickAtStep(ctx, System.currentTimeMillis());
            }
        });
        timer.setRepeats(false);
        timer.start();
        ctx.pendingClickTimerRef.set(timer);
    }

    private static void handleTableMouseEvent(MouseEvent me, RecordingContext ctx, JTable table) {
        int id = me.getID();
        if (id != MouseEvent.MOUSE_PRESSED && id != MouseEvent.MOUSE_RELEASED) return;
        if (EventFilterConfig.shouldSkipMouseKeyboard(table)) return;
        if (!table.isShowing() || !table.isEnabled()) return;

        long now = System.currentTimeMillis();
        int button = me.getButton();
        Point p = SwingUtilities.convertPoint(me.getComponent(), me.getPoint(), table);
        int row = table.rowAtPoint(p);
        int col = table.columnAtPoint(p);

        boolean headerActivated = (row < 0 && col >= 0);
        boolean cellActivated = (row >= 0 && col >= 0);
        if (!headerActivated && !cellActivated) return;

        if (headerActivated) {
            ctx.lastTableInteractionTimeRef[0] = now;
            return;
        }

        if (id == MouseEvent.MOUSE_PRESSED) {
            try {
                Rectangle rect = table.getCellRect(row, col, true);
                Point loc = table.getLocationOnScreen();
                int sx = loc != null ? loc.x + rect.x : 0;
                int sy = loc != null ? loc.y + rect.y : 0;
                String parentType = table.getParent() != null ? table.getParent().getClass().getName() : "";
                String msg = "ActivateTableCell time=" + now + ", cellType=TableCell, parentType=" + parentType
                        + ", row=" + row + ", col=" + col
                        + ", x=" + sx + ", y=" + sy + ", w=" + rect.width + ", h=" + rect.height;
                AgentLogger.info(LOG, msg);
                RecordAgent.appendDebugLog(ctx.outputDir, msg);
            } catch (Exception e) {
                AgentLogger.logException(LOG, Level.FINE, "table cell bounds", e);
            }
        }

        ctx.lastTableInteractionTimeRef[0] = now;

        if (button == MouseEvent.BUTTON3 && id == MouseEvent.MOUSE_RELEASED) {
            String targetColumn = RecordAgent.getTableColumnName(table, col);
            if (targetColumn == null || targetColumn.trim().isEmpty()) {
                targetColumn = String.valueOf(col);
            }
            String cellValue = RecordAgent.getTableCellValue(table, row, col);
            String[] condCols = RecordAgent.getTableConditionColumns(table);
            String[] condVals = RecordAgent.getTableConditionValues(table, row, condCols);
            String param = "RightClick;" + RecordAgent.buildTableParameter(targetColumn, condCols);
            String data = RecordAgent.buildTableDataWithConditions(condVals, cellValue);
            Map<String, Object> step = MarsKeyword.buildScriptStep("SearchAndClick", table, param, data, "");
            step.put("event", "searchAndClick");
            step.put("timestamp", now);
            RecordAgent.putComponentInfo(step, table);
            RecordAgent.putTableCellBounds(step, table, row, col);
            step.put("content", data);
            emitStep(ctx, step);
            emitRightClickAtStep(ctx, now);
            String emitMsg = "StepEmit keyword=SearchAndClick, event=searchAndClick, trigger=mouse-right, row=" + row + ", col=" + col;
            AgentLogger.info(LOG, emitMsg);
            RecordAgent.appendDebugLog(ctx.outputDir, emitMsg);

            ctx.lastTableRightClickRef[0] = table;
            ctx.lastTableRightClickRowRef[0] = row;
            ctx.lastTableRightClickColRef[0] = col;
            ctx.lastTableRightClickColumnNameRef[0] = targetColumn;
            ctx.lastTableRightClickCellValueRef[0] = cellValue;
            ctx.lastTableRightClickConditionColumnsRef[0] = condCols;
            ctx.lastTableRightClickConditionValuesRef[0] = condVals;
            ctx.lastTableRightClickTimeRef[0] = now;
            return;
        }

        if (button == MouseEvent.BUTTON1 && id == MouseEvent.MOUSE_PRESSED) {
            boolean changedCell = ctx.currentTableRef[0] != table
                    || ctx.currentTableRowRef[0] != row
                    || ctx.currentTableColRef[0] != col;
            if (!changedCell) return;
            RecordAgent.ensureCurrentTableCell(ctx, table, row, col, now, changedCell);
        }
    }

    /**
     * Menu handling: only emit SelectMenuItem when the last-level (leaf) item is clicked.
     * Data = path from top-level menu to leaf, ";". When clicking a Menu (submenu), create step and cache;
     * when clicking a leaf MenuItem, update cached step object/data and emit then clear cache.
     */
    private static void handleMenuItemClick(RecordingContext ctx, JMenuItem mi, long now, int button) {
        String data = RecordAgent.buildMenuPathFromRootToLeaf(mi);
        if (RecordAgent.isMenuWithSubmenu(mi)) {
            Map<String, Object> step = MarsKeyword.buildScriptStep("SelectMenuItem", mi, "", data, "");
            step.put("event", "selectMenuItem");
            step.put("timestamp", now);
            RecordAgent.putComponentInfo(step, mi);
            step.put("content", data);
            appendRightClickParameter(step, button);
            ctx.pendingSelectMenuItemStepRef[0] = step;
            return;
        }
        Map<String, Object> step = ctx.pendingSelectMenuItemStepRef[0];
        if (step != null && "SelectMenuItem".equals(step.get("keyword"))) {
            step.put("objectIdentifier", MarsKeyword.buildObjectIdentifier(mi));
            step.put("data", data);
            step.put("content", data);
            step.put("timestamp", now);
            RecordAgent.putComponentInfo(step, mi);
            appendRightClickParameter(step, button);
            emitStep(ctx, step);
            ctx.pendingSelectMenuItemStepRef[0] = null;
        } else {
            step = MarsKeyword.buildScriptStep("SelectMenuItem", mi, "", data, "");
            step.put("event", "selectMenuItem");
            step.put("timestamp", now);
            RecordAgent.putComponentInfo(step, mi);
            step.put("content", data);
            appendRightClickParameter(step, button);
            emitStep(ctx, step);
        }
        if (button == MouseEvent.BUTTON3) {
            emitRightClickAtStep(ctx, now);
        }
    }

    private static boolean isPopupMenuItem(JMenuItem item) {
        if (item == null) return false;
        for (Component p = item; p != null; p = p.getParent()) {
            if (p instanceof JPopupMenu) return true;
            if (p instanceof JMenu) return false;
        }
        return false;
    }

    private static boolean isRecentTableRightClick(RecordingContext ctx, long now) {
        if (ctx == null || ctx.lastTableRightClickRef[0] == null) return false;
        long last = ctx.lastTableRightClickTimeRef[0];
        return last > 0 && (now - last) <= 5000;
    }

    private static void emitStep(RecordingContext ctx, Map<String, Object> step) {
        try {
            Writer w = ctx.writerRef.get();
            if (w != null) RecordAgent.writeLine(w, step);
        } catch (IOException e) {
            AgentLogger.logException(LOG, Level.WARNING, "writeLine failed", e);
        }
        WebSocket c = ctx.clientConn.get();
        if (c != null && c.isOpen()) c.send(RecordAgent.toJson(step));
    }

    private static void emitRightClickAtStep(RecordingContext ctx, long timestamp) {
        Map<String, Object> clickAt = new LinkedHashMap<>();
        clickAt.put("keyword", "ClickAT");
        clickAt.put("parameter", "CURRENT_POSITION");
        clickAt.put("data", "Rightclick");
        clickAt.put("parentIdentifier", new LinkedHashMap<>());
        clickAt.put("objectIdentifier", new LinkedHashMap<>());
        clickAt.put("event", "clickAt");
        clickAt.put("timestamp", timestamp);
        emitStep(ctx, clickAt);
    }

    private static void appendRightClickParameter(Map<String, Object> step, int button) {
        if (button != MouseEvent.BUTTON3 || step == null) return;
        Object keywordObj = step.get("keyword");
        String keyword = keywordObj != null ? String.valueOf(keywordObj) : "";
        if (keyword.isEmpty()) return;
        boolean isSelectKeyword = "SelectMenuItem".equals(keyword)
            || "SelectPopupMenu".equals(keyword)
            || "SelectTreeList".equals(keyword)
            || "SelectTab".equals(keyword)
            || "SelectDropList".equals(keyword)
            || "SelectDropDown".equals(keyword)
            || "SelectListItem".equals(keyword);
        if (!isSelectKeyword) return;
        String para = step.get("parameter") != null ? String.valueOf(step.get("parameter")) : "";
        if (containsParameterTokenIgnoreCase(para, "rightclick")) return;
        if (para.trim().isEmpty()) {
            step.put("parameter", "rightclick");
        } else {
            step.put("parameter", para + ";rightclick");
        }
    }

    private static boolean containsParameterTokenIgnoreCase(String parameter, String token) {
        if (parameter == null || token == null) return false;
        String[] parts = parameter.split("[;,]");
        for (String part : parts) {
            if (part != null && part.trim().equalsIgnoreCase(token)) {
                return true;
            }
        }
        return false;
    }

    private static boolean isRadioButtonLike(Component c) {
        if (c == null) return false;
        if (c instanceof JRadioButton) return true;
        String type = c.getClass().getName();
        if (type != null && type.toLowerCase().contains("radiobutton")) return true;
        String role = getAccessibleRoleName(c);
        return role.contains("radio");
    }

    private static boolean isCheckBoxLike(Component c) {
        if (c == null) return false;
        if (c instanceof JCheckBox) return true;
        String type = c.getClass().getName();
        if (type != null && type.toLowerCase().contains("checkbox")) return true;
        String role = getAccessibleRoleName(c);
        return role.contains("check");
    }

    private static String getAccessibleRoleName(Component c) {
        try {
            AccessibleContext ac = c.getAccessibleContext();
            if (ac == null) return "";
            AccessibleRole role = ac.getAccessibleRole();
            return role != null ? role.toDisplayString().toLowerCase() : "";
        } catch (Exception ignored) {
            return "";
        }
    }

    private static String extractButtonText(Component c) {
        if (c instanceof AbstractButton) {
            String text = ((AbstractButton) c).getText();
            if (text != null && !text.isEmpty()) return text;
        }
        return RecordAgent.getComponentNameForLog(c);
    }

    private static String resolveSelectedRadioText(Component c) {
        if (!(c instanceof AbstractButton)) return extractButtonText(c);
        AbstractButton btn = (AbstractButton) c;
        Container parent = btn.getParent();
        if (parent != null) {
            for (Component child : parent.getComponents()) {
                if (child instanceof AbstractButton && isRadioButtonLike(child) && ((AbstractButton) child).isSelected()) {
                    String t = extractButtonText(child);
                    if (t != null && !t.isEmpty()) return t;
                }
            }
        }
        if (btn.isSelected()) {
            String t = extractButtonText(btn);
            if (t != null && !t.isEmpty()) return t;
        }
        return extractButtonText(c);
    }

    private static boolean isButtonSelected(Component c) {
        if (c instanceof AbstractButton) {
            return ((AbstractButton) c).isSelected();
        }
        return false;
    }
}
