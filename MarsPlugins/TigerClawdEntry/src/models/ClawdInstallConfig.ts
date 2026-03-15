/**
 * Configuration file for TigerClawd install and resolution.
 * Describes all modules and templates that Clawd can install/manage.
 */

export interface ClawdInstallEntry {
  /** Module or template id from knowledge base / scenario templates */
  id: string;
  /** Optional version pin */
  version?: string;
  /** Optional config overrides (e.g. apiKey placeholder) */
  config?: Record<string, string>;
}

export interface ClawdInstallConfig {
  /** Schema version for migration */
  version: number;
  /** LLM modules to install (order preserved) */
  llm?: ClawdInstallEntry[];
  /** Agent runtimes */
  agent?: ClawdInstallEntry[];
  /** Code/IDE integrations */
  code?: ClawdInstallEntry[];
  /** Vector DBs */
  vectorDb?: ClawdInstallEntry[];
  /** Tool categories or ids */
  tools?: ClawdInstallEntry[];
  /** Scenario template ids applied (for rollback/delete) */
  appliedTemplates?: string[];
}

export const CLAWD_CONFIG_FILENAME = ".tigerclawd/install.json";

export const DEFAULT_CLAWD_INSTALL_CONFIG: ClawdInstallConfig = {
  version: 1,
  llm: [],
  agent: [],
  code: [],
  vectorDb: [],
  tools: [],
  appliedTemplates: []
};
