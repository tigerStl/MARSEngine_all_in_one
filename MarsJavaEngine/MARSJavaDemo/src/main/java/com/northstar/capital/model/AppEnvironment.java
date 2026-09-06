package com.northstar.capital.model;

public enum AppEnvironment {
    DEV,
    UAT,
    PROD;

    public static AppEnvironment fromName(String value) {
        if (value == null) {
            return UAT;
        }
        try {
            return AppEnvironment.valueOf(value.trim().toUpperCase());
        } catch (IllegalArgumentException ex) {
            return UAT;
        }
    }
}
