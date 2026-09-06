package com.northstar.capital.service;

import java.time.DayOfWeek;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;

/**
 * Fixed business date so demo data and booked trade IDs stay reproducible.
 */
public final class DemoClock {
    public static final LocalDate BUSINESS_DATE = LocalDate.of(2026, 9, 5);

    private DemoClock() {
    }

    public static LocalDate today() {
        return BUSINESS_DATE;
    }

    public static LocalDateTime now() {
        return LocalDateTime.of(BUSINESS_DATE, LocalTime.of(14, 32, 19));
    }

    public static LocalDate defaultSettlementDate(LocalDate tradeDate) {
        LocalDate start = tradeDate == null ? today() : tradeDate;
        while (isWeekend(start)) {
            start = start.plusDays(1);
        }
        return nextBusinessDay(start);
    }

    public static LocalDate nextBusinessDay(LocalDate date) {
        LocalDate next = date.plusDays(1);
        while (isWeekend(next)) {
            next = next.plusDays(1);
        }
        return next;
    }

    public static boolean isWeekend(LocalDate date) {
        DayOfWeek day = date.getDayOfWeek();
        return day == DayOfWeek.SATURDAY || day == DayOfWeek.SUNDAY;
    }
}
