import { spawn } from "child_process";
import * as fs from "fs";
import * as path from "path";
import type { LoggerService } from "../../services/logging/LoggerService";
import type { SetupStepResult } from "../models/SetupStepResult";

export class ShellStepRunner {
  constructor(
    private readonly logger: LoggerService,
    private readonly workspacePath?: string
  ) {}

  async runStep(
    stepId: string,
    kind: "run" | "validate" | "rollback"
  ): Promise<SetupStepResult> {
    const baseDir = this.workspacePath ?? process.cwd();
    const templateSegment = stepId.split("_")[0] || "DefaultTemplate";
    const primaryDir = path.join(baseDir, "Scripts", templateSegment);
    const fallbackDir = path.join(baseDir, "Scripts", "DefaultTemplate");

    const isWin = process.platform === "win32";
    const ext = isWin ? ".cmd" : ".sh";
    const scriptName = `${stepId}_${kind}${ext}`;
    let scriptsDir = primaryDir;
    let scriptPath = path.join(scriptsDir, scriptName);

    if (!fs.existsSync(scriptPath) && fs.existsSync(fallbackDir)) {
      scriptsDir = fallbackDir;
      scriptPath = path.join(scriptsDir, scriptName);
    }

    const startedAt = new Date().toISOString();

    if (!fs.existsSync(scriptPath)) {
      const msg = `No ${kind} script for step ${stepId} at ${scriptPath}`;
      this.logger.error(msg);
      const endedAt = new Date().toISOString();
      return {
        id: stepId,
        status: "warning",
        startedAt,
        endedAt,
        retries: 0,
        messages: [msg]
      };
    }

    const commandLine = isWin
      ? `"${scriptPath}"`
      : `bash "${scriptPath}"`;
    this.logger.info(
      `[CLI][step:${stepId}] ${commandLine}  (cwd=${baseDir})`
    );

    return await new Promise<SetupStepResult>(resolve => {
      const cmd = isWin ? scriptPath : "bash";
      const args = isWin ? [] : [scriptPath];

      const child = spawn(cmd, args, {
        cwd: baseDir,
        shell: false
      });

      const messages: string[] = [];

      child.stdout.on("data", data => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) => {
            const msg = `[${stepId}] ${line}`;
            messages.push(msg);
            this.logger.info(msg);
          });
      });

      child.stderr.on("data", data => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) => {
            const msg = `[${stepId}] ${line}`;
            messages.push(msg);
            this.logger.error(msg);
          });
      });

      child.on("close", code => {
        const endedAt = new Date().toISOString();
        const success = code === 0;
        resolve({
          id: stepId,
          status: success ? "success" : "failed",
          startedAt,
          endedAt,
          retries: 0,
          messages,
          errorDetails: success
            ? undefined
            : `Script exited with code ${code ?? "unknown"}`
        });
      });
    });
  }
}

