package com.northstar.capital.ui.lab;

import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.UiFactory;
import com.northstar.capital.ui.dialog.SimpleInfoDialog;

import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JLabel;
import javax.swing.JMenuItem;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JScrollPane;
import javax.swing.JTabbedPane;
import javax.swing.JTable;
import javax.swing.JTextField;
import javax.swing.SwingUtilities;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.awt.GridLayout;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;

public class AutomationLabPanel extends JPanel {
    public AutomationLabPanel() {
        setName("automationLabPanel");
        setLayout(new BorderLayout(6, 6));
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        JPanel outer = UiFactory.titled("Nested Discovery Surface", "lab.outer");
        outer.setLayout(new GridLayout(1, 2, 6, 6));

        JPanel left = UiFactory.titled("Ticket A", "lab.ticketA");
        left.setLayout(new GridLayout(0, 2, 4, 4));
        left.add(UiFactory.label("Currency", "label.lab.currency.a"));
        JTextField currencyA = UiFactory.text("lab.currency.a", 8);
        currencyA.setText("USD");
        left.add(currencyA);
        left.add(UiFactory.label("Notional", "label.lab.notional.a"));
        left.add(UiFactory.text("lab.notional.a", 10));
        left.add(UiFactory.label("Counterparty", "label.lab.counterparty.a"));
        left.add(UiFactory.text("lab.counterparty.a", 10));

        JPanel right = UiFactory.titled("Ticket B", "lab.ticketB");
        right.setLayout(new GridLayout(0, 2, 4, 4));
        right.add(UiFactory.label("Currency", "label.lab.currency.b"));
        JTextField currencyB = UiFactory.text("lab.currency.b", 8);
        currencyB.setText("EUR");
        right.add(currencyB);
        right.add(UiFactory.label("Notional", "label.lab.notional.b"));
        leftStyle(right);
        JTextField notionalB = UiFactory.text("amountField", 10);
        right.add(notionalB);
        right.add(UiFactory.label("Counterparty", "label.lab.counterparty.b"));
        right.add(UiFactory.text("amountField", 10));

        JTextField disabled = UiFactory.text("lab.disabledField", 12);
        disabled.setText("Locked reference");
        disabled.setEnabled(false);
        JComboBox<String> combo = UiFactory.combo("lab.combo", java.util.List.of("NYC", "LON", "TKY"));
        JButton dialogButton = UiFactory.button("Open Lab Dialog", "lab.openDialog");
        dialogButton.addActionListener(e -> new SimpleInfoDialog(
                (Frame) SwingUtilities.getWindowAncestor(this),
                "Automation Lab Dialog",
                "dialog.automationLab",
                "Modal dialog used to stress-test object discovery.").setVisible(true));

        JPanel controls = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 2));
        controls.setOpaque(false);
        controls.add(new JLabel("Disabled"));
        controls.add(disabled);
        controls.add(combo);
        controls.add(dialogButton);

        outer.add(left);
        outer.add(right);

        JTabbedPane tabs = new JTabbedPane();
        tabs.setName("lab.tabs");
        JPanel tabOne = new JPanel(new FlowLayout(FlowLayout.LEFT));
        tabOne.setName("lab.tab.one");
        JCheckBox inside = UiFactory.check("Confirm nested checkbox", "lab.tab.checkbox");
        tabOne.add(inside);
        JPanel tabTwo = new JPanel(new BorderLayout());
        tabTwo.setName("lab.tab.two");
        tabTwo.add(new JLabel("Dynamic tab content loaded."), BorderLayout.NORTH);
        tabs.addTab("Nested Tab", tabOne);
        tabs.addTab("Dynamic Tab", tabTwo);

        DefaultTableModel model = new DefaultTableModel(new Object[]{"Field", "Value"}, 0);
        model.addRow(new Object[]{"Book", "FI_USD_BOOK"});
        model.addRow(new Object[]{"Trader", "TRADER01"});
        JTable table = new JTable(model);
        table.setName("lab.editableTable");
        table.setRowHeight(18);

        JPopupMenu menu = new JPopupMenu();
        menu.setName("lab.popup");
        JMenuItem inspect = new JMenuItem("Inspect Cell");
        inspect.setName("lab.popup.inspect");
        inspect.addActionListener(e -> dialogButton.doClick());
        menu.add(inspect);
        table.setComponentPopupMenu(menu);
        table.addMouseListener(new MouseAdapter() {
            @Override
            public void mousePressed(MouseEvent e) {
                if (e.isPopupTrigger()) {
                    menu.show(table, e.getX(), e.getY());
                }
            }

            @Override
            public void mouseReleased(MouseEvent e) {
                if (e.isPopupTrigger()) {
                    menu.show(table, e.getX(), e.getY());
                }
            }
        });

        JPanel south = new JPanel(new BorderLayout());
        south.add(tabs, BorderLayout.NORTH);
        south.add(new JScrollPane(table), BorderLayout.CENTER);

        add(outer, BorderLayout.NORTH);
        add(controls, BorderLayout.CENTER);
        add(south, BorderLayout.SOUTH);
    }

    private static void leftStyle(JPanel ignored) {
        // grouping helper kept for readability of nested construction
    }
}
