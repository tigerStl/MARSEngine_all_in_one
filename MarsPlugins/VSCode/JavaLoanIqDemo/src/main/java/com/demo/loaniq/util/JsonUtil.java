/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.util;

import com.demo.loaniq.model.Deal;
import com.demo.loaniq.model.Facility;
import com.demo.loaniq.model.Loan;
import com.demo.loaniq.model.Payment;

public final class JsonUtil {
    private JsonUtil() {
    }

    private static String esc(String s) {
        if (s == null) {
            return "null";
        }
        StringBuilder sb = new StringBuilder("\"");
        for (int i = 0; i < s.length(); ++i) {
            char c = s.charAt(i);
            if (c == '\"') {
                sb.append("\\\"");
                continue;
            }
            if (c == '\\') {
                sb.append("\\\\");
                continue;
            }
            if (c == '\n') {
                sb.append("\\n");
                continue;
            }
            if (c == '\r') {
                sb.append("\\r");
                continue;
            }
            if (c == '\t') {
                sb.append("\\t");
                continue;
            }
            sb.append(c);
        }
        return sb.append("\"").toString();
    }

    public static String toJson(Deal d) {
        if (d == null) {
            return "{}";
        }
        return "{\n  \"dealName\":" + JsonUtil.esc(d.getDealName()) + ",\n  \"dealType\":" + JsonUtil.esc(d.getDealType()) + ",\n  \"borrower\":" + JsonUtil.esc(d.getBorrower()) + ",\n  \"currency\":" + JsonUtil.esc(d.getCurrency()) + ",\n  \"country\":" + JsonUtil.esc(d.getCountry()) + ",\n  \"status\":" + JsonUtil.esc(d.getStatus()) + ",\n  \"agreementDate\":" + JsonUtil.esc(d.getAgreementDate()) + ",\n  \"effectiveDate\":" + JsonUtil.esc(d.getEffectiveDate()) + ",\n  \"maturityDate\":" + JsonUtil.esc(d.getMaturityDate()) + ",\n  \"amount\":" + d.getAmount() + "\n}";
    }

    public static String toJson(Facility f) {
        if (f == null) {
            return "{}";
        }
        return "{\n  \"facilityName\":" + JsonUtil.esc(f.getFacilityName()) + ",\n  \"facilityType\":" + JsonUtil.esc(f.getFacilityType()) + ",\n  \"currency\":" + JsonUtil.esc(f.getCurrency()) + ",\n  \"commitment\":" + f.getCommitment() + ",\n  \"pricingOption\":" + JsonUtil.esc(f.getPricingOption()) + ",\n  \"dayCount\":" + JsonUtil.esc(f.getDayCount()) + ",\n  \"status\":" + JsonUtil.esc(f.getStatus()) + "\n}";
    }

    public static String toJson(Loan l) {
        if (l == null) {
            return "{}";
        }
        return "{\n  \"alias\":" + JsonUtil.esc(l.getAlias()) + ",\n  \"currency\":" + JsonUtil.esc(l.getCurrency()) + ",\n  \"principal\":" + l.getPrincipal() + ",\n  \"rateType\":" + JsonUtil.esc(l.getRateType()) + ",\n  \"index\":" + JsonUtil.esc(l.getIndex()) + ",\n  \"spread\":" + l.getSpread() + ",\n  \"allInRate\":" + l.getAllInRate() + ",\n  \"startDate\":" + JsonUtil.esc(l.getStartDate()) + ",\n  \"endDate\":" + JsonUtil.esc(l.getEndDate()) + ",\n  \"status\":" + JsonUtil.esc(l.getStatus()) + "\n}";
    }

    public static String toJson(Payment p) {
        if (p == null) {
            return "{}";
        }
        return "{\n  \"paymentType\":" + JsonUtil.esc(p.getPaymentType()) + ",\n  \"amount\":" + p.getAmount() + ",\n  \"valueDate\":" + JsonUtil.esc(p.getValueDate()) + ",\n  \"status\":" + JsonUtil.esc(p.getStatus()) + "\n}";
    }
}

