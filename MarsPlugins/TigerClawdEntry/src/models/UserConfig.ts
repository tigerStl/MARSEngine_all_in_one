import type { ScenarioModuleSelection } from "./ScenarioModuleSelection";

export interface UserConfig {
  lastScenarioId?: string;
  lastSelection?: ScenarioModuleSelection;
  installedModules: Record<
    string,
    {
      version: string;
      configured: boolean;
    }
  >;
}

