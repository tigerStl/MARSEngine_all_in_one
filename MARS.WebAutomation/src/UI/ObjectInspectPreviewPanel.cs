using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Playwright;
using MARS.WebAutomation.Models;
using NLog;

namespace MARS.WebAutomation.UI
{
    internal enum PreviewWindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    /// <summary>
    /// Floating preview of the selected DOM element: caption (min / max / close), draggable, resizable, zoom with scrollbars.
    /// </summary>
    internal sealed class ObjectInspectPreviewPanel : UserControl
    {
        private static readonly Logger Log = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".UI.ObjectInspectPreviewPanel");
        private readonly Panel _dragBar;
        private readonly TableLayoutPanel _caption;
        private readonly Label _lblTitle;
        private readonly Button _btnMinimize;
        private readonly Button _btnMaximize;
        private readonly Button _btnClose;
        private readonly Panel _scrollHost;
        private readonly PictureBox _picture;
        private readonly Button _btnZoomIn;
        private readonly Button _btnZoomOut;
        private readonly Label _lblZoom;
        private readonly Panel _statusBar;
        private readonly Label _lblStatus;
        private readonly Panel _resizeGripPanel;
        private double _zoom = 1.0;
        private bool _dragging;
        private Point _dragStart;
        private Point _panelStart;
        private bool _resizing;
        private Point _resizeStart;
        private Size _sizeStart;
        private PreviewWindowState _state = PreviewWindowState.Normal;
        private Rectangle _restoredBounds;
        private bool _restoredBoundsValid;
        private readonly Size _floatedMinimumSize;
        private const int DragBarHeight = 24;
        private const int ToolbarRest = 26;
        private const int ResizeGrip = 12;
        private const int CaptionButtonWidth = 28;

        public ObjectInspectPreviewPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            MinimumSize = new Size(120, 80 + DragBarHeight + ToolbarRest);
            _floatedMinimumSize = MinimumSize;
            Size = new Size(280, 200);
            _restoredBounds = new Rectangle(0, 0, Width, Height);

            _dragBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = DragBarHeight,
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _caption = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(2, 0, 2, 0)
            };
            _caption.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _caption.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CaptionButtonWidth));
            _caption.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CaptionButtonWidth));
            _caption.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CaptionButtonWidth));
            _caption.RowStyles.Add(new RowStyle(SizeType.Absolute, DragBarHeight));

            _lblTitle = new Label
            {
                Text = "Preview",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Cursor = Cursors.SizeAll,
                Margin = Padding.Empty
            };

            _btnMinimize = CreateCaptionButton("0", "\u2212", "Minimize");
            _btnMaximize = CreateCaptionButton("1", "\u25A1", "Maximize");
            _btnClose = CreateCaptionButton("r", "\u00D7", "Close");
            _btnMinimize.Click += (_, __) => ToggleMinimize();
            _btnMaximize.Click += (_, __) => ToggleMaximize();
            _btnClose.Click += (_, __) => ClosePreview();

            _caption.Controls.Add(_lblTitle, 0, 0);
            _caption.Controls.Add(_btnMinimize, 1, 0);
            _caption.Controls.Add(_btnMaximize, 2, 0);
            _caption.Controls.Add(_btnClose, 3, 0);

            _dragBar.Controls.Add(_caption);

            var tool = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = ToolbarRest,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 2, 4, 2),
                AutoSize = false
            };
            _btnZoomOut = new Button { Text = "−", Width = 28, Height = 22, TabIndex = 0 };
            _lblZoom = new Label { Text = "100%", AutoSize = true, Padding = new Padding(4, 4, 4, 0) };
            _btnZoomIn = new Button { Text = "+", Width = 28, Height = 22, TabIndex = 1 };
            _btnZoomOut.Click += (_, __) => ChangeZoom(-0.25);
            _btnZoomIn.Click += (_, __) => ChangeZoom(0.25);
            tool.Controls.Add(_btnZoomOut);
            tool.Controls.Add(_lblZoom);
            tool.Controls.Add(_btnZoomIn);

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(0)
            };
            _picture = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(0, 0),
                BackColor = Color.White
            };
            _scrollHost.Controls.Add(_picture);

            _statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _resizeGripPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 16,
                Cursor = Cursors.SizeNWSE,
                BackColor = Color.Transparent
            };
            _statusBar.Controls.Add(_lblStatus);
            _statusBar.Controls.Add(_resizeGripPanel);

            Controls.Add(_scrollHost);
            Controls.Add(_statusBar);
            Controls.Add(tool);
            Controls.Add(_dragBar);

            void WireDrag(Control c)
            {
                c.MouseDown += DragBarOnMouseDown;
                c.MouseMove += DragBarOnMouseMove;
                c.MouseUp += DragBarOnMouseUp;
            }

            WireDrag(_caption);
            WireDrag(_lblTitle);

            MouseDown += PanelOnMouseDownResize;
            MouseMove += PanelOnMouseMoveResize;
            MouseUp += (_, __) => _resizing = false;
            _dragBar.MouseDown += PanelOnMouseDownResize;
            _dragBar.MouseMove += PanelOnMouseMoveResize;
            _dragBar.MouseUp += (_, __) => _resizing = false;
            _scrollHost.MouseDown += PanelOnMouseDownResize;
            _scrollHost.MouseMove += PanelOnMouseMoveResize;
            _scrollHost.MouseUp += (_, __) => _resizing = false;
            _picture.MouseDown += PanelOnMouseDownResize;
            _picture.MouseMove += PanelOnMouseMoveResize;
            _picture.MouseUp += (_, __) => _resizing = false;
            _resizeGripPanel.MouseDown += PanelOnMouseDownResize;
            _resizeGripPanel.MouseMove += PanelOnMouseMoveResize;
            _resizeGripPanel.MouseUp += (_, __) => _resizing = false;
            Resize += (_, __) => ClampPictureLayout();
        }

        /// <summary>When hidden via close, call from host after a new tree selection so the panel shows again in normal floating mode.</summary>
        public void ShowAfterTreeSelection()
        {
            if (!Visible)
            {
                Visible = true;
                ApplyNormalFloatingFromDefaults();
            }
        }

        public bool IsFloatingLayout => _state == PreviewWindowState.Normal && Visible;

        /// <summary>Anchor preview bottom-right inside <paramref name="parent"/> (normal state only).</summary>
        public void AlignBottomRight(Control parent)
        {
            if (parent == null || !Visible || _state != PreviewWindowState.Normal)
                return;
            Location = new Point(
                Math.Max(0, parent.ClientSize.Width - Width - 6),
                Math.Max(0, parent.ClientSize.Height - Height - 6));
            _restoredBounds = new Rectangle(Location, Size);
            _restoredBoundsValid = true;
        }

        public void SetTitle(string text)
        {
            _lblTitle.Text = string.IsNullOrWhiteSpace(text) ? "Preview" : text;
        }

        public void ClearImage()
        {
            var old = _picture.Image;
            _picture.Image = null;
            old?.Dispose();
            _zoom = 1.0;
            _lblZoom.Text = "100%";
            _lblStatus.Text = "No object selected";
        }

        public async Task TryCaptureFromPageAsync(IPage page, ObjectTreeNodeDto dto)
        {
            if (page == null || dto?.Bounds == null)
            {
                ClearImage();
                return;
            }

            var w = Math.Max(1d, dto.Bounds.Width);
            var h = Math.Max(1d, dto.Bounds.Height);
            var maxClip = 800d;
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
                        X = (float)dto.Bounds.X,
                        Y = (float)dto.Bounds.Y,
                        Width = (float)w,
                        Height = (float)h
                    }
                }).ConfigureAwait(true);

                using (var ms = new MemoryStream(bytes))
                using (var bmp = new Bitmap(ms))
                {
                    var shown = new Bitmap(bmp);
                    var old = _picture.Image;
                    _picture.Image = shown;
                    old?.Dispose();
                }

                _zoom = 1.0;
                ApplyZoomLayout();
                _lblStatus.Text = $"Object: {Math.Round(w)}x{Math.Round(h)}";
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "TryCaptureFromPageAsync failed.");
                ClearImage();
            }
        }

        private static Button CreateCaptionButton(string marlettChar, string fallbackChar, string accessibleName)
        {
            Font f;
            string text = marlettChar;
            try
            {
                f = new Font("Marlett", 11f, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (ArgumentException ex)
            {
                Log.Warn(ex, "Failed to initialize Marlett caption font; using fallback.");
                f = new Font(SystemFonts.CaptionFont.FontFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);
                text = fallbackChar;
            }

            var b = new Button
            {
                Text = text,
                Font = f,
                Dock = DockStyle.Fill,
                Margin = new Padding(1, 2, 1, 2),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Default,
                TabStop = false,
                AccessibleName = accessibleName
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(203, 213, 225);
            b.Tag = string.Equals(f.Name, "Marlett", StringComparison.Ordinal);
            return b;
        }

        private void ApplyNormalFloatingFromDefaults()
        {
            _state = PreviewWindowState.Normal;
            MinimumSize = _floatedMinimumSize;
            Dock = DockStyle.None;
            Anchor = AnchorStyles.None;
            if (!_restoredBoundsValid || _restoredBounds.Width < MinimumSize.Width || _restoredBounds.Height < MinimumSize.Height)
            {
                Size = new Size(280, 200);
                _restoredBounds = new Rectangle(0, 0, Width, Height);
                _restoredBoundsValid = true;
            }
            else
            {
                Size = _restoredBounds.Size;
            }

            TrySetMarlett(_btnMaximize, "1", "\u25A1");
            _btnMaximize.AccessibleName = "Maximize";
            TrySetMarlett(_btnMinimize, "0", "\u2212");
            _btnMinimize.AccessibleName = "Minimize";
            ShowContentChrome(true);
            Parent?.PerformLayout();
        }

        private static void TrySetMarlett(Button b, string marlett, string fallback)
        {
            if (b.Tag is bool useMarlett && useMarlett)
                b.Text = marlett;
            else
                b.Text = fallback;
        }

        private void ToggleMinimize()
        {
            if (_state == PreviewWindowState.Minimized)
            {
                RestoreFromMinimized();
                return;
            }

            if (_state == PreviewWindowState.Maximized)
                ExitMaximized();

            SaveRestoredBoundsIfFloating();
            _state = PreviewWindowState.Minimized;
            MinimumSize = new Size(60, DragBarHeight + 4);
            Dock = DockStyle.Bottom;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Height = MinimumSize.Height;
            ShowContentChrome(false);
            TrySetMarlett(_btnMinimize, "2", "\u25A4");
            _btnMinimize.AccessibleName = "Restore";
            Parent?.PerformLayout();
        }

        private void RestoreFromMinimized()
        {
            _state = PreviewWindowState.Normal;
            MinimumSize = _floatedMinimumSize;
            Dock = DockStyle.None;
            Anchor = AnchorStyles.None;
            if (_restoredBoundsValid)
            {
                Size = _restoredBounds.Size;
                Location = _restoredBounds.Location;
            }
            else
                Size = new Size(280, 200);

            ShowContentChrome(true);
            TrySetMarlett(_btnMinimize, "0", "\u2212");
            _btnMinimize.AccessibleName = "Minimize";
            if (Parent != null)
                AlignBottomRight(Parent);
            Parent?.PerformLayout();
        }

        private void ToggleMaximize()
        {
            if (_state == PreviewWindowState.Maximized)
            {
                ExitMaximized();
                return;
            }

            if (_state == PreviewWindowState.Minimized)
                RestoreFromMinimized();

            SaveRestoredBoundsIfFloating();
            _state = PreviewWindowState.Maximized;
            MinimumSize = new Size(40, 40);
            Dock = DockStyle.Fill;
            Anchor = AnchorStyles.None;
            ShowContentChrome(true);
            TrySetMarlett(_btnMaximize, "2", "\u2750");
            _btnMaximize.AccessibleName = "Restore";
            BringToFront();
            Parent?.PerformLayout();
        }

        private void ExitMaximized()
        {
            _state = PreviewWindowState.Normal;
            MinimumSize = _floatedMinimumSize;
            Dock = DockStyle.None;
            Anchor = AnchorStyles.None;
            if (_restoredBoundsValid)
            {
                Bounds = _restoredBounds;
            }
            else
            {
                Size = new Size(280, 200);
            }

            TrySetMarlett(_btnMaximize, "1", "\u25A1");
            _btnMaximize.AccessibleName = "Maximize";
            if (Parent != null)
                AlignBottomRight(Parent);
            Parent?.PerformLayout();
        }

        private void ClosePreview()
        {
            Visible = false;
        }

        private void SaveRestoredBoundsIfFloating()
        {
            if (_state != PreviewWindowState.Normal || Parent == null)
                return;
            _restoredBounds = new Rectangle(Location, Size);
            _restoredBoundsValid = true;
        }

        private void ShowContentChrome(bool show)
        {
            foreach (Control c in Controls)
            {
                if (!ReferenceEquals(c, _dragBar))
                    c.Visible = show;
            }
        }

        private void ChangeZoom(double delta)
        {
            _zoom = Math.Min(4.0, Math.Max(0.25, _zoom + delta));
            _lblZoom.Text = (int)Math.Round(_zoom * 100) + "%";
            ApplyZoomLayout();
        }

        private void ApplyZoomLayout()
        {
            if (_picture.Image == null)
                return;
            var iw = (int)Math.Max(1, _picture.Image.Width * _zoom);
            var ih = (int)Math.Max(1, _picture.Image.Height * _zoom);
            _picture.Size = new Size(iw, ih);
            _picture.Location = new Point(0, 0);
            ClampPictureLayout();
        }

        private void ClampPictureLayout()
        {
            if (_picture.Image == null)
                return;
            _scrollHost.AutoScrollMinSize = _picture.Size;
        }

        private void DragBarOnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _state == PreviewWindowState.Maximized)
                return;
            if (sender is Button)
                return;
            _dragging = true;
            _dragStart = PointToScreen(e.Location);
            _panelStart = Location;
        }

        private void DragBarOnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || Parent == null || _state != PreviewWindowState.Normal)
                return;
            var cur = PointToScreen(e.Location);
            var dx = cur.X - _dragStart.X;
            var dy = cur.Y - _dragStart.Y;
            Location = new Point(_panelStart.X + dx, _panelStart.Y + dy);
        }

        private void DragBarOnMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
            if (_state == PreviewWindowState.Normal && Parent != null)
                _restoredBounds = new Rectangle(Location, Size);
        }

        private bool InResizeGrip(Point p)
        {
            if (_state != PreviewWindowState.Normal)
                return false;
            if (_resizeGripPanel.RectangleToScreen(_resizeGripPanel.ClientRectangle).Contains(Cursor.Position))
                return true;
            return p.X >= Width - ResizeGrip && p.Y >= Height - ResizeGrip;
        }

        private void PanelOnMouseDownResize(object sender, MouseEventArgs e)
        {
            var p = PointToClient(Cursor.Position);
            if (e.Button != MouseButtons.Left || !InResizeGrip(p))
                return;
            _resizing = true;
            _resizeStart = Cursor.Position;
            _sizeStart = Size;
        }

        private void PanelOnMouseMoveResize(object sender, MouseEventArgs e)
        {
            var p = PointToClient(Cursor.Position);
            if (!_resizing)
            {
                Cursor = InResizeGrip(p) ? Cursors.SizeNWSE : Cursors.Default;
                return;
            }

            var cur = Cursor.Position;
            var dw = cur.X - _resizeStart.X;
            var dh = cur.Y - _resizeStart.Y;
            var nw = Math.Max(MinimumSize.Width, _sizeStart.Width + dw);
            var nh = Math.Max(MinimumSize.Height, _sizeStart.Height + dh);
            Size = new Size(nw, nh);
            if (Parent != null)
                _restoredBounds = new Rectangle(Location, Size);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_scrollHost.Visible && _scrollHost.ClientRectangle.Contains(_scrollHost.PointToClient(PointToClient(e.Location))))
            {
                var delta = e.Delta > 0 ? 0.1 : -0.1;
                _zoom = Math.Min(4.0, Math.Max(0.25, _zoom + delta));
                _lblZoom.Text = (int)Math.Round(_zoom * 100) + "%";
                ApplyZoomLayout();
            }
            base.OnMouseWheel(e);
        }
    }
}
