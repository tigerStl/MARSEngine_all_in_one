package com.northstar.capital.ui;

import com.northstar.capital.model.Book;
import com.northstar.capital.model.Counterparty;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.SettlementInstruction;
import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.service.AppLog;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.blotter.TradeBlotterPanel;
import com.northstar.capital.ui.common.PlaceholderPanel;
import com.northstar.capital.ui.common.UiFactory;
import com.northstar.capital.ui.dashboard.DashboardPanel;
import com.northstar.capital.ui.dialog.DiagnosticsDialog;
import com.northstar.capital.ui.dialog.EnvironmentDialog;
import com.northstar.capital.ui.dialog.PreferencesDialog;
import com.northstar.capital.ui.dialog.SecuritySearchDialog;
import com.northstar.capital.ui.dialog.SimpleInfoDialog;
import com.northstar.capital.ui.lab.AutomationLabPanel;
import com.northstar.capital.ui.operations.SettlementExceptionsPanel;
import com.northstar.capital.ui.reference.ReferenceTablePanel;
import com.northstar.capital.ui.risk.PositionsPanel;
import com.northstar.capital.ui.risk.RiskMonitorPanel;
import com.northstar.capital.ui.trade.BondTradeEntryPanel;

import javax.swing.AbstractAction;
import javax.swing.JButton;
import javax.swing.JComponent;
import javax.swing.JFrame;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JSplitPane;
import javax.swing.JTabbedPane;
import javax.swing.JToolBar;
import javax.swing.KeyStroke;
import javax.swing.SwingUtilities;
import javax.swing.WindowConstants;
import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.event.ActionEvent;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.util.HashMap;
import java.util.Map;

public class MainFrame extends JFrame {
    private final ApplicationContext context;
    private final HeaderPanel headerPanel;
    private final StatusBar statusBar;
    private final NavigationPanel navigationPanel;
    private final JTabbedPane workspace = new JTabbedPane();
    private final BondTradeEntryPanel bondTradeEntryPanel;
    private final TradeBlotterPanel tradeBlotterPanel;
    private final Map<String, JComponent> tabs = new HashMap<>();

    public MainFrame(ApplicationContext context) {
        this.context = context;
        setTitle(ApplicationContext.APP_NAME);
        setName("mainFrame");
        setDefaultCloseOperation(WindowConstants.EXIT_ON_CLOSE);
        setMinimumSize(new Dimension(1280, 800));
        setSize(1440, 900);
        setLocationRelativeTo(null);

        headerPanel = new HeaderPanel(context);
        statusBar = new StatusBar(context);
        bondTradeEntryPanel = new BondTradeEntryPanel(context);
        tradeBlotterPanel = new TradeBlotterPanel(context);
        tradeBlotterPanel.setOpenHandler(this::openTrade);
        tradeBlotterPanel.setCloneHandler(this::cloneTrade);
        tradeBlotterPanel.setAmendHandler(this::amendTrade);

        workspace.setName("workspaceTabs");
        workspace.setFont(Theme.UI_FONT);
        addTab("Bond Trade Entry", bondTradeEntryPanel, true);
        addTab("Trade Blotter", tradeBlotterPanel, true);
        addTab("Positions", new PositionsPanel(context), true);
        addTab("Risk Monitor", new RiskMonitorPanel(context), true);
        addTab("Settlement Exceptions", new SettlementExceptionsPanel(context), true);

        navigationPanel = new NavigationPanel(this::onNavigate);
        JSplitPane split = new JSplitPane(JSplitPane.HORIZONTAL_SPLIT, navigationPanel, workspace);
        split.setName("mainSplit");
        split.setDividerLocation(220);
        split.setContinuousLayout(true);

        JPanel center = new JPanel(new BorderLayout());
        center.add(buildToolbar(), BorderLayout.NORTH);
        center.add(split, BorderLayout.CENTER);

        setJMenuBar(buildMenuBar());
        getContentPane().setLayout(new BorderLayout());
        getContentPane().add(headerPanel, BorderLayout.NORTH);
        getContentPane().add(center, BorderLayout.CENTER);
        getContentPane().add(statusBar, BorderLayout.SOUTH);

        bindKeys();
        context.addTradeListener(reason -> SwingUtilities.invokeLater(() -> {
            tradeBlotterPanel.reload();
            statusBar.refresh(context);
            headerPanel.refresh(context);
        }));
        SwingUtilities.invokeLater(bondTradeEntryPanel.initialFocus()::requestFocusInWindow);
    }

    private JMenuBar buildMenuBar() {
        JMenuBar bar = new JMenuBar();
        bar.setName("menuBar");
        bar.setBackground(Theme.MENU_BG);

        JMenu file = menu("File", "menu.file");
        file.add(item("New Trade", "menu.file.newTrade", KeyStroke.getKeyStroke(KeyEvent.VK_N, InputEvent.CTRL_DOWN_MASK), this::newTrade));
        file.add(item("Open", "menu.file.open", null, () -> showTab("Trade Blotter")));
        file.add(item("Save", "menu.file.save", KeyStroke.getKeyStroke(KeyEvent.VK_S, InputEvent.CTRL_DOWN_MASK), bondTradeEntryPanel::saveTrade));
        file.add(item("Export", "menu.file.export", null, () -> JOptionPane.showMessageDialog(this, "Blotter export requested.", "Export", JOptionPane.INFORMATION_MESSAGE)));
        file.addSeparator();
        file.add(item("Exit", "menu.file.exit", null, this::confirmExit));

        JMenu trade = menu("Trade", "menu.trade");
        trade.add(item("New Bond Trade", "menu.trade.newBond", null, this::newTrade));
        trade.add(item("Validate", "menu.trade.validate", null, bondTradeEntryPanel::validateTrade));
        trade.add(item("Clone", "menu.trade.clone", null, () -> {
            Trade selected = tradeBlotterPanel.getSelectedTrade();
            if (selected != null) {
                cloneTrade(selected);
            }
        }));
        trade.add(item("Amend", "menu.trade.amend", null, () -> {
            Trade selected = tradeBlotterPanel.getSelectedTrade();
            if (selected != null) {
                amendTrade(selected);
            }
        }));
        trade.add(item("Cancel", "menu.trade.cancel", null, () -> showTab("Trade Blotter")));

        JMenu market = menu("Market", "menu.market");
        market.add(item("Security Search", "menu.market.securitySearch", null, this::openSecuritySearch));
        market.add(item("Market Data", "menu.market.marketData", null, () ->
                new SimpleInfoDialog(this, "Market Data", "dialog.marketData",
                        "UST 5.00 2035  98.250 / 98.265\nUST 4.25 2032  97.540 / 97.560\nBund 2.50 2034  97.180 / 97.210").setVisible(true)));
        market.add(item("Yield Curves", "menu.market.yieldCurves", null, () ->
                new SimpleInfoDialog(this, "Yield Curves", "dialog.yieldCurves",
                        "USD SOFR\n2Y 3.41\n5Y 3.62\n10Y 3.88\n30Y 4.12").setVisible(true)));

        JMenu risk = menu("Risk", "menu.risk");
        risk.add(item("Positions", "menu.risk.positions", null, () -> showTab("Positions")));
        risk.add(item("Market Risk", "menu.risk.marketRisk", null, () -> showTab("Risk Monitor")));
        risk.add(item("Limits", "menu.risk.limits", null, () -> showTab("Risk Monitor")));

        JMenu operations = menu("Operations", "menu.operations");
        operations.add(item("Settlement Exceptions", "menu.operations.exceptions", null, () -> showTab("Settlement Exceptions")));
        operations.add(item("Confirmations", "menu.operations.confirmations", null, this::openConfirmations));

        JMenu reference = menu("Reference Data", "menu.reference");
        reference.add(item("Counterparties", "menu.reference.counterparties", null, this::openCounterparties));
        reference.add(item("Books", "menu.reference.books", null, this::openBooks));
        reference.add(item("Settlement Instructions", "menu.reference.ssi", null, this::openSsi));

        JMenu tools = menu("Tools", "menu.tools");
        tools.add(item("Preferences", "menu.tools.preferences", null, () ->
                new PreferencesDialog(this, context, user -> refreshChrome()).setVisible(true)));
        tools.add(item("Environment", "menu.tools.environment", null, () ->
                new EnvironmentDialog(this, context, env -> refreshChrome()).setVisible(true)));
        tools.add(item("Diagnostics", "menu.tools.diagnostics", null, () ->
                new DiagnosticsDialog(this, context).setVisible(true)));
        tools.add(item("Automation Lab", "menu.tools.automationLab", null, this::openAutomationLab));

        JMenu help = menu("Help", "menu.help");
        help.add(item("User Guide", "menu.help.userGuide", null, () ->
                new SimpleInfoDialog(this, "User Guide", "dialog.userGuide", userGuide()).setVisible(true)));
        help.add(item("About", "menu.help.about", null, () ->
                new SimpleInfoDialog(this, "About", "dialog.about",
                        ApplicationContext.APP_NAME + "\nVersion " + ApplicationContext.VERSION
                                + "\nFictional capital-markets workstation for MARS Java automation demos."
                                + "\nNo commercial vendor branding.").setVisible(true)));

        bar.add(file);
        bar.add(trade);
        bar.add(market);
        bar.add(risk);
        bar.add(operations);
        bar.add(reference);
        bar.add(tools);
        bar.add(help);
        return bar;
    }

    private JToolBar buildToolbar() {
        JToolBar bar = new JToolBar();
        bar.setName("toolbar");
        bar.setFloatable(false);
        bar.setBackground(new java.awt.Color(0xE4E8EE));
        bar.add(tool("New Trade", "toolbar.newTrade", "Create a new bond draft", this::newTrade));
        bar.add(tool("Validate", "toolbar.validate", "Validate the active bond ticket", bondTradeEntryPanel::validateTrade));
        bar.add(tool("Save", "toolbar.save", "Save the validated bond ticket", bondTradeEntryPanel::saveTrade));
        bar.add(tool("Clone", "toolbar.clone", "Clone the selected blotter trade", () -> {
            Trade selected = tradeBlotterPanel.getSelectedTrade();
            if (selected != null) {
                cloneTrade(selected);
            }
        }));
        bar.add(tool("Refresh", "toolbar.refresh", "Refresh blotter and status", this::refreshAll));
        bar.add(tool("Search", "toolbar.search", "Open security search", this::openSecuritySearch));
        return bar;
    }

    private void bindKeys() {
        JComponent root = getRootPane();
        root.getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW)
                .put(KeyStroke.getKeyStroke(KeyEvent.VK_N, InputEvent.CTRL_DOWN_MASK), "newTrade");
        root.getActionMap().put("newTrade", action(this::newTrade));
        root.getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW)
                .put(KeyStroke.getKeyStroke(KeyEvent.VK_S, InputEvent.CTRL_DOWN_MASK), "saveTrade");
        root.getActionMap().put("saveTrade", action(bondTradeEntryPanel::saveTrade));
        root.getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW)
                .put(KeyStroke.getKeyStroke(KeyEvent.VK_F5, 0), "refresh");
        root.getActionMap().put("refresh", action(this::refreshAll));
        root.getInputMap(JComponent.WHEN_IN_FOCUSED_WINDOW)
                .put(KeyStroke.getKeyStroke(KeyEvent.VK_ESCAPE, 0), "escape");
        root.getActionMap().put("escape", action(() -> context.setStatusMessage("Ready")));
    }

    private void onNavigate(String key) {
        switch (key) {
            case NavigationPanel.NAV_DASHBOARD -> openOrSelect("Dashboard", () -> new DashboardPanel(context));
            case NavigationPanel.NAV_BOND_ENTRY -> {
                showTab("Bond Trade Entry");
                AppLog.ui("Open screen: Bond Trade Entry");
            }
            case NavigationPanel.NAV_REPO -> openOrSelect("Repo", () ->
                    new PlaceholderPanel("repoPanel", "Repo Ticket", "Repo capture is view-only in this demo."));
            case NavigationPanel.NAV_SECURITY_SEARCH -> openSecuritySearch();
            case NavigationPanel.NAV_FX_SPOT -> openOrSelect("FX Spot", () ->
                    new PlaceholderPanel("fxSpotPanel", "FX Spot", "FX Spot ticket is view-only in this demo."));
            case NavigationPanel.NAV_FX_FORWARD -> openOrSelect("FX Forward", () ->
                    new PlaceholderPanel("fxForwardPanel", "FX Forward", "FX Forward ticket is view-only in this demo."));
            case NavigationPanel.NAV_FX_SWAP -> openOrSelect("FX Swap", () ->
                    new PlaceholderPanel("fxSwapPanel", "FX Swap", "FX Swap ticket is view-only in this demo."));
            case NavigationPanel.NAV_IRS -> openOrSelect("Interest Rate Swap", () ->
                    new PlaceholderPanel("irsPanel", "Interest Rate Swap", "IRS ticket is view-only in this demo."));
            case NavigationPanel.NAV_FRA -> openOrSelect("FRA", () ->
                    new PlaceholderPanel("fraPanel", "FRA", "FRA ticket is view-only in this demo."));
            case NavigationPanel.NAV_BLOTTER -> showTab("Trade Blotter");
            case NavigationPanel.NAV_AMENDMENTS -> {
                showTab("Trade Blotter");
                tradeBlotterPanel.applyStatusFilter(TradeStatus.AMENDED);
            }
            case NavigationPanel.NAV_CANCELLATIONS -> {
                showTab("Trade Blotter");
                tradeBlotterPanel.applyStatusFilter(TradeStatus.CANCELLED);
            }
            case NavigationPanel.NAV_POSITIONS -> showTab("Positions");
            case NavigationPanel.NAV_MARKET_RISK, NavigationPanel.NAV_LIMITS -> showTab("Risk Monitor");
            case NavigationPanel.NAV_SETTLEMENTS, NavigationPanel.NAV_EXCEPTIONS -> showTab("Settlement Exceptions");
            case NavigationPanel.NAV_CONFIRMATIONS -> openConfirmations();
            case NavigationPanel.NAV_COUNTERPARTIES -> openCounterparties();
            case NavigationPanel.NAV_SECURITIES -> openSecurities();
            case NavigationPanel.NAV_BOOKS -> openBooks();
            case NavigationPanel.NAV_SSI -> openSsi();
            default -> {
            }
        }
    }

    private void newTrade() {
        bondTradeEntryPanel.loadNewTrade();
        showTab("Bond Trade Entry");
        context.setStatusMessage("New bond draft ready.");
        refreshChrome();
    }

    private void openTrade(Trade trade) {
        bondTradeEntryPanel.loadTrade(trade, false);
        showTab("Bond Trade Entry");
        context.setStatusMessage("Opened " + trade.getTradeId());
        refreshChrome();
    }

    private void cloneTrade(Trade trade) {
        bondTradeEntryPanel.loadTrade(context.getTradeService().cloneTrade(trade), true);
        showTab("Bond Trade Entry");
        context.setStatusMessage("Cloned " + trade.getTradeId());
        refreshChrome();
    }

    private void amendTrade(Trade trade) {
        bondTradeEntryPanel.loadTrade(trade, false);
        showTab("Bond Trade Entry");
        context.setStatusMessage("Amend mode for " + trade.getTradeId());
        refreshChrome();
    }

    private void openSecuritySearch() {
        var security = SecuritySearchDialog.open(this, context);
        if (security != null) {
            bondTradeEntryPanel.applySecurity(security);
            showTab("Bond Trade Entry");
        }
    }

    private void openConfirmations() {
        Object[][] rows = context.getTradeService().getTrades().stream().limit(12)
                .map(trade -> new Object[]{trade.getTradeId(), trade.getCounterparty(), "Affirmed", "Electronic"})
                .toArray(Object[][]::new);
        openOrSelect("Confirmations", () -> new ReferenceTablePanel("confirmationsPanel",
                new String[]{"Trade ID", "Counterparty", "Status", "Channel"}, rows, new int[]{160, 100, 90, 100}));
    }

    private void openCounterparties() {
        Object[][] rows = context.getReferenceDataService().getCounterparties().stream()
                .map(this::cptyRow).toArray(Object[][]::new);
        openOrSelect("Counterparties", () -> new ReferenceTablePanel("counterpartiesPanel",
                new String[]{"Code", "Name", "Country", "Type", "Rating", "Status"}, rows,
                new int[]{70, 220, 80, 80, 70, 80}));
    }

    private void openSecurities() {
        Object[][] rows = context.getReferenceDataService().getSecurities().stream()
                .map(this::secRow).toArray(Object[][]::new);
        openOrSelect("Securities", () -> new ReferenceTablePanel("securitiesPanel",
                new String[]{"Security ID", "Issuer", "Description", "Coupon", "Maturity", "Currency", "Type"}, rows,
                new int[]{120, 180, 190, 70, 90, 70, 80}));
    }

    private void openBooks() {
        Object[][] rows = context.getReferenceDataService().getBooks().stream()
                .map(this::bookRow).toArray(Object[][]::new);
        openOrSelect("Books", () -> new ReferenceTablePanel("booksPanel",
                new String[]{"Code", "Name", "Currency", "Desk", "Legal Entity"}, rows,
                new int[]{120, 160, 80, 120, 180}));
    }

    private void openSsi() {
        Object[][] rows = context.getReferenceDataService().getSettlementInstructions().stream()
                .map(this::ssiRow).toArray(Object[][]::new);
        openOrSelect("Settlement Instructions", () -> new ReferenceTablePanel("ssiPanel",
                new String[]{"Code", "Counterparty", "Currency", "Location", "Account", "Custodian"}, rows,
                new int[]{150, 100, 80, 80, 130, 130}));
    }

    private void openAutomationLab() {
        openOrSelect("Automation Lab", AutomationLabPanel::new);
        AppLog.ui("Open screen: Automation Lab");
    }

    private void refreshAll() {
        tradeBlotterPanel.reload();
        context.setStatusMessage("Ready");
        refreshChrome();
    }

    private void refreshChrome() {
        headerPanel.refresh(context);
        statusBar.refresh(context);
    }

    private void confirmExit() {
        int choice = JOptionPane.showConfirmDialog(this, "Exit Northstar CM?", "Exit", JOptionPane.YES_NO_OPTION);
        if (choice == JOptionPane.YES_OPTION) {
            dispose();
            System.exit(0);
        }
    }

    private void addTab(String title, JComponent component, boolean persistent) {
        tabs.put(title, component);
        workspace.addTab(title, component);
        if (!persistent) {
            // reserved for closable optional workspaces
        }
    }

    private void showTab(String title) {
        JComponent component = tabs.get(title);
        if (component != null) {
            workspace.setSelectedComponent(component);
        }
    }

    private void openOrSelect(String title, java.util.function.Supplier<JComponent> factory) {
        if (!tabs.containsKey(title)) {
            addTab(title, factory.get(), false);
        }
        showTab(title);
        AppLog.ui("Open screen: " + title);
    }

    private JMenu menu(String text, String name) {
        JMenu menu = new JMenu(text);
        menu.setName(name);
        menu.setForeground(Theme.HEADER_FG);
        return menu;
    }

    private JMenuItem item(String text, String name, KeyStroke stroke, Runnable action) {
        JMenuItem item = new JMenuItem(text);
        item.setName(name);
        if (stroke != null) {
            item.setAccelerator(stroke);
        }
        item.addActionListener(e -> action.run());
        return item;
    }

    private JButton tool(String text, String name, String tooltip, Runnable action) {
        JButton button = UiFactory.button(text, name);
        button.setToolTipText(tooltip);
        button.addActionListener(e -> action.run());
        return button;
    }

    private static AbstractAction action(Runnable runnable) {
        return new AbstractAction() {
            @Override
            public void actionPerformed(ActionEvent e) {
                runnable.run();
            }
        };
    }

    private Object[] cptyRow(Counterparty item) {
        return new Object[]{item.getCode(), item.getName(), item.getCountry(), item.getType(), item.getRating(), item.getStatus()};
    }

    private Object[] secRow(Security item) {
        return new Object[]{item.getSecurityId(), item.getIssuer(), item.getDescription(), item.getCoupon(),
                MoneyFormats.formatDate(item.getMaturity()), item.getCurrency(), item.getType()};
    }

    private Object[] bookRow(Book item) {
        return new Object[]{item.getCode(), item.getName(), item.getCurrency(), item.getDesk(), item.getLegalEntity()};
    }

    private Object[] ssiRow(SettlementInstruction item) {
        return new Object[]{item.getCode(), item.getCounterparty(), item.getCurrency(), item.getLocation(),
                item.getAccount(), item.getCustodian()};
    }

    private static String userGuide() {
        return """
                Northstar Capital Markets Workstation

                1. Open Bond Trade Entry.
                2. Enter notional, price, counterparty and settlement location.
                3. Click Validate Trade.
                4. After PASSED, click Save Trade.
                5. Confirm the booking dialog and inspect Trade Blotter.

                Keyboard: Ctrl+N new trade, Ctrl+S save, F5 refresh.
                """;
    }
}
