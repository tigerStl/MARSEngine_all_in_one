package com.mars.javaui.record.eventoperation;

import java.awt.event.ItemEvent;
import java.io.Writer;
import java.util.Map;
import java.util.logging.Level;
import java.util.logging.Logger;

import javax.swing.JComboBox;

import org.java_websocket.WebSocket;

import com.mars.javaui.keyword.KeywordConstants;
import com.mars.javaui.keyword.MarsKeyword;
import com.mars.javaui.record.AgentLogger;
import com.mars.javaui.record.RecordAgent;
import com.mars.javaui.record.config.EventFilterConfig;

/** Handles item events for ComboBox selection. */
public final class ItemEventHandler {

    private static final Logger LOG = Logger.getLogger(ItemEventHandler.class.getName());

    public static void handle(ItemEvent ie, RecordingContext ctx) {
        if (ie.getStateChange() != ItemEvent.SELECTED) return;
        Object src = ie.getSource();
        if (!(src instanceof JComboBox)) return;
        JComboBox<?> cb = (JComboBox<?>) src;
        if (EventFilterConfig.shouldSkipMouseKeyboard(cb)) return;
        if (!cb.isShowing() || !cb.isEnabled()) return;

        if (ctx.currentComboBoxRef[0] == null) {
            ctx.currentComboBoxRef[0] = cb;
        }
        if (ctx.currentComboInitialRef[0] == null) {
            ctx.currentComboInitialRef[0] = RecordAgent.getComboSelectedText(cb);
        }

        ctx.currentComboSelectedRef[0] = RecordAgent.getComboSelectedText(cb);
        ctx.currentComboInteractedRef[0] = true;
        AgentLogger.info(LOG, "ComboItemSelected name=" + RecordAgent.getComponentNameForLog(cb)
            + ", selected=" + ctx.currentComboSelectedRef[0]
            + ", popupVisible=" + cb.isPopupVisible());

        if (cb.isPopupVisible()) return;
        if (ctx.currentComboEmittedRef[0]) return;

        String data = ctx.currentComboSelectedRef[0] != null ? ctx.currentComboSelectedRef[0] : RecordAgent.getComboSelectedText(cb);
        Map<String, Object> step = MarsKeyword.buildScriptStep(KeywordConstants.SELECT_DROP_LIST, cb, "", data, "");
        step.put("event", "selectDropList");
        step.put("timestamp", System.currentTimeMillis());
        RecordAgent.putComponentInfo(step, cb);
        step.put("content", data);
        emitStep(ctx, step);
        ctx.currentComboEmittedRef[0] = true;
    }

    private static void emitStep(RecordingContext ctx, Map<String, Object> step) {
        try {
            Writer w = ctx.writerRef.get();
            if (w != null) RecordAgent.writeLine(w, step);
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "writeLine failed", e);
        }
        WebSocket c = ctx.clientConn.get();
        if (c != null && c.isOpen()) c.send(RecordAgent.toJson(step));
    }
}
