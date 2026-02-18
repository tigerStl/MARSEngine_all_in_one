package com.mars.javaui.record.eventoperation;

import java.awt.Component;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.io.IOException;
import java.io.Writer;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.logging.Level;
import javax.swing.JComboBox;
import javax.swing.JMenu;
import javax.swing.JMenuItem;
import javax.swing.JTree;
import org.java_websocket.WebSocket;

import com.mars.javaui.record.AgentLogger;
import com.mars.javaui.record.RecordAgent;
import com.mars.javaui.record.config.EventFilterConfig;
import com.mars.javaui.record.keyword.MarsKeyword;

/** Handles mouse events for recording: click/double-click merge, menu, tree, toolbar, ComboBox. */
public final class MouseEventHandler {

    private static final java.util.logging.Logger LOG = java.util.logging.Logger.getLogger(MouseEventHandler.class.getName());

    public static void handle(MouseEvent me, RecordingContext ctx) {
        int id = me.getID();
        if (id == MouseEvent.MOUSE_CLICKED) return;

        Component clickTarget = RecordAgent.resolveClickTarget(me.getComponent());
        if (clickTarget == null) return;
        if (EventFilterConfig.shouldSkipMouseKeyboard(clickTarget)) return;
        if (!clickTarget.isShowing() || !clickTarget.isEnabled()) return;

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
        String eventKind = (id == MouseEvent.MOUSE_PRESSED) ? "Pressed" : (id == MouseEvent.MOUSE_RELEASED) ? "Released" : "Other";
        if (id == MouseEvent.MOUSE_PRESSED || id == MouseEvent.MOUSE_RELEASED) {
            String objectName = RecordAgent.getComponentNameForLog(clickTarget);
            AgentLogger.info(LOG, "ClickEvent time=" + now + ", name=" + objectName + ", type=" + objectType
                    + ", phase=" + eventKind + ", x=" + x + ", y=" + y + ", screenX=" + screenX + ", screenY=" + screenY);
        }
        RecordAgent.appendDebugLog(ctx.outputDir, "MouseEvent_[" + eventKind + "], " + now + ", " + objectType
                + ", x=" + x + ",y=" + y + ", screenX=" + screenX + ",screenY=" + screenY
                + ", rect=" + rect.x + "," + rect.y + "," + rect.width + "," + rect.height);

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

        if (clickTarget instanceof JMenuItem && !(clickTarget instanceof JMenu)) {
            if (id == MouseEvent.MOUSE_RELEASED) {
                JMenuItem mi = (JMenuItem) clickTarget;
                String data = RecordAgent.buildMenuPathString(mi);
                Map<String, Object> step = MarsKeyword.buildScriptStep("SelectMenuItem", mi, "", data, "");
                step.put("event", "selectMenuItem");
                step.put("timestamp", now);
                RecordAgent.putComponentInfo(step, mi);
                step.put("content", data);
                emitStep(ctx, step);
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
            Map<String, Object> step = MarsKeyword.buildScriptStep("ClickButton", pressComp, param, "", "");
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

        javax.swing.Timer timer = new javax.swing.Timer(ctx.PENDING_CLICK_DELAY_MS, ev -> {
            ctx.pendingClickTimerRef.set(null);
            if (ctx.pendingClickComponentRef[0] != emitComp) return;
            ctx.pendingClickComponentRef[0] = null;
            int clickCount = 1;
            String param = "button=" + emitButton + ",clickCount=" + clickCount;
            Map<String, Object> step = MarsKeyword.buildScriptStep("ClickButton", emitComp, param, "", "");
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
        });
        timer.setRepeats(false);
        timer.start();
        ctx.pendingClickTimerRef.set(timer);
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
}
