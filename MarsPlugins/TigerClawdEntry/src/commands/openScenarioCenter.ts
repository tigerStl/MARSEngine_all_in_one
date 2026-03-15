import * as vscode from "vscode";
import type { ScenarioService } from "../services/scenarios/ScenarioService";
import type { ScenarioTemplate } from "../models/ScenarioTemplate";
import { getStrings, resolveLocale } from "../constants/locale";

function scenarioLocaleKey(id: string, suffix: string): string {
  return "scenario_" + id.replace(/-/g, "_") + "_" + suffix;
}

function getDetailHtml(
  template: ScenarioTemplate,
  locale: "en" | "zh"
): string {
  const strings = getStrings(locale);

  const zhOverrides: Record<
    string,
    {
      fullDescription?: string;
      targetUsers?: string[];
      installNotes?: string;
      futureNotes?: string;
    }
  > = {
    "basic-coding-setup": {
      fullDescription:
        "一个简单、低门槛的配置，用于启用基于 LLM 的代码补全和基础开发工具，无需额外基础设施。",
      targetUsers: ["个人开发者", "刚开始尝试 AI 编程的团队"],
      installNotes: "安装以代码编辑为中心的最小模块组合，不依赖向量数据库。",
      futureNotes: "后续可以平滑升级到 AI 编程助手或自主开发等更高级场景。"
    },
    "ai-coding-assistant": {
      fullDescription:
        "一个项目感知型的编程环境，结合 LangGraph 与轻量级向量数据库，为代码提供上下文。",
      targetUsers: ["构建 AI 优先应用的开发者", "正在引入 AI 结对编程的团队"],
      installNotes: "需要配置 Chroma 用于生成嵌入并索引工作区代码。",
      futureNotes: "后续可以连接到 MARS 运行时，实现更深度的测试自动化与调试能力。"
    },
    "retrieval-knowledge-base": {
      fullDescription:
        "一个用于文档问答与知识库搜索的检索增强生成（RAG）方案。",
      targetUsers: ["知识管理团队", "客服支持工具", "内部文档检索场景"],
      installNotes: "需要搭建文档导入与嵌入生成的流水线。",
      futureNotes:
        "可集成企业级向量数据库，并结合 MARS 测试体系对问答质量做回归验证。"
    },
    "video-media-workflow": {
      fullDescription:
        "一个用于处理媒体提示、转录与转换的多模态流水线。",
      targetUsers: ["媒体团队", "内容运营", "营销自动化"],
      installNotes:
        "需要访问如 FFmpeg 以及云存储等媒体处理工具与基础设施。",
      futureNotes: "未来可通过 MARS 任务验证媒体流水线并自动化回归测试。"
    },
    "autonomous-development": {
      fullDescription:
        "一个完整的代理式开发环境，让智能体可以直接与 IDE、Git 与测试系统交互。",
      targetUsers: ["高级工程团队", "研究团队", "自动化程度较高的组织"],
      installNotes:
        "需要谨慎的沙箱隔离和完善的测试体系，建议优先在非生产仓库中试运行。",
      futureNotes:
        "未来的 MARS 运行时可以为该场景提供更强的策略约束与安全执行能力。"
    }
  };

  const mods = template.recommendedModules;
  const categories = Object.keys(mods).filter(
    k => mods[k as keyof typeof mods]?.length
  );
  const modulesList = categories
    .map(cat => {
      const list = mods[cat as keyof typeof mods] || [];
      return `<li><strong>${escapeHtml(cat)}</strong>: ${list
        .map(m => escapeHtml(m))
        .join(", ")}</li>`;
    })
    .join("");

  const nameKey = scenarioLocaleKey(template.id, "name");
  const shortKey = scenarioLocaleKey(template.id, "shortDesc");
  const displayName = strings[nameKey] || template.name;
  const displayShort = strings[shortKey] || template.shortDescription;

  const bestForLabel =
    strings[`bestFor_${template.bestFor}`] || template.bestFor;
  const levelLabel = strings[`level_${template.level}`] || template.level;
  const complexityLabel =
    strings[`complexity_${template.complexity}`] || template.complexity;

  const labelDescription = strings.scenarioDetail_description;
  const labelTargetUsers = strings.scenarioDetail_targetUsers;
  const labelRecommended = strings.scenarioDetail_recommendedModules;
  const labelInstallNotes = strings.scenarioDetail_installNotes;
  const labelFutureNotes = strings.scenarioDetail_futureNotes;

  const zh = locale === "zh" ? zhOverrides[template.id] : undefined;
  const fullDescription =
    zh?.fullDescription ?? template.fullDescription;
  const targetUsers =
    zh?.targetUsers ?? template.targetUsers;
  const installNotes =
    zh?.installNotes ?? template.installNotes;
  const futureNotes =
    zh?.futureNotes ?? template.futureNotes;

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: var(--vscode-font-family); font-size: 13px; padding: 1em; line-height: 1.5; }
    h1 { font-size: 1.3em; margin-top: 0; }
    h2 { font-size: 1.1em; margin: 1em 0 0.4em; color: var(--vscode-descriptionForeground); }
    p, ul { margin: 0.4em 0; color: var(--vscode-foreground); }
    ul { padding-left: 1.2em; }
    .pill { display: inline-block; margin-right: 0.5em; padding: 0.2em 0.6em; border-radius: 4px; background: var(--vscode-badge-background); font-size: 12px; }
  </style>
</head>
<body>
  <h1>${escapeHtml(displayName)}</h1>
  <p>${escapeHtml(displayShort)}</p>
  <div style="margin: 0.6em 0;">
    <span class="pill">${escapeHtml(complexityLabel)}</span>
    <span class="pill">${escapeHtml(template.resourceProfile)}</span>
    <span class="pill">${escapeHtml(bestForLabel)}</span>
    <span class="pill">${escapeHtml(levelLabel)}</span>
  </div>

  <h2>${escapeHtml(labelDescription)}</h2>
  <p>${escapeHtml(fullDescription)}</p>

  <h2>${escapeHtml(labelTargetUsers)}</h2>
  <ul>${targetUsers.map(u => `<li>${escapeHtml(u)}</li>`).join("")}</ul>

  <h2>${escapeHtml(labelRecommended)}</h2>
  <ul>${modulesList}</ul>

  <h2>${escapeHtml(labelInstallNotes)}</h2>
  <p>${escapeHtml(installNotes)}</p>

  <h2>${escapeHtml(labelFutureNotes)}</h2>
  <p>${escapeHtml(futureNotes)}</p>
</body>
</html>`;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

export function registerOpenScenarioCenterCommand(
  context: vscode.ExtensionContext,
  scenarioService: ScenarioService
): void {
  const disposable = vscode.commands.registerCommand(
    "tigerClawdEntry.openScenarioCenter",
    async (arg?: string | { id?: string; lang?: string }) => {
      let scenarioId: string | undefined;
      let langOverride: "en" | "zh" | undefined;

      if (typeof arg === "string") {
        scenarioId = arg;
      } else if (arg) {
        scenarioId = arg.id;
        if (arg.lang === "zh") {
          langOverride = "zh";
        } else if (arg.lang === "en") {
          langOverride = "en";
        }
      }

      const locale = langOverride ?? resolveLocale(vscode.env.language);
      const lang = getStrings(locale);
      if (!scenarioId) {
        vscode.window.showInformationMessage(lang.dialogOpenTemplateDetails);
        return;
      }

      const scenario = scenarioService.getScenario(scenarioId);
      if (!scenario) {
        vscode.window.showWarningMessage(lang.dialogTemplateNotFound.replace("{0}", scenarioId));
        return;
      }

      const panel = vscode.window.createWebviewPanel(
        "tigerClawdScenarioDetail",
        getStrings(locale)[scenarioLocaleKey(scenario.id, "name")] ||
          scenario.name,
        vscode.ViewColumn.One,
        { enableScripts: false }
      );
      panel.webview.html = getDetailHtml(scenario, locale);
    }
  );
  context.subscriptions.push(disposable);
}
