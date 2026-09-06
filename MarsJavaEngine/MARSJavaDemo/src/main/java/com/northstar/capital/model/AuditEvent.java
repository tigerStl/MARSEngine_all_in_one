package com.northstar.capital.model;

import java.time.LocalDateTime;

public class AuditEvent {
    private LocalDateTime timestamp;
    private String user;
    private String action;
    private String field;
    private String oldValue;
    private String newValue;

    public AuditEvent() {
    }

    public AuditEvent(LocalDateTime timestamp, String user, String action, String field, String oldValue, String newValue) {
        this.timestamp = timestamp;
        this.user = user;
        this.action = action;
        this.field = field;
        this.oldValue = oldValue;
        this.newValue = newValue;
    }

    public LocalDateTime getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(LocalDateTime timestamp) {
        this.timestamp = timestamp;
    }

    public String getUser() {
        return user;
    }

    public void setUser(String user) {
        this.user = user;
    }

    public String getAction() {
        return action;
    }

    public void setAction(String action) {
        this.action = action;
    }

    public String getField() {
        return field;
    }

    public void setField(String field) {
        this.field = field;
    }

    public String getOldValue() {
        return oldValue;
    }

    public void setOldValue(String oldValue) {
        this.oldValue = oldValue;
    }

    public String getNewValue() {
        return newValue;
    }

    public void setNewValue(String newValue) {
        this.newValue = newValue;
    }
}
