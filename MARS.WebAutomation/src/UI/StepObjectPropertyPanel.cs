using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;

namespace MARS.WebAutomation.UI
{
    /// <summary>Scrollable property inspector for the selected test step object (Record / Replay canvas).</summary>
    internal sealed class StepObjectPropertyPanel : UserControl
    {
        private static readonly Color HeaderBack = Color.FromArgb(241, 245, 249);
        private static readonly Color HeaderFore = Color.FromArgb(51, 65, 85);
        private static readonly Color LabelFore = Color.FromArgb(100, 116, 139);
        private static readonly Color ValueFore = Color.FromArgb(15, 23, 42);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Font ValueMonoFont = new Font("Consolas", 8.25f);
        private const int HeaderHeight = 28;
        private const int LabelWidth = 68;
        private const int CopyColWidth = 20;
        private const int ScreenshotHeight = 140;
        private static readonly Color CopyIconColor = Color.FromArgb(100, 116, 139);

        private readonly Panel _header;
        private readonly Button _btnCollapse;
        private readonly Label _lblTitle;
        private readonly Button _btnHighlight;
        private readonly Panel _scrollHost;
        private readonly Panel _contentPanel;
        private readonly Panel _screenshotHost;
        private readonly PictureBox _screenshot;
        private readonly Label _lblScreenshotStatus;
        private readonly Label _lblEmpty;

        private string _uiLanguage = "en";
        private bool _collapsed;
        private bool _layoutReady;
        private int _expandedWidth = 240;
        private SemanticStepRecord _boundStep;

        public event EventHandler HighlightRequested;
        public event EventHandler CollapsedChanged;

        public bool IsCollapsed => _collapsed;

        public int ExpandedWidth
        {
            get => _expandedWidth;
            set => _expandedWidth = Math.Max(180, value);
        }

        public StepObjectPropertyPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MinimumSize = new Size(28, 80);
            Width = 300;

            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = HeaderBack,
                Padding = new Padding(2, 0, 4, 0)
            };

            _btnCollapse = new Button
            {
                Text = "\u00AB",
                Width = 26,
                Height = 22,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = HeaderFore,
                BackColor = HeaderBack,
                Cursor = Cursors.Hand
            };
            _btnCollapse.FlatAppearance.BorderSize = 0;
            _btnCollapse.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
            _btnCollapse.Click += (_, __) => SetCollapsed(!_collapsed);

            _lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = HeaderFore,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                Padding = new Padding(2, 0, 0, 0),
                AutoEllipsis = true
            };

            _btnHighlight = new Button
            {
                Text = "...",
                Width = 52,
                Height = 22,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 8f),
                ForeColor = HeaderFore,
                BackColor = HeaderBack,
                Cursor = Cursors.Hand
            };
            _btnHighlight.FlatAppearance.BorderSize = 1;
            _btnHighlight.FlatAppearance.BorderColor = BorderColor;
            _btnHighlight.Click += (_, __) => HighlightRequested?.Invoke(this, EventArgs.Empty);

            var titleHost = new Panel { Dock = DockStyle.Fill };
            titleHost.Controls.Add(_lblTitle);
            titleHost.Controls.Add(_btnHighlight);
            _header.Controls.Add(titleHost);
            _header.Controls.Add(_btnCollapse);

            _contentPanel = new Panel
            {
                Location = Point.Empty,
                AutoSize = false,
                BackColor = Color.White
            };

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(6, 4, 6, 6)
            };
            _scrollHost.Controls.Add(_contentPanel);
            _scrollHost.Resize += (_, __) => LayoutContent();

            _screenshotHost = new Panel
            {
                Height = ScreenshotHeight + 22,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            _screenshot = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _lblScreenshotStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 18,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = LabelFore,
                Font = new Font("Segoe UI", 7.5f),
                Padding = new Padding(4, 0, 0, 0)
            };
            _screenshotHost.Controls.Add(_screenshot);
            _screenshotHost.Controls.Add(_lblScreenshotStatus);

            _lblEmpty = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            Controls.Add(_scrollHost);
            Controls.Add(_lblEmpty);
            Controls.Add(_header);

            _layoutReady = true;
            ApplyLocalizedChrome();
        }

        private static string T(string key, string language) => UiStrings.T(key, language);

        public void SetUiLanguage(string language)
        {
            _uiLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
            ApplyLocalizedChrome();
            if (_boundStep != null)
                Bind(_boundStep);
        }

        private void ApplyLocalizedChrome()
        {
            _lblTitle.Text = T("StepProp.Title", _uiLanguage);
            _btnHighlight.Text = T("GridHighlight", _uiLanguage);
            _btnCollapse.AccessibleName = _collapsed
                ? T("StepProp.ExpandPanel", _uiLanguage)
                : T("StepProp.CollapsePanel", _uiLanguage);
            if (_boundStep == null)
                _lblEmpty.Text = T("StepProp.SelectStep", _uiLanguage);
        }

        public void SetCollapsed(bool collapsed)
        {
            if (_collapsed == collapsed)
                return;
            _collapsed = collapsed;
            _btnCollapse.Text = collapsed ? "\u00BB" : "\u00AB";
            UpdateChromeVisibility();
            ApplyLocalizedChrome();
            CollapsedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateChromeVisibility()
        {
            if (_collapsed)
            {
                _scrollHost.Visible = false;
                _lblEmpty.Visible = false;
                _btnHighlight.Visible = false;
                _lblTitle.Text = T("StepProp.TitleShort", _uiLanguage);
                return;
            }

            _btnHighlight.Visible = true;
            var empty = _boundStep == null;
            _lblEmpty.Visible = empty;
            _scrollHost.Visible = !empty;
            if (empty)
            {
                _lblEmpty.Text = T("StepProp.SelectStep", _uiLanguage);
                _lblEmpty.BringToFront();
            }
            else
            {
                _scrollHost.BringToFront();
            }
        }

        public void Bind(SemanticStepRecord step)
        {
            _boundStep = step;
            ClearScreenshot();
            _contentPanel.Controls.Clear();
            UpdateChromeVisibility();
            if (step == null)
                return;

            var y = 0;
            y = PlaceControl(_screenshotHost, y);
            y = AddSectionAt(y, T("StepProp.SectionSummary", _uiLanguage), BuildSummaryRows(step));
            y = AddSectionAt(y, T("StepProp.SectionGeometry", _uiLanguage), BuildGeometryRows(step));
            y = AddSectionAt(y, T("StepProp.SectionClassification", _uiLanguage), BuildClassificationRows(step));
            y = AddSectionAt(y, T("StepProp.SectionLocators", _uiLanguage), BuildLocatorRows(step));
            y = AddSectionAt(y, T("StepProp.SectionXPath", _uiLanguage), BuildXPathRows(step));
            if (HasTargetSection(step))
                y = AddSectionAt(y, T("StepProp.SectionTarget", _uiLanguage), BuildTargetRows(step));
            if (HasRecordingMeta(step))
                y = AddSectionAt(y, T("StepProp.SectionRecording", _uiLanguage), BuildRecordingRows(step));

            _contentPanel.Height = y + 4;
            LayoutContent();
        }

        public void SetScreenshot(Image image, string statusText)
        {
            var old = _screenshot.Image;
            _screenshot.Image = image;
            old?.Dispose();
            _lblScreenshotStatus.Text = statusText ?? string.Empty;
        }

        public void ClearScreenshot() => SetScreenshot(null, string.Empty);

        public void SetScreenshotUnavailable(string reason)
        {
            ClearScreenshot();
            _lblScreenshotStatus.Text = reason ?? T("StepProp.ScreenshotUnavailable", _uiLanguage);
        }

        private int ContentWidth()
        {
            if (_scrollHost == null || !_layoutReady)
                return Math.Max(120, Width - 24);
            return Math.Max(120, _scrollHost.ClientSize.Width - 12);
        }

        private int PlaceControl(Control c, int y)
        {
            c.Width = ContentWidth();
            c.Location = new Point(0, y);
            _contentPanel.Controls.Add(c);
            return y + c.Height + 6;
        }

        private int AddSectionAt(int y, string title, (string label, string value, bool mono, bool copy)[] rows)
        {
            if (rows == null || rows.Length == 0)
                return y;

            var w = ContentWidth();
            var gb = new GroupBox
            {
                Text = title,
                Width = w,
                Location = new Point(0, y),
                ForeColor = LabelFore,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Padding = new Padding(6, 4, 6, 6)
            };

            var valueColW = Math.Max(80, w - LabelWidth - CopyColWidth - 24);
            var table = new TableLayoutPanel
            {
                ColumnCount = 3,
                AutoSize = true,
                Dock = DockStyle.Top,
                Width = w - 16,
                Margin = Padding.Empty
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelWidth));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, valueColW));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CopyColWidth));

            var rowIndex = 0;
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.label))
                    continue;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var lbl = new Label
                {
                    Text = row.label,
                    ForeColor = LabelFore,
                    Font = new Font("Segoe UI", 8f),
                    AutoSize = true,
                    MaximumSize = new Size(LabelWidth, 0),
                    Margin = new Padding(0, 2, 4, 2),
                    TextAlign = ContentAlignment.TopRight
                };
                var val = new Label
                {
                    Text = string.IsNullOrEmpty(row.value) ? "—" : row.value,
                    ForeColor = string.IsNullOrEmpty(row.value) ? Color.FromArgb(148, 163, 184) : ValueFore,
                    Font = row.mono ? ValueMonoFont : new Font("Segoe UI", 8f),
                    AutoSize = true,
                    MaximumSize = new Size(valueColW - 4, 200),
                    Margin = new Padding(0, 2, 0, 2)
                };

                table.Controls.Add(lbl, 0, rowIndex);
                table.Controls.Add(val, 1, rowIndex);
                if (row.copy && !string.IsNullOrEmpty(row.value))
                    table.Controls.Add(CreateCopyButton(row.value), 2, rowIndex);
                else
                    table.Controls.Add(new Panel { Width = CopyColWidth, Height = 1, Margin = Padding.Empty }, 2, rowIndex);
                rowIndex++;
            }

            if (rowIndex == 0)
                return y;

            gb.Controls.Add(table);
            gb.Height = table.Height + 28;
            _contentPanel.Controls.Add(gb);
            return y + gb.Height + 4;
        }

        private Button CreateCopyButton(string text)
        {
            var copyBtn = new Button
            {
                Width = CopyColWidth,
                Height = CopyColWidth,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1),
                AccessibleName = T("StepProp.Copy", _uiLanguage)
            };
            copyBtn.FlatAppearance.BorderSize = 0;
            copyBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            try
            {
                copyBtn.Image = FormsIconHelper.ToBitmap(IconChar.Copy, CopyIconColor, 12, 0d, FlipOrientation.Normal);
                copyBtn.ImageAlign = ContentAlignment.MiddleCenter;
            }
            catch
            {
                copyBtn.Text = "\u2398";
                copyBtn.Font = new Font("Segoe UI", 7f);
                copyBtn.ForeColor = CopyIconColor;
            }

            copyBtn.Click += (_, __) =>
            {
                try { Clipboard.SetText(text); }
                catch { /* ignore */ }
            };
            return copyBtn;
        }

        private void LayoutContent()
        {
            if (!_layoutReady || _scrollHost == null || _contentPanel == null)
                return;

            var w = ContentWidth();
            var y = 0;
            foreach (Control c in _contentPanel.Controls)
            {
                c.Width = w;
                c.Location = new Point(0, y);
                if (c is GroupBox gb && gb.Controls.Count > 0 && gb.Controls[0] is TableLayoutPanel tbl)
                {
                    var valueColW = Math.Max(80, w - LabelWidth - CopyColWidth - 24);
                    if (tbl.ColumnStyles.Count >= 2)
                        tbl.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, valueColW);
                    tbl.Width = Math.Max(80, w - 20);
                }
                y += c.Height + 4;
            }

            _contentPanel.Width = w;
            _contentPanel.Height = Math.Max(0, y);
            _scrollHost.AutoScrollMinSize = new Size(0, _contentPanel.Height + 8);
        }

        private static bool HasTargetSection(SemanticStepRecord step) =>
            !string.IsNullOrWhiteSpace(step.TargetTag)
            || !string.IsNullOrWhiteSpace(step.TargetRole)
            || !string.IsNullOrWhiteSpace(step.TargetLocator)
            || !string.IsNullOrWhiteSpace(step.TargetXpath);

        private static bool HasRecordingMeta(SemanticStepRecord step) =>
            step.TimestampUtc != default
            || (step.PerformanceRequestRefs != null && step.PerformanceRequestRefs.Count > 0)
            || !string.IsNullOrWhiteSpace(step.PlaywrightScript);

        private (string label, string value, bool mono, bool copy)[] BuildSummaryRows(SemanticStepRecord step) =>
            new[]
            {
                (T("StepsColOrder", _uiLanguage), step.RunOrder.ToString(CultureInfo.InvariantCulture), false, false),
                (T("StepsColKeyword", _uiLanguage), step.Keyword ?? string.Empty, false, false),
                (T("StepsColEvent", _uiLanguage), step.SourceEvent ?? string.Empty, false, false),
                (T("StepsColData", _uiLanguage), step.Data ?? string.Empty, false, false),
                (T("StepsColElapsed", _uiLanguage), step.ElapsedMsSincePrev.ToString("N0", CultureInfo.InvariantCulture) + " ms", false, false),
                (T("StepsColParameter", _uiLanguage), step.Parameter ?? string.Empty, false, false)
            };

        private (string label, string value, bool mono, bool copy)[] BuildGeometryRows(SemanticStepRecord step)
        {
            var rows = new System.Collections.Generic.List<(string, string, bool, bool)>
            {
                (T("StepsColBounds", _uiLanguage), step.BoundsDisplay ?? string.Empty, false, false)
            };
            if (step.BoundingRect != null)
            {
                rows.Add(("X", step.BoundingRect.X.ToString("0.##", CultureInfo.InvariantCulture), false, false));
                rows.Add(("Y", step.BoundingRect.Y.ToString("0.##", CultureInfo.InvariantCulture), false, false));
                rows.Add((T("Prop.W", _uiLanguage), step.BoundingRect.Width.ToString("0.##", CultureInfo.InvariantCulture), false, false));
                rows.Add((T("Prop.H", _uiLanguage), step.BoundingRect.Height.ToString("0.##", CultureInfo.InvariantCulture), false, false));
            }

            if (step.CanvasX.HasValue || step.CanvasY.HasValue)
            {
                var cx = step.CanvasX.HasValue ? step.CanvasX.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";
                var cy = step.CanvasY.HasValue ? step.CanvasY.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";
                rows.Add((T("StepProp.Canvas", _uiLanguage), cx + ", " + cy, false, false));
            }

            return rows.ToArray();
        }

        private (string label, string value, bool mono, bool copy)[] BuildClassificationRows(SemanticStepRecord step)
        {
            var rows = new System.Collections.Generic.List<(string, string, bool, bool)>
            {
                (T("StepsColLogicalKind", _uiLanguage), step.LogicalKind ?? string.Empty, false, false)
            };
            if (!string.IsNullOrWhiteSpace(step.TargetTag))
                rows.Add((T("Prop.Tag", _uiLanguage), step.TargetTag, false, false));
            if (!string.IsNullOrWhiteSpace(step.TargetRole))
                rows.Add((T("Prop.Role", _uiLanguage), step.TargetRole, false, false));
            if (!string.IsNullOrWhiteSpace(step.RecordedPageUrl))
                rows.Add((T("StepProp.RecordedUrl", _uiLanguage), step.RecordedPageUrl, false, true));
            if (!string.IsNullOrWhiteSpace(step.RecordedPageTitle))
                rows.Add((T("StepProp.RecordedTitle", _uiLanguage), step.RecordedPageTitle, false, false));
            return rows.ToArray();
        }

        private (string label, string value, bool mono, bool copy)[] BuildLocatorRows(SemanticStepRecord step)
        {
            var rows = new System.Collections.Generic.List<(string, string, bool, bool)>();
            if (!string.IsNullOrWhiteSpace(step.Locator))
                rows.Add((T("StepsColLocator", _uiLanguage), step.Locator, true, true));
            var effective = SemanticStepLocatorUtil.EffectivePlaywrightSelector(step);
            if (!string.IsNullOrWhiteSpace(effective))
                rows.Add((T("StepProp.EffectiveLocator", _uiLanguage), effective, true, true));
            if (!string.IsNullOrWhiteSpace(step.LocatorAlternates))
                rows.Add((T("StepsColLocatorAlt", _uiLanguage), step.LocatorAlternates, true, true));
            if (!string.IsNullOrWhiteSpace(step.PlaywrightScript))
                rows.Add((T("StepProp.PlaywrightScript", _uiLanguage), step.PlaywrightScript, true, true));
            return rows.ToArray();
        }

        private (string label, string value, bool mono, bool copy)[] BuildXPathRows(SemanticStepRecord step)
        {
            var rows = new System.Collections.Generic.List<(string, string, bool, bool)>();
            if (!string.IsNullOrWhiteSpace(step.ElementXpath))
                rows.Add((T("StepsColXPath", _uiLanguage), step.ElementXpath, true, true));
            if (!string.IsNullOrWhiteSpace(step.TargetXpath)
                && !string.Equals(step.TargetXpath, step.ElementXpath, StringComparison.Ordinal))
                rows.Add((T("StepProp.TargetXPath", _uiLanguage), step.TargetXpath, true, true));
            return rows.ToArray();
        }

        private (string label, string value, bool mono, bool copy)[] BuildTargetRows(SemanticStepRecord step) =>
            new[]
            {
                (T("Prop.Tag", _uiLanguage), step.TargetTag ?? string.Empty, false, false),
                (T("Prop.Role", _uiLanguage), step.TargetRole ?? string.Empty, false, false),
                (T("StepProp.TargetLocator", _uiLanguage), step.TargetLocator ?? string.Empty, true, true),
                (T("StepProp.TargetXPath", _uiLanguage), step.TargetXpath ?? string.Empty, true, true)
            };

        private (string label, string value, bool mono, bool copy)[] BuildRecordingRows(SemanticStepRecord step)
        {
            var rows = new System.Collections.Generic.List<(string, string, bool, bool)>();
            if (step.TimestampUtc != default)
            {
                rows.Add((T("StepProp.Timestamp", _uiLanguage),
                    step.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    false, false));
            }

            if (step.PerformanceRequestRefs != null && step.PerformanceRequestRefs.Count > 0)
                rows.Add(("Perf#", string.Join(", ", step.PerformanceRequestRefs), false, false));
            return rows.ToArray();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
        }
    }
}
