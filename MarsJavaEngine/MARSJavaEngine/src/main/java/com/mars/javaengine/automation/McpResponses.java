package com.mars.javaengine.automation;

import java.util.LinkedHashMap;
import java.util.Map;

public final class McpResponses {
    private McpResponses() {
    }

    public static Map<String, Object> ok(Object data) {
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", true);
        result.put("data", data);
        return result;
    }

    public static Map<String, Object> error(String code, String message, boolean recoverable, String suggestedAction) {
        Map<String, Object> error = new LinkedHashMap<>();
        error.put("code", code);
        error.put("message", message);
        error.put("recoverable", recoverable);
        if (suggestedAction != null) {
            error.put("suggestedAction", suggestedAction);
        }
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("success", false);
        result.put("error", error);
        return result;
    }

    public static Map<String, Object> notAttached() {
        return error("APPLICATION_NOT_ATTACHED", "No Java application is attached.", true,
                "Call attach_application first.");
    }
}
