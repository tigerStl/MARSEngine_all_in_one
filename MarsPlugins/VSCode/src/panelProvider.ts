/**
 * Java UI Automation - Dockable Panel Provider
 * 4 parts: Toolbar, Process list (left), Object info, Test steps table
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { getJavaProcesses } from './processInfo';
import { loadAgentAndScan, runHighlightOverlay, replaySteps, startRecordAgent, stopRecordAgent } from './agentLoader';
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
  private _panelLoadedOnce: boolean = false;
  private _recordingPid: number | null = null;
  private _recordingWebview: vscode.Webview | null = null;
  private _recordStop: (() => void) | null = null;

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
          if (!this._panelLoadedOnce) {
            this._panelLoadedOnce = true;
            this._safePost(webviewView.webview, { type: 'clearPanel' });
          } else {
            const state = this._getState();
            if (state.processes.length > 0 || state.objects.length > 0 || state.steps.length > 0 || state.logText) {
              this._safePost(webviewView.webview, { type: 'restoreState', state });
            }
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
        case 'copyToClipboard':
          if (typeof msg.text === 'string') {
            vscode.env.clipboard.writeText(msg.text).then(
              () => this._log(webviewView.webview, '[action] Copied to clipboard.\r\n'),
              () => this._log(webviewView.webview, '[action] Copy failed.\r\n')
            );
          }
          break;
        case 'highlight':
          this._handleHighlight(webviewView.webview, msg.pid, msg.object);
          break;
        case 'generateSteps':
          this._handleGenerateSteps(webviewView.webview);
          break;
        case 'execute':
          this._handleExecute(webviewView.webview, msg.pid);
          break;
        case 'executeVisualStep':
          this._handleExecuteVisualStep(webviewView.webview, msg.pid, msg.index);
          break;
        case 'showVisualAbout':
          this._handleShowVisualAbout(webviewView.webview);
          break;
        case 'settings':
          webviewView.webview.postMessage({ type: 'log', data: '[action] Settings button is clicked.\r\n' });
          vscode.commands.executeCommand('workbench.action.openSettings', 'javaUiAutomation');
          break;
        case 'startRecord':
          this._handleStartRecord(webviewView.webview, msg.pid);
          break;
        case 'stopRecord':
          this.stopRecordAndShowDialog();
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

      const raw = fs.readFileSync(result.outputPath, 'utf-8');
      const scan: ScanOutput = JSON.parse(raw);
      const rootsCount = scan.roots?.length ?? 0;
      const objects = convertScanToUIObjects(scan);
      const objectTree = convertScanToUIObjectTree(scan);
      const withToolTip = objects.filter((o) => (o.identifier?.toolTipText ?? '').trim().length > 0).length;
      this._log(
        webview,
        `[extension<-agent] path=${result.outputPath} roots=${rootsCount} objects=${objects.length} withToolTipText=${withToolTip} jsonLen=${raw.length}\r\n`
      );
      const toolTipSamples = objects.filter((o) => (o.identifier?.toolTipText ?? '').trim().length > 0).slice(0, 3);
      for (const o of toolTipSamples) {
        const tt = (o.identifier?.toolTipText ?? '').trim();
        this._log(webview, `  [toolTipText] javaType=${o.identifier?.javaType ?? '-'} toolTipText=${tt.length > 60 ? tt.substring(0, 60) + '...' : tt}\r\n`);
      }
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
    const id = obj.identifier || {};
    const props = [
      `javaType=${id.javaType ?? '-'}`,
      `text=${id.text ?? '-'}`,
      `name=${id.name ?? '-'}`,
      `caption=${id.caption ?? '-'}`,
      `toolTipText=${id.toolTipText ?? '-'}`,
      `title=${id.title ?? '-'}`,
      `value=${id.value ?? '-'}`,
    ].join(', ');
    this._log(webview, `[property] ${props}\r\n`);
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

  /** Minimum highlight size when component reports 0x0 so the box is visible at (x,y). */
  private static readonly MIN_HIGHLIGHT_SIZE = 8;

  private async _handleHighlight(webview: vscode.Webview, _pid: number, obj: UIObject): Promise<void> {
    const id = obj?.identifier;
    const bounds = id?.screenBounds;
    if (!bounds || bounds.width == null || bounds.height == null) {
      this._log(webview, '[action] Highlight: need screenBounds (absolute position). Object has no screenBounds.\r\n');
      return;
    }
    const x = bounds.x ?? 0;
    const y = bounds.y ?? 0;
    const rawW = bounds.width;
    const rawH = bounds.height;
    const w = rawW <= 0 ? JavaUIPanelProvider.MIN_HIGHLIGHT_SIZE : rawW;
    const h = rawH <= 0 ? JavaUIPanelProvider.MIN_HIGHLIGHT_SIZE : rawH;
    if (rawW <= 0 || rawH <= 0) {
      this._log(webview, `[action] Highlight: object size was ${rawW}x${rawH}, using min ${w}x${h} at (${x},${y}) for visibility.\r\n`);
    } else {
      this._log(webview, `[action] Highlight at (${x},${y}) ${w}x${h} pixels (ProcessInfo -highlight).\r\n`);
    }
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

  private async _handleExecuteVisualStep(webview: vscode.Webview, pid?: number, index?: number): Promise<void> {
    if (pid == null || pid <= 0) {
      this._log(webview, '[action] Try execute step: select an application first.\r\n');
      return;
    }
    if (index == null || index < 0) {
      this._log(webview, '[action] Try execute step: select a visual node first.\r\n');
      return;
    }
    const outputPath = path.join('c:', 'temp', 'marsjavarecordreplay.json');
    if (!fs.existsSync(outputPath)) {
      this._log(webview, '[action] Try execute step: no replay file. Record first.\r\n');
      return;
    }
    let steps: unknown[] = [];
    try {
      const data = JSON.parse(fs.readFileSync(outputPath, 'utf-8'));
      steps = Array.isArray(data.steps) ? data.steps : [];
    } catch {
      this._log(webview, '[action] Try execute step: failed to load replay file.\r\n');
      return;
    }
    const step = steps[index];
    if (!step || typeof step !== 'object') {
      this._log(webview, `[action] Try execute step: step ${index + 1} not found.\r\n`);
      return;
    }
    this._log(webview, `[begin] Replaying step ${index + 1} on PID=${pid}...\r\n`);
    const outDir = this._getScanDir();
    try {
      const result = await replaySteps(pid, outDir, [step as Record<string, unknown>]);
      if (result.success) {
        this._log(webview, `[end] Step ${index + 1} executed.\r\n`);
      } else {
        this._log(webview, `[end] Step ${index + 1} failed: ${result.error ?? 'unknown'}.\r\n`);
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Step ${index + 1} error: ${msg}\r\n`);
    }
  }

  private _handleShowVisualAbout(webview: vscode.Webview): void {
    const text = 'Java UI Automation - Visual flowchart for Record & Replay. Record UI events, view them in Visual tab, and replay with Execute.';
    vscode.window.showInformationMessage(text);
    this._log(webview, '[info] About: Java UI Automation - Record & Replay.\r\n');
  }

  private async _handleExecute(webview: vscode.Webview, pid?: number): Promise<void> {
    if (pid == null || pid <= 0) {
      this._log(webview, '[action] Execute: please select a Java application first.\r\n');
      this._safePost(webview, { type: 'log', data: '[info] Select an application from the dropdown before Execute.\r\n' });
      return;
    }
    const outputPath = path.join('c:', 'temp', 'marsjavarecordreplay.json');
    if (!fs.existsSync(outputPath)) {
      this._log(webview, '[action] Execute: no replay file. Record first, then Execute.\r\n');
      this._safePost(webview, { type: 'log', data: '[info] No marsjavarecordreplay.json. Record UI events first, then Execute.\r\n' });
      return;
    }
    let steps: unknown[] = [];
    try {
      const data = JSON.parse(fs.readFileSync(outputPath, 'utf-8'));
      steps = Array.isArray(data.steps) ? data.steps : [];
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[action] Execute: failed to load replay file: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: `Load replay file failed: ${msg}` });
      return;
    }
    if (steps.length === 0) {
      this._log(webview, '[action] Execute: no steps to replay. Record first.\r\n');
      this._safePost(webview, { type: 'log', data: '[info] No steps in replay file. Record UI events first.\r\n' });
      return;
    }
    this._log(webview, `[begin] Replaying ${steps.length} step(s) on PID=${pid}...\r\n`);
    const outDir = this._getScanDir();
    try {
      const result = await replaySteps(pid, outDir, steps as Record<string, unknown>[]);
      if (result.success) {
        this._log(webview, `[end] Replay completed. ${result.count ?? steps.length} step(s) executed.\r\n`);
      } else {
        this._log(webview, `[end] Replay failed: ${result.error ?? 'unknown'}.\r\n`);
        this._safePost(webview, { type: 'error', message: result.error ?? 'Replay failed.' });
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Replay error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  private async _handleStartRecord(webview: vscode.Webview, pid: number): Promise<void> {
    if (this._recordingPid !== null) {
      this._log(webview, '[action] Recording already in progress.\r\n');
      return;
    }
    const outDir = this._getScanDir();
    this._recordingPid = pid;
    this._recordingWebview = webview;

    try {
      this._log(webview, `[begin] Injecting record agent and connecting (PID=${pid}).\r\n`);
      const result = await startRecordAgent(pid, outDir, (ev) => {
        const webviewRef = this._recordingWebview;
        if (ev.event === 'componentProperties' && webviewRef) {
          const componentClass = ev.componentClass ?? 'unknown';
          const props = ev.properties as Record<string, unknown> | undefined;
          let logLine = `[ToolButton] ${componentClass} properties:\r\n`;
          if (props && typeof props === 'object') {
            for (const [k, v] of Object.entries(props)) {
              logLine += `  ${k} = ${String(v)}\r\n`;
            }
          }
          this._log(webviewRef, logLine);
          return;
        }
        const name = ev.componentName ?? ev.name ?? '';
        const type = ev.componentClass ?? ev.javaType ?? ev.type ?? '';
        const event = ev.event ?? 'record';
        const textVal = event === 'fillEdit' ? (ev.content ?? '') : event === 'pressKey' || event === 'keyChordAction' ? (ev.keys ?? '') : event === 'textInputAction' ? (ev.text ?? '') : event === 'rawKeyEventAction' ? (`keyCode=${ev.keyCode ?? ''}`) : (ev.text ?? ev.content ?? '');
        if (webviewRef) {
          this._safePost(webviewRef, {
            type: 'visualNode',
            data: {
              name: String(name),
              type: String(type),
              text: String(textVal),
              event: String(event),
              screenX: ev.screenX != null ? Number(ev.screenX) : undefined,
              screenY: ev.screenY != null ? Number(ev.screenY) : undefined,
              width: ev.width != null ? Number(ev.width) : undefined,
              height: ev.height != null ? Number(ev.height) : undefined,
            },
          });
        }
      });

      if (!result.success) {
        this._log(webview, `[end] error: ${result.error ?? 'Failed to start record agent.'}\r\n`);
        this._safePost(webview, { type: 'error', message: result.error ?? 'Failed to start record agent.' });
        this._recordingPid = null;
        this._recordingWebview = null;
        return;
      }
      this._recordStop = result.stop ?? null;
      this._log(webview, '[end] Recording started. Events will appear in Visual tab. Use Ctrl+Alt+F12 to stop.\r\n');
      this._safePost(webview, { type: 'recordingStarted' });
    } catch (e) {
      this._recordingPid = null;
      this._recordingWebview = null;
      this._recordStop = null;
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  /** Called from extension command (Ctrl+Shift+F10). Stops recording, shows dialog, notifies panel. */
  async stopRecordAndShowDialog(): Promise<void> {
    if (this._recordingPid === null) {
      vscode.window.showInformationMessage('No recording in progress.');
      return;
    }
    const pid = this._recordingPid;
    const webview = this._recordingWebview;
    const outDir = this._getScanDir();
    const recordDir = path.join(outDir, `record-${pid}`);
    const recordFile = path.join(recordDir, 'record.jsonl');
    const stopFile = path.join(recordDir, 'record-stop.txt');

    this._recordingPid = null;
    this._recordingWebview = null;

    try {
      if (this._recordStop) {
        this._recordStop();
        this._recordStop = null;
      }
    } catch (e) {
      this._outputChannel.appendLine(`[Java UI] record stop error: ${String(e)}`);
    }

    try {
      stopRecordAgent(pid, outDir);
    } catch (e) {
      this._outputChannel.appendLine(`[Java UI] stopRecordAgent error: ${String(e)}`);
    }

    await new Promise((r) => setTimeout(r, 1500));

    const outputPath = path.join('c:', 'temp', 'marsjavarecordreplay.json');
    let eventCount = 0;
    const steps: unknown[] = [];

    if (fs.existsSync(recordFile)) {
      const content = fs.readFileSync(recordFile, 'utf-8');
      const lines = content.split('\n').filter((line) => line.trim().length > 0);
      eventCount = lines.length;
      for (const line of lines) {
        try {
          steps.push(JSON.parse(line));
        } catch {
          // skip malformed lines
        }
      }
    }

    try {
      const dir = path.dirname(outputPath);
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
      fs.writeFileSync(outputPath, JSON.stringify({ steps }, null, 2), 'utf-8');
      this._outputChannel.appendLine(`[Java UI] Record & Replay steps saved to ${outputPath}`);
    } catch (e) {
      this._outputChannel.appendLine(`[Java UI] Failed to save record replay: ${String(e)}`);
      vscode.window.showWarningMessage(`Recording stopped but save failed: ${outputPath}`);
    }

    const message = `Recording stopped. Events: ${eventCount}. Saved to ${outputPath}`;
    vscode.window.showInformationMessage(message);

    if (webview) {
      this._safePost(webview, { type: 'recordingStopped' });
    }
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
