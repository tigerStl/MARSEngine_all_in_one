/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.model;

public class Loan {
    private String alias;
    private String currency;
    private double principal;
    private String rateType;
    private String index;
    private double spread;
    private double allInRate;
    private String startDate;
    private String endDate;
    private String status;

    public String getAlias() {
        return this.alias;
    }

    public void setAlias(String alias) {
        this.alias = alias;
    }

    public String getCurrency() {
        return this.currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public double getPrincipal() {
        return this.principal;
    }

    public void setPrincipal(double principal) {
        this.principal = principal;
    }

    public String getRateType() {
        return this.rateType;
    }

    public void setRateType(String rateType) {
        this.rateType = rateType;
    }

    public String getIndex() {
        return this.index;
    }

    public void setIndex(String index) {
        this.index = index;
    }

    public double getSpread() {
        return this.spread;
    }

    public void setSpread(double spread) {
        this.spread = spread;
    }

    public double getAllInRate() {
        return this.allInRate;
    }

    public void setAllInRate(double allInRate) {
        this.allInRate = allInRate;
    }

    public String getStartDate() {
        return this.startDate;
    }

    public void setStartDate(String startDate) {
        this.startDate = startDate;
    }

    public String getEndDate() {
        return this.endDate;
    }

    public void setEndDate(String endDate) {
        this.endDate = endDate;
    }

    public String getStatus() {
        return this.status;
    }

    public void setStatus(String status) {
        this.status = status;
    }
}

