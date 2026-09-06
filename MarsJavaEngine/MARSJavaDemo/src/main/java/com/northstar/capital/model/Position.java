package com.northstar.capital.model;

import java.math.BigDecimal;

public class Position {
    private String book;
    private String security;
    private String product;
    private BigDecimal positionQty;
    private BigDecimal averagePrice;
    private BigDecimal marketPrice;
    private BigDecimal marketValue;
    private BigDecimal dailyPnl;
    private String currency;
    private BigDecimal dv01;

    public Position() {
    }

    public Position(String book, String security, String product, BigDecimal positionQty, BigDecimal averagePrice,
                    BigDecimal marketPrice, BigDecimal marketValue, BigDecimal dailyPnl, String currency, BigDecimal dv01) {
        this.book = book;
        this.security = security;
        this.product = product;
        this.positionQty = positionQty;
        this.averagePrice = averagePrice;
        this.marketPrice = marketPrice;
        this.marketValue = marketValue;
        this.dailyPnl = dailyPnl;
        this.currency = currency;
        this.dv01 = dv01;
    }

    public String getBook() {
        return book;
    }

    public void setBook(String book) {
        this.book = book;
    }

    public String getSecurity() {
        return security;
    }

    public void setSecurity(String security) {
        this.security = security;
    }

    public String getProduct() {
        return product;
    }

    public void setProduct(String product) {
        this.product = product;
    }

    public BigDecimal getPositionQty() {
        return positionQty;
    }

    public void setPositionQty(BigDecimal positionQty) {
        this.positionQty = positionQty;
    }

    public BigDecimal getAveragePrice() {
        return averagePrice;
    }

    public void setAveragePrice(BigDecimal averagePrice) {
        this.averagePrice = averagePrice;
    }

    public BigDecimal getMarketPrice() {
        return marketPrice;
    }

    public void setMarketPrice(BigDecimal marketPrice) {
        this.marketPrice = marketPrice;
    }

    public BigDecimal getMarketValue() {
        return marketValue;
    }

    public void setMarketValue(BigDecimal marketValue) {
        this.marketValue = marketValue;
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

    public BigDecimal getDv01() {
        return dv01;
    }

    public void setDv01(BigDecimal dv01) {
        this.dv01 = dv01;
    }
}
