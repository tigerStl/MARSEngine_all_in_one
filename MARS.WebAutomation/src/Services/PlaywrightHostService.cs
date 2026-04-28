using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using MARS.WebAutomation.Models;
using NLog;

namespace MARS.WebAutomation.Services
{
    /// <summary>
    /// Playwright lifecycle: launch new browser, attach over CDP, or shutdown.
    /// CDP attach entry: <see cref="TryConnectChromiumOverCdpAsync"/> → <c>Chromium.ConnectOverCDPAsync</c>.
    /// </summary>
    public sealed class PlaywrightHostService : IDisposable
    {
        private static readonly Logger Log = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".Services.PlaywrightHostService");

        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPage _page;
        private bool _attachedOverCdp;

        public IPage Page => _page;
        public IBrowserContext Context => _context;
        public bool IsRunning => _page != null;

        /// <summary>Fired when the active page's main-frame URL changes (navigation, SPA hash, etc.).</summary>
        public event EventHandler<string> ActiveDocumentUrlChanged;

        public async Task StartAsync(WorkbenchSettings settings)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(StartAsync),
                (nameof(settings), settings == null ? "null" : $"Headless={settings.Headless},Channel={settings.BrowserChannel},TimeoutMs={settings.DefaultTimeoutMs},Viewport={settings.ViewportWidth}x{settings.ViewportHeight}")))
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));

                await ShutdownAsync().ConfigureAwait(false);
                _attachedOverCdp = false;

                _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = settings.Headless,
                    Channel = string.IsNullOrWhiteSpace(settings.BrowserChannel) ? null : settings.BrowserChannel.Trim()
                }).ConfigureAwait(false);

                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize
                    {
                        Width = Math.Max(400, settings.ViewportWidth),
                        Height = Math.Max(300, settings.ViewportHeight)
                    }
                }).ConfigureAwait(false);

                _page = await _context.NewPageAsync().ConfigureAwait(false);
                _page.SetDefaultTimeout(Math.Max(1000, settings.DefaultTimeoutMs));
                WireActivePageUrlEvents();
                RaiseDocumentUrl(_page.Url);
            }
        }

        public async Task NavigateAsync(string url)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not started.");
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
            RaiseDocumentUrl(_page.Url ?? string.Empty);
        }

        /// <summary>
        /// Attaches to an existing Chromium-based browser via CDP (e.g. Chrome with <c>--remote-debugging-port=9222</c>).
        /// Playwright call: <see cref="IChromium.ConnectOverCDPAsync"/> on line with <c>ConnectOverCDPAsync</c>.
        /// On failure, <see cref="CdpAttachResult.ErrorMessage"/> contains the last error (e.g. missing Playwright driver node).
        /// </summary>
        public Task<CdpAttachResult> TryConnectChromiumOverCdpAsync(WorkbenchSettings settings, params string[] endpoints) =>
            TryConnectChromiumOverCdpAsync(settings, endpoints, null);

        /// <param name="pickContext">When set (Web Spy), selects the browser tab that best matches the drop point / UIA hints instead of the first matching page.</param>
        public async Task<CdpAttachResult> TryConnectChromiumOverCdpAsync(WorkbenchSettings settings, string[] endpoints, global::MARS.WebAutomation.WebSpyPickContext pickContext)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(TryConnectChromiumOverCdpAsync),
                (nameof(settings), settings == null ? "null" : $"TimeoutMs={settings.DefaultTimeoutMs}"),
                (nameof(endpoints), endpoints == null ? "null" : string.Join(", ", endpoints))))
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));
                if (endpoints == null || endpoints.Length == 0)
                    return CdpAttachResult.Failed("No CDP endpoints were supplied.");

                await ShutdownAsync().ConfigureAwait(false);

                string lastError = null;
                foreach (var raw in endpoints)
                {
                    var ep = (raw ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(ep))
                        continue;
                    try
                    {
                        Log.Info("CDP attach attempt: endpoint={Endpoint}", ep);
                        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                        _browser = await _playwright.Chromium.ConnectOverCDPAsync(ep).ConfigureAwait(false);

                        var defaultContext = _browser.Contexts.FirstOrDefault();
                        if (defaultContext == null)
                        {
                            lastError = "Connected to " + ep + " but the browser reported no contexts.";
                            Log.Warn("CDP attach: no browser contexts after ConnectOverCDP for {Endpoint}", ep);
                            await ShutdownAsync().ConfigureAwait(false);
                            continue;
                        }

                        _context = defaultContext;
                        _page = await PickAttachedPageAsync(_context, pickContext).ConfigureAwait(false);

                        if (_page == null)
                        {
                            lastError = "Connected to " + ep + " but no usable page was found (empty/devtools/chrome URLs only). Page count: " + _context.Pages.Count + ".";
                            Log.Warn("CDP attach: no suitable page in context for {Endpoint}; page count={Count}",
                                ep, _context.Pages.Count);
                            await ShutdownAsync().ConfigureAwait(false);
                            continue;
                        }

                        _page.SetDefaultTimeout(Math.Max(1000, settings.DefaultTimeoutMs));
                        _attachedOverCdp = true;
                        WireActivePageUrlEvents();
                        RaiseDocumentUrl(_page.Url);
                        Log.Info("CDP attach succeeded: endpoint={Endpoint}, pageUrl={PageUrl}", ep, _page.Url);
                        return CdpAttachResult.Ok();
                    }
                    catch (Exception ex)
                    {
                        lastError = FormatCdpAttachException(ep, ex);
                        Log.Warn(ex, "CDP attach failed for endpoint {Endpoint}", ep);
                        try
                        {
                            await ShutdownAsync().ConfigureAwait(false);
                        }
                        catch (Exception shutdownEx)
                        {
                            Log.Warn(shutdownEx, "ShutdownAsync failed after CDP attach failure.");
                        }
                    }
                }

                return CdpAttachResult.Failed(lastError ?? "Could not attach over CDP on any of the given endpoints.");
            }
        }

        private static bool IsUsablePageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase))
                return false;
            if (url.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(url, "about:blank", StringComparison.OrdinalIgnoreCase))
                return false;
            if (url.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        /// <summary>
        /// Picks the page that represents the user's current tab: prefer URL/title match from UIA, then viewport hit-test at screen coords, else last usable tab (often rightmost / front in Chromium ordering).
        /// </summary>
        private static async Task<IPage> PickAttachedPageAsync(IBrowserContext context, global::MARS.WebAutomation.WebSpyPickContext pick)
        {
            if (context == null)
                return null;

            var pages = context.Pages.Where(p => IsUsablePageUrl(p.Url)).ToList();
            if (pages.Count == 0)
                return context.Pages.FirstOrDefault();

            if (pages.Count == 1)
                return pages[0];

            if (pick != null)
            {
                foreach (var page in Enumerable.Reverse(pages))
                {
                    if (!string.IsNullOrWhiteSpace(pick.UiName))
                    {
                        var hint = pick.UiName.Trim();
                        if (hint.Length > 1)
                        {
                            if (page.Url != null && page.Url.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                                return page;
                            try
                            {
                                var title = await page.TitleAsync().ConfigureAwait(false);
                                if (!string.IsNullOrEmpty(title) && title.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                                    return page;
                            }
                            catch (Exception titleEx)
                            {
                                Log.Debug(titleEx, "TitleAsync failed while selecting attached page.");
                            }
                        }
                    }
                }

                var sx = pick.ScreenX;
                var sy = pick.ScreenY;
                foreach (var page in Enumerable.Reverse(pages))
                {
                    try
                    {
                        var hit = await page.EvaluateAsync<bool>(@"o => {
                            try {
                              const sx = o.sx|0, sy = o.sy|0;
                              const sw = window.screenX !== undefined ? window.screenX : (window.screenLeft || 0);
                              const sh = window.screenY !== undefined ? window.screenY : (window.screenTop || 0);
                              const outerW = window.outerWidth || 0;
                              const innerW = window.innerWidth || 0;
                              const outerH = window.outerHeight || 0;
                              const innerH = window.innerHeight || 0;
                              const chromeX = Math.max(0, Math.floor((outerW - innerW) / 2));
                              const chromeY = Math.max(0, outerH - innerH);
                              const x = sx - sw - chromeX;
                              const y = sy - sh - chromeY;
                              if (x < -20 || y < -20 || x > innerW + 40 || y > innerH + 160) return false;
                              const px = Math.max(0, Math.min((innerW || 1) - 1, x));
                              const py = Math.max(0, Math.min((innerH || 1) - 1, y));
                              const el = document.elementFromPoint(px, py);
                              return !!el;
                            } catch (e) { return false; }
                        }", new { sx, sy }).ConfigureAwait(false);
                        if (hit)
                            return page;
                    }
                    catch (Exception hitEx)
                    {
                        Log.Debug(hitEx, "Viewport hit-test failed while selecting attached page.");
                    }
                }
            }

            return pages[pages.Count - 1];
        }

        private void WireActivePageUrlEvents()
        {
            UnwireActivePageUrlEvents();
            if (_page == null)
                return;
            _page.FrameNavigated += OnActivePageFrameNavigated;
        }

        private void UnwireActivePageUrlEvents()
        {
            try
            {
                if (_page != null)
                    _page.FrameNavigated -= OnActivePageFrameNavigated;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "UnwireActivePageUrlEvents failed.");
            }
        }

        private void OnActivePageFrameNavigated(object sender, IFrame frame)
        {
            try
            {
                if (_page == null || frame == null)
                    return;
                if (!frame.Equals(_page.MainFrame))
                    return;
                RaiseDocumentUrl(_page.Url);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "OnActivePageFrameNavigated failed.");
            }
        }

        private void RaiseDocumentUrl(string url)
        {
            try
            {
                ActiveDocumentUrlChanged?.Invoke(this, url ?? string.Empty);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "ActiveDocumentUrlChanged subscriber raised an exception.");
            }
        }

        private static string FormatCdpAttachException(string endpoint, Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Endpoint: ").AppendLine(endpoint);
            for (var e = ex; e != null; e = e.InnerException)
            {
                sb.AppendLine(e.GetType().Name + ": " + e.Message);
            }

            var combined = sb.ToString();
            if (combined.IndexOf("node.exe", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Driver not found", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf(".playwright", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                sb.AppendLine();
                sb.AppendLine("Playwright driver is missing or the path is wrong. From the folder that contains Microsoft.Playwright.dll, run:");
                sb.AppendLine("  pwsh .\\playwright.ps1 install");
                sb.AppendLine("or install browsers as described in the Playwright .NET documentation. Then copy the .playwright folder next to the host EXE if needed.");
            }

            return sb.ToString().TrimEnd();
        }

        public async Task ShutdownAsync()
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(ShutdownAsync)))
            {
                UnwireActivePageUrlEvents();
                try
                {
                    if (_page != null && !_attachedOverCdp)
                    {
                        await _page.CloseAsync().ConfigureAwait(false);
                    }
                    _page = null;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to close Playwright page.");
                }

                try
                {
                    if (_context != null && !_attachedOverCdp)
                    {
                        await _context.CloseAsync().ConfigureAwait(false);
                    }
                    _context = null;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to close Playwright context.");
                }

                try
                {
                    if (_browser != null && !_attachedOverCdp)
                    {
                        await _browser.CloseAsync().ConfigureAwait(false);
                    }
                    _browser = null;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to close Playwright browser.");
                }

                try
                {
                    _playwright?.Dispose();
                    _playwright = null;
                    _attachedOverCdp = false;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to dispose Playwright instance.");
                }
            }
        }

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}
