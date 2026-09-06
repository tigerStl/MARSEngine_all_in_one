package com.northstar.capital.ui.dashboard;

import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;

import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.table.DefaultTableModel;
import java.awt.BorderLayout;
import java.awt.GridLayout;
import java.util.List;

public class DashboardPanel extends JPanel {
    public DashboardPanel(ApplicationContext context) {
        setName("dashboardPanel");
        setLayout(new BorderLayout(6, 6));
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        List<Trade> trades = context.getTradeService().getTrades();
        long booked = trades.stream().filter(t -> t.getStatus() == TradeStatus.BOOKED).count();
        long cancelled = trades.stream().filter(t -> t.getStatus() == TradeStatus.CANCELLED).count();
        int exceptions = context.getReferenceDataService().getExceptions().size();

        JPanel kpis = new JPanel(new GridLayout(1, 4, 6, 0));
        kpis.setOpaque(false);
        kpis.add(kpi("Trades Loaded", String.valueOf(trades.size()), "dashboard.kpi.trades"));
        kpis.add(kpi("Booked", String.valueOf(booked), "dashboard.kpi.booked"));
        kpis.add(kpi("Cancelled", String.valueOf(cancelled), "dashboard.kpi.cancelled"));
        kpis.add(kpi("Open Exceptions", String.valueOf(exceptions), "dashboard.kpi.exceptions"));

        DefaultTableModel model = new DefaultTableModel(new Object[]{"Trade ID", "Status", "Counterparty", "Notional", "Price"}, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        trades.stream().limit(8).forEach(trade -> model.addRow(new Object[]{
                trade.getTradeId(),
                trade.getStatus().displayName(),
                trade.getCounterparty(),
                MoneyFormats.formatAmount(trade.getNotional()),
                MoneyFormats.formatPrice(trade.getPrice())
        }));
        JTable table = TableSupport.denseTable(model, "dashboard.recentTrades");
        TableSupport.setColumnWidths(table, 160, 80, 90, 120, 80);
        JPanel recent = UiFactory.titled("Recent Blotter Activity", "dashboard.recent");
        recent.setLayout(new BorderLayout());
        recent.add(new JScrollPane(table), BorderLayout.CENTER);

        add(kpis, BorderLayout.NORTH);
        add(recent, BorderLayout.CENTER);
    }

    private static JPanel kpi(String title, String value, String name) {
        JPanel panel = UiFactory.titled(title, name);
        panel.setLayout(new BorderLayout());
        JLabel label = new JLabel(value);
        label.setName(name + ".value");
        label.setFont(Theme.HEADER_FONT);
        panel.add(label, BorderLayout.CENTER);
        return panel;
    }
}
