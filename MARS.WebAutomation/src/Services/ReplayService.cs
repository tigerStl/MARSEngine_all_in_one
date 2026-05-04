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
        private sealed class ReplayCursor
        {
            public IPage Page;
            public IFrame Frame;
        }

        private static Dictionary<string, string> ParseStepParameter(string raw)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return map;
            var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var i = p.IndexOf('=');
                if (i <= 0)
                    continue;
                var k = p.Substring(0, i).Trim();
                var v = i + 1 < p.Length ? p.Substring(i + 1).Trim() : string.Empty;
                if (k.Length == 0)
                    continue;
                map[k] = v;
            }
            return map;
        }

        private static async Task<IPage> ResolvePegwindowTargetPageAsync(IPage primary, SemanticStepRecord step)
        {
            if (primary == null)
                return null;
            var all = OrderedAutomationPages(primary);
            if (all.Count == 0)
                return primary;
            var p = ParseStepParameter(step?.Parameter);
            var pageUrl = p.TryGetValue("PageUrl", out var pv) ? pv : (step?.RecordedPageUrl ?? string.Empty);
            var frameUrl = p.TryGetValue("FrameUrl", out var fv) ? fv : string.Empty;
            var title = p.TryGetValue("Title", out var tv) ? tv : (step?.RecordedPageTitle ?? string.Empty);
            var asFrame = p.TryGetValue("ASIFrame", out var av) && av.Equals("true", StringComparison.OrdinalIgnoreCase);

            IPage best = null;
            var bestScore = int.MinValue;
            foreach (var pg in all)
            {
                var score = await ScorePageMatchAsync(pg, pageUrl, title).ConfigureAwait(false);
                if (asFrame && !string.IsNullOrWhiteSpace(frameUrl))
                {
                    try
                    {
                        var anyFrame = pg.Frames?.Any(f => string.Equals(StripFragment(f.Url ?? string.Empty), StripFragment(frameUrl), StringComparison.OrdinalIgnoreCase)) == true;
                        if (anyFrame) score += 120;
                    }
                    catch
                    {
                        // ignore
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = pg;
                }
            }
            return best ?? primary;
        }

        private static IFrame ResolveFrameByPath(IPage page, string framePath)
        {
            if (page == null || string.IsNullOrWhiteSpace(framePath))
                return null;
            var parts = framePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            IFrame cur = page.MainFrame;
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var idx) || idx < 0)
                    return null;
                var children = cur?.ChildFrames?.ToList() ?? new List<IFrame>();
                if (idx >= children.Count)
                    return null;
                cur = children[idx];
            }
            return cur;
        }

        private static IFrame ResolveFrameOnPage(IPage page, Dictionary<string, string> p, SemanticStepRecord step)
        {
            if (page == null)
                return null;
            var framePath = p.TryGetValue("FramePath", out var fp) ? fp : string.Empty;
            var frameUrl = p.TryGetValue("FrameUrl", out var fu) ? fu : string.Empty;
            var text = p.TryGetValue("Text", out var tx) ? tx : string.Empty;
            if (!string.IsNullOrWhiteSpace(framePath))
            {
                var byPath = ResolveFrameByPath(page, framePath);
                if (byPath != null)
                    return byPath;
            }
            IFrame best = null;
            var bestScore = int.MinValue;
            foreach (var f in page.Frames ?? Array.Empty<IFrame>())
            {
                if (f == null || ReferenceEquals(f, page.MainFrame))
                    continue;
                var score = 0;
                var u = f.Url ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(frameUrl) && string.Equals(StripFragment(u), StripFragment(frameUrl), StringComparison.OrdinalIgnoreCase))
                    score += 100;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var name = f.Name ?? string.Empty;
                    if (string.Equals(name.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase))
                        score += 40;
                }
                if (!string.IsNullOrWhiteSpace(step?.RecordedPageUrl))
                {
                    if (string.Equals(StripFragment(u), StripFragment(step.RecordedPageUrl), StringComparison.OrdinalIgnoreCase))
                        score += 15;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = f;
                }
            }
            return best;
        }

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

            var cursor = new ReplayCursor { Page = page, Frame = null };
            foreach (var step in steps)
            {
                if (string.Equals(step?.Keyword, "Pegwindow", StringComparison.OrdinalIgnoreCase))
                {
                    var target = await ResolvePegwindowTargetPageAsync(cursor.Page ?? page, step).ConfigureAwait(false);
                    if (target != null && !target.IsClosed)
                        cursor.Page = target;
                    var pegParam = ParseStepParameter(step?.Parameter);
                    var asFrame = pegParam.TryGetValue("ASIFrame", out var av) && av.Equals("true", StringComparison.OrdinalIgnoreCase);
                    cursor.Frame = asFrame ? ResolveFrameOnPage(cursor.Page, pegParam, step) : null;
                    if (stepDelayMs > 0)
                        await Task.Delay(stepDelayMs).ConfigureAwait(false);
                    continue;
                }

                MarsWebKeywordImplBase.SetPreferredReplayFrame(cursor.Frame);
                KeywordExecuteResult result;
                try
                {
                    result = await ExecuteKeywordAsync(cursor.Page ?? page, step).ConfigureAwait(false);
                }
                finally
                {
                    MarsWebKeywordImplBase.SetPreferredReplayFrame(null);
                }
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
