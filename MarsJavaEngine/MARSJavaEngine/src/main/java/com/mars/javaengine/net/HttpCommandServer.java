package com.mars.javaengine.net;

import com.mars.javaengine.util.JsonUtil;
import com.sun.net.httpserver.Headers;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.io.InputStream;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.function.Function;
import java.util.logging.Logger;

public class HttpCommandServer {
    private final HttpServer server;
    private final Logger logger;

    public HttpCommandServer(int port, Logger logger, Function<String, String> commandHandler) throws IOException {
        this.logger = logger;
        this.server = HttpServer.create(new InetSocketAddress(port), 0);
        this.server.createContext("/command", new CommandHandler(commandHandler, logger));
    }

    public void start() {
        server.start();
        logger.info("HTTP command server started on " + server.getAddress());
    }

    public void stop() {
        server.stop(0);
    }

    public int getPort() {
        return server.getAddress().getPort();
    }

    private static final class CommandHandler implements HttpHandler {
        private final Function<String, String> commandHandler;
        private final Logger logger;

        private CommandHandler(Function<String, String> commandHandler, Logger logger) {
            this.commandHandler = commandHandler;
            this.logger = logger;
        }

        @Override
        public void handle(HttpExchange exchange) throws IOException {
            if (!"POST".equalsIgnoreCase(exchange.getRequestMethod())) {
                respond(exchange, 405, JsonUtil.toJson(Map.of("error", "Only POST supported")));
                return;
            }

            String body = readBody(exchange.getRequestBody());
            String result = "{\"success\":true}";
            if (commandHandler != null) {
                result = commandHandler.apply(body);
            }
            respond(exchange, 200, result == null ? "{\"success\":true}" : result);
        }

        private static String readBody(InputStream input) throws IOException {
            byte[] bytes = input.readAllBytes();
            return new String(bytes, StandardCharsets.UTF_8);
        }

        private static void respond(HttpExchange exchange, int status, String body) throws IOException {
            Headers headers = exchange.getResponseHeaders();
            headers.add("Content-Type", "application/json; charset=utf-8");
            byte[] payload = body.getBytes(StandardCharsets.UTF_8);
            exchange.sendResponseHeaders(status, payload.length);
            exchange.getResponseBody().write(payload);
            exchange.close();
        }
    }
}
