using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebFillTableImpl : MarsWebKeywordImplBase
    {
        private static string ParseCellControlType(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return string.Empty;
            var parts = parameter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                var p = (parts[i] ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(p))
                    continue;
                if (p.IndexOf(':') >= 0)
                    continue;
                return p.ToLowerInvariant();
            }
            return string.Empty;
        }

        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var obj = await FindObjectAsync(page, step).ConfigureAwait(false);
                if (obj == null)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = "Locator is empty." };
                var mode = ParseCellControlType(step?.Parameter);
                if (string.Equals(mode, "text", StringComparison.OrdinalIgnoreCase))
                {
                    await obj.DblClickAsync().ConfigureAwait(false);
                    var data = step?.Data ?? string.Empty;
                    await page.Keyboard.PressAsync("Control+A").ConfigureAwait(false);
                    await page.Keyboard.PressAsync("Backspace").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(data))
                        await page.Keyboard.TypeAsync(data).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(step?.Data))
                {
                    await obj.FillAsync(step.Data).ConfigureAwait(false);
                }
                else
                {
                    await obj.ClickAsync().ConfigureAwait(false);
                }
                return Ok(step?.Data ?? string.Empty);
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
