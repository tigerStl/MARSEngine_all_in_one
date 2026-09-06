package com.northstar.capital.ui.dialog;

import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JLabel;
import javax.swing.JPanel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.awt.GridLayout;

public class TradeBookedDialog extends JDialog {
    public TradeBookedDialog(Frame owner, String tradeId) {
        super(owner, "Trade Booked", true);
        setName("dialog.tradeBooked");
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(360, 170);
        setLocationRelativeTo(owner);
        setResizable(false);

        JLabel message = new JLabel("Trade successfully booked.");
        message.setName("dialog.tradeBooked.message");
        message.setFont(Theme.UI_FONT);

        JLabel idCaption = new JLabel("Trade ID:");
        idCaption.setName("dialog.tradeBooked.tradeIdLabel");
        JLabel idValue = new JLabel(tradeId);
        idValue.setName("dialog.tradeBooked.tradeId");
        idValue.setFont(Theme.UI_FONT_BOLD);

        JPanel center = new JPanel(new GridLayout(3, 1, 0, 4));
        center.setBorder(javax.swing.BorderFactory.createEmptyBorder(16, 20, 8, 20));
        center.add(message);
        center.add(idCaption);
        center.add(idValue);

        JButton ok = UiFactory.button("OK", "dialog.tradeBooked.ok");
        ok.addActionListener(e -> dispose());
        getRootPane().setDefaultButton(ok);
        JPanel south = new JPanel(new FlowLayout(FlowLayout.CENTER));
        south.add(ok);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(center, BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }
}
