package com.mars.javaui.protocol;

import java.awt.*;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.*;
import java.util.List;
import javax.swing.*;
import javax.swing.Action;

/**
 * Protocol utilities for agent communication.
 * Provides UI scanning and component resolution functionality.
 */
public class AgentProtocol {

    /**
     * Build object tree for UI scanning.
     * @param rootWindowHint Optional hint for root window name (e.g., "LoanIQ")
     * @return Map containing roots array with component tree
     */
    public static Object buildObjectTree(String rootWindowHint) {
        List<Map<String, Object>> roots = new ArrayList<>();
        
        if (GraphicsEnvironment.isHeadless()) {
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("roots", roots);
            return result;
        }

        Window[] windows = Window.getWindows();
        if (windows != null) {
            for (Window w : windows) {
                if (w.isVisible()) {
                    // If hint provided, try to match window title
                    if (rootWindowHint != null && !rootWindowHint.isEmpty()) {
                        String title = getWindowTitle(w);
                        if (title != null && title.contains(rootWindowHint)) {
                            String rootId = "root-" + System.identityHashCode(w);
                            Map<String, Object> node = scanComponent(w, rootId);
                            if (node != null) {
                                roots.add(node);
                            }
                            break; // Found matching window
                        }
                    } else {
                        String rootId = "root-" + System.identityHashCode(w);
                        Map<String, Object> node = scanComponent(w, rootId);
                        if (node != null) {
                            roots.add(node);
                        }
                    }
                }
            }
        }

        if (roots.isEmpty()) {
            Frame[] frames = Frame.getFrames();
            if (frames != null) {
                for (Frame f : frames) {
                    if (f.isVisible()) {
                        String rootId = "root-" + System.identityHashCode(f);
                        Map<String, Object> node = scanComponent(f, rootId);
                        if (node != null) {
                            roots.add(node);
                        }
                    }
                }
            }
        }

        Map<String, Object> result = new LinkedHashMap<>();
        result.put("roots", roots);
        return result;
    }

    /**
     * Find main window root by hint or return first visible window.
     */
    public static Component findMainWindowRoot(String hint) {
        if (GraphicsEnvironment.isHeadless()) {
            return null;
        }

        Window[] windows = Window.getWindows();
        if (windows != null) {
            for (Window w : windows) {
                if (w.isVisible()) {
                    if (hint != null && !hint.isEmpty()) {
                        String title = getWindowTitle(w);
                        if (title != null && title.contains(hint)) {
                            return w;
                        }
                    } else {
                        return w;
                    }
                }
            }
        }

        Frame[] frames = Frame.getFrames();
        if (frames != null) {
            for (Frame f : frames) {
                if (f.isVisible()) {
                    return f;
                }
            }
        }

        return null;
    }

    /**
     * Resolve component by parent and object keys.
     */
    @SuppressWarnings("unchecked")
    public static Component resolveComponent(Component root, Map<String, Object> parentKey, Map<String, Object> objectKey) {
        if (root == null || objectKey == null) {
            return null;
        }
        // Find component matching objectKey (scope by parent first if provided)
        return findComponentByKey(root, parentKey, objectKey);
    }

    private static String getWindowTitle(Window w) {
        if (w instanceof Frame) {
            return ((Frame) w).getTitle();
        } else if (w instanceof Dialog) {
            return ((Dialog) w).getTitle();
        }
        return null;
    }

    @SuppressWarnings("unchecked")
    private static Component findComponentByKey(Component root, Map<String, Object> parentKey, Map<String, Object> objectKey) {
        if (root == null) {
            return null;
        }
        Component scope = root;
        if (parentKey != null && !parentKey.isEmpty()) {
            Component parent = findComponentByKeySingle(root, parentKey);
            if (parent == null) return null;
            scope = parent;
        }
        return findComponentByKeySingle(scope, objectKey);
    }

    private static Component findComponentByKeySingle(Component root, Map<String, Object> key) {
        if (root == null || key == null) return null;
        if (matchesKey(root, key)) return root;
        if (root instanceof Container) {
            for (Component child : ((Container) root).getComponents()) {
                Component found = findComponentByKeySingle(child, key);
                if (found != null) return found;
            }
        }
        return null;
    }

    @SuppressWarnings("unchecked")
    private static boolean matchesKey(Component c, Map<String, Object> key) {
        if (c == null || key == null) {
            return false;
        }
        String keyName = key.containsKey("javaName") ? String.valueOf(key.get("javaName")) : null;
        if (keyName != null && !keyName.isEmpty()) {
            String name = getComponentJavaName(c);
            if (name == null || !name.equals(keyName)) return false;
        }

        String keyType = key.containsKey("javaType") ? String.valueOf(key.get("javaType")) : null;
        if (keyType != null && !keyType.isEmpty()) {
            if (!c.getClass().getName().equals(keyType)) return false;
        }

        List<String> keyNamePath = toPathList(key.get("javaNamePath"));
        if (!keyNamePath.isEmpty()) {
            List<String> compNamePath = buildJavaNamePath(c);
            if (!pathEquals(compNamePath, keyNamePath)) return false;
        }

        List<String> keyTypePath = toPathList(key.get("javaTypePath"));
        if (!keyTypePath.isEmpty()) {
            List<String> compTypePath = buildJavaTypePath(c);
            if (!pathEquals(compTypePath, keyTypePath)) return false;
        }

        Integer index = parseIndex(key.get("index"));
        if (index != null) {
            int actualIndex = getComponentIndex(c);
            if (actualIndex != index) return false;
        }

        return true;
    }

    private static Integer parseIndex(Object idxObj) {
        if (idxObj instanceof Number) return ((Number) idxObj).intValue();
        if (idxObj instanceof String) {
            String s = ((String) idxObj).trim();
            if (!s.isEmpty()) {
                try { return Integer.parseInt(s); } catch (NumberFormatException ignored) { }
            }
        }
        return null;
    }

    private static String getComponentJavaName(Component comp) {
        if (comp == null) return "";
        String name = comp.getName();
        if (name != null && !name.isEmpty()) return name;
        try {
            if (comp instanceof JComponent && ((JComponent) comp).getAccessibleContext() != null) {
                String acc = ((JComponent) comp).getAccessibleContext().getAccessibleName();
                if (acc != null && !acc.isEmpty()) return acc;
            }
        } catch (Exception ignored) { }
        return "";
    }

    private static int getComponentIndex(Component comp) {
        if (comp == null) return 0;
        Container parent = comp.getParent();
        if (parent == null) return 0;
        List<Component> matches = new ArrayList<>();
        String javaName = getComponentJavaName(comp);
        String javaType = comp.getClass().getName();
        for (Component c : parent.getComponents()) {
            if (c == null) continue;
            if (!javaType.equals(c.getClass().getName())) continue;
            if (Objects.equals(javaName, getComponentJavaName(c))) matches.add(c);
        }
        int idx = matches.indexOf(comp);
        return idx >= 0 ? idx : 0;
    }

    private static List<String> buildJavaNamePath(Component comp) {
        List<String> list = new ArrayList<>();
        for (Component p = comp; p != null; p = p.getParent()) {
            String n = getComponentJavaName(p);
            if (n != null && !n.isEmpty()) list.add(0, n);
        }
        return list;
    }

    private static List<String> buildJavaTypePath(Component comp) {
        List<String> list = new ArrayList<>();
        for (Component p = comp; p != null; p = p.getParent()) {
            list.add(0, p.getClass().getName());
        }
        return list;
    }

    private static List<String> toPathList(Object raw) {
        if (raw == null) return Collections.emptyList();
        if (raw instanceof List) {
            List<?> l = (List<?>) raw;
            List<String> out = new ArrayList<>();
            for (Object o : l) {
                if (o != null) out.add(String.valueOf(o));
            }
            return out;
        }
        String s = String.valueOf(raw).trim();
        if (s.isEmpty()) return Collections.emptyList();
        String[] parts = s.split("/");
        List<String> out = new ArrayList<>();
        for (String p : parts) {
            if (!p.isEmpty()) out.add(p);
        }
        return out;
    }

    private static boolean pathEquals(List<String> a, List<String> b) {
        if (a == null || b == null) return false;
        if (a.size() != b.size()) return false;
        for (int i = 0; i < a.size(); i++) {
            if (!Objects.equals(a.get(i), b.get(i))) return false;
        }
        return true;
    }

    private static Map<String, Object> scanComponent(Component c, String parentId) {
        if (c == null) return null;

        Map<String, Object> node = new LinkedHashMap<>();
        String id = "c-" + System.identityHashCode(c);
        node.put("id", id);
        node.put("parentId", parentId);
        String javaType = c.getClass().getName();
        node.put("javaType", javaType);

        List<String> baseTypes = getBaseTypes(c.getClass());
        node.put("baseTypes", baseTypes);

        String text = invokeStringGetter(c, "getText");
        if (text != null && !text.isEmpty()) {
            node.put("text", text);
        }

        String value = getValue(c);
        if (value != null) {
            node.put("value", value);
        }

        String name = c.getName();
        if (name != null && !name.isEmpty()) {
            node.put("name", name);
        }

        String caption = invokeStringGetter(c, "getCaption");
        if (caption != null && !caption.isEmpty()) {
            node.put("caption", caption);
        }

        String title = invokeStringGetter(c, "getTitle");
        if (title != null && !title.isEmpty()) {
            node.put("title", title);
        }

        String toolTipText = getToolTipText(c);
        if (toolTipText != null && !toolTipText.isEmpty()) {
            node.put("toolTipText", toolTipText);
            if (isToolButtonLike(c) && (text == null || text.isEmpty()) && (caption == null || caption.isEmpty())) {
                node.put("text", toolTipText);
                node.put("caption", toolTipText);
            }
        }

        Rectangle rect = c.getBounds();
        if (rect != null && (rect.width > 0 || rect.height > 0)) {
            Map<String, Integer> bounds = new LinkedHashMap<>();
            bounds.put("x", rect.x);
            bounds.put("y", rect.y);
            bounds.put("width", rect.width);
            bounds.put("height", rect.height);
            node.put("bounds", bounds);
        }
        
        try {
            Point loc = c.getLocationOnScreen();
            Dimension dim = c.getSize();
            if (loc != null && dim != null) {
                Map<String, Integer> screenBounds = new LinkedHashMap<>();
                screenBounds.put("x", loc.x);
                screenBounds.put("y", loc.y);
                screenBounds.put("width", dim.width);
                screenBounds.put("height", dim.height);
                node.put("screenBounds", screenBounds);
            }
        } catch (Exception ignored) { }
        
        node.put("visible", c.isVisible());

        List<Map<String, Object>> children = new ArrayList<>();
        if (c instanceof Container) {
            Component[] comps = ((Container) c).getComponents();
            for (Component child : comps) {
                Map<String, Object> childNode = scanComponent(child, id);
                if (childNode != null) {
                    children.add(childNode);
                }
            }
        }
        node.put("children", children);

        return node;
    }

    private static String getToolTipText(Component c) {
        String s = invokeToolTipTextGetter(c);
        if (s != null && !s.isEmpty()) return s;
        if (c instanceof AbstractButton) {
            Action a = ((AbstractButton) c).getAction();
            if (a != null) {
                Object v = a.getValue(Action.SHORT_DESCRIPTION);
                if (v != null) {
                    String t = v.toString().trim();
                    if (!t.isEmpty()) return t;
                }
            }
        }
        return null;
    }

    private static String invokeToolTipTextGetter(Component c) {
        if (c == null) return null;
        Class<?> clz = c.getClass();
        while (clz != null && clz != Object.class) {
            try {
                Method m = clz.getMethod("getToolTipText");
                if (m != null && m.getReturnType() == String.class) {
                    Object v = m.invoke(c);
                    if (v != null) {
                        String s = v.toString().trim();
                        if (!s.isEmpty()) return s;
                    }
                }
            } catch (NoSuchMethodException ignored) {
            } catch (IllegalAccessException | InvocationTargetException e) {
                // Continue searching
            }
            try {
                Method m = clz.getMethod("getTooltipText");
                if (m != null && m.getReturnType() == String.class) {
                    Object v = m.invoke(c);
                    if (v != null) {
                        String s = v.toString().trim();
                        if (!s.isEmpty()) return s;
                    }
                }
            } catch (NoSuchMethodException ignored) {
            } catch (IllegalAccessException | InvocationTargetException e) {
                // Continue searching
            }
            clz = clz.getSuperclass();
        }
        for (Method m : c.getClass().getMethods()) {
            String name = m.getName();
            if (("getToolTipText".equals(name) || "getTooltipText".equals(name))
                    && m.getParameterCount() == 0 && m.getReturnType() == String.class) {
                try {
                    Object v = m.invoke(c);
                    if (v != null) {
                        String s = v.toString().trim();
                        if (!s.isEmpty()) return s;
                    }
                } catch (IllegalAccessException | InvocationTargetException e) {
                    // Continue
                }
            }
        }
        return null;
    }

    private static boolean isToolButtonLike(Component c) {
        if (c == null) return false;
        Class<?> clz = c.getClass();
        while (clz != null) {
            if (clz.getName().toLowerCase(Locale.ROOT).contains("toolbutton")) {
                return true;
            }
            clz = clz.getSuperclass();
        }
        return false;
    }

    private static String invokeStringGetter(Component c, String methodName) {
        try {
            Method m = c.getClass().getMethod(methodName);
            Object v = m.invoke(c);
            if (v == null) return null;
            String s = v.toString().trim();
            return s.isEmpty() ? null : s;
        } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
            return null;
        }
    }

    private static List<String> getBaseTypes(Class<?> clazz) {
        List<String> list = new ArrayList<>();
        try {
            Class<?> c = clazz;
            while (c != null && c != Object.class) {
                String name = c.getName();
                list.add(name);
                if (name.startsWith("java.") || name.startsWith("javax.")) {
                    break;
                }
                c = c.getSuperclass();
            }
            if (list.isEmpty() && clazz != null) {
                list.add(clazz.getName());
            }
        } catch (Exception e) {
            if (clazz != null) list.add(clazz.getName());
        }
        return list;
    }

    private static String getValue(Component c) {
        String s = invokeStringGetter(c, "getText");
        if (s != null) return s;
        if (c instanceof AbstractButton) {
            String label = ((AbstractButton) c).getText();
            if (label != null && !label.trim().isEmpty()) return label;
        }
        try {
            Method m = c.getClass().getMethod("getSelectedItem");
            Object v = m.invoke(c);
            if (v != null) return v.toString();
        } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
            // ignore
        }
        try {
            Method m = c.getClass().getMethod("getValue");
            Object v = m.invoke(c);
            if (v != null) return v.toString();
        } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
            // ignore
        }
        return null;
    }
}
