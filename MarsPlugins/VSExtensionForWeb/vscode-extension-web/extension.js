const vscode = require('vscode');
const fs = require('fs');
const path = require('path');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
  const disposable = vscode.commands.registerCommand('marsWebviewDemo.openPanel', () => {
    const panel = vscode.window.createWebviewPanel(
      'marsWebviewDemo',
      'MARS Web Automation Engine',
      vscode.ViewColumn.One,
      {
        enableScripts: true,
        retainContextWhenHidden: true
      }
    );

    const licenseInfo = loadLicense(context);
    panel.webview.html = getWebviewContent(licenseInfo);

    panel.webview.onDidReceiveMessage(async (message) => {
      switch (message.type) {
        case 'ready':
          panel.webview.postMessage({
            type: 'licenseStatus',
            license: licenseInfo
          });
          break;
        case 'exportTest':
          await handleExport(panel, message);
          break;
        case 'importTest':
          await handleImport(panel);
          break;
        default:
          break;
      }
    });
  });

  context.subscriptions.push(disposable);
}

function deactivate() {}

function loadLicense(context) {
  // MVP: read license.json from extension folder if present.
  // Example per requirement:
  // { "licenseType":"personal", "region":"US", "expire":"2027-01-01" }
  try {
    const licensePath = path.join(context.extensionPath, 'license.json');
    if (fs.existsSync(licensePath)) {
      const raw = fs.readFileSync(licensePath, 'utf8');
      const json = JSON.parse(raw);
      return {
        ...json,
        status: 'valid',
      };
    }
  } catch (err) {
    console.error('Failed to read license.json', err);
  }
  return {
    licenseType: 'trial',
    region: 'UNKNOWN',
    expire: null,
    status: 'trial',
  };
}

async function handleExport(panel, message) {
  const steps = message.steps || [];
  const format = message.format || 'json';

  if (format !== 'json') {
    vscode.window.showWarningMessage(`Only JSON export is implemented in MVP (requested: ${format}).`);
    return;
  }

  const uri = await vscode.window.showSaveDialog({
    filters: { 'JSON': ['json'] },
    saveLabel: 'Export MARS Web Test as JSON',
    defaultUri: vscode.Uri.file('mars-web-test.json')
  });
  if (!uri) {
    return;
  }

  const content = JSON.stringify({
    testName: message.testName || 'WebTest',
    steps
  }, null, 2);

  await vscode.workspace.fs.writeFile(uri, Buffer.from(content, 'utf8'));
  vscode.window.showInformationMessage(`Exported test to ${uri.fsPath}`);
}

async function handleImport(panel) {
  const uris = await vscode.window.showOpenDialog({
    canSelectMany: false,
    filters: { 'All Supported': ['json', 'yaml', 'yml', 'mars'], 'JSON': ['json'] },
    openLabel: 'Import MARS Web Test'
  });
  if (!uris || uris.length === 0) {
    return;
  }

  const uri = uris[0];
  const bytes = await vscode.workspace.fs.readFile(uri);
  const text = Buffer.from(bytes).toString('utf8');

  let steps = [];
  let testName = path.basename(uri.fsPath);
  try {
    const parsed = JSON.parse(text);
    if (Array.isArray(parsed)) {
      steps = parsed;
    } else if (Array.isArray(parsed.steps)) {
      steps = parsed.steps;
      if (parsed.testName) {
        testName = parsed.testName;
      }
    }
  } catch (err) {
    vscode.window.showErrorMessage('Only JSON import is implemented in MVP.');
    return;
  }

  panel.webview.postMessage({
    type: 'importedTest',
    testName,
    steps
  });
}

function getWebviewContent(licenseInfo) {
  const initialLicenseText = (() => {
    if (!licenseInfo) return 'License: unknown';
    const type = licenseInfo.licenseType || 'trial';
    const region = licenseInfo.region || 'N/A';
    if (licenseInfo.status === 'valid') {
      return `License: ${type} (${region})` + (licenseInfo.expire ? ` • Expires ${licenseInfo.expire}` : '');
    }
    if (licenseInfo.status === 'trial') {
      return 'License: trial (limited features)';
    }
    return 'License: invalid';
  })();

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>MARS Web Automation Engine</title>
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; padding: 0; font-family: var(--vscode-font-family); font-size: 13px; color: var(--vscode-foreground); background: var(--vscode-editor-background); }
    .container { display: flex; flex-direction: column; height: 100vh; }
    .toolbar { display: flex; align-items: center; gap: 6px; padding: 6px 8px; border-bottom: 1px solid var(--vscode-panel-border); background: var(--vscode-editor-background); }
    .toolbar button { border-radius: 4px; border: 1px solid var(--vscode-button-border); padding: 4px 8px; background: var(--vscode-button-background); color: var(--vscode-button-foreground); cursor: pointer; font-size: 12px; }
    .toolbar button:hover { background: var(--vscode-button-hoverBackground); }
    .toolbar-title { font-weight: 600; margin-right: 12px; }
    .toolbar-spacer { flex: 1; }
    .badge { padding: 1px 8px; border-radius: 999px; border: 1px solid var(--vscode-panel-border); font-size: 11px; }
    .main { display: grid; grid-template-columns: 260px minmax(260px, 1.2fr) minmax(280px, 1.3fr); grid-template-rows: 2.2fr 1.1fr; grid-template-areas:
      "objects steps steps"
      "recorder replay replay";
      flex: 1;
      min-height: 0;
    }
    .panel { border-right: 1px solid var(--vscode-panel-border); border-bottom: 1px solid var(--vscode-panel-border); display: flex; flex-direction: column; min-height: 0; }
    .panel-header { padding: 4px 8px; font-size: 12px; font-weight: 600; border-bottom: 1px solid var(--vscode-panel-border); background: var(--vscode-editor-inactiveSelectionBackground); }
    .panel-body { flex: 1; padding: 4px 6px; overflow: auto; font-size: 12px; }
    .panel-footer { padding: 4px 6px; font-size: 11px; color: var(--vscode-descriptionForeground); border-top: 1px solid var(--vscode-panel-border); }

    /* Web Object Inspector */
    .objects { grid-area: objects; }
    .object-tree { margin-bottom: 6px; max-height: 40%; overflow: auto; border-bottom: 1px solid var(--vscode-panel-border); padding-bottom: 4px; }
    .tree-node { padding: 2px 0 2px 12px; cursor: pointer; border-left: 3px solid transparent; }
    .tree-node:hover { background: var(--vscode-list-hoverBackground); }
    .tree-node.selected { background: var(--vscode-list-activeSelectionBackground); border-left-color: var(--vscode-focusBorder); }
    .object-details table { width: 100%; border-collapse: collapse; }
    .object-details td { padding: 2px 4px; vertical-align: top; }
    .object-details td:first-child { width: 90px; color: var(--vscode-descriptionForeground); }

    /* Test Step List */
    .steps { grid-area: steps; }
    .steps-toolbar { display: flex; gap: 4px; margin-bottom: 4px; }
    .steps-table { width: 100%; border-collapse: collapse; table-layout: fixed; font-size: 12px; }
    .steps-table th, .steps-table td { padding: 4px 4px; border-bottom: 1px solid var(--vscode-panel-border); text-align: left; }
    .steps-table th { background: var(--vscode-editor-inactiveSelectionBackground); position: sticky; top: 0; z-index: 1; }
    .steps-row.selected { background: var(--vscode-list-activeSelectionBackground); }
    .steps-row.disabled { opacity: 0.6; text-decoration: line-through; }
    .steps-actions button { font-size: 11px; padding: 1px 4px; margin-right: 2px; }

    /* Recorder Control */
    .recorder { grid-area: recorder; }
    .recorder-body { display: flex; flex-direction: column; gap: 4px; }

    /* Replay Console */
    .replay { grid-area: replay; }
    .console { width: 100%; height: 100%; resize: none; border: 0; background: var(--vscode-editor-background); color: var(--vscode-editor-foreground); font-family: monospace; font-size: 11px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="toolbar">
      <span class="toolbar-title">MARS Web Automation Engine</span>
      <button id="btnStartRecord">Start Record</button>
      <button id="btnStopRecord">Stop Record</button>
      <button id="btnReplay">Replay Test</button>
      <button id="btnStepReplay">Step Replay</button>
      <button id="btnExport">Export Test</button>
      <button id="btnImport">Import Test</button>
      <div class="toolbar-spacer"></div>
      <span class="badge" id="licenseStatus">${initialLicenseText}</span>
    </div>
    <div class="main">
      <!-- Web Object Inspector -->
      <div class="panel objects">
        <div class="panel-header">Web Object Inspector</div>
        <div class="panel-body">
          <div class="object-tree" id="objectTree">
            <div class="tree-node selected" data-id="root" data-tag="html" data-selector="html">Document Root</div>
            <div class="tree-node" data-id="loginButton" data-tag="button" data-selector="#login">button#login (Login)</div>
            <div class="tree-node" data-id="usernameInput" data-tag="input" data-selector="input[name='username']">input[name="username"]</div>
          </div>
          <div class="object-details">
            <table>
              <tr><td>Object</td><td id="obj-name">Document Root</td></tr>
              <tr><td>Tag</td><td id="obj-tag">html</td></tr>
              <tr><td>Selector</td><td id="obj-selector">html</td></tr>
              <tr><td>XPath</td><td id="obj-xpath">//html</td></tr>
              <tr><td>Text</td><td id="obj-text">-</td></tr>
            </table>
          </div>
        </div>
        <div class="panel-footer" id="objectsFooter">Objects: 3 (mock data for MVP)</div>
      </div>

      <!-- Test Step List -->
      <div class="panel steps">
        <div class="panel-header">Test Step List</div>
        <div class="panel-body">
          <div class="steps-toolbar">
            <button id="btnAddStep">Add</button>
            <button id="btnDeleteStep">Delete</button>
            <button id="btnToggleStep">Enable/Disable</button>
            <button id="btnMoveUp">Move Up</button>
            <button id="btnMoveDown">Move Down</button>
          </div>
          <table class="steps-table">
            <thead>
              <tr>
                <th style="width:32px;">Step</th>
                <th style="width:90px;">Keyword</th>
                <th style="width:120px;">Object</th>
                <th>Parameter</th>
                <th style="width:90px;">Status</th>
                <th style="width:130px;">Actions</th>
              </tr>
            </thead>
            <tbody id="stepsBody"></tbody>
          </table>
        </div>
        <div class="panel-footer" id="stepsFooter">0 steps</div>
      </div>

      <!-- Recorder Control -->
      <div class="panel recorder">
        <div class="panel-header">Recorder Control</div>
        <div class="panel-body recorder-body">
          <div>Mode: <span id="rec-mode">Idle</span></div>
          <div>Last event: <span id="rec-last">-</span></div>
          <div>Hint: connect to MARS local engine via WebSocket in future implementation.</div>
        </div>
        <div class="panel-footer">Recording controls for browser automation (stub in MVP)</div>
      </div>

      <!-- Replay Console -->
      <div class="panel replay">
        <div class="panel-header">Replay Console</div>
        <div class="panel-body">
          <textarea id="console" class="console" readonly></textarea>
        </div>
        <div class="panel-footer">Execution logs, errors, and debug output will appear here.</div>
      </div>
    </div>
    <script>
      (function() {
        const vscode = acquireVsCodeApi();

        const state = {
          steps: [],
          selectedIndex: -1,
          recording: false,
          replaying: false
        };

        function log(msg) {
          const el = document.getElementById('console');
          const now = new Date().toISOString().slice(11, 19);
          el.value += '[' + now + '] ' + msg + '\\n';
          el.scrollTop = el.scrollHeight;
        }

        function renderSteps() {
          const tbody = document.getElementById('stepsBody');
          tbody.innerHTML = '';
          state.steps.forEach((step, idx) => {
            const tr = document.createElement('tr');
            tr.className = 'steps-row' + (idx === state.selectedIndex ? ' selected' : '') + (step.enabled === false ? ' disabled' : '');
            tr.dataset.index = idx.toString();
            tr.innerHTML =
              '<td>' + (idx + 1) + '</td>' +
              '<td>' + (step.keyword || '') + '</td>' +
              '<td>' + (step.object || '') + '</td>' +
              '<td>' + (step.parameter || '') + '</td>' +
              '<td>' + (step.status || 'Pending') + '</td>' +
              '<td class="steps-actions">' +
              '<button data-action="edit">Edit</button>' +
              '<button data-action="run">Run</button>' +
              '</td>';
            tbody.appendChild(tr);
          });
          document.getElementById('stepsFooter').textContent = state.steps.length + ' step(s)';
        }

        function selectStep(index) {
          state.selectedIndex = index;
          renderSteps();
        }

        function addStep(step) {
          state.steps.push({
            keyword: step.keyword || 'ClickButton',
            object: step.object || 'login',
            parameter: step.parameter || '',
            status: 'Pending',
            enabled: true
          });
          selectStep(state.steps.length - 1);
        }

        function deleteSelectedStep() {
          if (state.selectedIndex < 0) return;
          state.steps.splice(state.selectedIndex, 1);
          if (state.selectedIndex >= state.steps.length) {
            state.selectedIndex = state.steps.length - 1;
          }
          renderSteps();
        }

        function toggleSelectedStep() {
          if (state.selectedIndex < 0) return;
          const step = state.steps[state.selectedIndex];
          step.enabled = !step.enabled;
          renderSteps();
        }

        function moveSelected(delta) {
          const i = state.selectedIndex;
          if (i < 0) return;
          const j = i + delta;
          if (j < 0 || j >= state.steps.length) return;
          const tmp = state.steps[i];
          state.steps[i] = state.steps[j];
          state.steps[j] = tmp;
          state.selectedIndex = j;
          renderSteps();
        }

        function editStep(index) {
          const step = state.steps[index];
          const keyword = prompt('Keyword', step.keyword || '');
          if (keyword === null) return;
          const object = prompt('Object', step.object || '');
          if (object === null) return;
          const parameter = prompt('Parameter', step.parameter || '');
          if (parameter === null) return;
          step.keyword = keyword;
          step.object = object;
          step.parameter = parameter;
          renderSteps();
        }

        function runStep(index) {
          const step = state.steps[index];
          if (!step || step.enabled === false) {
            log('Step ' + (index + 1) + ' is disabled or missing.');
            return;
          }
          step.status = 'Running';
          renderSteps();
          setTimeout(() => {
            step.status = 'Done';
            renderSteps();
            log('Executed step ' + (index + 1) + ': ' + (step.keyword || '') + ' ' + (step.object || ''));
          }, 200);
        }

        // Web Object Inspector behavior
        const objectTree = document.getElementById('objectTree');
        objectTree.addEventListener('click', (e) => {
          const node = e.target.closest('.tree-node');
          if (!node) return;
          for (const el of objectTree.querySelectorAll('.tree-node')) {
            el.classList.remove('selected');
          }
          node.classList.add('selected');
          const name = node.textContent || '';
          const tag = node.dataset.tag || '';
          const selector = node.dataset.selector || '';
          const id = node.dataset.id || '';
          document.getElementById('obj-name').textContent = name;
          document.getElementById('obj-tag').textContent = tag;
          document.getElementById('obj-selector').textContent = selector;
          document.getElementById('obj-xpath').textContent = '//mock/xpath/' + id;
          document.getElementById('obj-text').textContent = '(demo text)';
          log('Selected object ' + id + ' (' + selector + ')');

          // When recording, auto-create a test step for this object.
          if (state.recording) {
            const keyword = tag === 'input' ? 'FillEdit' : 'ClickButton';
            addStep({
              keyword,
              object: id || selector || name,
              parameter: keyword === 'FillEdit' ? '<value>' : ''
            });
            document.getElementById('rec-last').textContent =
              'Recorded ' + keyword + ' on ' + (id || selector || name);
            log('Recorded step: ' + keyword + ' ' + (id || selector || name));
          }
        });

        // Toolbar buttons
        document.getElementById('btnStartRecord').addEventListener('click', () => {
          state.recording = true;
          document.getElementById('rec-mode').textContent = 'Recording';
          document.getElementById('rec-last').textContent = 'Started';
          log('Start recording (MVP stub – connect to MARS engine later).');
        });
        document.getElementById('btnStopRecord').addEventListener('click', () => {
          state.recording = false;
          document.getElementById('rec-mode').textContent = 'Idle';
          document.getElementById('rec-last').textContent = 'Stopped';
          log('Stop recording.');
        });
        document.getElementById('btnReplay').addEventListener('click', () => {
          if (!state.steps.length) {
            log('No steps to replay.');
            return;
          }
          state.replaying = true;
          log('Replay test started.');
          let i = 0;
          const runNext = () => {
            if (i >= state.steps.length) {
              state.replaying = false;
              log('Replay finished.');
              return;
            }
            runStep(i);
            i++;
            setTimeout(runNext, 250);
          };
          runNext();
        });
        document.getElementById('btnStepReplay').addEventListener('click', () => {
          if (state.selectedIndex < 0) {
            log('Select a step first.');
            return;
          }
          runStep(state.selectedIndex);
        });
        document.getElementById('btnExport').addEventListener('click', () => {
          vscode.postMessage({
            type: 'exportTest',
            format: 'json',
            testName: 'WebTest',
            steps: state.steps
          });
        });
        document.getElementById('btnImport').addEventListener('click', () => {
          vscode.postMessage({ type: 'importTest' });
        });

        // Step list controls
        document.getElementById('btnAddStep').addEventListener('click', () => {
          const keyword = prompt('Keyword (FillEdit / ClickButton / Navigate)', 'FillEdit');
          if (keyword === null) return;
          const object = prompt('Object name', 'username');
          if (object === null) return;
          const parameter = prompt('Parameter', '');
          if (parameter === null) return;
          addStep({ keyword, object, parameter });
        });
        document.getElementById('btnDeleteStep').addEventListener('click', deleteSelectedStep);
        document.getElementById('btnToggleStep').addEventListener('click', toggleSelectedStep);
        document.getElementById('btnMoveUp').addEventListener('click', () => moveSelected(-1));
        document.getElementById('btnMoveDown').addEventListener('click', () => moveSelected(1));

        document.getElementById('stepsBody').addEventListener('click', (e) => {
          const tr = e.target.closest('tr');
          if (!tr) return;
          const idx = parseInt(tr.dataset.index, 10);
          if (Number.isNaN(idx)) return;
          if (e.target.tagName === 'BUTTON') {
            const action = e.target.dataset.action;
            if (action === 'edit') {
              editStep(idx);
            } else if (action === 'run') {
              runStep(idx);
            }
          } else {
            selectStep(idx);
          }
        });

        // Handle messages from extension
        window.addEventListener('message', (event) => {
          const msg = event.data;
          if (!msg) return;
          switch (msg.type) {
            case 'licenseStatus':
              if (msg.license) {
                document.getElementById('licenseStatus').textContent =
                  msg.license.licenseType === 'trial'
                    ? 'License: trial'
                    : 'License: ' + msg.license.licenseType + ' (' + (msg.license.region || 'N/A') + ')';
              }
              break;
            case 'importedTest':
              if (Array.isArray(msg.steps)) {
                state.steps = msg.steps.map((s) => ({
                  keyword: s.keyword || '',
                  object: s.object || '',
                  parameter: s.parameter || s.value || '',
                  status: s.status || 'Pending',
                  enabled: s.enabled !== false
                }));
                state.selectedIndex = state.steps.length ? 0 : -1;
                renderSteps();
                log('Imported test "' + (msg.testName || '') + '" with ' + state.steps.length + ' step(s).');
              }
              break;
            default:
              break;
          }
        });

        // Initial demo steps
        addStep({ keyword: 'FillEdit', object: 'username', parameter: 'admin' });
        addStep({ keyword: 'FillEdit', object: 'password', parameter: '123456' });
        addStep({ keyword: 'ClickButton', object: 'login', parameter: '' });
        renderSteps();

        vscode.postMessage({ type: 'ready' });
        log('MARS Web Automation panel ready.');
      })();
    </script>
  </div>
</body>
</html>`;
}

module.exports = {
  activate,
  deactivate
};

