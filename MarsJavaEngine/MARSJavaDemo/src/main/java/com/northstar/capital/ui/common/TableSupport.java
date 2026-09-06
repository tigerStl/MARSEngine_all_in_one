package com.northstar.capital.ui.common;

import com.northstar.capital.ui.Theme;

import javax.swing.JTable;
import javax.swing.ListSelectionModel;
import javax.swing.table.DefaultTableCellRenderer;
import javax.swing.table.JTableHeader;
import javax.swing.table.TableModel;
import javax.swing.table.TableRowSorter;
import java.awt.Color;
import java.awt.Component;

public final class TableSupport {
    private TableSupport() {
    }

    public static JTable denseTable(TableModel model, String name) {
        JTable table = new JTable(model);
        table.setName(name);
        table.setAutoCreateRowSorter(true);
        table.setRowHeight(18);
        table.setFont(Theme.UI_FONT);
        table.setSelectionMode(ListSelectionModel.SINGLE_SELECTION);
        table.setFillsViewportHeight(true);
        table.setShowGrid(true);
        table.setGridColor(new Color(0xD0D5DD));
        table.setAutoResizeMode(JTable.AUTO_RESIZE_OFF);
        JTableHeader header = table.getTableHeader();
        header.setFont(Theme.UI_FONT_BOLD);
        header.setBackground(Theme.TABLE_HEADER);
        header.setForeground(Color.WHITE);
        header.setReorderingAllowed(true);
        table.setDefaultRenderer(Object.class, new StripeRenderer());
        table.setRowSorter(new TableRowSorter<>(model));
        return table;
    }

    public static void setColumnWidths(JTable table, int... widths) {
        for (int i = 0; i < widths.length && i < table.getColumnCount(); i++) {
            table.getColumnModel().getColumn(i).setPreferredWidth(widths[i]);
        }
    }

    private static final class StripeRenderer extends DefaultTableCellRenderer {
        @Override
        public Component getTableCellRendererComponent(JTable table, Object value, boolean isSelected,
                                                       boolean hasFocus, int row, int column) {
            Component component = super.getTableCellRendererComponent(table, value, isSelected, hasFocus, row, column);
            if (!isSelected) {
                component.setBackground(row % 2 == 0 ? Color.WHITE : Theme.TABLE_ALT);
            }
            return component;
        }
    }
}
