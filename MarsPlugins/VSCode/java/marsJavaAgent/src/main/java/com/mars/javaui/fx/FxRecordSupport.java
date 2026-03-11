package com.mars.javaui.fx;

import java.io.OutputStreamWriter;
import java.lang.reflect.InvocationHandler;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Collections;
import java.util.IdentityHashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicReference;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * JavaFX recording hooks and step emission (isolated from Swing/AWT).
 * All logic for attaching/detaching FX event filters and mapping events to steps lives here.
 */
public final class FxRecordSupport extends FxReflectionSupport {

    private static final Logger LOG = Logger.getLogger(FxRecordSupport.class.getName());
    private static final DateTimeFormatter FX_TS_FORMAT = DateTimeFormatter.ofPattern("yyyyMMdd HH:mm:ss SSS");

    private static String ts() {
        return LocalDateTime.now().format(FX_TS_FORMAT);
    }

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

    /** Holder for Window.getWindows() list listener; used to remove on detach. */
    public static final class FxWindowListHook {
        public final Object windowsList;
        public final Object listener;

        public FxWindowListHook(Object windowsList, Object listener) {
            this.windowsList = windowsList;
            this.listener = listener;
        }
    }

    /** Identity set of Scene instances already registered; avoids duplicate hooks (e.g. ContextMenu show/hide). */
    private static final Set<Object> REGISTERED_FX_SCENES =
            Collections.newSetFromMap(new IdentityHashMap<>());

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

    // Context for JavaFX FillEdit dedupe between Enter/Tab and focus-lost.
    private static volatile Object fxLastFillEditControl;
    private static volatile long fxLastFillEditTimeMs;
    private static final long FX_FILLEDIT_DEDUPE_MS = 500L;

    // Optional reflection for addEventHandler and PickResult logging (set once in attachJavaFxRecordHooks).
    private static volatile Method fxSceneAddEventHandler;
    private static volatile Method fxMouseGetPickResult;
    private static volatile Method fxPickGetIntersectedNode;
    private static volatile Class<?> fxMouseEventClzForPick;

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
            AtomicReference<List<FxWindowListHook>> fxWindowHooksRef,
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

            // Optional: addEventHandler + PickResult/intersectedNode for logging
            try {
                fxSceneAddEventHandler = sceneClz.getMethod("addEventHandler", Class.forName("javafx.event.EventType"), eventHandlerClz);
                fxMouseGetPickResult = mouseEventClz.getMethod("getPickResult");
                Class<?> pickResultClz = Class.forName("javafx.scene.input.PickResult");
                fxPickGetIntersectedNode = pickResultClz.getMethod("getIntersectedNode");
                fxMouseEventClzForPick = mouseEventClz;
            } catch (Exception e) {
                LOG.log(Level.FINE, "[JavaFXHook] optional addEventHandler/PickResult reflection failed", e);
                fxSceneAddEventHandler = null;
                fxMouseGetPickResult = null;
                fxPickGetIntersectedNode = null;
                fxMouseEventClzForPick = null;
            }

            Object mouseClickedType = mouseEventClz.getField("MOUSE_CLICKED").get(null);
            Object mp = null;
            try {
                mp = mouseEventClz.getField("MOUSE_PRESSED").get(null);
            } catch (Exception ignored) { }
            final Object mousePressedTypeRef = mp;
            Object keyPressedType = keyEventClz.getField("KEY_PRESSED").get(null);
            Object keyReleasedType = keyEventClz.getField("KEY_RELEASED").get(null);

            List<FxFilterHook> hooks = new ArrayList<>();
            List<FxFocusHook> focusHooks = new ArrayList<>();

            Runnable init = () -> {
                try {
                    registerHooksForAllCurrentWindows(
                            windowClz, sceneClz, addEventFilter,
                            mouseClickedType, mousePressedTypeRef, keyPressedType, keyReleasedType,
                            recording, runLater, hooks, focusHooks, stepSender);
                    List<FxWindowListHook> windowHooks = attachJavaFxWindowListListener(
                            windowClz, runLater, addEventFilter,
                            mouseClickedType, mousePressedTypeRef, keyPressedType, keyReleasedType,
                            recording, hooks, focusHooks, stepSender, sceneClz);
                    if (fxWindowHooksRef != null) fxWindowHooksRef.set(windowHooks);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] attachJavaFxRecordHooks init failed", e);
                }
            };

            boolean fxThread = Boolean.TRUE.equals(isFxThread.invoke(null));
            if (fxThread) {
                init.run();
            } else {
                CountDownLatch latch = new CountDownLatch(1);
                runLater.invoke(null, (Runnable) () -> {
                    try {
                        init.run();
                    } finally {
                        latch.countDown();
                    }
                });
                latch.await(1500, TimeUnit.MILLISECONDS);
            }
            fxHooksRef.set(hooks);
            if (fxFocusHooksRef != null) fxFocusHooksRef.set(focusHooks);
        } catch (ClassNotFoundException e) {
            LOG.log(Level.FINE, "[ERROR] JavaFX not present, skip FX record hooks", e);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] attachJavaFxRecordHooks failed", e);
        }
    }

    /**
     * Register event filters and focus listener for a single Scene. Idempotent per scene (identity).
     * If scene is null or already in REGISTERED_FX_SCENES, returns without registering.
     */
    private static void registerJavaFxSceneHooks(
            Object scene,
            Method addEventFilter,
            Object mouseClickedType,
            Object mousePressedType,
            Object keyPressedType,
            Object keyReleasedType,
            boolean[] recording,
            Method runLater,
            List<FxFilterHook> hooks,
            List<FxFocusHook> focusHooks,
            FxStepSender stepSender,
            Class<?> sceneClz) {
        if (scene == null) return;
        synchronized (REGISTERED_FX_SCENES) {
            if (REGISTERED_FX_SCENES.contains(scene)) return;
        }
        try {
            // Filter path: log PickResult/intersectedNode then run recording
            Object mouseHandler = createHandlerProxy(event -> {
                if (!recording[0]) return;
                try {
                    logJavaFxPickResult("[JavaFXHook][filter]", event);
                    runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "[ERROR] FX runLater (mouse) failed", e);
                }
            });
            addEventFilter.invoke(scene, mouseClickedType, mouseHandler);
            hooks.add(new FxFilterHook(scene, mouseClickedType, mouseHandler));

            if (mousePressedType != null) {
                Object mousePressedHandler = createHandlerProxy(event -> {
                    if (!recording[0]) return;
                    try {
                        logJavaFxPickResult("[JavaFXHook][filter]", event);
                        runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                    } catch (Exception e) {
                        LOG.log(Level.WARNING, "[ERROR] FX runLater (mousePressed) failed", e);
                    }
                });
                addEventFilter.invoke(scene, mousePressedType, mousePressedHandler);
                hooks.add(new FxFilterHook(scene, mousePressedType, mousePressedHandler));
            }

            // addEventHandler in addition to addEventFilter: log only (no second handleJavaFxRecordEvent)
            Method addEventHandler = fxSceneAddEventHandler;
            if (addEventHandler != null) {
                Object handlerOnlyMouseClicked = createHandlerProxy(event -> {
                    if (!recording[0]) return;
                    logJavaFxPickResult("[JavaFXHook][handler]", event);
                });
                addEventHandler.invoke(scene, mouseClickedType, handlerOnlyMouseClicked);
                if (mousePressedType != null) {
                    Object handlerOnlyMousePressed = createHandlerProxy(event -> {
                        if (!recording[0]) return;
                        logJavaFxPickResult("[JavaFXHook][handler]", event);
                    });
                    addEventHandler.invoke(scene, mousePressedType, handlerOnlyMousePressed);
                }
            }

            Object keyPressedHandler = createHandlerProxy(event -> {
                if (!recording[0]) return;
                try {
                    runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "[ERROR] FX runLater (keyPressed) failed", e);
                }
            });
            addEventFilter.invoke(scene, keyPressedType, keyPressedHandler);
            hooks.add(new FxFilterHook(scene, keyPressedType, keyPressedHandler));

            Object keyHandler = createHandlerProxy(event -> {
                if (!recording[0]) return;
                try {
                    runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender));
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "[ERROR] FX runLater (keyReleased) failed", e);
                }
            });
            addEventFilter.invoke(scene, keyReleasedType, keyHandler);
            hooks.add(new FxFilterHook(scene, keyReleasedType, keyHandler));

            try {
                Method focusOwnerProp = sceneClz.getMethod("focusOwnerProperty");
                Object focusProp = focusOwnerProp.invoke(scene);
                if (focusProp != null) {
                    Object focusListener = createFocusChangeListenerProxy((oldOwner, newOwner) -> {
                        if (!recording[0]) return;
                        try {
                            runLater.invoke(null, (Runnable) () -> onJavaFxFocusChange(oldOwner, newOwner, stepSender));
                        } catch (Exception e) {
                            LOG.log(Level.WARNING, "[ERROR] FX runLater (focus) failed", e);
                        }
                    });
                    Class<?> changeListenerClz = Class.forName("javafx.beans.value.ChangeListener");
                    Method addListener = focusProp.getClass().getMethod("addListener", changeListenerClz);
                    addListener.invoke(focusProp, focusListener);
                    focusHooks.add(new FxFocusHook(scene, focusProp, focusListener));
                }
                        } catch (Exception e) {
                            LOG.log(Level.FINE, "[ERROR] FX focusOwnerProperty/addListener not available", e);
            }

            synchronized (REGISTERED_FX_SCENES) {
                REGISTERED_FX_SCENES.add(scene);
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] registerJavaFxSceneHooks failed for scene=" + (scene != null ? scene.getClass().getName() : "null"), e);
        }
    }

    /** When window has null scene (e.g. HeavyweightDialog), retry getScene() via runLater up to retriesLeft times. */
    private static void scheduleRegisterSceneWhenReady(
            Object win,
            int retriesLeft,
            Method addEventFilter,
            Object mouseClickedType,
            Object mousePressedType,
            Object keyPressedType,
            Object keyReleasedType,
            boolean[] recording,
            Method runLater,
            List<FxFilterHook> hooks,
            List<FxFocusHook> focusHooks,
            FxStepSender stepSender,
            Class<?> sceneClz) {
        if (win == null || retriesLeft < 0) return;
        Runnable task = () -> {
            Object scene = invokeNoArg(win, "getScene");
            if (scene != null) {
                try {
                    registerJavaFxSceneHooks(scene, addEventFilter, mouseClickedType, mousePressedType,
                            keyPressedType, keyReleasedType, recording, runLater, hooks, focusHooks, stepSender, sceneClz);
                } catch (Exception ex) {
                    LOG.log(Level.WARNING, "[ERROR] registerJavaFxSceneHooks for late scene failed", ex);
                }
                return;
            }
            if (retriesLeft > 0) {
                try {
                    runLater.invoke(null, (Runnable) () -> scheduleRegisterSceneWhenReady(win, retriesLeft - 1,
                            addEventFilter, mouseClickedType, mousePressedType, keyPressedType, keyReleasedType,
                            recording, runLater, hooks, focusHooks, stepSender, sceneClz));
        } catch (Exception e) {
            LOG.log(Level.FINE, "[ERROR] scheduleRegisterSceneWhenReady runLater failed", e);
                }
            }
        };
        try {
            runLater.invoke(null, task);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] scheduleRegisterSceneWhenReady failed", e);
        }
    }

    /** Initial scan: register hooks for all current showing windows' scenes. */
    private static void registerHooksForAllCurrentWindows(
            Class<?> windowClz,
            Class<?> sceneClz,
            Method addEventFilter,
            Object mouseClickedType,
            Object mousePressedType,
            Object keyPressedType,
            Object keyReleasedType,
            boolean[] recording,
            Method runLater,
            List<FxFilterHook> hooks,
            List<FxFocusHook> focusHooks,
            FxStepSender stepSender) throws Exception {
        Object winsObj = windowClz.getMethod("getWindows").invoke(null);
        if (!(winsObj instanceof Iterable<?>)) return;
        for (Object win : (Iterable<?>) winsObj) {
            if (win == null) continue;
            Object scene = invokeNoArg(win, "getScene");
            if (scene == null) continue;
            registerJavaFxSceneHooks(scene, addEventFilter, mouseClickedType, mousePressedType,
                    keyPressedType, keyReleasedType, recording, runLater, hooks, focusHooks, stepSender, sceneClz);
        }
    }

    /**
     * Attach ListChangeListener to Window.getWindows(); on add runLater + registerJavaFxSceneHooks,
     * on remove runLater + removeHooksForScenesOfWindows. Returns list of FxWindowListHook for detach.
     */
    private static List<FxWindowListHook> attachJavaFxWindowListListener(
            Class<?> windowClz,
            Method runLater,
            Method addEventFilter,
            Object mouseClickedType,
            Object mousePressedType,
            Object keyPressedType,
            Object keyReleasedType,
            boolean[] recording,
            List<FxFilterHook> hooks,
            List<FxFocusHook> focusHooks,
            FxStepSender stepSender,
            Class<?> sceneClz) {
        List<FxWindowListHook> result = new ArrayList<>();
        try {
            Object winsObj = windowClz.getMethod("getWindows").invoke(null);
            if (winsObj == null) return result;
            Class<?> listChangeListenerClz = Class.forName("javafx.collections.ListChangeListener");
            Method nextM = null;
            Method wasAddedM = null;
            Method wasRemovedM = null;
            Method getAddedSubListM = null;
            Method getRemovedM = null;
            try {
                Class<?> changeClz = Class.forName("javafx.collections.ListChangeListener$Change");
                nextM = changeClz.getMethod("next");
                wasAddedM = changeClz.getMethod("wasAdded");
                wasRemovedM = changeClz.getMethod("wasRemoved");
                getAddedSubListM = changeClz.getMethod("getAddedSubList");
                getRemovedM = changeClz.getMethod("getRemoved");
            } catch (Exception e) {
                LOG.log(Level.FINE, "[ERROR] ListChangeListener.Change methods not available", e);
                return result;
            }
            final Method nextMethod = nextM;
            final Method wasAddedMethod = wasAddedM;
            final Method wasRemovedMethod = wasRemovedM;
            final Method getAddedSubListMethod = getAddedSubListM;
            final Method getRemovedMethod = getRemovedM;

            InvocationHandler ih = (proxy, method, args) -> {
                if (!"onChanged".equals(method.getName()) || args == null || args.length == 0) return null;
                Object change = args[0];
                try {
                    while (Boolean.TRUE.equals(nextMethod.invoke(change))) {
                        if (Boolean.TRUE.equals(wasAddedMethod.invoke(change))) {
                            Object added = getAddedSubListMethod.invoke(change);
                            if (added instanceof Iterable<?>) {
                                for (Object win : (Iterable<?>) added) {
                                    if (win == null) continue;
                                    Object scene = invokeNoArg(win, "getScene");
                                    if (scene == null) {
                                        // HeavyweightDialog etc.: scene may be set later; retry with delayed runLater (up to 2 retries).
                                        scheduleRegisterSceneWhenReady(win, 2, addEventFilter, mouseClickedType, mousePressedType,
                                                keyPressedType, keyReleasedType, recording, runLater, hooks, focusHooks, stepSender, sceneClz);
                                        continue;
                                    }
                                    final Object sceneRef = scene;
                                    runLater.invoke(null, (Runnable) () -> {
                                        try {
                                            registerJavaFxSceneHooks(sceneRef, addEventFilter, mouseClickedType, mousePressedType,
                                                    keyPressedType, keyReleasedType, recording, runLater, hooks, focusHooks, stepSender, sceneClz);
                                        } catch (Exception e) {
                                            LOG.log(Level.WARNING, "registerJavaFxSceneHooks for added window failed", e);
                                        }
                                    });
                                }
                            }
                        }
                        if (Boolean.TRUE.equals(wasRemovedMethod.invoke(change))) {
                            Object removed = getRemovedMethod.invoke(change);
                            if (removed instanceof Iterable<?>) {
                                List<Object> wins = new ArrayList<>();
                                for (Object w : (Iterable<?>) removed) wins.add(w);
                                runLater.invoke(null, (Runnable) () -> removeHooksForScenesOfWindows(wins, hooks, focusHooks, sceneClz));
                            }
                        }
                    }
                } catch (Exception e) {
                    LOG.log(Level.WARNING, "[ERROR] FX Window list onChanged failed", e);
                }
                return null;
            };
            Object listener = Proxy.newProxyInstance(
                    listChangeListenerClz.getClassLoader(),
                    new Class<?>[]{listChangeListenerClz},
                    ih);
            Method addListener = winsObj.getClass().getMethod("addListener", listChangeListenerClz);
            addListener.invoke(winsObj, listener);
            result.add(new FxWindowListHook(winsObj, listener));
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] attachJavaFxWindowListListener failed", e);
        }
        return result;
    }

    /** Remove hooks for scenes of the given (removed) windows; remove scenes from REGISTERED_FX_SCENES. */
    private static void removeHooksForScenesOfWindows(
            List<Object> windows,
            List<FxFilterHook> hooks,
            List<FxFocusHook> focusHooks,
            Class<?> sceneClz) {
        if (windows == null || windows.isEmpty()) return;
        try {
            Class<?> sceneClzRef = sceneClz;
            Method removeEventFilter = sceneClzRef.getMethod("removeEventFilter", Class.forName("javafx.event.EventType"), Class.forName("javafx.event.EventHandler"));
            Class<?> changeListenerClz = Class.forName("javafx.beans.value.ChangeListener");
            for (Object win : windows) {
                Object scene = invokeNoArg(win, "getScene");
                if (scene == null) continue;
                synchronized (REGISTERED_FX_SCENES) {
                    REGISTERED_FX_SCENES.remove(scene);
                }
                List<FxFilterHook> toRemoveF = new ArrayList<>();
                for (FxFilterHook h : hooks) {
                    if (h.scene == scene) toRemoveF.add(h);
                }
                for (FxFilterHook h : toRemoveF) {
                    try {
                        removeEventFilter.invoke(h.scene, h.eventType, h.handler);
                    } catch (Exception e) {
                        LOG.log(Level.FINE, "removeEventFilter failed", e);
                    }
                    hooks.remove(h);
                }
                List<FxFocusHook> toRemoveFocus = new ArrayList<>();
                for (FxFocusHook h : focusHooks) {
                    if (h.scene == scene) toRemoveFocus.add(h);
                }
                for (FxFocusHook h : toRemoveFocus) {
                    try {
                        if (h.property != null && h.listener != null) {
                            Method removeListener = h.property.getClass().getMethod("removeListener", changeListenerClz);
                            removeListener.invoke(h.property, h.listener);
                        }
                    } catch (Exception e) {
                        LOG.log(Level.FINE, "removeListener (focus) failed", e);
                    }
                    focusHooks.remove(h);
                }
            }
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] removeHooksForScenesOfWindows failed", e);
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
            LOG.log(Level.WARNING, "[ERROR] detachJavaFxRecordHooks failed", e);
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
            LOG.log(Level.WARNING, "[ERROR] detachJavaFxFocusHooks failed", e);
        }
    }

    public static void detachJavaFxWindowListHooks(List<FxWindowListHook> windowHooks) {
        if (windowHooks == null || windowHooks.isEmpty()) return;
        try {
            Class<?> listChangeListenerClz = Class.forName("javafx.collections.ListChangeListener");
            Class<?> platformClz = Class.forName("javafx.application.Platform");
            Method isFxThread = platformClz.getMethod("isFxApplicationThread");
            Method runLater = platformClz.getMethod("runLater", Runnable.class);
            Runnable remove = () -> {
                for (FxWindowListHook h : windowHooks) {
                    try {
                        if (h.windowsList != null && h.listener != null) {
                            Method removeListener = h.windowsList.getClass().getMethod("removeListener", listChangeListenerClz);
                            removeListener.invoke(h.windowsList, h.listener);
                        }
                    } catch (Exception e) {
                        LOG.log(Level.WARNING, "detachJavaFxWindowListHooks removeListener failed", e);
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
            LOG.log(Level.WARNING, "[ERROR] detachJavaFxWindowListHooks failed", e);
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

    /** Called on FX thread when focus changes; emit SearchAndUpdate when leaving tracked table cell,
     *  and emit FillEdit when leaving a text input control (with dedupe vs Enter/Tab-triggered FillEdit). */
    private static void onJavaFxFocusChange(Object oldOwner, Object newOwner, FxStepSender stepSender) {
        if (stepSender == null) return;

        // 1) Table SearchAndUpdate on focus leaving the tracked cell.
        Object table = fxTableContextTableView;
        if (table != null && oldOwner != null) {
            TableCellInfo cell = getTableCellInfoFromNode(oldOwner);
            if (cell != null
                    && cell.tableView == table
                    && cell.row == fxTableContextRow
                    && cell.col == fxTableContextCol) {
                List<String> condCols = fxTableContextConditionColumns;
                List<String> condVals = fxTableContextConditionValues;
                if (condCols != null && condVals != null && condCols.size() == condVals.size()) {
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
                    // Do not return here; same focus change might also correspond to leaving a text input control.
                }
            }
        }

        // 2) Text input FillEdit on focus lost, with dedupe vs Enter/Tab-generated FillEdit.
        if (oldOwner == null) return;
        if (!isInsideTextInput(oldOwner)) return;

        long now = System.currentTimeMillis();
        if (fxLastFillEditControl == oldOwner
                && (now - fxLastFillEditTimeMs) >= 0
                && (now - fxLastFillEditTimeMs) < FX_FILLEDIT_DEDUPE_MS) {
            return;
        }

        String text = asString(invokeNoArg(oldOwner, "getText"));
        if (text == null) text = "";

        Map<String, Object> step = new LinkedHashMap<>();
        step.put("keyword", "FillEdit");
        step.put("event", "FillEdit");
        step.put("timestamp", now);
        if (!text.isEmpty()) step.put("data", text);
        step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(oldOwner, null));
        step.put("objectIdentifier", buildJavaFxObjectIdentifier(oldOwner));
        stepSender.sendStep(step);

        fxLastFillEditControl = oldOwner;
        fxLastFillEditTimeMs = now;
    }

    private static void clearFxTableContext() {
        fxTableContextTableView = null;
        fxTableContextRow = -1;
        fxTableContextCol = -1;
        fxTableContextConditionColumns = null;
        fxTableContextConditionValues = null;
    }
    private static int debugCount = 0;
    // topcontroltext 是产生事件的对象的文本
    private static String topControlText = "";

    /** Resolve top-level control text for the given node (e.g. window title from scene/window). Used for conditional breakpoint and logging. */
    private static String getTopControlText(Object node) {
        if (node == null) return "";
        try {
            Object scene = invokeNoArg(node, "getScene");
            if (scene == null) return "";
            Object window = invokeNoArg(scene, "getWindow");
            if (window == null) return "";
            String title = asString(invokeNoArg(window, "getTitle"));
            return title != null ? title : "";
        } catch (Exception e) {
            LOG.log(Level.FINE, "getTopControlText failed", e);
            return "";
        }
    }

    // 调试：打印 node 自身、向上两层 parent、向下两层 children 的类型、name、屏幕位置
    private static void debugFxNodeContext(Object node) {
        try {
            if (node == null) {
                System.out.println("[FxDebug] node is null");
                return;
            }
            String cn = node.getClass().getName();
            if (!"javafx.scene.layout.StackPane".equals(cn)) {
                // 只在需要时打印 StackPane 的上下文，避免刷屏
                System.out.println("[FxDebug] node is not StackPane, class=" + cn);
                return;
            }

            System.out.println("========== [FxDebug] StackPane context ==========");
            // 1) 当前节点
            printFxNodeInfo("[Self]", node);

            // 2) 向上两层 parent
            Object p = FxNodeClassifier.parentOf(node);
            for (int i = 1; i <= 2 && p != null; i++) {
                printFxNodeInfo("[Parent " + i + "]", p);
                p = FxNodeClassifier.parentOf(p);
            }

            // 3) 向下两层 children（children + grandchildren）
            List<Object> level1 = getFxNodeChildrenSafe(node);
            if (!level1.isEmpty()) {
                System.out.println("----- [FxDebug] Children (level 1) -----");
                for (Object c1 : level1) {
                    printFxNodeInfo("[Child 1]", c1);
                }
            }
            List<Object> level2 = new ArrayList<>();
            for (Object c1 : level1) {
                level2.addAll(getFxNodeChildrenSafe(c1));
            }
            if (!level2.isEmpty()) {
                System.out.println("----- [FxDebug] Children (level 2) -----");
                for (Object c2 : level2) {
                    printFxNodeInfo("[Child 2]", c2);
                }
            }
            System.out.println("========== [FxDebug] End ==========");

        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    // 打印单个节点的：前缀、类名、name(javaName)、屏幕位置
    private static void printFxNodeInfo(String prefix, Object node) {
        if (node == null) {
            System.out.println(prefix + " node=null");
            return;
        }
        String cn = node.getClass().getName();
        Map<String, Object> id = buildJavaFxObjectIdentifier(node);
        Object javaName = id.get("javaName");

        // 计算屏幕坐标（尽量重用现有逻辑）
        Integer sx = null, sy = null, sw = null, sh = null;
        try {
            Object layoutBounds = invokeNoArg(node, "getLayoutBounds");
            Object screenBounds = null;
            if (layoutBounds != null) {
                Class<?> boundsClass = Class.forName("javafx.geometry.Bounds");
                Method localToScreen = node.getClass().getMethod("localToScreen", boundsClass);
                screenBounds = localToScreen.invoke(node, layoutBounds);
            }
            if (screenBounds != null) {
                sx = asInt(invokeNoArg(screenBounds, "getMinX"));
                sy = asInt(invokeNoArg(screenBounds, "getMinY"));
                sw = asInt(invokeNoArg(screenBounds, "getWidth"));
                sh = asInt(invokeNoArg(screenBounds, "getHeight"));
            }
        } catch (Exception e) {
            // 调试打印里忽略异常
        }

        System.out.println(prefix
                + " class=" + cn
                + ", name=" + (javaName != null ? javaName : "")
                + ", screenBounds=("
                + (sx != null ? sx : "null") + ","
                + (sy != null ? sy : "null") + ","
                + (sw != null ? sw : "null") + "x"
                + (sh != null ? sh : "null") + ")"
        );
    }

    // 安全获取 children 列表（优先 getChildrenUnmodifiable，然后 getChildren）
    @SuppressWarnings("unchecked")
    private static List<Object> getFxNodeChildrenSafe(Object node) {
        if (node == null) return Collections.emptyList();
        try {
            // 优先尝试 getChildrenUnmodifiable()
            Object listObj = invokeNoArg(node, "getChildrenUnmodifiable");
            if (listObj == null) {
                listObj = invokeNoArg(node, "getChildren");
            }
            if (listObj == null) return Collections.emptyList();
            if (listObj instanceof java.util.List) {
                return new ArrayList<>((List<Object>) listObj);
            }
            // 退化：通过 List 接口反射访问
            Method size = java.util.List.class.getMethod("size");
            Method get = java.util.List.class.getMethod("get", int.class);
            int n = ((Number) size.invoke(listObj)).intValue();
            List<Object> out = new ArrayList<>(n);
            for (int i = 0; i < n; i++) {
                out.add(get.invoke(listObj, i));
            }
            return out;
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }


    /** Log MouseEvent.getPickResult() and PickResult.getIntersectedNode() for debugging; no-op if reflection not available. */
    private static void logJavaFxPickResult(String prefix, Object event) {
        if (event == null || fxMouseEventClzForPick == null || !fxMouseEventClzForPick.isInstance(event)) return;
        Object pickResult = null;
        Object intersected = null;
        try {
            if (fxMouseGetPickResult != null) pickResult = fxMouseGetPickResult.invoke(event);
        } catch (Exception e) {
            LOG.log(Level.FINE, prefix + " getPickResult failed", e);
        }
        try {
            if (pickResult != null && fxPickGetIntersectedNode != null) intersected = fxPickGetIntersectedNode.invoke(pickResult);
        } catch (Exception e) {
            LOG.log(Level.FINE, prefix + " getIntersectedNode failed", e);
        }
        String prStr = pickResult != null ? pickResult.getClass().getName() : "null";
        String inStr = intersected != null ? intersected.getClass().getName() : "null";
        System.out.println(prefix + " pickResult=" + prStr + " intersectedNode=" + inStr);
        if (LOG.isLoggable(Level.INFO)) LOG.info(prefix + " pickResult=" + prStr + " intersectedNode=" + inStr);
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
            debugCount++;
            if (debugCount % 1000 == 0) {
                LOG.info("[INFO] " + ts() + " [FxRecord] debugCount=" + debugCount);
            }
            String eventTypeName = String.valueOf(invokeNoArg(event, "getEventType"));
            Object target = invokeNoArg(event, "getTarget");
            if (target == null) return;

            if (target != null && "javafx.scene.layout.StackPane".equals(target.getClass().getName())) {
                debugFxNodeContext(target);
            }
            topControlText = getTopControlText(target);

            if (LOG.isLoggable(Level.INFO)) {
                LOG.info("[INFO] " + ts() + " [FxRecord] eventType=" + eventTypeName + " | target(sender)=" + describeNodeForLog(target) + " | topControlText=" + topControlText);
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
                    LOG.info("[INFO] " + ts() + " [FxRecord] foldAndLift foldedChain(" + lr.chainFoldedNodes.size() + ")= " + folded);
                }
                LOG.info("[INFO] " + ts() + " [FxRecord] foldAndLift semanticTarget=" + (semanticTarget != null ? describeNodeForLog(semanticTarget) : "null"));
                LOG.info("[INFO] " + ts() + " [FxRecord] foldAndLift semanticParent=" + (semanticParent != null ? describeNodeForLog(semanticParent) : "null"));
                if (semanticTarget != null) {
                    FxNodeCategory.NodeMeta meta = FxNodeClassifier.classify(semanticTarget);
                    if (meta != null) {
                        LOG.info("[INFO] " + ts() + " [FxRecord] semanticTarget meta: category=" + meta.category + " boundary=" + meta.boundary + " semanticType=" + meta.semanticType);
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
            // Type+event mapping: ContextMenuContent only responds to MOUSE_PRESSED; MenuBarButton etc. to MOUSE_CLICKED (avoid duplicate).
            String semanticTargetClassName = semanticTarget != null ? semanticTarget.getClass().getName() : "";
            boolean isContextMenuMenuItem = "MENUITEM".equals(semanticType) && semanticTargetClassName.contains("ContextMenuContent");
            if (eventTypeName.contains("MOUSE_CLICKED")) {
                if (isContextMenuMenuItem) return; // already handled on MOUSE_PRESSED
                if ("TEXT_INPUT".equals(semanticType) || isInsideTextInput(target) || isInsideTextInput(semanticTarget)) {
                    return;
                }
                keyword = keywordForMouseClick(semanticTarget, semanticType);
                data = dataForMouseClick(semanticTarget, semanticType, keyword);
            } else if (eventTypeName.contains("MOUSE_PRESSED") && isContextMenuMenuItem) {
                if ("TEXT_INPUT".equals(semanticType) || isInsideTextInput(target) || isInsideTextInput(semanticTarget)) {
                    return;
                }
                keyword = keywordForMouseClick(semanticTarget, semanticType);
                data = dataForMouseClick(semanticTarget, semanticType, keyword);
            }
            if (keyword != null && "SelectMenuItem".equals(keyword) && LOG.isLoggable(Level.INFO)) {
                LOG.info("[INFO] " + ts() + " [FxRecord] SelectMenuItem data controlClass=" + semanticTargetClassName + " data=" + (data != null ? data : ""));
            }
            else if (eventTypeName.contains("KEY_PRESSED")) {
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
                    // Remember last FillEdit control/time so focus-lost handler can skip duplicate step.
                    fxLastFillEditControl = target;
                    fxLastFillEditTimeMs = System.currentTimeMillis();
                }
            }
            if (keyword == null) return;

            stepTarget = "FillEdit".equals(keyword) ? target : semanticTarget;
            Object stepParent = "FillEdit".equals(keyword) ? null : semanticParent;

            if (!"SelectMenuItem".equals(keyword)) {
                pendingFxSelectMenuItemStep = null;
            } else {
                if (isFxMenuWithSubmenu(stepTarget)) {
                    // Use latest menu path: update cached step if present, so extension can update display.
                    Map<String, Object> step;
                    if (pendingFxSelectMenuItemStep != null) {
                        step = pendingFxSelectMenuItemStep;
                        step.put("objectIdentifier", buildJavaFxObjectIdentifier(stepTarget));
                        step.put("data", data != null ? data : "");
                        step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(stepTarget, stepParent));
                        step.put("timestamp", System.currentTimeMillis());
                        if (semanticType != null && !semanticType.isEmpty()) step.put("semanticType", semanticType);
                    } else {
                        step = new LinkedHashMap<>();
                        step.put("keyword", keyword);
                        step.put("event", keyword);
                        step.put("timestamp", System.currentTimeMillis());
                        if (data != null && !data.isEmpty()) step.put("data", data);
                        step.put("parentIdentifier", buildJavaFxParentIdentifierFrom(stepTarget, stepParent));
                        step.put("objectIdentifier", buildJavaFxObjectIdentifier(stepTarget));
                        if (semanticType != null && !semanticType.isEmpty()) step.put("semanticType", semanticType);
                        pendingFxSelectMenuItemStep = step;
                    }
                    stepSender.sendStep(step);
                    return;
                }
                if (pendingFxSelectMenuItemStep != null) {
                    pendingFxSelectMenuItemStep.put("objectIdentifier", buildJavaFxObjectIdentifier(stepTarget));
                    pendingFxSelectMenuItemStep.put("data", data != null ? data : "");
                    pendingFxSelectMenuItemStep.put("timestamp", System.currentTimeMillis());
                    stepSender.sendStep(pendingFxSelectMenuItemStep);
                    pendingFxSelectMenuItemStep = null;
                    return;
                }
            }

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
            LOG.log(Level.WARNING, "[ERROR] emitStep failed: keyword=" + keyword + ", target=" + (stepTarget != null ? stepTarget.getClass().getName() : "null"), e);
        }
    }

    /** Use java.util.List interface for reflection to avoid touching com.sun.javafx.collections (not exported by javafx.base). */
    private static Object getFromObservableList(Object list, int idx) {
        if (list == null) return null;
        try {
            java.lang.reflect.Method getMethod = java.util.List.class.getMethod("get", int.class);
            return getMethod.invoke(list, idx);
        } catch (Exception e) {
            LOG.log(Level.WARNING, "[ERROR] getFromObservableList failed: list=" + list.getClass().getName() + ", idx=" + idx, e);
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
            LOG.log(Level.FINE, "[ERROR] indexOfInObservableList indexOf failed, will try linear scan", e);
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
            LOG.log(Level.WARNING, "[ERROR] indexOfInObservableList linear scan failed", e);
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

    /**
     * Build SelectMenuItem data: path from top-level menu to this item, "root;...;leaf".
     * Menu/semantic object is usually found by lifting up (e.g. MenuBarButton); text for that
     * object may be on the model (getText) or on a child node. So we resolve text by trying
     * model getText first, then looking down into children for a node with getText().
     */
    private static String buildFxMenuPathFromRootToLeaf(Object control) {
        if (control == null) return "";
        String controlClass = control.getClass().getName();
        Object cur = resolveMenuModel(control);
        if (cur == null) cur = control;
        List<String> segments = new ArrayList<>();
        while (cur != null) {
            String text = getMenuSegmentText(cur);
            if (LOG.isLoggable(Level.INFO)) {
                LOG.info("[INFO] " + ts() + " [FxRecord] getMenuPath segment nodeClass=" + cur.getClass().getName() + " text=" + (text != null ? text : ""));
            }
            segments.add(0, text != null ? text : "");
            cur = invokeNoArg(cur, "getParentMenu");
        }
        String path = String.join(";", segments);
        if (LOG.isLoggable(Level.INFO)) {
            LOG.info("[INFO] " + ts() + " [FxRecord] getMenuPath controlClass=" + controlClass + " path=" + path);
        }
        return path;
    }

    /** Resolve to Menu/MenuItem model when control is a Skin (e.g. MenuBarButton). */
    private static Object resolveMenuModel(Object control) {
        if (control == null) return null;
        try {
            Object skinnable = invokeNoArg(control, "getSkinnable");
            if (skinnable != null) return skinnable;
        } catch (Exception ignored) { }
        return control;
    }

    /**
     * Get display text for one segment (menu/menu item). Semantic object may be the model
     * (MenuItem/Menu has getText) or a visual (e.g. MenuBarButton); for visual, text is
     * resolved by rule textFrom (search child chain for matching class, invoke method) or look down.
     */
    private static String getMenuSegmentText(Object node) {
        if (node == null) return null;
        String text = asString(invokeNoArg(node, "getText"));
        if (text != null) return text;
        Object model = resolveMenuModel(node);
        if (model != null && model != node) {
            text = asString(invokeNoArg(model, "getText"));
            if (text != null) return text;
        }
        List<String> textFromHints = FxNodeClassifier.getTextFromHints(node);
        if (!textFromHints.isEmpty()) {
            text = getTextFromDescendantByRule(node, textFromHints);
            if (text != null) return text;
        }
        return getTextFromChild(node);
    }

    /**
     * Search semantic object's child chain for a node whose class matches a textFrom entry
     * (className#methodName), then invoke the method (e.g. getText) on that node.
     */
    private static String getTextFromDescendantByRule(Object semanticNode, List<String> textFromHints) {
        if (semanticNode == null || textFromHints == null) return null;
        for (String entry : textFromHints) {
            int hash = entry.indexOf('#');
            String className = hash >= 0 ? entry.substring(0, hash).trim() : entry;
            String methodName = hash >= 0 && hash < entry.length() - 1 ? entry.substring(hash + 1).trim() : "getText";
            if (className.isEmpty() || methodName.isEmpty()) continue;
            Object found = findDescendantWithClass(semanticNode, className);
            if (found != null) {
                String t = asString(invokeNoArg(found, methodName));
                if (t != null) return t;
            }
        }
        return null;
    }

    /** Find first descendant of parent whose getClass().getName() contains the given className. */
    private static Object findDescendantWithClass(Object parent, String className) {
        if (parent == null || className == null || className.isEmpty()) return null;
        try {
            // JavaFX Parent exposes getChildrenUnmodifiable() as public; prefer that over getChildren().
            Object children = invokeNoArg(parent, "getChildrenUnmodifiable");
            if (children == null) {
                children = invokeNoArg(parent, "getChildren");
            }
            if (children == null || !(children instanceof java.util.List)) return null;
            @SuppressWarnings("unchecked")
            java.util.List<Object> list = (java.util.List<Object>) children;
            for (Object child : list) {
                if (child == null) continue;
                if (child.getClass().getName().contains(className)) return child;
                Object deep = findDescendantWithClass(child, className);
                if (deep != null) return deep;
            }
        } catch (Exception e) {
            LOG.log(Level.FINE, "[ERROR] findDescendantWithClass failed: parent=" + parent.getClass().getName(), e);
        }
        return null;
    }

    /** Get first non-empty getText() from a direct or nested child (look down). */
    private static String getTextFromChild(Object parent) {
        if (parent == null) return null;
        try {
            // Prefer JavaFX Parent.getChildrenUnmodifiable() when available.
            Object children = invokeNoArg(parent, "getChildrenUnmodifiable");
            if (children == null) {
                children = invokeNoArg(parent, "getChildren");
            }
            if (children == null || !(children instanceof java.util.List)) return null;
            @SuppressWarnings("unchecked")
            java.util.List<Object> list = (java.util.List<Object>) children;
            for (Object child : list) {
                if (child == null) continue;
                String t = asString(invokeNoArg(child, "getText"));
                if (t != null) return t;
                t = getTextFromChild(child);
                if (t != null) return t;
            }
        } catch (Exception e) {
            LOG.log(Level.FINE, "[ERROR] getTextFromChild failed: parent=" + parent.getClass().getName(), e);
        }
        return null;
    }

    /** True if control is a Menu with submenu (getItems() non-empty). */
    private static boolean isFxMenuWithSubmenu(Object control) {
        if (control == null) return false;
        try {
            Object items = invokeNoArg(control, "getItems");
            if (items == null) return false;
            Object sz = java.util.List.class.getMethod("size").invoke(items);
            return (sz instanceof Number) && ((Number) sz).intValue() > 0;
        } catch (Exception e) {
            return false;
        }
    }

    /** Cached SelectMenuItem step while user navigates submenus; emit when leaf is clicked. */
    private static volatile Map<String, Object> pendingFxSelectMenuItemStep;

    /** Build SelectTreeList data: path from root to selected node, segments separated by ";". */
    private static String buildFxTreePathFromRootToNode(Object control) {
        if (control == null) return "";
        Object treeItem = null;
        try {
            treeItem = invokeNoArg(control, "getTreeItem");
        } catch (Exception e) {
            LOG.log(Level.FINE, "[ERROR] buildFxTreePathFromRootToNode getTreeItem failed: " + control.getClass().getName(), e);
        }
        if (treeItem == null) {
            Object selModel = invokeNoArg(control, "getSelectionModel");
            if (selModel != null) treeItem = invokeNoArg(selModel, "getSelectedItem");
        }
        if (treeItem == null) return "";
        List<String> segments = new ArrayList<>();
        Object cur = treeItem;
        while (cur != null) {
            Object val = invokeNoArg(cur, "getValue");
            segments.add(val != null ? String.valueOf(val) : "");
            cur = invokeNoArg(cur, "getParent");
        }
        Collections.reverse(segments);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < segments.size(); i++) {
            if (i > 0) sb.append(';');
            sb.append(segments.get(i));
        }
        return sb.toString();
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
            return buildFxTreePathFromRootToNode(control);
        }
        if ("SelectMenuItem".equals(keyword)) {
            return buildFxMenuPathFromRootToLeaf(control);
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
            LOG.log(Level.FINE, "[ERROR] SelectTab fast path getText failed: control=" + (control != null ? control.getClass().getName() : "null"), e);
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
            LOG.log(Level.FINE, "[ERROR] SelectTab getSkinnable failed: control=" + (control != null ? control.getClass().getName() : "null"), e);
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
            LOG.log(Level.WARNING, "[ERROR] SelectTab dataForMouseClick failed (getTabs/getSelectionModel/getFromObservableList): control=" + (control != null ? control.getClass().getName() : "null"), e);
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
            LOG.log(Level.FINE, "[ERROR] isSecondaryMouseButton failed", e);
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
            LOG.log(Level.WARNING, "[ERROR] getFxTableColumnNames failed", e);
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
            LOG.log(Level.WARNING, "[ERROR] getFxTableRowValues failed", e);
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
            LOG.log(Level.FINE, "[ERROR] getCellObservableValue failed, try getCellValue", e);
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
            LOG.log(Level.WARNING, "[ERROR] getFxTableCellValue failed", e);
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

    /**
     * Build parent identifier for FX replay.
     * <p>
     * Parent is the <b>top-level container</b> for this interaction: Stage/Window/Dialog/Popup
     * (e.g. ContextMenu window), not the semanticParent control (TableView, TreeView, etc.).
     * semanticParent is only used as a better starting node to locate the top window.
     */
    private static Map<String, Object> buildJavaFxParentIdentifierFrom(Object target, Object semanticParent) {
        // Base node to locate parent window from: prefer semanticParent when available
        Object node = (semanticParent != null) ? semanticParent : target;
        // Climb to topmost Parent in the scene graph
        Object parent = invokeNoArg(node, "getParent");
        while (parent != null) {
            node = parent;
            parent = invokeNoArg(node, "getParent");
        }
        // Prefer window for replay parent (Stage/Dialog/Popup/ContextMenu window)
        Object scene = invokeNoArg(node, "getScene");
        Object window = scene != null ? invokeNoArg(scene, "getWindow") : null;
        if (window != null) {
            Map<String, Object> id = new LinkedHashMap<>();
            id.put("javaType", window.getClass().getName());
            String title = asString(invokeNoArg(window, "getTitle"));
            if (title != null && !title.isEmpty()) id.put("javaName", title);
            return id;
        }
        return buildJavaFxObjectIdentifier(node);
    }

    public static Map<String, Object> buildJavaFxObjectIdentifier(Object node) {
        Map<String, Object> id = new LinkedHashMap<>();
        if (node == null) return id;
        id.put("javaType", node.getClass().getName());
        String nodeId = asString(invokeNoArg(node, "getId"));
        if (nodeId != null && !nodeId.isEmpty()) {
            id.put("javaName", nodeId);
        }
        String text = asString(invokeNoArg(node, "getText"));
        if (text != null && !text.isEmpty()) {
            id.put("text", text);
            // 对于没有 id 的 JavaFX 控件，使用可见文本作为 javaName，便于在 Test Step 中统一通过 javaName 定位。
            if (!id.containsKey("javaName")) {
                id.put("javaName", text);
            }
        }
        String value = asString(invokeNoArg(node, "getValue"));
        if (value != null && !value.isEmpty()) id.put("value", value);
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
            LOG.log(Level.WARNING, "[ERROR] fillJavaFxScreenBounds failed: node=" + (fxObject != null ? fxObject.getClass().getName() : "null"), e);
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

}
