using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;

namespace MARS.WebAutomation.Services
{
    public sealed class DomAssertionDiffMetrics
    {
        public long BeforeCaptureMs { get; set; }
        public long AfterCaptureMs { get; set; }
        public long DiffMs { get; set; }
        public int BeforeElementCount { get; set; }
        public int AfterElementCount { get; set; }
        public int StateChangeCount { get; set; }
        public int NewElementCount { get; set; }
        public int ScreenshotStepsAdded { get; set; }

        public string ToStatusSummary()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "assertDiff before={0}ms after={1}ms diff={2}ms elemsBefore={3} elemsAfter={4} stateChg={5} newEl={6} shot={7}",
                BeforeCaptureMs, AfterCaptureMs, DiffMs, BeforeElementCount, AfterElementCount,
                StateChangeCount, NewElementCount, ScreenshotStepsAdded);
        }
    }

    public sealed class DomAssertionCaptureResult
    {
        public List<DomAssertionElementState> Elements { get; } = new List<DomAssertionElementState>();
        public long CaptureMs { get; set; }
        public string PageUrl { get; set; }
        public string PageTitle { get; set; }
    }

    /// <summary>Collects per-frame DOM assertion surfaces and builds <see cref="SemanticStepRecord"/> diffs.</summary>
    public static class DomAssertionSnapshotService
    {
        public static async Task<DomAssertionCaptureResult> CapturePageAsync(IPage page, int settleMs, int maxPerFrame)
        {
            var sw = Stopwatch.StartNew();
            var result = new DomAssertionCaptureResult();
            if (page == null || page.IsClosed)
                return result;
            try
            {
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded).ConfigureAwait(false);
            }
            catch
            {
                /* non-fatal */
            }
            if (settleMs > 0)
                await Task.Delay(settleMs).ConfigureAwait(false);
            try
            {
                result.PageUrl = page.Url ?? string.Empty;
                result.PageTitle = await page.TitleAsync().ConfigureAwait(false) ?? string.Empty;
            }
            catch
            {
                result.PageUrl = string.Empty;
                result.PageTitle = string.Empty;
            }

            var frames = page.Frames?.ToList() ?? new List<IFrame>();
            foreach (var fr in frames)
            {
                if (fr == null)
                    continue;
                string json;
                try
                {
                    json = await fr.EvaluateAsync<string>(DomAssertionCaptureScripts.CollectInteractiveSurfaceJson, maxPerFrame).ConfigureAwait(false)
                        ?? "[]";
                }
                catch
                {
                    continue;
                }
                var fp = FramePathUtil.BuildIndexedPathToMain(fr, page);
                JArray arr;
                try
                {
                    arr = JArray.Parse(json);
                }
                catch
                {
                    continue;
                }
                foreach (var t in arr)
                {
                    if (!(t is JObject jo))
                        continue;
                    var row = new DomAssertionElementState
                    {
                        FramePath = fp ?? string.Empty,
                        CssLocator = jo.Value<string>("CssLocator") ?? string.Empty,
                        Xpath = jo.Value<string>("Xpath") ?? string.Empty,
                        Signature = jo.Value<string>("Signature") ?? string.Empty,
                        Tag = jo.Value<string>("Tag") ?? string.Empty,
                        ReadOnly = jo.Value<bool?>("ReadOnly") == true,
                        Disabled = jo.Value<bool?>("Disabled") == true,
                        AriaDisabled = jo.Value<string>("AriaDisabled") ?? string.Empty,
                        AriaReadonly = jo.Value<string>("AriaReadonly") ?? string.Empty,
                        ContentEditable = jo.Value<bool?>("ContentEditable") == true,
                        Color = jo.Value<string>("Color") ?? string.Empty,
                        BackgroundColor = jo.Value<string>("BackgroundColor") ?? string.Empty
                    };
                    if (string.IsNullOrWhiteSpace(row.CssLocator))
                        continue;
                    row.Signature = string.IsNullOrWhiteSpace(row.Signature)
                        ? fp + "\x1f" + row.CssLocator
                        : fp + "\x1f" + row.Signature;
                    result.Elements.Add(row);
                }
            }

            sw.Stop();
            result.CaptureMs = sw.ElapsedMilliseconds;
            return result;
        }

        public static List<SemanticStepRecord> BuildStepsFromDiff(
            IReadOnlyList<DomAssertionElementState> before,
            IReadOnlyList<DomAssertionElementState> after,
            SemanticStepRecord anchorMeta,
            bool emitScreenshotOnColorChange,
            string screenshotBaselineDirectoryAbsolute,
            out DomAssertionDiffMetrics metrics)
        {
            metrics = new DomAssertionDiffMetrics
            {
                BeforeElementCount = before?.Count ?? 0,
                AfterElementCount = after?.Count ?? 0
            };
            var sw = Stopwatch.StartNew();
            var steps = new List<SemanticStepRecord>();
            if (after == null || after.Count == 0)
            {
                sw.Stop();
                metrics.DiffMs = sw.ElapsedMilliseconds;
                return steps;
            }

            var beforeBySig = new Dictionary<string, DomAssertionElementState>(StringComparer.Ordinal);
            if (before != null)
            {
                foreach (var b in before)
                {
                    if (b == null || string.IsNullOrWhiteSpace(b.Signature))
                        continue;
                    if (!beforeBySig.ContainsKey(b.Signature))
                        beforeBySig[b.Signature] = b;
                }
            }

            foreach (var a in after)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.CssLocator))
                    continue;
                if (!beforeBySig.TryGetValue(a.Signature, out var b))
                {
                    steps.Add(CreateCountStep(a, anchorMeta));
                    metrics.NewElementCount++;
                    continue;
                }

                var paramParts = new List<string>();
                if (a.ReadOnly != b.ReadOnly)
                {
                    paramParts.Add("ReadOnly=" + (a.ReadOnly ? "true" : "false"));
                    metrics.StateChangeCount++;
                }
                if (a.Disabled != b.Disabled)
                {
                    paramParts.Add("Disabled=" + (a.Disabled ? "true" : "false"));
                    metrics.StateChangeCount++;
                }
                if (!string.Equals(a.AriaDisabled ?? string.Empty, b.AriaDisabled ?? string.Empty, StringComparison.Ordinal))
                {
                    paramParts.Add("AriaDisabled=" + (string.IsNullOrEmpty(a.AriaDisabled) ? "(empty)" : a.AriaDisabled));
                    metrics.StateChangeCount++;
                }
                if (!string.Equals(a.AriaReadonly ?? string.Empty, b.AriaReadonly ?? string.Empty, StringComparison.Ordinal))
                {
                    paramParts.Add("AriaReadonly=" + (string.IsNullOrEmpty(a.AriaReadonly) ? "(empty)" : a.AriaReadonly));
                    metrics.StateChangeCount++;
                }
                if (a.ContentEditable != b.ContentEditable)
                {
                    paramParts.Add("ContentEditable=" + (a.ContentEditable ? "true" : "false"));
                    metrics.StateChangeCount++;
                }

                var colorChanged = !string.Equals(NormalizeCssColor(a.Color), NormalizeCssColor(b.Color), StringComparison.OrdinalIgnoreCase);
                var bgChanged = !string.Equals(NormalizeCssColor(a.BackgroundColor), NormalizeCssColor(b.BackgroundColor), StringComparison.OrdinalIgnoreCase);
                if (colorChanged)
                {
                    paramParts.Add("Color=" + (a.Color ?? string.Empty));
                    metrics.StateChangeCount++;
                }
                if (bgChanged)
                {
                    paramParts.Add("BackgroundColor=" + (a.BackgroundColor ?? string.Empty));
                    metrics.StateChangeCount++;
                }

                if (paramParts.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(a.FramePath))
                        paramParts.Insert(0, "FramePath=" + a.FramePath);
                    steps.Add(new SemanticStepRecord
                    {
                        TimestampUtc = DateTime.UtcNow,
                        Keyword = "AssertElementState",
                        Locator = a.CssLocator.Trim(),
                        ElementXpath = a.Xpath ?? string.Empty,
                        Parameter = string.Join(";", paramParts),
                        SourceEvent = "assertDiff",
                        RecordedPageUrl = anchorMeta?.RecordedPageUrl ?? string.Empty,
                        RecordedPageTitle = anchorMeta?.RecordedPageTitle ?? string.Empty,
                        LogicalKind = "assert"
                    });
                }

                if (emitScreenshotOnColorChange && (colorChanged || bgChanged)
                    && !string.IsNullOrWhiteSpace(screenshotBaselineDirectoryAbsolute))
                {
                    var safe = SanitizeFileStub(a.Signature);
                    var rel = Path.Combine("auto", safe + ".png");
                    var full = Path.Combine(screenshotBaselineDirectoryAbsolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), rel);
                    try
                    {
                        var dir = Path.GetDirectoryName(full);
                        if (!string.IsNullOrWhiteSpace(dir))
                            Directory.CreateDirectory(dir);
                    }
                    catch
                    {
                        /* non-fatal */
                    }
                    var shot = new SemanticStepRecord
                    {
                        TimestampUtc = DateTime.UtcNow,
                        Keyword = "AssertScreenshot",
                        Locator = a.CssLocator.Trim(),
                        Parameter = "BaselinePath=" + full + ";MaxDiffRatio=0.03",
                        SourceEvent = "assertDiffColor",
                        RecordedPageUrl = anchorMeta?.RecordedPageUrl ?? string.Empty,
                        RecordedPageTitle = anchorMeta?.RecordedPageTitle ?? string.Empty,
                        LogicalKind = "assertVisual"
                    };
                    if (!string.IsNullOrWhiteSpace(a.FramePath))
                        shot.Parameter = "FramePath=" + a.FramePath + ";" + shot.Parameter;
                    steps.Add(shot);
                    metrics.ScreenshotStepsAdded++;
                }
            }

            sw.Stop();
            metrics.DiffMs = sw.ElapsedMilliseconds;
            return steps;
        }

        private static SemanticStepRecord CreateCountStep(DomAssertionElementState a, SemanticStepRecord anchorMeta)
        {
            var p = string.IsNullOrWhiteSpace(a.FramePath) ? "Expected=1" : "FramePath=" + a.FramePath + ";Expected=1";
            return new SemanticStepRecord
            {
                TimestampUtc = DateTime.UtcNow,
                Keyword = "AssertLocatorCount",
                Locator = a.CssLocator.Trim(),
                ElementXpath = a.Xpath ?? string.Empty,
                Parameter = p,
                SourceEvent = "assertDiffNew",
                RecordedPageUrl = anchorMeta?.RecordedPageUrl ?? string.Empty,
                RecordedPageTitle = anchorMeta?.RecordedPageTitle ?? string.Empty,
                LogicalKind = "assert"
            };
        }

        private static string NormalizeCssColor(string c)
        {
            if (string.IsNullOrWhiteSpace(c))
                return string.Empty;
            return c.Trim().ToLowerInvariant();
        }

        private static string SanitizeFileStub(string raw)
        {
            var s = raw ?? "el";
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            if (s.Length > 80)
                s = s.Substring(0, 80);
            return string.IsNullOrWhiteSpace(s) ? "el" : s;
        }
    }
}
