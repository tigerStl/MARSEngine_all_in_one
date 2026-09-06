package com.northstar.capital.service;

import com.northstar.capital.model.Security;
import com.northstar.capital.model.Trade;

import java.math.BigDecimal;
import java.math.RoundingMode;

public final class EconomicsCalculator {
    private EconomicsCalculator() {
    }

    public static void apply(Trade trade, Security security) {
        if (trade.getNotional() == null || trade.getPrice() == null) {
            trade.setGrossAmount(null);
            trade.setAccruedInterest(null);
            trade.setNetSettlement(null);
            return;
        }
        BigDecimal gross = trade.getNotional()
                .multiply(trade.getPrice())
                .divide(new BigDecimal("100"), 2, RoundingMode.HALF_UP);
        int days = trade.getAccruedDays() != null ? trade.getAccruedDays() : 52;
        BigDecimal coupon = security != null && security.getCoupon() != null
                ? security.getCoupon()
                : new BigDecimal("5.00");
        BigDecimal accrued = trade.getNotional()
                .multiply(coupon)
                .multiply(BigDecimal.valueOf(days))
                .divide(new BigDecimal("36000"), 2, RoundingMode.HALF_UP);
        trade.setGrossAmount(gross);
        trade.setAccruedInterest(accrued);
        trade.setNetSettlement(gross.add(accrued));
        if (trade.getYield() == null) {
            trade.setYield(impliedYield(trade.getPrice(), coupon));
        }
    }

    public static BigDecimal impliedYield(BigDecimal price, BigDecimal coupon) {
        if (price == null || coupon == null || price.compareTo(BigDecimal.ZERO) == 0) {
            return null;
        }
        return coupon.multiply(new BigDecimal("100"))
                .divide(price, 3, RoundingMode.HALF_UP);
    }
}
