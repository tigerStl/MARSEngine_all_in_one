import * as vscode from "vscode";
import type { AgentService } from "../services/agent/AgentService";
import type { LoggerService } from "../services/logging/LoggerService";
import { getAgentConsoleHtml } from "../templates/agentConsoleHtml";

const PANEL_VIEW_TYPE = "tigerClawdEntry.agentConsole";
const PANEL_TITLE = "TigerClawdEntry Agent Console";

export class AgentConsolePanel implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private readonly extensionUri: vscode.Uri;
  private readonly agentService: AgentService;
  private readonly logger: LoggerService;

  constructor(
    extensionUri: vscode.Uri,
    agentService: AgentService,
    logger: LoggerService
  ) {
    this.extensionUri = extensionUri;
    this.agentService = agentService;
    this.logger = logger;
  }

  createOrReveal(): void {
    const workspacePath =
      vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    if (this.panel) {
      this.panel.webview.html = this.buildHtml();
      this.panel.reveal(vscode.ViewColumn.Active);
      return;
    }
    this.panel = vscode.window.createWebviewPanel(
      PANEL_VIEW_TYPE,
      PANEL_TITLE,
      vscode.ViewColumn.Active,
      {
        enableScripts: true,
        retainContextWhenHidden: false,
        localResourceRoots: [
          vscode.Uri.joinPath(this.extensionUri, "media"),
          vscode.Uri.joinPath(this.extensionUri, "out"),
        ],
      }
    );
    this.panel.onDidDispose(() => {
      this.panel = undefined;
    });
    this.panel.webview.onDidReceiveMessage(
      async (message: { type: string; prompt?: string }) => {
        if (message.type !== "agentAction" || !this.panel) return;
        const prompt = String(message.prompt ?? "").trim();
        this.logger.info(
          `Agent Console: received agentAction, prompt="${prompt.substring(0, 100)}${prompt.length > 100 ? "…" : ""}"`
        );
        const workspacePath =
          vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
        try {
          const result = await this.agentService.runAgentTask({
            prompt,
            workspacePath,
          });
          this.panel.webview.postMessage({ type: "agentResult", result });
        } catch (err) {
          const messageText = err instanceof Error ? err.message : String(err);
          this.panel.webview.postMessage({
            type: "agentResult",
            result: {
              request: { prompt, workspacePath },
              plan: [],
              logs: [],
              resultSummary: "",
              error: {
                message: messageText,
                suggestion: "Check the Output channel (TigerClawdEntry) for details.",
              },
            },
          });
        }
      }
    );
    this.panel.webview.html = getAgentConsoleHtml(
      this.panel.webview,
      this.extensionUri,
      { workspacePath }
    );
  }

  reveal(): void {
    if (this.panel) {
      this.panel.webview.html = this.buildHtml();
      this.panel.reveal(vscode.ViewColumn.Active);
    } else {
      this.createOrReveal();
    }
  }

  private buildHtml(): string {
    if (!this.panel) {
      return "";
    }
    const workspacePath =
      vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    return getAgentConsoleHtml(
      this.panel.webview,
      this.extensionUri,
      { workspacePath }
    );
  }

  dispose(): void {
    if (this.panel) {
      this.panel.dispose();
      this.panel = undefined;
    }
  }
}
