export type ValidationStatus = "PASS" | "WARN" | "FAIL";

export interface ValidationResult {
  id: string;
  name: string;
  target: "LLM" | "Agent" | "VectorDB" | "Tools" | "Workspace";
  status: ValidationStatus;
  message: string;
}

