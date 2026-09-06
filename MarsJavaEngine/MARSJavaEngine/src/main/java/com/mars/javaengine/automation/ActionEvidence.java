package com.mars.javaengine.automation;

import java.time.OffsetDateTime;
import java.util.LinkedHashMap;
import java.util.Map;

public class ActionEvidence {
    private String action;
    private String field;
    private Object before;
    private Object requested;
    private Object after;
    private boolean success;
    private String timestamp;
    private String controlId;

    public void record(String action, String field, String controlId, Object before, Object requested, Object after, boolean success) {
        this.action = action;
        this.field = field;
        this.controlId = controlId;
        this.before = before;
        this.requested = requested;
        this.after = after;
        this.success = success;
        this.timestamp = OffsetDateTime.now().toString();
    }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new LinkedHashMap<>();
        map.put("action", action);
        map.put("field", field);
        map.put("controlId", controlId);
        map.put("before", before);
        map.put("requested", requested);
        map.put("after", after);
        map.put("success", success);
        map.put("timestamp", timestamp);
        return map;
    }
}
