/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.data;

import java.util.Arrays;
import java.util.List;

public final class ReferenceData {
    public static final List<String> ENVIRONMENTS = Arrays.asList("DEV", "UAT", "PROD_SIM");
    public static final List<String> USERS = Arrays.asList("tiger.liu", "demo.user");
    public static final List<String> CURRENCIES = Arrays.asList("USD", "EUR", "GBP", "JPY", "CHF", "CAD");
    public static final List<String> DAY_COUNT = Arrays.asList("30/360", "ACT/360", "ACT/365");
    public static final List<String> RATE_INDEX = Arrays.asList("SOFR", "LIBOR", "PRIME", "FIXED");
    public static final List<String> PAYMENT_FREQ = Arrays.asList("1M", "3M", "6M", "12M");
    public static final List<String> STATUS = Arrays.asList("Draft", "Pending", "Approved", "Released", "Closed");
    public static final List<String> DEAL_TYPE = Arrays.asList("Bilateral", "Syndicated");
    public static final List<String> FACILITY_TYPE = Arrays.asList("Revolver", "TermLoanA", "TermLoanB");
    public static final List<String> BORROWERS = Arrays.asList("ACME_CORP", "CUSTOMER1", "CUSTOMER2");
    public static final List<String> LENDERS = Arrays.asList("BIG_BANK_NA", "CCP_CLEARING", "NORTH_TRUST", "CITI");
    public static final List<String> COUNTRY = Arrays.asList("US", "UK", "CN", "HK");
    public static final List<String> CALENDARS = Arrays.asList("NYC", "LON", "TARGET");

    private ReferenceData() {
    }
}

