using System;
using System.Globalization;
using System.Text;
using NLog;

namespace MARS.WebAutomation
{
    /// <summary>
    /// Writes <c>[BEGIN]\tMethod\tparameters</c> and <c>[END]\tMethod</c> to the engine log (use with <c>using</c> so all return paths emit END).
    /// </summary>
    internal static class WebAutomationMethodTrace
    {
        public static IDisposable Begin(Logger log, string methodName, params (string name, object value)[] parameters)
        {
            WebAutomationNLog.EnsureConfigured();

            var sb = new StringBuilder();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(parameters[i].name);
                sb.Append('=');
                sb.Append(FormatValue(parameters[i].value));
            }

            log.Info("[BEGIN]\t{0}\t{1}", methodName, sb.ToString());
            return new EndScope(log, methodName);
        }

        private static string FormatValue(object v)
        {
            if (v == null)
                return "null";

            if (v is string s)
            {
                if (s.Length > 800)
                    return "\"" + s.Substring(0, 800).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\"", "\\\"") + "...(truncated)\"";
                return "\"" + s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\"", "\\\"") + "\"";
            }

            if (v is bool b)
                return b ? "true" : "false";

            if (v is char c)
                return "'" + c + "'";

            if (v is IFormattable fmt)
                return fmt.ToString(null, CultureInfo.InvariantCulture);

            var t = Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;
            return t.Length > 800 ? t.Substring(0, 800) + "...(truncated)" : t;
        }

        private sealed class EndScope : IDisposable
        {
            private readonly Logger _log;
            private readonly string _method;
            private bool _done;

            public EndScope(Logger log, string method)
            {
                _log = log;
                _method = method;
            }

            public void Dispose()
            {
                if (_done)
                    return;
                _done = true;
                _log.Info("[END]\t{0}", _method);
            }
        }
    }
}
