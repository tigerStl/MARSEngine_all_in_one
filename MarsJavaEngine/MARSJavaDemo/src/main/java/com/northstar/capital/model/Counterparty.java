package com.northstar.capital.model;

public class Counterparty {
    private String code;
    private String name;
    private String country;
    private String type;
    private String rating;
    private String status;

    public Counterparty() {
    }

    public Counterparty(String code, String name, String country, String type, String rating, String status) {
        this.code = code;
        this.name = name;
        this.country = country;
        this.type = type;
        this.rating = rating;
        this.status = status;
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

    public String getCountry() {
        return country;
    }

    public void setCountry(String country) {
        this.country = country;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getRating() {
        return rating;
    }

    public void setRating(String rating) {
        this.rating = rating;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    @Override
    public String toString() {
        return code;
    }
}
