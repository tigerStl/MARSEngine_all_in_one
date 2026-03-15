import type { ValidationService } from "../validators/ValidationService";
import type { UserConfig } from "../../models/UserConfig";
import type { ValidationResult } from "../../models/ValidationResult";
import { ToolExecutionService } from "../agent/ToolExecutionService";
import { nowIso } from "../../utils/time";

export class DefaultValidationService implements ValidationService {
  constructor(private readonly toolExec: ToolExecutionService) {}

  async validateAll(config: UserConfig): Promise<ValidationResult[]> {
    const results: ValidationResult[] = [];

    results.push(await this.envCheck());
    results.push(await this.pythonCheck());
    results.push(await this.pipCheck());
    results.push(await this.fileToolCheck());
    results.push(await this.commandCheck());
    results.push(await this.gitCheck());
    results.push(await this.llmCheck());
    results.push(await this.agentCheck());
    results.push(await this.vectordbCheck());
    results.push(await this.installedModulesCheck(config));

    return results;
  }

  private async envCheck(): Promise<ValidationResult> {
    try {
      const node = await this.toolExec.runCommand("node", ["-v"]);
      const npm = await this.toolExec.runCommand("npm", ["-v"]);
      const git = await this.toolExec.runCommand("git", ["--version"]);
      const ok =
        node.exitCode === 0 && npm.exitCode === 0 && git.exitCode === 0;
      return {
        id: "env",
        name: "Environment Check",
        target: "Workspace",
        status: ok ? "PASS" : "WARN",
        message: ok
          ? `node=${node.stdout.trim()} npm=${npm.stdout.trim()} git=${git.stdout.trim()}`
          : "Some environment tools are missing or returned non-zero exit code."
      };
    } catch (e) {
      return {
        id: "env",
        name: "Environment Check",
        target: "Workspace",
        status: "FAIL",
        message: String(e)
      };
    }
  }

  private async pythonCheck(): Promise<ValidationResult> {
    try {
      const res = await this.toolExec.runCommand("python", ["--version"], 5000);
      const ok = res.exitCode === 0;
      const ver = res.stdout.trim() || res.stderr.trim();
      return {
        id: "python",
        name: "Python Check",
        target: "Workspace",
        status: ok ? "PASS" : "WARN",
        message: ok
          ? (ver || "Python available.")
          : "Python not found or failed. Some modules (LLM/Agent/VectorDB) may require it."
      };
    } catch (e) {
      return {
        id: "python",
        name: "Python Check",
        target: "Workspace",
        status: "WARN",
        message: `Python check failed: ${String(e)}. Optional for Node-only setups.`
      };
    }
  }

  private async pipCheck(): Promise<ValidationResult> {
    try {
      const res = await this.toolExec.runCommand("pip", ["--version"], 5000);
      const ok = res.exitCode === 0;
      const ver = res.stdout.trim() || res.stderr.trim();
      return {
        id: "pip",
        name: "Pip Check",
        target: "Workspace",
        status: ok ? "PASS" : "WARN",
        message: ok
          ? (ver.split(/\r?\n/)[0] || "pip available.")
          : "pip not found. Install Python and pip for LLM/Agent/VectorDB modules."
      };
    } catch (e) {
      return {
        id: "pip",
        name: "Pip Check",
        target: "Workspace",
        status: "WARN",
        message: `Pip check failed: ${String(e)}. Optional for Node-only setups.`
      };
    }
  }

  private async fileToolCheck(): Promise<ValidationResult> {
    try {
      const name = `.tigerclawd-temp-${nowIso().replace(/[:.]/g, "-")}.txt`;
      await this.toolExec.writeFile(name, "temp");
      const content = await this.toolExec.readFile(name);
      const ok = content.trim() === "temp";
      return {
        id: "file",
        name: "File Tool Check",
        target: "Tools",
        status: ok ? "PASS" : "FAIL",
        message: ok
          ? "File read/write in workspace succeeded."
          : "File content mismatch during read/write test."
      };
    } catch (e) {
      const msg = String(e);
      return {
        id: "file",
        name: "File Tool Check",
        target: "Tools",
        status: "FAIL",
        message: msg.includes("Workspace not available")
          ? "Workspace not available; open a folder and try again."
          : msg
      };
    }
  }

  private async commandCheck(): Promise<ValidationResult> {
    try {
      const res = await this.toolExec.runCommand("node", ["-e", "console.log(1+1)"]);
      const ok = res.exitCode === 0 && res.stdout.trim() === "2";
      return {
        id: "command",
        name: "Command Execution Check",
        target: "Tools",
        status: ok ? "PASS" : "FAIL",
        message: ok
          ? "Successfully executed a safe Node.js command."
          : "Failed to execute Node.js command or output mismatch."
      };
    } catch (e) {
      return {
        id: "command",
        name: "Command Execution Check",
        target: "Tools",
        status: "FAIL",
        message: String(e)
      };
    }
  }

  private async gitCheck(): Promise<ValidationResult> {
    try {
      const res = await this.toolExec.runCommand("git", ["status", "-sb"]);
      const ok = res.exitCode === 0;
      return {
        id: "git",
        name: "Git Check",
        target: "Tools",
        status: ok ? "PASS" : "WARN",
        message: ok
          ? "Git repository detected and git status succeeded."
          : "Git status failed; this may not be a repository."
      };
    } catch (e) {
      return {
        id: "git",
        name: "Git Check",
        target: "Tools",
        status: "WARN",
        message: String(e)
      };
    }
  }

  private async llmCheck(): Promise<ValidationResult> {
    try {
      const probes = [
        { code: "import openai", name: "openai" },
        { code: "import anthropic", name: "anthropic" },
        { code: "import google.generativeai", name: "google.generativeai" }
      ];
      for (const p of probes) {
        const res = await this.toolExec.runCommand("python", ["-c", p.code], 5000);
        if (res.exitCode === 0) {
          return {
            id: "llm",
            name: "LLM SDK Check",
            target: "LLM",
            status: "PASS",
            message: `At least one LLM SDK available (e.g. ${p.name}).`
          };
        }
      }
      return {
        id: "llm",
        name: "LLM SDK Check",
        target: "LLM",
        status: "WARN",
        message: "No LLM SDK found (openai, anthropic, google.generativeai). Install one for AI Coding / RAG scenarios."
      };
    } catch (e) {
      return {
        id: "llm",
        name: "LLM SDK Check",
        target: "LLM",
        status: "WARN",
        message: `LLM check failed: ${String(e)}. Install Python and pip, then pip install openai anthropic google-generativeai as needed.`
      };
    }
  }

  private async agentCheck(): Promise<ValidationResult> {
    try {
      const probes = [
        { code: "import langchain", name: "langchain" },
        { code: "import langgraph", name: "langgraph" }
      ];
      for (const p of probes) {
        const res = await this.toolExec.runCommand("python", ["-c", p.code], 5000);
        if (res.exitCode === 0) {
          return {
            id: "agent",
            name: "Agent Framework Check",
            target: "Agent",
            status: "PASS",
            message: `At least one agent framework available (e.g. ${p.name}).`
          };
        }
      }
      return {
        id: "agent",
        name: "Agent Framework Check",
        target: "Agent",
        status: "WARN",
        message: "No agent framework found (langchain, langgraph). Install for AI Coding / Autonomous scenarios."
      };
    } catch (e) {
      return {
        id: "agent",
        name: "Agent Framework Check",
        target: "Agent",
        status: "WARN",
        message: `Agent check failed: ${String(e)}. Optional if using Node-only agent runtimes.`
      };
    }
  }

  private async vectordbCheck(): Promise<ValidationResult> {
    try {
      const probes = [
        { code: "import chromadb", name: "chromadb" },
        { code: "import pymilvus", name: "pymilvus" }
      ];
      for (const p of probes) {
        const res = await this.toolExec.runCommand("python", ["-c", p.code], 5000);
        if (res.exitCode === 0) {
          return {
            id: "vectordb",
            name: "VectorDB Client Check",
            target: "VectorDB",
            status: "PASS",
            message: `At least one vector DB client available (e.g. ${p.name}).`
          };
        }
      }
      return {
        id: "vectordb",
        name: "VectorDB Client Check",
        target: "VectorDB",
        status: "WARN",
        message: "No vector DB client found (chromadb, pymilvus). Install for RAG / Knowledge Base scenarios."
      };
    } catch (e) {
      return {
        id: "vectordb",
        name: "VectorDB Client Check",
        target: "VectorDB",
        status: "WARN",
        message: `VectorDB check failed: ${String(e)}. Optional for non-RAG setups.`
      };
    }
  }

  private async installedModulesCheck(config: UserConfig): Promise<ValidationResult> {
    try {
      const ids = Object.keys(config.installedModules || {});
      const count = ids.length;
      if (count === 0) {
        return {
          id: "installed-modules",
          name: "Installed Modules",
          target: "Workspace",
          status: "PASS",
          message: "No modules installed yet. Apply a template to install modules."
        };
      }
      const list = ids.slice(0, 8).join(", ") + (ids.length > 8 ? ` +${ids.length - 8} more` : "");
      return {
        id: "installed-modules",
        name: "Installed Modules",
        target: "Workspace",
        status: "PASS",
        message: `${count} module(s) recorded: ${list}.`
      };
    } catch (e) {
      return {
        id: "installed-modules",
        name: "Installed Modules",
        target: "Workspace",
        status: "WARN",
        message: String(e)
      };
    }
  }
}

