/**
 * Invokes ProcessInfo tool to list running Java processes
 */

import * as path from 'path';
import * as fs from 'fs';
import { spawn } from 'child_process';
import { WebSocketServer, WebSocket, RawData } from 'ws';
import { JavaProcess } from './types';

const isWindows = process.platform === 'win32';

export type ProgressEvent = { kind: 'checking'; pid: number; name: string } | { kind: 'found'; pid: number; display: string } | { kind: 'skip'; pid: number };

/** Resolve ProcessInfo.exe path (used for process list and for -highlight). */
export function findProcessInfoExe(extensionPath: string): string | null {
  const base = path.join(extensionPath, 'ProcessInfo', 'bin');
  const configs = ['Release', 'Debug'];
  const frameworks = ['net8.0', 'net8.0-windows', 'net9.0', 'net7.0'];
  const ext = isWindows ? 'ProcessInfo.exe' : 'ProcessInfo';
  let bestPath: string | null = null;
  let bestMtime = -1;
  for (const cfg of configs) {
    for (const fx of frameworks) {
      const exe = path.join(base, cfg, fx, ext);
      if (!fs.existsSync(exe)) continue;
      try {
        const stat = fs.statSync(exe);
        const mt = stat.mtimeMs || 0;
        if (mt > bestMtime) {
          bestMtime = mt;
          bestPath = exe;
        }
      } catch {
        // ignore and continue
      }
    }
  }
  return bestPath;
}

function getLatestProcessesCachePath(): string {
  const extensionPath = path.join(__dirname, '..');
  const dir = path.join(extensionPath, 'scanedfiles');
  try {
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  } catch {
    // ignore
  }
  return path.join(dir, 'processes-latest.json');
}

function readLatestProcessesCache(): JavaProcess[] {
  try {
    const p = getLatestProcessesCachePath();
    if (!fs.existsSync(p)) return [];
    const raw = fs.readFileSync(p, 'utf-8');
    const parsed = JSON.parse(raw) as { processes?: JavaProcess[] };
    const list = Array.isArray(parsed?.processes) ? parsed.processes : [];
    return list.filter((x) => x && Number.isFinite(Number(x.pid)) && Number(x.pid) > 0)
      .map((x) => ({
        pid: Number(x.pid),
        displayName: String(x.displayName ?? x.mainClass ?? `PID ${x.pid}`),
        mainClass: x.mainClass,
        source: typeof x.source === 'string' ? x.source : undefined,
      }));
  } catch {
    return [];
  }
}

function writeLatestProcessesCache(processes: JavaProcess[]): void {
  try {
    const p = getLatestProcessesCachePath();
    fs.writeFileSync(p, JSON.stringify({
      generatedAt: new Date().toISOString(),
      count: processes.length,
      processes,
    }, null, 2), 'utf-8');
  } catch {
    // ignore
  }
}

export async function createWebSocketServer(): Promise<{ wss: WebSocketServer; port: number }> {
  const startPort = 10000;
  const endPort = 11000;

  for (let port = startPort; port <= endPort; port++) {
    try {
      const wss = await new Promise<WebSocketServer>((resolve, reject) => {
        const server = new WebSocketServer({ port, host: '127.0.0.1' });
        const onError = (err: NodeJS.ErrnoException) => {
          server.removeAllListeners();
          try { server.close(); } catch { /* ignore */ }
          reject(err);
        };
        server.once('listening', () => {
          server.off('error', onError);
          resolve(server);
        });
        server.once('error', onError);
      });
      return { wss, port };
    } catch (err) {
      const code = (err as NodeJS.ErrnoException).code;
      if (code !== 'EADDRINUSE') throw err;
    }
  }

  throw new Error('No available port in range 10000-11000 for WebSocket server');
}

export async function getJavaProcesses(onProgress?: (e: ProgressEvent) => void): Promise<JavaProcess[]> {
  const extensionPath = path.join(__dirname, '..');
  const processInfoExe = findProcessInfoExe(extensionPath);

  if (!processInfoExe) {
    const cached = readLatestProcessesCache();
    if (cached.length > 0) return cached;
    const base = path.join(path.join(__dirname, '..'), 'ProcessInfo', 'bin');
    throw new Error(`ProcessInfo not found at ${base}. Run: cd ProcessInfo && dotnet publish -c Release`);
  }

  const { wss, port } = await createWebSocketServer();
  const acquireId = `${Date.now()}-${Math.floor(Math.random() * 1000)}`;
  const command = {
    command: 'scan_java_process',
    version: '1.0',
    acquireId,
    datetime: new Date().toISOString(),
  };

  return new Promise((resolve, reject) => {
    let resolved = false;
    let proc: ReturnType<typeof spawn> | undefined;

    const cleanup = (err?: Error) => {
      if (resolved) return;
      resolved = true;
      try { wss.close(); } catch { /* ignore */ }
      try { proc?.kill(); } catch { /* ignore */ }
      if (err) reject(err);
    };

    const successCleanup = (processes: JavaProcess[]) => {
      if (resolved) return;
      resolved = true;
      clearTimeout(timeout);
      try { wss.close(); } catch { /* ignore */ }
      try { proc?.kill(); } catch { /* ignore */ }
      resolve(processes);
    };

    const timeout = setTimeout(() => {
      cleanup(new Error('WebSocket response timeout from ProcessInfo'));
    }, 15000);

    wss.on('connection', (ws: WebSocket) => {
      ws.send(JSON.stringify(command));

      ws.on('message', (data: RawData) => {
        try {
          const text = data.toString();
          console.error('[ProcessInfo] WS message received, length=', text.length, 'raw=', text.substring(0, 500));
          const payload = JSON.parse(text) as {
            acquireId?: string;
            javaProcess?: { pid: number; displayName?: string; mainClass?: string; source?: string }[];
          };
          const list = Array.isArray(payload.javaProcess) ? payload.javaProcess : [];
          const processes: JavaProcess[] = list.map((p) => ({
            pid: Number(p.pid),
            displayName: p.displayName ?? p.mainClass ?? `PID ${p.pid}`,
            mainClass: p.mainClass,
            source: p.source,
          }));
          writeLatestProcessesCache(processes);
          successCleanup(processes);
        } catch (err) {
          clearTimeout(timeout);
          cleanup(new Error(`Invalid JSON from ProcessInfo WS: ${String(err)}`));
        }
      });

      ws.on('error', (err: Error) => {
        clearTimeout(timeout);
        cleanup(new Error(`WebSocket error: ${String(err)}`));
      });
    });

    wss.on('error', (err: Error) => {
      clearTimeout(timeout);
      cleanup(new Error(`WebSocket server error: ${String(err)}`));
    });

    proc = spawn(processInfoExe, ['--ws', String(port)], {
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    proc.on('error', (err) => {
      clearTimeout(timeout);
      const cached = readLatestProcessesCache();
      if (cached.length > 0) {
        resolved = true;
        try { wss.close(); } catch { /* ignore */ }
        try { proc?.kill(); } catch { /* ignore */ }
        resolve(cached);
        return;
      }
      cleanup(new Error(`Failed to start ProcessInfo: ${err.message}`));
    });

    proc.stderr?.on('data', (d) => {
      const msg = d.toString();
      if (onProgress) {
        const lines = msg.split(/\r?\n/);
        for (const line of lines) {
          if (line.startsWith('CHECKING:')) {
            const rest = line.slice(9);
            const idx = rest.indexOf(':');
            const pid = parseInt(rest.slice(0, idx), 10);
            const name = idx >= 0 ? rest.slice(idx + 1) : rest;
            onProgress({ kind: 'checking', pid, name });
          } else if (line.startsWith('FOUND:')) {
            const rest = line.slice(6);
            const idx = rest.indexOf('\t');
            const pid = parseInt(idx >= 0 ? rest.slice(0, idx) : rest, 10);
            const display = idx >= 0 ? rest.slice(idx + 1) : '';
            onProgress({ kind: 'found', pid, display });
          } else if (line.startsWith('SKIP:')) {
            const pid = parseInt(line.slice(5), 10);
            onProgress({ kind: 'skip', pid });
          }
        }
      }
    });

    proc.on('close', (code) => {
      if (!resolved && code !== 0) {
        clearTimeout(timeout);
        const cached = readLatestProcessesCache();
        if (cached.length > 0) {
          resolved = true;
          try { wss.close(); } catch { /* ignore */ }
          resolve(cached);
          return;
        }
        cleanup(new Error(`ProcessInfo exited with code ${code}`));
      }
    });
  });
}
