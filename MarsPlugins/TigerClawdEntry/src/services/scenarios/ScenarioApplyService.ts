import type { ScenarioTemplate } from "../../models/ScenarioTemplate";
import type { ScenarioModuleSelection } from "../../models/ScenarioModuleSelection";
import type { ScenarioApplyResult } from "../../models/ScenarioApplyResult";
import type { UserConfig } from "../../models/UserConfig";

export interface ScenarioApplyOptions {
  onProgress?: () => void;
  onPrereqDone?: () => void;
}

export interface ScenarioApplyService {
  applyScenario(
    scenario: ScenarioTemplate,
    selection: ScenarioModuleSelection | undefined,
    config: UserConfig,
    options?: ScenarioApplyOptions
  ): Promise<{ config: UserConfig; result: ScenarioApplyResult }>;
}

