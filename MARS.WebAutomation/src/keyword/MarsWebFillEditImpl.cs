using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebFillEditImpl : MarsWebKeywordImplBase
    {
        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var obj = await ResolveLocatorForStepAsync(page, step).ConfigureAwait(false);
                if (obj == null)
                    return LocatorResolveFailed(step);
                await obj.FillAsync(step?.Data ?? string.Empty).ConfigureAwait(false);
                return Ok(step?.Data ?? string.Empty);
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
