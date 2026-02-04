package com.mars.agent;

import com.mars.javaengine.config.EngineConfig;
import com.mars.javaengine.net.TcpJsonClient;
import com.mars.javaengine.util.JsonUtil;
import com.mars.javaengine.util.LogUtil;
import com.sun.tools.attach.VirtualMachine;
import com.sun.tools.attach.VirtualMachineDescriptor;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.DirectoryStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Duration;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import java.util.logging.Logger;

public class MarsJavaEngineAgentMain {
    public static void main(String[] args) {
        if (args.length < 5) {
            System.err.println("Usage: <processName> <processId> <swapDirectory> <serverIp> <serverPort> [debug-single]");
            return;
        }

        String processName = args[0];
        String processId = args[1];
        String swapDirectory = args[2];
        String serverIp = args[3];
        int serverPort = Integer.parseInt(args[4]);
        boolean debugSingle = hasDebugSingle(args);
        boolean unload = hasUnload(args);
        int highlightLimit = resolveHighlightLimit();

        Logger logger = LogUtil.createLogger("MarsJavaEngineAgent", Path.of(swapDirectory));

        if (unload) {
            sendCommand(swapDirectory, "UNLOAD_ENGINE", logger);
            return;
        }

        if (debugSingle) {
            String resolvedProcessId = findProcessIdByName(processName);
            if (resolvedProcessId == null) {
                sendFailure(serverIp, serverPort, "Target java process not found by name", logger);
                return;
            }
            processId = resolvedProcessId;
        } else {
            if (!isJavaProcess(processId, processName)) {
                sendFailure(serverIp, serverPort, "Target process is not a Java application", logger);
                return;
            }
        }

        try {
            Path agentJar = resolveEngineJar();
            EngineConfig config = new EngineConfig(
                swapDirectory,
                serverIp,
                serverPort,
                processName,
                processId,
                debugSingle,
                highlightLimit
            );
            String payload = JsonUtil.toJson(config);

            VirtualMachine vm = VirtualMachine.attach(processId);
            vm.loadAgent(agentJar.toString(), payload);
            vm.detach();
            logger.info("Injected MARSJavaEngine into process " + processId);

            if (debugSingle) {
                sendCommand(swapDirectory, "GET_UIOBJECTS_ALL", logger);
            }
        } catch (Exception ex) {
            sendFailure(serverIp, serverPort, ex.getMessage(), logger);
        }
    }

    private static boolean isJavaProcess(String processId, String processName) {
        for (VirtualMachineDescriptor descriptor : VirtualMachine.list()) {
            if (descriptor.id().equals(processId)) {
                if (processName == null || processName.isBlank()) {
                    return true;
                }
                String displayName = Optional.ofNullable(descriptor.displayName()).orElse("");
                return displayName.contains(processName);
            }
        }
        return false;
    }

    private static String findProcessIdByName(String processName) {
        if (processName == null || processName.isBlank()) {
            return null;
        }
        String currentPid = String.valueOf(ProcessHandle.current().pid());
        for (VirtualMachineDescriptor descriptor : VirtualMachine.list()) {
            if (descriptor.id().equals(currentPid)) {
                continue;
            }
            String displayName = Optional.ofNullable(descriptor.displayName()).orElse("");
            if (displayName.toLowerCase().contains(processName.toLowerCase())) {
                return descriptor.id();
            }
        }
        return null;
    }

    private static Path resolveEngineJar() throws Exception {
        String override = System.getenv("MARS_ENGINE_JAR");
        if (override != null && !override.isBlank()) {
            return Path.of(override);
        }

        URI location = MarsJavaEngineAgentMain.class.getProtectionDomain()
            .getCodeSource().getLocation().toURI();
        Path basePath = Path.of(location);
        Path dir = Files.isDirectory(basePath) ? basePath : basePath.getParent();

        try (DirectoryStream<Path> stream = Files.newDirectoryStream(dir, "MARSJavaEngine-*.jar")) {
            for (Path path : stream) {
                return path;
            }
        }

        throw new IllegalStateException("MARSJavaEngine jar not found in " + dir);
    }

    private static void sendFailure(String serverIp, int serverPort, String message, Logger logger) {
        Map<String, Object> payload = new HashMap<>();
        payload.put("MessageSource", "MARSJavaEngineAgent");
        payload.put("MessageType", "INJECT_JAVAENGINE_STATUS");
        payload.put("ResultType", "Failed");
        payload.put("Message", message == null ? "" : message);

        String json = JsonUtil.toJson(payload);
        try {
            TcpJsonClient.sendJson(serverIp, serverPort, json);
        } catch (Exception ex) {
            logger.warning("Failed to send failure status: " + ex.getMessage());
        }
        logger.warning("Inject failed: " + message);
    }

    private static boolean hasDebugSingle(String[] args) {
        for (int i = 5; i < args.length; i++) {
            if ("debug-single".equalsIgnoreCase(args[i])) {
                return true;
            }
        }
        return false;
    }

    private static boolean hasUnload(String[] args) {
        for (int i = 5; i < args.length; i++) {
            if ("unload".equalsIgnoreCase(args[i])) {
                return true;
            }
        }
        return false;
    }

    private static int resolveHighlightLimit() {
        String env = System.getenv("MARS_HIGHLIGHT_LIMIT");
        if (env != null && !env.isBlank()) {
            try {
                return Integer.parseInt(env.trim());
            } catch (NumberFormatException ignored) {
            }
        }
        return 30;
    }

    private static void sendCommand(String swapDirectory, String messageType, Logger logger) {
        try {
            Path swapFile = waitForSwapFile(Path.of(swapDirectory), Duration.ofSeconds(15));
            if (swapFile == null) {
                logger.warning("Swap file not found for command");
                return;
            }

            Map<String, Object> swap = JsonUtil.fromJson(Files.readString(swapFile), Map.class);
            String svcIp = String.valueOf(swap.get("SvcIp"));
            Object httpPortObj = swap.get("HttpPort");
            if (httpPortObj == null) {
                logger.warning("HttpPort not found in swap file");
                return;
            }
            int httpPort = Integer.parseInt(String.valueOf(httpPortObj));

            Map<String, Object> cmd = new HashMap<>();
            cmd.put("MessageSource", "MARSJavaEngineAgent");
            cmd.put("MessageType", messageType);
            String payload = JsonUtil.toJson(cmd);

            postJson("http://" + svcIp + ":" + httpPort + "/command", payload);
            logger.info("Command sent to " + svcIp + ":" + httpPort + " type=" + messageType);
        } catch (Exception ex) {
            logger.warning("Failed to send command: " + ex.getMessage());
        }
    }

    private static Path waitForSwapFile(Path swapDir, Duration timeout) throws Exception {
        Path swapFile = swapDir.resolve("MarsJavaEngineSwap.json");
        long deadline = System.currentTimeMillis() + timeout.toMillis();
        while (System.currentTimeMillis() < deadline) {
            if (Files.exists(swapFile)) {
                return swapFile;
            }
            Thread.sleep(200);
        }
        return null;
    }

    private static void postJson(String url, String json) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        connection.setRequestMethod("POST");
        connection.setDoOutput(true);
        connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        byte[] payload = json.getBytes(StandardCharsets.UTF_8);
        connection.setFixedLengthStreamingMode(payload.length);
        try (OutputStream output = connection.getOutputStream()) {
            output.write(payload);
        }
        try (InputStream ignored = connection.getInputStream()) {
        }
        connection.disconnect();
    }
}
