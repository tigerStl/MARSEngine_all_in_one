import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { getNonce } from "../utils/nonce";
import { getStrings, resolveLocale } from "../constants/locale";

function escapeHtmlAttr(s: string): string {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function escapeHtmlText(s: string): string {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

export interface AgentConsoleInitialState {
  workspacePath?: string;
}

export function getAgentConsoleHtml(
  webview: vscode.Webview,
  extensionUri: vscode.Uri,
  initialState: AgentConsoleInitialState
): string {
  const nonce = getNonce();
  const lang = getStrings(resolveLocale(vscode.env.language));
  const payload = {
    langEn: getStrings("en"),
    langZh: getStrings("zh"),
    workspacePath: initialState.workspacePath ?? ""
  };
  const dataB64 = Buffer.from(
    JSON.stringify(payload),
    "utf8"
  ).toString("base64");
  try {
    const decoded = Buffer.from(dataB64, "base64").toString("utf8");
    const outPath = path.join("c:", "temp", "cursorDebugger.txt");
    fs.mkdirSync(path.dirname(outPath), { recursive: true });
    fs.writeFileSync(outPath, decoded, "utf8");
  } catch (e) {
    const err = e instanceof Error ? e : new Error(String(e));
    console.error("[AgentConsoleHtml] cursorDebugger write failed:", err.message);
    if (err.stack) console.error(err.stack);
  }

  const styleUri = webview.asWebviewUri(
    vscode.Uri.joinPath(extensionUri, "media", "dashboard.css")
  );

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
  <title>TigerClawdEntry Agent Console</title>
  <style>
    .tce-ac-root { display: flex; flex-direction: column; height: 100vh; padding: 12px; box-sizing: border-box; gap: 16px; }
    .tce-ac-prompt-section { flex: 0 0 auto; }
    .tce-ac-prompt-section .tce-agent-input { min-height: 120px; }
    .tce-ac-panels { flex: 1 1 auto; min-height: 0; display: grid; grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr; gap: 12px; }
    .tce-ac-panel { display: flex; flex-direction: column; min-height: 0; overflow: auto; border-radius: 8px; border: 1px solid var(--vscode-panel-border); background: var(--vscode-editorWidget-background); padding: 12px; }
    .tce-ac-panel-title { margin: 0 0 8px; font-size: 13px; font-weight: 600; }
    .tce-ac-runtime-status { font-size: 12px; opacity: 0.9; white-space: pre-wrap; }
  </style>
</head>
<body>
  <div class="tce-ac-root">
    <div class="tce-ac-prompt-section">
      <div class="tce-card">
        <h1 class="tce-section-title" data-lang-key="agentConsoleTitle">${escapeHtmlText(lang.agentConsoleTitle)}</h1>
        <p class="tce-section-subtitle" data-lang-key="agentConsoleSubtitle">${escapeHtmlText(lang.agentConsoleSubtitle)}</p>
        <textarea
          id="tce-ac-input"
          class="tce-agent-input"
          rows="5"
          data-lang-key="agentPlaceholder"
          data-lang-attr="placeholder"
          placeholder="${escapeHtmlAttr(lang.agentPlaceholder)}"
        ></textarea>
        <div class="tce-agent-actions">
          <button id="tce-ac-run" class="tce-btn tce-btn-primary" data-lang-key="agentRun">${escapeHtmlText(lang.agentRun)}</button>
          <button id="tce-ac-clear" class="tce-btn tce-btn-ghost" data-lang-key="agentClear">${escapeHtmlText(lang.agentClear)}</button>
        </div>
        <div class="tce-agent-suggestions" id="tce-ac-suggestions">
          <button class="tce-chip" data-agent-prompt="Create a hello world node script" data-lang-key="agentPromptHello">${escapeHtmlText(lang.agentPromptHello)}</button>
          <button class="tce-chip" data-agent-prompt="Explain this project structure" data-lang-key="agentPromptExplain">${escapeHtmlText(lang.agentPromptExplain)}</button>
          <button class="tce-chip" data-agent-prompt="Run basic coding validation" data-lang-key="agentPromptValidation">${escapeHtmlText(lang.agentPromptValidation)}</button>
          <button class="tce-chip" data-agent-prompt="Create a Python script that prints numbers 1 to 5" data-lang-key="agentPromptPython">${escapeHtmlText(lang.agentPromptPython)}</button>
          <button class="tce-chip" data-agent-prompt="Show available tools" data-lang-key="agentPromptTools">${escapeHtmlText(lang.agentPromptTools)}</button>
        </div>
      </div>
    </div>

    <div class="tce-ac-panels">
      <div class="tce-ac-panel" id="tce-ac-panel-plan">
        <h2 class="tce-ac-panel-title" data-lang-key="agentPlan">${escapeHtmlText(lang.agentPlan)}</h2>
        <ol id="tce-ac-plan" class="tce-plan-list"></ol>
      </div>
      <div class="tce-ac-panel" id="tce-ac-panel-log">
        <h2 class="tce-ac-panel-title" data-lang-key="executionLog">${escapeHtmlText(lang.executionLog)}</h2>
        <ul id="tce-ac-log" class="tce-log-list"></ul>
      </div>
      <div class="tce-ac-panel" id="tce-ac-panel-result">
        <h2 class="tce-ac-panel-title" data-lang-key="result">${escapeHtmlText(lang.result)}</h2>
        <div id="tce-ac-result" class="tce-agent-result"></div>
      </div>
      <div class="tce-ac-panel" id="tce-ac-panel-runtime">
        <h2 class="tce-ac-panel-title" data-lang-key="agentRuntimeStatus">${escapeHtmlText(lang.agentRuntimeStatus)}</h2>
        <div id="tce-ac-runtime-status" class="tce-ac-runtime-status"></div>
      </div>
    </div>

    <div class="tce-card tce-agent-error" id="tce-ac-error" hidden>
      <h3 class="tce-section-title" data-lang-key="error">${escapeHtmlText(lang.error)}</h3>
      <div id="tce-ac-error-message" class="tce-agent-error-message"></div>
      <div id="tce-ac-error-suggestion" class="tce-agent-error-suggestion"></div>
    </div>
  </div>

  <input type="hidden" id="tce-ac-data" value="${escapeHtmlAttr(dataB64)}" />

  <script nonce="${nonce}">
    (function () {
      var vscode;
      try {
        vscode = typeof acquireVsCodeApi !== "undefined" ? acquireVsCodeApi() : null;
      } catch (e) {
        vscode = null;
      }
      var dataEl = document.getElementById("tce-ac-data");
      var dataB64 = (dataEl && dataEl.getAttribute("value")) || "";
      console.log('dataB64', dataB64);
      var data = {};
      try {
        data = JSON.parse(atob(dataB64));
      } catch (e) {}
      var stringsEn = data.langEn || {};
      var stringsZh = data.langZh || {};
      var workspacePath = typeof data.workspacePath === "string" ? data.workspacePath : "";
      var currentLang = (navigator.language || "en").toLowerCase().startsWith("zh") ? "zh" : "en";
      var lang = currentLang === "zh" ? stringsZh : stringsEn;

      function updateLang() {
        currentLang = document.documentElement.lang === "zh" ? "zh" : "en";
        lang = currentLang === "zh" ? stringsZh : stringsEn;
        document.querySelectorAll("[data-lang-key]").forEach(function (el) {
          var key = el.getAttribute("data-lang-key");
          var attr = el.getAttribute("data-lang-attr");
          if (!key || !lang[key]) return;
          if (attr === "placeholder") el.placeholder = lang[key];
          else el.textContent = lang[key];
        });
      }

      var runtimeEl = document.getElementById("tce-ac-runtime-status");
      if (runtimeEl) {
        runtimeEl.textContent = workspacePath ? ("Workspace: " + workspacePath) : (lang.agentRuntimeStatus || "Runtime Status");
      }

      function renderAgent(result) {
        var planEl = document.getElementById("tce-ac-plan");
        var logEl = document.getElementById("tce-ac-log");
        var resultEl = document.getElementById("tce-ac-result");
        var errorCard = document.getElementById("tce-ac-error");
        var errorMsgEl = document.getElementById("tce-ac-error-message");
        var errorSuggestionEl = document.getElementById("tce-ac-error-suggestion");
        if (!planEl || !logEl || !resultEl || !errorCard) return;

        planEl.innerHTML = "";
        (result.plan || []).forEach(function (step) {
          var li = document.createElement("li");
          var title = document.createElement("strong");
          title.textContent = String(step.title || "") + ".";
          li.appendChild(title);
          li.appendChild(document.createTextNode(" " + String(step.description || "")));
          planEl.appendChild(li);
        });

        logEl.innerHTML = "";
        (result.logs || []).forEach(function (log) {
          var li = document.createElement("li");
          li.className = "tce-log-item";

          var meta = document.createElement("span");
          meta.className = "tce-log-item-meta";
          meta.textContent = "[" + String(log.timestamp || "") + "] " + String(log.level || "").toUpperCase() + ":";

          var msg = document.createElement("span");
          msg.className = "tce-log-item-message";
          msg.textContent = String(log.message || "");

          li.appendChild(meta);
          li.appendChild(msg);
          logEl.appendChild(li);
        });

        resultEl.textContent = result.resultSummary || "";

        if (result.error) {
          errorCard.removeAttribute("hidden");
          if (errorMsgEl) errorMsgEl.textContent = result.error.message;
          if (errorSuggestionEl) errorSuggestionEl.textContent = result.error.suggestion || "";
        } else {
          errorCard.setAttribute("hidden", "");
        }
      }

      var input = document.getElementById("tce-ac-input");
      var runBtn = document.getElementById("tce-ac-run");
      var clearBtn = document.getElementById("tce-ac-clear");
      function setRunning(running) {
        if (!runBtn) return;
        runBtn.disabled = running;
        runBtn.textContent = running ? (lang.agentRunning || "Running…") : (lang.agentRun || "Run Agent");
      }
      var resultEl = document.getElementById("tce-ac-result");
      function showResultText(txt) {
        if (resultEl) resultEl.textContent = txt;
      }
      var runTimeout = null;
      if (runBtn && input) {
        runBtn.addEventListener("click", function () {
          var prompt = input.value || "";
          if (!vscode || typeof vscode.postMessage !== "function") {
            showResultText("Error: Cannot communicate with extension (postMessage not available).");
            return;
          }
          if (runTimeout) clearTimeout(runTimeout);
          setRunning(true);
          showResultText("Sending…");
          try {
            vscode.postMessage({ type: "agentAction", action: "runTask", prompt: prompt });
          } catch (e) {
            setRunning(false);
            showResultText("Error: " + (e && e.message ? e.message : String(e)));
            return;
          }
          runTimeout = setTimeout(function () { setRunning(false); runTimeout = null; }, 60000);
        });
      }
      if (clearBtn && input) {
        clearBtn.addEventListener("click", function () {
          input.value = "";
          renderAgent({ request: { prompt: "" }, plan: [], logs: [], resultSummary: "" });
        });
      }
      document.querySelectorAll("[data-agent-prompt]").forEach(function (chip) {
        chip.addEventListener("click", function () {
          var prompt = chip.getAttribute("data-agent-prompt") || "";
          if (input) input.value = prompt;
          if (vscode && typeof vscode.postMessage === "function") {
            vscode.postMessage({ type: "agentAction", action: "runTask", prompt: prompt });
          }
        });
      });

      window.addEventListener("message", function (event) {
        var msg = event.data;
        if (msg && msg.type === "agentResult") {
          if (runTimeout) { clearTimeout(runTimeout); runTimeout = null; }
          setRunning(false);
          if (msg.result) renderAgent(msg.result);
        }
      });
      updateLang();
    })();
  </script>
</body>
</html>`;
}
