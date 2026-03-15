import type { SetupTemplateId } from "../models/SetupTemplateId";
import type { SetupExecutionResult } from "../models/SetupExecutionResult";
import type { Locale } from "../../constants/locale";

export interface SetupOrchestratorService {
  runTemplate(
    templateId: SetupTemplateId,
    options?: { resume?: boolean; locale?: Locale }
  ): Promise<SetupExecutionResult>;
}

