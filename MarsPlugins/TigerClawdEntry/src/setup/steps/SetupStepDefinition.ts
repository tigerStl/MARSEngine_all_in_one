import type { SetupExecutionContext } from "../models/SetupExecutionContext";
import type { SetupStepResult } from "../models/SetupStepResult";

export interface SetupStepDefinition {
  id: string;
  title: string;
  description: string;
  moduleCategory?: "LLM" | "Agent" | "Code" | "VectorDB" | "Tools";
  required: boolean;
  timeoutMs: number;
  retryable: boolean;
  skippable: boolean;
  rollbackOnFailure: boolean;
  validateBeforeRun?: (
    ctx: SetupExecutionContext
  ) => Promise<SetupStepResult | null>;
  run: (ctx: SetupExecutionContext) => Promise<SetupStepResult>;
  validateAfterRun?: (
    ctx: SetupExecutionContext
  ) => Promise<SetupStepResult | null>;
  getFailureMessage: (err: unknown, ctx: SetupExecutionContext) => string;
  getWarningMessage?: (ctx: SetupExecutionContext) => string | undefined;
  getSuccessMessage: (ctx: SetupExecutionContext) => string;
}

