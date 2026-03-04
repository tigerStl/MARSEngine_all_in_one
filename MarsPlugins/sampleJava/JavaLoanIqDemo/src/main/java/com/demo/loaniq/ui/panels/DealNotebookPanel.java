/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui.panels;

import com.demo.loaniq.data.ReferenceData;
import com.demo.loaniq.model.Deal;
import com.demo.loaniq.model.DemoState;
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

public class DealNotebookPanel
extends JPanel {
    private final DemoState state;
    private final Runnable onRefresh;
    private JTextField dealNameF;
    private JTextField agreementDateF;
    private JTextField effectiveDateF;
    private JTextField maturityDateF;
    private JTextField amountF;
    private JComboBox<String> dealTypeCb;
    private JComboBox<String> borrowerCb;
    private JComboBox<String> currencyCb;
    private JComboBox<String> countryCb;
    private JComboBox<String> statusCb;
    private JTable grid;

    public DealNotebookPanel(DemoState state, Runnable onRefresh) {
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
        p.setBorder(BorderFactory.createTitledBorder(null, "Deal", 4, 2));
        GridBagConstraints c = new GridBagConstraints();
        c.insets = new Insets(2, 4, 2, 4);
        c.fill = 2;
        int row = 0;
        int n = row++;
        this.dealNameF = new JTextField(20);
        this.addFormRow(p, c, n, "Deal Name", this.dealNameF, "LIQ_DEAL_DEALNAME");
        int n2 = row++;
        this.dealTypeCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n2, "Deal Type", this.dealTypeCb, "LIQ_DEAL_DEALTYPE");
        int n3 = row++;
        this.borrowerCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n3, "Borrower", this.borrowerCb, "LIQ_DEAL_BORROWER");
        int n4 = row++;
        this.currencyCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n4, "Currency", this.currencyCb, "LIQ_DEAL_CURRENCY");
        int n5 = row++;
        this.countryCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n5, "Country", this.countryCb, "LIQ_DEAL_COUNTRY");
        int n6 = row++;
        this.statusCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n6, "Status", this.statusCb, "LIQ_DEAL_STATUS");
        int n7 = row++;
        this.agreementDateF = new JTextField(12);
        this.addFormRow(p, c, n7, "Agreement Date", this.agreementDateF, "LIQ_DEAL_AGREEMENT_DATE");
        int n8 = row++;
        this.effectiveDateF = new JTextField(12);
        this.addFormRow(p, c, n8, "Effective Date", this.effectiveDateF, "LIQ_DEAL_EFFECTIVE_DATE");
        int n9 = row++;
        this.maturityDateF = new JTextField(12);
        this.addFormRow(p, c, n9, "Maturity Date", this.maturityDateF, "LIQ_DEAL_MATURITY_DATE");
        int n10 = row++;
        this.amountF = new JTextField(15);
        this.addFormRow(p, c, n10, "Amount", this.amountF, "LIQ_DEAL_AMOUNT");
        ReferenceData.DEAL_TYPE.forEach(this.dealTypeCb::addItem);
        ReferenceData.BORROWERS.forEach(this.borrowerCb::addItem);
        ReferenceData.CURRENCIES.forEach(this.currencyCb::addItem);
        ReferenceData.COUNTRY.forEach(this.countryCb::addItem);
        ReferenceData.STATUS.forEach(this.statusCb::addItem);
        return p;
    }

    private void addFormRow(JPanel p, GridBagConstraints c, int row, String labelText, JComponent field, String fieldName) {
        c.gridy = row;
        c.gridx = 0;
        c.weightx = 0.0;
        JLabel lbl = new JLabel(labelText);
        lbl.setName(labelText.replace(" ", "_") + "_LABEL");
        p.add((Component)lbl, c);
        c.gridx = 1;
        c.weightx = 1.0;
        field.setName(fieldName);
        p.add((Component)field, c);
    }

    private void addFormRowCb(JPanel p, GridBagConstraints c, int row, String labelText, JComboBox<String> cb, String fieldName) {
        this.addFormRow(p, c, row, labelText, cb, fieldName);
    }

    private JPanel buildGridPanel() {
        this.grid = new JTable(new Object[][]{{"Event", "2013/08/12", "Deal Created"}}, new String[]{"Type", "Date", "Description"});
        this.grid.setName("LIQ_DEAL_GRID");
        JScrollPane sp = new JScrollPane(this.grid);
        sp.setBorder(BorderFactory.createTitledBorder(null, "Events", 4, 2));
        JPanel p = new JPanel(new BorderLayout());
        p.add((Component)sp, "Center");
        return p;
    }

    private JPanel buildButtons() {
        JPanel p = new JPanel(new FlowLayout(0));
        JButton save = new JButton("Save");
        save.setName("LIQ_DEAL_BTN_SAVE");
        save.addActionListener(e -> this.doSave());
        JButton validate = new JButton("Validate");
        validate.setName("LIQ_DEAL_BTN_VALIDATE");
        validate.addActionListener(e -> this.doValidate());
        JButton approve = new JButton("Approve");
        approve.setName("LIQ_DEAL_BTN_APPROVE");
        approve.addActionListener(e -> this.doApprove());
        JButton close = new JButton("Close");
        close.setName("LIQ_DEAL_BTN_CLOSE");
        close.addActionListener(e -> this.doClose());
        p.add(save);
        p.add(validate);
        p.add(approve);
        p.add(close);
        return p;
    }

    public void loadFrom(Deal d) {
        if (d == null) {
            return;
        }
        this.dealNameF.setText(d.getDealName());
        this.dealTypeCb.setSelectedItem(d.getDealType());
        this.borrowerCb.setSelectedItem(d.getBorrower());
        this.currencyCb.setSelectedItem(d.getCurrency());
        this.countryCb.setSelectedItem(d.getCountry());
        this.statusCb.setSelectedItem(d.getStatus());
        this.agreementDateF.setText(d.getAgreementDate());
        this.effectiveDateF.setText(d.getEffectiveDate());
        this.maturityDateF.setText(d.getMaturityDate());
        this.amountF.setText(FormatUtil.formatNumber(d.getAmount()));
    }

    public void saveTo(Deal d) {
        if (d == null) {
            return;
        }
        d.setDealName(this.dealNameF.getText());
        d.setDealType(Objects.toString(this.dealTypeCb.getSelectedItem(), ""));
        d.setBorrower(Objects.toString(this.borrowerCb.getSelectedItem(), ""));
        d.setCurrency(Objects.toString(this.currencyCb.getSelectedItem(), ""));
        d.setCountry(Objects.toString(this.countryCb.getSelectedItem(), ""));
        d.setStatus(Objects.toString(this.statusCb.getSelectedItem(), ""));
        d.setAgreementDate(this.agreementDateF.getText());
        d.setEffectiveDate(this.effectiveDateF.getText());
        d.setMaturityDate(this.maturityDateF.getText());
        d.setAmount(FormatUtil.parseNumber(this.amountF.getText()));
    }

    private void doSave() {
        this.saveTo(this.state.getSelectedDeal());
        JOptionPane.showMessageDialog(this, JsonUtil.toJson(this.state.getSelectedDeal()), "Deal JSON", 1);
        this.state.setLastMessage("Deal saved");
        this.onRefresh.run();
    }

    private void doValidate() {
        double amt;
        StringBuilder err = new StringBuilder();
        if (this.dealNameF.getText().isBlank()) {
            err.append("Deal Name required.\n");
        }
        if (this.effectiveDateF.getText().isBlank()) {
            err.append("Effective Date required.\n");
        }
        if (this.maturityDateF.getText().isBlank()) {
            err.append("Maturity Date required.\n");
        }
        if ((amt = FormatUtil.parseNumber(this.amountF.getText())) <= 0.0) {
            err.append("Amount must be positive.\n");
        }
        if (err.length() > 0) {
            JOptionPane.showMessageDialog(this, err.toString(), "Validation", 0);
            return;
        }
        JOptionPane.showMessageDialog(this, "Validation passed.", "Validate", 1);
        this.state.setLastMessage("Deal validated");
        this.onRefresh.run();
    }

    private void doApprove() {
        Deal d = this.state.getSelectedDeal();
        if (d != null) {
            d.setStatus("Approved");
            this.statusCb.setSelectedItem("Approved");
        }
        this.state.setLastMessage("Deal Approved");
        this.onRefresh.run();
    }

    private void doClose() {
        Deal d = this.state.getSelectedDeal();
        if (d != null) {
            d.setStatus("Closed");
            this.statusCb.setSelectedItem("Closed");
        }
        this.state.setLastMessage("Deal Closed");
        this.onRefresh.run();
    }
}

