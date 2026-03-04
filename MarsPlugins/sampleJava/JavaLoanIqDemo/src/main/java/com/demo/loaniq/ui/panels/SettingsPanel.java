package com.demo.loaniq.ui.panels;

import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.Insets;
import javax.swing.BorderFactory;
import javax.swing.ButtonGroup;
import javax.swing.JCheckBox;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JRadioButton;

/**
 * LoanIQ Settings tab for SetBox keyword verification.
 * Deal type, facility/loan options with radio and checkbox controls.
 */
public class SettingsPanel extends JPanel {

    public SettingsPanel() {
        setLayout(new GridBagLayout());
        setBorder(BorderFactory.createEmptyBorder(12, 12, 12, 12));
        GridBagConstraints c = new GridBagConstraints();
        c.insets = new Insets(4, 6, 4, 6);
        c.anchor = GridBagConstraints.WEST;
        c.fill = GridBagConstraints.HORIZONTAL;
        c.gridx = 0;
        c.gridy = 0;

        // --- Deal workflow type (Radio - SetBox) ---
        JPanel radioPanel = new JPanel(new GridBagLayout());
        radioPanel.setBorder(BorderFactory.createTitledBorder("Deal Workflow Type"));
        radioPanel.setName("LIQ_SETTINGS_RADIO_GROUP");
        ButtonGroup radioGroup = new ButtonGroup();
        JRadioButton syndicated = new JRadioButton("Syndicated", true);
        syndicated.setName("LIQ_SETTINGS_RADIO_SYNDICATED");
        JRadioButton bilateral = new JRadioButton("Bilateral", false);
        bilateral.setName("LIQ_SETTINGS_RADIO_BILATERAL");
        JRadioButton singleLender = new JRadioButton("Single Lender", false);
        singleLender.setName("LIQ_SETTINGS_RADIO_SINGLE_LENDER");
        radioGroup.add(syndicated);
        radioGroup.add(bilateral);
        radioGroup.add(singleLender);
        GridBagConstraints rc = new GridBagConstraints();
        rc.insets = new Insets(2, 4, 2, 4);
        rc.anchor = GridBagConstraints.WEST;
        rc.gridx = 0;
        rc.gridy = 0;
        radioPanel.add(syndicated, rc);
        rc.gridy = 1;
        radioPanel.add(bilateral, rc);
        rc.gridy = 2;
        radioPanel.add(singleLender, rc);
        c.gridy = 0;
        add(radioPanel, c);

        // --- Facility / Loan options (Checkbox - SetBox) ---
        JPanel checkPanel = new JPanel(new GridBagLayout());
        checkPanel.setBorder(BorderFactory.createTitledBorder("Facility & Loan Options"));
        checkPanel.setName("LIQ_SETTINGS_CHECK_GROUP");
        JCheckBox chkAutoApprove = new JCheckBox("Auto-approve facility on release", false);
        chkAutoApprove.setName("LIQ_SETTINGS_CHK_AUTO_APPROVE");
        JCheckBox chkConfirmRelease = new JCheckBox("Confirm loan release", true);
        chkConfirmRelease.setName("LIQ_SETTINGS_CHK_CONFIRM_RELEASE");
        JCheckBox chkVerboseAudit = new JCheckBox("Verbose audit trail", false);
        chkVerboseAudit.setName("LIQ_SETTINGS_CHK_VERBOSE_AUDIT");
        GridBagConstraints cc = new GridBagConstraints();
        cc.insets = new Insets(2, 4, 2, 4);
        cc.anchor = GridBagConstraints.WEST;
        cc.gridx = 0;
        cc.gridy = 0;
        checkPanel.add(chkAutoApprove, cc);
        cc.gridy = 1;
        checkPanel.add(chkConfirmRelease, cc);
        cc.gridy = 2;
        checkPanel.add(chkVerboseAudit, cc);
        c.gridy = 1;
        add(checkPanel, c);

        JLabel hint = new JLabel("<html>SetBox: data = yes/on/true or no/off/false.</html>");
        c.gridy = 2;
        c.weighty = 1.0;
        add(hint, c);
    }
}
