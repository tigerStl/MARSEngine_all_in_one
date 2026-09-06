# Northstar Capital Markets Workstation

Fictional enterprise capital-markets Java desktop demo for Cursor, Codex, and MARS Java Automation MCP.

The application is a dense front-office / middle-office workstation used to book, validate, and inspect fixed-income trades. It does not copy any commercial vendor UI, logo, product name, or screenshot.

## Build

From this directory:

```bash
mvn clean package
```

Java 17+ is required.

## Run

```bash
mvn exec:java
```

Or from the packaged JAR:

```bash
java -jar target/northstar-financial-demo-1.0.0.jar
```

The window title is **Northstar Capital Markets Workstation**. Default session:

- Environment: `UAT`
- User: `TRADER01`
- Server: `NCM-UAT-01`

## Demo Scenarios

These natural-language instructions are the intended Agent / MARS exercises. Full expected steps are in `docs/agent-demo-scenarios.md`.

### Scenario A — Simple Trade Creation

> Create a USD bond trade. Buy 100 million notional, use FHLB as the counterparty, price 98.25, settle in NYC, validate and save it.

### Scenario B — Natural Business Language

> Book 50m of the Treasury at 99.125 against JPM with New York settlement.

### Scenario C — Validation Failure

> Create a 100m USD trade at price 250 and save it.

The application must reject the price. Save stays disabled until validation passes.

### Scenario D — Lookup

> Create a bond trade using the 2035 US Treasury and FHLB.

Use **Search...** for the instrument and **Lookup...** for the counterparty.

### Scenario E — Verify Trade

> Verify that the trade I just created appears in the blotter with status Booked.

### Scenario F — Context Menu

> Open the audit history for the latest FHLB trade.

Right-click the blotter row and choose **View Audit History**.

## Using Cursor / Codex + MARS Java Automation MCP

Start the workstation, attach MARS Java Automation MCP to the running JVM, then give the agent a business instruction. The agent should resolve labels, named controls, tables, menus, and dialogs without asking for coordinates.

Examples:

> Open the Northstar application and create a USD bond trade for 100m notional with FHLB at 98.25. Validate the trade, save it, and tell me the resulting Trade ID.

> Find the most recent FHLB bond trade in the blotter and verify that its status is Booked.

> Create a trade with price 250 and verify that the application prevents it from being booked.

The user can speak in business terms:

```text
Book 100m USD with FHLB at 98.25.
```

The agent interprets the task. MARS operates the Swing controls. The UI exposes deterministic state for verification: Trade ID, Status, Counterparty, Notional, Currency, Price, Settlement Location, and the booking confirmation dialog.

## Keyboard

- `Ctrl+N` — New Trade
- `Ctrl+S` — Save
- `F5` — Refresh
- `Tab` / `Shift+Tab` — focus traversal
- `Escape` — clear status to Ready

## Tests

```bash
mvn test
```

Covered business rules: notional, price, settlement date, and trade booking.

## Project Layout

```text
MarsJavaDemo/
├── pom.xml
├── README.md
├── doc/                 specification
├── docs/                agent demo scenarios
├── sample-data/         static seed excerpts
└── src/
```
