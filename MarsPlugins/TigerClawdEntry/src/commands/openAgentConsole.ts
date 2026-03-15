import * as vscode from "vscode";
import type { AgentConsolePanel } from "../views/AgentConsolePanel";
import { COMMAND_OPEN_AGENT_CONSOLE, COMMAND_REVEAL_AGENT_CONSOLE } from "../constants/commandIds";

export function registerOpenAgentConsoleCommand(
  context: vscode.ExtensionContext,
  agentConsolePanel: AgentConsolePanel
): void {
  context.subscriptions.push(
    vscode.commands.registerCommand(COMMAND_OPEN_AGENT_CONSOLE, () => {
      agentConsolePanel.createOrReveal();
    })
  );
  context.subscriptions.push(
    vscode.commands.registerCommand(COMMAND_REVEAL_AGENT_CONSOLE, () => {
      agentConsolePanel.reveal();
    })
  );
}
