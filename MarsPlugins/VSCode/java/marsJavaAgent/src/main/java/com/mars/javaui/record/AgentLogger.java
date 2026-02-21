package com.mars.javaui.record;

import java.io.File;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.logging.FileHandler;
import java.util.logging.Formatter;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.LogRecord;
import java.util.logging.Logger;

/**
 * Utility class for agent logging with begin/end markers.
 */
public class AgentLogger {

    private static volatile boolean initialized = false;
    private static final DateTimeFormatter LOG_TS_FORMAT = DateTimeFormatter.ofPattern("yyyyMMdd-HHmm");

    public static synchronized void setup(File logsDir) {
        if (initialized) return;
        try {
            if (logsDir == null) {
                logsDir = new File(System.getProperty("java.io.tmpdir"), "mars-javaagent-logs");
            }
            if (!logsDir.exists()) {
                logsDir.mkdirs();
            }
            String suffix = LocalDateTime.now().format(LOG_TS_FORMAT);
            File logFile = new File(logsDir, "marsJavaagent-" + suffix + ".log");
            int seq = 1;
            while (logFile.exists()) {
                logFile = new File(logsDir, "marsJavaagent-" + suffix + "-" + String.format("%02d", seq) + ".log");
                seq++;
            }

            FileHandler fh = new FileHandler(logFile.getAbsolutePath(), false);
            fh.setFormatter(new Formatter() {
                @Override
                public String format(LogRecord record) {
                    return (record.getMessage() != null ? record.getMessage() : "") + System.lineSeparator();
                }
            });
            fh.setLevel(Level.INFO);

            Logger root = Logger.getLogger("");
            for (Handler h : root.getHandlers()) {
                root.removeHandler(h);
            }
            root.addHandler(fh);
            root.setLevel(Level.INFO);
            initialized = true;
        } catch (Exception e) {
            Logger.getLogger(AgentLogger.class.getName()).log(Level.WARNING, "setup log failed", e);
        }
    }

    public static void begin(Logger logger, String message) {
        JavaLog.begin(logger, message);
    }

    public static void end(Logger logger, String message) {
        JavaLog.end(logger, message);
    }

    public static void info(Logger logger, String message) {
        JavaLog.info(logger, message);
    }

    public static void warning(Logger logger, String message) {
        JavaLog.warning(logger, message);
    }

    public static void logException(Logger logger, Level level, String message, Throwable t) {
        JavaLog.error(logger, level, message, t);
    }
}
