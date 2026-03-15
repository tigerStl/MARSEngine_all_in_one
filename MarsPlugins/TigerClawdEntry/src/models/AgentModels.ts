export interface AgentExecutionRequest {
  prompt: string;
  workspacePath?: string;
}

export type AgentPlanStepStatus = "pending" | "running" | "done" | "skipped";

export interface AgentPlanStep {
  id: string;
  title: string;
  description: string;
  status: AgentPlanStepStatus;
}

export type AgentLogLevel = "info" | "success" | "warn" | "error";

export interface AgentExecutionLog {
  timestamp: string;
  level: AgentLogLevel;
  message: string;
}

export interface AgentExecutionResult {
  request: AgentExecutionRequest;
  plan: AgentPlanStep[];
  logs: AgentExecutionLog[];
  resultSummary: string;
  error?: {
    message: string;
    suggestion?: string;
  };
}

