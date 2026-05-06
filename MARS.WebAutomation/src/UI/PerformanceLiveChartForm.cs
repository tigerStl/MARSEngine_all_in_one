using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MARS.WebAutomation.Services;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceLiveChartForm : Form
    {
        private const uint PM_REMOVE = 0x0001;
        private const uint WM_NULL = 0x0000;
        private const uint WM_QUIT = 0x0012;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMsg
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public NativePoint pt;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMsg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref NativeMsg lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref NativeMsg lpMsg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly PerformanceMetricsCollector _collector;
        private readonly int _intervalMs;
        private readonly List<PerformanceBucketSnapshot> _buckets = new List<PerformanceBucketSnapshot>();
        private readonly object _bucketLock = new object();
        private readonly System.Windows.Forms.Timer _bucketUiTimer;
        private bool _bucketQuickFirstDone;
        private bool _bucketUiTickReentrancyGuard;
        private bool _followLatest = true;
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
            _aggregatePanel.Paint += AggregatePanel_Paint;
            _chartScrollHost.Scroll += ChartScrollHost_Scroll;

            EnableControlDoubleBuffering(_chartPanel);
            EnableControlDoubleBuffering(_aggregatePanel);

            _bucketUiTimer = new System.Windows.Forms.Timer { Interval = Math.Min(500, _intervalMs) };
            _bucketUiTimer.Tick += OnBucketUiTick;

            Shown += (_, __) => _bucketUiTimer.Start();

            Resize += (_, __) =>
            {
                ResizeChartCanvas();
                if (_followLatest)
                    ScrollChartHostToShowLatestContent();
                _aggregatePanel.Invalidate(false);
            };
            FormClosing += (_, __) => { _bucketUiTimer.Stop(); };
            FormClosed += (_, __) =>
            {
                _aggregatePanel.Paint -= AggregatePanel_Paint;
                _chartScrollHost.Scroll -= ChartScrollHost_Scroll;
                _bucketUiTimer.Tick -= OnBucketUiTick;
                _bucketUiTimer.Dispose();
            };
        }

        private static void EnableControlDoubleBuffering(Control c)
        {
            if (c == null)
                return;
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(c, true, null);
            }
            catch
            {
                /* optional */
            }
        }

        private static bool IsQuietBucket(PerformanceBucketSnapshot b) =>
            b.CreatedDelta == 0 && b.ReturnedDelta == 0 && b.SuccessDelta == 0 && b.ErrorsDelta == 0
            && b.IntervalLatencySampleCount == 0;

        private void OnBucketUiTick(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            if (_bucketUiTickReentrancyGuard)
                return;
            _bucketUiTickReentrancyGuard = true;
            try
            {

            if (!_bucketQuickFirstDone)
            {
                _bucketQuickFirstDone = true;
                _bucketUiTimer.Interval = _intervalMs;
            }

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
                if (IsQuietBucket(b) && _buckets.Count > 0 && IsQuietBucket(_buckets[_buckets.Count - 1]))
                {
                    /* Skip consecutive empty buckets after traffic stops (avoids runaway width + post-run flicker). */
                }
                else
                {
                    _buckets.Add(b);
                    while (_buckets.Count > 240)
                        _buckets.RemoveAt(0);
                }
            }

            ResizeChartCanvas();
            if (_followLatest)
                ScrollChartHostToShowLatestContent();
            _chartPanel.Invalidate(false);
            _aggregatePanel.Invalidate(false);
            PumpPendingWindowsMessages(24);
            try
            {
                if (IsHandleCreated)
                    PostMessage(Handle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                /* ignore */
            }
            }
            finally
            {
                _bucketUiTickReentrancyGuard = false;
            }
        }

        private static void PumpPendingWindowsMessages(int maxMessages)
        {
            var n = 0;
            while (n < maxMessages && PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
            {
                n++;
                if (msg.message == WM_QUIT)
                    break;
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        private void ChartScrollHost_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                UpdateFollowLatestFromScrollPosition();
        }

        private void UpdateFollowLatestFromScrollPosition()
        {
            try
            {
                var visW = _chartScrollHost.ClientRectangle.Width;
                if (_chartScrollHost.VerticalScroll.Visible)
                    visW -= SystemInformation.VerticalScrollBarWidth;
                visW = Math.Max(1, visW);
                var maxScroll = Math.Max(0, _chartPanel.Width - visW);
                if (maxScroll <= 0)
                {
                    _followLatest = true;
                    return;
                }
                var pos = Math.Max(0, -_chartScrollHost.AutoScrollPosition.X);
                _followLatest = pos >= maxScroll - 36;
            }
            catch
            {
                _followLatest = true;
            }
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

            var aggMeasureW = Math.Max(120, _aggregatePanel.ClientSize.Width > 0 ? _aggregatePanel.ClientSize.Width : ClientSize.Width);
            ChartLayout lay = default;
            for (var pass = 0; pass < 2; pass++)
            {
                lay = ComputeLayout(copy, agg, _chartScrollHost.ClientSize, aggMeasureW);
                ApplyAggregatePanelHeight(lay);
            }

            var minViewportW = Math.Max(720, _chartScrollHost.ClientSize.Width - 24);
            _chartPanel.Width = Math.Max(minViewportW, lay.TotalWidth);
            var minViewportH = Math.Max(400, _chartScrollHost.ClientSize.Height - 4);
            _chartPanel.Height = Math.Max(minViewportH, lay.TotalHeight);
        }

        private void ApplyAggregatePanelHeight(ChartLayout lay)
        {
            var padY = 20;
            var h = lay.AggregateBoxSize.Height + ShadowOffset + padY * 2;
            h = Math.Max(88, Math.Min(360, h));
            if (_aggregatePanel.Height != h)
                _aggregatePanel.Height = h;
        }

        /// <summary>Chart strip layout (scrollable). Overall aggregate is painted on <see cref="_aggregatePanel"/>.</summary>
        private ChartLayout ComputeLayout(List<PerformanceBucketSnapshot> buckets, PerformanceAggregateSnapshot agg, Size viewport, int aggregateTextMeasureWidth)
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
            var aggSize = TextRenderer.MeasureText(aggText, font, Size.Empty, TextFormatFlags.SingleLine);
            var aggBoxW = Math.Max(160, aggSize.Width + 24);
            var aggBoxH = aggSize.Height + 20;

            var fixedTop = LegendH + BelowLegendPad;
            var fixedBottom = GapBarsToStatBoxes + statBoxH + BottomMargin;
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

        /// <summary>After content width changes, scroll horizontally so the newest interval columns stay in view.</summary>
        private void ScrollChartHostToShowLatestContent()
        {
            try
            {
                _chartScrollHost.PerformLayout();
                var visW = _chartScrollHost.ClientRectangle.Width;
                if (_chartScrollHost.VerticalScroll.Visible)
                    visW -= SystemInformation.VerticalScrollBarWidth;
                visW = Math.Max(1, visW);
                var overflow = _chartPanel.Width - visW;
                if (overflow <= 0)
                    return;
                var scrollX = overflow;
                var scrollY = Math.Max(0, -_chartScrollHost.AutoScrollPosition.Y);
                _chartScrollHost.AutoScrollPosition = new Point(scrollX, scrollY);
                _followLatest = true;
            }
            catch
            {
                /* scroll range may be invalid momentarily */
            }
        }

        private void AggregatePanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            var panelRect = _aggregatePanel.ClientRectangle;
            if (panelRect.Width < 40 || panelRect.Height < 24)
                return;

            List<PerformanceBucketSnapshot> copy;
            lock (_bucketLock)
            {
                copy = _buckets.ToList();
            }

            var agg = _collector.GetAggregateSnapshot();
            var lay = ComputeLayout(
                copy,
                agg,
                _chartScrollHost.ClientSize,
                Math.Max(120, panelRect.Width));

            var font = lay.Font;
            var w = lay.AggregateBoxSize.Width;
            var h = lay.AggregateBoxSize.Height;
            var x = panelRect.X + (panelRect.Width - w) / 2;
            var y = panelRect.Y + (panelRect.Height - h) / 2;
            var box = new Rectangle(x, y, w, h);
            DrawShadowedRoundBox(g, box, StatBoxRadius + 2);
            TextRenderer.DrawText(
                g,
                lay.AggregateText,
                font,
                new Rectangle(box.X + 12, box.Y + 10, box.Width - 24, box.Height - 18),
                Color.Black,
                TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
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
            var aggW = Math.Max(120, _aggregatePanel.ClientSize.Width > 0 ? _aggregatePanel.ClientSize.Width : ClientSize.Width);
            var lay = ComputeLayout(copy, agg, _chartScrollHost.ClientSize, aggW);

            DrawLegend(g, rect.X + 16, rect.Y + 4);

            var chartRect = new Rectangle(LeftPad, rect.Y + LegendH + BelowLegendPad, rect.Width - LeftPad - RightPad, lay.BarsAreaHeight);
            g.DrawLine(Pens.DimGray, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
            g.DrawLine(Pens.DimGray, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);

            DrawBars(g, chartRect, copy, lay);
            DrawIntervalStatBoxes(g, chartRect, copy, lay);
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

            string Line(string key, string value) => string.Format(CultureInfo.InvariantCulture, "{0,-8}: {1}", key, value);
            return string.Join(Environment.NewLine, new[]
            {
                Line("min", minS + " ms"),
                Line("max", maxS + " ms"),
                Line("avg", avgS + " ms"),
                Line("created", b.CreatedDelta.ToString(CultureInfo.InvariantCulture)),
                Line("ok", b.SuccessDelta.ToString(CultureInfo.InvariantCulture)),
                Line("err", b.ErrorsDelta.ToString(CultureInfo.InvariantCulture)),
                Line("returned", b.ReturnedDelta.ToString(CultureInfo.InvariantCulture))
            });
        }

        private static string FormatAggregateText(PerformanceAggregateSnapshot agg)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Overall | min: {0:0.#} ms | max: {1:0.#} ms | avg: {2:0.#} ms | success: {3:0.00}% | completed: {4} | ok: {5} | errors: {6}",
                agg.MinLatencyMs,
                agg.MaxLatencyMs,
                agg.AverageLatencyMs,
                agg.SuccessRatePercent,
                agg.TotalCompleted,
                agg.TotalSuccess,
                agg.TotalErrors);
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
