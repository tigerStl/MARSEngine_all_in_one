using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public bool IsSyncRequest { get; set; }
    }

    public sealed class RecordingService
    {
        private static readonly Logger Log = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".Services.RecordingService");
        private static string _installScriptCache;
        private static string _installScriptResolvedPath;
        private static readonly object ScriptSync = new object();
        private DateTime? _lastEngineInjectedUtc;

        public string CurrentEngineScriptPath => _installScriptResolvedPath ?? string.Empty;
        public DateTime? LastEngineInjectedUtc => _lastEngineInjectedUtc;

        public event EventHandler<RecorderEventArgs> RecordedStep;
        public event EventHandler<PickEventArgs> Picked;

        private bool _bindingInstalled;
        private IBrowserContext _contextBound;
        private string _lastRecorderMode = "off";
        private IBrowserContext _pageEventSubscribedContext;
        private readonly HashSet<IPage> _pagesWithFrameNavListener = new HashSet<IPage>();
        private WorkbenchSettings _listenerSettings;
        private SemanticStepRecord _lastPegwindowMoveStep;

        public async Task InstallAsync(IPage page, WorkbenchSettings settings = null)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var ctx = page.Context;
            if (_contextBound != ctx)
            {
                DetachContextPageListeners();
                _contextBound = ctx;
                _bindingInstalled = false;
            }

            if (!_bindingInstalled)
            {
                try
                {
                    await ctx.ExposeBindingAsync("marsRecorderPush", (BindingSource _, object payload) =>
                    {
                        OnPayload(payload);
                    }).ConfigureAwait(false);
                    _bindingInstalled = true;
                }
                catch (Exception ex) when (IsTargetClosed(ex))
                {
                    // Context closed during install race (navigation/window close). Let caller retry later.
                    Log.Info(ex, "ExposeBinding skipped because browser context is already closed.");
                    return;
                }
            }

            var script = GetInstallScriptText();
            var ignoredPrefixes = ParseIgnoredPrefixes(settings);
            var tabDepth = settings?.RecorderTabContextAncestorDepth ?? 5;
            if (tabDepth < 1) tabDepth = 1;
            if (tabDepth > 12) tabDepth = 12;
            var scriptWithDepth = "(function(){window.__marsRecoTabAncestorDepth=" + tabDepth + ";})();\n" + script;
            try
            {
                await ctx.AddInitScriptAsync(scriptWithDepth).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                Log.Info(ex, "AddInitScript skipped because target has been closed.");
                return;
            }

            var pages = ctx.Pages?.ToList() ?? new List<IPage>();
            foreach (var p in pages)
            {
                if (ShouldSkipPageByPrefix(p?.Url, ignoredPrefixes))
                    continue;
                try
                {
                    await p.EvaluateAsync(scriptWithDepth).ConfigureAwait(false);
                    var frames = p.Frames?.ToList() ?? new List<IFrame>();
                    foreach (var f in frames)
                    {
                        try
                        {
                            await f.EvaluateAsync(scriptWithDepth).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, "Evaluate recorder script skipped on one frame.");
                        }
                    }
                }
                catch (Exception ex) when (IsTargetClosed(ex))
                {
                    // Page may have closed between snapshot and evaluate; continue other pages.
                    Log.Info(ex, "Skipped recorder install on closed page {Url}", p?.Url);
                }
                catch (Exception ex)
                {
                    // Keep working for remaining pages.
                    Log.Warn(ex, "Failed to evaluate recorder install script on page {Url}", p?.Url);
                }
            }
            _lastEngineInjectedUtc = DateTime.UtcNow;
            _listenerSettings = settings;
            EnsureContextPageListener(page.Context, settings);
            foreach (var p in pages)
                EnsureFrameNavigatedListener(p);
        }

        public async Task ReloadEngineAsync(IPage page, WorkbenchSettings settings = null)
        {
            InvalidateScriptCache();
            if (page == null)
                return;
            await InstallAsync(page, settings).ConfigureAwait(false);
        }

        public void ResetForNewContext()
        {
            DetachContextPageListeners();
            _bindingInstalled = false;
            _contextBound = null;
            _lastRecorderMode = "off";
        }

        public async Task SetModeAsync(IPage page, string mode, WorkbenchSettings settings = null)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            var m = string.IsNullOrEmpty(mode) ? "off" : mode;
            _lastRecorderMode = m;
            var prefixes = ParseIgnoredPrefixes(settings);
            var ctx = page.Context;
            EnsureContextPageListener(ctx, settings ?? _listenerSettings);
            var pages = ctx.Pages?.ToList() ?? new List<IPage>();
            foreach (var p in pages)
            {
                if (ShouldSkipPageByPrefix(p?.Url, prefixes))
                    continue;
                try
                {
                    await ApplyRecorderModeToAllFramesAsync(p, m).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTargetClosed(ex))
                {
                    Log.Info(ex, "Set recorder mode skipped for a page (closed). mode={Mode}", m);
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Set recorder mode failed for page {Url}", p?.Url);
                }
            }
        }

        private void EnsureContextPageListener(IBrowserContext ctx, WorkbenchSettings settings)
        {
            if (ctx == null)
                return;
            if (_pageEventSubscribedContext == ctx)
                return;
            DetachContextPageListeners();
            _listenerSettings = settings ?? _listenerSettings;
            _pageEventSubscribedContext = ctx;
            ctx.Page += OnBrowserContextPageOpened;
        }

        private void DetachContextPageListeners()
        {
            if (_pageEventSubscribedContext != null)
            {
                try
                {
                    _pageEventSubscribedContext.Page -= OnBrowserContextPageOpened;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Detach context Page listener failed.");
                }
                _pageEventSubscribedContext = null;
            }
            foreach (var p in _pagesWithFrameNavListener.ToList())
            {
                try
                {
                    p.FrameNavigated -= OnPageFrameNavigatedApplyMode;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Detach FrameNavigated listener failed.");
                }
            }
            _pagesWithFrameNavListener.Clear();
        }

        private async void OnBrowserContextPageOpened(object sender, IPage newPage)
        {
            if (newPage == null)
                return;
            var prefixes = ParseIgnoredPrefixes(_listenerSettings);
            try
            {
                if (ShouldSkipPageByPrefix(newPage.Url, prefixes))
                    return;
                await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Info(ex, "WaitForLoadState on new page before recorder mode (non-fatal).");
            }
            try
            {
                if (ShouldSkipPageByPrefix(newPage.Url, prefixes))
                    return;
                EnsureFrameNavigatedListener(newPage);
                await ApplyRecorderModeToAllFramesAsync(newPage, _lastRecorderMode).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                Log.Info(ex, "Apply recorder mode on new page skipped (closed).");
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Apply recorder mode on new page failed.");
            }
        }

        private void EnsureFrameNavigatedListener(IPage p)
        {
            if (p == null || _pagesWithFrameNavListener.Contains(p))
                return;
            p.FrameNavigated += OnPageFrameNavigatedApplyMode;
            _pagesWithFrameNavListener.Add(p);
        }

        private async void OnPageFrameNavigatedApplyMode(object sender, IFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(_lastRecorderMode))
                return;
            try
            {
                await frame.EvaluateAsync("m => { window.__marsRecoMode = m; }", _lastRecorderMode).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                // ignore
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Frame mode sync after navigation (non-fatal).");
            }
        }

        private static async Task ApplyRecorderModeToAllFramesAsync(IPage p, string mode)
        {
            if (p == null)
                return;
            var frames = p.Frames?.ToList() ?? new List<IFrame>();
            foreach (var f in frames)
            {
                try
                {
                    await f.EvaluateAsync("m => { window.__marsRecoMode = m; }", mode).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTargetClosed(ex))
                {
                    // continue other frames
                }
            }
        }

        private void OnPayload(object payload)
        {
            try
            {
                var jo = payload is JObject o ? o : JObject.FromObject(payload);
                var kind = (string)jo["Kind"] ?? "record";
                TryLogPageEvent(jo, kind);
                var step = MapToStep(jo);
                if (string.Equals(kind, "pick", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kind, "sync", StringComparison.OrdinalIgnoreCase))
                    Picked?.Invoke(this, new PickEventArgs
                    {
                        Snapshot = step,
                        IsSyncRequest = string.Equals(kind, "sync", StringComparison.OrdinalIgnoreCase)
                    });
                else
                {
                    if (step != null
                        && string.Equals(step.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase)
                        && _lastPegwindowMoveStep != null
                        && string.Equals(_lastPegwindowMoveStep.RecordedPageUrl ?? string.Empty, step.RecordedPageUrl ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastPegwindowMoveStep.TimestampUtc = DateTime.UtcNow;
                        _lastPegwindowMoveStep.Data = step.Data;
                        _lastPegwindowMoveStep.Parameter = step.Parameter;
                        _lastPegwindowMoveStep.RecordedPageTitle = step.RecordedPageTitle;
                        _lastPegwindowMoveStep.SourceEvent = "update";
                        RecordedStep?.Invoke(this, new RecorderEventArgs { Step = _lastPegwindowMoveStep });
                        return;
                    }
                    if (step != null && string.Equals(step.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase))
                        _lastPegwindowMoveStep = step;
                    else if (step != null && !string.Equals(step.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase))
                        _lastPegwindowMoveStep = null;
                    RecordedStep?.Invoke(this, new RecorderEventArgs { Step = step });
                }
            }
            catch (Exception ex)
            {
                // ignore malformed payloads
                Log.Warn(ex, "OnPayload failed to parse/dispatch recorder payload.");
            }
        }

        private static void TryLogPageEvent(JObject jo, string kind)
        {
            try
            {
                var source = (string)jo["SourceEvent"] ?? string.Empty;
                // Focus on user element interactions.
                if (!string.Equals(source, "click", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(source, "input", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(source, "change", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(source, "blur", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var tag = (string)jo["Tag"] ?? string.Empty;
                var role = (string)jo["Role"] ?? string.Empty;
                var text = (string)jo["Text"] ?? string.Empty;
                var locator = (string)jo["Locator"] ?? string.Empty;
                var value = (string)jo["Value"] ?? string.Empty;

                string pos = string.Empty;
                var b = jo["Bounds"];
                if (b != null && b.Type != JTokenType.Null)
                {
                    var x = (double?)b["X"] ?? 0;
                    var y = (double?)b["Y"] ?? 0;
                    var w = (double?)b["Width"] ?? 0;
                    var h = (double?)b["Height"] ?? 0;
                    pos = $"x={x:0.##}, y={y:0.##}, w={w:0.##}, h={h:0.##}";
                }

                Log.Info(
                    "PageEvent kind={Kind} source={Source} pos={Pos} tag={Tag} role={Role} text={Text} value={Value} locator={Locator}",
                    kind,
                    source,
                    pos,
                    tag,
                    role,
                    text,
                    value,
                    locator);
            }
            catch (Exception ex)
            {
                // Never break recorder flow due to logging issues.
                Log.Warn(ex, "TryLogPageEvent failed.");
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
            var locatorAlternates = (string)jo["LocatorAlternates"] ?? string.Empty;
            var elementXpath = (string)jo["ElementXpath"] ?? string.Empty;
            var text = (string)jo["Text"] ?? string.Empty;
            var value = (string)jo["Value"] ?? string.Empty;
            var pageTitle = (string)jo["PageTitle"] ?? string.Empty;
            var tableCtx = (string)jo["TableContext"] ?? string.Empty;
            var chk = jo["Checked"] != null && jo["Checked"].Value<bool>();

            var recorderKeyword = ((string)jo["RecorderKeyword"] ?? string.Empty).Trim();
            var keyword = !string.IsNullOrEmpty(recorderKeyword)
                ? recorderKeyword
                : ResolveKeyword(tag, typeAttr, role, tableCtx, source);
            var logicalKind = (string)jo["LogicalKind"];
            if (string.IsNullOrWhiteSpace(logicalKind))
                logicalKind = InferLogicalKindFallback(tag, typeAttr, role);
            // Keep keyword semantics consistent with logical tab classification.
            if (string.Equals(logicalKind, "webTab", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SelectTab", StringComparison.OrdinalIgnoreCase))
                keyword = "SelectTab";
            if (string.Equals(logicalKind, "webMenu", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SelectMenuItem", StringComparison.OrdinalIgnoreCase))
                keyword = "SelectMenuItem";
            if (string.Equals(logicalKind, "webCombobox", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SelectDropDown", StringComparison.OrdinalIgnoreCase))
                keyword = "SelectDropDown";
            if (string.Equals(logicalKind, "webTable", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "FillTable", StringComparison.OrdinalIgnoreCase))
                keyword = "FillTable";
            if (string.Equals(logicalKind, "webRadio", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SetBox", StringComparison.OrdinalIgnoreCase))
                keyword = "SetBox";
            if (string.Equals(logicalKind, "webCheckbox", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SetBox", StringComparison.OrdinalIgnoreCase))
                keyword = "SetBox";
            if (string.Equals(logicalKind, "webButton", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "ClickButton", StringComparison.OrdinalIgnoreCase))
                keyword = "ClickButton";
            if (string.Equals(logicalKind, "webSelect", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "SelectDropDown", StringComparison.OrdinalIgnoreCase))
                keyword = "SelectDropDown";
            if (string.Equals(logicalKind, "webWindow", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "Pegwindow", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase))
                keyword = "Pegwindow";
            if (string.Equals(logicalKind, "webFileBrowser", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(keyword, "FileBrowser", StringComparison.OrdinalIgnoreCase))
                keyword = "FileBrowser";
            if (string.Equals(keyword, "SelectTab", StringComparison.OrdinalIgnoreCase))
                logicalKind = "webTab";
            if (string.Equals(keyword, "SelectMenuItem", StringComparison.OrdinalIgnoreCase))
                logicalKind = "webMenu";
            if (string.Equals(keyword, "Pegwindow", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase))
                logicalKind = "webWindow";
            if (string.Equals(keyword, "FileBrowser", StringComparison.OrdinalIgnoreCase))
                logicalKind = "webFileBrowser";

            var data = BuildData(keyword, text, value, chk, pageTitle, jo);
            var param = BuildParameter(keyword, tableCtx, tag, role);
            var incomingParam = ((string)jo["Parameter"] ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(incomingParam))
                param = incomingParam;

            string targetTag = null;
            string targetRole = null;
            string targetLocator = null;
            string targetXpath = null;
            if (string.Equals(keyword, "SelectTab", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "SelectMenuItem", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "SelectDropDown", StringComparison.OrdinalIgnoreCase))
            {
                targetTag = (string)jo["TargetTag"];
                targetRole = (string)jo["TargetRole"];
                targetLocator = (string)jo["TargetLocator"];
                targetXpath = (string)jo["TargetXpath"];
            }

            BoundingRectDto bounds = null;
            var b = jo["Bounds"];
            if (b != null && b.Type != JTokenType.Null)
                bounds = b.ToObject<BoundingRectDto>();

            var pageUrl = ((string)jo["PageUrl"] ?? string.Empty).Trim();
            return new SemanticStepRecord
            {
                TimestampUtc = DateTime.UtcNow,
                SourceEvent = source,
                Keyword = keyword,
                Locator = locator,
                LocatorAlternates = locatorAlternates,
                ElementXpath = elementXpath,
                Parameter = param,
                Data = data,
                BoundingRect = bounds,
                LogicalKind = logicalKind,
                TargetTag = targetTag ?? string.Empty,
                TargetRole = targetRole ?? string.Empty,
                TargetLocator = targetLocator ?? string.Empty,
                TargetXpath = targetXpath ?? string.Empty,
                RecordedPageUrl = pageUrl,
                RecordedPageTitle = pageTitle ?? string.Empty
            };
        }

        private static bool IsTextCommitElement(string tag, string typeAttr, string role)
        {
            if (string.Equals(role, "textbox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "searchbox", StringComparison.OrdinalIgnoreCase))
                return true;
            if (tag == "textarea")
                return true;
            if (tag != "input")
                return false;
            var t = typeAttr ?? string.Empty;
            if (t == "checkbox" || t == "radio" || t == "button" || t == "submit" || t == "reset" || t == "file" || t == "hidden")
                return false;
            return true;
        }

        private static string InferLogicalKindFallback(string tag, string typeAttr, string role)
        {
            if (tag == "input" && typeAttr == "checkbox")
                return "webCheckbox";
            if (tag == "input" && typeAttr == "radio")
                return "webRadio";
            if (tag == "input" && typeAttr == "file")
                return "webFileBrowser";
            if (string.Equals(role, "checkbox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "switch", StringComparison.OrdinalIgnoreCase))
                return "webCheckbox";
            if (string.Equals(role, "radio", StringComparison.OrdinalIgnoreCase))
                return "webRadio";
            if (tag == "table")
                return "webTable";
            if (string.Equals(role, "grid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "table", StringComparison.OrdinalIgnoreCase))
                return "webTable";
            if (tag == "select" || role == "combobox" || role == "listbox")
                return "webCombobox";
            if (role == "menu" || role == "menubar" || role == "menuitem")
                return "webMenu";
            if (role == "tab")
                return "webTab";
            if (tag == "button" || typeAttr == "button" || typeAttr == "submit" || typeAttr == "reset" || role == "button")
                return "webButton";
            if (tag == "textarea" || tag == "input" || role == "textbox" || role == "searchbox")
                return "webEdit";
            return "webUnknown";
        }

        private static string BuildData(string keyword, string text, string value, bool chk, string pageTitle, JObject jo = null)
        {
            var srcTag = ((string)(jo?["Tag"] ?? string.Empty) ?? string.Empty).Trim();
            var isInputTag = string.Equals(srcTag, "input", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(keyword, "SelectTab", StringComparison.OrdinalIgnoreCase))
            {
                var tabLabel = jo != null ? (string)jo["TabLabel"] : null;
                if (!string.IsNullOrEmpty(tabLabel))
                    return tabLabel;
                if (!string.IsNullOrEmpty(text))
                    return text;
                return value ?? string.Empty;
            }
            if (string.Equals(keyword, "SelectMenuItem", StringComparison.OrdinalIgnoreCase))
            {
                var menuPath = jo != null ? (string)jo["MenuPath"] : null;
                if (!string.IsNullOrWhiteSpace(menuPath))
                    return menuPath;
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
                return value ?? string.Empty;
            }

            if (string.Equals(keyword, "Pegwindow", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "WindowGeometry", StringComparison.OrdinalIgnoreCase))
            {
                if (jo != null && jo["Value"] != null && jo["Value"].Type != JTokenType.Null)
                {
                    var v = (string)jo["Value"];
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
                if (!string.IsNullOrEmpty(value))
                    return value;
                return text ?? string.Empty;
            }
            if (string.Equals(keyword, "FileBrowser", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(value))
                    return value;
                return text ?? string.Empty;
            }

            if (keyword == "SetBox")
                return chk ? "true" : "false";
            if (isInputTag && !string.IsNullOrEmpty(value))
                return value;
            if (keyword == "FillEdit" || keyword == "SelectDropDown" || keyword == "FillTable")
            {
                if (!string.IsNullOrEmpty(value))
                    return value;
                return text ?? string.Empty;
            }

            if (keyword == "ClickButton")
                return string.IsNullOrEmpty(text) ? pageTitle : text;
            return text ?? string.Empty;
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
            if (string.Equals(source, "blur", StringComparison.OrdinalIgnoreCase))
            {
                if (IsTextCommitElement(tag, typeAttr, role))
                    return "FillEdit";
                return "FillEdit";
            }

            if (!string.IsNullOrEmpty(tableCtx) && (source == "click" || source == "change"))
            {
                if (tag == "td" || tag == "th" || tableCtx.StartsWith("webtable:", StringComparison.OrdinalIgnoreCase))
                    return "FillTable";
            }

            if (tag == "select" || role == "combobox" || role == "listbox")
                return "SelectDropDown";
            if (tag == "input" && typeAttr == "file")
                return "FileBrowser";
            if (role == "menuitem" || role == "menuitemcheckbox" || role == "menuitemradio" || role == "menu" || role == "menubar")
                return "SelectMenuItem";

            if (string.Equals(role, "checkbox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "switch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "radio", StringComparison.OrdinalIgnoreCase))
                return "SetBox";

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

        private static void InvalidateScriptCache()
        {
            lock (ScriptSync)
            {
                _installScriptCache = null;
            }
        }

        private static List<string> ParseIgnoredPrefixes(WorkbenchSettings settings)
        {
            var raw = settings?.RecorderIgnoredPageUrlPrefixes;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "chrome://;devtools://;edge://;about:";
            return raw
                .Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static bool ShouldSkipPageByPrefix(string url, List<string> prefixes)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;
            if (prefixes == null || prefixes.Count == 0)
                return false;
            foreach (var p in prefixes)
            {
                if (url.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsTargetClosed(Exception ex)
        {
            if (ex == null)
                return false;
            var msg = ex.Message ?? string.Empty;
            return msg.IndexOf("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("Target closed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetInstallScriptText()
        {
            lock (ScriptSync)
            {
                if (!string.IsNullOrWhiteSpace(_installScriptCache))
                    return _installScriptCache;
                var rel = ConfigurationManager.AppSettings["RecorderEngineScriptFile"];
                if (string.IsNullOrWhiteSpace(rel))
                    rel = @"scripts\recorder.install.js";
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, rel);
                if (!File.Exists(path))
                    throw new FileNotFoundException("Recorder engine script file not found.", path);
                _installScriptCache = File.ReadAllText(path);
                _installScriptResolvedPath = path;
                return _installScriptCache;
            }
        }
    }
}
