package com.northstar.capital;

import com.northstar.capital.service.ValidationService;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class PriceValidationTest {
    private final ValidationService service = new ValidationService();

    @Test
    void acceptsMarketPrice() {
        assertTrue(service.isValidPrice(new BigDecimal("98.25")));
    }

    @Test
    void rejectsPriceAtOrAbove200() {
        assertFalse(service.isValidPrice(new BigDecimal("200")));
        assertFalse(service.isValidPrice(new BigDecimal("250")));
        assertFalse(service.isValidPrice(BigDecimal.ZERO));
        assertFalse(service.isValidPrice(null));
    }
}
