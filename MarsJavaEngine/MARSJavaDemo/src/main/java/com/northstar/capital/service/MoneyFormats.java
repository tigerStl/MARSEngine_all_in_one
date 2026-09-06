package com.northstar.capital.service;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.format.DateTimeFormatter;
import java.util.Locale;

public final class MoneyFormats {
    public static final DateTimeFormatter DATE = DateTimeFormatter.ofPattern("MM/dd/yyyy");
    public static final DateTimeFormatter TIME = DateTimeFormatter.ofPattern("HH:mm:ss");
    public static final DateTimeFormatter TRADE_ID_DATE = DateTimeFormatter.ofPattern("yyyyMMdd");

    private MoneyFormats() {
    }

    public static DecimalFormat amountFormat() {
        DecimalFormatSymbols symbols = DecimalFormatSymbols.getInstance(Locale.US);
        DecimalFormat format = new DecimalFormat("#,##0.00", symbols);
        format.setParseBigDecimal(true);
        return format;
    }

    public static DecimalFormat priceFormat() {
        DecimalFormatSymbols symbols = DecimalFormatSymbols.getInstance(Locale.US);
        DecimalFormat format = new DecimalFormat("0.0000", symbols);
        format.setParseBigDecimal(true);
        return format;
    }

    public static String formatAmount(BigDecimal value) {
        if (value == null) {
            return "";
        }
        return amountFormat().format(value);
    }

    public static String formatPrice(BigDecimal value) {
        if (value == null) {
            return "";
        }
        return priceFormat().format(value);
    }

    public static String formatDate(LocalDate value) {
        return value == null ? "" : DATE.format(value);
    }

    public static String formatTime(LocalDateTime value) {
        return value == null ? "" : TIME.format(value.toLocalTime());
    }

    public static String formatTime(LocalTime value) {
        return value == null ? "" : TIME.format(value);
    }

    public static LocalDate parseDate(String text) {
        if (text == null || text.isBlank()) {
            return null;
        }
        return LocalDate.parse(text.trim(), DATE);
    }

    public static BigDecimal parseNumber(String text) {
        if (text == null || text.isBlank()) {
            return null;
        }
        String cleaned = text.trim().replace(",", "");
        return new BigDecimal(cleaned);
    }

    public static BigDecimal scale(BigDecimal value, int scale) {
        if (value == null) {
            return null;
        }
        return value.setScale(scale, RoundingMode.HALF_UP);
    }
}
