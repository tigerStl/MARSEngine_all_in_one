import type { ModuleCategory } from "./ModuleCategory";

export interface ScenarioModuleSelection {
  scenarioId: string;
  overrides: {
    [K in ModuleCategory]?: string[];
  };
}

