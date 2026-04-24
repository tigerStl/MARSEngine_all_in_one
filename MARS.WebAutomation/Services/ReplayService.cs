using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Services
{
    public sealed class ReplayService
    {
        public async Task ReplayAsync(IPage page, IEnumerable<SemanticStepRecord> steps, int stepDelayMs)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            if (steps == null)
                return;

            foreach (var step in steps)
            {
                await ReplayOneAsync(page, step).ConfigureAwait(false);
                if (stepDelayMs > 0)
                    await Task.Delay(stepDelayMs).ConfigureAwait(false);
            }
        }

        private static async Task ReplayOneAsync(IPage page, SemanticStepRecord step)
        {
            var locator = string.IsNullOrWhiteSpace(step.Locator) ? null : page.Locator(step.Locator);
            var kw = step.Keyword ?? string.Empty;

            if (locator == null)
                return;

            switch (kw)
            {
                case "ClickButton":
                    await locator.ClickAsync().ConfigureAwait(false);
                    break;
                case "FillEdit":
                    await locator.FillAsync(step.Data ?? string.Empty).ConfigureAwait(false);
                    break;
                case "SelectDropDown":
                    await locator.SelectOptionAsync(new SelectOptionValue { Label = step.Data ?? string.Empty }).ConfigureAwait(false);
                    break;
                case "SetBox":
                    var on = string.Equals(step.Data, "true", StringComparison.OrdinalIgnoreCase);
                    await locator.SetCheckedAsync(on).ConfigureAwait(false);
                    break;
                case "FillTable":
                    if (!string.IsNullOrEmpty(step.Data))
                        await locator.FillAsync(step.Data).ConfigureAwait(false);
                    else
                        await locator.ClickAsync().ConfigureAwait(false);
                    break;
                case "SearchAndClick":
                    await locator.ClickAsync().ConfigureAwait(false);
                    break;
                case "SearchAndUpdate":
                    await locator.FillAsync(step.Data ?? string.Empty).ConfigureAwait(false);
                    break;
                default:
                    await locator.ClickAsync().ConfigureAwait(false);
                    break;
            }
        }
    }
}
