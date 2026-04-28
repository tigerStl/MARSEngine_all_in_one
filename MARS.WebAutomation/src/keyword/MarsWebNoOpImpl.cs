using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    /// <summary>Steps that are recorded for traceability but do not map to DOM actions (e.g. browser window geometry).</summary>
    public sealed class MarsWebNoOpImpl : MarsWebKeywordImplBase
    {
        public override Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            return Task.FromResult(Ok(step?.Data ?? string.Empty));
        }
    }
}
