import * as vscode from "vscode";
import { registerOpenDashboardCommand } from "./commands/openDashboard";
import { registerRefreshStatusCommand } from "./commands/refreshStatus";
import { registerRunHealthCheckCommand } from "./commands/runHealthCheck";
import { registerOpenSetupWizardCommand } from "./commands/openSetupWizard";
import { registerOpenScenarioCenterCommand } from "./commands/openScenarioCenter";
import { registerOpenAgentConsoleCommand } from "./commands/openAgentConsole";
import { TigerClawdSidebarProvider } from "./views/TigerClawdSidebarProvider";
import { AgentConsolePanel } from "./views/AgentConsolePanel";
import { NodeEnvironmentDetectionService } from "./services/environment/NodeEnvironmentDetectionService";
import { DefaultKnowledgeBaseService } from "./services/knowledge/DefaultKnowledgeBaseService";
import { DefaultScenarioService } from "./services/scenarios/DefaultScenarioService";
import { VsCodeConfigService } from "./services/config/VsCodeConfigService";
import { ShellInstallerService } from "./services/installers/ShellInstallerService";
import { DefaultScenarioApplyService } from "./services/scenarios/DefaultScenarioApplyService";
import { OutputChannelLoggerService } from "./services/logging/OutputChannelLoggerService";
import { StateManager } from "./state/StateManager";
import { DefaultSetupOrchestratorService } from "./setup/services/DefaultSetupOrchestratorService";
import { DefaultAgentService } from "./services/agent/DefaultAgentService";
import { ToolExecutionService } from "./services/agent/ToolExecutionService";
import { DefaultValidationService } from "./services/validation/DefaultValidationService";

export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  const logger = new OutputChannelLoggerService();
  logger.info("TigerClawdEntry extension activated.");

  const envService = new NodeEnvironmentDetectionService();
  const knowledge = new DefaultKnowledgeBaseService();
  const scenarios = new DefaultScenarioService();
  const config = new VsCodeConfigService();
  const workspacePath = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  const installer = new ShellInstallerService(logger, workspacePath);
  const toolExec = new ToolExecutionService(workspacePath);
  const validator = new DefaultValidationService(toolExec);
  const applyService = new DefaultScenarioApplyService(installer, validator);
  const stateManager = new StateManager();
  const setupOrchestrator = new DefaultSetupOrchestratorService(
    logger,
    stateManager,
    envService,
    config,
    vscode.workspace.workspaceFolders
  );
  const agentService = new DefaultAgentService(toolExec, validator);

  const sidebarProvider = new TigerClawdSidebarProvider(
    context.extensionUri,
    envService,
    knowledge,
    scenarios,
    config,
    applyService,
    agentService,
    installer,
    logger,
    stateManager,
    setupOrchestrator,
    toolExec
  );

  const agentConsolePanel = new AgentConsolePanel(
    context.extensionUri,
    agentService,
    logger
  );

  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider(
      TigerClawdSidebarProvider.viewType,
      sidebarProvider
    )
  );
  context.subscriptions.push(agentConsolePanel);

  registerOpenDashboardCommand(context);
  registerRefreshStatusCommand(context);
  registerRunHealthCheckCommand(context, validator, config, stateManager);
  registerOpenSetupWizardCommand(context, setupOrchestrator);
  registerOpenScenarioCenterCommand(context, scenarios);
  registerOpenAgentConsoleCommand(context, agentConsolePanel);

}

export function deactivate(): void {
  // Nothing to clean up yet.
}

