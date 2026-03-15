import * as vscode from "vscode";
import { getNonce } from "../utils/nonce";
import { getStrings, resolveLocale } from "../constants/locale";

/** Escape string for safe embedding inside HTML <script> (prevents </script> from closing the tag). */
function escapeForScriptEmbed(s: string): string {
  return s.replace(/<\/script/gi, "<\\/script");
}

export function getDashboardHtml(
  webview: vscode.Webview,
  extensionUri: vscode.Uri,
  initialStateJson: string
): string {
  const nonce = getNonce();
  const safeInitialState = escapeForScriptEmbed(initialStateJson);
  const safeStringsEn = escapeForScriptEmbed(JSON.stringify(getStrings("en")));
  const safeStringsZh = escapeForScriptEmbed(JSON.stringify(getStrings("zh")));
  const scriptUri = webview.asWebviewUri(
    vscode.Uri.joinPath(extensionUri, "out", "webview", "dashboard.js")
  );
  const styleUri = webview.asWebviewUri(
    vscode.Uri.joinPath(extensionUri, "media", "dashboard.css")
  );
  const logoUri = webview.asWebviewUri(
    vscode.Uri.joinPath(extensionUri, "image", "tigerClaw.png")
  );

  const lang = getStrings(resolveLocale(vscode.env.language));

  return /* html */ `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta
    http-equiv="Content-Security-Policy"
    content="default-src 'none'; style-src ${webview.cspSource} 'unsafe-inline'; script-src 'nonce-${nonce}'; img-src ${webview.cspSource} https: data:; connect-src ${webview.cspSource} https:"
  />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <link href="${styleUri}" rel="stylesheet" />
  <title>${lang.title}</title>
</head>
<body>
  <div class="tce-root">
    <div class="tce-main">
    <header class="tce-header">
      <div class="tce-header-logo-card">
        <div class="tce-logo-wrap">
          <img class="tce-logo" src="${logoUri}" alt="TigerClawdEntry" />
        </div>
      </div>
      <div class="tce-header-right-card">
        <div class="tce-header-actions">
          <div class="tce-header-controls">
            <div class="tce-lang-toggle">
              <button class="tce-lang-btn tce-lang-btn-active" data-lang="en">EN</button>
              <button class="tce-lang-btn" data-lang="zh">中</button>
            </div>
            <button class="tce-btn tce-btn-ghost" data-header-action="refresh" data-lang-key="headerRefresh">
              ${lang.headerRefresh}
            </button>
            <div class="tce-dropdown">
              <button
                type="button"
                class="tce-btn tce-btn-primary tce-dropdown-trigger"
                id="tce-actions-trigger"
                aria-haspopup="true"
                aria-expanded="false"
                data-lang-key="headerActionsLabel"
              >
                ${lang.headerActionsLabel} ▾
              </button>
              <div class="tce-dropdown-panel" id="tce-actions-panel" role="menu" hidden>
                <button
                  type="button"
                  class="tce-dropdown-item"
                  role="menuitem"
                  data-header-action="validate"
                  data-lang-key="headerValidate"
                >
                  ${lang.headerValidate}
                </button>
                <button
                  type="button"
                  class="tce-dropdown-item"
                  role="menuitem"
                  data-header-action="wizard"
                  data-lang-key="headerWizard"
                >
                  ${lang.headerWizard}
                </button>
              </div>
            </div>
          </div>
          <div class="tce-header-stats" id="tce-header-stats">
            <!-- populated by script -->
          </div>
        </div>
      </div>
    </header>

    <div class="tce-tabs">
      <div class="tce-tabs-header tce-tabs-header-full" id="tce-tabs-header">
        <button class="tce-tab tce-tab-active" data-tab="overview" data-lang-key="tabOverview">${lang.tabOverview}</button>
        <button class="tce-tab" data-tab="runtime" data-lang-key="tabRuntime">${lang.tabRuntime}</button>
        <button class="tce-tab" data-tab="templates" data-lang-key="tabTemplates">${lang.tabTemplates}</button>
        <button class="tce-tab" data-tab="health" data-lang-key="tabHealth">${lang.tabHealth}</button>
        <button class="tce-tab" data-tab="settings" data-lang-key="tabSettings">${lang.tabSettings}</button>
      </div>

      <section class="tce-section tce-tab-panel tce-tab-panel-active" data-tab-panel="overview">
        <div class="tce-overview-grid">
          <div class="tce-card tce-card-metric-block tce-hero-agent">
            <p class="tce-section-subtitle" data-lang-key="agentConsoleSubtitle">${lang.agentConsoleSubtitle}</p>
            <button type="button" class="tce-btn tce-btn-primary" id="tce-open-agent-console" data-lang-key="openAgentConsole">${lang.openAgentConsole}</button>
          </div>
          <div class="tce-card tce-card-metric-block">
            <div id="tce-env" class="tce-grid tce-grid-2 tce-metric-grid"></div>
          </div>
        </div>
      </section>

      <section class="tce-section tce-tab-panel" data-tab-panel="runtime">
        <h2 class="tce-section-title" data-lang-key="sectionModules">${lang.sectionModules}</h2>
        <div id="tce-modules" class="tce-modules-container"></div>
      </section>

      <section class="tce-section tce-tab-panel" data-tab-panel="templates">
        <div id="tce-templates" class="tce-grid tce-grid-3"></div>
      </section>

      <section class="tce-section tce-tab-panel" data-tab-panel="health">
        <div class="tce-card">
          <h2 class="tce-section-title" data-lang-key="healthTitle">${lang.healthTitle}</h2>
          <p class="tce-section-subtitle" data-lang-key="healthSubtitle">${lang.healthSubtitle}</p>
          <div class="tce-install-checks" id="tce-install-checks"></div>
        </div>
      </section>

      <section class="tce-section tce-tab-panel" data-tab-panel="settings">
        <div class="tce-card">
          <h2 class="tce-section-title" data-lang-key="settingsTitle">${lang.settingsTitle}</h2>
          <p class="tce-section-subtitle" data-lang-key="settingsSubtitle">${lang.settingsSubtitle}</p>
        </div>
      </section>
    </div>
    </div>

    <div class="tce-console" id="tce-console" data-console-size="normal">
      <div class="tce-console-resizer" id="tce-console-resizer"></div>
      <div class="tce-console-header">
        <div class="tce-console-title" data-lang-key="sectionLogs">${lang.sectionLogs}</div>
        <div class="tce-console-toolbar">
          <button type="button" class="tce-console-btn tce-console-btn--min" id="tce-console-min" title="Minimize (keep one line)" aria-label="Minimize">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="5" y1="12" x2="19" y2="12"/></svg>
          </button>
          <button type="button" class="tce-console-btn tce-console-btn--max" id="tce-console-max" title="Maximize" aria-label="Maximize">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3"/></svg>
          </button>
        </div>
      </div>
      <div id="tce-logs" class="tce-activity-panel"></div>
    </div>
  </div>

  <script nonce="${nonce}">
    (function () {
      const vscode = acquireVsCodeApi();
      const initialState = ${safeInitialState};

      const strings = ${safeStringsEn};
      const stringsZh = ${safeStringsZh};
      var currentLang = (navigator.language || "en").toLowerCase().startsWith("zh") ? "zh" : "en";
      var lang = currentLang === "zh" ? stringsZh : strings;
      var lastState = initialState;

      function updatePageLang() {
        document.querySelectorAll("[data-lang-key]").forEach(function (el) {
          var key = el.getAttribute("data-lang-key");
          var attr = el.getAttribute("data-lang-attr");
          if (!key || lang[key] === undefined) return;
          if (attr === "placeholder") {
            el.setAttribute("placeholder", lang[key]);
          } else {
            el.textContent = key === "headerActionsLabel" ? lang[key] + " \u25BE" : lang[key];
          }
        });
      }

      document.querySelectorAll(".tce-lang-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var next = btn.getAttribute("data-lang");
          if (!next || next === currentLang) return;
          currentLang = next;
          lang = currentLang === "zh" ? stringsZh : strings;
          document.querySelectorAll(".tce-lang-btn").forEach(function (b) { b.classList.remove("tce-lang-btn-active"); });
          btn.classList.add("tce-lang-btn-active");
          updatePageLang();
          render(lastState);
        });
      });

      window.addEventListener("message", function (event) {
        const msg = event.data;
        if (msg.type === "stateUpdate") {
          lastState = msg.state;
          render(msg.state);
        } else if (msg.type === "installCheckResult") {
          var row = document.querySelector('.tce-install-check-row[data-check-id="' + (msg.checkId || "") + '"]');
          if (!row) return;
          var resultEl = row.querySelector(".tce-install-check-result");
          var runBtn = row.querySelector(".tce-install-check-run");
          if (resultEl) {
            resultEl.textContent = msg.message || "";
            resultEl.className = "tce-install-check-result tce-install-check-result--" + (msg.status || "WARN");
          }
          if (runBtn) runBtn.disabled = false;
        }
      });

      (function setupInstallChecks() {
        try {
        var checks = [
          { id: "env", labelEn: "Environment (node/npm/git)", labelZh: "环境 (node/npm/git)" },
          { id: "python", labelEn: "Python", labelZh: "Python" },
          { id: "pip", labelEn: "Pip", labelZh: "Pip" },
          { id: "llm-openai", labelEn: "LLM OpenAI", labelZh: "LLM OpenAI" },
          { id: "llm-anthropic", labelEn: "LLM Anthropic", labelZh: "LLM Anthropic" },
          { id: "llm-gemini", labelEn: "LLM Gemini", labelZh: "LLM Gemini" },
          { id: "agent-langchain", labelEn: "Agent LangChain", labelZh: "Agent LangChain" },
          { id: "agent-langgraph", labelEn: "Agent LangGraph", labelZh: "Agent LangGraph" },
          { id: "vectordb-chroma", labelEn: "VectorDB Chroma", labelZh: "VectorDB Chroma" },
          { id: "vectordb-milvus", labelEn: "VectorDB Milvus", labelZh: "VectorDB Milvus" },
          { id: "tools-jest", labelEn: "Tools Jest", labelZh: "Tools Jest" },
          { id: "tools-playwright", labelEn: "Tools Playwright", labelZh: "Tools Playwright" },
          { id: "git", labelEn: "Git", labelZh: "Git" },
          { id: "code-cli", labelEn: "VS Code CLI", labelZh: "VS Code CLI" },
          { id: "cursor-cli", labelEn: "Cursor CLI", labelZh: "Cursor CLI" }
        ];
        var container = document.getElementById("tce-install-checks");
        if (!container) return;
        var isZh = currentLang === "zh";
        checks.forEach(function (c) {
          var row = document.createElement("div");
          row.className = "tce-install-check-row";
          row.setAttribute("data-check-id", c.id);
          row.innerHTML =
            "<span class=\\"tce-install-check-name\\">" + (isZh ? c.labelZh : c.labelEn) + "</span>" +
            "<button type=\\"button\\" class=\\"tce-install-check-run tce-btn tce-btn-ghost\\" data-lang-key=\\"btnRun\\">" + lang.btnRun + "</button>" +
            "<span class=\\"tce-install-check-result\\"></span>";
          var runBtn = row.querySelector(".tce-install-check-run");
          if (runBtn) {
            runBtn.addEventListener("click", function () {
              var resultEl = row.querySelector(".tce-install-check-result");
              if (resultEl) resultEl.textContent = "…";
              runBtn.disabled = true;
              vscode.postMessage({ type: "runInstallCheck", checkId: c.id });
            });
          }
          container.appendChild(row);
        });
        } catch (e) { console.warn("Install checks setup:", e); }
      })();

      // tab interactions
      document.querySelectorAll(".tce-tab").forEach(function (tab) {
        tab.addEventListener("click", function () {
          const target = tab.getAttribute("data-tab");
          if (!target) return;
          document
            .querySelectorAll(".tce-tab")
            .forEach(function (t) { t.classList.remove("tce-tab-active"); });
          tab.classList.add("tce-tab-active");
          document
            .querySelectorAll(".tce-tab-panel")
            .forEach(function (panel) {
              const panelKey = panel.getAttribute("data-tab-panel");
              if (panelKey === target) {
                panel.classList.add("tce-tab-panel-active");
              } else {
                panel.classList.remove("tce-tab-panel-active");
              }
            });
        });
      });

      var actionsTrigger = document.getElementById("tce-actions-trigger");
      var actionsPanel = document.getElementById("tce-actions-panel");
      var actionsDropdown = actionsTrigger && actionsTrigger.closest(".tce-dropdown");
      if (actionsTrigger && actionsPanel) {
        actionsTrigger.addEventListener("click", function (e) {
          e.stopPropagation();
          var open = actionsPanel.getAttribute("hidden") === null;
          if (open) {
            actionsPanel.setAttribute("hidden", "");
            actionsTrigger.setAttribute("aria-expanded", "false");
          } else {
            actionsPanel.removeAttribute("hidden");
            actionsTrigger.setAttribute("aria-expanded", "true");
          }
        });
        document.addEventListener("click", function () {
          actionsPanel.setAttribute("hidden", "");
          actionsTrigger.setAttribute("aria-expanded", "false");
        });
        if (actionsDropdown) {
          actionsDropdown.addEventListener("click", function (e) { e.stopPropagation(); });
        }
      }
      document.querySelectorAll("[data-header-action]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var action = btn.getAttribute("data-header-action");
          vscode.postMessage({ type: "headerAction", action: action });
          if (actionsPanel && btn.closest("#tce-actions-panel")) {
            actionsPanel.setAttribute("hidden", "");
            if (actionsTrigger) actionsTrigger.setAttribute("aria-expanded", "false");
          }
        });
      });

      function render(state) {
        renderHeaderStats(state);
        renderEnv(state.environment);
        renderModules(state);
        renderTemplates(state.scenarios || [], state.lastSetupResult);
        renderLogs(state.logs || []);
      }

      function renderHeaderStats(state) {
        const el = document.getElementById("tce-header-stats");
        if (!el) return;

        const logs = state.logs || [];
        const totalActions = logs.length;
        const warnings = logs.filter(function (l) { return /\bWARN\b/.test(l); }).length;
        const errors = logs.filter(function (l) { return /\bERROR\b/.test(l); }).length;
        const healthy = Math.max(totalActions - warnings - errors, 0);
        const lastLine = logs[logs.length - 1] || "";
        const tsMatch = lastLine.match(/^\[(.*?)\]/);
        const lastScan = tsMatch ? tsMatch[1] : "—";

        const chips = [
          { label: lang.chipInstalled, value: String(totalActions || 0) },
          { label: lang.chipHealthy, value: String(healthy) },
          { label: lang.chipWarning, value: String(warnings + errors) },
          { label: lang.chipLastScan, value: lastScan }
        ];

        el.innerHTML = chips
          .map(function (c) {
            return (
              '<div class="tce-stat-chip">' +
              '<div class="tce-stat-label">' + c.label + "</div>" +
              '<div class="tce-stat-value">' + c.value + "</div>" +
              "</div>"
            );
          })
          .join("");
      }

      function renderEnv(env) {
        const el = document.getElementById("tce-env");
        if (!env) {
          el.innerHTML = "<p>" + lang.envNotReady + "</p>";
          return;
        }
        const items = [
          ["labelOs", env.os],
          ["labelNode", env.nodeVersion],
          ["labelNpm", env.npmVersion],
          ["labelGit", env.gitVersion],
          ["labelPython", env.pythonVersion],
          ["labelJava", env.javaVersion],
          ["labelWorkspace", env.workspacePath]
        ];
        el.innerHTML = items
          .map(function (pair) {
            const labelKey = pair[0];
            const value = pair[1];
            const label = lang[labelKey] !== undefined ? lang[labelKey] : labelKey;
            return (
              '<div class="tce-card tce-card-metric">' +
              '<div class="tce-label">' + label + "</div>" +
              '<div class="tce-value">' + (value || lang.notDetected) + "</div>" +
              "</div>"
            );
          })
          .join("");
      }

      function renderModules(state) {
        const el = document.getElementById("tce-modules");
        const stack = state.stack || [];
        const moduleOrder = state.moduleOrder || {};
        const moduleDisplayNames = state.moduleDisplayNames || { en: {}, zh: {} };
        const installedModules = state.installedModules || {};
        const locale = currentLang === "zh" ? "zh" : "en";
        const displayNames = moduleDisplayNames[locale] || {};

        if (!stack.length) {
          el.innerHTML = "<p>" + (lang.modulesNotLoaded || "Module knowledge not loaded.") + "</p>";
          return;
        }

        var categories = ["LLM", "Agent", "Code", "VectorDB", "Tools"];
        var html = "";
        categories.forEach(function (categoryKey) {
          var cat = stack.find(function (c) { return c.category === categoryKey; });
          if (!cat) return;
          var categoryTitleKey = "category_" + categoryKey;
          var categoryTitle = (lang[categoryTitleKey] !== undefined ? lang[categoryTitleKey] : cat.category);
          var orderedIds = (moduleOrder[categoryKey] && moduleOrder[categoryKey][locale]) ? moduleOrder[categoryKey][locale] : cat.candidates.map(function (c) { return c.id; });
          var byId = {};
          cat.candidates.forEach(function (c) { byId[c.id] = c; });
          var rows = orderedIds.map(function (id) {
            var c = byId[id];
            if (!c) return "";
            var displayName = displayNames[id] || c.name;
            var installed = !!installedModules[id];
            var statusText = installed ? (lang.statusInstalled || "Installed") : (lang.statusNotInstalled || "Not installed");
            var statusClass = installed ? "tce-badge-status-ok" : "tce-badge-status-muted";
            return (
              '<div class="tce-module-candidate-row">' +
              '<div class="tce-module-candidate-info">' +
              '<div class="tce-module-candidate-name">' + displayName + "</div>" +
              '<div class="tce-module-candidate-usage">' + (c.typicalUsage || "") + "</div>" +
              "</div>" +
              '<div class="tce-module-candidate-actions">' +
              '<span class="tce-badge-status ' + statusClass + '">' + statusText + "</span>" +
              (installed ? '' : '<button type="button" class="tce-btn tce-btn-primary tce-btn-sm" data-module-id="' + c.id + '" data-module-action="install">' + (lang.btnInstall || "Install") + "</button>") +
              (installed ? '<button type="button" class="tce-btn tce-btn-ghost tce-btn-sm" data-module-id="' + c.id + '" data-module-action="uninstall">' + (lang.btnUninstall || "Uninstall") + "</button>" : "") +
              '<button type="button" class="tce-btn tce-btn-ghost tce-btn-sm" data-module-id="' + c.id + '" data-module-action="configure">' + (lang.btnConfigure || "Configure") + "</button>" +
              "</div>" +
              "</div>"
            );
          }).join("");
          html += (
            '<div class="tce-module-category-card">' +
            '<div class="tce-module-category-title">' + categoryTitle + "</div>" +
            '<p class="tce-card-description">' + cat.description + "</p>" +
            rows +
            "</div>"
          );
        });
        el.innerHTML = html;

        el.querySelectorAll("[data-module-action]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            var moduleId = btn.getAttribute("data-module-id");
            var action = btn.getAttribute("data-module-action");
            if (moduleId && action) {
              vscode.postMessage({ type: "moduleAction", moduleId: moduleId, action: action });
            }
          });
        });
      }

      function scenarioLocaleKey(id, suffix) {
        return "scenario_" + id.replace(/-/g, "_") + "_" + suffix;
      }

      function renderTemplates(scenarios, lastSetupResult) {
        const el = document.getElementById("tce-templates");
        if (!scenarios.length) {
          el.innerHTML = "<p>" + (lang.noTemplates || "No templates defined yet.") + "</p>";
          return;
        }

        const preferredOrder = [
          "basic-coding-setup",
          "ai-coding-assistant",
          "retrieval-knowledge-base",
          "video-media-workflow",
          "autonomous-development"
        ];

        const ordered = scenarios
          .slice()
          .sort(function (a, b) {
            return preferredOrder.indexOf(a.id) - preferredOrder.indexOf(b.id);
          });

        var cardsHtml = ordered
          .map(function (s) {
            var name = lang[scenarioLocaleKey(s.id, "name")] || s.name;
            var shortDesc = lang[scenarioLocaleKey(s.id, "shortDesc")] || s.shortDescription;
            var bestFor = lang["bestFor_" + s.bestFor] || s.bestFor;
            var level = lang["level_" + s.level] || s.level;
            var complexity = lang["complexity_" + s.complexity] || s.complexity;
            return (
              '<article class="tce-card tce-card-template tce-card-clickable">' +
              '<h3 class="tce-card-title">' + name + "</h3>" +
              '<p class="tce-card-description">' + shortDesc + "</p>" +
              '<div class="tce-pill-row">' +
              '<span class="tce-pill">' + bestFor + "</span>" +
              '<span class="tce-pill">' + level + "</span>" +
              '<span class="tce-pill">' + complexity + "</span>" +
              "</div>" +
              '<div class="tce-card-template-footer">' +
              '<button class="tce-btn tce-btn-primary" data-id="' + s.id + '" data-action="apply">' + (lang.btnUseTemplate || "Use Template") + "</button>" +
              '<button class="tce-btn tce-btn-ghost" data-id="' + s.id + '" data-action="details">' + (lang.btnDetails || "Details") + "</button>" +
              '<button class="tce-btn tce-btn-ghost" data-id="' + s.id + '" data-action="remove">' + (lang.btnDeleteTemplate || "Remove template") + "</button>" +
              "</div>" +
              "</article>"
            );
          })
          .join("");

        var summaryHtml = "";
        if (lastSetupResult) {
          var badgeClass =
            lastSetupResult.overallStatus === "success"
              ? "tce-badge-status-ok"
              : lastSetupResult.overallStatus === "warning"
              ? "tce-badge-status-warn"
              : "tce-badge-status-fail";
          summaryHtml =
            '<div class="tce-setup-summary">' +
            '<div class="tce-setup-summary-header">' +
            '<span class="tce-badge-status ' +
            badgeClass +
            '">' +
            lastSetupResult.overallStatus.toUpperCase() +
            "</span>" +
            "<span>" +
            lastSetupResult.summaryMessage +
            "</span>" +
            "</div>" +
            '<div class="tce-setup-summary-actions">' +
            '<button class="tce-btn tce-btn-primary" data-setup-template="' +
            lastSetupResult.templateId +
            '" data-setup-action="run">Run Again</button>' +
            '<button class="tce-btn tce-btn-ghost" data-setup-template="' +
            lastSetupResult.templateId +
            '" data-setup-action="resume">Resume</button>' +
            '<button class="tce-btn tce-btn-ghost" data-setup-template="' +
            lastSetupResult.templateId +
            '" data-setup-action="retry">Retry Failed Steps</button>' +
            "</div>" +
            "</div>";
        }

        el.innerHTML = cardsHtml + summaryHtml;

        el.querySelectorAll("button").forEach(function (btn) {
          btn.addEventListener("click", function () {
            var setupTemplate = btn.getAttribute("data-setup-template");
            var setupAction = btn.getAttribute("data-setup-action");
            if (setupTemplate && setupAction) {
              vscode.postMessage({
                type: "setupAction",
                templateId: setupTemplate,
                action: setupAction
              });
              return;
            }

            var scenarioId = btn.getAttribute("data-id");
            var action = btn.getAttribute("data-action");

            if (!scenarioId || !action) {
              return;
            }

            if (action === "remove") {
              var card = btn.closest(".tce-card-template");
              var titleEl = card && card.querySelector(".tce-card-title");
              var name = titleEl ? titleEl.textContent : scenarioId;
              var confirmed = window.confirm(
                'Uninstall template "' +
                  name +
                  '" and uninstall all its modules?'
              );
              if (!confirmed) {
                return;
              }
            }

            vscode.postMessage({
              type: "scenarioAction",
              scenarioId: scenarioId,
              action: action,
              lang: currentLang
            });
          });
        });
      }

      function renderLogs(logs) {
        const el = document.getElementById("tce-logs");
        if (!logs.length) {
          el.innerHTML = "<p>" + lang.noLogs + "</p>";
          return;
        }

        const items = logs.slice(-40).map(function (line) {
          const tsMatch = line.match(/^\[(.*?)\]\s+\[(.*?)\]\s+(.*)$/);
          if (!tsMatch) {
            const level = line.indexOf("[ERROR]") !== -1 ? "ERROR" : line.indexOf("[WARN]") !== -1 ? "WARN" : "INFO";
            return { time: "", level: level, message: line };
          }
          return { time: tsMatch[1], level: tsMatch[2], message: tsMatch[3] };
        });

        var levelLabel = function (level) {
          if (level === "ERROR" || level === lang.logLevelError) return lang.logLevelError;
          if (level === "WARN" || level === lang.logLevelWarn) return lang.logLevelWarn;
          return (level === "INFO" || level === lang.logLevelInfo) ? lang.logLevelInfo : (level || "INFO");
        };

        el.innerHTML =
          '<ul class="tce-activity-list">' +
          items
            .map(function (item) {
              const levelClass =
                item.level === "ERROR"
                  ? "tce-badge-status-fail"
                  : item.level === "WARN"
                  ? "tce-badge-status-warn"
                  : "tce-badge-status-ok";
              const rowClass =
                item.level === "ERROR"
                  ? "tce-activity-item tce-activity-item--error"
                  : "tce-activity-item";
              return (
                '<li class="' + rowClass + '">' +
                '<div class="tce-activity-main">' +
                '<span class="tce-badge-status ' + levelClass + '">' + levelLabel(item.level) + "</span>" +
                '<span class="tce-activity-message">' + item.message + "</span>" +
                "</div>" +
                '<div class="tce-activity-meta">' + item.time + "</div>" +
                "</li>"
              );
            })
            .join("") +
          "</ul>";
      }

      (function setupOpenAgentConsole() {
        var btn = document.getElementById("tce-open-agent-console");
        if (btn) {
          btn.addEventListener("click", function () {
            vscode.postMessage({ type: "openAgentConsole" });
          });
        }
      })();

      (function setupConsoleResize() {
        var consoleEl = document.getElementById("tce-console");
        var handle = document.getElementById("tce-console-resizer");
        if (!consoleEl || !handle) return;
        var startY = 0;
        var startHeight = 0;
        function onMove(e) {
          var dy = e.clientY - startY;
          var newHeight = Math.max(80, startHeight - dy);
          consoleEl.style.height = newHeight + "px";
        }
        function onUp() {
          window.removeEventListener("mousemove", onMove);
          window.removeEventListener("mouseup", onUp);
        }
        handle.addEventListener("mousedown", function (e) {
          startY = e.clientY;
          startHeight = consoleEl.getBoundingClientRect().height;
          window.addEventListener("mousemove", onMove);
          window.addEventListener("mouseup", onUp);
        });
      })();

      (function setupConsoleMinMax() {
        var consoleEl = document.getElementById("tce-console");
        var panel = document.getElementById("tce-logs");
        var btnMin = document.getElementById("tce-console-min");
        var btnMax = document.getElementById("tce-console-max");
        if (!consoleEl || !panel || !btnMin || !btnMax) return;
        var MIN_HEIGHT_PX = 70;
        var MAX_HEIGHT_VH = (100 * 2) / 3;
        function setSize(mode) {
          consoleEl.setAttribute("data-console-size", mode);
          consoleEl.classList.remove("tce-console--minimized", "tce-console--maximized");
          consoleEl.style.height = "";
          if (mode === "minimized") {
            consoleEl.classList.add("tce-console--minimized");
            consoleEl.style.height = MIN_HEIGHT_PX + "px";
          } else if (mode === "maximized") {
            consoleEl.classList.add("tce-console--maximized");
            consoleEl.style.height = MAX_HEIGHT_VH + "vh";
          }
        }
        btnMin.addEventListener("click", function () {
          var current = consoleEl.getAttribute("data-console-size") || "normal";
          setSize(current === "minimized" ? "normal" : "minimized");
        });
        btnMax.addEventListener("click", function () {
          var current = consoleEl.getAttribute("data-console-size") || "normal";
          setSize(current === "maximized" ? "normal" : "maximized");
        });
      })();

      document.querySelectorAll(".tce-lang-btn").forEach(function (b) {
        b.classList.toggle("tce-lang-btn-active", b.getAttribute("data-lang") === currentLang);
      });
      updatePageLang();
      render(initialState);
    })();
  </script>
</body>
</html>`;
}

