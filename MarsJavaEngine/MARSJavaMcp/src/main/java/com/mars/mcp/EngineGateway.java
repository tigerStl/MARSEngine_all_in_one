package com.mars.mcp;

import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.nio.file.DirectoryStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Duration;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;

public class EngineGateway {
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private String attachedPid;
    private String sessionId;
    private String applicationName;
    private String technology;
    private String commandUrl;
    private Path swapDirectory;

    public synchronized Map<String, Object> listApplications() {
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", true);
        result.put("applications", JavaProcessLister.list(attachedPid));
        return result;
    }

    public synchronized Map<String, Object> attach(Map<String, Object> input) throws Exception {
        String processId = resolveProcessId(input);
        if (processId == null) {
            return error("APPLICATION_NOT_FOUND", "applicationId or processId is required.");
        }
        if (processId.equals(attachedPid) && commandUrl != null) {
            return attachedResult();
        }
        if (attachedPid != null) {
            detach();
        }

        Path agentJar = resolveJar("MARS_AGENT_JAR", "MARSJavaEngineAgent-*.jar");
        Path engineJar = resolveJar("MARS_ENGINE_JAR", "MARSJavaEngine-*.jar");
        swapDirectory = Files.createTempDirectory("mars-java-mcp-");
        String displayName = displayNameFor(processId);

        ProcessBuilder builder = new ProcessBuilder(
                ProcessHandle.current().info().command().orElse("java"),
                "-jar",
                agentJar.toString(),
                displayName,
                processId,
                swapDirectory.toString(),
                "127.0.0.1",
                "9"
        );
        builder.environment().put("MARS_ENGINE_JAR", engineJar.toString());
        builder.redirectErrorStream(true);
        Process process = builder.start();
        process.waitFor();

        Path swapFile = waitForSwap(swapDirectory, Duration.ofSeconds(20));
        if (swapFile == null) {
            return error("APPLICATION_NOT_FOUND", "Engine swap file was not created. Injection may have failed.");
        }
        @SuppressWarnings("unchecked")
        Map<String, Object> swap = MAPPER.readValue(Files.readString(swapFile), Map.class);
        Object httpPort = swap.get("HttpPort");
        if (httpPort == null) {
            return error("INTERNAL_ERROR", "HttpPort missing from swap file.");
        }
        String svcIp = String.valueOf(swap.get("SvcIp"));
        commandUrl = "http://" + svcIp + ":" + httpPort + "/command";
        attachedPid = processId;
        sessionId = "mars-session-" + processId;
        applicationName = JavaProcessLister.inferName(displayName);
        technology = JavaProcessLister.inferTechnology(displayName);
        return attachedResult();
    }

    public synchronized Map<String, Object> detach() {
        if (commandUrl != null) {
            try {
                invoke("detach_application", Map.of("MessageType", "UNLOAD_ENGINE"));
            } catch (Exception ignored) {
            }
        }
        attachedPid = null;
        sessionId = null;
        commandUrl = null;
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", true);
        result.put("detached", true);
        return result;
    }

    public synchronized Map<String, Object> invoke(String tool, Map<String, Object> arguments) throws Exception {
        if (commandUrl == null) {
            return error("APPLICATION_NOT_ATTACHED", "No Java application is attached. Call attach_application first.");
        }
        Map<String, Object> payload = new LinkedHashMap<>();
        payload.put("MessageSource", "MARSJavaMcp");
        payload.put("MessageType", tool);
        payload.put("RequestId", UUID.randomUUID().toString());
        payload.put("MessageInfo", arguments == null ? Map.of() : arguments);
        String body = MAPPER.writeValueAsString(payload);
        HttpURLConnection connection = (HttpURLConnection) URI.create(commandUrl).toURL().openConnection();
        connection.setRequestMethod("POST");
        connection.setDoOutput(true);
        connection.setConnectTimeout(5000);
        connection.setReadTimeout(30000);
        connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        byte[] bytes = body.getBytes(StandardCharsets.UTF_8);
        connection.setFixedLengthStreamingMode(bytes.length);
        try (OutputStream output = connection.getOutputStream()) {
            output.write(bytes);
        }
        String response;
        try (InputStream input = connection.getInputStream()) {
            response = new String(input.readAllBytes(), StandardCharsets.UTF_8);
        }
        connection.disconnect();
        @SuppressWarnings("unchecked")
        Map<String, Object> parsed = MAPPER.readValue(response, Map.class);
        return parsed;
    }

    public boolean isAttached() {
        return commandUrl != null;
    }

    private Map<String, Object> attachedResult() {
        Map<String, Object> application = new LinkedHashMap<>();
        application.put("name", applicationName);
        application.put("technology", technology);
        application.put("processId", Integer.parseInt(attachedPid));
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", true);
        result.put("sessionId", sessionId);
        result.put("application", application);
        return result;
    }

    private String resolveProcessId(Map<String, Object> input) {
        if (input == null) {
            return null;
        }
        if (input.get("processId") != null) {
            return String.valueOf(input.get("processId"));
        }
        Object applicationId = input.get("applicationId");
        if (applicationId != null && String.valueOf(applicationId).startsWith("app-")) {
            return String.valueOf(applicationId).substring(4);
        }
        return applicationId == null ? null : String.valueOf(applicationId);
    }

    private String displayNameFor(String processId) {
        for (Map<String, Object> app : JavaProcessLister.list(attachedPid)) {
            if (String.valueOf(app.get("processId")).equals(processId)) {
                return String.valueOf(app.get("displayName"));
            }
        }
        return "";
    }

    private Path resolveJar(String envName, String glob) throws Exception {
        String env = System.getenv(envName);
        if (env != null && !env.isBlank()) {
            return Path.of(env);
        }
        Path mcpLocation = Path.of(EngineGateway.class.getProtectionDomain().getCodeSource().getLocation().toURI());
        Path dir = Files.isDirectory(mcpLocation) ? mcpLocation : mcpLocation.getParent();
        Path found = findJar(dir, glob);
        if (found != null) {
            return found;
        }
        Path repo = Path.of("").toAbsolutePath();
        String module = glob.startsWith("MARSJavaEngineAgent") ? "MARSJavaEngineAgent" : "MARSJavaEngine";
        found = findJar(repo.resolve(module).resolve("target"), glob);
        if (found != null) {
            return found;
        }
        throw new IllegalStateException(glob + " not found. Set " + envName + " or place the JAR next to MARSJavaMcp.");
    }

    private Path findJar(Path dir, String glob) throws Exception {
        if (dir == null || !Files.isDirectory(dir)) {
            return null;
        }
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(dir, glob)) {
            for (Path path : stream) {
                if (!path.getFileName().toString().contains("sources")) {
                    return path;
                }
            }
        }
        return null;
    }

    private Path waitForSwap(Path swapDir, Duration timeout) throws Exception {
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

    private static Map<String, Object> error(String code, String message) {
        Map<String, Object> error = new LinkedHashMap<>();
        error.put("code", code);
        error.put("message", message);
        error.put("recoverable", true);
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", false);
        result.put("error", error);
        return result;
    }
}
