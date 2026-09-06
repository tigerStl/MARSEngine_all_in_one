package com.northstar.capital.model;

public class Book {
    private String code;
    private String name;
    private String currency;
    private String desk;
    private String legalEntity;

    public Book() {
    }

    public Book(String code, String name, String currency, String desk, String legalEntity) {
        this.code = code;
        this.name = name;
        this.currency = currency;
        this.desk = desk;
        this.legalEntity = legalEntity;
    }

    public String getCode() {
        return code;
    }

    public void setCode(String code) {
        this.code = code;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public String getDesk() {
        return desk;
    }

    public void setDesk(String desk) {
        this.desk = desk;
    }

    public String getLegalEntity() {
        return legalEntity;
    }

    public void setLegalEntity(String legalEntity) {
        this.legalEntity = legalEntity;
    }

    @Override
    public String toString() {
        return code;
    }
}
