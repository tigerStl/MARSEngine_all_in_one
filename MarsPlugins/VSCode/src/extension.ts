/**
 * Java UI Automation Test Extension
 * Scans Java application UI, generates test scripts (no execution)
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
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
    description: p.mainClass ?? '',
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

export function activate(context: vscode.ExtensionContext): void {
  const outputCh = getOutputChannel();
  const panelProvider = new JavaUIPanelProvider(context.extensionUri, outputCh, context);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider('javaUiAutomation.panel', panelProvider)
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
