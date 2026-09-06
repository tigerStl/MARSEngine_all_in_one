package com.mars.javaengine;

import com.mars.javaengine.automation.CommandDispatcher;
import com.mars.javaengine.automation.UiAutomationService;
import com.mars.javaengine.config.EngineConfig;
import com.mars.javaengine.net.HttpCommandServer;
import com.mars.javaengine.net.MarsWebSocketServer;
import com.mars.javaengine.net.TcpJsonClient;
import com.mars.javaengine.util.JsonUtil;
import com.mars.javaengine.util.LogUtil;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.logging.Logger;

public class EngineService {
    private final EngineConfig config;
    private final Logger logger;
    private volatile MarsWebSocketServer webSocketServer;
    private volatile HttpCommandServer httpCommandServer;
    private volatile CountDownLatch keepAliveLatch;
    private volatile boolean stopped;
    private volatile CommandDispatcher dispatcher;

    public EngineService(EngineConfig config) {
        this.config = config;
        Path swapDir = Path.of(config.getSwapDirectory());
        this.logger = LogUtil.createLogger("MarsJavaEngine", swapDir);
    }

    public void start() {
        try {
            Path swapDir = Path.of(config.getSwapDirectory());
            Files.createDirectories(swapDir);
            UiAutomationService automation = new UiAutomationService(swapDir);
            this.dispatcher = new CommandDispatcher(automation, swapDir, logger, config.getHighlightLimit());

            int websocketPort = findFreePort();
            String svcIp = resolveLocalIp();

            MarsWebSocketServer server = new MarsWebSocketServer(websocketPort, logger, this::handleCommand);
            server.start();
            this.webSocketServer = server;

            Integer httpPort = startHttpServer();
            writeSwapFile(swapDir, svcIp, websocketPort, httpPort);
            sendHandshake(svcIp, websocketPort);
            keepAlive();
        } catch (Exception ex) {
            logger.warning("Engine start failed: " + ex.getMessage());
        }
    }

    private void sendHandshake(String svcIp, int websocketPort) {
        Map<String, Object> payload = new HashMap<>();
        payload.put("MessageSource", "MARSJaveEngine");
        payload.put("MessageType", "HAND_SHAKING");
        payload.put("ResultType", String.valueOf(websocketPort));
        payload.put("SvcIp", svcIp);
        payload.put("targetProcessName", config.getTargetProcessName());
        payload.put("targetProcessPath", "");

        String json = JsonUtil.toJson(payload);
        try {
            TcpJsonClient.sendJson(config.getServerIp(), config.getServerPort(), json);
            logger.info("Handshake sent: " + json);
        } catch (Exception ex) {
            logger.warning("Failed to send handshake: " + ex.getMessage());
        }
    }

    private void writeSwapFile(Path swapDir, String svcIp, int websocketPort, Integer httpPort) throws Exception {
        Map<String, Object> swap = new HashMap<>();
        swap.put("SvcIp", svcIp);
        swap.put("PortNumber", websocketPort);
        if (httpPort != null) {
            swap.put("HttpPort", httpPort);
        }
        String json = JsonUtil.toJson(swap);
        Files.writeString(swapDir.resolve("MarsJavaEngineSwap.json"), json);
        logger.info("Swap file written: " + json);
    }

    private int findFreePort() throws Exception {
        try (ServerSocket socket = new ServerSocket(0)) {
            return socket.getLocalPort();
        }
    }

    private String resolveLocalIp() {
        try {
            return InetAddress.getLocalHost().getHostAddress();
        } catch (Exception ex) {
            return "127.0.0.1";
        }
    }

    private void keepAlive() {
        keepAliveLatch = new CountDownLatch(1);
        Thread t = new Thread(() -> {
            try {
                keepAliveLatch.await();
            } catch (InterruptedException ignored) {
            }
        }, "MarsJavaEngine-keepalive");
        t.setDaemon(false);
        t.start();
    }

    private int startHttpServer() throws Exception {
        int port = findFreePort();
        HttpCommandServer httpServer = new HttpCommandServer(port, logger, this::handleCommand);
        httpServer.start();
        this.httpCommandServer = httpServer;
        return httpServer.getPort();
    }

    private String handleCommand(String json) {
        try {
            @SuppressWarnings("unchecked")
            Map<String, Object> payload = JsonUtil.fromJson(json, Map.class);
            Object messageType = payload.get("MessageType");
            if ("UNLOAD_ENGINE".equalsIgnoreCase(String.valueOf(messageType))) {
                String result = dispatcher == null ? "{\"success\":true}" : dispatcher.handleRaw(json);
                stopEngine();
                return result;
            }
            if (dispatcher != null) {
                return dispatcher.handleRaw(json);
            }
            return "{\"success\":false,\"error\":{\"code\":\"INTERNAL_ERROR\",\"message\":\"Dispatcher not ready\"}}";
        } catch (Exception ex) {
            logger.warning("Failed to handle command: " + ex.getMessage());
            return "{\"success\":false,\"error\":{\"code\":\"INTERNAL_ERROR\",\"message\":\""
                    + String.valueOf(ex.getMessage()).replace("\"", "'") + "\"}}";
        }
    }

    private void stopEngine() {
        if (stopped) {
            return;
        }
        stopped = true;
        try {
            if (httpCommandServer != null) {
                httpCommandServer.stop();
            }
        } catch (Exception ex) {
            logger.warning("Failed to stop HTTP server: " + ex.getMessage());
        }
        try {
            if (webSocketServer != null) {
                webSocketServer.stop();
            }
        } catch (Exception ex) {
            logger.warning("Failed to stop WebSocket server: " + ex.getMessage());
        }
        try {
            if (keepAliveLatch != null) {
                keepAliveLatch.countDown();
            }
        } catch (Exception ex) {
            logger.warning("Failed to stop keepalive: " + ex.getMessage());
        }
        logger.info("Engine stopped");
    }
}
