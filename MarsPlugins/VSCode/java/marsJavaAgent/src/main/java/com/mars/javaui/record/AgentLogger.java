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
 * Log directory and file name can be configured via system properties; see docs.
 */
public class AgentLogger {

    /** System property: log directory (default: ${java.io.tmpdir}/javaUIAutomationLog). */
    public static final String SYS_PROP_LOG_DIR = "mars.javaagent.log.dir";

    /** System property: log file name (default: MARSJavaEngineLog_yyyyMMdd.log). If set, use as exact filename (date not appended). */
    public static final String SYS_PROP_LOG_FILE = "mars.javaagent.log.file";

    private static volatile boolean initialized = false;
    private static final DateTimeFormatter LOG_DATE_FORMAT = DateTimeFormatter.ofPattern("yyyyMMdd");
    private static final String DEFAULT_LOG_DIR_NAME = "javaUIAutomationLog";
    private static final String DEFAULT_LOG_FILE_PREFIX = "MARSJavaEngineLog_";

    public static synchronized void setup(File logsDir) {
        if (initialized) return;
        File dirToUse = logsDir;
        if (dirToUse == null) {
            String propDir = System.getProperty(SYS_PROP_LOG_DIR);
            if (propDir != null && !propDir.isEmpty()) {
                dirToUse = new File(propDir);
            } else {
                dirToUse = new File(System.getProperty("java.io.tmpdir", ""), DEFAULT_LOG_DIR_NAME);
            }
        }
        if (!dirToUse.exists() && !dirToUse.mkdirs()) {
            dirToUse = new File(System.getProperty("java.io.tmpdir", ""), DEFAULT_LOG_DIR_NAME);
            dirToUse.mkdirs();
        }
        File logFile = createLogFile(dirToUse);
        if (logFile == null) {
            File fallback = new File(System.getProperty("java.io.tmpdir", ""), DEFAULT_LOG_DIR_NAME);
            if (fallback.exists() || fallback.mkdirs()) logFile = createLogFile(fallback);
        }
        if (logFile != null) {
            try {
                FileHandler fh = new FileHandler(logFile.getAbsolutePath(), true);
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
                System.err.println("[marsJavaAgent] log file: " + logFile.getAbsolutePath());
            } catch (Exception e) {
                Logger.getLogger(AgentLogger.class.getName()).log(Level.WARNING, "setup log failed", e);
            }
        }
    }

    private static File createLogFile(File logsDir) {
        if (logsDir == null || !logsDir.isDirectory()) return null;
        try {
            String fileName = System.getProperty(SYS_PROP_LOG_FILE);
            if (fileName != null && !fileName.trim().isEmpty()) {
                return new File(logsDir, fileName.trim());
            }
            String dateStr = LocalDateTime.now().format(LOG_DATE_FORMAT);
            return new File(logsDir, DEFAULT_LOG_FILE_PREFIX + dateStr + ".log");
        } catch (Exception e) {
            return null;
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
