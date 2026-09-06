package com.northstar.capital.ui.common;

import com.northstar.capital.ui.Theme;

import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JFormattedTextField;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JRadioButton;
import javax.swing.JTextField;
import javax.swing.border.Border;
import java.awt.Color;
import java.text.Format;
import java.util.Collection;

public final class UiFactory {
    private UiFactory() {
    }

    public static JLabel label(String text, String name) {
        JLabel label = new JLabel(text);
        label.setName(name);
        label.setFont(Theme.UI_FONT);
        label.getAccessibleContext().setAccessibleName(text);
        return label;
    }

    public static JTextField text(String name, int columns) {
        JTextField field = new JTextField(columns);
        field.setName(name);
        field.setFont(Theme.UI_FONT);
        field.setBorder(Theme.fieldBorder());
        field.getAccessibleContext().setAccessibleName(name);
        return field;
    }

    public static JFormattedTextField formatted(String name, Format format, int columns) {
        JFormattedTextField field = new JFormattedTextField(format);
        field.setName(name);
        field.setColumns(columns);
        field.setFont(Theme.UI_FONT);
        field.setBorder(Theme.fieldBorder());
        field.getAccessibleContext().setAccessibleName(name);
        return field;
    }

    public static JComboBox<String> combo(String name, Collection<String> values) {
        JComboBox<String> combo = new JComboBox<>(values.toArray(String[]::new));
        combo.setName(name);
        combo.setFont(Theme.UI_FONT);
        combo.getAccessibleContext().setAccessibleName(name);
        return combo;
    }

    public static JCheckBox check(String text, String name) {
        JCheckBox box = new JCheckBox(text);
        box.setName(name);
        box.setFont(Theme.UI_FONT);
        box.setOpaque(false);
        box.getAccessibleContext().setAccessibleName(text);
        return box;
    }

    public static JRadioButton radio(String text, String name) {
        JRadioButton button = new JRadioButton(text);
        button.setName(name);
        button.setFont(Theme.UI_FONT);
        button.setOpaque(false);
        button.getAccessibleContext().setAccessibleName(text);
        return button;
    }

    public static JButton button(String text, String name) {
        JButton button = new JButton(text);
        button.setName(name);
        button.setFont(Theme.UI_FONT);
        button.setMargin(new java.awt.Insets(2, 8, 2, 8));
        button.getAccessibleContext().setAccessibleName(text);
        return button;
    }

    public static JPanel titled(String title, String name) {
        JPanel panel = new JPanel();
        panel.setName(name);
        panel.setBackground(Theme.PANEL_BG);
        panel.setBorder(Theme.sectionBorder(title));
        panel.setAlignmentX(java.awt.Component.LEFT_ALIGNMENT);
        panel.setMaximumSize(new java.awt.Dimension(Integer.MAX_VALUE, Short.MAX_VALUE));
        return panel;
    }

    public static void markInvalid(JComponent component, boolean invalid) {
        Border border = invalid ? Theme.invalidBorder() : Theme.fieldBorder();
        component.setBorder(border);
        if (invalid) {
            component.setBackground(new Color(0xFFF4F2));
        } else if (component.isEnabled()) {
            component.setBackground(Color.WHITE);
        }
    }
}
