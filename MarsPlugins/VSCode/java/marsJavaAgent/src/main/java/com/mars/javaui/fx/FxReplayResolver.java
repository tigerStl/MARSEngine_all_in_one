package com.mars.javaui.fx;

import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.Collections;
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
public final class FxReplayResolver extends FxReflectionSupport {

    private static volatile String lastParentError;

    private FxReplayResolver() {}

    /** Returns the last error set by resolveParent when it returns null (ambiguous or index out of range). */
    public static String getLastParentError() {
        return lastParentError;
    }

    /**
     * Resolve parent (top-level Window/Stage) from parent identifier.
     * Parent is the JavaFX top window/dialog that contains the target object.
     *
     * @param parentKey map with javaType, javaName/title, etc.; may be null/empty to mean "any window"
     * @return the Window (Stage) that matches, or null if not found (use getLastParentError() for reason)
     */
    public static Object resolveParent(Map<String, Object> parentKey) {
        lastParentError = null;
        List<Object> windows = getJavaFxWindows(false);
        if (windows == null || windows.isEmpty()) {
            lastParentError = "no JavaFX windows";
            return null;
        }
        if (parentKey == null || parentKey.isEmpty()) {
            return windows.get(0);
        }
        List<Object> matching = new ArrayList<>();
        String lastMatchError = null;
        for (Object win : windows) {
            String err = windowMatchesKey(win, parentKey);
            if (err == null) matching.add(win);
            else lastMatchError = err;
        }
        if (matching.isEmpty()) {
            lastParentError = lastMatchError != null ? lastMatchError : "no matching window";
            return null;
        }
        Integer index = parseIndex(parentKey.get("index"));
        if (matching.size() > 1 && index == null) {
            lastParentError = "multiple windows matched (" + matching.size() + "), specify index";
            return null;
        }
        if (index != null) {
            if (index < 0 || index >= matching.size()) {
                lastParentError = "parent index out of range: " + index + " (valid 0.." + (matching.size() - 1) + ")";
                return null;
            }
            return matching.get(index);
        }
        return matching.get(0);
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
        List<Object> matches = resolveObjects(parentWindow, objectKey);
        if (matches.isEmpty()) return null;
        Integer index = parseIndex(objectKey.get("index"));
        if (index != null && index >= 0 && index < matches.size()) {
            return matches.get(index);
        }
        return matches.get(0);
    }

    /**
     * Resolve all matching child objects (Nodes) under the given parent window from object identifier.
     * The parent must be the top-level window obtained from windowMatchesKey / resolveParent; this method
     * does not re-resolve the window—it only collects nodes under that parent's scene root.
     * Does NOT apply index filtering; caller can inspect matches.size() for ambiguity diagnostics.
     *
     * @param parent the top-level Window (from resolveParent / window matching)
     * @param objectKey map with javaType, javaTypePath, javaName/Name, index, title, text
     * @return list of matching Nodes under parent's scene
     */
    public static List<Object> resolveObjects(Object parent, Map<String, Object> objectKey) {
        if (parent == null || objectKey == null || objectKey.isEmpty()) return Collections.emptyList();
        Object root = getSceneRoot(parent);
        if (root == null) return Collections.emptyList();
        List<Object> matches = new ArrayList<>();
        collectMatchingNodes(root, objectKey, matches);
        return matches;
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

    private static List<Object> getJavaFxWindows(boolean onlyShowing) {
        try {
            Class<?> windowClz = Class.forName("javafx.stage.Window");
            Method getWindows = windowClz.getMethod("getWindows");
            Object listObj = getWindows.invoke(null);
            if (listObj == null) return Collections.emptyList();
            List<Object> all = listToJavaList(listObj);
            if (!onlyShowing) return all;
            List<Object> visible = new ArrayList<>();
            for (Object w : all) {
                if (Boolean.TRUE.equals(invokeNoArg(w, "isShowing"))) visible.add(w);
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

    /**
     * Checks whether a top-level window matches the given key.
     * Only attributes present in the key are evaluated; not all need to be specified.
     * Supported keys: javaType, javaName, title (or Title), isDialog, isShowing, index.
     * javaType, javaName and title/Title are matched as regex if the value is not an exact match.
     * index is not used here; it is applied in resolveParent when multiple windows match.
     *
     * @return null if the window matches; otherwise an error message describing which check failed
     */
    private static String windowMatchesKey(Object window, Map<String, Object> key) {
        if (window == null || key == null) return "window or key is null";

        String keyType = stringVal(key.get("javaType"));
        if (keyType != null && !keyType.isEmpty()) {
            String actual = window.getClass().getName();
            if (!stringMatches(actual, keyType)) {
                return "javaType mismatch: key='" + keyType + "', window='" + actual + "'";
            }
        }

        String keyName = stringVal(key.get("javaName"));
        if (keyName == null || keyName.isEmpty()) keyName = stringVal(key.get("name"));
        if (keyName != null && !keyName.isEmpty()) {
            String title = asString(invokeNoArg(window, "getTitle"));
            if (!stringMatches(title, keyName)) {
                return "javaName mismatch: key='" + keyName + "', window title='" + (title != null ? title : "") + "'";
            }
        }

        String keyTitle = stringVal(key.get("title"));
        if (keyTitle == null || keyTitle.isEmpty()) keyTitle = stringVal(key.get("Title"));
        if (keyTitle != null && !keyTitle.isEmpty()) {
            String title = asString(invokeNoArg(window, "getTitle"));
            if (!stringMatches(title, keyTitle)) {
                return "title mismatch: key='" + keyTitle + "', window title='" + (title != null ? title : "") + "'";
            }
        }

        Object keyDialog = key.get("isDialog");
        if (keyDialog != null) {
            boolean wantDialog = Boolean.TRUE.equals(keyDialog) || "true".equalsIgnoreCase(String.valueOf(keyDialog));
            boolean isDialog = isWindowDialog(window);
            if (isDialog != wantDialog) {
                return "isDialog mismatch: key=" + wantDialog + ", window isDialog=" + isDialog;
            }
        }

        Object keyShowing = key.get("isShowing");
        if (keyShowing != null) {
            boolean wantShowing = Boolean.TRUE.equals(keyShowing) || "true".equalsIgnoreCase(String.valueOf(keyShowing));
            Object showing = invokeNoArg(window, "isShowing");
            boolean actualShowing = Boolean.TRUE.equals(showing);
            if (actualShowing != wantShowing) {
                return "isShowing mismatch: key=" + wantShowing + ", window isShowing=" + actualShowing;
            }
        }

        return null;
    }

    private static boolean isWindowDialog(Object window) {
        if (window == null) return false;
        String cn = window.getClass().getName();
        if (cn != null && cn.contains("Dialog")) return true;
        Object mod = invokeNoArg(window, "getModality");
        if (mod == null) return false;
        String modStr = mod.toString();
        return modStr != null && !modStr.contains("NONE");
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

    public static Integer parseIndex(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return Integer.parseInt(String.valueOf(v).trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }

    // asString/asInt/invokeNoArg are inherited from FxReflectionSupport
}
