package com.northstar.capital.model;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

public class Trade {
    private String tradeId = "NEW";
    private TradeStatus status = TradeStatus.DRAFT;
    private String product = "Bond";
    private TradeSide side = TradeSide.BUY;
    private String instrument;
    private String securityId;
    private BigDecimal notional;
    private String currency = "USD";
    private BigDecimal price;
    private String priceType = "Clean";
    private BigDecimal yield;
    private String counterparty;
    private LocalDate tradeDate;
    private LocalDate settlementDate;
    private String settlementLocation = "NYC";
    private String settlementCurrency = "USD";
    private String ssi;
    private boolean useStandardSsi = true;
    private boolean manualSettlementOverride;
    private String settlementAccount;
    private String custodian;
    private String agentBank;
    private String specialInstructions;
    private String book = "FI_USD_BOOK";
    private String legalEntity = "NORTHSTAR BANK NA";
    private String trader = "TRADER01";
    private BigDecimal grossAmount;
    private BigDecimal accruedInterest;
    private BigDecimal netSettlement;
    private BigDecimal spread;
    private String benchmark;
    private BigDecimal discountMargin;
    private Integer accruedDays;
    private Integer lots = 1;
    private LocalDateTime lastUpdated;
    private final List<AuditEvent> auditTrail = new ArrayList<>();

    public Trade copy() {
        Trade copy = new Trade();
        copy.tradeId = tradeId;
        copy.status = status;
        copy.product = product;
        copy.side = side;
        copy.instrument = instrument;
        copy.securityId = securityId;
        copy.notional = notional;
        copy.currency = currency;
        copy.price = price;
        copy.priceType = priceType;
        copy.yield = yield;
        copy.counterparty = counterparty;
        copy.tradeDate = tradeDate;
        copy.settlementDate = settlementDate;
        copy.settlementLocation = settlementLocation;
        copy.settlementCurrency = settlementCurrency;
        copy.ssi = ssi;
        copy.useStandardSsi = useStandardSsi;
        copy.manualSettlementOverride = manualSettlementOverride;
        copy.settlementAccount = settlementAccount;
        copy.custodian = custodian;
        copy.agentBank = agentBank;
        copy.specialInstructions = specialInstructions;
        copy.book = book;
        copy.legalEntity = legalEntity;
        copy.trader = trader;
        copy.grossAmount = grossAmount;
        copy.accruedInterest = accruedInterest;
        copy.netSettlement = netSettlement;
        copy.spread = spread;
        copy.benchmark = benchmark;
        copy.discountMargin = discountMargin;
        copy.accruedDays = accruedDays;
        copy.lots = lots;
        copy.lastUpdated = lastUpdated;
        copy.auditTrail.addAll(auditTrail);
        return copy;
    }

    public String getTradeId() {
        return tradeId;
    }

    public void setTradeId(String tradeId) {
        this.tradeId = tradeId;
    }

    public TradeStatus getStatus() {
        return status;
    }

    public void setStatus(TradeStatus status) {
        this.status = status;
    }

    public String getProduct() {
        return product;
    }

    public void setProduct(String product) {
        this.product = product;
    }

    public TradeSide getSide() {
        return side;
    }

    public void setSide(TradeSide side) {
        this.side = side;
    }

    public String getInstrument() {
        return instrument;
    }

    public void setInstrument(String instrument) {
        this.instrument = instrument;
    }

    public String getSecurityId() {
        return securityId;
    }

    public void setSecurityId(String securityId) {
        this.securityId = securityId;
    }

    public BigDecimal getNotional() {
        return notional;
    }

    public void setNotional(BigDecimal notional) {
        this.notional = notional;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public String getPriceType() {
        return priceType;
    }

    public void setPriceType(String priceType) {
        this.priceType = priceType;
    }

    public BigDecimal getYield() {
        return yield;
    }

    public void setYield(BigDecimal yield) {
        this.yield = yield;
    }

    public String getCounterparty() {
        return counterparty;
    }

    public void setCounterparty(String counterparty) {
        this.counterparty = counterparty;
    }

    public LocalDate getTradeDate() {
        return tradeDate;
    }

    public void setTradeDate(LocalDate tradeDate) {
        this.tradeDate = tradeDate;
    }

    public LocalDate getSettlementDate() {
        return settlementDate;
    }

    public void setSettlementDate(LocalDate settlementDate) {
        this.settlementDate = settlementDate;
    }

    public String getSettlementLocation() {
        return settlementLocation;
    }

    public void setSettlementLocation(String settlementLocation) {
        this.settlementLocation = settlementLocation;
    }

    public String getSettlementCurrency() {
        return settlementCurrency;
    }

    public void setSettlementCurrency(String settlementCurrency) {
        this.settlementCurrency = settlementCurrency;
    }

    public String getSsi() {
        return ssi;
    }

    public void setSsi(String ssi) {
        this.ssi = ssi;
    }

    public boolean isUseStandardSsi() {
        return useStandardSsi;
    }

    public void setUseStandardSsi(boolean useStandardSsi) {
        this.useStandardSsi = useStandardSsi;
    }

    public boolean isManualSettlementOverride() {
        return manualSettlementOverride;
    }

    public void setManualSettlementOverride(boolean manualSettlementOverride) {
        this.manualSettlementOverride = manualSettlementOverride;
    }

    public String getSettlementAccount() {
        return settlementAccount;
    }

    public void setSettlementAccount(String settlementAccount) {
        this.settlementAccount = settlementAccount;
    }

    public String getCustodian() {
        return custodian;
    }

    public void setCustodian(String custodian) {
        this.custodian = custodian;
    }

    public String getAgentBank() {
        return agentBank;
    }

    public void setAgentBank(String agentBank) {
        this.agentBank = agentBank;
    }

    public String getSpecialInstructions() {
        return specialInstructions;
    }

    public void setSpecialInstructions(String specialInstructions) {
        this.specialInstructions = specialInstructions;
    }

    public String getBook() {
        return book;
    }

    public void setBook(String book) {
        this.book = book;
    }

    public String getLegalEntity() {
        return legalEntity;
    }

    public void setLegalEntity(String legalEntity) {
        this.legalEntity = legalEntity;
    }

    public String getTrader() {
        return trader;
    }

    public void setTrader(String trader) {
        this.trader = trader;
    }

    public BigDecimal getGrossAmount() {
        return grossAmount;
    }

    public void setGrossAmount(BigDecimal grossAmount) {
        this.grossAmount = grossAmount;
    }

    public BigDecimal getAccruedInterest() {
        return accruedInterest;
    }

    public void setAccruedInterest(BigDecimal accruedInterest) {
        this.accruedInterest = accruedInterest;
    }

    public BigDecimal getNetSettlement() {
        return netSettlement;
    }

    public void setNetSettlement(BigDecimal netSettlement) {
        this.netSettlement = netSettlement;
    }

    public BigDecimal getSpread() {
        return spread;
    }

    public void setSpread(BigDecimal spread) {
        this.spread = spread;
    }

    public String getBenchmark() {
        return benchmark;
    }

    public void setBenchmark(String benchmark) {
        this.benchmark = benchmark;
    }

    public BigDecimal getDiscountMargin() {
        return discountMargin;
    }

    public void setDiscountMargin(BigDecimal discountMargin) {
        this.discountMargin = discountMargin;
    }

    public Integer getAccruedDays() {
        return accruedDays;
    }

    public void setAccruedDays(Integer accruedDays) {
        this.accruedDays = accruedDays;
    }

    public Integer getLots() {
        return lots;
    }

    public void setLots(Integer lots) {
        this.lots = lots;
    }

    public LocalDateTime getLastUpdated() {
        return lastUpdated;
    }

    public void setLastUpdated(LocalDateTime lastUpdated) {
        this.lastUpdated = lastUpdated;
    }

    public List<AuditEvent> getAuditTrail() {
        return auditTrail;
    }
}
