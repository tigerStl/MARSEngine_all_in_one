/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui.panels;

import com.demo.loaniq.data.ReferenceData;
import com.demo.loaniq.model.DemoState;
import com.demo.loaniq.model.Loan;
import com.demo.loaniq.util.FormatUtil;
import com.demo.loaniq.util.JsonUtil;
import java.awt.BorderLayout;
import java.awt.Component;
import java.awt.FlowLayout;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;
import java.util.Objects;
import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextField;

public class LoanNotebookPanel
extends JPanel {
    private final DemoState state;
    private final Runnable onRefresh;
    private JTextField aliasF;
    private JTextField principalF;
    private JTextField spreadF;
    private JTextField allInRateF;
    private JTextField startDateF;
    private JTextField endDateF;
    private JComboBox<String> currencyCb;
    private JComboBox<String> rateTypeCb;
    private JComboBox<String> indexCb;
    private JComboBox<String> statusCb;
    private JTable grid;

    public LoanNotebookPanel(DemoState state, Runnable onRefresh) {
        this.state = state;
        this.onRefresh = onRefresh != null ? onRefresh : () -> {};
        this.setLayout(new BorderLayout(5, 5));
        this.setBorder(BorderFactory.createEmptyBorder(8, 8, 8, 8));
        this.add((Component)this.buildFormPanel(), "North");
        this.add((Component)this.buildGridPanel(), "Center");
        this.add((Component)this.buildButtons(), "South");
    }

    private JPanel buildFormPanel() {
        JPanel p = new JPanel(new GridBagLayout());
        p.setBorder(BorderFactory.createTitledBorder(null, "Loan", 4, 2));
        GridBagConstraints c = new GridBagConstraints();
        c.insets = new Insets(2, 4, 2, 4);
        c.fill = 2;
        int row = 0;
        int n = row++;
        this.aliasF = new JTextField(15);
        this.addFormRow(p, c, n, "Alias", this.aliasF, "LIQ_LOAN_ALIAS");
        int n2 = row++;
        this.currencyCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n2, "Currency", this.currencyCb, "LIQ_LOAN_CURRENCY");
        int n3 = row++;
        this.principalF = new JTextField(15);
        this.addFormRow(p, c, n3, "Principal", this.principalF, "LIQ_LOAN_PRINCIPAL");
        int n4 = row++;
        this.rateTypeCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n4, "Rate Type", this.rateTypeCb, "LIQ_LOAN_RATETYPE");
        int n5 = row++;
        this.indexCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n5, "Index", this.indexCb, "LIQ_LOAN_INDEX");
        int n6 = row++;
        this.spreadF = new JTextField(10);
        this.addFormRow(p, c, n6, "Spread", this.spreadF, "LIQ_LOAN_SPREAD");
        int n7 = row++;
        this.allInRateF = new JTextField(10);
        this.addFormRow(p, c, n7, "All-in Rate", this.allInRateF, "LIQ_LOAN_ALLINRATE");
        int n8 = row++;
        this.startDateF = new JTextField(12);
        this.addFormRow(p, c, n8, "Start Date", this.startDateF, "LIQ_LOAN_START_DATE");
        int n9 = row++;
        this.endDateF = new JTextField(12);
        this.addFormRow(p, c, n9, "End Date", this.endDateF, "LIQ_LOAN_END_DATE");
        int n10 = row++;
        this.statusCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n10, "Status", this.statusCb, "LIQ_LOAN_STATUS");
        ReferenceData.CURRENCIES.forEach(this.currencyCb::addItem);
        this.rateTypeCb.addItem("Floating");
        this.rateTypeCb.addItem("Fixed");
        ReferenceData.RATE_INDEX.forEach(this.indexCb::addItem);
        ReferenceData.STATUS.forEach(this.statusCb::addItem);
        return p;
    }

    private void addFormRow(JPanel p, GridBagConstraints c, int row, String labelText, JComponent field, String fieldName) {
        c.gridy = row;
        c.gridx = 0;
        c.weightx = 0.0;
        p.add((Component)new JLabel(labelText), c);
        c.gridx = 1;
        c.weightx = 1.0;
        field.setName(fieldName);
        p.add((Component)field, c);
    }

    private void addFormRowCb(JPanel p, GridBagConstraints c, int row, String labelText, JComboBox<String> cb, String fieldName) {
        this.addFormRow(p, c, row, labelText, cb, fieldName);
    }

    private JPanel buildGridPanel() {
        this.grid = new JTable(new Object[][]{{"Drawdown", "2012/05/16", "50000000"}}, new String[]{"Type", "Date", "Amount"});
        this.grid.setName("LIQ_LOAN_GRID");
        JScrollPane sp = new JScrollPane(this.grid);
        sp.setBorder(BorderFactory.createTitledBorder(null, "Drawdowns", 4, 2));
        JPanel p = new JPanel(new BorderLayout());
        p.add((Component)sp, "Center");
        return p;
    }

    private JPanel buildButtons() {
        JPanel p = new JPanel(new FlowLayout(0));
        JButton save = new JButton("Save");
        save.setName("LIQ_LOAN_BTN_SAVE");
        save.addActionListener(e -> this.doSave());
        JButton validate = new JButton("Validate");
        validate.setName("LIQ_LOAN_BTN_VALIDATE");
        validate.addActionListener(e -> this.doValidate());
        JButton approve = new JButton("Release");
        approve.setName("LIQ_LOAN_BTN_RELEASE");
        approve.addActionListener(e -> this.doRelease());
        JButton close = new JButton("Close");
        close.setName("LIQ_LOAN_BTN_CLOSE");
        close.addActionListener(e -> this.doClose());
        p.add(save);
        p.add(validate);
        p.add(approve);
        p.add(close);
        return p;
    }

    public void loadFrom(Loan l) {
        if (l == null) {
            return;
        }
        this.aliasF.setText(l.getAlias());
        this.currencyCb.setSelectedItem(l.getCurrency());
        this.principalF.setText(FormatUtil.formatNumber(l.getPrincipal()));
        this.rateTypeCb.setSelectedItem(l.getRateType());
        this.indexCb.setSelectedItem(l.getIndex());
        this.spreadF.setText(FormatUtil.formatNumber(l.getSpread()));
        this.allInRateF.setText(FormatUtil.formatNumber(l.getAllInRate()));
        this.startDateF.setText(l.getStartDate());
        this.endDateF.setText(l.getEndDate());
        this.statusCb.setSelectedItem(l.getStatus());
    }

    public void saveTo(Loan l) {
        if (l == null) {
            return;
        }
        l.setAlias(this.aliasF.getText());
        l.setCurrency(Objects.toString(this.currencyCb.getSelectedItem(), ""));
        l.setPrincipal(FormatUtil.parseNumber(this.principalF.getText()));
        l.setRateType(Objects.toString(this.rateTypeCb.getSelectedItem(), ""));
        l.setIndex(Objects.toString(this.indexCb.getSelectedItem(), ""));
        l.setSpread(FormatUtil.parseNumber(this.spreadF.getText()));
        l.setAllInRate(FormatUtil.parseNumber(this.allInRateF.getText()));
        l.setStartDate(this.startDateF.getText());
        l.setEndDate(this.endDateF.getText());
        l.setStatus(Objects.toString(this.statusCb.getSelectedItem(), ""));
    }

    private void doSave() {
        this.saveTo(this.state.getSelectedLoan());
        JOptionPane.showMessageDialog(this, JsonUtil.toJson(this.state.getSelectedLoan()), "Loan JSON", 1);
        this.state.setLastMessage("Loan saved");
        this.onRefresh.run();
    }

    private void doValidate() {
        StringBuilder err = new StringBuilder();
        if (this.aliasF.getText().isBlank()) {
            err.append("Alias required.\n");
        }
        if (FormatUtil.parseNumber(this.principalF.getText()) <= 0.0) {
            err.append("Principal must be positive.\n");
        }
        if (this.startDateF.getText().isBlank()) {
            err.append("Start Date required.\n");
        }
        if (this.endDateF.getText().isBlank()) {
            err.append("End Date required.\n");
        }
        if (err.length() > 0) {
            JOptionPane.showMessageDialog(this, err.toString(), "Validation", 0);
            return;
        }
        JOptionPane.showMessageDialog(this, "Validation passed.", "Validate", 1);
        this.state.setLastMessage("Loan validated");
        this.onRefresh.run();
    }

    private void doRelease() {
        Loan l = this.state.getSelectedLoan();
        if (l != null) {
            l.setStatus("Released");
            this.statusCb.setSelectedItem("Released");
        }
        this.state.setLastMessage("Loan Released");
        this.onRefresh.run();
    }

    private void doClose() {
        Loan l = this.state.getSelectedLoan();
        if (l != null) {
            l.setStatus("Closed");
            this.statusCb.setSelectedItem("Closed");
        }
        this.state.setLastMessage("Loan Closed");
        this.onRefresh.run();
    }
}

