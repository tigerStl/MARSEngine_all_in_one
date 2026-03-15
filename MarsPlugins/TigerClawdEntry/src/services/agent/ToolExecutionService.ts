import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { spawn } from "child_process";

export interface CommandResult {
  command: string;
  cwd: string;
  exitCode: number | null;
  stdout: string;
  stderr: string;
}

export class ToolExecutionService {
  constructor(private readonly workspacePath?: string) {}

  private getWorkspaceRoot(): string | undefined {
    const fromWorkspace =
      this.workspacePath ??
      vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    if (fromWorkspace) return fromWorkspace;
    // Fallback when no folder is open: use extension root (e.g. when running from Extension Development Host).
    const extensionRoot = path.resolve(__dirname, "..", "..", "..");
    return fs.existsSync(extensionRoot) ? extensionRoot : undefined;
  }

  async listWorkspaceFiles(limit = 100): Promise<string[]> {
    const root = this.getWorkspaceRoot();
    if (!root || !fs.existsSync(root)) return [];
    const entries: string[] = [];
    const walk = (dir: string) => {
      if (entries.length >= limit) return;
      const items = fs.readdirSync(dir, { withFileTypes: true });
      for (const it of items) {
        const full = path.join(dir, it.name);
        const rel = path.relative(root, full);
        entries.push(rel);
        if (entries.length >= limit) break;
        if (it.isDirectory()) {
          walk(full);
        }
      }
    };
    walk(root);
    return entries;
  }

  async readFile(relPath: string): Promise<string> {
    const root = this.getWorkspaceRoot();
    if (!root) throw new Error("Workspace not available.");
    const full = path.join(root, relPath);
    if (!full.startsWith(root)) {
      throw new Error("Path is outside workspace.");
    }
    return fs.readFileSync(full, "utf8");
  }

  async writeFile(relPath: string, contents: string): Promise<void> {
    const root = this.getWorkspaceRoot();
    if (!root) throw new Error("Workspace not available.");
    const full = path.join(root, relPath);
    if (!full.startsWith(root)) {
      throw new Error("Path is outside workspace.");
    }
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, contents, "utf8");
  }

  runCommand(
    command: string,
    args: string[],
    timeoutMs = 15000,
    useShell?: boolean
  ): Promise<CommandResult> {
    const root = this.getWorkspaceRoot() || process.cwd();
    const fullCommand = `${command} ${args.join(" ")}`.trim();
    return new Promise(resolve => {
      let child: ReturnType<typeof spawn>;
      try {
        if (useShell) {
          const quote = (a: string) =>
            a.includes(" ") || a.includes('"') ? `"${String(a).replace(/"/g, process.platform === "win32" ? '""' : '\\"')}"` : a;
          const cmdLine = [command, ...args].map(quote).join(" ");
          child = spawn(cmdLine, [], { cwd: root, shell: true });
        } else {
          child = spawn(command, args, { cwd: root, shell: false });
        }
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        resolve({
          command: fullCommand,
          cwd: root,
          exitCode: null,
          stdout: "",
          stderr: `spawn failed: ${msg}`
        });
        return;
      }

      let stdout = "";
      let stderr = "";
      let finished = false;

      const done = (code: number | null, extraStderr = "") => {
        if (finished) return;
        finished = true;
        clearTimeout(timer);
        resolve({
          command: fullCommand,
          cwd: root,
          exitCode: code,
          stdout,
          stderr: stderr + extraStderr
        });
      };

      const timer = setTimeout(() => {
        if (!finished) {
          child.kill();
          done(null, "\n[timeout] command exceeded timeout");
        }
      }, timeoutMs);

      child.on("error", (err: NodeJS.ErrnoException) => {
        const msg = err.code === "ENOENT"
          ? `command not found: ${command} (not on PATH or not installed)`
          : err.message;
        done(null, msg);
      });

      child.stdout?.on("data", (d: Buffer | string) => {
        stdout += d.toString();
      });
      child.stderr?.on("data", (d: Buffer | string) => {
        stderr += d.toString();
      });
      child.on("close", code => {
        done(code);
      });
    });
  }
}

