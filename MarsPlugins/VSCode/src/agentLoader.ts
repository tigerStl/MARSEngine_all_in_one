/**
 * Spawns Java Agent Loader to attach UI Scanner / Highlight agent to target JVM process
 */

import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import { spawn } from 'child_process';

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
  const scannerAgentJar = path.join(
    extensionPath,
    'java',
    'ui-scanner-agent',
    'target',
    'ui-scanner-agent-1.0.jar'
  );

  if (!fs.existsSync(agentLoaderJar) || !fs.existsSync(scannerAgentJar)) {
    const missing = [
      !fs.existsSync(agentLoaderJar) && agentLoaderJar,
      !fs.existsSync(scannerAgentJar) && scannerAgentJar,
    ].filter(Boolean);
    return {
      success: false,
      error: `Agent JARs not found. Build Java projects first.\n  Run: cd java && mvn package\n  Missing: ${missing.join(', ')}`,
    };
  }

  const outputPath = path.join(outputDir, `ui-scan-${pid}-${Date.now()}.json`);

  // Copy scanner JAR to a temp path so each load uses a different path (avoids "agent already loaded" when re-scanning same process)
  const tempScannerJar = path.join(
    os.tmpdir(),
    `ui-scanner-agent-${pid}-${Date.now()}.jar`
  );
  try {
    fs.copyFileSync(scannerAgentJar, tempScannerJar);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { success: false, error: `Failed to copy agent JAR: ${msg}` };
  }

  return new Promise((resolve) => {
    const javaExe = getJavaExecutable();
    const proc = spawn(
      javaExe,
      [
        '-jar',
        agentLoaderJar,
        String(pid),
        tempScannerJar,
        outputPath,
      ],
      {
        stdio: ['pipe', 'pipe', 'pipe'],
      }
    );

    let stderr = '';
    proc.stderr?.on('data', (d) => (stderr += d.toString()));
    proc.on('close', (code) => {
      try {
        if (fs.existsSync(tempScannerJar)) fs.unlinkSync(tempScannerJar);
      } catch {
        // ignore cleanup error
      }
      if (code === 0 && fs.existsSync(outputPath)) {
        resolve({ success: true, outputPath });
      } else {
        resolve({
          success: false,
          error: stderr || `Process exited with code ${code}`,
        });
      }
    });
  });
}

/** Run C# HighlightOverlay.exe with screen coordinates (x,y,w,h). Absolute/screen position. */
export async function runHighlightOverlay(
  extensionPath: string,
  x: number,
  y: number,
  w: number,
  h: number
): Promise<HighlightResult> {
  if (os.platform() !== 'win32') {
    return { success: false, error: 'Highlight overlay is supported on Windows only.' };
  }
  const base = path.join(extensionPath, 'HighlightOverlay');
  const published = path.join(base, 'bin', 'Release', 'net8.0-windows', 'publish', 'HighlightOverlay.exe');
  const debug = path.join(base, 'bin', 'Debug', 'net8.0-windows', 'HighlightOverlay.exe');
  const exe = fs.existsSync(published) ? published : (fs.existsSync(debug) ? debug : null);
  if (!exe) {
    return {
      success: false,
      error: 'HighlightOverlay.exe not found. Run: cd HighlightOverlay && dotnet publish -c Release',
    };
  }
  const args = [String(Math.round(x)), String(Math.round(y)), String(Math.round(w)), String(Math.round(h))];
  return new Promise((resolve) => {
    const proc = spawn(exe, args, { stdio: ['pipe', 'pipe', 'pipe'] });
    let stderr = '';
    proc.stderr?.on('data', (d) => (stderr += d.toString()));
    proc.on('close', (code) => {
      resolve(
        code === 0
          ? { success: true }
          : { success: false, error: stderr || `Exit code ${code}` }
      );
    });
  });
}
