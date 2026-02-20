# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

## [0.9.0-alpha] - 2026-02-20

### Added
- Replay pre-action highlight support controlled by `IsHighlightObjectWhileReplay`.
- Runtime config distribution with `marsJavaAgent-config.json`.
- Visual `parameter` editing and persistence through replay payload.
- CI workflow for Node + Java + .NET build validation.

### Changed
- Version bumped to `0.9.0-alpha`.
- Documentation updated to state replay execution capability.
- Visual node now shows Para/Data on separate lines.

### Fixed
- Object tree click no longer adds visual nodes outside recording mode.
- SearchAndUpdate replay enforces edit mode with stabilization delay.
- SelectTreeList replay now fails strictly when target/path is invalid.
- Data/Para table edit supports Enter commit and Esc revert.

### Added
- Product release readiness document at `doc/release-readiness_zh.md`.
- Visual test step now supports `parameter` editing in detail modal and persistence to step storage.
- Data column in Test Steps is directly editable and syncs on blur.
- Java agent runtime config file `marsJavaAgent-config.json` is packaged in resources and supports replay highlight toggle.
- Replay pre-action highlight support controlled by `IsHighlightObjectWhileReplay`.

### Changed
- Object tree click no longer creates visual node outside recording mode.
- SearchAndUpdate replay now enforces cell edit mode and adds stabilization delay before keyboard input.
- Visual node rendering splits `Para` and `Data` into separate lines.
- Documentation updated to reflect replay execution capability.

### Fixed
- Visual data edits now sync to Test Steps and replay execution payload.
- SearchAndUpdate target cell editing reliability improved for JTable replay.

## [0.1.0] - 2026-02-20

### Added
- Initial public baseline for Java UI Automation extension.
- Java process discovery, object scanning, object tree, and object highlighting.
- Record & replay flow with Visual/Test Steps editing.
- Java agent integration (`marsJavaAgent`, `agent-loader`) and ProcessInfo helper.
