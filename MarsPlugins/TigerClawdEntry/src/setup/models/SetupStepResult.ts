import type { SetupStepStatus } from "./SetupStatus";

export interface SetupStepResult {
  id: string;
  status: SetupStepStatus;
  startedAt: string;
  endedAt: string;
  retries: number;
  messages: string[];
  errorDetails?: string;
}

