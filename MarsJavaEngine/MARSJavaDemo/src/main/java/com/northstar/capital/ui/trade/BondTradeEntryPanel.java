package com.northstar.capital.ui.trade;

import com.northstar.capital.model.Counterparty;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeSide;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.model.ValidationResult;
import com.northstar.capital.service.AppLog;
import com.northstar.capital.service.ApplicationContext;
import com.northstar.capital.service.EconomicsCalculator;
import com.northstar.capital.service.MoneyFormats;
import com.northstar.capital.ui.Theme;
import com.northstar.capital.ui.common.FormGrid;
import com.northstar.capital.ui.common.UiFactory;
import com.northstar.capital.ui.dialog.CounterpartySearchDialog;
import com.northstar.capital.ui.dialog.SecuritySearchDialog;
import com.northstar.capital.ui.dialog.TradeBookedDialog;

import javax.swing.ButtonGroup;
import javax.swing.JButton;
import javax.swing.JCheckBox;
import javax.swing.JComboBox;
import javax.swing.JComponent;
import javax.swing.JFormattedTextField;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JRadioButton;
import javax.swing.JScrollPane;
import javax.swing.JSpinner;
import javax.swing.JTextField;
import javax.swing.SpinnerNumberModel;
import javax.swing.SwingUtilities;
import javax.swing.event.DocumentEvent;
import javax.swing.event.DocumentListener;
import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.FlowLayout;
import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.time.format.DateTimeParseException;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

public class BondTradeEntryPanel extends JPanel {
    private final ApplicationContext context;

    private final JTextField tradeIdField = UiFactory.text("bond.tradeId", 16);
    private final JTextField statusField = UiFactory.text("bond.status", 14);
    private final JFormattedTextField tradeDateField = dateField("bond.tradeDate");
    private final JTextField traderField = UiFactory.text("bond.trader", 12);
    private final JComboBox<String> bookCombo;
    private final JComboBox<String> legalEntityCombo;

    private final JTextField instrumentField = UiFactory.text("bond.instrument", 28);
    private final JButton searchSecurityButton = UiFactory.button("Search...", "bond.instrument.search");
    private final JTextField securityIdField = UiFactory.text("bond.securityId", 18);
    private final JRadioButton buyRadio = UiFactory.radio("Buy", "bond.side.buy");
    private final JRadioButton sellRadio = UiFactory.radio("Sell", "bond.side.sell");
    private final JTextField notionalField = UiFactory.text("bond.notional", 16);
    private final JComboBox<String> currencyCombo;
    private final JTextField priceField = UiFactory.text("bond.price", 12);
    private final JComboBox<String> priceTypeCombo = UiFactory.combo("bond.priceType", List.of("Clean", "Dirty"));
    private final JTextField yieldField = UiFactory.text("bond.yield", 10);
    private final JSpinner lotsSpinner = new JSpinner(new SpinnerNumberModel(1, 1, 99, 1));

    private final JTextField counterpartyField = UiFactory.text("bond.counterparty", 16);
    private final JButton lookupCptyButton = UiFactory.button("Lookup...", "bond.counterparty.lookup");
    private final JFormattedTextField settlementDateField = dateField("bond.settlementDate");
    private final JComboBox<String> settlementLocationCombo;
    private final JComboBox<String> ssiCombo;
    private final JComboBox<String> settlementCurrencyCombo;
    private final JCheckBox useStandardSsi = UiFactory.check("Use standard settlement instructions", "bond.useStandardSSI");
    private final JCheckBox manualOverride = UiFactory.check("Manual settlement override", "bond.manualSettlementOverride");
    private final JCheckBox advancedPricing = UiFactory.check("Show Advanced Pricing", "bond.showAdvancedPricing");

    private final JPanel manualPanel;
    private final JTextField settlementAccountField = UiFactory.text("bond.settlementAccount", 16);
    private final JTextField custodianField = UiFactory.text("bond.custodian", 16);
    private final JTextField agentBankField = UiFactory.text("bond.agentBank", 16);
    private final JTextField specialInstructionsField = UiFactory.text("bond.specialInstructions", 24);

    private final JPanel advancedPanel;
    private final JTextField spreadField = UiFactory.text("bond.spread", 10);
    private final JTextField benchmarkField = UiFactory.text("bond.benchmark", 14);
    private final JTextField discountMarginField = UiFactory.text("bond.discountMargin", 10);
    private final JSpinner accruedDaysSpinner = new JSpinner(new SpinnerNumberModel(52, 0, 366, 1));

    private final JTextField grossField = UiFactory.text("bond.grossAmount", 16);
    private final JTextField accruedField = UiFactory.text("bond.accruedInterest", 16);
    private final JTextField netField = UiFactory.text("bond.netSettlement", 16);
    private final JLabel jpyBanner = new JLabel("JPY settlement convention applied.");

    private final JButton validateButton = UiFactory.button("Validate Trade", "bond.validate");
    private final JButton saveButton = UiFactory.button("Save Trade", "bond.save");
    private final JButton cancelButton = UiFactory.button("Cancel", "bond.cancel");
    private final JLabel validationStatus = new JLabel("Validation: Ready");

    private boolean applying;
    private boolean validated;

    public BondTradeEntryPanel(ApplicationContext context) {
        this.context = context;
        setName("bondTradeEntryPanel");
        setLayout(new BorderLayout());
        setBackground(Theme.WORKSPACE_BG);

        bookCombo = UiFactory.combo("bond.book", context.getReferenceDataService().getBookCodes());
        legalEntityCombo = UiFactory.combo("bond.legalEntity", context.getReferenceDataService().getLegalEntities());
        currencyCombo = UiFactory.combo("bond.currency", context.getReferenceDataService().getCurrencies());
        settlementLocationCombo = UiFactory.combo("settlementCombo", context.getReferenceDataService().getSettlementLocations());
        settlementCurrencyCombo = UiFactory.combo("settlementCurrencyCombo", context.getReferenceDataService().getCurrencies());
        ssiCombo = UiFactory.combo("bond.ssi", context.getReferenceDataService().getSettlementInstructions()
                .stream().map(item -> item.getCode()).toList());

        lotsSpinner.setName("bond.lots");
        accruedDaysSpinner.setName("bond.accruedDays");
        ((JSpinner.DefaultEditor) lotsSpinner.getEditor()).getTextField().setName("bond.lots.editor");
        ((JSpinner.DefaultEditor) accruedDaysSpinner.getEditor()).getTextField().setName("bond.accruedDays.editor");

        tradeIdField.setEditable(false);
        statusField.setEditable(false);
        traderField.setEditable(false);
        grossField.setEditable(false);
        accruedField.setEditable(false);
        netField.setEditable(false);
        saveButton.setEnabled(false);
        validationStatus.setName("bond.validationStatus");
        validationStatus.setFont(Theme.UI_FONT_BOLD);

        jpyBanner.setName("bond.jpyConvention");
        jpyBanner.setForeground(new Color(0x7A4E00));
        jpyBanner.setVisible(false);

        ButtonGroup sideGroup = new ButtonGroup();
        sideGroup.add(buyRadio);
        sideGroup.add(sellRadio);
        buyRadio.setSelected(true);

        JPanel header = headerPanel();
        JPanel product = productPanel();
        JPanel cpty = counterpartyPanel();
        manualPanel = manualSettlementPanel();
        advancedPanel = advancedPricingPanel();
        JPanel economics = economicsPanel();
        JPanel workflow = workflowPanel();

        JPanel stack = new JPanel();
        stack.setLayout(new javax.swing.BoxLayout(stack, javax.swing.BoxLayout.Y_AXIS));
        stack.setOpaque(false);
        stack.add(header);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(product);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(cpty);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(manualPanel);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(advancedPanel);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(economics);
        stack.add(javax.swing.Box.createVerticalStrut(4));
        stack.add(workflow);

        JPanel wrap = new JPanel(new BorderLayout());
        wrap.setBackground(Theme.WORKSPACE_BG);
        wrap.setBorder(javax.swing.BorderFactory.createEmptyBorder(6, 6, 6, 6));
        wrap.add(stack, BorderLayout.NORTH);
        add(new JScrollPane(wrap), BorderLayout.CENTER);

        wireEvents();
        loadNewTrade();
        SwingUtilities.invokeLater(instrumentField::requestFocusInWindow);
    }

    public void loadNewTrade() {
        loadTrade(context.getTradeService().newDraft(), true);
        AppLog.ui("Open screen: Bond Trade Entry");
    }

    public void loadTrade(Trade trade, boolean asDraftTemplate) {
        applying = true;
        try {
            tradeIdField.setText(asDraftTemplate ? "NEW" : trade.getTradeId());
            statusField.setText(asDraftTemplate ? TradeStatus.DRAFT.displayName() : trade.getStatus().displayName());
            tradeDateField.setText(MoneyFormats.formatDate(trade.getTradeDate()));
            traderField.setText(trade.getTrader());
            bookCombo.setSelectedItem(trade.getBook());
            legalEntityCombo.setSelectedItem(trade.getLegalEntity());
            instrumentField.setText(nvl(trade.getInstrument()));
            securityIdField.setText(nvl(trade.getSecurityId()));
            if (trade.getSide() == TradeSide.SELL) {
                sellRadio.setSelected(true);
            } else {
                buyRadio.setSelected(true);
            }
            notionalField.setText(trade.getNotional() == null ? "" : trade.getNotional().toPlainString());
            currencyCombo.setSelectedItem(trade.getCurrency());
            priceField.setText(trade.getPrice() == null ? "" : trade.getPrice().stripTrailingZeros().toPlainString());
            priceTypeCombo.setSelectedItem(trade.getPriceType());
            yieldField.setText(trade.getYield() == null ? "" : trade.getYield().toPlainString());
            lotsSpinner.setValue(trade.getLots() == null ? 1 : trade.getLots());
            counterpartyField.setText(nvl(trade.getCounterparty()));
            settlementDateField.setText(MoneyFormats.formatDate(trade.getSettlementDate()));
            settlementLocationCombo.setSelectedItem(trade.getSettlementLocation());
            settlementCurrencyCombo.setSelectedItem(trade.getSettlementCurrency());
            refreshSsiChoices();
            if (trade.getSsi() != null) {
                ssiCombo.setSelectedItem(trade.getSsi());
            }
            useStandardSsi.setSelected(trade.isUseStandardSsi());
            manualOverride.setSelected(trade.isManualSettlementOverride());
            settlementAccountField.setText(nvl(trade.getSettlementAccount()));
            custodianField.setText(nvl(trade.getCustodian()));
            agentBankField.setText(nvl(trade.getAgentBank()));
            specialInstructionsField.setText(nvl(trade.getSpecialInstructions()));
            spreadField.setText(trade.getSpread() == null ? "" : trade.getSpread().toPlainString());
            benchmarkField.setText(nvl(trade.getBenchmark()));
            discountMarginField.setText(trade.getDiscountMargin() == null ? "" : trade.getDiscountMargin().toPlainString());
            accruedDaysSpinner.setValue(trade.getAccruedDays() == null ? 52 : trade.getAccruedDays());
            updateEconomics();
            setValidated(false, "Validation: Ready");
            clearInvalid();
            updateDynamicSections();
        } finally {
            applying = false;
        }
    }

    public void validateTrade() {
        context.setStatusMessage("Validating trade...");
        Trade trade = captureTrade();
        ValidationResult result = context.getTradeService().validate(trade);
        highlight(result);
        statusField.setText(trade.getStatus().displayName());
        if (result.isPassed()) {
            setValidated(true, result.getMessage());
            context.setStatusMessage("Trade validation passed.");
        } else {
            setValidated(false, result.getMessage());
            context.setStatusMessage(result.getMessage());
        }
        fireStatusChanged();
    }

    public void saveTrade() {
        if (!validated) {
            JOptionPane.showMessageDialog(this, "Validate the trade before saving.", "Save blocked",
                    JOptionPane.WARNING_MESSAGE);
            return;
        }
        context.setStatusMessage("Saving trade...");
        fireStatusChanged();
        try {
            Trade booked = context.getTradeService().book(captureTrade());
            loadTrade(booked, false);
            context.setStatusMessage("Trade " + booked.getTradeId() + " booked successfully.");
            context.notifyTradesChanged("booked:" + booked.getTradeId());
            fireStatusChanged();
            JFrame frame = (JFrame) SwingUtilities.getWindowAncestor(this);
            new TradeBookedDialog(frame, booked.getTradeId()).setVisible(true);
        } catch (RuntimeException ex) {
            setValidated(false, ex.getMessage());
            context.setStatusMessage(ex.getMessage());
            fireStatusChanged();
        }
    }

    public void applySecurity(Security security) {
        if (security == null) {
            return;
        }
        instrumentField.setText(security.getDescription());
        securityIdField.setText(security.getSecurityId());
        currencyCombo.setSelectedItem(security.getCurrency());
        settlementCurrencyCombo.setSelectedItem(security.getCurrency());
        invalidateValidation();
        updateEconomics();
    }

    public void applyCounterparty(Counterparty counterparty) {
        if (counterparty == null) {
            return;
        }
        counterpartyField.setText(counterparty.getCode());
        refreshSsiChoices();
        invalidateValidation();
    }

    public JComponent initialFocus() {
        return instrumentField;
    }

    private JPanel headerPanel() {
        JPanel panel = UiFactory.titled("Trade Header", "bond.header");
        FormGrid grid = new FormGrid(panel);
        grid.addPair(UiFactory.label("Trade ID", "label.bond.tradeId"), tradeIdField,
                UiFactory.label("Status", "label.bond.status"), statusField);
        grid.addPair(UiFactory.label("Trade Date", "label.bond.tradeDate"), tradeDateField,
                UiFactory.label("Trader", "label.bond.trader"), traderField);
        grid.addPair(UiFactory.label("Book", "label.bond.book"), bookCombo,
                UiFactory.label("Legal Entity", "label.bond.legalEntity"), legalEntityCombo);
        return panel;
    }

    private JPanel productPanel() {
        JPanel panel = UiFactory.titled("Product", "bond.product");
        FormGrid grid = new FormGrid(panel);
        JPanel instrumentRow = row(instrumentField, searchSecurityButton);
        grid.add(UiFactory.label("Instrument", "label.bond.instrument"), instrumentRow);
        grid.add(UiFactory.label("Security ID", "label.bond.securityId"), securityIdField);

        JPanel side = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 0));
        side.setOpaque(false);
        side.add(UiFactory.label("Side", "label.bond.side"));
        side.add(buyRadio);
        side.add(sellRadio);
        side.add(UiFactory.label("Lots", "label.bond.lots"));
        side.add(lotsSpinner);
        grid.addFull(side);

        JPanel notionalRow = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 0));
        notionalRow.setOpaque(false);
        notionalRow.add(notionalField);
        notionalRow.add(UiFactory.label("Trade Currency", "label.bond.currency"));
        notionalRow.add(currencyCombo);
        grid.add(UiFactory.label("Notional", "label.bond.notional"), notionalRow);

        JPanel priceRow = new JPanel(new FlowLayout(FlowLayout.LEFT, 6, 0));
        priceRow.setOpaque(false);
        priceRow.add(priceField);
        priceRow.add(UiFactory.label("Clean/Dirty", "label.bond.priceType"));
        priceRow.add(priceTypeCombo);
        grid.add(UiFactory.label("Price", "label.bond.price"), priceRow);
        grid.add(UiFactory.label("Yield", "label.bond.yield"), yieldField);
        return panel;
    }

    private JPanel counterpartyPanel() {
        JPanel panel = UiFactory.titled("Counterparty & Settlement", "bond.settlement");
        FormGrid grid = new FormGrid(panel);
        grid.add(UiFactory.label("Counterparty", "label.bond.counterparty"), row(counterpartyField, lookupCptyButton));
        grid.addPair(UiFactory.label("Settlement Dt", "label.bond.settlementDate"), settlementDateField,
                UiFactory.label("Settlement Location", "label.bond.settlementLocation"), settlementLocationCombo);
        grid.addPair(UiFactory.label("SSI", "label.bond.ssi"), ssiCombo,
                UiFactory.label("Settlement Currency", "label.bond.settlementCurrency"), settlementCurrencyCombo);
        JPanel checks = new JPanel(new FlowLayout(FlowLayout.LEFT, 12, 0));
        checks.setOpaque(false);
        checks.add(useStandardSsi);
        checks.add(manualOverride);
        checks.add(advancedPricing);
        grid.addFull(checks);
        grid.addFull(jpyBanner);
        return panel;
    }

    private JPanel manualSettlementPanel() {
        JPanel panel = UiFactory.titled("Manual Settlement", "bond.manualSettlement");
        FormGrid grid = new FormGrid(panel);
        grid.addPair(UiFactory.label("Settlement Account", "label.bond.settlementAccount"), settlementAccountField,
                UiFactory.label("Custodian", "label.bond.custodian"), custodianField);
        grid.addPair(UiFactory.label("Agent Bank", "label.bond.agentBank"), agentBankField,
                UiFactory.label("Special Instructions", "label.bond.specialInstructions"), specialInstructionsField);
        panel.setVisible(false);
        return panel;
    }

    private JPanel advancedPricingPanel() {
        JPanel panel = UiFactory.titled("Advanced Pricing", "bond.advancedPricing");
        FormGrid grid = new FormGrid(panel);
        grid.addPair(UiFactory.label("Spread", "label.bond.spread"), spreadField,
                UiFactory.label("Benchmark", "label.bond.benchmark"), benchmarkField);
        grid.addPair(UiFactory.label("Discount Margin", "label.bond.discountMargin"), discountMarginField,
                UiFactory.label("Accrued Days", "label.bond.accruedDays"), accruedDaysSpinner);
        panel.setVisible(false);
        return panel;
    }

    private JPanel economicsPanel() {
        JPanel panel = UiFactory.titled("Economics", "bond.economics");
        FormGrid grid = new FormGrid(panel);
        grid.add(UiFactory.label("Gross Amount", "label.bond.grossAmount"), grossField);
        grid.add(UiFactory.label("Accrued Interest", "label.bond.accruedInterest"), accruedField);
        grid.add(UiFactory.label("Net Settlement", "label.bond.netSettlement"), netField);
        return panel;
    }

    private JPanel workflowPanel() {
        JPanel panel = UiFactory.titled("Workflow", "bond.workflow");
        panel.setLayout(new BorderLayout());
        JPanel buttons = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 2));
        buttons.setOpaque(false);
        buttons.add(validateButton);
        buttons.add(saveButton);
        buttons.add(cancelButton);
        panel.add(buttons, BorderLayout.WEST);
        panel.add(validationStatus, BorderLayout.CENTER);
        return panel;
    }

    private void wireEvents() {
        DocumentListener invalidate = new SimpleDocumentListener(this::onFieldEdited);
        for (JTextField field : List.of(notionalField, priceField, counterpartyField, instrumentField,
                securityIdField, yieldField, settlementAccountField, custodianField, agentBankField,
                specialInstructionsField, spreadField, benchmarkField, discountMarginField)) {
            field.getDocument().addDocumentListener(invalidate);
        }
        tradeDateField.getDocument().addDocumentListener(invalidate);
        settlementDateField.getDocument().addDocumentListener(invalidate);

        buyRadio.addActionListener(e -> invalidateValidation());
        sellRadio.addActionListener(e -> invalidateValidation());
        currencyCombo.addActionListener(e -> {
            updateDynamicSections();
            invalidateValidation();
            updateEconomics();
        });
        settlementLocationCombo.addActionListener(e -> {
            refreshSsiChoices();
            invalidateValidation();
        });
        settlementCurrencyCombo.addActionListener(e -> invalidateValidation());
        bookCombo.addActionListener(e -> invalidateValidation());
        lotsSpinner.addChangeListener(e -> invalidateValidation());
        accruedDaysSpinner.addChangeListener(e -> {
            invalidateValidation();
            updateEconomics();
        });
        manualOverride.addActionListener(e -> {
            updateDynamicSections();
            invalidateValidation();
        });
        advancedPricing.addActionListener(e -> updateDynamicSections());
        useStandardSsi.addActionListener(e -> invalidateValidation());

        searchSecurityButton.addActionListener(e -> {
            JFrame frame = (JFrame) SwingUtilities.getWindowAncestor(this);
            applySecurity(SecuritySearchDialog.open(frame, context));
        });
        lookupCptyButton.addActionListener(e -> {
            JFrame frame = (JFrame) SwingUtilities.getWindowAncestor(this);
            applyCounterparty(CounterpartySearchDialog.open(frame, context));
        });
        validateButton.addActionListener(e -> validateTrade());
        saveButton.addActionListener(e -> saveTrade());
        cancelButton.addActionListener(e -> {
            int choice = JOptionPane.showConfirmDialog(this, "Discard the current ticket and start a new draft?",
                    "Cancel Trade", JOptionPane.YES_NO_OPTION);
            if (choice == JOptionPane.YES_OPTION) {
                loadNewTrade();
            }
        });
    }

    private void onFieldEdited() {
        if (applying) {
            return;
        }
        invalidateValidation();
        updateEconomics();
    }

    private void invalidateValidation() {
        if (applying) {
            return;
        }
        setValidated(false, "Validation: Ready");
        clearInvalid();
    }

    private void setValidated(boolean value, String message) {
        validated = value;
        saveButton.setEnabled(value);
        validationStatus.setText(message);
        validationStatus.setForeground(value ? Theme.SUCCESS : message.contains("FAILED") ? Theme.FAIL : new Color(0x334155));
    }

    private void updateDynamicSections() {
        manualPanel.setVisible(manualOverride.isSelected());
        advancedPanel.setVisible(advancedPricing.isSelected());
        jpyBanner.setVisible("JPY".equals(currencyCombo.getSelectedItem()));
        revalidate();
        repaint();
    }

    private void refreshSsiChoices() {
        String cpty = counterpartyField.getText();
        String ccy = String.valueOf(currencyCombo.getSelectedItem());
        String loc = String.valueOf(settlementLocationCombo.getSelectedItem());
        List<String> codes = context.getReferenceDataService().instructionsFor(cpty, ccy, loc)
                .stream().map(item -> item.getCode()).toList();
        Object previous = ssiCombo.getSelectedItem();
        ssiCombo.removeAllItems();
        Set<String> unique = new LinkedHashSet<>(codes);
        for (String code : unique) {
            ssiCombo.addItem(code);
        }
        if (previous != null) {
            ssiCombo.setSelectedItem(previous);
        }
    }

    private void updateEconomics() {
        Trade trade = captureTrade();
        Security security = context.getReferenceDataService().findSecurity(trade.getSecurityId()).orElse(null);
        EconomicsCalculator.apply(trade, security);
        grossField.setText(MoneyFormats.formatAmount(trade.getGrossAmount()));
        accruedField.setText(MoneyFormats.formatAmount(trade.getAccruedInterest()));
        netField.setText(MoneyFormats.formatAmount(trade.getNetSettlement()));
        if ((yieldField.getText() == null || yieldField.getText().isBlank()) && trade.getYield() != null) {
            yieldField.setText(trade.getYield().toPlainString());
        }
    }

    private Trade captureTrade() {
        Trade trade = new Trade();
        trade.setTradeId(blankToNew(tradeIdField.getText()));
        trade.setStatus(parseStatus(statusField.getText()));
        trade.setTrader(traderField.getText());
        trade.setBook(String.valueOf(bookCombo.getSelectedItem()));
        trade.setLegalEntity(String.valueOf(legalEntityCombo.getSelectedItem()));
        trade.setInstrument(instrumentField.getText());
        trade.setSecurityId(securityIdField.getText());
        trade.setSide(sellRadio.isSelected() ? TradeSide.SELL : TradeSide.BUY);
        trade.setNotional(parseDecimal(notionalField.getText()));
        trade.setCurrency(String.valueOf(currencyCombo.getSelectedItem()));
        trade.setPrice(parseDecimal(priceField.getText()));
        trade.setPriceType(String.valueOf(priceTypeCombo.getSelectedItem()));
        trade.setYield(parseDecimal(yieldField.getText()));
        trade.setLots((Integer) lotsSpinner.getValue());
        trade.setCounterparty(counterpartyField.getText() == null ? "" : counterpartyField.getText().trim().toUpperCase());
        trade.setTradeDate(parseDate(tradeDateField.getText()));
        trade.setSettlementDate(parseDate(settlementDateField.getText()));
        trade.setSettlementLocation(String.valueOf(settlementLocationCombo.getSelectedItem()));
        trade.setSettlementCurrency(String.valueOf(settlementCurrencyCombo.getSelectedItem()));
        trade.setSsi(String.valueOf(ssiCombo.getSelectedItem()));
        trade.setUseStandardSsi(useStandardSsi.isSelected());
        trade.setManualSettlementOverride(manualOverride.isSelected());
        trade.setSettlementAccount(settlementAccountField.getText());
        trade.setCustodian(custodianField.getText());
        trade.setAgentBank(agentBankField.getText());
        trade.setSpecialInstructions(specialInstructionsField.getText());
        trade.setSpread(parseDecimal(spreadField.getText()));
        trade.setBenchmark(benchmarkField.getText());
        trade.setDiscountMargin(parseDecimal(discountMarginField.getText()));
        trade.setAccruedDays((Integer) accruedDaysSpinner.getValue());
        return trade;
    }

    private void highlight(ValidationResult result) {
        clearInvalid();
        for (String field : result.getInvalidFields()) {
            switch (field) {
                case "notional" -> UiFactory.markInvalid(notionalField, true);
                case "currency" -> currencyCombo.setBorder(Theme.invalidBorder());
                case "counterparty" -> UiFactory.markInvalid(counterpartyField, true);
                case "price" -> UiFactory.markInvalid(priceField, true);
                case "settlementLocation" -> settlementLocationCombo.setBorder(Theme.invalidBorder());
                case "tradeDate" -> UiFactory.markInvalid(tradeDateField, true);
                case "settlementDate" -> UiFactory.markInvalid(settlementDateField, true);
                default -> {
                }
            }
        }
    }

    private void clearInvalid() {
        UiFactory.markInvalid(notionalField, false);
        UiFactory.markInvalid(counterpartyField, false);
        UiFactory.markInvalid(priceField, false);
        UiFactory.markInvalid(tradeDateField, false);
        UiFactory.markInvalid(settlementDateField, false);
        currencyCombo.setBorder(javax.swing.BorderFactory.createEmptyBorder());
        settlementLocationCombo.setBorder(javax.swing.BorderFactory.createEmptyBorder());
    }

    private void fireStatusChanged() {
        context.notifyTradesChanged("status");
    }

    private static JPanel row(JComponent left, JComponent right) {
        JPanel panel = new JPanel(new FlowLayout(FlowLayout.LEFT, 4, 0));
        panel.setOpaque(false);
        panel.add(left);
        panel.add(right);
        return panel;
    }

    private static String nvl(String value) {
        return value == null ? "" : value;
    }

    private static String blankToNew(String value) {
        return value == null || value.isBlank() ? "NEW" : value;
    }

    private static TradeStatus parseStatus(String value) {
        for (TradeStatus status : TradeStatus.values()) {
            if (status.displayName().equalsIgnoreCase(value) || status.name().equalsIgnoreCase(value)) {
                return status;
            }
        }
        return TradeStatus.DRAFT;
    }

    private static BigDecimal parseDecimal(String text) {
        try {
            return MoneyFormats.parseNumber(text);
        } catch (Exception ex) {
            return null;
        }
    }

    private static JFormattedTextField dateField(String name) {
        SimpleDateFormat format = new SimpleDateFormat("MM/dd/yyyy");
        format.setLenient(false);
        return UiFactory.formatted(name, format, 10);
    }

    private static java.time.LocalDate parseDate(String text) {
        try {
            return MoneyFormats.parseDate(text);
        } catch (DateTimeParseException ex) {
            return null;
        }
    }

    private record SimpleDocumentListener(Runnable action) implements DocumentListener {
        @Override
        public void insertUpdate(DocumentEvent e) {
            action.run();
        }

        @Override
        public void removeUpdate(DocumentEvent e) {
            action.run();
        }

        @Override
        public void changedUpdate(DocumentEvent e) {
            action.run();
        }
    }
}
