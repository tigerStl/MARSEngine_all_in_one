# MCP Chat Demo Test Script (Windows)

> Purpose: Demo all 13 MCP tools through chat.
> Usage: Send each sample prompt to Chat in order, then verify expected tool call and result fields.

---

## 0. Preconditions

- Demo Java app is running and visible
- Extension is loaded and panel can be opened
- Commands available: `javaUiAutomation.mcp.callTool`, `javaUiAutomation.mcp.callToolInteractive`
- Build passed: `npm run compile`

---

## 1. Demo Flow (10-15 minutes)

1. List processes
2. Select process
3. Start recording
4. Stop recording
5. Get object tree
6. Get steps
7. Update step
8. Execute single step
9. Run replay
10. Highlight object
11. Export objects
12. Export diagnostics
13. Get last errors

---

## 2. Chat-by-Chat Script

### 2.0 If Chat uses terminal instead of MCP tool

Use one of the following retry prompts:

- `Do not run powershell/terminal. Call MCP tool only: mars-list-processes. Return raw JSON.`
- `You must use MCP tool only, no shell execution. Call mars-list-processes with requestId=demo-r1.`
- `Call in order: 1) mars-list-processes 2) no terminal command 3) return top 5 data.items.`
- `If mars-list-processes is not available in this session (only functions.*), explicitly say MCP tool is unavailable and do not run shell.`

Fallback path:

- Use Command Palette -> `Java UI Automation: MCP Call Tool (Interactive)`
- Select tool and submit JSON input
- Validate raw JSON structure (`ok/requestId/data/errorCode`)

### 2.1 Quick MCP discovery check (3 steps)

1. Prompt: `List all callable tool names in this chat session.`

- Pass: output includes `mars-list-processes` (or other `mars-*`)

2. If not found, prompt: `When only functions.* are available, do not use terminal; just state mars tools are unavailable.`

- Pass: Chat does not trigger terminal

3. Once found, prompt: `Call mars-list-processes only and return raw JSON.`

- Pass: response includes raw fields (`ok/requestId/data/errorCode`)

---

### Round 1: List Java processes

- User prompt: `List current Java processes`
- Expected tool: `mars-list-processes`
- Expected fields: `ok=true`, `data.items[]` with `pid/displayName`
- Acceptance: demo app PID is visible

---

### Round 2: Select target process

- User prompt: `Select process pid=12345`
- Expected tool: `mars-select-process`
- Input: `{ "pid": 12345 }`
- Expected fields: `ok=true`, `data.selectedPid=12345`
- Acceptance: next object-tree call does not return `MARS_E_NO_PROCESS_SELECTED`

---

### Round 3: Start recording

- User prompt: `Start recording with pid=12345`
- Expected tool: `mars-start-record`
- Input: `{ "pid": 12345 }`
- Expected fields: `ok=true`, `data.status="recording"`, `data.pid=12345`
- Acceptance: panel enters recording state (Stop semantics, process dropdown disabled)

---

### Round 4: Stop recording

- User prompt: `Stop recording`
- Expected tool: `mars-stop-record`
- Expected fields: `ok=true`, `data.status="stopped"`, `data.stepCount` is numeric
- Acceptance: panel exits recording state and step list updates

---

### Round 5: Get object tree

- User prompt: `Scan object tree`
- Expected tool: `mars-get-object-tree`
- Input: `{ "refresh": true }`
- Expected fields: `ok=true`, `data.roots[]`
- Acceptance: window/component hierarchy is visible

---

### Round 6: Get test steps

- User prompt: `Get current test steps`
- Expected tool: `mars-get-steps`
- Expected fields: `ok=true`, `data.steps[]`
- Acceptance: step count matches panel

---

### Round 7: Update one step

- User prompt: `Set expected value of step 3 to Deal JSON`
- Expected tool: `mars-update-step`
- Input example:

```json
{
  "index": 2,
  "patch": {
    "assertValue": "Deal JSON"
  }
}
```

- Expected fields: `ok=true`, updated `data.step`
- Acceptance: step 3 expected value is updated in panel

---

### Round 8: Execute one step

- User prompt: `Execute step 3`
- Expected tool: `mars-execute-step`
- Input: `{ "index": 2 }`
- Expected fields: `ok=true`, `data.status` in `success|failed`, numeric `data.durationMs`
- Acceptance: visible UI action; clear error when failed

---

### Round 9: Run replay

- User prompt: `Replay from step 1 to step 5`
- Expected tool: `mars-run-replay`
- Input example: `{ "fromIndex": 0, "toIndex": 4, "strictParent": true }`
- Expected fields: success `data.status=done`; failure `data.status=failed` with `failedIndex/error`
- Acceptance: replay result is explainable

---

### Round 10: Highlight object

- User prompt: `Highlight object javaType=javax.swing.JButton name=okButton`
- Expected tool: `mars-highlight-object`
- Input example:

```json
{
  "objectKey": {
    "javaType": "javax.swing.JButton",
    "name": "okButton"
  }
}
```

- Expected fields: `ok=true`, `data.message` includes highlight coordinates
- Acceptance: highlight rectangle appears on screen

---

### Round 11: Export objects

- User prompt: `Export objects as json with parents`
- Expected tool: `mars-export-objects`
- Input: `{ "format": "json", "includeParents": true }`
- Expected fields: `ok=true`, valid `data.filePath`
- Acceptance: exported file exists and opens

---

### Round 12: Export diagnostics

- User prompt: `Export diagnostics with logs`
- Expected tool: `mars-export-diagnostics`
- Input: `{ "includeLogs": true }`
- Expected fields: `ok=true`, diagnostics directory path in `data.filePath`
- Acceptance: bundle includes at least `summary.json` and log file

---

### Round 13: Get recent errors

- User prompt: `Get latest 20 errors`
- Expected tool: `mars-get-last-errors`
- Input: `{ "limit": 20 }`
- Expected fields: `ok=true`, `data.items[]` with `ts/scope/message`
- Acceptance: latest failure reason is traceable

---

## 3. Negative Test Cases (run at least 2)

### Case A: Execute without selected process

- Prompt: `Execute step 1`
- Expected: `ok=false`, `errorCode=MARS_E_NO_PROCESS_SELECTED`

### Case B: Invalid step index

- Prompt: `Execute step 9999`
- Expected: `ok=false`, `errorCode=MARS_E_STEP_INDEX_INVALID`

---

## 4. Demo Pass Criteria

- All 13 tools succeed at least once
- At least 2 negative cases return correct error codes
- User can finish full loop in chat: select process -> scan -> update -> execute -> replay -> export

---

## 5. Demo Record Template

- Time:
- Demo app:
- Successful tools:
- Failed tools:
- Typical error codes:
- Top 3 follow-up items:
