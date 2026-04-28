using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MARS.WebAutomation.Keyword;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Services
{
    public sealed class ReplayService
    {
        private static bool IsInternalAutomationUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;
            if (url.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase))
                return true;
            if (url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase))
                return true;
            if (url.StartsWith("chrome-devtools://", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static List<IPage> OrderedAutomationPages(IPage primary)
        {
            var list = new List<IPage>();
            if (primary != null && !primary.IsClosed && !IsInternalAutomationUrl(primary.Url))
                list.Add(primary);
            var ctx = primary?.Context;
            if (ctx?.Pages == null)
                return list;
            foreach (var p in ctx.Pages)
            {
                if (p == null || p.IsClosed || IsInternalAutomationUrl(p.Url))
                    continue;
                if (list.Any(x => ReferenceEquals(x, p)))
                    continue;
                list.Add(p);
            }
            return list;
        }

        private static string StripFragment(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;
            var i = url.IndexOf('#');
            return i >= 0 ? url.Substring(0, i) : url;
        }

        private static async Task<int> ScorePageMatchAsync(IPage page, string recordedUrl, string recordedTitle)
        {
            var score = 0;
            try
            {
                var cur = page.Url ?? string.Empty;
                var rec = recordedUrl ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(rec) && !string.IsNullOrWhiteSpace(cur))
                {
                    if (string.Equals(StripFragment(cur), StripFragment(rec), StringComparison.OrdinalIgnoreCase))
                        score += 100;
                    else if (Uri.TryCreate(cur, UriKind.Absolute, out var uCur)
                        && Uri.TryCreate(rec, UriKind.Absolute, out var uRec)
                        && string.Equals(uCur.GetLeftPart(UriPartial.Path | UriPartial.Query), uRec.GetLeftPart(UriPartial.Path | UriPartial.Query), StringComparison.OrdinalIgnoreCase))
                        score += 90;
                    else if (cur.StartsWith(rec, StringComparison.OrdinalIgnoreCase) || rec.StartsWith(cur, StringComparison.OrdinalIgnoreCase))
                        score += 40;
                }

                if (!string.IsNullOrWhiteSpace(recordedTitle))
                {
                    var t = await page.TitleAsync().ConfigureAwait(false) ?? string.Empty;
                    if (string.Equals(t.Trim(), recordedTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                        score += 50;
                }
            }
            catch
            {
                /* ignore */
            }
            return score;
        }

        /// <summary>Builds pages to try for a step: best URL/title match first, then locator presence, then remaining pages.</summary>
        public async Task<IReadOnlyList<IPage>> BuildCandidatePagesOrderedAsync(IPage primary, SemanticStepRecord step)
        {
            if (primary == null)
                return Array.Empty<IPage>();
            var all = OrderedAutomationPages(primary);
            if (all.Count <= 1)
                return all;

            var recUrl = step?.RecordedPageUrl ?? string.Empty;
            var recTitle = step?.RecordedPageTitle ?? string.Empty;
            var hasMeta = !string.IsNullOrWhiteSpace(recUrl) || !string.IsNullOrWhiteSpace(recTitle);

            if (hasMeta)
            {
                var scored = new List<(IPage P, int S)>();
                foreach (var p in all)
                    scored.Add((p, await ScorePageMatchAsync(p, recUrl, recTitle).ConfigureAwait(false)));
                scored.Sort((a, b) => b.S.CompareTo(a.S));
                var ordered = scored.Select(x => x.P).Distinct().ToList();
                if (scored[0].S > 0)
                    return ordered;
            }

            if (!string.IsNullOrWhiteSpace(step?.Locator))
            {
                var withLoc = new List<IPage>();
                var rest = new List<IPage>();
                foreach (var p in all)
                {
                    try
                    {
                        var n = await p.Locator(step.Locator).CountAsync().ConfigureAwait(false);
                        if (n > 0)
                            withLoc.Add(p);
                        else
                            rest.Add(p);
                    }
                    catch
                    {
                        rest.Add(p);
                    }
                }
                if (withLoc.Count > 0)
                    return withLoc.Concat(rest).Distinct().ToList();
            }

            return all;
        }

        /// <summary>Preferred page for highlight / single-target operations.</summary>
        public async Task<IPage> ResolvePageForStepAsync(IPage primary, SemanticStepRecord step)
        {
            var list = await BuildCandidatePagesOrderedAsync(primary, step).ConfigureAwait(false);
            return list.Count > 0 ? list[0] : primary;
        }

        public async Task ReplayAsync(IPage page, IEnumerable<SemanticStepRecord> steps, int stepDelayMs)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            if (steps == null)
                return;

            foreach (var step in steps)
            {
                var result = await ExecuteKeywordAsync(page, step).ConfigureAwait(false);
                if (!result.Success)
                    throw new InvalidOperationException(result.ErrorMessage ?? "Keyword execution failed.");
                if (stepDelayMs > 0)
                    await Task.Delay(stepDelayMs).ConfigureAwait(false);
            }
        }

        public async Task<KeywordExecuteResult> ExecuteKeywordAsync(IPage primary, SemanticStepRecord step)
        {
            if (primary == null)
                throw new ArgumentNullException(nameof(primary));
            if (step == null)
                return new KeywordExecuteResult { Success = false, ErrorMessage = "Step is null." };

            var candidates = await BuildCandidatePagesOrderedAsync(primary, step).ConfigureAwait(false);
            if (candidates.Count == 0)
                candidates = new List<IPage> { primary };

            KeywordExecuteResult last = null;
            foreach (var p in candidates)
            {
                try
                {
                    var impl = MarsWebKeywordRegistry.Resolve(step);
                    last = await impl.KeywordExecute(p, step).ConfigureAwait(false);
                    if (last.Success)
                        return last;
                }
                catch (Exception ex)
                {
                    last = new KeywordExecuteResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ErrorStackTrace = ex.ToString()
                    };
                }
            }

            return last ?? new KeywordExecuteResult { Success = false, ErrorMessage = "Keyword execution failed." };
        }
    }
}
