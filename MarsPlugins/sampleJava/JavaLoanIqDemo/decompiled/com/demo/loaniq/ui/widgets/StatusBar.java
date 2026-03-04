/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui.widgets;

import java.awt.FlowLayout;
import javax.swing.BorderFactory;
import javax.swing.Box;
import javax.swing.JLabel;
import javax.swing.JPanel;

public class StatusBar
extends JPanel {
    private final JLabel envLabel;
    private final JLabel userLabel;
    private final JLabel messageLabel;

    public StatusBar() {
        this.setLayout(new FlowLayout(0));
        this.setBorder(BorderFactory.createEtchedBorder());
        this.envLabel = new JLabel("ENV: ");
        this.envLabel.setName("LIQ_STATUS_ENV");
        this.userLabel = new JLabel("User: ");
        this.userLabel.setName("LIQ_STATUS_USER");
        this.messageLabel = new JLabel(" ");
        this.messageLabel.setName("LIQ_STATUS_MESSAGE");
        this.add(this.envLabel);
        this.add(Box.createHorizontalStrut(20));
        this.add(this.userLabel);
        this.add(Box.createHorizontalStrut(20));
        this.add(this.messageLabel);
    }

    public void setEnv(String env) {
        this.envLabel.setText("ENV: " + (env != null ? env : ""));
    }

    public void setUser(String user) {
        this.userLabel.setText("User: " + (user != null ? user : ""));
    }

    public void setMessage(String msg) {
        this.messageLabel.setText(msg != null ? msg : "");
    }
}

