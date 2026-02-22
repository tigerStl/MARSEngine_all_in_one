const { spawn } = require('child_process');
const path = require('path');

const workspace = process.cwd();
const serverPath = path.join(workspace, 'out', 'mcp-server.js');

function encodeMessage(obj) {
  return Buffer.from(`${JSON.stringify(obj)}\n`, 'utf8');
}

function createParser(onMessage) {
  let buffer = Buffer.alloc(0);
  return (chunk) => {
    buffer = Buffer.concat([buffer, chunk]);
    while (true) {
      const headerEnd = buffer.indexOf('\r\n\r\n');
      if (headerEnd >= 0) {
        const headerText = buffer.subarray(0, headerEnd).toString('utf8');
        const lenLine = headerText.split('\r\n').find((line) => line.toLowerCase().startsWith('content-length:'));
        if (!lenLine) return;
        const len = Number((lenLine.split(':')[1] || '').trim());
        if (!Number.isInteger(len) || len < 0) return;
        const messageStart = headerEnd + 4;
        const messageEnd = messageStart + len;
        if (buffer.length < messageEnd) return;
        const bodyText = buffer.subarray(messageStart, messageEnd).toString('utf8');
        buffer = buffer.subarray(messageEnd);
        try {
          onMessage(JSON.parse(bodyText));
        } catch (e) {
          onMessage({ __parseError: String(e), raw: bodyText });
        }
        continue;
      }

      const lineEnd = buffer.indexOf('\n');
      if (lineEnd < 0) return;
      const line = buffer.subarray(0, lineEnd).toString('utf8').trim();
      buffer = buffer.subarray(lineEnd + 1);
      if (!line) {
        continue;
      }
      try {
        onMessage(JSON.parse(line));
      } catch (e) {
        onMessage({ __parseError: String(e), raw: line });
      }
    }
  };
}

async function run() {
  const child = spawn(process.execPath, [serverPath], {
    cwd: workspace,
    env: {
      ...process.env,
      MARS_WORKSPACE: workspace,
      MARS_MCP_TRACE: path.join(workspace, 'scanedfiles', 'mcp-smoke.trace.log'),
    },
    stdio: ['pipe', 'pipe', 'pipe'],
  });

  const pending = new Map();
  let nextId = 1;

  const parser = createParser((msg) => {
    if (msg && typeof msg.id !== 'undefined' && pending.has(msg.id)) {
      const p = pending.get(msg.id);
      pending.delete(msg.id);
      p.resolve(msg);
      return;
    }
    if (msg && msg.__parseError) {
      console.error('[mcp-smoke] parse error:', msg.__parseError);
      console.error('[mcp-smoke] raw:', msg.raw);
      return;
    }
    console.log('[mcp-smoke] unsolicited:', JSON.stringify(msg));
  });

  child.stdout.on('data', parser);
  child.stderr.on('data', (d) => {
    const text = d.toString('utf8').trim();
    if (text) console.error('[mcp-server:stderr]', text);
  });

  child.on('exit', (code, signal) => {
    if (pending.size > 0) {
      for (const [, p] of pending) {
        p.reject(new Error(`Server exited early code=${code} signal=${signal}`));
      }
      pending.clear();
    }
  });

  function request(method, params, timeoutMs = 10000) {
    const id = nextId++;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        pending.delete(id);
        reject(new Error(`Timeout waiting for response id=${id} method=${method}`));
      }, timeoutMs);

      pending.set(id, {
        resolve: (msg) => {
          clearTimeout(timer);
          resolve(msg);
        },
        reject: (err) => {
          clearTimeout(timer);
          reject(err);
        },
      });

      const payload = { jsonrpc: '2.0', id, method, params };
      child.stdin.write(encodeMessage(payload));
    });
  }

  try {
    const initResp = await request('initialize', {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: { name: 'mcp-smoke', version: '1.0.0' },
    });

    if (initResp.error) {
      throw new Error(`initialize error: ${JSON.stringify(initResp.error)}`);
    }
    if (!initResp.result || !initResp.result.protocolVersion) {
      throw new Error(`initialize missing protocolVersion: ${JSON.stringify(initResp)}`);
    }

    child.stdin.write(encodeMessage({ jsonrpc: '2.0', method: 'notifications/initialized', params: {} }));

    const listResp = await request('tools/list', {});
    if (listResp.error) {
      throw new Error(`tools/list error: ${JSON.stringify(listResp.error)}`);
    }

    const tools = listResp.result && Array.isArray(listResp.result.tools) ? listResp.result.tools : [];
    const hasListProcesses = tools.some((t) => t && t.name === 'mars-list-processes');
    if (!hasListProcesses) {
      throw new Error(`tools/list missing mars-list-processes: ${JSON.stringify(listResp.result)}`);
    }

    const callResp = await request('tools/call', {
      name: 'mars-list-processes',
      arguments: {},
    }, 20000);

    if (callResp.error) {
      throw new Error(`tools/call error: ${JSON.stringify(callResp.error)}`);
    }

    const content = callResp.result && Array.isArray(callResp.result.content) ? callResp.result.content : [];
    if (content.length === 0) {
      throw new Error(`tools/call returned empty content: ${JSON.stringify(callResp.result)}`);
    }

    console.log('[mcp-smoke] PASS');
    console.log('[mcp-smoke] initialize.protocolVersion =', initResp.result.protocolVersion);
    console.log('[mcp-smoke] tools.count =', tools.length);
    console.log('[mcp-smoke] tools includes mars-list-processes = true');
  } finally {
    try { child.kill(); } catch {}
  }
}

run().catch((err) => {
  console.error('[mcp-smoke] FAIL:', err && err.message ? err.message : String(err));
  process.exit(1);
});
