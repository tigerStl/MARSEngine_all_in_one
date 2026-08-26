using System;
using System.Linq;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebSelectMenuItemImpl : MarsWebKeywordImplBase
    {
        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            try
            {
                var path = (step?.Data ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(path) && path.Contains(";"))
                {
                    var parts = path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    foreach (var p in parts)
                        await page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = p }).First.ClickAsync().ConfigureAwait(false);
                    return Ok(path);
                }

                var obj = await FindObjectAsync(page, step).ConfigureAwait(false);
                if (obj == null)
                    return LocatorResolveFailed(step);
                await obj.ClickAsync().ConfigureAwait(false);
                return Ok(path);
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }
    }
}
