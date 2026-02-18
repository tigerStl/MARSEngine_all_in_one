/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.model;

public class Facility {
    private String facilityName;
    private String facilityType;
    private String currency;
    private double commitment;
    private String pricingOption;
    private String dayCount;
    private String status;

    public String getFacilityName() {
        return this.facilityName;
    }

    public void setFacilityName(String facilityName) {
        this.facilityName = facilityName;
    }

    public String getFacilityType() {
        return this.facilityType;
    }

    public void setFacilityType(String facilityType) {
        this.facilityType = facilityType;
    }

    public String getCurrency() {
        return this.currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public double getCommitment() {
        return this.commitment;
    }

    public void setCommitment(double commitment) {
        this.commitment = commitment;
    }

    public String getPricingOption() {
        return this.pricingOption;
    }

    public void setPricingOption(String pricingOption) {
        this.pricingOption = pricingOption;
    }

    public String getDayCount() {
        return this.dayCount;
    }

    public void setDayCount(String dayCount) {
        this.dayCount = dayCount;
    }

    public String getStatus() {
        return this.status;
    }

    public void setStatus(String status) {
        this.status = status;
    }
}

