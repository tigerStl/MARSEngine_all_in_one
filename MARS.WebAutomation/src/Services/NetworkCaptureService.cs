using System;
using System.Collections.Generic;
using System.Linq;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Services
{
    public sealed class NetworkCaptureService : IDisposable
    {
        public sealed class NetworkCaptureEntryEventArgs : EventArgs
        {
            public NetworkCaptureEntry Entry { get; set; }
        }

        private readonly List<NetworkCaptureEntry> _entries = new List<NetworkCaptureEntry>();
        private readonly object _lock = new object();
        private IPage _page;
        private IBrowserContext _context;
        private bool _persistSensitive;
        private EventHandler<IRequest> _requestHandler;
        private EventHandler<IResponse> _responseHandler;

        /// <summary>When set and returns false, request/response capture is skipped (no entries, no <see cref="EntryCompleted"/>).</summary>
        public Func<bool> IsCaptureEnabled { get; set; }

        public IReadOnlyList<NetworkCaptureEntry> Entries
        {
            get
            {
                lock (_lock)
                    return _entries.ToArray();
            }
        }

        public event EventHandler<NetworkCaptureEntryEventArgs> EntryCompleted;

        public void Attach(IPage page, bool persistSensitiveHeaders)
        {
            Detach();
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _context = _page.Context;
            _persistSensitive = persistSensitiveHeaders;

            _requestHandler = OnRequest;
            _responseHandler = OnResponse;
            if (_context != null)
            {
                _context.Request += _requestHandler;
                _context.Response += _responseHandler;
            }
            else
            {
                // Fallback when context is unavailable.
                _page.Request += _requestHandler;
                _page.Response += _responseHandler;
            }
        }

        public void Detach()
        {
            if (_page == null && _context == null)
                return;
            try
            {
                if (_context != null)
                {
                    if (_requestHandler != null)
                        _context.Request -= _requestHandler;
                    if (_responseHandler != null)
                        _context.Response -= _responseHandler;
                }
                if (_page != null)
                {
                    if (_requestHandler != null)
                        _page.Request -= _requestHandler;
                    if (_responseHandler != null)
                        _page.Response -= _responseHandler;
                }
            }
            catch
            {
                // ignore
            }
            _page = null;
            _context = null;
        }

        private static bool IsTrackedRequest(IRequest request, out string reason)
        {
            // Diagnostic mode: capture everything first, then decide what to filter.
            reason = request == null ? "request-null" : "all-capture";
            return request != null;
        }

        private void OnRequest(object sender, IRequest request)
        {
            var tracked = IsTrackedRequest(request, out _);
            if (!tracked)
                return;
            if (IsCaptureEnabled != null && !IsCaptureEnabled())
                return;

            var entry = new NetworkCaptureEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Method = request.Method,
                Url = request.Url,
                ResourceType = request.ResourceType,
                RequestBody = TrimForStorage(request.PostData)
            };

            TryCopyHeaders(request.Headers, entry.RequestHeaders);

            lock (_lock)
                _entries.Add(entry);
            EntryCompleted?.Invoke(this, new NetworkCaptureEntryEventArgs { Entry = entry });
        }

        private async void OnResponse(object sender, IResponse response)
        {
            try
            {
                var req = response.Request;
                var tracked = IsTrackedRequest(req, out _);
                if (!tracked)
                    return;
                if (IsCaptureEnabled != null && !IsCaptureEnabled())
                    return;

                NetworkCaptureEntry entry = null;
                lock (_lock)
                {
                    for (var i = _entries.Count - 1; i >= 0; i--)
                    {
                        if (_entries[i].Url == req.Url && _entries[i].Method == req.Method && _entries[i].Status == null)
                        {
                            entry = _entries[i];
                            break;
                        }
                    }
                }

                if (entry == null)
                    return;

                entry.Status = response.Status;
                TryCopyHeaders(response.Headers, entry.ResponseHeaders);
                try
                {
                    entry.ResponseBody = TrimForStorage(await response.TextAsync().ConfigureAwait(false));
                }
                catch
                {
                    // Keep the capture entry even if response body is not text-readable (e.g. binary/image stream).
                    entry.ResponseBody = string.Empty;
                }
                if (string.IsNullOrWhiteSpace(entry.CookiesSummary))
                    entry.CookiesSummary = ExtractCookiesFromHeaders(entry.RequestHeaders);

                if (!_persistSensitive)
                {
                    RedactHeaders(entry.RequestHeaders);
                    RedactHeaders(entry.ResponseHeaders);
                    entry.RequestBody = RedactPotentialSecrets(entry.RequestBody);
                    entry.ResponseBody = RedactPotentialSecrets(entry.ResponseBody);
                }

                EntryCompleted?.Invoke(this, new NetworkCaptureEntryEventArgs { Entry = entry });
            }
            catch
            {
                // ignore
            }
        }

        private static string RedactPotentialSecrets(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;
            var text = raw.Trim();

            // Keep form structure visible for parameterization; only mask sensitive field values.
            if (text.IndexOf('=') >= 0 && text.IndexOf('&') >= 0)
            {
                var parts = text.Split(new[] { '&' }, StringSplitOptions.None);
                for (var i = 0; i < parts.Length; i++)
                {
                    var kv = parts[i];
                    var pos = kv.IndexOf('=');
                    if (pos <= 0)
                        continue;
                    var key = kv.Substring(0, pos);
                    if (IsSensitiveFieldName(key))
                        parts[i] = key + "=[redacted]";
                }
                return string.Join("&", parts);
            }

            if (text.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("authorization", StringComparison.OrdinalIgnoreCase) >= 0)
                return "[redacted]";

            return raw;
        }

        private static bool IsSensitiveFieldName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            var k = key.Trim();
            return k.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0
                   || k.IndexOf("passwd", StringComparison.OrdinalIgnoreCase) >= 0
                   || k.IndexOf("authorization", StringComparison.OrdinalIgnoreCase) >= 0
                   || k.IndexOf("access_token", StringComparison.OrdinalIgnoreCase) >= 0
                   || k.IndexOf("refresh_token", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ExtractCookiesFromHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return string.Empty;
            var kv = headers.FirstOrDefault(p => p.Key.Equals("cookie", StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(kv.Value) ? string.Empty : kv.Value;
        }

        private static string TrimForStorage(string raw, int max = 4000)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;
            if (raw.Length <= max)
                return raw;
            return raw.Substring(0, max) + "...[truncated]";
        }

        public static async System.Threading.Tasks.Task<string> BuildCookiesSummaryAsync(IBrowserContext context)
        {
            if (context == null)
                return string.Empty;
            try
            {
                var cookies = await context.CookiesAsync().ConfigureAwait(false);
                if (cookies == null || cookies.Count == 0)
                    return string.Empty;
                var sb = new System.Text.StringBuilder();
                foreach (var c in cookies)
                {
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(c.Name).Append("=").Append(c.Value);
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void RedactHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                return;
            var keys = new List<string>(headers.Keys);
            foreach (var k in keys)
            {
                if (k.Equals("authorization", StringComparison.OrdinalIgnoreCase)
                    || k.Equals("cookie", StringComparison.OrdinalIgnoreCase)
                    || k.Equals("set-cookie", StringComparison.OrdinalIgnoreCase))
                {
                    headers[k] = "[redacted]";
                }
            }
        }

        private static void TryCopyHeaders(IDictionary<string, string> from, Dictionary<string, string> to)
        {
            if (from == null)
                return;
            foreach (var kv in from)
            {
                if (!to.ContainsKey(kv.Key))
                    to[kv.Key] = kv.Value;
            }
        }

        public void Clear()
        {
            lock (_lock)
                _entries.Clear();
        }

        public void Dispose()
        {
            Detach();
        }

        // debug traces removed
    }
}
