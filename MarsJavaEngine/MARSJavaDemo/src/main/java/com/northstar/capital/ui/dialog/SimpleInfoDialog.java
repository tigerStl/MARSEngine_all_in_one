package com.northstar.capital.ui.dialog;

import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JScrollPane;
import javax.swing.JTextArea;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;

public class SimpleInfoDialog extends JDialog {
    public SimpleInfoDialog(Frame owner, String title, String name, String body) {
        super(owner, title, true);
        setName(name);
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);
        setSize(520, 360);
        setLocationRelativeTo(owner);

        JTextArea area = new JTextArea(body);
        area.setName(name + ".body");
        area.setEditable(false);
        area.setFont(Theme.UI_FONT);
        area.setLineWrap(true);
        area.setWrapStyleWord(true);

        JButton close = UiFactory.button("Close", name + ".close");
        close.addActionListener(e -> dispose());
        javax.swing.JPanel south = new javax.swing.JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(close);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(new JScrollPane(area), BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }
}
