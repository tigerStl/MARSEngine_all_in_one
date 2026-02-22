# MCP Demo Test Manual (Windows)

> Scenario: You already launched the demo Java app and want to validate the MARS MCP + fallback flow.

---

## 1. Prerequisites

- VS Code workspace opened: `MarsPlugins/VSCode`
- Extension build passed: `npm run compile`
- Java Agent build passed: `cd java && mvn clean package`
- ProcessInfo build passed: `cd ProcessInfo && dotnet build -c release`
- Demo Java app is running and visible (not headless)

---

## 2. Quick Smoke Test (5 minutes)

1. Open panel

- Command Palette: `Java UI Automation: Show Panel`

2. List processes (maps to `mars-list-processes`)

- Click `Java Applications`
- Expected: Java processes appear in the dropdown

3. Select process (maps to `mars-select-process`)

- Select your demo PID in the dropdown
- Expected: panel log shows process selected

4. Scan object tree (maps to `mars-get-object-tree`)

- Double-click process or trigger scan
- Expected: object tree appears and Object Info shows properties

5. Execute one step (maps to `mars-execute-step`)

- Click Execute on one row in Test Steps
- Expected: step status becomes success/failed with duration/error

---

## 3. 13-Tool Validation Matrix

> MCP routing is implemented. If you do not directly call MCP tools yet, you can validate capabilities via equivalent UI actions.

1. `mars-list-processes`

- Action: click `Java Applications`
- Expected: process list returned

2. `mars-select-process`

- Action: select process in dropdown
- Expected: selectedPid is effective

3. `mars-start-record`

- Action: start recording (with optional pid)
- Expected: recording state enters (Record button switches to Stop semantics, process dropdown disabled)

4. `mars-stop-record`

- Action: stop recording
- Expected: recording state exits, step count updates

5. `mars-get-object-tree`

- Action: scan process
- Expected: `roots` is usually non-empty (depends on app)

6. `mars-highlight-object`

- Action: highlight selected object
- Expected: target region is highlighted (Windows)

7. `mars-get-steps`

- Action: view steps after load/edit
- Expected: list matches panel

8. `mars-update-step`

- Action: edit Parent/Object/Para/Data/Expected columns
- Expected: step updates immediately and can be saved

9. `mars-execute-step`

- Action: execute one row
- Expected: success/failed + duration

10. `mars-run-replay`

- Action: click Execute for full replay
- Expected: replayProgress continues; failed replay includes failedIndex

11. `mars-export-objects`

- Action: export objects
- Expected: JSON export succeeds

12. `mars-export-diagnostics`

- Action: click `Diag`
- Expected: diagnostics bundle generated (summary/log/steps)

13. `mars-get-last-errors`

- Action: trigger one failure, then query errors
- Expected: recent errors are aggregated and traceable

---

## 4. Recommended Minimal Test Data

Prepare 3 steps:

1. `Click` on a visible button
2. `FillEdit` on an editable input
3. `VerifyObjectValue` for assertion

Also add 1 intentionally failing step (wrong parent or wrong object) to validate:

- error-code mapping
- failedIndex
- diagnostics readability

---

## 5. Demo Gate (Pass Criteria)

- Stable flow works: list process -> select process -> start record -> stop record -> scan objects -> execute one step -> run replay
- At least 1 successful step and 1 explainable failed step
- Object export and diagnostics export both succeed
- No crash, freeze, or silent failure

---

## 6. Troubleshooting

1. No process shown

- Confirm demo Java process is still running
- Check ProcessInfo build output

2. Empty scan result

- Ensure target window is visible
- Check agent injection logs

3. Highlight failed

- Windows only
- Target object must contain `screenBounds`

4. Replay failed: Parent object not found

- Check `parentIdentifier` in step data
- Use compare logs to identify field mismatch

---

## 7. Suggested Follow-up

After testing, share:

- success/failure screenshots or log snippets
- `errorCode` / `errorMessage` from failures
- top 2 optimization priorities (stability/speed/log readability)
