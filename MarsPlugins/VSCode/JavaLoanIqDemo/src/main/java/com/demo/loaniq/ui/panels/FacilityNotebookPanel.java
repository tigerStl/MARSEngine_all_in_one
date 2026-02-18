/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui.panels;

import com.demo.loaniq.data.ReferenceData;
import com.demo.loaniq.model.DemoState;
import com.demo.loaniq.model.Facility;
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

public class FacilityNotebookPanel
extends JPanel {
    private final DemoState state;
    private final Runnable onRefresh;
    private JTextField facilityNameF;
    private JTextField commitmentF;
    private JComboBox<String> facilityTypeCb;
    private JComboBox<String> currencyCb;
    private JComboBox<String> dayCountCb;
    private JComboBox<String> statusCb;
    private JTextField pricingOptionF;
    private JTable grid;

    public FacilityNotebookPanel(DemoState state, Runnable onRefresh) {
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
        p.setBorder(BorderFactory.createTitledBorder(null, "Facility", 4, 2));
        GridBagConstraints c = new GridBagConstraints();
        c.insets = new Insets(2, 4, 2, 4);
        c.fill = 2;
        int row = 0;
        int n = row++;
        this.facilityNameF = new JTextField(20);
        this.addFormRow(p, c, n, "Facility Name", this.facilityNameF, "LIQ_FACILITY_NAME");
        int n2 = row++;
        this.facilityTypeCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n2, "Facility Type", this.facilityTypeCb, "LIQ_FACILITY_TYPE");
        int n3 = row++;
        this.currencyCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n3, "Currency", this.currencyCb, "LIQ_FACILITY_CURRENCY");
        int n4 = row++;
        this.commitmentF = new JTextField(15);
        this.addFormRow(p, c, n4, "Commitment", this.commitmentF, "LIQ_FACILITY_COMMITMENT");
        int n5 = row++;
        this.pricingOptionF = new JTextField(20);
        this.addFormRow(p, c, n5, "Pricing Option", this.pricingOptionF, "LIQ_FACILITY_PRICING");
        int n6 = row++;
        this.dayCountCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n6, "Day Count", this.dayCountCb, "LIQ_FACILITY_DAYCOUNT");
        int n7 = row++;
        this.statusCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n7, "Status", this.statusCb, "LIQ_FACILITY_STATUS");
        ReferenceData.FACILITY_TYPE.forEach(this.facilityTypeCb::addItem);
        ReferenceData.CURRENCIES.forEach(this.currencyCb::addItem);
        ReferenceData.DAY_COUNT.forEach(this.dayCountCb::addItem);
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
        this.grid = new JTable(new Object[][]{{"Fee", "Upfront", "1000"}}, new String[]{"Type", "Name", "Amount"});
        this.grid.setName("LIQ_FACILITY_GRID");
        JScrollPane sp = new JScrollPane(this.grid);
        sp.setBorder(BorderFactory.createTitledBorder(null, "Fees", 4, 2));
        JPanel p = new JPanel(new BorderLayout());
        p.add((Component)sp, "Center");
        return p;
    }

    private JPanel buildButtons() {
        JPanel p = new JPanel(new FlowLayout(0));
        JButton save = new JButton("Save");
        save.setName("LIQ_FACILITY_BTN_SAVE");
        save.addActionListener(e -> this.doSave());
        JButton validate = new JButton("Validate");
        validate.setName("LIQ_FACILITY_BTN_VALIDATE");
        validate.addActionListener(e -> this.doValidate());
        JButton approve = new JButton("Approve");
        approve.setName("LIQ_FACILITY_BTN_APPROVE");
        approve.addActionListener(e -> this.doApprove());
        JButton close = new JButton("Close");
        close.setName("LIQ_FACILITY_BTN_CLOSE");
        close.addActionListener(e -> this.doClose());
        p.add(save);
        p.add(validate);
        p.add(approve);
        p.add(close);
        return p;
    }

    public void loadFrom(Facility f) {
        if (f == null) {
            return;
        }
        this.facilityNameF.setText(f.getFacilityName());
        this.facilityTypeCb.setSelectedItem(f.getFacilityType());
        this.currencyCb.setSelectedItem(f.getCurrency());
        this.commitmentF.setText(FormatUtil.formatNumber(f.getCommitment()));
        this.pricingOptionF.setText(f.getPricingOption());
        this.dayCountCb.setSelectedItem(f.getDayCount());
        this.statusCb.setSelectedItem(f.getStatus());
    }

    public void saveTo(Facility f) {
        if (f == null) {
            return;
        }
        f.setFacilityName(this.facilityNameF.getText());
        f.setFacilityType(Objects.toString(this.facilityTypeCb.getSelectedItem(), ""));
        f.setCurrency(Objects.toString(this.currencyCb.getSelectedItem(), ""));
        f.setCommitment(FormatUtil.parseNumber(this.commitmentF.getText()));
        f.setPricingOption(this.pricingOptionF.getText());
        f.setDayCount(Objects.toString(this.dayCountCb.getSelectedItem(), ""));
        f.setStatus(Objects.toString(this.statusCb.getSelectedItem(), ""));
    }

    private void doSave() {
        this.saveTo(this.state.getSelectedFacility());
        JOptionPane.showMessageDialog(this, JsonUtil.toJson(this.state.getSelectedFacility()), "Facility JSON", 1);
        this.state.setLastMessage("Facility saved");
        this.onRefresh.run();
    }

    private void doValidate() {
        if (this.facilityNameF.getText().isBlank()) {
            JOptionPane.showMessageDialog(this, "Facility Name required.", "Validation", 0);
            return;
        }
        if (FormatUtil.parseNumber(this.commitmentF.getText()) <= 0.0) {
            JOptionPane.showMessageDialog(this, "Commitment must be positive.", "Validation", 0);
            return;
        }
        JOptionPane.showMessageDialog(this, "Validation passed.", "Validate", 1);
        this.state.setLastMessage("Facility validated");
        this.onRefresh.run();
    }

    private void doApprove() {
        Facility f = this.state.getSelectedFacility();
        if (f != null) {
            f.setStatus("Approved");
            this.statusCb.setSelectedItem("Approved");
        }
        this.state.setLastMessage("Facility Approved");
        this.onRefresh.run();
    }

    private void doClose() {
        Facility f = this.state.getSelectedFacility();
        if (f != null) {
            f.setStatus("Closed");
            this.statusCb.setSelectedItem("Closed");
        }
        this.state.setLastMessage("Facility Closed");
        this.onRefresh.run();
    }
}

