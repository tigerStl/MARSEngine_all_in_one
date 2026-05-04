using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebSelectTabImpl : MarsWebKeywordImplBase
    {
        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var obj = await FindObjectAsync(page, step).ConfigureAwait(false);
                if (obj == null)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = "Locator is empty." };
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
