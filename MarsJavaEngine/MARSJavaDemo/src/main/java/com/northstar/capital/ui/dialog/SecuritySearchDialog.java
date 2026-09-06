package com.northstar.capital.ui.dialog;

import com.northstar.capital.model.Security;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
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
import javax.swing.ListSelectionModel;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.time.LocalDate;
import java.time.format.DateTimeParseException;
import java.util.List;

public class SecuritySearchDialog extends JDialog {
    private final ApplicationContext context;
    private final DefaultTableModel model;
    private final JTable table;
    private final JTextField securityIdField = UiFactory.text("securitySearch.securityId", 14);
    private final JTextField issuerField = UiFactory.text("securitySearch.issuer", 16);
    private final JComboBox<String> currencyCombo;
    private final JTextField maturityFromField = UiFactory.text("securitySearch.maturityFrom", 10);
    private final JTextField maturityToField = UiFactory.text("securitySearch.maturityTo", 10);
    private Security selected;

    public SecuritySearchDialog(Frame owner, ApplicationContext context) {
        super(owner, "Security Search", true);
        this.context = context;
        setName("dialog.securitySearch");
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(820, 460);
        setLocationRelativeTo(owner);

        currencyCombo = UiFactory.combo("securitySearch.currency", withAll(context.getReferenceDataService().getCurrencies()));
        currencyCombo.setSelectedItem("USD");

        JPanel filters = UiFactory.titled("Search Criteria", "securitySearch.filters");
        FormGrid grid = new FormGrid(filters);
        grid.addPair(UiFactory.label("Security ID", "label.securitySearch.securityId"), securityIdField,
                UiFactory.label("Issuer", "label.securitySearch.issuer"), issuerField);
        grid.addPair(UiFactory.label("Currency", "label.securitySearch.currency"), currencyCombo,
                UiFactory.label("Maturity From", "label.securitySearch.maturityFrom"), maturityFromField);
        grid.add(UiFactory.label("Maturity To", "label.securitySearch.maturityTo"), maturityToField);

        JButton search = UiFactory.button("Search", "securitySearch.search");
        JButton clear = UiFactory.button("Clear", "securitySearch.clear");
        JButton cancel = UiFactory.button("Cancel", "securitySearch.cancel");
        JButton use = UiFactory.button("Use Selected", "securitySearch.useSelected");
        JPanel buttons = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 2));
        buttons.setOpaque(false);
        buttons.add(search);
        buttons.add(clear);
        buttons.add(cancel);
        buttons.add(use);

        model = new DefaultTableModel(new Object[]{
                "Security ID", "Issuer", "Description", "Coupon", "Maturity", "Currency", "Type"}, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        table = TableSupport.denseTable(model, "securitySearch.results");
        table.setSelectionMode(ListSelectionModel.SINGLE_SELECTION);
        TableSupport.setColumnWidths(table, 120, 160, 190, 70, 90, 70, 80);
        table.addMouseListener(new java.awt.event.MouseAdapter() {
            @Override
            public void mouseClicked(java.awt.event.MouseEvent e) {
                if (e.getClickCount() == 2) {
                    chooseSelected();
                }
            }
        });

        search.addActionListener(e -> runSearch());
        clear.addActionListener(e -> clearFilters());
        cancel.addActionListener(e -> dispose());
        use.addActionListener(e -> chooseSelected());

        JPanel north = new JPanel(new BorderLayout());
        north.add(filters, BorderLayout.CENTER);
        north.add(buttons, BorderLayout.SOUTH);

        getContentPane().setLayout(new BorderLayout(4, 4));
        getContentPane().add(north, BorderLayout.NORTH);
        JScrollPane scroll = new JScrollPane(table);
        scroll.setPreferredSize(new Dimension(780, 260));
        getContentPane().add(scroll, BorderLayout.CENTER);
        runSearch();
    }

    public static Security open(JFrame owner, ApplicationContext context) {
        SecuritySearchDialog dialog = new SecuritySearchDialog(owner, context);
        dialog.setVisible(true);
        return dialog.selected;
    }

    private void runSearch() {
        LocalDate from = parseDate(maturityFromField.getText());
        LocalDate to = parseDate(maturityToField.getText());
        String currency = String.valueOf(currencyCombo.getSelectedItem());
        if ("ALL".equals(currency)) {
            currency = "";
        }
        List<Security> results = context.getReferenceDataService()
                .searchSecurities(securityIdField.getText(), issuerField.getText(), currency, from, to);
        model.setRowCount(0);
        for (Security security : results) {
            model.addRow(new Object[]{
                    security.getSecurityId(),
                    security.getIssuer(),
                    security.getDescription(),
                    security.getCoupon(),
                    MoneyFormats.formatDate(security.getMaturity()),
                    security.getCurrency(),
                    security.getType()
            });
        }
    }

    private void clearFilters() {
        securityIdField.setText("");
        issuerField.setText("");
        currencyCombo.setSelectedItem("USD");
        maturityFromField.setText("");
        maturityToField.setText("");
        runSearch();
    }

    private void chooseSelected() {
        int viewRow = table.getSelectedRow();
        if (viewRow < 0) {
            return;
        }
        int row = table.convertRowIndexToModel(viewRow);
        String id = String.valueOf(model.getValueAt(row, 0));
        selected = context.getReferenceDataService().findSecurity(id).orElse(null);
        dispose();
    }

    private static LocalDate parseDate(String text) {
        try {
            return MoneyFormats.parseDate(text);
        } catch (DateTimeParseException ex) {
            return null;
        }
    }

    private static List<String> withAll(List<String> values) {
        java.util.ArrayList<String> list = new java.util.ArrayList<>();
        list.add("ALL");
        list.addAll(values);
        return list;
    }
}
