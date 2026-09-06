package com.northstar.capital;

import com.northstar.capital.service.AppLog;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.ui.MainFrame;
import com.northstar.capital.ui.Theme;

import javax.swing.SwingUtilities;

public final class Main {
    private Main() {
    }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            Theme.install();
            ApplicationContext context = new ApplicationContext();
            MainFrame frame = new MainFrame(context);
            frame.setVisible(true);
            AppLog.ui("Northstar Capital Markets Workstation started");
        });
    }
}
