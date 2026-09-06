package com.northstar.capital.ui.blotter;

import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.TableSupport;
import com.northstar.capital.ui.common.UiFactory;
import com.northstar.capital.ui.dialog.TradeAuditDialog;

import javax.swing.JButton;
import javax.swing.JLabel;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JScrollPane;
import javax.swing.JTable;
import javax.swing.JTextField;
import javax.swing.RowFilter;
import javax.swing.SwingUtilities;
import javax.swing.table.DefaultTableModel;
import javax.swing.table.TableRowSorter;
import java.awt.BorderLayout;
import java.awt.FlowLayout;
import java.awt.Frame;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.util.List;
import java.util.function.Consumer;

public class TradeBlotterPanel extends JPanel {
    private final ApplicationContext context;
    private final DefaultTableModel model;
    private final JTable table;
    private final JTextField searchField = UiFactory.text("blotter.search", 20);
    private final TableRowSorter<DefaultTableModel> sorter;
    private Consumer<Trade> openHandler = trade -> {
    };
    private Consumer<Trade> cloneHandler = trade -> {
    };
    private Consumer<Trade> amendHandler = trade -> {
    };

    public TradeBlotterPanel(ApplicationContext context) {
        this.context = context;
        setName("tradeBlotterPanel");
        setLayout(new BorderLayout(4, 4));
        setBackground(Theme.WORKSPACE_BG);
        setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));

        model = new DefaultTableModel(new Object[]{
                "Trade ID", "Status", "Product", "Side", "Security", "Notional", "Currency",
                "Price", "Counterparty", "Trade Date", "Settlement Date", "Trader", "Book", "Last Updated"
        }, 0) {
            @Override
            public boolean isCellEditable(int row, int column) {
                return false;
            }
        };
        table = TableSupport.denseTable(model, "blotter.table");
        sorter = new TableRowSorter<>(model);
        table.setRowSorter(sorter);
        TableSupport.setColumnWidths(table, 150, 80, 70, 50, 160, 110, 70, 70, 90, 90, 110, 80, 110, 90);

        JPopupMenu menu = new JPopupMenu();
        menu.setName("blotter.contextMenu");
        menu.add(item("Open Trade", "blotter.menu.open", () -> withSelected(openHandler)));
        menu.add(item("Clone Trade", "blotter.menu.clone", () -> withSelected(cloneHandler)));
        menu.add(item("Amend Trade", "blotter.menu.amend", () -> withSelected(amendHandler)));
        menu.add(item("Cancel Trade", "blotter.menu.cancel", this::cancelSelected));
        menu.add(item("View Audit History", "blotter.menu.audit", this::showAudit));
        menu.add(item("Export", "blotter.menu.export", this::exportRows));

        table.addMouseListener(new MouseAdapter() {
            @Override
            public void mousePressed(MouseEvent e) {
                maybePopup(e);
            }

            @Override
            public void mouseReleased(MouseEvent e) {
                maybePopup(e);
            }

            @Override
            public void mouseClicked(MouseEvent e) {
                if (e.getClickCount() == 2 && SwingUtilities.isLeftMouseButton(e)) {
                    withSelected(openHandler);
                }
            }

            private void maybePopup(MouseEvent e) {
                if (e.isPopupTrigger()) {
                    int row = table.rowAtPoint(e.getPoint());
                    if (row >= 0) {
                        table.setRowSelectionInterval(row, row);
                    }
                    menu.show(table, e.getX(), e.getY());
                }
            }
        });

        JPanel filter = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 2));
        filter.setOpaque(false);
        JLabel label = UiFactory.label("Search", "label.blotter.search");
        JButton refresh = UiFactory.button("Refresh", "blotter.refresh");
        refresh.addActionListener(e -> reload());
        searchField.getDocument().addDocumentListener(new javax.swing.event.DocumentListener() {
            @Override
            public void insertUpdate(javax.swing.event.DocumentEvent e) {
                applyFilter();
            }

            @Override
            public void removeUpdate(javax.swing.event.DocumentEvent e) {
                applyFilter();
            }

            @Override
            public void changedUpdate(javax.swing.event.DocumentEvent e) {
                applyFilter();
            }
        });
        filter.add(label);
        filter.add(searchField);
        filter.add(refresh);

        add(filter, BorderLayout.NORTH);
        add(new JScrollPane(table), BorderLayout.CENTER);
        reload();
    }

    public void setOpenHandler(Consumer<Trade> openHandler) {
        this.openHandler = openHandler;
    }

    public void setCloneHandler(Consumer<Trade> cloneHandler) {
        this.cloneHandler = cloneHandler;
    }

    public void setAmendHandler(Consumer<Trade> amendHandler) {
        this.amendHandler = amendHandler;
    }

    public void reload() {
        model.setRowCount(0);
        for (Trade trade : context.getTradeService().getTrades()) {
            model.addRow(toRow(trade));
        }
    }

    public void applyStatusFilter(TradeStatus status) {
        searchField.setText(status.displayName());
    }

    public Trade getSelectedTrade() {
        int viewRow = table.getSelectedRow();
        if (viewRow < 0) {
            return null;
        }
        int row = table.convertRowIndexToModel(viewRow);
        String tradeId = String.valueOf(model.getValueAt(row, 0));
        return context.getTradeService().findById(tradeId).orElse(null);
    }

    private void applyFilter() {
        String query = searchField.getText();
        if (query == null || query.isBlank()) {
            sorter.setRowFilter(null);
        } else {
            sorter.setRowFilter(RowFilter.regexFilter("(?i)" + java.util.regex.Pattern.quote(query)));
        }
    }

    private void withSelected(Consumer<Trade> handler) {
        Trade trade = getSelectedTrade();
        if (trade != null) {
            handler.accept(trade);
        }
    }

    private void cancelSelected() {
        Trade trade = getSelectedTrade();
        if (trade == null) {
            return;
        }
        int choice = JOptionPane.showConfirmDialog(this,
                "Cancel trade " + trade.getTradeId() + "?",
                "Cancel Trade", JOptionPane.YES_NO_OPTION);
        if (choice == JOptionPane.YES_OPTION) {
            context.getTradeService().cancel(trade);
            context.notifyTradesChanged("cancelled:" + trade.getTradeId());
            reload();
        }
    }

    private void showAudit() {
        Trade trade = getSelectedTrade();
        if (trade == null) {
            return;
        }
        Frame frame = (Frame) SwingUtilities.getWindowAncestor(this);
        new TradeAuditDialog(frame, trade).setVisible(true);
    }

    private void exportRows() {
        JOptionPane.showMessageDialog(this,
                "Exported " + table.getRowCount() + " blotter rows to workspace clipboard buffer.",
                "Export", JOptionPane.INFORMATION_MESSAGE);
    }

    private JMenuItem item(String text, String name, Runnable action) {
        JMenuItem item = new JMenuItem(text);
        item.setName(name);
        item.addActionListener(e -> action.run());
        return item;
    }

    private static Object[] toRow(Trade trade) {
        return new Object[]{
                trade.getTradeId(),
                trade.getStatus() == null ? "" : trade.getStatus().displayName(),
                trade.getProduct(),
                trade.getSide() == null ? "" : trade.getSide().displayName(),
                trade.getInstrument(),
                MoneyFormats.formatAmount(trade.getNotional()),
                trade.getCurrency(),
                MoneyFormats.formatPrice(trade.getPrice()),
                trade.getCounterparty(),
                MoneyFormats.formatDate(trade.getTradeDate()),
                MoneyFormats.formatDate(trade.getSettlementDate()),
                trade.getTrader(),
                trade.getBook(),
                MoneyFormats.formatTime(trade.getLastUpdated())
        };
    }
}
