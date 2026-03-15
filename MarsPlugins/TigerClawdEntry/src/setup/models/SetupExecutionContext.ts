import type * as vscode from "vscode";
import type { UserConfig } from "../../models/UserConfig";
import type { LoggerService } from "../../services/logging/LoggerService";
import type { StateManager } from "../../state/StateManager";
import type { SetupTemplateDefinition } from "../templates/SetupTemplateDefinition";

export interface SetupExecutionContext {
  workspacePath: string | undefined;
  environment: unknown;
  currentConfig: UserConfig;
  template: SetupTemplateDefinition;
  permissionLevel: "safe" | "standard" | "elevated";
  logger: LoggerService;
  state: StateManager;
  cancellationToken?: vscode.CancellationToken;
}

