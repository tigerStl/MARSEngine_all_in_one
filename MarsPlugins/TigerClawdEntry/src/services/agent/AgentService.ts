import type {
  AgentExecutionRequest,
  AgentExecutionResult
} from "../../models/AgentModels";

export interface AgentService {
  runAgentTask(request: AgentExecutionRequest): Promise<AgentExecutionResult>;
}

