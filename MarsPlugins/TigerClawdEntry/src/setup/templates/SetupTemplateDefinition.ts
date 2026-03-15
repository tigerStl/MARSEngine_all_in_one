import type { SetupTemplateId } from "../models/SetupTemplateId";
import type { SetupStepDefinition } from "../steps/SetupStepDefinition";
import type { SetupExecutionResult } from "../models/SetupExecutionResult";

export interface SetupTemplateDefinition {
  id: SetupTemplateId;
  name: string;
  description: string;
  targetModules: ("LLM" | "Agent" | "Code" | "VectorDB" | "Tools")[];
  requiredSteps: SetupStepDefinition[];
  optionalSteps: SetupStepDefinition[];
  estimatedLevel: "basic" | "standard" | "advanced";
  riskLevel: "low" | "medium" | "high";
  supportsRetry: boolean;
  supportsResume: boolean;
  successCriteria: (result: SetupExecutionResult) => boolean;
  recommendedFor: string[];
}

