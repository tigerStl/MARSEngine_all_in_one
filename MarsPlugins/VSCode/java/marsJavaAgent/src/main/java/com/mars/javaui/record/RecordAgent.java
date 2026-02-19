package com.mars.javaui.record;

import java.awt.AWTEvent;
import java.awt.Component;
import java.awt.Container;
import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Frame;
import java.awt.GraphicsEnvironment;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.Robot;
import java.awt.Toolkit;
import java.awt.Window;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.StringSelection;
import java.awt.event.AWTEventListener;
import java.awt.event.FocusEvent;
import java.awt.event.InputEvent;
import java.awt.event.ItemEvent;
import java.awt.event.KeyEvent;
import java.awt.event.MouseEvent;
import java.io.File;
import java.io.FileOutputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.lang.instrument.Instrumentation;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.atomic.AtomicReference;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

import javax.swing.AbstractButton;
import javax.swing.JComboBox;
import javax.swing.JLabel;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JScrollPane;
import javax.swing.JSeparator;
import javax.swing.JSplitPane;
import javax.swing.JTabbedPane;
import javax.swing.JTable;
import javax.swing.JToolBar;
import javax.swing.JTree;
import javax.swing.event.PopupMenuEvent;
import javax.swing.event.PopupMenuListener;
import javax.swing.event.TableModelEvent;
import javax.swing.event.TableModelListener;
import javax.swing.event.TreeExpansionEvent;
import javax.swing.event.TreeExpansionListener;
import javax.swing.text.JTextComponent;
import javax.swing.tree.TreeModel;
import javax.swing.tree.TreePath;

import org.java_websocket.WebSocket;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.mars.javaui.protocol.AgentProtocol;
import com.mars.javaui.record.eventoperation.ItemEventHandler;
import com.mars.javaui.record.eventoperation.KeyboardEventHandler;
import com.mars.javaui.record.eventoperation.MouseEventHandler;
import com.mars.javaui.record.eventoperation.RecordingContext;
import com.mars.javaui.record.keyword.MarsKeyword;

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

    private static final class SearchAndUpdateReplaySpec {
        String mode;
        String targetColumn;
        String sourceValue;
        String targetValue;
        java.util.List<String> conditionColumns = new ArrayList<>();
        java.util.List<String> conditionValues = new ArrayList<>();
    }

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
        // Table state
        final JTable[] currentTableRef = new JTable[1];
        final int[] currentTableRowRef = new int[] { -1 };
        final int[] currentTableColRef = new int[] { -1 };
        final String[] currentTableColumnNameRef = new String[1];
        final String[] currentTableInitialValueRef = new String[1];
        final boolean[] currentTableHadKeyRef = new boolean[1];
        final boolean[] currentTableValueChangedRef = new boolean[1];
        final boolean[] currentTableEmittedRef = new boolean[1];
        final String[][] currentTableConditionColumnsRef = new String[1][];
        final String[][] currentTableConditionValuesRef = new String[1][];
        final long[] lastTableInteractionTimeRef = new long[1];
        final JTable[] lastTableRightClickRef = new JTable[1];
        final int[] lastTableRightClickRowRef = new int[] { -1 };
        final int[] lastTableRightClickColRef = new int[] { -1 };
        final String[] lastTableRightClickColumnNameRef = new String[1];
        final String[] lastTableRightClickCellValueRef = new String[1];
        final String[][] lastTableRightClickConditionColumnsRef = new String[1][];
        final String[][] lastTableRightClickConditionValuesRef = new String[1][];
        final long[] lastTableRightClickTimeRef = new long[1];
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
                                currentTableRef, currentTableRowRef, currentTableColRef, currentTableColumnNameRef,
                                currentTableInitialValueRef, currentTableHadKeyRef, currentTableValueChangedRef,
                                currentTableEmittedRef, currentTableConditionColumnsRef, currentTableConditionValuesRef,
                                lastTableInteractionTimeRef, lastTableRightClickRef, lastTableRightClickRowRef,
                                lastTableRightClickColRef, lastTableRightClickColumnNameRef, lastTableRightClickCellValueRef,
                                lastTableRightClickConditionColumnsRef, lastTableRightClickConditionValuesRef, lastTableRightClickTimeRef,
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
                                                if (isTableEditorComponent(edit)) {
                                                    JTable table = resolveJTable(edit);
                                                    if (table == null) table = currentTableRef[0];
                                                    if (table != null) {
                                                        int row = table.getEditingRow();
                                                        int col = table.getEditingColumn();
                                                        if (row < 0 || col < 0) {
                                                            row = table.getSelectedRow();
                                                            col = table.getSelectedColumn();
                                                        }
                                                        ensureCurrentTableCell(ctx, table, row, col, System.currentTimeMillis(), true);
                                                    }
                                                    currentEditComponentRef[0] = null;
                                                    currentEditInitialTextRef[0] = null;
                                                    currentEditHadKeyRef[0] = false;
                                                    return;
                                                }
                                                JTable table = resolveJTable(edit);
                                                if (table != null) {
                                                    int row = table.getEditingRow();
                                                    int col = table.getEditingColumn();
                                                    if (row < 0 || col < 0) {
                                                        row = table.getSelectedRow();
                                                        col = table.getSelectedColumn();
                                                    }
                                                    ensureCurrentTableCell(ctx, table, row, col, System.currentTimeMillis(), true);
                                                    return;
                                                }
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
                                            if (isTableEditorComponent(edit)) {
                                                JTable table = resolveJTable(edit);
                                                if (table == null) table = currentTableRef[0];
                                                if (table != null) {
                                                    int er = table.getEditingRow();
                                                    int ec = table.getEditingColumn();
                                                    if (er < 0 || ec < 0) {
                                                        er = table.getSelectedRow();
                                                        ec = table.getSelectedColumn();
                                                    }
                                                    ensureCurrentTableCell(ctx, table, er, ec, nowFocus, false);
                                                    lastTableInteractionTimeRef[0] = nowFocus;
                                                }
                                                currentEditComponentRef[0] = null;
                                                currentEditHadKeyRef[0] = false;
                                                currentEditInitialTextRef[0] = null;
                                                typedBuffer.setLength(0);
                                                lastTypedTimeRef[0] = 0L;
                                                return;
                                            }
                                            JTable editTable = resolveJTable(edit);
                                            if (editTable != null) {
                                                int er = editTable.getEditingRow();
                                                int ec = editTable.getEditingColumn();
                                                if (er < 0 || ec < 0) {
                                                    er = editTable.getSelectedRow();
                                                    ec = editTable.getSelectedColumn();
                                                }
                                                ensureCurrentTableCell(ctx, editTable, er, ec, nowFocus, false);
                                                lastTableInteractionTimeRef[0] = nowFocus;
                                                currentEditComponentRef[0] = null;
                                                currentEditHadKeyRef[0] = false;
                                                currentEditInitialTextRef[0] = null;
                                                return;
                                            }
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
                            if ("SearchAndUpdate".equals(keyword)) {
                                if (!(comp instanceof JTable)) {
                                    int idx = i;
                                    EventQueue.invokeLater(() -> sendReplayDone(conn, total, idx, "SearchAndUpdate target is not JTable"));
                                    return;
                                }
                                String replayErr = replaySearchAndUpdate((JTable) comp, step, robot);
                                if (replayErr != null) {
                                    int idx = i;
                                    EventQueue.invokeLater(() -> sendReplayDone(conn, total, idx, replayErr));
                                    return;
                                }
                                continue;
                            }
                            final String data = getJsonStr(step, "data");
                            if ("SelectDropList".equals(keyword) || "SelectDropDown".equals(keyword)) {
                                if (comp instanceof JComboBox) {
                                    JComboBox<?> cb = (JComboBox<?>) comp;
                                    EventQueue.invokeAndWait(() -> selectComboValue(cb, data));
                                    robot.delay(150);
                                }
                                continue;
                            }
                            if ("SelectTreeList".equals(keyword)) {
                                if (comp instanceof JTree) {
                                    JTree tree = (JTree) comp;
                                    EventQueue.invokeAndWait(() -> selectTreeByPath(tree, data));
                                    robot.delay(150);
                                }
                                continue;
                            }
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

    private static String replaySearchAndUpdate(JTable table, JsonObject step, Robot robot) {
        final String method = "replaySearchAndUpdate";
        AgentLogger.begin(LOG, "[" + method + ":L900] begin");
        try {
            if (table == null) {
                AgentLogger.info(LOG, "[" + method + ":L903] table is null");
                return "SearchAndUpdate target table is null";
            }

            String para = getJsonStr(step, "parameter");
            if (para == null || para.trim().isEmpty()) {
                AgentLogger.info(LOG, "[" + method + ":L909] para missing");
                return "No para found. Table columnName is not set, please set para.";
            }
            String data = getJsonStr(step, "data");
            SearchAndUpdateReplaySpec spec = parseSearchAndUpdateSpec(para, data);
            if (spec == null) {
                AgentLogger.info(LOG, "[" + method + ":L915] parse spec failed, para=" + para + ", data=" + data);
                return "SearchAndUpdate para/data format is invalid";
            }
            AgentLogger.info(LOG, "[" + method + ":L918] mode=" + spec.mode + ", targetColumn=" + spec.targetColumn
                    + ", condColumns=" + spec.conditionColumns + ", source=" + spec.sourceValue + ", target=" + spec.targetValue
                    + ", condValues=" + spec.conditionValues);

            int targetCol = findTableColumnIndexByName(table, spec.targetColumn);
            if (targetCol < 0) {
                AgentLogger.info(LOG, "[" + method + ":L924] target column not found: " + spec.targetColumn);
                return "Column not found in table header: " + spec.targetColumn;
            }
            java.util.List<Integer> condColIndexes = new ArrayList<>();
            if ("conditionColumns".equals(spec.mode)) {
                for (String condCol : spec.conditionColumns) {
                    int idx = findTableColumnIndexByName(table, condCol);
                    if (idx < 0) {
                        AgentLogger.info(LOG, "[" + method + ":L932] condition column not found: " + condCol);
                        return "Column not found in table header: " + condCol;
                    }
                    condColIndexes.add(idx);
                }
            }

            int rows = table.getRowCount();
            AgentLogger.info(LOG, "[" + method + ":L940] rowCount=" + rows);
            if (rows <= 0) {
                return "No data in table";
            }

            int matchedRow = -1;
            if ("singleColumn".equals(spec.mode)) {
                for (int r = 0; r < rows; r++) {
                    String cell = getTableCellValue(table, r, targetCol);
                    if (matchesByEqualOrRegex(cell, spec.sourceValue)) {
                        matchedRow = r;
                        break;
                    }
                }
            } else {
                for (int r = 0; r < rows; r++) {
                    boolean ok = true;
                    for (int i = 0; i < condColIndexes.size(); i++) {
                        String cell = getTableCellValue(table, r, condColIndexes.get(i));
                        if (!matchesByEqualOrRegex(cell, spec.conditionValues.get(i))) {
                            ok = false;
                            break;
                        }
                    }
                    if (ok) {
                        matchedRow = r;
                        break;
                    }
                }
            }

            if (matchedRow < 0) {
                AgentLogger.info(LOG, "[" + method + ":L968] no matched row");
                return "Unable to locate target table cell by para/data";
            }
            AgentLogger.info(LOG, "[" + method + ":L971] matchedRow=" + matchedRow + ", targetCol=" + targetCol);

            String setErr = updateTableCellByUserSimulation(table, matchedRow, targetCol, spec.targetValue, robot);
            if (setErr != null) {
                AgentLogger.info(LOG, "[" + method + ":L975] update failed: " + setErr);
                return setErr;
            }

            AgentLogger.info(LOG, "[" + method + ":L979] SearchAndUpdate replay success");
            return null;
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L982] exception", e);
            return "SearchAndUpdate replay exception: " + e.getMessage();
        } finally {
            AgentLogger.end(LOG, "[" + method + ":L985] end");
        }
    }

    private static SearchAndUpdateReplaySpec parseSearchAndUpdateSpec(String para, String data) {
        final String method = "parseSearchAndUpdateSpec";
        AgentLogger.begin(LOG, "[" + method + ":L990] para=" + para + ", data=" + data);
        try {
            if (para == null || para.trim().isEmpty() || data == null) {
                AgentLogger.info(LOG, "[" + method + ":L993] para/data null or empty");
                return null;
            }
            SearchAndUpdateReplaySpec spec = new SearchAndUpdateReplaySpec();
            String p = para.trim();
            if (p.contains(":[") && p.endsWith("]")) {
                int split = p.indexOf(':');
                String targetColumn = p.substring(0, split).trim();
                String colsPart = p.substring(split + 1).trim();
                if (!colsPart.startsWith("[") || !colsPart.endsWith("]") || targetColumn.isEmpty()) return null;
                String inside = colsPart.substring(1, colsPart.length() - 1).trim();
                if (inside.isEmpty()) return null;
                String[] parts = inside.split(",");
                for (String part : parts) {
                    String c = part.trim();
                    if (!c.isEmpty()) spec.conditionColumns.add(c);
                }
                if (spec.conditionColumns.isEmpty()) return null;
                spec.mode = "conditionColumns";
                spec.targetColumn = targetColumn;

                String d = data.trim();
                if (!d.startsWith("[") || d.indexOf(']') < 0) {
                    AgentLogger.info(LOG, "[" + method + ":L1014] condition mode data not starts with []");
                    return null;
                }
                int right = d.indexOf(']');
                String valPart = d.substring(1, right).trim();
                String remain = d.substring(right + 1).trim();
                if (!remain.startsWith(";")) {
                    AgentLogger.info(LOG, "[" + method + ":L1021] condition mode data missing ';' separator");
                    return null;
                }
                String target = remain.substring(1).trim();
                if (target.isEmpty()) return null;
                String[] vals = valPart.isEmpty() ? new String[0] : valPart.split(":");
                for (String val : vals) spec.conditionValues.add(val.trim());
                if (spec.conditionValues.size() != spec.conditionColumns.size()) {
                    AgentLogger.info(LOG, "[" + method + ":L1029] condition values count mismatch, cols="
                            + spec.conditionColumns.size() + ", vals=" + spec.conditionValues.size());
                    return null;
                }
                spec.targetValue = target;
            } else {
                spec.mode = "singleColumn";
                spec.targetColumn = p;
                String d = data.trim();
                int split = d.indexOf(':');
                if (split <= 0 || split >= d.length() - 1) {
                    AgentLogger.info(LOG, "[" + method + ":L1040] single mode data invalid, expected source:target");
                    return null;
                }
                spec.sourceValue = d.substring(0, split).trim();
                spec.targetValue = d.substring(split + 1).trim();
                if (spec.sourceValue.isEmpty() || spec.targetValue.isEmpty()) return null;
            }
            AgentLogger.info(LOG, "[" + method + ":L1047] parsed mode=" + spec.mode + ", targetColumn=" + spec.targetColumn);
            return spec;
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1050] parse failed", e);
            return null;
        } finally {
            AgentLogger.end(LOG, "[" + method + ":L1053] end");
        }
    }

    private static boolean matchesByEqualOrRegex(String actual, String expected) {
        final String method = "matchesByEqualOrRegex";
        String a = actual != null ? actual : "";
        String e = expected != null ? expected : "";
        if (Objects.equals(a, e)) return true;
        try {
            boolean matched = Pattern.matches(e, a);
            AgentLogger.info(LOG, "[" + method + ":L1062] regexMatch expected=" + e + ", actual=" + a + ", result=" + matched);
            return matched;
        } catch (PatternSyntaxException ex) {
            AgentLogger.logException(LOG, Level.FINE, "[" + method + ":L1065] invalid regex: " + e, ex);
            return false;
        }
    }

    private static String updateTableCellByUserSimulation(JTable table, int row, int col, String targetValue, Robot robot) {
        final String method = "updateTableCellByUserSimulation";
        AgentLogger.begin(LOG, "[" + method + ":L1071] row=" + row + ", col=" + col + ", targetValue=" + targetValue);
        try {
            Rectangle rect = table.getCellRect(row, col, true);
            Point loc = table.getLocationOnScreen();
            if (rect == null || loc == null) {
                AgentLogger.info(LOG, "[" + method + ":L1076] cell rect or location is null");
                return "Target table cell bounds unavailable";
            }
            int cx = loc.x + rect.x + Math.max(1, rect.width / 2);
            int cy = loc.y + rect.y + Math.max(1, rect.height / 2);
            AgentLogger.info(LOG, "[" + method + ":L1081] click center x=" + cx + ", y=" + cy + ", w=" + rect.width + ", h=" + rect.height);

            robot.mouseMove(cx, cy);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(80);

            robot.keyPress(KeyEvent.VK_HOME);
            robot.keyRelease(KeyEvent.VK_HOME);
            robot.delay(20);
            for (int i = 0; i < 30; i++) {
                robot.keyPress(KeyEvent.VK_DELETE);
                robot.keyRelease(KeyEvent.VK_DELETE);
                robot.delay(10);
            }

            if (targetValue != null && !targetValue.isEmpty()) {
                pasteViaClipboard(robot, targetValue);
            }
            robot.delay(50);

            robot.mouseMove(cx, cy);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(20);
            robot.keyPress(KeyEvent.VK_ENTER);
            robot.keyRelease(KeyEvent.VK_ENTER);
            robot.delay(80);

            AgentLogger.info(LOG, "[" + method + ":L1107] update finished");
            return null;
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1110] update failed", e);
            return "Failed to update table cell: " + e.getMessage();
        } finally {
            AgentLogger.end(LOG, "[" + method + ":L1113] end");
        }
    }

    private static void selectComboValue(JComboBox<?> cb, String value) {
        if (cb == null || value == null) return;
        String target = value.trim();
        if (target.isEmpty()) return;
        int count = cb.getItemCount();
        for (int i = 0; i < count; i++) {
            Object item = cb.getItemAt(i);
            if (item != null && target.equals(String.valueOf(item))) {
                cb.setSelectedIndex(i);
                return;
            }
        }
        if (cb.isEditable()) {
            cb.setSelectedItem(target);
        }
    }

    private static void selectTreeByPath(JTree tree, String data) {
        if (tree == null || data == null) return;
        String raw = data.trim();
        if (raw.isEmpty()) return;
        String[] parts = raw.split(";");
        java.util.List<String> list = new ArrayList<>();
        for (String p : parts) {
            String s = p.trim();
            if (!s.isEmpty()) list.add(s);
        }
        if (list.isEmpty()) return;
        Collections.reverse(list); // root -> leaf

        TreeModel model = tree.getModel();
        Object node = model.getRoot();
        java.util.List<Object> path = new ArrayList<>();
        path.add(node);
        int idx = 0;
        if (node != null && String.valueOf(node).equals(list.get(0))) {
            idx = 1;
        }
        for (; idx < list.size(); idx++) {
            Object child = findChildByName(model, node, list.get(idx));
            if (child == null) return;
            node = child;
            path.add(node);
        }
        TreePath tp = new TreePath(path.toArray());
        tree.setSelectionPath(tp);
        tree.scrollPathToVisible(tp);
    }

    private static Object findChildByName(TreeModel model, Object parent, String name) {
        if (model == null || parent == null || name == null) return null;
        int count = model.getChildCount(parent);
        for (int i = 0; i < count; i++) {
            Object child = model.getChild(parent, i);
            if (child != null && name.equals(String.valueOf(child))) return child;
        }
        return null;
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

public static void putTableCellBounds(Map<String, Object> step, JTable table, int row, int col) {
    if (step == null || table == null || row < 0 || col < 0) return;
    try {
        Rectangle rect = table.getCellRect(row, col, true);
        Point loc = table.getLocationOnScreen();
        if (rect == null || loc == null) return;
        Map<String, Object> bounds = new LinkedHashMap<>();
        bounds.put("x", loc.x + rect.x);
        bounds.put("y", loc.y + rect.y);
        bounds.put("w", rect.width);
        bounds.put("h", rect.height);
        Object metaObj = step.get("meta");
        Map<String, Object> meta = metaObj instanceof Map ? (Map<String, Object>) metaObj : new LinkedHashMap<>();
        meta.put("cellBounds", bounds);
        meta.put("debugBounds", bounds);
        step.put("meta", meta);
    } catch (Exception ignored) { }
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
    if (c instanceof JTextComponent) return false;
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

public static boolean isTableEditorComponent(Component c) {
    if (c == null) return false;
    if (resolveJTable(c) != null) return true;
    String name = c.getName();
    if ("Table.editor".equals(name)) return true;
    Component edit = resolveEditComponent(c);
    return edit != null && "Table.editor".equals(edit.getName());
}

public static JTree resolveJTree(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JTree) return (JTree) p;
    }
    return null;
}

public static JTable resolveJTable(Component c) {
    if (c == null) return null;
    for (Component p = c; p != null; p = p.getParent()) {
        if (p instanceof JTable) return (JTable) p;
    }
    return null;
}

public static void ensureCurrentTableCell(RecordingContext ctx, JTable table, int row, int col, long now, boolean forceResetInitial) {
    if (ctx == null || table == null || row < 0 || col < 0) return;
    boolean same = (ctx.currentTableRef[0] == table && ctx.currentTableRowRef[0] == row && ctx.currentTableColRef[0] == col);
    if (same && !forceResetInitial && ctx.currentTableInitialValueRef[0] != null) return;
    ctx.currentTableRef[0] = table;
    ctx.currentTableRowRef[0] = row;
    ctx.currentTableColRef[0] = col;
    ctx.currentTableColumnNameRef[0] = getTableColumnName(table, col);
    ctx.currentTableInitialValueRef[0] = getTableCellValue(table, row, col);
    ctx.currentTableHadKeyRef[0] = false;
    ctx.currentTableValueChangedRef[0] = false;
    ctx.currentTableEmittedRef[0] = false;
    String[] condCols = getTableConditionColumns(table);
    ctx.currentTableConditionColumnsRef[0] = condCols;
    ctx.currentTableConditionValuesRef[0] = getTableConditionValues(table, row, condCols);
    ctx.lastTableInteractionTimeRef[0] = now;
    attachTableModelListener(table, ctx);
}

public static boolean emitTableSearchAndUpdate(RecordingContext ctx, long timestamp) {
    final String method = "emitTableSearchAndUpdate";
    AgentLogger.begin(LOG, "[" + method + ":L1077] timestamp=" + timestamp);
    try {
        if (ctx == null) {
            AgentLogger.info(LOG, "[" + method + ":L1080] ctx is null, skip");
            return false;
        }
        JTable table = ctx.currentTableRef[0];
        if (table == null) {
            AgentLogger.info(LOG, "[" + method + ":L1084] table is null, skip");
            return false;
        }
        if (ctx.currentTableEmittedRef[0]) {
            AgentLogger.info(LOG, "[" + method + ":L1088] already emitted, skip");
            return false;
        }
        if (!ctx.currentTableHadKeyRef[0] && !ctx.currentTableValueChangedRef[0]) {
            AgentLogger.info(LOG, "[" + method + ":L1092] no key/value change, skip");
            return false;
        }

        String trigger = ctx.currentTableHadKeyRef[0] && ctx.currentTableValueChangedRef[0]
                ? "keyboard+valuechange"
                : (ctx.currentTableHadKeyRef[0] ? "keyboard" : "valuechange");
        int row = ctx.currentTableRowRef[0];
        int col = ctx.currentTableColRef[0];
        int editingCol = -1;
        try {
            editingCol = table.getEditingColumn();
            AgentLogger.info(LOG, "[" + method + ":L1103] editingCol=" + editingCol + ", currentCol=" + col);
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1105] getEditingColumn failed", e);
        }
        if (editingCol >= 0) col = editingCol;
        if (row < 0 || col < 0) {
            AgentLogger.info(LOG, "[" + method + ":L1110] invalid row/col, row=" + row + ", col=" + col);
            return false;
        }

        String targetColumn = getTableColumnHeaderByReflection(table, col);
        if (targetColumn == null || targetColumn.trim().isEmpty()) {
            String trackedColumn = ctx.currentTableColumnNameRef[0];
            if (trackedColumn != null && !trackedColumn.trim().isEmpty()) {
                targetColumn = trackedColumn;
            } else {
                targetColumn = getTableColumnName(table, col);
            }
        }
        if (targetColumn == null || targetColumn.trim().isEmpty()) {
            targetColumn = "Column" + col;
        }
        String source = ctx.currentTableInitialValueRef[0] != null ? ctx.currentTableInitialValueRef[0] : getTableCellValue(table, row, col);
        String target = getTableCellValue(table, row, col);
        String[] condCols = ctx.currentTableConditionColumnsRef[0];
        if (condCols == null) condCols = getTableConditionColumns(table);
        String[] condVals = ctx.currentTableConditionValuesRef[0];
        if (condVals == null) condVals = getTableConditionValues(table, row, condCols);
        String param = buildTableParameter(targetColumn, condCols);
        String data = buildTableSearchAndUpdateData(condVals, source, target);
        AgentLogger.info(LOG, "[" + method + ":L1128] trigger=" + trigger + ", row=" + row + ", col=" + col
                + ", parameter=" + param + ", data=" + data);

        Map<String, Object> step = MarsKeyword.buildScriptStep("SearchAndUpdate", table, param, data, "");
        step.put("event", "searchAndUpdate");
        step.put("timestamp", timestamp);
        putComponentInfo(step, table);
        putTableCellBounds(step, table, row, col);
        step.put("content", data);
        AgentLogger.info(LOG, "[" + method + ":L1138] stepPayload=" + toJson(step));
        String emitMsg = "StepEmit keyword=SearchAndUpdate, event=searchAndUpdate, trigger=" + trigger
                + ", row=" + row + ", col=" + col
                + ", parameter=" + param
                + ", data=" + data;
        AgentLogger.info(LOG, emitMsg);
        appendDebugLog(ctx.outputDir, emitMsg);
        try {
            Writer w = ctx.writerRef.get();
            if (w != null) writeLine(w, step);
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1148] writeLine failed", e);
        }
        WebSocket c = ctx.clientConn.get();
        if (c != null && c.isOpen()) c.send(toJson(step));
        ctx.currentTableEmittedRef[0] = true;
        ctx.currentTableInitialValueRef[0] = target;
        ctx.currentTableHadKeyRef[0] = false;
        ctx.currentTableValueChangedRef[0] = false;
        return true;
    } finally {
        AgentLogger.end(LOG, "[" + method + ":L1158] end");
    }
}

public static String getTableColumnName(JTable table, int col) {
    if (table == null || col < 0) return "";
    String reflected = getTableColumnHeaderByReflection(table, col);
    if (reflected != null && !reflected.trim().isEmpty()) return reflected.trim();
    try {
        String name = table.getColumnName(col);
        if (name != null && !name.trim().isEmpty()) return name.trim();
    } catch (Exception ignored) { }
    try {
        Object header = table.getColumnModel().getColumn(col).getHeaderValue();
        return header != null ? String.valueOf(header) : "";
    } catch (Exception ignored) { }
    return "Column" + col;
}

public static String getTableColumnHeaderByReflection(JTable table, int fallbackCol) {
    final String method = "getTableColumnHeaderByReflection";
    AgentLogger.begin(LOG, "[" + method + ":L1144] table=" + (table == null ? "null" : table.getClass().getName())
            + ", fallbackCol=" + fallbackCol);
    try {
        if (table == null) {
            AgentLogger.info(LOG, "[" + method + ":L1149] table is null, return empty header");
            return "";
        }

        int col = fallbackCol;
        try {
            Method getEditingColumnMethod = table.getClass().getMethod("getEditingColumn");
            Object editingColObj = getEditingColumnMethod.invoke(table);
            AgentLogger.info(LOG, "[" + method + ":L1151] getEditingColumn result=" + editingColObj);
            if (editingColObj instanceof Number) {
                int editingCol = ((Number) editingColObj).intValue();
                if (editingCol >= 0) col = editingCol;
            }
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1154] getEditingColumn reflection failed", e);
        }

        if (col < 0) {
            try {
                col = table.getSelectedColumn();
                AgentLogger.info(LOG, "[" + method + ":L1160] fallback selectedColumn=" + col);
            } catch (Exception e) {
                AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1161] getSelectedColumn failed", e);
            }
        }
        if (col < 0) {
            AgentLogger.info(LOG, "[" + method + ":L1166] no valid column, return empty header");
            return "";
        }

        try {
            AgentLogger.info(LOG, "[" + method + ":L1168] resolve tableHeader/columnModel for col=" + col);
            Method getTableHeaderMethod = table.getClass().getMethod("getTableHeader");
            Object tableHeader = getTableHeaderMethod.invoke(table);
            if (tableHeader == null) {
                AgentLogger.info(LOG, "[" + method + ":L1172] tableHeader is null");
                return "";
            }

            Method getColumnModelMethod = tableHeader.getClass().getMethod("getColumnModel");
            Object columnModel = getColumnModelMethod.invoke(tableHeader);
            if (columnModel == null) {
                AgentLogger.info(LOG, "[" + method + ":L1178] columnModel is null");
                return "";
            }

            Method getColumnCountMethod = columnModel.getClass().getMethod("getColumnCount");
            Object countObj = getColumnCountMethod.invoke(columnModel);
            int count = countObj instanceof Number ? ((Number) countObj).intValue() : -1;
            AgentLogger.info(LOG, "[" + method + ":L1184] columnCount=" + count + ", targetCol=" + col);
            if (count <= 0 || col >= count) return "";

            Method getColumnMethod = columnModel.getClass().getMethod("getColumn", int.class);
            Object tableColumn = getColumnMethod.invoke(columnModel, col);
            if (tableColumn == null) {
                AgentLogger.info(LOG, "[" + method + ":L1188] tableColumn is null");
                return "";
            }

            Method getHeaderValueMethod = tableColumn.getClass().getMethod("getHeaderValue");
            Object headerValue = getHeaderValueMethod.invoke(tableColumn);
            String header = headerValue != null ? String.valueOf(headerValue).trim() : "";
            AgentLogger.info(LOG, "[" + method + ":L1194] headerValue=" + header);
            return header;
        } catch (Exception e) {
            AgentLogger.logException(LOG, Level.WARNING, "[" + method + ":L1197] resolve header by reflection failed", e);
            return "";
        }
    } finally {
        AgentLogger.end(LOG, "[" + method + ":L1201] end");
    }
}

public static String getTableCellValue(JTable table, int row, int col) {
    if (table == null || row < 0 || col < 0) return "";
    try {
        Object v = table.getValueAt(row, col);
        return v != null ? String.valueOf(v) : "";
    } catch (Exception ignored) { }
    return "";
}

public static String[] getTableConditionColumns(JTable table) {
    if (table == null) return new String[0];
    Object v = table.getClientProperty("mars.keyColumns");
    if (v == null) v = table.getClientProperty("mars.conditionColumns");
    if (v instanceof String) {
        String s = ((String) v).trim();
        if (s.isEmpty()) return new String[0];
        String[] parts = s.split("[;,]");
        java.util.List<String> list = new ArrayList<>();
        for (String p : parts) {
            String t = p != null ? p.trim() : "";
            if (!t.isEmpty()) list.add(t);
        }
        return list.toArray(new String[0]);
    }
    if (v instanceof String[]) return (String[]) v;
    if (v instanceof java.util.List) {
        java.util.List<?> list = (java.util.List<?>) v;
        java.util.List<String> out = new ArrayList<>();
        for (Object o : list) {
            if (o == null) continue;
            String t = String.valueOf(o).trim();
            if (!t.isEmpty()) out.add(t);
        }
        return out.toArray(new String[0]);
    }
    return new String[0];
}

public static String[] getTableConditionValues(JTable table, int row, String[] condCols) {
    if (table == null || row < 0 || condCols == null || condCols.length == 0) return new String[0];
    java.util.List<String> values = new ArrayList<>();
    for (String colName : condCols) {
        int idx = findTableColumnIndexByName(table, colName);
        if (idx < 0) continue;
        values.add(getTableCellValue(table, row, idx));
    }
    return values.toArray(new String[0]);
}

private static int findTableColumnIndexByName(JTable table, String name) {
    if (table == null || name == null) return -1;
    String n = name.trim();
    if (n.isEmpty()) return -1;
    int count = table.getColumnCount();
    for (int i = 0; i < count; i++) {
        String colName = "";
        try { colName = table.getColumnName(i); } catch (Exception ignored) { }
        if (n.equals(colName)) return i;
        if (!colName.isEmpty() && n.equalsIgnoreCase(colName)) return i;
        try {
            Object header = table.getColumnModel().getColumn(i).getHeaderValue();
            if (header != null) {
                String hv = String.valueOf(header);
                if (n.equals(hv) || n.equalsIgnoreCase(hv)) return i;
            }
        } catch (Exception ignored) { }
    }
    return -1;
}

public static String buildTableParameter(String targetColumn, String[] condCols) {
    String base = targetColumn != null ? targetColumn : "";
    if (condCols == null || condCols.length == 0) return base;
    String cond = String.join(";", condCols);
    return base + ";[" + cond + "]";
}

public static String buildTableDataWithConditions(String[] condVals, String baseData) {
    String prefix = "";
    if (condVals != null && condVals.length > 0) {
        prefix = "[" + String.join(";", condVals) + "]";
    }
    String data = baseData != null ? baseData : "";
    return prefix + data;
}

public static String buildTableSearchAndUpdateData(String[] condVals, String source, String target) {
    String src = source != null ? source : "";
    String tgt = target != null ? target : "";
    return buildTableDataWithConditions(condVals, src + ":" + tgt);
}

private static void attachTableModelListener(JTable table, RecordingContext ctx) {
    if (table == null || ctx == null) return;
    final String key = "mars.tableModelListenerAttached";
    Object existing = table.getClientProperty(key);
    if (existing instanceof TableModelListener) return;
    TableModelListener listener = e -> {
        if (!ctx.recording[0]) return;
        if (e.getType() != TableModelEvent.UPDATE) return;
        int row = e.getFirstRow();
        int col = e.getColumn();
        if (row < 0 || col < 0) return;
        long now = System.currentTimeMillis();
        long lastInteract = ctx.lastTableInteractionTimeRef[0];
        if (lastInteract > 0 && (now - lastInteract) > 5000) return;
        if (ctx.currentTableRef[0] == table && ctx.currentTableRowRef[0] == row && ctx.currentTableColRef[0] == col) {
            ctx.currentTableValueChangedRef[0] = true;
        }
    };
    table.getModel().addTableModelListener(listener);
    table.putClientProperty(key, listener);
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

