import type { EnvironmentInfo } from "../../models/EnvironmentInfo";

export interface EnvironmentDetectionService {
  detectEnvironment(): Promise<EnvironmentInfo>;
}

