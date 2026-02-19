/**
 * Spawns Java Agent Loader to attach UI Scanner / Highlight agent to target JVM process
 */

import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import { spawn } from 'child_process';
import WebSocket from 'ws';

const LOG_FILE = path.join(os.tmpdir(), 'marsExtension-agentLoader.log');

function writeLog(message: string): void {
  try {
    const line = `${new Date().toISOString()} ${message}\n`;
    fs.appendFileSync(LOG_FILE, line, 'utf-8');
  } catch {
    // ignore
  }
}

function getJavaExecutable(): string {
  const javaHome = process.env.JAVA_HOME;
  if (javaHome) {
    const exe = os.platform() === 'win32' ? 'java.exe' : 'java';
    return path.join(javaHome, 'bin', exe);
  }
  return 'java';
}

export interface ScanResult {
  success: boolean;
  outputPath?: string;
  error?: string;
}

export interface HighlightResult {
  success: boolean;
  error?: string;
}

export interface RecordAgentResult {
  success: boolean;
  error?: string;
  /** Call to send stopRecordAndReplay and close the connection. */
  stop?: () => void;
  /** Send a message to the agent (e.g. pauseRecordAndReplay / resumeRecordAndReplay during highlight). */
  send?: (msg: Record<string, unknown>) => void;
}

export async function loadAgentAndScan(
  pid: number,
  outputDir: string
): Promise<ScanResult> {
  const extensionPath = path.join(__dirname, '..');
  const agentLoaderJar = path.join(
    extensionPath,
    'java',
    'agent-loader',
    'target',
    'agent-loader-1.0.jar'
  );
  const marsJavaAgentJar = path.join(
    extensionPath,
    'java',
    'marsJavaAgent',
    'target',
    'marsJavaAgent-1.0.jar'
  );

  if (!fs.existsSync(agentLoaderJar) || !fs.existsSync(marsJavaAgentJar)) {
    const missing = [
      !fs.existsSync(agentLoaderJar) && agentLoaderJar,
      !fs.existsSync(marsJavaAgentJar) && marsJavaAgentJar,
    ].filter(Boolean);
    writeLog(`[loadAgentAndScan] branch: JARs missing pid=${pid} missing=${JSON.stringify(missing)}`);
    return {
      success: false,
      error: `Agent JARs not found. Build Java projects first.\n  Run: cd java && mvn package\n  Missing: ${missing.join(', ')}`,
    };
  }

  const ts = Date.now();
  const outputPath = path.join(outputDir, `ui-scan-${pid}-${ts}.json`);
  // Pass a directory to the agent (not the file path), so it does not create a directory named ui-scan-xxx.json
  const scanRecordDir = path.join(outputDir, `scan-${pid}-${ts}`);
  if (!fs.existsSync(scanRecordDir)) {
    fs.mkdirSync(scanRecordDir, { recursive: true });
  }

  // Copy agent JAR to a temp path so each load uses a different path (avoids "agent already loaded" when re-scanning same process)
  const tempAgentJar = path.join(os.tmpdir(), `marsJavaAgent-scan-${pid}-${ts}.jar`);
  try {
    fs.copyFileSync(marsJavaAgentJar, tempAgentJar);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    writeLog(`[loadAgentAndScan] branch: copy JAR failed pid=${pid} error=${msg}`);
    return { success: false, error: `Failed to copy agent JAR: ${msg}` };
  }

  writeLog(`[loadAgentAndScan] spawn pid=${pid} scanRecordDir=${scanRecordDir} tempJar=${tempAgentJar}`);
  return new Promise((resolve) => {
    const javaExe = getJavaExecutable();
    const proc = spawn(
      javaExe,
      ['-jar', agentLoaderJar, String(pid), tempAgentJar, scanRecordDir],
      { stdio: ['pipe', 'pipe', 'pipe'] }
    );

    let stderr = '';
    proc.stderr?.on('data', (d) => (stderr += d.toString()));
    proc.on('close', async (code) => {
      try {
        if (fs.existsSync(tempAgentJar)) fs.unlinkSync(tempAgentJar);
      } catch {
        // ignore
      }
      if (code !== 0) {
        writeLog(`[loadAgentAndScan] close: failure pid=${pid} code=${code} stderr=${stderr.trim().slice(0, 300)}`);
        resolve({ success: false, error: stderr || `Process exited with code ${code}` });
        return;
      }
      // Agent receives directory and starts WebSocket server; get object tree via JSON-RPC and write to outputPath
      const infoPath = path.join(scanRecordDir, MARS_AGENT_INFO_FILE);
      const deadline = Date.now() + INFO_FILE_TIMEOUT_MS;
      let port: number | undefined;
      while (Date.now() < deadline) {
        await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
        if (fs.existsSync(infoPath)) {
          try {
            const raw = fs.readFileSync(infoPath, 'utf-8');
            const info = JSON.parse(raw) as { port?: number };
            if (typeof info.port === 'number') {
              port = info.port;
              break;
            }
          } catch {
            // retry
          }
        }
      }
      if (port == null) {
        writeLog(`[loadAgentAndScan] timeout waiting for ${MARS_AGENT_INFO_FILE}`);
        resolve({ success: false, error: 'Agent did not write marsJavaAgentInfo.json in time.' });
        return;
      }
      let resolved = false;
      const finish = (result: ScanResult) => {
        if (resolved) return;
        resolved = true;
        resolve(result);
      };
      const ws = new WebSocket(`ws://127.0.0.1:${port}`);
      const rpcId = 1;
      const timeoutHandle = setTimeout(() => {
        if (ws.readyState === WebSocket.OPEN) ws.close();
        finish({ success: false, error: 'Timeout waiting for agent.getObjectTree response.' });
      }, 10000);
      ws.on('open', () => {
        ws.send(JSON.stringify({ method: 'agent.getObjectTree', id: rpcId, params: {} }));
      });
      ws.on('message', (data: Buffer | ArrayBuffer | Buffer[]) => {
        const text = (typeof data === 'string' ? data : data.toString()) as string;
        try {
          const msg = JSON.parse(text) as { id?: number; result?: unknown; error?: string };
          if (msg.id === rpcId) {
            clearTimeout(timeoutHandle);
            ws.close();
            if (msg.error) {
              finish({ success: false, error: msg.error });
              return;
            }
            const tree = msg.result;
            const roots = tree && typeof tree === 'object' && 'roots' in tree ? (tree as { roots: unknown }).roots : [];
            const scanOutput = JSON.stringify({ roots });
            fs.writeFileSync(outputPath, scanOutput, 'utf-8');
            writeLog(`[loadAgentAndScan] got object tree and wrote ${outputPath}`);
            finish({ success: true, outputPath });
          }
        } catch (e) {
          writeLog(`[loadAgentAndScan] message parse error: ${String(e)}`);
        }
      });
      ws.on('error', (err) => {
        clearTimeout(timeoutHandle);
        finish({ success: false, error: err.message });
      });
      ws.on('close', () => {
        if (!resolved) {
          clearTimeout(timeoutHandle);
          finish({ success: false, error: 'WebSocket closed before receiving object tree.' });
        }
      });
    });
    proc.on('error', (err) => {
      writeLog(`[loadAgentAndScan] spawn error pid=${pid} message=${err.message}`);
      resolve({ success: false, error: err.message });
    });
  });
}

const MARS_AGENT_INFO_FILE = 'marsJavaAgentInfo.json';
const POLL_INTERVAL_MS = 200;
const INFO_FILE_TIMEOUT_MS = 15000;

/** Agent injects and runs as WS server; extension connects as client, reads port from recordDir/marsJavaAgentInfo.json. */
export async function startRecordAgent(
  pid: number,
  outputDir: string,
  onEvent: (data: Record<string, unknown>) => void
): Promise<RecordAgentResult> {
  const extensionPath = path.join(__dirname, '..');
  const agentLoaderJar = path.join(
    extensionPath,
    'java',
    'agent-loader',
    'target',
    'agent-loader-1.0.jar'
  );
  const marsJavaAgentJar = path.join(
    extensionPath,
    'java',
    'marsJavaAgent',
    'target',
    'marsJavaAgent-1.0.jar'
  );

  const recordDir = path.join(outputDir, `record-${pid}`);
  if (!fs.existsSync(recordDir)) {
    fs.mkdirSync(recordDir, { recursive: true });
  }

  const agentArgs = `${recordDir}|${pid}`;

  if (!fs.existsSync(agentLoaderJar) || !fs.existsSync(marsJavaAgentJar)) {
    const missing = [
      !fs.existsSync(agentLoaderJar) && agentLoaderJar,
      !fs.existsSync(marsJavaAgentJar) && marsJavaAgentJar,
    ].filter(Boolean);
    writeLog(`[startRecordAgent] JARs missing pid=${pid} missing=${JSON.stringify(missing)}`);
    return {
      success: false,
      error: `Agent JARs not found. Build: cd java && mvn package. Missing: ${missing.join(', ')}`,
    };
  }

  const tempAgentJar = path.join(os.tmpdir(), `marsJavaAgent-record-${pid}-${Date.now()}.jar`);
  try {
    fs.copyFileSync(marsJavaAgentJar, tempAgentJar);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    writeLog(`[startRecordAgent] copy JAR failed pid=${pid} error=${msg}`);
    return { success: false, error: `Failed to copy agent JAR: ${msg}` };
  }

  writeLog(`[startRecordAgent] spawn pid=${pid} recordDir=${recordDir}`);
  const loadResult = await new Promise<{ ok: boolean; stderr?: string; spawnError?: string }>((resolve) => {
    const javaExe = getJavaExecutable();
    const proc = spawn(
      javaExe,
      ['-jar', agentLoaderJar, String(pid), tempAgentJar, agentArgs],
      { stdio: ['pipe', 'pipe', 'pipe'] }
    );
    let stderr = '';
    proc.stderr?.on('data', (d) => (stderr += d.toString()));
    proc.on('close', (code) => {
      try {
        if (fs.existsSync(tempAgentJar)) fs.unlinkSync(tempAgentJar);
      } catch {
        // ignore
      }
      if (code === 0) {
        writeLog(`[startRecordAgent] loader exit 0 pid=${pid}`);
        resolve({ ok: true });
      } else {
        const errTrim = stderr.trim().slice(0, 500);
        writeLog(`[startRecordAgent] loader exit pid=${pid} code=${code} stderr=${errTrim}`);
        resolve({ ok: false, stderr: errTrim || `Exit code ${code}` });
      }
    });
    proc.on('error', (err) => {
      writeLog(`[startRecordAgent] spawn error pid=${pid} message=${err.message}`);
      resolve({ ok: false, spawnError: err.message });
    });
  });

  if (!loadResult.ok) {
    const detail = loadResult.stderr ?? loadResult.spawnError ?? 'unknown';
    return {
      success: false,
      error: `Agent loader failed: ${detail}. Check ${LOG_FILE} for details.`,
    };
  }

  const infoPath = path.join(recordDir, MARS_AGENT_INFO_FILE);
  const deadline = Date.now() + INFO_FILE_TIMEOUT_MS;
  let port: number | undefined;
  while (Date.now() < deadline) {
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
    if (fs.existsSync(infoPath)) {
      try {
        const raw = fs.readFileSync(infoPath, 'utf-8');
        const info = JSON.parse(raw) as { port?: number; pid?: number };
        if (typeof info.port === 'number') {
          port = info.port;
          break;
        }
      } catch {
        // retry
      }
    }
  }

  if (port == null) {
    writeLog(`[startRecordAgent] timeout waiting for ${MARS_AGENT_INFO_FILE}`);
    return { success: false, error: 'Agent did not write marsJavaAgentInfo.json in time.' };
  }

  writeLog(`[startRecordAgent] connecting to ws://127.0.0.1:${port}`);
  return new Promise((resolve) => {
    const ws = new WebSocket(`ws://127.0.0.1:${port}`);
    let handshakeDone = false;
    let resolved = false;

    const finish = (result: RecordAgentResult) => {
      if (resolved) return;
      resolved = true;
      resolve(result);
    };

    const handshakeTimeout = setTimeout(() => {
      if (!handshakeDone) {
        writeLog(`[startRecordAgent] handshake timeout`);
        ws.close();
        finish({ success: false, error: 'Handshake timeout.' });
      }
    }, 8000);

    const stop = () => {
      try {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify({ type: 'stopRecordAndReplay' }));
        }
        ws.close();
      } catch (e) {
        writeLog(`[startRecordAgent] stop error: ${String(e)}`);
      }
    };

    const send = (msg: Record<string, unknown>) => {
      try {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify(msg));
        }
      } catch (e) {
        writeLog(`[startRecordAgent] send error: ${String(e)}`);
      }
    };

    ws.on('open', () => {
      ws.send(JSON.stringify({ type: 'handshake', pid }));
    });

    ws.on('message', (data: Buffer | ArrayBuffer | Buffer[]) => {
      const text = (typeof data === 'string' ? data : data.toString()) as string;
      try {
        const msg = JSON.parse(text) as Record<string, unknown>;
        if (!handshakeDone) {
          if (msg.type === 'handshake_ack') {
            clearTimeout(handshakeTimeout);
            handshakeDone = true;
            ws.send(JSON.stringify({ type: 'startRecordAndReplay' }));
            finish({ success: true, stop, send });
          }
          return;
        }
        if (msg.event === 'click' || msg.event === 'clickButton' || msg.event === 'focusLost' || msg.event === 'componentProperties' || msg.event === 'fillEdit' || msg.event === 'pressKey' || msg.event === 'keyChordAction' || msg.event === 'textInputAction' || msg.event === 'rawKeyEventAction' || msg.event === 'selectDropDown' || msg.event === 'selectDropList' || msg.event === 'selectMenuItem' || msg.event === 'selectMenuIcon' || msg.event === 'selectTreeList' || msg.event === 'expandTreeNode' || msg.event === 'collapseTreeNode' || msg.event === 'searchAndUpdate' || msg.event === 'searchAndClick' || msg.event === 'selectPopupMenu') {
          onEvent(msg);
        }
      } catch (e) {
        writeLog(`[startRecordAgent] message parse error: ${String(e)}`);
      }
    });

    ws.on('error', (err) => {
      if (!resolved) {
        clearTimeout(handshakeTimeout);
        finish({ success: false, error: err.message });
      }
    });

    ws.on('close', () => {
      // connection closed by agent or network
    });
  });
}

/** Replay recorded steps: inject record-agent, connect, send replay command, wait for completion. */
export async function replaySteps(
  pid: number,
  outputDir: string,
  steps: Record<string, unknown>[]
): Promise<{ success: boolean; count?: number; error?: string; failedIndex?: number }> {
  const extensionPath = path.join(__dirname, '..');
  const agentLoaderJar = path.join(
    extensionPath,
    'java',
    'agent-loader',
    'target',
    'agent-loader-1.0.jar'
  );
  const marsJavaAgentJar = path.join(
    extensionPath,
    'java',
    'marsJavaAgent',
    'target',
    'marsJavaAgent-1.0.jar'
  );

  const recordDir = path.join(outputDir, `record-${pid}`);
  if (!fs.existsSync(recordDir)) {
    fs.mkdirSync(recordDir, { recursive: true });
  }

  const agentArgs = `${recordDir}|${pid}`;

  if (!fs.existsSync(agentLoaderJar) || !fs.existsSync(marsJavaAgentJar)) {
    const missing = [
      !fs.existsSync(agentLoaderJar) && agentLoaderJar,
      !fs.existsSync(marsJavaAgentJar) && marsJavaAgentJar,
    ].filter(Boolean);
    writeLog(`[replaySteps] JARs missing pid=${pid} missing=${JSON.stringify(missing)}`);
    return {
      success: false,
      error: `Agent JARs not found. Build: cd java && mvn package. Missing: ${missing.join(', ')}`,
    };
  }

  const tempAgentJar = path.join(os.tmpdir(), `marsJavaAgent-replay-${pid}-${Date.now()}.jar`);
  try {
    fs.copyFileSync(marsJavaAgentJar, tempAgentJar);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    writeLog(`[replaySteps] copy JAR failed pid=${pid} error=${msg}`);
    return { success: false, error: `Failed to copy agent JAR: ${msg}` };
  }

  writeLog(`[replaySteps] spawn pid=${pid} recordDir=${recordDir} steps=${steps.length}`);
  const loadResult = await new Promise<{ ok: boolean; stderr?: string; spawnError?: string }>((resolve) => {
    const javaExe = getJavaExecutable();
    const proc = spawn(
      javaExe,
      ['-jar', agentLoaderJar, String(pid), tempAgentJar, agentArgs],
      { stdio: ['pipe', 'pipe', 'pipe'] }
    );
    let stderr = '';
    proc.stderr?.on('data', (d) => (stderr += d.toString()));
    proc.on('close', (code) => {
      try {
        if (fs.existsSync(tempAgentJar)) fs.unlinkSync(tempAgentJar);
      } catch {
        // ignore
      }
      if (code === 0) {
        writeLog(`[replaySteps] loader exit 0 pid=${pid}`);
        resolve({ ok: true });
      } else {
        const errTrim = stderr.trim().slice(0, 500);
        writeLog(`[replaySteps] loader exit pid=${pid} code=${code} stderr=${errTrim}`);
        resolve({ ok: false, stderr: errTrim || `Exit code ${code}` });
      }
    });
    proc.on('error', (err) => {
      writeLog(`[replaySteps] spawn error pid=${pid} message=${err.message}`);
      resolve({ ok: false, spawnError: err.message });
    });
  });

  if (!loadResult.ok) {
    const detail = loadResult.stderr ?? loadResult.spawnError ?? 'unknown';
    return {
      success: false,
      error: `Agent loader failed: ${detail}. Check ${LOG_FILE} for details.`,
    };
  }

  const infoPath = path.join(recordDir, MARS_AGENT_INFO_FILE);
  const deadline = Date.now() + INFO_FILE_TIMEOUT_MS;
  let port: number | undefined;
  while (Date.now() < deadline) {
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
    if (fs.existsSync(infoPath)) {
      try {
        const raw = fs.readFileSync(infoPath, 'utf-8');
        const info = JSON.parse(raw) as { port?: number; pid?: number };
        if (typeof info.port === 'number') {
          port = info.port;
          break;
        }
      } catch {
        // retry
      }
    }
  }

  if (port == null) {
    writeLog(`[replaySteps] timeout waiting for ${MARS_AGENT_INFO_FILE}`);
    return { success: false, error: 'Agent did not write marsJavaAgentInfo.json in time.' };
  }

  writeLog(`[replaySteps] connecting to ws://127.0.0.1:${port}`);
  return new Promise((resolve) => {
    const ws = new WebSocket(`ws://127.0.0.1:${port}`);
    let handshakeDone = false;
    let resolved = false;

    const finish = (result: { success: boolean; count?: number; error?: string; failedIndex?: number }) => {
      if (resolved) return;
      resolved = true;
      try {
        ws.close();
      } catch {
        // ignore
      }
      resolve(result);
    };

    const handshakeTimeout = setTimeout(() => {
      if (!handshakeDone) {
        writeLog(`[replaySteps] handshake timeout`);
        finish({ success: false, error: 'Handshake timeout.' });
      }
    }, 8000);

    const replayTimeout = setTimeout(() => {
      if (!resolved) {
        writeLog(`[replaySteps] replay timeout`);
        finish({ success: false, error: 'Replay timeout.' });
      }
    }, 60000);

    ws.on('open', () => {
      ws.send(JSON.stringify({ type: 'handshake', pid }));
    });

    ws.on('message', (data: Buffer | ArrayBuffer | Buffer[]) => {
      const text = (typeof data === 'string' ? data : data.toString()) as string;
      try {
        const msg = JSON.parse(text) as Record<string, unknown>;
        if (!handshakeDone) {
          if (msg.type === 'handshake_ack') {
            clearTimeout(handshakeTimeout);
            handshakeDone = true;
            ws.send(JSON.stringify({ type: 'replay', steps }));
            return;
          }
          return;
        }
        if (msg.type === 'replayDone') {
          clearTimeout(replayTimeout);
          const err = msg.error as string | undefined;
          const count = typeof msg.count === 'number' ? msg.count : steps.length;
          const failedIndex = typeof msg.failedIndex === 'number' ? msg.failedIndex : undefined;
          finish(err ? { success: false, error: err, count, failedIndex } : { success: true, count });
        }
      } catch (e) {
        writeLog(`[replaySteps] message parse error: ${String(e)}`);
      }
    });

    ws.on('error', (err) => {
      if (!resolved) {
        clearTimeout(handshakeTimeout);
        clearTimeout(replayTimeout);
        finish({ success: false, error: err.message });
      }
    });

    ws.on('close', () => {
      // connection closed
    });
  });
}

/** Write stop file so the record agent in the target JVM stops and flushes. */
export function stopRecordAgent(pid: number, outputDir: string): void {
  const recordDir = path.join(outputDir, `record-${pid}`);
  const stopFile = path.join(recordDir, 'record-stop.txt');
  try {
    fs.writeFileSync(stopFile, 'stop', 'utf-8');
    writeLog(`[stopRecordAgent] wrote stop file pid=${pid} path=${stopFile}`);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    writeLog(`[stopRecordAgent] error pid=${pid} message=${msg}`);
    throw e;
  }
}

/** Run highlight via ProcessInfo -highlight x y w h (borderless topmost form, flash 3x, close). */
export async function runHighlightOverlay(
  extensionPath: string,
  x: number,
  y: number,
  w: number,
  h: number
): Promise<HighlightResult> {
  writeLog(`[runHighlightOverlay] entry extensionPath=${extensionPath} x=${x} y=${y} w=${w} h=${h}`);
  if (os.platform() !== 'win32') {
    writeLog('[runHighlightOverlay] branch: non-Windows, return error');
    return { success: false, error: 'Highlight is supported on Windows only.' };
  }
  const { findProcessInfoExe } = await import('./processInfo');
  const exe = findProcessInfoExe(extensionPath);
  if (!exe) {
    writeLog(`[runHighlightOverlay] branch: ProcessInfo exe not found, extensionPath=${extensionPath}`);
    return {
      success: false,
      error: 'ProcessInfo not found. Run: cd ProcessInfo && dotnet publish -c Release',
    };
  }
  const xr = Math.round(x);
  const yr = Math.round(y);
  const wr = Math.round(w);
  const hr = Math.round(h);
  const args = ['-highlight', String(xr), String(yr), String(wr), String(hr)];
  writeLog(`[runHighlightOverlay] spawn exe=${exe} args=${JSON.stringify(args)}`);
  return new Promise((resolve) => {
    const proc = spawn(exe, args, {
      stdio: 'ignore',
      cwd: path.dirname(exe),
      windowsHide: true,
    });
    proc.on('close', (code) => {
      if (code === 0) {
        writeLog(`[runHighlightOverlay] close: success code=${code}`);
        resolve({ success: true });
      } else {
        writeLog(`[runHighlightOverlay] close: failure code=${code}`);
        resolve({
          success: false,
          error: code !== null ? `Exit code ${code}` : 'Process exited',
        });
      }
    });
    proc.on('error', (err) => {
      writeLog(`[runHighlightOverlay] spawn error: ${err.message}`);
      resolve({ success: false, error: err.message });
    });
  });
}
