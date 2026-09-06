package com.northstar.capital;

import com.northstar.capital.service.ValidationService;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class NotionalValidationTest {
    private final ValidationService service = new ValidationService();

    @Test
    void acceptsPositiveNotionalWithinLimit() {
        assertTrue(service.isValidNotional(new BigDecimal("100000000")));
    }

    @Test
    void rejectsZeroNegativeAndExcessNotional() {
        assertFalse(service.isValidNotional(BigDecimal.ZERO));
        assertFalse(service.isValidNotional(new BigDecimal("-1")));
        assertFalse(service.isValidNotional(new BigDecimal("5000000001")));
        assertFalse(service.isValidNotional(null));
    }
}
