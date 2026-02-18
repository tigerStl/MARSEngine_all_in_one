package com.mars.javaui.record.eventoperation;

import java.awt.Component;
import java.awt.KeyboardFocusManager;
import java.awt.event.KeyEvent;
import java.io.Writer;
import java.util.Map;

import javax.swing.JComboBox;

import com.mars.javaui.record.RecordAgent;
import com.mars.javaui.record.config.EventFilterConfig;
import com.mars.javaui.record.keyword.MarsKeyword;

import org.java_websocket.WebSocket;

/**
 * Handles key events for recording.
 * For text fields: do NOT create a test step on each key. Only create one FillEdit step when
 * the user presses Enter/Tab (here) or when the control loses focus (RecordAgent FOCUS_LOST).
 * KEY_TYPED and normal KEY_PRESSED only update session state; no step is sent.
 */
public final class KeyboardEventHandler {

    public static void handle(KeyEvent ke, RecordingContext ctx) {
        Component keyTarget = KeyboardFocusManager.getCurrentKeyboardFocusManager().getFocusOwner();
        if (keyTarget == null) keyTarget = ke.getComponent();
        if (keyTarget != null && EventFilterConfig.shouldSkipMouseKeyboard(keyTarget)) return;

        int id = ke.getID();
        long now = System.currentTimeMillis();
        int keyCode = ke.getKeyCode();
        int modifiersEx = ke.getModifiersEx();
        Object source = ke.getSource();

        if (id == ctx.lastKeyDedupIdRef[0] && keyCode == ctx.lastKeyDedupCodeRef[0]
                && modifiersEx == ctx.lastKeyDedupModifiersRef[0] && source == ctx.lastKeyDedupSourceRef[0]
                && (now - ctx.lastKeyDedupWhenRef[0]) < ctx.KEY_DEDUP_MS) {
            return;
        }
        ctx.lastKeyDedupWhenRef[0] = now;
        ctx.lastKeyDedupIdRef[0] = id;
        ctx.lastKeyDedupCodeRef[0] = keyCode;
        ctx.lastKeyDedupModifiersRef[0] = modifiersEx;
        ctx.lastKeyDedupSourceRef[0] = source;

        if (id == KeyEvent.KEY_PRESSED) {
            if (ctx.pressedKeyCodes.contains(keyCode)) return;
            ctx.pressedKeyCodes.add(keyCode);

            if (keyCode == KeyEvent.VK_ENTER && ctx.currentComboBoxRef[0] != null) {
                JComboBox<?> cb = ctx.currentComboBoxRef[0];
                if (ctx.currentComboInitialRef[0] == null) {
                    ctx.currentComboInitialRef[0] = RecordAgent.getComboSelectedText(cb);
                }
                ctx.currentComboSelectedRef[0] = RecordAgent.getComboSelectedText(cb);
                ctx.currentComboInteractedRef[0] = true;
                return;
            }
            if ((keyCode == KeyEvent.VK_UP || keyCode == KeyEvent.VK_DOWN) && ctx.currentComboBoxRef[0] != null) {
                JComboBox<?> cb = ctx.currentComboBoxRef[0];
                if (ctx.currentComboInitialRef[0] == null) {
                    ctx.currentComboInitialRef[0] = RecordAgent.getComboSelectedText(cb);
                }
                ctx.currentComboSelectedRef[0] = RecordAgent.getComboSelectedText(cb);
                ctx.currentComboInteractedRef[0] = true;
                return;
            }

            Component focusOwner = KeyboardFocusManager.getCurrentKeyboardFocusManager().getFocusOwner();
            Component comp = RecordAgent.resolveEditComponent(focusOwner);
            if (comp == null) comp = RecordAgent.resolveEditComponent(ke.getComponent());

            if (comp != null) {
                ctx.currentEditComponentRef[0] = comp;
                boolean isTab = (keyCode == KeyEvent.VK_TAB);
                boolean isEnter = (keyCode == KeyEvent.VK_ENTER);
                // Only emit FillEdit when user commits with Tab or Enter; never on normal key press
                if (isTab || isEnter) {
                    ctx.lastFillEditComponentRef[0] = comp;
                    ctx.lastFillEditTimeRef[0] = now;
                    String text = RecordAgent.getEditText(comp);
                    String data = RecordAgent.buildFillEditData(text);
                    Map<String, Object> step = MarsKeyword.buildScriptStep("FillEdit", comp, "", data, "");
                    step.put("event", "fillEdit");
                    step.put("timestamp", now);
                    RecordAgent.putComponentInfo(step, comp);
                    step.put("content", data);
                    emitStep(ctx, step);
                    ctx.typedBuffer.setLength(0);
                    ctx.lastTypedTimeRef[0] = 0L;
                    ctx.currentEditComponentRef[0] = null;
                    ctx.currentEditHadKeyRef[0] = false;
                    ctx.currentEditInitialTextRef[0] = null;
                } else {
                    // Normal key in text field: do not create step; only mark that we had input (for focus-lost step)
                    ctx.currentEditHadKeyRef[0] = true;
                }
            }
        } else if (id == KeyEvent.KEY_RELEASED) {
            ctx.pressedKeyCodes.remove(keyCode);
        } else if (id == KeyEvent.KEY_TYPED) {
            // Do not send any step on KEY_TYPED; only buffer for session (step created on blur/Enter/Tab)
            char keyChar = ke.getKeyChar();
            if (keyChar != KeyEvent.CHAR_UNDEFINED) {
                ctx.typedBuffer.append(keyChar);
                ctx.lastTypedTimeRef[0] = now;
            }
        }
    }

    private static void emitStep(RecordingContext ctx, Map<String, Object> step) {
        try {
            Writer w = ctx.writerRef.get();
            if (w != null) RecordAgent.writeLine(w, step);
        } catch (Exception ignored) { }
        WebSocket c = ctx.clientConn.get();
        if (c != null && c.isOpen()) c.send(RecordAgent.toJson(step));
    }
}
