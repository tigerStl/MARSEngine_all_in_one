namespace MARS.WebAutomation.Services
{
    /// <summary>In-page script: returns JSON array of interactive element surface rows (see <see cref="Models.DomAssertionElementState"/>).</summary>
    internal static class DomAssertionCaptureScripts
    {
        public const string CollectInteractiveSurfaceJson = @"(maxElements) => {
  function esc(s) {
    if (!s) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/""/g, '\\""');
  }
  function attr(el, name) {
    try { return el.getAttribute(name) || ''; } catch (e) { return ''; }
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
    if (/^(rowid|cellid|__)/.test(sl)) return true;
    if (/(uuid|guid|react-|mui-|radix-|headlessui|ngb-)/i.test(s)) return true;
    if (/^:r[0-9a-z]+$/i.test(s)) return true;
    if (/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(s)) return true;
    if (/[-_]\d{5,}$/.test(s)) return true;
    return false;
  }
  function looksDynamicName(name) {
    if (!name || typeof name !== 'string') return true;
    var sl = String(name).toLowerCase();
    if (/^(rowid|cellid|rowkey|cellkey)$/.test(sl)) return true;
    if (/_\d{5,}$/.test(sl)) return true;
    return false;
  }
  function tryCompoundUnique(el, baseSel) {
    var n0 = countCss(baseSel);
    if (n0 === 1) return baseSel;
    if (n0 < 1) return '';
    var s = baseSel;
    var attrs = [['data-testid', el.getAttribute('data-testid')], ['name', el.getAttribute('name')], ['type', el.getAttribute('type')], ['aria-label', el.getAttribute('aria-label')], ['placeholder', el.getAttribute('placeholder')]];
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
  function cssLocator(el) {
    try {
      var idv = el.id || '';
      if (idv && !looksDynamicId(idv) && idMatchCount(idv) === 1) return '#' + CSS.escape(String(idv));
      if (idv && idMatchCount(idv) > 1) {
        var comp = tryCompoundUnique(el, '[id=""' + esc(idv) + '""]');
        if (comp) return comp;
      }
      var tid = el.getAttribute('data-testid');
      if (tid && countCss('[data-testid=""' + esc(tid) + '""]') === 1) return '[data-testid=""' + esc(tid) + '""]';
      var nm = el.getAttribute('name');
      var tag = (el.tagName || 'div').toLowerCase();
      if (nm && !looksDynamicName(nm) && (tag === 'input' || tag === 'select' || tag === 'textarea')) {
        var s = tag + '[name=""' + esc(nm) + '""]';
        if (countCss(s) === 1) return s;
      }
      var al = el.getAttribute('aria-label');
      if (al && countCss('[aria-label=""' + esc(al) + '""]') === 1) return '[aria-label=""' + esc(al) + '""]';
    } catch (ex) {}
    var t = (el.tagName || 'div').toLowerCase();
    return t;
  }
  function simpleXPath(el) {
    var parts = [];
    var c = el;
    for (var d = 0; d < 5 && c && c.nodeType === 1; d++) {
      var tag = (c.tagName || 'div').toLowerCase();
      if (tag === 'html' || tag === 'body') break;
      var parent = c.parentElement;
      if (!parent) break;
      var idx = 1;
      var s = c.previousElementSibling;
      while (s) {
        if ((s.tagName || '').toLowerCase() === tag) idx++;
        s = s.previousElementSibling;
      }
      parts.unshift(tag + '[' + idx + ']');
      c = parent;
    }
    return parts.length ? '/' + parts.join('/') : '';
  }
  function signatureFor(el, css, xp) {
    var idv = (el.id || '').trim();
    if (idv && !looksDynamicId(idv)) return 'id:' + idv;
    var tid = attr(el, 'data-testid');
    if (tid) return 'tid:' + tid;
    var nm = attr(el, 'name');
    if (nm && !looksDynamicName(nm)) return 'nm:' + nm.toLowerCase() + ':' + (el.tagName || '').toLowerCase();
    return 'xp:' + xp + ':css:' + css;
  }
  function readOnlyBool(el) {
    try {
      if (el.readOnly === true) return true;
      if (el.hasAttribute && el.hasAttribute('readonly')) return true;
      if (attr(el, 'aria-readonly') === 'true') return true;
    } catch (e3) {}
    return false;
  }
  function disabledBool(el) {
    try {
      if (el.disabled === true) return true;
      if (attr(el, 'aria-disabled') === 'true') return true;
    } catch (e4) {}
    return false;
  }
  function styleColor(el, prop) {
    try {
      var cs = window.getComputedStyle(el);
      var v = cs ? cs.getPropertyValue(prop) : '';
      return (v || '').trim().substring(0, 120);
    } catch (e5) { return ''; }
  }
  function contentEditableBool(el) {
    try {
      return (attr(el, 'contenteditable') || '').toLowerCase() === 'true' || el.isContentEditable === true;
    } catch (e6) { return false; }
  }
  var cap = (typeof maxElements === 'number' && maxElements > 0) ? maxElements : 500;
  var sel = ""input,textarea,select,button,a[href],[role],[contenteditable]"";
  var nodes = [];
  try {
    nodes = Array.prototype.slice.call(document.querySelectorAll(sel), 0, cap);
  } catch (e7) { nodes = []; }
  var out = [];
  for (var i = 0; i < nodes.length; i++) {
    var el = nodes[i];
    if (!el || el.nodeType !== 1) continue;
    var tag = (el.tagName || '').toLowerCase();
    var css = cssLocator(el);
    var xp = simpleXPath(el);
    var sig = signatureFor(el, css, xp);
    out.push({
      CssLocator: css,
      Xpath: xp,
      Signature: sig,
      Tag: tag,
      ReadOnly: readOnlyBool(el),
      Disabled: disabledBool(el),
      AriaDisabled: attr(el, 'aria-disabled'),
      AriaReadonly: attr(el, 'aria-readonly'),
      ContentEditable: contentEditableBool(el),
      Color: styleColor(el, 'color'),
      BackgroundColor: styleColor(el, 'background-color')
    });
  }
  return JSON.stringify(out);
}";
    }
}
