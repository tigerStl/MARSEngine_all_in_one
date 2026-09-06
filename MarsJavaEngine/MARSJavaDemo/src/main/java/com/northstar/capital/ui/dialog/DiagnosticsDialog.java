package com.northstar.capital.ui.dialog;

import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.common.FormGrid;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JPanel;
import javax.swing.JTextField;
import javax.swing.SwingUtilities;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;

public class DiagnosticsDialog extends JDialog {
    public DiagnosticsDialog(Frame owner, ApplicationContext context) {
        super(owner, "Diagnostics", true);
        setName("dialog.diagnostics");
        setSize(460, 320);
        setLocationRelativeTo(owner);

        JPanel form = UiFactory.titled("Runtime", "diagnostics.form");
        FormGrid grid = new FormGrid(form);
        grid.add(UiFactory.label("Java Version", "label.diagnostics.java"), readonly("diagnostics.java", System.getProperty("java.version")));
        grid.add(UiFactory.label("Application Version", "label.diagnostics.app"), readonly("diagnostics.app", ApplicationContext.VERSION));
        grid.add(UiFactory.label("OS", "label.diagnostics.os"), readonly("diagnostics.os", System.getProperty("os.name") + " " + System.getProperty("os.version")));
        grid.add(UiFactory.label("Current User", "label.diagnostics.user"), readonly("diagnostics.user", context.getCurrentUser()));
        grid.add(UiFactory.label("Current Environment", "label.diagnostics.env"), readonly("diagnostics.env", context.getEnvironment().name()));
        grid.add(UiFactory.label("Loaded Trade Count", "label.diagnostics.trades"),
                readonly("diagnostics.trades", String.valueOf(context.getTradeService().getTrades().size())));
        grid.add(UiFactory.label("UI Thread Status", "label.diagnostics.edt"),
                readonly("diagnostics.edt", SwingUtilities.isEventDispatchThread() ? "EDT active" : "Off-EDT"));

        JButton close = UiFactory.button("Close", "diagnostics.close");
        close.addActionListener(e -> dispose());
        JPanel south = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(close);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(form, BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }

    private static JTextField readonly(String name, String value) {
        JTextField field = UiFactory.text(name, 28);
        field.setText(value);
        field.setEditable(false);
        return field;
    }
}
