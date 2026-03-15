import type { ScenarioTemplate } from "../../models/ScenarioTemplate";

export interface ScenarioService {
  listScenarios(): ScenarioTemplate[];
  getScenario(id: string): ScenarioTemplate | undefined;
}

