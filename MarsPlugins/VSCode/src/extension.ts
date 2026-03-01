/**
 * Java UI Automation Test Extension
 * Scans Java application UI, generates test scripts (no execution)
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import * as http from 'http';
import { randomBytes } from 'crypto';
import { getJavaProcesses } from './processInfo';
import { loadAgentAndScan } from './agentLoader';
import {
  generateScriptStep,
  addConstant,
  saveScript,
  saveConstants,
  inferKeywordFromJavaType,
} from './scriptGenerator';
import { convertScanToUIObjects, ScanOutput } from './objectConverter';
import { JavaProcess, UIObject, TestScriptStep, ConstantsFile } from './types';
import { JavaUIPanelProvider } from './panelProvider';

const SCANED_FILES_DIR = 'scanedfiles';

let outputChannel: vscode.OutputChannel;

type McpBridgeInfo = {
  port: number;
  token: string;
};

function getScanDir(context: vscode.ExtensionContext): string {
  const dir = path.join(context.extensionPath, SCANED_FILES_DIR);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  return dir;
}

function getOutputChannel(): vscode.OutputChannel {
  if (!outputChannel) {
    outputChannel = vscode.window.createOutputChannel('Java UI Automation');
  }
  return outputChannel;
}

async function selectJavaProcess(): Promise<JavaProcess | undefined> {
  getOutputChannel().appendLine('Fetching Java processes...');
  let processes: JavaProcess[];
  try {
    processes = await getJavaProcesses();
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    vscode.window.showErrorMessage(
      `Failed to get Java processes. Ensure ProcessInfo is built. ${msg}`
    );
    getOutputChannel().appendLine(`Error: ${msg}`);
    return undefined;
  }

  if (processes.length === 0) {
    vscode.window.showInformationMessage('No Java processes found.');
    return undefined;
  }

  const items = processes.map((p) => ({
    label: `${p.pid}: ${p.displayName}`,
    description: `${p.mainClass ?? ''}${p.source ? ` [${p.source}]` : ''}`.trim(),
    detail: p.commandLine,
    process: p,
  }));

  const picked = await vscode.window.showQuickPick(items, {
    matchOnDescription: true,
    matchOnDetail: true,
    placeHolder: 'Select Java process to scan',
  });

  return picked?.process;
}

async function startMcpToolBridge(context: vscode.ExtensionContext, outputCh: vscode.OutputChannel): Promise<McpBridgeInfo | undefined> {
  const token = randomBytes(24).toString('hex');

  const server = http.createServer((req, res) => {
    if (req.method !== 'POST' || req.url !== '/tool') {
      res.statusCode = 404;
      res.end('Not Found');
      return;
    }

    const auth = req.headers['x-mars-token'];
    if (auth !== token) {
      res.statusCode = 401;
      res.end('Unauthorized');
      return;
    }

    const chunks: Buffer[] = [];
    req.on('data', (chunk) => {
      chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
    });

    req.on('end', async () => {
      try {
        const text = Buffer.concat(chunks).toString('utf8') || '{}';
        const payload = JSON.parse(text) as { tool?: string; input?: unknown; requestId?: string };
        const result = await vscode.commands.executeCommand('javaUiAutomation.mcp.callTool', payload);

        res.statusCode = 200;
        res.setHeader('Content-Type', 'application/json; charset=utf-8');
        res.end(JSON.stringify(result ?? null));
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        res.statusCode = 500;
        res.setHeader('Content-Type', 'application/json; charset=utf-8');
        res.end(JSON.stringify({ ok: false, errorCode: 'MARS_E_REPLAY_FAILED', errorMessage: msg, data: null }));
      }
    });
  });

  try {
    await new Promise<void>((resolve, reject) => {
      server.once('error', reject);
      server.listen(0, '127.0.0.1', () => {
        server.off('error', reject);
        resolve();
      });
    });
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    outputCh.appendLine(`[MCP] Failed to start tool bridge server: ${msg}`);
    try { server.close(); } catch { }
    return undefined;
  }

  const addr = server.address();
  if (!addr || typeof addr === 'string') {
    outputCh.appendLine('[MCP] Failed to resolve tool bridge address after listen.');
    try { server.close(); } catch { }
    return undefined;
  }

  const bridgeInfo: McpBridgeInfo = { port: addr.port, token };
  context.subscriptions.push({ dispose: () => server.close() });
  outputCh.appendLine(`[MCP] Tool bridge started on 127.0.0.1:${bridgeInfo.port}`);
  return bridgeInfo;
}

function registerMarsMcpServerProvider(context: vscode.ExtensionContext, outputCh: vscode.OutputChannel, bridgeInfo?: McpBridgeInfo): void {
  const vsAny = vscode as any;
  const McpStdioServerDefinition = vsAny?.McpStdioServerDefinition;

  if (typeof vsAny?.lm?.registerMcpServerDefinitionProvider !== 'function' || typeof McpStdioServerDefinition !== 'function') {
    outputCh.appendLine('[MCP] MCP server definition API is unavailable in current VS Code version.');
    return;
  }

  const providerId = 'javaUiAutomation.marsMcpServerProvider';
  const serverScriptPath = path.join(context.extensionPath, 'out', 'mcp-server.js');

  try {
    context.subscriptions.push(
      vsAny.lm.registerMcpServerDefinitionProvider(providerId, {
        provideMcpServerDefinitions: async () => {
          outputCh.appendLine(`[MCP] provideMcpServerDefinitions invoked. bridgeStarted=${!!bridgeInfo}`);
          if (!fs.existsSync(serverScriptPath)) {
            outputCh.appendLine(`[MCP] Skip provider output: server script not found at ${serverScriptPath}`);
            return [];
          }

          const server = new McpStdioServerDefinition(
            'mars-local',
            process.execPath,
            [serverScriptPath],
            {
              MARS_WORKSPACE: context.extensionPath,
              MARS_MCP_BRIDGE_PORT: bridgeInfo ? String(bridgeInfo.port) : '',
              MARS_MCP_BRIDGE_TOKEN: bridgeInfo?.token ?? '',
              MARS_MCP_TRACE: path.join(context.extensionPath, 'scanedfiles', 'mcp-server.trace.log'),
            },
            '0.1.0'
          );
          server.cwd = vscode.Uri.file(context.extensionPath);

          return [server];
        },
      })
    );
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    outputCh.appendLine(`[MCP] Failed to register provider: ${msg}`);
    return;
  }

  outputCh.appendLine(`[MCP] Registered MCP server definition provider: ${providerId}`);
}

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const outputCh = getOutputChannel();
  const panelProvider = new JavaUIPanelProvider(context.extensionUri, outputCh, context);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider('javaUiAutomation.panel', panelProvider)
  );

  const bridgeInfo = await startMcpToolBridge(context, outputCh);
  registerMarsMcpServerProvider(context, outputCh, bridgeInfo);

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.mcp.showStatus', async () => {
      const vsAny = vscode as any;
      const serverScriptPath = path.join(context.extensionPath, 'out', 'mcp-server.js');
      const status = {
        activatedAt: new Date().toISOString(),
        vscodeVersion: vscode.version,
        mcpApiAvailable: {
          lmNamespace: !!vsAny?.lm,
          registerMcpServerDefinitionProvider: typeof vsAny?.lm?.registerMcpServerDefinitionProvider === 'function',
          mcpStdioServerDefinition: typeof vsAny?.McpStdioServerDefinition === 'function',
        },
        extensionProvider: {
          providerId: 'javaUiAutomation.marsMcpServerProvider',
          serverScriptPath,
          serverScriptExists: fs.existsSync(serverScriptPath),
        },
        bridge: {
          started: !!bridgeInfo,
          port: bridgeInfo?.port ?? null,
        },
      };

      outputCh.appendLine(`[MCP] Status: ${JSON.stringify(status)}`);
      const doc = await vscode.workspace.openTextDocument({
        content: JSON.stringify(status, null, 2),
        language: 'json',
      });
      await vscode.window.showTextDocument(doc, { preview: false });
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.mcp.probe', async () => {
      const probe = {
        ts: new Date().toISOString(),
        listProcesses: null as unknown,
        error: null as string | null,
      };

      try {
        probe.listProcesses = await vscode.commands.executeCommand('javaUiAutomation.mcp.callTool', {
          tool: 'mars.listProcesses',
          input: {},
          requestId: `probe-${Date.now()}`,
        });
      } catch (e) {
        probe.error = e instanceof Error ? e.message : String(e);
      }

      outputCh.appendLine(`[MCP] Probe result: ${JSON.stringify(probe)}`);
      const doc = await vscode.workspace.openTextDocument({
        content: JSON.stringify(probe, null, 2),
        language: 'json',
      });
      await vscode.window.showTextDocument(doc, { preview: false });
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.mcp.callTool', async (payload?: { tool?: string; input?: unknown; requestId?: string }) => {
      const requestId = payload?.requestId ?? `${Date.now()}`;
      const tool = payload?.tool ?? '';
      const input = (payload?.input ?? {}) as Record<string, unknown>;

      const ok = (data: unknown) => ({ ok: true, requestId, errorCode: null, errorMessage: null, data });
      const fail = (errorCode: string, errorMessage: string) => ({ ok: false, requestId, errorCode, errorMessage, data: null });

      try {
        switch (tool) {
          case 'mars.listProcesses': {
            const items = await panelProvider.mcpListProcesses();
            return ok({ items });
          }
          case 'mars.selectProcess': {
            const pid = Number(input.pid);
            if (!Number.isInteger(pid) || pid <= 0) {
              return fail('MARS_E_INVALID_ARGUMENT', 'pid must be a positive integer');
            }
            const result = panelProvider.mcpSelectProcess(pid);
            return ok(result);
          }
          case 'mars.startRecord': {
            const pid = input.pid == null ? undefined : Number(input.pid);
            if (pid != null && (!Number.isInteger(pid) || pid <= 0)) {
              return fail('MARS_E_INVALID_ARGUMENT', 'pid must be a positive integer');
            }
            const result = await panelProvider.mcpStartRecord({ pid });
            return ok(result);
          }
          case 'mars.stopRecord': {
            const result = await panelProvider.mcpStopRecord();
            return ok(result);
          }
          case 'mars.getObjectTree': {
            const result = await panelProvider.mcpGetObjectTree({
              rootWindowHint: typeof input.rootWindowHint === 'string' ? input.rootWindowHint : undefined,
              refresh: typeof input.refresh === 'boolean' ? input.refresh : true,
            });
            return ok(result);
          }
          case 'mars.executeStep': {
            const index = Number(input.index);
            if (!Number.isInteger(index) || index < 0) {
              return fail('MARS_E_INVALID_ARGUMENT', 'index must be an integer >= 0');
            }
            const result = await panelProvider.mcpExecuteStep(index);
            return ok(result);
          }
          case 'mars.getSteps': {
            return ok(panelProvider.mcpGetSteps());
          }
          case 'mars.updateStep': {
            const index = Number(input.index);
            const patch = (input.patch ?? {}) as Record<string, unknown>;
            if (!Number.isInteger(index) || index < 0) {
              return fail('MARS_E_INVALID_ARGUMENT', 'index must be an integer >= 0');
            }
            if (!patch || typeof patch !== 'object' || Object.keys(patch).length === 0) {
              return fail('MARS_E_INVALID_ARGUMENT', 'patch must be a non-empty object');
            }
            return ok(panelProvider.mcpUpdateStep(index, patch as never));
          }
          case 'mars.runReplay': {
            const fromIndex = input.fromIndex == null ? undefined : Number(input.fromIndex);
            const toIndex = input.toIndex == null ? undefined : Number(input.toIndex);
            if (fromIndex != null && (!Number.isInteger(fromIndex) || fromIndex < 0)) {
              return fail('MARS_E_INVALID_ARGUMENT', 'fromIndex must be integer >= 0');
            }
            if (toIndex != null && (!Number.isInteger(toIndex) || toIndex < 0)) {
              return fail('MARS_E_INVALID_ARGUMENT', 'toIndex must be integer >= 0');
            }
            const result = await panelProvider.mcpRunReplay({
              fromIndex,
              toIndex,
              strictParent: typeof input.strictParent === 'boolean' ? input.strictParent : true,
            });
            return ok(result);
          }
          case 'mars.highlightObject': {
            const objectKey = input.objectKey;
            if (!objectKey || typeof objectKey !== 'object') {
              return fail('MARS_E_INVALID_ARGUMENT', 'objectKey must be an object');
            }
            const result = await panelProvider.mcpHighlightObject({
              objectKey: objectKey as Record<string, unknown>,
              parentKey: (input.parentKey && typeof input.parentKey === 'object') ? input.parentKey as Record<string, unknown> : undefined,
            });
            return ok(result);
          }
          case 'mars.exportObjects': {
            const format = typeof input.format === 'string' ? input.format : 'json';
            const includeParents = typeof input.includeParents === 'boolean' ? input.includeParents : true;
            return ok(panelProvider.mcpExportObjects({ format, includeParents }));
          }
          case 'mars.exportDiagnostics': {
            const includeLogs = typeof input.includeLogs === 'boolean' ? input.includeLogs : true;
            return ok(panelProvider.mcpExportDiagnostics({ includeLogs }));
          }
          case 'mars.getLastErrors': {
            const limit = input.limit == null ? undefined : Number(input.limit);
            if (limit != null && (!Number.isInteger(limit) || limit < 0)) {
              return fail('MARS_E_INVALID_ARGUMENT', 'limit must be integer >= 0');
            }
            return ok(panelProvider.mcpGetLastErrors(limit));
          }
          default:
            return fail('MARS_E_INVALID_ARGUMENT', `Unsupported tool: ${tool}`);
        }
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        if (msg.includes('No process selected')) {
          return fail('MARS_E_NO_PROCESS_SELECTED', msg);
        }
        if (msg.includes('Step index')) {
          return fail('MARS_E_STEP_INDEX_INVALID', msg);
        }
        if (msg.includes('Object') || msg.includes('screenBounds')) {
          return fail('MARS_E_OBJECT_NOT_FOUND', msg);
        }
        return fail('MARS_E_REPLAY_FAILED', msg);
      }
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.mcp.callToolInteractive', async () => {
      const toolPick = await vscode.window.showQuickPick([
        'mars.listProcesses',
        'mars.selectProcess',
        'mars.startRecord',
        'mars.stopRecord',
        'mars.getObjectTree',
        'mars.highlightObject',
        'mars.getSteps',
        'mars.updateStep',
        'mars.executeStep',
        'mars.runReplay',
        'mars.exportObjects',
        'mars.exportDiagnostics',
        'mars.getLastErrors',
      ], { placeHolder: 'Select MCP tool to call' });
      if (!toolPick) return;

      const defaultInputs: Record<string, unknown> = {
        'mars.listProcesses': {},
        'mars.selectProcess': { pid: 0 },
        'mars.startRecord': { pid: 0 },
        'mars.stopRecord': {},
        'mars.getObjectTree': { refresh: true },
        'mars.highlightObject': { objectKey: { javaType: 'javax.swing.JButton', name: 'okButton' } },
        'mars.getSteps': {},
        'mars.updateStep': { index: 0, patch: { assertValue: 'expected' } },
        'mars.executeStep': { index: 0 },
        'mars.runReplay': { fromIndex: 0, toIndex: 0, strictParent: true },
        'mars.exportObjects': { format: 'json', includeParents: true },
        'mars.exportDiagnostics': { includeLogs: true },
        'mars.getLastErrors': { limit: 20 },
      };

      const inputText = await vscode.window.showInputBox({
        prompt: 'Input JSON for selected MCP tool',
        value: JSON.stringify(defaultInputs[toolPick] ?? {}),
      });
      if (inputText === undefined) return;

      let input: unknown = {};
      try {
        input = inputText.trim() ? JSON.parse(inputText) : {};
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        vscode.window.showErrorMessage(`Invalid JSON input: ${msg}`);
        return;
      }

      const result = await vscode.commands.executeCommand('javaUiAutomation.mcp.callTool', {
        tool: toolPick,
        input,
        requestId: `interactive-${Date.now()}`,
      });
      const content = JSON.stringify(result, null, 2);
      const doc = await vscode.workspace.openTextDocument({ content, language: 'json' });
      await vscode.window.showTextDocument(doc, { preview: false });
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.selectProcess', async () => {
      const proc = await selectJavaProcess();
      if (!proc) return;

      const outDir = getScanDir(context);

      vscode.window.withProgress(
        {
          location: vscode.ProgressLocation.Notification,
          title: `Scanning UI of process ${proc.pid}...`,
        },
        async () => {
          const result = await loadAgentAndScan(proc.pid, outDir);
          if (result.success && result.outputPath) {
            const scanJson = fs.readFileSync(result.outputPath, 'utf-8');
            const scan: ScanOutput = JSON.parse(scanJson);
            const objects = convertScanToUIObjects(scan);
            const objectsPath = path.join(outDir, 'objects.json');
            fs.writeFileSync(objectsPath, JSON.stringify(objects, null, 2), 'utf-8');
            vscode.window.showInformationMessage(
              `UI scan done. Objects: ${objects.length}`
            );
            const doc = await vscode.workspace.openTextDocument(objectsPath);
            vscode.window.showTextDocument(doc);
          } else {
            vscode.window.showErrorMessage(
              result.error ?? 'UI scan failed'
            );
          }
        }
      );
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.scanUi', async () => {
      vscode.commands.executeCommand('javaUiAutomation.selectProcess');
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.showPanel', () => {
      vscode.commands.executeCommand('javaUiAutomation.panel.focus');
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.stopRecord', () => {
      panelProvider.stopRecordAndShowDialog();
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('javaUiAutomation.generateScript', async () => {
      const scanDir = getScanDir(context);
      const objectsPath = path.join(scanDir, 'objects.json');
      const constantsPath = path.join(scanDir, 'constants.json');

      let objects: UIObject[] = [];
      if (fs.existsSync(objectsPath)) {
        objects = JSON.parse(fs.readFileSync(objectsPath, 'utf-8'));
      }

      let constants: ConstantsFile = { constants: [] };
      if (fs.existsSync(constantsPath)) {
        constants = JSON.parse(fs.readFileSync(constantsPath, 'utf-8'));
      }

      const dataVal = await vscode.window.showInputBox({
        prompt: 'Data to fill (will create constant)',
        placeHolder: 'e.g. test@example.com',
      });
      if (dataVal === undefined) return;

      const dataConstId = addConstant(constants, dataVal);
      saveConstants(constants, constantsPath);

      const steps: TestScriptStep[] = [];
      for (const obj of objects) {
        const kw = inferKeywordFromJavaType(obj.identifier.javaType ?? '');
        if (kw === 'FillEdit' || kw === 'SelectDropDown') {
          const parentObj: UIObject = {
            uniqueName: 'parent',
            identifier: obj.parent ?? {},
            parent: null,
          };
          steps.push(
            generateScriptStep({
              keyword: kw,
              parentObject: parentObj,
              targetObject: obj,
              dataConstantId: dataConstId,
            })
          );
        }
      }

      const scriptPath = path.join(scanDir, `script-${Date.now()}.json`);
      saveScript(steps, scriptPath);
      vscode.window.showInformationMessage(`Script saved: ${path.basename(scriptPath)}`);
      const doc = await vscode.workspace.openTextDocument(scriptPath);
      vscode.window.showTextDocument(doc);
    })
  );
}

export function deactivate(): void {
  outputChannel?.dispose();
}
