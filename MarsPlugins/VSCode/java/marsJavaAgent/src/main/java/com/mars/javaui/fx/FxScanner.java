package com.mars.javaui.fx;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * JavaFX object tree scanner (isolated from Swing/AWT).
 *
 * This class uses reflection only. If JavaFX classes are not present, it returns empty roots.
 */
public final class FxScanner {
    private FxScanner() {}

    public static List<Map<String, Object>> scanJavaFxRoots(String rootWindowHint, FxSemanticConfig semantic) {
        List<Map<String, Object>> roots = new ArrayList<>();
        try {
            Class<?> windowClz = Class.forName("javafx.stage.Window");
            Method getWindows = windowClz.getMethod("getWindows");
            Object windowsObj = getWindows.invoke(null);
            if (!(windowsObj instanceof Iterable<?>)) return roots;
            for (Object w : (Iterable<Object>) windowsObj) {
                if (w == null) continue;
                boolean showing = Boolean.TRUE.equals(invokeNoArg(w, "isShowing"));
                if (!showing) continue;
                String title = asNonEmptyString(invokeNoArg(w, "getTitle"));
                if (rootWindowHint != null && !rootWindowHint.isEmpty()) {
                    if (title == null || !title.contains(rootWindowHint)) continue;
                }
                String rootId = "fx-root-" + System.identityHashCode(w);
                Map<String, Object> node = new LinkedHashMap<>();
                node.put("id", rootId);
                node.put("parentId", rootId);
                String wType = w.getClass().getName();
                node.put("javaType", wType);
                if (semantic != null) {
                    String role = semantic.roleForTypeName(wType);
                    if (role != null) node.put("semanticRole", role);
                }
                if (title != null) {
                    node.put("title", title);
                    node.put("text", title);
                    node.put("name", title);
                }
                fillJavaFxWindowBounds(w, node);
                node.put("visible", true);
                List<Map<String, Object>> children = new ArrayList<>();
                Object scene = invokeNoArg(w, "getScene");
                if (scene != null) {
                    Object sceneRoot = invokeNoArg(scene, "getRoot");
                    Map<String, Object> rootNode = scanJavaFxNode(sceneRoot, rootId, semantic);
                    if (rootNode != null) children.add(rootNode);
                }
                node.put("children", children);
                roots.add(node);
                if (rootWindowHint != null && !rootWindowHint.isEmpty()) break;
            }
        } catch (ClassNotFoundException e) {
            // JavaFX not present in target process.
        } catch (Exception e) {
            // Ignore JavaFX reflection errors and keep empty roots fallback.
        }
        return roots;
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> scanJavaFxNode(Object fxNode, String parentId, FxSemanticConfig semantic) {
        if (fxNode == null) return null;
        Map<String, Object> node = new LinkedHashMap<>();
        String id = "fx-" + System.identityHashCode(fxNode);
        node.put("id", id);
        node.put("parentId", parentId);
        String typeName = fxNode.getClass().getName();
        node.put("javaType", typeName);
        if (semantic != null) {
            String role = semantic.roleForTypeName(typeName);
            if (role != null) node.put("semanticRole", role);
        }

        String name = asNonEmptyString(invokeNoArg(fxNode, "getId"));
        if (name != null) node.put("name", name);

        String text = asNonEmptyString(invokeNoArg(fxNode, "getText"));
        if (text != null) node.put("text", text);

        String value = asNonEmptyString(invokeNoArg(fxNode, "getValue"));
        if (value != null) node.put("value", value);

        String prompt = asNonEmptyString(invokeNoArg(fxNode, "getPromptText"));
        if (prompt != null && !node.containsKey("text")) node.put("text", prompt);

        // best-effort tooltip text (Control.getTooltip())
        try {
            Object tip = invokeNoArg(fxNode, "getTooltip");
            String tipText = asNonEmptyString(invokeNoArg(tip, "getText"));
            if (tipText != null) node.put("toolTipText", tipText);
        } catch (Exception ignored) { }

        fillJavaFxNodeBoundsAndScreenBounds(fxNode, node);
        node.put("visible", !Boolean.FALSE.equals(invokeNoArg(fxNode, "isVisible")));

        List<Map<String, Object>> children = new ArrayList<>();
        Object childListObj = invokeNoArg(fxNode, "getChildrenUnmodifiable");
        if (childListObj instanceof Iterable<?>) {
            for (Object c : (Iterable<Object>) childListObj) {
                Map<String, Object> childNode = scanJavaFxNode(c, id, semantic);
                if (childNode != null) children.add(childNode);
            }
        } else if (childListObj instanceof Collection<?>) {
            for (Object c : (Collection<Object>) childListObj) {
                Map<String, Object> childNode = scanJavaFxNode(c, id, semantic);
                if (childNode != null) children.add(childNode);
            }
        }
        node.put("children", children);
        return node;
    }

    private static void fillJavaFxWindowBounds(Object fxWindow, Map<String, Object> node) {
        Integer x = asInteger(invokeNoArg(fxWindow, "getX"));
        Integer y = asInteger(invokeNoArg(fxWindow, "getY"));
        Integer width = asInteger(invokeNoArg(fxWindow, "getWidth"));
        Integer height = asInteger(invokeNoArg(fxWindow, "getHeight"));
        if (x == null || y == null || width == null || height == null) return;
        Map<String, Integer> bounds = new LinkedHashMap<>();
        bounds.put("x", x);
        bounds.put("y", y);
        bounds.put("width", width);
        bounds.put("height", height);
        node.put("bounds", bounds);
        node.put("screenBounds", new LinkedHashMap<>(bounds));
    }

    private static void fillJavaFxNodeBoundsAndScreenBounds(Object fxNode, Map<String, Object> node) {
        Object layoutBounds = invokeNoArg(fxNode, "getLayoutBounds");
        if (layoutBounds == null) return;
        Integer x = asInteger(invokeNoArg(layoutBounds, "getMinX"));
        Integer y = asInteger(invokeNoArg(layoutBounds, "getMinY"));
        Integer width = asInteger(invokeNoArg(layoutBounds, "getWidth"));
        Integer height = asInteger(invokeNoArg(layoutBounds, "getHeight"));
        if (x != null && y != null && width != null && height != null) {
            Map<String, Integer> bounds = new LinkedHashMap<>();
            bounds.put("x", x);
            bounds.put("y", y);
            bounds.put("width", width);
            bounds.put("height", height);
            node.put("bounds", bounds);
        }

        // Absolute screen bounds: Node.localToScreen(Bounds)
        try {
            Object screenBounds = null;
            try {
                Class<?> boundsClass = Class.forName("javafx.geometry.Bounds");
                Method localToScreen = fxNode.getClass().getMethod("localToScreen", boundsClass);
                screenBounds = localToScreen.invoke(fxNode, layoutBounds);
            } catch (NoSuchMethodException | ClassNotFoundException ignored) {
                screenBounds = null;
            }
            if (screenBounds != null) {
                Integer sx = asInteger(invokeNoArg(screenBounds, "getMinX"));
                Integer sy = asInteger(invokeNoArg(screenBounds, "getMinY"));
                Integer sw = asInteger(invokeNoArg(screenBounds, "getWidth"));
                Integer sh = asInteger(invokeNoArg(screenBounds, "getHeight"));
                if (sx != null && sy != null && sw != null && sh != null) {
                    Map<String, Integer> sb = new LinkedHashMap<>();
                    sb.put("x", sx);
                    sb.put("y", sy);
                    sb.put("width", sw);
                    sb.put("height", sh);
                    node.put("screenBounds", sb);
                }
            }
        } catch (Exception ignored) {
            // ignore
        }
    }

    private static Object invokeNoArg(Object target, String methodName) {
        if (target == null || methodName == null || methodName.isEmpty()) return null;
        try {
            Method m = target.getClass().getMethod(methodName);
            return m.invoke(target);
        } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
            return null;
        }
    }

    private static String asNonEmptyString(Object value) {
        if (value == null) return null;
        String s = String.valueOf(value).trim();
        return s.isEmpty() ? null : s;
    }

    private static Integer asInteger(Object value) {
        if (value instanceof Number) return ((Number) value).intValue();
        if (value == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(value)));
        } catch (NumberFormatException e) {
            return null;
        }
    }
}

