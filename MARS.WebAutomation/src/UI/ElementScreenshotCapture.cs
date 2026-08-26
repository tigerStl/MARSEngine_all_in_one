using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using MARS.WebAutomation.Models;
using NLog;

namespace MARS.WebAutomation.UI
{
    /// <summary>Captures a viewport clip screenshot for a DOM element bounds rectangle.</summary>
    internal static class ElementScreenshotCapture
    {
        private static readonly Logger Log = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".UI.ElementScreenshotCapture");

        public static async Task<Bitmap> TryCaptureAsync(IPage page, BoundingRectDto bounds, int maxClip = 800)
        {
            if (page == null || bounds == null)
                return null;

            var w = Math.Max(1d, bounds.Width);
            var h = Math.Max(1d, bounds.Height);
            if (w > maxClip || h > maxClip)
            {
                var scale = Math.Min(maxClip / w, maxClip / h);
                w *= scale;
                h *= scale;
            }

            try
            {
                var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    Clip = new Clip
                    {
                        X = (float)bounds.X,
                        Y = (float)bounds.Y,
                        Width = (float)w,
                        Height = (float)h
                    }
                }).ConfigureAwait(false);

                using (var ms = new MemoryStream(bytes))
                using (var bmp = new Bitmap(ms))
                    return new Bitmap(bmp);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Element screenshot capture failed.");
                return null;
            }
        }
    }
}
