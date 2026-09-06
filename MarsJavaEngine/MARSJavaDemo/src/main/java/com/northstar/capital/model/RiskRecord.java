package com.northstar.capital.model;

import java.math.BigDecimal;

public class RiskRecord {
    private String book;
    private BigDecimal dv01;
    private BigDecimal cs01;
    private BigDecimal var;
    private BigDecimal limitUsage;
    private BigDecimal dailyPnl;
    private String currency;

    public RiskRecord() {
    }

    public RiskRecord(String book, BigDecimal dv01, BigDecimal cs01, BigDecimal var,
                      BigDecimal limitUsage, BigDecimal dailyPnl, String currency) {
        this.book = book;
        this.dv01 = dv01;
        this.cs01 = cs01;
        this.var = var;
        this.limitUsage = limitUsage;
        this.dailyPnl = dailyPnl;
        this.currency = currency;
    }

    public String getBook() {
        return book;
    }

    public void setBook(String book) {
        this.book = book;
    }

    public BigDecimal getDv01() {
        return dv01;
    }

    public void setDv01(BigDecimal dv01) {
        this.dv01 = dv01;
    }

    public BigDecimal getCs01() {
        return cs01;
    }

    public void setCs01(BigDecimal cs01) {
        this.cs01 = cs01;
    }

    public BigDecimal getVar() {
        return var;
    }

    public void setVar(BigDecimal var) {
        this.var = var;
    }

    public BigDecimal getLimitUsage() {
        return limitUsage;
    }

    public void setLimitUsage(BigDecimal limitUsage) {
        this.limitUsage = limitUsage;
    }

    public BigDecimal getDailyPnl() {
        return dailyPnl;
    }

    public void setDailyPnl(BigDecimal dailyPnl) {
        this.dailyPnl = dailyPnl;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }
}
