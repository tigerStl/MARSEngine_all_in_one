package com.northstar.capital;

import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeStatus;
import com.northstar.capital.model.ValidationResult;
import com.northstar.capital.service.ApplicationContext;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

class TradeBookingServiceTest {
    private final ApplicationContext context = new ApplicationContext();

    @Test
    void booksValidatedTradeAndAssignsId() {
        int before = context.getTradeService().getTrades().size();
        Trade trade = context.getTradeService().newDraft();
        trade.setNotional(new BigDecimal("100000000"));
        trade.setCurrency("USD");
        trade.setCounterparty("FHLB");
        trade.setPrice(new BigDecimal("98.25"));
        trade.setSettlementLocation("NYC");

        ValidationResult validation = context.getTradeService().validate(trade);
        assertTrue(validation.isPassed());

        Trade booked = context.getTradeService().book(trade);
        assertEquals(TradeStatus.BOOKED, booked.getStatus());
        assertTrue(booked.getTradeId().startsWith("FI-"));
        assertEquals(before + 1, context.getTradeService().getTrades().size());
        assertTrue(context.getTradeService().findById(booked.getTradeId()).isPresent());
    }

    @Test
    void rejectsInvalidPriceBeforeBooking() {
        Trade trade = context.getTradeService().newDraft();
        trade.setNotional(new BigDecimal("100000000"));
        trade.setCurrency("USD");
        trade.setCounterparty("FHLB");
        trade.setPrice(new BigDecimal("250"));
        trade.setSettlementLocation("NYC");

        ValidationResult validation = context.getTradeService().validate(trade);
        assertFalse(validation.isPassed());
        assertTrue(validation.getMessage().contains("Price must be less than 200"));
        assertThrows(IllegalStateException.class, () -> context.getTradeService().book(trade));
    }
}
