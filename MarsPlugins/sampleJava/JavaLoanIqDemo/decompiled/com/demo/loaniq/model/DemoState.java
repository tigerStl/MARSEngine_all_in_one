/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.model;

import com.demo.loaniq.model.Deal;
import com.demo.loaniq.model.Facility;
import com.demo.loaniq.model.Loan;
import com.demo.loaniq.model.Payment;
import java.util.ArrayList;
import java.util.List;

public class DemoState {
    private String env;
    private String user;
    private Deal selectedDeal;
    private Facility selectedFacility;
    private Loan selectedLoan;
    private Payment selectedPayment;
    private String lastMessage;
    private final List<Deal> deals = new ArrayList<Deal>();
    private final List<Facility> facilities = new ArrayList<Facility>();
    private final List<Loan> loans = new ArrayList<Loan>();
    private final List<Payment> payments = new ArrayList<Payment>();

    public String getEnv() {
        return this.env;
    }

    public void setEnv(String env) {
        this.env = env;
    }

    public String getUser() {
        return this.user;
    }

    public void setUser(String user) {
        this.user = user;
    }

    public Deal getSelectedDeal() {
        return this.selectedDeal;
    }

    public void setSelectedDeal(Deal selectedDeal) {
        this.selectedDeal = selectedDeal;
    }

    public Facility getSelectedFacility() {
        return this.selectedFacility;
    }

    public void setSelectedFacility(Facility selectedFacility) {
        this.selectedFacility = selectedFacility;
    }

    public Loan getSelectedLoan() {
        return this.selectedLoan;
    }

    public void setSelectedLoan(Loan selectedLoan) {
        this.selectedLoan = selectedLoan;
    }

    public Payment getSelectedPayment() {
        return this.selectedPayment;
    }

    public void setSelectedPayment(Payment selectedPayment) {
        this.selectedPayment = selectedPayment;
    }

    public String getLastMessage() {
        return this.lastMessage;
    }

    public void setLastMessage(String lastMessage) {
        this.lastMessage = lastMessage;
    }

    public List<Deal> getDeals() {
        return this.deals;
    }

    public List<Facility> getFacilities() {
        return this.facilities;
    }

    public List<Loan> getLoans() {
        return this.loans;
    }

    public List<Payment> getPayments() {
        return this.payments;
    }
}

