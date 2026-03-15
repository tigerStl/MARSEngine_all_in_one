import type { SetupOverallStatus } from "./SetupStatus";
import type { SetupStepResult } from "./SetupStepResult";
import type { SetupTemplateId } from "./SetupTemplateId";

export interface SetupExecutionResult {
  templateId: SetupTemplateId;
  overallStatus: SetupOverallStatus;
  startedAt: string;
  endedAt: string;
  completedSteps: SetupStepResult[];
  failedSteps: SetupStepResult[];
  skippedSteps: SetupStepResult[];
  warnings: string[];
  summaryMessage: string;
  recommendedNextActions: string[];
}

