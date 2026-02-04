package com.mars.javaui.scanner;

import java.io.File;
import java.net.URL;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.logging.*;

/**
 * File logger under {jar-dir}/javaagentLog/
 */
public final class AgentLogUtil {
    private static final String LOG_DIR_NAME = "javaagentLog";

    public static Logger createLogger(Class<?> clazz, String logFileName) {
        Logger logger = Logger.getLogger(clazz.getName());
        logger.setUseParentHandlers(false);
        logger.setLevel(Level.ALL);
        try {
            File logDir = resolveLogDir(clazz);
            if (!logDir.exists()) {
                logDir.mkdirs();
            }
            File logFile = new File(logDir, logFileName);
            FileHandler fh = new FileHandler(logFile.getAbsolutePath(), true);
            fh.setEncoding("UTF-8");
            fh.setFormatter(new SimpleFormatter());
            fh.setLevel(Level.ALL);
            logger.addHandler(fh);
        } catch (Exception e) {
            logger.addHandler(new ConsoleHandler());
            logger.warning("Could not create file handler: " + e.getMessage());
        }
        return logger;
    }

    static File resolveLogDir(Class<?> clazz) {
        try {
            URL location = clazz.getProtectionDomain().getCodeSource().getLocation();
            Path path = Paths.get(location.toURI());
            File dir = path.toFile();
            if (!dir.isDirectory()) {
                dir = path.getParent() != null ? path.getParent().toFile() : new File(".");
            }
            return new File(dir, LOG_DIR_NAME);
        } catch (Exception e) {
            return new File(".", LOG_DIR_NAME);
        }
    }
}
