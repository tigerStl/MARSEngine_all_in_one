const http = require('http');
const https = require('https');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

loadDotEnv(path.join(__dirname, '.env'));

const PORT = Number(process.env.PORT || 8787);
const ADMIN_API_KEY = process.env.ADMIN_API_KEY || '';
const LICENSE_SIGNING_SECRET = process.env.LICENSE_SIGNING_SECRET || '';
const PRIVACY_HASH_SALT = process.env.PRIVACY_HASH_SALT || '';
const STRIPE_SECRET_KEY = process.env.STRIPE_SECRET_KEY || '';
const STRIPE_WEBHOOK_SECRET = process.env.STRIPE_WEBHOOK_SECRET || '';
const PUBLIC_BASE_URL = (process.env.PUBLIC_BASE_URL || `http://127.0.0.1:${PORT}`).replace(/\/+$/, '');
const DATA_DIR = path.resolve(process.env.DATA_DIR || path.join(__dirname, 'data'));
const REVOCATION_FILE = path.join(DATA_DIR, 'revocations.json');
const TEST_CLAIMS_FILE = path.join(DATA_DIR, 'test-claims.json');
const ISSUED_LICENSES_FILE = path.join(DATA_DIR, 'issued-licenses.json');
const ZELLE_ORDERS_FILE = path.join(DATA_DIR, 'zelle-orders.json');
const STRIPE_ORDERS_FILE = path.join(DATA_DIR, 'stripe-orders.json');
const AUDIT_FILE = path.join(DATA_DIR, 'audit.log');
const MAX_BODY_BYTES = 64 * 1024;
const MAX_AUDIT_EVENTS = 5000;
const TEST_REGION_LIMIT = 200;
const ZELLE_RECEIVER_NAME = process.env.ZELLE_RECEIVER_NAME || 'MARS Licensing';
const ZELLE_RECEIVER_ACCOUNT = process.env.ZELLE_RECEIVER_ACCOUNT || 'zelle@example.com';
const STRIPE_PRICE_US_CENTS = Number.parseInt(process.env.STRIPE_PRICE_US_CENTS || '499', 10);
const STRIPE_PRICE_CN_CENTS = Number.parseInt(process.env.STRIPE_PRICE_CN_CENTS || '500', 10);
const DECLARATION_EN = [
  'MARS License Declaration',
  '',
  'By using this software, you agree to the applicable license policy.',
  'Trial-limited mode: after 7 days, replay is limited to 10 steps.',
  'Recording remains available; replay beyond 10 steps requires paid license.',
  'US paid price: $4.99. CN paid price: CNY 5.',
].join('\n');
const DECLARATION_ZH = [
  'MARS 许可证声明',
  '',
  '使用本软件即表示同意适用的许可证策略。',
  '受限试用版：7天后，回放最多10步。',
  '录制可继续使用；超过10步回放需要付费授权。',
  '美国收费版价格：4.99美元；中国收费版价格：5元。',
].join('\n');

function loadDotEnv(filePath) {
  try {
    if (!fs.existsSync(filePath)) return;
    const lines = fs.readFileSync(filePath, 'utf8').split(/\r?\n/);
    for (const raw of lines) {
      const line = raw.trim();
      if (!line || line.startsWith('#')) continue;
      const idx = line.indexOf('=');
      if (idx <= 0) continue;
      const key = line.substring(0, idx).trim();
      let val = line.substring(idx + 1).trim();
      if ((val.startsWith('"') && val.endsWith('"')) || (val.startsWith("'") && val.endsWith("'"))) {
        val = val.substring(1, val.length - 1);
      }
      if (!Object.prototype.hasOwnProperty.call(process.env, key) || !process.env[key]) {
        process.env[key] = val;
      }
    }
  } catch {
    // ignore .env load failures, required env check below will handle missing values
  }
}

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
  testClaims: readJsonFile(TEST_CLAIMS_FILE, { US: {}, CN: {} }),
  issuedLicenses: readJsonFile(ISSUED_LICENSES_FILE, []),
  zelleOrders: readJsonFile(ZELLE_ORDERS_FILE, []),
  stripeOrders: readJsonFile(STRIPE_ORDERS_FILE, []),
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

function persistTestClaims() {
  if (!state.testClaims || typeof state.testClaims !== 'object') {
    state.testClaims = { US: {}, CN: {} };
  }
  if (!state.testClaims.US || typeof state.testClaims.US !== 'object') state.testClaims.US = {};
  if (!state.testClaims.CN || typeof state.testClaims.CN !== 'object') state.testClaims.CN = {};
  writeJsonFile(TEST_CLAIMS_FILE, state.testClaims);
}

function persistIssuedLicenses() {
  if (!Array.isArray(state.issuedLicenses)) {
    state.issuedLicenses = [];
  }
  writeJsonFile(ISSUED_LICENSES_FILE, state.issuedLicenses);
}

function persistZelleOrders() {
  if (!Array.isArray(state.zelleOrders)) {
    state.zelleOrders = [];
  }
  writeJsonFile(ZELLE_ORDERS_FILE, state.zelleOrders);
}

function persistStripeOrders() {
  if (!Array.isArray(state.stripeOrders)) {
    state.stripeOrders = [];
  }
  writeJsonFile(STRIPE_ORDERS_FILE, state.stripeOrders);
}

function normalizeRegion(input) {
  const raw = String(input || '').toUpperCase();
  if (raw === 'US') return 'US';
  if (raw === 'CN') return 'CN';
  return 'GLOBAL';
}

function getTestPoolStats() {
  const usCount = Object.keys((state.testClaims && state.testClaims.US) || {}).length;
  const cnCount = Object.keys((state.testClaims && state.testClaims.CN) || {}).length;
  return {
    US: { used: usCount, limit: TEST_REGION_LIMIT, remaining: Math.max(0, TEST_REGION_LIMIT - usCount) },
    CN: { used: cnCount, limit: TEST_REGION_LIMIT, remaining: Math.max(0, TEST_REGION_LIMIT - cnCount) },
  };
}

function claimTestLicense(input) {
  const region = normalizeRegion(input.region);
  if (region !== 'US' && region !== 'CN') {
    return { ok: false, reason: 'region_must_be_us_or_cn' };
  }
  const customerRef = hashPrivacy(input.customerEmail || input.customerRef || '');
  if (!customerRef) {
    return { ok: false, reason: 'customer_identifier_required' };
  }
  if (!state.testClaims || typeof state.testClaims !== 'object') {
    state.testClaims = { US: {}, CN: {} };
  }
  if (!state.testClaims[region] || typeof state.testClaims[region] !== 'object') {
    state.testClaims[region] = {};
  }
  const regionClaims = state.testClaims[region];
  const existed = regionClaims[customerRef];
  if (existed && typeof existed === 'object') {
    return {
      ok: true,
      reused: true,
      claim: existed,
      stats: getTestPoolStats(),
    };
  }
  const used = Object.keys(regionClaims).length;
  if (used >= TEST_REGION_LIMIT) {
    return {
      ok: false,
      reason: 'test_pool_exhausted',
      stats: getTestPoolStats(),
    };
  }
  const claim = {
    claimId: crypto.randomUUID(),
    customerRef,
    customerEmail: typeof input.customerEmail === 'string' ? input.customerEmail.trim() : '',
    region,
    issuedAt: nowIso(),
    licenseType: 'TEST',
    expiresAt: new Date(Date.now() + 365 * 86400000).toISOString(),
  };
  regionClaims[customerRef] = claim;
  persistTestClaims();
  return {
    ok: true,
    reused: false,
    claim,
    stats: getTestPoolStats(),
  };
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

function sendHtml(res, code, html) {
  res.writeHead(code, {
    'Content-Type': 'text/html; charset=utf-8',
    'Content-Length': Buffer.byteLength(html),
    'Cache-Control': 'no-store',
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'Referrer-Policy': 'no-referrer',
  });
  res.end(html);
}

function getClaimsList() {
  const out = [];
  const us = state.testClaims?.US || {};
  const cn = state.testClaims?.CN || {};
  for (const [customerRef, claim] of Object.entries(us)) {
    out.push({ customerRef, region: 'US', ...claim });
  }
  for (const [customerRef, claim] of Object.entries(cn)) {
    out.push({ customerRef, region: 'CN', ...claim });
  }
  return out;
}

function normalizeLicenseTypeFromPlan(plan, fallback) {
  const raw = String(plan || fallback || '').trim().toUpperCase();
  if (raw === 'PAID' || raw === 'PRO') return 'PAID';
  if (raw === 'TEST') return 'TEST';
  if (raw === 'TRIAL' || raw === 'TRIAL_LIMITED') return 'TRIAL_LIMITED';
  return raw || 'UNKNOWN';
}

function isAdminKeyText(value) {
  return timingSafeEqualText(String(value || ''), ADMIN_API_KEY);
}

function upsertIssuedLicenseRecord(input, license) {
  if (!Array.isArray(state.issuedLicenses)) {
    state.issuedLicenses = [];
  }
  const record = {
    licenseId: license.licenseId,
    licenseType: normalizeLicenseTypeFromPlan(input.plan, 'PAID'),
    plan: license.plan,
    region: license.region,
    issuedAt: license.issuedAt,
    expiresAt: license.expiresAt,
    customerEmail: typeof input.customerEmail === 'string' ? input.customerEmail.trim() : '',
    customerRef: typeof input.customerRef === 'string' ? input.customerRef.trim() : '',
    customerRefHash: license.customerRef || null,
    amount: license.amount,
    currency: license.currency,
    createdAt: nowIso(),
    license,
  };
  const idx = state.issuedLicenses.findIndex((x) => x && x.licenseId === license.licenseId);
  if (idx >= 0) state.issuedLicenses[idx] = record;
  else state.issuedLicenses.push(record);
  persistIssuedLicenses();
  return record;
}

function buildAdminRows() {
  const rows = [];
  const issued = Array.isArray(state.issuedLicenses) ? state.issuedLicenses : [];
  for (const rec of issued) {
    const licenseId = rec.licenseId || rec.license?.licenseId || '';
    const revoked = !!(licenseId && state.revoked.has(hashPrivacy(licenseId)));
    rows.push({
      rowId: `issued:${licenseId}`,
      source: 'ISSUED',
      licenseType: normalizeLicenseTypeFromPlan(rec.licenseType || rec.plan || rec.license?.plan, 'PAID'),
      plan: rec.plan || rec.license?.plan || '',
      licenseId: licenseId || null,
      claimId: null,
      region: rec.region || rec.license?.region || 'GLOBAL',
      issuedAt: rec.issuedAt || rec.license?.issuedAt || null,
      expiresAt: rec.expiresAt || rec.license?.expiresAt || null,
      customerEmail: rec.customerEmail || null,
      customerRef: rec.customerRef || rec.customerRefHash || rec.license?.customerRef || null,
      revoked,
      raw: rec,
    });
  }
  for (const c of getClaimsList()) {
    const claimRevoked = !!c.revokedAt || String(c.status || '').toUpperCase() === 'REVOKED';
    rows.push({
      rowId: `claim:${c.claimId || c.customerRef}`,
      source: 'TEST_CLAIM',
      licenseType: 'TEST',
      plan: 'TEST',
      licenseId: null,
      claimId: c.claimId || null,
      region: c.region || 'GLOBAL',
      issuedAt: c.issuedAt || null,
      expiresAt: c.expiresAt || null,
      customerEmail: c.customerEmail || null,
      customerRef: c.customerRef || null,
      revoked: claimRevoked,
      raw: c,
    });
  }
  rows.sort((a, b) => Date.parse(String(b.issuedAt || 0)) - Date.parse(String(a.issuedAt || 0)));
  return rows;
}

function renewIssuedLicenseById(licenseId, days) {
  const idx = Array.isArray(state.issuedLicenses)
    ? state.issuedLicenses.findIndex((x) => x && (x.licenseId === licenseId || x.license?.licenseId === licenseId))
    : -1;
  if (idx < 0) return { ok: false, error: 'license_not_found' };
  const rec = state.issuedLicenses[idx];
  const addDays = Number.isInteger(days) && days > 0 ? days : 365;
  const baseTs = Math.max(Date.now(), Date.parse(String(rec.expiresAt || rec.license?.expiresAt || 0)) || 0);
  const nextTs = baseTs + addDays * 86400000;
  const nextIso = new Date(nextTs).toISOString();
  rec.expiresAt = nextIso;
  if (rec.license && typeof rec.license === 'object') {
    rec.license.expiresAt = nextIso;
    const signed = { ...rec.license };
    delete signed.signature;
    rec.license.signature = signLicensePayload(signed);
  }
  rec.updatedAt = nowIso();
  state.issuedLicenses[idx] = rec;
  persistIssuedLicenses();
  return { ok: true, rowId: `issued:${licenseId}`, expiresAt: nextIso, daysAdded: addDays };
}

function revokeIssuedLicenseById(licenseId) {
  const idHash = hashPrivacy(licenseId);
  if (!idHash) return { ok: false, error: 'licenseId_required' };
  state.revoked.add(idHash);
  persistRevocations();
  return { ok: true, rowId: `issued:${licenseId}` };
}

function renewTestClaimById(claimId, days) {
  const addDays = Number.isInteger(days) && days > 0 ? days : 365;
  const regions = ['US', 'CN'];
  for (const region of regions) {
    const claims = (state.testClaims && state.testClaims[region]) || {};
    for (const customerRef of Object.keys(claims)) {
      const claim = claims[customerRef];
      if (!claim || claim.claimId !== claimId) continue;
      const baseTs = Math.max(Date.now(), Date.parse(String(claim.expiresAt || 0)) || 0);
      const nextIso = new Date(baseTs + addDays * 86400000).toISOString();
      claim.expiresAt = nextIso;
      delete claim.revokedAt;
      claim.status = 'ACTIVE';
      claims[customerRef] = claim;
      state.testClaims[region] = claims;
      persistTestClaims();
      return { ok: true, rowId: `claim:${claimId}`, expiresAt: nextIso, daysAdded: addDays };
    }
  }
  return { ok: false, error: 'claim_not_found' };
}

function revokeTestClaimById(claimId) {
  const regions = ['US', 'CN'];
  for (const region of regions) {
    const claims = (state.testClaims && state.testClaims[region]) || {};
    for (const customerRef of Object.keys(claims)) {
      const claim = claims[customerRef];
      if (!claim || claim.claimId !== claimId) continue;
      claim.revokedAt = nowIso();
      claim.status = 'REVOKED';
      claims[customerRef] = claim;
      state.testClaims[region] = claims;
      persistTestClaims();
      return { ok: true, rowId: `claim:${claimId}` };
    }
  }
  return { ok: false, error: 'claim_not_found' };
}

function applyAdminLicenseAction(input) {
  const rowId = String(input.rowId || '').trim();
  const action = String(input.action || '').trim().toLowerCase();
  const days = Number.parseInt(String(input.days || '365'), 10);
  if (!rowId) return { ok: false, error: 'rowId_required' };
  if (action !== 'renew' && action !== 'revoke') return { ok: false, error: 'action_must_be_renew_or_revoke' };
  if (rowId.startsWith('issued:')) {
    const licenseId = rowId.slice('issued:'.length);
    if (!licenseId) return { ok: false, error: 'licenseId_required' };
    return action === 'renew' ? renewIssuedLicenseById(licenseId, days) : revokeIssuedLicenseById(licenseId);
  }
  if (rowId.startsWith('claim:')) {
    const claimId = rowId.slice('claim:'.length);
    if (!claimId) return { ok: false, error: 'claimId_required' };
    return action === 'renew' ? renewTestClaimById(claimId, days) : revokeTestClaimById(claimId);
  }
  return { ok: false, error: 'unsupported_rowId' };
}

function generateZelleOrderId() {
  const d = new Date();
  const yyyy = d.getUTCFullYear();
  const mm = String(d.getUTCMonth() + 1).padStart(2, '0');
  const dd = String(d.getUTCDate()).padStart(2, '0');
  const suffix = crypto.randomBytes(3).toString('hex').toUpperCase();
  return `ZELLE-${yyyy}${mm}${dd}-${suffix}`;
}

function getZelleDefaultPrice(region) {
  return region === 'US'
    ? { currency: 'USD', amount: 4.99 }
    : region === 'CN'
      ? { currency: 'CNY', amount: 5 }
      : { currency: 'USD', amount: 4.99 };
}

function createZelleOrder(input) {
  const customerEmail = String(input.customerEmail || '').trim().toLowerCase();
  if (!customerEmail) return { ok: false, error: 'customerEmail_required' };
  const region = normalizeRegion(input.region);
  const price = getZelleDefaultPrice(region);
  const order = {
    orderId: generateZelleOrderId(),
    channel: 'ZELLE',
    status: 'CREATED',
    customerEmail,
    customerRefHash: hashPrivacy(customerEmail),
    region,
    currency: price.currency,
    amount: price.amount,
    transferProofRef: '',
    transferProofNote: '',
    createdAt: nowIso(),
    updatedAt: nowIso(),
    reviewedAt: null,
    reviewedBy: null,
    reviewNote: '',
    license: null,
  };
  state.zelleOrders.push(order);
  persistZelleOrders();
  return { ok: true, order };
}

function submitZelleProof(input) {
  const orderId = String(input.orderId || '').trim();
  const customerEmail = String(input.customerEmail || '').trim().toLowerCase();
  const transferProofRef = String(input.transferProofRef || '').trim();
  const transferProofNote = String(input.transferProofNote || '').trim();
  if (!orderId) return { ok: false, error: 'orderId_required' };
  if (!customerEmail) return { ok: false, error: 'customerEmail_required' };
  if (!transferProofRef && !transferProofNote) return { ok: false, error: 'proof_required' };
  const idx = state.zelleOrders.findIndex((x) => x && x.orderId === orderId);
  if (idx < 0) return { ok: false, error: 'order_not_found' };
  const order = state.zelleOrders[idx];
  if (String(order.customerEmail || '').toLowerCase() !== customerEmail) {
    return { ok: false, error: 'email_mismatch' };
  }
  if (order.status === 'APPROVED') {
    return { ok: true, order, reused: true };
  }
  order.status = 'PROOF_SUBMITTED';
  order.transferProofRef = transferProofRef;
  order.transferProofNote = transferProofNote;
  order.updatedAt = nowIso();
  state.zelleOrders[idx] = order;
  persistZelleOrders();
  return { ok: true, order, reused: false };
}

function getUserOrderStatus(orderId, customerEmail) {
  const idx = state.zelleOrders.findIndex((x) => x && x.orderId === orderId);
  if (idx < 0) return { ok: false, error: 'order_not_found' };
  const order = state.zelleOrders[idx];
  const email = String(customerEmail || '').trim().toLowerCase();
  if (!email) return { ok: false, error: 'customerEmail_required' };
  if (String(order.customerEmail || '').toLowerCase() !== email) return { ok: false, error: 'email_mismatch' };
  return {
    ok: true,
    order: {
      orderId: order.orderId,
      status: order.status,
      customerEmail: order.customerEmail,
      region: order.region,
      amount: order.amount,
      currency: order.currency,
      updatedAt: order.updatedAt,
      reviewedAt: order.reviewedAt,
      reviewNote: order.reviewNote || '',
      hasLicense: !!order.license,
    },
  };
}

function listZelleOrders(statusFilter, qText) {
  const status = String(statusFilter || 'ALL').trim().toUpperCase();
  const q = String(qText || '').trim().toLowerCase();
  const items = (Array.isArray(state.zelleOrders) ? state.zelleOrders : [])
    .filter((o) => {
      if (!o || typeof o !== 'object') return false;
      if (status !== 'ALL' && String(o.status || '').toUpperCase() !== status) return false;
      if (!q) return true;
      const fields = [
        o.orderId, o.status, o.customerEmail, o.region, o.transferProofRef, o.transferProofNote, o.reviewNote,
      ].map((v) => String(v || '').toLowerCase());
      return fields.some((f) => f.includes(q));
    })
    .sort((a, b) => Date.parse(String(b.updatedAt || b.createdAt || 0)) - Date.parse(String(a.updatedAt || a.createdAt || 0)));
  return items.slice(0, 500);
}

function adminActionZelleOrder(input) {
  const orderId = String(input.orderId || '').trim();
  const action = String(input.action || '').trim().toUpperCase();
  const reviewNote = String(input.reviewNote || '').trim();
  if (!orderId) return { ok: false, error: 'orderId_required' };
  if (action !== 'APPROVE' && action !== 'REJECT') return { ok: false, error: 'action_must_be_APPROVE_or_REJECT' };
  const idx = state.zelleOrders.findIndex((x) => x && x.orderId === orderId);
  if (idx < 0) return { ok: false, error: 'order_not_found' };
  const order = state.zelleOrders[idx];
  if (action === 'REJECT') {
    order.status = 'REJECTED';
    order.reviewNote = reviewNote || 'Rejected by admin';
    order.reviewedAt = nowIso();
    order.updatedAt = nowIso();
    state.zelleOrders[idx] = order;
    persistZelleOrders();
    return { ok: true, order };
  }
  const issued = issueLicense({
    plan: 'PAID',
    customerEmail: order.customerEmail,
    region: order.region,
    currency: order.currency,
    amount: order.amount,
    durationDays: 365,
  });
  upsertIssuedLicenseRecord({
    plan: 'PAID',
    customerEmail: order.customerEmail,
    region: order.region,
    currency: order.currency,
    amount: order.amount,
  }, issued);
  order.status = 'APPROVED';
  order.reviewNote = reviewNote || 'Approved';
  order.reviewedAt = nowIso();
  order.updatedAt = nowIso();
  order.license = issued;
  state.zelleOrders[idx] = order;
  persistZelleOrders();
  return { ok: true, order };
}

function zellePageHtml() {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>MARS Zelle Payment</title>
  <style>
    body{font-family:Arial,sans-serif;max-width:860px;margin:20px auto;padding:0 12px;color:#1f2937}
    .card{border:1px solid #d1d5db;border-radius:10px;padding:12px;margin-bottom:12px}
    .row{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:8px}
    input,select,button,textarea{padding:8px;font-size:14px}
    input,select,textarea{border:1px solid #cbd5e1;border-radius:6px}
    button{background:#2563eb;color:#fff;border:none;border-radius:6px;cursor:pointer}
    pre{background:#0b1220;color:#e5e7eb;padding:10px;border-radius:8px;white-space:pre-wrap}
  </style>
</head>
<body>
  <h2>MARS License via Zelle</h2>
  <div class="card">
    <div><b>Receiver:</b> ${ZELLE_RECEIVER_NAME} (${ZELLE_RECEIVER_ACCOUNT})</div>
    <div>Step 1: Create order (server generates unique order ID).</div>
    <div>Step 2: Transfer by Zelle with order ID in memo.</div>
    <div>Step 3: Submit transfer proof and wait for admin review.</div>
  </div>
  <div class="card">
    <h3>Create Order</h3>
    <div class="row">
      <input id="email" placeholder="Email" style="min-width:280px" />
      <select id="region"><option value="US">US</option><option value="CN">CN</option></select>
      <button onclick="createOrder()">Create</button>
    </div>
    <pre id="created">-</pre>
  </div>
  <div class="card">
    <h3>Submit Transfer Proof</h3>
    <div class="row">
      <input id="orderId" placeholder="Order ID" style="min-width:220px" />
      <input id="email2" placeholder="Email" style="min-width:280px" />
    </div>
    <div class="row">
      <input id="proofRef" placeholder="Bank/Zelle reference ID (optional)" style="min-width:280px" />
    </div>
    <div class="row">
      <textarea id="proofNote" placeholder="Proof note/screenshot link" rows="3" style="min-width:520px"></textarea>
    </div>
    <button onclick="submitProof()">Submit Proof</button>
    <pre id="proofResp">-</pre>
  </div>
  <div class="card">
    <h3>Check Status</h3>
    <div class="row">
      <input id="orderId3" placeholder="Order ID" style="min-width:220px" />
      <input id="email3" placeholder="Email" style="min-width:280px" />
      <button onclick="checkStatus()">Check</button>
    </div>
    <pre id="statusResp">-</pre>
  </div>
  <script>
    async function postJson(url, body){
      const r = await fetch(url,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body||{})});
      const j = await r.json();
      return {status:r.status,body:j};
    }
    async function getJson(url){
      const r = await fetch(url);
      const j = await r.json();
      return {status:r.status,body:j};
    }
    async function createOrder(){
      const email = document.getElementById('email').value || '';
      const region = document.getElementById('region').value || 'US';
      const res = await postJson('/v1/zelle/order/create',{customerEmail:email,region});
      document.getElementById('created').textContent = JSON.stringify(res, null, 2);
      if (res.body && res.body.order && res.body.order.orderId) {
        document.getElementById('orderId').value = res.body.order.orderId;
        document.getElementById('orderId3').value = res.body.order.orderId;
        document.getElementById('email2').value = email;
        document.getElementById('email3').value = email;
      }
    }
    async function submitProof(){
      const payload = {
        orderId: document.getElementById('orderId').value || '',
        customerEmail: document.getElementById('email2').value || '',
        transferProofRef: document.getElementById('proofRef').value || '',
        transferProofNote: document.getElementById('proofNote').value || '',
      };
      const res = await postJson('/v1/zelle/order/proof', payload);
      document.getElementById('proofResp').textContent = JSON.stringify(res, null, 2);
    }
    async function checkStatus(){
      const orderId = encodeURIComponent(document.getElementById('orderId3').value || '');
      const email = encodeURIComponent(document.getElementById('email3').value || '');
      const res = await getJson('/v1/zelle/order/status?orderId=' + orderId + '&email=' + email);
      document.getElementById('statusResp').textContent = JSON.stringify(res, null, 2);
    }
  </script>
</body>
</html>`;
}

function stripeConfigured() {
  return !!STRIPE_SECRET_KEY;
}

function generateStripeOrderId() {
  const d = new Date();
  const yyyy = d.getUTCFullYear();
  const mm = String(d.getUTCMonth() + 1).padStart(2, '0');
  const dd = String(d.getUTCDate()).padStart(2, '0');
  const suffix = crypto.randomBytes(3).toString('hex').toUpperCase();
  return `STRIPE-${yyyy}${mm}${dd}-${suffix}`;
}

function stripePriceForRegion(region) {
  if (region === 'CN') {
    return { currency: 'cny', unitAmount: Number.isFinite(STRIPE_PRICE_CN_CENTS) ? STRIPE_PRICE_CN_CENTS : 500 };
  }
  return { currency: 'usd', unitAmount: Number.isFinite(STRIPE_PRICE_US_CENTS) ? STRIPE_PRICE_US_CENTS : 499 };
}

function createStripeOrder(input) {
  if (!stripeConfigured()) return { ok: false, error: 'stripe_not_configured' };
  const customerEmail = String(input.customerEmail || '').trim().toLowerCase();
  if (!customerEmail) return { ok: false, error: 'customerEmail_required' };
  const region = normalizeRegion(input.region);
  const p = stripePriceForRegion(region);
  const order = {
    orderId: generateStripeOrderId(),
    channel: 'STRIPE',
    status: 'CREATED',
    customerEmail,
    customerRefHash: hashPrivacy(customerEmail),
    region,
    currency: p.currency.toUpperCase(),
    amount: p.unitAmount / 100,
    amountCents: p.unitAmount,
    stripeSessionId: null,
    stripePaymentIntentId: null,
    checkoutUrl: null,
    createdAt: nowIso(),
    updatedAt: nowIso(),
    paidAt: null,
    finalizedAt: null,
    reviewNote: '',
    license: null,
  };
  state.stripeOrders.push(order);
  persistStripeOrders();
  return { ok: true, order };
}

function formUrlEncoded(input) {
  const body = new URLSearchParams();
  for (const [k, v] of Object.entries(input || {})) {
    if (v === undefined || v === null) continue;
    body.append(k, String(v));
  }
  return body.toString();
}

function stripeApiRequest(method, apiPath, formData) {
  return new Promise((resolve, reject) => {
    const payload = formData ? formUrlEncoded(formData) : '';
    const req = https.request({
      method,
      hostname: 'api.stripe.com',
      port: 443,
      path: apiPath,
      headers: {
        Authorization: `Bearer ${STRIPE_SECRET_KEY}`,
        'Content-Type': 'application/x-www-form-urlencoded',
        'Content-Length': Buffer.byteLength(payload),
      },
    }, (res) => {
      const chunks = [];
      res.on('data', (d) => chunks.push(Buffer.isBuffer(d) ? d : Buffer.from(d)));
      res.on('end', () => {
        const text = Buffer.concat(chunks).toString('utf8');
        let parsed = null;
        try {
          parsed = text ? JSON.parse(text) : {};
        } catch {
          parsed = { raw: text };
        }
        if ((res.statusCode || 500) < 200 || (res.statusCode || 500) >= 300) {
          const msg = (parsed && parsed.error && parsed.error.message) ? parsed.error.message : text;
          reject(new Error(`stripe_http_${res.statusCode}: ${msg}`));
          return;
        }
        resolve(parsed);
      });
    });
    req.on('error', reject);
    if (payload) req.write(payload);
    req.end();
  });
}

async function createStripeCheckoutForOrder(order) {
  const successUrl = `${PUBLIC_BASE_URL}/stripe/success?session_id={CHECKOUT_SESSION_ID}&orderId=${encodeURIComponent(order.orderId)}`;
  const cancelUrl = `${PUBLIC_BASE_URL}/stripe/cancel?orderId=${encodeURIComponent(order.orderId)}`;
  const params = {
    mode: 'payment',
    success_url: successUrl,
    cancel_url: cancelUrl,
    customer_email: order.customerEmail,
    'metadata[orderId]': order.orderId,
    'metadata[customerEmail]': order.customerEmail,
    'line_items[0][quantity]': 1,
    'line_items[0][price_data][currency]': String(order.currency || 'USD').toLowerCase(),
    'line_items[0][price_data][unit_amount]': order.amountCents,
    'line_items[0][price_data][product_data][name]': 'MARS VSCode Plugin License',
    'line_items[0][price_data][product_data][description]': `Order ${order.orderId}`,
  };
  const created = await stripeApiRequest('POST', '/v1/checkout/sessions', params);
  return created;
}

function getStripeOrderByOrderId(orderId) {
  const idx = state.stripeOrders.findIndex((x) => x && x.orderId === orderId);
  if (idx < 0) return { idx: -1, order: null };
  return { idx, order: state.stripeOrders[idx] };
}

function getStripeOrderBySessionId(sessionId) {
  const idx = state.stripeOrders.findIndex((x) => x && x.stripeSessionId === sessionId);
  if (idx < 0) return { idx: -1, order: null };
  return { idx, order: state.stripeOrders[idx] };
}

async function finalizeStripeSessionBySessionId(sessionId, note) {
  if (!stripeConfigured()) return { ok: false, error: 'stripe_not_configured' };
  const session = await stripeApiRequest('GET', `/v1/checkout/sessions/${encodeURIComponent(sessionId)}`, null);
  const paymentStatus = String(session.payment_status || '').toLowerCase();
  const sessionStatus = String(session.status || '').toLowerCase();
  const paid = paymentStatus === 'paid' || sessionStatus === 'complete';
  if (!paid) return { ok: false, error: 'payment_not_completed', session };
  let orderInfo = getStripeOrderBySessionId(sessionId);
  if (!orderInfo.order) {
    const orderIdFromMeta = session && session.metadata ? String(session.metadata.orderId || '').trim() : '';
    if (orderIdFromMeta) orderInfo = getStripeOrderByOrderId(orderIdFromMeta);
  }
  if (!orderInfo.order) return { ok: false, error: 'order_not_found_for_session' };
  const order = orderInfo.order;
  if (order.license) {
    return { ok: true, reused: true, order };
  }
  const issued = issueLicense({
    plan: 'PAID',
    customerEmail: order.customerEmail,
    region: order.region,
    currency: order.currency,
    amount: order.amount,
    durationDays: 365,
  });
  upsertIssuedLicenseRecord({
    plan: 'PAID',
    customerEmail: order.customerEmail,
    region: order.region,
    currency: order.currency,
    amount: order.amount,
  }, issued);
  order.status = 'PAID';
  order.stripePaymentIntentId = session.payment_intent || order.stripePaymentIntentId || null;
  order.paidAt = nowIso();
  order.finalizedAt = nowIso();
  order.updatedAt = nowIso();
  order.reviewNote = note || 'Auto-finalized after successful Stripe payment.';
  order.license = issued;
  state.stripeOrders[orderInfo.idx] = order;
  persistStripeOrders();
  return { ok: true, reused: false, order };
}

function listStripeOrders(statusFilter, qText) {
  const status = String(statusFilter || 'ALL').trim().toUpperCase();
  const q = String(qText || '').trim().toLowerCase();
  const items = (Array.isArray(state.stripeOrders) ? state.stripeOrders : [])
    .filter((o) => {
      if (!o || typeof o !== 'object') return false;
      if (status !== 'ALL' && String(o.status || '').toUpperCase() !== status) return false;
      if (!q) return true;
      const fields = [
        o.orderId, o.status, o.customerEmail, o.region, o.stripeSessionId, o.stripePaymentIntentId, o.reviewNote,
      ].map((v) => String(v || '').toLowerCase());
      return fields.some((f) => f.includes(q));
    })
    .sort((a, b) => Date.parse(String(b.updatedAt || b.createdAt || 0)) - Date.parse(String(a.updatedAt || a.createdAt || 0)));
  return items.slice(0, 500);
}

function stripePageHtml() {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>MARS Stripe Payment</title>
  <style>
    body{font-family:Arial,sans-serif;max-width:860px;margin:20px auto;padding:0 12px;color:#1f2937}
    .card{border:1px solid #d1d5db;border-radius:10px;padding:12px;margin-bottom:12px}
    .row{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:8px}
    input,select,button{padding:8px;font-size:14px}
    input,select{border:1px solid #cbd5e1;border-radius:6px}
    button{background:#2563eb;color:#fff;border:none;border-radius:6px;cursor:pointer}
    pre{background:#0b1220;color:#e5e7eb;padding:10px;border-radius:8px;white-space:pre-wrap}
  </style>
</head>
<body>
  <h2>MARS License via Stripe</h2>
  <div class="card">
    <div>1) Create order (unique order ID from server)</div>
    <div>2) Redirect to Stripe Checkout and pay</div>
    <div>3) Payment success will immediately grant license permission</div>
  </div>
  <div class="card">
    <h3>Start Payment</h3>
    <div class="row">
      <input id="email" placeholder="Email" style="min-width:280px" />
      <select id="region"><option value="US">US</option><option value="CN">CN</option></select>
      <button onclick="startPay()">Pay with Stripe</button>
    </div>
    <pre id="resp">-</pre>
  </div>
  <script>
    async function postJson(url, body){
      const r = await fetch(url,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body||{})});
      const j = await r.json();
      return {status:r.status,body:j};
    }
    async function startPay(){
      const email = document.getElementById('email').value || '';
      const region = document.getElementById('region').value || 'US';
      const res = await postJson('/v1/stripe/checkout/create',{customerEmail:email,region});
      document.getElementById('resp').textContent = JSON.stringify(res, null, 2);
      if (res.status >= 200 && res.status < 300 && res.body && res.body.checkoutUrl) {
        window.location.href = res.body.checkoutUrl;
      }
    }
  </script>
</body>
</html>`;
}

function verifyStripeWebhookSignature(rawBodyBuffer, signatureHeader) {
  if (!STRIPE_WEBHOOK_SECRET) return true;
  const sig = String(signatureHeader || '').trim();
  if (!sig) return false;
  const parts = Object.fromEntries(sig.split(',').map((p) => {
    const i = p.indexOf('=');
    if (i <= 0) return [p, ''];
    return [p.slice(0, i), p.slice(i + 1)];
  }));
  const timestamp = parts.t;
  const v1 = parts.v1;
  if (!timestamp || !v1) return false;
  const signedPayload = `${timestamp}.${rawBodyBuffer.toString('utf8')}`;
  const expected = crypto.createHmac('sha256', STRIPE_WEBHOOK_SECRET).update(signedPayload).digest('hex');
  return timingSafeEqualText(expected, v1);
}

function stripeSuccessHtml(sessionId, orderId) {
  return `<!doctype html>
<html><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><title>Payment Success</title></head>
<body style="font-family:Arial,sans-serif;max-width:860px;margin:20px auto;padding:0 12px">
  <h2>Payment received</h2>
  <p>Finalizing your license, please wait...</p>
  <pre id="result">Working...</pre>
  <script>
    (async function(){
      const sid = ${JSON.stringify(sessionId || '')};
      const oid = ${JSON.stringify(orderId || '')};
      const url = '/v1/stripe/session/finalize?sessionId=' + encodeURIComponent(sid) + '&orderId=' + encodeURIComponent(oid || '');
      const r = await fetch(url);
      const j = await r.json();
      document.getElementById('result').textContent = JSON.stringify(j, null, 2);
    })();
  </script>
</body></html>`;
}

function filterAdminRows(rows, typeFilter, queryText) {
  const type = String(typeFilter || 'ALL').trim().toUpperCase();
  const q = String(queryText || '').trim();
  const qLower = q.toLowerCase();
  const qHash = q ? hashPrivacy(q) : null;
  return rows.filter((r) => {
    if (type !== 'ALL' && String(r.licenseType || '').toUpperCase() !== type) return false;
    if (!q) return true;
    const fields = [
      r.licenseType, r.plan, r.source, r.licenseId, r.claimId, r.region, r.customerEmail, r.customerRef, r.issuedAt, r.expiresAt,
    ].map((x) => String(x || '').toLowerCase());
    if (fields.some((f) => f.includes(qLower))) return true;
    if (qHash && String(r.customerRef || '').toLowerCase() === String(qHash).toLowerCase()) return true;
    if (qHash && r.licenseId && state.revoked.has(qHash) && hashPrivacy(r.licenseId) === qHash) return true;
    return false;
  });
}

function summarizeRows(rows) {
  const summary = {
    total: rows.length,
    revokedCount: rows.filter((r) => r.revoked).length,
    byType: {},
  };
  for (const r of rows) {
    const t = String(r.licenseType || 'UNKNOWN').toUpperCase();
    if (!summary.byType[t]) summary.byType[t] = { count: 0 };
    summary.byType[t].count += 1;
  }
  return summary;
}

function buildAdminStats() {
  const rows = buildAdminRows();
  return {
    serverTime: nowIso(),
    pool: getTestPoolStats(),
    ...summarizeRows(rows),
  };
}

function buildAdminLicenseView(typeFilter, queryText) {
  const allRows = buildAdminRows();
  const filtered = filterAdminRows(allRows, typeFilter, queryText);
  return {
    summary: summarizeRows(allRows),
    filteredSummary: summarizeRows(filtered),
    total: filtered.length,
    items: filtered.slice(0, 500),
  };
}

function buildAdminQueryResult(rawQuery) {
  const q = String(rawQuery || '').trim();
  const view = buildAdminLicenseView('ALL', q);
  const qHash = q ? hashPrivacy(q) : null;
  const revokedHit = !!(qHash && state.revoked.has(qHash));
  const revokedSample = Array.from(state.revoked.values()).filter((h) => !q || h.includes(String(q).toLowerCase())).slice(0, 50);
  return {
    query: q,
    queryHash: qHash,
    revokedHit,
    revokedSample,
    ...view,
  };
}

function adminPageHtml() {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>MARS License Admin</title>
  <style>
    body{font-family:Arial,sans-serif;margin:0;background:#f5f7fb;color:#1f2937}
    .login-wrap{max-width:420px;margin:80px auto;background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px}
    .login-wrap h1{font-size:20px;margin:0 0 8px}
    .login-wrap p{color:#6b7280;margin:0 0 12px}
    input,button,select{padding:8px;font-size:14px;border:1px solid #cbd5e1;border-radius:6px}
    button{cursor:pointer;background:#2563eb;color:#fff;border:none}
    button.secondary{background:#64748b}
    .top{display:flex;justify-content:space-between;align-items:center;padding:10px 14px;background:#fff;border-bottom:1px solid #dbe1ea}
    .layout{display:grid;grid-template-columns:55% 45%;gap:12px;padding:12px;height:calc(100vh - 52px);box-sizing:border-box}
    .panel{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:10px;overflow:auto}
    .row{display:flex;gap:8px;flex-wrap:wrap;align-items:center;margin-bottom:8px}
    .summary-grid{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:8px;margin-bottom:8px}
    .card{border:1px solid #e5e7eb;border-radius:8px;padding:8px;background:#f9fafb}
    .k{color:#6b7280;font-size:12px}
    .v{font-size:18px;font-weight:600}
    table{width:100%;border-collapse:collapse;font-size:13px}
    th,td{border-bottom:1px solid #eef2f7;padding:6px;text-align:left}
    tr:hover{background:#f8fafc}
    tr.active{background:#eaf2ff}
    pre{white-space:pre-wrap;background:#0b1220;color:#e5e7eb;padding:10px;border-radius:8px;min-height:280px}
    #app{display:none}
    #msg{color:#b91c1c;font-size:13px;min-height:18px}
  </style>
</head>
<body>
  <div id="login" class="login-wrap">
    <h1>Admin Login</h1>
    <p>Enter admin key to access license management.</p>
    <div class="row">
      <input id="loginKey" type="password" placeholder="x-admin-key" style="flex:1" />
      <button onclick="doLogin()">Login</button>
    </div>
    <div id="msg"></div>
  </div>

  <div id="app">
    <div class="top">
      <div><b>MARS License Admin</b></div>
      <div class="row" style="margin:0">
        <button class="secondary" onclick="refreshData()">Refresh</button>
        <button class="secondary" onclick="logout()">Logout</button>
      </div>
    </div>
    <div class="layout">
      <div class="panel">
        <div class="row">
          <input id="query" placeholder="query: email / customerRef / licenseId / claimId" style="flex:1;min-width:220px" />
          <select id="type">
            <option value="ALL">ALL</option>
            <option value="PAID">PAID</option>
            <option value="TEST">TEST</option>
            <option value="TRIAL_LIMITED">TRIAL_LIMITED</option>
          </select>
          <button onclick="refreshData()">Search</button>
        </div>
        <div class="summary-grid">
          <div class="card"><div class="k">Total</div><div class="v" id="sumTotal">0</div></div>
          <div class="card"><div class="k">PAID</div><div class="v" id="sumPaid">0</div></div>
          <div class="card"><div class="k">TEST</div><div class="v" id="sumTest">0</div></div>
          <div class="card"><div class="k">TRIAL</div><div class="v" id="sumTrial">0</div></div>
        </div>
        <table>
          <thead><tr><th>Type</th><th>Region</th><th>Email</th><th>Issued</th><th>ID</th></tr></thead>
          <tbody id="rows"></tbody>
        </table>
      </div>
      <div class="panel">
        <div class="row"><b>License Detail</b></div>
        <div class="row">
          <input id="renewDays" type="number" min="1" value="365" style="width:120px" />
          <button onclick="doAction('renew')">Renew</button>
          <button class="secondary" onclick="doAction('revoke')">Revoke</button>
          <span id="actionMsg" style="color:#b91c1c;font-size:12px"></span>
        </div>
        <pre id="detail">Select one row from the left list.</pre>
        <div class="row" style="margin-top:10px"><b>Stripe Orders</b></div>
        <div class="row">
          <select id="zelleStatus">
            <option value="ALL">ALL</option>
            <option value="CREATED">CREATED</option>
            <option value="PENDING_PAYMENT">PENDING_PAYMENT</option>
            <option value="PAID">PAID</option>
          </select>
          <input id="zelleQuery" placeholder="orderId/email/session/paymentIntent" style="flex:1;min-width:160px" />
          <button class="secondary" onclick="refreshZelle()">Refresh Stripe</button>
        </div>
        <div class="row">
          <input id="zelleNote" placeholder="admin note" style="flex:1;min-width:160px" />
          <button onclick="zelleAction('FINALIZE')">Finalize</button>
        </div>
        <div id="zelleMsg" style="font-size:12px;color:#b91c1c;min-height:16px"></div>
        <table>
          <thead><tr><th>Order</th><th>Status</th><th>Email</th><th>Updated</th></tr></thead>
          <tbody id="zelleRows"></tbody>
        </table>
      </div>
    </div>
  </div>

  <script>
    const KEY_STORE = 'mars_admin_key';
    const loginView = document.getElementById('login');
    const appView = document.getElementById('app');
    const loginKeyEl = document.getElementById('loginKey');
    const msgEl = document.getElementById('msg');
    const rowsEl = document.getElementById('rows');
    const detailEl = document.getElementById('detail');
    const actionMsgEl = document.getElementById('actionMsg');
    const renewDaysEl = document.getElementById('renewDays');
    const zelleRowsEl = document.getElementById('zelleRows');
    const zelleMsgEl = document.getElementById('zelleMsg');
    let adminKey = localStorage.getItem(KEY_STORE) || '';
    let currentItems = [];
    let selectedRowId = '';
    let zelleOrders = [];
    let selectedOrderId = '';

    function hdr(){ return {'x-admin-key': adminKey}; }
    function esc(s){ return String(s || '').replace(/[&<>"']/g, (ch) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch])); }
    function setSummary(sum){
      const byType = (sum && sum.byType) || {};
      document.getElementById('sumTotal').textContent = String(sum ? sum.total : 0);
      document.getElementById('sumPaid').textContent = String((byType.PAID && byType.PAID.count) || 0);
      document.getElementById('sumTest').textContent = String((byType.TEST && byType.TEST.count) || 0);
      document.getElementById('sumTrial').textContent = String((byType.TRIAL_LIMITED && byType.TRIAL_LIMITED.count) || 0);
    }
    function pickDetail(){
      const it = currentItems.find(x => x.rowId === selectedRowId);
      detailEl.textContent = it ? JSON.stringify(it, null, 2) : 'Select one row from the left list.';
    }
    function renderZelleRows(items){
      zelleOrders = Array.isArray(items) ? items : [];
      if (!selectedOrderId && zelleOrders.length) selectedOrderId = zelleOrders[0].orderId;
      zelleRowsEl.innerHTML = zelleOrders.map((it) => {
        const cls = it.orderId === selectedOrderId ? 'active' : '';
        return '<tr class="' + cls + '" data-orderid="' + esc(it.orderId) + '">' +
          '<td>' + esc(it.orderId) + '</td>' +
          '<td>' + esc(it.status || '-') + '</td>' +
          '<td>' + esc(it.customerEmail || '-') + '</td>' +
          '<td>' + esc(it.updatedAt || it.createdAt || '-') + '</td>' +
          '</tr>';
      }).join('');
      Array.from(zelleRowsEl.querySelectorAll('tr')).forEach((tr) => {
        tr.addEventListener('click', () => {
          selectedOrderId = tr.getAttribute('data-orderid') || '';
          renderZelleRows(zelleOrders);
        });
      });
    }
    function renderRows(items){
      currentItems = Array.isArray(items) ? items : [];
      if (!selectedRowId && currentItems.length) selectedRowId = currentItems[0].rowId;
      rowsEl.innerHTML = currentItems.map((it) => {
        const idText = it.licenseId || it.claimId || '';
        const cls = it.rowId === selectedRowId ? 'active' : '';
        return '<tr class="'+cls+'" data-id="'+esc(it.rowId)+'">' +
          '<td>'+esc(it.licenseType)+'</td>' +
          '<td>'+esc(it.region)+'</td>' +
          '<td>'+esc(it.customerEmail || '-')+'</td>' +
          '<td>'+esc(it.issuedAt || '-')+'</td>' +
          '<td>'+esc(idText)+'</td>' +
          '</tr>';
      }).join('');
      Array.from(rowsEl.querySelectorAll('tr')).forEach((tr) => {
        tr.addEventListener('click', () => {
          selectedRowId = tr.getAttribute('data-id') || '';
          renderRows(currentItems);
          pickDetail();
        });
      });
      pickDetail();
    }
    async function apiJson(url){
      const r = await fetch(url, {headers: hdr()});
      const j = await r.json();
      if (!r.ok || !j.ok) throw new Error((j && j.error) || ('http_' + r.status));
      return j;
    }
    async function apiJsonPost(url, body){
      const r = await fetch(url, {
        method:'POST',
        headers:{...hdr(), 'Content-Type':'application/json'},
        body:JSON.stringify(body || {})
      });
      const j = await r.json();
      if (!r.ok || !j.ok) throw new Error((j && j.error) || ('http_' + r.status));
      return j;
    }
    async function doLogin(){
      msgEl.textContent = '';
      adminKey = (loginKeyEl.value || '').trim();
      try{
        await fetch('/v1/admin/login', {
          method:'POST',
          headers:{'Content-Type':'application/json','x-admin-key':adminKey},
          body:JSON.stringify({adminKey})
        }).then(async (r) => {
          const j = await r.json();
          if (!r.ok || !j.ok) throw new Error((j && j.error) || ('http_' + r.status));
        });
        localStorage.setItem(KEY_STORE, adminKey);
        loginView.style.display = 'none';
        appView.style.display = 'block';
        await refreshData();
      } catch(e){
        msgEl.textContent = 'Login failed: ' + (e && e.message ? e.message : String(e));
      }
    }
    function logout(){
      adminKey = '';
      localStorage.removeItem(KEY_STORE);
      loginKeyEl.value = '';
      appView.style.display = 'none';
      loginView.style.display = 'block';
    }
    async function refreshData(){
      actionMsgEl.textContent = '';
      try{
        const q = encodeURIComponent(document.getElementById('query').value || '');
        const t = encodeURIComponent(document.getElementById('type').value || 'ALL');
        const data = await apiJson('/v1/admin/licenses?type=' + t + '&q=' + q);
        setSummary(data.summary);
        renderRows(data.items || []);
        await refreshZelle();
      }catch(e){
        msgEl.textContent = String(e && e.message ? e.message : e);
        if (String(msgEl.textContent).includes('unauthorized')) logout();
      }
    }
    async function refreshZelle(){
      zelleMsgEl.textContent = '';
      const status = encodeURIComponent(document.getElementById('zelleStatus').value || 'ALL');
      const q = encodeURIComponent(document.getElementById('zelleQuery').value || '');
      const data = await apiJson('/v1/admin/stripe/orders?status=' + status + '&q=' + q);
      renderZelleRows(data.items || []);
    }
    async function zelleAction(action){
      zelleMsgEl.textContent = '';
      if (!selectedOrderId) {
        zelleMsgEl.textContent = 'Please select a zelle order first.';
        return;
      }
      try {
        const reviewNote = document.getElementById('zelleNote').value || '';
        await apiJsonPost('/v1/admin/stripe/finalize', { orderId: selectedOrderId, action, reviewNote });
        zelleMsgEl.style.color = '#166534';
        zelleMsgEl.textContent = action + ' success';
        await refreshZelle();
        await refreshData();
      } catch (e) {
        zelleMsgEl.style.color = '#b91c1c';
        zelleMsgEl.textContent = 'Action failed: ' + (e && e.message ? e.message : String(e));
      }
    }
    async function doAction(action){
      actionMsgEl.textContent = '';
      if (!selectedRowId) {
        actionMsgEl.textContent = 'Please select a license row first.';
        return;
      }
      try{
        const days = Number.parseInt(String(renewDaysEl.value || '365'), 10);
        const res = await apiJsonPost('/v1/admin/license/action', { rowId: selectedRowId, action, days });
        actionMsgEl.style.color = '#166534';
        actionMsgEl.textContent = action.toUpperCase() + ' success';
        await refreshData();
        if (res && res.rowId) selectedRowId = res.rowId;
      }catch(e){
        actionMsgEl.style.color = '#b91c1c';
        actionMsgEl.textContent = 'Action failed: ' + (e && e.message ? e.message : String(e));
      }
    }
    loginKeyEl.value = adminKey;
    if (adminKey) doLogin();
  </script>
</body>
</html>`;
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

function parseRawBody(req) {
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
      chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
    });
    req.on('end', () => resolve(Buffer.concat(chunks)));
    req.on('error', reject);
  });
}

function isAdmin(req) {
  const provided = req.headers['x-admin-key'];
  return typeof provided === 'string' && timingSafeEqualText(provided, ADMIN_API_KEY);
}

function buildClientLicenseState(query) {
  const region = normalizeRegion(query.get('region'));
  const planRaw = String(query.get('plan') || '').toUpperCase();
  const licenseType = planRaw === 'PAID' ? 'PAID' : planRaw === 'TEST' ? 'TEST' : 'TRIAL_LIMITED';
  const now = new Date();
  const trialStartAt = query.get('trialStartAt') || now.toISOString();
  const trialDays = Number.parseInt(String(query.get('trialDays') || '7'), 10);
  const replayMax = Number.parseInt(String(query.get('replayMaxStepsAfterTrialDays') || '10'), 10);
  const expiresAt = query.get('expiresAt') || undefined;
  const message = query.get('message') || (
    region === 'US'
      ? 'Trial mode: after 7 days replay supports up to 10 steps. Upgrade price: $4.99.'
      : region === 'CN'
        ? '试用模式：7天后回放最多10步。升级价格：5元。'
        : 'Trial mode: after 7 days replay supports up to 10 steps.'
  );
  return {
    licenseType,
    region,
    trialStartAt,
    trialDays: Number.isFinite(trialDays) && trialDays > 0 ? trialDays : 7,
    replayMaxStepsAfterTrialDays: Number.isFinite(replayMax) && replayMax > 0 ? replayMax : 10,
    expiresAt,
    price: region === 'US' ? { currency: 'USD', amount: 4.99 } : region === 'CN' ? { currency: 'CNY', amount: 5 } : undefined,
    testPool: getTestPoolStats(),
    message,
  };
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

  if (req.method === 'GET' && url.pathname === '/admin') {
    sendHtml(res, 200, adminPageHtml());
    return;
  }

  if (req.method === 'GET' && url.pathname === '/zelle') {
    sendHtml(res, 200, zellePageHtml());
    return;
  }

  if (req.method === 'GET' && url.pathname === '/stripe') {
    sendHtml(res, 200, stripePageHtml());
    return;
  }

  if (req.method === 'GET' && url.pathname === '/stripe/success') {
    const sid = String(url.searchParams.get('session_id') || '').trim();
    const oid = String(url.searchParams.get('orderId') || '').trim();
    sendHtml(res, 200, stripeSuccessHtml(sid, oid));
    return;
  }

  if (req.method === 'GET' && url.pathname === '/stripe/cancel') {
    sendHtml(res, 200, '<html><body style="font-family:Arial,sans-serif;padding:20px"><h2>Payment canceled</h2><p>You can retry payment any time.</p></body></html>');
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/stripe/checkout/create') {
    try {
      const body = await parseBody(req);
      const created = createStripeOrder(body || {});
      if (!created.ok) {
        sendJson(res, 400, { ok: false, requestId, error: created.error || 'create_failed' });
        return;
      }
      const order = created.order;
      const session = await createStripeCheckoutForOrder(order);
      order.stripeSessionId = session.id || null;
      order.checkoutUrl = session.url || null;
      order.status = 'PENDING_PAYMENT';
      order.updatedAt = nowIso();
      const info = getStripeOrderByOrderId(order.orderId);
      if (info.idx >= 0) state.stripeOrders[info.idx] = order;
      persistStripeOrders();
      appendAudit({ action: 'stripe_checkout_create', ok: true, requestId, subjectHash: hashPrivacy(order.customerEmail || '') });
      sendJson(res, 200, {
        ok: true,
        requestId,
        order,
        checkoutUrl: order.checkoutUrl,
      });
    } catch (e) {
      appendAudit({ action: 'stripe_checkout_create', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/stripe/session/finalize') {
    try {
      const sessionId = String(url.searchParams.get('sessionId') || '').trim();
      const orderId = String(url.searchParams.get('orderId') || '').trim();
      let sessionToFinalize = sessionId;
      if (!sessionToFinalize && orderId) {
        const byOrder = getStripeOrderByOrderId(orderId);
        sessionToFinalize = byOrder.order ? String(byOrder.order.stripeSessionId || '').trim() : '';
      }
      if (!sessionToFinalize) {
        sendJson(res, 400, { ok: false, requestId, error: 'sessionId_or_orderId_required' });
        return;
      }
      const finalized = await finalizeStripeSessionBySessionId(sessionToFinalize, 'Finalized from success callback.');
      if (!finalized.ok) {
        sendJson(res, 409, { ok: false, requestId, error: finalized.error || 'finalize_failed' });
        return;
      }
      appendAudit({
        action: 'stripe_finalize',
        ok: true,
        requestId,
        subjectHash: hashPrivacy(finalized.order.customerEmail || ''),
      });
      sendJson(res, 200, { ok: true, requestId, reused: !!finalized.reused, order: finalized.order, license: finalized.order.license });
    } catch (e) {
      appendAudit({ action: 'stripe_finalize', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/stripe/order/license') {
    const orderId = String(url.searchParams.get('orderId') || '').trim();
    const email = String(url.searchParams.get('email') || '').trim().toLowerCase();
    const info = getStripeOrderByOrderId(orderId);
    if (!info.order) {
      sendJson(res, 404, { ok: false, requestId, error: 'order_not_found' });
      return;
    }
    const order = info.order;
    if (String(order.customerEmail || '').toLowerCase() !== email) {
      sendJson(res, 400, { ok: false, requestId, error: 'email_mismatch' });
      return;
    }
    if (!order.license) {
      if (String(order.status || '').toUpperCase() === 'PENDING_PAYMENT' && order.stripeSessionId) {
        try {
          const fin = await finalizeStripeSessionBySessionId(order.stripeSessionId, 'Lazy finalize on license fetch.');
          if (fin.ok) {
            sendJson(res, 200, { ok: true, requestId, orderId: fin.order.orderId, status: fin.order.status, license: fin.order.license });
            return;
          }
        } catch {
          // ignore and fall through
        }
      }
      sendJson(res, 409, { ok: false, requestId, error: 'license_not_ready', status: order.status });
      return;
    }
    sendJson(res, 200, { ok: true, requestId, orderId: order.orderId, status: order.status, license: order.license });
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/stripe/webhook') {
    try {
      const raw = await parseRawBody(req);
      const sig = req.headers['stripe-signature'];
      if (!verifyStripeWebhookSignature(raw, sig)) {
        sendJson(res, 401, { ok: false, requestId, error: 'invalid_webhook_signature' });
        return;
      }
      const event = JSON.parse(raw.toString('utf8'));
      const eventType = String(event.type || '');
      if (eventType === 'checkout.session.completed') {
        const sessionId = String(event.data?.object?.id || '').trim();
        if (sessionId) {
          const finalized = await finalizeStripeSessionBySessionId(sessionId, 'Finalized via Stripe webhook.');
          appendAudit({
            action: 'stripe_webhook_completed',
            ok: !!finalized.ok,
            requestId,
            subjectHash: hashPrivacy(finalized.order?.customerEmail || ''),
            reason: finalized.ok ? null : finalized.error,
          });
        }
      }
      sendJson(res, 200, { ok: true, requestId });
    } catch (e) {
      appendAudit({ action: 'stripe_webhook', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/zelle/order/create') {
    try {
      const body = await parseBody(req);
      const created = createZelleOrder(body || {});
      if (!created.ok) {
        sendJson(res, 400, { ok: false, requestId, error: created.error || 'create_failed' });
        return;
      }
      appendAudit({
        action: 'zelle_create',
        ok: true,
        requestId,
        subjectHash: hashPrivacy(created.order.customerEmail || ''),
      });
      sendJson(res, 200, {
        ok: true,
        requestId,
        receiver: {
          name: ZELLE_RECEIVER_NAME,
          account: ZELLE_RECEIVER_ACCOUNT,
        },
        order: created.order,
      });
    } catch (e) {
      appendAudit({ action: 'zelle_create', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/zelle/order/proof') {
    try {
      const body = await parseBody(req);
      const submitted = submitZelleProof(body || {});
      if (!submitted.ok) {
        sendJson(res, 400, { ok: false, requestId, error: submitted.error || 'proof_submit_failed' });
        return;
      }
      appendAudit({
        action: 'zelle_proof',
        ok: true,
        requestId,
        subjectHash: hashPrivacy(submitted.order.customerEmail || ''),
      });
      sendJson(res, 200, { ok: true, requestId, order: submitted.order, reused: !!submitted.reused });
    } catch (e) {
      appendAudit({ action: 'zelle_proof', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/zelle/order/status') {
    const orderId = String(url.searchParams.get('orderId') || '').trim();
    const email = String(url.searchParams.get('email') || '').trim();
    const status = getUserOrderStatus(orderId, email);
    if (!status.ok) {
      sendJson(res, 400, { ok: false, requestId, error: status.error || 'status_failed' });
      return;
    }
    sendJson(res, 200, { ok: true, requestId, ...status });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/zelle/order/license') {
    const orderId = String(url.searchParams.get('orderId') || '').trim();
    const email = String(url.searchParams.get('email') || '').trim().toLowerCase();
    const idx = state.zelleOrders.findIndex((x) => x && x.orderId === orderId);
    if (idx < 0) {
      sendJson(res, 404, { ok: false, requestId, error: 'order_not_found' });
      return;
    }
    const order = state.zelleOrders[idx];
    if (String(order.customerEmail || '').toLowerCase() !== email) {
      sendJson(res, 400, { ok: false, requestId, error: 'email_mismatch' });
      return;
    }
    if (order.status !== 'APPROVED' || !order.license) {
      sendJson(res, 409, { ok: false, requestId, error: 'license_not_ready', status: order.status });
      return;
    }
    sendJson(res, 200, {
      ok: true,
      requestId,
      orderId: order.orderId,
      status: order.status,
      license: order.license,
    });
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/admin/login') {
    try {
      const body = await parseBody(req);
      const fromHeader = typeof req.headers['x-admin-key'] === 'string' ? req.headers['x-admin-key'] : '';
      const fromBody = body && typeof body.adminKey === 'string' ? body.adminKey : '';
      const key = fromHeader || fromBody;
      if (!isAdminKeyText(key)) {
        sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
        return;
      }
      sendJson(res, 200, { ok: true, requestId, profile: { role: 'admin', loginAt: nowIso() } });
    } catch (e) {
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/admin/stats') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    sendJson(res, 200, { ok: true, requestId, stats: buildAdminStats() });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/admin/licenses') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    const type = url.searchParams.get('type') || 'ALL';
    const q = url.searchParams.get('q') || '';
    const view = buildAdminLicenseView(type, q);
    sendJson(res, 200, { ok: true, requestId, ...view });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/admin/stripe/orders') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    const status = url.searchParams.get('status') || 'ALL';
    const q = url.searchParams.get('q') || '';
    const items = listStripeOrders(status, q);
    sendJson(res, 200, { ok: true, requestId, total: items.length, items });
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/admin/stripe/finalize') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const orderId = String(body.orderId || '').trim();
      const orderInfo = getStripeOrderByOrderId(orderId);
      if (!orderInfo.order) {
        sendJson(res, 404, { ok: false, requestId, error: 'order_not_found' });
        return;
      }
      const sessionId = String(orderInfo.order.stripeSessionId || '').trim();
      if (!sessionId) {
        sendJson(res, 400, { ok: false, requestId, error: 'missing_stripe_session' });
        return;
      }
      const finalized = await finalizeStripeSessionBySessionId(sessionId, String(body.reviewNote || '').trim() || 'Finalized by admin.');
      if (!finalized.ok) {
        sendJson(res, 409, { ok: false, requestId, error: finalized.error || 'finalize_failed' });
        return;
      }
      appendAudit({
        action: 'stripe_admin_finalize',
        ok: true,
        requestId,
        subjectHash: hashPrivacy(finalized.order.customerEmail || ''),
      });
      sendJson(res, 200, { ok: true, requestId, reused: !!finalized.reused, order: finalized.order });
    } catch (e) {
      appendAudit({ action: 'stripe_admin_finalize', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/admin/license/action') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const result = applyAdminLicenseAction(body || {});
      if (!result.ok) {
        sendJson(res, 400, { ok: false, requestId, error: result.error || 'action_failed' });
        return;
      }
      appendAudit({
        action: `admin_${String(body.action || '').toLowerCase()}`,
        ok: true,
        requestId,
        subjectHash: hashPrivacy(body.rowId || ''),
      });
      sendJson(res, 200, { ok: true, requestId, ...result });
    } catch (e) {
      appendAudit({ action: 'admin_action', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/admin/revocations') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    const items = Array.from(state.revoked.values()).slice(0, 2000);
    sendJson(res, 200, { ok: true, requestId, total: state.revoked.size, items });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/admin/query') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    const result = buildAdminQueryResult(url.searchParams.get('q'));
    sendJson(res, 200, { ok: true, requestId, result });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/license/client-state') {
    const statePayload = buildClientLicenseState(url.searchParams);
    sendJson(res, 200, {
      ok: true,
      requestId,
      state: statePayload,
      declarationUrl: '/v1/license/declaration',
      policyUrl: '/v1/license/policy',
    });
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/license/policy') {
    sendJson(res, 200, {
      ok: true,
      requestId,
      policy: {
        trialDays: 7,
        replayMaxStepsAfterTrialDays: 10,
        paidPriceUS: 4.99,
        paidPriceCN: 5,
        currencyUS: 'USD',
        currencyCN: 'CNY',
        testPoolPerRegion: TEST_REGION_LIMIT,
        testPoolRegions: ['US', 'CN'],
      },
      testPool: getTestPoolStats(),
    });
    return;
  }

  if (req.method === 'POST' && url.pathname === '/v1/license/test/claim') {
    if (!isAdmin(req)) {
      sendJson(res, 401, { ok: false, requestId, error: 'unauthorized' });
      return;
    }
    try {
      const body = await parseBody(req);
      const claimed = claimTestLicense(body || {});
      appendAudit({
        action: 'test_claim',
        ok: !!claimed.ok,
        requestId,
        subjectHash: hashPrivacy(body.customerEmail || body.customerRef || ''),
        reason: claimed.reason || null,
      });
      if (!claimed.ok) {
        const code = claimed.reason === 'test_pool_exhausted' ? 409 : 400;
        sendJson(res, code, { ok: false, requestId, error: claimed.reason, testPool: claimed.stats || getTestPoolStats() });
        return;
      }
      sendJson(res, 200, { ok: true, requestId, ...claimed });
    } catch (e) {
      appendAudit({ action: 'test_claim', ok: false, requestId, reason: e.message });
      sendJson(res, 400, { ok: false, requestId, error: e.message });
    }
    return;
  }

  if (req.method === 'GET' && url.pathname === '/v1/license/declaration') {
    const lang = String(url.searchParams.get('lang') || 'en').toLowerCase();
    const text = lang.startsWith('zh') ? DECLARATION_ZH : DECLARATION_EN;
    sendJson(res, 200, {
      ok: true,
      requestId,
      lang: lang.startsWith('zh') ? 'zh' : 'en',
      declaration: text,
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
      upsertIssuedLicenseRecord(body || {}, license);
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
