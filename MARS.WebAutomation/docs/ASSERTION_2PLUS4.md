# Assertion authoring: “2 + 4” mode

This document captures the product/design discussion for **structured assertions after recording (2)** plus **visual / computed-style checks where semantics are insufficient (4)**, and how it is implemented in MARS.WebAutomation.

## Goals

- Reduce manual entry when asserting **many elements** (readonly, disabled, visibility, counts).
- Keep **When** (recorded actions) separate from **Then** (assertions), but make **Then** cheap to author from the live DOM.
- Support **before / after** DOM snapshots (e.g. after changing a field) and **automatic diff → steps**.
- Record **efficiency metrics** for capture and diff (milliseconds, element counts) to monitor cost as pages grow.

## Mode “2”: snapshot → assertion steps

1. User performs actions (record as today).
2. **Before** state: global hotkey *Assert snapshot — Before* captures a **surface list** of interactive controls (inputs, selects, textareas, buttons, links with `href`, elements with `role`, `contenteditable`).
3. User triggers the UI change (or continues recording).
4. **After** state: global hotkey *Assert snapshot — After & diff* captures again, **diffs** against the pending *Before* snapshot, and **inserts** steps:
   - **`AssertElementState`**: expected `ReadOnly`, `Disabled`, optional `Color` / `BackgroundColor` (from `getComputedStyle`), `AriaDisabled`, `AriaReadonly`, `ContentEditable`.
   - **`AssertLocatorCount`**: for **new** signatures seen only in *After* (treated as “appeared”), `Expected=1`.
   - Optionally **`AssertScreenshot`** when enabled and a **color** change is detected (mode “4” bridge).

Captured locators prefer **stable CSS** (`id`, `data-testid`, `name`, compound selectors). `FramePath` is stored in `Parameter` when the node is not in the main frame so replay resolves the correct `IFrame`.

### Pitfalls (explicitly handled)

| Risk | Mitigation |
|------|------------|
| Wrong tab / page | Snapshots run on **ordered real pages** (active page first); URL/title stored on pending *Before* and **warned** if *After* differs. |
| Async DOM half-updated | Configurable **`AssertSnapshotSettleMs`** wait after `DOMContentLoaded` before evaluating the script. |
| Huge DOM / slow diff | **Cap** on elements per frame (`AssertSnapshotMaxElementsPerFrame`, default 500); metrics show counts so tuning is evidence-based. |
| Fragile color asserts | Color is **optional**; prefer readonly/disabled/ARIA. Screenshot asserts are **opt-in** via **`AssertDiffEmitScreenshotOnColorChange`**. |
| Hotkey conflicts | Separate modifier/key defaults from record/replay hotkey; persisted in settings. |

## Mode “4”: screenshot baseline compare

Keyword **`AssertScreenshot`**:

- `Parameter`: `BaselineRelativePath=...;MaxDiffRatio=0.02` (optional), `FramePath=...` when needed.
- Baselines live under `{DataRootFolder}\assert_baselines\`.
- First run with a missing baseline **writes** the baseline PNG (establish mode). Later runs **compare** pixels with a channel threshold and **fail** if the differing pixel ratio exceeds `MaxDiffRatio`.
- `DataReturned` on success includes **`CompareMs=`** for runtime visibility.

Use for gradients, icons, or theme-driven visuals where DOM state is not enough.

## Efficiency metrics (before/after hotkey diff)

If the diff produces **no** new steps, the **Before** snapshot is **kept** so you can stabilize the UI and press *After* again without recapturing *Before*.

Each *After & diff* run records:

- `BeforeCaptureMs`, `AfterCaptureMs` — wall time to collect all frames on the chosen pages.
- `BeforeElementCount`, `AfterElementCount`.
- `DiffMs` — pure in-process diff + step materialization (no browser).
- `StateChangeCount`, `NewElementCount`, optional `ScreenshotStepsAdded`.

These are written to **NLog** (`FormLog.Info`) and shown in the **status bar** / confirmation dialog so regressions in performance are visible.

## Settings (Workbench)

| Setting | Purpose |
|---------|---------|
| `AssertSnapshotSettleMs` | Delay after navigation settle before capture. |
| `AssertSnapshotMaxElementsPerFrame` | Hard cap per frame for capture. |
| `AssertDiffEmitScreenshotOnColorChange` | When true, append `AssertScreenshot` for color-only diffs. |
| `AssertHotkeyBefore*` / `AssertHotkeyAfter*` | Global hotkeys (modifiers + F8–F12). |

## Related code

- `DomAssertionSnapshotService` — capture + diff + step building.
- `DomAssertionCaptureScripts` — in-page JSON collector.
- Keywords: `AssertElementState`, `AssertLocatorCount`, `AssertScreenshot`.
- `MainWorkbenchForm` — hotkey registration, pending snapshot state, step insertion.
