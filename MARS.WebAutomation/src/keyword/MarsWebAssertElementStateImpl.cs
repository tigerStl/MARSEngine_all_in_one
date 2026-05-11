using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebAssertElementStateImpl : MarsWebKeywordImplBase
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
                ILocator loc;
                if (string.IsNullOrEmpty(fp))
                {
                    loc = await FindObjectAsync(page, step).ConfigureAwait(false);
                    if (loc == null)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertElementState: locator matched no elements." };
                }
                else
                {
                    var frame = FramePathUtil.ResolveFrameByPath(page, fp);
                    var root = frame ?? page.MainFrame;
                    loc = root.Locator(step.Locator).First;
                    if (await loc.CountAsync().ConfigureAwait(false) == 0)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertElementState: locator matched no elements." };
                }

                if (p.TryGetValue("ReadOnly", out var ro))
                {
                    var expected = ro.Equals("true", StringComparison.OrdinalIgnoreCase);
                    var actual = await loc.EvaluateAsync<bool>(
                        "el => !!(el && (el.readOnly === true || el.hasAttribute('readonly') || (el.getAttribute && (el.getAttribute('aria-readonly')||'')==='true')))").ConfigureAwait(false);
                    if (actual != expected)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"ReadOnly: expected {expected}, actual {actual}." };
                }

                if (p.TryGetValue("Disabled", out var dis))
                {
                    var expected = dis.Equals("true", StringComparison.OrdinalIgnoreCase);
                    var actual = await loc.EvaluateAsync<bool>(
                        "el => !!(el && (el.disabled === true || (el.getAttribute && (el.getAttribute('aria-disabled')||'')==='true')))").ConfigureAwait(false);
                    if (actual != expected)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"Disabled: expected {expected}, actual {actual}." };
                }

                if (p.TryGetValue("AriaDisabled", out var ad))
                {
                    var expected = NormalizeExpectedAttr(ad);
                    var actual = (await loc.EvaluateAsync<string>("el => (el && el.getAttribute) ? (el.getAttribute('aria-disabled')||'') : ''").ConfigureAwait(false) ?? string.Empty).Trim();
                    if (!string.Equals(actual, expected, StringComparison.Ordinal))
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"aria-disabled: expected '{expected}', actual '{actual}'." };
                }

                if (p.TryGetValue("AriaReadonly", out var ar))
                {
                    var expected = NormalizeExpectedAttr(ar);
                    var actual = (await loc.EvaluateAsync<string>("el => (el && el.getAttribute) ? (el.getAttribute('aria-readonly')||'') : ''").ConfigureAwait(false) ?? string.Empty).Trim();
                    if (!string.Equals(actual, expected, StringComparison.Ordinal))
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"aria-readonly: expected '{expected}', actual '{actual}'." };
                }

                if (p.TryGetValue("ContentEditable", out var ce))
                {
                    var expected = ce.Equals("true", StringComparison.OrdinalIgnoreCase);
                    var actual = await loc.EvaluateAsync<bool>(
                        "el => !!(el && (((el.getAttribute('contenteditable')||'').toLowerCase()==='true') || el.isContentEditable === true))").ConfigureAwait(false);
                    if (actual != expected)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"ContentEditable: expected {expected}, actual {actual}." };
                }

                if (p.TryGetValue("Color", out var col) && !string.IsNullOrWhiteSpace(col))
                {
                    var actual = (await loc.EvaluateAsync<string>(
                        "el => { try { return window.getComputedStyle(el).getPropertyValue('color').trim(); } catch(e){ return ''; } }").ConfigureAwait(false) ?? string.Empty).Trim();
                    if (!string.Equals(NormalizeCssColor(actual), NormalizeCssColor(col), StringComparison.OrdinalIgnoreCase))
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"color: expected '{col}', actual '{actual}'." };
                }

                if (p.TryGetValue("BackgroundColor", out var bg) && !string.IsNullOrWhiteSpace(bg))
                {
                    var actual = (await loc.EvaluateAsync<string>(
                        "el => { try { return window.getComputedStyle(el).getPropertyValue('background-color').trim(); } catch(e){ return ''; } }").ConfigureAwait(false) ?? string.Empty).Trim();
                    if (!string.Equals(NormalizeCssColor(actual), NormalizeCssColor(bg), StringComparison.OrdinalIgnoreCase))
                        return new KeywordExecuteResult { Success = false, ErrorMessage = $"background-color: expected '{bg}', actual '{actual}'." };
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }

        private static string NormalizeExpectedAttr(string raw)
        {
            if (string.Equals(raw, "(empty)", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return raw ?? string.Empty;
        }

        private static string NormalizeCssColor(string c)
        {
            if (string.IsNullOrWhiteSpace(c))
                return string.Empty;
            return c.Trim().ToLowerInvariant();
        }
    }
}
