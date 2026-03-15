import * as vscode from "vscode";
import { SIDEBAR_VIEW_ID } from "../constants/viewIds";

export function registerOpenDashboardCommand(
  context: vscode.ExtensionContext
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.openDashboard",
    async () => {
      await vscode.commands.executeCommand("workbench.view.extension.tigerClawdEntry");
      try {
        await vscode.commands.executeCommand(`${SIDEBAR_VIEW_ID}.focus`);
      } catch {
        // The container is already opened above; ignore focus failures for compatibility.
      }
    }
  );
  context.subscriptions.push(disposable);
}

