package com.mars.javaui.record;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.logging.Level;
import java.util.logging.Logger;

final class JavaLog {

    // Log timestamp format: yyyyMMdd HH24:mm:ss fff
    private static final DateTimeFormatter TS_FORMAT = DateTimeFormatter.ofPattern("yyyyMMdd HH:mm:ss SSS");

    private JavaLog() {
    }

    static void begin(Logger logger, String message) {
        logger.info(format("BEGIN", message, null));
    }

    static void end(Logger logger, String message) {
        logger.info(format("END", message, null));
    }

    static void info(Logger logger, String message) {
        logger.info(format("INFO", message, null));
    }

    static void warning(Logger logger, String message) {
        logger.warning(format("ERROR", message, null));
    }

    static void error(Logger logger, Level level, String message, Throwable t) {
        logger.log(level, format("ERROR", message, t));
    }

    private static String format(String level, String message, Throwable t) {
        StackTraceElement caller = resolveCaller();
        int line = caller != null ? caller.getLineNumber() : -1;
        String method = caller != null ? caller.getClassName() + "." + caller.getMethodName() : "unknown.unknown";
        String ts = LocalDateTime.now().format(TS_FORMAT);

        StringBuilder sb = new StringBuilder();
        sb.append("[").append(level).append("] ")
          .append("[").append(line).append("] ")
          .append(ts)
          .append("\t")
          .append(method);

        if ("INFO".equals(level) || "ERROR".equals(level)) {
            sb.append(" MESSAGE:").append(message != null ? message : "");
        } else if (message != null && !message.isEmpty()) {
            sb.append(" MESSAGE:").append(message);
        }

        if (t != null) {
            sb.append(System.lineSeparator()).append("\t").append(t.toString());
            StringWriter sw = new StringWriter();
            t.printStackTrace(new PrintWriter(sw));
            String[] lines = sw.toString().split("\\r?\\n");
            for (String l : lines) {
                if (l == null || l.isEmpty()) continue;
                sb.append(System.lineSeparator()).append("\t").append(l);
            }
        }
        return sb.toString();
    }

    private static StackTraceElement resolveCaller() {
        StackTraceElement[] stack = Thread.currentThread().getStackTrace();
        for (StackTraceElement e : stack) {
            String c = e.getClassName();
            if (c.equals(Thread.class.getName())) continue;
            if (c.equals(JavaLog.class.getName())) continue;
            if (c.equals(AgentLogger.class.getName())) continue;
            return e;
        }
        return null;
    }
}
