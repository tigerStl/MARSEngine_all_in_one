package com.northstar.capital.service;

import java.time.LocalTime;
import java.time.format.DateTimeFormatter;

public final class AppLog {
    private static final DateTimeFormatter TIME = DateTimeFormatter.ofPattern("HH:mm:ss");

    private AppLog() {
    }

    public static void ui(String message) {
        log("UI", message);
    }

    public static void trade(String message) {
        log("TRADE", message);
    }

    public static void blotter(String message) {
        log("BLOTTER", message);
    }

    public static void log(String category, String message) {
        System.out.println("[" + LocalTime.now().format(TIME) + "] [" + category + "] " + message);
    }
}
