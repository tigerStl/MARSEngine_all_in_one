package com.northstar.capital.service;

import com.northstar.capital.model.AuditEvent;
import com.northstar.capital.model.Book;
import com.northstar.capital.model.Counterparty;
import com.northstar.capital.model.Position;
import com.northstar.capital.model.RiskRecord;
import com.northstar.capital.model.Security;
import com.northstar.capital.model.SettlementException;
import com.northstar.capital.model.SettlementInstruction;
import com.northstar.capital.model.Trade;
import com.northstar.capital.model.TradeSide;
import com.northstar.capital.model.TradeStatus;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.util.List;

public final class DemoDataFactory {
    private DemoDataFactory() {
    }

    public static DemoRepository create() {
        DemoRepository repository = new DemoRepository();
        loadLegalEntities(repository);
        loadLocations(repository);
        loadCounterparties(repository);
        loadSecurities(repository);
        loadBooks(repository);
        loadSettlementInstructions(repository);
        loadPositions(repository);
        loadRisk(repository);
        loadExceptions(repository);
        loadTrades(repository);
        return repository;
    }

    private static void loadLegalEntities(DemoRepository repository) {
        repository.getLegalEntities().addAll(List.of(
                "NORTHSTAR BANK NA",
                "NORTHSTAR CAPITAL LLC",
                "NORTHSTAR EUROPE LTD"));
    }

    private static void loadLocations(DemoRepository repository) {
        repository.getSettlementLocations().addAll(List.of("NYC", "LON", "TKY", "TOR", "ZRH"));
    }

    private static void loadCounterparties(DemoRepository repository) {
        repository.getCounterparties().addAll(List.of(
                new Counterparty("FHLB", "Federal Home Loan Bank", "USA", "Bank", "AA+", "Active"),
                new Counterparty("JPM", "JPM Demo Bank", "USA", "Bank", "AA", "Active"),
                new Counterparty("CITI", "Citi Demo Bank", "USA", "Bank", "A+", "Active"),
                new Counterparty("GS", "GS Demo Markets", "USA", "Broker", "A+", "Active"),
                new Counterparty("BAC", "BAC Demo Bank", "USA", "Bank", "A", "Active"),
                new Counterparty("MS", "MS Demo Securities", "USA", "Broker", "A", "Active"),
                new Counterparty("BARC", "Barc Demo Bank", "GBR", "Bank", "A", "Active"),
                new Counterparty("DB", "DB Demo Bank", "DEU", "Bank", "A-", "Active"),
                new Counterparty("UBS", "UBS Demo AG", "CHE", "Bank", "AA-", "Active"),
                new Counterparty("WFC", "WFC Demo Bank", "USA", "Bank", "A+", "Active"),
                new Counterparty("HSBC", "HSBC Demo Bank", "GBR", "Bank", "A+", "Active")));
    }

    private static void loadSecurities(DemoRepository repository) {
        repository.getSecurities().addAll(List.of(
                security("US91282ABC12", "United States Treasury", "US Treasury 5.00 2035", "5.00", "2035-11-15", "USD", "Treasury"),
                security("US91282CDE34", "United States Treasury", "US Treasury 4.25 2032", "4.25", "2032-05-15", "USD", "Treasury"),
                security("US91282FGH56", "United States Treasury", "US Treasury 3.875 2030", "3.875", "2030-08-15", "USD", "Treasury"),
                security("US912810JK78", "United States Treasury", "US Treasury 4.75 2045", "4.75", "2045-02-15", "USD", "Treasury"),
                security("US3133EMAB01", "FHLB Demo Issuer", "FHLB 4.50 2029", "4.50", "2029-06-12", "USD", "Agency"),
                security("US3134A2CD23", "FHLMC Demo Issuer", "FHLMC 3.75 2028", "3.75", "2028-03-01", "USD", "Agency"),
                security("US037833EF45", "Northstar Industrials", "NSI 5.125 2031", "5.125", "2031-09-15", "USD", "Corporate"),
                security("US459200GH67", "Atlas Demo Corp", "ATLAS 4.80 2033", "4.80", "2033-01-20", "USD", "Corporate"),
                security("US191216IJ89", "Cascade Demo Inc", "CSCD 6.10 2027", "6.10", "2027-12-01", "USD", "Corporate"),
                security("DE000NSTAR01", "Federal Republic Demo", "Bund 2.50 2034", "2.50", "2034-08-15", "EUR", "Sovereign"),
                security("FR000NSTAR02", "Republic Demo France", "OAT 3.00 2031", "3.00", "2031-04-25", "EUR", "Sovereign"),
                security("GB00NSTAR003", "United Kingdom Demo", "Gilt 4.125 2034", "4.125", "2034-07-22", "GBP", "Sovereign"),
                security("JP110NSTAR04", "Japan Demo MOF", "JGB 0.80 2032", "0.80", "2032-03-20", "JPY", "Sovereign"),
                security("CA135NSTAR05", "Canada Demo", "CAN 3.25 2033", "3.25", "2033-06-01", "CAD", "Sovereign"),
                security("CH000NSTAR06", "Swiss Confed Demo", "SWISS 1.25 2036", "1.25", "2036-06-27", "CHF", "Sovereign"),
                security("US91282KLM90", "United States Treasury", "US Treasury 4.00 2027", "4.00", "2027-10-31", "USD", "Treasury")));
    }

    private static void loadBooks(DemoRepository repository) {
        repository.getBooks().addAll(List.of(
                new Book("FI_USD_BOOK", "USD Fixed Income", "USD", "Rates / Credit", "NORTHSTAR BANK NA"),
                new Book("FI_EUR_BOOK", "EUR Fixed Income", "EUR", "Rates / Credit", "NORTHSTAR EUROPE LTD"),
                new Book("FI_GBP_BOOK", "GBP Fixed Income", "GBP", "Rates / Credit", "NORTHSTAR EUROPE LTD"),
                new Book("FX_SPOT_BOOK", "FX Spot", "USD", "FX", "NORTHSTAR CAPITAL LLC"),
                new Book("RATES_BOOK", "Linear Rates", "USD", "Rates", "NORTHSTAR BANK NA")));
    }

    private static void loadSettlementInstructions(DemoRepository repository) {
        repository.getSettlementInstructions().addAll(List.of(
                new SettlementInstruction("FHLB_USD_NYC_01", "FHLB", "USD", "NYC", "FHLB-USD-9921", "FEDWIRE DEMO"),
                new SettlementInstruction("JPM_USD_NYC_01", "JPM", "USD", "NYC", "JPM-USD-4410", "DTC DEMO"),
                new SettlementInstruction("CITI_USD_NYC_01", "CITI", "USD", "NYC", "CITI-USD-1188", "DTC DEMO"),
                new SettlementInstruction("GS_USD_NYC_01", "GS", "USD", "NYC", "GS-USD-2201", "DTC DEMO"),
                new SettlementInstruction("BARC_GBP_LON_01", "BARC", "GBP", "LON", "BARC-GBP-0091", "CREST DEMO"),
                new SettlementInstruction("DB_EUR_LON_01", "DB", "EUR", "LON", "DB-EUR-3302", "EUROCLEAR DEMO"),
                new SettlementInstruction("UBS_CHF_ZRH_01", "UBS", "CHF", "ZRH", "UBS-CHF-5510", "SIX DEMO"),
                new SettlementInstruction("MS_USD_NYC_01", "MS", "USD", "NYC", "MS-USD-7744", "DTC DEMO"),
                new SettlementInstruction("BAC_USD_NYC_01", "BAC", "USD", "NYC", "BAC-USD-6633", "DTC DEMO"),
                new SettlementInstruction("HSBC_USD_NYC_01", "HSBC", "USD", "NYC", "HSBC-USD-1010", "DTC DEMO")));
    }

    private static void loadPositions(DemoRepository repository) {
        repository.getPositions().addAll(List.of(
                position("FI_USD_BOOK", "UST 5.00 2035", "Bond", "185000000", "98.10", "98.25", "181762500", "277500", "USD", "41200"),
                position("FI_USD_BOOK", "UST 4.25 2032", "Bond", "120000000", "97.40", "97.55", "117060000", "180000", "USD", "28800"),
                position("FI_USD_BOOK", "UST 3.875 2030", "Bond", "95000000", "96.80", "96.62", "91789000", "-171000", "USD", "21400"),
                position("FI_USD_BOOK", "FHLB 4.50 2029", "Bond", "60000000", "99.20", "99.05", "59430000", "-90000", "USD", "13200"),
                position("FI_USD_BOOK", "NSI 5.125 2031", "Bond", "35000000", "101.15", "101.40", "35490000", "87500", "USD", "9800"),
                position("FI_USD_BOOK", "ATLAS 4.80 2033", "Bond", "28000000", "98.75", "98.90", "27692000", "42000", "USD", "7600"),
                position("FI_EUR_BOOK", "Bund 2.50 2034", "Bond", "80000000", "97.10", "97.25", "77800000", "120000", "EUR", "26500"),
                position("FI_EUR_BOOK", "OAT 3.00 2031", "Bond", "45000000", "99.40", "99.20", "44640000", "-90000", "EUR", "14800"),
                position("FI_GBP_BOOK", "Gilt 4.125 2034", "Bond", "30000000", "100.20", "100.35", "30105000", "45000", "GBP", "9200"),
                position("RATES_BOOK", "USD 5Y IRS", "IRS", "250000000", "0.00", "0.00", "1250000", "85000", "USD", "62500"),
                position("RATES_BOOK", "USD 10Y IRS", "IRS", "180000000", "0.00", "0.00", "980000", "-42000", "USD", "54000"),
                position("FX_SPOT_BOOK", "EURUSD", "FX", "40000000", "1.0840", "1.0865", "43460000", "100000", "USD", "0"),
                position("FX_SPOT_BOOK", "GBPUSD", "FX", "25000000", "1.2680", "1.2710", "31775000", "75000", "USD", "0"),
                position("FI_USD_BOOK", "UST 4.75 2045", "Bond", "40000000", "96.50", "96.80", "38720000", "120000", "USD", "18600"),
                position("FI_USD_BOOK", "CSCD 6.10 2027", "Bond", "15000000", "102.10", "101.85", "15277500", "-37500", "USD", "3100"),
                position("FI_EUR_BOOK", "Bund 2.50 2034 short", "Bond", "-20000000", "97.30", "97.25", "-19450000", "10000", "EUR", "-6600"),
                position("FI_GBP_BOOK", "Gilt 4.125 2034 short", "Bond", "-8000000", "100.40", "100.35", "-8028000", "4000", "GBP", "-2450"),
                position("RATES_BOOK", "USD FRA 3x6", "FRA", "100000000", "0.00", "0.00", "210000", "18000", "USD", "8900"),
                position("FI_USD_BOOK", "UST 4.00 2027", "Bond", "75000000", "99.10", "99.05", "74287500", "-37500", "USD", "9800"),
                position("FI_USD_BOOK", "FHLMC 3.75 2028", "Bond", "22000000", "97.80", "97.95", "21549000", "33000", "USD", "5400"),
                position("FX_SPOT_BOOK", "USDJPY", "FX", "30000000", "147.20", "147.05", "30000000", "-30500", "USD", "0")));
    }

    private static void loadRisk(DemoRepository repository) {
        repository.getRiskRecords().addAll(List.of(
                risk("FI_USD_BOOK", "125000", "42500", "1250000", "0.63", "412000", "USD"),
                risk("FI_EUR_BOOK", "92000", "33100", "880000", "0.54", "40000", "EUR"),
                risk("FI_GBP_BOOK", "41000", "15400", "420000", "0.38", "49000", "GBP"),
                risk("RATES_BOOK", "156000", "12000", "980000", "0.71", "61000", "USD"),
                risk("FX_SPOT_BOOK", "8000", "0", "310000", "0.22", "144500", "USD")));
    }

    private static void loadExceptions(DemoRepository repository) {
        repository.getExceptions().addAll(List.of(
                exception("EX-10021", "FI-20260904-000012", "High", "SSI Missing",
                        "Standard SSI not found for GS / USD / TKY", "OPS01", "Open", "1d",
                        "Trade booked without a matching SSI. Operations must confirm custodian before release."),
                exception("EX-10022", "FI-20260903-000008", "Medium", "Settlement Date Mismatch",
                        "Counterparty confirm shows 09/09/2026; ticket shows 09/08/2026", "OPS01", "In Progress", "2d",
                        "Economic details otherwise match. Await amended confirmation."),
                exception("EX-10023", "FI-20260902-000019", "High", "Counterparty Hold",
                        "FHLB credit hold pending KYC refresh", "OPS01", "Open", "3d",
                        "Static data team requested updated onboarding pack."),
                exception("EX-10024", "FI-20260904-000027", "Low", "Confirmation Pending",
                        "Electronic confirm not received from CITI", "OPS01", "Open", "1d",
                        "Auto-chase scheduled for next business morning."),
                exception("EX-10025", "FI-20260901-000006", "Medium", "SSI Missing",
                        "Manual override used; account number incomplete", "OPS01", "In Progress", "4d",
                        "Trader entered override without custodian BIC."),
                exception("EX-10026", "FI-20260829-000031", "High", "Settlement Date Mismatch",
                        "Holiday calendar difference for TKY", "OPS01", "Open", "7d",
                        "Japan market holiday not applied on ticket."),
                exception("EX-10027", "FI-20260904-000015", "Low", "Confirmation Pending",
                        "Paper confirm outstanding for BARC gilt trade", "OPS01", "Open", "1d",
                        "Expected via courier."),
                exception("EX-10028", "FI-20260903-000022", "Medium", "Counterparty Hold",
                        "DB compliance review on large ticket", "RISK01", "In Progress", "2d",
                        "Hold expires if no objection by 16:00 UAT."),
                exception("EX-10029", "FI-20260905-000003", "Low", "Confirmation Pending",
                        "Matching engine waiting for UBS affirm", "OPS01", "Open", "0d",
                        "Same-day affirm still inside cutoff."),
                exception("EX-10030", "FI-20260902-000011", "Medium", "SSI Missing",
                        "CAD SSI not set up for WFC", "OPS01", "Open", "3d",
                        "Reference data request RD-4481 is open."),
                exception("EX-10031", "FI-20260828-000004", "High", "Settlement Date Mismatch",
                        "Fail risk: value date before trade date in affirmation", "OPS01", "Escalated", "8d",
                        "Legal entity mapping error on affirmation platform.")));
    }

    private static void loadTrades(DemoRepository repository) {
        LocalDate d = DemoClock.today();
        addTrade(repository, 1, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 5.00 2035", "US91282ABC12",
                "100000000", "USD", "98.2500", "FHLB", d, d.plusDays(3), "NYC", "TRADER01", "FI_USD_BOOK", "14:31:02");
        addTrade(repository, 2, TradeStatus.BOOKED, TradeSide.SELL, "US Treasury 4.25 2032", "US91282CDE34",
                "50000000", "USD", "97.5625", "JPM", d, DemoClock.defaultSettlementDate(d), "NYC", "TRADER01", "FI_USD_BOOK", "14:18:41");
        addTrade(repository, 3, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 3.875 2030", "US91282FGH56",
                "25000000", "USD", "96.7500", "CITI", d.minusDays(1), d.plusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "11:05:12");
        addTrade(repository, 4, TradeStatus.BOOKED, TradeSide.BUY, "FHLB 4.50 2029", "US3133EMAB01",
                "40000000", "USD", "99.1000", "FHLB", d.minusDays(1), d.plusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "10:44:09");
        addTrade(repository, 5, TradeStatus.BOOKED, TradeSide.SELL, "NSI 5.125 2031", "US037833EF45",
                "15000000", "USD", "101.2500", "GS", d.minusDays(2), d.plusDays(1), "NYC", "TRADER01", "FI_USD_BOOK", "16:12:55");
        addTrade(repository, 6, TradeStatus.CANCELLED, TradeSide.BUY, "ATLAS 4.80 2033", "US459200GH67",
                "20000000", "USD", "98.8750", "MS", d.minusDays(3), d.minusDays(1), "NYC", "TRADER01", "FI_USD_BOOK", "09:21:33");
        addTrade(repository, 7, TradeStatus.BOOKED, TradeSide.BUY, "Bund 2.50 2034", "DE000NSTAR01",
                "30000000", "EUR", "97.2000", "DB", d.minusDays(1), d.plusDays(1), "LON", "TRADER01", "FI_EUR_BOOK", "08:55:18");
        addTrade(repository, 8, TradeStatus.BOOKED, TradeSide.SELL, "OAT 3.00 2031", "FR000NSTAR02",
                "18000000", "EUR", "99.3500", "BARC", d.minusDays(2), d, "LON", "TRADER01", "FI_EUR_BOOK", "13:02:47");
        addTrade(repository, 9, TradeStatus.BOOKED, TradeSide.BUY, "Gilt 4.125 2034", "GB00NSTAR003",
                "22000000", "GBP", "100.1250", "BARC", d.minusDays(1), d.plusDays(1), "LON", "TRADER01", "FI_GBP_BOOK", "09:40:22");
        addTrade(repository, 10, TradeStatus.BOOKED, TradeSide.BUY, "JGB 0.80 2032", "JP110NSTAR04",
                "1500000000", "JPY", "99.4000", "UBS", d.minusDays(4), d.minusDays(1), "TKY", "TRADER01", "FI_USD_BOOK", "02:18:05");
        addTrade(repository, 11, TradeStatus.BOOKED, TradeSide.SELL, "CAN 3.25 2033", "CA135NSTAR05",
                "12000000", "CAD", "98.5000", "JPM", d.minusDays(3), d.minusDays(1), "TOR", "TRADER01", "FI_USD_BOOK", "15:27:19");
        addTrade(repository, 12, TradeStatus.BOOKED, TradeSide.BUY, "SWISS 1.25 2036", "CH000NSTAR06",
                "8000000", "CHF", "97.8000", "UBS", d.minusDays(2), d, "ZRH", "TRADER01", "FI_EUR_BOOK", "07:11:44");
        addTrade(repository, 13, TradeStatus.AMENDED, TradeSide.BUY, "US Treasury 4.75 2045", "US912810JK78",
                "35000000", "USD", "96.6250", "BAC", d.minusDays(1), d.plusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "12:36:08");
        addTrade(repository, 14, TradeStatus.BOOKED, TradeSide.SELL, "US Treasury 4.00 2027", "US91282KLM90",
                "60000000", "USD", "99.0625", "CITI", d, DemoClock.defaultSettlementDate(d), "NYC", "TRADER01", "FI_USD_BOOK", "14:02:51");
        addTrade(repository, 15, TradeStatus.BOOKED, TradeSide.BUY, "FHLMC 3.75 2028", "US3134A2CD23",
                "17500000", "USD", "97.9000", "FHLB", d.minusDays(2), d, "NYC", "TRADER01", "FI_USD_BOOK", "11:49:30");
        addTrade(repository, 16, TradeStatus.BOOKED, TradeSide.BUY, "CSCD 6.10 2027", "US191216IJ89",
                "9000000", "USD", "102.0000", "GS", d.minusDays(5), d.minusDays(3), "NYC", "TRADER01", "FI_USD_BOOK", "10:15:26");
        addTrade(repository, 17, TradeStatus.BOOKED, TradeSide.SELL, "US Treasury 5.00 2035", "US91282ABC12",
                "45000000", "USD", "98.1875", "MS", d.minusDays(1), d.plusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "15:58:13");
        addTrade(repository, 18, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 4.25 2032", "US91282CDE34",
                "80000000", "USD", "97.5000", "BAC", d.minusDays(3), d.minusDays(1), "NYC", "TRADER01", "FI_USD_BOOK", "09:03:40");
        addTrade(repository, 19, TradeStatus.BOOKED, TradeSide.BUY, "Bund 2.50 2034", "DE000NSTAR01",
                "55000000", "EUR", "97.1500", "DB", d.minusDays(4), d.minusDays(2), "LON", "TRADER01", "FI_EUR_BOOK", "08:22:17");
        addTrade(repository, 20, TradeStatus.CANCELLED, TradeSide.SELL, "Gilt 4.125 2034", "GB00NSTAR003",
                "10000000", "GBP", "100.2500", "HSBC", d.minusDays(6), d.minusDays(4), "LON", "TRADER01", "FI_GBP_BOOK", "16:44:02");
        addTrade(repository, 21, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 3.875 2030", "US91282FGH56",
                "70000000", "USD", "96.8125", "JPM", d.minusDays(2), d, "NYC", "TRADER01", "FI_USD_BOOK", "13:29:55");
        addTrade(repository, 22, TradeStatus.BOOKED, TradeSide.SELL, "FHLB 4.50 2029", "US3133EMAB01",
                "25000000", "USD", "99.0500", "FHLB", d.minusDays(1), d.plusDays(2), "NYC", "OPS01", "FI_USD_BOOK", "17:05:38");
        addTrade(repository, 23, TradeStatus.BOOKED, TradeSide.BUY, "NSI 5.125 2031", "US037833EF45",
                "12000000", "USD", "101.3750", "WFC", d.minusDays(3), d.minusDays(1), "NYC", "TRADER01", "FI_USD_BOOK", "10:51:14");
        addTrade(repository, 24, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 4.75 2045", "US912810JK78",
                "20000000", "USD", "96.5000", "CITI", d.minusDays(5), d.minusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "14:47:29");
        addTrade(repository, 25, TradeStatus.BOOKED, TradeSide.SELL, "ATLAS 4.80 2033", "US459200GH67",
                "16000000", "USD", "98.7000", "GS", d, DemoClock.defaultSettlementDate(d), "NYC", "TRADER01", "FI_USD_BOOK", "14:09:07");
        addTrade(repository, 26, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 5.00 2035", "US91282ABC12",
                "30000000", "USD", "98.3125", "UBS", d.minusDays(2), d, "NYC", "TRADER01", "FI_USD_BOOK", "11:16:42");
        addTrade(repository, 27, TradeStatus.BOOKED, TradeSide.BUY, "OAT 3.00 2031", "FR000NSTAR02",
                "14000000", "EUR", "99.4000", "DB", d.minusDays(1), d.plusDays(1), "LON", "TRADER01", "FI_EUR_BOOK", "08:08:51");
        addTrade(repository, 28, TradeStatus.AMENDED, TradeSide.SELL, "US Treasury 4.00 2027", "US91282KLM90",
                "42000000", "USD", "99.1250", "MS", d.minusDays(1), d.plusDays(2), "NYC", "TRADER01", "FI_USD_BOOK", "15:33:26");
        addTrade(repository, 29, TradeStatus.BOOKED, TradeSide.BUY, "CAN 3.25 2033", "CA135NSTAR05",
                "9000000", "CAD", "98.6250", "JPM", d.minusDays(4), d.minusDays(2), "TOR", "TRADER01", "FI_USD_BOOK", "16:19:03");
        addTrade(repository, 30, TradeStatus.BOOKED, TradeSide.SELL, "SWISS 1.25 2036", "CH000NSTAR06",
                "6000000", "CHF", "97.8500", "UBS", d.minusDays(3), d.minusDays(1), "ZRH", "RISK01", "FI_EUR_BOOK", "07:42:18");
        addTrade(repository, 31, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 4.25 2032", "US91282CDE34",
                "110000000", "USD", "97.4375", "FHLB", d.minusDays(6), d.minusDays(3), "NYC", "TRADER01", "FI_USD_BOOK", "09:57:44");
        addTrade(repository, 32, TradeStatus.BOOKED, TradeSide.BUY, "US Treasury 5.00 2035", "US91282ABC12",
                "75000000", "USD", "98.0000", "BAC", d.minusDays(7), d.minusDays(4), "NYC", "TRADER01", "FI_USD_BOOK", "13:14:09");
        addTrade(repository, 41, TradeStatus.BOOKED, TradeSide.BUY, "UST 5.00 2035", "US91282ABC12",
                "100000000", "USD", "98.2500", "FHLB", d, DemoClock.defaultSettlementDate(d), "NYC", "TRADER01", "FI_USD_BOOK", "14:31:02");
    }

    private static void addTrade(DemoRepository repository, int seq, TradeStatus status, TradeSide side,
                                 String instrument, String securityId, String notional, String ccy, String price,
                                 String cpty, LocalDate tradeDate, LocalDate settleDate, String location,
                                 String trader, String book, String time) {
        Trade trade = new Trade();
        String id = "FI-" + tradeDate.format(MoneyFormats.TRADE_ID_DATE) + "-" + String.format("%06d", seq);
        // Keep the well-known blotter example on the business date even when tradeDate is reused.
        if (seq == 41) {
            id = "FI-20260905-000041";
        }
        trade.setTradeId(id);
        trade.setStatus(status);
        trade.setProduct("Bond");
        trade.setSide(side);
        trade.setInstrument(instrument);
        trade.setSecurityId(securityId);
        trade.setNotional(new BigDecimal(notional));
        trade.setCurrency(ccy);
        trade.setSettlementCurrency(ccy);
        trade.setPrice(new BigDecimal(price));
        trade.setCounterparty(cpty);
        trade.setTradeDate(tradeDate);
        trade.setSettlementDate(settleDate);
        trade.setSettlementLocation(location);
        trade.setTrader(trader);
        trade.setBook(book);
        trade.setSsi(cpty + "_" + ccy + "_" + location + "_01");
        trade.setLastUpdated(LocalDateTime.of(tradeDate, LocalTime.parse(time)));
        Security security = repository.getSecurities().stream()
                .filter(item -> item.getSecurityId().equals(securityId))
                .findFirst()
                .orElse(null);
        EconomicsCalculator.apply(trade, security);
        trade.getAuditTrail().add(new AuditEvent(trade.getLastUpdated().minusMinutes(8), trader, "CREATE", "Trade", "-", id));
        trade.getAuditTrail().add(new AuditEvent(trade.getLastUpdated().minusMinutes(3), trader, "VALIDATE", "Status", "Draft", "Validated"));
        trade.getAuditTrail().add(new AuditEvent(trade.getLastUpdated(), trader, "SAVE", "Status", "Draft", status.displayName()));
        repository.getTrades().add(trade);
    }

    private static Security security(String id, String issuer, String description, String coupon,
                                    String maturity, String ccy, String type) {
        return new Security(id, issuer, description, new BigDecimal(coupon), LocalDate.parse(maturity), ccy, type);
    }

    private static Position position(String book, String security, String product, String qty, String avg,
                                    String mkt, String mv, String pnl, String ccy, String dv01) {
        return new Position(book, security, product, new BigDecimal(qty), new BigDecimal(avg),
                new BigDecimal(mkt), new BigDecimal(mv), new BigDecimal(pnl), ccy, new BigDecimal(dv01));
    }

    private static RiskRecord risk(String book, String dv01, String cs01, String var, String usage,
                                  String pnl, String ccy) {
        return new RiskRecord(book, new BigDecimal(dv01), new BigDecimal(cs01), new BigDecimal(var),
                new BigDecimal(usage), new BigDecimal(pnl), ccy);
    }

    private static SettlementException exception(String id, String tradeId, String severity, String type,
                                                String description, String owner, String status, String age, String details) {
        return new SettlementException(id, tradeId, severity, type, description, owner, status, age, details);
    }
}
