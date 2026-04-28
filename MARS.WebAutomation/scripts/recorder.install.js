(() => {
  if (window.__marsDomRecorder) return;
  window.__marsDomRecorder = true;
  if (window === window.top) {
    window.__marsRecoMode = window.__marsRecoMode || 'off';
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

  function esc(s) {
    if (s == null) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
  }

  function isTextLikeInput(el) {
    if (!el || el.tagName !== 'INPUT') return false;
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

  function isContentEditableSurface(el) {
    if (!el || el.nodeType !== 1) return false;
    if (el.isContentEditable) return true;
    const v = (el.getAttribute('contenteditable') || '').toLowerCase();
    return v === 'true' || v === '';
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
      const segs = [];
      let c = el;
      for (let d = 0; d < 6 && c && c.nodeType === 1; d++) {
        const tag = (c.tagName || '').toLowerCase();
        if (tag === 'body' || tag === 'html') break;
        segs.unshift(xpathSegmentFrom(c));
        c = c.parentElement;
      }
      if (!segs.length) return '';
      return '//body/' + segs.join('/');
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

  function buildMultiLocator(el) {
    if (!el || el.nodeType !== 1) return { primary: '', alternates: [], shortXPath: '' };
    const strategies = collectUniqueCssStrategies(el);
    const primary = strategies[0] || '';
    const alternates = strategies.slice(1, 5);
    const shortXPath = buildShortXPath(el);
    return { primary, alternates, shortXPath };
  }

  /** Primary CSS locator for Playwright (multi-strategy + preview uniqueness). */
  function buildLocator(el) {
    return buildMultiLocator(el).primary;
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
    if (tag === 'select' || role === 'combobox' || role === 'listbox') return 'webMenu';
    if (role === 'menu' || role === 'menubar' || role === 'menuitem' || role === 'menuitemcheckbox' || role === 'menuitemradio')
      return 'webMenu';
    if (hasPopup === 'true' || hasPopup === 'menu' || hasPopup === 'listbox') return 'webMenu';

    if (role === 'tab' || (cls.toLowerCase().indexOf('tab') >= 0 && (role === 'tab' || el.getAttribute('data-tab'))))
      return 'webTab';

    if (tag === 'button' || type === 'button' || type === 'submit' || type === 'reset' || role === 'button')
      return 'webButton';

    if (tag === 'textarea' || isTextLikeInput(el) || role === 'textbox' || role === 'searchbox' || isContentEditableSurface(el))
      return 'webEdit';

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
      if (cls.indexOf('pq-grid') >= 0 || cls.indexOf('ag-root') >= 0 || cls.indexOf('MuiDataGrid') >= 0)
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

  function roleLower(el) {
    return ((el && el.getAttribute && el.getAttribute('role')) || '').toLowerCase();
  }

  function isMenuItemElement(el) {
    if (!el || el.nodeType !== 1) return false;
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

  function trySelectMenuContext(clickEl) {
    if (!clickEl || !clickEl.closest) return null;
    const menuItem = clickEl.closest('[role="menuitem"],[role="menuitemcheckbox"],[role="menuitemradio"],.menu-item,.menuitem,.el-menu-item');
    if (!menuItem || !isMenuItemElement(menuItem)) return null;
    if (menuItemHasSubmenu(menuItem)) return { skipRecord: true };

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

  function describe(ev, sourceEvent) {
    const rawEl = ev.target;
    if (!rawEl || rawEl.nodeType !== 1) return;
    const semanticEl = sourceEvent === 'blur' ? resolveTextSemanticElement(rawEl) || rawEl : rawEl;
    const el = semanticEl;
    const mode = currentRecoMode();
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
        if (tag === 'input' && isTextLikeInput(el)) return;
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
    }

    let tag = (el.tagName || '').toLowerCase();
    let type = (el.getAttribute('type') || '').toLowerCase();
    let role = (el.getAttribute('role') || '').toLowerCase();
    let text = (el.innerText || el.textContent || '').trim().substring(0, 500);
    const title = document.title || '';
    const pageUrl = typeof location !== 'undefined' && location.href ? String(location.href) : '';
    let tbl = tableHint(el);
    let boundsEl = el;
    const rawVal =
      sourceEvent === 'blur' ? committedTextValue(el) : el.value != null ? String(el.value) : '';
    let value = rawVal;
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

    if (mode === 'record' && sourceEvent === 'click') {
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

      const tctx = trySelectTabContext(rawEl, tag, role);
      if (!recorderKeyword && tctx) {
        recorderKeyword = 'SelectTab';
        tabLabel = tctx.tabLabel;
        locatorOverride = tctx.tabLocator;
        logicalKindOverride = 'webTab';
        targetTag = tctx.actualTag;
        targetRole = tctx.actualRole;
        targetLocator = tctx.targetCssLocator;
        targetXpath = tctx.targetXpath;
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

    let logicalKind = logicalKindOverride || inferLogicalKind(el);

    const locPack = buildMultiLocator(boundsEl);
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
      TableContext: tbl
    };
    if (recorderKeyword) {
      payload.RecorderKeyword = recorderKeyword;
      payload.TabLabel = tabLabel;
      if (recorderKeyword === 'SelectMenuItem' && mctx && mctx.menuPath)
        payload.MenuPath = mctx.menuPath;
      payload.TargetTag = targetTag;
      payload.TargetRole = targetRole;
      payload.TargetLocator = targetLocator;
      payload.TargetXpath = targetXpath;
    }
    pushPayload(payload);
  }

  document.addEventListener('click', (e) => describe(e, 'click'), true);
  document.addEventListener('change', (e) => describe(e, 'change'), true);
  document.addEventListener('blur', (e) => describe(e, 'blur'), true);

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
      if (mode !== 'record') return;
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
        RecorderKeyword: 'Pegwindow'
      });
    }
    window.setInterval(emitPegwindowIfChanged, 140);
    window.addEventListener('resize', emitPegwindowIfChanged, true);
  }
})();
