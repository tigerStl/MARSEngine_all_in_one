using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebAssertLocatorCountImpl : MarsWebKeywordImplBase
    {
        private static Dictionary<string, string> ParseParam(string raw)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return map;
            foreach (var part in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var i = part.IndexOf('=');
                if (i <= 0)
                    continue;
                var k = part.Substring(0, i).Trim();
                var v = i + 1 < part.Length ? part.Substring(i + 1).Trim() : string.Empty;
                if (k.Length > 0)
                    map[k] = v;
            }
            return map;
        }

        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null || string.IsNullOrWhiteSpace(step.Locator))
                return new KeywordExecuteResult { Success = false, ErrorMessage = "Locator is empty." };
            try
            {
                var p = ParseParam(step.Parameter);
                var fp = p.TryGetValue("FramePath", out var f) ? f.Trim() : string.Empty;
                long count;
                if (string.IsNullOrEmpty(fp))
                {
                    count = 0;
                    foreach (var fr in page.Frames ?? Array.Empty<IFrame>())
                    {
                        if (fr == null)
                            continue;
                        try
                        {
                            count += await fr.Locator(step.Locator).CountAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            /* cross-origin / detached */
                        }
                    }
                }
                else
                {
                    var frame = FramePathUtil.ResolveFrameByPath(page, fp);
                    var root = frame ?? page.MainFrame;
                    count = await root.Locator(step.Locator).CountAsync().ConfigureAwait(false);
                }
                long exp = 1;
                if (p.TryGetValue("Expected", out var ev) && long.TryParse(ev, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    exp = parsed;
                if (count != exp)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = $"AssertLocatorCount: expected {exp}, actual {count}." };
                return Ok("Count=" + count.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
