import * as vscode from "vscode";
import * as path from "path";
import * as fs from "fs";
import { SIDEBAR_VIEW_ID } from "../constants/viewIds";
import { getDashboardHtml } from "../templates/dashboardHtml";
import type { EnvironmentDetectionService } from "../services/environment/EnvironmentDetectionService";
import type { KnowledgeBaseService } from "../services/knowledge/KnowledgeBaseService";
import type { ScenarioService } from "../services/scenarios/ScenarioService";
import type { ConfigService } from "../services/config/ConfigService";
import type { ScenarioApplyService } from "../services/scenarios/ScenarioApplyService";
import type { SetupOrchestratorService } from "../setup/services/SetupOrchestratorService";
import type { LoggerService } from "../services/logging/LoggerService";
import type { InstallerService } from "../services/installers/InstallerService";
import type { AgentService } from "../services/agent/AgentService";
import type { ToolExecutionService } from "../services/agent/ToolExecutionService";
import { StateManager } from "../state/StateManager";
import { MODULE_ORDER, MODULE_DISPLAY_NAMES } from "../constants/moduleOrder";
import { getStrings, resolveLocale } from "../constants/locale";
import type { ScenarioApplyResult } from "../models/ScenarioApplyResult";

export class TigerClawdSidebarProvider implements vscode.WebviewViewProvider {
  public static readonly viewType = SIDEBAR_VIEW_ID;

  constructor(
    private readonly extensionUri: vscode.Uri,
    private readonly envService: EnvironmentDetectionService,
    private readonly knowledge: KnowledgeBaseService,
    private readonly scenarios: ScenarioService,
    private readonly config: ConfigService,
    private readonly applyService: ScenarioApplyService,
    private readonly agentService: AgentService,
    private readonly installer: InstallerService,
    private readonly logger: LoggerService,
    private readonly state: StateManager,
    private readonly setupOrchestrator: SetupOrchestratorService,
    private readonly toolExec: ToolExecutionService
  ) {}

  async resolveWebviewView(
    webviewView: vscode.WebviewView,
    _context: vscode.WebviewViewResolveContext,
    _token: vscode.CancellationToken
  ): Promise<void> {
    webviewView.webview.options = {
      enableScripts: true
    };

    const config = await this.config.loadConfig();
    const initialState = this.buildInitialState(config);
    webviewView.webview.html = getDashboardHtml(
      webviewView.webview,
      this.extensionUri,
      JSON.stringify(initialState)
    );

    await this.refreshEnvironmentAndNotify(webviewView);

      webviewView.webview.onDidReceiveMessage(async message => {
      try {
        if (message.type === "scenarioAction") {
          const scenarioId = message.scenarioId as string;
          const action = message.action as "details" | "apply" | "remove";
          const lang = typeof message.lang === "string" ? message.lang : undefined;
          const scenario = this.scenarios.getScenario(scenarioId);
          if (action === "remove") {
            await this.removeTemplate(scenarioId, webviewView);
            return;
          }
          if (!scenario) {
            this.logger.warn(`Unknown scenario: ${scenarioId}`);
            return;
          }
          if (action === "details") {
            await vscode.commands.executeCommand("tigerClawdEntry.openScenarioCenter", {
              id: scenarioId,
              lang
            });
          } else {
            const result = await this.applyScenario(scenarioId, webviewView);
            this.pushState(webviewView);
            if (result) {
              setTimeout(() => this.showInstallResultDialog(result), 150);
            }
          }
        } else if (message.type === "headerAction") {
          const action = message.action as "refresh" | "validate" | "wizard";
          if (action === "refresh") {
            await this.refreshEnvironmentAndNotify(webviewView);
          } else if (action === "validate") {
            await vscode.commands.executeCommand("tigerClawdEntry.runHealthCheck");
          } else if (action === "wizard") {
            await vscode.commands.executeCommand("tigerClawdEntry.openSetupWizard");
          }
        } else if (message.type === "moduleAction") {
          const moduleId = message.moduleId as string;
          const action = message.action as "install" | "uninstall" | "configure";
          await this.handleModuleAction(moduleId, action, webviewView);
        } else if (message.type === "setupAction") {
          const templateId = message.templateId as
            | "basicCoding"
            | "agentSetup"
            | "retrievalSetup"
            | "toolingSetup"
            | "fullLocal";
          const action = message.action as "run" | "retry" | "resume";
          if (!templateId) {
            this.logger.warn("Missing templateId for setupAction");
            return;
          }
          const resume = action === "resume" || action === "retry";
          this.logger.info(
            `Setup action "${action}" requested for template ${templateId}`
          );
          const result = await this.setupOrchestrator.runTemplate(templateId, {
            resume,
            locale: resolveLocale(vscode.env.language)
          });
          this.logger.info(result.summaryMessage);
          result.warnings.forEach(w => this.logger.warn(w));
          this.pushState(webviewView);
        } else if (message.type === "agentAction") {
          const prompt = String(message.prompt || "");
          this.logger.info(`Agent task requested: ${prompt}`);
          const workspacePath =
            this.state.getState().environment?.workspacePath ?? undefined;
          const result = await this.agentService.runAgentTask({
            prompt,
            workspacePath
          });
          webviewView.webview.postMessage({
            type: "agentResult",
            result
          });
        } else if (message.type === "runInstallCheck") {
          const checkId = message.checkId as string;
          const result = await this.runInstallCheck(checkId);
          webviewView.webview.postMessage({
            type: "installCheckResult",
            checkId,
            status: result.status,
            message: result.message
          });
        }
      } catch (err) {
        const messageText = err instanceof Error ? err.message : String(err);
        const stack = err instanceof Error ? err.stack : undefined;
        this.logger.error(`Webview message handler error: ${messageText}`);
        if (stack) this.logger.error(stack);
        if (message.type === "agentAction") {
          webviewView.webview.postMessage({
            type: "agentResult",
            result: {
              request: { prompt: String((message as { prompt?: string }).prompt ?? "") },
              plan: [],
              logs: [],
              resultSummary: "",
              error: {
                message: messageText,
                suggestion: "Check the Output channel (TigerClawdEntry) for details."
              }
            }
          });
        }
      }
    });
  }

  private buildInitialState(config?: { installedModules: Record<string, { version: string; configured: boolean }> }) {
    const state = this.state.getState();
    return {
      environment: state.environment,
      scenarios: this.scenarios.listScenarios(),
      stack: this.knowledge.getAllCategories(),
      logs: this.logger.getRecentLogs(),
      installedModules: config?.installedModules ?? {},
      moduleOrder: MODULE_ORDER,
      moduleDisplayNames: MODULE_DISPLAY_NAMES,
      lastSetupResult: state.lastSetupResult
    };
  }

  private async refreshEnvironmentAndNotify(
    view: vscode.WebviewView
  ): Promise<void> {
    const env = await this.envService.detectEnvironment();
    this.state.update({ environment: env });
    const config = await this.config.loadConfig();
    const state = this.buildInitialState(config);
    view.webview.postMessage({ type: "stateUpdate", state });
  }

  private pushState(view: vscode.WebviewView): void {
    this.config.loadConfig().then(config => {
      const state = this.buildInitialState(config);
      view.webview.postMessage({ type: "stateUpdate", state });
    });
  }

  private async handleModuleAction(
    moduleId: string,
    action: "install" | "uninstall" | "configure",
    view: vscode.WebviewView
  ): Promise<void> {
    if (action === "configure") {
      // Defer so input box is not suppressed when triggered from webview focus
      const key = await new Promise<string | undefined>(resolve => {
        setTimeout(() => {
          vscode.window
            .showInputBox({
              title: "Configure module: " + moduleId,
              prompt: "API Key or config value (saved locally)",
              placeHolder: "sk-...",
              ignoreFocusOut: true
            })
            .then(resolve);
        }, 100);
      });
      if (key !== undefined) {
        this.logger.info(`Configure ${moduleId}: value set`);
        const config = await this.config.loadConfig();
        if (!config.installedModules[moduleId]) {
          config.installedModules[moduleId] = { version: "0.0.0", configured: false };
        }
        config.installedModules[moduleId].configured = true;
        await this.config.saveConfig(config);
        this.pushState(view);
      }
      return;
    }
    const config = await this.config.loadConfig();
    let nextConfig: typeof config;
    if (action === "install") {
      this.logger.info(`Install requested: ${moduleId}`);
      nextConfig = await this.installer.installModules([moduleId], config);
    } else {
      this.logger.info(`Uninstall requested: ${moduleId}`);
      nextConfig = await this.installer.uninstallModules([moduleId], config);
    }
    await this.config.saveConfig(nextConfig);
    this.pushState(view);
  }

  private async removeTemplate(scenarioId: string, view: vscode.WebviewView): Promise<void> {
    const config = await this.config.loadConfig();
    const scenario = this.scenarios.getScenario(scenarioId);
    let nextConfig = config;
    if (scenario) {
      const moduleIds = this.collectScenarioModuleIds(scenario);
      nextConfig = await this.installer.uninstallModules(moduleIds, config);
    }
    await this.config.saveConfig(nextConfig);
    this.logger.info(`Removed template: ${scenarioId}`);
    this.pushState(view);
  }

  private collectScenarioModuleIds(scenario: { recommendedModules: Record<string, string[] | undefined> }): string[] {
    const ids: string[] = [];
    for (const list of Object.values(scenario.recommendedModules)) {
      if (list) ids.push(...list);
    }
    return ids;
  }

  private async applyScenario(
    scenarioId: string,
    webviewView: vscode.WebviewView
  ): Promise<ScenarioApplyResult | undefined> {
    const scenario = this.scenarios.getScenario(scenarioId);
    if (!scenario) return undefined;

    this.logger.info(`Applying scenario: ${scenario.name}`);
    this.pushState(webviewView);

    const currentConfig = await this.config.loadConfig();
    const { config, result } = await this.applyService.applyScenario(
      scenario,
      undefined,
      currentConfig,
      {
        onProgress: () => this.pushState(webviewView),
        onPrereqDone: () => this.pushState(webviewView)
      }
    );
    await this.config.saveConfig(config);
    this.state.update({ lastApplyResult: result });

    this.logger.info(result.summary);
    if (result.warnings && result.warnings.length) {
      result.warnings.forEach(w => this.logger.warn(w));
    }
    if (result.errors && result.errors.length) {
      result.errors.forEach(e => this.logger.error(e));
    }
    this.pushState(webviewView);
    return result;
  }

  private async runInstallCheck(
    checkId: string
  ): Promise<{ status: "PASS" | "WARN" | "FAIL"; message: string }> {
    const timeout = 8000;
    try {
      switch (checkId) {
        case "env": {
          const useShell = process.platform === "win32" || process.platform === "darwin";
          const [node, npm, git] = await Promise.all([
            this.toolExec.runCommand("node", ["-v"], timeout, useShell),
            this.toolExec.runCommand("npm", ["-v"], timeout, useShell),
            this.toolExec.runCommand("git", ["--version"], timeout, useShell)
          ]);
          const ok = node.exitCode === 0 && npm.exitCode === 0 && git.exitCode === 0;
          if (ok) {
            return {
              status: "PASS",
              message: `node ${node.stdout.trim()} npm ${npm.stdout.trim()} git ${git.stdout.trim()}`
            };
          }
          const parts: string[] = [];
          if (node.exitCode !== 0) parts.push(`node: ${node.stderr || node.stdout || "failed"}`);
          if (npm.exitCode !== 0) parts.push(`npm: ${npm.stderr || npm.stdout || "failed"}`);
          if (git.exitCode !== 0) parts.push(`git: ${git.stderr || git.stdout || "failed"}`);
          return {
            status: "WARN",
            message: parts.length ? parts.join("; ") : "One or more of node/npm/git missing or failed."
          };
        }
        case "python": {
          const r = await this.toolExec.runCommand("python", ["--version"], timeout);
          const ver = r.stdout.trim() || r.stderr.trim();
          return {
            status: r.exitCode === 0 ? "PASS" : "WARN",
            message: r.exitCode === 0 ? ver || "Python available" : "Python not found on PATH."
          };
        }
        case "pip": {
          const r = await this.toolExec.runCommand("pip", ["--version"], timeout);
          const line = (r.stdout.trim() || r.stderr.trim()).split(/\r?\n/)[0] || "";
          return {
            status: r.exitCode === 0 ? "PASS" : "WARN",
            message: r.exitCode === 0 ? line : "pip not found on PATH."
          };
        }
        case "llm-openai": {
          const r = await this.toolExec.runCommand("python", ["-c", "import openai; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "openai SDK available" : "Run: pip install --user openai" };
        }
        case "llm-anthropic": {
          const r = await this.toolExec.runCommand("python", ["-c", "import anthropic; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "anthropic SDK available" : "Run: pip install --user anthropic" };
        }
        case "llm-gemini": {
          const r = await this.toolExec.runCommand("python", ["-c", "import google.generativeai; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "google-generativeai available" : "Run: pip install --user google-generativeai" };
        }
        case "agent-langchain": {
          const r = await this.toolExec.runCommand("python", ["-c", "import langchain; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "langchain available" : "Run: pip install --user langchain" };
        }
        case "agent-langgraph": {
          const r = await this.toolExec.runCommand("python", ["-c", "import langgraph; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "langgraph available" : "Run: pip install --user langgraph" };
        }
        case "vectordb-chroma": {
          const r = await this.toolExec.runCommand("python", ["-c", "import chromadb; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "chromadb available" : "Run: pip install --user chromadb" };
        }
        case "vectordb-milvus": {
          const r = await this.toolExec.runCommand("python", ["-c", "import pymilvus; print('ok')"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "pymilvus available" : "Run: pip install --user pymilvus" };
        }
        case "tools-jest": {
          const r = await this.toolExec.runCommand("jest", ["--version"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? (r.stdout.trim() || "jest available") : "Run: npm install -g jest" };
        }
        case "tools-playwright": {
          const r = await this.toolExec.runCommand("npx", ["playwright", "--version"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? (r.stdout.trim() || "playwright available") : "Run: npm install -g playwright" };
        }
        case "git": {
          const r = await this.toolExec.runCommand("git", ["--version"], timeout);
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? r.stdout.trim() : "Install Git and add to PATH." };
        }
        case "code-cli": {
          const useShell = process.platform === "win32" || process.platform === "darwin";
          let r = await this.toolExec.runCommand("code", ["--version"], timeout, useShell);
          if (r.exitCode !== 0) {
            const appRoot = vscode.env.appRoot;
            const isWin = process.platform === "win32";
            const ext = isWin ? ".cmd" : "";
            const binDirs = isWin
              ? [path.join(appRoot, "..", "..", "bin"), path.join(appRoot, "..", "bin")]
              : [
                  path.join(appRoot, "bin"),
                  path.join(appRoot, "..", "bin"),
                  path.join(appRoot, "..", "..", "bin")
                ];
            for (const binDir of binDirs) {
              const codeCmd = path.join(binDir, "code" + ext);
              const cursorCmd = path.join(binDir, "cursor" + ext);
              if (fs.existsSync(codeCmd)) {
                r = await this.toolExec.runCommand(codeCmd, ["--version"], timeout, useShell);
                break;
              }
              if (fs.existsSync(cursorCmd)) {
                r = await this.toolExec.runCommand(cursorCmd, ["--version"], timeout, useShell);
                break;
              }
            }
          }
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "code CLI on PATH" : "Install 'code' in PATH from VS Code." };
        }
        case "cursor-cli": {
          const useShell = process.platform === "win32" || process.platform === "darwin";
          let r = await this.toolExec.runCommand("cursor", ["--version"], timeout, useShell);
          if (r.exitCode !== 0) {
            const appRoot = vscode.env.appRoot;
            const isWin = process.platform === "win32";
            const ext = isWin ? ".cmd" : "";
            const binDirs = isWin
              ? [path.join(appRoot, "..", "..", "bin"), path.join(appRoot, "..", "bin")]
              : [
                  path.join(appRoot, "bin"),
                  path.join(appRoot, "..", "bin"),
                  path.join(appRoot, "..", "..", "bin")
                ];
            for (const binDir of binDirs) {
              const cursorCmd = path.join(binDir, "cursor" + ext);
              if (fs.existsSync(cursorCmd)) {
                r = await this.toolExec.runCommand(cursorCmd, ["--version"], timeout, useShell);
                break;
              }
            }
          }
          return { status: r.exitCode === 0 ? "PASS" : "WARN", message: r.exitCode === 0 ? "cursor CLI on PATH" : "Add Cursor to PATH for CLI." };
        }
        default:
          return { status: "WARN", message: `Unknown check: ${checkId}` };
      }
    } catch (e) {
      return { status: "FAIL", message: String(e) };
    }
  }

  private showInstallResultDialog(result: ScenarioApplyResult): void {
    const lang = getStrings(resolveLocale(vscode.env.language));
    const scenarioNameKey = "scenario_" + result.scenarioId.replace(/-/g, "_") + "_name";
    const scenarioName = (lang[scenarioNameKey] !== undefined ? lang[scenarioNameKey] : result.scenarioName) || result.scenarioId;
    const summaryLine = lang.dialogAppliedScenario
      .replace("{0}", scenarioName)
      .replace("{1}", String((result.appliedModules && result.appliedModules.length) || 0));
    const moduleList =
      result.appliedModules && result.appliedModules.length > 0
        ? result.appliedModules.map(id => `• ${id}`).join("\n")
        : "";
    const hasErrors = result.errors && result.errors.length > 0;
    const detail = [
      summaryLine,
      result.errors && result.errors.length > 0
        ? `\n${lang.dialogLabelErrors}\n` + result.errors.join("\n")
        : "",
      result.warnings && result.warnings.length > 0
        ? `\n${lang.dialogLabelWarnings}\n` + result.warnings.join("\n")
        : "",
      !hasErrors && moduleList ? `\n${lang.dialogLabelInstalledModules}\n${moduleList}` : "",
      !hasErrors ? `\n${lang.dialogNoteGlobalUse}` : ""
    ]
      .filter(Boolean)
      .join("");

    if (hasErrors) {
      vscode.window.showErrorMessage(lang.dialogInstallFailure, { modal: true, detail });
    } else {
      vscode.window.showInformationMessage(lang.dialogInstallSuccess, { modal: true, detail });
    }
  }
}

