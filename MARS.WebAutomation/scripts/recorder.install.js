(() => {
  if (window.__marsDomRecorder) return;
  window.__marsDomRecorder = true;
  if (window === window.top) {
    window.__marsRecoMode = window.__marsRecoMode || 'off';
    window.__marsRecoCaptureMode = window.__marsRecoCaptureMode || 'semantic';
  }

  if (window === window.top && !window.__marsRecorderHeartbeat) {
    console.log('[MARS Recorder] heartbeat timer started');
    window.__marsRecorderHeartbeat = window.setInterval(() => {
      console.log('[MARS Recorder] heartbeat - engine alive -', new Date().toISOString());
    }, 60 * 1000);
  }

  function currentRecoMode() {
    try {
      if (window.top && window.top !== window) {
        const m = window.top.__marsRecoMode;
        if (typeof m === 'string' && m.length) return m;
      }
    } catch (_) {}
    return window.__marsRecoMode || 'off';
  }

  function currentCaptureMode() {
    try {
      if (window.top && window.top !== window) {
        const m = window.top.__marsRecoCaptureMode;
        if (typeof m === 'string' && m.length) return lower(m);
      }
    } catch (_) {}
    const m = window.__marsRecoCaptureMode;
    if (typeof m === 'string' && m.length) return lower(m);
    return 'semantic';
  }

  function buildPlaywrightSnippetForPayload(payload, sourceEvent) {
    const loc = str(payload.Locator || '').trim();
    if (!loc) return '';
    const pwSel = loc.indexOf('//') === 0 || loc.indexOf('(/') === 0 ? 'xpath=' + loc : loc;
    const base = 'await page.locator(' + JSON.stringify(pwSel) + ')';
    const se = lower(sourceEvent || '');
    const tag = lower(payload.Tag || '');
    const typ = lower(payload.TypeAttr || '');
    if (se === 'click' || se === 'mousedown') return base + '.click();';
    if (se === 'change') {
      if (tag === 'select') {
        const lab = str(payload.Value || payload.Text || '').trim();
        return base + '.selectOption({ label: ' + JSON.stringify(lab) + ' });';
      }
      if (typ === 'checkbox' || typ === 'radio') return base + '.setChecked(' + (payload.Checked ? 'true' : 'false') + ');';
      return base + '.fill(' + JSON.stringify(str(payload.Value ?? '')) + ');';
    }
    if (se === 'blur' || se === 'input') {
      const v = str(payload.Value != null ? payload.Value : payload.Text || '');
      return base + '.fill(' + JSON.stringify(v) + ');';
    }
    return base + '.click();';
  }

  // Semantic rules/config can be injected by host:
  // window.__marsRecoSemanticConfig = {...}
  function semanticCfg() {
    try {
      const c = window.__marsRecoSemanticConfig;
      if (c && typeof c === 'object') return c;
    } catch (_) {}
    return {};
  }

  function asArray(v) {
    return Array.isArray(v) ? v : [];
  }

  function str(v) {
    return v == null ? '' : String(v);
  }

  function lower(v) {
    return str(v).toLowerCase();
  }

  function getRulePriority(rule) {
    const p = rule && typeof rule.priority === 'number' ? Math.floor(rule.priority) : 0;
    return Number.isFinite(p) ? p : 0;
  }

  function matchesByMethod(actual, expected, method) {
    const a = str(actual);
    const e = str(expected);
    const m = lower(method || 'equal');
    if (m === 'equal') return lower(a) === lower(e);
    if (m === 'include') return lower(a).indexOf(lower(e)) >= 0;
    if (m === 'regex' || m === 'regular') {
      try {
        return new RegExp(e, 'i').test(a);
      } catch (_) {
        return false;
      }
    }
    return false;
  }

  function getElementProperty(el, prop) {
    if (!el || !prop) return '';
    const p = lower(prop);
    if (p === 'class') return str(el.className || '');
    if (p === 'role') return str(el.getAttribute && el.getAttribute('role'));
    if (p === 'id') return str(el.id || '');
    if (p === 'text' || p === 'textpreview') return normalizeSpace(el.innerText || el.textContent || '');
    return str(el.getAttribute && el.getAttribute(prop));
  }

  function matchesSemanticRule(el, rule) {
    if (!el || !rule) return false;
    const tagNeed = lower(rule.htmlTag || '*');
    if (tagNeed !== '*' && lower(el.tagName) !== tagNeed) return false;
    const classIncludes = asArray(rule.classIncludes || rule.classIndex);
    for (let i = 0; i < classIncludes.length; i++) {
      if (!hasClassToken(el, classIncludes[i])) return false;
    }
    const props = asArray(rule.properties);
    for (let i = 0; i < props.length; i++) {
      const pr = props[i] || {};
      const actual = getElementProperty(el, pr.property);
      if (!matchesByMethod(actual, pr.value, pr.method)) return false;
    }
    return true;
  }

  function keywordForObjectType(objectType) {
    const cfg = semanticCfg();
    const km = asArray(cfg.keywordMapping);
    for (let i = 0; i < km.length; i++) {
      const m = km[i] || {};
      if (lower(m.objectType) === lower(objectType) && m.keyword) return String(m.keyword);
    }
    if (lower(objectType) === 'webtab') return 'SelectTab';
    if (lower(objectType) === 'webmenu') return 'SelectMenuItem';
    if (lower(objectType) === 'webbutton') return 'ClickButton';
    return '';
  }

  function buildPropertySourceValue(name, source, semEl, clickedEl, childTargetEl) {
    const s = lower(source);
    if (!s) return '';
    if (s === 'self.class') return str(semEl && semEl.className);
    if (s === 'self.id') return str(semEl && semEl.id);
    if (s === 'self.idpath') return buildWebIdPath(semEl);
    if (s === 'self.xpath') return buildXPath(semEl);
    if (s === 'const:a') return 'a';
    if (s === 'selforchild.innertext') {
      const t = normalizeSpace((clickedEl && (clickedEl.innerText || clickedEl.textContent)) || '');
      return t;
    }
    if (s === 'children:a.innertext') {
      const t = normalizeSpace((childTargetEl && (childTargetEl.innerText || childTargetEl.textContent)) || '');
      return t;
    }
    if (s === 'self.innertext') return normalizeSpace((semEl && (semEl.innerText || semEl.textContent)) || '');
    if (s.indexOf('self.attr:') === 0) {
      const attr = source.substring('self.attr:'.length);
      return str(semEl && semEl.getAttribute && semEl.getAttribute(attr));
    }
    return '';
  }

  function applyPropertyMappings(payload, rule, semEl, clickedEl, childTargetEl) {
    const pms = asArray(rule && rule.propertyMappings);
    for (let i = 0; i < pms.length; i++) {
      const pm = pms[i] || {};
      const name = str(pm.name);
      if (!name) continue;
      const v = buildPropertySourceValue(name, pm.source, semEl, clickedEl, childTargetEl);
      if (!v && pm.optional) continue;
      payload[name] = v;
    }
  }

  function hasRequiredProperties(payload, rule) {
    const req = asArray(rule && rule.requiredProperties);
    for (let i = 0; i < req.length; i++) {
      const key = str(req[i]);
      if (!key) continue;
      if (payload[key] == null || str(payload[key]).trim() === '') return false;
    }
    return true;
  }

  function esc(s) {
    if (s == null) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
  }

  function isTextLikeInput(el) {
    if (!el) return false;
    const tag = String(el.tagName || '').toLowerCase();
    if (tag !== 'input') return false;
    const t = (el.getAttribute('type') || 'text').toLowerCase();
    return (
      t === '' ||
      t === 'text' ||
      t === 'password' ||
      t === 'search' ||
      t === 'email' ||
      t === 'tel' ||
      t === 'url' ||
      t === 'number' ||
      t === 'date' ||
      t === 'time' ||
      t === 'datetime-local' ||
      t === 'month' ||
      t === 'week'
    );
  }

  function fileNamesValue(el) {
    if (!el || !el.files || !el.files.length) return '';
    try {
      const arr = [];
      for (let i = 0; i < el.files.length; i++) {
        const f = el.files[i];
        if (f && f.name) arr.push(String(f.name));
      }
      return arr.join(';');
    } catch (_) {
      return '';
    }
  }

  function isContentEditableSurface(el) {
    if (!el || el.nodeType !== 1) return false;
    if (el.isContentEditable) return true;
    const v = (el.getAttribute('contenteditable') || '').toLowerCase();
    return v === 'true' || v === '';
  }

  const __marsValueCache = new WeakMap();

  function readControlValue(el) {
    if (!el || el.nodeType !== 1) return '';
    try {
      const tag = String(el.tagName || '').toLowerCase();
      if (tag === 'input' || tag === 'textarea' || tag === 'select') {
        if (el.value != null) return String(el.value);
      }
    } catch (_) {}
    return '';
  }

  function cacheControlValue(el) {
    if (!el || el.nodeType !== 1) return;
    try {
      const v = readControlValue(el);
      if (v !== '') __marsValueCache.set(el, v);
    } catch (_) {}
  }

  function cachedControlValue(el) {
    if (!el || el.nodeType !== 1) return '';
    try {
      const v = __marsValueCache.get(el);
      return v == null ? '' : String(v);
    } catch (_) {
      return '';
    }
  }

  function idMatchCount(id) {
    if (!id) return 0;
    try {
      if (typeof CSS !== 'undefined' && CSS.escape) return document.querySelectorAll('#' + CSS.escape(String(id))).length;
    } catch (_) {}
    try {
      return document.querySelectorAll('[id="' + esc(id) + '"]').length;
    } catch (_) {
      return -1;
    }
  }

  function countCss(sel) {
    try {
      return document.querySelectorAll(sel).length;
    } catch (_) {
      return -1;
    }
  }

  function looksDynamicId(id) {
    if (!id || typeof id !== 'string') return true;
    const s = String(id);
    const sl = s.toLowerCase();
    if (/^(rowid|cellid|__)/.test(sl) || /^(cellid|rowid)$/i.test(s)) return true;
    if (/(uuid|guid|react-|mui-|radix-|headlessui|ngb-)/i.test(s)) return true;
    if (/^:r[0-9a-z]+$/i.test(s)) return true;
    if (/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(s)) return true;
    if (/[-_]\d{5,}$/.test(s)) return true;
    if (/[-.](row|cell|item|idx|key)[-_]?\d+/i.test(s)) return true;
    return false;
  }

  function looksDynamicName(name) {
    if (!name || typeof name !== 'string') return true;
    const sl = String(name).toLowerCase();
    if (/^(rowid|cellid|rowkey|cellkey)$/.test(sl)) return true;
    if (/_\d{5,}$/.test(sl)) return true;
    return false;
  }

  function xpathLiteral(s) {
    if (s == null || s === '') return "''";
    const str = String(s);
    if (!str.includes("'")) return "'" + str.replace(/\\/g, '\\\\') + "'";
    return 'concat(' + str.split("'").map((p) => "'" + p.replace(/\\/g, '\\\\') + "'").join(", \"'\", ") + ')';
  }

  function xpathCount(nodesetExpr) {
    try {
      const r = document.evaluate('count(' + nodesetExpr + ')', document, null, XPathResult.NUMBER_TYPE, null);
      return typeof r.numberValue === 'number' ? r.numberValue : -1;
    } catch (_) {
      return -1;
    }
  }

  function siblingSameTagIndex(el) {
    const parent = el.parentElement;
    if (!parent) return 1;
    const tn = (el.tagName || 'DIV').toUpperCase();
    let idx = 1;
    for (let s = el.previousElementSibling; s; s = s.previousElementSibling) {
      if ((s.tagName || '').toUpperCase() === tn) idx++;
    }
    return idx;
  }

  function ktAttributeIndicatesMenu(el) {
    if (!el || !el.attributes) return false;
    for (let i = 0; i < el.attributes.length; i++) {
      const name = String(el.attributes[i].name || '').toLowerCase();
      const val = String(el.attributes[i].value || '').toLowerCase();
      if (name === 'data-ktmenu' || name === 'data-kt-menu') return true;
      if (/^data-kt[-_]*menu/.test(name)) return true;
      if (name.indexOf('kt-') === 0 && name.indexOf('menu') >= 0) return true;
      if (name.indexOf('kt') >= 0 && val.indexOf('nav') >= 0) return true;
    }
    return false;
  }

  function isKtLikeMenuItemLi(el) {
    if (!el || el.nodeType !== 1) return false;
    if (String(el.tagName || '').toLowerCase() !== 'li') return false;
    let cls = '';
    try {
      cls = ((el.className && String(el.className)) || '').toLowerCase();
    } catch (_) {
      cls = '';
    }
    if (cls.indexOf('kt-menu__item') >= 0) return true;
    if (cls.indexOf('kt_menu') >= 0) return true;
    if (cls.indexOf('kt-menu') >= 0 && cls.indexOf('item') >= 0) return true;
    if (ktAttributeIndicatesMenu(el)) return true;
    return false;
  }

  function normalizeSpace(s) {
    return String(s || '')
      .replace(/\s+/g, ' ')
      .trim();
  }

  function pickStableClassToken(el) {
    const raw = (el.className && String(el.className)) || '';
    const parts = raw.split(/\s+/).filter(Boolean);
    for (let i = 0; i < parts.length; i++) {
      const p = parts[i];
      const pl = p.toLowerCase();
      if (pl.length < 4) continue;
      if (/^(ng-|js-|is-|css-|aria|v-|x-)/.test(pl)) continue;
      if (/^(active|selected|hover|focus|disabled|show|open|collapsed)$/.test(pl)) continue;
      if (pl.indexOf('kt-menu') >= 0 || pl.indexOf('menu') >= 0) return p;
    }
    for (let j = 0; j < parts.length; j++) {
      const p2 = parts[j];
      if (p2.length >= 6) return p2;
    }
    return '';
  }

  function tryXPathTagClassText(el) {
    const tag = (el.tagName || 'div').toLowerCase();
    const tok = pickStableClassToken(el);
    const t = normalizeSpace(el.innerText || el.textContent || '');
    if (!tok || t.length < 1) return '';
    const sub = t.length > 40 ? t.substring(0, 40) : t;
    const xp =
      '//' + tag + '[contains(@class,' + xpathLiteral(tok) + ')][contains(normalize-space(.),' + xpathLiteral(sub) + ')]';
    if (xpathCount(xp) === 1) return xp;
    return '';
  }

  function tryXPathClassTagUnique(el) {
    const tag = (el.tagName || 'div').toLowerCase();
    const tok = pickStableClassToken(el);
    if (!tok) return '';
    const xp = '//' + tag + '[contains(@class,' + xpathLiteral(tok) + ')]';
    if (xpathCount(xp) === 1) return xp;
    return '';
  }

  function tryXPathTextUnique(el) {
    const t = normalizeSpace(el.innerText || el.textContent || '');
    if (t.length < 2) return '';
    const sub = t.length > 48 ? t.substring(0, 48) : t;
    const lit = xpathLiteral(sub);
    const tag = (el.tagName || '*').toLowerCase();
    const xp1 = '//' + tag + '[contains(normalize-space(.),' + lit + ')]';
    if (xpathCount(xp1) === 1) return xp1;
    const xp2 = '//*[contains(normalize-space(.),' + lit + ')]';
    if (xpathCount(xp2) === 1) return xp2;
    return '';
  }

  function buildFullAbsoluteXPath(el) {
    if (!el || el.nodeType !== 1) return '';
    const segs = [];
    let c = el;
    for (let d = 0; d < 28 && c && c.nodeType === 1; d++) {
      const tag = (c.tagName || 'div').toLowerCase();
      if (tag === 'html') {
        segs.unshift('html');
        break;
      }
      const idx = siblingSameTagIndex(c);
      segs.unshift(tag + '[' + idx + ']');
      c = c.parentElement;
    }
    if (!segs.length) return '';
    return '/' + segs.join('/');
  }

  function xpathSegmentFrom(el) {
    const tn = (el.tagName || 'div').toLowerCase();
    const nm = el.getAttribute('name');
    const parent = el.parentElement;
    if (nm && parent && !looksDynamicName(nm)) {
      try {
        const matches = parent.querySelectorAll(':scope > ' + tn + '[name="' + esc(nm) + '"]');
        if (matches.length === 1 && matches[0] === el) return tn + '[@name=' + xpathLiteral(nm) + ']';
      } catch (_) {}
    }
    const typ = (el.getAttribute('type') || '').toLowerCase();
    if (tn === 'input' && typ && parent) {
      try {
        const matches = parent.querySelectorAll(':scope > input[type="' + esc(typ) + '"]');
        if (matches.length === 1 && matches[0] === el) return 'input[@type=' + xpathLiteral(typ) + ']';
      } catch (_) {}
    }
    const al = el.getAttribute('aria-label');
    const role = (el.getAttribute('role') || '').toLowerCase();
    if (al && parent && role) {
      try {
        const matches = parent.querySelectorAll(':scope > *' + '[role="' + esc(role) + '"][aria-label="' + esc(al) + '"]');
        if (matches.length === 1 && matches[0] === el)
          return '*[@role=' + xpathLiteral(role) + '][@aria-label=' + xpathLiteral(al) + ']';
      } catch (_) {}
    }
    return tn + '[' + siblingSameTagIndex(el) + ']';
  }

  function buildShortXPath(el) {
    if (!el || el.nodeType !== 1) return '';
    try {
      const idv = el.id || '';
      if (idv && !looksDynamicId(idv)) {
        const xp = '//*[@id=' + xpathLiteral(idv) + ']';
        if (xpathCount(xp) === 1) return xp;
      }
      const tid = el.getAttribute && el.getAttribute('data-testid');
      if (tid) {
        const xp = '//*[@data-testid=' + xpathLiteral(tid) + ']';
        if (xpathCount(xp) === 1) return xp;
      }
      let hit = tryXPathTagClassText(el);
      if (hit) return hit;
      hit = tryXPathClassTagUnique(el);
      if (hit) return hit;
      hit = tryXPathTextUnique(el);
      if (hit) return hit;
      const segs = [];
      let c = el;
      for (let d = 0; d < 6 && c && c.nodeType === 1; d++) {
        const tag = (c.tagName || '').toLowerCase();
        if (tag === 'body' || tag === 'html') break;
        segs.unshift(xpathSegmentFrom(c));
        c = c.parentElement;
      }
      if (segs.length) {
        const bodyPath = '//body/' + segs.join('/');
        if (xpathCount(bodyPath) === 1) return bodyPath;
      }
      const abs = buildFullAbsoluteXPath(el);
      if (abs && xpathCount(abs) === 1) return abs;
      if (segs.length) return '//body/' + segs.join('/');
      return abs || '';
    } catch (_) {
      return '';
    }
  }

  function tryCompoundUnique(el, baseSel) {
    if (!el || !baseSel || countCss(baseSel) === 1) return baseSel;
    if (countCss(baseSel) < 1) return '';
    const attrs = [
      ['data-col', el.getAttribute('data-col')],
      ['data-row-index', el.getAttribute('data-row-index')],
      ['data-testid', el.getAttribute('data-testid')],
      ['name', el.getAttribute('name')],
      ['type', el.getAttribute('type')],
      ['aria-label', el.getAttribute('aria-label')],
      ['placeholder', el.getAttribute('placeholder')],
      ['title', el.getAttribute('title')],
    ];
    let s = baseSel;
    for (let i = 0; i < attrs.length; i++) {
      const an = attrs[i][0];
      const av = attrs[i][1];
      if (!av || String(av).length > 160) continue;
      const piece = '[' + an + '="' + esc(String(av)) + '"]';
      const next = s + piece;
      const n = countCss(next);
      if (n === 1) return next;
      if (n === 0) continue;
      s = next;
    }
    return countCss(s) === 1 ? s : '';
  }

  function collectUniqueCssStrategies(el) {
    const out = [];
    const seen = Object.create(null);
    const push = (s) => {
      if (!s || s.length < 2) return;
      if (seen[s]) return;
      const n = countCss(s);
      if (n !== 1) return;
      seen[s] = 1;
      out.push(s);
    };

    const tid = el.getAttribute && el.getAttribute('data-testid');
    if (tid) push('[data-testid="' + esc(tid) + '"]');

    const idv = el.id || '';
    if (idv) {
      if (!looksDynamicId(idv) && idMatchCount(idv) === 1) push('[id="' + esc(idv) + '"]');
      else if (idMatchCount(idv) >= 1) {
        const base = '[id="' + esc(idv) + '"]';
        const comp = tryCompoundUnique(el, base);
        if (comp) push(comp);
      }
    }

    const tag = (el.tagName || 'div').toLowerCase();
    const nm = el.getAttribute('name');
    if (nm && (tag === 'input' || tag === 'select' || tag === 'textarea') && !looksDynamicName(nm)) {
      const s = tag + '[name="' + esc(nm) + '"]';
      if (countCss(s) === 1) push(s);
    }

    const role = el.getAttribute('role');
    const aria = el.getAttribute('aria-label');
    if (role && aria) {
      const s = '[role="' + esc(role) + '"][aria-label="' + esc(aria) + '"]';
      if (countCss(s) === 1) push(s);
    }

    const maxDepth = 8;
    const parts = [];
    let cur = el;
    for (let d = 0; d < maxDepth && cur && cur.nodeType === 1 && cur !== document.documentElement; d++) {
      const tcur = (cur.tagName || 'div').toLowerCase();
      const parent = cur.parentElement;
      if (!parent) {
        parts.unshift(tcur);
        break;
      }
      const siblings = [].filter.call(parent.children, (c) => c.tagName === cur.tagName);
      const idx = siblings.indexOf(cur) + 1;
      parts.unshift(tcur + ':nth-of-type(' + idx + ')');
      cur = parent;
    }
    let chain = parts.join(' > ');
    if (chain.length > 420) chain = chain.slice(0, 417) + '...';
    if (chain) push(chain);

    return out;
  }

  /** Playwright .NET expects the xpath engine prefix for XPath selector strings. */
  function toPlaywrightXpathSelector(xp) {
    const s = (xp || '').trim();
    if (!s) return '';
    const sl = s.toLowerCase();
    if (sl.indexOf('xpath=') === 0) return s;
    if (s.indexOf('//') === 0 || s.indexOf('(/') === 0) return 'xpath=' + s;
    return s;
  }

  function isUsableXPathExpression(xp) {
    const s = (xp || '').trim();
    if (s.length < 3) return false;
    const sl = s.toLowerCase();
    if (sl.indexOf('xpath=') === 0) return sl.length > 6;
    return s.indexOf('//') === 0 || s.indexOf('(/') === 0;
  }

  function buildMultiLocator(el) {
    if (!el || el.nodeType !== 1) return { primary: '', alternates: [], shortXPath: '' };
    const strategies = collectUniqueCssStrategies(el);
    const cssPrimary = strategies[0] || '';
    const cssRest = strategies.slice(1, 5);
    const shortXPathRaw = buildShortXPath(el);
    const shortXPath = (shortXPathRaw || '').trim();

    if (isUsableXPathExpression(shortXPathRaw)) {
      const primary = toPlaywrightXpathSelector(shortXPathRaw);
      const alternates = [];
      if (cssPrimary) alternates.push(cssPrimary);
      for (let i = 0; i < cssRest.length && alternates.length < 5; i++) {
        if (cssRest[i]) alternates.push(cssRest[i]);
      }
      return { primary, alternates, shortXPath };
    }

    const primary = cssPrimary;
    const alternates = cssRest;
    return { primary, alternates, shortXPath };
  }

  /** Primary locator for Playwright (XPath preferred when buildShortXPath yields a match). */
  function buildLocator(el) {
    return buildMultiLocator(el).primary;
  }

  /** Bootstrap / Ant / Element / MUI-style button skins on anchors and other non-button tags. */
  function classLooksLikeButton(el) {
    const cls = ((el && el.className && String(el.className)) || '').toLowerCase();
    if (!cls) return false;
    if (cls.indexOf('btn-') >= 0 || cls.indexOf(' btn') >= 0 || cls.indexOf('btn ') >= 0 || cls === 'btn') return true;
    if (cls.indexOf('btn') >= 0) return true;
    if (cls.indexOf('button') >= 0) return true;
    if (cls.indexOf('mat-button') >= 0 || cls.indexOf('mdc-button') >= 0) return true;
    if (cls.indexOf('ant-btn') >= 0 || cls.indexOf('el-button') >= 0) return true;
    return false;
  }

  function inferLogicalKind(el) {
    if (!el || el.nodeType !== 1) return 'webUnknown';
    const tag = (el.tagName || '').toLowerCase();
    const type = (el.getAttribute('type') || '').toLowerCase();
    const role = (el.getAttribute('role') || '').toLowerCase();
    const cls = (el.className && String(el.className)) || '';
    const hasPopup = el.getAttribute('aria-haspopup');

    if (tag === 'input' && type === 'checkbox') return 'webCheckbox';
    if (tag === 'input' && type === 'radio') return 'webRadio';
    if (role === 'checkbox' || role === 'switch') return 'webCheckbox';
    if (role === 'radio') return 'webRadio';
    if (tag === 'table') return 'webTable';
    if (role === 'grid' || role === 'table') return 'webTable';
    if (tag === 'select' || role === 'combobox' || role === 'listbox') return 'webCombobox';
    if (role === 'menu' || role === 'menubar' || role === 'menuitem' || role === 'menuitemcheckbox' || role === 'menuitemradio')
      return 'webMenu';
    if (hasPopup === 'true' || hasPopup === 'menu' || hasPopup === 'listbox') return 'webMenu';
    if (tag === 'li' && isKtLikeMenuItemLi(el)) return 'webMenu';

    if (role === 'tab' || (cls.toLowerCase().indexOf('tab') >= 0 && (role === 'tab' || el.getAttribute('data-tab'))))
      return 'webTab';

    if (tag === 'button' || type === 'button' || type === 'submit' || type === 'reset' || role === 'button')
      return 'webButton';

    // Link / chip styled as a button — must run before role=textbox heuristics (some themes mis-label anchors).
    if (['a', 'span', 'div', 'label', 'li', 'i', 'svg'].indexOf(tag) >= 0 && classLooksLikeButton(el)) return 'webButton';

    // Truly editable controls only (not plain anchors / list items with bogus ARIA).
    if (tag === 'textarea' || (tag === 'input' && isTextLikeInput(el)) || isContentEditableSurface(el)) return 'webEdit';
    if ((role === 'textbox' || role === 'searchbox') && tag !== 'a' && tag !== 'li') return 'webEdit';

    if (tag === 'a' && el.getAttribute('href')) return 'webButton';
    if (tag === 'label') return 'webButton';

    return 'webUnknown';
  }

  function rectOf(el) {
    try {
      const r = el.getBoundingClientRect();
      return { X: r.x, Y: r.y, Width: r.width, Height: r.height };
    } catch {
      return null;
    }
  }

  function tableHint(el) {
    let cur = el;
    for (let i = 0; i < 8 && cur; i++, cur = cur.parentElement) {
      if (!cur.tagName) continue;
      const tag = cur.tagName.toLowerCase();
      if (tag === 'table') return 'table';
      const cls = (cur.className && String(cur.className)) || '';
      if (cls.indexOf('pq-grid') >= 0) return 'webtable:pq_grid';
      if (cls.indexOf('ag-root') >= 0 || cls.indexOf('MuiDataGrid') >= 0)
        return 'webtable:' + cls.split(' ')[0];
    }
    return '';
  }

  function tabContextDepth() {
    const n = window.__marsRecoTabAncestorDepth;
    return typeof n === 'number' && n > 0 ? Math.min(12, Math.floor(n)) : 5;
  }

  function clsLower(el) {
    return ((el.className && String(el.className)) || '').toLowerCase();
  }

  function isUnderTabPanelContent(clickEl) {
    try {
      if (clickEl.closest && clickEl.closest('[role="tabpanel"]')) {
        if (!clickEl.closest('[role="tablist"]') && !(clickEl.closest && clickEl.closest('.el-tabs__header')))
          return true;
      }
      if (clickEl.closest && clickEl.closest('.el-tabs__content')) {
        if (!(clickEl.closest && clickEl.closest('.el-tabs__header'))) return true;
      }
    } catch (_) {}
    return false;
  }

  function resolveTabStrip(clickEl) {
    let strip = null;
    try {
      strip = clickEl.closest('[role="tablist"]');
    } catch (_) {}
    if (strip) return strip;
    const hdr = clickEl.closest ? clickEl.closest('.el-tabs__header') : null;
    if (hdr) return hdr.closest('.el-tabs') || hdr;
    const max = tabContextDepth();
    let cur = clickEl;
    for (let d = 0; d < max && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      const cl = clsLower(cur);
      if (cl.indexOf('tabs') >= 0) return cur;
    }
    return null;
  }

  function findTabItemForClick(clickEl, tabStrip) {
    const max = tabContextDepth();
    let best = null;
    let cur = clickEl;
    for (let d = 0; d < max && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      if (!tabStrip.contains(cur)) break;
      const r = (cur.getAttribute('role') || '').toLowerCase();
      if (r === 'tab') best = cur;
    }
    if (best) return best;
    if (clickEl.closest) {
      const elItem = clickEl.closest('.el-tabs__item');
      if (elItem && tabStrip.contains(elItem)) return elItem;
    }
    const tnm = (clickEl.tagName || '').toUpperCase();
    if (tabStrip.contains(clickEl) && (tnm === 'BUTTON' || tnm === 'A' || tnm === 'SPAN' || tnm === 'LI' || tnm === 'DIV'))
      return clickEl;
    return null;
  }

  function tabLabelFromClick(semanticEl, clickEl) {
    let t = (clickEl.innerText || clickEl.textContent || '').trim();
    if (t) return t.substring(0, 500);
    const sp = clickEl.querySelector && clickEl.querySelector('span');
    if (sp) {
      t = (sp.innerText || sp.textContent || '').trim();
      if (t) return t.substring(0, 500);
    }
    t = (semanticEl.innerText || semanticEl.textContent || '').trim();
    return t.substring(0, 500);
  }

  function buildXPath(el) {
    return buildMultiLocator(el).shortXPath;
  }

  function trySelectTabContext(clickEl, tag, role) {
    const r0 = (role || '').toLowerCase();
    if (isUnderTabPanelContent(clickEl) && r0 !== 'tab') return null;
    const tabStrip = resolveTabStrip(clickEl);
    if (!tabStrip || !tabStrip.contains(clickEl)) return null;
    const tabItem = findTabItemForClick(clickEl, tabStrip);
    if (!tabItem) return null;
    const tabPack = buildMultiLocator(tabItem);
    const clickPack = buildMultiLocator(clickEl);
    const tabLabel = tabLabelFromClick(tabItem, clickEl);
    return {
      tabLocator: tabPack.primary,
      tabLabel,
      actualTag: tag,
      actualRole: role,
      targetCssLocator: clickPack.primary,
      targetXpath: clickPack.shortXPath
    };
  }

  function hasClassToken(el, token) {
    if (!el || !token) return false;
    try {
      const cls = ((el.className && String(el.className)) || '').toLowerCase();
      return cls.indexOf(String(token).toLowerCase()) >= 0;
    } catch (_) {
      return false;
    }
  }

  function isStopParentNode(el, cfg) {
    const stopTags = asArray(cfg.stopParentTags).map((x) => lower(x));
    if (stopTags.indexOf(lower(el && el.tagName)) >= 0) return true;
    const stopTypes = asArray(cfg.stopParentObjectTypes).map((x) => lower(x));
    if (!stopTypes.length) return false;
    const inferred = lower(inferLogicalKind(el));
    return stopTypes.indexOf(inferred) >= 0;
  }

  function findBestRuleMatchOnElement(el, rules) {
    let best = null;
    for (let i = 0; i < rules.length; i++) {
      const r = rules[i] || {};
      if (!matchesSemanticRule(el, r)) continue;
      if (!best || getRulePriority(r) > getRulePriority(best.rule)) best = { element: el, rule: r };
    }
    return best;
  }

  function closestAncestorMatchingTag(el, tagNeed, maxDepth) {
    if (!el || !tagNeed || lower(tagNeed) === '*') return null;
    const want = lower(tagNeed);
    let cur = el;
    for (let d = 0; d < maxDepth && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      if (!cur.tagName) continue;
      if (lower(cur.tagName) === want) return cur;
    }
    return null;
  }

  /** When semantic config targets e.g. <a>, resolve that host from clicks on SPAN/I/etc. inside it. */
  function resolveSemanticHostElement(clickedEl, cfg) {
    if (!clickedEl || clickedEl.nodeType !== 1) return null;
    const rawT = cfg && cfg.targetHtmlTag;
    const targetTag = rawT == null || String(rawT).trim() === '' ? 'a' : lower(String(rawT));
    if (targetTag === '*') return clickedEl;
    if (lower(clickedEl.tagName) === targetTag) return clickedEl;
    const maxUp =
      typeof (cfg && cfg.semanticHostAncestorDepth) === 'number'
        ? Math.max(1, Math.min(32, Math.floor(cfg.semanticHostAncestorDepth)))
        : 16;
    return closestAncestorMatchingTag(clickedEl.parentElement, targetTag, maxUp);
  }

  function findSemanticByRules(clickedEl) {
    const cfg = semanticCfg();
    const host = resolveSemanticHostElement(clickedEl, cfg);
    if (!host) return null;

    const selfRules = asArray(cfg.selfSemanticRules);
    const parentRules = asArray(cfg.parentsSemanticRules);
    const selfHit = findBestRuleMatchOnElement(host, selfRules);
    if (selfHit) return { element: selfHit.element, rule: selfHit.rule, origin: 'self-rule' };

    const maxDepth = typeof cfg.maxParentDepth === 'number' ? Math.max(1, Math.min(20, Math.floor(cfg.maxParentDepth))) : 5;
    let cur = host.parentElement;
    for (let d = 0; d < maxDepth && cur; d++, cur = cur.parentElement) {
      if (isStopParentNode(cur, cfg)) break;
      const hit = findBestRuleMatchOnElement(cur, parentRules);
      if (hit) return { element: hit.element, rule: hit.rule, origin: 'parent-rule' };
    }

    // semi: when no parent (or self) semantic rule matched, use resolved host (e.g. <a>) as semantic object.
    const tst = lower(cfg.targetSemanticType || '');
    if (tst === 'semi') {
      const ot = str(cfg.semiDefaultObjectType || inferLogicalKind(host));
      return {
        element: host,
        rule: {
          objectType: ot,
          semanticType: 'semi',
          propertyMappings: asArray(cfg.semiSelfPropertyMappings),
          requiredProperties: asArray(cfg.semiSelfRequiredProperties)
        },
        origin: 'semi-self'
      };
    }
    return null;
  }

  function findChildTargetByRules(semEl, clickedEl) {
    const cfg = semanticCfg();
    const rules = cfg.childrenTargetRules || {};
    const tag = lower(rules.tag || 'a');
    if (!semEl || !semEl.querySelectorAll) return { ok: false, error: 'semanticElementMissing' };
    const all = semEl.querySelectorAll(tag);
    if (!all || all.length === 0) return { ok: false, error: 'noChildTag', tag };

    const textSource = lower(rules.textSource || 'selforchild.innertext');
    const clickedText = normalizeSpace((clickedEl && (clickedEl.innerText || clickedEl.textContent)) || '');
    const mode = lower(rules.textMatchMode || 'exact');
    const allowRegex = !!rules.allowRegex;
    const want = clickedText;
    const hits = [];
    let rx = null;
    if (mode === 'regex' && allowRegex) {
      try {
        rx = new RegExp(want, 'i');
      } catch (_) {
        return { ok: false, error: 'invalidRegex', want };
      }
    }
    for (let i = 0; i < all.length; i++) {
      const c = all[i];
      const tv = textSource === 'children:a.innertext'
        ? normalizeSpace(c.innerText || c.textContent || '')
        : normalizeSpace(c.innerText || c.textContent || '');
      if (mode === 'regex' && rx) {
        if (rx.test(tv)) hits.push(c);
      } else if (tv === want) {
        hits.push(c);
      }
    }
    const mustUnique = rules.mustBeUnique !== false;
    if (!mustUnique && hits.length > 0) return { ok: true, el: hits[0] };
    if (hits.length === 1) return { ok: true, el: hits[0] };
    if (hits.length === 0) return { ok: false, error: 'noChildMatch', want };
    return { ok: false, error: 'ambiguousChildMatch', count: hits.length, want };
  }

  function buildWebIdPath(el) {
    if (!el || el.nodeType !== 1) return '';
    const parts = [];
    let cur = el;
    for (let d = 0; d < 8 && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      const idv = cur.id ? String(cur.id) : '';
      if (idv && !looksDynamicId(idv)) parts.unshift(idv);
    }
    return parts.join('>');
  }

  function roleLower(el) {
    return ((el && el.getAttribute && el.getAttribute('role')) || '').toLowerCase();
  }

  function isMenuItemElement(el) {
    if (!el || el.nodeType !== 1) return false;
    if (isKtLikeMenuItemLi(el)) return true;
    const r = roleLower(el);
    if (r === 'menuitem' || r === 'menuitemcheckbox' || r === 'menuitemradio') return true;
    const cls = clsLower(el);
    return cls.indexOf('menuitem') >= 0 || cls.indexOf('menu-item') >= 0 || cls.indexOf('el-menu-item') >= 0;
  }

  function menuItemHasSubmenu(menuItem) {
    if (!menuItem) return false;
    const popup = ((menuItem.getAttribute && menuItem.getAttribute('aria-haspopup')) || '').toLowerCase();
    if (popup === 'true' || popup === 'menu' || popup === 'listbox') return true;
    if (menuItem.querySelector && menuItem.querySelector('[role="menu"]')) return true;
    const cls = clsLower(menuItem);
    return cls.indexOf('submenu') >= 0 || cls.indexOf('has-sub') >= 0;
  }

  /** True if this menu row includes a real navigation link (do not suppress click recording for submenu heuristics). */
  function hasRealNavHrefInMenuItem(menuItem) {
    if (!menuItem || menuItem.nodeType !== 1) return false;
    try {
      if (lower(menuItem.tagName) === 'a') {
        const h0 = (menuItem.getAttribute('href') || '').trim();
        if (h0 && h0 !== '#' && lower(h0).indexOf('javascript:') !== 0) return true;
      }
      const links = menuItem.querySelectorAll ? menuItem.querySelectorAll('a[href]') : [];
      for (let i = 0; i < links.length; i++) {
        const h = (links[i].getAttribute('href') || '').trim();
        if (h && h !== '#' && lower(h).indexOf('javascript:') !== 0) return true;
      }
    } catch (_) {}
    return false;
  }

  function isActionableMenuControl(el) {
    if (!el || el.nodeType !== 1) return false;
    const tag = lower(el.tagName);
    if (tag !== 'a' && tag !== 'button') return false;
    const oc = (el.getAttribute && el.getAttribute('onclick')) || '';
    if (oc && String(oc).trim()) return true;
    const dpj = el.getAttribute && el.getAttribute('data-ts-pj-id');
    if (dpj && String(dpj).trim()) return true;
    const href = (el.getAttribute('href') || '').trim();
    if (href && href !== '#' && lower(href).indexOf('javascript:') === 0 && href.length > 12) return true;
    if (el.classList && el.classList.contains('kt-menu__link')) return true;
    return false;
  }

  function findActionableMenuAnchor(menuItem) {
    if (!menuItem || menuItem.nodeType !== 1) return null;
    try {
      if (isActionableMenuControl(menuItem)) return menuItem;
      const links = menuItem.querySelectorAll
        ? menuItem.querySelectorAll('a.kt-menu__link, a[data-ts-pj-id], a[onclick], button[onclick]')
        : [];
      for (let i = 0; i < links.length; i++) {
        if (isActionableMenuControl(links[i])) return links[i];
      }
    } catch (_) {}
    return null;
  }

  function findActionableMenuClickTarget(clickEl, menuItem) {
    if (!clickEl || !menuItem || !menuItem.contains) return null;
    let cur = clickEl;
    for (let d = 0; d < 10 && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      if (!menuItem.contains(cur)) break;
      if (isActionableMenuControl(cur)) return cur;
    }
    return findActionableMenuAnchor(menuItem);
  }

  /** onclick-driven menu entries (e.g. KT GetTestSuite) — record as click, not skip. */
  function menuItemHasActionableOnClick(menuItem) {
    return !!findActionableMenuAnchor(menuItem);
  }

  function menuLabel(el) {
    if (!el || el.nodeType !== 1) return '';
    let t = (el.innerText || el.textContent || '').trim().replace(/\s+/g, ' ');
    if (!t && el.querySelector) {
      const s = el.querySelector('span');
      if (s) t = (s.innerText || s.textContent || '').trim().replace(/\s+/g, ' ');
    }
    return t.substring(0, 200);
  }

  function findNearestSelectAncestor(el) {
    if (!el || !el.closest) return null;
    const s = el.closest('select');
    if (s) return s;
    return el.closest('[role="combobox"],[role="listbox"]');
  }

  function findSemanticTableAncestor(el) {
    if (!el || !el.closest) return null;
    const t = el.closest('table,[role="grid"],[role="table"]');
    if (t) return t;
    let cur = el;
    for (let i = 0; i < 28 && cur; i++, cur = cur.parentElement) {
      if (!cur || cur.nodeType !== 1) break;
      const cls = (cur.className && String(cur.className)) || '';
      if (cls.indexOf('pq-grid') >= 0 || cls.indexOf('ag-root') >= 0 || cls.indexOf('MuiDataGrid') >= 0) return cur;
    }
    return null;
  }

  /** Button under a select: walk up — table/grid contains select → webTable; else webSelect + SelectDropDown on select. */
  function tryButtonSelectTableSemantics(clickEl, tag, type, role) {
    const isBtn =
      tag === 'button' ||
      (tag === 'input' && (type === 'button' || type === 'submit' || type === 'reset')) ||
      role === 'button';
    if (!isBtn) return null;
    const sel = findNearestSelectAncestor(clickEl);
    if (!sel) return null;
    const tbl = findSemanticTableAncestor(clickEl);
    if (tbl && tbl.contains(sel)) return { kind: 'webTable' };
    return { kind: 'webSelect', selectEl: sel };
  }

  function isTableSemanticControl(tag, type, role) {
    const t = (tag || '').toLowerCase();
    const tp = (type || '').toLowerCase();
    const r = (role || '').toLowerCase();
    if (t === 'label' || t === 'select' || t === 'button') return true;
    if (t === 'input') {
      if (tp === 'checkbox' || tp === 'radio' || tp === 'button' || tp === 'submit' || tp === 'reset') return true;
      if (isTextLikeInput({ tagName: 'INPUT', getAttribute: (n) => (n === 'type' ? tp : '') })) return true;
      return true;
    }
    if (r === 'checkbox' || r === 'radio' || r === 'combobox' || r === 'button' || r === 'textbox' || r === 'searchbox') return true;
    return false;
  }

  function rowNumberFromIndex(rowIdx, totalRows) {
    if (typeof rowIdx !== 'number' || rowIdx < 0) return 1;
    if (typeof totalRows === 'number' && totalRows > 0) {
      if (rowIdx === totalRows - 1) return -1;
      if (rowIdx === totalRows - 2) return -2;
    }
    return rowIdx + 1;
  }

  function isPqGridElement(el) {
    return !!(el && hasClassToken(el, 'pq-grid'));
  }

  function findPqGridAncestor(el) {
    if (!el || !el.closest) return null;
    const direct = el.closest('.pq-grid');
    if (direct) return direct;
    let cur = el;
    for (let i = 0; i < 28 && cur; i++, cur = cur.parentElement) {
      if (isPqGridElement(cur)) return cur;
    }
    return null;
  }

  function firstAttr(el, names) {
    if (!el || !el.getAttribute) return '';
    for (let i = 0; i < names.length; i++) {
      const v = el.getAttribute(names[i]);
      if (v != null && String(v).trim() !== '') return String(v).trim();
    }
    return '';
  }

  function parseIntAttr(el, names, oneBased) {
    const raw = firstAttr(el, names);
    if (!/^-?\d+$/.test(raw)) return -1;
    const n = parseInt(raw, 10);
    return oneBased ? n - 1 : n;
  }

  function formatGridToken(v) {
    return normalizeSpace(v || '').replace(/[;\[\]]+/g, ' ').trim();
  }

  function cellDisplayText(cell) {
    if (!cell) return '';
    try {
      const ctl = cell.querySelector && cell.querySelector('input,textarea,select,[contenteditable="true"]');
      if (ctl) {
        const tag = lower(ctl.tagName);
        const typ = lower(ctl.getAttribute && ctl.getAttribute('type'));
        if (tag === 'select' && ctl.options) {
          const idx = typeof ctl.selectedIndex === 'number' ? ctl.selectedIndex : -1;
          if (idx >= 0 && ctl.options[idx]) return normalizeSpace(ctl.options[idx].text || ctl.value || '');
        }
        if (typ === 'checkbox' || typ === 'radio') return ctl.checked ? 'true' : 'false';
        if (ctl.value != null) return normalizeSpace(ctl.value);
      }
    } catch (_) {}
    return normalizeSpace(cell.innerText || cell.textContent || '');
  }

  function flattenPqColModel(cols, out) {
    if (!Array.isArray(cols)) return out;
    for (let i = 0; i < cols.length; i++) {
      const c = cols[i] || {};
      if (Array.isArray(c.colModel) && c.colModel.length) flattenPqColModel(c.colModel, out);
      else out.push(c);
    }
    return out;
  }

  function readPqColModel(grid) {
    try {
      const jq = window.jQuery || window.$;
      if (!jq || !jq.fn || !jq.fn.pqGrid) return [];
      const cm = jq(grid).pqGrid('option', 'colModel');
      return flattenPqColModel(cm, []);
    } catch (_) {
      return [];
    }
  }

  function pqColumnIndex(cell) {
    let idx = parseIntAttr(cell, ['pq-col-indx', 'data-col-indx', 'data-col-index', 'data-column-index'], false);
    if (idx >= 0) return idx;
    idx = parseIntAttr(cell, ['aria-colindex'], true);
    if (idx >= 0) return idx;
    if (typeof cell.cellIndex === 'number' && cell.cellIndex >= 0) return cell.cellIndex;
    return -1;
  }

  function pqColumnName(grid, cell, colIdx) {
    const direct = firstAttr(cell, ['data-field', 'data-col', 'col-id', 'data-col-id', 'data-key']);
    if (direct && !/^-?\d+$/.test(direct)) return direct;
    const cm = readPqColModel(grid);
    if (colIdx >= 0 && colIdx < cm.length) {
      const c = cm[colIdx] || {};
      const n = c.dataIndx || c.title || c.name || c.label;
      if (n != null && String(n).trim()) return String(n).trim();
    }
    try {
      const hdr = grid.querySelector(
        '.pq-grid-col[pq-col-indx="' +
          colIdx +
          '"],.pq-grid-title-row [pq-col-indx="' +
          colIdx +
          '"],[role="columnheader"][aria-colindex="' +
          (colIdx + 1) +
          '"]'
      );
      const ht = hdr ? normalizeSpace(hdr.innerText || hdr.textContent || '') : '';
      if (ht) return ht;
    } catch (_) {}
    return colIdx >= 0 ? 'c' + (colIdx + 1) : 'Auto';
  }

  function pqRowCells(grid, cell) {
    const row = cell.closest ? cell.closest('.pq-grid-row,[role="row"],[pq-row-indx],[data-row-index]') : null;
    if (row) {
      const cells = row.querySelectorAll('.pq-grid-cell,[role="gridcell"],[role="cell"],[pq-col-indx]');
      if (cells && cells.length) return [].slice.call(cells);
    }
    const rowIdx = firstAttr(cell, ['pq-row-indx', 'data-row-indx', 'data-row-index', 'aria-rowindex']);
    if (rowIdx && grid && grid.querySelectorAll) {
      try {
        const cells = grid.querySelectorAll(
          '.pq-grid-cell[pq-row-indx="' +
            rowIdx +
            '"],[role="gridcell"][aria-rowindex="' +
            rowIdx +
            '"],[role="cell"][aria-rowindex="' +
            rowIdx +
            '"]'
        );
        if (cells && cells.length) return [].slice.call(cells);
      } catch (_) {}
    }
    return [cell];
  }

  function buildPqGridCellContext(grid, cell) {
    if (!grid || !cell) return null;
    const tblPack = buildMultiLocator(grid);
    if (!tblPack.primary) return null;
    const clickedColIdx = pqColumnIndex(cell);
    const clickedColName = formatGridToken(pqColumnName(grid, cell, clickedColIdx));
    const clickedValue = formatGridToken(cellDisplayText(cell));
    const cols = [];
    const data = [];
    const cells = pqRowCells(grid, cell);
    for (let i = 0; i < cells.length; i++) {
      const c = cells[i];
      if (!c || c === cell) continue;
      const ci = pqColumnIndex(c);
      if (ci === clickedColIdx) continue;
      const cv = formatGridToken(cellDisplayText(c));
      if (!cv) continue;
      const cn = formatGridToken(pqColumnName(grid, c, ci));
      if (!cn) continue;
      cols.push(cn);
      data.push(cv);
    }
    return {
      tableLocator: tblPack.primary,
      logicalKind: 'webTable',
      tableContext: 'webtable:pq_grid',
      parameter: '[ConditionCols:' + cols.join(';') + '];columnName:' + clickedColName,
      data: '[' + data.join(';') + ']:' + clickedValue
    };
  }

  function tryResolveTableCellContext(el, tag, type, role) {
    const cellSelector = 'td,th,[role="gridcell"],[role="cell"],.pq-grid-cell,.ag-cell,.MuiDataGrid-cell,[data-col]';
    const hasCell = !!(el && el.closest && el.closest(cellSelector));
    if (!el || (!isTableSemanticControl(tag, type, role) && !hasCell)) return null;
    const cell = el.closest
      ? el.closest(cellSelector)
      : null;
    if (!cell) return null;
    const pqGrid = findPqGridAncestor(cell) || findPqGridAncestor(el);
    if (pqGrid) return buildPqGridCellContext(pqGrid, cell);
    const tbl = findSemanticTableAncestor(cell) || findSemanticTableAncestor(el);
    if (!tbl) return null;
    const tblPack = buildMultiLocator(tbl);
    if (!tblPack.primary) return null;

    let colName = '';
    let rowNumber = 1;

    const dataCol = cell.getAttribute ? (cell.getAttribute('data-col') || cell.getAttribute('col-id') || cell.getAttribute('data-field')) : '';
    if (dataCol) colName = String(dataCol).trim();

    if (cell.matches && (cell.matches('td') || cell.matches('th'))) {
      const tr = cell.closest('tr');
      if (tr) {
        const rowContainer = tr.parentElement || tr.closest('table');
        const rows = rowContainer ? rowContainer.querySelectorAll(':scope > tr') : null;
        const rowIdx = rows ? [].indexOf.call(rows, tr) : -1;
        rowNumber = rowNumberFromIndex(rowIdx, rows ? rows.length : 0);
      }
      if (!colName) {
        const ci = typeof cell.cellIndex === 'number' ? cell.cellIndex : -1;
        if (ci >= 0) {
          const table = cell.closest('table');
          if (table) {
            let ths = table.querySelectorAll('thead th');
            if (!ths || !ths.length) ths = table.querySelectorAll('th');
            if (ths && ci < ths.length) colName = (ths[ci].innerText || ths[ci].textContent || '').trim();
          }
          if (!colName) colName = 'c' + (ci + 1);
        }
      }
    } else {
      const row = cell.closest ? cell.closest('[role="row"],.pq-grid-row,.ag-row,.MuiDataGrid-row,[data-row-index]') : null;
      if (row) {
        let idx = -1;
        const dr = row.getAttribute ? row.getAttribute('data-row-index') : '';
        if (dr && /^-?\d+$/.test(dr)) idx = parseInt(dr, 10);
        if (idx < 0) {
          const parent = row.parentElement;
          const rows = parent ? parent.querySelectorAll(':scope > [role="row"], :scope > .pq-grid-row, :scope > .ag-row, :scope > .MuiDataGrid-row, :scope > [data-row-index]') : null;
          idx = rows ? [].indexOf.call(rows, row) : -1;
          rowNumber = rowNumberFromIndex(idx, rows ? rows.length : 0);
        } else {
          rowNumber = idx + 1;
        }
      }
      if (!colName) {
        const ac = cell.getAttribute ? (cell.getAttribute('aria-colindex') || '') : '';
        if (ac && /^\d+$/.test(ac)) colName = 'c' + ac;
      }
    }

    if (!colName) colName = 'Auto';
    let controlType = 'input';
    if (tag === 'select' || role === 'combobox' || role === 'listbox') controlType = 'combobox';
    else if (tag === 'input' && type === 'checkbox') controlType = 'checkbox';
    else if (tag === 'input' && type === 'radio') controlType = 'radio';
    else if (tag === 'label') controlType = 'label';
    else if (tag === 'button' || role === 'button') controlType = 'button';
    else if (tag === 'input' || tag === 'textarea' || role === 'textbox' || role === 'searchbox') controlType = 'text';
    return {
      tableLocator: tblPack.primary,
      logicalKind: 'webTable',
      parameter: 'col:' + colName + ';row:' + String(rowNumber) + ';' + controlType
    };
  }

  function trySelectMenuContext(clickEl) {
    if (!clickEl || !clickEl.closest) return null;
    let menuItem = clickEl.closest(
      '[role="menuitem"],[role="menuitemcheckbox"],[role="menuitemradio"],.menu-item,.menuitem,.el-menu-item,li.kt-menu__item,li[class*="kt-menu__item"],li[data-ktmenu],li[data-kt-menu]'
    );
    if (!menuItem) {
      let cur = clickEl;
      for (let d = 0; d < 14 && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
        if (isKtLikeMenuItemLi(cur)) {
          menuItem = cur;
          break;
        }
      }
    }
    if (!menuItem || !isMenuItemElement(menuItem)) return null;
    if (menuItemHasSubmenu(menuItem)) {
      if (findActionableMenuClickTarget(clickEl, menuItem)) return null;
      if (!hasRealNavHrefInMenuItem(menuItem) && !menuItemHasActionableOnClick(menuItem))
        return { skipRecord: true, menuElement: menuItem };
      return null;
    }

    const chain = [];
    let cur = menuItem;
    for (let i = 0; i < 8 && cur && cur.nodeType === 1; i++, cur = cur.parentElement) {
      if (isMenuItemElement(cur)) {
        const lbl = menuLabel(cur);
        if (lbl) chain.unshift(lbl);
      }
      if (roleLower(cur) === 'menubar') break;
    }
    const path = chain.join(';');
    const menuPack = buildMultiLocator(menuItem);
    const clickPack = buildMultiLocator(clickEl);
    return {
      menuPath: path || menuLabel(menuItem),
      menuLocator: menuPack.primary,
      menuElement: menuItem,
      actualTag: (clickEl.tagName || '').toLowerCase(),
      actualRole: roleLower(clickEl),
      targetCssLocator: clickPack.primary,
      targetXpath: clickPack.shortXPath
    };
  }

  function committedTextValue(el) {
    if (!el || el.nodeType !== 1) return '';
    if (isContentEditableSurface(el)) return (el.innerText || el.textContent || '').trim().substring(0, 4000);
    if (el.value != null) {
      const v = String(el.value);
      if (v) return v;
    }
    const av = el.getAttribute ? (el.getAttribute('value') || '') : '';
    if (av) return String(av);
    const txt = (el.innerText || el.textContent || '').trim();
    if (txt) return txt.substring(0, 4000);
    return '';
  }

  function resolveTextSemanticElement(el) {
    if (!el || el.nodeType !== 1) return null;
    const role = (el.getAttribute('role') || '').toLowerCase();
    const tag = (el.tagName || '').toLowerCase();
    if (
      tag === 'textarea' ||
      (tag === 'input' && isTextLikeInput(el)) ||
      role === 'textbox' ||
      role === 'searchbox' ||
      isContentEditableSurface(el)
    ) {
      return el;
    }
    const up = el.closest
      ? el.closest('textarea,input,[role="textbox"],[role="searchbox"],[contenteditable="true"]')
      : null;
    if (up && up.nodeType === 1) return up;
    const down = el.querySelector
      ? el.querySelector('textarea,input,[role="textbox"],[role="searchbox"],[contenteditable="true"]')
      : null;
    if (down && down.nodeType === 1) return down;
    return null;
  }

  function pushPayload(payload) {
    try {
      if (window.marsRecorderPush) window.marsRecorderPush(payload);
    } catch {}
  }

  function topWin() {
    try { return window.top || window; } catch (_) { return window; }
  }

  function toggleSig(locator, checked) {
    return String(locator || '') + '|' + (checked ? '1' : '0');
  }

  function shouldSkipToggleDuplicate(locator, checked) {
    const t = topWin();
    const now = Date.now();
    const sig = toggleSig(locator, checked);
    const lastSig = t.__marsRecoLastToggleSig || '';
    const lastTs = typeof t.__marsRecoLastToggleTs === 'number' ? t.__marsRecoLastToggleTs : 0;
    if (lastSig === sig && now - lastTs < 280) return true;
    t.__marsRecoLastToggleSig = sig;
    t.__marsRecoLastToggleTs = now;
    return false;
  }

  function shouldSkipTabDuplicate(sig) {
    const t = topWin();
    const now = Date.now();
    const key = String(sig || '');
    const lastSig = t.__marsRecoLastTabSig || '';
    const lastTs = typeof t.__marsRecoLastTabTs === 'number' ? t.__marsRecoLastTabTs : 0;
    if (lastSig === key && now - lastTs < 420) return true;
    t.__marsRecoLastTabSig = key;
    t.__marsRecoLastTabTs = now;
    return false;
  }

  /** Many custom dropdowns open on mousedown and suppress click; recorder used to drop those entirely. */
  function likelyInsideRecordedMenuSurface(el) {
    if (!el || !el.closest) return false;
    if (
      el.closest(
        '[role="menuitem"],[role="menuitemcheckbox"],[role="menuitemradio"],.menu-item,.menuitem,.el-menu-item,li.kt-menu__item,li[class*="kt-menu__item"],li[data-ktmenu],li[data-kt-menu]'
      )
    )
      return true;
    let cur = el;
    for (let d = 0; d < 14 && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
      if (isKtLikeMenuItemLi(cur)) return true;
    }
    return false;
  }

  function shouldRecordMouseDownWhenClickMayBeLost(tag, role, el) {
    const t = lower(tag);
    const r = lower(role || '');
    if (
      t === 'div' ||
      t === 'span' ||
      t === 'p' ||
      t === 'label' ||
      t === 'li' ||
      t === 'img' ||
      t === 'i' ||
      t === 'svg' ||
      t === 'button'
    )
      return true;
    if (r === 'button' || r === 'combobox' || r === 'listbox') return true;
    if (el && el.getAttribute) {
      const hp = lower(el.getAttribute('aria-haspopup') || '');
      if (hp && hp !== 'false') return true;
    }
    return false;
  }

  function dedupeMouseRecordSig(p) {
    return String((p && p.Locator) || '') + '|' + String((p && p.PageUrl) || '') + '|' + String((p && p.RecorderKeyword) || '');
  }

  function shouldSkipDuplicateClickAfterMouseDown(payload) {
    if (currentRecoMode() !== 'record' || !payload || lower(payload.Kind) !== 'record') return false;
    if (lower(payload.SourceEvent) !== 'click') return false;
    const tw = topWin();
    const last = tw.__marsRecoMdEmit;
    if (!last || typeof last.ts !== 'number') return false;
    if (Date.now() - last.ts > 480) return false;
    return last.sig === dedupeMouseRecordSig(payload);
  }

  function noteMouseDownRecordEmit(payload) {
    const tw = topWin();
    tw.__marsRecoMdEmit = { ts: Date.now(), sig: dedupeMouseRecordSig(payload) };
  }

  function clearMouseDownRecordEmit() {
    try {
      const tw = topWin();
      tw.__marsRecoMdEmit = null;
    } catch (_) {}
  }

  function normalizeEventTarget(t) {
    if (!t) return null;
    if (t.nodeType === 1) return t;
    try {
      if (t.parentElement && t.parentElement.nodeType === 1) return t.parentElement;
      if (t.parentNode && t.parentNode.nodeType === 1) return t.parentNode;
    } catch (_) {}
    return null;
  }

  function framePathFromTop() {
    try {
      if (window === window.top) return '';
      const idx = [];
      let cur = window;
      for (let guard = 0; guard < 16 && cur && cur !== cur.top; guard++) {
        const p = cur.parent;
        if (!p || p === cur) break;
        let found = -1;
        try {
          for (let i = 0; i < p.frames.length; i++) {
            if (p.frames[i] === cur) {
              found = i;
              break;
            }
          }
        } catch (_) {
          found = -1;
        }
        if (found < 0) break;
        idx.unshift(String(found));
        cur = p;
      }
      return idx.join('/');
    } catch (_) {
      return '';
    }
  }

  function currentContextMeta() {
    const asIFrame = window !== window.top;
    let pageUrl = '';
    try {
      pageUrl = asIFrame ? String(window.top.location.href || '') : String(location.href || '');
    } catch (_) {
      pageUrl = String(location.href || '');
    }
    const frameUrl = String((typeof location !== 'undefined' && location.href) || '');
    const title = String(document.title || '');
    const text = typeof window.name === 'string' ? window.name : '';
    const framePath = asIFrame ? framePathFromTop() : '';
    const sig = (asIFrame ? 'F' : 'W') + '|' + pageUrl + '|' + frameUrl + '|' + framePath;
    return { asIFrame, pageUrl, frameUrl, title, text, framePath, sig };
  }

  function emitPegwindowIfContextChanged(mode) {
    if (mode !== 'record') return null;
    const m = currentContextMeta();
    let last = '';
    try {
      if (window.top && typeof window.top.__marsRecoLastContextSig === 'string') last = window.top.__marsRecoLastContextSig;
    } catch (_) {
      last = window.__marsRecoLastContextSig || '';
    }
    if (last === m.sig) return m;
    try {
      if (window.top) window.top.__marsRecoLastContextSig = m.sig;
    } catch (_) {
      window.__marsRecoLastContextSig = m.sig;
    }
    const parameter =
      'ASIFrame=' +
      (m.asIFrame ? 'true' : 'false') +
      ';PageUrl=' +
      m.pageUrl +
      ';FrameUrl=' +
      m.frameUrl +
      ';Title=' +
      m.title +
      ';Text=' +
      m.text +
      ';FramePath=' +
      m.framePath;
    pushPayload({
      Kind: 'record',
      SourceEvent: 'switchcontext',
      Tag: m.asIFrame ? 'iframe' : 'window',
      TypeAttr: '',
      Role: '',
      Text: m.text,
      Value: m.asIFrame ? m.frameUrl : m.pageUrl,
      Checked: false,
      Locator: '',
      LocatorAlternates: '',
      ElementXpath: '',
      LogicalKind: 'webWindow',
      Bounds: null,
      PageTitle: m.title,
      PageUrl: m.pageUrl,
      TableContext: '',
      RecorderKeyword: 'Pegwindow',
      Parameter: parameter
    });
    return m;
  }

  function describe(ev, sourceEvent) {
    const rawEl = normalizeEventTarget(ev.target);
    if (!rawEl || rawEl.nodeType !== 1) return;
    cacheControlValue(rawEl);
    const semanticEl = sourceEvent === 'blur' ? resolveTextSemanticElement(rawEl) || rawEl : rawEl;
    const el = semanticEl;
    const mode = currentRecoMode();
    const recoPlain = mode === 'record' && currentCaptureMode() === 'plain';
    if (mode === 'off') return;

    if (mode === 'pick') {
      if (sourceEvent !== 'click') return;
      ev.preventDefault();
      ev.stopPropagation();
      ev.stopImmediatePropagation();
    }

    if (mode === 'sync' && sourceEvent !== 'click') return;

    if (mode === 'record') {
      if (sourceEvent === 'input') return;
      if (sourceEvent === 'change') {
        const tag = (el.tagName || '').toLowerCase();
        const type = (el.getAttribute('type') || '').toLowerCase();
        const inTableCtx = recoPlain
          ? false
          : !!tryResolveTableCellContext(rawEl, tag, type, (el.getAttribute('role') || '').toLowerCase());
        if (tag === 'input' && type === 'file') {
          // Keep change for file picker input.
        } else if (tag === 'input' && isTextLikeInput(el) && !inTableCtx) return;
        if (tag === 'textarea') return;
        if (isContentEditableSurface(el)) return;
      }
      if (sourceEvent === 'click') {
        const tag = (el.tagName || '').toLowerCase();
        const type = (el.getAttribute('type') || '').toLowerCase();
        if (tag === 'textarea') return;
        if (tag === 'select') return;
        if (tag === 'input' && isTextLikeInput(el)) return;
        if (tag === 'input' && (type === 'checkbox' || type === 'radio')) return;
        if (isContentEditableSurface(el)) return;
      }
      if (sourceEvent === 'blur') {
        const tag = (el.tagName || '').toLowerCase();
        const role = (el.getAttribute('role') || '').toLowerCase();
        const ok =
          tag === 'textarea' ||
          (tag === 'input' && isTextLikeInput(el)) ||
          role === 'textbox' ||
          role === 'searchbox' ||
          isContentEditableSurface(el);
        if (!ok) return;
      }
      if (sourceEvent === 'keyup') {
        const key = ev && ev.key ? String(ev.key) : '';
        const tag = (el.tagName || '').toLowerCase();
        const type = (el.getAttribute('type') || '').toLowerCase();
        const role = (el.getAttribute('role') || '').toLowerCase();
        if (key !== 'Enter' && key !== 'Tab') return;
        if (recoPlain || !tryResolveTableCellContext(rawEl, tag, type, role)) return;
      }
    }

    let tag = (el.tagName || '').toLowerCase();
    let type = (el.getAttribute('type') || '').toLowerCase();
    let role = (el.getAttribute('role') || '').toLowerCase();
    let text = (el.innerText || el.textContent || '').trim().substring(0, 500);
    const ctxMeta = emitPegwindowIfContextChanged(mode) || currentContextMeta();
    const title = ctxMeta.title || '';
    const pageUrl = ctxMeta.pageUrl || (typeof location !== 'undefined' && location.href ? String(location.href) : '');
    let tbl = tableHint(el);
    let boundsEl = el;
    const directVal = readControlValue(el);
    const cachedVal = cachedControlValue(el) || cachedControlValue(rawEl);
    const rawVal =
      sourceEvent === 'blur'
        ? committedTextValue(el) || directVal || cachedVal
        : directVal || cachedVal;
    let value = rawVal;
    if ((tag === 'select' || role === 'combobox' || role === 'listbox') && el && el.options) {
      try {
        const idx = typeof el.selectedIndex === 'number' ? el.selectedIndex : -1;
        if (idx >= 0 && el.options[idx]) {
          const selText = String(el.options[idx].text || '').trim();
          if (selText) value = selText;
        }
      } catch (_) {}
    }
    if (tag === 'input' && type === 'file') {
      const fv = fileNamesValue(el);
      if (fv) value = fv;
    }
    if (tag === 'textarea' || isTextLikeInput(el) || role === 'textbox' || role === 'searchbox') {
      if (!value && text) value = text;
      if (sourceEvent === 'blur' && value) text = value.length > 500 ? value.substring(0, 500) : value;
    }

    let recorderKeyword = null;
    let tabLabel = '';
    let locatorOverride = null;
    let logicalKindOverride = null;
    let targetTag = tag;
    let targetRole = role;
    let targetLocator = null;
    let targetXpath = null;
    let mctx = null;
    let parameterOverride = null;
    let dataOverride = null;

    if (mode === 'record' && (sourceEvent === 'click' || sourceEvent === 'mousedown')) {
      if (tag === 'input' && (type === 'checkbox' || type === 'radio')) {
        sourceEvent = 'change';
      }
      let tctx = null;
      let configuredSemanticMatched = false;
      if (!recoPlain) {
        tctx = trySelectTabContext(rawEl, tag, role);
        if (!tctx && sourceEvent !== 'mousedown') {
          const semHit = findSemanticByRules(rawEl);
          if (semHit && semHit.element && semHit.rule) {
            configuredSemanticMatched = true;
            const semEl = semHit.element;
            const semRule = semHit.rule;
            const objType = str(semRule.objectType || 'webUnknown');
            const kw = keywordForObjectType(objType);
            if (kw) recorderKeyword = kw;
            logicalKindOverride = objType;
            const semPack = buildMultiLocator(semEl);
            locatorOverride = semPack.primary;
            boundsEl = semEl;
            tag = lower(semEl.tagName);
            role = roleLower(semEl);

            const skipChildren = semHit.origin === 'semi-self';
            if (skipChildren) {
              const selfPack = buildMultiLocator(rawEl);
              targetTag = lower(rawEl.tagName);
              targetRole = roleLower(rawEl);
              targetLocator = selfPack.primary;
              targetXpath = selfPack.shortXPath;
              tabLabel = normalizeSpace(rawEl.innerText || rawEl.textContent || '');
              text = tabLabel || text;
              value = tabLabel || value;
            } else {
              const childRes = findChildTargetByRules(semEl, rawEl);
              if (!childRes.ok) {
                const onNo = lower((semanticCfg().childrenTargetRules || {}).onNoMatch || 'error');
                const onAmb = lower((semanticCfg().childrenTargetRules || {}).onAmbiguousMatch || 'error');
                const isAmb = childRes.error === 'ambiguousChildMatch';
                const shouldError = (isAmb && onAmb === 'error') || (!isAmb && onNo === 'error');
                if (shouldError) {
                  pushPayload({
                    Kind: 'record',
                    SourceEvent: 'error',
                    Tag: tag,
                    TypeAttr: type,
                    Role: role,
                    Text: normalizeSpace(rawEl.innerText || rawEl.textContent || ''),
                    Value: '',
                    Checked: false,
                    Locator: '',
                    LocatorAlternates: '',
                    ElementXpath: '',
                    LogicalKind: objType,
                    Bounds: rectOf(rawEl),
                    PageTitle: title,
                    PageUrl: pageUrl,
                    TableContext: tbl,
                    RecorderKeyword: recorderKeyword || kw || '',
                    Error: 'semantic children target failed: ' + JSON.stringify(childRes)
                  });
                }
                return;
              }
              targetTag = lower(childRes.el.tagName || 'a');
              targetRole = roleLower(childRes.el);
              const childPack = buildMultiLocator(childRes.el);
              targetLocator = childPack.primary;
              targetXpath = childPack.shortXPath;
              tabLabel = normalizeSpace(childRes.el.innerText || childRes.el.textContent || '');
              text = tabLabel || text;
              value = tabLabel || value;
            }
          }
        }
        if (!configuredSemanticMatched && tctx) {
          recorderKeyword = 'SelectTab';
          tabLabel = tctx.tabLabel;
          locatorOverride = tctx.tabLocator;
          logicalKindOverride = 'webTab';
          targetTag = tctx.actualTag;
          targetRole = tctx.actualRole;
          targetLocator = tctx.targetCssLocator;
          targetXpath = tctx.targetXpath;
        }
        if (sourceEvent !== 'mousedown') {
          mctx = trySelectMenuContext(rawEl);
          if (mctx && mctx.skipRecord) return;
          if (mctx && mctx.menuPath) {
            recorderKeyword = 'SelectMenuItem';
            locatorOverride = mctx.menuLocator;
            logicalKindOverride = 'webMenu';
            targetTag = mctx.actualTag;
            targetRole = mctx.actualRole;
            targetLocator = mctx.targetCssLocator;
            targetXpath = mctx.targetXpath;
          }
        }

        if (!recorderKeyword) {
          const stx = tryButtonSelectTableSemantics(rawEl, tag, type, role);
          if (stx && stx.kind === 'webTable') {
            logicalKindOverride = 'webTable';
            const ta = findSemanticTableAncestor(rawEl);
            if (ta) tbl = tableHint(ta);
          }
          if (stx && stx.kind === 'webSelect' && stx.selectEl) {
            recorderKeyword = 'SelectDropDown';
            logicalKindOverride = 'webSelect';
            const sel = stx.selectEl;
            const sp = buildMultiLocator(sel);
            locatorOverride = sp.primary;
            targetTag = (sel.tagName || '').toLowerCase();
            targetRole = (sel.getAttribute('role') || '').toLowerCase();
            targetLocator = sp.primary;
            targetXpath = sp.shortXPath;
            tag = targetTag;
            type = (sel.getAttribute('type') || '').toLowerCase();
            role = targetRole;
            text = (sel.innerText || sel.textContent || '').trim().substring(0, 500);
            value = sel.value != null ? String(sel.value) : '';
            boundsEl = sel;
          }
        }
      }
      if (sourceEvent === 'mousedown' && !tctx && !configuredSemanticMatched) {
        if (likelyInsideRecordedMenuSurface(rawEl)) {
          let menuItem = null;
          try {
            menuItem = rawEl.closest
              ? rawEl.closest(
                  'li.kt-menu__item,li[class*="kt-menu__item"],[role="menuitem"],.el-menu-item,.menu-item'
                )
              : null;
          } catch (_) {}
          if (!menuItem) {
            let cur = rawEl;
            for (let d = 0; d < 14 && cur && cur.nodeType === 1; d++, cur = cur.parentElement) {
              if (isKtLikeMenuItemLi(cur)) {
                menuItem = cur;
                break;
              }
            }
          }
          if (!findActionableMenuClickTarget(rawEl, menuItem)) return;
        }
        const rt = lower(rawEl.tagName);
        const rr = roleLower(rawEl);
        if (!shouldRecordMouseDownWhenClickMayBeLost(rt, rr, rawEl)) return;
      }
    }

    if (mode === 'record' && sourceEvent === 'change' && tag === 'input' && type === 'file') {
      recorderKeyword = 'FileBrowser';
      logicalKindOverride = 'webFileBrowser';
    }

    if (
      !recorderKeyword &&
      mode === 'record' &&
      !recoPlain &&
      (sourceEvent === 'click' ||
        sourceEvent === 'mousedown' ||
        sourceEvent === 'change' ||
        sourceEvent === 'blur' ||
        sourceEvent === 'keyup')
    ) {
      const tcx = tryResolveTableCellContext(rawEl, tag, type, role);
      if (tcx) {
        recorderKeyword = 'FillTable';
        logicalKindOverride = tcx.logicalKind;
        locatorOverride = tcx.tableLocator;
        parameterOverride = tcx.parameter;
        if (tcx.tableContext) tbl = tcx.tableContext;
        if (tcx.data != null) dataOverride = tcx.data;
      }
    }

    let logicalKind = logicalKindOverride || inferLogicalKind(el);

    const locPack = buildMultiLocator(boundsEl);
    if (mode === 'record' && sourceEvent === 'change' && tag === 'input' && (type === 'checkbox' || type === 'radio')) {
      const sigLoc = (locatorOverride || locPack.primary || '');
      if (shouldSkipToggleDuplicate(sigLoc, !!(boundsEl && boundsEl.checked))) return;
    }
    let webClassOut = '';
    try {
      webClassOut = boundsEl && boundsEl.className != null ? String(boundsEl.className) : '';
    } catch (_) {}
    const payload = {
      Kind: mode === 'pick' ? 'pick' : mode === 'sync' ? 'sync' : 'record',
      SourceEvent: sourceEvent,
      Tag: tag,
      TypeAttr: type,
      Role: role,
      Text: text,
      Value: value,
      Checked: !!(boundsEl && boundsEl.checked),
      Locator: locatorOverride || locPack.primary,
      LocatorAlternates: locPack.alternates.join('\n'),
      ElementXpath: locPack.shortXPath,
      LogicalKind: logicalKind,
      Bounds: rectOf(boundsEl),
      PageTitle: title,
      PageUrl: pageUrl,
      TableContext: tbl,
      WebClass: webClassOut
    };
    if (window !== window.top) {
      try {
        const oh = (el.outerHTML || '').substring(0, 500);
        console.log('[MARS iframe event]', { name: text, xpath: locPack.shortXPath, outerHtml: oh });
      } catch (_) {}
    }
    if (recorderKeyword) {
      if (recorderKeyword === 'SelectTab') {
        const tabSig = (locatorOverride || locPack.primary || '') + '|' + (tabLabel || text || '');
        if (shouldSkipTabDuplicate(tabSig)) return;
        if (sourceEvent === 'mousedown') sourceEvent = 'click';
      }
      payload.RecorderKeyword = recorderKeyword;
      if (parameterOverride) payload.Parameter = parameterOverride;
      if (dataOverride != null) payload.Data = dataOverride;
      payload.TabLabel = tabLabel;
      if (recorderKeyword === 'SelectMenuItem' && mctx && mctx.menuPath)
        payload.MenuPath = mctx.menuPath;
      payload.TargetTag = targetTag;
      payload.TargetRole = targetRole;
      payload.TargetLocator = targetLocator;
      payload.TargetXpath = targetXpath;
    }
    if (recorderKeyword === 'SelectMenuItem' && mctx && mctx.menuElement) {
      const me = mctx.menuElement;
      let wc = '';
      try {
        wc = (me.className && String(me.className)) || '';
      } catch (_) {
        wc = '';
      }
      payload.WebClass = wc;
      payload.HtmlTag = (me.tagName || '').toLowerCase();
      let preview = normalizeSpace(me.innerText || me.textContent || '');
      if (preview.length > 500) preview = preview.substring(0, 500);
      payload.TextPreview = preview;
      payload.Placeholder =
        me.getAttribute && me.getAttribute('placeholder') ? String(me.getAttribute('placeholder')) : '';
      payload.DomId = me.id ? String(me.id) : '';
      payload.TreeNodeId = '';
    }
    if (recorderKeyword === 'SelectTab') {
      const semEl = boundsEl;
      const semHit = findSemanticByRules(rawEl);
      if (semHit && semHit.rule) {
        let childEl = null;
        if (semHit.origin === 'semi-self') {
          childEl = rawEl;
        } else {
          const childRes = findChildTargetByRules(semEl, rawEl);
          childEl = childRes.ok ? childRes.el : null;
        }
        applyPropertyMappings(payload, semHit.rule, semEl, rawEl, childEl);
        if (!hasRequiredProperties(payload, semHit.rule)) {
          pushPayload({
            Kind: 'record',
            SourceEvent: 'error',
            Tag: tag,
            TypeAttr: type,
            Role: role,
            Text: normalizeSpace(rawEl.innerText || rawEl.textContent || ''),
            Value: '',
            Checked: false,
            Locator: '',
            LocatorAlternates: '',
            ElementXpath: '',
            LogicalKind: payload.LogicalKind || 'webTab',
            Bounds: rectOf(rawEl),
            PageTitle: title,
            PageUrl: pageUrl,
            TableContext: tbl,
            RecorderKeyword: recorderKeyword,
            Error: 'requiredProperties missing for SelectTab'
          });
          return;
        }
      } else {
        let wc = '';
        try {
          wc = (semEl.className && String(semEl.className)) || '';
        } catch (_) {
          wc = '';
        }
        payload.WebClass = wc;
        payload.WebId = semEl && semEl.id ? String(semEl.id) : '';
        payload.WebIdPath = buildWebIdPath(semEl);
        payload.WebXpath = buildXPath(semEl);
        payload.WebChildrenTargetTag = 'a';
      }
    }
    if (recoPlain && mode === 'record' && payload.Kind === 'record' && !payload.Error) {
      payload.PlaywrightScript = buildPlaywrightSnippetForPayload(payload, sourceEvent);
    }
    if (mode === 'record' && payload.Kind === 'record' && !payload.Error) {
      payload.SourceEvent = sourceEvent;
      if (shouldSkipDuplicateClickAfterMouseDown(payload)) {
        clearMouseDownRecordEmit();
        return;
      }
    }
    pushPayload(payload);
    if (mode === 'record' && payload.Kind === 'record' && !payload.Error) {
      const se = lower(payload.SourceEvent);
      if (se === 'mousedown') noteMouseDownRecordEmit(payload);
      else if (se === 'click') clearMouseDownRecordEmit();
    }
  }

  document.addEventListener('click', (e) => describe(e, 'click'), true);
  document.addEventListener('mousedown', (e) => describe(e, 'mousedown'), true);
  document.addEventListener(
    'input',
    (e) => {
      const t = normalizeEventTarget(e.target);
      if (t) cacheControlValue(t);
      describe(e, 'input');
    },
    true
  );
  document.addEventListener('change', (e) => describe(e, 'change'), true);
  document.addEventListener('blur', (e) => describe(e, 'blur'), true);
  document.addEventListener('keyup', (e) => describe(e, 'keyup'), true);

  if (window === window.top) {
    let __marsLastGeom = '';
    function readGeomSig() {
      try {
        return (
          window.outerWidth +
          'x' +
          window.outerHeight +
          ',' +
          (window.screenX | 0) +
          ',' +
          (window.screenY | 0) +
          ',' +
          (window.screenLeft | 0) +
          ',' +
          (window.screenTop | 0)
        );
      } catch (_) {
        return '';
      }
    }
    function emitPegwindowIfChanged() {
      const mode = currentRecoMode();
      if (mode !== 'record') {
        __marsLastGeom = '';
        return;
      }
      const g = readGeomSig();
      if (!g) return;
      if (!__marsLastGeom) {
        __marsLastGeom = g;
        return;
      }
      if (g === __marsLastGeom) return;
      __marsLastGeom = g;
      const title = document.title || '';
      const pageUrl = typeof location !== 'undefined' && location.href ? String(location.href) : '';
      const wname = typeof window.name === 'string' ? window.name : '';
      pushPayload({
        Kind: 'record',
        SourceEvent: 'windowgeometry',
        Tag: 'window',
        TypeAttr: '',
        Role: '',
        Text: wname ? 'name=' + wname : '',
        Value: g,
        Checked: false,
        Locator: '',
        LocatorAlternates: '',
        ElementXpath: '',
        LogicalKind: 'webWindow',
        Bounds: null,
        PageTitle: title,
        PageUrl: pageUrl,
        TableContext: '',
        RecorderKeyword: 'PegwindowMove'
      });
    }
    window.setInterval(emitPegwindowIfChanged, 140);
    window.addEventListener('resize', emitPegwindowIfChanged, true);
  }
})();
