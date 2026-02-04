package com.mars.javaengine.util;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.logging.FileHandler;
import java.util.logging.Formatter;
import java.util.logging.Level;
import java.util.logging.LogRecord;
import java.util.logging.Logger;

public final class LogUtil {
    private LogUtil() {
    }

    public static Logger createLogger(String name, Path swapDirectory) {
        Logger logger = Logger.getLogger(name);
        logger.setUseParentHandlers(false);
        logger.setLevel(Level.INFO);

        try {
            Path logDir = swapDirectory.resolve("log");
            Files.createDirectories(logDir);
            Path logFile = logDir.resolve(name + ".log");
            FileHandler fileHandler = new FileHandler(logFile.toString(), true);
            fileHandler.setFormatter(new SimpleFormatter());
            logger.addHandler(fileHandler);
        } catch (IOException ex) {
            logger.log(Level.WARNING, "Failed to init log file", ex);
        }

        return logger;
    }

    private static final class SimpleFormatter extends Formatter {
        @Override
        public String format(LogRecord record) {
            return String.format(
                "%1$tF %1$tT [%2$s] %3$s%n",
                record.getMillis(),
                record.getLevel().getName(),
                record.getMessage()
            );
        }
    }
}
