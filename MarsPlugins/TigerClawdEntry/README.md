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

### Core Concepts

- **Module Knowledge Base**: Describes LLM, Agent, Code, Vector DB, and Tools options.
- **Scenarios**: Predefined stacks (e.g., Basic Coding Setup, AI Coding Assistant, RAG).
- **Setup Wizard**: Guides the user through selecting and customizing a scenario.
- **Health Center**: Runs mock health checks for LLM, Agent runtime, Vector DB, Tools, and workspace.
- **MARS Integration (Future)**: Placeholder `MARS` module category and comments in services indicate where future MARS runtimes and tools can plug in.

