using System;
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

        public IPage Page => _page;
        public IBrowserContext Context => _context;
        public bool IsRunning => _page != null;

        public async Task StartAsync(WorkbenchSettings settings)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(StartAsync),
                (nameof(settings), settings == null ? "null" : $"Headless={settings.Headless},Channel={settings.BrowserChannel},TimeoutMs={settings.DefaultTimeoutMs},Viewport={settings.ViewportWidth}x{settings.ViewportHeight}")))
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));

                await ShutdownAsync().ConfigureAwait(false);

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
            }
        }

        public async Task NavigateAsync(string url)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not started.");
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        }

        /// <summary>
        /// Attaches to an existing Chromium-based browser via CDP (e.g. Chrome with <c>--remote-debugging-port=9222</c>).
        /// Playwright call: <see cref="IChromium.ConnectOverCDPAsync"/> on line with <c>ConnectOverCDPAsync</c>.
        /// </summary>
        public async Task<bool> TryConnectChromiumOverCdpAsync(WorkbenchSettings settings, params string[] endpoints)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(TryConnectChromiumOverCdpAsync),
                (nameof(settings), settings == null ? "null" : $"TimeoutMs={settings.DefaultTimeoutMs}"),
                (nameof(endpoints), endpoints == null ? "null" : string.Join(", ", endpoints))))
            {
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));
                if (endpoints == null || endpoints.Length == 0)
                    return false;

                await ShutdownAsync().ConfigureAwait(false);

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
                            Log.Warn("CDP attach: no browser contexts after ConnectOverCDP for {Endpoint}", ep);
                            await ShutdownAsync().ConfigureAwait(false);
                            continue;
                        }

                        _context = defaultContext;
                        _page = _context.Pages.FirstOrDefault(pg =>
                                   !string.IsNullOrEmpty(pg.Url)
                                   && !pg.Url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase)
                                   && !pg.Url.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(pg.Url, "about:blank", StringComparison.OrdinalIgnoreCase))
                               ?? _context.Pages.FirstOrDefault();

                        if (_page == null)
                        {
                            Log.Warn("CDP attach: no suitable page in context for {Endpoint}; page count={Count}",
                                ep, _context.Pages.Count);
                            await ShutdownAsync().ConfigureAwait(false);
                            continue;
                        }

                        _page.SetDefaultTimeout(Math.Max(1000, settings.DefaultTimeoutMs));
                        Log.Info("CDP attach succeeded: endpoint={Endpoint}, pageUrl={PageUrl}", ep, _page.Url);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex, "CDP attach failed for endpoint {Endpoint}", ep);
                        try
                        {
                            await ShutdownAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            /* ignore */
                        }
                    }
                }

                return false;
            }
        }

        public async Task ShutdownAsync()
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(ShutdownAsync)))
            {
                try
                {
                    if (_page != null)
                    {
                        await _page.CloseAsync().ConfigureAwait(false);
                        _page = null;
                    }
                }
                catch
                {
                    /* ignore */
                }

                try
                {
                    if (_context != null)
                    {
                        await _context.CloseAsync().ConfigureAwait(false);
                        _context = null;
                    }
                }
                catch
                {
                    /* ignore */
                }

                try
                {
                    if (_browser != null)
                    {
                        await _browser.CloseAsync().ConfigureAwait(false);
                        _browser = null;
                    }
                }
                catch
                {
                    /* ignore */
                }

                try
                {
                    _playwright?.Dispose();
                    _playwright = null;
                }
                catch
                {
                    /* ignore */
                }
            }
        }

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}
