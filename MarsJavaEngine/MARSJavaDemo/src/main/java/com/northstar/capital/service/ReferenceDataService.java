package com.northstar.capital.service;

import com.northstar.capital.model.Book;
import com.northstar.capital.model.Counterparty;
import com.northstar.capital.model.Position;
import com.northstar.capital.model.RiskRecord;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.SettlementException;
import com.northstar.capital.model.SettlementInstruction;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Optional;
import java.util.stream.Collectors;

public class ReferenceDataService {
    private final List<Counterparty> counterparties;
    private final List<Security> securities;
    private final List<Book> books;
    private final List<SettlementInstruction> settlementInstructions;
    private final List<Position> positions;
    private final List<RiskRecord> riskRecords;
    private final List<SettlementException> exceptions;
    private final List<String> legalEntities;
    private final List<String> settlementLocations;

    public ReferenceDataService(DemoRepository repository) {
        this.counterparties = repository.getCounterparties();
        this.securities = repository.getSecurities();
        this.books = repository.getBooks();
        this.settlementInstructions = repository.getSettlementInstructions();
        this.positions = repository.getPositions();
        this.riskRecords = repository.getRiskRecords();
        this.exceptions = repository.getExceptions();
        this.legalEntities = repository.getLegalEntities();
        this.settlementLocations = repository.getSettlementLocations();
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

    public List<String> getCurrencies() {
        return List.of("USD", "EUR", "GBP", "JPY", "CAD", "CHF");
    }

    public List<String> getBookCodes() {
        return books.stream().map(Book::getCode).collect(Collectors.toList());
    }

    public Optional<Counterparty> findCounterparty(String code) {
        if (code == null) {
            return Optional.empty();
        }
        String normalized = code.trim().toUpperCase(Locale.ROOT);
        return counterparties.stream()
                .filter(item -> item.getCode().equalsIgnoreCase(normalized))
                .findFirst();
    }

    public Optional<Security> findSecurity(String securityId) {
        if (securityId == null) {
            return Optional.empty();
        }
        return securities.stream()
                .filter(item -> item.getSecurityId().equalsIgnoreCase(securityId.trim()))
                .findFirst();
    }

    public Optional<Security> findSecurityByDescription(String description) {
        if (description == null) {
            return Optional.empty();
        }
        return securities.stream()
                .filter(item -> item.getDescription().equalsIgnoreCase(description.trim()))
                .findFirst();
    }

    public List<Security> searchSecurities(String securityId, String issuer, String currency,
                                           java.time.LocalDate maturityFrom, java.time.LocalDate maturityTo) {
        return securities.stream()
                .filter(item -> contains(item.getSecurityId(), securityId))
                .filter(item -> contains(item.getIssuer(), issuer) || contains(item.getDescription(), issuer))
                .filter(item -> currency == null || currency.isBlank() || currency.equals(item.getCurrency()))
                .filter(item -> maturityFrom == null || !item.getMaturity().isBefore(maturityFrom))
                .filter(item -> maturityTo == null || !item.getMaturity().isAfter(maturityTo))
                .collect(Collectors.toList());
    }

    public List<Counterparty> searchCounterparties(String name, String country, String type, String status) {
        return counterparties.stream()
                .filter(item -> contains(item.getCode(), name) || contains(item.getName(), name))
                .filter(item -> country == null || country.isBlank() || country.equals(item.getCountry()))
                .filter(item -> type == null || type.isBlank() || type.equals(item.getType()))
                .filter(item -> status == null || status.isBlank() || status.equals(item.getStatus()))
                .collect(Collectors.toList());
    }

    public List<SettlementInstruction> instructionsFor(String counterparty, String currency, String location) {
        List<SettlementInstruction> matches = settlementInstructions.stream()
                .filter(item -> counterparty == null || counterparty.isBlank()
                        || item.getCounterparty().equalsIgnoreCase(counterparty))
                .filter(item -> currency == null || currency.isBlank() || item.getCurrency().equals(currency))
                .filter(item -> location == null || location.isBlank() || item.getLocation().equals(location))
                .collect(Collectors.toList());
        return matches.isEmpty() ? new ArrayList<>(settlementInstructions) : matches;
    }

    private static boolean contains(String value, String query) {
        if (query == null || query.isBlank()) {
            return true;
        }
        return value != null && value.toLowerCase(Locale.ROOT).contains(query.toLowerCase(Locale.ROOT));
    }
}
