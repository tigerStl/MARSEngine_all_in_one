package com.northstar.capital.ui;

import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;

import javax.swing.BorderFactory;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.Timer;
import javax.swing.SwingConstants;
import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.time.LocalTime;

public class StatusBar extends JPanel {
    private final JLabel environmentLabel = new JLabel();
    private final JLabel userLabel = new JLabel();
    private final JLabel serverLabel = new JLabel();
    private final JLabel connectionLabel = new JLabel();
    private final JLabel timeLabel = new JLabel();
    private final JLabel messageLabel = new JLabel();

    public StatusBar(ApplicationContext context) {
        setName("statusBar");
        setLayout(new BorderLayout());
        setBackground(Theme.STATUS_BG);
        setPreferredSize(new Dimension(100, 24));
        setBorder(BorderFactory.createMatteBorder(1, 0, 0, 0, new Color(0x0E1726)));

        JPanel left = new JPanel(new FlowLayout(FlowLayout.LEFT, 12, 2));
        left.setOpaque(false);
        style(environmentLabel, "status.environment");
        style(userLabel, "status.user");
        style(serverLabel, "status.server");
        style(connectionLabel, "status.connection");
        style(timeLabel, "status.time");
        left.add(environmentLabel);
        left.add(separator());
        left.add(userLabel);
        left.add(separator());
        left.add(serverLabel);
        left.add(separator());
        left.add(connectionLabel);
        left.add(separator());
        left.add(timeLabel);

        messageLabel.setName("status.message");
        messageLabel.setForeground(Theme.HEADER_FG);
        messageLabel.setFont(Theme.UI_FONT);
        messageLabel.setHorizontalAlignment(SwingConstants.RIGHT);
        messageLabel.setBorder(BorderFactory.createEmptyBorder(0, 0, 0, 10));

        add(left, BorderLayout.WEST);
        add(messageLabel, BorderLayout.CENTER);
        refresh(context);
        Timer timer = new Timer(1000, e -> timeLabel.setText(MoneyFormats.formatTime(LocalTime.now())));
        timer.start();
    }

    public void refresh(ApplicationContext context) {
        environmentLabel.setText("Environment: " + context.getEnvironment().name());
        userLabel.setText("User: " + context.getCurrentUser());
        serverLabel.setText("Server: " + context.getServerName());
        connectionLabel.setText("Connection: Connected");
        timeLabel.setText(MoneyFormats.formatTime(LocalTime.now()));
        setMessage(context.getStatusMessage());
    }

    public void setMessage(String message) {
        messageLabel.setText(message == null ? "" : message);
    }

    private static void style(JLabel label, String name) {
        label.setName(name);
        label.setForeground(Theme.HEADER_FG);
        label.setFont(Theme.UI_FONT);
    }

    private static JLabel separator() {
        JLabel label = new JLabel("|");
        label.setForeground(new Color(0x6C7A92));
        label.setFont(Theme.UI_FONT);
        return label;
    }
}
