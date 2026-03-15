## TigerClawdEntry

**TigerClawdEntry** is a unified AI Runtime Entry and Installer Assistant for VS Code / Cursor.

It provides:

- **Guided environment overview** for AI development (LLM, Agent, Code, Vector DB, Tools).
- **Scenario-based templates** that recommend typical stacks for coding, RAG, media workflows, and automation.
- **Mock installers and validators** (V1 mock implementation – replace with real installer later).

### Running the extension

1. Install dependencies:

```bash
npm install
```

2. Compile:

```bash
npm run compile
```

3. Press **F5** in VS Code / Cursor to launch the Extension Development Host.

### Known issues / Troubleshooting

- **"Found unexpected service worker controller"**  
  This message comes from the editor (Cursor / VS Code) when loading a webview. The extension does **not** register any service worker; the host may reuse a previous webview’s controller. It is usually harmless. If the Agent Console or dashboard behaves oddly, close the panel and open it again (e.g. run “TigerClawdEntry: Open Agent Console” again).

- **Agent Console: "Unexpected identifier 'tce'" or blank panel**  
  Fixed by escaping all locale/text in the HTML and passing initial data via Base64. Ensure you have the latest build (`npm run compile`) and reopen the Agent Console.

### Core Concepts

- **Module Knowledge Base**: Describes LLM, Agent, Code, Vector DB, and Tools options.
- **Scenarios**: Predefined stacks (e.g., Basic Coding Setup, AI Coding Assistant, RAG).
- **Setup Wizard**: Guides the user through selecting and customizing a scenario.
- **Health Center**: Runs mock health checks for LLM, Agent runtime, Vector DB, Tools, and workspace.
- **MARS Integration (Future)**: Placeholder `MARS` module category and comments in services indicate where future MARS runtimes and tools can plug in.

