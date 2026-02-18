package com.mars.javaui.record;

import java.beans.BeanInfo;
import java.beans.IntrospectionException;
import java.beans.Introspector;
import java.beans.PropertyDescriptor;
import java.awt.*;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.StringSelection;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.awt.event.AWTEventListener;
import java.awt.event.FocusEvent;
import java.awt.event.InputEvent;
import java.awt.event.ItemEvent;
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
import javax.swing.tree.TreePath;
import javax.swing.event.TreeExpansionListener;
import javax.swing.event.TreeExpansionEvent;
import javax.swing.event.PopupMenuEvent;
import javax.swing.event.PopupMenuListener;
import javax.swing.tree.TreeModel;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.mars.javaui.record.config.EventFilterConfig;
import com.mars.javaui.record.eventoperation.KeyboardEventHandler;
import com.mars.javaui.record.eventoperation.ItemEventHandler;
import com.mars.javaui.record.eventoperation.MouseEventHandler;
import com.mars.javaui.record.eventoperation.RecordingContext;
import com.mars.javaui.record.keyword.MarsKeyword;
import com.mars.javaui.protocol.AgentProtocol;
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
        try {
            File jarFile = new File(RecordAgent.class.getProtectionDomain().getCodeSource().getLocation().toURI());
            File jarDir = jarFile.getParentFile();
            File logsDir = jarDir != null ? new File(jarDir, "logs") : null;
            AgentLogger.setup(logsDir);
        } catch (Exception ignored) { }
        AgentLogger.begin(LOG, "agentArgs=" + agentArgs);
        try {
            AgentLogger.info(LOG, "agentmain called, agentArgs=" + agentArgs);
            if (agentArgs == null || agentArgs.isEmpty()) {
                AgentLogger.warning(LOG, "No agentArgs provided");
                AgentLogger.end(LOG, "no agentArgs");
                return;
            }
            String recordDirStr = agentArgs.trim();
            int pid = -1;
            if (recordDirStr.contains("|")) {
                String[] parts = recordDirStr.split("\\|", 2);
                recordDirStr = parts[0].trim();
                try {
                    pid = Integer.parseInt(parts[1].trim());
                    AgentLogger.info(LOG, "parsed pid=" + pid);
                } catch (NumberFormatException e) {
                    AgentLogger.logException(LOG, Level.WARNING, "Invalid pid in agentArgs: " + (parts.length > 1 ? parts[1] : ""), e);
                }
            }
            File outputDir = new File(recordDirStr);
            if (!outputDir.isDirectory()) {
                if (!outputDir.mkdirs()) {
                    AgentLogger.warning(LOG, "Could not create directory: " + outputDir);
                    AgentLogger.end(LOG, "mkdir failed");
                    return;
                }
                AgentLogger.info(LOG, "created outputDir=" + outputDir);
            }
            if (GraphicsEnvironment.isHeadless()) {
                AgentLogger.info(LOG, "Headless environment, skipping record");
                AgentLogger.end(LOG, "headless");
                return;
            }
            final int pidForRun = pid;
            EventQueue.invokeAndWait(() -> run(outputDir, pidForRun));
            AgentLogger.end(LOG, "ok");
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.SEVERE, "Record agent run failed", e);
            AgentLogger.end(LOG, "exception");
        }
    }

    private static void run(File outputDir, int expectedPid) {
        AgentLogger.begin(LOG, "outputDir=" + outputDir + ", expectedPid=" + expectedPid);
        File recordFile = new File(outputDir, RECORD_FILE);
        File stopFile = new File(outputDir, STOP_FILE);
        File infoFile = new File(outputDir, INFO_FILE);
        if (stopFile.exists()) {
            try {
                Files.delete(stopFile.toPath());
                AgentLogger.info(LOG, "deleted stopFile");
            } catch (IOException e) {
                AgentLogger.logException(LOG, Level.WARNING, "Delete stopFile failed", e);
            }
        }

        // Record file is opened only when startRecordAndReplay is received (no data until recording is activated)
        final AtomicReference<OutputStreamWriter> writerRef = new AtomicReference<>();

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
        final String[] currentEditInitialTextRef = new String[1];
        final JComboBox<?>[] currentComboBoxRef = new JComboBox[1];
        final String[] currentComboInitialRef = new String[1];
        final String[] currentComboSelectedRef = new String[1];
        final boolean[] currentComboInteractedRef = new boolean[1];
        final boolean[] currentComboEmittedRef = new boolean[1];
        // Track current menu component and accumulated key sequence
        final Component[] currentMenuComponentRef = new Component[1];
        final StringBuilder[] currentMenuKeysRef = new StringBuilder[1];
        final int DEDUPE_MS = 200;
        final int DEDUPE_POS_TOLERANCE = 8;
        // Press→Release click: valid window 50–400ms; double-click: same object, same button, distance <= 6px, time <= DBLCLICK_MS
        final long PRESS_RELEASE_MIN_MS = 50;
        final long PRESS_RELEASE_MAX_MS = 400;
        /** Max ms between two clicks to merge as double-click (350–450ms recommended). */
        final long DBLCLICK_MS = 450;
        final int SCREEN_POS_TOLERANCE = 15;
        /** Max pixel distance for merging two clicks into double-click. */
        final int CLICK_MERGE_DISTANCE_PX = 6;
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
        // Pending click: defer emit until double-click window (DBLCLICK_MS). If second release on same target, same button, <=6px → emit one DoubleClick; else emit one Click.
        final int PENDING_CLICK_DELAY_MS = (int) DBLCLICK_MS;
        final java.util.concurrent.atomic.AtomicReference<javax.swing.Timer> pendingClickTimerRef = new java.util.concurrent.atomic.AtomicReference<>();
        final Component[] pendingClickComponentRef = new Component[1];
        final int[] pendingClickButtonRef = new int[1];
        final int[] pendingClickXRef = new int[1];
        final int[] pendingClickYRef = new int[1];
        final long[] pendingClickReleaseTimeRef = new long[1];

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
        // Merge continuous typing within 10 seconds into a single FillEdit
        final long TYPED_MERGE_WINDOW_MS = 10_000;
        // Skip duplicate FillEdit: when Tab/Enter already emitted fillEdit, skip FOCUS_LOST for same component within 500ms
        final Component[] lastFillEditComponentRef = new Component[1];
        final long[] lastFillEditTimeRef = new long[1];
        final long FILLEDIT_DEDUPE_MS = 500;
        final AtomicReference<TreeExpansionListener> treeExpansionListenerRef = new AtomicReference<>();
        final AtomicReference<java.util.List<JTree>> treeExpansionTreesRef = new AtomicReference<>();

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
                    javax.swing.Timer pt = pendingClickTimerRef.getAndSet(null);
                    if (pt != null) pt.stop();
                    pendingClickComponentRef[0] = null;
                    AWTEventListener L = listenerRef.getAndSet(null);
                    if (L != null) {
                        EventQueue.invokeLater(() -> Toolkit.getDefaultToolkit().removeAWTEventListener(L));
                    }
                }
            }

            @Override
            public void onMessage(WebSocket conn, String message) {
                AgentLogger.begin(LOG, "type=" + getJsonStringValue(message, "type"));
                try {
                    if (message != null && message.contains("\"method\"")) {
                        handleJsonRpc(conn, message);
                        AgentLogger.end(LOG, "jsonrpc");
                        return;
                    }
                    String type = getJsonStringValue(message, "type");
                    if ("handshake".equals(type)) {
                        AgentLogger.info(LOG, "handshake");
                        Map<String, Object> ack = new LinkedHashMap<>();
                        ack.put("type", "handshake_ack");
                        ack.put("pid", expectedPid);
                        conn.send(toJson(ack));
                    } else if ("startRecordAndReplay".equals(type)) {
                        AgentLogger.info(LOG, "startRecordAndReplay");
                        if (writerRef.get() == null) {
                            try {
                                writerRef.set(new OutputStreamWriter(new FileOutputStream(recordFile, true), StandardCharsets.UTF_8));
                            } catch (IOException e) {
                                AgentLogger.logException(LOG, Level.SEVERE, "Record file open failed", e);
                                AgentLogger.end(LOG, "exception");
                                return;
                            }
                        }
                        recording[0] = true;
                        sessionStartTimeRef[0] = System.currentTimeMillis();
                        pressedKeyCodes.clear();
                        typedBuffer.setLength(0);
                        final RecordingContext ctx = new RecordingContext(
                                writerRef, clientConn, outputDir, recording,
                                PRESS_RELEASE_MIN_MS, PRESS_RELEASE_MAX_MS, DBLCLICK_MS,
                                SCREEN_POS_TOLERANCE, CLICK_MERGE_DISTANCE_PX, PENDING_CLICK_DELAY_MS,
                                lastPressedTimeRef, lastPressedComponentRef, lastPressedXRef, lastPressedYRef,
                                lastPressedScreenXRef, lastPressedScreenYRef, lastPressedButtonRef,
                                lastReleasedTimeRef, lastReleasedComponentRef,
                                pendingClickTimerRef, pendingClickComponentRef, pendingClickButtonRef,
                                pendingClickXRef, pendingClickYRef, pendingClickReleaseTimeRef,
                                currentComboBoxRef, currentComboInitialRef, currentComboSelectedRef, currentComboInteractedRef, currentComboEmittedRef,
                                currentEditComponentRef, currentEditInitialTextRef, currentEditHadKeyRef,
                                lastKeyDedupWhenRef, lastKeyDedupIdRef, lastKeyDedupCodeRef,
                                lastKeyDedupModifiersRef, lastKeyDedupSourceRef,
                                pressedKeyCodes, typedBuffer, lastTypedTimeRef, KEY_DEDUP_MS,
                                lastFillEditComponentRef, lastFillEditTimeRef, FILLEDIT_DEDUPE_MS);
                        AWTEventListener listener = event -> {
                            if (!recording[0]) return;
                            try {
                                if (event instanceof MouseEvent) {
                                    MouseEventHandler.handle((MouseEvent) event, ctx);
                                    return;
                                }
                                if (event instanceof KeyEvent) {
                                    KeyboardEventHandler.handle((KeyEvent) event, ctx);
                                    return;
                                }
                                if (event instanceof ItemEvent) {
                                    ItemEventHandler.handle((ItemEvent) event, ctx);
                                    return;
                                }
                                if (event instanceof FocusEvent) {
                                    FocusEvent fe = (FocusEvent) event;
                                    if (fe.getID() == FocusEvent.FOCUS_GAINED) {
                                        Component comp = resolveComboBoxOrEditComponent(fe.getComponent());
                                        if (comp instanceof JComboBox) {
                                            currentComboBoxRef[0] = (JComboBox<?>) comp;
                                            currentComboInitialRef[0] = getComboSelectedText((JComboBox<?>) comp);
                                            currentComboSelectedRef[0] = currentComboInitialRef[0];
                                            currentComboInteractedRef[0] = false;
                                            currentComboEmittedRef[0] = false;
                                            attachComboPopupListener((JComboBox<?>) comp, ctx);
                                            AgentLogger.info(LOG, "ComboFocusGained name=" + getComponentNameForLog(comp)
                                                    + ", selected=" + currentComboSelectedRef[0]);
                                        } else if (comp != null) {
                                            Component edit = resolveEditComponent(comp);
                                            if (edit != null) {
                                                currentEditComponentRef[0] = edit;
                                                currentEditInitialTextRef[0] = getEditText(edit);
                                                currentEditHadKeyRef[0] = false;
                                            }
                                        }
                                        return;
                                    }

                                    if (fe.getID() == FocusEvent.FOCUS_LOST) {
                                        long nowFocus = System.currentTimeMillis();
                                        Component comp = resolveComboBoxOrEditComponent(fe.getComponent());

                                        // ComboBox finalize
                                        if (comp instanceof JComboBox) {
                                            JComboBox<?> cb = (JComboBox<?>) comp;
                                            String data = currentComboSelectedRef[0] != null ? currentComboSelectedRef[0] : getComboSelectedText(cb);
                                            AgentLogger.info(LOG, "ComboFocusLost name=" + getComponentNameForLog(cb)
                                                    + ", interacted=" + currentComboInteractedRef[0]
                                                    + ", emitted=" + currentComboEmittedRef[0]
                                                    + ", data=" + data);
                                            if (currentComboInteractedRef[0] && !currentComboEmittedRef[0]) {
                                                WebSocket c0 = clientConn.get();
                                                boolean wsOpen0 = c0 != null && c0.isOpen();
                                                boolean hasWriter0 = writerRef.get() != null;
                                                AgentLogger.info(LOG, "ComboEmit FocusLost attempt name=" + getComponentNameForLog(cb)
                                                        + ", data=" + data
                                                        + ", recording=" + recording[0]
                                                        + ", writer=" + hasWriter0
                                                        + ", wsOpen=" + wsOpen0);
                                                Map<String, Object> step = MarsKeyword.buildScriptStep("SelectDropList", cb, "", data, "");
                                                step.put("event", "selectDropList");
                                                step.put("timestamp", nowFocus);
                                                putComponentInfo(step, cb);
                                                step.put("content", data);
                                                Writer w = writerRef.get();
                                                if (w != null) writeLine(w, step);
                                                WebSocket c = clientConn.get();
                                                if (c != null && c.isOpen()) c.send(toJson(step));
                                                currentComboEmittedRef[0] = true;
                                                AgentLogger.info(LOG, "ComboEmit FocusLost name=" + getComponentNameForLog(cb)
                                                        + ", data=" + data);
                                            }
                                            currentComboBoxRef[0] = null;
                                            currentComboInitialRef[0] = null;
                                            currentComboSelectedRef[0] = null;
                                            currentComboInteractedRef[0] = false;
                                            currentComboEmittedRef[0] = false;
                                            return;
                                        }

                                        // Edit finalize: create one FillEdit step on focus lost (no step on each key)
                                        Component edit = resolveEditComponent(comp);
                                        if (edit != null) {
                                            if (edit == lastFillEditComponentRef[0] && (nowFocus - lastFillEditTimeRef[0]) < FILLEDIT_DEDUPE_MS) {
                                                lastFillEditComponentRef[0] = null;
                                                lastFillEditTimeRef[0] = 0L;
                                                currentEditComponentRef[0] = null;
                                                currentEditHadKeyRef[0] = false;
                                                currentEditInitialTextRef[0] = null;
                                                return;
                                            }

                                            String finalText = getEditText(edit);
                                            String initialText = currentEditInitialTextRef[0] != null ? currentEditInitialTextRef[0] : "";
                                            boolean changed = !Objects.equals(finalText, initialText);
                                            if (currentEditHadKeyRef[0] || changed) {
                                                String data = buildFillEditData(finalText);
                                                Map<String, Object> step = MarsKeyword.buildScriptStep("FillEdit", edit, "", data, "");
                                                step.put("event", "fillEdit");
                                                step.put("timestamp", nowFocus);
                                                putComponentInfo(step, edit);
                                                step.put("content", data);
                                                Writer w = writerRef.get();
                                                if (w != null) writeLine(w, step);
                                                WebSocket c = clientConn.get();
                                                if (c != null && c.isOpen()) c.send(toJson(step));
                                            }
                                            typedBuffer.setLength(0);
                                            lastTypedTimeRef[0] = 0L;
                                        }

                                        currentEditComponentRef[0] = null;
                                        currentEditHadKeyRef[0] = false;
                                        currentEditInitialTextRef[0] = null;
                                    }
                                }
                            } catch (Exception e) {
                                AgentLogger.logException(LOG, Level.WARNING, "Record event error", e);
                            }
                        };
                        listenerRef.set(listener);
                        long mask = AWTEvent.MOUSE_EVENT_MASK | AWTEvent.FOCUS_EVENT_MASK | AWTEvent.KEY_EVENT_MASK | AWTEvent.ITEM_EVENT_MASK;
                        Toolkit.getDefaultToolkit().addAWTEventListener(listener, mask);
                        EventQueue.invokeLater(() -> {
                            java.util.List<JTree> treeList = new ArrayList<>();
                            for (Window w : Window.getWindows()) {
                                if (w instanceof Container) collectJTrees((Container) w, treeList);
                            }
                            TreeExpansionListener tel = new TreeExpansionListener() {
                                @Override
                                public void treeExpanded(TreeExpansionEvent e) { emitTreeExpansion(e, true); }
                                @Override
                                public void treeCollapsed(TreeExpansionEvent e) { emitTreeExpansion(e, false); }
                                private void emitTreeExpansion(TreeExpansionEvent e, boolean expanded) {
                                    if (!recording[0]) return;
                                    Object src = e.getSource();
                                    if (!(src instanceof JTree)) return;
                                    JTree tree = (JTree) src;
                                    String pathData = buildTreePathStringFromPath(e.getPath());
                                    String keyword = expanded ? "ExpandTreeNode" : "CollapseTreeNode";
                                    Map<String, Object> step = MarsKeyword.buildScriptStep(keyword, tree, "", pathData, "");
                                    step.put("event", expanded ? "expandTreeNode" : "collapseTreeNode");
                                    step.put("timestamp", System.currentTimeMillis());
                                    Writer w = writerRef.get();
                                    if (w != null) {
                                        try { writeLine(w, step); } catch (IOException ioe) {
                                            AgentLogger.logException(LOG, Level.WARNING, "writeLine failed", ioe);
                                        }
                                    }
                                    WebSocket c = clientConn.get();
                                    if (c != null && c.isOpen()) c.send(toJson(step));
                                }
                            };
                            for (JTree t : treeList) t.addTreeExpansionListener(tel);
                            treeExpansionListenerRef.set(tel);
                            treeExpansionTreesRef.set(treeList);
                        });
                        AgentLogger.info(LOG, "Recording started (startRecordAndReplay)");
                    } else if ("stopRecordAndReplay".equals(type)) {
                        AgentLogger.info(LOG, "stopRecordAndReplay");
                        recording[0] = false;
                        AWTEventListener L = listenerRef.getAndSet(null);
                        if (L != null) {
                            Toolkit.getDefaultToolkit().removeAWTEventListener(L);
                        }
                        final TreeExpansionListener tel = treeExpansionListenerRef.getAndSet(null);
                        final java.util.List<JTree> treeList = treeExpansionTreesRef.getAndSet(null);
                        if (tel != null && treeList != null) {
                            EventQueue.invokeLater(() -> {
                                for (JTree t : treeList) t.removeTreeExpansionListener(tel);
                            });
                        }
                        OutputStreamWriter w = writerRef.getAndSet(null);
                        if (w != null) {
                            try { w.close(); } catch (IOException e) {
                                AgentLogger.logException(LOG, Level.WARNING, "Close record file on stop failed", e);
                            }
                        }
                        AgentLogger.info(LOG, "Recording stopped (stopRecordAndReplay)");
                    } else if ("pauseRecordAndReplay".equals(type)) {
                        AgentLogger.info(LOG, "pauseRecordAndReplay (e.g. during highlight)");
                        recording[0] = false;
                    } else if ("resumeRecordAndReplay".equals(type)) {
                        AgentLogger.info(LOG, "resumeRecordAndReplay");
                        if (writerRef.get() != null) recording[0] = true;
                    } else if ("replay".equals(type)) {
                        AgentLogger.info(LOG, "replay");
                        runReplay(conn, message);
                    }
                    AgentLogger.end(LOG, "ok");
                } catch (Exception e) {
                    AgentLogger.logException(LOG, Level.WARNING, "Message parse error", e);
                    AgentLogger.end(LOG, "exception");
                }
            }

            @Override
            public void onError(WebSocket conn, Exception ex) {
                AgentLogger.logException(LOG, Level.WARNING, "WS server error", ex);
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
                    AgentLogger.info(LOG, "Wrote " + INFO_FILE + " port=" + port + " pid=" + expectedPid);
                } catch (IOException e) {
                    AgentLogger.logException(LOG, Level.WARNING, "Write " + INFO_FILE + " failed", e);
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
                    AgentLogger.logException(LOG, Level.INFO, "Stop poller interrupted", e);
                    Thread.currentThread().interrupt();
                    break;
                }
                if (stopFile.exists()) {
                    running[0] = false;
                    recording[0] = false;
                    javax.swing.Timer pt = pendingClickTimerRef.getAndSet(null);
                    if (pt != null) pt.stop();
                    pendingClickComponentRef[0] = null;
                    AWTEventListener L = listenerRef.getAndSet(null);
                    if (L != null) {
                        EventQueue.invokeLater(() -> Toolkit.getDefaultToolkit().removeAWTEventListener(L));
                    }
                    try {
                        server.stop(1000);
                    } catch (InterruptedException e) {
                        AgentLogger.logException(LOG, Level.INFO, "server.stop interrupted", e);
                        Thread.currentThread().interrupt();
                    }
                    try {
                        OutputStreamWriter w = writerRef.getAndSet(null);
                        if (w != null) w.close();
                    } catch (IOException e) {
                        AgentLogger.logException(LOG, Level.WARNING, "Close record file failed", e);
                    }
                    LOG.info("Stop file detected, recording stopped");
                    break;
                }
            }
        }, "record-stop-poller");
        stopPoller.setDaemon(true);
        stopPoller.start();
        AgentLogger.end(LOG, "server and poller started");
    }

    @SuppressWarnings("unchecked")
    private static void handleJsonRpc(WebSocket conn, String message) {
        try {
            JsonObject root = JsonParser.parseString(message).getAsJsonObject();
            if (!root.has("method") || !root.has("id")) return;
            String method = root.get("method").getAsString();
            int id = root.get("id").getAsInt();
            JsonObject params = root.has("params") && root.get("params").isJsonObject() ? root.get("params").getAsJsonObject() : new JsonObject();
            Object result = null;
            if ("agent.getObjectTree".equals(method)) {
                String hint = params.has("rootWindowHint") ? params.get("rootWindowHint").getAsString() : null;
                final String h = hint;
                final Object[] treeHolder = new Object[1];
                EventQueue.invokeAndWait(() -> treeHolder[0] = AgentProtocol.buildObjectTree(h));
                result = treeHolder[0];
            } else if ("agent.perform".equals(method)) {
                JsonObject performParams = params.has("params") ? params.get("params").getAsJsonObject() : params;
                String action = performParams.has("action") ? performParams.get("action").getAsString() : "";
                JsonElement targetEl = performParams.get("target");
                String data = performParams.has("data") ? performParams.get("data").getAsString() : "";
                Map<String, Object> parentKey = null;
                Map<String, Object> objectKey = null;
                if (targetEl != null && targetEl.isJsonObject()) {
                    JsonObject target = targetEl.getAsJsonObject();
                    if (target.has("parentKey") && !target.get("parentKey").isJsonNull()) {
                        parentKey = jsonToMap(target.get("parentKey").getAsJsonObject());
                    }
                    if (target.has("objectKey")) {
                        objectKey = jsonToMap(target.get("objectKey").getAsJsonObject());
                    }
                }
                if (objectKey != null) {
                    final Map<String, Object> pk = parentKey;
                    final Map<String, Object> ok = objectKey;
                    Boolean[] done = new Boolean[1];
                    EventQueue.invokeAndWait(() -> {
                                Component rootWin = AgentProtocol.findMainWindowRoot("LoanIQ");
                                if (rootWin == null) rootWin = AgentProtocol.findMainWindowRoot(null);
                                Component comp = AgentProtocol.resolveComponent(rootWin, pk, ok);
                                if (comp == null) { done[0] = false; return; }
                                try {
                                    Robot robot = new Robot();
                                    robot.setAutoDelay(30);
                                    int[] b = getScreenBounds(comp);
                                    if (b == null || b[2] <= 0 || b[3] <= 0) { done[0] = false; return; }
                                    int cx = b[0] + b[2] / 2;
                                    int cy = b[1] + b[3] / 2;
                                    if ("Click".equals(action)) {
                                        robot.mouseMove(cx, cy);
                                        robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                    } else if ("DoubleClick".equals(action)) {
                                        robot.mouseMove(cx, cy);
                                        robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.delay(50);
                                        robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                    } else if ("SetText".equals(action)) {
                                        robot.mouseMove(cx, cy);
                                        robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                        robot.delay(100);
                                        Object[] parsed = parseFillEditData(data);
                                        boolean doHomeDel = (Boolean) parsed[0];
                                        String text = (String) parsed[1];
                                        // Default behavior: always clear (HOME + 30x DEL) unless caller explicitly disables.
                                        if (!doHomeDel) doHomeDel = true;
                                        if (doHomeDel) {
                                            robot.keyPress(KeyEvent.VK_HOME);
                                            robot.keyRelease(KeyEvent.VK_HOME);
                                            robot.delay(30);
                                            for (int d = 0; d < 30; d++) {
                                                robot.keyPress(KeyEvent.VK_DELETE);
                                                robot.keyRelease(KeyEvent.VK_DELETE);
                                                robot.delay(20);
                                            }
                                        }
                                        if (text != null && !text.isEmpty()) {
                                            pasteViaClipboard(robot, text);
                                        }
                                        robot.delay(150);
                                    }
                                    done[0] = true;
                                } catch (Exception e) {
                                    AgentLogger.logException(LOG, Level.WARNING, "agent.perform failed", e);
                                    done[0] = false;
                                }
                            });
                            if (done[0] != null && done[0]) {
                                result = Collections.singletonMap("ok", true);
                            } else {
                                result = Collections.singletonMap("ok", false);
                            }
                        }
                    }
                Map<String, Object> resp = new LinkedHashMap<>();
                resp.put("id", id);
                if (result != null) resp.put("result", result);
                conn.send(toJson(resp));
            } catch (Exception e) {
                try {
                    Map<String, Object> resp = new LinkedHashMap<>();
                    resp.put("id", -1);
                    resp.put("error", e.getMessage());
                    conn.send(toJson(resp));
                } catch (Exception ignored) { }
            }
        }

    private static void runReplay(WebSocket conn, String message) {
        try {
            JsonObject root = JsonParser.parseString(message).getAsJsonObject();
            JsonArray steps = root.has("steps") && root.get("steps").isJsonArray() ? root.get("steps").getAsJsonArray() : null;
            if (steps == null) {
                sendReplayDone(conn, 0, 0, "replay: steps missing");
                return;
            }
            final int total = steps.size();
            new Thread(() -> {
                try {
                    Robot robot = new Robot();
                    robot.setAutoDelay(30);
                    for (int i = 0; i < total; i++) {
                        try {
                            JsonObject step = steps.get(i).getAsJsonObject();
                            String keyword = getJsonStr(step, "keyword");
                            JsonObject obj = step.has("object") && step.get("object").isJsonObject() ? step.get("object").getAsJsonObject() : null;
                            JsonObject parentId = obj != null && obj.has("parentKey") ? obj.get("parentKey").getAsJsonObject()
                                    : (step.has("parentIdentifier") ? step.get("parentIdentifier").getAsJsonObject() : null);
                            JsonObject objectId = obj != null && obj.has("objectKey") ? obj.get("objectKey").getAsJsonObject()
                                    : (step.has("objectIdentifier") ? step.get("objectIdentifier").getAsJsonObject() : null);
                            if (objectId == null) {
                                final int failIdx = i;
                                EventQueue.invokeLater(() -> sendReplayDone(conn, total, failIdx, "Object identifier missing"));
                                return;
                            }
                            Map<String, Object> pk = parentId != null ? jsonToMap(parentId) : null;
                            Map<String, Object> ok = jsonToMap(objectId);
                            Component rootWin = AgentProtocol.findMainWindowRoot("LoanIQ");
                            if (rootWin == null) rootWin = AgentProtocol.findMainWindowRoot(null);
                            Component comp = AgentProtocol.resolveComponent(rootWin, pk, ok);
                            if (comp == null) {
                                int idx = i;
                                EventQueue.invokeLater(() -> sendReplayDone(conn, total, idx, "Object not found"));
                                return;
                            }
                            String action = "Click";
                            if ("DoubleClickButton".equals(keyword) || "DoubleClick".equals(keyword)) action = "DoubleClick";
                            else if ("FillEdit".equals(keyword)) action = "SetText";
                            else if ("SelectTab".equals(keyword)) action = "SelectTab";
                            final String data = getJsonStr(step, "data");
                            int[] b = getScreenBounds(comp);
                            if (b == null || b[2] <= 0 || b[3] <= 0) {
                                int idx = i;
                                EventQueue.invokeLater(() -> sendReplayDone(conn, total, idx, "Object has no bounds"));
                                return;
                            }
                            int cx = b[0] + b[2] / 2;
                            int cy = b[1] + b[3] / 2;
                            if ("Click".equals(action) || "SelectTab".equals(action)) {
                                robot.mouseMove(cx, cy);
                                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                robot.delay(150);
                            } else if ("DoubleClick".equals(action)) {
                                robot.mouseMove(cx, cy);
                                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                robot.delay(50);
                                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                robot.delay(150);
                            } else if ("SetText".equals(action)) {
                                robot.mouseMove(cx, cy);
                                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                                robot.delay(100);
                                Object[] parsed = parseFillEditData(data);
                                boolean doHomeDel = (Boolean) parsed[0];
                                String text = (String) parsed[1];
                                if (doHomeDel) {
                                    robot.keyPress(KeyEvent.VK_HOME);
                                    robot.keyRelease(KeyEvent.VK_HOME);
                                    robot.delay(30);
                                    for (int d = 0; d < 30; d++) {
                                        robot.keyPress(KeyEvent.VK_DELETE);
                                        robot.keyRelease(KeyEvent.VK_DELETE);
                                        robot.delay(20);
                                    }
                                }
                                if (text != null && !text.isEmpty()) {
                                    pasteViaClipboard(robot, text);
                                }
                                robot.delay(150);
                            }
                        } catch (Exception e) {
                            AgentLogger.logException(LOG, Level.WARNING, "Replay step failed", e);
                        }
                    }
                    EventQueue.invokeLater(() -> sendReplayDone(conn, total, null, null));
                } catch (Exception e) {
                    AgentLogger.logException(LOG, Level.WARNING, "Replay failed", e);
                    EventQueue.invokeLater(() -> sendReplayDone(conn, 0, null, e.getMessage()));
                }
            }, "replay-thread").start();
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "Replay parse failed", e);
            sendReplayDone(conn, 0, null, e.getMessage());
        }
    }



public static void putComponentInfo(Map<String, Object> step, Component comp) {
    if (comp == null) return;
    Map<String, Object> meta = new LinkedHashMap<>();
    try {
        Point loc = comp.getLocationOnScreen();
        Dimension dim = comp.getSize();
        if (loc != null && dim != null) {
            Map<String, Object> debugBounds = new LinkedHashMap<>();
            debugBounds.put("x", loc.x);
            debugBounds.put("y", loc.y);
            debugBounds.put("w", dim.width);
            debugBounds.put("h", dim.height);
            meta.put("debugBounds", debugBounds);
        }
    } catch (Exception ignored) { }
    if (!meta.isEmpty()) step.put("meta", meta);
}

public static boolean shouldRecordClickTarget(Component c) {
    if (c == null) return false;
    String t = c.getClass().getName();
    String simple = c.getClass().getSimpleName();
    if (c instanceof JLabel && !c.isFocusable()) return false;
    if (c instanceof JPanel || c instanceof JScrollPane || c instanceof JSplitPane || c instanceof JSeparator || c instanceof JToolBar) return false;
    if (simple.contains("Renderer") || t.contains("Renderer")) return false;
    if (simple.contains("StatusBar")) return false;
    if (c instanceof AbstractButton) return true;
    if (c instanceof JTextComponent) return true;
    if (c instanceof JComboBox) return true;
    if (c instanceof JTree) return true;
    if (c instanceof JMenuItem) return true;
    if (c instanceof JTabbedPane) return true;
    return false;
}

public static Component resolveClickTarget(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof AbstractButton || p instanceof JMenuItem || p instanceof JMenu || p instanceof JMenuBar || p instanceof JComboBox || p instanceof JTree || p instanceof JTextComponent || isToolButtonLike(p)) {
            return p;
        }
    }
    return c;
}

public static Component resolveComboBoxOrEditComponent(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JComboBox || p instanceof JTextComponent) return p;
    }
    return c;
}

public static Component resolveEditComponent(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JTextComponent) return p;
    }
    return null;
}

public static JTree resolveJTree(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JTree) return (JTree) p;
    }
    return null;
}

private static void collectJTrees(Container root, java.util.List<JTree> out) {
    if (root == null) return;
    for (Component c : root.getComponents()) {
        if (c instanceof JTree) out.add((JTree) c);
        if (c instanceof Container) collectJTrees((Container) c, out);
    }
}

public static String buildTreePathString(JTree tree) {
    if (tree == null) return "";
    TreePath path = tree.getSelectionPath();
    if (path == null) return "";
    return buildTreePathStringFromPath(path);
}

private static String buildTreePathStringFromPath(TreePath path) {
    if (path == null) return "";
    Object[] arr = path.getPath();
    if (arr == null || arr.length == 0) return "";
    StringBuilder sb = new StringBuilder();
    for (int i = arr.length - 1; i >= 0; i--) {
        String s = String.valueOf(arr[i]);
        if (sb.length() > 0) sb.append(';');
        sb.append(s);
    }
    return sb.toString();
}

public static String buildMenuPathString(JMenuItem item) {
    if (item == null) return "";
    java.util.List<String> parts = new ArrayList<>();
    String text = item.getText();
    if (text != null && !text.isEmpty()) parts.add(text);
    Component p = item.getParent();
    while (p != null) {
        if (p instanceof JPopupMenu) {
            Component inv = ((JPopupMenu) p).getInvoker();
            if (inv instanceof JMenu) {
                String t = ((JMenu) inv).getText();
                if (t != null && !t.isEmpty()) parts.add(t);
                p = inv.getParent();
                continue;
            }
        }
        if (p instanceof JMenu) {
            String t = ((JMenu) p).getText();
            if (t != null && !t.isEmpty()) parts.add(t);
        }
        if (p instanceof JMenuBar) break;
        p = p.getParent();
    }
    return String.join(";", parts);
}

public static boolean isToolButtonLike(Component c) {
    if (c == null) return false;
    if (c instanceof AbstractButton && c.getParent() instanceof JToolBar) return true;
    String t = c.getClass().getName();
    return t.contains("ToolButton");
}

public static Component findToolbarParentOrSelf(Component c) {
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JToolBar) return p;
    }
    return c;
}

public static String getToolButtonSemanticText(Component c) {
    if (c instanceof AbstractButton) {
        String text = ((AbstractButton) c).getText();
        if (text != null && !text.trim().isEmpty()) return text.trim();
    }
    String toolTip = getToolTipTextByReflection(c);
    if (toolTip != null && !toolTip.trim().isEmpty()) return toolTip.trim();
    String name = c != null ? c.getName() : "";
    return name != null ? name : "";
}

private static String getToolTipTextByReflection(Component c) {
    if (c == null) return "";
    Class<?> clz = c.getClass();
    while (clz != null) {
        try {
            Method m = clz.getMethod("getToolTipText");
            Object val = m.invoke(c);
            if (val != null) return val.toString();
        } catch (NoSuchMethodException ignored) {
        } catch (Exception e) {
            return "";
        }
        clz = clz.getSuperclass();
    }
    return "";
}

public static String getComboSelectedText(JComboBox<?> cb) {
    Object sel = cb != null ? cb.getSelectedItem() : null;
    return sel != null ? sel.toString() : "";
}

public static String getComponentNameForLog(Component c) {
    if (c == null) return "";
    String name = c.getName();
    if (name != null && !name.trim().isEmpty()) return name.trim();
    if (c instanceof AbstractButton) {
        String t = ((AbstractButton) c).getText();
        if (t != null && !t.trim().isEmpty()) return t.trim();
    }
    if (c instanceof JComboBox) {
        Object sel = ((JComboBox<?>) c).getSelectedItem();
        if (sel != null) return sel.toString();
    }
    String text = invokeStringGetter(c, "getText");
    return text != null ? text : "";
}

private static void attachComboPopupListener(JComboBox<?> cb, com.mars.javaui.record.eventoperation.RecordingContext ctx) {
    if (cb == null || ctx == null) return;
    final String key = "mars.popupListenerAttached";
    Object existing = cb.getClientProperty(key);
    if (existing instanceof PopupMenuListener) return;

    PopupMenuListener listener = new PopupMenuListener() {
        @Override
        public void popupMenuWillBecomeVisible(PopupMenuEvent e) {
            ctx.currentComboInteractedRef[0] = true;
            ctx.currentComboSelectedRef[0] = getComboSelectedText(cb);
            AgentLogger.info(LOG, "ComboPopupVisible name=" + getComponentNameForLog(cb)
                    + ", selected=" + ctx.currentComboSelectedRef[0]);
        }

        @Override
        public void popupMenuWillBecomeInvisible(PopupMenuEvent e) {
            ctx.currentComboInteractedRef[0] = true;
            ctx.currentComboSelectedRef[0] = getComboSelectedText(cb);
            String data = ctx.currentComboSelectedRef[0] != null ? ctx.currentComboSelectedRef[0] : "";
            AgentLogger.info(LOG, "ComboPopupHidden name=" + getComponentNameForLog(cb)
                    + ", emitted=" + ctx.currentComboEmittedRef[0]
                    + ", data=" + data);
            if (!ctx.currentComboEmittedRef[0]) {
                WebSocket c0 = ctx.clientConn.get();
                boolean wsOpen0 = c0 != null && c0.isOpen();
                boolean hasWriter0 = ctx.writerRef.get() != null;
                AgentLogger.info(LOG, "ComboEmit PopupHidden attempt name=" + getComponentNameForLog(cb)
                        + ", data=" + data
                        + ", recording=" + ctx.recording[0]
                        + ", writer=" + hasWriter0
                        + ", wsOpen=" + wsOpen0);
                Map<String, Object> step = MarsKeyword.buildScriptStep("SelectDropList", cb, "", data, "");
                step.put("event", "selectDropList");
                step.put("timestamp", System.currentTimeMillis());
                putComponentInfo(step, cb);
                step.put("content", data);
                try {
                    Writer w = ctx.writerRef.get();
                    if (w != null) writeLine(w, step);
                } catch (Exception ignored) { }
                WebSocket c = ctx.clientConn.get();
                if (c != null && c.isOpen()) c.send(toJson(step));
                ctx.currentComboEmittedRef[0] = true;
                AgentLogger.info(LOG, "ComboEmit PopupHidden name=" + getComponentNameForLog(cb)
                        + ", data=" + data);
            }
        }

        @Override
        public void popupMenuCanceled(PopupMenuEvent e) {
            AgentLogger.info(LOG, "ComboPopupCanceled name=" + getComponentNameForLog(cb));
        }
    };

    cb.addPopupMenuListener(listener);
    cb.putClientProperty(key, listener);
}

public static String getEditText(Component c) {
    if (c instanceof JTextComponent) return nullToEmpty(((JTextComponent) c).getText());
    return "";
}

public static String buildFillEditData(String finalText) {
    return finalText != null ? finalText : "";
}

public static void appendDebugLog(File outputDir, String line) {
    try {
        File f = new File(outputDir, RECORD_DEBUG_LOG);
        try (FileWriter fw = new FileWriter(f, true)) {
            fw.write(line + System.lineSeparator());
        }
    } catch (IOException ignored) { }
}

public static void writeLine(Writer w, Map<String, Object> step) throws IOException {
    w.write(toJson(step));
    w.write("\n");
    w.flush();
}

private static String getJsonStringValue(String json, String key) {
    try {
        JsonObject obj = JsonParser.parseString(json).getAsJsonObject();
        return obj.has(key) ? obj.get(key).getAsString() : null;
    } catch (Exception e) {
        return null;
    }
}

private static Map<String, Object> jsonToMap(JsonObject obj) {
    Map<String, Object> map = new LinkedHashMap<>();
    for (Map.Entry<String, JsonElement> e : obj.entrySet()) {
        JsonElement v = e.getValue();
        if (v.isJsonObject()) map.put(e.getKey(), jsonToMap(v.getAsJsonObject()));
        else if (v.isJsonArray()) {
            JsonArray arr = v.getAsJsonArray();
            java.util.List<Object> list = new ArrayList<>();
            for (JsonElement a : arr) {
                if (a.isJsonObject()) list.add(jsonToMap(a.getAsJsonObject()));
                else if (a.isJsonPrimitive()) list.add(a.getAsString());
            }
            map.put(e.getKey(), list);
        } else if (v.isJsonPrimitive()) {
            map.put(e.getKey(), v.getAsString());
        }
    }
    return map;
}

private static String nullToEmpty(String s) {
    return s == null ? "" : s;
}


/** Find first component in the window hierarchy that matches the given identifier (javaType, text, caption). */
private static Component findComponentByIdentifier(JsonObject id) {
    AgentLogger.begin(LOG, "id=" + (id != null ? id.toString() : "null"));
    Window[] windows = Window.getWindows();
    if (windows != null) {
        for (Window w : windows) {
            Component c = findInContainer(w, id);
            if (c != null) {
                AgentLogger.end(LOG, "found in Window");
                return c;
            }
        }
    }
    Frame[] frames = Frame.getFrames();
    if (frames != null) {
        for (Frame f : frames) {
            if (f != null && !Arrays.asList(Window.getWindows()).contains(f)) {
                Component c = findInContainer(f, id);
                if (c != null) {
                    AgentLogger.end(LOG, "found in Frame");
                    return c;
                }
            }
        }
    }
    AgentLogger.end(LOG, "not found");
    return null;
}

private static Component findInContainer(Component c, JsonObject id) {
    if (c == null) return null;
    if (componentMatchesIdentifier(c, id)) return c;
    if (c instanceof Container) {
        for (Component child : ((Container) c).getComponents()) {
            Component found = findInContainer(child, id);
            if (found != null) return found;
        }
    }
    return null;
}

private static boolean componentMatchesIdentifier(Component c, JsonObject id) {
    String wantJavaType = getJsonStr(id, "javaType");
    if (wantJavaType != null && !wantJavaType.isEmpty()) {
        if (!wantJavaType.equals(c.getClass().getName())) return false;
    }
    String wantText = getJsonStr(id, "text");
    if (wantText != null) {
        String have = getComponentText(c);
        if (have == null) have = "";
        if (!wantText.equals(have)) return false;
    }
    String wantCaption = getJsonStr(id, "caption");
    if (wantCaption != null) {
        String have = getComponentCaption(c);
        if (have == null) have = "";
        if (!wantCaption.equals(have)) return false;
    }
    return true;
}

private static String getComponentText(Component c) {
    if (c instanceof AbstractButton) return nullToEmpty(((AbstractButton) c).getText());
    if (c instanceof JLabel) return nullToEmpty(((JLabel) c).getText());
    if (c instanceof JTextComponent) return nullToEmpty(((JTextComponent) c).getText());
    return invokeStringGetter(c, "getText");
}

private static String getComponentCaption(Component c) {
    return invokeStringGetter(c, "getCaption");
}

/** Get component state string for assertion (e.g. getText(), getSelectedItem()). */
private static String getComponentStateString(Component c) {
    if (c == null) return "";
    if (c instanceof JTextComponent) return nullToEmpty(((JTextComponent) c).getText());
    if (c instanceof JComboBox) {
        Object sel = ((JComboBox<?>) c).getSelectedItem();
        return sel != null ? sel.toString() : "";
    }
    if (c instanceof AbstractButton) return nullToEmpty(((AbstractButton) c).getText());
    return invokeStringGetter(c, "getText");
}

private static String invokeStringGetter(Component c, String methodName) {
    if (c == null) return null;
    try {
        Method m = c.getClass().getMethod(methodName);
        Object v = m.invoke(c);
        if (v == null) return null;
        String s = v.toString().trim();
        return s.isEmpty() ? null : s;
    } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
        AgentLogger.logException(LOG, Level.FINE, "invokeStringGetter " + methodName, e);
        return null;
    }
}

/** Get screen bounds [x, y, width, height] or null. */
private static int[] getScreenBounds(Component c) {
    if (c == null) return null;
    try {
        Point loc = c.getLocationOnScreen();
        Dimension dim = c.getSize();
        if (loc != null && dim != null) {
            return new int[] { loc.x, loc.y, dim.width, dim.height };
        }
    } catch (Exception e) {
        AgentLogger.logException(LOG, Level.FINE, "getScreenBounds", e);
    }
    return null;
}



/** Parse FillEdit data with optional {HOME}{DEL}... prefix. Returns [doHomeDel, remainingText]. */
private static Object[] parseFillEditData(String data) {
    boolean doHomeDel = false;
    if (data == null) return new Object[]{false, ""};
    String s = data;
    // Accept tokens like {HOME}{DEL}{DEL}...
    if (s.startsWith("{HOME}")) {
        doHomeDel = true;
        s = s.substring("{HOME}".length());
        while (s.startsWith("{DEL}")) {
            s = s.substring("{DEL}".length());
        }
    }
    return new Object[]{doHomeDel, s};
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

private static void sendReplayDone(WebSocket conn, int count, Integer failedIndex, String error) {
    try {
        Map<String, Object> ack = new LinkedHashMap<>();
        ack.put("type", "replayDone");
        ack.put("count", count);
        if (failedIndex != null) ack.put("failedIndex", failedIndex);
        if (error != null) ack.put("error", error);
        conn.send(toJson(ack));
    } catch (Exception e) {
        AgentLogger.logException(LOG, Level.WARNING, "Send replayDone failed", e);
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
        AgentLogger.logException(LOG, Level.FINE, "getJsonInt key=" + key, ex);
        return def;
    }
}

public static String toJson(Object o) {
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
    if (o instanceof java.util.List) {
        StringBuilder sb = new StringBuilder("[");
        java.util.List<?> list = (java.util.List<?>) o;
        for (int i = 0; i < list.size(); i++) {
            if (i > 0) sb.append(",");
            sb.append(toJson(list.get(i)));
        }
        sb.append("]");
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

