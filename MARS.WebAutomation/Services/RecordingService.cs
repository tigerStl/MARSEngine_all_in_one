using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using MARS.WebAutomation;
using MARS.WebAutomation.Models;
using NLog;

namespace MARS.WebAutomation.Services
{
    public sealed class RecorderEventArgs : EventArgs
    {
        public SemanticStepRecord Step { get; set; }
    }

    public sealed class PickEventArgs : EventArgs
    {
        public SemanticStepRecord Snapshot { get; set; }
    }

    public sealed class RecordingService
    {
        private static readonly Logger Log = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".Services.RecordingService");

        private const string InstallScript = @"
(() => {
  if (window !== window.top) return;
  if (window.__marsDomRecorder) return;
  window.__marsDomRecorder = true;
  window.__marsRecoMode = window.__marsRecoMode || 'off';

  function esc(s) {
    if (s == null) return '';
    return String(s).replace(/\\/g, '\\\\').replace(/""/g, '\\""');
  }

  function buildLocator(el) {
    if (!el || el.nodeType !== 1) return '';
    if (el.id) return '[id=""' + esc(el.id) + '""]';
    var tid = el.getAttribute('data-testid');
    if (tid) return '[data-testid=""' + esc(tid) + '""]';
    var nm = el.getAttribute('name');
    if (nm && (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA'))
      return el.tagName.toLowerCase() + '[name=""' + esc(nm) + '""]';
    var role = el.getAttribute('role');
    var aria = el.getAttribute('aria-label');
    if (role && aria) return 'role=' + role + '[name=""' + esc(aria) + '""]';
    if (el.tagName === 'A' && el.getAttribute('href'))
      return 'a[href=""' + esc(el.getAttribute('href')) + '""]';
    return el.tagName ? el.tagName.toLowerCase() : 'body';
  }

  function rectOf(el) {
    try {
      var r = el.getBoundingClientRect();
      return { X: r.x, Y: r.y, Width: r.width, Height: r.height };
    } catch (e) { return null; }
  }

  function tableHint(el) {
    var cur = el;
    for (var i = 0; i < 8 && cur; i++, cur = cur.parentElement) {
      if (!cur.tagName) continue;
      var tag = cur.tagName.toLowerCase();
      if (tag === 'table') return 'table';
      var cls = (cur.className && String(cur.className)) || '';
      if (cls.indexOf('pq-grid') >= 0 || cls.indexOf('ag-root') >= 0 || cls.indexOf('MuiDataGrid') >= 0)
        return 'webtable:' + cls.split(' ')[0];
    }
    return '';
  }

  function pushPayload(payload) {
    try {
      if (window.marsRecorderPush)
        window.marsRecorderPush(payload);
    } catch (e) { }
  }

  function describe(ev, sourceEvent) {
    var el = ev.target;
    if (!el || el.nodeType !== 1) return;
    var mode = window.__marsRecoMode || 'off';
    if (mode === 'pick' && sourceEvent === 'click') {
      ev.preventDefault();
      ev.stopPropagation();
      ev.stopImmediatePropagation();
    }
    if (mode === 'off') return;

    var tag = (el.tagName || '').toLowerCase();
    var type = (el.getAttribute('type') || '').toLowerCase();
    var role = (el.getAttribute('role') || '').toLowerCase();
    var text = (el.innerText || el.textContent || '').trim().substring(0, 500);
    var title = document.title || '';
    var tbl = tableHint(el);

    var payload = {
      Kind: mode === 'pick' ? 'pick' : 'record',
      SourceEvent: sourceEvent,
      Tag: tag,
      TypeAttr: type,
      Role: role,
      Text: text,
      Value: el.value != null ? String(el.value) : '',
      Checked: !!el.checked,
      Locator: buildLocator(el),
      Bounds: rectOf(el),
      PageTitle: title,
      TableContext: tbl
    };

    pushPayload(payload);
  }

  document.addEventListener('click', function (e) { describe(e, 'click'); }, true);
  document.addEventListener('input', function (e) { describe(e, 'input'); }, true);
  document.addEventListener('change', function (e) { describe(e, 'change'); }, true);
})();
";

        public event EventHandler<RecorderEventArgs> RecordedStep;
        public event EventHandler<PickEventArgs> Picked;

        private bool _bindingInstalled;
        private IBrowserContext _contextBound;

        public async Task InstallAsync(IPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var ctx = page.Context;
            if (_contextBound != ctx)
            {
                _contextBound = ctx;
                _bindingInstalled = false;
            }

            if (!_bindingInstalled)
            {
                await ctx.ExposeBindingAsync("marsRecorderPush", (BindingSource _, object payload) =>
                {
                    OnPayload(payload);
                }).ConfigureAwait(false);
                await ctx.AddInitScriptAsync(InstallScript).ConfigureAwait(false);
                _bindingInstalled = true;
            }
        }

        public void ResetForNewContext()
        {
            _bindingInstalled = false;
            _contextBound = null;
        }

        public Task SetModeAsync(IPage page, string mode)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            var m = string.IsNullOrEmpty(mode) ? "off" : mode;
            return page.EvaluateAsync("m => { window.__marsRecoMode = m; }", m);
        }

        private void OnPayload(object payload)
        {
            try
            {
                var jo = payload is JObject o ? o : JObject.FromObject(payload);
                var kind = (string)jo["Kind"] ?? "record";
                var step = MapToStep(jo);
                if (string.Equals(kind, "pick", StringComparison.OrdinalIgnoreCase))
                    Picked?.Invoke(this, new PickEventArgs { Snapshot = step });
                else
                    RecordedStep?.Invoke(this, new RecorderEventArgs { Step = step });
            }
            catch
            {
                // ignore malformed payloads
            }
        }

        private static SemanticStepRecord MapToStep(JObject jo)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(MapToStep),
                ("Tag", (string)jo["Tag"]),
                ("SourceEvent", (string)jo["SourceEvent"]),
                ("Kind", (string)jo["Kind"])))
            {
                return MapToStepCore(jo);
            }
        }

        private static SemanticStepRecord MapToStepCore(JObject jo)
        {
            var tag = ((string)jo["Tag"] ?? string.Empty).ToLowerInvariant();
            var typeAttr = ((string)jo["TypeAttr"] ?? string.Empty).ToLowerInvariant();
            var role = ((string)jo["Role"] ?? string.Empty).ToLowerInvariant();
            var source = (string)jo["SourceEvent"] ?? string.Empty;
            var locator = (string)jo["Locator"] ?? string.Empty;
            var text = (string)jo["Text"] ?? string.Empty;
            var value = (string)jo["Value"] ?? string.Empty;
            var pageTitle = (string)jo["PageTitle"] ?? string.Empty;
            var tableCtx = (string)jo["TableContext"] ?? string.Empty;
            var chk = jo["Checked"] != null && jo["Checked"].Value<bool>();

            var keyword = ResolveKeyword(tag, typeAttr, role, tableCtx, source);
            var data = BuildData(keyword, text, value, chk, pageTitle);
            var param = BuildParameter(keyword, tableCtx, tag, role);

            BoundingRectDto bounds = null;
            var b = jo["Bounds"];
            if (b != null && b.Type != JTokenType.Null)
                bounds = b.ToObject<BoundingRectDto>();

            return new SemanticStepRecord
            {
                TimestampUtc = DateTime.UtcNow,
                SourceEvent = source,
                Keyword = keyword,
                Locator = locator,
                Parameter = param,
                Data = data,
                BoundingRect = bounds
            };
        }

        private static string BuildData(string keyword, string text, string value, bool chk, string pageTitle)
        {
            if (keyword == "SetBox")
                return chk ? "true" : "false";
            if (keyword == "FillEdit" || keyword == "SelectDropDown")
                return string.IsNullOrEmpty(value) ? text : value;
            if (keyword == "ClickButton")
                return string.IsNullOrEmpty(text) ? pageTitle : text;
            return text;
        }

        private static string BuildParameter(string keyword, string tableCtx, string tag, string role)
        {
            if (keyword == "FillTable")
                return "Column:Auto;RowFilter:;Context:" + (string.IsNullOrEmpty(tableCtx) ? "none" : tableCtx);
            if (keyword == "SearchAndClick" || keyword == "SearchAndUpdate")
                return "SearchField:" + tag + ";Role:" + role;
            return string.Empty;
        }

        private static string ResolveKeyword(string tag, string typeAttr, string role, string tableCtx, string source)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(ResolveKeyword),
                (nameof(tag), tag),
                (nameof(typeAttr), typeAttr),
                (nameof(role), role),
                (nameof(tableCtx), tableCtx),
                (nameof(source), source)))
            {
                return ResolveKeywordCore(tag, typeAttr, role, tableCtx, source);
            }
        }

        private static string ResolveKeywordCore(string tag, string typeAttr, string role, string tableCtx, string source)
        {
            if (!string.IsNullOrEmpty(tableCtx) && (source == "click" || source == "change"))
            {
                if (tag == "td" || tag == "th" || tableCtx.StartsWith("webtable:", StringComparison.OrdinalIgnoreCase))
                    return "FillTable";
            }

            if (tag == "select" || role == "combobox" || role == "listbox")
                return "SelectDropDown";

            if (tag == "input" && (typeAttr == "checkbox" || typeAttr == "radio"))
                return "SetBox";

            if (tag == "input" || tag == "textarea")
                return "FillEdit";

            if (tag == "button" || role == "button" || tag == "a" || tag == "img" || tag == "i" || tag == "span")
                return "ClickButton";

            if (source == "click")
                return "ClickButton";

            return "FillEdit";
        }
    }
}
