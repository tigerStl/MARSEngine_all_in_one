import * as vscode from "vscode";
import { spawn } from "child_process";
import * as fs from "fs";
import * as path from "path";
import type { InstallerService } from "./InstallerService";
import type { UserConfig } from "../../models/UserConfig";
import type { LoggerService } from "../logging/LoggerService";
import { getStrings, resolveLocale } from "../../constants/locale";

function getWorkspaceRoot(): string | undefined {
  return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
}

/**
 * Real shell-based installer.
 *
 * For each module id, this will look for an install script in:
 *   <workspace>/.tigerclawd/installers/<moduleId>.sh   (macOS/Linux)
 *   <workspace>/.tigerclawd/installers/<moduleId>.cmd  (Windows)
 *
 * If the script exists, it is executed and all stdout/stderr is streamed
 * into the logger, which feeds the Recent Activity console.
 *
 * Install scope: scripts are run with TIGERCLAWD_INSTALL_SCOPE=global so that
 * installs (e.g. pip install --user, npm install -g) are global/user-level and
 * the installed capabilities can be used from any terminal, outside Cursor/VS Code.
 */
export class ShellInstallerService implements InstallerService {
  constructor(
    private readonly logger: LoggerService,
    private readonly workspacePath?: string
  ) {}

  async runPrerequisites(): Promise<void> {
    const scriptName = "prereq-node-npm";
    const candidateRoots: (string | undefined)[] = [
      this.workspacePath,
      getWorkspaceRoot(),
      path.resolve(__dirname, "..", "..", ".."),
      process.cwd()
    ];
    const isWin = process.platform === "win32";
    const scriptExt = isWin ? ".cmd" : ".sh";
    for (const root of candidateRoots) {
      if (!root) continue;
      const scriptPath = path.join(root, ".tigerclawd", "installers", `${scriptName}${scriptExt}`);
      if (fs.existsSync(scriptPath)) {
        this.logger.info("────────────────────────────────────────────────────────────");
        this.logger.info("正在执行前置步骤 / Running prerequisites: ensure Node.js and npm");
        await this.runScript(scriptPath, root, "Prereq");
        return;
      }
    }
    this.logger.info("[Prereq] No prereq-node-npm script found; skipping. Install Node.js from https://nodejs.org if needed.");
  }

  private runScript(scriptPath: string, baseDir: string, logTag = "script"): Promise<void> {
    const isWin = process.platform === "win32";
    const cmd = isWin ? process.env.comspec || "cmd.exe" : "bash";
    const args = isWin ? ["/c", scriptPath] : [scriptPath];
    this.logger.info(`[CLI][${logTag}] ${cmd} ${args.join(" ")}  (cwd=${baseDir})`);
    return new Promise(resolve => {
      const child = spawn(cmd, args, {
        cwd: baseDir,
        shell: false,
        env: { ...process.env, TIGERCLAWD_INSTALL_SCOPE: "global" }
      });
      child.stdout?.on("data", (data: Buffer | string) => {
        data.toString().split(/\r?\n/).filter(Boolean).forEach((line: string) =>
          this.logger.info(`[${logTag}] ${line}`)
        );
      });
      child.stderr?.on("data", (data: Buffer | string) => {
        data.toString().split(/\r?\n/).filter(Boolean).forEach((line: string) =>
          this.logger.error(`[${logTag}] ${line}`)
        );
      });
      child.on("close", code => {
        if (code === 0) {
          this.logger.info(`[${logTag}] Completed successfully.`);
        } else {
          this.logger.warn(`[${logTag}] Exited with code ${code}.`);
        }
        resolve();
      });
      child.on("error", (err: NodeJS.ErrnoException) => {
        this.logger.error(`[${logTag}] spawn error: ${err.message}`);
        resolve();
      });
    });
  }

  async installModules(
    moduleIds: string[],
    config: UserConfig,
    options?: { onProgress?: (moduleId: string) => void }
  ): Promise<UserConfig> {
    const next: UserConfig = {
      ...config,
      installedModules: { ...config.installedModules }
    };

    for (const id of moduleIds) {
      // Visual separator in Recent Activity for each module install.
      this.logger.info(
        "────────────────────────────────────────────────────────────"
      );
      this.logger.info(getStrings(resolveLocale(vscode.env.language)).log_installing_module.replace("{0}", id));
      await this.runInstallCommand(id);
      next.installedModules[id] = {
        version: "0.1.0",
        configured: false
      };
      options?.onProgress?.(id);
    }

    return next;
  }

  async uninstallModules(
    moduleIds: string[],
    config: UserConfig
  ): Promise<UserConfig> {
    const next: UserConfig = {
      ...config,
      installedModules: { ...config.installedModules }
    };

    for (const id of moduleIds) {
      this.logger.info(
        "────────────────────────────────────────────────────────────"
      );
      this.logger.info(getStrings(resolveLocale(vscode.env.language)).log_uninstalling_module.replace("{0}", id));
      await this.runUninstallCommand(id);
      delete next.installedModules[id];
    }

    return next;
  }

  private runInstallCommand(moduleId: string): Promise<void> {
    const candidateRoots: (string | undefined)[] = [
      this.workspacePath,
      getWorkspaceRoot(),
      // When running from compiled JS in /out, this walks back to the extension root.
      path.resolve(__dirname, "..", "..", ".."),
      process.cwd()
    ];

    let baseDir: string | undefined;
    let scriptPath: string | undefined;

    const isWin = process.platform === "win32";
    const scriptExt = isWin ? ".cmd" : ".sh";
    for (const root of candidateRoots) {
      if (!root) continue;
      const installersDir = path.join(root, ".tigerclawd", "installers");
      const candidate = path.join(installersDir, `${moduleId}${scriptExt}`);
      if (fs.existsSync(candidate)) {
        baseDir = root;
        scriptPath = candidate;
        break;
      }
    }

    if (!baseDir || !scriptPath) {
      const attempted = candidateRoots
        .filter((r): r is string => !!r)
        .map(r => path.join(r, ".tigerclawd", "installers", `${moduleId}${scriptExt}`))
        .join(" | ");
      this.logger.error(
        getStrings(resolveLocale(vscode.env.language)).log_no_installer_script.replace("{0}", moduleId).replace("{1}", attempted)
      );
      return Promise.resolve();
    }

    const commandLine = isWin
      ? `${process.env.comspec || "cmd.exe"} /c "${scriptPath}"`
      : `bash "${scriptPath}"`;
    this.logger.info(
      `[CLI][module:${moduleId}] ${commandLine}  (cwd=${baseDir})`
    );

    return new Promise(resolve => {
      const cmd: string = isWin ? process.env.comspec || "cmd.exe" : "bash";
      const args: string[] = isWin ? ["/c", scriptPath!] : [scriptPath!];

      const child = spawn(cmd, args, {
        cwd: baseDir,
        shell: false,
        env: { ...process.env, TIGERCLAWD_INSTALL_SCOPE: "global" }
      });

      child.stdout?.on("data", (data: Buffer | string) => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) =>
            this.logger.info(`[${moduleId}] ${line}`)
          );
      });

      child.stderr?.on("data", (data: Buffer | string) => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) =>
            this.logger.error(`[${moduleId}] ${line}`)
          );
      });

      child.on("close", (code: number | null) => {
        const lang = getStrings(resolveLocale(vscode.env.language));
        if (code === 0) {
          this.logger.info(lang.log_installer_completed.replace("{0}", moduleId));
        } else {
          this.logger.error(lang.log_installer_exited.replace("{0}", moduleId).replace("{1}", String(code)));
        }
        resolve();
      });
    });
  }

  private runUninstallCommand(moduleId: string): Promise<void> {
    const candidateRoots: (string | undefined)[] = [
      this.workspacePath,
      getWorkspaceRoot(),
      path.resolve(__dirname, "..", "..", ".."),
      process.cwd()
    ];

    const isWin = process.platform === "win32";
    const scriptExt = isWin ? ".cmd" : ".sh";

    let baseDir: string | undefined;
    let scriptPath: string | undefined;

    for (const root of candidateRoots) {
      if (!root) continue;
      const installersDir = path.join(root, ".tigerclawd", "installers");
      const candidate = path.join(
        installersDir,
        `${moduleId}_uninstall${scriptExt}`
      );
      if (fs.existsSync(candidate)) {
        baseDir = root;
        scriptPath = candidate;
        break;
      }
    }

    if (!baseDir || !scriptPath) {
      const attempted = candidateRoots
        .filter((r): r is string => !!r)
        .map(r =>
          path.join(
            r,
            ".tigerclawd",
            "installers",
            `${moduleId}_uninstall${scriptExt}`
          )
        )
        .join(" | ");
      this.logger.warn(
        `No uninstall script found for ${moduleId}. Tried: ${attempted}`
      );
      return Promise.resolve();
    }

    const cmd = isWin ? process.env.comspec || "cmd.exe" : "bash";
    const args = isWin ? ["/c", scriptPath] : [scriptPath];

    this.logger.info(
      `[CLI][module:${moduleId}] ${cmd} ${args.join(" ")}  (cwd=${baseDir})`
    );

    return new Promise(resolve => {
      const child = spawn(cmd, args, {
        cwd: baseDir,
        shell: false,
        env: { ...process.env, TIGERCLAWD_INSTALL_SCOPE: "global" }
      });

      child.stdout.on("data", data => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) =>
            this.logger.info(`[${moduleId}] ${line}`)
          );
      });

      child.stderr.on("data", data => {
        data
          .toString()
          .split(/\r?\n/)
          .filter(Boolean)
          .forEach((line: string) =>
            this.logger.error(`[${moduleId}] ${line}`)
          );
      });

      child.on("close", (code: number | null) => {
        const lang = getStrings(resolveLocale(vscode.env.language));
        if (code === 0) {
          this.logger.info(lang.log_uninstaller_completed.replace("{0}", moduleId));
        } else {
          this.logger.error(lang.log_uninstaller_exited.replace("{0}", moduleId).replace("{1}", String(code)));
        }
        resolve();
      });
    });
  }
}

