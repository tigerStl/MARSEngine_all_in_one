# Java UI Automation Test Extension

VS Code extension for Java application UI automation testing. It uses a JVM Agent to scan AWT/Swing UI elements and supports recording, editing, and replay execution of test scripts in JSON.

## Features

1. **JVM Agent scanning**: Attaches to a running Java process and scans the AWT/Swing UI hierarchy
2. **Multi-process selection**: If multiple Java applications are running, a dialog or panel dropdown lets the user choose one
3. **Object model**: Two-layer structure (parent + object), stored as JSON, each object with a unique name
4. **Locators**: text, caption, name, namePath, javaType, objectTypePath; index used when needed (top-to-bottom, left-to-right)
5. **Keywords**: Edit/TextArea → FillEdit; ComboBox → SelectDropDown
6. **Constants**: All string literals are stored in constants.json; no hardcoded strings in scripts
7. **Process info**: Provided by the ProcessInfo tool (C# .NET Core) in the same repo
8. **Panel**: Dockable bottom panel with **Java Applications** button, process combo, object list, Object Info (x, y, w, h, visible), test steps; state persists when switching tabs
9. **Highlight**: Double-click an object in the list to draw a red flashing box (3 times) at the corresponding position in the target process window
10. **Java Agent logs**: agent-loader logs go to `javaagentLog/` next to its JAR; **unified-agent** (scan+record in one JAR) logs: scan to `javaagentLog/` next to the runtime JAR, record to the **record directory** (recordDir): `record-debug.log`, `toolbutton-tooltips.log`

## Project Structure

```
VSCode/
├── src/                      # VS Code extension
│   ├── extension.ts          # Entry point and commands
│   ├── panelProvider.ts     # Panel (process list, object tree, Object Info, test steps)
│   ├── agentLoader.ts        # Loads unified-agent (uses JAVA_HOME/bin/java)
│   ├── processInfo.ts        # Invokes ProcessInfo for Java processes
│   ├── objectConverter.ts   # Converts scan output to UIObject (bounds, screenBounds, visible)
│   └── scriptGenerator.ts   # Generates test scripts
├── schemas/                  # JSON schemas
├── java/
│   ├── agent-loader/         # Loads unified-agent via Attach API
│   ├── unified-agent/        # Scan + record in one JAR (dispatches to UIScannerAgent / RecordAgent)
│   ├── ui-scanner-agent/     # Scan logic (unified-agent dependency)
│   ├── record-agent/         # Record logic (unified-agent dependency)
│   └── highlight-agent/      # JVM Agent, red flashing box at screen coordinates
├── ProcessInfo/              # C# .NET Core tool to list Java processes
└── package.json
```

## Data and Output Paths

- **Scan and scripts**: Stored under the **extension install directory** in `scanedfiles/` (no workspace required)
- **Java Agent logs**:
  - **agent-loader**: Under `javaagentLog/` next to its JAR (e.g. `java/agent-loader/target/javaagentLog/agent-loader.log`)
  - **unified-agent**: Extension copies the JAR to a temp dir before load; scan logs under `javaagentLog/` next to that JAR, record logs in the **record directory** (recordDir): `record-debug.log`, `toolbutton-tooltips.log`
    - Temporary mouse-click trace switch (position/object class/parent class): `-Dmars.record.mouse.click.trace.enabled=true|false` (default `true`; set to `false` to disable this temporary trace)

## Requirements vs Implementation

| Requirement | Implementation |
|-------------|----------------|
| JVM Agent scanning | UIScannerAgent attaches via agentmain and scans Window/Frame and children |
| Multi-process selection | ProcessInfo lists Java processes; panel **Java Applications** button + combo/list |
| Two-layer object structure | parent + identifier; supports text, caption, name, javaType, bounds, screenBounds, visible |
| Index mode | Sorted by bounds top-to-bottom, left-to-right; index used when multiple matches |
| FillEdit / SelectDropDown | Inferred from javaType: JTextField/JTextArea → FillEdit, JComboBox → SelectDropDown |
| Constants | All strings in constants.json; scripts reference constant ids only |
| Script replay execution | Supports replay execution from Test Steps; editable in Visual/Test Steps before execution |
| ProcessInfo | Separate C# project; uses WMI (Windows) and /proc (Linux) for process info |
| Panel state persistence | Processes, objects, steps, log saved to workspaceState; restored when switching tabs |
| Object highlight | Double-click object → load highlight-agent, draw red box flash 3× at screen coords |

## Script Format Example

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "username" },
      "data": "CONST_TEST_USER_ABC123",
      "assertValue": null
    }
  ]
}
```

## VerifyObjectValue Examples

- If a step has `assertValue` (Expected), verification runs automatically after that step.
- You can also add an explicit `VerifyObjectValue` step for standalone verification.

### Example 1: Regular component (JTextField)

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtCustomer" },
      "data": "ACME-001",
      "assertValue": "ACME-001"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtCustomer" },
      "parameter": "",
      "data": "ACME-001",
      "assertValue": ""
    }
  ]
}
```

### Example 2: JTable verification

```json
{
  "steps": [
    {
      "keyword": "SearchAndUpdate",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTable", "name": "loanTable" },
      "parameter": "Amount:[Deal]",
      "data": "[D-1001];1000:1500",
      "assertValue": "1500"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTable", "name": "loanTable" },
      "parameter": "Amount",
      "data": "1500",
      "assertValue": ""
    }
  ]
}
```

### Example 3: Regex-based verification

Both `VerifyObjectValue` and `assertValue` support regex matching (Java Pattern).

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtRef" },
      "data": "ACME-2026-001",
      "assertValue": "^ACME-\\d{4}-\\d{3}$"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtRef" },
      "parameter": "",
      "data": "^ACME-\\d{4}-\\d{3}$",
      "assertValue": ""
    }
  ]
}
```

## Constants File Example

```json
{
  "constants": [
    { "id": "CONST_TEST_USER_ABC123", "value": "test@example.com", "category": "INPUT_DATA" }
  ]
}
```

## Build and Run

1. **Install and compile extension**: `npm install`, `npm run compile`
2. **Build Java projects**: `cd java && mvn package` (agent-loader, ui-scanner-agent, highlight-agent)
3. **Build ProcessInfo**: `dotnet publish -c Release` in ProcessInfo/
4. **Run**: Press F5 to start Extension Development Host; open the **Java UI Automation** bottom panel, click **Java Applications** to get processes, double-click a process to scan, double-click an object to highlight; or use Command Palette **Java UI: Select Java Process and Scan** / **Java UI: Generate Test Script**

## Prerequisites

- Node.js 18+
- Java 17+ (JDK with Attach API); extension invokes `JAVA_HOME/bin/java` (or `java.exe` on Windows)
- .NET 8 SDK (for ProcessInfo)
- Maven 3.6+
- VS Code 1.85+
