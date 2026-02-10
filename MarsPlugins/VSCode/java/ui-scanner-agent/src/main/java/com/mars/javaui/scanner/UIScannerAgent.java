package com.mars.javaui.scanner;

import java.awt.*;
import java.io.*;
import java.lang.instrument.Instrumentation;
import java.lang.reflect.*;
import java.util.*;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;
import javax.swing.*;

/**
 * JVM Agent to scan AWT/Swing UI hierarchy.
 * Supports agentmain (attach) and premain (-javaagent).
 * Output: JSON file with component tree (text, name, caption, javaType, bounds).
 * Log file: {jar-dir}/javaagentLog/ui-scanner-agent.log
 */
public class UIScannerAgent {

    private static final Logger LOG = AgentLogUtil.createLogger(UIScannerAgent.class, "ui-scanner-agent.log");

    public static void agentmain(String agentArgs, Instrumentation inst) {
        LOG.info("agentmain called, agentArgs=" + agentArgs);
        run(agentArgs);
    }

    public static void premain(String agentArgs, Instrumentation inst) {
        LOG.info("premain called, agentArgs=" + agentArgs);
        run(agentArgs);
    }

    private static void run(String outputPath) {
        LOG.info("run start, outputPath=" + outputPath);
        try {
            List<Map<String, Object>> roots = new ArrayList<>();
            if (GraphicsEnvironment.isHeadless()) {
                LOG.info("headless=true, writing empty roots");
                writeOutput(outputPath, roots);
                return;
            }

            LOG.info("headless=false, scanning UI on EDT");
            EventQueue.invokeAndWait(() -> {
                try {
                    Window[] windows = Window.getWindows();
                    LOG.info("Window.getWindows() count=" + (windows != null ? windows.length : 0));
                    if (windows != null) {
                        for (Window w : windows) {
                            if (w.isVisible()) {
                                String rootId = "root-" + System.identityHashCode(w);
                                LOG.info("scan root: id=" + rootId + " javaType=" + w.getClass().getName());
                                Map<String, Object> node = scanComponent(w, rootId);
                                if (node != null) {
                                    roots.add(node);
                                }
                            }
                        }
                    }
                    if (roots.isEmpty()) {
                        Frame[] frames = Frame.getFrames();
                        LOG.info("roots empty, Frame.getFrames() count=" + (frames != null ? frames.length : 0));
                        if (frames != null) {
                            for (Frame f : frames) {
                                if (f.isVisible()) {
                                    String rootId = "root-" + System.identityHashCode(f);
                                    LOG.info("scan root: id=" + rootId + " javaType=" + f.getClass().getName());
                                    Map<String, Object> node = scanComponent(f, rootId);
                                    if (node != null) {
                                        roots.add(node);
                                    }
                                }
                            }
                        }
                    }
                    LOG.info("scan complete, roots.size=" + roots.size());
                } catch (Exception e) {
                    LOG.log(Level.SEVERE, "scan failed", e);
                    e.printStackTrace();
                }
            });

            LOG.info("writeOutput path=" + outputPath + " roots.size=" + roots.size());
            writeOutput(outputPath, roots);
            LOG.info("run success");
        } catch (Exception e) {
            LOG.log(Level.SEVERE, "run failed", e);
            e.printStackTrace();
        }
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

        LOG.fine("scanComponent id=" + id + " parentId=" + parentId + " javaType=" + javaType);

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

        // Log component info: type, name, etc.
        logComponentInfo(c, javaType, name);
        // If ToolButton, log all properties
        if (javaType != null && javaType.toLowerCase(Locale.ROOT).contains("toolbutton")) {
            logAllProperties(c, javaType);
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
            java.awt.Point loc = c.getLocationOnScreen();
            java.awt.Dimension dim = c.getSize();
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

    /** Get tooltip text: try getToolTipText() (with robust reflection), then for AbstractButton try Action.SHORT_DESCRIPTION. */
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

    /** Invoke getToolTipText / getTooltipText by traversing class hierarchy and trying getMethods(). */
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
                // try next class or alternate name
            } catch (IllegalAccessException | InvocationTargetException e) {
                LOG.fine("getToolTipText invoke failed on " + clz.getName() + ": " + e.getMessage());
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
                LOG.fine("getTooltipText invoke failed on " + clz.getName() + ": " + e.getMessage());
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
                    LOG.fine(name + " invoke failed: " + e.getMessage());
                }
            }
        }
        return null;
    }

    /** True if component's class name contains "ToolButton" (case-insensitive). */
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

    /** Use reflection to invoke a no-arg getter (e.g. getText, getCaption, getTitle, getToolTipText) and return trimmed string or null. */
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

    /** Inheritance chain from concrete class down to (and including) first base whose name starts with java. or javax. */
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
            LOG.fine("getBaseTypes failed for " + clazz + ": " + e.getMessage());
            if (clazz != null) list.add(clazz.getName());
        }
        return list;
    }

    /** Component display value: getText(), getLabel(), getSelectedItem(), getValue(), etc. */
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

    private static void writeOutput(String path, List<Map<String, Object>> roots) throws IOException {
        StringBuilder sb = new StringBuilder();
        sb.append("{\"roots\":");
        sb.append(toJson(roots));
        sb.append("}");
        String json = sb.toString();
        try (Writer w = new OutputStreamWriter(new FileOutputStream(path), "UTF-8")) {
            w.write(json);
        }
        logTransmitSummary(path, roots, json.length());
    }

    /** Log summary of transmitted data for debugging. */
    private static void logTransmitSummary(String path, List<Map<String, Object>> roots, int jsonLen) {
        int[] counts = countNodes(roots);
        LOG.info("[agent->extension] path=" + path + " roots=" + roots.size()
                + " totalNodes=" + counts[0] + " withToolTipText=" + counts[1] + " jsonLen=" + jsonLen);
        int[] remaining = { 3 };
        logNodeToolTipsSample(roots, remaining);
    }

    @SuppressWarnings("unchecked")
    private static void logNodeToolTipsSample(List<Map<String, Object>> nodes, int[] remaining) {
        if (remaining[0] <= 0) return;
        for (Map<String, Object> n : nodes) {
            Object tt = n.get("toolTipText");
            if (tt != null && !tt.toString().trim().isEmpty()) {
                String javaType = String.valueOf(n.get("javaType"));
                String val = tt.toString();
                LOG.info("  [toolTipText] javaType=" + javaType + " toolTipText="
                        + (val.length() > 60 ? val.substring(0, 60) + "..." : val));
                if (--remaining[0] <= 0) return;
            }
            Object ch = n.get("children");
            if (ch instanceof List) {
                logNodeToolTipsSample((List<Map<String, Object>>) ch, remaining);
                if (remaining[0] <= 0) return;
            }
        }
    }

    @SuppressWarnings("unchecked")
    private static int[] countNodes(List<Map<String, Object>> nodes) {
        int total = 0, withToolTip = 0;
        for (Map<String, Object> n : nodes) {
            total++;
            if (n.get("toolTipText") != null && !n.get("toolTipText").toString().trim().isEmpty()) {
                withToolTip++;
            }
            Object ch = n.get("children");
            if (ch instanceof List) {
                int[] sub = countNodes((List<Map<String, Object>>) ch);
                total += sub[0];
                withToolTip += sub[1];
            }
        }
        return new int[] { total, withToolTip };
    }

    private static String toJson(Object o) {
        if (o == null) return "null";
        if (o instanceof List) {
            StringBuilder sb = new StringBuilder("[");
            List<?> list = (List<?>) o;
            for (int i = 0; i < list.size(); i++) {
                if (i > 0) sb.append(",");
                sb.append(toJson(list.get(i)));
            }
            sb.append("]");
            return sb.toString();
        }
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

    /** Log component basic info: type, name, etc. */
    private static void logComponentInfo(Component c, String javaType, String name) {
        LOG.info("[component] type=" + (javaType != null ? javaType : "null")
                + " name=" + (name != null && !name.isEmpty() ? name : "null")
                + " id=c-" + System.identityHashCode(c));
    }

    /** Log all properties of a ToolButton component using reflection. */
    private static void logAllProperties(Component c, String javaType) {
        if (c == null) return;
        LOG.info("[ToolButton properties] type=" + javaType + " id=c-" + System.identityHashCode(c));
        try {
            Class<?> clz = c.getClass();
            Method[] methods = clz.getMethods();
            Map<String, Object> props = new LinkedHashMap<>();
            for (Method m : methods) {
                String name = m.getName();
                if (name.startsWith("get") && m.getParameterCount() == 0 && !name.equals("getClass")) {
                    try {
                        Object val = m.invoke(c);
                        String propName = name.substring(3);
                        if (propName.length() > 0) {
                            propName = Character.toLowerCase(propName.charAt(0)) + propName.substring(1);
                            props.put(propName, val);
                        }
                    } catch (IllegalAccessException | InvocationTargetException e) {
                        // skip methods that throw
                    }
                }
            }
            for (Map.Entry<String, Object> e : props.entrySet()) {
                Object val = e.getValue();
                String valStr = val == null ? "null" : val.toString();
                if (valStr.length() > 100) {
                    valStr = valStr.substring(0, 100) + "...";
                }
                LOG.info("  " + e.getKey() + " = " + valStr);
            }
        } catch (Exception e) {
            LOG.warning("[ToolButton properties] failed to get properties: " + e.getMessage());
        }
    }
}
