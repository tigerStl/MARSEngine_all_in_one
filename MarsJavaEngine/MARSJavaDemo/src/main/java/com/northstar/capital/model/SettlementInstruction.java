package com.northstar.capital.model;

public class SettlementInstruction {
    private String code;
    private String counterparty;
    private String currency;
    private String location;
    private String account;
    private String custodian;

    public SettlementInstruction() {
    }

    public SettlementInstruction(String code, String counterparty, String currency,
                                 String location, String account, String custodian) {
        this.code = code;
        this.counterparty = counterparty;
        this.currency = currency;
        this.location = location;
        this.account = account;
        this.custodian = custodian;
    }

    public String getCode() {
        return code;
    }

    public void setCode(String code) {
        this.code = code;
    }

    public String getCounterparty() {
        return counterparty;
    }

    public void setCounterparty(String counterparty) {
        this.counterparty = counterparty;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public String getLocation() {
        return location;
    }

    public void setLocation(String location) {
        this.location = location;
    }

    public String getAccount() {
        return account;
    }

    public void setAccount(String account) {
        this.account = account;
    }

    public String getCustodian() {
        return custodian;
    }

    public void setCustodian(String custodian) {
        this.custodian = custodian;
    }

    @Override
    public String toString() {
        return code;
    }
}
