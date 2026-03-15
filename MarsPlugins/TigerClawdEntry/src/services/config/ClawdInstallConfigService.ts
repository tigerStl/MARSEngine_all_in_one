import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import type { ClawdInstallConfig } from "../../models/ClawdInstallConfig";
import { DEFAULT_CLAWD_INSTALL_CONFIG } from "../../models/ClawdInstallConfig";

const DEFAULT_CONFIG = { ...DEFAULT_CLAWD_INSTALL_CONFIG };

/**
 * Loads and saves the Clawd install config from workspace .tigerclawd/install.json.
 * Supports multiple apps installation and resolution.
 */
export class ClawdInstallConfigService {
  private configPath(workspaceRoot: string): string {
    return path.join(workspaceRoot, ".tigerclawd", "install.json");
  }

  async loadConfig(workspaceRoot: string | undefined): Promise<ClawdInstallConfig> {
    if (!workspaceRoot) return { ...DEFAULT_CONFIG };
    const filePath = this.configPath(workspaceRoot);
    try {
      const raw = await fs.promises.readFile(filePath, "utf-8");
      const data = JSON.parse(raw) as ClawdInstallConfig;
      return { ...DEFAULT_CONFIG, ...data };
    } catch {
      return { ...DEFAULT_CONFIG };
    }
  }

  async saveConfig(workspaceRoot: string | undefined, config: ClawdInstallConfig): Promise<void> {
    if (!workspaceRoot) return;
    const dir = path.join(workspaceRoot, ".tigerclawd");
    const filePath = this.configPath(workspaceRoot);
    try {
      await fs.promises.mkdir(dir, { recursive: true });
      await fs.promises.writeFile(filePath, JSON.stringify(config, null, 2), "utf-8");
    } catch (e) {
      throw new Error(`Failed to write Clawd config: ${e}`);
    }
  }
}
