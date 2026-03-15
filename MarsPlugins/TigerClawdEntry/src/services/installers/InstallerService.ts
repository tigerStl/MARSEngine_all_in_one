import type { UserConfig } from "../../models/UserConfig";

export interface InstallerService {
  /**
   * Run prerequisite steps (e.g. ensure Node.js/npm) before installing any template.
   * Called once per scenario apply before installModules.
   */
  runPrerequisites(): Promise<void>;

  /**
   * Install the given module ids and return the updated config.
   * Implementations are expected to perform real installation work
   * (shell commands, scripts, etc.) and report progress via logging.
   * Optional onProgress is called after each module so the UI can refresh (e.g. Recent Activity).
   */
  installModules(
    moduleIds: string[],
    config: UserConfig,
    options?: { onProgress?: (moduleId: string) => void }
  ): Promise<UserConfig>;

  /**
   * Uninstall the given module ids and return the updated config.
   * Implementations are expected to perform real uninstall work
   * (shell commands, scripts, etc.) and report progress via logging.
   */
  uninstallModules(
    moduleIds: string[],
    config: UserConfig
  ): Promise<UserConfig>;
}

