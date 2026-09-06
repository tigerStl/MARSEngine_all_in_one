package com.northstar.capital.ui.operations;

import com.northstar.capital.model.SettlementException;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.dialog.ExceptionDetailsDialog;

import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.SwingUtilities;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.Frame;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.util.List;

public class SettlementExceptionsPanel extends JPanel {
    private final List<SettlementException> exceptions;
    private final DefaultTableModel model;
    private final JTable table;

    public SettlementExceptionsPanel(ApplicationContext context) {
        this.exceptions = context.getReferenceDataService().getExceptions();
        setName("settlementExceptionsPanel");
        setLayout(new BorderLayout());
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        model = new DefaultTableModel(new Object[]{
                "Exception ID", "Trade ID", "Severity", "Type", "Description", "Owner", "Status", "Age"
        }, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        for (SettlementException exception : exceptions) {
            model.addRow(new Object[]{
                    exception.getExceptionId(),
                    exception.getTradeId(),
                    exception.getSeverity(),
                    exception.getType(),
                    exception.getDescription(),
                    exception.getOwner(),
                    exception.getStatus(),
                    exception.getAge()
            });
        }
        table = TableSupport.denseTable(model, "exceptions.table");
        TableSupport.setColumnWidths(table, 100, 160, 80, 170, 280, 70, 90, 50);
        table.addMouseListener(new MouseAdapter() {
            @Override
            public void mouseClicked(MouseEvent e) {
                if (e.getClickCount() == 2) {
                    openSelected();
                }
            }
        });
        add(new JScrollPane(table), BorderLayout.CENTER);
    }

    private void openSelected() {
        int viewRow = table.getSelectedRow();
        if (viewRow < 0) {
            return;
        }
        int row = table.convertRowIndexToModel(viewRow);
        String id = String.valueOf(model.getValueAt(row, 0));
        exceptions.stream()
                .filter(item -> item.getExceptionId().equals(id))
                .findFirst()
                .ifPresent(item -> new ExceptionDetailsDialog((Frame) SwingUtilities.getWindowAncestor(this), item)
                        .setVisible(true));
    }
}
