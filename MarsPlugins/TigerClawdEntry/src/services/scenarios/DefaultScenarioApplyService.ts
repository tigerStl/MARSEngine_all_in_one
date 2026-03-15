import type { ScenarioApplyService, ScenarioApplyOptions } from "./ScenarioApplyService";
import type { ScenarioTemplate } from "../../models/ScenarioTemplate";
import type { ScenarioModuleSelection } from "../../models/ScenarioModuleSelection";
import type { ScenarioApplyResult } from "../../models/ScenarioApplyResult";
import type { UserConfig } from "../../models/UserConfig";
import type { InstallerService } from "../installers/InstallerService";
import type { ValidationService } from "../validators/ValidationService";
import { nowIso } from "../../utils/time";

export class DefaultScenarioApplyService implements ScenarioApplyService {
  constructor(
    private readonly installer: InstallerService,
    private readonly validator: ValidationService
  ) {}

  async applyScenario(
    scenario: ScenarioTemplate,
    selection: ScenarioModuleSelection | undefined,
    config: UserConfig,
    options?: ScenarioApplyOptions
  ): Promise<{ config: UserConfig; result: ScenarioApplyResult }> {
    await this.installer.runPrerequisites();
    options?.onPrereqDone?.();
    const chosenModules = this.resolveModules(scenario, selection);
    const afterInstall = await this.installer.installModules(
      chosenModules,
      config,
      { onProgress: options?.onProgress ? () => options.onProgress!() : undefined }
    );
    const validations = await this.validator.validateAll(afterInstall);

    const warnings = validations
      .filter(v => v.status === "WARN")
      .map(v => `${v.name}: ${v.message}`);
    const errors = validations
      .filter(v => v.status === "FAIL")
      .map(v => `${v.name}: ${v.message}`);

    const result: ScenarioApplyResult = {
      scenarioId: scenario.id,
      scenarioName: scenario.name,
      appliedAt: nowIso(),
      summary: `Applied scenario "${scenario.name}" with ${chosenModules.length} modules.`,
      appliedModules: chosenModules,
      warnings,
      errors
    };

    const finalConfig: UserConfig = {
      ...afterInstall,
      lastScenarioId: scenario.id,
      lastSelection: selection
    };

    return { config: finalConfig, result };
  }

  private resolveModules(
    scenario: ScenarioTemplate,
    selection: ScenarioModuleSelection | undefined
  ): string[] {
    const base = new Set<string>();
    for (const list of Object.values(scenario.recommendedModules)) {
      if (list) {
        for (const id of list) base.add(id);
      }
    }

    if (selection) {
      for (const list of Object.values(selection.overrides)) {
        if (list) {
          for (const id of list) base.add(id);
        }
      }
    }

    return Array.from(base);
  }
}

