namespace MARS.WebAutomation.Services
{
    internal static class PageInspectionScripts
    {
        /// <summary>Sets per-page markers before building the object tree JSON: unique node id prefix and a stable page instance id (returned).</summary>
        public const string PrepareObjectTreeCapture = @"
(ix) => {
  try {
    window.__marsTreeNodeIdPrefix = 'w' + ix + '_';
    window.__marsPageInstanceId = 'p-' + Date.now() + '-' + Math.floor(Math.random() * 1e9);
    return window.__marsPageInstanceId;
  } catch (e) { return ''; }
}";

        /// <summary>Clears only the node-id prefix. <c>__marsPageInstanceId</c> stays on each tab so highlight can map DTOs back to the correct Playwright page.</summary>
        public const string ClearObjectTreeCaptureMarkers = @"
() => {
  try { delete window.__marsTreeNodeIdPrefix; } catch (e0) {}
  return true;
}";

        public const string BuildObjectTreeJson = @"
() => {
  function rect(el, offX, offY) {
    try {
      var r = el.getBoundingClientRect();
      var ox = (typeof offX === 'number') ? offX : 0;
      var oy = (typeof offY === 'number') ? offY : 0;
      return { X: r.x + ox, Y: r.y + oy, Width: r.width, Height: r.height };
    } catch (e) { return null; }
  }
  function esc(s) {
    if (!s) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/""/g, '\\""');
  }
  function attr(el, name) {
    try { return el.getAttribute(name) || ''; } catch (e) { return ''; }
  }
  function boolStr(v) { return v ? 'true' : 'false'; }
  function dataAttrs(el) {
    try {
      var names = [];
      var attrs = el.attributes;
      if (!attrs) return '';
      for (var i = 0; i < attrs.length; i++) {
        var n = attrs[i].name || '';
        if (n.length >= 5 && n.substring(0, 5).toLowerCase() === 'data-') names.push(n);
      }
      names.sort(function(a, b) { return a.toLowerCase().localeCompare(b.toLowerCase()); });
      var parts = [];
      for (var j = 0; j < names.length; j++) {
        var nm = names[j];
        parts.push(nm + '=' + (el.getAttribute(nm) || ''));
      }
      return parts.join('; ');
    } catch (e) { return ''; }
  }
  function idMatchCount(id) {
    if (!id) return 0;
    try {
      if (typeof CSS !== 'undefined' && CSS.escape) return document.querySelectorAll('#' + CSS.escape(String(id))).length;
    } catch (e0) {}
    try { return document.querySelectorAll('[id=""' + esc(id) + '""]').length; } catch (e1) { return -1; }
  }
  function countCss(sel) {
    try { return document.querySelectorAll(sel).length; } catch (e2) { return -1; }
  }
  function looksDynamicId(id) {
    if (!id || typeof id !== 'string') return true;
    var s = String(id), sl = s.toLowerCase();
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
    var sl = String(name).toLowerCase();
    if (/^(rowid|cellid|rowkey|cellkey)$/.test(sl)) return true;
    if (/_\d{5,}$/.test(sl)) return true;
    return false;
  }
  function xpathLiteral(s) {
    if (s == null || s === '') return ""''"";
    var str = String(s);
    if (!str.includes(""'"")) return ""'"" + str.replace(/\\/g, '\\\\') + ""'"";
    var q = ""'"";
    var j = ', ' + String.fromCharCode(34, 39, 34) + ', ';
    return 'concat(' + str.split(q).map(function(p) { return q + p.replace(/\\/g, '\\\\') + q; }).join(j) + ')';
  }
  function xpathCount(nodesetExpr) {
    try {
      var r = document.evaluate('count(' + nodesetExpr + ')', document, null, XPathResult.NUMBER_TYPE, null);
      return typeof r.numberValue === 'number' ? r.numberValue : -1;
    } catch (e3) { return -1; }
  }
  function siblingSameTagIndex(el) {
    var parent = el.parentElement;
    if (!parent) return 1;
    var tn = (el.tagName || 'DIV').toUpperCase(), idx = 1, s = el.previousElementSibling;
    while (s) { if ((s.tagName || '').toUpperCase() === tn) idx++; s = s.previousElementSibling; }
    return idx;
  }
  function xpathSegmentFrom(el) {
    var tn = (el.tagName || 'div').toLowerCase();
    var nm = el.getAttribute('name');
    var parent = el.parentElement;
    if (nm && parent && !looksDynamicName(nm)) {
      try {
        var matches = parent.querySelectorAll(':scope > ' + tn + '[name=""' + esc(nm) + '""]');
        if (matches.length === 1 && matches[0] === el) return tn + '[@name=' + xpathLiteral(nm) + ']';
      } catch (e4) {}
    }
    var typ = (el.getAttribute('type') || '').toLowerCase();
    if (tn === 'input' && typ && parent) {
      try {
        var m2 = parent.querySelectorAll(':scope > input[type=""' + esc(typ) + '""]');
        if (m2.length === 1 && m2[0] === el) return 'input[@type=' + xpathLiteral(typ) + ']';
      } catch (e5) {}
    }
    return tn + '[' + siblingSameTagIndex(el) + ']';
  }
  function tryCompoundUnique(el, baseSel) {
    var n0 = countCss(baseSel);
    if (n0 === 1) return baseSel;
    if (n0 < 1) return '';
    var s = baseSel;
    var attrs = [['data-col', el.getAttribute('data-col')], ['data-row-index', el.getAttribute('data-row-index')], ['data-testid', el.getAttribute('data-testid')], ['name', el.getAttribute('name')], ['type', el.getAttribute('type')], ['aria-label', el.getAttribute('aria-label')], ['placeholder', el.getAttribute('placeholder')], ['title', el.getAttribute('title')]];
    for (var ai = 0; ai < attrs.length; ai++) {
      var an = attrs[ai][0], av = attrs[ai][1];
      if (!av || String(av).length > 160) continue;
      var piece = '[' + an + '=""' + esc(String(av)) + '""]';
      var next = s + piece;
      var n = countCss(next);
      if (n === 1) return next;
      if (n === 0) continue;
      s = next;
    }
    return countCss(s) === 1 ? s : '';
  }
  function xpathFor(el) {
    try {
      if (!el || el.nodeType !== 1) return '';
      var idv = el.id || '';
      if (idv && !looksDynamicId(idv)) {
        var xp = '//*[@id=' + xpathLiteral(idv) + ']';
        if (xpathCount(xp) === 1) return xp;
      }
      var tid = el.getAttribute && el.getAttribute('data-testid');
      if (tid) {
        var xp2 = '//*[@data-testid=' + xpathLiteral(tid) + ']';
        if (xpathCount(xp2) === 1) return xp2;
      }
      var segs = [], c = el;
      for (var d = 0; d < 6 && c && c.nodeType === 1; d++) {
        var tag = (c.tagName || '').toLowerCase();
        if (tag === 'body' || tag === 'html') break;
        segs.unshift(xpathSegmentFrom(c));
        c = c.parentElement;
      }
      if (!segs.length) return '';
      return '//body/' + segs.join('/');
    } catch (e6) { return ''; }
  }
  function readOnlyStr(el) {
    try {
      if (el.readOnly === true) return 'true';
      if (el.getAttribute('readonly') != null) return 'true';
    } catch (e3) {}
    return '';
  }
  function hiddenStr(el, tag) {
    try {
      if (el.hasAttribute('hidden')) return 'true';
      if (tag === 'INPUT' && (attr(el, 'type') || '').toLowerCase() === 'hidden') return 'true';
    } catch (e4) {}
    return '';
  }
  function requiredStr(el) {
    try {
      if (el.required === true) return 'true';
      if (el.hasAttribute('required')) return 'true';
    } catch (e5) {}
    return '';
  }
  function valueStr(el, tag) {
    try {
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT')
        return el.value != null ? String(el.value) : '';
    } catch (e6) {}
    return '';
  }
  function forStr(el, tag) {
    try {
      if (tag === 'LABEL') return el.htmlFor || attr(el, 'for') || '';
    } catch (e7) {}
    return '';
  }
  function locatorHint(el) {
    if (!el || el.nodeType !== 1) return '';
    var xpf = xpathFor(el);
    if (xpf && xpf.length > 1) {
      if (xpf.toLowerCase().indexOf('xpath=') === 0) return xpf;
      if (xpf.indexOf('//') === 0 || xpf.indexOf('(/') === 0) return 'xpath=' + xpf;
      return xpf;
    }
    var idv = el.id;
    if (idv) {
      if (!looksDynamicId(idv) && idMatchCount(idv) === 1) return '[id=""' + esc(idv) + '""]';
      if (idMatchCount(idv) > 0) {
        var comp = tryCompoundUnique(el, '[id=""' + esc(idv) + '""]');
        if (comp) return comp;
      }
    }
    var tid = el.getAttribute('data-testid');
    if (tid && countCss('[data-testid=""' + esc(tid) + '""]') === 1) return '[data-testid=""' + esc(tid) + '""]';
    var nm = el.getAttribute('name');
    if (nm && !looksDynamicName(nm) && (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA')) {
      var s = el.tagName.toLowerCase() + '[name=""' + esc(nm) + '""]';
      if (countCss(s) === 1) return s;
    }
    return el.tagName ? el.tagName.toLowerCase() : '';
  }
  function playwrightLocator(el, tag, role) {
    try {
      var t = tag ? tag.toLowerCase() : '';
      if (el.id && !looksDynamicId(el.id) && idMatchCount(el.id) === 1) return ""page.Locator('#"" + esc(el.id) + ""')"";
      var xp0 = xpathFor(el);
      if (xp0 && xp0.length > 1) {
        var xpSel = (xp0.toLowerCase().indexOf('xpath=') === 0) ? xp0 : ((xp0.indexOf('//') === 0 || xp0.indexOf('(/') === 0) ? ('xpath=' + xp0) : xp0);
        return ""page.Locator('"" + esc(xpSel) + ""')"";
      }
      if (el.id && idMatchCount(el.id) > 1) {
        var comp = tryCompoundUnique(el, '[id=""' + esc(el.id) + '""]');
        if (comp) return ""page.Locator('"" + esc(comp) + ""')"";
      }
      var tid = el.getAttribute('data-testid');
      if (tid) return ""page.GetByTestId('"" + esc(tid) + ""')"";
      var nm = el.getAttribute('name');
      if (nm && (t === 'input' || t === 'select' || t === 'textarea'))
        return ""page.Locator('"" + t + ""[name=\"""" + esc(nm) + ""\""]'')"";
      var al = el.getAttribute('aria-label');
      if (al) return ""page.GetByLabel('"" + esc(al) + ""')"";
      var ph = el.getAttribute('placeholder');
      if (ph && t === 'input') return ""page.GetByPlaceholder('"" + esc(ph) + ""')"";
    } catch (ex) {}
    var h = locatorHint(el);
    return h ? ""page.Locator('"" + esc(h) + ""')"" : '';
  }
  function shortText(el) {
    var t = (el.innerText || el.textContent || '').trim().replace(/\s+/g, ' ');
    return t.length > 200 ? t.substring(0, 200) + '…' : t;
  }
  function appendPathArr(arr, cur) {
    var out = arr ? arr.slice() : [];
    var c = (cur == null) ? '' : String(cur).trim();
    out.push(c);
    return out;
  }
  function interactiveKind(tag, role, el) {
    var t = tag ? tag.toUpperCase() : '';
    var r = (role || '').toLowerCase();
    if (t === 'A' || t === 'BUTTON' || t === 'INPUT' || t === 'SELECT' || t === 'TEXTAREA' || t === 'OPTION' || t === 'LABEL')
      return 'interactive';
    if (r === 'button' || r === 'link' || r === 'checkbox' || r === 'radio' || r === 'textbox' || r === 'combobox' || r === 'tab')
      return 'interactive';
    if (el.tabIndex >= 0 && el.tabIndex !== 0 && t !== 'DIV' && t !== 'SPAN' && t !== 'P') return 'interactive';
    return 'container';
  }
  var idSeq = 0;
  function walk(el, parentId, depth, offX, offY, framePath, rolePathArr, tagPathArr, idPathArr, namePathArr, textPathArr) {
    if (!el || depth > 24) return null;
    if (el.nodeType !== 1) return null;
    var ox = (typeof offX === 'number') ? offX : 0;
    var oy = (typeof offY === 'number') ? offY : 0;
    var id = (window.__marsTreeNodeIdPrefix || '') + 'n' + (++idSeq);
    var fp = framePath || '';
    var tag = el.tagName || '';
    var role = el.getAttribute('role') || '';
    var currRolePathArr = appendPathArr(rolePathArr, role);
    var currTagPathArr = appendPathArr(tagPathArr, tag);
    var currIdPathArr = appendPathArr(idPathArr, attr(el, 'id'));
    var currNamePathArr = appendPathArr(namePathArr, attr(el, 'name'));
    var currTextPathArr = appendPathArr(textPathArr, shortText(el));
    var disp = tag + (el.id ? '#' + el.id : '') + (role ? '[role=' + role + ']' : '') + ' ' + shortText(el).substring(0, 80);
    var href = (tag === 'A' && el.href) ? String(el.href) : (attr(el, 'href') || '');
    var dis = false;
    try { dis = !!el.disabled; } catch (e0) { dis = false; }
    var node = {
      Id: id,
      ParentId: parentId || null,
      DisplayName: disp.trim().substring(0, 200),
      Tag: tag,
      Role: role,
      LocatorHint: locatorHint(el),
      Bounds: rect(el, ox, oy),
      InteractiveKind: interactiveKind(tag, role, el),
      ClassName: attr(el, 'class'),
      NameAttr: attr(el, 'name'),
      Title: attr(el, 'title'),
      Href: href,
      InputType: attr(el, 'type'),
      Placeholder: attr(el, 'placeholder'),
      AriaLabel: attr(el, 'aria-label'),
      AriaRole: role,
      TabIndexStr: (function(){ try { var v = el.tabIndex; return (v === null || v === undefined) ? '' : String(v); } catch(e){ return ''; } })(),
      Disabled: boolStr(dis),
      ContentEditable: boolStr((el.getAttribute('contenteditable') || '') === 'true' || el.isContentEditable === true),
      TextPreview: shortText(el),
      PlaywrightLocator: playwrightLocator(el, tag, role),
      HtmlId: attr(el, 'id'),
      AriaChecked: attr(el, 'aria-checked'),
      AriaControls: attr(el, 'aria-controls'),
      AriaDescribedby: attr(el, 'aria-describedby'),
      AriaExpanded: attr(el, 'aria-expanded'),
      AriaLabelledby: attr(el, 'aria-labelledby'),
      AriaSelected: attr(el, 'aria-selected'),
      Autocomplete: attr(el, 'autocomplete'),
      Value: valueStr(el, tag),
      Required: requiredStr(el),
      Pattern: attr(el, 'pattern'),
      ForAttr: forStr(el, tag),
      Readonly: readOnlyStr(el),
      Hidden: hiddenStr(el, tag),
      DataAttributes: dataAttrs(el),
      Xpath: xpathFor(el),
      OuterHtml: (function() {
        try {
          var h = el.outerHTML || '';
          return h.length > 4000 ? h.substring(0, 3997) + '...' : h;
        } catch (e9) { return ''; }
      })(),
      PageInstanceId: (typeof window.__marsPageInstanceId === 'string' ? window.__marsPageInstanceId : ''),
      FramePath: fp,
      RolePath: currRolePathArr.join(';'),
      HtmlTagPath: currTagPathArr.join(';'),
      IdPath: currIdPathArr.join(';'),
      NamePath: currNamePathArr.join(';'),
      TextPath: currTextPathArr.join(';'),
      Children: []
    };
    var ch = el.children;
    for (var i = 0; i < ch.length; i++) {
      var w = walk(ch[i], id, depth + 1, ox, oy, fp, currRolePathArr, currTagPathArr, currIdPathArr, currNamePathArr, currTextPathArr);
      if (w) node.Children.push(w);
    }
    if (tag === 'SLOT') {
      try {
        var assign = el.assignedElements && el.assignedElements({ flatten: true });
        if (assign && assign.length) {
          for (var ai = 0; ai < assign.length; ai++) {
            var aw = walk(assign[ai], id, depth + 1, ox, oy, fp, currRolePathArr, currTagPathArr, currIdPathArr, currNamePathArr, currTextPathArr);
            if (aw) node.Children.push(aw);
          }
        }
      } catch (eSlot) {}
    }
    // Include open shadow-root elements so controls inside custom cells are visible.
    try {
      var sr = el.shadowRoot;
      var sch = sr ? sr.children : null;
      if (sch && sch.length) {
        for (var si = 0; si < sch.length; si++) {
          var sw = walk(sch[si], id, depth + 1, ox, oy, fp, currRolePathArr, currTagPathArr, currIdPathArr, currNamePathArr, currTextPathArr);
          if (sw) node.Children.push(sw);
        }
      }
    } catch (e14) {
      // closed shadow root / access denied
    }
    // Expand iframe/frame internal DOM when same-origin access is allowed.
    if (tag === 'IFRAME' || tag === 'FRAME') {
      try {
        var fd = el.contentDocument;
        var fr = fd ? (fd.body || fd.documentElement) : null;
        if (fr && fr.nodeType === 1) {
          var frRect = null;
          try { frRect = el.getBoundingClientRect(); } catch (e11) { frRect = null; }
          var nx = ox + (frRect ? frRect.left : 0);
          var ny = oy + (frRect ? frRect.top : 0);
          var idx = -1;
          try {
            var ff = fd.querySelectorAll ? fd.querySelectorAll('iframe,frame') : [];
            idx = [].indexOf.call(ff, el);
          } catch (e12) { idx = -1; }
          if (idx < 0) {
            try {
              var ff2 = el.ownerDocument.querySelectorAll('iframe,frame');
              for (var ii = 0; ii < ff2.length; ii++) { if (ff2[ii] === el) { idx = ii; break; } }
            } catch (e13) { idx = -1; }
          }
          if (idx < 0) idx = 0;
          var childPath = fp ? (fp + '/' + idx) : String(idx);
          var fw = walk(fr, id, depth + 1, nx, ny, childPath, currRolePathArr, currTagPathArr, currIdPathArr, currNamePathArr, currTextPathArr);
          if (fw) node.Children.push(fw);
        }
      } catch (e10) {
        // Cross-origin frames are not accessible; keep frame element node only.
      }
    }
    return node;
  }
  var root = document.body || document.documentElement;
  if (!root) return '[]';
  var tree = walk(root, null, 0, 0, 0, '', [], [], [], [], []);
  return JSON.stringify(tree ? [tree] : []);
}";

        /// <summary>Removes the transient DOM highlight overlay (if present).</summary>
        public const string RemoveObjectHighlight = @"
() => {
  var old = document.getElementById('__mars_wa_inspect_hl');
  if (old) old.remove();
  return true;
}";

        /// <summary>Highlights a DOM node. Argument: hint, xpath, x,y,w,h, kind (container|interactive). Skips ambiguous tag-only CSS hints; resolves via xpath then bounds.</summary>
        public const string ApplyObjectHighlight = @"
(p) => {
  function hintLooksSpecific(h) {
    if (!h) return false;
    var t = String(h).trim();
    if (t.indexOf('[') >= 0) return true;
    if (t.indexOf('#') >= 0) return true;
    if (t.charAt(0) === '.') return true;
    return false;
  }
  function findInDoc(doc, hint, xpath) {
    var el = null;
    if (hint && hintLooksSpecific(hint)) {
      try { el = doc.querySelector(hint); } catch (e0) { el = null; }
    }
    if (!el && xpath) {
      try {
        var xr = doc.evaluate(xpath, doc, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        var n = xr.singleNodeValue;
        if (n && n.nodeType === 1) el = n;
      } catch (e1) { el = null; }
    }
    return el;
  }
  function findWithFrames(doc, hint, xpath, offX, offY, depth) {
    if (!doc || depth > 8) return null;
    var el = findInDoc(doc, hint, xpath);
    if (el) {
      try {
        var r = el.getBoundingClientRect();
        return {
          el: el,
          rect: { left: r.left + offX, top: r.top + offY, width: r.width, height: r.height }
        };
      } catch (e2) {}
    }
    var fns = [];
    try { fns = doc.querySelectorAll('iframe,frame'); } catch (e3) { fns = []; }
    for (var i = 0; i < fns.length; i++) {
      var fr = fns[i];
      try {
        var sub = fr.contentDocument;
        if (!sub) continue;
        var frRect = fr.getBoundingClientRect();
        var got = findWithFrames(sub, hint, xpath, offX + frRect.left, offY + frRect.top, depth + 1);
        if (got) return got;
      } catch (e4) {
        // cross-origin frame, skip
      }
    }
    return null;
  }
  function resolveDocByFramePath(path) {
    if (!path) return document;
    var parts = String(path).split('/').filter(function(x){ return x.length > 0; });
    var doc = document;
    for (var i = 0; i < parts.length; i++) {
      var idx = parseInt(parts[i], 10);
      if (isNaN(idx) || idx < 0) return null;
      var fns = [];
      try { fns = doc.querySelectorAll('iframe,frame'); } catch (e0) { return null; }
      if (idx >= fns.length) return null;
      var fr = fns[idx];
      try { doc = fr.contentDocument; } catch (e1) { return null; }
      if (!doc) return null;
    }
    return doc;
  }
  function frameOffsetByPath(path) {
    if (!path) return { x: 0, y: 0 };
    var parts = String(path).split('/').filter(function(x){ return x.length > 0; });
    var doc = document, ox = 0, oy = 0;
    for (var i = 0; i < parts.length; i++) {
      var idx = parseInt(parts[i], 10);
      if (isNaN(idx) || idx < 0) break;
      var fns = [];
      try { fns = doc.querySelectorAll('iframe,frame'); } catch (e0) { break; }
      if (idx >= fns.length) break;
      var fr = fns[idx];
      try {
        var rr = fr.getBoundingClientRect();
        ox += rr.left; oy += rr.top;
        doc = fr.contentDocument;
      } catch (e1) { break; }
      if (!doc) break;
    }
    return { x: ox, y: oy };
  }
  var old = document.getElementById('__mars_wa_inspect_hl');
  if (old) old.remove();
  var hint = (p && p.hint) ? String(p.hint) : '';
  var xpath = (p && p.xpath) ? String(p.xpath) : '';
  var framePath = (p && p.framePath) ? String(p.framePath) : '';
  var got = null;
  if (framePath) {
    var d0 = resolveDocByFramePath(framePath);
    if (d0) {
      var of0 = frameOffsetByPath(framePath);
      var e0 = findInDoc(d0, hint, xpath);
      if (e0) {
        try {
          var r0 = e0.getBoundingClientRect();
          got = { el: e0, rect: { left: r0.left + of0.x, top: r0.top + of0.y, width: r0.width, height: r0.height } };
        } catch (e2) {}
      }
    }
  }
  if (!got) got = findWithFrames(document, hint, xpath, 0, 0, 0);
  var el = got ? got.el : null;
  var r = got ? got.rect : null;
  if (!r && p && typeof p.x === 'number' && typeof p.y === 'number') {
    var w = (typeof p.w === 'number' && p.w > 0) ? p.w : 2;
    var h = (typeof p.h === 'number' && p.h > 0) ? p.h : 2;
    r = { left: p.x, top: p.y, width: w, height: h };
  }
  if (!r || (r.width < 1 && r.height < 1)) return false;
  try {
    if (el && el.scrollIntoView) el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    var got2 = null;
    if (framePath) {
      var d1 = resolveDocByFramePath(framePath);
      if (d1) {
        var of1 = frameOffsetByPath(framePath);
        var e1 = findInDoc(d1, hint, xpath);
        if (e1) {
          try {
            var r1 = e1.getBoundingClientRect();
            got2 = { el: e1, rect: { left: r1.left + of1.x, top: r1.top + of1.y, width: r1.width, height: r1.height } };
          } catch (e6) {}
        }
      }
    }
    if (!got2) got2 = findWithFrames(document, hint, xpath, 0, 0, 0);
    if (got2 && got2.rect) r = got2.rect;
  } catch (e5) {}
  var kind = (p && p.kind) ? String(p.kind) : '';
  var interactive = kind !== 'container';
  if (p && typeof p.interactive === 'boolean') interactive = p.interactive;
  var border = interactive ? '3px solid #f97316' : '3px solid #2563eb';
  var bg = interactive ? 'rgba(249, 115, 22, 0.12)' : 'rgba(37, 99, 235, 0.12)';
  var box = document.createElement('div');
  box.id = '__mars_wa_inspect_hl';
  box.style.position = 'fixed';
  box.style.left = Math.max(0, r.left) + 'px';
  box.style.top = Math.max(0, r.top) + 'px';
  box.style.width = Math.max(2, r.width) + 'px';
  box.style.height = Math.max(2, r.height) + 'px';
  box.style.boxSizing = 'border-box';
  box.style.border = border;
  box.style.background = bg;
  box.style.zIndex = '2147483646';
  box.style.pointerEvents = 'none';
  document.documentElement.appendChild(box);
  return true;
}";
    }
}
