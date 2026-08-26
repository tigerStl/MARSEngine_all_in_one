using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public abstract class MarsWebKeywordImplBase
    {
        private static IFrame _preferredReplayFrame;

        /// <summary>Optional per-step override (ms) via <c>LocatorTimeoutMs=</c> or <c>LocatorWaitMs=</c> in <see cref="SemanticStepRecord.Parameter"/>.</summary>
        private static float? ParseLocatorTimeoutMsFromParameter(SemanticStepRecord step)
        {
            if (step == null)
                return null;
            var p = ParseStepParameter(step.Parameter);
            foreach (var key in new[] { "LocatorTimeoutMs", "LocatorWaitMs" })
            {
                if (!p.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                    continue;
                if (float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0f)
                    return v;
            }
            return null;
        }

        /// <summary>Shorter wait when probing many frames so replay does not multiply full timeouts.</summary>
        private static float SubsidiaryFrameWaitMs(float? fullBudgetMs)
        {
            if (fullBudgetMs.HasValue)
                return Math.Max(200f, Math.Min(8000f, fullBudgetMs.Value * 0.35f));
            return 3000f;
        }

        /// <summary>Waits for the first match under <paramref name="scoped"/> to attach; returns that locator or null.</summary>
        private static async Task<ILocator> TryWaitForFirstAttachedAsync(ILocator scoped, float? timeoutMs)
        {
            var first = scoped.First;
            try
            {
                var opts = new LocatorWaitForOptions { State = WaitForSelectorState.Attached };
                if (timeoutMs.HasValue)
                    opts.Timeout = timeoutMs.Value;
                await first.WaitForAsync(opts).ConfigureAwait(false);
                return first;
            }
            catch (PlaywrightException)
            {
                return null;
            }
        }

        public static void SetPreferredReplayFrame(IFrame frame)
        {
            _preferredReplayFrame = frame;
        }

        protected static string EffectivePlaywrightSelector(SemanticStepRecord step) =>
            SemanticStepLocatorUtil.EffectivePlaywrightSelector(step);

        /// <summary>Standard failure when <see cref="FindObjectAsync"/> / <see cref="ResolveLocatorForStepAsync"/> returned null.</summary>
        protected static KeywordExecuteResult LocatorResolveFailed(SemanticStepRecord step)
        {
            if (string.IsNullOrWhiteSpace(SemanticStepLocatorUtil.EffectivePlaywrightSelector(step)))
            {
                return new KeywordExecuteResult
                {
                    Success = false,
                    ErrorMessage = "No Playwright selector resolved. " + SemanticStepLocatorUtil.DescribeMissingSelectors(step)
                };
            }

            var sel = SemanticStepLocatorUtil.EffectivePlaywrightSelector(step);
            return new KeywordExecuteResult
            {
                Success = false,
                ErrorMessage = "No matching element for selector: " + sel
            };
        }

        protected async Task<ILocator> FindObjectAsync(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null)
                return null;
            var sel = SemanticStepLocatorUtil.EffectivePlaywrightSelector(step);
            if (string.IsNullOrWhiteSpace(sel))
                return null;
            var fullBudget = ParseLocatorTimeoutMsFromParameter(step);
            var subBudget = SubsidiaryFrameWaitMs(fullBudget);

            if (_preferredReplayFrame != null)
            {
                try
                {
                    var hit = await TryWaitForFirstAttachedAsync(_preferredReplayFrame.Locator(sel), fullBudget).ConfigureAwait(false);
                    if (hit != null)
                        return hit;
                }
                catch
                {
                    // preferred frame may be detached/cross-origin; continue fallback flow
                }
            }
            try
            {
                var hit = await TryWaitForFirstAttachedAsync(page.Locator(sel), fullBudget).ConfigureAwait(false);
                if (hit != null)
                    return hit;
            }
            catch
            {
                // continue searching child frames
            }

            var seen = new HashSet<IFrame>();
            var frames = page.Frames?.ToList() ?? new List<IFrame>();
            foreach (var f in frames)
            {
                if (f == null || seen.Contains(f))
                    continue;
                if (page.MainFrame != null && ReferenceEquals(f, page.MainFrame))
                    continue;
                seen.Add(f);
                try
                {
                    var hit = await TryWaitForFirstAttachedAsync(f.Locator(sel), subBudget).ConfigureAwait(false);
                    if (hit != null)
                        return hit;
                }
                catch
                {
                    // cross-origin frame / detached frame, ignore and continue
                }
            }
            return null;
        }

        protected static Dictionary<string, string> ParseStepParameter(string raw)
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

        /// <summary>When <c>FramePath</c> is present in <see cref="SemanticStepRecord.Parameter"/>, resolves that frame first; otherwise uses <see cref="FindObjectAsync"/>.</summary>
        protected async Task<ILocator> ResolveLocatorForStepAsync(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null)
                return null;
            var sel = SemanticStepLocatorUtil.EffectivePlaywrightSelector(step);
            if (string.IsNullOrWhiteSpace(sel))
                return null;
            var p = ParseStepParameter(step.Parameter);
            if (p.TryGetValue("FramePath", out var fp) && !string.IsNullOrWhiteSpace(fp))
            {
                var frame = FramePathUtil.ResolveFrameByPath(page, fp.Trim());
                var root = frame ?? page.MainFrame;
                try
                {
                    var fullBudget = ParseLocatorTimeoutMsFromParameter(step);
                    var hit = await TryWaitForFirstAttachedAsync(root.Locator(sel), fullBudget).ConfigureAwait(false);
                    if (hit != null)
                        return hit;
                }
                catch
                {
                    return null;
                }
                return null;
            }
            return await FindObjectAsync(page, step).ConfigureAwait(false);
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
