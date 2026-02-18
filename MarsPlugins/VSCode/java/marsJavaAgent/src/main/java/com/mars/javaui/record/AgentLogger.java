package com.mars.javaui.record;

import java.io.File;
import java.util.logging.FileHandler;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.logging.SimpleFormatter;

/**
 * Utility class for agent logging with begin/end markers.
 */
public class AgentLogger {

    private static volatile boolean initialized = false;

    public static synchronized void setup(File logsDir) {
        if (initialized) return;
        try {
            logsDir = new File("C:\\temp\\logs");
            if (!logsDir.exists()) {
                logsDir.mkdirs();
            }
            File logFile = new File(logsDir, "marsJavaAgent.log");
            FileHandler fh = new FileHandler(logFile.getAbsolutePath(), true);
            fh.setFormatter(new SimpleFormatter());
            fh.setLevel(Level.ALL);

            Logger root = Logger.getLogger("");
            for (Handler h : root.getHandlers()) {
                root.removeHandler(h);
            }
            root.addHandler(fh);
            root.setLevel(Level.ALL);
            initialized = true;
        } catch (Exception e) {
            Logger.getLogger(AgentLogger.class.getName()).log(Level.WARNING, "setup log failed", e);
        }
    }

    public static void begin(Logger logger, String message) {
        logger.info("[BEGIN] " + message);
    }

    public static void end(Logger logger, String message) {
        logger.info("[END] " + message);
    }

    public static void info(Logger logger, String message) {
        logger.info(message);
    }

    public static void warning(Logger logger, String message) {
        logger.warning(message);
    }

    public static void logException(Logger logger, Level level, String message, Throwable t) {
        logger.log(level, message, t);
    }
}
