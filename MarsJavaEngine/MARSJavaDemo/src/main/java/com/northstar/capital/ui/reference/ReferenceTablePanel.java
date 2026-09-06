package com.northstar.capital.ui.reference;

import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;

import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;

public class ReferenceTablePanel extends JPanel {
    public ReferenceTablePanel(String name, String[] columns, Object[][] rows, int[] widths) {
        setName(name);
        setLayout(new BorderLayout());
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));
        DefaultTableModel model = new DefaultTableModel(columns, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        for (Object[] row : rows) {
            model.addRow(row);
        }
        JTable table = TableSupport.denseTable(model, name + ".table");
        TableSupport.setColumnWidths(table, widths);
        add(new JScrollPane(table), BorderLayout.CENTER);
    }
}
