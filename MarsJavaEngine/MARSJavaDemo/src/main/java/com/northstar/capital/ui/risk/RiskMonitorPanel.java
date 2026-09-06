package com.northstar.capital.ui.risk;

import com.northstar.capital.model.RiskRecord;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.Timer;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.GridLayout;
import java.math.BigDecimal;
import java.time.LocalTime;

public class RiskMonitorPanel extends JPanel {
    private final ApplicationContext context;
    private final DefaultTableModel marketModel;
    private final DefaultTableModel limitModel;
    private final DefaultTableModel pnlModel;
    private final JLabel summaryLabel = new JLabel();
    private final JCheckBox autoRefresh = UiFactory.check("Auto Refresh", "risk.autoRefresh");
    private final Timer timer;

    public RiskMonitorPanel(ApplicationContext context) {
        this.context = context;
        setName("riskMonitorPanel");
        setLayout(new BorderLayout(4, 4));
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        summaryLabel.setName("risk.portfolioSummary");
        summaryLabel.setFont(Theme.UI_FONT);

        marketModel = sectionModel("Book", "DV01", "CS01", "VaR", "Limit Usage");
        limitModel = sectionModel("Book", "Limit Usage", "Currency");
        pnlModel = sectionModel("Book", "Daily P&L", "Currency");

        JTable marketTable = TableSupport.denseTable(marketModel, "risk.marketTable");
        JTable limitTable = TableSupport.denseTable(limitModel, "risk.limitTable");
        JTable pnlTable = TableSupport.denseTable(pnlModel, "risk.pnlTable");
        TableSupport.setColumnWidths(marketTable, 120, 90, 90, 90, 90);
        TableSupport.setColumnWidths(limitTable, 120, 100, 80);
        TableSupport.setColumnWidths(pnlTable, 120, 110, 80);

        JPanel tables = new JPanel(new GridLayout(2, 2, 6, 6));
        tables.setOpaque(false);
        tables.add(wrap("Portfolio Summary", summaryPanel()));
        tables.add(wrap("Market Risk", new JScrollPane(marketTable)));
        tables.add(wrap("Limit Utilization", new JScrollPane(limitTable)));
        tables.add(wrap("P&L", new JScrollPane(pnlTable)));

        JButton refresh = UiFactory.button("Refresh", "risk.refresh");
        refresh.addActionListener(e -> reload("Manual refresh"));
        autoRefresh.setSelected(true);
        JPanel toolbar = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 2));
        toolbar.setOpaque(false);
        toolbar.add(autoRefresh);
        toolbar.add(refresh);

        add(toolbar, BorderLayout.NORTH);
        add(tables, BorderLayout.CENTER);

        timer = new Timer(15000, e -> {
            if (autoRefresh.isSelected()) {
                reload("Auto refresh " + MoneyFormats.formatTime(LocalTime.now()));
            }
        });
        timer.start();
        reload("Ready");
    }

    private JPanel summaryPanel() {
        JPanel panel = new JPanel(new BorderLayout());
        panel.setBackground(Theme.PANEL_BG);
        panel.add(summaryLabel, BorderLayout.NORTH);
        return panel;
    }

    private JPanel wrap(String title, java.awt.Component body) {
        JPanel panel = UiFactory.titled(title, "risk.section." + title.replace(" ", ""));
        panel.setLayout(new BorderLayout());
        panel.add(body, BorderLayout.CENTER);
        return panel;
    }

    private void reload(String reason) {
        marketModel.setRowCount(0);
        limitModel.setRowCount(0);
        pnlModel.setRowCount(0);
        BigDecimal dv01 = BigDecimal.ZERO;
        BigDecimal var = BigDecimal.ZERO;
        for (RiskRecord record : context.getReferenceDataService().getRiskRecords()) {
            marketModel.addRow(new Object[]{
                    record.getBook(),
                    MoneyFormats.formatAmount(record.getDv01()),
                    MoneyFormats.formatAmount(record.getCs01()),
                    formatVar(record.getVar()),
                    percent(record.getLimitUsage())
            });
            limitModel.addRow(new Object[]{record.getBook(), percent(record.getLimitUsage()), record.getCurrency()});
            pnlModel.addRow(new Object[]{record.getBook(), MoneyFormats.formatAmount(record.getDailyPnl()), record.getCurrency()});
            dv01 = dv01.add(record.getDv01());
            var = var.add(record.getVar());
        }
        summaryLabel.setText(String.format(
                "<html>Firm DV01 %s&nbsp;&nbsp;|&nbsp;&nbsp;Aggregated VaR %s&nbsp;&nbsp;|&nbsp;&nbsp;%s<br/>%s</html>",
                MoneyFormats.formatAmount(dv01), formatVar(var), reason,
                "Environment " + context.getEnvironment() + " — indicative risk only."));
    }

    private static String formatVar(BigDecimal value) {
        if (value == null) {
            return "";
        }
        return MoneyFormats.formatAmount(value.divide(new BigDecimal("1000000"), 2, java.math.RoundingMode.HALF_UP)) + "M";
    }

    private static String percent(BigDecimal value) {
        if (value == null) {
            return "";
        }
        return value.multiply(new BigDecimal("100")).setScale(0, java.math.RoundingMode.HALF_UP) + "%";
    }

    private static DefaultTableModel sectionModel(String... columns) {
        return new DefaultTableModel(columns, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
    }
}
