package com.mars.javaui.agentloader;

import com.sun.tools.attach.VirtualMachine;

import java.io.File;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Attaches UI Scanner agent to target JVM process.
 * Usage: java -jar agent-loader.jar <pid> <agent-jar-path> <output-path>
 * Log file: {jar-dir}/javaagentLog/agent-loader.log
 */
public class Main {
    private static final Logger LOG = AgentLogUtil.createLogger(Main.class, "agent-loader.log");

    public static void main(String[] args) {
        if (args.length < 3) {
            System.err.println("Usage: java -jar agent-loader.jar <pid> <agent-jar-path> <output-path>");
            LOG.severe("Invalid args: length=" + args.length);
            System.exit(1);
        }

        String pid = args[0];
        String agentJar = args[1];
        String outputPath = args[2];

        LOG.info("Startup args: pid=" + pid + ", agentJar=" + agentJar + ", outputPath=" + outputPath);

        File agentFile = new File(agentJar);
        if (!agentFile.exists()) {
            LOG.severe("Agent JAR not found: " + agentJar);
            System.err.println("Agent JAR not found: " + agentJar);
            System.exit(1);
        }

        try {
            LOG.info("Attaching to JVM pid=" + pid);
            VirtualMachine vm = VirtualMachine.attach(pid);
            LOG.info("loadAgent(agentJar=" + agentJar + ", outputPath=" + outputPath + ")");
            vm.loadAgent(agentJar, outputPath);
            vm.detach();
            LOG.info("Agent loaded successfully. Output: " + outputPath);
            System.out.println("Agent loaded successfully. Output: " + outputPath);
        } catch (Exception e) {
            LOG.log(Level.SEVERE, "Failed to attach: " + e.getMessage(), e);
            System.err.println("Failed to attach: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }
}
