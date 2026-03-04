/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq;

import com.demo.loaniq.ui.MainFrame;
import javax.swing.SwingUtilities;

public class LoanIQDemoApp {
    public static void main(String[] args) {
        System.setProperty("java.awt.headless", "false");
        SwingUtilities.invokeLater(() -> {
            MainFrame frame = new MainFrame();
            frame.setVisible(true);
        });
    }
}

