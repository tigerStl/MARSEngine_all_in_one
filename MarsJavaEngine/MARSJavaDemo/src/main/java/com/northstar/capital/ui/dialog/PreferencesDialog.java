package com.northstar.capital.ui.dialog;

import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.common.FormGrid;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JComboBox;
import javax.swing.JDialog;
import javax.swing.JPanel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.util.List;
import java.util.function.Consumer;

public class PreferencesDialog extends JDialog {
    public PreferencesDialog(Frame owner, ApplicationContext context, Consumer<String> onUserChange) {
        super(owner, "Preferences", true);
        setName("dialog.preferences");
        setSize(360, 180);
        setLocationRelativeTo(owner);

        JComboBox<String> userCombo = UiFactory.combo("preferences.user", List.of("TRADER01", "OPS01", "RISK01"));
        userCombo.setSelectedItem(context.getCurrentUser());

        JPanel form = UiFactory.titled("Session", "preferences.form");
        FormGrid grid = new FormGrid(form);
        grid.add(UiFactory.label("Current User", "label.preferences.user"), userCombo);

        JButton save = UiFactory.button("Save", "preferences.save");
        save.addActionListener(e -> {
            context.setCurrentUser(String.valueOf(userCombo.getSelectedItem()));
            onUserChange.accept(context.getCurrentUser());
            dispose();
        });
        JPanel south = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(save);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(form, BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }
}
