package com.northstar.capital.service;

import com.northstar.capital.model.Trade;
import com.northstar.capital.model.ValidationResult;

import java.math.BigDecimal;
import java.util.Set;

public class ValidationService {
    public static final BigDecimal MAX_NOTIONAL = new BigDecimal("5000000000");
    public static final Set<String> CURRENCIES = Set.of("USD", "EUR", "GBP", "JPY", "CAD", "CHF");
    public static final Set<String> COUNTERPARTIES =
            Set.of("FHLB", "JPM", "CITI", "GS", "BAC", "MS", "BARC", "DB", "UBS");
    public static final Set<String> SETTLEMENT_LOCATIONS = Set.of("NYC", "LON", "TKY", "TOR", "ZRH");

    public ValidationResult validate(Trade trade) {
        if (trade.getNotional() == null) {
            return ValidationResult.failed("Notional is required.", "notional");
        }
        if (trade.getNotional().compareTo(BigDecimal.ZERO) <= 0) {
            return ValidationResult.failed("Notional must be greater than 0.", "notional");
        }
        if (trade.getNotional().compareTo(MAX_NOTIONAL) > 0) {
            return ValidationResult.failed("Notional must be less than or equal to 5,000,000,000.", "notional");
        }

        if (trade.getCurrency() == null || trade.getCurrency().isBlank()) {
            return ValidationResult.failed("Currency is required.", "currency");
        }
        if (!CURRENCIES.contains(trade.getCurrency())) {
            return ValidationResult.failed("Currency is not supported.", "currency");
        }

        if (trade.getCounterparty() == null || trade.getCounterparty().isBlank()) {
            return ValidationResult.failed("Counterparty is required.", "counterparty");
        }
        if (!COUNTERPARTIES.contains(trade.getCounterparty().trim().toUpperCase())) {
            return ValidationResult.failed("Counterparty is not recognized.", "counterparty");
        }

        if (trade.getPrice() == null) {
            return ValidationResult.failed("Price is required.", "price");
        }
        if (trade.getPrice().compareTo(BigDecimal.ZERO) <= 0 || trade.getPrice().compareTo(new BigDecimal("200")) >= 0) {
            return ValidationResult.failed("Price must be less than 200.", "price");
        }

        if (trade.getSettlementLocation() == null || trade.getSettlementLocation().isBlank()) {
            return ValidationResult.failed("Settlement Location is required.", "settlementLocation");
        }
        if (!SETTLEMENT_LOCATIONS.contains(trade.getSettlementLocation())) {
            return ValidationResult.failed("Settlement Location is not supported.", "settlementLocation");
        }

        if (trade.getTradeDate() == null) {
            return ValidationResult.failed("Trade Date is required.", "tradeDate");
        }
        if (trade.getSettlementDate() == null) {
            return ValidationResult.failed("Settlement Date is required.", "settlementDate");
        }
        if (trade.getSettlementDate().isBefore(trade.getTradeDate())) {
            return ValidationResult.failed("Settlement Date must be on or after Trade Date.", "settlementDate");
        }

        return ValidationResult.passed();
    }

    public boolean isValidNotional(BigDecimal notional) {
        return notional != null
                && notional.compareTo(BigDecimal.ZERO) > 0
                && notional.compareTo(MAX_NOTIONAL) <= 0;
    }

    public boolean isValidPrice(BigDecimal price) {
        return price != null
                && price.compareTo(BigDecimal.ZERO) > 0
                && price.compareTo(new BigDecimal("200")) < 0;
    }
}
