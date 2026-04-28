using System;
using System.Collections.Generic;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Services
{
    public sealed class NetworkCaptureService : IDisposable
    {
        private readonly List<NetworkCaptureEntry> _entries = new List<NetworkCaptureEntry>();
        private readonly object _lock = new object();
        private IPage _page;
        private bool _persistSensitive;
        private EventHandler<IRequest> _requestHandler;
        private EventHandler<IResponse> _responseHandler;

        public IReadOnlyList<NetworkCaptureEntry> Entries
        {
            get
            {
                lock (_lock)
                    return _entries.ToArray();
            }
        }

        public void Attach(IPage page, bool persistSensitiveHeaders)
        {
            Detach();
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _persistSensitive = persistSensitiveHeaders;

            _requestHandler = OnRequest;
            _responseHandler = OnResponse;
            _page.Request += _requestHandler;
            _page.Response += _responseHandler;
        }

        public void Detach()
        {
            if (_page == null)
                return;
            try
            {
                if (_requestHandler != null)
                    _page.Request -= _requestHandler;
                if (_responseHandler != null)
                    _page.Response -= _responseHandler;
            }
            catch
            {
                // ignore
            }
            _page = null;
        }

        private static bool IsXHR(IRequest request)
        {
            var t = request.ResourceType ?? string.Empty;
            return string.Equals(t, "xhr", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(t, "fetch", StringComparison.OrdinalIgnoreCase);
        }

        private void OnRequest(object sender, IRequest request)
        {
            if (!IsXHR(request))
                return;

            var entry = new NetworkCaptureEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Method = request.Method,
                Url = request.Url,
                ResourceType = request.ResourceType
            };

            TryCopyHeaders(request.Headers, entry.RequestHeaders);

            lock (_lock)
                _entries.Add(entry);
        }

        private void OnResponse(object sender, IResponse response)
        {
            try
            {
                var req = response.Request;
                if (!IsXHR(req))
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

                if (!_persistSensitive)
                {
                    RedactHeaders(entry.RequestHeaders);
                    RedactHeaders(entry.ResponseHeaders);
                }
            }
            catch
            {
                // ignore
            }
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
    }
}
