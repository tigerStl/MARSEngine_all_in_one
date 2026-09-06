package com.northstar.capital.model;

public class SettlementException {
    private String exceptionId;
    private String tradeId;
    private String severity;
    private String type;
    private String description;
    private String owner;
    private String status;
    private String age;
    private String details;

    public SettlementException() {
    }

    public SettlementException(String exceptionId, String tradeId, String severity, String type,
                               String description, String owner, String status, String age, String details) {
        this.exceptionId = exceptionId;
        this.tradeId = tradeId;
        this.severity = severity;
        this.type = type;
        this.description = description;
        this.owner = owner;
        this.status = status;
        this.age = age;
        this.details = details;
    }

    public String getExceptionId() {
        return exceptionId;
    }

    public void setExceptionId(String exceptionId) {
        this.exceptionId = exceptionId;
    }

    public String getTradeId() {
        return tradeId;
    }

    public void setTradeId(String tradeId) {
        this.tradeId = tradeId;
    }

    public String getSeverity() {
        return severity;
    }

    public void setSeverity(String severity) {
        this.severity = severity;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getOwner() {
        return owner;
    }

    public void setOwner(String owner) {
        this.owner = owner;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public String getAge() {
        return age;
    }

    public void setAge(String age) {
        this.age = age;
    }

    public String getDetails() {
        return details;
    }

    public void setDetails(String details) {
        this.details = details;
    }
}
