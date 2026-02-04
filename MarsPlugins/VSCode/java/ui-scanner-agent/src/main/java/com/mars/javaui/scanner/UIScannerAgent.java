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

        LOG.fine("scanComponent id=" + id + " parentId=" + parentId + " javaType=" + javaType);

        String text = getText(c);
        if (text != null && !text.isEmpty()) {
            node.put("text", text);
        }

        String name = c.getName();
        if (name != null && !name.isEmpty()) {
            node.put("name", name);
        }

        String caption = getCaption(c);
        if (caption != null && !caption.isEmpty()) {
            node.put("caption", caption);
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

    private static String getText(Component c) {
        try {
            Method m = c.getClass().getMethod("getText");
            Object v = m.invoke(c);
            return v != null ? v.toString() : null;
        } catch (NoSuchMethodException | IllegalAccessException | InvocationTargetException e) {
            return null;
        }
    }

    private static String getCaption(Component c) {
        if (c instanceof Frame) {
            return ((Frame) c).getTitle();
        }
        if (c instanceof Dialog) {
            return ((Dialog) c).getTitle();
        }
        return null;
    }

    private static void writeOutput(String path, List<Map<String, Object>> roots) throws IOException {
        StringBuilder sb = new StringBuilder();
        sb.append("{\"roots\":");
        sb.append(toJson(roots));
        sb.append("}");
        try (Writer w = new OutputStreamWriter(new FileOutputStream(path), "UTF-8")) {
            w.write(sb.toString());
        }
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
}
