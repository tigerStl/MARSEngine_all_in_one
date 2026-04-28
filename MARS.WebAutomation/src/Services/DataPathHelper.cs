using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MARS.WebAutomation.Services
{
    public static class DataPathHelper
    {
        private static readonly Regex InvalidFileChars = new Regex(@"[\\/:\*\?""<>\|]", RegexOptions.Compiled);

        public static string SanitizeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return "unknown_host";
            var h = InvalidFileChars.Replace(host, "_").Trim('.');
            return string.IsNullOrEmpty(h) ? "unknown_host" : h;
        }

        public static string SanitizeUrlToFileKey(Uri uri)
        {
            if (uri == null)
                return "blank";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(uri.Host))
                sb.Append(uri.Host);
            if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                sb.Append(uri.AbsolutePath.Replace('/', '_'));
            if (!string.IsNullOrEmpty(uri.Query))
                sb.Append("_q_" + uri.Query.TrimStart('?'));

            var raw = sb.Length == 0 ? "root" : sb.ToString();
            raw = InvalidFileChars.Replace(raw, "_");
            foreach (var c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');

            if (raw.Length > 120)
                raw = raw.Substring(0, 120) + "_" + Math.Abs(raw.GetHashCode());

            return raw;
        }

        public static string GetDomainKeyFolder(string host)
        {
            return SanitizeHost(host) + "_key";
        }

        public static string BuildJsonPath(string dataRoot, Uri pageUri)
        {
            var domainFolder = GetDomainKeyFolder(pageUri.Host);
            var fileKey = SanitizeUrlToFileKey(pageUri);
            var dir = Path.Combine(dataRoot, domainFolder);
            return Path.Combine(dir, "test_" + fileKey + ".json");
        }

        public static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static string GetAssemblyBaseDirectory()
        {
            var loc = typeof(DataPathHelper).Assembly.Location;
            if (string.IsNullOrEmpty(loc))
                return AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetDirectoryName(loc);
        }
    }
}
