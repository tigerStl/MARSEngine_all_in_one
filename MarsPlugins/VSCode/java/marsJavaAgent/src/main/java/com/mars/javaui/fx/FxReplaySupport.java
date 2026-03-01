package com.mars.javaui.fx;

import java.awt.Robot;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.util.Map;

/**
 * JavaFX replay by screen bounds (isolated from Swing/AWT).
 * Uses Robot at (cx,cy) from objectKey.screenBounds; no Node resolution.
 */
public final class FxReplaySupport {

    private FxReplaySupport() {}

    /** Callbacks for typing/clearing text and step data. Implemented by RecordAgent. */
    public interface FxReplayCallbacks {
        String getStepData(Object step);
        void typeText(Robot robot, String text);
        void clearFocusedText(Robot robot, int deleteCount);
        String sanitizeFillEditInput(String data);
    }

    public static boolean isJavaFxObjectKey(Map<String, Object> key) {
        if (key == null) return false;
        Object jt = key.get("javaType");
        if (jt == null) return false;
        String t = String.valueOf(jt);
        return t.startsWith("javafx.");
    }

    @SuppressWarnings("unchecked")
    public static int[] getScreenBoundsFromObjectKey(Map<String, Object> key) {
        if (key == null) return null;
        Object sb = key.get("screenBounds");
        if (!(sb instanceof Map)) {
            sb = key.get("bounds");
        }
        if (!(sb instanceof Map)) return null;
        Map<String, Object> m = (Map<String, Object>) sb;
        Integer x = parseAnyInt(m.get("x"));
        Integer y = parseAnyInt(m.get("y"));
        Integer w = parseAnyInt(m.containsKey("width") ? m.get("width") : m.get("w"));
        Integer h = parseAnyInt(m.containsKey("height") ? m.get("height") : m.get("h"));
        if (x == null || y == null || w == null || h == null) return null;
        return new int[]{x, y, w, h};
    }

    private static Integer parseAnyInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(v)));
        } catch (NumberFormatException e) {
            return null;
        }
    }

    /**
     * Replay one JavaFX step by Robot at screen bounds center.
     *
     * @param objectKey step object identifier (must contain screenBounds)
     * @param keyword   step keyword
     * @param step      full step (for data); pass-through to callbacks
     * @param robot     AWT Robot
     * @param callbacks typing/clear/sanitize (from RecordAgent)
     * @return null on success, error message on failure
     */
    public static String replayJavaFxByBounds(
            Map<String, Object> objectKey,
            String keyword,
            Object step,
            Robot robot,
            FxReplayCallbacks callbacks) {
        int[] b = getScreenBoundsFromObjectKey(objectKey);
        if (b == null || b[2] <= 0 || b[3] <= 0) {
            return "JavaFX object has no screenBounds";
        }
        int cx = b[0] + b[2] / 2;
        int cy = b[1] + b[3] / 2;
        String data = callbacks != null ? callbacks.getStepData(step) : null;
        try {
            if ("FillEdit".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                String text = callbacks != null ? callbacks.sanitizeFillEditInput(data) : (data != null ? data : "");
                if (callbacks != null) callbacks.clearFocusedText(robot, 30);
                if (text != null && !text.isEmpty() && callbacks != null) callbacks.typeText(robot, text);
                robot.keyPress(KeyEvent.VK_ENTER);
                robot.keyRelease(KeyEvent.VK_ENTER);
                robot.delay(120);
                return null;
            }
            if ("DoubleClickButton".equals(keyword) || "DoubleClick".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(60);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                return null;
            }
            if ("SelectDropList".equals(keyword) || "SelectDropDown".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(120);
                String text = callbacks != null ? callbacks.sanitizeFillEditInput(data) : (data != null ? data : "");
                if (text != null && !text.isEmpty() && callbacks != null) {
                    callbacks.typeText(robot, text);
                    robot.keyPress(KeyEvent.VK_ENTER);
                    robot.keyRelease(KeyEvent.VK_ENTER);
                }
                robot.delay(120);
                return null;
            }
            if ("SelectMenuItem".equals(keyword) || "SelectPopupMenu".equals(keyword) || "SelectListItem".equals(keyword)
                    || "SelectTreeList".equals(keyword) || "SetRadioBox".equals(keyword) || "SetCheckBox".equals(keyword)
                    || "ClickButton".equals(keyword)) {
                robot.mouseMove(cx, cy);
                robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
                robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
                robot.delay(150);
                return null;
            }
            robot.mouseMove(cx, cy);
            robot.mousePress(InputEvent.BUTTON1_DOWN_MASK);
            robot.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
            robot.delay(120);
            return null;
        } catch (Exception e) {
            return "JavaFX replay failed: " + e.getMessage();
        }
    }
}
