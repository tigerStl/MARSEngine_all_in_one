import type { UserConfig } from "../../models/UserConfig";
import type { ValidationResult } from "../../models/ValidationResult";

export interface RepairService {
  attemptAutoRepair(
    validations: ValidationResult[],
    config: UserConfig
  ): Promise<UserConfig>;
}

