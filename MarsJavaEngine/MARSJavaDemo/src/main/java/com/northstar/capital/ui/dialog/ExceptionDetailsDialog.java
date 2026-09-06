package com.northstar.capital.ui.dialog;

import com.northstar.capital.model.SettlementException;
import com.northstar.capital.ui.common.FormGrid;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTextArea;
import javax.swing.JTextField;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;

public class ExceptionDetailsDialog extends JDialog {
    public ExceptionDetailsDialog(Frame owner, SettlementException exception) {
        super(owner, "Exception Details", true);
        setName("dialog.exceptionDetails");
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(520, 340);
        setLocationRelativeTo(owner);

        JPanel form = UiFactory.titled("Settlement Exception", "dialog.exceptionDetails.form");
        FormGrid grid = new FormGrid(form);
        grid.add(UiFactory.label("Exception ID", "label.exception.exceptionId"), readonly("exception.exceptionId", exception.getExceptionId()));
        grid.add(UiFactory.label("Trade ID", "label.exception.tradeId"), readonly("exception.tradeId", exception.getTradeId()));
        grid.add(UiFactory.label("Severity", "label.exception.severity"), readonly("exception.severity", exception.getSeverity()));
        grid.add(UiFactory.label("Type", "label.exception.type"), readonly("exception.type", exception.getType()));
        grid.add(UiFactory.label("Owner", "label.exception.owner"), readonly("exception.owner", exception.getOwner()));
        grid.add(UiFactory.label("Status", "label.exception.status"), readonly("exception.status", exception.getStatus()));

        JTextArea details = new JTextArea(exception.getDetails(), 5, 40);
        details.setName("exception.details");
        details.setLineWrap(true);
        details.setWrapStyleWord(true);
        details.setEditable(false);

        JButton close = UiFactory.button("Close", "dialog.exceptionDetails.close");
        close.addActionListener(e -> dispose());
        JPanel south = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(close);

        getContentPane().setLayout(new BorderLayout(4, 4));
        getContentPane().add(form, BorderLayout.NORTH);
        getContentPane().add(new JScrollPane(details), BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }

    private static JTextField readonly(String name, String value) {
        JTextField field = UiFactory.text(name, 22);
        field.setText(value);
        field.setEditable(false);
        return field;
    }
}
