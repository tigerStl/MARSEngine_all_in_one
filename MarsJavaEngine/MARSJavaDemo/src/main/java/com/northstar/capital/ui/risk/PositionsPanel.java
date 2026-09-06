package com.northstar.capital.ui.risk;

import com.northstar.capital.model.Position;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JComboBox;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.RowFilter;
import javax.swing.table.DefaultTableModel;
import javax.swing.table.TableRowSorter;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

public class PositionsPanel extends JPanel {
    private final DefaultTableModel model;
    private final TableRowSorter<DefaultTableModel> sorter;
    private final JComboBox<String> bookFilter;
    private final JComboBox<String> currencyFilter;
    private final JComboBox<String> productFilter;

    public PositionsPanel(ApplicationContext context) {
        setName("positionsPanel");
        setLayout(new BorderLayout(4, 4));
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        Set<String> books = new LinkedHashSet<>(List.of("ALL"));
        Set<String> currencies = new LinkedHashSet<>(List.of("ALL"));
        Set<String> products = new LinkedHashSet<>(List.of("ALL"));
        List<Position> positions = context.getReferenceDataService().getPositions();
        for (Position position : positions) {
            books.add(position.getBook());
            currencies.add(position.getCurrency());
            products.add(position.getProduct());
        }

        bookFilter = UiFactory.combo("positions.filter.book", books);
        currencyFilter = UiFactory.combo("positions.filter.currency", currencies);
        productFilter = UiFactory.combo("positions.filter.product", products);

        JPanel filters = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 2));
        filters.setOpaque(false);
        filters.add(UiFactory.label("Book", "label.positions.book"));
        filters.add(bookFilter);
        filters.add(UiFactory.label("Currency", "label.positions.currency"));
        filters.add(currencyFilter);
        filters.add(UiFactory.label("Product", "label.positions.product"));
        filters.add(productFilter);

        model = new DefaultTableModel(new Object[]{
                "Book", "Security", "Position", "Average Price", "Market Price",
                "Market Value", "Daily P&L", "Currency", "DV01", "Product"
        }, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        for (Position position : positions) {
            model.addRow(new Object[]{
                    position.getBook(),
                    position.getSecurity(),
                    MoneyFormats.formatAmount(position.getPositionQty()),
                    MoneyFormats.formatPrice(position.getAveragePrice()),
                    MoneyFormats.formatPrice(position.getMarketPrice()),
                    MoneyFormats.formatAmount(position.getMarketValue()),
                    MoneyFormats.formatAmount(position.getDailyPnl()),
                    position.getCurrency(),
                    MoneyFormats.formatAmount(position.getDv01()),
                    position.getProduct()
            });
        }
        JTable table = TableSupport.denseTable(model, "positions.table");
        sorter = new TableRowSorter<>(model);
        table.setRowSorter(sorter);
        TableSupport.setColumnWidths(table, 120, 170, 110, 100, 100, 120, 100, 80, 90, 80);

        bookFilter.addActionListener(e -> applyFilters());
        currencyFilter.addActionListener(e -> applyFilters());
        productFilter.addActionListener(e -> applyFilters());

        add(filters, BorderLayout.NORTH);
        add(new JScrollPane(table), BorderLayout.CENTER);
    }

    private void applyFilters() {
        sorter.setRowFilter(new RowFilter<DefaultTableModel, Integer>() {
            @Override
            public boolean include(Entry<? extends DefaultTableModel, ? extends Integer> entry) {
                return matches(bookFilter, entry.getStringValue(0))
                        && matches(currencyFilter, entry.getStringValue(7))
                        && matches(productFilter, entry.getStringValue(9));
            }
        });
    }

    private static boolean matches(JComboBox<String> combo, String value) {
        String selected = String.valueOf(combo.getSelectedItem());
        return "ALL".equals(selected) || selected.equals(value);
    }
}
