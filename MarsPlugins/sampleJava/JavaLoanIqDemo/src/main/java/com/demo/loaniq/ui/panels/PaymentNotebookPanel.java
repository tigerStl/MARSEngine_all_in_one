/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui.panels;

import com.demo.loaniq.data.ReferenceData;
import com.demo.loaniq.model.DemoState;
import com.demo.loaniq.model.Payment;
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

public class PaymentNotebookPanel
extends JPanel {
    private final DemoState state;
    private final Runnable onRefresh;
    private JTextField paymentTypeF;
    private JTextField amountF;
    private JTextField valueDateF;
    private JComboBox<String> statusCb;
    private JTable grid;

    public PaymentNotebookPanel(DemoState state, Runnable onRefresh) {
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
        p.setBorder(BorderFactory.createTitledBorder(null, "Payment", 4, 2));
        GridBagConstraints c = new GridBagConstraints();
        c.insets = new Insets(2, 4, 2, 4);
        c.fill = 2;
        int row = 0;
        int n = row++;
        this.paymentTypeF = new JTextField(20);
        this.addFormRow(p, c, n, "Payment Type", this.paymentTypeF, "LIQ_PAYMENT_TYPE");
        int n2 = row++;
        this.amountF = new JTextField(15);
        this.addFormRow(p, c, n2, "Amount", this.amountF, "LIQ_PAYMENT_AMOUNT");
        int n3 = row++;
        this.valueDateF = new JTextField(12);
        this.addFormRow(p, c, n3, "Value Date", this.valueDateF, "LIQ_PAYMENT_VALUE_DATE");
        int n4 = row++;
        this.statusCb = new JComboBox<String>();
        this.addFormRowCb(p, c, n4, "Status", this.statusCb, "LIQ_PAYMENT_STATUS");
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
        this.grid = new JTable(new Object[][]{{"Schedule", "2013/09/16", "86250.00"}}, new String[]{"Item", "Date", "Amount"});
        this.grid.setName("LIQ_PAYMENT_GRID");
        JScrollPane sp = new JScrollPane(this.grid);
        sp.setBorder(BorderFactory.createTitledBorder(null, "Schedule", 4, 2));
        JPanel p = new JPanel(new BorderLayout());
        p.add((Component)sp, "Center");
        return p;
    }

    private JPanel buildButtons() {
        JPanel p = new JPanel(new FlowLayout(0));
        JButton save = new JButton("Save");
        save.setName("LIQ_PAYMENT_BTN_SAVE");
        save.addActionListener(e -> this.doSave());
        JButton validate = new JButton("Validate");
        validate.setName("LIQ_PAYMENT_BTN_VALIDATE");
        validate.addActionListener(e -> this.doValidate());
        JButton close = new JButton("Close");
        close.setName("LIQ_PAYMENT_BTN_CLOSE");
        close.addActionListener(e -> this.doClose());
        p.add(save);
        p.add(validate);
        p.add(close);
        return p;
    }

    public void loadFrom(Payment pay) {
        if (pay == null) {
            return;
        }
        this.paymentTypeF.setText(pay.getPaymentType());
        this.amountF.setText(FormatUtil.formatNumber(pay.getAmount()));
        this.valueDateF.setText(pay.getValueDate());
        this.statusCb.setSelectedItem(pay.getStatus());
    }

    public void saveTo(Payment pay) {
        if (pay == null) {
            return;
        }
        pay.setPaymentType(this.paymentTypeF.getText());
        pay.setAmount(FormatUtil.parseNumber(this.amountF.getText()));
        pay.setValueDate(this.valueDateF.getText());
        pay.setStatus(Objects.toString(this.statusCb.getSelectedItem(), ""));
    }

    private void doSave() {
        this.saveTo(this.state.getSelectedPayment());
        JOptionPane.showMessageDialog(this, JsonUtil.toJson(this.state.getSelectedPayment()), "Payment JSON", 1);
        this.state.setLastMessage("Payment saved");
        this.onRefresh.run();
    }

    private void doValidate() {
        if (this.valueDateF.getText().isBlank()) {
            JOptionPane.showMessageDialog(this, "Value Date required.", "Validation", 0);
            return;
        }
        if (FormatUtil.parseNumber(this.amountF.getText()) < 0.0) {
            JOptionPane.showMessageDialog(this, "Amount must be non-negative.", "Validation", 0);
            return;
        }
        JOptionPane.showMessageDialog(this, "Validation passed.", "Validate", 1);
        this.state.setLastMessage("Payment validated");
        this.onRefresh.run();
    }

    private void doClose() {
        Payment pay = this.state.getSelectedPayment();
        if (pay != null) {
            pay.setStatus("Closed");
            this.statusCb.setSelectedItem("Closed");
        }
        this.state.setLastMessage("Payment Closed");
        this.onRefresh.run();
    }
}

