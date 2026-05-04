using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MARS.WebAutomation.Services;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceLiveChartForm : Form
    {
        private readonly PerformanceMetricsCollector _collector;
        private readonly int _intervalMs;
        private readonly List<PerformanceBucketSnapshot> _buckets = new List<PerformanceBucketSnapshot>();
        private readonly object _bucketLock = new object();
        private readonly System.Threading.Timer _bucketTimer;
        private readonly System.Windows.Forms.Timer _uiRefreshTimer;
        private long _lastAggCompleted;
        private long _lastAggSuccess;
        private long _lastAggErrors;

        private const int BarWidth = 10;
        private const int BarGap = 2;
        private const int LeftPad = 46;
        private const int RightPad = 36;
        private const int LegendH = 28;
        private const int BelowLegendPad = 8;
        private const int GapBarsToStatBoxes = 10;
        private const int GapStatBoxesToAggregate = 12;
        private const int BottomMargin = 10;
        private const int MinBarsAreaHeight = 120;
        private const int StatBoxPadX = 10;
        private const int StatBoxPadY = 8;
        private const int StatBoxRadius = 8;
        private const int ShadowOffset = 3;
        private const int ColumnGap = 10;

        public PerformanceLiveChartForm(PerformanceMetricsCollector collector, int chartSampleIntervalSeconds)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _intervalMs = Math.Max(500, chartSampleIntervalSeconds * 1000);
            InitializeComponent();

            var firstTickMs = Math.Min(500, _intervalMs);
            _bucketTimer = new System.Threading.Timer(_ => TimerTickBackground(), null, firstTickMs, _intervalMs);
            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 350 };
            _uiRefreshTimer.Tick += (_, __) =>
            {
                if (!IsDisposed)
                    _chartPanel.Invalidate();
            };
            Shown += (_, __) => _uiRefreshTimer.Start();

            Resize += (_, __) => ResizeChartCanvas();
            FormClosed += (_, __) =>
            {
                _bucketTimer.Dispose();
                _uiRefreshTimer.Stop();
                _uiRefreshTimer.Dispose();
            };
        }

        private void TimerTickBackground()
        {
            var b = _collector.ConsumeBucket();
            var agg = _collector.GetAggregateSnapshot();
            if (b.CreatedDelta == 0 && b.SuccessDelta == 0 && b.ErrorsDelta == 0)
            {
                var dCompleted = Math.Max(0, agg.TotalCompleted - _lastAggCompleted);
                var dSuccess = Math.Max(0, agg.TotalSuccess - _lastAggSuccess);
                var dErrors = Math.Max(0, agg.TotalErrors - _lastAggErrors);
                if (dCompleted > 0 || dSuccess > 0 || dErrors > 0)
                {
                    b.ReturnedDelta = (int)Math.Min(int.MaxValue, dCompleted);
                    b.CreatedDelta = (int)Math.Min(int.MaxValue, dCompleted);
                    b.SuccessDelta = (int)Math.Min(int.MaxValue, dSuccess);
                    b.ErrorsDelta = (int)Math.Min(int.MaxValue, dErrors);
                }
            }
            _lastAggCompleted = agg.TotalCompleted;
            _lastAggSuccess = agg.TotalSuccess;
            _lastAggErrors = agg.TotalErrors;
            lock (_bucketLock)
            {
                _buckets.Add(b);
                while (_buckets.Count > 240)
                    _buckets.RemoveAt(0);
            }
            if (IsDisposed || !IsHandleCreated)
                return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                    return;
                ResizeChartCanvas();
                try
                {
                    var hs = _chartScrollHost.HorizontalScroll;
                    if (hs.Maximum > hs.Minimum)
                        hs.Value = hs.Maximum;
                }
                catch
                {
                    /* scroll range may be invalid momentarily */
                }
                _chartPanel.Invalidate();
            }));
        }

        private void ResizeChartCanvas()
        {
            List<PerformanceBucketSnapshot> copy;
            PerformanceAggregateSnapshot agg;
            lock (_bucketLock)
            {
                copy = _buckets.ToList();
            }
            agg = _collector.GetAggregateSnapshot();

            var lay = ComputeLayout(copy, agg, _chartScrollHost.ClientSize);
            var minViewportW = Math.Max(720, _chartScrollHost.ClientSize.Width - 24);
            _chartPanel.Width = Math.Max(minViewportW, lay.TotalWidth);
            var minViewportH = Math.Max(400, _chartScrollHost.ClientSize.Height - 4);
            _chartPanel.Height = Math.Max(minViewportH, lay.TotalHeight);
        }

        private ChartLayout ComputeLayout(List<PerformanceBucketSnapshot> buckets, PerformanceAggregateSnapshot agg, Size viewport)
        {
            var font = SystemFonts.MessageBoxFont;
            var n = Math.Max(1, buckets.Count);

            var stripW = BarWidth * 3 + BarGap * 2;
            var measureW = Math.Max(160, Math.Min(360, viewport.Width > 100 ? viewport.Width / 3 : 240));
            var maxBoxInnerW = 40;
            var maxContentH = 24;
            using (var titleFont = new Font(font, FontStyle.Bold))
            {
                for (var i = 0; i < buckets.Count; i++)
                {
                    var body = FormatIntervalStatBody(buckets[i], i);
                    var bodySz = TextRenderer.MeasureText(body, font, new Size(measureW, int.MaxValue), TextFormatFlags.WordBreak);
                    var titleSz = TextRenderer.MeasureText(FormatIntervalTitle(i), titleFont, Size.Empty, TextFormatFlags.SingleLine);
                    maxBoxInnerW = Math.Max(maxBoxInnerW, bodySz.Width);
                    maxBoxInnerW = Math.Max(maxBoxInnerW, titleSz.Width);
                    maxContentH = Math.Max(maxContentH, titleSz.Height + 2 + bodySz.Height);
                }
                if (buckets.Count == 0)
                {
                    var body = FormatIntervalStatBody(default, -1);
                    var bodySz = TextRenderer.MeasureText(body, font, new Size(measureW, int.MaxValue), TextFormatFlags.WordBreak);
                    var titleSz = TextRenderer.MeasureText(FormatIntervalTitle(-1), titleFont, Size.Empty, TextFormatFlags.SingleLine);
                    maxBoxInnerW = Math.Max(maxBoxInnerW, bodySz.Width);
                    maxBoxInnerW = Math.Max(maxBoxInnerW, titleSz.Width);
                    maxContentH = Math.Max(maxContentH, titleSz.Height + 2 + bodySz.Height);
                }
            }

            var statBoxW = maxBoxInnerW + StatBoxPadX * 2;
            var statBoxH = maxContentH + StatBoxPadY * 2;
            var groupPitch = Math.Max(stripW + ColumnGap, statBoxW + ColumnGap);

            var aggText = FormatAggregateText(agg);
            var aggSize = TextRenderer.MeasureText(aggText, font, new Size((int)(viewport.Width * 0.92f), int.MaxValue), TextFormatFlags.WordBreak);
            var aggBoxW = aggSize.Width + 24;
            var aggBoxH = aggSize.Height + 20;

            var fixedTop = LegendH + BelowLegendPad;
            var fixedBottom = GapBarsToStatBoxes + statBoxH + GapStatBoxesToAggregate + aggBoxH + BottomMargin;
            var barsH = Math.Max(MinBarsAreaHeight, (viewport.Height > 80 ? viewport.Height - 4 : 420) - fixedTop - fixedBottom);
            if (barsH < MinBarsAreaHeight)
                barsH = MinBarsAreaHeight;
            var totalH = fixedTop + barsH + fixedBottom;
            var totalW = LeftPad + n * groupPitch + RightPad;

            return new ChartLayout
            {
                Font = font,
                GroupPitch = groupPitch,
                StatBoxSize = new Size(statBoxW, statBoxH),
                BarsAreaHeight = barsH,
                TotalWidth = totalW,
                TotalHeight = totalH,
                AggregateText = aggText,
                AggregateBoxSize = new Size(aggBoxW, aggBoxH)
            };
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            var rect = _chartPanel.ClientRectangle;
            if (rect.Width < 40 || rect.Height < 40)
                return;

            List<PerformanceBucketSnapshot> copy;
            lock (_bucketLock)
            {
                copy = _buckets.ToList();
            }
            var agg = _collector.GetAggregateSnapshot();
            var lay = ComputeLayout(copy, agg, _chartScrollHost.ClientSize);

            DrawLegend(g, rect.X + 16, rect.Y + 4);

            var chartRect = new Rectangle(LeftPad, rect.Y + LegendH + BelowLegendPad, rect.Width - LeftPad - RightPad, lay.BarsAreaHeight);
            g.DrawLine(Pens.DimGray, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
            g.DrawLine(Pens.DimGray, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);

            DrawBars(g, chartRect, copy, lay);
            DrawIntervalStatBoxes(g, chartRect, copy, lay);
            DrawAggregateBoxCentered(g, rect, chartRect.Bottom + GapBarsToStatBoxes, lay);
        }

        private static void DrawLegend(Graphics g, int x, int y)
        {
            void Dot(Color c, string text, ref int ox)
            {
                using (var b = new SolidBrush(c))
                    g.FillRectangle(b, ox, y + 4, 10, 10);
                g.DrawString(text, SystemFonts.MessageBoxFont, Brushes.Black, ox + 14, y);
                ox += TextRenderer.MeasureText(text, SystemFonts.MessageBoxFont).Width + 24;
            }

            var ox = x;
            Dot(Color.FromArgb(59, 130, 246), "Created", ref ox);
            Dot(Color.FromArgb(34, 197, 94), "Success", ref ox);
            Dot(Color.FromArgb(239, 68, 68), "Errors", ref ox);
        }

        private string FormatIntervalTitle(int index)
        {
            if (index < 0)
                return "—";
            var seconds = ((index + 1) * _intervalMs) / 1000.0;
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#} s", seconds);
        }

        private string FormatIntervalStatBody(PerformanceBucketSnapshot b, int index)
        {
            string minS, maxS, avgS;
            if (b.IntervalLatencySampleCount > 0)
            {
                minS = string.Format(CultureInfo.InvariantCulture, "{0:0.#}", b.IntervalMinLatencyMs);
                maxS = string.Format(CultureInfo.InvariantCulture, "{0:0.#}", b.IntervalMaxLatencyMs);
                avgS = string.Format(CultureInfo.InvariantCulture, "{0:0.#}", b.IntervalAverageLatencyMs);
            }
            else
            {
                minS = maxS = avgS = "—";
            }

            return string.Format(CultureInfo.InvariantCulture,
                "min {0} ms{4}max {1} ms{4}avg {2} ms{4}created {3}  ok {5}  err {6}",
                minS, maxS, avgS, b.CreatedDelta, Environment.NewLine, b.SuccessDelta, b.ErrorsDelta);
        }

        private static string FormatAggregateText(PerformanceAggregateSnapshot agg)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Overall  min {0:0.#} ms   max {1:0.#} ms   avg {2:0.#} ms   success {3:0.00}%{5}Completed {4}   ok {6}   errors {7}",
                agg.MinLatencyMs, agg.MaxLatencyMs, agg.AverageLatencyMs, agg.SuccessRatePercent, agg.TotalCompleted,
                Environment.NewLine, agg.TotalSuccess, agg.TotalErrors);
        }

        private void DrawBars(Graphics g, Rectangle chartRect, List<PerformanceBucketSnapshot> buckets, ChartLayout lay)
        {
            if (buckets.Count == 0)
            {
                g.DrawString("Waiting for samples...", SystemFonts.MessageBoxFont, Brushes.Gray, chartRect.Left + 6, chartRect.Top + 6);
                return;
            }

            // Some adapters may increment "returned" earlier than "created" in a bucket window.
            // Use returned as fallback so active traffic always produces visible bars.
            int EffectiveCreated(PerformanceBucketSnapshot b) => b.CreatedDelta > 0 ? b.CreatedDelta : b.ReturnedDelta;
            var maxBar = buckets.SelectMany(b => new[] { EffectiveCreated(b), b.SuccessDelta, b.ErrorsDelta }).DefaultIfEmpty(1).Max();
            maxBar = Math.Max(maxBar, 1);
            var scale = (chartRect.Height - 14) / (double)maxBar;
            var stripW = BarWidth * 3 + BarGap * 2;

            for (var i = 0; i < buckets.Count; i++)
            {
                var b = buckets[i];
                var created = EffectiveCreated(b);
                var colLeft = chartRect.Left + i * lay.GroupPitch + (lay.GroupPitch - stripW) / 2;
                DrawBar(g, colLeft, chartRect.Bottom - 1, BarWidth, (int)(created * scale), Color.FromArgb(59, 130, 246));
                DrawBar(g, colLeft + BarWidth + BarGap, chartRect.Bottom - 1, BarWidth, (int)(b.SuccessDelta * scale), Color.FromArgb(34, 197, 94));
                DrawBar(g, colLeft + (BarWidth + BarGap) * 2, chartRect.Bottom - 1, BarWidth, (int)(b.ErrorsDelta * scale), Color.FromArgb(239, 68, 68));
            }
        }

        private void DrawIntervalStatBoxes(Graphics g, Rectangle chartRect, List<PerformanceBucketSnapshot> buckets, ChartLayout lay)
        {
            if (buckets.Count == 0)
                return;

            var top = chartRect.Bottom + GapBarsToStatBoxes;
            var font = lay.Font;
            using (var titleFont = new Font(font, FontStyle.Bold))
            {
                for (var i = 0; i < buckets.Count; i++)
                {
                    var bx = chartRect.Left + i * lay.GroupPitch + (lay.GroupPitch - lay.StatBoxSize.Width) / 2;
                    var boxRect = new Rectangle(bx, top, lay.StatBoxSize.Width, lay.StatBoxSize.Height);
                    DrawShadowedRoundBox(g, boxRect, StatBoxRadius);

                    var title = FormatIntervalTitle(i);
                    var body = FormatIntervalStatBody(buckets[i], i);
                    var textX = boxRect.X + StatBoxPadX;
                    var textY = boxRect.Y + StatBoxPadY;
                    g.DrawString(title, titleFont, Brushes.Black, textX, textY);
                    var th = TextRenderer.MeasureText(title, titleFont, Size.Empty, TextFormatFlags.SingleLine).Height;
                    g.DrawString(body, font, Brushes.DimGray, textX, textY + th + 2);
                }
            }
        }

        private void DrawAggregateBoxCentered(Graphics g, Rectangle panelRect, int statBoxStripTop, ChartLayout lay)
        {
            var top = statBoxStripTop + lay.StatBoxSize.Height + GapStatBoxesToAggregate;
            var w = lay.AggregateBoxSize.Width;
            var h = lay.AggregateBoxSize.Height;
            var x = panelRect.X + (panelRect.Width - w) / 2;
            var box = new Rectangle(x, top, w, h);
            DrawShadowedRoundBox(g, box, StatBoxRadius + 2);
            TextRenderer.DrawText(g, lay.AggregateText, lay.Font, new Rectangle(box.X + 12, box.Y + 10, box.Width - 24, box.Height - 18), Color.Black, TextFormatFlags.WordBreak);
        }

        private static void DrawShadowedRoundBox(Graphics g, Rectangle bounds, int radius)
        {
            var shadowRect = new Rectangle(bounds.X + ShadowOffset, bounds.Y + ShadowOffset, bounds.Width, bounds.Height);
            using (var sh = RoundedRect(shadowRect, radius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(72, 60, 60, 60)))
                g.FillPath(shadowBrush, sh);

            using (var face = RoundedRect(bounds, radius))
            {
                using (var grad = new LinearGradientBrush(bounds,
                    Color.FromArgb(255, 253, 254, 255),
                    Color.FromArgb(255, 232, 236, 242),
                    LinearGradientMode.Vertical))
                    g.FillPath(grad, face);
                using (var hi = new Pen(Color.FromArgb(240, 255, 255, 255), 1f))
                    g.DrawPath(hi, face);
                using (var lo = new Pen(Color.FromArgb(200, 180, 185, 195), 1f))
                    g.DrawPath(lo, face);
            }
        }

        private static void DrawBar(Graphics g, int x, int bottom, int width, int height, Color color)
        {
            height = Math.Max(0, Math.Min(bottom - 4, height));
            if (height <= 0)
                return;
            using (var b = new SolidBrush(color))
                g.FillRectangle(b, x, bottom - height, width, height);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            if (bounds.Width < d || bounds.Height < d)
            {
                radius = Math.Max(1, Math.Min(bounds.Width, bounds.Height) / 2 - 1);
                d = radius * 2;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private struct ChartLayout
        {
            public Font Font;
            public int GroupPitch;
            public Size StatBoxSize;
            public int BarsAreaHeight;
            public int TotalWidth;
            public int TotalHeight;
            public string AggregateText;
            public Size AggregateBoxSize;
        }
    }
}
