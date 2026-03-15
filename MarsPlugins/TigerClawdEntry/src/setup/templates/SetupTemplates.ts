import type { SetupTemplateDefinition } from "./SetupTemplateDefinition";
import type { SetupStepDefinition } from "../steps/SetupStepDefinition";
import type { SetupExecutionResult } from "../models/SetupExecutionResult";
import type { SetupExecutionContext } from "../models/SetupExecutionContext";
import { ShellStepRunner } from "../services/ShellStepRunner";

function makeScriptStep(
  id: string,
  title: string,
  description: string,
  required: boolean,
  moduleCategory?: "LLM" | "Agent" | "Code" | "VectorDB" | "Tools"
): SetupStepDefinition {
  return {
    id,
    title,
    description,
    moduleCategory,
    required,
    timeoutMs: 10 * 60 * 1000,
    retryable: true,
    skippable: !required,
    rollbackOnFailure: false,
    run: async (ctx: SetupExecutionContext) => {
      const runner = new ShellStepRunner(
        ctx.logger,
        ctx.workspacePath
      );
      return runner.runStep(id, "run");
    },
    validateAfterRun: async (ctx: SetupExecutionContext) => {
      const runner = new ShellStepRunner(
        ctx.logger,
        ctx.workspacePath
      );
      return runner.runStep(id, "validate");
    },
    getFailureMessage: (err, _ctx) =>
      `Step "${title}" failed: ${
        err instanceof Error ? err.message : String(err)
      }`,
    getSuccessMessage: () => `Step "${title}" completed successfully.`
  };
}

function basicSuccessCriteria(result: SetupExecutionResult): boolean {
  return (
    result.failedSteps.length === 0 &&
    result.completedSteps.some(s => s.status === "success")
  );
}

export const SETUP_TEMPLATES: SetupTemplateDefinition[] = [
  {
    id: "basicCoding",
    name: "Basic Coding Setup",
    description:
      "Fastest path to a usable coding runtime with a default LLM and basic tools.",
    targetModules: ["LLM", "Code", "Tools"],
    requiredSteps: [
      makeScriptStep(
        "basicCoding_envScan",
        "Environment scan",
        "Detect OS, runtimes, and basic tooling.",
        true
      ),
      makeScriptStep(
        "basicCoding_ensureConfigStore",
        "Ensure config store",
        "Create or migrate TigerClawdEntry config store.",
        true
      ),
      makeScriptStep(
        "basicCoding_bootstrapLlmConfig",
        "Bootstrap LLM config",
        "Set up a default LLM provider or local placeholder.",
        true,
        "LLM"
      ),
      makeScriptStep(
        "basicCoding_initializeCodeBridge",
        "Initialize code bridge",
        "Connect IDE workspace to TigerClawdEntry runtime.",
        true,
        "Code"
      ),
      makeScriptStep(
        "basicCoding_enableBasicTools",
        "Enable basic tools",
        "Enable safe file and navigation tools.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "basicCoding_validate",
        "Validate basic coding setup",
        "Run basic validation checks for coding scenario.",
        true
      )
    ],
    optionalSteps: [],
    estimatedLevel: "basic",
    riskLevel: "low",
    supportsRetry: true,
    supportsResume: true,
    successCriteria: basicSuccessCriteria,
    recommendedFor: ["Individual developers", "Teams starting with AI coding"]
  },
  {
    id: "agentSetup",
    name: "Agent Setup",
    description:
      "Prepare an agent runtime with safe command execution and IDE integration.",
    targetModules: ["LLM", "Agent", "Code", "Tools"],
    requiredSteps: [
      makeScriptStep(
        "agentSetup_envScan",
        "Environment scan",
        "Scan environment and permissions for agent runtime.",
        true
      ),
      makeScriptStep(
        "agentSetup_verifyLlmConfig",
        "Verify LLM readiness",
        "Check that at least one LLM provider is configured.",
        true,
        "LLM"
      ),
      makeScriptStep(
        "agentSetup_initializeAgent",
        "Initialize agent runtime",
        "Install and configure the agent runtime layer.",
        true,
        "Agent"
      ),
      makeScriptStep(
        "agentSetup_verifyCodeBridge",
        "Verify code bridge",
        "Ensure the IDE workspace connection is ready for agents.",
        true,
        "Code"
      ),
      makeScriptStep(
        "agentSetup_writePermissionProfile",
        "Write permission profile",
        "Configure command execution permissions for agents.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "agentSetup_validate",
        "Validate agent setup",
        "Run agent-focused validation checks.",
        true
      )
    ],
    optionalSteps: [],
    estimatedLevel: "standard",
    riskLevel: "medium",
    supportsRetry: true,
    supportsResume: true,
    successCriteria: basicSuccessCriteria,
    recommendedFor: ["Agent developers", "Automation teams"]
  },
  {
    id: "retrievalSetup",
    name: "Retrieval Setup",
    description:
      "Enable local retrieval and indexing capabilities backed by a vector store.",
    targetModules: ["LLM", "VectorDB", "Code"],
    requiredSteps: [
      makeScriptStep(
        "retrievalSetup_envScan",
        "Environment scan",
        "Scan environment for retrieval prerequisites.",
        true
      ),
      makeScriptStep(
        "retrievalSetup_verifyLlmConfig",
        "Verify LLM config",
        "Ensure an LLM is configured for retrieval flows.",
        true,
        "LLM"
      ),
      makeScriptStep(
        "retrievalSetup_initializeVectorStore",
        "Initialize vector store",
        "Set up local or remote vector store configuration.",
        true,
        "VectorDB"
      ),
      makeScriptStep(
        "retrievalSetup_prepareLocalCache",
        "Prepare local cache",
        "Create local cache and index folders.",
        true,
        "Code"
      ),
      makeScriptStep(
        "retrievalSetup_validate",
        "Validate retrieval setup",
        "Validate retrieval and indexing readiness.",
        true
      )
    ],
    optionalSteps: [],
    estimatedLevel: "standard",
    riskLevel: "medium",
    supportsRetry: true,
    supportsResume: true,
    successCriteria: basicSuccessCriteria,
    recommendedFor: ["Knowledge teams", "Support tooling"]
  },
  {
    id: "toolingSetup",
    name: "Tooling Setup",
    description:
      "Enable operational tooling such as shell, Git, and test runners with a safe profile.",
    targetModules: ["Tools", "Code"],
    requiredSteps: [
      makeScriptStep(
        "toolingSetup_envScan",
        "Environment scan",
        "Scan environment for CLI tooling availability.",
        true
      ),
      makeScriptStep(
        "toolingSetup_detectCliTools",
        "Detect CLI tools",
        "Detect shell, Git, and test command availability.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "toolingSetup_initializeToolRegistry",
        "Initialize tool registry",
        "Create or update the tools registry configuration.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "toolingSetup_writePermissionProfile",
        "Write permission profile",
        "Write a tools permission profile for safe defaults.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "toolingSetup_validate",
        "Validate tooling setup",
        "Validate that tool invocation is ready.",
        true
      )
    ],
    optionalSteps: [],
    estimatedLevel: "basic",
    riskLevel: "low",
    supportsRetry: true,
    supportsResume: true,
    successCriteria: basicSuccessCriteria,
    recommendedFor: ["Operators", "DevOps teams"]
  },
  {
    id: "fullLocal",
    name: "Full Local Setup",
    description:
      "Complete local bootstrap for LLM, Agent, Code, Vector DB, and Tools.",
    targetModules: ["LLM", "Agent", "Code", "VectorDB", "Tools"],
    requiredSteps: [
      makeScriptStep(
        "fullLocal_envScan",
        "Full environment scan",
        "Run a full environment scan for local runtime readiness.",
        true
      ),
      makeScriptStep(
        "fullLocal_configBootstrap",
        "Config bootstrap",
        "Bootstrap global configuration for the local runtime stack.",
        true
      ),
      makeScriptStep(
        "fullLocal_installModules",
        "Install modules",
        "Install and configure all required modules.",
        true
      ),
      makeScriptStep(
        "fullLocal_setupVectorMode",
        "Setup vector mode",
        "Configure vector store mode and indexing defaults.",
        true,
        "VectorDB"
      ),
      makeScriptStep(
        "fullLocal_writePermissionProfile",
        "Write permission profile",
        "Write a balanced permission profile for local runtime.",
        true,
        "Tools"
      ),
      makeScriptStep(
        "fullLocal_validationSuite",
        "Validation suite",
        "Run a full validation suite for the local setup.",
        true
      )
    ],
    optionalSteps: [],
    estimatedLevel: "advanced",
    riskLevel: "high",
    supportsRetry: true,
    supportsResume: true,
    successCriteria: basicSuccessCriteria,
    recommendedFor: ["Advanced teams", "Platform engineers"]
  }
];

