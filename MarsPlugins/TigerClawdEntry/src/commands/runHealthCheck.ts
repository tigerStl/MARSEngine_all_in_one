import * as vscode from "vscode";
import type { ValidationService } from "../services/validators/ValidationService";
import type { ConfigService } from "../services/config/ConfigService";
import type { StateManager } from "../state/StateManager";
import { getStrings, resolveLocale } from "../constants/locale";

export function registerRunHealthCheckCommand(
  context: vscode.ExtensionContext,
  validator: ValidationService,
  configService: ConfigService,
  stateManager: StateManager
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.runHealthCheck",
    async () => {
      const config = await configService.loadConfig();
      const results = await validator.validateAll(config);
      stateManager.update({ lastHealthCheck: results });

      const lang = getStrings(resolveLocale(vscode.env.language));
      const passed = results.filter(r => r.status === "PASS").length;
      const warningsCount = results.filter(r => r.status === "WARN").length;
      const failed = results.filter(r => r.status === "FAIL").length;
      const summary =
        failed > 0
          ? lang.dialogHealthCheckFailed.replace("{0}", String(passed)).replace("{1}", String(warningsCount)).replace("{2}", String(failed))
          : warningsCount > 0
            ? lang.dialogHealthCheckWarnings.replace("{0}", String(passed)).replace("{1}", String(warningsCount))
            : lang.dialogHealthCheckPassed.replace("{0}", String(passed));

      const detail = results
        .map(r => `[${r.status}] ${r.name}: ${r.message}`)
        .join("\n");

      if (failed > 0) {
        await vscode.window.showErrorMessage(summary, { modal: true, detail });
      } else {
        await vscode.window.showInformationMessage(summary, {
          modal: true,
          detail: detail || undefined
        });
      }
    }
  );
  context.subscriptions.push(disposable);
}
