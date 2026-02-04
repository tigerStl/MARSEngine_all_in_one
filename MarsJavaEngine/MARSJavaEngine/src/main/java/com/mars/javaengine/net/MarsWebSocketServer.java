package com.mars.javaengine.net;

import java.net.InetSocketAddress;
import java.util.function.Consumer;
import java.util.logging.Logger;
import org.java_websocket.WebSocket;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;

public class MarsWebSocketServer extends WebSocketServer {
    private final Logger logger;
    private final Consumer<String> messageHandler;

    public MarsWebSocketServer(int port, Logger logger, Consumer<String> messageHandler) {
        super(new InetSocketAddress(port));
        this.logger = logger;
        this.messageHandler = messageHandler;
    }

    @Override
    public void onOpen(WebSocket conn, ClientHandshake handshake) {
        logger.info("WebSocket client connected: " + conn.getRemoteSocketAddress());
    }

    @Override
    public void onClose(WebSocket conn, int code, String reason, boolean remote) {
        logger.info("WebSocket client closed: " + reason);
    }

    @Override
    public void onMessage(WebSocket conn, String message) {
        logger.info("WebSocket message: " + message);
        if (messageHandler != null) {
            messageHandler.accept(message);
        }
    }

    @Override
    public void onError(WebSocket conn, Exception ex) {
        logger.warning("WebSocket error: " + ex.getMessage());
    }

    @Override
    public void onStart() {
        logger.info("WebSocket server started");
    }
}
