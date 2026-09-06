package com.northstar.capital.ui.dialog;

import com.northstar.capital.model.AppEnvironment;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.ButtonGroup;
import javax.swing.JButton;
import javax.swing.JDialog;
import javax.swing.JPanel;
import javax.swing.JRadioButton;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.awt.GridLayout;
import java.util.function.Consumer;

public class EnvironmentDialog extends JDialog {
    public EnvironmentDialog(Frame owner, ApplicationContext context, Consumer<AppEnvironment> onChange) {
        super(owner, "Environment", true);
        setName("dialog.environment");
        setSize(320, 200);
        setLocationRelativeTo(owner);

        JPanel radios = new JPanel(new GridLayout(0, 1, 4, 4));
        radios.setBorder(javax.swing.BorderFactory.createEmptyBorder(12, 16, 8, 16));
        ButtonGroup group = new ButtonGroup();
        JRadioButton dev = UiFactory.radio("DEV", "environment.dev");
        JRadioButton uat = UiFactory.radio("UAT", "environment.uat");
        JRadioButton prod = UiFactory.radio("PROD", "environment.prod");
        group.add(dev);
        group.add(uat);
        group.add(prod);
        switch (context.getEnvironment()) {
            case DEV -> dev.setSelected(true);
            case PROD -> prod.setSelected(true);
            default -> uat.setSelected(true);
        }
        radios.add(dev);
        radios.add(uat);
        radios.add(prod);

        JButton apply = UiFactory.button("Apply", "environment.apply");
        apply.addActionListener(e -> {
            AppEnvironment env = prod.isSelected() ? AppEnvironment.PROD
                    : dev.isSelected() ? AppEnvironment.DEV : AppEnvironment.UAT;
            context.setEnvironment(env);
            onChange.accept(env);
            dispose();
        });
        JPanel south = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        south.add(apply);

        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(radios, BorderLayout.CENTER);
        getContentPane().add(south, BorderLayout.SOUTH);
    }
}
