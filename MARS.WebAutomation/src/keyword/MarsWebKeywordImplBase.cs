using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public abstract class MarsWebKeywordImplBase
    {
        protected ILocator FindObject(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null || string.IsNullOrWhiteSpace(step.Locator))
                return null;
            return page.Locator(step.Locator).First;
        }

        public abstract Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step);

        protected static KeywordExecuteResult Ok(string dataReturned = null)
        {
            return new KeywordExecuteResult { Success = true, DataReturned = dataReturned ?? string.Empty };
        }

        protected static KeywordExecuteResult Fail(Exception ex)
        {
            return new KeywordExecuteResult
            {
                Success = false,
                ErrorMessage = ex?.Message ?? "Unknown error",
                ErrorStackTrace = ex?.ToString() ?? string.Empty
            };
        }
    }
}
