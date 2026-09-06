package com.northstar.capital.model;

public enum TradeSide {
    BUY("Buy"),
    SELL("Sell");

    private final String displayName;

    TradeSide(String displayName) {
        this.displayName = displayName;
    }

    public String displayName() {
        return displayName;
    }

    public static TradeSide fromDisplay(String value) {
        if (value == null) {
            return BUY;
        }
        for (TradeSide side : values()) {
            if (side.displayName.equalsIgnoreCase(value) || side.name().equalsIgnoreCase(value)) {
                return side;
            }
        }
        return BUY;
    }
}
