export interface ScenarioApplyResult {
  scenarioId: string;
  /** Scenario display name (e.g. for logging); may be used as fallback when locale key is missing. */
  scenarioName?: string;
  appliedAt: string;
  summary: string;
  /** Module IDs that were installed. */
  appliedModules: string[];
  warnings: string[];
  errors: string[];
}

