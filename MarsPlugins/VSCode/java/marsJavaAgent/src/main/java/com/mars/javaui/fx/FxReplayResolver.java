package com.mars.javaui.fx;

import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

/**
 * Resolves JavaFX parent (Stage/Window) and object (Node) from identifier maps for replay.
 * Uses reflection only; no JavaFX API imports.
 * Locators: javaType, javaTypePath (semicolon or slash), javaName/Name, index, title, text.
 */
public final class FxReplayResolver {

    private FxReplayResolver() {}

    /**
     * Resolve parent (top-level Window/Stage) from parent identifier.
     * Parent is the JavaFX top window/dialog that contains the target object.
     *
     * @param parentKey map with javaType, javaName/title, etc.; may be null/empty to mean "any window"
     * @return the Window (Stage) that matches, or null if not found
     */
    public static Object resolveParent(Map<String, Object> parentKey) {
        List<Object> windows = getJavaFxWindows();
        if (windows == null || windows.isEmpty()) return null;
        if (parentKey == null || parentKey.isEmpty()) {
            return windows.isEmpty() ? null : windows.get(0);
        }
        for (Object win : windows) {
            if (windowMatchesKey(win, parentKey)) return win;
        }
        return null;
    }

    /**
     * Resolve object (Node) under the given parent window from object identifier.
     * Walks the JavaFX scene graph and filters by key; applies index if present.
     *
     * @param parentWindow the parent Stage/Window (from resolveParent)
     * @param objectKey    map with javaType, javaTypePath, javaName/Name, index, title, text
     * @return the matching Node, or null if not found
     */
    public static Object resolveObject(Object parentWindow, Map<String, Object> objectKey) {
        if (parentWindow == null || objectKey == null || objectKey.isEmpty()) return null;
        Object root = getSceneRoot(parentWindow);
        if (root == null) return null;
        List<Object> matches = new ArrayList<>();
        collectMatchingNodes(root, objectKey, matches);
        if (matches.isEmpty()) return null;
        Integer index = parseIndex(objectKey.get("index"));
        if (index != null && index >= 0 && index < matches.size()) {
            return matches.get(index);
        }
        return matches.get(0);
    }

    /**
     * Get screen bounds [x, y, width, height] for a Node (for Robot click at center).
     */
    public static int[] getNodeScreenBounds(Object node) {
        if (node == null) return null;
        try {
            Object layoutBounds = invokeNoArg(node, "getLayoutBounds");
            if (layoutBounds == null) return null;
            Class<?> boundsClass = Class.forName("javafx.geometry.Bounds");
            Method localToScreen = node.getClass().getMethod("localToScreen", boundsClass);
            Object screenBounds = localToScreen.invoke(node, layoutBounds);
            if (screenBounds == null) return null;
            Integer minX = asInt(invokeNoArg(screenBounds, "getMinX"));
            Integer minY = asInt(invokeNoArg(screenBounds, "getMinY"));
            Integer w = asInt(invokeNoArg(screenBounds, "getWidth"));
            Integer h = asInt(invokeNoArg(screenBounds, "getHeight"));
            if (minX == null || minY == null || w == null || h == null) return null;
            return new int[]{minX, minY, w, h};
        } catch (Exception e) {
            return null;
        }
    }

    // ---------- JavaFX window list (reflection) ----------

    private static List<Object> getJavaFxWindows() {
        try {
            Class<?> windowClz = Class.forName("javafx.stage.Window");
            Method getWindows = windowClz.getMethod("getWindows");
            Object listObj = getWindows.invoke(null);
            if (listObj == null) return Collections.emptyList();
            List<Object> all = listToJavaList(listObj);
            List<Object> visible = new ArrayList<>();
            for (Object w : all) {
                Object showing = invokeNoArg(w, "isShowing");
                if (Boolean.TRUE.equals(showing)) visible.add(w);
            }
            return visible;
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }

    private static List<Object> listToJavaList(Object observableList) {
        if (observableList == null) return Collections.emptyList();
        try {
            Method getMethod = java.util.List.class.getMethod("get", int.class);
            Method sizeMethod = java.util.List.class.getMethod("size");
            int n = ((Number) sizeMethod.invoke(observableList)).intValue();
            List<Object> out = new ArrayList<>(n);
            for (int i = 0; i < n; i++) {
                out.add(getMethod.invoke(observableList, i));
            }
            return out;
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }

    private static Object getSceneRoot(Object window) {
        if (window == null) return null;
        Object scene = invokeNoArg(window, "getScene");
        if (scene == null) return null;
        return invokeNoArg(scene, "getRoot");
    }

    private static boolean windowMatchesKey(Object window, Map<String, Object> key) {
        if (window == null || key == null) return false;
        String keyType = stringVal(key.get("javaType"));
        if (keyType != null && !keyType.isEmpty()) {
            if (!stringMatches(window.getClass().getName(), keyType)) return false;
        }
        String keyName = stringVal(key.get("javaName"));
        if (keyName == null || keyName.isEmpty()) keyName = stringVal(key.get("name"));
        if (keyName != null && !keyName.isEmpty()) {
            String title = asString(invokeNoArg(window, "getTitle"));
            if (!stringMatches(title, keyName)) return false;
        }
        String keyTitle = stringVal(key.get("title"));
        if (keyTitle != null && !keyTitle.isEmpty()) {
            String title = asString(invokeNoArg(window, "getTitle"));
            if (!stringMatches(title, keyTitle)) return false;
        }
        Object showing = invokeNoArg(window, "isShowing");
        if (showing instanceof Boolean && !((Boolean) showing)) return false;
        return true;
    }

    // ---------- Scene graph walk ----------

    private static void collectMatchingNodes(Object node, Map<String, Object> key, List<Object> matches) {
        if (node == null) return;
        if (nodeMatchesKey(node, key)) matches.add(node);
        List<Object> children = getNodeChildren(node);
        for (Object child : children) {
            collectMatchingNodes(child, key, matches);
        }
    }

    private static List<Object> getNodeChildren(Object node) {
        if (node == null) return Collections.emptyList();
        try {
            Method m = node.getClass().getMethod("getChildrenUnmodifiable");
            Object listObj = m.invoke(node);
            return listToJavaList(listObj);
        } catch (NoSuchMethodException e) {
            try {
                Method m = node.getClass().getMethod("getChildren");
                Object listObj = m.invoke(node);
                return listToJavaList(listObj);
            } catch (Exception e2) {
                return Collections.emptyList();
            }
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }

    private static boolean nodeMatchesKey(Object node, Map<String, Object> key) {
        if (node == null || key == null) return false;

        String keyType = stringVal(key.get("javaType"));
        if (keyType != null && !keyType.isEmpty()) {
            if (!stringMatches(node.getClass().getName(), keyType)) return false;
        }

        List<String> keyTypePath = toPathList(key.get("javaTypePath"));
        if (!keyTypePath.isEmpty()) {
            List<String> nodeTypePath = buildNodeJavaTypePath(node);
            if (!pathMatches(nodeTypePath, keyTypePath)) return false;
        }

        String keyName = stringVal(key.get("javaName"));
        if (keyName == null || keyName.isEmpty()) keyName = stringVal(key.get("name"));
        if (keyName != null && !keyName.isEmpty()) {
            String name = asString(invokeNoArg(node, "getId"));
            if (name == null || name.isEmpty()) name = asString(invokeNoArg(node, "getTitle"));
            if (!stringMatches(name, keyName)) return false;
        }

        String keyTitle = stringVal(key.get("title"));
        if (keyTitle != null && !keyTitle.isEmpty()) {
            String title = asString(invokeNoArg(node, "getTitle"));
            if (!stringMatches(title, keyTitle)) return false;
        }

        String keyText = stringVal(key.get("text"));
        if (keyText != null && !keyText.isEmpty()) {
            String text = asString(invokeNoArg(node, "getText"));
            if (!stringMatches(text, keyText)) return false;
        }

        Integer keyIndex = parseIndex(key.get("index"));
        if (keyIndex != null) {
            int actualIndex = getNodeIndexAmongSiblings(node);
            if (actualIndex != keyIndex) return false;
        }

        return true;
    }

    /** Index among same-type same-name siblings (0-based). */
    private static int getNodeIndexAmongSiblings(Object node) {
        Object parent = invokeNoArg(node, "getParent");
        if (parent == null) return 0;
        List<Object> siblings = getNodeChildren(parent);
        String nodeType = node.getClass().getName();
        String nodeName = asString(invokeNoArg(node, "getId"));
        int idx = 0;
        for (Object s : siblings) {
            if (s == null) continue;
            if (!nodeType.equals(s.getClass().getName())) continue;
            String sName = asString(invokeNoArg(s, "getId"));
            if (!Objects.equals(nodeName, sName)) continue;
            if (s == node) return idx;
            idx++;
        }
        return 0;
    }

    private static List<String> buildNodeJavaTypePath(Object node) {
        List<String> path = new ArrayList<>();
        Object cur = node;
        while (cur != null) {
            path.add(0, cur.getClass().getName());
            cur = invokeNoArg(cur, "getParent");
        }
        return path;
    }

    private static boolean pathMatches(List<String> actual, List<String> expected) {
        if (actual == null || expected == null) return false;
        if (actual.size() != expected.size()) return false;
        for (int i = 0; i < actual.size(); i++) {
            if (!stringMatches(actual.get(i), expected.get(i))) return false;
        }
        return true;
    }

    private static List<String> toPathList(Object raw) {
        if (raw == null) return Collections.emptyList();
        if (raw instanceof List) {
            List<String> out = new ArrayList<>();
            for (Object o : (List<?>) raw) {
                if (o != null) out.add(String.valueOf(o).trim());
            }
            return out;
        }
        String s = String.valueOf(raw).trim();
        if (s.isEmpty()) return Collections.emptyList();
        String[] parts = s.split("[;/]");
        List<String> out = new ArrayList<>();
        for (String p : parts) {
            String t = p.trim();
            if (!t.isEmpty()) out.add(t);
        }
        return out;
    }

    private static boolean stringMatches(String actual, String expected) {
        String a = actual != null ? actual : "";
        String e = expected != null ? expected : "";
        if (Objects.equals(a, e)) return true;
        if (e.isEmpty()) return a.isEmpty();
        try {
            return Pattern.compile(e).matcher(a).find();
        } catch (PatternSyntaxException ignored) {
            return false;
        }
    }

    private static String stringVal(Object v) {
        if (v == null) return null;
        String s = String.valueOf(v).trim();
        return s.isEmpty() ? null : s;
    }

    private static String asString(Object v) {
        if (v == null) return null;
        String s = String.valueOf(v).trim();
        return s.isEmpty() ? null : s;
    }

    private static Integer parseIndex(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return Integer.parseInt(String.valueOf(v).trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }

    private static Integer asInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(v)));
        } catch (NumberFormatException e) {
            return null;
        }
    }

    private static Object invokeNoArg(Object target, String methodName) {
        if (target == null || methodName == null) return null;
        try {
            Method m = target.getClass().getMethod(methodName);
            return m.invoke(target);
        } catch (Exception e) {
            return null;
        }
    }
}
