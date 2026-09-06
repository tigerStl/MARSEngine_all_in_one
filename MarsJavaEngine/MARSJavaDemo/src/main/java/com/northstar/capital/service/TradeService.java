package com.northstar.capital.service;

import com.northstar.capital.model.AuditEvent;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.model.ValidationResult;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicInteger;

public class TradeService {
    private final DemoRepository repository;
    private final ValidationService validationService;
    private final ReferenceDataService referenceDataService;
    private final AtomicInteger sequence;

    public TradeService(DemoRepository repository, ValidationService validationService,
                        ReferenceDataService referenceDataService) {
        this.repository = repository;
        this.validationService = validationService;
        this.referenceDataService = referenceDataService;
        this.sequence = new AtomicInteger(nextSequence(repository.getTrades()));
    }

    public List<Trade> getTrades() {
        return repository.getTrades();
    }

    public Optional<Trade> findById(String tradeId) {
        return repository.getTrades().stream()
                .filter(trade -> trade.getTradeId().equals(tradeId))
                .findFirst();
    }

    public Trade newDraft() {
        Trade trade = new Trade();
        trade.setTradeId("NEW");
        trade.setStatus(TradeStatus.DRAFT);
        trade.setTrader("TRADER01");
        trade.setTradeDate(DemoClock.today());
        trade.setSettlementDate(DemoClock.defaultSettlementDate(DemoClock.today()));
        Security defaultSecurity = referenceDataService.findSecurity("US91282ABC12")
                .orElse(referenceDataService.getSecurities().get(0));
        trade.setInstrument(defaultSecurity.getDescription());
        trade.setSecurityId(defaultSecurity.getSecurityId());
        trade.setCurrency(defaultSecurity.getCurrency());
        trade.setSettlementCurrency(defaultSecurity.getCurrency());
        trade.setSsi("FHLB_USD_NYC_01");
        trade.setAccruedDays(52);
        EconomicsCalculator.apply(trade, defaultSecurity);
        return trade;
    }

    public ValidationResult validate(Trade trade) {
        AppLog.trade("Validate requested");
        Security security = referenceDataService.findSecurity(trade.getSecurityId()).orElse(null);
        EconomicsCalculator.apply(trade, security);
        ValidationResult result = validationService.validate(trade);
        if (result.isPassed()) {
            trade.setStatus(trade.getStatus() == TradeStatus.BOOKED ? TradeStatus.BOOKED : TradeStatus.VALIDATED);
            addAudit(trade, "VALIDATE", "Status", "Draft", "Validated");
            AppLog.trade("Validation passed");
        } else {
            AppLog.trade("Validation failed: " + result.getMessage());
        }
        return result;
    }

    public Trade book(Trade draft) {
        ValidationResult result = validate(draft);
        if (!result.isPassed()) {
            throw new IllegalStateException(result.getMessage());
        }
        boolean existing = draft.getTradeId() != null && !draft.getTradeId().equals("NEW")
                && findById(draft.getTradeId()).isPresent();
        if (!existing) {
            String tradeId = nextTradeId(draft.getTradeDate());
            draft.setTradeId(tradeId);
            addAudit(draft, "CREATE", "Trade", "-", tradeId);
            repository.getTrades().add(0, draft);
            AppLog.blotter("Trade added");
        } else {
            addAudit(draft, "AMEND", "Status", draft.getStatus().displayName(), TradeStatus.AMENDED.displayName());
            replace(draft);
        }
        TradeStatus previous = draft.getStatus();
        draft.setStatus(TradeStatus.BOOKED);
        draft.setLastUpdated(DemoClock.now().withSecond(Math.min(59, sequence.get() % 60)));
        addAudit(draft, "SAVE", "Status", previous.displayName(), TradeStatus.BOOKED.displayName());
        AppLog.trade("Saved " + draft.getTradeId());
        return draft;
    }

    public Trade cloneTrade(Trade source) {
        Trade copy = source.copy();
        copy.setTradeId("NEW");
        copy.setStatus(TradeStatus.DRAFT);
        copy.getAuditTrail().clear();
        copy.setLastUpdated(null);
        return copy;
    }

    public void cancel(Trade trade) {
        TradeStatus previous = trade.getStatus();
        trade.setStatus(TradeStatus.CANCELLED);
        trade.setLastUpdated(DemoClock.now());
        addAudit(trade, "CANCEL", "Status", previous.displayName(), TradeStatus.CANCELLED.displayName());
        replace(trade);
    }

    public List<Trade> search(String query) {
        if (query == null || query.isBlank()) {
            return new ArrayList<>(repository.getTrades());
        }
        String needle = query.toLowerCase(Locale.ROOT);
        List<Trade> matches = new ArrayList<>();
        for (Trade trade : repository.getTrades()) {
            String haystack = String.join(" ",
                    safe(trade.getTradeId()),
                    trade.getStatus() == null ? "" : trade.getStatus().displayName(),
                    safe(trade.getInstrument()),
                    safe(trade.getSecurityId()),
                    safe(trade.getCounterparty()),
                    safe(trade.getBook()),
                    safe(trade.getTrader()));
            if (haystack.toLowerCase(Locale.ROOT).contains(needle)) {
                matches.add(trade);
            }
        }
        return matches;
    }

    public List<Trade> latestByCounterparty(String counterparty) {
        return repository.getTrades().stream()
                .filter(trade -> counterparty.equalsIgnoreCase(trade.getCounterparty()))
                .sorted(Comparator.comparing(Trade::getTradeId, Comparator.nullsLast(Comparator.reverseOrder())))
                .toList();
    }

    private void replace(Trade updated) {
        List<Trade> trades = repository.getTrades();
        for (int i = 0; i < trades.size(); i++) {
            if (trades.get(i).getTradeId().equals(updated.getTradeId())) {
                trades.set(i, updated);
                return;
            }
        }
        trades.add(0, updated);
    }

    private void addAudit(Trade trade, String action, String field, String oldValue, String newValue) {
        trade.getAuditTrail().add(new AuditEvent(DemoClock.now(), trade.getTrader(), action, field, oldValue, newValue));
    }

    public String nextTradeId(LocalDate tradeDate) {
        LocalDate date = tradeDate == null ? DemoClock.today() : tradeDate;
        int next = sequence.incrementAndGet();
        return "FI-" + date.format(MoneyFormats.TRADE_ID_DATE) + "-" + String.format("%06d", next);
    }

    private static int nextSequence(List<Trade> trades) {
        int max = 0;
        for (Trade trade : trades) {
            String id = trade.getTradeId();
            if (id != null && id.contains("-")) {
                String suffix = id.substring(id.lastIndexOf('-') + 1);
                try {
                    max = Math.max(max, Integer.parseInt(suffix));
                } catch (NumberFormatException ignored) {
                    // seed IDs are expected to be well-formed
                }
            }
        }
        return max;
    }

    private static String safe(String value) {
        return value == null ? "" : value;
    }
}
