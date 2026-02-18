/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.util;

import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.util.Locale;

public final class FormatUtil {
    private static final DecimalFormat NUM_FORMAT;

    private FormatUtil() {
    }

    public static String formatNumber(double v) {
        return NUM_FORMAT.format(v);
    }

    public static double parseNumber(String s) {
        if (s == null || s.isBlank()) {
            return 0.0;
        }
        try {
            return NUM_FORMAT.parse(s.trim().replace(" ", "").replace(",", "")).doubleValue();
        }
        catch (Exception e) {
            try {
                return Double.parseDouble(s.trim().replace(",", ""));
            }
            catch (NumberFormatException e2) {
                return 0.0;
            }
        }
    }

    static {
        DecimalFormatSymbols sym = new DecimalFormatSymbols(Locale.US);
        sym.setGroupingSeparator(',');
        NUM_FORMAT = new DecimalFormat("#,##0.00", sym);
        NUM_FORMAT.setGroupingUsed(true);
    }
}

