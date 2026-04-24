using System;

namespace MARS.WebAutomation.Models
{
    public sealed class PageInfoDto
    {
        public string OriginalUrl { get; set; }
        public string Title { get; set; }
        public string NormalizedFileKey { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string PathAndQuery { get; set; }
        public string Query { get; set; }

        public static PageInfoDto FromUri(Uri uri, string title, string normalizedFileKey)
        {
            return new PageInfoDto
            {
                OriginalUrl = uri.ToString(),
                Title = title ?? string.Empty,
                NormalizedFileKey = normalizedFileKey,
                Scheme = uri.Scheme,
                Host = uri.Host,
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                PathAndQuery = uri.PathAndQuery,
                Query = uri.Query ?? string.Empty
            };
        }
    }
}
