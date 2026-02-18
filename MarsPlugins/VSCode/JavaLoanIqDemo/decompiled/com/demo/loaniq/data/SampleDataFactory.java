/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.data;

import com.demo.loaniq.model.Deal;
import com.demo.loaniq.model.DemoState;
import com.demo.loaniq.model.Facility;
import com.demo.loaniq.model.Loan;
import com.demo.loaniq.model.Payment;

public final class SampleDataFactory {
    private SampleDataFactory() {
    }

    public static DemoState createSampleState() {
        DemoState state = new DemoState();
        state.setEnv("DEV");
        state.setUser("tiger.liu");
        state.setLastMessage("Ready");
        Deal deal = new Deal();
        deal.setDealName("US_Syndicated_Loan_001");
        deal.setDealType("Syndicated");
        deal.setBorrower("CUSTOMER1");
        deal.setCurrency("USD");
        deal.setCountry("US");
        deal.setStatus("Approved");
        deal.setAgreementDate("2013/08/12");
        deal.setEffectiveDate("2012/05/16");
        deal.setMaturityDate("2017/05/16");
        deal.setAmount(5.0E7);
        state.getDeals().add(deal);
        state.setSelectedDeal(deal);
        Facility facility = new Facility();
        facility.setFacilityName("FACILITY_A");
        facility.setFacilityType("Revolver");
        facility.setCurrency("USD");
        facility.setCommitment(5.0E7);
        facility.setPricingOption("SOFR + Spread");
        facility.setDayCount("ACT/360");
        facility.setStatus("Approved");
        state.getFacilities().add(facility);
        state.setSelectedFacility(facility);
        Loan loan = new Loan();
        loan.setAlias("T3750");
        loan.setCurrency("USD");
        loan.setPrincipal(5.0E7);
        loan.setRateType("Floating");
        loan.setIndex("SOFR");
        loan.setSpread(0.0025);
        loan.setAllInRate(0.0069);
        loan.setStartDate("2012/05/16");
        loan.setEndDate("2017/05/16");
        loan.setStatus("Released");
        state.getLoans().add(loan);
        state.setSelectedLoan(loan);
        Payment payment = new Payment();
        payment.setPaymentType("Interest Payment");
        payment.setAmount(86250.0);
        payment.setValueDate("2013/09/16");
        payment.setStatus("Pending");
        state.getPayments().add(payment);
        state.setSelectedPayment(payment);
        return state;
    }
}

