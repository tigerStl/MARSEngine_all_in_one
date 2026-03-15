import * as vscode from "vscode";
import { SIDEBAR_VIEW_ID } from "../constants/viewIds";

export function registerOpenDashboardCommand(
  context: vscode.ExtensionContext
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.openDashboard",
    async () => {
      await vscode.commands.executeCommand("workbench.view.extension.tigerClawdEntry");
      await vscode.commands.executeCommand(
        `workbench.view.extension.${SIDEBAR_VIEW_ID}`
      );
    }
  );
  context.subscriptions.push(disposable);
}

