package com.mars.javaui.highlight;

import javax.swing.*;
import java.awt.*;
import java.lang.instrument.Instrumentation;

/**
 * JVM Agent: draws a red flashing box at screen position (x,y,w,h).
 * agentArgs format: "x,y,width,height" (screen coordinates).
 * Flashes 3 times then exits.
 */
public class HighlightAgent {

    public static void agentmain(String agentArgs, Instrumentation inst) {
        run(agentArgs);
    }

    public static void premain(String agentArgs, Instrumentation inst) {
        run(agentArgs);
    }

    private static void run(String agentArgs) {
        if (agentArgs == null || agentArgs.isEmpty()) return;
        String[] parts = agentArgs.split(",");
        if (parts.length != 4) return;
        int x, y, w, h;
        try {
            x = Integer.parseInt(parts[0].trim());
            y = Integer.parseInt(parts[1].trim());
            w = Integer.parseInt(parts[2].trim());
            h = Integer.parseInt(parts[3].trim());
        } catch (NumberFormatException e) {
            return;
        }
        if (w <= 0 || h <= 0) return;

        try {
            EventQueue.invokeAndWait(() -> {
                JWindow win = new JWindow();
                win.setSize(w, h);
                win.setLocation(x, y);
                win.setAlwaysOnTop(true);
                win.getRootPane().setOpaque(false);
                win.getContentPane().setBackground(new Color(0, 0, 0, 0));
                JPanel panel = new JPanel() {
                    @Override
                    protected void paintComponent(Graphics g) {
                        Graphics2D g2 = (Graphics2D) g;
                        g2.setColor(Color.RED);
                        g2.setStroke(new BasicStroke(3));
                        g2.drawRect(1, 1, getWidth() - 2, getHeight() - 2);
                    }
                };
                panel.setOpaque(false);
                win.getContentPane().add(panel);
                try {
                    win.setOpacity(0.85f);
                } catch (UnsupportedOperationException ignored) { }
                for (int i = 0; i < 3; i++) {
                    win.setVisible(true);
                    try { Thread.sleep(200); } catch (InterruptedException ignored) { }
                    win.setVisible(false);
                    try { Thread.sleep(150); } catch (InterruptedException ignored) { }
                }
                win.setVisible(true);
                try { Thread.sleep(300); } catch (InterruptedException ignored) { }
                win.dispose();
            });
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
