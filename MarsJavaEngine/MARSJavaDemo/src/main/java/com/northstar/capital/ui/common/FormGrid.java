package com.northstar.capital.ui.common;

import javax.swing.JComponent;
import javax.swing.JPanel;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;

public class FormGrid {
    private final JPanel panel;
    private final GridBagConstraints gbc = new GridBagConstraints();
    private int row;

    public FormGrid(JPanel panel) {
        this.panel = panel;
        this.panel.setLayout(new GridBagLayout());
        gbc.insets = new Insets(2, 4, 2, 4);
        gbc.anchor = GridBagConstraints.WEST;
    }

    public void add(JComponent label, JComponent field) {
        add(label, field, 1.0, 1);
    }

    public void add(JComponent label, JComponent field, double weight, int fieldWidth) {
        gbc.gridy = row;
        gbc.gridx = 0;
        gbc.weightx = 0;
        gbc.gridwidth = 1;
        gbc.fill = GridBagConstraints.NONE;
        panel.add(label, gbc);

        gbc.gridx = 1;
        gbc.weightx = weight;
        gbc.gridwidth = fieldWidth;
        gbc.fill = GridBagConstraints.HORIZONTAL;
        panel.add(field, gbc);
        row++;
    }

    public void addPair(JComponent leftLabel, JComponent leftField, JComponent rightLabel, JComponent rightField) {
        gbc.gridy = row;
        gbc.gridwidth = 1;
        gbc.fill = GridBagConstraints.NONE;
        gbc.weightx = 0;
        gbc.gridx = 0;
        panel.add(leftLabel, gbc);
        gbc.gridx = 1;
        gbc.weightx = 0.5;
        gbc.fill = GridBagConstraints.HORIZONTAL;
        panel.add(leftField, gbc);
        gbc.gridx = 2;
        gbc.weightx = 0;
        gbc.fill = GridBagConstraints.NONE;
        panel.add(rightLabel, gbc);
        gbc.gridx = 3;
        gbc.weightx = 0.5;
        gbc.fill = GridBagConstraints.HORIZONTAL;
        panel.add(rightField, gbc);
        row++;
    }

    public void addFull(JComponent component) {
        gbc.gridy = row++;
        gbc.gridx = 0;
        gbc.gridwidth = 4;
        gbc.weightx = 1;
        gbc.fill = GridBagConstraints.HORIZONTAL;
        panel.add(component, gbc);
        gbc.gridwidth = 1;
    }

    public void spacer() {
        gbc.gridy = row++;
        gbc.gridx = 4;
        gbc.weightx = 0.01;
        gbc.weighty = 0;
        panel.add(new JPanel(), gbc);
        gbc.weighty = 0;
    }
}
