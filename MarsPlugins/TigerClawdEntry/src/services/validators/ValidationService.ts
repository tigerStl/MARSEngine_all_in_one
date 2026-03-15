import type { ValidationResult } from "../../models/ValidationResult";
import type { UserConfig } from "../../models/UserConfig";

export interface ValidationService {
  validateAll(config: UserConfig): Promise<ValidationResult[]>;
}

