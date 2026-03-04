package com.mars.javaui.fx;

import java.io.OutputStreamWriter;
import java.io.Writer;
import java.lang.reflect.InvocationHandler;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicReference;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

/**
 * JavaFX recording hooks and step emission (isolated from Swing/AWT).
 * All logic for attaching/detaching FX event filters and mapping events to steps lives here.
 */
public final class FxRecordSupport {

    private static final Logger LOG = Logger.getLogger(FxRecordSupport.class.getName());

    private FxRecordSupport() {}

    /** Hook holder for one scene filter (scene + eventType + handler). */
    public static final class FxFilterHook {
        public final Object scene;
        public final Object eventType;
        public final Object handler;

        public FxFilterHook(Object scene, Object eventType, Object handler) {
            this.scene = scene;
            this.eventType = eventType;
            this.handler = handler;
        }
    }

    /** Holder for focus listener (scene + listener) for table cell focus-lost → SearchAndUpdate. */
    public static final class FxFocusHook {
        public final Object scene;
        public final Object property;
        public final Object listener;

        public FxFocusHook(Object scene, Object property, Object listener) {
            this.scene = scene;
            this.property = property;
            this.listener = listener;
        }
    }

    /** Info for a TableView cell: table + row + column index. */
    private static final class TableCellInfo {
        final Object tableView;
        final int row;
        final int col;

        TableCellInfo(Object tableView, int row, int col) {
            this.tableView = tableView;
            this.row = row;
            this.col = col;
        }
    }

    // Context for recording SearchAndUpdate when focus leaves the cell (left-click in cell only).
    private static volatile Object fxTableContextTableView;
    private static volatile int fxTableContextRow = -1;
    private static volatile int fxTableContextCol = -1;
    private static volatile List<String> fxTableContextConditionColumns;
    private static volatile List<String> fxTableContextConditionValues;

    /** Sends a recorded step (e.g. to file + WebSocket). Implemented by RecordAgent. */
    public interface FxStepSender {
        void sendStep(Map<String, Object> step);
    }

    private static volatile FxSemanticTrackingConfig semanticTrackingConfig;

    private static FxSemanticTrackingConfig getSemanticTrackingConfig() {
        FxSemanticTrackingConfig c = semanticTrackingConfig;
        if (c == null) {
            synchronized (FxRecordSupport.class) {
                c = semanticTrackingConfig;
                if (c == null) {
                    String path = System.getProperty("mars.fx.semantic.config");
                    c = FxSemanticTrackingConfig.load(path);
                    semanticTrackingConfig = c;
                }
            }
        }
        return c;
    }

    public static void attachJavaFxRecordHooks(
            boolean[] recording,
            AtomicReference<OutputStreamWriter> writerRef,
            AtomicReference<?> clientConnRef,
            AtomicReference<List<FxFilterHook>> fxHooksRef,
            AtomicReference<List<FxFocusHook>> fxFocusHooksRef,
            FxStepSender stepSender) {
        try {
            Class<?> platformClz = Class.forName("javafx.application.Platform");
            Method isFxThread = platformClz.getMethod("isFxApplicationThread");
            Method runLater = platformClz.getMethod("runLater", Runnable.class);
            Class<?> windowClz = Class.forName("javafx.stage.Window");
            Class<?> sceneClz = Class.forName("javafx.scene.Scene");
            Class<?> eventHandlerClz = Class.forName("javafx.event.EventHandler");
            Class<?> mouseEventClz = Class.forName("javafx.scene.input.MouseEvent");
            Class<?> keyEventClz = Class.forName("javafx.scene.input.KeyEvent");
            Method addEventFilter = sceneClz.getMethod("addEventFilter", Class.forName("javafx.event.EventType"), eventHandlerClz);

            Object mouseClickedType = mouseEventClz.getField("MOUSE_CLICKED").get(null);
            Object keyPressedType = keyEventClz.getField("KEY_PRESSED").get(null);
            Object keyReleasedType = keyEventClz.getField("KEY_RELEASED").get(null);

            List<FxFilterHook> hooks = new ArrayList<>();
            List<FxFocusHook> focusHooks = new ArrayList<>();
            Runnable register = () -> {
                try {
                    Object winsObj = windowClz.getMethod("getWindows").invoke(null);
                    if (!(winsObj instanceof Iterable<?>)) return;
                    for (Object win : (Iterable<?>) winsObj) {
                        if (win == null) continue;
                        Object showingObj = invokeNoArg(win, "isShowing");
                        if (!(showingObj instanceof Boolean) || !((Boolean) showingObj)) continue;
                        Object scene = invokeNoArg(win, "getScene");
                        if (scene == null) continue;
                        // Defer handling to runLater so the event filter returns immediately: Tab/focus and
                        // other default behavior run in the same dispatch; recording runs next pulse.
                        Object mouseHandler = createHandlerProxy(event -> {
                            if (!recording[0]) return;
                            try {
                                runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                            } catch (Exception e) {
                                LOG.log(Level.WARNING, "FX runLater (mouse) failed", e);
                            }
                        });
                        addEventFilter.invoke(scene, mouseClickedType, mouseHandler);
                        hooks.add(new FxFilterHook(scene, mouseClickedType, mouseHandler));

                        Object keyPressedHandler = createHandlerProxy(event -> {
                            if (!recording[0]) return;
                            try {
                                runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                            } catch (Exception e) {
                                LOG.log(Level.WARNING, "FX runLater (keyPressed) failed", e);
                            }
                        });
                        addEventFilter.invoke(scene, keyPressedType, keyPressedHandler);
                        hooks.add(new FxFilterHook(scene, keyPressedType, keyPressedHandler));

                        Object keyHandler = createHandlerProxy(event -> {
                            if (!recording[0]) return;
                            try {
                                runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                            } catch (Exception e) {
                                LOG.log(Level.WARNING, "FX runLater (keyReleased) failed", e);
                            }
                        });
                        addEventFilter.invoke(scene, keyReleasedType, keyHandler);
                        hooks.add(new FxFilterHook(scene, keyReleasedType, keyHandler));

                        // Focus listener: when focus leaves a table cell we recorded, emit SearchAndUpdate
                        try {
                            Method focusOwnerProp = sceneClz.getMethod("focusOwnerProperty");
                            Object focusProp = focusOwnerProp.invoke(scene);
                            if (focusProp != null) {
                                Object focusListener = createFocusChangeListenerProxy((oldOwner, newOwner) -> {
                                    if (!recording[0]) return;
                                    try {
                                        runLater.invoke(null, (Runnable) () -> onJavaFxFocusChange(oldOwner, newOwner, stepSender));
                                    } catch (Exception e) {
                                        LOG.log(Level.WARNING, "FX runLater (focus) failed", e);
                                    }
                                });
                                Class<?> changeListenerClz = Class.forName("javafx.beans.value.ChangeListener");
                                Method addListener = focusProp.getClass().getMethod("addListener", changeListenerClz);
                                addListener.invoke(focusProp, focusListener);
                                focusHooks.add(new FxFocusHook(scene, focusProp, focusListener));
                            }
                        } catch (Exception e) {
                            LOG.log(Level.FINE, "FX focusOwnerProperty/addListener not available", e);
                        }
                    }
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "attachJavaFxRecordHooks register.run failed", e);
                }
            };

            boolean fxThread = Boolean.TRUE.equals(isFxThread.invoke(null));
            if (fxThread) {
                register.run();
            } else {
                CountDownLatch latch = new CountDownLatch(1);
                runLater.invoke(null, (Runnable) () -> {
                    try {
                        register.run();
                    } finally {
                        latch.countDown();
                    }
                });
                latch.await(1500, TimeUnit.MILLISECONDS);
            }
            fxHooksRef.set(hooks);
            if (fxFocusHooksRef != null) fxFocusHooksRef.set(focusHooks);
        } catch (ClassNotFoundException e) {
            LOG.log(Level.FINE, "JavaFX not present, skip FX record hooks", e);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "attachJavaFxRecordHooks failed", e);
        }
    }

    public static void detachJavaFxRecordHooks(List<FxFilterHook> hooks) {
        if (hooks == null || hooks.isEmpty()) return;
        try {
            Class<?> platformClz = Class.forName("javafx.application.Platform");
            Method isFxThread = platformClz.getMethod("isFxApplicationThread");
            Method runLater = platformClz.getMethod("runLater", Runnable.class);
            Class<?> sceneClz = Class.forName("javafx.scene.Scene");
            Method removeEventFilter = sceneClz.getMethod("removeEventFilter", Class.forName("javafx.event.EventType"), Class.forName("javafx.event.EventHandler"));
            Runnable remove = () -> {
                for (FxFilterHook h : hooks) {
                    try {
                        removeEventFilter.invoke(h.scene, h.eventType, h.handler);
                    } catch (Exception e) {
                        LOG.log(Level.WARNING, "detachJavaFxRecordHooks removeEventFilter failed for scene=" + (h.scene != null ? h.scene.getClass().getName() : "null"), e);
                    }
                }
            };
            boolean fxThread = Boolean.TRUE.equals(isFxThread.invoke(null));
            if (fxThread) {
                remove.run();
            } else {
                runLater.invoke(null, remove);
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "detachJavaFxRecordHooks failed", e);
        }
    }

    public static void detachJavaFxFocusHooks(List<FxFocusHook> focusHooks) {
        if (focusHooks == null || focusHooks.isEmpty()) return;
        clearFxTableContext();
        try {
            Class<?> platformClz = Class.forName("javafx.application.Platform");
            Method isFxThread = platformClz.getMethod("isFxApplicationThread");
            Method runLater = platformClz.getMethod("runLater", Runnable.class);
            Runnable remove = () -> {
                for (FxFocusHook h : focusHooks) {
                    try {
                        if (h.property != null && h.listener != null) {
                            Method removeListener = h.property.getClass().getMethod("removeListener", Class.forName("javafx.beans.value.ChangeListener"));
                            removeListener.invoke(h.property, h.listener);
                        }
                    } catch (Exception e) {
                        LOG.log(Level.WARNING, "detachJavaFxFocusHooks removeListener failed", e);
                    }
                }
            };
            boolean fxThread = Boolean.TRUE.equals(isFxThread.invoke(null));
            if (fxThread) {
                remove.run();
            } else {
                runLater.invoke(null, remove);
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "detachJavaFxFocusHooks failed", e);
        }
    }

    private interface EventConsumer {
        void onEvent(Object event);
    }

    private static Object createHandlerProxy(EventConsumer consumer) throws Exception {
        Class<?> eventHandlerClz = Class.forName("javafx.event.EventHandler");
        InvocationHandler ih = (proxy, method, args) -> {
            if ("handle".equals(method.getName()) && args != null && args.length == 1) {
                try {
                    consumer.onEvent(args[0]);
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "FX event handler onEvent failed", e);
                }
                return null;
            }
            return null;
        };
        return Proxy.newProxyInstance(
                FxRecordSupport.class.getClassLoader(),
                new Class<?>[]{eventHandlerClz},
                ih
        );
    }

    private interface FocusChangeConsumer {
        void onFocusChange(Object oldOwner, Object newOwner);
    }

    private static Object createFocusChangeListenerProxy(FocusChangeConsumer consumer) throws Exception {
        Class<?> changeListenerClz = Class.forName("javafx.beans.value.ChangeListener");
        InvocationHandler ih = (proxy, method, args) -> {
            if ("changed".equals(method.getName()) && args != null && args.length >= 3) {
                try {
                    consumer.onFocusChange(args[1], args[2]);
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "FX focus listener failed", e);
                }
                return null;
            }
            return null;
        };
        return Proxy.newProxyInstance(
                FxRecordSupport.class.getClassLoader(),
                new Class<?>[]{changeListenerClz},
                ih
        );
    }

    /** Called on FX thread when focus changes; emit SearchAndUpdate if focus left our tracked table cell. */
    private static void onJavaFxFocusChange(Object oldOwner, Object newOwner, FxStepSender stepSender) {
        Object table = fxTableContextTableView;
        if (table == null || oldOwner == null || stepSender == null) return;
        TableCellInfo cell = getTableCellInfoFromNode(oldOwner);
        if (cell == null || cell.tableView != table || cell.row != fxTableContextRow || cell.col != fxTableContextCol) return;
        List<String> condCols = fxTableContextConditionColumns;
        List<String> condVals = fxTableContextConditionValues;
        if (condCols == null || condVals == null || condCols.size() != condVals.size()) return;
        String targetValue = getFxTableCellValue(table, fxTableContextRow, fxTableContextCol);
        if (targetValue == null) targetValue = "";
        String param = buildFxTableParameter(condCols, fxTableContextCol);
        String data = buildFxTableSearchAndUpdateData(condVals, targetValue);
        Map<String, Object> step = new LinkedHashMap<>();
        step.put("keyword", "SearchAndUpdate");
        step.put("event", "searchAndUpdate");
        step.put("timestamp", System.currentTimeMillis());
        step.put("parameter", param);
        step.put("data", data);
        step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(table, null));
        step.put("objectIdentifier", buildJavaFxObjectIdentifier(table));
        step.put("objectCategory", "javaFxTable");
        step.put("semanticType", "TABLEVIEW");
        stepSender.sendStep(step);
        clearFxTableContext();
    }

    private static void clearFxTableContext() {
        fxTableContextTableView = null;
        fxTableContextRow = -1;
        fxTableContextCol = -1;
        fxTableContextConditionColumns = null;
        fxTableContextConditionValues = null;
    }

    /**
     * Build step from foldAndLift result: semanticTarget + semanticParent.
     * Structural containers are folded; parts hang under composite boundary.
     */
    private static void handleJavaFxRecordEvent(Object event, FxStepSender stepSender) {
        if (event == null || stepSender == null) return;
        String keyword = null;
        Object stepTarget = null;
        try {
            String eventTypeName = String.valueOf(invokeNoArg(event, "getEventType"));
            Object target = invokeNoArg(event, "getTarget");
            if (target == null) return;

            if (LOG.isLoggable(Level.INFO)) {
                LOG.info("[FxRecord] eventType=" + eventTypeName + " | target(sender)=" + describeNodeForLog(target));
            }

            int maxHops = getSemanticTrackingConfig().getMaxAncestorHops();
            FxNodeCategory.LiftResult lr = FxNodeClassifier.foldAndLift(target, maxHops);
            Object semanticTarget = lr.semanticTarget;
            Object semanticParent = lr.semanticParent;

            if (LOG.isLoggable(Level.INFO)) {
                if (lr.chainFoldedNodes != null && !lr.chainFoldedNodes.isEmpty()) {
                    StringBuilder folded = new StringBuilder();
                    for (int i = 0; i < lr.chainFoldedNodes.size(); i++) {
                        Object n = lr.chainFoldedNodes.get(i);
                        if (i > 0) folded.append(" <- ");
                        folded.append(describeNodeForLog(n));
                    }
                    LOG.info("[FxRecord] foldAndLift foldedChain(" + lr.chainFoldedNodes.size() + ")= " + folded);
                }
                LOG.info("[FxRecord] foldAndLift semanticTarget=" + (semanticTarget != null ? describeNodeForLog(semanticTarget) : "null"));
                LOG.info("[FxRecord] foldAndLift semanticParent=" + (semanticParent != null ? describeNodeForLog(semanticParent) : "null"));
                if (semanticTarget != null) {
                    FxNodeCategory.NodeMeta meta = FxNodeClassifier.classify(semanticTarget);
                    if (meta != null) {
                        LOG.info("[FxRecord] semanticTarget meta: category=" + meta.category + " boundary=" + meta.boundary + " semanticType=" + meta.semanticType);
                    }
                }
            }

            if (semanticTarget == null) return;

            FxNodeCategory.NodeMeta targetMeta = FxNodeClassifier.classify(semanticTarget);
            String semanticType = targetMeta != null ? targetMeta.semanticType : "UNKNOWN";

            // Table as whole semantic object: SearchAndUpdate / SearchAndClick (parent = top window, object = TableView)
            if ("TABLECELL".equals(semanticType) || "TABLEVIEW".equals(semanticType)) {
                TableCellInfo cellInfo = getTableCellInfoFromNode(target);
                    if (cellInfo != null && eventTypeName.contains("MOUSE_CLICKED")) {
                    Object tableView = cellInfo.tableView;
                    List<String> condCols = getFxTableColumnNames(tableView);
                    if (condCols != null && !condCols.isEmpty() && cellInfo.row >= 0 && cellInfo.col >= 0 && cellInfo.col < condCols.size()) {
                        List<String> condVals = getFxTableRowValues(tableView, cellInfo.row, condCols.size());
                        String param = buildFxTableParameter(condCols, cellInfo.col);
                        boolean rightClick = isSecondaryMouseButton(event);
                        if (rightClick) {
                            String dataClick = buildFxTableSearchAndClickData(condVals, "Action:RightClick");
                            Map<String, Object> step = new LinkedHashMap<>();
                            step.put("keyword", "SearchAndClick");
                            step.put("event", "searchAndClick");
                            step.put("timestamp", System.currentTimeMillis());
                            step.put("parameter", param);
                            step.put("data", dataClick);
                            step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(tableView, null));
                            step.put("objectIdentifier", buildJavaFxObjectIdentifier(tableView));
                            step.put("objectCategory", "javaFxTable");
                            step.put("semanticType", "TABLEVIEW");
                            stepSender.sendStep(step);
                            return;
                        }
                        // Left-click: store context; SearchAndUpdate will be emitted when cell loses focus
                        fxTableContextTableView = tableView;
                        fxTableContextRow = cellInfo.row;
                        fxTableContextCol = cellInfo.col;
                        fxTableContextConditionColumns = new ArrayList<>(condCols);
                        fxTableContextConditionValues = condVals != null ? new ArrayList<>(condVals) : new ArrayList<>();
                        return;
                    }
                }
                // Fall through to ClickButton if not a valid table cell or not mouse click
            }

            String data = "";
            if (eventTypeName.contains("MOUSE_CLICKED")) {
                if ("TEXT_INPUT".equals(semanticType) || isInsideTextInput(target) || isInsideTextInput(semanticTarget)) {
                    return;
                }
                keyword = keywordForMouseClick(semanticTarget, semanticType);
                data = dataForMouseClick(semanticTarget, semanticType, keyword);
            } else if (eventTypeName.contains("KEY_PRESSED")) {
                String code = String.valueOf(invokeNoArg(event, "getCode"));

                // Case 1: In table context (TABLECELL/TABLEVIEW) and user presses Enter → emit SearchAndUpdate immediately.
                if ("ENTER".equalsIgnoreCase(code)
                        && ("TABLECELL".equals(semanticType) || "TABLEVIEW".equals(semanticType))) {
                    TableCellInfo cellInfo = getTableCellInfoFromNode(target);
                    if (cellInfo != null) {
                        Object tableView = cellInfo.tableView;
                        List<String> condCols = getFxTableColumnNames(tableView);
                        if (condCols != null && !condCols.isEmpty()
                                && cellInfo.row >= 0
                                && cellInfo.col >= 0
                                && cellInfo.col < condCols.size()) {
                            List<String> condVals = getFxTableRowValues(tableView, cellInfo.row, condCols.size());
                            String param = buildFxTableParameter(condCols, cellInfo.col);
                            String targetValue = getFxTableCellValue(tableView, cellInfo.row, cellInfo.col);
                            if (targetValue == null) targetValue = "";
                            String dataSU = buildFxTableSearchAndUpdateData(condVals, targetValue);

                            Map<String, Object> step = new LinkedHashMap<>();
                            step.put("keyword", "SearchAndUpdate");
                            step.put("event", "searchAndUpdate");
                            step.put("timestamp", System.currentTimeMillis());
                            step.put("parameter", param);
                            step.put("data", dataSU);
                            step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(tableView, null));
                            step.put("objectIdentifier", buildJavaFxObjectIdentifier(tableView));
                            step.put("objectCategory", "javaFxTable");
                            step.put("semanticType", "TABLEVIEW");
                            stepSender.sendStep(step);

                            // Avoid emitting a second SearchAndUpdate on focus-lost with stale context.
                            clearFxTableContext();
                            return;
                        }
                    }
                }

                // Case 2: Simple text input control (semantic TEXT_INPUT) → FillEdit on Enter/Tab.
                if (("ENTER".equalsIgnoreCase(code) || "TAB".equalsIgnoreCase(code))
                        && "TEXT_INPUT".equals(semanticType)) {
                    keyword = "FillEdit";
                    String text = asString(invokeNoArg(target, "getText"));
                    data = text != null ? text : "";
                }
            }
            if (keyword == null) return;

            stepTarget = "FillEdit".equals(keyword) ? target : semanticTarget;
            Object stepParent = "FillEdit".equals(keyword) ? null : semanticParent;

            Map<String, Object> step = new LinkedHashMap<>();
            step.put("keyword", keyword);
            step.put("event", keyword);
            step.put("timestamp", System.currentTimeMillis());
            if (data != null && !data.isEmpty()) step.put("data", data);
            step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(stepTarget, stepParent));
            step.put("objectIdentifier", buildJavaFxObjectIdentifier(stepTarget));
            if (semanticType != null && !semanticType.isEmpty()) step.put("semanticType", semanticType);
            stepSender.sendStep(step);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "emitStep failed: keyword=" + keyword + ", target=" + (stepTarget != null ? stepTarget.getClass().getName() : "null"), e);
        }
    }

    /** Use java.util.List interface for reflection to avoid touching com.sun.javafx.collections (not exported by javafx.base). */
    private static Object getFromObservableList(Object list, int idx) {
        if (list == null) return null;
        try {
            java.lang.reflect.Method getMethod = java.util.List.class.getMethod("get", int.class);
            return getMethod.invoke(list, idx);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "getFromObservableList failed: list=" + list.getClass().getName() + ", idx=" + idx, e);
            return null;
        }
    }

    /** Use java.util.List interface for reflection to avoid touching com.sun.javafx.collections (not exported by javafx.base). */
    private static int indexOfInObservableList(Object list, Object item) {
        if (list == null) return -1;
        try {
            java.lang.reflect.Method m = java.util.List.class.getMethod("indexOf", Object.class);
            Object r = m.invoke(list, item);
            if (r instanceof Number) return ((Number) r).intValue();
        } catch (Exception e) {
            LOG.log(Level.FINE, "indexOfInObservableList indexOf failed, will try linear scan", e);
        }
        try {
            java.lang.reflect.Method sizeM = java.util.List.class.getMethod("size");
            Object sz = sizeM.invoke(list);
            int n = (sz instanceof Number) ? ((Number) sz).intValue() : -1;
            if (n <= 0) return -1;
            java.lang.reflect.Method getM = java.util.List.class.getMethod("get", int.class);
            for (int i = 0; i < n; i++) {
                Object o = getM.invoke(list, i);
                if (o == item || (o != null && o.equals(item))) return i;
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "indexOfInObservableList linear scan failed", e);
        }
        return -1;
    }

    /** True if node is a TextField/TextArea/PasswordField or is inside one (so we do not emit ClickButton for clicks in edit). */
    private static boolean isInsideTextInput(Object node) {
        Object cur = node;
        while (cur != null) {
            String cn = cur.getClass().getName();
            if (cn.contains("TextField") || cn.contains("TextArea") || cn.contains("PasswordField")) return true;
            cur = FxNodeClassifier.parentOf(cur);
        }
        return false;
    }

    private static String keywordForMouseClick(Object control, String semanticType) {
        if ("CHECKBOX".equals(semanticType)) return "SetCheckBox";
        if ("RADIOBUTTON".equals(semanticType)) return "SetRadioBox";
        if ("TREECELL".equals(semanticType) || "TREEVIEW".equals(semanticType)) return "SelectTreeList";
        if ("MENUITEM".equals(semanticType)) return "SelectMenuItem";
        if ("INPUT_CONTROL".equals(semanticType)) {
            String cn = control.getClass().getName();
            if (cn.contains("ComboBox") || cn.contains("ChoiceBox")) return "SelectDropList";
        }
        if ("TABLECELL".equals(semanticType) || "TABLEVIEW".equals(semanticType)) return "ClickButton";
        if ("LISTCELL".equals(semanticType) || "LISTVIEW".equals(semanticType)) return "ClickButton";
        if ("TAB".equals(semanticType) || "TABPANE".equals(semanticType)) return "SelectTab";
        if ("COLUMN_HEADER".equals(semanticType) || "BUTTON".equals(semanticType)
                || "DECORATION_AS_CONTROL".equals(semanticType) || "CUSTOM_INTERACTIVE".equals(semanticType))
            return "ClickButton";
        return "ClickButton";
    }

    private static String dataForMouseClick(Object control, String semanticType, String keyword) {
        if ("SetCheckBox".equals(keyword)) {
            Object selected = invokeNoArg(control, "isSelected");
            return String.valueOf(Boolean.TRUE.equals(selected));
        }
        if ("SetRadioBox".equals(keyword)) {
            String text = asString(invokeNoArg(control, "getText"));
            return text != null ? text : "";
        }
        if ("SelectTreeList".equals(keyword)) {
            String text = asString(invokeNoArg(control, "getText"));
            return text != null ? text : "";
        }
        if ("SelectMenuItem".equals(keyword)) {
            String text = asString(invokeNoArg(control, "getText"));
            return text != null ? text : "";
        }
        if ("SelectDropList".equals(keyword)) {
            String value = asString(invokeNoArg(control, "getValue"));
            return value != null ? value : "";
        }
        if ("SelectTab".equals(keyword)) {

            // 0) Fast path: if control itself has getText and returns non-empty (works for Tab / Label sometimes)
            try {
                String text = asString(invokeNoArg(control, "getText"));
                if (text != null && !text.isEmpty()) return text;
            } catch (Exception e) {
                LOG.log(Level.FINE, "SelectTab fast path getText failed: control=" + (control != null ? control.getClass().getName() : "null"), e);
            }
        
            try {
                // 1) Best path for injected agent: control is a Skin (e.g., TabPaneSkin$TabHeaderSkin)
                // Skin.getSkinnable() -> Tab
                Object tab = null;
        
                // 1.1 If control is Skin: try getSkinnable()
                try {
                    Object skinnable = invokeNoArg(control, "getSkinnable");
                    if (skinnable != null && skinnable.getClass().getName().contains("javafx.scene.control.Tab")) {
                        tab = skinnable;
                    }
                } catch (Exception e) {
                    LOG.log(Level.FINE, "SelectTab getSkinnable failed: control=" + (control != null ? control.getClass().getName() : "null"), e);
                }
        
                // 1.2 If control already looks like a Tab, accept it
                if (tab == null) {
                    String cn = control != null ? control.getClass().getName() : "";
                    if (cn.contains("javafx.scene.control.Tab")) {
                        tab = control;
                    }
                }
        
                // 1.3 If we got Tab: return Tab.getText(), fallback to index in its TabPane
                if (tab != null) {
                    String tabText = asString(invokeNoArg(tab, "getText"));
                    if (tabText != null && !tabText.isEmpty()) return tabText;
        
                    // fallback: return index under its TabPane (stable enough)
                    Object pane = invokeNoArg(tab, "getTabPane");
                    if (pane != null) {
                        Object tabs = invokeNoArg(pane, "getTabs");
                        if (tabs != null) {
                            int idx = indexOfInObservableList(tabs, tab); // implement helper below
                            if (idx >= 0) return String.valueOf(idx);
                        }
                    }
                    return "";
                }
        
                // 2) Fallback path: climb parents (only if control is Node-like). Your parentOf() must handle non-Node safely.
                Object pane = null;
                Object cur = control;
                while (cur != null) {
                    String name = cur.getClass().getName();
                    if (name.contains("TabPane") && !name.contains("TabPaneSkin")) {
                        pane = cur;
                        break;
                    }
                    cur = FxNodeClassifier.parentOf(cur);
                }
        
                if (pane != null) {
                    // IMPORTANT: don't rely only on selectedIndex for "click on a specific tab header"
                    // But if we cannot map header->tab, selectedIndex is last resort.
                    Object selModel = invokeNoArg(pane, "getSelectionModel");
                    Object selectedIndex = selModel != null ? invokeNoArg(selModel, "getSelectedIndex") : null;
                    if (selectedIndex instanceof Number) {
                        int idx = ((Number) selectedIndex).intValue();
        
                        Object tabs = invokeNoArg(pane, "getTabs");
                        if (tabs != null && idx >= 0) {
                            Object tab2 = getFromObservableList(tabs, idx);
                            if (tab2 != null) {
                                String tabText2 = asString(invokeNoArg(tab2, "getText"));
                                if (tabText2 != null && !tabText2.isEmpty()) return tabText2;
                            }
                        }
                        return String.valueOf(idx);
                    }
                }
        
            } catch (Exception e) {
                LOG.log(Level.WARNING, "SelectTab dataForMouseClick failed (getTabs/getSelectionModel/getFromObservableList): control=" + (control != null ? control.getClass().getName() : "null"), e);
            }
        
            return "";
        }
        return "";
    }

    private static boolean isSecondaryMouseButton(Object mouseEvent) {
        if (mouseEvent == null) return false;
        try {
            Object button = invokeNoArg(mouseEvent, "getButton");
            if (button != null) {
                String name = button.getClass().getName();
                if (name.contains("SECONDARY") || "SECONDARY".equals(String.valueOf(button))) return true;
                Method ordinal = button.getClass().getMethod("ordinal");
                Object ord = ordinal.invoke(button);
                if (ord instanceof Number && ((Number) ord).intValue() == 2) return true;
            }
        } catch (Exception e) {
            LOG.log(Level.FINE, "isSecondaryMouseButton failed", e);
        }
        return false;
    }

    /** Find TableCell ancestor and return (TableView, row, col) or null. */
    private static TableCellInfo getTableCellInfoFromNode(Object node) {
        Object cur = node;
        while (cur != null) {
            String cn = cur.getClass().getName();
            if (cn.contains("TableCell") && cn.contains("javafx")) {
                Object tableView = invokeNoArg(cur, "getTableView");
                if (tableView == null) return null;
                Object tableRow = invokeNoArg(cur, "getTableRow");
                int row = -1;
                if (tableRow != null) {
                    Object idx = invokeNoArg(tableRow, "getIndex");
                    if (idx instanceof Number) row = ((Number) idx).intValue();
                }
                Object tableColumn = invokeNoArg(cur, "getTableColumn");
                int col = -1;
                if (tableColumn != null && tableView != null) {
                    Object columns = invokeNoArg(tableView, "getColumns");
                    if (columns != null) col = indexOfInObservableList(columns, tableColumn);
                }
                if (row >= 0 && col >= 0) return new TableCellInfo(tableView, row, col);
                return null;
            }
            cur = FxNodeClassifier.parentOf(cur);
        }
        return null;
    }

    /** Get column names (header text or id) left to right. */
    private static List<String> getFxTableColumnNames(Object tableView) {
        if (tableView == null) return null;
        try {
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return null;
            int n = sizeOfObservableList(columns);
            List<String> names = new ArrayList<>(n);
            for (int i = 0; i < n; i++) {
                Object col = getFromObservableList(columns, i);
                if (col == null) { names.add("Column" + i); continue; }
                String text = asString(invokeNoArg(col, "getText"));
                if (text != null && !text.isEmpty()) { names.add(text); continue; }
                Object header = invokeNoArg(col, "getGraphic"); // some use setGraphic(Label)
                if (header != null) {
                    String t = asString(invokeNoArg(header, "getText"));
                    if (t != null && !t.isEmpty()) { names.add(t); continue; }
                }
                try {
                    Method getCellValue = col.getClass().getMethod("getCellObservableValue", Object.class);
                    Object rowItem = getFirstTableViewItem(tableView);
                    if (rowItem != null) {
                        Object obs = getCellValue.invoke(col, rowItem);
                        if (obs != null) {
                            Object v = invokeNoArg(obs, "getValue");
                            names.add(v != null ? String.valueOf(v) : "Column" + i);
                            continue;
                        }
                    }
                } catch (Exception ignored) { }
                String id = asString(invokeNoArg(col, "getId"));
                names.add(id != null && !id.isEmpty() ? id : "Column" + i);
            }
            return names;
        } catch (Exception e) {
            LOG.log(Level.WARNING, "getFxTableColumnNames failed", e);
            return null;
        }
    }

    private static Object getFirstTableViewItem(Object tableView) {
        if (tableView == null) return null;
        Object items = invokeNoArg(tableView, "getItems");
        if (items == null) return null;
        int n = sizeOfObservableList(items);
        return n > 0 ? getFromObservableList(items, 0) : null;
    }

    private static int sizeOfObservableList(Object list) {
        if (list == null) return 0;
        try {
            Method sizeM = java.util.List.class.getMethod("size");
            Object sz = sizeM.invoke(list);
            return (sz instanceof Number) ? ((Number) sz).intValue() : 0;
        } catch (Exception e) { return 0; }
    }

    /** Get cell values for a row, one per column (left to right). */
    private static List<String> getFxTableRowValues(Object tableView, int row, int columnCount) {
        if (tableView == null || row < 0) return null;
        List<String> values = new ArrayList<>(columnCount);
        try {
            Object items = invokeNoArg(tableView, "getItems");
            if (items == null) return new ArrayList<>();
            int itemCount = sizeOfObservableList(items);
            if (row >= itemCount) return new ArrayList<>();
            Object rowItem = getFromObservableList(items, row);
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return new ArrayList<>();
            for (int i = 0; i < columnCount; i++) {
                Object col = getFromObservableList(columns, i);
                if (col == null) { values.add(""); continue; }
                String val = getFxTableCellValueFromColumn(tableView, col, rowItem, row);
                values.add(val != null ? val : "");
            }
            return values;
        } catch (Exception e) {
            LOG.log(Level.WARNING, "getFxTableRowValues failed", e);
            return new ArrayList<>();
        }
    }

    private static String getFxTableCellValueFromColumn(Object tableView, Object tableColumn, Object rowItem, int row) {
        if (tableColumn == null) return "";
        try {
            Method getCellObs = tableColumn.getClass().getMethod("getCellObservableValue", Object.class);
            Object obs = getCellObs.invoke(tableColumn, rowItem);
            if (obs != null) {
                Object v = invokeNoArg(obs, "getValue");
                return v != null ? String.valueOf(v) : "";
            }
        } catch (Exception e) {
            LOG.log(Level.FINE, "getCellObservableValue failed, try getCellValue", e);
        }
        try {
            Method getCellValue = tableColumn.getClass().getMethod("getCellValue", Object.class);
            Object v = getCellValue.invoke(tableColumn, rowItem);
            return v != null ? String.valueOf(v) : "";
        } catch (Exception ignored) { }
        return "";
    }

    private static String getFxTableCellValue(Object tableView, int row, int col) {
        if (tableView == null || row < 0 || col < 0) return "";
        try {
            Object items = invokeNoArg(tableView, "getItems");
            if (items == null) return "";
            Object rowItem = getFromObservableList(items, row);
            if (rowItem == null) return "";
            Object columns = invokeNoArg(tableView, "getColumns");
            if (columns == null) return "";
            Object tableColumn = getFromObservableList(columns, col);
            return getFxTableCellValueFromColumn(tableView, tableColumn, rowItem, row);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "getFxTableCellValue failed", e);
            return "";
        }
    }

    /** Parameter format: [conditionColumn1;conditionColumn2;...];TargetColumn */
    private static String buildFxTableParameter(List<String> conditionColumns, int targetColIndex) {
        if (conditionColumns == null || conditionColumns.isEmpty()) return "";
        String targetName = targetColIndex >= 0 && targetColIndex < conditionColumns.size()
                ? conditionColumns.get(targetColIndex) : (conditionColumns.isEmpty() ? "" : conditionColumns.get(0));
        return "[" + String.join(";", conditionColumns) + "];" + targetName;
    }

    /** Data format for SearchAndUpdate: [conditionValue1;...];targetValue */
    private static String buildFxTableSearchAndUpdateData(List<String> conditionValues, String targetValue) {
        String prefix = (conditionValues != null && !conditionValues.isEmpty())
                ? "[" + String.join(";", conditionValues) + "]" : "[]";
        return prefix + ";" + (targetValue != null ? targetValue : "");
    }

    /** Data format for SearchAndClick: [conditionValue1;...];Action:RightClick or Action:DoubleClick */
    private static String buildFxTableSearchAndClickData(List<String> conditionValues, String action) {
        String prefix = (conditionValues != null && !conditionValues.isEmpty())
                ? "[" + String.join(";", conditionValues) + "]" : "[]";
        return prefix + ";" + (action != null ? action : "Action:RightClick");
    }

    /** Build parent identifier from semantic parent boundary (or window if parent is null). */
    private static Map<String, Object> buildJavaFxParentIdentifierFrom(Object target, Object semanticParent) {
        if (semanticParent != null) {
            return buildJavaFxObjectIdentifier(semanticParent);
        }
        Object node = target;
        Object parent = invokeNoArg(node, "getParent");
        while (parent != null) {
            node = parent;
            parent = invokeNoArg(node, "getParent");
        }
        Object scene = invokeNoArg(target, "getScene");
        Object window = scene != null ? invokeNoArg(scene, "getWindow") : null;
        if (window != null) {
            Map<String, Object> id = new LinkedHashMap<>();
            id.put("javaType", window.getClass().getName());
            String title = asString(invokeNoArg(window, "getTitle"));
            if (title != null && !title.isEmpty()) id.put("javaName", title);
            fillJavaFxScreenBounds(window, id);
            return id;
        }
        return buildJavaFxObjectIdentifier(node);
    }

    public static Map<String, Object> buildJavaFxObjectIdentifier(Object node) {
        Map<String, Object> id = new LinkedHashMap<>();
        if (node == null) return id;
        id.put("javaType", node.getClass().getName());
        String nodeId = asString(invokeNoArg(node, "getId"));
        if (nodeId != null && !nodeId.isEmpty()) id.put("javaName", nodeId);
        String text = asString(invokeNoArg(node, "getText"));
        if (text != null && !text.isEmpty()) id.put("text", text);
        String value = asString(invokeNoArg(node, "getValue"));
        if (value != null && !value.isEmpty()) id.put("value", value);
        fillJavaFxScreenBounds(node, id);
        return id;
    }

    private static void fillJavaFxScreenBounds(Object fxObject, Map<String, Object> id) {
        if (fxObject == null || id == null) return;
        try {
            Object layoutBounds = invokeNoArg(fxObject, "getLayoutBounds");
            Object screenBounds = null;
            if (layoutBounds != null) {
                Class<?> boundsClass = Class.forName("javafx.geometry.Bounds");
                Method localToScreen = fxObject.getClass().getMethod("localToScreen", boundsClass);
                screenBounds = localToScreen.invoke(fxObject, layoutBounds);
            }
            if (screenBounds == null) {
                Integer x = asInt(invokeNoArg(fxObject, "getX"));
                Integer y = asInt(invokeNoArg(fxObject, "getY"));
                Integer w = asInt(invokeNoArg(fxObject, "getWidth"));
                Integer h = asInt(invokeNoArg(fxObject, "getHeight"));
                if (x != null && y != null && w != null && h != null) {
                    Map<String, Object> sb = new LinkedHashMap<>();
                    sb.put("x", x);
                    sb.put("y", y);
                    sb.put("width", w);
                    sb.put("height", h);
                    id.put("screenBounds", sb);
                }
                return;
            }
            Integer minX = asInt(invokeNoArg(screenBounds, "getMinX"));
            Integer minY = asInt(invokeNoArg(screenBounds, "getMinY"));
            Integer width = asInt(invokeNoArg(screenBounds, "getWidth"));
            Integer height = asInt(invokeNoArg(screenBounds, "getHeight"));
            if (minX != null && minY != null && width != null && height != null) {
                Map<String, Object> sb = new LinkedHashMap<>();
                sb.put("x", minX);
                sb.put("y", minY);
                sb.put("width", width);
                sb.put("height", height);
                id.put("screenBounds", sb);
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "fillJavaFxScreenBounds failed: node=" + (fxObject != null ? fxObject.getClass().getName() : "null"), e);
        }
    }

    private static Object invokeNoArg(Object target, String methodName) {
        if (target == null || methodName == null) return null;
        try {
            Method m = target.getClass().getMethod(methodName);
            return m.invoke(target);
        } catch (Exception e) {
            LOG.log(Level.FINE, "invokeNoArg failed: target=" + target.getClass().getName() + " method=" + methodName, e);
            return null;
        }
    }

    /** Describe a node for debug log: type, name (id), text, value, baseTypes (superclass chain + interfaces). */
    private static String describeNodeForLog(Object node) {
        if (node == null) return "null";
        String type = node.getClass().getName();
        String id = asString(invokeNoArg(node, "getId"));
        String text = asString(invokeNoArg(node, "getText"));
        String value = asString(invokeNoArg(node, "getValue"));
        StringBuilder base = new StringBuilder();
        Class<?> c = node.getClass();
        int depth = 0;
        while (c != null && depth < 6) {
            if (depth > 0) base.append(" > ").append(c.getName());
            else base.append(c.getName());
            Class<?>[] ifaces = c.getInterfaces();
            if (ifaces != null && ifaces.length > 0) {
                base.append(" [");
                for (int i = 0; i < ifaces.length; i++) {
                    if (i > 0) base.append(", ");
                    base.append(ifaces[i].getSimpleName());
                }
                base.append("]");
            }
            c = c.getSuperclass();
            depth++;
        }
        StringBuilder sb = new StringBuilder();
        sb.append("type=").append(type);
        if (id != null) sb.append(" name(id)=").append(id);
        if (text != null) sb.append(" text=").append(text.length() > 40 ? text.substring(0, 40) + "..." : text);
        if (value != null) sb.append(" value=").append(value);
        sb.append(" baseTypes=").append(base);
        return sb.toString();
    }

    private static String asString(Object v) {
        if (v == null) return null;
        String s = String.valueOf(v).trim();
        return s.isEmpty() ? null : s;
    }

    private static Integer asInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(v)));
        } catch (NumberFormatException e) {
            LOG.log(Level.FINE, "asInt parse failed: v=" + v, e);
            return null;
        }
    }
}
