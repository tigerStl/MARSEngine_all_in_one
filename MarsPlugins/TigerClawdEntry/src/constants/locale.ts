/**
 * Global locale strings for TigerClawdEntry.
 * All UI string constants are defined here in English and Chinese.
 * Use getStrings(locale) to get the object for a given language.
 */

export type Locale = "en" | "zh";

export interface LocaleStrings {
  title: string;
  sectionEnv: string;
  sectionModules: string;
  sectionTemplates: string;
  sectionLogs: string;
  tabOverview: string;
  tabRuntime: string;
  tabAgent: string;
  tabTemplates: string;
  tabHealth: string;
  tabSettings: string;
  envNotReady: string;
  notDetected: string;
  noLogs: string;
  headerBadgeEntry: string;
  headerBadgeV1: string;
  chipInstalled: string;
  chipHealthy: string;
  chipWarning: string;
  chipLastScan: string;
  headerRefresh: string;
  headerValidate: string;
  headerWizard: string;
  headerActionsLabel: string;
  recommended: string;
  linkedToWorkspace: string;
  readyToConfigure: string;
  statusInstall: string;
  statusConfig: string;
  statusHealth: string;
  statusUnknown: string;
  statusNotConfigured: string;
  statusNotChecked: string;
  btnViewDetails: string;
  btnConfigure: string;
  btnValidate: string;
  modulesNotLoaded: string;
  noTemplates: string;
  btnUseTemplate: string;
  btnDetails: string;
  labelOs: string;
  labelNode: string;
  labelNpm: string;
  labelGit: string;
  labelPython: string;
  labelJava: string;
  labelWorkspace: string;
  bestFor_coding: string;
  bestFor_rag: string;
  bestFor_media: string;
  bestFor_automation: string;
  level_Beginner: string;
  level_Intermediate: string;
  level_Advanced: string;
  complexity_Low: string;
  complexity_Medium: string;
  complexity_High: string;
  scenarioDetail_description: string;
  scenarioDetail_targetUsers: string;
  scenarioDetail_recommendedModules: string;
  scenarioDetail_installNotes: string;
  scenarioDetail_futureNotes: string;
  dialogInstallSuccess: string;
  dialogInstallFailure: string;
  dialogAppliedScenario: string;
  dialogLabelErrors: string;
  dialogLabelWarnings: string;
  dialogLabelInstalledModules: string;
  dialogNoteGlobalUse: string;
  agentConsoleTitle: string;
  agentConsoleSubtitle: string;
  agentPlaceholder: string;
  agentRun: string;
  agentClear: string;
  agentPlan: string;
  executionLog: string;
  result: string;
  error: string;
  agentPromptHello: string;
  agentPromptExplain: string;
  agentPromptValidation: string;
  agentPromptPython: string;
  agentPromptTools: string;
  healthTitle: string;
  healthSubtitle: string;
  btnRun: string;
  settingsTitle: string;
  settingsSubtitle: string;
  category_LLM: string;
  category_Agent: string;
  category_Code: string;
  category_VectorDB: string;
  category_Tools: string;
  logLevelInfo: string;
  logLevelWarn: string;
  logLevelError: string;
  log_installer_completed: string;
  log_installer_exited: string;
  log_uninstaller_completed: string;
  log_uninstaller_exited: string;
  log_installing_module: string;
  log_uninstalling_module: string;
  log_no_installer_script: string;
  dialogHealthCheckPassed: string;
  dialogHealthCheckWarnings: string;
  dialogHealthCheckFailed: string;
  dialogOpenTemplateDetails: string;
  dialogTemplateNotFound: string;
  dialogSetupSelectTemplate: string;
  dialogSetupRunning: string;
  dialogSetupSuccess: string;
  dialogSetupWithFailures: string;
  setupReviewFailingSteps: string;
  setupFixAndRerun: string;
  setupRunValidation: string;
  [key: string]: string;
}

const EN: LocaleStrings = {
  title: "TigerClawdEntry",
  sectionEnv: "Environment Overview",
  sectionModules: "Runtime Modules",
  sectionTemplates: "Quick Setup Templates",
  sectionLogs: "Recent Activity",
  tabOverview: "Overview",
  tabRuntime: "Runtime",
  tabAgent: "Agent",
  tabTemplates: "Templates",
  tabHealth: "Health",
  tabSettings: "Settings",
  envNotReady: "Environment information not available yet.",
  notDetected: "Not detected",
  noLogs: "No recent activity yet.",
  headerBadgeEntry: "ENTRY PLATFORM",
  headerBadgeV1: "V1 · Real Installers",
  chipInstalled: "Modules Installed",
  chipHealthy: "Healthy",
  chipWarning: "Warnings",
  chipLastScan: "Last Scan",
  headerRefresh: "Refresh",
  headerValidate: "Run Full Validation",
  headerWizard: "Open Setup Wizard",
  headerActionsLabel: "Actions",
  recommended: "Recommended",
  linkedToWorkspace: "Linked to current workspace.",
  readyToConfigure: "Ready to configure.",
  statusInstall: "Install",
  statusConfig: "Config",
  statusHealth: "Health",
  statusUnknown: "Unknown",
  statusNotConfigured: "Not configured",
  statusNotChecked: "Not checked",
  btnViewDetails: "View Details",
  btnConfigure: "Configure",
  btnValidate: "Validate",
  btnInstall: "Install",
  btnUninstall: "Uninstall",
  statusInstalled: "Installed",
  statusNotInstalled: "Not installed",
  btnDeleteTemplate: "Uninstall template",
  modulesNotLoaded: "Module knowledge not loaded.",
  noTemplates: "No templates defined yet.",
  btnUseTemplate: "Install",
  btnDetails: "Details",
  labelOs: "OS",
  labelNode: "Node",
  labelNpm: "npm",
  labelGit: "Git",
  labelPython: "Python",
  labelJava: "Java",
  labelWorkspace: "Workspace",
  bestFor_coding: "coding",
  bestFor_rag: "rag",
  bestFor_media: "media",
  bestFor_automation: "automation",
  level_Beginner: "Beginner",
  level_Intermediate: "Intermediate",
  level_Advanced: "Advanced",
  complexity_Low: "Low",
  complexity_Medium: "Medium",
  complexity_High: "High",
  scenarioDetail_description: "Description",
  scenarioDetail_targetUsers: "Target users",
  scenarioDetail_recommendedModules: "Recommended modules",
  scenarioDetail_installNotes: "Install notes",
  scenarioDetail_futureNotes: "Future notes",
  scenario_basic_coding_setup_name: "Basic Coding Setup",
  scenario_basic_coding_setup_shortDesc: "Baseline configuration for LLM-assisted coding.",
  scenario_ai_coding_assistant_name: "AI Coding Assistant",
  scenario_ai_coding_assistant_shortDesc: "Project-aware coding assistant with LangGraph and Chroma.",
  scenario_retrieval_knowledge_base_name: "Retrieval / Knowledge Base",
  scenario_retrieval_knowledge_base_shortDesc: "RAG and document Q&A with LangChain.",
  scenario_video_media_workflow_name: "Video / Media Workflow",
  scenario_video_media_workflow_shortDesc: "Prompt generation and media pipelines.",
  scenario_autonomous_development_name: "Autonomous Development",
  scenario_autonomous_development_shortDesc: "Agent-driven development with OpenDevin-style runtime.",
  dialogInstallSuccess: "Installation complete.",
  dialogInstallFailure: "Installation had errors.",
  dialogAppliedScenario: "Applied scenario \"{0}\" with {1} modules.",
  dialogLabelErrors: "Errors:",
  dialogLabelWarnings: "Warnings:",
  dialogLabelInstalledModules: "Installed modules:",
  dialogNoteGlobalUse: "Installed capabilities are available globally (use from any terminal).",
  agentConsoleTitle: "Agent Console",
  agentConsoleSubtitle: "Use natural language to execute coding tasks through the runtime.",
  agentPlaceholder: "Create a hello world node script; Explain this project structure; Run basic coding validation",
  agentRun: "Run Agent",
  agentClear: "Clear",
  agentPlan: "Agent Plan",
  executionLog: "Execution Log",
  result: "Result",
  error: "Error",
  agentPromptHello: "Create hello world node script",
  agentPromptExplain: "Explain workspace",
  agentPromptValidation: "Run basic validation",
  agentPromptPython: "Create sample Python script",
  agentPromptTools: "Show available tools",
  healthTitle: "Health",
  healthSubtitle: "Click Run to verify installation (global use).",
  btnRun: "Run",
  settingsTitle: "Settings",
  settingsSubtitle: "Runtime and provider settings will be organized into professional panels in a dedicated phase.",
  category_LLM: "LLM",
  category_Agent: "Agent",
  category_Code: "Code",
  category_VectorDB: "VectorDB",
  category_Tools: "Tools",
  logLevelInfo: "INFO",
  logLevelWarn: "WARN",
  logLevelError: "ERROR",
  log_installer_completed: "Installer for {0} completed successfully.",
  log_installer_exited: "Installer for {0} exited with code {1}",
  log_uninstaller_completed: "Uninstaller for {0} completed successfully.",
  log_uninstaller_exited: "Uninstaller for {0} exited with code {1}",
  log_installing_module: "Installing module: {0}",
  log_uninstalling_module: "Uninstalling module: {0}",
  log_no_installer_script: "No installer script found for {0}. Tried: {1}",
  dialogHealthCheckPassed: "Health check: {0} passed.",
  dialogHealthCheckWarnings: "Health check: {0} passed, {1} warnings.",
  dialogHealthCheckFailed: "Health check: {0} passed, {1} warnings, {2} failed.",
  dialogOpenTemplateDetails: "Open a Quick Setup Template and click Details to see its information.",
  dialogTemplateNotFound: "Template not found: {0}",
  dialogSetupSelectTemplate: "Select a setup template to run",
  dialogSetupRunning: "Running setup: {0}",
  dialogSetupSuccess: "Setup completed.",
  dialogSetupWithFailures: "Setup completed with failures in {0} step(s).",
  setupReviewFailingSteps: "Review the failing steps in Recent Activity.",
  setupFixAndRerun: "Fix the reported issues, then rerun this setup template.",
  setupRunValidation: "You can now run validation or start using the configured modules."
};

const ZH: LocaleStrings = {
  title: "TigerClawdEntry",
  sectionEnv: "环境概览",
  sectionModules: "运行时模块",
  sectionTemplates: "快速安装模板",
  sectionLogs: "最近活动",
  tabOverview: "概览",
  tabRuntime: "运行时",
  tabAgent: "代理",
  tabTemplates: "模板",
  tabHealth: "健康",
  tabSettings: "设置",
  envNotReady: "环境信息尚未就绪。",
  notDetected: "未检测到",
  noLogs: "暂无活动日志。",
  headerBadgeEntry: "入口平台",
  headerBadgeV1: "V1 · 实际安装",
  chipInstalled: "已安装模块",
  chipHealthy: "健康",
  chipWarning: "告警",
  chipLastScan: "最近扫描",
  headerRefresh: "刷新",
  headerValidate: "运行全量校验",
  headerWizard: "打开安装向导",
  headerActionsLabel: "操作",
  recommended: "推荐",
  linkedToWorkspace: "已关联当前工作区。",
  readyToConfigure: "待配置。",
  statusInstall: "安装",
  statusConfig: "配置",
  statusHealth: "健康",
  statusUnknown: "未知",
  statusNotConfigured: "未配置",
  statusNotChecked: "未检查",
  btnViewDetails: "查看详情",
  btnConfigure: "配置",
  btnValidate: "校验",
  btnInstall: "安装",
  btnUninstall: "卸载",
  statusInstalled: "已安装",
  statusNotInstalled: "未安装",
  btnDeleteTemplate: "卸载该模板",
  modulesNotLoaded: "模块知识未加载。",
  noTemplates: "暂无可用模板。",
  btnUseTemplate: "安装",
  btnDetails: "详情",
  labelOs: "操作系统",
  labelNode: "Node",
  labelNpm: "npm",
  labelGit: "Git",
  labelPython: "Python",
  labelJava: "Java",
  labelWorkspace: "工作区",
  bestFor_coding: "编程",
  bestFor_rag: "检索",
  bestFor_media: "媒体",
  bestFor_automation: "自动化",
  level_Beginner: "入门",
  level_Intermediate: "进阶",
  level_Advanced: "高级",
  complexity_Low: "低",
  complexity_Medium: "中",
  complexity_High: "高",
  scenarioDetail_description: "描述",
  scenarioDetail_targetUsers: "目标用户",
  scenarioDetail_recommendedModules: "推荐模块",
  scenarioDetail_installNotes: "安装说明",
  scenarioDetail_futureNotes: "后续说明",
  scenario_basic_coding_setup_name: "基础编程环境",
  scenario_basic_coding_setup_shortDesc: "LLM 辅助编程的基线配置。",
  scenario_ai_coding_assistant_name: "AI 编程助手",
  scenario_ai_coding_assistant_shortDesc: "基于 LangGraph 与 Chroma 的项目感知编程助手。",
  scenario_retrieval_knowledge_base_name: "检索 / 知识库",
  scenario_retrieval_knowledge_base_shortDesc: "基于 LangChain 的 RAG 与文档问答。",
  scenario_video_media_workflow_name: "视频 / 媒体工作流",
  scenario_video_media_workflow_shortDesc: "提示生成与媒体处理流水线。",
  scenario_autonomous_development_name: "自主开发",
  scenario_autonomous_development_shortDesc: "基于 OpenDevin 式运行时的代理驱动开发。",
  dialogInstallSuccess: "安装完成。",
  dialogInstallFailure: "安装未完全成功。",
  dialogAppliedScenario: "已应用场景「{0}」，共 {1} 个模块。",
  dialogLabelErrors: "错误：",
  dialogLabelWarnings: "警告：",
  dialogLabelInstalledModules: "已安装模块：",
  dialogNoteGlobalUse: "已安装能力可在任意终端全局使用。",
  agentConsoleTitle: "代理控制台",
  agentConsoleSubtitle: "使用自然语言通过运行时执行编程任务。",
  agentPlaceholder: "创建 hello world 脚本；解释项目结构；运行基础校验",
  agentRun: "运行代理",
  agentClear: "清空",
  agentPlan: "执行计划",
  executionLog: "执行日志",
  result: "结果",
  error: "错误",
  agentPromptHello: "创建 hello world 脚本",
  agentPromptExplain: "解释工作区",
  agentPromptValidation: "运行基础校验",
  agentPromptPython: "创建示例 Python 脚本",
  agentPromptTools: "显示可用工具",
  healthTitle: "健康",
  healthSubtitle: "点击「运行」验证是否安装成功（全局可用）。",
  btnRun: "运行",
  settingsTitle: "设置",
  settingsSubtitle: "运行时与提供方设置将在后续阶段整合为专业面板。",
  category_LLM: "大语言模型",
  category_Agent: "代理",
  category_Code: "代码",
  category_VectorDB: "向量库",
  category_Tools: "工具",
  logLevelInfo: "信息",
  logLevelWarn: "警告",
  logLevelError: "错误",
  log_installer_completed: "安装 {0} 结束",
  log_installer_exited: "安装 {0} 退出，代码 {1}",
  log_uninstaller_completed: "卸载 {0} 结束",
  log_uninstaller_exited: "卸载 {0} 退出，代码 {1}",
  log_installing_module: "正在安装模块: {0}",
  log_uninstalling_module: "正在卸载模块: {0}",
  log_no_installer_script: "未找到 {0} 的安装脚本。尝试路径: {1}",
  dialogHealthCheckPassed: "健康检查：{0} 项通过。",
  dialogHealthCheckWarnings: "健康检查：{0} 项通过，{1} 项警告。",
  dialogHealthCheckFailed: "健康检查：{0} 项通过，{1} 项警告，{2} 项失败。",
  dialogOpenTemplateDetails: "请打开快速安装模板并点击「详情」查看说明。",
  dialogTemplateNotFound: "未找到模板: {0}",
  dialogSetupSelectTemplate: "选择要运行的安装模板",
  dialogSetupRunning: "正在运行安装: {0}",
  dialogSetupSuccess: "安装完成。",
  dialogSetupWithFailures: "安装完成，其中 {0} 个步骤失败。",
  setupReviewFailingSteps: "请在「最近活动」中查看失败步骤。",
  setupFixAndRerun: "修复报错后重新运行该安装模板。",
  setupRunValidation: "可运行校验或开始使用已配置模块。"
};

const LOCALE_MAP: Record<Locale, LocaleStrings> = { en: EN, zh: ZH };

/**
 * Returns the full set of UI strings for the given locale.
 */
export function getStrings(locale: Locale): LocaleStrings {
  return LOCALE_MAP[locale] ?? EN;
}

/**
 * Resolves locale from VS Code language or a string like "zh-cn".
 */
export function resolveLocale(language: string | undefined): Locale {
  if (!language) return "en";
  const lower = language.toLowerCase();
  return lower.startsWith("zh") ? "zh" : "en";
}
