# MARS.WebAutomation — Status tracking (WBS)

Last updated: 2026-05-11

## WBS and completion status

| ID | Task | Status | Notes |
|----|------|--------|-------|
| W1 | Set up .NET Framework 4.7.2 class library and NuGet (Playwright, Newtonsoft.Json) | Done | SDK-style `net472` class library; PackageReference participates in build |
| W2 | Author doc/需求.md, doc/详细设计.md, README.md | Done | |
| W3 | Data model and `data\[domain]_key\test_[url].json` persistence | Done | `DataPathHelper` + `JsonDataStore` |
| W4 | PlaywrightHost + navigation + URL resolution tab | Done | Target page tab |
| W5 | Object tree capture and Object tab (Tree + Grid) | Done | `PageInspectionScripts` + split layout |
| W6 | Recording (Binding + InitScript), semantic mapping, Record tab | Done | `RecordingService` + DataGridView |
| W7 | Replay engine and toolbar integration | Done | `ReplayService` |
| W8 | XHR/Fetch capture and JSON persistence | Done | `NetworkCaptureService` |
| W9 | Settings tab and configurable data root | Done | `%AppData%\MARS.WebAutomation\settings.json` |
| W10 | Main form: Menu, toolbar zones, Export/Import/Save | Done | `MainWorkbenchForm` |
| W11 | Record tab: split layout + step grid columns (delete / order / elapsed / event / bounds / logical kind) | Done | 2026-04-26 |
| W12 | Record tab: WebView workflow canvas, keyword styling, node drag, `CanvasX/Y` persisted | Done | 2026-04-26 |
| W13 | Recorder script: `blur` text commit, suppress noisy click/input, full CSS locator, `LogicalKind` | Done | `recorder.install.js` + `RecordingService` |
| W14 | Step row order: context move up/down, renumbering, sync with canvas | Done | 2026-04-26 |
| W15 | Docs: incremental updates to 需求.md / 详细设计.md / 功能列表.md / 状态追踪.md for Record enhancements | Done | 2026-04-26 |
| W16 | Recording: `SelectTab` semantics, configurable tab ancestor depth, text `Data` fill; object grid last row `outerHTML` | Done | 2026-04-26 |
| W17 | Recording capture modes (semantic / plain), menu + toolbar toggle, `PlaywrightScript`, `__marsRecoCaptureMode`; `semi` self-root when no parent semantic; docs §9 / §10.7 | Done | 2026-05-11 |

## Change log

- 2026-04-23: Created WBS and initial doc skeleton.
- 2026-04-23: Completed main form and external entry points; project moved to SDK-style `net472` so NuGet references compile under the legacy csproj pattern; `dotnet build` passes.
- 2026-04-26: Record/Replay workbench enhancements (split layout, workflow canvas, semantic recording, step grid columns); see `doc/需求.md` §8 and `doc/详细设计.md` §10.
- 2026-04-26: `SelectTab` recording semantics, setting `RecorderTabContextAncestorDepth`, `SemanticStepRecord` Target* fields, object grid last row `outerHTML`; see `doc/详细设计.md` §10.2–10.6.
- 2026-05-11: Semantic/plain dual capture (`RecorderCaptureMode`), toolbar + menu toggle, plain-mode `PlaywrightScript`; `RecorderSemanticConfigJson` + `semi` self-semantic root; requirements §9, design §10.7, `RecordingService` global sync.
