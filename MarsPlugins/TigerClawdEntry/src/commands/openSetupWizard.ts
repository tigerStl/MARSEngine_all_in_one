import * as vscode from "vscode";
import type { SetupOrchestratorService } from "../setup/services/SetupOrchestratorService";
import { SETUP_TEMPLATES } from "../setup/templates/SetupTemplates";
import { getStrings, resolveLocale } from "../constants/locale";

export function registerOpenSetupWizardCommand(
  context: vscode.ExtensionContext,
  orchestrator: SetupOrchestratorService
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.openSetupWizard",
    async () => {
      const lang = getStrings(resolveLocale(vscode.env.language));
      const pick = await vscode.window.showQuickPick(
        SETUP_TEMPLATES.map(t => ({
          label: t.name,
          description: t.description,
          detail: `Risk: ${t.riskLevel} · Level: ${t.estimatedLevel}`,
          id: t.id
        })),
        {
          placeHolder: lang.dialogSetupSelectTemplate
        }
      );
      if (!pick) return;

      await vscode.window.withProgress(
        {
          location: vscode.ProgressLocation.Notification,
          title: lang.dialogSetupRunning.replace("{0}", pick.label ?? pick.id),
          cancellable: false
        },
        async () => {
          const result = await orchestrator.runTemplate(pick.id, {
            locale: resolveLocale(vscode.env.language)
          });
          const icon =
            result.overallStatus === "success"
              ? "✅"
              : result.overallStatus === "warning"
              ? "⚠️"
              : "❌";
          const message =
            result.overallStatus === "success"
              ? lang.dialogSetupSuccess
              : lang.dialogSetupWithFailures.replace("{0}", String(result.failedSteps?.length ?? 0));
          vscode.window.showInformationMessage(`${icon} ${message}`);
        }
      );
    }
  );
  context.subscriptions.push(disposable);
}

