package com.northstar.capital.service;

import com.northstar.capital.model.Book;
import com.northstar.capital.model.Counterparty;
import com.northstar.capital.model.Position;
import com.northstar.capital.model.RiskRecord;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.SettlementException;
import com.northstar.capital.model.SettlementInstruction;
import com.northstar.capital.model.Trade;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

public class DemoRepository {
    private final List<Trade> trades = new CopyOnWriteArrayList<>();
    private final List<Counterparty> counterparties = new ArrayList<>();
    private final List<Security> securities = new ArrayList<>();
    private final List<Book> books = new ArrayList<>();
    private final List<SettlementInstruction> settlementInstructions = new ArrayList<>();
    private final List<Position> positions = new ArrayList<>();
    private final List<RiskRecord> riskRecords = new ArrayList<>();
    private final List<SettlementException> exceptions = new ArrayList<>();
    private final List<String> legalEntities = new ArrayList<>();
    private final List<String> settlementLocations = new ArrayList<>();

    public List<Trade> getTrades() {
        return trades;
    }

    public List<Counterparty> getCounterparties() {
        return counterparties;
    }

    public List<Security> getSecurities() {
        return securities;
    }

    public List<Book> getBooks() {
        return books;
    }

    public List<SettlementInstruction> getSettlementInstructions() {
        return settlementInstructions;
    }

    public List<Position> getPositions() {
        return positions;
    }

    public List<RiskRecord> getRiskRecords() {
        return riskRecords;
    }

    public List<SettlementException> getExceptions() {
        return exceptions;
    }

    public List<String> getLegalEntities() {
        return legalEntities;
    }

    public List<String> getSettlementLocations() {
        return settlementLocations;
    }

    public List<Trade> snapshotTrades() {
        return Collections.unmodifiableList(new ArrayList<>(trades));
    }
}
