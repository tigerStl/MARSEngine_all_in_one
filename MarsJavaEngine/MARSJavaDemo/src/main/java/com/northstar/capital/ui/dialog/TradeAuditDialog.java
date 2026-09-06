package com.northstar.capital.ui.dialog;

import com.northstar.capital.model.AuditEvent;
import com.northstar.capital.model.Trade;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;

public class TradeAuditDialog extends JDialog {
    public TradeAuditDialog(Frame owner, Trade trade) {
        super(owner, "Trade Audit History", true);
        setName("dialog.tradeAudit");
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(720, 320);
        setLocationRelativeTo(owner);

        DefaultTableModel model = new DefaultTableModel(
                new Object[]{"Timestamp", "User", "Action", "Field", "Old Value", "New Value"}, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        if (trade != null) {
            for (AuditEvent event : trade.getAuditTrail()) {
                model.addRow(new Object[]{
                        MoneyFormats.formatTime(event.getTimestamp()),
                        event.getUser(),
                        event.getAction(),
                        event.getField(),
                        event.getOldValue(),
                        event.getNewValue()
                });
            }
        }
        JTable table = TableSupport.denseTable(model, "dialog.tradeAudit.table");
        TableSupport.setColumnWidths(table, 90, 90, 90, 90, 140, 160);

        JButton close = UiFactory.button("Close", "dialog.tradeAudit.close");
        close.addActionListener(e -> dispose());
        JPanel south = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(close);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(new JScrollPane(table), BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }
}
