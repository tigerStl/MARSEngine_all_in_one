package com.northstar.capital.model;

import java.math.BigDecimal;
import java.time.LocalDate;

public class Security {
    private String securityId;
    private String issuer;
    private String description;
    private BigDecimal coupon;
    private LocalDate maturity;
    private String currency;
    private String type;

    public Security() {
    }

    public Security(String securityId, String issuer, String description, BigDecimal coupon,
                    LocalDate maturity, String currency, String type) {
        this.securityId = securityId;
        this.issuer = issuer;
        this.description = description;
        this.coupon = coupon;
        this.maturity = maturity;
        this.currency = currency;
        this.type = type;
    }

    public String getSecurityId() {
        return securityId;
    }

    public void setSecurityId(String securityId) {
        this.securityId = securityId;
    }

    public String getIssuer() {
        return issuer;
    }

    public void setIssuer(String issuer) {
        this.issuer = issuer;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public BigDecimal getCoupon() {
        return coupon;
    }

    public void setCoupon(BigDecimal coupon) {
        this.coupon = coupon;
    }

    public LocalDate getMaturity() {
        return maturity;
    }

    public void setMaturity(LocalDate maturity) {
        this.maturity = maturity;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    @Override
    public String toString() {
        return description;
    }
}
