package com.northstar.capital.model;

public enum TradeStatus {
    DRAFT("Draft"),
    VALIDATED("Validated"),
    BOOKED("Booked"),
    AMENDED("Amended"),
    CANCELLED("Cancelled");

    private final String displayName;

    TradeStatus(String displayName) {
        this.displayName = displayName;
    }

    public String displayName() {
        return displayName;
    }
}
