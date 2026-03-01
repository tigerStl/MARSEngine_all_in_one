# Java UI Automation User Guide (English)

## 1. Scope

This guide is for release users and covers:

- Installation and prerequisites
- End-to-end scan / record / replay workflow
- Common keyword behavior
- License modes (test / paid / limited trial)
- Troubleshooting and diagnostics

---

## 2. Prerequisites

- VS Code `1.85+`
- Node.js `18+`
- JDK `17+` (JDK required, not JRE)
- .NET SDK `8+` (for `ProcessInfo`)
- Maven `3.6+`

Recommended:

- `JAVA_HOME` points to a valid JDK
- `JAVA_HOME/bin/jcmd(.exe)` is available (better Java process naming)

---

## 3. Build and Setup

Run in extension root:

```bash
npm install
npm run compile
cd java && mvn -q -DskipTests package
cd ..
cd ProcessInfo && dotnet publish -c Release
cd ..
```

---

## 4. Panel and Main Actions

Open the bottom panel: `Java UI Automation`.

Toolbar actions:

- `Java Applications`: scan Java process list
- `Record & Replay`: start/stop recording
- `Execute`: replay current test steps
- `Save/Load`: save/load step files
- `Diag`: export diagnostics bundle
- `Refresh`: refresh object list

---

## 5. Scan Workflow

1. Start target Java app (Swing/AWT)
2. Click `Java Applications`
3. Select a process in dropdown
4. Double-click process to scan
5. Inspect objects in left tree and `Object Info`
6. Double-click object to highlight on target window

Dropdown source marker:

- `[jcmd]`: from `jcmd -l` (preferred)
- `[fallback]`: fallback detection

---

## 6. Record and Replay

### 6.1 Record

1. Select target process
2. Click `Record & Replay` to start
3. Perform actions on target app
4. Click `Record & Replay` again to stop
5. Steps appear in `Test Steps`

### 6.2 Replay

1. Review/edit steps in `Test Steps`
2. Click `Execute` for batch replay
3. Use per-row action for single-step replay

---

## 7. Common Keywords

- `FillEdit`
- `SelectDropList` / `SelectDropDown`
- `SelectTreeList`
- `SelectTab`
- `SelectMenuItem` / `SelectPopupMenu` / `SelectListItem`
- `SetRadioBox`
- `SetCheckBox`
- `ClickAT`

Right-click behavior:

- A right-click business step appends a `ClickAT` step
- Related `Select*` steps append `rightclick` in `parameter`
- Replay moves to selected item position before right-click

Menu stability behavior:

- `SelectMenuItem/SelectPopupMenu/SelectListItem` replay includes a default `1s` wait

---

## 8. License Rules (Release)

### 8.1 Modes

- `TEST`
- `PAID`
- `TRIAL_LIMITED`

### 8.2 Limited Trial Rule

- First `7` days: replay not limited by step count
- After `7` days: recording can exceed `30` steps, replay is limited to `10` steps
- When replay exceeds `10` steps: upgrade prompt is shown

### 8.3 Pricing

- US: `$4.99`
- CN: `CNY 5`

### 8.4 Test Pool

- Total `400` test licenses
- US `200`, CN `200`
- Controlled by License Server

### 8.5 License Status in UI

Top bar shows:

- license type
- region
- price hint
- tooltip details (trial policy + US/CN pool remaining)

---

## 9. Minimal License Server

Config key:

- `loaniq.licenseServerUrl` (default `http://127.0.0.1:8787`)

Main endpoints:

- `GET /v1/license/client-state`
- `GET /v1/license/policy`
- `GET /v1/license/declaration?lang=en|zh`
- `POST /v1/license/test/claim` (admin)

Client sync outputs:

- `scanedfiles/license.latest.json`
- `scanedfiles/license.declaration.latest.txt`

---

## 10. Output Files

Under extension `scanedfiles/`:

- `objects.json`
- `script.json` / `script-*.json`
- `processes-latest.json`
- `license.latest.json`
- `license.declaration.latest.txt`

---

## 11. Troubleshooting

### Q1: Process dropdown still shows generic `java -jar`

- Check if entry shows `[jcmd]`
- Verify `JAVA_HOME/bin/jcmd(.exe)` is callable
- If still `[fallback]`, export diagnostics and inspect logs

### Q2: Menu remains visible after replay

- Current build uses real mouse simulation + default 1s wait
- If reproducible, provide diagnostics bundle

### Q3: VS Code status bar shows `Java: Activating...` for long time

- Run `Java: Clean Java Language Server Workspace`
- Verify Java extension and JDK setup

---

## 12. Diagnostics Export

Click `Diag` to export:

- panel log
- steps JSON
- recent recording logs
- runtime config summary

Use this bundle when reporting issues.

