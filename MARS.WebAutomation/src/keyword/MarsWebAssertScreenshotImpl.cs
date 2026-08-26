using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Keyword
{
    public sealed class MarsWebAssertScreenshotImpl : MarsWebKeywordImplBase
    {
        private static Dictionary<string, string> ParseParam(string raw)
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

        public override async Task<KeywordExecuteResult> KeywordExecute(IPage page, SemanticStepRecord step)
        {
            if (page == null || step == null || string.IsNullOrWhiteSpace(EffectivePlaywrightSelector(step)))
                return LocatorResolveFailed(step);
            var sel = EffectivePlaywrightSelector(step);
            var p = ParseParam(step.Parameter);
            var baseline = p.TryGetValue("BaselinePath", out var bp) ? bp.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(baseline))
                return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertScreenshot: BaselinePath= is required." };
            var maxDiff = 0.02;
            if (p.TryGetValue("MaxDiffRatio", out var md) && double.TryParse(md, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var r))
                maxDiff = Math.Max(0, Math.Min(1, r));

            var sw = Stopwatch.StartNew();
            try
            {
                ILocator loc;
                var fp = p.TryGetValue("FramePath", out var f) ? f.Trim() : string.Empty;
                if (string.IsNullOrEmpty(fp))
                {
                    loc = await FindObjectAsync(page, step).ConfigureAwait(false);
                    if (loc == null)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertScreenshot: locator matched no elements." };
                }
                else
                {
                    var frame = FramePathUtil.ResolveFrameByPath(page, fp);
                    var root = frame ?? page.MainFrame;
                    loc = root.Locator(sel).First;
                    if (await loc.CountAsync().ConfigureAwait(false) == 0)
                        return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertScreenshot: locator matched no elements." };
                }

                var curBytes = await loc.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                if (curBytes == null || curBytes.Length == 0)
                    return new KeywordExecuteResult { Success = false, ErrorMessage = "AssertScreenshot: empty screenshot." };

                if (!File.Exists(baseline))
                {
                    var dir = Path.GetDirectoryName(baseline);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllBytes(baseline, curBytes);
                    sw.Stop();
                    return Ok("BaselineCreated=true;CompareMs=0;BaselinePath=" + baseline);
                }

                var baseBytes = File.ReadAllBytes(baseline);
                var ratio = ComparePngPixelRatio(curBytes, baseBytes, out var cmpMs);
                sw.Stop();
                var totalMs = sw.ElapsedMilliseconds;
                if (ratio > maxDiff)
                {
                    return new KeywordExecuteResult
                    {
                        Success = false,
                        ErrorMessage = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "AssertScreenshot: diffRatio {0:0.####} exceeds max {1:0.####}.", ratio, maxDiff)
                    };
                }
                return Ok(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "CompareMs={0};TotalMs={1};DiffRatio={2:0.####}", cmpMs, totalMs, ratio));
            }
            catch (Exception ex)
            {
                return Fail(ex);
            }
        }

        private static double ComparePngPixelRatio(byte[] a, byte[] b, out long pixelMs)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var msA = new MemoryStream(a))
                using (var msB = new MemoryStream(b))
                using (var ba = new Bitmap(msA))
                using (var bb = new Bitmap(msB))
                {
                    if (ba.Width != bb.Width || ba.Height != bb.Height)
                    {
                        return 1;
                    }
                    var w = ba.Width;
                    var h = ba.Height;
                    long diff = 0;
                    long total = (long)w * h;
                    const int thr = 18;
                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            var ca = ba.GetPixel(x, y);
                            var cb = bb.GetPixel(x, y);
                            if (Math.Abs(ca.R - cb.R) > thr || Math.Abs(ca.G - cb.G) > thr || Math.Abs(ca.B - cb.B) > thr || Math.Abs(ca.A - cb.A) > thr)
                                diff++;
                        }
                    }
                    return total <= 0 ? 0 : diff / (double)total;
                }
            }
            finally
            {
                sw.Stop();
                pixelMs = sw.ElapsedMilliseconds;
            }
        }
    }
}
