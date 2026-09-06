package com.northstar.capital;

import com.northstar.capital.model.Trade;
import com.northstar.capital.model.ValidationResult;
import com.northstar.capital.service.DemoClock;
import com.northstar.capital.service.ValidationService;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class SettlementDateValidationTest {
    private final ValidationService service = new ValidationService();

    @Test
    void settlementOnOrAfterTradeDatePasses() {
        Trade trade = validTrade();
        trade.setSettlementDate(trade.getTradeDate());
        assertTrue(service.validate(trade).isPassed());
    }

    @Test
    void settlementBeforeTradeDateFails() {
        Trade trade = validTrade();
        trade.setSettlementDate(trade.getTradeDate().minusDays(1));
        ValidationResult result = service.validate(trade);
        assertFalse(result.isPassed());
        assertTrue(result.getMessage().contains("Settlement Date"));
    }

    private static Trade validTrade() {
        Trade trade = new Trade();
        trade.setNotional(new BigDecimal("100000000"));
        trade.setCurrency("USD");
        trade.setCounterparty("FHLB");
        trade.setPrice(new BigDecimal("98.25"));
        trade.setSettlementLocation("NYC");
        trade.setTradeDate(DemoClock.today());
        trade.setSettlementDate(DemoClock.defaultSettlementDate(DemoClock.today()));
        return trade;
    }
}
