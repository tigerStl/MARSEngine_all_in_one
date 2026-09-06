# MARS Financial Desktop Demo — Development Specification for Cursor / Codex

## 1. Purpose

Build a realistic **enterprise capital-markets Java desktop demo application** that can be driven by:

- Cursor Agent
- Codex
- Other MCP-capable agents
- MARS Java Automation MCP

The application should visually and behaviorally resemble the **general class of dense front-office / middle-office financial desktop systems** used for trading, treasury, risk, operations, and trade support.

This is **not** intended to copy or reproduce the proprietary UI of any specific vendor product. Do not use vendor trademarks, logos, copyrighted screenshots, proprietary screen names, or copied layouts. The goal is to reproduce the **interaction patterns and information density** commonly found in enterprise trading applications.

The primary use case is an end-to-end AI Agent demo:

```text
User business instruction
        ↓
Cursor / Codex
        ↓
Reasoning and planning
        ↓
MARS Java Automation MCP
        ↓
Java object resolution
        ↓
Deterministic UI operation
        ↓
Verification
        ↓
Pass / Fail + evidence
```

Example user instruction:

> Create a USD fixed-income trade for 100,000,000 notional with FHLB as counterparty, price 98.25, settlement location NYC, validate it, and save the trade.

The AI Agent should be able to discover the application, identify the correct business fields, operate the UI through MARS MCP, and verify the resulting trade.

---

# 2. Product Name

Use the fictional product name:

**Northstar Capital Markets Workstation**

Optional short name:

**Northstar CM**

Do not use Murex, Summit, Calypso, Bloomberg, Refinitiv, Finastra, or any other commercial product branding inside the application.

---

# 3. Technology Stack

## Required

- Java 17+
- Maven
- Swing for the primary application
- Standard Java libraries wherever possible
- No database required for the first version
- Data may be held in memory and optionally persisted as JSON

## Optional Phase 2

Add a JavaFX module or JavaFX screen so that the same demo repository can test both:

- Swing automation
- JavaFX automation

Suggested structure:

```text
northstar-financial-demo/
├── pom.xml
├── README.md
├── src/main/java/
│   └── com/northstar/capital/
│       ├── Main.java
│       ├── model/
│       ├── service/
│       ├── ui/
│       │   ├── MainFrame.java
│       │   ├── trade/
│       │   ├── blotter/
│       │   ├── risk/
│       │   ├── reference/
│       │   └── common/
│       └── data/
└── src/main/resources/
```

---

# 4. Design Goals

The UI should look like a serious enterprise financial workstation rather than a consumer application.

Prioritize:

1. High information density
2. Multiple panels
3. Menus and toolbars
4. Tabbed workspaces
5. Large tables
6. Trade-entry forms
7. Status indicators
8. Validation messages
9. Modal dialogs
10. Context menus
11. Editable tables
12. Drop-down controls
13. Checkboxes and radio buttons
14. Date fields
15. Numeric fields
16. Dynamic enable/disable behavior

The goal is to provide enough complexity to meaningfully test MARS Java Automation MCP.

---

# 5. Overall Window Layout

Recommended application size:

```text
1440 x 900
```

Minimum:

```text
1280 x 800
```

Main structure:

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Northstar Capital Markets Workstation                     User: TRADER01   │
├─────────────────────────────────────────────────────────────────────────────┤
│ File  Trade  Market  Risk  Operations  Reference Data  Tools  Help         │
├─────────────────────────────────────────────────────────────────────────────┤
│ [New Trade] [Validate] [Save] [Clone] [Cancel] [Refresh] [Search]         │
├──────────────┬──────────────────────────────────────────────────────────────┤
│ Navigation   │ Workspace                                                    │
│              │ ┌──────────────────────────────────────────────────────────┐ │
│ Dashboard    │ │ Trade Entry | Blotter | Positions | Risk | Exceptions │ │
│ Trading      │ ├──────────────────────────────────────────────────────────┤ │
│  Fixed Income│ │                                                          │ │
│  FX          │ │                 ACTIVE WORKSPACE                         │ │
│  Rates       │ │                                                          │ │
│ Risk         │ │                                                          │ │
│ Operations   │ │                                                          │ │
│ Reference    │ └──────────────────────────────────────────────────────────┘ │
├──────────────┴──────────────────────────────────────────────────────────────┤
│ Environment: UAT | Server: NCM-UAT-01 | Connected | 14:32:19 | Ready     │
└─────────────────────────────────────────────────────────────────────────────┘
```

Use a professional desktop theme.

Avoid overly colorful consumer-style UI.

Recommended appearance:

- Dark blue / charcoal menu and header areas
- Light gray workspace
- White data-entry panels
- Compact fonts
- Dense tables
- Subtle separators
- Clear status indicators

---

# 6. Main Navigation

Left-side navigation tree:

```text
Dashboard

Trading
├── Fixed Income
│   ├── Bond Entry
│   ├── Repo
│   └── Security Search
├── Foreign Exchange
│   ├── FX Spot
│   ├── FX Forward
│   └── FX Swap
└── Rates
    ├── Interest Rate Swap
    └── FRA

Trade Management
├── Trade Blotter
├── Amendments
└── Cancellations

Risk
├── Positions
├── Market Risk
└── Limits

Operations
├── Settlements
├── Confirmations
└── Exceptions

Reference Data
├── Counterparties
├── Securities
├── Books
└── Settlement Instructions
```

Use `JTree` for Swing.

The tree must be automatable.

---

# 7. Core Demo Screen — Bond Trade Entry

This is the most important screen.

Tab name:

**Bond Trade Entry**

Layout:

```text
┌─ Trade Header ────────────────────────────────────────────────────────────┐
│ Trade ID      [ NEW           ]    Status        [ Draft               ] │
│ Trade Date    [ 09/05/2026    ]    Trader        [ TRADER01            ] │
│ Book          [ FI_USD_BOOK ▼ ]    Legal Entity  [ NORTHSTAR BANK NA ▼] │
└───────────────────────────────────────────────────────────────────────────┘

┌─ Product ─────────────────────────────────────────────────────────────────┐
│ Instrument    [ US Treasury 5.00 2035        ][Search...]                │
│ Security ID   [ US91282ABC12                  ]                          │
│ Side          (● Buy) (○ Sell)                                           │
│ Notional      [ 100000000                   ] Currency [ USD ▼ ]         │
│ Price         [ 98.2500                     ] Clean/Dirty [ Clean ▼ ]    │
│ Yield         [ 5.214                       ]                            │
└───────────────────────────────────────────────────────────────────────────┘

┌─ Counterparty & Settlement ────────────────────────────────────────────────┐
│ Counterparty  [ FHLB                         ][Lookup...]                 │
│ Settlement Dt [ 09/08/2026                   ]                           │
│ Settle Loc    [ NYC ▼                        ]                           │
│ SSI           [ FHLB_USD_NYC_01 ▼            ]                           │
│                                                                              │
│ [x] Use standard settlement instructions                                  │
│ [ ] Manual settlement override                                             │
└──────────────────────────────────────────────────────────────────────────────┘

┌─ Economics ───────────────────────────────────────────────────────────────┐
│ Gross Amount      [ 98,250,000.00 ]                                      │
│ Accrued Interest  [    725,000.00 ]                                      │
│ Net Settlement    [ 98,975,000.00 ]                                      │
└───────────────────────────────────────────────────────────────────────────┘

┌─ Workflow ────────────────────────────────────────────────────────────────┐
│ [Validate Trade]    [Save Trade]    [Cancel]                             │
│                                                                            │
│ Validation: Ready                                                        │
└───────────────────────────────────────────────────────────────────────────┘
```

---

# 8. Required Swing Controls

The Bond Trade Entry screen must include at least:

- `JTextField`
- `JFormattedTextField`
- `JComboBox`
- `JCheckBox`
- `JRadioButton`
- `JButton`
- `JLabel`
- `JTabbedPane`
- `JPanel`
- `JTable`
- `JTree`
- `JMenu`
- `JMenuItem`
- `JPopupMenu`
- `JDialog`
- `JOptionPane`
- `JSpinner`
- `JScrollPane`

This is important because the demo is also an automation testbed.

---

# 9. Object Naming

Every meaningful business control should have a stable programmatic name using:

```java
component.setName("...");
```

Examples:

```text
bond.tradeId
bond.tradeDate
bond.trader
bond.book
bond.legalEntity

bond.instrument
bond.securityId
bond.side.buy
bond.side.sell
bond.notional
bond.currency
bond.price
bond.priceType
bond.yield

bond.counterparty
bond.settlementDate
bond.settlementLocation
bond.ssi
bond.useStandardSSI
bond.manualSettlementOverride

bond.grossAmount
bond.accruedInterest
bond.netSettlement

bond.validate
bond.save
bond.cancel
bond.validationStatus
```

Labels should also have names:

```text
label.bond.notional
label.bond.currency
label.bond.counterparty
label.bond.settlementLocation
```

This allows MARS MCP to compare:

- direct control properties
- labels
- geometry
- parent containers
- visible text

---

# 10. Important Requirement: Do Not Make Automation Too Easy

The demo must include some real enterprise UI complexity.

Do not make every field trivially discoverable only by a perfect unique ID.

Create a mixture of:

## Type A — Fully identified controls

Example:

```text
bond.notional
```

## Type B — Label-associated controls

Example:

Visible label:

```text
Settlement Location
```

Combo box with generic internal name:

```text
settlementCombo
```

## Type C — Repeated field types

Example:

Several `JTextField` controls in the same screen.

The Agent/MARS combination must resolve the correct field from business context.

## Type D — Dynamic controls

Examples:

- Manual Settlement fields appear only when a checkbox is selected.
- Additional pricing fields appear when "Advanced" is enabled.
- Save is disabled until validation succeeds.

This makes the demo realistic.

---

# 11. Business Rules

Implement simple but realistic business validation.

## Notional

Required.

Must be:

```text
> 0
```

For demo purposes:

```text
<= 5,000,000,000
```

## Currency

Required.

Values:

```text
USD
EUR
GBP
JPY
CAD
CHF
```

## Counterparty

Required.

Valid demo counterparties:

```text
FHLB
JPM
CITI
GS
BAC
MS
BARC
DB
UBS
```

## Price

Must be:

```text
0 < price < 200
```

## Settlement Location

Values:

```text
NYC
LON
TKY
TOR
ZRH
```

## Settlement Date

Must be equal to or after Trade Date.

---

# 12. Validation Behavior

When the user clicks:

**Validate Trade**

perform validation.

If successful:

```text
Validation: PASSED
```

Enable:

```text
Save Trade
```

If validation fails:

```text
Validation: FAILED — Counterparty is required.
```

Save must remain disabled.

Highlight invalid fields using a subtle border.

The Agent should be able to read the validation result through MARS.

---

# 13. Save Behavior

After successful validation, clicking Save should:

1. Generate a Trade ID:

```text
FI-20260905-000001
```

2. Change status:

```text
Draft
    ↓
Booked
```

3. Add the trade to Trade Blotter.

4. Display modal dialog:

```text
Trade successfully booked.

Trade ID:
FI-20260905-000001
```

Buttons:

```text
[OK]
```

This modal dialog is important for automation testing.

---

# 14. Trade Blotter

Second major screen.

Use `JTable`.

Columns:

```text
Trade ID
Status
Product
Side
Security
Notional
Currency
Price
Counterparty
Trade Date
Settlement Date
Trader
Book
Last Updated
```

Populate with at least 30 fictional trades.

Example:

```text
FI-20260905-000041
Booked
Bond
Buy
UST 5.00 2035
100,000,000
USD
98.250
FHLB
09/05/2026
09/08/2026
TRADER01
FI_USD_BOOK
14:31:02
```

Support:

- sortable columns
- row selection
- double-click
- context menu
- search filter

---

# 15. Trade Blotter Context Menu

Right-click a trade.

Show:

```text
Open Trade
Clone Trade
Amend Trade
Cancel Trade
View Audit History
Export
```

This is intentionally included to test popup menu automation.

When selecting:

```text
View Audit History
```

open a modal dialog.

---

# 16. Audit History Dialog

Dialog title:

**Trade Audit History**

Table:

```text
Timestamp
User
Action
Field
Old Value
New Value
```

Example:

```text
14:21:13 | TRADER01 | CREATE   | Trade    | -     | FI-20260905-000041
14:22:02 | TRADER01 | VALIDATE | Status   | Draft | Validated
14:22:09 | TRADER01 | SAVE     | Status   | Draft | Booked
```

Buttons:

```text
[Close]
```

---

# 17. Security Search Dialog

Click:

```text
Search...
```

beside Instrument.

Open modal:

**Security Search**

Fields:

```text
Security ID      [              ]
Issuer           [              ]
Currency         [ USD ▼        ]
Maturity From    [              ]
Maturity To      [              ]
```

Buttons:

```text
Search
Clear
Cancel
```

Results table:

```text
Security ID
Issuer
Description
Coupon
Maturity
Currency
Type
```

Select row and click:

```text
Use Selected
```

The Bond Trade Entry screen should update.

---

# 18. Counterparty Lookup Dialog

Click:

```text
Lookup...
```

Open:

**Counterparty Search**

Controls:

```text
Name       [              ]
Country    [ USA ▼        ]
Type       [ Bank ▼       ]
Status     [ Active ▼     ]
```

Results:

```text
Code
Name
Country
Type
Rating
Status
```

Examples:

```text
FHLB | Federal Home Loan Bank | USA | Bank | AA+ | Active
JPM  | JPM Demo Bank          | USA | Bank | AA  | Active
CITI | Citi Demo Bank         | USA | Bank | A+  | Active
```

Use fictional names where necessary.

---

# 19. Positions Screen

Create a tab:

**Positions**

Table:

```text
Book
Security
Position
Average Price
Market Price
Market Value
Daily P&L
Currency
DV01
```

Use 20+ rows.

Add filters:

```text
Book
Currency
Product
```

---

# 20. Risk Screen

Create:

**Risk Monitor**

Sections:

```text
Portfolio Summary
Market Risk
Limit Utilization
P&L
```

Use tables rather than complex chart dependencies.

Example risk table:

```text
Book          DV01        CS01       VaR       Limit Usage
FI_USD_BOOK   125,000     42,500     1.25M     63%
FI_EUR_BOOK    92,000     33,100     0.88M     54%
```

Include a checkbox:

```text
[x] Auto Refresh
```

Include:

```text
Refresh
```

button.

---

# 21. Operations / Exceptions Screen

Create:

**Settlement Exceptions**

Table:

```text
Exception ID
Trade ID
Severity
Type
Description
Owner
Status
Age
```

Rows should include:

```text
SSI Missing
Settlement Date Mismatch
Counterparty Hold
Confirmation Pending
```

Double-clicking an exception opens an exception details dialog.

---

# 22. Menu Bar

Required menus:

## File

```text
New Trade
Open
Save
Export
Exit
```

## Trade

```text
New Bond Trade
Validate
Clone
Amend
Cancel
```

## Market

```text
Security Search
Market Data
Yield Curves
```

## Risk

```text
Positions
Market Risk
Limits
```

## Operations

```text
Settlement Exceptions
Confirmations
```

## Reference Data

```text
Counterparties
Books
Settlement Instructions
```

## Tools

```text
Preferences
Environment
Diagnostics
```

## Help

```text
User Guide
About
```

Menu interaction must be functional enough for MARS testing.

---

# 23. Toolbar

Add compact toolbar buttons:

```text
New Trade
Validate
Save
Clone
Refresh
Search
```

Toolbar buttons should have tooltips.

---

# 24. Status Bar

Bottom status bar should display:

```text
Environment: UAT
User: TRADER01
Server: NCM-UAT-01
Connection: Connected
Current Time
Message: Ready
```

Status changes should appear during operations:

```text
Validating trade...
Trade validation passed.
Saving trade...
Trade FI-20260905-000001 booked successfully.
```

---

# 25. Agent Demo Scenarios

The completed application must support the following end-to-end scenarios.

---

## Scenario A — Simple Trade Creation

User instruction:

> Create a USD bond trade. Buy 100 million notional, use FHLB as the counterparty, price 98.25, settle in NYC, validate and save it.

Expected Agent behavior:

```text
Open Bond Trade Entry
→ select Buy
→ fill Notional = 100000000
→ Currency = USD
→ Counterparty = FHLB
→ Price = 98.25
→ Settlement Location = NYC
→ Validate
→ Verify PASSED
→ Save
→ Verify booking confirmation
→ capture Trade ID
```

---

## Scenario B — Natural Business Language

User:

> Book 50m of the Treasury at 99.125 against JPM with New York settlement.

The Agent should infer:

```text
Side = Buy
Notional = 50,000,000
Price = 99.125
Counterparty = JPM
Settlement Location = NYC
```

Do not require the user to say:

```text
Click textbox to the right of Notional.
```

---

## Scenario C — Validation Failure

User:

> Create a 100m USD trade at price 250 and save it.

Application must reject price.

Expected Agent result:

```text
Validation FAILED
Price must be less than 200
```

Agent should report that the trade could not be booked.

---

## Scenario D — Lookup

User:

> Create a bond trade using the 2035 US Treasury and FHLB.

Agent may need to:

```text
Open Security Search
→ search "2035"
→ choose Treasury security
→ return to trade
→ open Counterparty Lookup
→ choose FHLB
```

---

## Scenario E — Verify Trade

User:

> Verify that the trade I just created appears in the blotter with status Booked.

Agent:

```text
Open Trade Blotter
→ find Trade ID
→ read Status
→ compare to Booked
```

---

## Scenario F — Context Menu

User:

> Open the audit history for the latest FHLB trade.

Agent:

```text
Open blotter
→ identify latest FHLB trade
→ right click
→ View Audit History
→ verify dialog
```

This scenario tests `JPopupMenu`.

---

# 26. MCP-Oriented UI Requirements

The UI must make it possible to test object resolution using several evidence sources.

Each relevant UI object should expose some combination of:

```text
Class
Name
Visible text
Accessible name if supported
Bounds
Parent
Children
Enabled
Visible
Selected
Value
```

Do not rely exclusively on coordinates.

The application should support Agent/MARS interaction patterns such as:

```text
Business concept
    ↓
Candidate labels / controls
    ↓
Object resolution
    ↓
Action
    ↓
State readback
```

---

# 27. Label-to-Control Association

Make form layouts realistic.

Example:

```text
Notional        [                ]
Currency        [ USD ▼          ]
Counterparty    [                ]
```

MARS may need to determine that a field corresponds to a nearby label.

Therefore:

- Keep label and field in the same logical panel.
- Use consistent alignment.
- Do not explicitly encode every business relationship in a hidden test map.
- Let UI structure and component properties be sufficient for discovery.

---

# 28. Repeated / Ambiguous Controls

Include controlled ambiguity.

For example, both of these sections may contain:

```text
Currency
```

Trade section:

```text
Trade Currency
```

Settlement section:

```text
Settlement Currency
```

This allows testing whether the Agent uses:

- current panel
- field labels
- parent container
- surrounding UI
- business context

instead of selecting the first matching component.

---

# 29. Tabs

Use `JTabbedPane`.

Required tabs:

```text
Bond Trade Entry
Trade Blotter
Positions
Risk Monitor
Settlement Exceptions
```

Optionally allow tabs to be closed and reopened through navigation.

---

# 30. Dynamic UI Behavior

Implement at least three dynamic behaviors.

## A. Manual Settlement Override

Unchecked by default.

When selected, show:

```text
Settlement Account
Custodian
Agent Bank
Special Instructions
```

## B. Advanced Pricing

Checkbox:

```text
Show Advanced Pricing
```

When selected:

```text
Spread
Benchmark
Discount Margin
Accrued Days
```

appear.

## C. Product-dependent controls

If Currency = JPY, display a small informational indicator:

```text
JPY settlement convention applied.
```

---

# 31. Keyboard Interaction

Support standard keyboard behavior:

```text
Tab
Shift+Tab
Enter
Space
Ctrl+S
Ctrl+N
Escape
```

Recommended:

```text
Ctrl+N = New Trade
Ctrl+S = Save
F5 = Refresh
```

This allows keyboard automation testing.

---

# 32. Focus Behavior

Focus should be visually visible.

On opening Bond Trade Entry:

```text
Instrument
```

or

```text
Notional
```

may receive initial focus.

Ensure ordinary Swing focus traversal works.

---

# 33. Data Initialization

On application start load realistic demo data.

At least:

```text
30 bond trades
10 counterparties
15 securities
5 books
5 settlement locations
10 settlement exceptions
20 positions
```

No random data that changes unpredictably between launches unless a fixed seed is used.

Tests must remain reproducible.

---

# 34. Demo Users

Use:

```text
TRADER01
OPS01
RISK01
```

No real people or organizations.

---

# 35. Environment Indicator

Support:

```text
DEV
UAT
PROD
```

Default:

```text
UAT
```

For the demo, PROD should display a warning:

```text
DEMO ENVIRONMENT — NO REAL TRADES
```

No actual external systems are used.

---

# 36. Diagnostics Screen

Under:

```text
Tools → Diagnostics
```

show:

```text
Java Version
Application Version
OS
Current User
Current Environment
Loaded Trade Count
UI Thread Status
```

This is useful for demos involving Codex/Cursor.

---

# 37. Logging

Create console logs for actions such as:

```text
[UI] Open screen: Bond Trade Entry
[TRADE] Validate requested
[TRADE] Validation passed
[TRADE] Saved FI-20260905-000001
[BLOTTER] Trade added
```

Do not depend on logs for the actual automation.

The UI itself must expose the result.

---

# 38. Testability

Create basic JUnit tests for business rules.

Examples:

```text
NotionalValidationTest
PriceValidationTest
SettlementDateValidationTest
TradeBookingServiceTest
```

However, the primary purpose of the project is UI automation through MARS, not unit testing.

---

# 39. Suggested Classes

```text
Main
MainFrame

Trade
Security
Counterparty
Position
RiskRecord
SettlementException

TradeService
ValidationService
ReferenceDataService

NavigationPanel
StatusBar

BondTradeEntryPanel
TradeBlotterPanel
PositionsPanel
RiskMonitorPanel
SettlementExceptionsPanel

SecuritySearchDialog
CounterpartySearchDialog
TradeAuditDialog
ExceptionDetailsDialog
AboutDialog
```

---

# 40. Coding Rules

Cursor/Codex must:

- Keep UI code readable.
- Avoid a single giant `MainFrame.java`.
- Separate model, service, and UI.
- Use Swing EDT correctly.
- Use `SwingUtilities.invokeLater`.
- Avoid blocking the UI thread.
- Avoid unnecessary third-party frameworks.
- Ensure the application starts with one Maven command.

Required:

```bash
mvn clean package
```

Then make the application runnable.

Preferred:

```bash
mvn exec:java
```

or produce an executable JAR.

---

# 41. README Requirements

Generate a README with:

## Build

```bash
mvn clean package
```

## Run

Provide the exact command.

## Demo Scenarios

Include all six Agent scenarios from this document.

## MARS MCP Demo

Add a section:

```text
Using Cursor / Codex + MARS Java Automation MCP
```

with examples such as:

> Open the Northstar application and create a USD bond trade for 100m notional with FHLB at 98.25. Validate the trade, save it, and tell me the resulting Trade ID.

Another:

> Find the most recent FHLB bond trade in the blotter and verify that its status is Booked.

Another:

> Create a trade with price 250 and verify that the application prevents it from being booked.

---

# 42. Important Visual Requirement

The result should look closer to a professional capital-markets workstation than a Java tutorial project.

Avoid:

- huge buttons
- excessive whitespace
- cartoon icons
- rounded mobile-style cards
- oversized text
- consumer dashboard appearance

Prefer:

- compact controls
- dense grids
- grouped forms
- thin borders
- professional menu/toolbar layout
- many visible business fields
- information-rich tables

---

# 43. Optional JavaFX Phase

After the Swing version works, add a second executable:

```text
NorthstarFX
```

It should reproduce a smaller subset of functionality using JavaFX.

Required JavaFX controls:

```text
TabPane
TextField
ComboBox
CheckBox
RadioButton
Button
TableView
TreeView
ContextMenu
MenuBar
Dialog
```

Implement:

```text
Bond Trade Entry
Trade Blotter
```

only.

This gives MARS a controlled comparison environment:

```text
Same business task
      ↓
Swing implementation
vs
JavaFX implementation
```

The same Agent instruction should ideally work against either application.

---

# 44. Optional Automation Challenge Screen

Create a hidden/advanced demo page:

```text
Tools → Automation Lab
```

Include:

- nested panels
- repeated labels
- several text fields
- dynamic tab content
- disabled controls
- popup menu
- modal dialog
- checkbox inside tab
- combo popup
- editable table cell

Purpose:

stress-test object discovery and event handling.

This screen should not affect normal trading workflows.

---

# 45. Acceptance Criteria

The project is complete when all conditions below are true.

### Application

- Starts successfully.
- Has professional enterprise financial UI.
- Contains no commercial vendor branding.
- Uses realistic fictional financial data.

### Swing

- Main application is Swing.
- Major Swing controls are present.
- Popup menus and dialogs work.
- Tables work.
- Tabs work.
- Navigation tree works.

### Trading

- Bond trade can be entered.
- Validation works.
- Save is blocked before successful validation.
- Successful save generates Trade ID.
- Saved trade appears in blotter.

### Agent Demo

The following natural-language instruction can be executed using Cursor/Codex + MARS MCP:

> Create a USD bond trade for 100 million notional with FHLB as counterparty, price 98.25, settlement location NYC, validate it, save it, and verify that it appears in the Trade Blotter as Booked.

### Verification

Agent should be able to retrieve:

```text
Trade ID
Status
Counterparty
Notional
Currency
Price
Settlement Location
```

from the application after execution.

---

# 46. Final Deliverables

Cursor/Codex should generate:

```text
northstar-financial-demo/
├── pom.xml
├── README.md
├── src/
├── sample-data/
└── docs/
    └── agent-demo-scenarios.md
```

Optional:

```text
screenshots/
```

Do not include proprietary screenshots or vendor assets.

---

# 47. Recommended Implementation Sequence

Implement in this exact order:

```text
1. Maven project
2. Main frame
3. Menu + toolbar + navigation + status bar
4. Bond Trade Entry
5. Validation logic
6. Save / booking workflow
7. Trade Blotter
8. Security Search
9. Counterparty Search
10. Context menu + audit dialog
11. Positions
12. Risk
13. Settlement Exceptions
14. Demo data
15. README
16. Agent demo scenarios
17. Optional JavaFX version
18. Automation Lab
```

After each major stage:

```text
Compile
→ Run
→ Fix errors
→ Verify visible behavior
```

Do not generate the entire implementation without compiling intermediate stages.

---

# 48. Cursor / Codex Master Instruction

Use the following as the implementation command:

> Build the complete Northstar Capital Markets Workstation according to this specification. Treat this as an enterprise desktop application, not a toy UI. Implement the Swing version first. Compile and run after each major milestone. Keep the code modular. Use realistic but fictional capital-markets data. Do not copy proprietary vendor UI, logos, product names, or screenshots. The completed application will be operated by AI Agents through the MARS Java Automation MCP, so controls, dialogs, tables, menus, state changes, validation messages, focus, and dynamic UI behavior must be real and functional. The application must support all acceptance scenarios defined in this specification.

---

# 49. Why This Demo Exists

This project is designed to demonstrate the architecture:

```text
        User
          │
   Business Language
          │
          ▼
  Cursor / Codex Agent
          │
       Planning
          │
          ▼
     MARS Java MCP
          │
  Object Resolution
          │
          ▼
  Deterministic Action
          │
          ▼
 Northstar Java GUI
          │
          ▼
  Verification / Evidence
```

The demo should make clear that the AI Agent does not need the user to describe screen coordinates or low-level UI objects.

The user can speak in business terms:

```text
"Book 100m USD with FHLB at 98.25."
```

The Agent interprets the task.

MARS resolves and operates the actual Java controls.

The application exposes deterministic state for verification.

This is the core demonstration objective.
