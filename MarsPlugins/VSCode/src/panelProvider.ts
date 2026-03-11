/**
 * Java UI Automation - Dockable Panel Provider
 * 4 parts: Toolbar, Process list (left), Object info, Test steps table
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import * as crypto from 'crypto';
import * as http from 'http';
import * as https from 'https';
import { spawn } from 'child_process';
import { getJavaProcesses, findProcessInfoExe } from './processInfo';
import { AGENT_LOADER_LOG_FILE, loadAgentAndScan, ReplayProgressEvent, runHighlightOverlay, replaySteps, startRecordAgent, stopRecordAgent } from './agentLoader';
import { convertScanToUIObjects, convertScanToUIObjectTree, ScanOutput } from './objectConverter';
import { JavaProcess, UIObject, TestScriptStep, ScriptKeyword } from './types';
import { RecordingEngine } from './recording/recorder';
import { recordedStepToTestScriptStep } from './recording/stepAdapter';

const PANEL_STATE_KEY = 'javaUiAutomation.panelState';
const SCANED_FILES_DIR = 'scanedfiles';
const MARS_STEPS_MARKER = 'MARS_TEST_STEPS_FILE';
const MARS_STEPS_COPYRIGHT = 'Copyright (c) MARS. All rights reserved.';
const MARS_STEPS_PURPOSE = 'Java UI Automation Test Steps storage and exchange.';
const MARS_STEPS_VERSION = '1.0.0';
const MARS_STEPS_MD5_SALT = 'MARS::JavaUI::Steps::Integrity::v1';
const LICENSE_FIRST_SEEN_KEY = 'javaUiAutomation.license.firstSeenAt';
const LICENSE_LAST_SYNC_KEY = 'javaUiAutomation.license.lastSyncAt';
const LICENSE_STATE_FILE = 'license.latest.json';
const LICENSE_DECLARATION_FILE = 'license.declaration.latest.txt';
const LICENSE_SERVER_URL_CONFIG = 'loaniq.licenseServerUrl';
const LICENSE_SYNC_INTERVAL_MS = 60 * 1000;

type LicenseType = 'TEST' | 'PAID' | 'TRIAL_LIMITED';

interface ClientLicenseState {
  licenseType: LicenseType;
  region: 'US' | 'CN' | 'GLOBAL';
  trialStartAt: string;
  replayMaxStepsAfterTrialDays: number;
  trialDays: number;
  expiresAt?: string;
  message?: string;
  price?: { currency?: string; amount?: number };
  testPool?: Record<string, { used?: number; limit?: number; remaining?: number }>;
}

interface PanelState {
  processes: JavaProcess[];
  objects: UIObject[];
  steps: TestScriptStep[];
  logText: string;
}

interface MarsStepsFilePayload {
  marker: string;
  copyright: string;
  purpose: string;
  version: string;
  generatedAt: string;
  steps: TestScriptStep[];
}

interface MarsStepsFile extends MarsStepsFilePayload {
  md5: string;
}

export class JavaUIPanelProvider implements vscode.WebviewViewProvider {
  private _activeWebview: vscode.Webview | null = null;
  private _selectedPid: number | null = null;
  private _currentProcesses: JavaProcess[] = [];
  private _currentObjects: UIObject[] = [];
  private _currentSteps: TestScriptStep[] = [];
  private _lastLogText: string = '';
  private _persistDebounce: ReturnType<typeof setTimeout> | undefined;
  private _panelLoadedOnce: boolean = false;
  private _recordingPid: number | null = null;
  private _recordingWebview: vscode.Webview | null = null;
  private _recordStop: (() => void) | null = null;
  private _recordSend: ((msg: Record<string, unknown>) => void) | null = null;
  private _recordingEngine: RecordingEngine | null = null;
  private _recordingSteps: TestScriptStep[] = [];
  private _licenseRefreshInFlight: Promise<void> | null = null;

  constructor(
    private readonly _extensionUri: vscode.Uri,
    private readonly _outputChannel: vscode.OutputChannel,
    private readonly _context: vscode.ExtensionContext
  ) {
    this._loadState();
  }

  public async mcpListProcesses(): Promise<JavaProcess[]> {
    const processes = await getJavaProcesses();
    this._currentProcesses = processes;
    this._persistStateDebounced();
    if (this._activeWebview) {
      this._safePost(this._activeWebview, { type: 'processes', data: processes });
    }
    return processes;
  }

  public mcpSelectProcess(pid: number): { selectedPid: number } {
    if (!Number.isInteger(pid) || pid <= 0) {
      throw new Error('Invalid pid');
    }
    this._selectedPid = pid;
    if (this._activeWebview) {
      this._safePost(this._activeWebview, { type: 'setSelectedProcess', pid, triggerChange: true });
      this._handleProcessSelected(this._activeWebview, pid);
    }
    return { selectedPid: pid };
  }

  public async mcpStartRecord(options?: { pid?: number }): Promise<{ status: 'recording'; pid: number }> {
    const pid = Number(options?.pid ?? this._selectedPid ?? 0);
    if (!Number.isInteger(pid) || pid <= 0) {
      throw new Error('No process selected');
    }
    if (!this._activeWebview) {
      throw new Error('Panel is not ready');
    }
    this._selectedPid = pid;
    this._safePost(this._activeWebview, { type: 'setSelectedProcess', pid, triggerChange: true });
    await this._handleStartRecord(this._activeWebview, pid);
    if (this._recordingPid !== pid) {
      throw new Error('Failed to start recording');
    }
    return { status: 'recording', pid };
  }

  public async mcpStopRecord(): Promise<{ status: 'stopped'; stepCount: number }> {
    if (this._recordingPid === null) {
      throw new Error('No recording in progress');
    }
    const before = this._currentSteps.length;
    await this.stopRecordAndShowDialog();
    const stepCount = this._currentSteps.length >= before ? this._currentSteps.length : before;
    return { status: 'stopped', stepCount };
  }

  public async mcpGetObjectTree(options?: { rootWindowHint?: string; refresh?: boolean }): Promise<{ roots: unknown[] }> {
    if (!this._selectedPid || this._selectedPid <= 0) {
      throw new Error('No process selected');
    }
    const refresh = options?.refresh !== false;
    const outDir = this._getScanDir();
    const scanPath = path.join(outDir, `ui-scan-${this._selectedPid}.json`);
    let scan: ScanOutput;

    if (!refresh && fs.existsSync(scanPath)) {
      scan = JSON.parse(fs.readFileSync(scanPath, 'utf-8')) as ScanOutput;
    } else {
      const result = await loadAgentAndScan(this._selectedPid, outDir);
      if (!result.success || !result.outputPath) {
        throw new Error(result.error ?? 'Scan failed');
      }
      const raw = fs.readFileSync(result.outputPath, 'utf-8');
      scan = JSON.parse(raw) as ScanOutput;
      fs.writeFileSync(scanPath, JSON.stringify(scan, null, 2), 'utf-8');
    }

    const objectTree = convertScanToUIObjectTree(scan);
    this._currentObjects = convertScanToUIObjects(scan);
    this._persistStateDebounced();
    if (this._activeWebview) {
      this._safePost(this._activeWebview, { type: 'objects', data: this._currentObjects });
      this._safePost(this._activeWebview, { type: 'objectTree', data: objectTree });
    }
    return { roots: scan.roots ?? [] };
  }

  public async mcpExecuteStep(index: number): Promise<{ status: 'success' | 'failed'; durationMs: number; error?: string }> {
    if (!this._selectedPid || this._selectedPid <= 0) {
      throw new Error('No process selected');
    }
    if (!Number.isInteger(index) || index < 0) {
      throw new Error('Invalid step index');
    }
    if (!Array.isArray(this._currentSteps) || index >= this._currentSteps.length) {
      throw new Error('Step index out of range');
    }

    const started = Date.now();
    const outDir = this._getScanDir();
    const step = this._currentSteps[index] as unknown as Record<string, unknown>;
    await this._refreshLicenseFromServerIfDue();
    const permit = this._evaluateReplayPermission(1);
    if (!permit.allowed) {
      const msg = permit.message ?? 'License restriction: replay is limited.';
      vscode.window.showWarningMessage(msg);
      return { status: 'failed', durationMs: Date.now() - started, error: msg };
    }
    const result = await replaySteps(this._selectedPid, outDir, [step]);
    const durationMs = Date.now() - started;
    if (result.success) {
      return { status: 'success', durationMs };
    }
    return { status: 'failed', durationMs, error: result.error ?? 'unknown' };
  }

  public mcpGetSteps(): { steps: TestScriptStep[] } {
    return { steps: [...this._currentSteps] };
  }

  public mcpUpdateStep(index: number, patch: Partial<TestScriptStep>): { step: TestScriptStep } {
    if (!Number.isInteger(index) || index < 0 || index >= this._currentSteps.length) {
      throw new Error('Step index out of range');
    }
    const current = this._currentSteps[index];
    const next: TestScriptStep = {
      ...current,
      ...patch,
      parentIdentifier: patch.parentIdentifier ?? current.parentIdentifier,
      objectIdentifier: patch.objectIdentifier ?? current.objectIdentifier,
    };
    this._currentSteps[index] = next;
    this._persistStateDebounced();
    if (this._activeWebview) {
      this._safePost(this._activeWebview, { type: 'steps', data: this._currentSteps });
    }
    return { step: next };
  }

  public async mcpRunReplay(options?: { fromIndex?: number; toIndex?: number; strictParent?: boolean }): Promise<{ status: 'done' | 'failed'; failedIndex?: number; error?: string }> {
    if (!this._selectedPid || this._selectedPid <= 0) {
      throw new Error('No process selected');
    }
    if (!Array.isArray(this._currentSteps) || this._currentSteps.length === 0) {
      throw new Error('No steps to replay');
    }
    const fromIndex = Number.isInteger(options?.fromIndex) ? Number(options?.fromIndex) : 0;
    const toIndex = Number.isInteger(options?.toIndex) ? Number(options?.toIndex) : (this._currentSteps.length - 1);
    if (fromIndex < 0 || toIndex < fromIndex || toIndex >= this._currentSteps.length) {
      throw new Error('Invalid replay range');
    }

    const steps = this._currentSteps.slice(fromIndex, toIndex + 1) as unknown as Record<string, unknown>[];
    await this._refreshLicenseFromServerIfDue();
    const permit = this._evaluateReplayPermission(steps.length);
    if (!permit.allowed) {
      const msg = permit.message ?? 'License restriction: replay is limited.';
      vscode.window.showWarningMessage(msg);
      return { status: 'failed', error: msg };
    }
    const outDir = this._getScanDir();
    const result = await replaySteps(this._selectedPid, outDir, steps);
    if (result.success) {
      return { status: 'done' };
    }
    const failedIndex = typeof result.failedIndex === 'number' ? (fromIndex + result.failedIndex) : undefined;
    return { status: 'failed', failedIndex, error: result.error ?? 'Replay failed' };
  }

  public async mcpHighlightObject(input: { objectKey: Record<string, unknown>; parentKey?: Record<string, unknown> }): Promise<{ message: string }> {
    const key = (input?.objectKey ?? {}) as Record<string, unknown>;
    const target = this._findObjectByKey(key);
    const bounds = target?.identifier?.screenBounds;
    if (!bounds) {
      throw new Error('Object has no screenBounds');
    }
    const x = Number(bounds.x ?? 0);
    const y = Number(bounds.y ?? 0);
    const width = Math.max(2, Number(bounds.width ?? 2));
    const height = Math.max(2, Number(bounds.height ?? 2));
    const result = await runHighlightOverlay(this._extensionUri.fsPath, x, y, width, height);
    if (!result.success) {
      throw new Error(result.error ?? 'Highlight failed');
    }
    return { message: `highlight at (${x},${y}) ${width}x${height}` };
  }

  public mcpExportObjects(options?: { format?: string; includeParents?: boolean }): { filePath: string } {
    const format = (options?.format ?? 'json').toLowerCase();
    if (format !== 'json') {
      throw new Error('Only json format is supported');
    }
    const outDir = this._getScanDir();
    const filePath = path.join(outDir, `mars-objects-${Date.now()}.json`);
    const includeParents = options?.includeParents !== false;
    const payload = {
      marker: 'MARS_UI_OBJECTS',
      generatedAt: new Date().toISOString(),
      parentObjects: includeParents ? this._currentObjects.map((o) => o.parent ?? {}) : [],
      objects: this._currentObjects,
    };
    fs.writeFileSync(filePath, JSON.stringify(payload, null, 2), 'utf-8');
    return { filePath };
  }

  public mcpExportDiagnostics(options?: { includeLogs?: boolean }): { filePath: string } {
    const outDir = this._getScanDir();
    const bundleDir = path.join(outDir, `mars-diagnostics-${Date.now()}`);
    const logsDir = path.join(bundleDir, 'logs');
    fs.mkdirSync(logsDir, { recursive: true });
    fs.writeFileSync(path.join(bundleDir, 'steps.json'), JSON.stringify({ steps: this._currentSteps }, null, 2), 'utf-8');
    fs.writeFileSync(path.join(bundleDir, 'summary.json'), JSON.stringify({
      generatedAt: new Date().toISOString(),
      platform: process.platform,
      processCount: this._currentProcesses.length,
      objectCount: this._currentObjects.length,
      stepCount: this._currentSteps.length,
    }, null, 2), 'utf-8');
    if (options?.includeLogs !== false) {
      fs.writeFileSync(path.join(logsDir, 'panel-log.txt'), this._lastLogText || '', 'utf-8');
    }
    return { filePath: bundleDir };
  }

  public mcpGetLastErrors(limit?: number): { items: Array<{ ts: number; scope: string; message: string }> } {
    const max = Number.isInteger(limit) && Number(limit) > 0 ? Number(limit) : 20;
    const lines = (this._lastLogText || '').split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
    const errors = lines.filter((line) => /error|failed|exception/i.test(line)).slice(-max).map((message) => ({
      ts: Date.now(),
      scope: 'panel',
      message,
    }));
    return { items: errors };
  }

  private _findObjectByKey(key: Record<string, unknown>): UIObject | undefined {
    const keyJavaType = typeof key.javaType === 'string' ? key.javaType : undefined;
    const keyName = typeof key.name === 'string' ? key.name : undefined;
    const keyText = typeof key.text === 'string' ? key.text : undefined;
    const keyIndex = typeof key.index === 'number' ? key.index : undefined;
    return this._currentObjects.find((obj) => {
      const id = obj.identifier || {};
      if (keyJavaType && id.javaType !== keyJavaType) return false;
      if (keyName && id.name !== keyName) return false;
      if (keyText && id.text !== keyText && id.caption !== keyText) return false;
      if (typeof keyIndex === 'number' && id.index !== keyIndex) return false;
      return true;
    });
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

  /** Remove cached object list files so loadObjects (sent on panel load) does not repopulate the tree after restart. */
  private _clearObjectListCache(): void {
    try {
      const scanDir = this._getScanDir();
      const objectsPath = path.join(scanDir, 'objects.json');
      if (fs.existsSync(objectsPath)) fs.unlinkSync(objectsPath);
      const files = fs.readdirSync(scanDir);
      for (const f of files) {
        if (f.startsWith('objects-') && f.endsWith('.json')) {
          fs.unlinkSync(path.join(scanDir, f));
        }
      }
    } catch {
      // ignore
    }
  }

  resolveWebviewView(
    webviewView: vscode.WebviewView,
    _context: vscode.WebviewViewResolveContext,
    _token: vscode.CancellationToken
  ): void {
    this._outputChannel.appendLine('[Java UI] resolveWebviewView invoked');
    this._activeWebview = webviewView.webview;
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [this._extensionUri],
    };

    webviewView.webview.html = this._getHtml(webviewView.webview);

    webviewView.onDidChangeVisibility(() => {
      if (webviewView.visible && this._panelLoadedOnce) {
        this._currentObjects = [];
        this._persistState();
        setTimeout(() => {
          if (webviewView.visible) {
            this._safePost(webviewView.webview, { type: 'clearObjectList' });
          }
        }, 350);
      }
    });

    webviewView.webview.onDidReceiveMessage(async (msg) => {
      this._outputChannel.appendLine(`[Java UI] onDidReceiveMessage: ${JSON.stringify(msg)}`);
      switch (msg.type) {
        case 'ping':
          this._outputChannel.appendLine('[Java UI] received ping from webview');
          this._safePost(webviewView.webview, { type: 'pong' });
          await this._refreshLicenseFromServerIfDue(true);
          this._pushLicenseStatus(webviewView.webview);
          if (!this._panelLoadedOnce) {
            this._panelLoadedOnce = true;
            this._selectedPid = null;
            this._currentProcesses = [];
            this._currentObjects = [];
            this._currentSteps = [];
            this._lastLogText = '';
            this._persistState();
            this._clearObjectListCache();
            this._safePost(webviewView.webview, { type: 'clearPanel' });
          } else {
            const state = this._getState();
            if (state.processes.length > 0 || state.objects.length > 0 || state.steps.length > 0 || state.logText) {
              this._safePost(webviewView.webview, { type: 'restoreState', state });
            }
            this._safePost(webviewView.webview, { type: 'clearObjectList' });
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
        case 'saveStepsFile':
          await this._handleSaveStepsFile(webviewView.webview, msg.steps);
          break;
        case 'loadStepsFile':
          await this._handleLoadStepsFile(webviewView.webview);
          break;
        case 'syncSteps':
          this._handleSyncSteps(webviewView.webview, msg.data);
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
          this._handleExecute(webviewView.webview, msg.pid, msg.steps);
          break;
        case 'executeVisualStep':
          this._handleExecuteVisualStep(webviewView.webview, msg.pid, msg.index);
          break;
        case 'executeStepAtIndex':
          this._handleExecuteStepAtIndex(webviewView.webview, msg.pid, msg.index, msg.steps);
          break;
        case 'showVisualAbout':
          this._handleShowVisualAbout(webviewView.webview);
          break;
        case 'showExecuteResultDialog':
          if (msg.success) {
            vscode.window.showInformationMessage(msg.message ?? 'Step executed successfully.');
          } else {
            vscode.window.showErrorMessage(msg.message ?? 'Step failed.');
          }
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
        case 'exportDiagnostics':
          await this._handleExportDiagnostics(webviewView.webview);
          break;
        case 'openZellePortal':
          await this._handleOpenZellePortal(webviewView.webview);
          break;
        case 'fetchZelleLicense':
          await this._handleFetchZelleLicense(webviewView.webview);
          break;
        case 'importLicenseFile':
          await this._handleImportLicenseFile(webviewView.webview);
          break;
        case 'exportObjects':
          await this._handleExportObjects(webviewView.webview, msg.data);
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

  private _isRecordingStepDebugEnabled(): boolean {
    return vscode.workspace.getConfiguration('loaniq').get<boolean>('recordingStepDebugLog', false);
  }

  private _logRecordingStep(step: TestScriptStep): void {
    if (!this._isRecordingStepDebugEnabled()) return;
    const keyword = step.keyword ?? '';
    const parameter = step.parameter ?? '';
    const data = step.data ?? '';
    this._outputChannel.appendLine(`[StepDebug] keyword=${keyword}, parameter=${parameter}, data=${data}`);
  }

  private async _handleGetProcesses(webview: vscode.Webview): Promise<void> {
    this._outputChannel.show(true);
    this._currentProcesses = [];
    this._safePost(webview, { type: 'processes', data: [] });
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
    this._selectedPid = Number.isInteger(pid) && pid > 0 ? pid : null;
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

  private _handleSyncSteps(webview: vscode.Webview, steps: unknown): void {
    const arr = Array.isArray(steps) ? steps : [];
    this._currentSteps = arr as TestScriptStep[];
    if (arr.length === 0) {
      this._recordingSteps = [];
      try {
        this._recordSend?.({ type: 'clearRecord' });
      } catch (e) {
        this._outputChannel.appendLine(`[Java UI] clearRecord send failed: ${e}`);
      }
    }
    const scriptPath = this._getScriptPath();
    if (scriptPath) {
      try {
        const dir = path.dirname(scriptPath);
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        fs.writeFileSync(scriptPath, JSON.stringify({ steps: arr }, null, 2), 'utf-8');
      } catch (e) {
        this._outputChannel.appendLine(`[Java UI] syncSteps write failed: ${e}`);
      }
    }
    this._persistState();
    this._safePost(webview, { type: 'steps', data: this._currentSteps });
  }

  private _createMarsStepsPayload(steps: TestScriptStep[]): MarsStepsFilePayload {
    return {
      marker: MARS_STEPS_MARKER,
      copyright: MARS_STEPS_COPYRIGHT,
      purpose: MARS_STEPS_PURPOSE,
      version: MARS_STEPS_VERSION,
      generatedAt: new Date().toISOString(),
      steps,
    };
  }

  private _computeStepsPayloadMd5(payload: MarsStepsFilePayload): string {
    const text = JSON.stringify(payload) + '|' + MARS_STEPS_MD5_SALT;
    return crypto.createHash('md5').update(text, 'utf8').digest('hex');
  }

  private async _handleSaveStepsFile(webview: vscode.Webview, stepsFromPanel: unknown): Promise<void> {
    const steps = Array.isArray(stepsFromPanel) ? (stepsFromPanel as TestScriptStep[]) : this._currentSteps;
    const defaultUri = vscode.Uri.file(path.join(this._getScanDir(), `mars-test-steps-${Date.now()}.json`));
    const targetUri = await vscode.window.showSaveDialog({
      title: 'Save Test Steps (MARS)',
      defaultUri,
      filters: { 'JSON Files': ['json'] },
      saveLabel: 'Save',
    });
    if (!targetUri) {
      this._log(webview, '[action] Save Test Steps canceled.\r\n');
      return;
    }

    try {
      const payload = this._createMarsStepsPayload(steps);
      const fileContent: MarsStepsFile = {
        ...payload,
        md5: this._computeStepsPayloadMd5(payload),
      };
      fs.writeFileSync(targetUri.fsPath, JSON.stringify(fileContent, null, 2), 'utf-8');
      this._log(webview, `[end] Save Test Steps success: ${targetUri.fsPath}\r\n`);
      vscode.window.showInformationMessage('Test steps saved successfully.');
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Save Test Steps failed: ${msg}\r\n`);
      vscode.window.showErrorMessage(`Save failed: ${msg}`);
    }
  }

  private async _handleLoadStepsFile(webview: vscode.Webview): Promise<void> {
    if (this._currentSteps.length > 0) {
      const confirm = await vscode.window.showWarningMessage(
        'Load will clear current test step content. Continue?',
        { modal: true },
        'Continue'
      );
      if (confirm !== 'Continue') {
        this._log(webview, '[action] Load Test Steps canceled by user.\r\n');
        return;
      }
    }

    const uris = await vscode.window.showOpenDialog({
      title: 'Load Test Steps (MARS)',
      canSelectFiles: true,
      canSelectFolders: false,
      canSelectMany: false,
      filters: { 'JSON Files': ['json'] },
      openLabel: 'Load',
    });
    if (!uris || uris.length === 0) {
      this._log(webview, '[action] Load Test Steps canceled.\r\n');
      return;
    }

    const filePath = uris[0].fsPath;
    try {
      const raw = fs.readFileSync(filePath, 'utf-8');
      const parsed = JSON.parse(raw) as Partial<MarsStepsFile>;
      if (!parsed || typeof parsed !== 'object') {
        throw new Error('Invalid JSON structure.');
      }
      if (
        parsed.marker !== MARS_STEPS_MARKER ||
        parsed.copyright !== MARS_STEPS_COPYRIGHT ||
        parsed.purpose !== MARS_STEPS_PURPOSE ||
        typeof parsed.version !== 'string' ||
        typeof parsed.generatedAt !== 'string' ||
        typeof parsed.md5 !== 'string' ||
        !Array.isArray(parsed.steps)
      ) {
        throw new Error('Required MARS metadata fields are missing or invalid.');
      }

      const payload: MarsStepsFilePayload = {
        marker: parsed.marker,
        copyright: parsed.copyright,
        purpose: parsed.purpose,
        version: parsed.version,
        generatedAt: parsed.generatedAt,
        steps: parsed.steps as TestScriptStep[],
      };
      if (!payload || !Array.isArray(payload.steps)) {
        throw new Error('Invalid steps payload.');
      }

      const loadedSteps = parsed.steps as TestScriptStep[];
      this._currentSteps = loadedSteps;
      this._safePost(webview, { type: 'loadedSteps', data: loadedSteps });

      const scriptPath = this._getScriptPath();
      if (scriptPath) {
        try {
          const dir = path.dirname(scriptPath);
          if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
          fs.writeFileSync(scriptPath, JSON.stringify({ steps: loadedSteps }, null, 2), 'utf-8');
        } catch (writeErr) {
          this._outputChannel.appendLine(`[Java UI] sync loaded steps to script.json failed: ${writeErr}`);
        }
      }

      this._persistState();
      this._log(webview, `[end] Load Test Steps success: ${filePath}, steps=${loadedSteps.length}\r\n`);
      vscode.window.showInformationMessage(`Test steps loaded successfully (${loadedSteps.length}).`);
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Load Test Steps failed: ${msg}\r\n`);
      vscode.window.showErrorMessage(`Load failed: ${msg}`);
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
      this._recordSend?.({ type: 'pauseRecordAndReplay' });
      try {
        const result = await runHighlightOverlay(this._extensionUri.fsPath, x, y, w, h);
        if (!result.success) {
          this._log(webview, `[end] highlight error: ${result.error ?? 'unknown'}\r\n`);
          this._safePost(webview, { type: 'error', message: result.error ?? 'Highlight failed.' });
        }
      } finally {
        this._recordSend?.({ type: 'resumeRecordAndReplay' });
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] highlight error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
      this._recordSend?.({ type: 'resumeRecordAndReplay' });
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

  /** Execute a single test step from the grid by index using current panel steps. */
  private async _handleExecuteStepAtIndex(webview: vscode.Webview, pid?: number, index?: number, stepsFromPanel?: unknown): Promise<void> {
    if (pid == null || pid <= 0) {
      this._log(webview, '[action] Execute step: select an application first.\r\n');
      return;
    }
    if (index == null || index < 0) {
      this._log(webview, '[action] Execute step: invalid index.\r\n');
      return;
    }
    const steps = Array.isArray(stepsFromPanel) ? stepsFromPanel : [];
    const step = steps[index];
    if (!step || typeof step !== 'object') {
      this._log(webview, `[action] Execute step: step ${index + 1} not found.\r\n`);
      return;
    }
    this._log(webview, `[begin] Replaying step ${index + 1} on PID=${pid}...\r\n`);
    await this._refreshLicenseFromServerIfDue();
    const permit = this._evaluateReplayPermission(1);
    if (!permit.allowed) {
      const msg = permit.message ?? 'License restriction: replay is limited.';
      this._log(webview, `[license] ${msg}\r\n`);
      vscode.window.showWarningMessage(msg);
      this._safePost(webview, {
        type: 'executeResult',
        fromActionColumn: true,
        success: false,
        error: msg,
        index,
        failedIndex: index,
      });
      return;
    }
    const outDir = this._getScanDir();
    try {
      const result = await replaySteps(
        pid,
        outDir,
        [step as Record<string, unknown>],
        (event: ReplayProgressEvent) => {
          if ((event.event === 'stepStart' || event.event === 'stepEnd') && typeof index === 'number') {
            this._safePost(webview, { type: 'replayProgress', data: { ...event, index, total: 1 } });
          } else {
            this._safePost(webview, { type: 'replayProgress', data: event });
          }
        }
      );
      if (result.success) {
        this._log(webview, `[end] Step ${index + 1} executed.\r\n`);
      } else {
        this._log(webview, `[end] Step ${index + 1} failed: ${result.error ?? 'unknown'}.\r\n`);
      }
      this._safePost(webview, {
        type: 'executeResult',
        fromActionColumn: true,
        success: result.success,
        error: result.error ?? '',
        index,
        failedIndex: typeof result.failedIndex === 'number' ? index : undefined,
      });
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Step ${index + 1} error: ${msg}\r\n`);
      this._safePost(webview, {
        type: 'executeResult',
        fromActionColumn: true,
        success: false,
        error: msg,
        index,
        failedIndex: index,
      });
    }
  }

  private _handleShowVisualAbout(webview: vscode.Webview): void {
    const text = 'Java UI Automation - Visual flowchart for Record & Replay. Record UI events, view them in Visual tab, and replay with Execute.';
    vscode.window.showInformationMessage(text);
    this._log(webview, '[info] About: Java UI Automation - Record & Replay.\r\n');
  }

  private async _handleExecute(webview: vscode.Webview, pid?: number, stepsFromPanel?: unknown): Promise<void> {
    if (pid == null || pid <= 0) {
      this._log(webview, '[action] Execute: please select a Java application first.\r\n');
      this._safePost(webview, { type: 'log', data: '[info] Select an application from the dropdown before Execute.\r\n' });
      return;
    }
    let steps: Record<string, unknown>[] = [];
    if (Array.isArray(stepsFromPanel) && stepsFromPanel.length > 0) {
      // Sanitize steps before sending to agent: drop empty index and bounds/screenBounds in identifiers.
      steps = (stepsFromPanel as Record<string, unknown>[]).map((raw) => {
        const step = { ...raw } as Record<string, unknown>;
        const sanitizeId = (id: unknown): unknown => {
          const obj = (id ?? {}) as Record<string, unknown>;
          const copy: Record<string, unknown> = { ...obj };
          if (copy.index === '' || copy.index == null) {
            delete copy.index;
          }
          delete copy.bounds;
          delete copy.screenBounds;
          return copy;
        };
        if (step.parentIdentifier) {
          step.parentIdentifier = sanitizeId(step.parentIdentifier);
        }
        if (step.objectIdentifier) {
          step.objectIdentifier = sanitizeId(step.objectIdentifier);
        }
        return step;
      });
    } else {
      this._log(webview, '[action] Execute: Test Steps \u4e3a\u7a7a\uff0c\u8bf7\u5148\u6dfb\u52a0\u6b65\u9aa4\u3002\r\n');
      this._safePost(webview, { type: 'log', data: '[info] Test Steps \u4e3a\u7a7a\uff0c\u8bf7\u5148\u5728 Visual \u6807\u7b7e\u70b9\u51fb\u6811\u8282\u70b9\u6dfb\u52a0\u6b65\u9aa4\u3002\r\n' });
      vscode.window.showWarningMessage('Test Steps \u4e3a\u7a7a\uff0c\u8bf7\u5148\u6dfb\u52a0\u6b65\u9aa4\u3002');
      return;
    }
    this._log(webview, `[begin] Replaying ${steps.length} step(s) on PID=${pid}...\r\n`);
    await this._refreshLicenseFromServerIfDue();
    const permit = this._evaluateReplayPermission(steps.length);
    if (!permit.allowed) {
      const msg = permit.message ?? 'License restriction: replay is limited.';
      this._log(webview, `[license] ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
      vscode.window.showWarningMessage(msg);
      return;
    }
    try {
      await this._bringProcessWindowToFront(pid);
    } catch {
      // ignore; replay continues
    }
    const outDir = this._getScanDir();
    try {
      const result = await replaySteps(pid, outDir, steps, (event: ReplayProgressEvent) => {
        this._safePost(webview, { type: 'replayProgress', data: event });
      });
      if (result.success) {
        this._log(webview, `[end] Replay completed. ${result.count ?? steps.length} step(s) executed.\r\n`);
      } else {
        this._log(webview, `[end] Replay failed: ${result.error ?? 'unknown'}.\r\n`);
        this._safePost(webview, { type: 'error', message: result.error ?? 'Replay failed.' });
        if (typeof result.failedIndex === 'number') {
          this._safePost(webview, { type: 'executeResult', failedIndex: result.failedIndex, error: result.error ?? '' });
        }
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Replay error: ${msg}\r\n`);
      this._safePost(webview, { type: 'error', message: msg });
    }
  }

  private async _handleExportDiagnostics(webview: vscode.Webview): Promise<void> {
    const baseUri = await vscode.window.showOpenDialog({
      title: 'Select a folder to export diagnostics',
      canSelectFiles: false,
      canSelectFolders: true,
      canSelectMany: false,
      defaultUri: vscode.Uri.file(this._getScanDir()),
      openLabel: 'Export Here',
    });
    if (!baseUri || baseUri.length === 0) {
      this._log(webview, '[action] Export diagnostics canceled.\r\n');
      return;
    }

    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const bundleDir = path.join(baseUri[0].fsPath, `mars-diagnostics-${timestamp}`);
    const logsDir = path.join(bundleDir, 'logs');
    const configDir = path.join(bundleDir, 'config');
    const runtimeDir = path.join(bundleDir, 'runtime');
    fs.mkdirSync(logsDir, { recursive: true });
    fs.mkdirSync(configDir, { recursive: true });
    fs.mkdirSync(runtimeDir, { recursive: true });

    const copiedFiles: string[] = [];
    const copyIfExists = (src: string, dst: string): void => {
      try {
        if (!fs.existsSync(src)) return;
        fs.mkdirSync(path.dirname(dst), { recursive: true });
        fs.copyFileSync(src, dst);
        copiedFiles.push(dst);
      } catch {
        // ignore
      }
    };

    const extensionRoot = this._extensionUri.fsPath;
    copyIfExists(AGENT_LOADER_LOG_FILE, path.join(logsDir, 'marsExtension-agentLoader.log'));
    copyIfExists(path.join(extensionRoot, 'java', 'marsJavaAgent', 'src', 'main', 'resources', 'marsJavaAgent-config.json'), path.join(configDir, 'marsJavaAgent-config.default.json'));
    copyIfExists(path.join(extensionRoot, 'java', 'marsJavaAgent', 'target', 'marsJavaAgent-config.json'), path.join(configDir, 'marsJavaAgent-config.runtime.json'));

    const scanDir = this._getScanDir();
    try {
      const recordDirs = fs.readdirSync(scanDir)
        .filter((name) => name.startsWith('record-'))
        .map((name) => ({ name, full: path.join(scanDir, name), mtime: fs.statSync(path.join(scanDir, name)).mtimeMs }))
        .sort((a, b) => b.mtime - a.mtime)
        .slice(0, 3);
      for (const dir of recordDirs) {
        copyIfExists(path.join(dir.full, 'record-debug.log'), path.join(logsDir, dir.name, 'record-debug.log'));
        copyIfExists(path.join(dir.full, 'toolbutton-tooltips.log'), path.join(logsDir, dir.name, 'toolbutton-tooltips.log'));
        copyIfExists(path.join(dir.full, 'record.jsonl'), path.join(logsDir, dir.name, 'record.jsonl'));
      }
    } catch {
      // ignore
    }

    const packageJsonPath = path.join(extensionRoot, 'package.json');
    let extensionVersion = 'unknown';
    try {
      const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf-8')) as { version?: string };
      if (typeof packageJson.version === 'string' && packageJson.version.trim()) {
        extensionVersion = packageJson.version.trim();
      }
    } catch {
      // ignore
    }

    fs.writeFileSync(path.join(runtimeDir, 'panel-log.txt'), this._lastLogText || '', 'utf-8');
    fs.writeFileSync(path.join(runtimeDir, 'steps.json'), JSON.stringify({ steps: this._currentSteps }, null, 2), 'utf-8');

    const summary = {
      generatedAt: new Date().toISOString(),
      extensionVersion,
      platform: process.platform,
      nodeVersion: process.version,
      processCount: this._currentProcesses.length,
      objectCount: this._currentObjects.length,
      stepCount: this._currentSteps.length,
      copiedFileCount: copiedFiles.length,
      copiedFiles,
      configSummary: {
        IsHighlightObjectWhileReplay: this._readReplayHighlightConfig(),
      },
    };
    fs.writeFileSync(path.join(bundleDir, 'summary.json'), JSON.stringify(summary, null, 2), 'utf-8');

    this._log(webview, `[end] Diagnostics exported: ${bundleDir}\r\n`);
    vscode.window.showInformationMessage(`Diagnostics exported: ${bundleDir}`);
  }

  private async _handleExportObjects(webview: vscode.Webview, payload: unknown): Promise<void> {
    const data = (payload && typeof payload === 'object') ? (payload as { objectTree?: unknown; objects?: unknown }) : {};
    const objectTree = Array.isArray(data.objectTree) ? data.objectTree : [];
    const objects = Array.isArray(data.objects) ? data.objects : [];
    const parentObjects = objectTree;
    const defaultUri = vscode.Uri.file(path.join(this._getScanDir(), `mars-objects-${Date.now()}.json`));
    const targetUri = await vscode.window.showSaveDialog({
      title: 'Export Objects (JSON)',
      defaultUri,
      filters: { 'JSON Files': ['json'] },
      saveLabel: 'Export',
    });
    if (!targetUri) {
      this._log(webview, '[action] Export objects canceled.\r\n');
      return;
    }

    try {
      const output = {
        marker: 'MARS_UI_OBJECTS',
        generatedAt: new Date().toISOString(),
        parentObjects,
        objectTree,
        objects,
      };
      fs.writeFileSync(targetUri.fsPath, JSON.stringify(output, null, 2), 'utf-8');
      this._log(webview, `[end] Export objects success: ${targetUri.fsPath}\r\n`);
      vscode.window.showInformationMessage('Objects exported successfully.');
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[end] Export objects failed: ${msg}\r\n`);
      vscode.window.showErrorMessage(`Export objects failed: ${msg}`);
    }
  }

  private _licenseStateFromIssuedLicense(
    license: Record<string, unknown>,
    base: ClientLicenseState,
    message?: string
  ): ClientLicenseState {
    const regionRaw = String(license.region ?? base.region).toUpperCase();
    const region: 'US' | 'CN' | 'GLOBAL' = regionRaw === 'US' ? 'US' : regionRaw === 'CN' ? 'CN' : 'GLOBAL';
    const amount = Number(license.amount ?? NaN);
    const currencyRaw = String(license.currency ?? '').toUpperCase();
    const price = Number.isFinite(amount)
      ? { currency: currencyRaw || (region === 'CN' ? 'CNY' : 'USD'), amount }
      : base.price;
    const plan = String(license.plan ?? '').toUpperCase();
    const licenseType: LicenseType = plan === 'TEST' ? 'TEST' : 'PAID';
    const next: ClientLicenseState = {
      ...base,
      licenseType,
      region,
      expiresAt: typeof license.expiresAt === 'string' ? license.expiresAt : base.expiresAt,
      message: message ?? `License imported (${licenseType}).`,
      price,
    };
    return next;
  }

  private async _handleOpenZellePortal(webview: vscode.Webview): Promise<void> {
    const base = this._getLicenseServerBaseUrl();
    const url = `${base}/stripe`;
    await vscode.env.openExternal(vscode.Uri.parse(url));
    this._log(webview, `[action] Opened payment page: ${url}\r\n`);
  }

  private async _handleImportLicenseFile(webview: vscode.Webview): Promise<void> {
    const pick = await vscode.window.showOpenDialog({
      title: 'Import License File',
      canSelectMany: false,
      canSelectFiles: true,
      canSelectFolders: false,
      filters: { 'JSON Files': ['json'] },
    });
    if (!pick || pick.length === 0) {
      this._log(webview, '[action] Import license canceled.\r\n');
      return;
    }
    try {
      const text = fs.readFileSync(pick[0].fsPath, 'utf-8');
      const parsed = JSON.parse(text) as Record<string, unknown>;
      const license = (parsed.license && typeof parsed.license === 'object')
        ? parsed.license as Record<string, unknown>
        : parsed;
      const licenseId = String(license.licenseId ?? '').trim();
      if (!licenseId) {
        throw new Error('Invalid license file: missing licenseId');
      }
      const current = this._loadLicenseState();
      const next = this._licenseStateFromIssuedLicense(license, current, 'Imported from file.');
      this._saveLicenseState(next);
      this._pushLicenseStatus(webview);
      this._log(webview, `[license] Imported license from file (${pick[0].fsPath}).\r\n`);
      vscode.window.showInformationMessage('License imported successfully.');
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[license] Import failed: ${msg}\r\n`);
      vscode.window.showErrorMessage(`Import license failed: ${msg}`);
    }
  }

  private async _handleFetchZelleLicense(webview: vscode.Webview): Promise<void> {
    const orderId = await vscode.window.showInputBox({
      title: 'Fetch License - Stripe Order ID',
      prompt: 'Enter Stripe order ID (e.g. STRIPE-YYYYMMDD-XXXXXX)',
      ignoreFocusOut: true,
    });
    if (!orderId || !orderId.trim()) return;
    const email = await vscode.window.showInputBox({
      title: 'Fetch License - Email',
      prompt: 'Enter the same payment email used for this order',
      ignoreFocusOut: true,
    });
    if (!email || !email.trim()) return;
    try {
      const base = this._getLicenseServerBaseUrl();
      const url = `${base}/v1/stripe/order/license?orderId=${encodeURIComponent(orderId.trim())}&email=${encodeURIComponent(email.trim().toLowerCase())}`;
      const resp = await this._httpGetJson(url, 3500);
      const licenseObj = (resp.license && typeof resp.license === 'object')
        ? resp.license as Record<string, unknown>
        : null;
      if (!licenseObj) {
        throw new Error('License not ready yet. Please wait for admin approval.');
      }
      const current = this._loadLicenseState();
      const next = this._licenseStateFromIssuedLicense(licenseObj, current, 'Imported from approved Stripe order.');
      this._saveLicenseState(next);
      this._pushLicenseStatus(webview);
      this._log(webview, `[license] Imported license from Stripe order ${orderId.trim()}.\r\n`);
      vscode.window.showInformationMessage(`License imported from order ${orderId.trim()}.`);
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      this._log(webview, `[license] Fetch by order failed: ${msg}\r\n`);
      vscode.window.showWarningMessage(`Fetch license failed: ${msg}`);
    }
  }

  private _readReplayHighlightConfig(): boolean | 'unknown' {
    const extensionRoot = this._extensionUri.fsPath;
    const runtimeConfigPath = path.join(extensionRoot, 'java', 'marsJavaAgent', 'target', 'marsJavaAgent-config.json');
    const defaultConfigPath = path.join(extensionRoot, 'java', 'marsJavaAgent', 'src', 'main', 'resources', 'marsJavaAgent-config.json');
    const parse = (filePath: string): boolean | undefined => {
      try {
        if (!fs.existsSync(filePath)) return undefined;
        const parsed = JSON.parse(fs.readFileSync(filePath, 'utf-8')) as { IsHighlightObjectWhileReplay?: unknown };
        if (typeof parsed.IsHighlightObjectWhileReplay === 'boolean') {
          return parsed.IsHighlightObjectWhileReplay;
        }
      } catch {
        return undefined;
      }
      return undefined;
    };
    const runtime = parse(runtimeConfigPath);
    if (typeof runtime === 'boolean') return runtime;
    const defaultVal = parse(defaultConfigPath);
    if (typeof defaultVal === 'boolean') return defaultVal;
    return 'unknown';
  }

  private _getLicenseStatePath(): string {
    return path.join(this._getScanDir(), LICENSE_STATE_FILE);
  }

  private _getLicenseDeclarationPath(): string {
    return path.join(this._getScanDir(), LICENSE_DECLARATION_FILE);
  }

  private _loadLicenseState(): ClientLicenseState {
    const nowIso = new Date().toISOString();
    let firstSeen = this._context.globalState.get<string>(LICENSE_FIRST_SEEN_KEY);
    if (!firstSeen || Number.isNaN(Date.parse(firstSeen))) {
      firstSeen = nowIso;
      void this._context.globalState.update(LICENSE_FIRST_SEEN_KEY, firstSeen);
    }

    const fallback: ClientLicenseState = {
      licenseType: 'TRIAL_LIMITED',
      region: 'GLOBAL',
      trialStartAt: firstSeen,
      trialDays: 7,
      replayMaxStepsAfterTrialDays: 10,
      message: 'Trial mode: after 7 days replay supports up to 10 steps. Upgrade to continue.',
    };

    const filePath = this._getLicenseStatePath();
    if (!fs.existsSync(filePath)) return fallback;
    try {
      const parsed = JSON.parse(fs.readFileSync(filePath, 'utf-8')) as Partial<ClientLicenseState>;
      const licenseType = (parsed.licenseType ?? fallback.licenseType) as LicenseType;
      const regionRaw = String(parsed.region ?? fallback.region).toUpperCase();
      const region: 'US' | 'CN' | 'GLOBAL' = regionRaw === 'US' ? 'US' : regionRaw === 'CN' ? 'CN' : 'GLOBAL';
      const trialStartAt = typeof parsed.trialStartAt === 'string' && !Number.isNaN(Date.parse(parsed.trialStartAt))
        ? parsed.trialStartAt
        : fallback.trialStartAt;
      const trialDays = Number.isFinite(Number(parsed.trialDays)) && Number(parsed.trialDays) > 0
        ? Number(parsed.trialDays)
        : fallback.trialDays;
      const replayMaxStepsAfterTrialDays = Number.isFinite(Number(parsed.replayMaxStepsAfterTrialDays)) && Number(parsed.replayMaxStepsAfterTrialDays) > 0
        ? Number(parsed.replayMaxStepsAfterTrialDays)
        : fallback.replayMaxStepsAfterTrialDays;
      const expiresAt = typeof parsed.expiresAt === 'string' ? parsed.expiresAt : undefined;
      const message = typeof parsed.message === 'string' && parsed.message.trim()
        ? parsed.message.trim()
        : fallback.message;
      const price = parsed.price && typeof parsed.price === 'object'
        ? parsed.price as { currency?: string; amount?: number }
        : undefined;
      const testPool = parsed.testPool && typeof parsed.testPool === 'object'
        ? parsed.testPool as Record<string, { used?: number; limit?: number; remaining?: number }>
        : undefined;
      return {
        licenseType: licenseType === 'TEST' || licenseType === 'PAID' || licenseType === 'TRIAL_LIMITED' ? licenseType : fallback.licenseType,
        region,
        trialStartAt,
        trialDays,
        replayMaxStepsAfterTrialDays,
        expiresAt,
        message,
        price,
        testPool,
      };
    } catch {
      return fallback;
    }
  }

  private _evaluateReplayPermission(stepCount: number): { allowed: boolean; message?: string } {
    const state = this._loadLicenseState();
    const now = Date.now();
    if (state.expiresAt && Number.isFinite(Date.parse(state.expiresAt)) && now >= Date.parse(state.expiresAt)) {
      return { allowed: false, message: 'License expired. Please renew your license.' };
    }
    if (state.licenseType === 'PAID' || state.licenseType === 'TEST') {
      return { allowed: true };
    }
    const trialStart = Number.isFinite(Date.parse(state.trialStartAt)) ? Date.parse(state.trialStartAt) : now;
    const days = Math.floor((now - trialStart) / 86400000);
    if (days < state.trialDays) {
      return { allowed: true };
    }
    if (stepCount <= state.replayMaxStepsAfterTrialDays) {
      return { allowed: true };
    }
    const priceHint = state.region === 'US'
      ? 'Upgrade price: $4.99'
      : state.region === 'CN'
        ? '升级价格：5元'
        : 'Upgrade required';
    const msg = state.message
      ?? `Trial limit reached: replay supports up to ${state.replayMaxStepsAfterTrialDays} steps after ${state.trialDays} days. ${priceHint}`;
    return { allowed: false, message: msg };
  }

  private _pushLicenseStatus(webview: vscode.Webview): void {
    const state = this._loadLicenseState();
    const pool = state.testPool ?? {};
    const us = pool.US ?? {};
    const cn = pool.CN ?? {};
    const priceText = state.region === 'US'
      ? '$4.99'
      : state.region === 'CN'
        ? '5 CNY'
        : '';
    const message = `License: ${state.licenseType} | Region: ${state.region}${priceText ? ` | Price: ${priceText}` : ''}`;
    const detail = `Trial=${state.trialDays}d, ReplayLimitAfterTrial=${state.replayMaxStepsAfterTrialDays}, Pool(US ${us.remaining ?? '-'} / CN ${cn.remaining ?? '-'})`;
    this._safePost(webview, {
      type: 'licenseStatus',
      data: {
        message,
        detail,
        licenseType: state.licenseType,
        region: state.region,
      },
    });
  }

  private _getLicenseServerBaseUrl(): string {
    const configured = vscode.workspace.getConfiguration('loaniq').get<string>(LICENSE_SERVER_URL_CONFIG, '').trim();
    if (configured) return configured.replace(/\/+$/, '');
    return 'http://127.0.0.1:8787';
  }

  private _httpGetJson(urlText: string, timeoutMs: number): Promise<Record<string, unknown>> {
    return new Promise((resolve, reject) => {
      const u = new URL(urlText);
      const lib = u.protocol === 'https:' ? https : http;
      const req = lib.request({
        method: 'GET',
        hostname: u.hostname,
        port: u.port ? Number(u.port) : (u.protocol === 'https:' ? 443 : 80),
        path: `${u.pathname}${u.search}`,
        headers: {
          Accept: 'application/json',
        },
      }, (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (d) => chunks.push(Buffer.isBuffer(d) ? d : Buffer.from(d)));
        res.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf-8');
          if ((res.statusCode ?? 500) < 200 || (res.statusCode ?? 500) >= 300) {
            reject(new Error(`HTTP ${res.statusCode}: ${body.slice(0, 300)}`));
            return;
          }
          try {
            const parsed = JSON.parse(body) as Record<string, unknown>;
            resolve(parsed);
          } catch (e) {
            reject(new Error(`Invalid JSON from license server: ${String(e)}`));
          }
        });
      });
      req.setTimeout(timeoutMs, () => req.destroy(new Error('request timeout')));
      req.on('error', reject);
      req.end();
    });
  }

  private _saveLicenseState(state: ClientLicenseState): void {
    try {
      fs.writeFileSync(this._getLicenseStatePath(), JSON.stringify(state, null, 2), 'utf-8');
    } catch {
      // ignore
    }
  }

  private async _refreshLicenseFromServerIfDue(force = false): Promise<void> {
    if (this._licenseRefreshInFlight) {
      await this._licenseRefreshInFlight;
      return;
    }
    const lastSync = this._context.globalState.get<number>(LICENSE_LAST_SYNC_KEY, 0);
    const now = Date.now();
    if (!force && now - lastSync < LICENSE_SYNC_INTERVAL_MS) return;

    this._licenseRefreshInFlight = (async () => {
      try {
        const base = this._getLicenseServerBaseUrl();
        const current = this._loadLicenseState();
        const region = current.region || 'GLOBAL';
        const lang = (vscode.env.language || 'en').toLowerCase().startsWith('zh') ? 'zh' : 'en';

        const stateUrl = `${base}/v1/license/client-state?region=${encodeURIComponent(region)}`;
        const stateResp = await this._httpGetJson(stateUrl, 2500);
        const stateObj = (stateResp.state ?? {}) as Partial<ClientLicenseState>;
        if (stateObj && typeof stateObj === 'object') {
          const merged: ClientLicenseState = {
            ...current,
            ...stateObj,
            licenseType: (stateObj.licenseType === 'TEST' || stateObj.licenseType === 'PAID' || stateObj.licenseType === 'TRIAL_LIMITED')
              ? stateObj.licenseType
              : current.licenseType,
            region: stateObj.region === 'US' || stateObj.region === 'CN' || stateObj.region === 'GLOBAL'
              ? stateObj.region
              : current.region,
            trialStartAt: typeof stateObj.trialStartAt === 'string' ? stateObj.trialStartAt : current.trialStartAt,
            trialDays: Number.isFinite(Number(stateObj.trialDays)) ? Number(stateObj.trialDays) : current.trialDays,
            replayMaxStepsAfterTrialDays: Number.isFinite(Number(stateObj.replayMaxStepsAfterTrialDays))
              ? Number(stateObj.replayMaxStepsAfterTrialDays)
              : current.replayMaxStepsAfterTrialDays,
          };
          this._saveLicenseState(merged);
        }

        const declUrl = `${base}/v1/license/declaration?lang=${lang}`;
        const declResp = await this._httpGetJson(declUrl, 2500);
        const declaration = typeof declResp.declaration === 'string' ? declResp.declaration : '';
        if (declaration.trim()) {
          fs.writeFileSync(this._getLicenseDeclarationPath(), declaration, 'utf-8');
        }
      } catch {
        // keep local fallback silently
      } finally {
        await this._context.globalState.update(LICENSE_LAST_SYNC_KEY, Date.now());
      }
    })();

    try {
      await this._licenseRefreshInFlight;
    } finally {
      this._licenseRefreshInFlight = null;
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

    this._recordingSteps = [];
    this._recordingEngine = new RecordingEngine({
      onStep: (step) => {
        const testStep = recordedStepToTestScriptStep(step);
        if (!testStep.keyword?.trim() || !testStep.objectIdentifier?.javaType) return;
        this._logRecordingStep(testStep);
        this._recordingSteps.push(testStep);
        const webviewRef = this._recordingWebview;
        if (webviewRef) {
          this._safePost(webviewRef, { type: 'step', data: testStep });
        }
      },
    });

    try {
      this._log(webview, `[begin] Injecting record agent and connecting (PID=${pid}).\r\n`);
      const result = await startRecordAgent(pid, outDir, (ev) => {
        this._recordingEngine?.onAgentEvent(ev as Record<string, unknown>);
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
        // Events that produce a step are already synced via onStep -> type 'step'; webview derives visual from steps.
        // Do not post a separate visualNode for these to avoid duplicate nodes. For text field we only get fillEdit
        // on lost focus / Enter / Tab (no per-key events), so no intermediate nodes.
        const stepEvent = ev.event as string | undefined;
        const isStepEvent =
          stepEvent === 'fillEdit' ||
          stepEvent === 'clickButton' ||
          stepEvent === 'selectMenuItem' ||
          stepEvent === 'selectPopupMenu' ||
          stepEvent === 'selectTreeList' ||
          stepEvent === 'selectMenuIcon' ||
          stepEvent === 'selectDropList' ||
          stepEvent === 'selectDropDown' ||
          stepEvent === 'searchAndClick' ||
          stepEvent === 'searchAndUpdate' ||
          stepEvent === 'selectTab' ||
          stepEvent === 'expandTreeNode' ||
          stepEvent === 'collapseTreeNode';
        if (isStepEvent) return;

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
      this._recordSend = result.send ?? null;
      this._log(webview, '[end] Recording started. Events will appear in Visual tab. Use Ctrl+Alt+F12 to stop.\r\n');
      this._safePost(webview, { type: 'recordingStarted' });
    } catch (e) {
      this._recordingPid = null;
      this._recordingWebview = null;
      this._recordStop = null;
      this._recordSend = null;
      this._recordingEngine = null;
      this._recordingSteps = [];
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

    this._recordingEngine?.flush();
    const stepsFromRecorder = [...this._recordingSteps];
    this._recordingEngine = null;
    this._recordingSteps = [];
    this._recordingPid = null;
    this._recordingWebview = null;
    this._recordSend = null;

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
    let steps: TestScriptStep[] = stepsFromRecorder;

    if (steps.length === 0 && fs.existsSync(recordFile)) {
      const content = fs.readFileSync(recordFile, 'utf-8');
      const lines = content.split('\n').filter((line) => line.trim().length > 0);
      for (const line of lines) {
        try {
          const obj = JSON.parse(line) as Record<string, unknown>;
          const step = this._normalizeRecordLineToStep(obj);
          if (step && JavaUIPanelProvider._isMeaningfulStep(step)) steps.push(step);
        } catch {
          // skip malformed lines
        }
      }
    }
    steps = steps.filter((s) => JavaUIPanelProvider._isMeaningfulStep(s));

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

    this._currentSteps = steps;
    const scriptPath = this._getScriptPath();
    if (scriptPath) {
      try {
        const scriptDir = path.dirname(scriptPath);
        if (!fs.existsSync(scriptDir)) fs.mkdirSync(scriptDir, { recursive: true });
        fs.writeFileSync(scriptPath, JSON.stringify({ steps }, null, 2), 'utf-8');
      } catch (e) {
        this._outputChannel.appendLine(`[Java UI] sync steps to script.json failed: ${e}`);
      }
    }
    this._persistState();

    const message = `Recording stopped. Steps: ${steps.length}. Saved to ${outputPath}`;
    vscode.window.showInformationMessage(message);

    if (webview) {
      this._safePost(webview, { type: 'steps', data: steps });
      this._safePost(webview, { type: 'recordingStopped' });
    }
  }

  /** Bring target process main window to foreground (single-screen observation). */
  private _bringProcessWindowToFront(pid: number): Promise<void> {
    const exe = findProcessInfoExe(this._extensionUri.fsPath);
    if (!exe) return Promise.resolve();
    return new Promise((resolve) => {
      const proc = spawn(exe, ['-bringToFront', String(pid)], { stdio: 'ignore' });
      proc.on('close', () => resolve());
      proc.on('error', () => resolve());
      setTimeout(() => resolve(), 2000);
    });
  }

  private static _isMeaningfulStep(step: TestScriptStep): boolean {
    const o = step.objectIdentifier;
    if (!o) return false;
    const hasType = typeof o.javaType === 'string' && o.javaType.trim().length > 0;
    const hasBounds = o.screenBounds && typeof o.screenBounds === 'object';
    return hasType || !!hasBounds;
  }

  /** Normalize a record.jsonl line (keyword step or legacy event) to TestScriptStep. */
  private _normalizeRecordLineToStep(obj: Record<string, unknown>): TestScriptStep | null {
    const emptyId = {};
    if (obj.keyword && obj.objectIdentifier && typeof obj.objectIdentifier === 'object') {
      const oid = obj.objectIdentifier as Record<string, unknown>;
      const hasType = typeof oid.javaType === 'string' && (oid.javaType as string).trim().length > 0;
      const hasBounds = oid.screenBounds && typeof oid.screenBounds === 'object';
      if (!hasType && !hasBounds) return null;
      const kw = obj.keyword as string;
      const validKw: ScriptKeyword[] = [
        'Click', 'ClickButton', 'DoubleClickButton', 'ClickMenuIcon', 'FillEdit',
        'SelectDropDown', 'SelectDropList', 'SelectListItem', 'SelectMenuItem',
        'SelectTreeList', 'SelectTab', 'SelectMenuIcon', 'SelectPopupMenu', 'ClickAT',
        'SearchAndClick', 'SearchAndUpdate', 'SetRadioBox', 'SetCheckBox', 'Check', 'Uncheck'
        , 'VerifyObjectValue'
      ];
      return {
        keyword: validKw.includes(kw as ScriptKeyword) ? (kw as ScriptKeyword) : 'Click',
        parentIdentifier: (obj.parentIdentifier as object) || emptyId,
        objectIdentifier: obj.objectIdentifier as import('./types').ElementIdentifier,
        parameter: typeof obj.parameter === 'string' ? obj.parameter : '',
        data: typeof obj.data === 'string' ? obj.data : '',
        assertValue: typeof obj.assertValue === 'string' ? obj.assertValue : '',
        skipped: false,
      };
    }
    if (obj.event === 'click') {
      const o = obj as Record<string, unknown>;
      const javaType = (o.componentClass as string) || '';
      const objectIdentifier: import('./types').ElementIdentifier = { javaType };
      if (o.componentName) objectIdentifier.name = String(o.componentName);
      if (o.text) objectIdentifier.text = String(o.text);
      if (o.screenX != null && o.screenY != null) {
        objectIdentifier.screenBounds = {
          x: Number(o.screenX),
          y: Number(o.screenY),
          width: Number(o.width) || 0,
          height: Number(o.height) || 0,
        };
      }
      return { keyword: 'Click', parentIdentifier: emptyId, objectIdentifier, parameter: '', data: '', assertValue: '', skipped: false };
    }
    if (obj.event === 'fillEdit') {
      const o = obj as Record<string, unknown>;
      const javaType = (o.componentClass as string) || '';
      const objectIdentifier: import('./types').ElementIdentifier = { javaType };
      if (o.componentName) objectIdentifier.name = String(o.componentName);
      if (o.text) objectIdentifier.text = String(o.text);
      if (o.screenX != null && o.screenY != null) {
        objectIdentifier.screenBounds = {
          x: Number(o.screenX),
          y: Number(o.screenY),
          width: Number(o.width) || 0,
          height: Number(o.height) || 0,
        };
      }
      const data = (o.content as string) ?? (o.data as string) ?? '';
      return { keyword: 'FillEdit', parentIdentifier: emptyId, objectIdentifier, parameter: '', data, assertValue: '', skipped: false };
    }
    return null;
  }

  private _getHtml(webview: vscode.Webview): string {
    // Load HTML from separate file
    const htmlPath = path.join(this._extensionUri.fsPath, 'src', 'panel.html');
    try {
      const htmlContent = fs.readFileSync(htmlPath, 'utf-8');
      const locale = (vscode.env.language || 'en').trim() || 'en';
      return htmlContent.replace(/__MARS_LOCALE__/g, locale.replace(/'/g, "\\'"));
    } catch (err) {
      this._outputChannel.appendLine(`[Java UI] Error loading panel.html: ${String(err)}`);
      return `<html><body><h1>Failed to load panel</h1><p>Error: ${String(err)}</p></body></html>`;
    }
  }
}
