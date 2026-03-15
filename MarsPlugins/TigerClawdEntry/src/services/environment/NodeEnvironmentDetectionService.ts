import * as vscode from "vscode";
import type { EnvironmentDetectionService } from "./EnvironmentDetectionService";
import type { EnvironmentInfo } from "../../models/EnvironmentInfo";
import { getOS } from "../../utils/platform";
import { runCommand } from "../../utils/exec";

export class NodeEnvironmentDetectionService
  implements EnvironmentDetectionService
{
  async detectEnvironment(): Promise<EnvironmentInfo> {
    const workspacePath = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    return {
      os: getOS(),
      nodeVersion: await this.safeVersion("node -v"),
      npmVersion: await this.safeVersion("npm -v"),
      gitVersion: await this.safeVersion("git --version"),
      pythonVersion: await this.safeVersion("python --version"),
      javaVersion: await this.safeVersion("java -version"),
      workspacePath
    };
  }

  private async safeVersion(cmd: string): Promise<string | undefined> {
    try {
      const { stdout, stderr } = await runCommand(cmd);
      return stdout || stderr || undefined;
    } catch {
      return undefined;
    }
  }
}

