import * as vscode from "vscode";
import type { ConfigService } from "./ConfigService";
import type { UserConfig } from "../../models/UserConfig";

const SECTION = "tigerClawdEntry";

export class VsCodeConfigService implements ConfigService {
  async loadConfig(): Promise<UserConfig> {
    const config = vscode.workspace.getConfiguration(SECTION);
    const value = config.get<UserConfig>("userConfig");
    return (
      value ?? {
        installedModules: {}
      }
    );
  }

  async saveConfig(userConfig: UserConfig): Promise<void> {
    const config = vscode.workspace.getConfiguration(SECTION);
    await config.update(
      "userConfig",
      userConfig,
      vscode.ConfigurationTarget.Global
    );
  }
}

