package com.northstar.capital.ui.dialog;

import com.northstar.capital.model.Counterparty;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.common.FormGrid;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JComboBox;
import javax.swing.JDialog;
import javax.swing.JFrame;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextField;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.util.List;

public class CounterpartySearchDialog extends JDialog {
    private final ApplicationContext context;
    private final DefaultTableModel model;
    private final JTable table;
    private final JTextField nameField = UiFactory.text("counterpartySearch.name", 16);
    private final JComboBox<String> countryCombo = UiFactory.combo("counterpartySearch.country",
            List.of("ALL", "USA", "GBR", "DEU", "CHE"));
    private final JComboBox<String> typeCombo = UiFactory.combo("counterpartySearch.type",
            List.of("ALL", "Bank", "Broker"));
    private final JComboBox<String> statusCombo = UiFactory.combo("counterpartySearch.status",
            List.of("Active", "Inactive", "ALL"));
    private Counterparty selected;

    public CounterpartySearchDialog(Frame owner, ApplicationContext context) {
        super(owner, "Counterparty Search", true);
        this.context = context;
        setName("dialog.counterpartySearch");
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(720, 420);
        setLocationRelativeTo(owner);

        countryCombo.setSelectedItem("USA");
        typeCombo.setSelectedItem("Bank");
        statusCombo.setSelectedItem("Active");

        JPanel filters = UiFactory.titled("Search Criteria", "counterpartySearch.filters");
        FormGrid grid = new FormGrid(filters);
        grid.addPair(UiFactory.label("Name", "label.counterpartySearch.name"), nameField,
                UiFactory.label("Country", "label.counterpartySearch.country"), countryCombo);
        grid.addPair(UiFactory.label("Type", "label.counterpartySearch.type"), typeCombo,
                UiFactory.label("Status", "label.counterpartySearch.status"), statusCombo);

        JButton search = UiFactory.button("Search", "counterpartySearch.search");
        JButton clear = UiFactory.button("Clear", "counterpartySearch.clear");
        JButton cancel = UiFactory.button("Cancel", "counterpartySearch.cancel");
        JButton use = UiFactory.button("Use Selected", "counterpartySearch.useSelected");
        JPanel buttons = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 2));
        buttons.setOpaque(false);
        buttons.add(search);
        buttons.add(clear);
        buttons.add(cancel);
        buttons.add(use);

        model = new DefaultTableModel(new Object[]{"Code", "Name", "Country", "Type", "Rating", "Status"}, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        table = TableSupport.denseTable(model, "counterpartySearch.results");
        TableSupport.setColumnWidths(table, 70, 220, 80, 80, 70, 80);
        table.addMouseListener(new java.awt.event.MouseAdapter() {
            @Override
            public void mouseClicked(java.awt.event.MouseEvent e) {
                if (e.getClickCount() == 2) {
                    chooseSelected();
                }
            }
        });

        search.addActionListener(e -> runSearch());
        clear.addActionListener(e -> {
            nameField.setText("");
            countryCombo.setSelectedItem("USA");
            typeCombo.setSelectedItem("Bank");
            statusCombo.setSelectedItem("Active");
            runSearch();
        });
        cancel.addActionListener(e -> dispose());
        use.addActionListener(e -> chooseSelected());

        JPanel north = new JPanel(new BorderLayout());
        north.add(filters, BorderLayout.CENTER);
        north.add(buttons, BorderLayout.SOUTH);
        getContentPane().setLayout(new BorderLayout(4, 4));
        getContentPane().add(north, BorderLayout.NORTH);
        getContentPane().add(new JScrollPane(table), BorderLayout.CENTER);
        runSearch();
    }

    public static Counterparty open(JFrame owner, ApplicationContext context) {
        CounterpartySearchDialog dialog = new CounterpartySearchDialog(owner, context);
        dialog.setVisible(true);
        return dialog.selected;
    }

    private void runSearch() {
        String country = selectedOrBlank(countryCombo);
        String type = selectedOrBlank(typeCombo);
        String status = selectedOrBlank(statusCombo);
        List<Counterparty> results = context.getReferenceDataService()
                .searchCounterparties(nameField.getText(), country, type, status);
        model.setRowCount(0);
        for (Counterparty item : results) {
            model.addRow(new Object[]{
                    item.getCode(), item.getName(), item.getCountry(), item.getType(), item.getRating(), item.getStatus()
            });
        }
    }

    private void chooseSelected() {
        int viewRow = table.getSelectedRow();
        if (viewRow < 0) {
            return;
        }
        int row = table.convertRowIndexToModel(viewRow);
        String code = String.valueOf(model.getValueAt(row, 0));
        selected = context.getReferenceDataService().findCounterparty(code).orElse(null);
        dispose();
    }

    private static String selectedOrBlank(JComboBox<String> combo) {
        Object value = combo.getSelectedItem();
        if (value == null || "ALL".equals(value)) {
            return "";
        }
        return String.valueOf(value);
    }
}
