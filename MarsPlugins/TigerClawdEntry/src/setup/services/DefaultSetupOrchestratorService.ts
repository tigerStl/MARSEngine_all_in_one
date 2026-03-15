import type { SetupOrchestratorService } from "./SetupOrchestratorService";
import { SETUP_TEMPLATES } from "../templates/SetupTemplates";
import type { SetupTemplateId } from "../models/SetupTemplateId";
import type { SetupExecutionResult } from "../models/SetupExecutionResult";
import type { SetupStepResult } from "../models/SetupStepResult";
import type { SetupStepDefinition } from "../steps/SetupStepDefinition";
import type { SetupExecutionContext } from "../models/SetupExecutionContext";
import type { LoggerService } from "../../services/logging/LoggerService";
import type { StateManager } from "../../state/StateManager";
import type { EnvironmentDetectionService } from "../../services/environment/EnvironmentDetectionService";
import type { ConfigService } from "../../services/config/ConfigService";
import type { Locale } from "../../constants/locale";
import { getStrings } from "../../constants/locale";
import type * as vscode from "vscode";

export class DefaultSetupOrchestratorService
  implements SetupOrchestratorService
{
  constructor(
    private readonly logger: LoggerService,
    private readonly state: StateManager,
    private readonly envService: EnvironmentDetectionService,
    private readonly configService: ConfigService,
    private readonly workspaceFolders: readonly vscode.WorkspaceFolder[] | undefined
  ) {}

  async runTemplate(
    templateId: SetupTemplateId,
    options?: { resume?: boolean; locale?: Locale }
  ): Promise<SetupExecutionResult> {
    const template = SETUP_TEMPLATES.find(t => t.id === templateId);
    if (!template) {
      throw new Error(`Unknown setup template: ${templateId}`);
    }

    const startedAt = new Date().toISOString();
    const previous =
      options?.resume && this.state.getState().lastSetupResult?.templateId === templateId
        ? this.state.getState().lastSetupResult
        : undefined;
    const env = await this.envService.detectEnvironment();
    const config = await this.configService.loadConfig();

    const ctx: SetupExecutionContext = {
      workspacePath: this.workspaceFolders?.[0]?.uri.fsPath,
      environment: env,
      currentConfig: config,
      template,
      permissionLevel: "safe",
      logger: this.logger,
      state: this.state
    };

    const steps: SetupStepDefinition[] = [
      ...template.requiredSteps,
      ...template.optionalSteps
    ];

    const completedSteps: SetupStepResult[] = [];
    const failedSteps: SetupStepResult[] = [];
    const skippedSteps: SetupStepResult[] = [];
    const warnings: string[] = [];

    const maxRetries = 2;

    for (const step of steps) {
      const prevResult = previous?.completedSteps.find(s => s.id === step.id);

      if (options?.resume && prevResult && prevResult.status === "success") {
        this.logger.info(
          `Setup "${template.name}" – skipping step (already successful): ${step.title}`
        );
        completedSteps.push(prevResult);
        continue;
      }

      try {
        this.logger.info(`Setup "${template.name}" – running step: ${step.title}`);

        let attempt = 0;
        let result: SetupStepResult | null = null;

        while (true) {
          attempt++;
          result = await step.run(ctx);
          result.retries = attempt - 1;

          if (result.status !== "failed" || !step.retryable || attempt > maxRetries) {
            break;
          }

          this.logger.warn(
            `Step "${step.title}" failed (attempt ${attempt}). Retrying...`
          );
        }

        if (!result) {
          throw new Error("Step did not produce a result.");
        }
        completedSteps.push(result);

        if (result.status === "failed") {
          const msg = step.getFailureMessage(
            new Error(result.errorDetails ?? "step failed"),
            ctx
          );
          this.logger.error(msg);
          failedSteps.push(result);
          warnings.push(msg);
          if (step.required) {
            break;
          }
        } else if (result.status === "warning") {
          const warnMsg =
            step.getWarningMessage?.(ctx) ??
            `Step "${step.title}" reported warnings.`;
          this.logger.warn(warnMsg);
          warnings.push(warnMsg);
        }
      } catch (err) {
        const msg = step.getFailureMessage(err, ctx);
        this.logger.error(msg);
        const now = new Date().toISOString();
        const failed: SetupStepResult = {
          id: step.id,
          status: "failed",
          startedAt: now,
          endedAt: now,
          retries: 0,
          messages: [msg],
          errorDetails: msg
        };
        completedSteps.push(failed);
        failedSteps.push(failed);
        warnings.push(msg);
        if (step.required) {
          break;
        }
      }
    }

    const endedAt = new Date().toISOString();
    const locale = options?.locale ?? "en";
    const lang = getStrings(locale);

    const result: SetupExecutionResult = {
      templateId,
      overallStatus: failedSteps.length
        ? "failed"
        : warnings.length
        ? "warning"
        : "success",
      startedAt,
      endedAt,
      completedSteps,
      failedSteps,
      skippedSteps,
      warnings,
      summaryMessage:
        failedSteps.length === 0
          ? lang.dialogSetupSuccess
          : lang.dialogSetupWithFailures.replace("{0}", String(failedSteps.length)),
      recommendedNextActions: failedSteps.length
        ? [lang.setupReviewFailingSteps, lang.setupFixAndRerun]
        : [lang.setupRunValidation]
    };

    this.state.update({ lastSetupResult: result });

    return result;
  }
}

