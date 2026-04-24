namespace MARS.WebAutomation.Services
{
    internal static class PageInspectionScripts
    {
        public const string BuildObjectTreeJson = @"
() => {
  function rect(el) {
    try {
      var r = el.getBoundingClientRect();
      return { X: r.x, Y: r.y, Width: r.width, Height: r.height };
    } catch (e) { return null; }
  }
  function esc(s) {
    if (!s) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/""/g, '\\""');
  }
  function locatorHint(el) {
    if (el.id) return '[id=""' + esc(el.id) + '""]';
    var tid = el.getAttribute('data-testid');
    if (tid) return '[data-testid=""' + esc(tid) + '""]';
    var nm = el.getAttribute('name');
    if (nm && (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA'))
      return el.tagName.toLowerCase() + '[name=""' + esc(nm) + '""]';
    return el.tagName ? el.tagName.toLowerCase() : '';
  }
  function shortText(el) {
    var t = (el.innerText || el.textContent || '').trim().replace(/\s+/g, ' ');
    return t.length > 60 ? t.substring(0, 60) + '…' : t;
  }
  var idSeq = 0;
  function walk(el, parentId, depth) {
    if (!el || depth > 10) return null;
    if (el.nodeType !== 1) return null;
    var id = 'n' + (++idSeq);
    var tag = el.tagName || '';
    var role = el.getAttribute('role') || '';
    var disp = tag + (el.id ? '#' + el.id : '') + (role ? '[role=' + role + ']' : '') + ' ' + shortText(el);
    var node = {
      Id: id,
      ParentId: parentId || null,
      DisplayName: disp.trim().substring(0, 200),
      Tag: tag,
      Role: role,
      LocatorHint: locatorHint(el),
      Bounds: rect(el),
      Children: []
    };
    var ch = el.children;
    for (var i = 0; i < ch.length; i++) {
      var w = walk(ch[i], id, depth + 1);
      if (w) node.Children.push(w);
    }
    return node;
  }
  var root = document.body || document.documentElement;
  if (!root) return '[]';
  var tree = walk(root, null, 0);
  return JSON.stringify(tree ? [tree] : []);
}";
    }
}
