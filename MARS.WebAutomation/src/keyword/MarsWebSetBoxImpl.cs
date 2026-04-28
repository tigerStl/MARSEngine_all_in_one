using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebSetBoxImpl : MarsWebKeywordImplBase
    {
        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var obj = FindObject(page, step);
                if (obj == null)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = "Locator is empty." };
                var on = string.Equals(step?.Data, "true", StringComparison.OrdinalIgnoreCase);
                await obj.SetCheckedAsync(on).ConfigureAwait(false);
                return Ok(on ? "true" : "false");
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
