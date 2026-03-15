import type { UserConfig } from "../../models/UserConfig";

export interface ConfigService {
  loadConfig(): Promise<UserConfig>;
  saveConfig(config: UserConfig): Promise<void>;
}

