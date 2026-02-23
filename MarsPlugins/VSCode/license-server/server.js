const http = require('http');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const PORT = Number(process.env.PORT || 8787);
const ADMIN_API_KEY = process.env.ADMIN_API_KEY || '';
const LICENSE_SIGNING_SECRET = process.env.LICENSE_SIGNING_SECRET || '';
const PRIVACY_HASH_SALT = process.env.PRIVACY_HASH_SALT || '';
const DATA_DIR = path.resolve(process.env.DATA_DIR || path.join(__dirname, 'data'));
const REVOCATION_FILE = path.join(DATA_DIR, 'revocations.json');
const AUDIT_FILE = path.join(DATA_DIR, 'audit.log');
const MAX_BODY_BYTES = 64 * 1024;
const MAX_AUDIT_EVENTS = 5000;

if (!ADMIN_API_KEY || !LICENSE_SIGNING_SECRET || !PRIVACY_HASH_SALT) {
  throw new Error('Missing required env: ADMIN_API_KEY, LICENSE_SIGNING_SECRET, PRIVACY_HASH_SALT');
}

fs.mkdirSync(DATA_DIR, { recursive: true });

function readJsonFile(filePath, fallback) {
  try {
    if (!fs.existsSync(filePath)) return fallback;
    const raw = fs.readFileSync(filePath, 'utf8');
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

function writeJsonFile(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
}

const state = {
  revoked: new Set(readJsonFile(REVOCATION_FILE, [])),
  audit: [],
};

function nowIso() {
  return new Date().toISOString();
}

function base64Url(buffer) {
  return buffer.toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

function hashPrivacy(value) {
  const normalized = String(value || '').trim().toLowerCase();
  if (!normalized) return null;
  return crypto.createHash('sha256').update(`${PRIVACY_HASH_SALT}:${normalized}`).digest('hex');
}

function stableStringify(input) {
  if (Array.isArray(input)) {
    return `[${input.map((item) => stableStringify(item)).join(',')}]`;
  }
  if (input && typeof input === 'object') {
    const keys = Object.keys(input).sort();
    return `{${keys.map((key) => `${JSON.stringify(key)}:${stableStringify(input[key])}`).join(',')}}`;
  }
  return JSON.stringify(input);
}

function signLicensePayload(payloadWithoutSignature) {
  const canonical = stableStringify(payloadWithoutSignature);
  const mac = crypto.createHmac('sha256', LICENSE_SIGNING_SECRET).update(canonical).digest();
  return base64Url(mac);
}

function timingSafeEqualText(a, b) {
  const aa = Buffer.from(String(a || ''), 'utf8');
  const bb = Buffer.from(String(b || ''), 'utf8');
  if (aa.length !== bb.length) return false;
  return crypto.timingSafeEqual(aa, bb);
}

function issueLicense(input) {
  const now = new Date();
  const durationDays = Number.isInteger(input.durationDays) && input.durationDays > 0 ? input.durationDays : 365;
  const expiresAt = input.expiresAt ? new Date(input.expiresAt) : new Date(now.getTime() + durationDays * 86400000);

  const license = {
    version: 1,
    licenseId: crypto.randomUUID(),
    issuedAt: now.toISOString(),
    expiresAt: expiresAt.toISOString(),
    plan: typeof input.plan === 'string' && input.plan.trim() ? input.plan.trim() : 'pro',
    features: Array.isArray(input.features) ? input.features.filter((f) => typeof f === 'string' && f.trim()) : ['mcp_tools', 'replay', 'diagnostics'],
    customerRef: hashPrivacy(input.customerEmail || input.customerRef || ''),
    region: typeof input.region === 'string' ? input.region.toUpperCase() : 'GLOBAL',
    currency: typeof input.currency === 'string' ? input.currency.toUpperCase() : 'USD',
    amount: Number.isFinite(Number(input.amount)) ? Number(input.amount) : 19,
    maxMajorVersion: Number.isInteger(input.maxMajorVersion) ? input.maxMajorVersion : 1,
  };

  license.signature = signLicensePayload(license);
  return license;
}

function verifyLicense(license) {
  if (!license || typeof license !== 'object') {
    return { valid: false, reason: 'invalid_payload' };
  }

  const signed = { ...license };
  const signature = signed.signature;
  delete signed.signature;

  if (!signature || typeof signature !== 'string') {
    return { valid: false, reason: 'missing_signature' };
  }

  const expected = signLicensePayload(signed);
  if (!timingSafeEqualText(expected, signature)) {
    return { valid: false, reason: 'signature_mismatch' };
  }

  const expiresAt = Date.parse(String(license.expiresAt || ''));
  if (!Number.isFinite(expiresAt) || Date.now() >= expiresAt) {
    return { valid: false, reason: 'expired' };
  }

  const revokedHash = hashPrivacy(license.licenseId);
  if (revokedHash && state.revoked.has(revokedHash)) {
    return { valid: false, reason: 'revoked' };
  }

  return {
    valid: true,
    reason: 'ok',
    plan: license.plan,
    features: Array.isArray(license.features) ? license.features : [],
    expiresAt: license.expiresAt,
  };
}

function appendAudit(event) {
  const sanitized = {
    ts: nowIso(),
    action: event.action,
    ok: !!event.ok,
    requestId: event.requestId || null,
    subjectHash: event.subjectHash || null,
    reason: event.reason || null,
  };
  state.audit.push(sanitized);
  if (state.audit.length > MAX_AUDIT_EVENTS) {
    state.audit.splice(0, state.audit.length - MAX_AUDIT_EVENTS);
  }
  try {
    fs.appendFileSync(AUDIT_FILE, JSON.stringify(sanitized) + '\n', 'utf8');
  } catch {
    // ignore audit write errors
  }
}

function persistRevocations() {
  writeJsonFile(REVOCATION_FILE, Array.from(state.revoked.values()));
}

function sendJson(res, code, data) {
  const body = JSON.stringify(data);
  res.writeHead(code, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body),
    'Cache-Control': 'no-store',
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'Referrer-Policy': 'no-referrer',
    'Permissions-Policy': 'geolocation=(), microphone=(), camera=()',
  });
  res.end(body);
}

function parseBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let total = 0;

    req.on('data', (chunk) => {
      total += chunk.length;
      if (total > MAX_BODY_BYTES) {
        reject(new Error('payload_too_large'));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });

    req.on('end', () => {
      if (chunks.length === 0) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString('utf8')));
      } catch {
        reject(new Error('invalid_json'));
      }
    });

    req.on('error', reject);
  });
}

function isAdmin(req) {
  const provided = req.headers['x-admin-key'];
  return typeof provided === 'string' && timingSafeEqualText(provided, ADMIN_API_KEY);
}

const server = http.createServer(async (req, res) => {
  const requestId = typeof req.headers['x-request-id'] === 'string' ? req.headers['x-request-id'] : crypto.randomUUID();
  const url = new URL(req.url || '/', 'http://127.0.0.1');

  if (req.method === 'GET' && url.pathname === '/health') {
    sendJson(res, 200, {
      ok: true,
      service: 'mars-license-server',
      time: nowIso(),
      privacyMode: 'strict-minimal',
    });
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/license/verify') {
    try {
      const body = await parseBody(req);
      const result = verifyLicense(body.license);
      appendAudit({
        action: 'verify',
        ok: result.valid,
        requestId,
        subjectHash: hashPrivacy(body.license?.licenseId),
        reason: result.reason,
      });
      sendJson(res, 200, {
        ok: true,
        requestId,
        result,
      });
    } catch (e) {
      appendAudit({ action: 'verify', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/license/issue') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const license = issueLicense(body);
      appendAudit({ action: 'issue', ok: true, requestId, subjectHash: hashPrivacy(license.licenseId) });
      sendJson(res, 200, { ok: true, requestId, license });
    } catch (e) {
      appendAudit({ action: 'issue', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/license/revoke') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const idHash = hashPrivacy(body.licenseId);
      if (!idHash) {
        sendJson(res, 400, { ok: false, requestId, error: 'licenseId_required' });
        return;
      }
      state.revoked.add(idHash);
      persistRevocations();
      appendAudit({ action: 'revoke', ok: true, requestId, subjectHash: idHash });
      sendJson(res, 200, { ok: true, requestId });
    } catch (e) {
      appendAudit({ action: 'revoke', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/privacy/delete-audit-by-customer') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const subjectHash = hashPrivacy(body.customerEmail || body.customerRef || '');
      if (!subjectHash) {
        sendJson(res, 400, { ok: false, requestId, error: 'customer_identifier_required' });
        return;
      }

      const before = state.audit.length;
      state.audit = state.audit.filter((item) => item.subjectHash !== subjectHash);
      const deleted = before - state.audit.length;
      appendAudit({ action: 'privacy_delete', ok: true, requestId, subjectHash });
      sendJson(res, 200, { ok: true, requestId, deleted });
    } catch (e) {
      appendAudit({ action: 'privacy_delete', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  sendJson(res, 404, { ok: false, requestId, error: 'not_found' });
});

server.listen(PORT, '127.0.0.1', () => {
  console.log(`[license-server] listening on 127.0.0.1:${PORT}`);
});
