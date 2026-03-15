# TigerClawd Installers

Scripts in this folder install and uninstall runtime modules. **All templates use global scope**: every installer is run with `TIGERCLAWD_INSTALL_SCOPE=global` so that installed capabilities are available **outside** Cursor/VS Code—from any terminal or script, on this machine.

## Conventions

- **Python packages**: use `pip install --user <package>` so they are available to any `python` on the machine. **Users may not have Python installed**: every Python-based install script must check `where python` first and, if missing, print a clear message (e.g. "This module requires Python. Install from https://www.python.org/ and add to PATH.") and exit with code 1. Prefer `python -m pip` when Python is available so it works even if `pip` is not on PATH.
- **Node CLI tools**: use `npm install -g <package>` so the CLI is on PATH in any shell.
- **Other tools**: install to a location that is on the user's PATH (e.g. system or user bin directory).

After installation, you can use the same runtimes and CLIs from a normal terminal without opening the editor.

## Script naming

- Install: `<moduleId>.cmd` (Windows) / `<moduleId>.sh` (macOS/Linux)
- Uninstall: `<moduleId>_uninstall.cmd` or `<moduleId>_uninstall.sh` — must **run the real uninstaller** (e.g. find the app’s `Uninstall.exe` from its install path and execute it, or call `winget uninstall`).
- Check: `<moduleId>_check.cmd` or `<moduleId>_check.sh` — exit **0** if the module is installed, **non-zero** otherwise (e.g. `where ollama`, `python -c "import openai"`). Used to verify installation without modifying anything.

All module IDs from the runtime module table (MODULE_KNOWLEDGE_BASE / MODULE_ORDER) have a corresponding install script so that "Install" in the dashboard never fails with "No installer script found". Placeholder modules (e.g. code-oss, code-cli, tools-mars-future, mars-runtime-placeholder) use no-op scripts that exit 0.

## Environment

When run by the extension, scripts receive:

- `TIGERCLAWD_INSTALL_SCOPE=global` — install for global/user use, not workspace-only.
