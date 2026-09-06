package com.northstar.capital.ui.common;

import com.northstar.capital.ui.Theme;

import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JTextField;
import java.awt.BorderLayout;
import java.awt.FlowLayout;

public class PlaceholderPanel extends JPanel {
    public PlaceholderPanel(String name, String title, String message) {
        setName(name);
        setLayout(new BorderLayout());
        setBackground(Theme.WORKSPACE_BG);
        JPanel inner = UiFactory.titled(title, name + ".section");
        inner.setLayout(new FlowLayout(FlowLayout.LEFT, 8, 8));
        JLabel label = UiFactory.label(message, name + ".message");
        JTextField note = UiFactory.text(name + ".note", 40);
        note.setText("View-only workspace in this demo release.");
        inner.add(label);
        inner.add(note);
        add(inner, BorderLayout.NORTH);
    }
}
