using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebFillTableImpl : MarsWebKeywordImplBase
    {
        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var obj = FindObject(page, step);
                if (obj == null)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = "Locator is empty." };
                if (!string.IsNullOrEmpty(step?.Data))
                    await obj.FillAsync(step.Data).ConfigureAwait(false);
                else
                    await obj.ClickAsync().ConfigureAwait(false);
                return Ok(step?.Data ?? string.Empty);
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
