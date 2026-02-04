/**
 * Java UI Automation - Dockable Panel Provider
 * 4 parts: Toolbar, Process list (left), Object info, Test steps table
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { getJavaProcesses } from './processInfo';
import { loadAgentAndScan, runHighlightOverlay } from './agentLoader';
import { convertScanToUIObjects, convertScanToUIObjectTree, ScanOutput } from './objectConverter';
import { JavaProcess, UIObject, TestScriptStep } from './types';

const PANEL_STATE_KEY = 'javaUiAutomation.panelState';
const SCANED_FILES_DIR = 'scanedfiles';

interface PanelState {
  processes: JavaProcess[];
  objects: UIObject[];
  steps: TestScriptStep[];
  logText: string;
}

export class JavaUIPanelProvider implements vscode.WebviewViewProvider {
  private _currentProcesses: JavaProcess[] = [];
  private _currentObjects: UIObject[] = [];
  private _currentSteps: TestScriptStep[] = [];
  private _lastLogText: string = '';
  private _persistDebounce: ReturnType<typeof setTimeout> | undefined;

  constructor(
    private readonly _extensionUri: vscode.Uri,
    private readonly _outputChannel: vscode.OutputChannel,
    private readonly _context: vscode.ExtensionContext
  ) {
    this._loadState();
  }

  private _loadState(): void {
    const raw = this._context.workspaceState.get<PanelState>(PANEL_STATE_KEY);
    if (raw) {
      this._currentProcesses = Array.isArray(raw.processes) ? raw.processes : [];
      this._currentObjects = Array.isArray(raw.objects) ? raw.objects : [];
      this._currentSteps = Array.isArray(raw.steps) ? raw.steps : [];
      this._lastLogText = typeof raw.logText === 'string' ? raw.logText : '';
    }
  }

  private _getState(): PanelState {
    return {
      processes: this._currentProcesses,
      objects: this._currentObjects,
      steps: this._currentSteps,
      logText: this._lastLogText,
    };
  }

  private _persistState(): void {
    this._context.workspaceState.update(PANEL_STATE_KEY, this._getState());
  }

  private _persistStateDebounced(): void {
    if (this._persistDebounce) clearTimeout(this._persistDebounce);
    this._persistDebounce = setTimeout(() => {
      this._persistDebounce = undefined;
      this._persistState();
    }, 800);
  }

  /** Scan output and objects/script under extension path, no workspace required. */
  private _getScanDir(): string {
    const dir = path.join(this._extensionUri.fsPath, SCANED_FILES_DIR);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    return dir;
  }

  resolveWebviewView(
    webviewView: vscode.WebviewView,
    _context: vscode.WebviewViewResolveContext,
    _token: vscode.CancellationToken
  ): void {
    this._outputChannel.appendLine('[Java UI] resolveWebviewView invoked');
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [this._extensionUri],
    };

    webviewView.webview.html = this._getHtml(webviewView.webview);
    
    webviewView.webview.onDidReceiveMessage(async (msg) => {
      this._outputChannel.appendLine(`[Java UI] onDidReceiveMessage: ${JSON.stringify(msg)}`);
      switch (msg.type) {
        case 'ping':
          this._outputChannel.appendLine('[Java UI] received ping from webview');
          this._safePost(webviewView.webview, { type: 'pong' });
          const state = this._getState();
          if (state.processes.length > 0 || state.objects.length > 0 || state.steps.length > 0 || state.logText) {
            this._safePost(webviewView.webview, { type: 'restoreState', state });
          }
          break;
        case 'getProcesses':
          this._outputChannel.appendLine('[Java UI] getProcesses received, starting...');
          this._handleGetProcesses(webviewView.webview);
          break;
        case 'scanProcess':
          this._handleScanProcess(webviewView.webview, msg.pid);
          break;
        case 'selectObject':
          this._handleSelectObject(webviewView.webview, msg.object);
          break;
        case 'loadObjects':
          this._handleLoadObjects(webviewView.webview);
          break;
        case 'loadSteps':
          this._handleLoadSteps(webviewView.webview);
          break;
        case 'deleteStep':
          this._handleDeleteStep(webviewView.webview, msg.index);
          break;
        case 'skipStep':
          this._handleSkipStep(webviewView.webview, msg.index);
          break;
        case 'processSelected':
          this._handleProcessSelected(webviewView.webview, msg.pid);
          break;
        case 'highlight':
          this._handleHighlight(webviewView.webview, msg.pid, msg.object);
          break;
        case 'generateSteps':
          this._handleGenerateSteps(webviewView.webview);
          break;
        case 'execute':
          this._handleExecute(webviewView.webview);
          break;
        case 'settings':
          webviewView.webview.postMessage({ type: 'log', data: '[action] Settings button is clicked.\r\n' });
          vscode.commands.executeCommand('workbench.action.openSettings', 'javaUiAutomation');
          break;
        case 'showDialog':
          this._outputChannel.appendLine('[Java UI] showDialog requested');
          try {
            const res = await vscode.window.showInformationMessage('Webview requested a dialog', 'OK');
            webviewView.webview.postMessage({ type: 'log', data: `[dialog] selected: ${res ?? 'none'}\r\n` });
          } catch (err) {
            this._outputChannel.appendLine(`[Java UI] showDialog error: ${String(err)}`);
          }
          break;
        default:
          this._outputChannel.appendLine(`[Java UI] Unknown message type: ${JSON.stringify(msg)}`);
          break;
      }
    });
  }

  private _log(webview: vscode.Webview, text: string): void {
    this._lastLogText += text;
    this._outputChannel.append(text);
    this._safePost(webview, { type: 'log', data: text });
    this._persistStateDebounced();
  }

  private _safePost(webview: vscode.Webview, msg: any): void {
    try {
      this._outputChannel.appendLine(`[Java UI] postMessage -> ${JSON.stringify(msg)}`);
      webview.postMessage(msg);
    } catch (err) {
      this._outputChannel.appendLine(`[Java UI] postMessage error: ${String(err)}`);
    }
  }

  private async _handleGetProcesses(webview: vscode.Webview): Promise<void> {
    this._outputChannel.show(true);
    this._log(webview, '[begin] Window Spy button is clicked.\r\n');
    this._log(webview, 'Traversing system processes...\r\n');
    try {
      const processes = await getJavaProcesses((e) => {
        if (e.kind === 'checking') {
          this._safePost(webview, { type: 'logAnalyzing', pid: e.pid, name: e.name });
        } else if (e.kind === 'found') {
          this._safePost(webview, { type: 'logFound', pid: e.pid, display: e.display });
        } else if (e.kind === 'skip') {
          this._safePost(webview, { type: 'logSkip', pid: e.pid });
        }
      });
      const names = processes.map((p) => `\t${p.pid}: ${p.mainClass || p.displayName || 'java'}`).join('\r\n');
      this._log(webview, `[end] has found |${processes.length}| java application(s)\r\n${names}\r\n`);
      this._outputChannel.appendLine(`[Java UI] Posting ${processes.length} processes to webview`);
      this._currentProcesses = processes;
      this._safePost(webview, { type: 'processes', data: processes });
      this._persistState();
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  /** Double-click process: attach Java agent to target PID, scan all UI objects, send to panel. */
  private async _handleScanProcess(webview: vscode.Webview, pid: number): Promise<void> {
    this._log(webview, `[begin] Attaching agent and scanning UI. PID=${pid}\r\n`);
    const outDir = this._getScanDir();

    try {
      const result = await loadAgentAndScan(pid, outDir);
      if (!result.success || !result.outputPath) {
        this._log(webview, `[end] error: ${result.error ?? 'Scan failed.'}\r\n`);
        this._safePost(webview, { type: 'error', message: result.error ?? 'Scan failed.' });
        return;
      }

      const scan: ScanOutput = JSON.parse(fs.readFileSync(result.outputPath, 'utf-8'));
      const objects = convertScanToUIObjects(scan);
      const objectTree = convertScanToUIObjectTree(scan);
      const objectsPath = path.join(outDir, 'objects.json');
      const objectsPidPath = path.join(outDir, `objects-${pid}.json`);
      fs.writeFileSync(objectsPath, JSON.stringify(objects, null, 2), 'utf-8');
      fs.writeFileSync(objectsPidPath, JSON.stringify(objects, null, 2), 'utf-8');

      this._log(webview, `[end] Scanned |${objects.length}| UI object(s), sent to panel.\r\n`);
      this._currentObjects = objects;
      this._safePost(webview, { type: 'objects', data: objects });
      this._safePost(webview, { type: 'objectTree', data: objectTree });
      this._persistState();
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  private _handleSelectObject(webview: vscode.Webview, obj: UIObject): void {
    this._log(webview, `[action] Object selected: ${obj.uniqueName}\r\n`);
    this._safePost(webview, { type: 'objectSelected', data: obj });
  }

  private _handleLoadObjects(webview: vscode.Webview): void {
    this._log(webview, '[action] Load objects from extension scan folder.\r\n');
    const scanDir = this._getScanDir();
    const objectsPath = path.join(scanDir, 'objects.json');
    if (!fs.existsSync(objectsPath)) return;

    try {
      const objects: UIObject[] = JSON.parse(fs.readFileSync(objectsPath, 'utf-8'));
      this._currentObjects = objects;
      this._safePost(webview, { type: 'objects', data: objects });
      this._persistState();
    } catch {
      // ignore
    }
  }

  /** When selected application (combobox) changes: load objects for that PID if objects-<pid>.json exists. */
  private _handleProcessSelected(webview: vscode.Webview, pid: number): void {
    this._log(webview, `[action] Application selected: PID=${pid}, loading objects.\r\n`);
    const scanDir = this._getScanDir();
    const objectsPidPath = path.join(scanDir, `objects-${pid}.json`);
    if (!fs.existsSync(objectsPidPath)) {
      this._currentObjects = [];
      this._safePost(webview, { type: 'objects', data: [] });
      this._persistState();
      return;
    }
    try {
      const objects: UIObject[] = JSON.parse(fs.readFileSync(objectsPidPath, 'utf-8'));
      this._currentObjects = objects;
      this._safePost(webview, { type: 'objects', data: objects });
      this._persistState();
    } catch {
      this._currentObjects = [];
      this._safePost(webview, { type: 'objects', data: [] });
    }
  }

  private _handleLoadSteps(webview: vscode.Webview): void {
    const scriptPath = this._getScriptPath();
    if (!scriptPath) return;

    if (!fs.existsSync(scriptPath)) {
      this._currentSteps = [];
      this._safePost(webview, { type: 'steps', data: [] });
      this._persistState();
      return;
    }

    try {
      const data = JSON.parse(fs.readFileSync(scriptPath, 'utf-8'));
      const steps: TestScriptStep[] = Array.isArray(data.steps) ? data.steps : [];
      this._currentSteps = steps;
      this._safePost(webview, { type: 'steps', data: steps });
      this._persistState();
    } catch {
      this._currentSteps = [];
      this._safePost(webview, { type: 'steps', data: [] });
      this._persistState();
    }
  }

  private _getScriptPath(): string | null {
    const dir = this._getScanDir();
    let scriptPath = path.join(dir, 'script.json');
    if (!fs.existsSync(scriptPath)) {
      const files = fs.readdirSync(dir).filter((f) => f.startsWith('script-') && f.endsWith('.json'));
      const latest = files.sort().reverse()[0];
      if (latest) scriptPath = path.join(dir, latest);
    }
    return scriptPath;
  }

  private _handleDeleteStep(webview: vscode.Webview, index: number): void {
    this._log(webview, `[action] Delete step at index ${index}.\r\n`);
    const scriptPath = this._getScriptPath();
    if (!scriptPath) return;
    let steps: TestScriptStep[] = [];
    if (fs.existsSync(scriptPath)) {
      try {
        const data = JSON.parse(fs.readFileSync(scriptPath, 'utf-8'));
        steps = Array.isArray(data.steps) ? data.steps : [];
      } catch {
        // ignore
      }
    }

    steps = steps.filter((_, i) => i !== index);
    fs.writeFileSync(scriptPath, JSON.stringify({ steps }, null, 2), 'utf-8');
    this._currentSteps = steps;
    this._safePost(webview, { type: 'steps', data: steps });
    this._persistState();
  }

  private _handleSkipStep(webview: vscode.Webview, index: number): void {
    this._log(webview, `[action] Skip step at index ${index}.\r\n`);
    const scriptPath = this._getScriptPath();
    if (!scriptPath) return;
    let steps: TestScriptStep[] = [];
    if (fs.existsSync(scriptPath)) {
      try {
        const data = JSON.parse(fs.readFileSync(scriptPath, 'utf-8'));
        steps = Array.isArray(data.steps) ? data.steps : [];
      } catch {
        //
      }
    }

    if (index >= 0 && index < steps.length) {
      steps[index] = { ...steps[index], skipped: !steps[index].skipped };
      fs.writeFileSync(scriptPath, JSON.stringify({ steps }, null, 2), 'utf-8');
      this._currentSteps = steps;
      webview.postMessage({ type: 'steps', data: steps });
      this._persistState();
    }
  }

  private async _handleHighlight(webview: vscode.Webview, _pid: number, obj: UIObject): Promise<void> {
    const id = obj?.identifier;
    const bounds = id?.screenBounds ?? id?.bounds;
    if (!bounds || bounds.width == null || bounds.height == null) {
      this._log(webview, '[action] Highlight: no bounds on object (use screenBounds for screen position).\r\n');
      return;
    }
    const x = bounds.x ?? 0;
    const y = bounds.y ?? 0;
    const w = Math.max(1, bounds.width);
    const h = Math.max(1, bounds.height);
    const useScreen = !!id?.screenBounds;
    this._log(webview, `[action] Highlight at (${x},${y}) ${w}x${h} (${useScreen ? 'screen/absolute' : 'relative'} coords), C# overlay.\r\n`);
    try {
      const result = await runHighlightOverlay(this._extensionUri.fsPath, x, y, w, h);
      if (!result.success) {
        this._log(webview, `[end] highlight error: ${result.error ?? 'unknown'}\r\n`);
        this._safePost(webview, { type: 'error', message: result.error ?? 'Highlight failed.' });
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] highlight error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  private _handleGenerateSteps(webview: vscode.Webview): void {
    this._log(webview, '[action] Generate Test Steps.\r\n');
    vscode.commands.executeCommand('javaUiAutomation.generateScript');
  }

  private _handleExecute(webview: vscode.Webview): void {
    this._log(webview, '[action] Execute (not implemented: plugin only generates scripts).\r\n');
    this._safePost(webview, { type: 'log', data: '[info] Execute is not implemented. Plugin only generates test scripts.\r\n' });
  }

  private _getHtml(webview: vscode.Webview): string {
    // Load HTML from separate file
    const htmlPath = path.join(this._extensionUri.fsPath, 'src', 'panel.html');
    try {
      const htmlContent = fs.readFileSync(htmlPath, 'utf-8');
      return htmlContent;
    } catch (err) {
      this._outputChannel.appendLine(`[Java UI] Error loading panel.html: ${String(err)}`);
      return `<html><body><h1>Failed to load panel</h1><p>Error: ${String(err)}</p></body></html>`;
    }
  }
}
