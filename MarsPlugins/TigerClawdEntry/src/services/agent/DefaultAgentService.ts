import { nowIso } from "../../utils/time";
import type {
  AgentExecutionRequest,
  AgentExecutionResult,
  AgentExecutionLog,
  AgentPlanStep
} from "../../models/AgentModels";
import type { AgentService } from "./AgentService";
import type { ValidationService } from "../validators/ValidationService";
import type { ToolExecutionService } from "./ToolExecutionService";

export class DefaultAgentService implements AgentService {
  constructor(
    private readonly tools: ToolExecutionService,
    private readonly validator: ValidationService
  ) {}

  async runAgentTask(
    request: AgentExecutionRequest
  ): Promise<AgentExecutionResult> {
    const logs: AgentExecutionLog[] = [];

    const addLog = (level: AgentExecutionLog["level"], message: string) => {
      logs.push({
        timestamp: nowIso(),
        level,
        message
      });
    };

    const trimmed = request.prompt.trim().toLowerCase();
    let plan: AgentPlanStep[] = [];
    let resultSummary = "";
    let error: AgentExecutionResult["error"] | undefined;

    const requireWorkspace = (): boolean => {
      if (request.workspacePath) return true;
      error = {
        message: "No workspace folder open.",
        suggestion: "Open a folder (File → Open Folder) and try again."
      };
      addLog("warn", "Agent task requires an open workspace folder.");
      return false;
    };

    if (!trimmed) {
      error = {
        message: "Empty prompt.",
        suggestion: "Enter a task description, e.g. 'Create hello world script'."
      };
      addLog("warn", "Agent task aborted: empty prompt.");
    } else if (trimmed.includes("hello") && trimmed.includes("node")) {
      if (!requireWorkspace()) {
        return { request, plan, logs, resultSummary, error };
      }
      // Real hello.js creation and execution
      plan = [
        {
          id: "step1",
          title: "Create hello.js",
          description: "Create a Node.js script file in the workspace.",
          status: "pending"
        },
        {
          id: "step2",
          title: "Write hello world code",
          description:
            "Write a simple function that prints 'Hello TigerClawd' to the console.",
          status: "pending"
        },
        {
          id: "step3",
          title: "Run node hello.js",
          description: "Execute the script using Node.js.",
          status: "pending"
        }
      ];
      try {
        plan[0].status = "running";
        addLog("info", "Creating hello.js in workspace root.");
        const code =
          'function hello() {\n  console.log("Hello TigerClawd");\n}\nhello();\n';
        await this.tools.writeFile("hello.js", code);
        plan[0].status = "done";

        plan[1].status = "done";
        addLog("success", "hello.js written successfully.");

        plan[2].status = "running";
        addLog("info", "Executing: node hello.js");
        const res = await this.tools.runCommand("node", ["hello.js"]);
        plan[2].status = "done";
        addLog(
          res.exitCode === 0 ? "success" : "error",
          `node hello.js exited with code ${res.exitCode}, stdout="${res.stdout.trim()}", stderr="${res.stderr.trim()}"`
        );
        resultSummary =
          "Node.js hello world executed.\n\n" +
          `Command: ${res.command}\n` +
          `Exit code: ${res.exitCode}\n` +
          `Stdout:\n${res.stdout}\n` +
          (res.stderr ? `Stderr:\n${res.stderr}` : "");
      } catch (e) {
        error = {
          message: "Failed to create or run hello.js.",
          suggestion:
            "Check that Node.js is installed and the workspace is writable."
        };
        addLog("error", String(e));
      }
    } else if (trimmed.includes("python") && trimmed.includes("1 to 5")) {
      if (!requireWorkspace()) {
        return { request, plan, logs, resultSummary, error };
      }
      // Real Python script creation and execution
      plan = [
        {
          id: "step1",
          title: "Create test.py",
          description: "Create a Python script file in the workspace.",
          status: "pending"
        },
        {
          id: "step2",
          title: "Write loop code",
          description:
            "Write a for-loop that prints numbers 1 to 5 to the console.",
          status: "pending"
        },
        {
          id: "step3",
          title: "Run python test.py",
          description: "Execute the script using Python.",
          status: "pending"
        }
      ];
      try {
        plan[0].status = "running";
        addLog("info", "Creating test.py in workspace root.");
        const code = "for i in range(1, 6):\n    print(i)\n";
        await this.tools.writeFile("test.py", code);
        plan[0].status = "done";

        plan[1].status = "done";
        addLog("success", "test.py written successfully.");

        plan[2].status = "running";
        addLog("info", "Executing: python test.py (or python3).");
        let res = await this.tools.runCommand("python", ["test.py"]);
        if (res.exitCode !== 0) {
          // try python3 as fallback
          addLog(
            "warn",
            "python test.py failed, trying python3 test.py as fallback."
          );
          res = await this.tools.runCommand("python3", ["test.py"]);
        }
        plan[2].status = "done";
        addLog(
          res.exitCode === 0 ? "success" : "error",
          `python test.py exited with code ${res.exitCode}, stdout="${res.stdout.trim()}", stderr="${res.stderr.trim()}"`
        );
        resultSummary =
          "Python script executed.\n\n" +
          `Command: ${res.command}\n` +
          `Exit code: ${res.exitCode}\n` +
          `Stdout:\n${res.stdout}\n` +
          (res.stderr ? `Stderr:\n${res.stderr}` : "");
      } catch (e) {
        error = {
          message: "Failed to create or run test.py.",
          suggestion:
            "Check that Python is installed and the workspace is writable."
        };
        addLog("error", String(e));
      }
    } else if (trimmed.includes("explain") || trimmed.includes("workspace")) {
      plan = [
        {
          id: "step1",
          title: "Inspect workspace",
          description: "List key folders and files in the current workspace.",
          status: "running"
        },
        {
          id: "step2",
          title: "Summarize structure",
          description: "Generate a high-level summary for the developer.",
          status: "pending"
        }
      ];
      try {
        addLog("info", "Listing workspace files (limited).");
        const files = await this.tools.listWorkspaceFiles(80);
        plan[0].status = "done";

        plan[1].status = "running";
        const top = files.slice(0, 20);
        const more = files.length > top.length;
        resultSummary =
          "Workspace overview (sampled):\n\n" +
          top.map(f => "- " + f).join("\n") +
          (more ? `\n\n... and ${files.length - top.length} more entries.` : "");
        plan[1].status = "done";
        addLog("success", "Workspace summary generated from file listing.");
      } catch (e) {
        error = {
          message: "Failed to inspect workspace.",
          suggestion:
            "Ensure the workspace folder is available and readable by the extension."
        };
        addLog("error", String(e));
      }
    } else if (trimmed.includes("available tools") || trimmed.includes("tools")) {
      plan = [
        {
          id: "step1",
          title: "List runtime tools",
          description:
            "Summarize available tools from the TigerClawdEntry runtime.",
          status: "done"
        }
      ];
      addLog("info", "Listing runtime tools (static V1 list).");
      resultSummary =
        "Available tools:\n\n" +
        "- File tools (read/write within workspace)\n" +
        "- Shell tools (safe command execution)\n" +
        "- Git tools (basic git status checks)\n" +
        "- Validation tools (environment, file, command, git checks)\n" +
        "- OpenClaw runtime placeholder (runtime-openclaw module)";
      addLog("success", "Reported available tools.");
    } else if (
      trimmed.includes("basic coding validation") ||
      trimmed.includes("basic coding") ||
      trimmed.includes("validation")
    ) {
      if (!requireWorkspace()) {
        return { request, plan, logs, resultSummary, error };
      }
      plan = [
        {
          id: "step1",
          title: "Run basic coding validation",
          description:
            "Trigger the Basic Coding validation workflow via the validation service.",
          status: "running"
        }
      ];
      try {
        addLog("info", "Running Basic Coding Validation via ValidationService.");
        const config = { installedModules: {}, lastScenarioId: undefined } as any;
        const checks = await this.validator.validateAll(config);
        plan[0].status = "done";
        const ok = checks.filter(c => c.status === "PASS").length;
        const warn = checks.filter(c => c.status === "WARN").length;
        const fail = checks.filter(c => c.status === "FAIL").length;
        resultSummary =
          "Basic Coding Validation Report:\n\n" +
          checks
            .map(
              c => `- [${c.status}] ${c.name}: ${c.message}`
            )
            .join("\n") +
          `\n\nSummary: PASS=${ok}, WARN=${warn}, FAIL=${fail}`;
        addLog("success", "Basic Coding Validation completed.");
      } catch (e) {
        error = {
          message: "Basic Coding Validation failed.",
          suggestion:
            "Check the validation logs and ensure environment tools (node/npm/git) are installed."
        };
        addLog("error", String(e));
      }
    } else {
      plan = [
        {
          id: "step1",
          title: "Parse task",
          description: "Analyze the requested task and available tools.",
          status: "done"
        }
      ];
      addLog("info", "Received generic agent task.");
      addLog(
        "warn",
        "V1 agent can only handle a few predefined task types; falling back to summary."
      );
      resultSummary =
        "Agent received your task but this V1 demo only supports a small set of predefined workflows (hello world Node, Python 1..5, explain workspace, show tools, basic coding validation).";
    }

    return {
      request,
      plan,
      logs,
      resultSummary,
      error
    };
  }
}

