import * as vscode from "vscode";

export function registerRefreshStatusCommand(
  context: vscode.ExtensionContext
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.refreshStatus",
    async () => {
      await vscode.commands.executeCommand("tigerClawdEntry.openDashboard");
    }
  );
  context.subscriptions.push(disposable);
}

