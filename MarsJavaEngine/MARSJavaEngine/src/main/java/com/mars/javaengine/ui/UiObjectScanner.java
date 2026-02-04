package com.mars.javaengine.ui;

import java.awt.Color;
import java.awt.Component;
import java.awt.EventQueue;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.Window;
import java.util.ArrayList;
import java.util.Collection;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Set;
import java.util.concurrent.CountDownLatch;
import java.util.logging.Logger;
import javax.swing.AbstractButton;
import javax.swing.BorderFactory;
import javax.swing.JComponent;
import javax.swing.JTree;
import javax.swing.JWindow;
import javax.swing.JTabbedPane;
import javax.swing.JLabel;
import javax.swing.SwingUtilities;
import javax.swing.text.JTextComponent;
import javax.swing.tree.TreeModel;
import javax.swing.tree.TreePath;

public class UiObjectScanner {
    private final Logger logger;

    public UiObjectScanner(Logger logger) {
        this.logger = logger;
    }

    public List<UiObjectInfo> scanAndHighlight(int limit) {
        List<Component> components = collectComponents();
        List<UiObjectInfo> infos = new ArrayList<>();
        List<Rectangle> highlightTargets = new ArrayList<>();
        int highlightLimit = limit;
        int[] highlightedCount = new int[] {0};
        for (int i = 0; i < components.size(); i++) {
            Component component = components.get(i);
            Rectangle bounds = getOnScreenBounds(component);
            int x = bounds != null ? bounds.x : -1;
            int y = bounds != null ? bounds.y : -1;
            int width = bounds != null ? bounds.width : -1;
            int height = bounds != null ? bounds.height : -1;
            String text = extractComponentText(component);
            String javaTypePath = buildJavaTypePath(component.getClass());
            String javaNamePath = buildJavaNamePath(component, component.getName());
            UiObjectInfo info = new UiObjectInfo(
                component.getClass().getName(),
                component.getName(),
                text,
                javaTypePath,
                javaNamePath,
                x,
                y,
                width,
                height
            );
            infos.add(info);

            if (bounds != null && shouldHighlight(highlightedCount[0], highlightLimit) && isValidBounds(bounds)) {
                highlightTargets.add(bounds);
                highlightedCount[0]++;
            }

            if (component instanceof JTree) {
                appendTreeNodeInfos(
                    (JTree) component,
                    infos,
                    highlightTargets
                );
            }

            if (component instanceof JTabbedPane) {
                appendTabbedPaneInfos((JTabbedPane) component, infos, highlightTargets);
            }

            if (isJavaFxPanel(component)) {
                appendJavaFxInfos(component, infos, highlightTargets, highlightLimit, highlightedCount);
            }
        }

        appendSwtInfos(infos, highlightTargets, highlightLimit, highlightedCount);

        logger.info("UI objects scanned: " + infos.size());
        highlightSequentially(highlightTargets);
        return infos;
    }

    private List<Component> collectComponents() {
        List<Component> result = new ArrayList<>();
        try {
            if (!EventQueue.isDispatchThread()) {
                CountDownLatch latch = new CountDownLatch(1);
                EventQueue.invokeLater(() -> {
                    collectWindows(result);
                    latch.countDown();
                });
                latch.await();
            } else {
                collectWindows(result);
            }
        } catch (Exception ex) {
            logger.warning("Failed to scan UI components: " + ex.getMessage());
        }
        return result;
    }

    private void collectWindows(List<Component> result) {
        for (Window window : Window.getWindows()) {
            if (!window.isShowing()) {
                continue;
            }
            collectComponentTree(window, result);
        }
    }

    private void collectComponentTree(Component component, List<Component> result) {
        if (component == null) {
            return;
        }
        if (component.isShowing()) {
            result.add(component);
        }
        if (component instanceof java.awt.Container) {
            java.awt.Container container = (java.awt.Container) component;
            for (Component child : container.getComponents()) {
                collectComponentTree(child, result);
            }
        }
    }

    private Rectangle getOnScreenBounds(Component component) {
        try {
            if (!component.isShowing()) {
                return null;
            }
            Point location = component.getLocationOnScreen();
            return new Rectangle(location.x, location.y, component.getWidth(), component.getHeight());
        } catch (Exception ex) {
            return null;
        }
    }

    private void appendTreeNodeInfos(JTree tree, List<UiObjectInfo> infos,
                                    List<Rectangle> highlightTargets) {
        Runnable task = () -> {
            try {
                TreeModel model = tree.getModel();
                if (model == null) {
                    return;
                }
                Object root = model.getRoot();
                if (root == null) {
                    return;
                }
                TreePath rootPath = new TreePath(root);
                collectTreeNodeInfos(tree, model, root, rootPath, infos, highlightTargets);
            } catch (Exception ex) {
                logger.warning("Failed to scan JTree nodes: " + ex.getMessage());
            }
        };

        try {
            if (EventQueue.isDispatchThread()) {
                task.run();
            } else {
                SwingUtilities.invokeAndWait(task);
            }
        } catch (Exception ex) {
            logger.warning("Failed to scan JTree nodes: " + ex.getMessage());
        }
    }

    private void collectTreeNodeInfos(JTree tree, TreeModel model, Object node, TreePath path,
                                      List<UiObjectInfo> infos, List<Rectangle> highlightTargets) {
        Rectangle bounds = getTreeNodeBounds(tree, path);
        int x = bounds != null ? bounds.x : -1;
        int y = bounds != null ? bounds.y : -1;
        int width = bounds != null ? bounds.width : -1;
        int height = bounds != null ? bounds.height : -1;

        String nodeText = extractTreeNodeText(tree, model, node, path);
        String javaTypePath = buildJavaTypePath(node.getClass());
        String javaNamePath = buildTreeNodeNamePath(tree, model, path);
        UiObjectInfo info = new UiObjectInfo(
            node.getClass().getName(),
            path.toString(),
            nodeText,
            javaTypePath,
            javaNamePath,
            x,
            y,
            width,
            height
        );
        infos.add(info);

        if (bounds != null && isValidBounds(bounds)) {
            highlightTargets.add(bounds);
        }

        int childCount = model.getChildCount(node);
        for (int i = 0; i < childCount; i++) {
            Object child = model.getChild(node, i);
            TreePath childPath = path.pathByAddingChild(child);
            collectTreeNodeInfos(tree, model, child, childPath,
                infos, highlightTargets);
        }
    }

    private Rectangle getTreeNodeBounds(JTree tree, TreePath path) {
        try {
            Rectangle pathBounds = tree.getPathBounds(path);
            if (pathBounds == null) {
                return null;
            }
            Point treeLocation = tree.getLocationOnScreen();
            return new Rectangle(
                treeLocation.x + pathBounds.x,
                treeLocation.y + pathBounds.y,
                pathBounds.width,
                pathBounds.height
            );
        } catch (Exception ex) {
            return null;
        }
    }

    private boolean isValidBounds(Rectangle bounds) {
        return bounds.width > 0 && bounds.height > 0;
    }

    private boolean shouldHighlight(int currentCount, int highlightLimit) {
        return highlightLimit < 0 || currentCount < highlightLimit;
    }

    private void appendTabbedPaneInfos(JTabbedPane tabbedPane, List<UiObjectInfo> infos,
                                       List<Rectangle> highlightTargets) {
        int count = tabbedPane.getTabCount();
        for (int i = 0; i < count; i++) {
            String title = tabbedPane.getTitleAt(i);
            Rectangle bounds = tabbedPane.getBoundsAt(i);
            int x = -1;
            int y = -1;
            int width = -1;
            int height = -1;
            if (bounds != null) {
                Point location = tabbedPane.getLocationOnScreen();
                x = location.x + bounds.x;
                y = location.y + bounds.y;
                width = bounds.width;
                height = bounds.height;
                if (isValidBounds(bounds)) {
                    highlightTargets.add(new Rectangle(x, y, width, height));
                }
            }
            String baseTypePath = buildJavaTypePath(tabbedPane.getClass());
            String javaTypePath = "javax.swing.JTabbedPane$Tab"
                + (baseTypePath == null ? "" : ";" + baseTypePath);
            String javaNamePath = joinPathSegments(title, buildJavaNamePath(tabbedPane, tabbedPane.getName()));
            UiObjectInfo info = new UiObjectInfo(
                "javax.swing.JTabbedPane$Tab",
                tabbedPane.getName(),
                title,
                javaTypePath,
                javaNamePath,
                x,
                y,
                width,
                height
            );
            infos.add(info);
        }
    }

    private String extractComponentText(Component component) {
        try {
            if (component instanceof JLabel) {
                JLabel label = (JLabel) component;
                if (label.getText() != null) {
                    return label.getText();
                }
                if (label.getIcon() != null) {
                    String desc = label.getIcon().toString();
                    return desc == null ? label.getIcon().getClass().getName() : desc;
                }
            }
            if (component instanceof AbstractButton) {
                return ((AbstractButton) component).getText();
            }
            if (component instanceof JTextComponent) {
                return ((JTextComponent) component).getText();
            }
        } catch (Exception ignored) {
        }
        return null;
    }

    private String extractTreeNodeText(JTree tree, TreeModel model, Object node, TreePath path) {
        try {
            boolean selected = tree.isPathSelected(path);
            boolean expanded = tree.isExpanded(path);
            boolean leaf = model.isLeaf(node);
            int row = tree.getRowForPath(path);
            return tree.convertValueToText(node, selected, expanded, leaf, row, false);
        } catch (Exception ex) {
            return path.toString();
        }
    }

    private String buildTreeNodeNamePath(JTree tree, TreeModel model, TreePath path) {
        Object[] elements = path.getPath();
        if (elements == null || elements.length == 0) {
            return null;
        }
        List<String> names = new ArrayList<>();
        TreePath currentPath = new TreePath(elements[0]);
        String rootName = extractTreeNodeText(tree, model, elements[0], currentPath);
        if (rootName != null && !rootName.isBlank()) {
            names.add(rootName);
        }
        for (int i = 1; i < elements.length; i++) {
            currentPath = currentPath.pathByAddingChild(elements[i]);
            String name = extractTreeNodeText(tree, model, elements[i], currentPath);
            if (name != null && !name.isBlank()) {
                names.add(name);
            }
        }
        return names.isEmpty() ? null : String.join(";", names);
    }

    private boolean isJavaFxPanel(Component component) {
        return "javafx.embed.swing.JFXPanel".equals(component.getClass().getName());
    }

    private void appendJavaFxInfos(Component jfxPanel, List<UiObjectInfo> infos,
                                   List<Rectangle> highlightTargets,
                                   int highlightLimit, int[] highlightedCount) {
        try {
            Object scene = null;
            for (int i = 0; i < 5 && scene == null; i++) {
                scene = invokeNoArg(jfxPanel, "getScene");
                if (scene == null) {
                    Thread.sleep(200);
                }
            }
            if (scene == null) {
                logger.warning("JavaFX scene not available on JFXPanel");
                return;
            }
            Object finalScene = scene;
            Runnable task = () -> {
                Object root = invokeNoArg(finalScene, "getRoot");
                if (root != null) {
                    collectJavaFxNodeInfos(root, infos, highlightTargets, highlightLimit, highlightedCount);
                } else {
                    logger.warning("JavaFX scene root is null");
                }
                Object tabPaneRoot = findJavaFxTabPane(root);
                if (tabPaneRoot != null) {
                    collectJavaFxTabInfos(tabPaneRoot, infos, highlightTargets, highlightLimit, highlightedCount);
                }
            };
            runOnJavaFxThread(task);
        } catch (Exception ex) {
            logger.warning("Failed to scan JavaFX nodes: " + ex.getMessage());
        }
    }

    private void collectJavaFxNodeInfos(Object node, List<UiObjectInfo> infos,
                                        List<Rectangle> highlightTargets,
                                        int highlightLimit, int[] highlightedCount) {
        String className = node.getClass().getName();
        String name = safeString(invokeNoArg(node, "getId"));
        String text = extractJavaFxText(node);
        String javaTypePath = buildJavaTypePath(node.getClass());
        String javaNamePath = buildJavaNamePath(node, name);
        Rectangle bounds = getJavaFxNodeBounds(node);
        int x = bounds != null ? bounds.x : -1;
        int y = bounds != null ? bounds.y : -1;
        int width = bounds != null ? bounds.width : -1;
        int height = bounds != null ? bounds.height : -1;
        UiObjectInfo info = new UiObjectInfo(className, name, text, javaTypePath, javaNamePath, x, y, width, height);
        infos.add(info);

        if (bounds != null && shouldHighlight(highlightedCount[0], highlightLimit) && isValidBounds(bounds)) {
            highlightTargets.add(bounds);
            highlightedCount[0]++;
        }

        Object children = invokeNoArg(node, "getChildrenUnmodifiable");
        if (children instanceof Collection) {
            for (Object child : (Collection<?>) children) {
                collectJavaFxNodeInfos(child, infos, highlightTargets, highlightLimit, highlightedCount);
            }
        }
    }

    private Object findJavaFxTabPane(Object root) {
        if (root == null) {
            return null;
        }
        if ("javafx.scene.control.TabPane".equals(root.getClass().getName())) {
            return root;
        }
        Object children = invokeNoArg(root, "getChildrenUnmodifiable");
        if (children instanceof Collection) {
            for (Object child : (Collection<?>) children) {
                Object found = findJavaFxTabPane(child);
                if (found != null) {
                    return found;
                }
            }
        }
        return null;
    }

    private void collectJavaFxTabInfos(Object tabPane, List<UiObjectInfo> infos,
                                       List<Rectangle> highlightTargets,
                                       int highlightLimit, int[] highlightedCount) {
        Object tabs = invokeNoArg(tabPane, "getTabs");
        if (tabs instanceof Collection) {
            for (Object tab : (Collection<?>) tabs) {
                String text = safeString(invokeNoArg(tab, "getText"));
                UiObjectInfo info = new UiObjectInfo(
                    "javafx.scene.control.Tab",
                    safeString(invokeNoArg(tab, "getId")),
                    text,
                    buildJavaTypePath(tab.getClass()),
                    buildJavaNamePath(tab, safeString(invokeNoArg(tab, "getId"))),
                    -1,
                    -1,
                    -1,
                    -1
                );
                infos.add(info);
                Object content = invokeNoArg(tab, "getContent");
                if (content != null) {
                    collectJavaFxNodeInfos(content, infos, highlightTargets, highlightLimit, highlightedCount);
                }
            }
        }
    }

    private Rectangle getJavaFxNodeBounds(Object node) {
        try {
            Object boundsInLocal = invokeNoArg(node, "getBoundsInLocal");
            if (boundsInLocal == null) {
                return null;
            }
            Object screenBounds = invokeOneArg(node, "localToScreen", boundsInLocal);
            if (screenBounds == null) {
                return null;
            }
            Double minX = toDouble(invokeNoArg(screenBounds, "getMinX"));
            Double minY = toDouble(invokeNoArg(screenBounds, "getMinY"));
            Double width = toDouble(invokeNoArg(screenBounds, "getWidth"));
            Double height = toDouble(invokeNoArg(screenBounds, "getHeight"));
            if (minX == null || minY == null || width == null || height == null) {
                return null;
            }
            return new Rectangle(minX.intValue(), minY.intValue(), width.intValue(), height.intValue());
        } catch (Exception ex) {
            return null;
        }
    }

    private String extractJavaFxText(Object node) {
        String className = node.getClass().getName();
        String text = safeString(invokeNoArg(node, "getText"));
        if (text != null && !text.isBlank()) {
            return text;
        }
        if ("javafx.scene.web.WebView".equals(className)) {
            Object engine = invokeNoArg(node, "getEngine");
            if (engine != null) {
                String url = safeString(invokeNoArg(engine, "getLocation"));
                if (url != null && !url.isBlank()) {
                    return url;
                }
            }
        }
        if ("javafx.scene.text.Text".equals(className)) {
            return safeString(invokeNoArg(node, "getText"));
        }
        if ("javafx.scene.image.ImageView".equals(className)) {
            Object image = invokeNoArg(node, "getImage");
            if (image != null) {
                String url = safeString(invokeNoArg(image, "getUrl"));
                if (url != null) {
                    return url;
                }
            }
        }
        return null;
    }

    private void runOnJavaFxThread(Runnable task) {
        try {
            Class<?> platformClass = Class.forName("javafx.application.Platform");
            boolean isFxThread = (boolean) platformClass.getMethod("isFxApplicationThread").invoke(null);
            if (isFxThread) {
                task.run();
                return;
            }
            CountDownLatch latch = new CountDownLatch(1);
            platformClass.getMethod("runLater", Runnable.class).invoke(null, (Runnable) () -> {
                try {
                    task.run();
                } finally {
                    latch.countDown();
                }
            });
            latch.await();
        } catch (Exception ex) {
            task.run();
        }
    }

    private void appendSwtInfos(List<UiObjectInfo> infos, List<Rectangle> highlightTargets,
                                int highlightLimit, int[] highlightedCount) {
        try {
            Class<?> displayClass = Class.forName("org.eclipse.swt.widgets.Display");
            Object display = invokeStaticNoArg(displayClass, "getCurrent");
            if (display == null) {
                display = invokeStaticNoArg(displayClass, "getDefault");
            }
            if (display == null) {
                return;
            }
            Object shells = invokeNoArg(display, "getShells");
            if (shells != null && shells.getClass().isArray()) {
                int length = java.lang.reflect.Array.getLength(shells);
                for (int i = 0; i < length; i++) {
                    Object shell = java.lang.reflect.Array.get(shells, i);
                    collectSwtWidgetInfos(shell, infos, highlightTargets, highlightLimit, highlightedCount);
                }
            }
        } catch (Exception ex) {
            logger.warning("Failed to scan SWT widgets: " + ex.getMessage());
        }
    }

    private void collectSwtWidgetInfos(Object widget, List<UiObjectInfo> infos,
                                       List<Rectangle> highlightTargets,
                                       int highlightLimit, int[] highlightedCount) {
        if (widget == null) {
            return;
        }
        String className = widget.getClass().getName();
        String name = extractObjectName(widget);
        String text = extractSwtText(widget);
        String javaTypePath = buildJavaTypePath(widget.getClass());
        String javaNamePath = buildJavaNamePath(widget, name);
        Rectangle bounds = extractSwtBounds(widget);
        int x = bounds != null ? bounds.x : -1;
        int y = bounds != null ? bounds.y : -1;
        int width = bounds != null ? bounds.width : -1;
        int height = bounds != null ? bounds.height : -1;

        UiObjectInfo info = new UiObjectInfo(className, name, text, javaTypePath, javaNamePath, x, y, width, height);
        infos.add(info);

        if (bounds != null && shouldHighlight(highlightedCount[0], highlightLimit) && isValidBounds(bounds)) {
            highlightTargets.add(bounds);
            highlightedCount[0]++;
        }

        if ("org.eclipse.swt.widgets.Tree".equals(className)) {
            collectSwtTreeItems(widget, infos, highlightTargets, highlightLimit, highlightedCount);
        }
        if ("org.eclipse.swt.widgets.TabFolder".equals(className)) {
            collectSwtTabItems(widget, infos, highlightTargets, highlightLimit, highlightedCount);
        }

        Object children = invokeNoArg(widget, "getChildren");
        if (children != null && children.getClass().isArray()) {
            int length = java.lang.reflect.Array.getLength(children);
            for (int i = 0; i < length; i++) {
                Object child = java.lang.reflect.Array.get(children, i);
                collectSwtWidgetInfos(child, infos, highlightTargets, highlightLimit, highlightedCount);
            }
        }
    }

    private void collectSwtTreeItems(Object tree, List<UiObjectInfo> infos,
                                     List<Rectangle> highlightTargets,
                                     int highlightLimit, int[] highlightedCount) {
        Object items = invokeNoArg(tree, "getItems");
        if (items != null && items.getClass().isArray()) {
            int length = java.lang.reflect.Array.getLength(items);
            for (int i = 0; i < length; i++) {
                Object item = java.lang.reflect.Array.get(items, i);
                collectSwtTreeItemInfos(item, infos, highlightTargets, highlightLimit, highlightedCount);
            }
        }
    }

    private void collectSwtTreeItemInfos(Object item, List<UiObjectInfo> infos,
                                         List<Rectangle> highlightTargets,
                                         int highlightLimit, int[] highlightedCount) {
        String text = extractSwtText(item);
        Rectangle bounds = extractSwtBounds(item);
        int x = bounds != null ? bounds.x : -1;
        int y = bounds != null ? bounds.y : -1;
        int width = bounds != null ? bounds.width : -1;
        int height = bounds != null ? bounds.height : -1;

        UiObjectInfo info = new UiObjectInfo(
            "org.eclipse.swt.widgets.TreeItem",
            null,
            text,
            buildJavaTypePath(item.getClass()),
            buildJavaNamePath(item, null),
            x,
            y,
            width,
            height
        );
        infos.add(info);

        if (bounds != null && shouldHighlight(highlightedCount[0], highlightLimit) && isValidBounds(bounds)) {
            highlightTargets.add(bounds);
            highlightedCount[0]++;
        }

        Object children = invokeNoArg(item, "getItems");
        if (children != null && children.getClass().isArray()) {
            int length = java.lang.reflect.Array.getLength(children);
            for (int i = 0; i < length; i++) {
                Object child = java.lang.reflect.Array.get(children, i);
                collectSwtTreeItemInfos(child, infos, highlightTargets, highlightLimit, highlightedCount);
            }
        }
    }

    private void collectSwtTabItems(Object tabFolder, List<UiObjectInfo> infos,
                                    List<Rectangle> highlightTargets,
                                    int highlightLimit, int[] highlightedCount) {
        Object items = invokeNoArg(tabFolder, "getItems");
        if (items != null && items.getClass().isArray()) {
            int length = java.lang.reflect.Array.getLength(items);
            for (int i = 0; i < length; i++) {
                Object item = java.lang.reflect.Array.get(items, i);
                String text = extractSwtText(item);
                UiObjectInfo info = new UiObjectInfo(
                    "org.eclipse.swt.widgets.TabItem",
                    null,
                    text,
                    buildJavaTypePath(item.getClass()),
                    buildJavaNamePath(item, null),
                    -1,
                    -1,
                    -1,
                    -1
                );
                infos.add(info);
                Object control = invokeNoArg(item, "getControl");
                if (control != null) {
                    collectSwtWidgetInfos(control, infos, highlightTargets, highlightLimit, highlightedCount);
                }
            }
        }
    }

    private String extractSwtText(Object widget) {
        String text = safeString(invokeNoArg(widget, "getText"));
        if (text != null && !text.isBlank()) {
            return text;
        }
        text = safeString(invokeNoArg(widget, "getMessage"));
        if (text != null && !text.isBlank()) {
            return text;
        }
        text = safeString(invokeNoArg(widget, "getToolTipText"));
        if (text != null && !text.isBlank()) {
            return text;
        }
        Object image = invokeNoArg(widget, "getImage");
        if (image != null) {
            return image.toString();
        }
        return null;
    }

    private Rectangle extractSwtBounds(Object widget) {
        try {
            Object swtBounds = invokeNoArg(widget, "getBounds");
            if (swtBounds == null) {
                return null;
            }
            Double x = toDouble(readField(swtBounds, "x"));
            Double y = toDouble(readField(swtBounds, "y"));
            Double width = toDouble(readField(swtBounds, "width"));
            Double height = toDouble(readField(swtBounds, "height"));
            if (x == null || y == null || width == null || height == null) {
                return null;
            }
            return new Rectangle(x.intValue(), y.intValue(), width.intValue(), height.intValue());
        } catch (Exception ex) {
            return null;
        }
    }

    private Object invokeNoArg(Object target, String method) {
        try {
            return target.getClass().getMethod(method).invoke(target);
        } catch (Exception ex) {
            return null;
        }
    }

    private Object invokeOneArg(Object target, String method, Object arg) {
        try {
            for (var candidate : target.getClass().getMethods()) {
                if (!candidate.getName().equals(method)) {
                    continue;
                }
                if (candidate.getParameterCount() != 1) {
                    continue;
                }
                Class<?> paramType = candidate.getParameterTypes()[0];
                if (paramType.isAssignableFrom(arg.getClass())) {
                    return candidate.invoke(target, arg);
                }
            }
        } catch (Exception ex) {
            return null;
        }
        return null;
    }

    private Double toDouble(Object value) {
        if (value instanceof Number) {
            return ((Number) value).doubleValue();
        }
        return null;
    }

    private String safeString(Object value) {
        return value == null ? null : String.valueOf(value);
    }

    private Object invokeStaticNoArg(Class<?> type, String method) {
        try {
            return type.getMethod(method).invoke(null);
        } catch (Exception ex) {
            return null;
        }
    }

    private Object readField(Object target, String fieldName) {
        try {
            java.lang.reflect.Field field = target.getClass().getField(fieldName);
            return field.get(target);
        } catch (Exception ex) {
            return null;
        }
    }

    private String buildJavaTypePath(Class<?> type) {
        if (type == null) {
            return null;
        }
        List<String> parts = new ArrayList<>();
        Class<?> current = type;
        while (current != null) {
            parts.add(current.getName());
            if (isTypePathStop(current.getName())) {
                break;
            }
            current = current.getSuperclass();
        }
        return parts.isEmpty() ? null : String.join(";", parts);
    }

    private boolean isTypePathStop(String className) {
        return className.startsWith("java.awt.")
            || className.startsWith("javax.swing.")
            || className.startsWith("javafx.scene.")
            || className.startsWith("org.eclipse.swt.");
    }

    private String buildJavaNamePath(Object obj, String name) {
        List<String> names = new ArrayList<>();
        String selfName = normalizeName(name);
        if (selfName == null) {
            selfName = extractObjectName(obj);
        }
        if (selfName != null) {
            names.add(selfName);
        }
        Set<Object> seen = java.util.Collections.newSetFromMap(new IdentityHashMap<>());
        seen.add(obj);
        Object parent = resolveParent(obj);
        int depth = 0;
        while (parent != null && depth < 32 && !seen.contains(parent)) {
            seen.add(parent);
            String parentName = extractObjectName(parent);
            if (parentName != null) {
                names.add(parentName);
            }
            parent = resolveParent(parent);
            depth++;
        }
        return names.isEmpty() ? null : String.join(";", names);
    }

    private String joinPathSegments(String first, String second) {
        String a = normalizeName(first);
        String b = normalizeName(second);
        if (a == null) {
            return b;
        }
        if (b == null) {
            return a;
        }
        return a + ";" + b;
    }

    private String normalizeName(String name) {
        if (name == null) {
            return null;
        }
        String trimmed = name.trim();
        return trimmed.isEmpty() ? null : trimmed;
    }

    private String extractObjectName(Object obj) {
        if (obj == null) {
            return null;
        }
        if (obj instanceof Component) {
            return normalizeName(((Component) obj).getName());
        }
        String name = normalizeName(safeString(invokeNoArg(obj, "getName")));
        if (name != null) {
            return name;
        }
        name = normalizeName(safeString(invokeNoArg(obj, "getId")));
        if (name != null) {
            return name;
        }
        Object data = invokeNoArg(obj, "getData");
        return normalizeName(data == null ? null : String.valueOf(data));
    }

    private Object resolveParent(Object obj) {
        if (obj instanceof Component) {
            return ((Component) obj).getParent();
        }
        Object parent = invokeNoArg(obj, "getParent");
        if (parent != null) {
            return parent;
        }
        parent = invokeNoArg(obj, "getParentItem");
        if (parent != null) {
            return parent;
        }
        return null;
    }

    private void highlightSequentially(List<Rectangle> boundsList) {
        Thread t = new Thread(() -> {
            try {
                for (Rectangle bounds : boundsList) {
                    JWindow window = createHighlightWindow(bounds);
                    for (int i = 0; i < 3; i++) {
                        showWindow(window, true);
                        Thread.sleep(200);
                        showWindow(window, false);
                        Thread.sleep(200);
                    }
                    showWindow(window, false);
                    disposeWindow(window);
                }
            } catch (Exception ex) {
                logger.warning("Highlight failed: " + ex.getMessage());
            }
        }, "MarsJavaEngine-highlight");
        t.setDaemon(true);
        t.start();
    }

    private JWindow createHighlightWindow(Rectangle bounds) throws Exception {
        final JWindow[] holder = new JWindow[1];
        SwingUtilities.invokeAndWait(() -> {
            JWindow window = new JWindow();
            window.setAlwaysOnTop(true);
            window.setFocusableWindowState(false);
            window.setBackground(new Color(255, 0, 0, 60));
            if (window.getContentPane() instanceof JComponent) {
                JComponent component = (JComponent) window.getContentPane();
                component.setBorder(BorderFactory.createLineBorder(Color.RED, 2));
                component.setOpaque(false);
            }
            window.setBounds(bounds);
            holder[0] = window;
        });
        return holder[0];
    }

    private void showWindow(JWindow window, boolean visible) throws Exception {
        SwingUtilities.invokeAndWait(() -> window.setVisible(visible));
    }

    private void disposeWindow(JWindow window) throws Exception {
        SwingUtilities.invokeAndWait(window::dispose);
    }
}
