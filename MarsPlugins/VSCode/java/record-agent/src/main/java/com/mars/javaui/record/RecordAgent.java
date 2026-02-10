package com.mars.javaui.record;

import java.beans.BeanInfo;
import java.beans.IntrospectionException;
import java.beans.Introspector;
import java.beans.PropertyDescriptor;
import java.awt.*;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.StringSelection;
import java.lang.reflect.Method;
import java.awt.event.AWTEventListener;
import java.awt.event.FocusEvent;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.awt.event.MouseEvent;
import java.io.*;
import java.lang.instrument.Instrumentation;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.text.SimpleDateFormat;
import java.util.*;
import java.util.concurrent.atomic.AtomicReference;
import java.util.logging.Level;
import java.util.logging.Logger;
import javax.swing.*;
import javax.swing.text.JTextComponent;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import org.java_websocket.WebSocket;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;

/**
 * Record agent: runs as WebSocket server inside target JVM.
 * Writes port to recordDir/marsJavaAgentInfo.json. Extension connects as client,
 * sends handshake (confirm pid), then "startRecordAndReplay"; agent records and sends events to extension.
 */
public class RecordAgent {

    private static final Logger LOG = Logger.getLogger(RecordAgent.class.getName());
    private static final long POLL_INTERVAL_MS = 500;
    private static final String RECORD_FILE = "record.jsonl";
    private static final String STOP_FILE = "record-stop.txt";
    private static final String INFO_FILE = "marsJavaAgentInfo.json";
    private static final String TOOLBUTTON_LOG_FILE = "toolbutton-tooltips.log";
    private static final String RECORD_DEBUG_LOG = "record-debug.log";

    public static void agentmain(String agentArgs, Instrumentation inst) {
        LOG.info("agentmain called, agentArgs=" + agentArgs);
        if (agentArgs == null || agentArgs.isEmpty()) {
            LOG.warning("No agentArgs provided");
            return;
        }
        String recordDirStr = agentArgs.trim();
        int pid = -1;
        if (recordDirStr.contains("|")) {
            String[] parts = recordDirStr.split("\\|", 2);
            recordDirStr = parts[0].trim();
            try {
                pid = Integer.parseInt(parts[1].trim());
            } catch (NumberFormatException e) {
                LOG.warning("Invalid pid in agentArgs: " + parts[1]);
            }
        }
        File outputDir = new File(recordDirStr);
        if (!outputDir.isDirectory()) {
            if (!outputDir.mkdirs()) {
                LOG.warning("Could not create directory: " + outputDir);
                return;
            }
        }
        if (GraphicsEnvironment.isHeadless()) {
            LOG.info("Headless environment, skipping record");
            return;
        }
        final int pidForRun = pid;
        try {
            EventQueue.invokeAndWait(() -> run(outputDir, pidForRun));
        } catch (Exception e) {
            LOG.log(Level.SEVERE, "Record agent run failed", e);
        }
    }

    private static void run(File outputDir, int expectedPid) {
        File recordFile = new File(outputDir, RECORD_FILE);
        File stopFile = new File(outputDir, STOP_FILE);
        File infoFile = new File(outputDir, INFO_FILE);
        if (stopFile.exists()) {
            try { Files.delete(stopFile.toPath()); } catch (IOException ignored) {}
        }

        final AtomicReference<OutputStreamWriter> writerRef = new AtomicReference<>();
        try {
            writerRef.set(new OutputStreamWriter(new FileOutputStream(recordFile, false), StandardCharsets.UTF_8));
        } catch (IOException e) {
            LOG.log(Level.SEVERE, "Record file open failed", e);
            return;
        }

        final boolean[] recording = { false };
        final boolean[] running = { true };
        final AtomicReference<WebSocket> clientConn = new AtomicReference<>();
        final AtomicReference<AWTEventListener> listenerRef = new AtomicReference<>();
        final Component[] lastRecordedComponentRef = new Component[1];
        final long[] lastRecordedTimeRef = new long[1];
        final int[] lastRecordedXRef = new int[1];
        final int[] lastRecordedYRef = new int[1];
        // Track current editing component and whether it saw any key events
        final Component[] currentEditComponentRef = new Component[1];
        final boolean[] currentEditHadKeyRef = new boolean[1];
        // Track current menu component and accumulated key sequence
        final Component[] currentMenuComponentRef = new Component[1];
        final StringBuilder[] currentMenuKeysRef = new StringBuilder[1];
        final int DEDUPE_MS = 200;
        final int DEDUPE_POS_TOLERANCE = 8;
        // Press→Release click: valid window 50–400ms; double-click: second release within 300ms of first; match by screen position so Cancel etc. work
        final long PRESS_RELEASE_MIN_MS = 50;
        final long PRESS_RELEASE_MAX_MS = 400;
        final long DOUBLE_CLICK_RELEASE_INTERVAL_MS = 300;
        final int SCREEN_POS_TOLERANCE = 15;
        final long[] lastPressedTimeRef = new long[1];
        final Component[] lastPressedComponentRef = new Component[1];
        final int[] lastPressedXRef = new int[1];
        final int[] lastPressedYRef = new int[1];
        final int[] lastPressedScreenXRef = new int[1];
        final int[] lastPressedScreenYRef = new int[1];
        final int[] lastPressedButtonRef = new int[1];
        final long[] lastReleasedTimeRef = new long[1];
        final Component[] lastReleasedComponentRef = new Component[1];
        lastPressedTimeRef[0] = 0L;
        lastReleasedTimeRef[0] = 0L;

        // Key action-level recording: session start, dedup, auto-repeat, typed merge, chord
        final long[] sessionStartTimeRef = new long[1];
        final long[] lastKeyDedupWhenRef = new long[1];
        final int[] lastKeyDedupIdRef = new int[1];
        final int[] lastKeyDedupCodeRef = new int[1];
        final int[] lastKeyDedupModifiersRef = new int[1];
        final Object[] lastKeyDedupSourceRef = new Object[1];
        final Set<Integer> pressedKeyCodes = new HashSet<>();
        final StringBuilder typedBuffer = new StringBuilder();
        final long[] lastTypedTimeRef = new long[1];
        final long KEY_DEDUP_MS = 10;
        final long TYPED_MERGE_WINDOW_MS = 300;

        WebSocketServer server = new WebSocketServer(new InetSocketAddress("127.0.0.1", 0)) {
            @Override
            public void onOpen(WebSocket conn, ClientHandshake handshake) {
                LOG.info("Extension connected");
                clientConn.set(conn);
            }

            @Override
            public void onClose(WebSocket conn, int code, String reason, boolean remote) {
                if (clientConn.get() == conn) {
                    clientConn.set(null);
                    recording[0] = false;
                    AWTEventListener L = listenerRef.getAndSet(null);
                    if (L != null) {
                        EventQueue.invokeLater(() -> Toolkit.getDefaultToolkit().removeAWTEventListener(L));
                    }
                }
            }

            @Override
            public void onMessage(WebSocket conn, String message) {
                try {
                    String type = getJsonStringValue(message, "type");
                    if ("handshake".equals(type)) {
                        Map<String, Object> ack = new LinkedHashMap<>();
                        ack.put("type", "handshake_ack");
                        ack.put("pid", expectedPid);
                        conn.send(toJson(ack));
                    } else if ("startRecordAndReplay".equals(type)) {
                        recording[0] = true;
                        sessionStartTimeRef[0] = System.currentTimeMillis();
                        pressedKeyCodes.clear();
                        typedBuffer.setLength(0);
                        AWTEventListener listener = event -> {
                            if (!recording[0]) return;
                            try {
                                if (event instanceof MouseEvent) {
                                    MouseEvent me = (MouseEvent) event;
                                    int id = me.getID();
                                    Component clickTarget = resolveClickTarget(me.getComponent());
                                    long now = System.currentTimeMillis();
                                    int x = me.getX();
                                    int y = me.getY();
                                    int button = me.getButton();
                                    Rectangle rect = clickTarget != null ? clickTarget.getBounds() : new Rectangle(0, 0, 0, 0);
                                    String objectType = clickTarget != null ? clickTarget.getClass().getName() : "null";
                                    int screenX = x, screenY = y;
                                    try {
                                        Point onScreen = me.getLocationOnScreen();
                                        if (onScreen != null) {
                                            screenX = onScreen.x + x;
                                            screenY = onScreen.y + y;
                                        }
                                    } catch (Exception ignored) {}
                                    // Debug log: position (x,y), object type, rectangle as required
                                    String eventKind = (id == MouseEvent.MOUSE_CLICKED) ? "Click" : ((id == MouseEvent.MOUSE_PRESSED) ? "Pressed" : ((id == MouseEvent.MOUSE_RELEASED) ? "Released" : "Other"));
                                    appendDebugLog(outputDir, "MouseEvent_[" + eventKind + "], " + now + ", " + objectType + ", x=" + x + ",y=" + y + ", screenX=" + screenX + ",screenY=" + screenY + ", rect=" + rect.x + "," + rect.y + "," + rect.width + "," + rect.height);
                                    // Use PRESSED as click start; synthesize one Click on RELEASED in 50–400ms window; match by same component or same screen position (Cancel etc.)
                                    if (id == MouseEvent.MOUSE_PRESSED && button != MouseEvent.NOBUTTON) {
                                        lastPressedTimeRef[0] = now;
                                        lastPressedComponentRef[0] = clickTarget;
                                        lastPressedXRef[0] = x;
                                        lastPressedYRef[0] = y;
                                        lastPressedScreenXRef[0] = screenX;
                                        lastPressedScreenYRef[0] = screenY;
                                        lastPressedButtonRef[0] = button;
                                        return;
                                    }
                                    if (id == MouseEvent.MOUSE_CLICKED) {
                                        return;
                                    }
                                    if (id != MouseEvent.MOUSE_RELEASED || button == MouseEvent.NOBUTTON) return;
                                    long pressTime = lastPressedTimeRef[0];
                                    Component pressComp = lastPressedComponentRef[0];
                                    long dt = now - pressTime;
                                    boolean sameComponent = (pressComp == clickTarget);
                                    boolean sameScreenPos = (Math.abs(screenX - lastPressedScreenXRef[0]) <= SCREEN_POS_TOLERANCE && Math.abs(screenY - lastPressedScreenYRef[0]) <= SCREEN_POS_TOLERANCE);
                                    if (pressTime == 0L || pressComp == null) return;
                                    if (!sameComponent && !sameScreenPos) return;
                                    if (dt < PRESS_RELEASE_MIN_MS || dt > PRESS_RELEASE_MAX_MS) return;
                                    lastPressedTimeRef[0] = 0L;
                                    int clickCount = 1;
                                    if (lastReleasedTimeRef[0] > 0 && lastReleasedComponentRef[0] == pressComp && (now - lastReleasedTimeRef[0]) < DOUBLE_CLICK_RELEASE_INTERVAL_MS) {
                                        clickCount = 2;
                                    }
                                    lastReleasedTimeRef[0] = now;
                                    lastReleasedComponentRef[0] = pressComp;
                                    lastRecordedComponentRef[0] = pressComp;
                                    lastRecordedTimeRef[0] = now;
                                    lastRecordedXRef[0] = lastPressedXRef[0];
                                    lastRecordedYRef[0] = lastPressedYRef[0];
                                    Map<String, Object> ev = new LinkedHashMap<>();
                                    ev.put("event", "click");
                                    ev.put("timestamp", now);
                                    ev.put("x", lastPressedXRef[0]);
                                    ev.put("y", lastPressedYRef[0]);
                                    ev.put("button", button);
                                    ev.put("clickCount", clickCount);
                                    putComponentInfo(ev, pressComp);
                                    Writer w = writerRef.get();
                                    if (w != null) writeLine(w, ev);
                                    WebSocket c = clientConn.get();
                                    if (c != null && c.isOpen()) c.send(toJson(ev));
                                    if (isToolButtonLike(pressComp)) {
                                        String compClass = pressComp.getClass().getName();
                                        String toolTipText = getToolTipTextByReflection(pressComp);
                                        appendToToolButtonLog(outputDir, compClass, toolTipText);
                                        Map<String, Object> propsEv = new LinkedHashMap<>();
                                        propsEv.put("event", "componentProperties");
                                        propsEv.put("componentClass", compClass);
                                        propsEv.put("properties", getComponentPropertiesForLog(pressComp));
                                        if (c != null && c.isOpen()) c.send(toJson(propsEv));
                                    }
                                } else if (event instanceof FocusEvent) {
                                    FocusEvent fe = (FocusEvent) event;
                                    if (fe.getID() == FocusEvent.FOCUS_LOST) {
                                        Component comp = resolveEditComponent(fe.getComponent());
                                        // Do not record raw focusLost events; if it's an edit component, emit a FillEdit event once
                                        if (comp != null) {
                                            Map<String, Object> ev = new LinkedHashMap<>();
                                            ev.put("event", "fillEdit");
                                            ev.put("timestamp", System.currentTimeMillis());
                                            putComponentInfo(ev, comp);
                                            putComponentContent(ev, comp);
                                            Writer w = writerRef.get();
                                            if (w != null) writeLine(w, ev);
                                            WebSocket c = clientConn.get();
                                            if (c != null && c.isOpen()) c.send(toJson(ev));
                                        }
                                        currentEditComponentRef[0] = null;
                                        currentEditHadKeyRef[0] = false;
                                    }
                                } else if (event instanceof KeyEvent) {
                                    KeyEvent ke = (KeyEvent) event;
                                    appendDebugLog(outputDir, "[" + new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS").format(new Date(System.currentTimeMillis())) + "]KeyEvent_ENTER-[id=" + ke.getID() + ",keyCode=" + ke.getKeyCode() + "]");
                                    int id = ke.getID();
                                    long now = System.currentTimeMillis();
                                    int keyCode = ke.getKeyCode();
                                    int modifiersEx = ke.getModifiersEx();
                                    Object source = ke.getSource();
                                    long sessionStart = sessionStartTimeRef[0];
                                    Map<String, Object> ctx = getRecordingContext(sessionStart);
                                    ctx.put("timestamp", now - sessionStart);

                                    // 1) Dedup: same (id, when, keyCode, modifiersEx, source) within 10ms
                                    if (id == lastKeyDedupIdRef[0] && keyCode == lastKeyDedupCodeRef[0]
                                            && modifiersEx == lastKeyDedupModifiersRef[0] && source == lastKeyDedupSourceRef[0]
                                            && (now - lastKeyDedupWhenRef[0]) < KEY_DEDUP_MS) {
                                        appendDebugLog(outputDir, "[" + new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS").format(new Date(now)) + "]KeyEvent_DEDUP_SKIP-[id=" + id + ",keyCode=" + keyCode + "]");
                                        return;
                                    }
                                    lastKeyDedupWhenRef[0] = now;
                                    lastKeyDedupIdRef[0] = id;
                                    lastKeyDedupCodeRef[0] = keyCode;
                                    lastKeyDedupModifiersRef[0] = modifiersEx;
                                    lastKeyDedupSourceRef[0] = source;

                                    Component focusOwner = KeyboardFocusManager.getCurrentKeyboardFocusManager().getFocusOwner();
                                    Component comp = resolveEditComponent(focusOwner);
                                    if (comp == null) comp = resolveEditComponent(ke.getComponent());
                                    Component menuComp = resolveMenuComponent(focusOwner);
                                    if (menuComp == null) menuComp = resolveMenuComponent(ke.getComponent());

                                    String keyEventName = (id == KeyEvent.KEY_PRESSED) ? "KEY_PRESSED" : (id == KeyEvent.KEY_RELEASED) ? "KEY_RELEASED" : "KEY_TYPED";
                                    Component logTarget = focusOwner != null ? focusOwner : (ke.getSource() instanceof Component ? (Component) ke.getSource() : null);
                                    String classTypePaths = buildClassTypePath(logTarget);
                                    String timeStr = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS").format(new Date(now));
                                    appendDebugLog(outputDir, "[" + timeStr + "][" + keyEventName + "]-[" + classTypePaths + "]");

                                    if (id == KeyEvent.KEY_PRESSED) {
                                        appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_BRANCH_KEY_PRESSED-[keyCode=" + keyCode + "]");
                                        // 2) Auto-repeat: only first pressed until released
                                        if (pressedKeyCodes.contains(keyCode)) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_AUTOREPEAT_SKIP-[keyCode=" + keyCode + "]");
                                            return;
                                        }
                                        pressedKeyCodes.add(keyCode);
                                        String keyStr = keyEventToString(ke);
                                        if (keyStr.isEmpty()) keyStr = "keyCode=" + keyCode;
                                        appendDebugLog(outputDir, "KeyEvent_[KeyDown], " + now + ", " + keyStr);
                                        currentEditComponentRef[0] = comp;
                                        if (comp != null) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_COMP_NOT_NULL-[comp=" + comp.getClass().getName() + "]");
                                            int code = ke.getKeyCode();
                                            boolean isTab = (code == KeyEvent.VK_TAB);
                                            boolean isEnter = (code == KeyEvent.VK_ENTER);
                                            if (isTab || isEnter) {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_FILLEDIT_TAB_OR_ENTER-[code=" + code + "]");
                                                Map<String, Object> ev = new LinkedHashMap<>();
                                                ev.put("event", "fillEdit");
                                                ev.put("timestamp", now);
                                                ev.put("deltaMillis", now - sessionStart);
                                                putComponentInfo(ev, comp);
                                                putComponentContent(ev, comp);
                                                Writer w = writerRef.get();
                                                if (w != null) writeLine(w, ev);
                                                WebSocket c = clientConn.get();
                                                if (c != null && c.isOpen()) c.send(toJson(ev));
                                                currentEditComponentRef[0] = null;
                                                currentEditHadKeyRef[0] = false;
                                            } else {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_EDIT_KEY_NO_TAB_ENTER-[code=" + code + "]");
                                                currentEditHadKeyRef[0] = true;
                                            }
                                        }
                                        if (menuComp != null) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_MENU_COMP_NOT_NULL");
                                            if (currentMenuComponentRef[0] == null) {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_MENU_START");
                                                currentMenuComponentRef[0] = menuComp;
                                                currentMenuKeysRef[0] = new StringBuilder();
                                            }
                                            int code = ke.getKeyCode();
                                            if (code == KeyEvent.VK_ENTER) {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_MENU_ENTER");
                                                StringBuilder sb = currentMenuKeysRef[0];
                                                if (sb != null && sb.length() > 0) {
                                                    appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_MENU_ENTER_FLUSH-[keys=" + sb.toString() + "]");
                                                    Map<String, Object> ev = new LinkedHashMap<>();
                                                    ev.put("event", "pressKey");
                                                    ev.put("timestamp", now);
                                                    ev.put("keys", sb.toString());
                                                    putComponentInfo(ev, currentMenuComponentRef[0]);
                                                    Writer w = writerRef.get();
                                                    if (w != null) writeLine(w, ev);
                                                    WebSocket c = clientConn.get();
                                                    if (c != null && c.isOpen()) c.send(toJson(ev));
                                                }
                                                currentMenuComponentRef[0] = null;
                                                currentMenuKeysRef[0] = null;
                                            } else {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_MENU_APPEND_KEY");
                                                String keyText = keyEventToString(ke);
                                                if (keyText != null && !keyText.isEmpty()) {
                                                    StringBuilder sb = currentMenuKeysRef[0];
                                                    if (sb != null) {
                                                        if (sb.length() > 0) sb.append(" ");
                                                        sb.append(keyText);
                                                    }
                                                }
                                            }
                                        }
                                    } else if (id == KeyEvent.KEY_RELEASED) {
                                        appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_BRANCH_KEY_RELEASED-[keyCode=" + keyCode + "]");
                                        pressedKeyCodes.remove(keyCode);
                                        // 4) Chord: on non-modifier key release, flush pending text then output KeyChordAction
                                        if (!isModifierKey(keyCode)) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_NON_MODIFIER_RELEASE-[keyCode=" + keyCode + "]");
                                            if (typedBuffer.length() > 0) {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_FLUSH_TYPED_BUFFER-[len=" + typedBuffer.length() + "]");
                                                Map<String, Object> ev = new LinkedHashMap<>();
                                                ev.put("event", "textInputAction");
                                                ev.put("text", typedBuffer.toString());
                                                ev.put("deltaMillis", now - sessionStart);
                                                ev.put("windowTitle", ctx.get("windowTitle"));
                                                ev.put("focusedComponentClass", ctx.get("focusedComponentClass"));
                                                ev.put("timestamp", now);
                                                Writer w = writerRef.get();
                                                if (w != null) writeLine(w, ev);
                                                WebSocket c = clientConn.get();
                                                if (c != null && c.isOpen()) c.send(toJson(ev));
                                                typedBuffer.setLength(0);
                                            }
                                            int mod = modifiersFromPressedSet(pressedKeyCodes);
                                            String chord = chordToString(keyCode, mod);
                                            if (!chord.isEmpty()) {
                                                appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_CHORD_ACTION-[chord=" + chord + "]");
                                                Map<String, Object> ev = new LinkedHashMap<>();
                                                ev.put("event", "keyChordAction");
                                                ev.put("keys", chord);
                                                ev.put("deltaMillis", now - sessionStart);
                                                ev.put("windowTitle", ctx.get("windowTitle"));
                                                ev.put("focusedComponentClass", ctx.get("focusedComponentClass"));
                                                ev.put("timestamp", now);
                                                Writer w = writerRef.get();
                                                if (w != null) writeLine(w, ev);
                                                WebSocket c = clientConn.get();
                                                if (c != null && c.isOpen()) c.send(toJson(ev));
                                            }
                                        }
                                    } else if (id == KeyEvent.KEY_TYPED) {
                                        appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_BRANCH_KEY_TYPED-[keyCode=" + keyCode + "]");
                                        char keyChar = ke.getKeyChar();
                                        // 5) IME: keyChar == CHAR_UNDEFINED → RawKeyEventAction fallback
                                        if (keyChar == KeyEvent.CHAR_UNDEFINED) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_CHAR_UNDEFINED_RAW-[keyCode=" + keyCode + "]");
                                            Map<String, Object> ev = new LinkedHashMap<>();
                                            ev.put("event", "rawKeyEventAction");
                                            ev.put("keyCode", keyCode);
                                            ev.put("modifiersEx", modifiersEx);
                                            ev.put("deltaMillis", now - sessionStart);
                                            ev.put("windowTitle", ctx.get("windowTitle"));
                                            ev.put("focusedComponentClass", ctx.get("focusedComponentClass"));
                                            ev.put("timestamp", now);
                                            Writer w = writerRef.get();
                                            if (w != null) writeLine(w, ev);
                                            WebSocket c = clientConn.get();
                                            if (c != null && c.isOpen()) c.send(toJson(ev));
                                            return;
                                        }
                                        // 3) KEY_TYPED merge: 300ms window
                                        if (typedBuffer.length() > 0 && (now - lastTypedTimeRef[0]) > TYPED_MERGE_WINDOW_MS) {
                                            appendDebugLog(outputDir, "[" + timeStr + "]KeyEvent_TYPED_MERGE_FLUSH-[len=" + typedBuffer.length() + "]");
                                            Map<String, Object> ev = new LinkedHashMap<>();
                                            ev.put("event", "textInputAction");
                                            ev.put("text", typedBuffer.toString());
                                            ev.put("deltaMillis", lastTypedTimeRef[0] - sessionStart);
                                            ev.put("windowTitle", ctx.get("windowTitle"));
                                            ev.put("focusedComponentClass", ctx.get("focusedComponentClass"));
                                            ev.put("timestamp", now);
                                            Writer w = writerRef.get();
                                            if (w != null) writeLine(w, ev);
                                            WebSocket c = clientConn.get();
                                            if (c != null && c.isOpen()) c.send(toJson(ev));
                                            typedBuffer.setLength(0);
                                        }
                                        typedBuffer.append(keyChar);
                                        lastTypedTimeRef[0] = now;
                                    }
                                }
                            } catch (Exception e) {
                                LOG.log(Level.WARNING, "Record event error", e);
                            }
                        };
                        listenerRef.set(listener);
                        long mask = AWTEvent.MOUSE_EVENT_MASK | AWTEvent.FOCUS_EVENT_MASK | AWTEvent.KEY_EVENT_MASK;
                        Toolkit.getDefaultToolkit().addAWTEventListener(listener, mask);
                        LOG.info("Recording started (startRecordAndReplay)");
                    } else if ("stopRecordAndReplay".equals(type)) {
                        recording[0] = false;
                        AWTEventListener L = listenerRef.getAndSet(null);
                        if (L != null) {
                            Toolkit.getDefaultToolkit().removeAWTEventListener(L);
                        }
                        LOG.info("Recording stopped (stopRecordAndReplay)");
                    } else if ("replay".equals(type)) {
                        runReplay(conn, message);
                    }
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "Message parse error", e);
                }
            }

            @Override
            public void onError(WebSocket conn, Exception ex) {
                LOG.log(Level.WARNING, "WS server error", ex);
            }

            @Override
            public void onStart() {
                int port = getPort();
                Map<String, Object> info = new LinkedHashMap<>();
                info.put("port", port);
                info.put("pid", expectedPid);
                info.put("status", "ready");
                try {
                    Files.write(infoFile.toPath(), toJson(info).getBytes(StandardCharsets.UTF_8));
                    LOG.info("Wrote " + INFO_FILE + " port=" + port + " pid=" + expectedPid);
                } catch (IOException e) {
                    LOG.log(Level.WARNING, "Write " + INFO_FILE + " failed", e);
                }
            }
        };

        server.start();
        LOG.info("Record agent WS server started on port " + server.getPort());

        Thread stopPoller = new Thread(() -> {
            while (running[0]) {
                try {
                    Thread.sleep(POLL_INTERVAL_MS);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    break;
                }
                if (stopFile.exists()) {
                    running[0] = false;
                    recording[0] = false;
                    AWTEventListener L = listenerRef.getAndSet(null);
                    if (L != null) {
                        EventQueue.invokeLater(() -> Toolkit.getDefaultToolkit().removeAWTEventListener(L));
                    }
                    try {
                        server.stop(1000);
                    } catch (InterruptedException ignored) {}
                    try {
                        OutputStreamWriter w = writerRef.getAndSet(null);
                        if (w != null) w.close();
                    } catch (IOException e) {
                        LOG.log(Level.WARNING, "Close record file failed", e);
                    }
                    LOG.info("Stop file detected, recording stopped");
                    break;
                }
            }
        }, "record-stop-poller");
        stopPoller.setDaemon(true);
        stopPoller.start();
    }

    /** Minimal JSON: get string value for key "key" from message like {"type":"handshake","pid":123}. */
    private static String getJsonStringValue(String message, String key) {
        if (message == null) return null;
        int idx = message.indexOf("\"" + key + "\"");
        if (idx < 0) return null;
        int colon = message.indexOf(':', idx);
        if (colon < 0) return null;
        int q = message.indexOf('"', colon);
        if (q < 0) return null;
        int q2 = message.indexOf('"', q + 1);
        if (q2 < 0) return null;
        return message.substring(q + 1, q2);
    }

    /** Get toolTipText via reflection (for ToolButton-like components). */
    private static String getToolTipTextByReflection(Component c) {
        if (c == null) return "";
        Class<?> clz = c.getClass();
        while (clz != null) {
            try {
                Method m = clz.getMethod("getToolTipText");
                Object val = m.invoke(c);
                if (val != null) {
                    String s = val.toString().trim();
                    if (!s.isEmpty()) return s;
                }
            } catch (NoSuchMethodException ignored) {
            } catch (Exception e) {
                return "";
            }
            clz = clz.getSuperclass();
        }
        return "";
    }

    private static void putComponentInfo(Map<String, Object> ev, Component c) {
        if (c == null) return;
        ev.put("componentClass", c.getClass().getName());
        ev.put("componentName", nullToEmpty(c.getName()));
        if (c instanceof AbstractButton) {
            ev.put("text", nullToEmpty(((AbstractButton) c).getText()));
        } else if (c instanceof JLabel) {
            ev.put("text", nullToEmpty(((JLabel) c).getText()));
        }
        if (isToolButtonLike(c)) {
            String toolTip = getToolTipTextByReflection(c);
            if (!toolTip.isEmpty()) {
                String currentText = ev.get("text") != null ? ev.get("text").toString().trim() : "";
                String currentCaption = ev.get("caption") != null ? ev.get("caption").toString().trim() : "";
                if (currentText.isEmpty() && currentCaption.isEmpty()) {
                    ev.put("text", toolTip);
                    ev.put("caption", toolTip);
                }
            }
        }
        Rectangle b = c.getBounds();
        Point loc = c.getLocationOnScreen();
        if (loc != null) {
            ev.put("screenX", loc.x);
            ev.put("screenY", loc.y);
        }
        ev.put("width", b.width);
        ev.put("height", b.height);
    }

    private static void putComponentContent(Map<String, Object> ev, Component c) {
        if (c instanceof JTextComponent) {
            ev.put("content", nullToEmpty(((JTextComponent) c).getText()));
        } else if (c instanceof JComboBox) {
            JComboBox<?> cb = (JComboBox<?>) c;
            Object sel = cb.getSelectedItem();
            ev.put("content", sel != null ? sel.toString() : "");
        } else if (c instanceof AbstractButton) {
            ev.put("content", nullToEmpty(((AbstractButton) c).getText()));
        }
    }

    private static String nullToEmpty(String s) {
        return s == null ? "" : s;
    }

    /** True if component is an editable text-like control (single-line, multi-line, editable combo, etc.). */
    private static boolean isEditComponent(Component c) {
        if (c instanceof JTextComponent) return true;
        if (c instanceof JComboBox) {
            JComboBox<?> cb = (JComboBox<?>) c;
            return cb.isEditable();
        }
        return false;
    }

    /** Resolve the component that received the event to the actual edit control (JTextField etc.). KeyEvent/FocusEvent may target an inner component. */
    private static Component resolveEditComponent(Component c) {
        if (c == null) return null;
        for (Component p = c; p != null; p = p.getParent()) {
            if (isEditComponent(p)) return p;
        }
        return null;
    }

    /** Resolve to the logical click target: AbstractButton, ToolButton-like, JMenuItem, JMenu, JMenuBar, or edit component (so ToolButton inner view maps to the button). */
    private static Component resolveClickTarget(Component c) {
        if (c == null) return null;
        for (Component p = c; p != null; p = p.getParent()) {
            if (p instanceof AbstractButton || p instanceof JMenuItem || p instanceof JMenu || p instanceof JMenuBar || isEditComponent(p) || isToolButtonLike(p)) {
                return p;
            }
        }
        return c;
    }

    /** True if component is button-like (click should be recorded on MOUSE_PRESSED if CLICKED is not fired). */
    private static boolean isButtonLike(Component c) {
        return (c instanceof AbstractButton) || isToolButtonLike(c);
    }

    /** Resolve to menu component (JMenuItem, JMenu, JMenuBar) in parent chain. */
    private static Component resolveMenuComponent(Component c) {
        if (c == null) return null;
        for (Component p = c; p != null; p = p.getParent()) {
            if (isMenuComponent(p)) return p;
        }
        return null;
    }

    /** True if component is a menu-related control. */
    private static boolean isMenuComponent(Component c) {
        return (c instanceof JMenuItem) || (c instanceof JMenu) || (c instanceof JMenuBar);
    }

    /** Convert a KeyEvent to a human-readable key string, e.g. Ctrl+N, Alt+F. */
    private static String keyEventToString(KeyEvent e) {
        if (e == null) return "";
        return chordToString(e.getKeyCode(), e.getModifiersEx());
    }

    /** Build chord string from keyCode and modifiersEx. */
    private static String chordToString(int keyCode, int modifiersEx) {
        StringBuilder sb = new StringBuilder();
        if ((modifiersEx & InputEvent.CTRL_DOWN_MASK) != 0) {
            if (sb.length() > 0) sb.append("+");
            sb.append("Ctrl");
        }
        if ((modifiersEx & InputEvent.ALT_DOWN_MASK) != 0) {
            if (sb.length() > 0) sb.append("+");
            sb.append("Alt");
        }
        if ((modifiersEx & InputEvent.SHIFT_DOWN_MASK) != 0) {
            if (sb.length() > 0) sb.append("+");
            sb.append("Shift");
        }
        if ((modifiersEx & InputEvent.META_DOWN_MASK) != 0) {
            if (sb.length() > 0) sb.append("+");
            sb.append("Meta");
        }
        String keyText = KeyEvent.getKeyText(keyCode);
        if (keyText != null && !keyText.isEmpty() && keyCode != KeyEvent.VK_SHIFT
                && keyCode != KeyEvent.VK_CONTROL && keyCode != KeyEvent.VK_ALT
                && keyCode != KeyEvent.VK_META) {
            if (sb.length() > 0) sb.append("+");
            sb.append(keyText);
        }
        return sb.toString();
    }

    private static boolean isModifierKey(int keyCode) {
        return keyCode == KeyEvent.VK_CONTROL || keyCode == KeyEvent.VK_ALT
                || keyCode == KeyEvent.VK_SHIFT || keyCode == KeyEvent.VK_META;
    }

    /** Current modifiers from set of pressed key codes. */
    private static int modifiersFromPressedSet(Set<Integer> pressed) {
        int mod = 0;
        if (pressed.contains(KeyEvent.VK_CONTROL)) mod |= InputEvent.CTRL_DOWN_MASK;
        if (pressed.contains(KeyEvent.VK_ALT)) mod |= InputEvent.ALT_DOWN_MASK;
        if (pressed.contains(KeyEvent.VK_SHIFT)) mod |= InputEvent.SHIFT_DOWN_MASK;
        if (pressed.contains(KeyEvent.VK_META)) mod |= InputEvent.META_DOWN_MASK;
        return mod;
    }

    /** Recording context: window title, focused component class, deltaMillis from session start. */
    private static Map<String, Object> getRecordingContext(long sessionStart) {
        Map<String, Object> ctx = new LinkedHashMap<>();
        long delta = System.currentTimeMillis() - sessionStart;
        ctx.put("deltaMillis", delta);
        try {
            KeyboardFocusManager kfm = KeyboardFocusManager.getCurrentKeyboardFocusManager();
            Window active = kfm.getActiveWindow();
            if (active != null) {
                String title = null;
                if (active instanceof Frame) title = ((Frame) active).getTitle();
                else if (active instanceof Dialog) title = ((Dialog) active).getTitle();
                ctx.put("windowTitle", title != null ? title : "");
            } else {
                ctx.put("windowTitle", "");
            }
            Component focus = kfm.getFocusOwner();
            ctx.put("focusedComponentClass", focus != null ? focus.getClass().getName() : "");
        } catch (Exception e) {
            ctx.put("windowTitle", "");
            ctx.put("focusedComponentClass", "");
        }
        return ctx;
    }

    /** Append ToolButton class and toolTipText to recordDir/toolbutton-tooltips.log for inspection. */
    private static synchronized void appendToToolButtonLog(File recordDir, String componentClass, String toolTipText) {
        try {
            File logFile = new File(recordDir, TOOLBUTTON_LOG_FILE);
            String line = System.currentTimeMillis() + "\t" + componentClass + "\ttoolTipText=" + (toolTipText == null ? "" : toolTipText) + "\n";
            Files.write(logFile.toPath(), line.getBytes(StandardCharsets.UTF_8), StandardOpenOption.CREATE, StandardOpenOption.APPEND);
        } catch (IOException e) {
            LOG.log(Level.WARNING, "Write toolbutton log failed", e);
        }
    }

    /** Build class type path from root to component (comma-separated class names). */
    private static String buildClassTypePath(Component c) {
        if (c == null) return "";
        java.util.List<String> path = new ArrayList<>();
        for (Component p = c; p != null; p = p.getParent()) {
            path.add(p.getClass().getName());
        }
        Collections.reverse(path);
        return String.join(",", path);
    }

    /** Append a line to recordDir/record-debug.log for mouse/key debug. */
    private static synchronized void appendDebugLog(File recordDir, String line) {
        try {
            File logFile = new File(recordDir, RECORD_DEBUG_LOG);
            Files.write(logFile.toPath(), (line + "\n").getBytes(StandardCharsets.UTF_8), StandardOpenOption.CREATE, StandardOpenOption.APPEND);
        } catch (IOException e) {
            LOG.log(Level.WARNING, "Write debug log failed", e);
        }
    }

    /** True if component's class name contains "ToolButton" (case-insensitive) or extends such a class. */
    private static boolean isToolButtonLike(Component c) {
        if (c == null) return false;
        Class<?> clz = c.getClass();
        while (clz != null) {
            if (clz.getName().toLowerCase(java.util.Locale.ROOT).contains("toolbutton")) {
                return true;
            }
            clz = clz.getSuperclass();
        }
        return false;
    }

    /** Dump readable bean properties (name=value) for debugging; used for ToolButton to find text/tooltip. */
    private static Map<String, Object> getComponentPropertiesForLog(Component c) {
        Map<String, Object> out = new LinkedHashMap<>();
        if (c == null) return out;
        java.util.Set<String> seen = new java.util.HashSet<>();
        try {
            BeanInfo info = Introspector.getBeanInfo(c.getClass(), Object.class);
            for (PropertyDescriptor pd : info.getPropertyDescriptors()) {
                Method getter = pd.getReadMethod();
                if (getter == null) continue;
                String name = pd.getName();
                if ("class".equals(name)) continue;
                if (seen.contains(name)) continue;
                seen.add(name);
                try {
                    Object val = getter.invoke(c);
                    String str = val == null ? "null" : val.toString();
                    if (str.length() > 200) str = str.substring(0, 200) + "...";
                    out.put(name, str);
                } catch (Exception e) {
                    out.put(name, "[err: " + e.getMessage() + "]");
                }
            }
        } catch (IntrospectionException e) {
            out.put("_introspectError", e.getMessage());
        }
        for (Method m : c.getClass().getMethods()) {
            String name = m.getName();
            if (name.length() < 4) continue;
            if ((name.startsWith("get") && name.length() > 3 && Character.isUpperCase(name.charAt(3)))
                    || (name.startsWith("is") && name.length() > 2 && Character.isUpperCase(name.charAt(2)))) {
                String prop = name.startsWith("get")
                        ? name.substring(3, 4).toLowerCase(java.util.Locale.ROOT) + name.substring(4)
                        : name.substring(2, 3).toLowerCase(java.util.Locale.ROOT) + name.substring(3);
                if (seen.contains(prop)) continue;
                if (m.getParameterCount() != 0 || m.getReturnType() == void.class) continue;
                try {
                    Object val = m.invoke(c);
                    String str = val == null ? "null" : val.toString();
                    if (str.length() > 200) str = str.substring(0, 200) + "...";
                    seen.add(prop);
                    out.put(prop, str);
                } catch (Exception ignored) {
                }
            }
        }
        return out;
    }

    private static synchronized void writeLine(Writer w, Map<String, Object> ev) {
        try {
            w.write(toJson(ev) + "\n");
            w.flush();
        } catch (IOException e) {
            LOG.log(Level.WARNING, "Write line failed", e);
        }
    }

    /** Run replay on EDT: parse steps from message, use Robot for clicks and text input. */
    private static void runReplay(WebSocket conn, String message) {
        try {
            JsonObject root = JsonParser.parseString(message).getAsJsonObject();
            JsonElement stepsEl = root.get("steps");
            if (stepsEl == null || !stepsEl.isJsonArray()) {
                LOG.warning("Replay: no steps array in message");
                sendReplayDone(conn, 0, "no steps");
                return;
            }
            JsonArray steps = stepsEl.getAsJsonArray();
            final int count = steps.size();
            EventQueue.invokeAndWait(() -> {
                try {
                    Robot robot = new Robot();
                    robot.setAutoDelay(50);
                    for (int i = 0; i < count; i++) {
                        JsonObject step = steps.get(i).getAsJsonObject();
                        String event = getJsonStr(step, "event");
                        if ("click".equals(event)) {
                            int x = getJsonInt(step, "x", 0);
                            int y = getJsonInt(step, "y", 0);
                            int screenX = getJsonInt(step, "screenX", 0);
                            int screenY = getJsonInt(step, "screenY", 0);
                            int btn = getJsonInt(step, "button", 1);
                            int px = screenX + x;
                            int py = screenY + y;
                            robot.mouseMove(px, py);
                            int mask = (btn == 1) ? InputEvent.BUTTON1_DOWN_MASK
                                    : (btn == 2) ? InputEvent.BUTTON2_DOWN_MASK
                                    : InputEvent.BUTTON3_DOWN_MASK;
                            robot.mousePress(mask);
                            robot.mouseRelease(mask);
                            robot.delay(150);
                        } else if ("focusLost".equals(event)) {
                            int screenX = getJsonInt(step, "screenX", 0);
                            int screenY = getJsonInt(step, "screenY", 0);
                            int w = getJsonInt(step, "width", 0);
                            int h = getJsonInt(step, "height", 0);
                            int cx = screenX + w / 2;
                            int cy = screenY + h / 2;
                            robot.mouseMove(cx, cy);
                            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                            robot.delay(100);
                            String content = getJsonStr(step, "content");
                            if (content != null && !content.isEmpty()) {
                                pasteViaClipboard(robot, content);
                            }
                            robot.delay(150);
                        }
                    }
                    EventQueue.invokeLater(() -> sendReplayDone(conn, count, null));
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "Replay failed", e);
                    EventQueue.invokeLater(() -> sendReplayDone(conn, 0, e.getMessage()));
                }
            });
        } catch (Exception e) {
            LOG.log(Level.WARNING, "Replay parse failed", e);
            sendReplayDone(conn, 0, e.getMessage());
        }
    }

    private static void pasteViaClipboard(Robot robot, String text) {
        Clipboard cb = Toolkit.getDefaultToolkit().getSystemClipboard();
        StringSelection sel = new StringSelection(text);
        cb.setContents(sel, null);
        robot.keyPress(KeyEvent.VK_CONTROL);
        robot.keyPress(KeyEvent.VK_A);
        robot.keyRelease(KeyEvent.VK_A);
        robot.keyRelease(KeyEvent.VK_CONTROL);
        robot.delay(30);
        robot.keyPress(KeyEvent.VK_CONTROL);
        robot.keyPress(KeyEvent.VK_V);
        robot.keyRelease(KeyEvent.VK_V);
        robot.keyRelease(KeyEvent.VK_CONTROL);
    }

    private static void sendReplayDone(WebSocket conn, int count, String error) {
        try {
            Map<String, Object> ack = new LinkedHashMap<>();
            ack.put("type", "replayDone");
            ack.put("count", count);
            if (error != null) ack.put("error", error);
            conn.send(toJson(ack));
        } catch (Exception e) {
            LOG.log(Level.WARNING, "Send replayDone failed", e);
        }
    }

    private static String getJsonStr(JsonObject obj, String key) {
        JsonElement e = obj.get(key);
        return (e != null && e.isJsonPrimitive()) ? e.getAsString() : null;
    }

    private static int getJsonInt(JsonObject obj, String key, int def) {
        JsonElement e = obj.get(key);
        if (e == null || !e.isJsonPrimitive()) return def;
        try {
            return e.getAsInt();
        } catch (Exception ex) {
            return def;
        }
    }

    private static String toJson(Object o) {
        if (o == null) return "null";
        if (o instanceof Map) {
            StringBuilder sb = new StringBuilder("{");
            Map<?, ?> map = (Map<?, ?>) o;
            boolean first = true;
            for (Map.Entry<?, ?> e : map.entrySet()) {
                if (!first) sb.append(",");
                first = false;
                sb.append("\"").append(escape(String.valueOf(e.getKey()))).append("\":");
                sb.append(toJson(e.getValue()));
            }
            sb.append("}");
            return sb.toString();
        }
        if (o instanceof Number || o instanceof Boolean) {
            return String.valueOf(o);
        }
        return "\"" + escape(String.valueOf(o)) + "\"";
    }

    private static String escape(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\n", "\\n")
                .replace("\r", "\\r")
                .replace("\t", "\\t");
    }
}
