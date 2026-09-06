package com.northstar.capital.ui;

import com.northstar.capital.model.AppEnvironment;
import com.northstar.capital.service.ApplicationContext;

import javax.swing.BorderFactory;
import javax.swing.JLabel;
import javax.swing.JPanel;
import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Dimension;

public class HeaderPanel extends JPanel {
    private final JLabel titleLabel = new JLabel(ApplicationContext.APP_NAME);
    private final JLabel userLabel = new JLabel();
    private final JLabel warningLabel = new JLabel("DEMO ENVIRONMENT — NO REAL TRADES");

    public HeaderPanel(ApplicationContext context) {
        setName("headerPanel");
        setLayout(new BorderLayout());
        setBackground(Theme.HEADER_BG);
        setPreferredSize(new Dimension(100, 28));
        setBorder(BorderFactory.createEmptyBorder(4, 10, 4, 10));

        titleLabel.setName("header.title");
        titleLabel.setForeground(Theme.HEADER_FG);
        titleLabel.setFont(Theme.HEADER_FONT);

        userLabel.setName("header.user");
        userLabel.setForeground(Theme.ACCENT);
        userLabel.setFont(Theme.UI_FONT_BOLD);

        warningLabel.setName("header.prodWarning");
        warningLabel.setForeground(Color.WHITE);
        warningLabel.setFont(Theme.UI_FONT_BOLD);
        warningLabel.setOpaque(true);
        warningLabel.setBackground(Theme.PROD_WARN);
        warningLabel.setBorder(BorderFactory.createEmptyBorder(1, 8, 1, 8));
        warningLabel.setVisible(false);

        JPanel right = new JPanel(new BorderLayout(12, 0));
        right.setOpaque(false);
        right.add(warningLabel, BorderLayout.WEST);
        right.add(userLabel, BorderLayout.EAST);

        add(titleLabel, BorderLayout.WEST);
        add(right, BorderLayout.EAST);
        refresh(context);
    }

    public void refresh(ApplicationContext context) {
        userLabel.setText("User: " + context.getCurrentUser());
        warningLabel.setVisible(context.getEnvironment() == AppEnvironment.PROD);
        setBackground(context.getEnvironment() == AppEnvironment.PROD ? Theme.PROD_WARN : Theme.HEADER_BG);
    }
}
