using System;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Linq;

namespace MARS.WebAutomation.Keyword
{
    public abstract class MarsWebKeywordImplBase
    {
        private static IFrame _preferredReplayFrame;

        public static void SetPreferredReplayFrame(IFrame frame)
        {
            _preferredReplayFrame = frame;
        }

        protected async Task<ILocator> FindObjectAsync(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null || string.IsNullOrWhiteSpace(step.Locator))
                return null;
            if (_preferredReplayFrame != null)
            {
                try
                {
                    var fl = _preferredReplayFrame.Locator(step.Locator);
                    if (await fl.CountAsync().ConfigureAwait(false) > 0)
                        return fl.First;
                }
                catch
                {
                    // preferred frame may be detached/cross-origin; continue fallback flow
                }
            }
            try
            {
                var top = page.Locator(step.Locator);
                if (await top.CountAsync().ConfigureAwait(false) > 0)
                    return top.First;
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
                seen.Add(f);
                try
                {
                    var loc = f.Locator(step.Locator);
                    if (await loc.CountAsync().ConfigureAwait(false) > 0)
                        return loc.First;
                }
                catch
                {
                    // cross-origin frame / detached frame, ignore and continue
                }
            }
            return null;
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
