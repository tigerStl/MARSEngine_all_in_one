# Agent Demo Scenarios

These scenarios are the acceptance path for Cursor / Codex + MARS Java Automation MCP against Northstar Capital Markets Workstation.

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

Useful control names:

- `bond.notional`
- `bond.currency`
- `bond.counterparty`
- `bond.price`
- `settlementCombo` (label: Settlement Location)
- `bond.validate`
- `bond.save`
- `bond.validationStatus`
- `dialog.tradeBooked.tradeId`

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

## Scenario C — Validation Failure

User:

> Create a 100m USD trade at price 250 and save it.

Application must reject price.

Expected Agent result:

```text
Validation FAILED
Price must be less than 200
```

Save remains disabled. The agent should report that the trade could not be booked.

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

Dialogs:

- `dialog.securitySearch`
- `dialog.counterpartySearch`

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

Table name: `blotter.table`

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

Popup name: `blotter.contextMenu`  
Menu item: `blotter.menu.audit`  
Dialog: `dialog.tradeAudit`

## Acceptance instruction

> Create a USD bond trade for 100 million notional with FHLB as counterparty, price 98.25, settlement location NYC, validate it, save it, and verify that it appears in the Trade Blotter as Booked.

After execution the agent should be able to read:

```text
Trade ID
Status
Counterparty
Notional
Currency
Price
Settlement Location
```
