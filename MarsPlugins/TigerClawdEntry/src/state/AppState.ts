import type { EnvironmentInfo } from "../models/EnvironmentInfo";
import type { ScenarioTemplate } from "../models/ScenarioTemplate";
import type { ScenarioApplyResult } from "../models/ScenarioApplyResult";
import type { ValidationResult } from "../models/ValidationResult";
import type { SetupExecutionResult } from "../setup/models/SetupExecutionResult";

export interface AppState {
  environment?: EnvironmentInfo;
  scenarios: ScenarioTemplate[];
  lastApplyResult?: ScenarioApplyResult;
  lastHealthCheck?: ValidationResult[];
  lastSetupResult?: SetupExecutionResult;
}

