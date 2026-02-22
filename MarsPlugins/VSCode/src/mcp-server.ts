import { getJavaProcesses } from './processInfo';
import * as http from 'http';
import * as fs from 'fs';
import * as path from 'path';

type JsonRpcId = number | string | null;

type JsonRpcRequest = {
  jsonrpc: '2.0';
  id?: JsonRpcId;
  method: string;
  params?: any;
};

type JsonRpcResponse = {
  jsonrpc: '2.0';
  id: JsonRpcId;
  result?: unknown;
  error?: { code: number; message: string; data?: unknown };
};

let buffer = Buffer.alloc(0);
let initialized = false;

const tracePath = process.env.MARS_MCP_TRACE?.trim() || path.join(process.env.MARS_WORKSPACE || process.cwd(), 'scanedfiles', 'mcp-server.trace.log');

function trace(message: string): void {
  try {
    const line = `${new Date().toISOString()} ${message}\n`;
    fs.mkdirSync(path.dirname(tracePath), { recursive: true });
    fs.appendFileSync(tracePath, line, 'utf8');
  } catch {
    // ignore trace errors
  }
}

console.log = (...args: unknown[]) => {
  console.error(...args);
  trace(`[redirected-stdout] ${args.map((x) => String(x)).join(' ')}`);
};

const MCP_TOOLS = [
  { name: 'mars-list-processes', routeTool: 'mars.listProcesses', description: 'List running Java processes' },
  { name: 'mars-select-process', routeTool: 'mars.selectProcess', description: 'Select target Java process by pid' },
  { name: 'mars-start-record', routeTool: 'mars.startRecord', description: 'Start recording on selected process or specified pid' },
  { name: 'mars-stop-record', routeTool: 'mars.stopRecord', description: 'Stop current recording and save steps' },
  { name: 'mars-get-object-tree', routeTool: 'mars.getObjectTree', description: 'Get object tree from selected process' },
  { name: 'mars-highlight-object', routeTool: 'mars.highlightObject', description: 'Highlight object on screen' },
  { name: 'mars-get-steps', routeTool: 'mars.getSteps', description: 'Get current test steps' },
  { name: 'mars-update-step', routeTool: 'mars.updateStep', description: 'Update one test step by index' },
  { name: 'mars-execute-step', routeTool: 'mars.executeStep', description: 'Execute one test step by index' },
  { name: 'mars-run-replay', routeTool: 'mars.runReplay', description: 'Replay a range of test steps' },
  { name: 'mars-export-objects', routeTool: 'mars.exportObjects', description: 'Export scanned objects' },
  { name: 'mars-export-diagnostics', routeTool: 'mars.exportDiagnostics', description: 'Export diagnostics bundle' },
  { name: 'mars-get-last-errors', routeTool: 'mars.getLastErrors', description: 'Get latest error items' },
] as const;

const TOOL_ALIAS_TO_NAME: Record<string, string> = {
  'mars.listProcesses': 'mars-list-processes',
  'mars_list_processes': 'mars-list-processes',
  'mars.selectProcess': 'mars-select-process',
  'mars_select_process': 'mars-select-process',
  'mars.startRecord': 'mars-start-record',
  'mars_start_record': 'mars-start-record',
  'mars.stopRecord': 'mars-stop-record',
  'mars_stop_record': 'mars-stop-record',
  'mars.getObjectTree': 'mars-get-object-tree',
  'mars_get_object_tree': 'mars-get-object-tree',
  'mars.highlightObject': 'mars-highlight-object',
  'mars_highlight_object': 'mars-highlight-object',
  'mars.getSteps': 'mars-get-steps',
  'mars_get_steps': 'mars-get-steps',
  'mars.updateStep': 'mars-update-step',
  'mars_update_step': 'mars-update-step',
  'mars.executeStep': 'mars-execute-step',
  'mars_execute_step': 'mars-execute-step',
  'mars.runReplay': 'mars-run-replay',
  'mars_run_replay': 'mars-run-replay',
  'mars.exportObjects': 'mars-export-objects',
  'mars_export_objects': 'mars-export-objects',
  'mars.exportDiagnostics': 'mars-export-diagnostics',
  'mars_export_diagnostics': 'mars-export-diagnostics',
  'mars.getLastErrors': 'mars-get-last-errors',
  'mars_get_last_errors': 'mars-get-last-errors',
};

function resolveToolDef(name: unknown) {
  if (typeof name !== 'string' || !name) return undefined;
  const canonical = TOOL_ALIAS_TO_NAME[name] ?? name;
  return MCP_TOOLS.find((t) => t.name === canonical);
}

function getBridgeConfig(): { port?: number; token?: string } {
  const port = Number(process.env.MARS_MCP_BRIDGE_PORT ?? '');
  const token = process.env.MARS_MCP_BRIDGE_TOKEN;
  return {
    port: Number.isInteger(port) && port > 0 ? port : undefined,
    token: token && token.length > 0 ? token : undefined,
  };
}

function callBridge(tool: string, input: unknown, requestId: string): Promise<unknown> {
  return new Promise((resolve, reject) => {
    const cfg = getBridgeConfig();
    if (!cfg.port || !cfg.token) {
      reject(new Error('MCP bridge is unavailable. Ensure extension host started this server.'));
      return;
    }

    const body = Buffer.from(JSON.stringify({ tool, input, requestId }), 'utf8');
    const req = http.request(
      {
        hostname: '127.0.0.1',
        port: cfg.port,
        path: '/tool',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': body.length,
          'x-mars-token': cfg.token,
        },
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk) => {
          chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
        });
        res.on('end', () => {
          try {
            const text = Buffer.concat(chunks).toString('utf8') || 'null';
            resolve(JSON.parse(text));
          } catch (e) {
            reject(new Error(`Invalid bridge response JSON: ${String(e)}`));
          }
        });
      }
    );

    req.on('error', (e) => {
      reject(new Error(`Bridge call failed: ${String(e)}`));
    });

    req.write(body);
    req.end();
  });
}

function writeMessage(payload: unknown): void {
  process.stdout.write(`${JSON.stringify(payload)}\n`);
}

function sendResult(id: JsonRpcId, result: unknown): void {
  const response: JsonRpcResponse = { jsonrpc: '2.0', id, result };
  trace(`[tx-result] id=${String(id)}`);
  writeMessage(response);
}

function sendError(id: JsonRpcId, code: number, message: string, data?: unknown): void {
  const response: JsonRpcResponse = {
    jsonrpc: '2.0',
    id,
    error: { code, message, data },
  };
  trace(`[tx-error] id=${String(id)} code=${code} message=${message}`);
  writeMessage(response);
}

function parseHeaderAndBodyLength(source: Buffer): { headerEnd: number; separatorLength: number; contentLength: number } | null {
  const crlfHeaderEnd = source.indexOf('\r\n\r\n');
  const lfHeaderEnd = source.indexOf('\n\n');

  let headerEnd = -1;
  let separatorLength = 0;

  if (crlfHeaderEnd >= 0 && (lfHeaderEnd < 0 || crlfHeaderEnd <= lfHeaderEnd)) {
    headerEnd = crlfHeaderEnd;
    separatorLength = 4;
  } else if (lfHeaderEnd >= 0) {
    headerEnd = lfHeaderEnd;
    separatorLength = 2;
  }

  if (headerEnd < 0) return null;

  const headerText = source.subarray(0, headerEnd).toString('utf8');
  const lines = headerText.split(/\r?\n/);
  const contentLengthLine = lines.find((line) => line.toLowerCase().startsWith('content-length:'));
  if (!contentLengthLine) return null;

  const lenText = contentLengthLine.split(':')[1]?.trim() ?? '';
  const contentLength = Number(lenText);
  if (!Number.isInteger(contentLength) || contentLength < 0) return null;

  return { headerEnd, separatorLength, contentLength };
}

async function handleRequest(req: JsonRpcRequest): Promise<void> {
  const id = req.id ?? null;
  const method = req.method;
  trace(`[rx] method=${method} id=${String(id)}`);

  try {
    if (method === 'initialize') {
      initialized = true;
      sendResult(id, {
        protocolVersion: '2024-11-05',
        serverInfo: {
          name: 'mars-local',
          version: '0.1.0',
        },
        capabilities: {
          tools: {},
        },
      });
      return;
    }

    if (method === 'notifications/initialized') {
      return;
    }

    if (!initialized) {
      sendError(id, -32002, 'Server not initialized');
      return;
    }

    if (method === 'tools/list') {
      sendResult(id, {
        tools: MCP_TOOLS.map((tool) => ({
          name: tool.name,
          description: tool.description,
          inputSchema: {
            type: 'object',
            properties: {},
            additionalProperties: true,
          },
        })),
      });
      return;
    }

    if (method === 'tools/call') {
      const name = req.params?.name;
      const toolDef = resolveToolDef(name);
      if (!toolDef) {
        sendError(id, -32602, `Unsupported tool: ${String(name)}`);
        return;
      }

      const argumentsObj = req.params?.arguments ?? {};
      const requestId = `mcp-${Date.now()}`;
      const routedTool = toolDef.routeTool;

      if (routedTool === 'mars.listProcesses') {
        const bridge = getBridgeConfig();
        if (!bridge.port || !bridge.token) {
          const items = await getJavaProcesses();
          sendResult(id, {
            content: [
              {
                type: 'text',
                text: JSON.stringify(
                  {
                    ok: true,
                    requestId,
                    errorCode: null,
                    errorMessage: null,
                    data: { items },
                  },
                  null,
                  2
                ),
              },
            ],
          });
          return;
        }
      }

      const routed = await callBridge(routedTool, argumentsObj, requestId);
      sendResult(id, {
        content: [
          {
            type: 'text',
            text: JSON.stringify(routed, null, 2),
          },
        ],
      });
      return;
    }

    sendError(id, -32601, `Method not found: ${method}`);
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    sendError(id, -32000, message);
  }
}

function processBuffer(): void {
  while (true) {
    const parsed = parseHeaderAndBodyLength(buffer);
    if (!parsed) {
      const lineBreak = buffer.indexOf('\n');
      if (lineBreak >= 0) {
        const line = buffer.subarray(0, lineBreak).toString('utf8').trim();
        buffer = buffer.subarray(lineBreak + 1);
        if (line.startsWith('{') && line.endsWith('}')) {
          try {
            const parsedBody = JSON.parse(line) as JsonRpcRequest;
            void handleRequest(parsedBody);
          } catch {
            // keep consuming until a valid JSON line appears
          }
        }
        continue;
      }
      return;
    }

    const { headerEnd, separatorLength, contentLength } = parsed;
    const messageStart = headerEnd + separatorLength;
    const messageEnd = messageStart + contentLength;
    if (buffer.length < messageEnd) return;

    const body = buffer.subarray(messageStart, messageEnd).toString('utf8');
    buffer = buffer.subarray(messageEnd);

    let parsedBody: JsonRpcRequest;
    try {
      parsedBody = JSON.parse(body) as JsonRpcRequest;
    } catch {
      continue;
    }

    void handleRequest(parsedBody);
  }
}

process.stdin.on('data', (chunk: Buffer) => {
  trace(`[stdin-data] bytes=${chunk.length}`);
  buffer = Buffer.concat([buffer, chunk]);
  processBuffer();
});

process.stdin.resume();

process.stdin.on('error', () => {
  trace('[stdin-error]');
  process.exit(1);
});

process.on('uncaughtException', (err) => {
  trace(`[uncaughtException] ${String(err)}`);
  console.error('[mcp-server] uncaughtException', err);
});

process.on('unhandledRejection', (reason) => {
  trace(`[unhandledRejection] ${String(reason)}`);
  console.error('[mcp-server] unhandledRejection', reason);
});

trace('[startup] mcp-server started');
