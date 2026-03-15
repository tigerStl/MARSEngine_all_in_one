import type { ScenarioService } from "./ScenarioService";
import { SCENARIO_TEMPLATES } from "../../constants/scenarioTemplates";
import type { ScenarioTemplate } from "../../models/ScenarioTemplate";

export class DefaultScenarioService implements ScenarioService {
  listScenarios(): ScenarioTemplate[] {
    return SCENARIO_TEMPLATES;
  }

  getScenario(id: string): ScenarioTemplate | undefined {
    return SCENARIO_TEMPLATES.find(s => s.id === id);
  }
}

