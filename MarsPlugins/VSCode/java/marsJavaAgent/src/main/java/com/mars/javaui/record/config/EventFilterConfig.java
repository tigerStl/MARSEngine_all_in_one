package com.mars.javaui.record.config;

import java.awt.Component;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashSet;
import java.util.Set;

/**
 * Configurable set of class names for which we do NOT handle mouse/keyboard events
 * and do NOT produce test steps or visual nodes. Typically Panel and similar containers.
 */
public final class EventFilterConfig {

    /** Class simple names or full names that skip mouse/keyboard (no test step, no visual node). */
    private static final Set<String> SKIP_MOUSE_KEYBOARD_SIMPLE = new HashSet<>(Arrays.asList(
        "Panel", "JPanel", "JScrollPane", "JSplitPane", "JSeparator", "JToolBar",
        "JRootPane", "JLayeredPane", "JViewport", "JDesktopPane", "JInternalFrame"
    ));

    /** Substring matches for class name (e.g. "Renderer", "StatusBar"). */
    private static final Set<String> SKIP_MOUSE_KEYBOARD_SUBSTRINGS = new HashSet<>(Arrays.asList(
        "Renderer", "StatusBar", "CellRenderer", "TableHeader"
    ));

    /**
     * Returns true if the component's class should skip mouse/keyboard handling.
     * For such components we do not produce test steps and do not produce visual nodes.
     */
    public static boolean shouldSkipMouseKeyboard(Component c) {
        if (c == null) return true;
        Class<?> clz = c.getClass();
        String simple = clz.getSimpleName();
        String full = clz.getName();
        if (SKIP_MOUSE_KEYBOARD_SIMPLE.contains(simple) || SKIP_MOUSE_KEYBOARD_SIMPLE.contains(full)) {
            return true;
        }
        for (String sub : SKIP_MOUSE_KEYBOARD_SUBSTRINGS) {
            if (simple.contains(sub) || full.contains(sub)) return true;
        }
        if (c instanceof javax.swing.JLabel && !c.isFocusable()) {
            return true;
        }
        return false;
    }

    /** Add a class simple name to the skip set (e.g. from config file). */
    public static void addSkipClass(String simpleOrFullName) {
        SKIP_MOUSE_KEYBOARD_SIMPLE.add(simpleOrFullName);
    }

    public static Set<String> getSkipSimpleNames() {
        return Collections.unmodifiableSet(SKIP_MOUSE_KEYBOARD_SIMPLE);
    }
}
