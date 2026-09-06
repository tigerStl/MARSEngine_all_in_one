package com.mars.mcp;

import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class McpStdioServer {
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private final EngineGateway gateway = new EngineGateway();

    public static void main(String[] args) throws Exception {
        new McpStdioServer().run();
    }

    private void run() throws Exception {
        InputStream input = System.in;
        while (true) {
            String message = readMessage(input);
            if (message == null) {
                return;
            }
            if (message.isBlank()) {
                continue;
            }
            @SuppressWarnings("unchecked")
            Map<String, Object> request = MAPPER.readValue(message, Map.class);
            Object id = request.get("id");
            String method = String.valueOf(request.get("method"));
            if ("notifications/initialized".equals(method) || method.startsWith("notifications/")) {
                continue;
            }
            Map<String, Object> response = new LinkedHashMap<>();
            response.put("jsonrpc", "2.0");
            response.put("id", id);
            try {
                response.put("result", handle(method, mapOf(request.get("params"))));
            } catch (Exception ex) {
                response.put("error", Map.of("code", -32000, "message", String.valueOf(ex.getMessage())));
            }
            writeMessage(MAPPER.writeValueAsString(response));
        }
    }

    private Object handle(String method, Map<String, Object> params) throws Exception {
        if ("initialize".equals(method)) {
            Map<String, Object> caps = new LinkedHashMap<>();
            caps.put("tools", Map.of("listChanged", false));
            Map<String, Object> server = new LinkedHashMap<>();
            server.put("name", "mars-java-automation");
            server.put("version", "1.0.0");
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("protocolVersion", "2024-11-05");
            result.put("capabilities", caps);
            result.put("serverInfo", server);
            return result;
        }
        if ("tools/list".equals(method) || "list_tools".equals(method)) {
            return Map.of("tools", ToolCatalog.tools());
        }
        if ("ping".equals(method)) {
            return Map.of();
        }
        if ("tools/call".equals(method)) {
            String name = String.valueOf(params.get("name"));
            Map<String, Object> args = mapOf(params.get("arguments"));
            Map<String, Object> data = callTool(name, args);
            boolean error = Boolean.FALSE.equals(data.get("success")) || data.get("error") != null
                    && !"RESOLVED".equals(data.get("status"));
            if ("RESOLVED".equals(data.get("status")) || Boolean.TRUE.equals(data.get("passed"))
                    || Boolean.TRUE.equals(data.get("success"))) {
                error = false;
            }
            Map<String, Object> content = new LinkedHashMap<>();
            content.put("type", "text");
            content.put("text", MAPPER.writerWithDefaultPrettyPrinter().writeValueAsString(data));
            Map<String, Object> result = new LinkedHashMap<>();
            result.put("content", List.of(content));
            result.put("isError", error);
            return result;
        }
        throw new IllegalArgumentException("Unsupported method: " + method);
    }

    private Map<String, Object> callTool(String name, Map<String, Object> args) throws Exception {
        switch (name) {
            case "list_java_applications":
                return gateway.listApplications();
            case "attach_application":
                return gateway.attach(args);
            case "detach_application":
                return gateway.detach();
            default:
                return gateway.invoke(name, args);
        }
    }

    private static String readMessage(InputStream input) throws Exception {
        ByteArrayOutputStream headerBytes = new ByteArrayOutputStream();
        int prev = -1;
        while (true) {
            int current = input.read();
            if (current < 0) {
                return headerBytes.size() == 0 ? null : headerBytes.toString(StandardCharsets.UTF_8);
            }
            headerBytes.write(current);
            if (prev == '\r' && current == '\n') {
                String headers = headerBytes.toString(StandardCharsets.UTF_8);
                if (headers.contains("Content-Length:")) {
                    while (true) {
                        int a = input.read();
                        int b = input.read();
                        if (a == '\r' && b == '\n') {
                            break;
                        }
                        if (a < 0) {
                            return null;
                        }
                    }
                    int length = parseLength(headers);
                    byte[] body = input.readNBytes(length);
                    return new String(body, StandardCharsets.UTF_8);
                }
            }
            if (current == '\n' && !headerBytes.toString(StandardCharsets.UTF_8).contains("Content-Length:")) {
                return headerBytes.toString(StandardCharsets.UTF_8).trim();
            }
            prev = current;
        }
    }

    private static int parseLength(String headers) {
        for (String line : headers.split("\r?\n")) {
            if (line.toLowerCase().startsWith("content-length:")) {
                return Integer.parseInt(line.substring(line.indexOf(':') + 1).trim());
            }
        }
        return 0;
    }

    private static void writeMessage(String json) throws Exception {
        byte[] body = json.getBytes(StandardCharsets.UTF_8);
        System.out.write(("Content-Length: " + body.length + "\r\n\r\n").getBytes(StandardCharsets.UTF_8));
        System.out.write(body);
        System.out.flush();
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> mapOf(Object value) {
        if (value instanceof Map) {
            return (Map<String, Object>) value;
        }
        return new LinkedHashMap<>();
    }
}
