using System.Drawing;
using System.Windows.Forms;
using Mars.VirtualDesktop.Desktop;
using Mars.VirtualDesktop.Native;

namespace Mars.VirtualDesktop.UI;

internal sealed class MainForm : Form
{
    private readonly VirtualDesktopManager _manager;
    private readonly ListView _desktopList;
    private readonly PictureBox _mirror;
    private readonly TextBox _logBox;
    private readonly TextBox _cmdBox;
    private readonly TextBox _typeBox;
    private readonly Label _status;
    private readonly Label _desktopBanner;
    private readonly Label _grabBanner;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly System.Windows.Forms.Timer _inventoryTimer;
    private Bitmap? _currentFrame;
    private bool _mirroring;
    private bool _suppressListSwitch;
    private bool _inputRedirectActive;
    private string? _viewDesktopName;

    public MainForm(VirtualDesktopManager manager)
    {
        _manager = manager;
        _manager.Log += AppendLog;
        _manager.Changed += () =>
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(RefreshDesktopUi);
            else RefreshDesktopUi();
        };

        KeyPreview = true;
        Text = "Mars.VirtualDesktop — subsystem (Core)";
        Width = 1180;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 560);

        _desktopBanner = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(32, 48, 64),
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 8, 0),
            Text = "Win32 desktops: …"
        };

        _grabBanner = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            BackColor = Color.FromArgb(80, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 8, 0),
            Visible = false,
            Text = "INPUT → VIRTUAL DESKTOP  |  Press Ctrl+End to return mouse/keyboard to main desktop"
        };

        var top = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8) };
        var btnCreate = MakeButton("1. Create Desktop", 8, 12, 140);
        var btnLaunch = MakeButton("2. Launch App", 156, 12, 120);
        var btnMirror = MakeButton("3. Start Mirror", 284, 12, 120);
        var btnStopMirror = MakeButton("Stop Mirror", 412, 12, 100);
        var btnSwitchBack = MakeButton("Switch Back", 520, 12, 110);
        var btnCapture = MakeButton("Capture Once", 638, 12, 110);
        var btnCloseVd = MakeButton("Close Selected VD", 756, 12, 130);
        var btnRefreshDesktops = MakeButton("Refresh", 894, 12, 90);

        _cmdBox = new TextBox
        {
            Left = 8,
            Top = 52,
            Width = 520,
            Text = @"C:\Windows\System32\notepad.exe"
        };
        _typeBox = new TextBox { Left = 540, Top = 52, Width = 320, PlaceholderText = "Text to type into VD..." };
        var btnType = MakeButton("Type", 870, 48, 80);

        top.Controls.AddRange(new Control[]
        {
            btnCreate, btnLaunch, btnMirror, btnStopMirror, btnSwitchBack, btnCapture, btnCloseVd, btnRefreshDesktops,
            _cmdBox, _typeBox, btnType
        });

        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = "Select a VD on the left for BitBlt view. Ctrl while mouse in right pane = input to VD; Ctrl+End = release.",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        _logBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 140,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9f)
        };

        _desktopList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            Font = new Font("Segoe UI", 9f)
        };
        _desktopList.Columns.Add("Desktop", 150);
        _desktopList.Columns.Add("Owned", 55);
        _desktopList.Columns.Add("Input", 50);
        _desktopList.Columns.Add("Selected", 60);
        _desktopList.SelectedIndexChanged += DesktopList_SelectedIndexChanged;

        var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        var leftTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "Desktops (click = BitBlt view)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        leftPanel.Controls.Add(_desktopList);
        leftPanel.Controls.Add(leftTitle);

        _mirror = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Cross,
            TabStop = true
        };
        _mirror.MouseClick += Mirror_MouseClick;
        _mirror.MouseMove += Mirror_MouseMove;
        _mirror.MouseDown += Mirror_MouseDown;
        _mirror.MouseEnter += (_, _) => TryBeginInputRedirectFromCtrl();
        _mirror.PreviewKeyDown += (_, e) => e.IsInputKey = true;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        };
        split.Panel1.Controls.Add(leftPanel);
        split.Panel2.Controls.Add(_mirror);

        Controls.Add(split);
        Controls.Add(_logBox);
        Controls.Add(_status);
        Controls.Add(top);
        Controls.Add(_grabBanner);
        Controls.Add(_desktopBanner);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _refreshTimer.Tick += (_, _) => RefreshMirrorFrame();

        _inventoryTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _inventoryTimer.Tick += (_, _) => RefreshDesktopUi();

        Load += (_, _) =>
        {
            try
            {
                split.Panel1MinSize = 120;
                split.Panel2MinSize = 200;
                int maxDist = Math.Max(120, split.Width - split.Panel2MinSize - split.SplitterWidth);
                int dist = Math.Min(280, Math.Max(120, ClientSize.Width / 4));
                split.SplitterDistance = Math.Min(dist, maxDist);
            }
            catch { /* ignore */ }
        };

        Shown += (_, _) =>
        {
            RefreshDesktopUi();
            _inventoryTimer.Start();
            Activate();
            BringToFront();
        };

        btnRefreshDesktops.Click += (_, _) => RefreshDesktopUi(forceLog: true);

        btnCreate.Click += (_, _) => SafeUi(() =>
        {
            var s = _manager.CreateNew();
            SetStatus($"Desktop created: {s.DesktopName}");
            RefreshDesktopUi();
            SelectListItem(s.DesktopName);
            StartBitBltView(s.DesktopName);
        });

        btnLaunch.Click += (_, _) => SafeUi(() =>
        {
            // Launch requires an owned desktop (CreateProcess lpDesktop) — create one if only external selected.
            var s = _manager.Selected;
            if (s == null || !s.IsCreated)
                throw new InvalidOperationException(
                    "Launch needs an owned desktop. Create a new VD first, or select one listed as Owned=Yes.");
            int pid = s.LaunchOnDesktop(_cmdBox.Text.Trim());
            SetStatus($"Launched pid={pid} on {s.DesktopName}");
        });

        btnMirror.Click += (_, _) =>
        {
            string? name = _manager.SelectedName;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "Select a desktop in the left list first.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (DesktopAccessor.IsProtectedSystemDesktop(name) &&
                name.Equals("Winlogon", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Refusing to mirror Winlogon.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            StartBitBltView(name);
        };

        btnStopMirror.Click += (_, _) => StopBitBltView();

        btnSwitchBack.Click += (_, _) => SafeUi(() =>
        {
            EndInputRedirect();
            _manager.SwitchToDefault();
            SetStatus("Switched back to default desktop.");
            RefreshDesktopUi();
        });

        btnCloseVd.Click += (_, _) => SafeUi(CloseSelectedVd);

        btnCapture.Click += (_, _) => SafeUi(() =>
        {
            using var bmp = _manager.CaptureSelectedBitBlt();
            ShowFrame((Bitmap)bmp.Clone());
            SetStatus($"BitBlt capture [{_manager.SelectedName}]: {bmp.Width}x{bmp.Height}");
        });

        btnType.Click += (_, _) => SafeUi(() =>
        {
            string text = _typeBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(this, "Enter text to type.", Text);
                return;
            }
            string detail = _manager.TypeSelected(text);
            SetStatus(detail);
        });

        FormClosed += (_, _) =>
        {
            EndInputRedirect();
            _refreshTimer.Stop();
            _inventoryTimer.Stop();
            _currentFrame?.Dispose();
        };

        AppendLog("Mars.VirtualDesktop ready.");
        AppendLog("Left click = select + BitBlt view (stays on main desktop).");
        AppendLog("Mouse in right pane + Ctrl = redirect input to VD; Ctrl+End = release.");
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.End))
        {
            if (_inputRedirectActive)
            {
                EndInputRedirect();
                SetStatus("Ctrl+End: input returned to main desktop.");
            }
            return true;
        }

        if (_inputRedirectActive)
        {
            // Forward keys (except pure Ctrl/Alt/Shift modifiers) to the virtual desktop.
            Keys keyCode = keyData & Keys.KeyCode;
            if (keyCode is not (Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.None))
            {
                ForwardKeyToVirtualDesktop(keyData);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Control && !_inputRedirectActive && IsMouseOverMirror())
            BeginInputRedirect();
    }

    private void DesktopList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressListSwitch) return;
        if (_desktopList.SelectedItems.Count == 0) return;
        string name = _desktopList.SelectedItems[0].Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            AppendLog($"Ignore invalid desktop list selection: [{name}]");
            return;
        }

        SafeUi(() =>
        {
            EndInputRedirect();
            // Do NOT SwitchDesktop here — that would hide this UI and block Close.
            _manager.Select(name);
            if (DesktopAccessor.IsProtectedSystemDesktop(name)
                && name.Equals("Winlogon", StringComparison.OrdinalIgnoreCase))
            {
                StopBitBltView();
                SetStatus($"Selected [{name}] — Winlogon not mirrored.");
            }
            else
            {
                StartBitBltView(name);
                string tag = _manager.IsOwned(name) ? "owned" : "external/orphan — full takeover";
                SetStatus($"Selected [{name}] ({tag}) — BitBlt view + Ctrl input available.");
            }
            RefreshDesktopUi();
        });
    }

    private void StartBitBltView(string desktopName)
    {
        _viewDesktopName = desktopName;
        _mirroring = true;
        if (!_refreshTimer.Enabled)
            _refreshTimer.Start();
        RefreshMirrorFrame();
        AppendLog($"BitBlt mirror started for [{desktopName}]");
    }

    private void StopBitBltView()
    {
        _mirroring = false;
        _refreshTimer.Stop();
        _viewDesktopName = null;
        SetStatus("Mirror stopped.");
    }

    private void CloseSelectedVd()
    {
        EndInputRedirect();
        StopBitBltView();

        if (_desktopList.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select a desktop in the list first.", Text);
            return;
        }
        string name = _desktopList.SelectedItems[0].Text;
        if (DesktopAccessor.IsProtectedSystemDesktop(name))
        {
            MessageBox.Show(this, $"Cannot close system desktop [{name}].", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(this,
            $"Close virtual desktop [{name}]?\nProcesses with windows on it will be killed.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try { _manager.SwitchToDefault(); } catch { /* ignore */ }

        _manager.CloseDesktop(name, killProcesses: true);
        _viewDesktopName = null;
        SetStatus($"Closed [{name}]");
        RefreshDesktopUi();
    }

    private void RefreshDesktopUi(bool forceLog = false)
    {
        try
        {
            var snap = DesktopInventory.Capture();
            string selectedName = _manager.Selected?.DesktopName ?? _viewDesktopName ?? "";
            string text = snap.FormatBanner(selectedName);
            if (_inputRedirectActive)
                text += "  |  INPUT→VD (Ctrl+End to release)";
            _desktopBanner.Text = text;
            Text = $"Mars.VirtualDesktop — desktops: {snap.Count} (owned: {_manager.OwnedNames.Count})"
                + (_inputRedirectActive ? " [INPUT→VD]" : "");

            string? keepSelection = _desktopList.SelectedItems.Count > 0
                ? _desktopList.SelectedItems[0].Text
                : selectedName;

            _suppressListSwitch = true;
            _desktopList.BeginUpdate();
            _desktopList.Items.Clear();
            foreach (string name in snap.Names)
            {
                bool owned = _manager.IsOwned(name);
                bool isInput = string.Equals(name, snap.InputDesktopName, StringComparison.OrdinalIgnoreCase);
                bool isSelected = string.Equals(name, selectedName, StringComparison.OrdinalIgnoreCase);

                var item = new ListViewItem(name);
                item.SubItems.Add(owned ? "Yes" : "");
                item.SubItems.Add(isInput ? "●" : "");
                item.SubItems.Add(isSelected ? "●" : "");
                if (owned)
                    item.ForeColor = Color.DarkBlue;
                if (isInput)
                    item.BackColor = Color.FromArgb(220, 255, 220);
                _desktopList.Items.Add(item);
            }
            _desktopList.EndUpdate();

            if (!string.IsNullOrEmpty(keepSelection))
                SelectListItem(keepSelection);

            _suppressListSwitch = false;

            if (forceLog)
                AppendLog(text);
        }
        catch (Exception ex)
        {
            _suppressListSwitch = false;
            _desktopBanner.Text = "Win32 desktops: (enumerate failed) " + ex.Message;
        }
    }

    private void SelectListItem(string name)
    {
        _suppressListSwitch = true;
        try
        {
            foreach (ListViewItem item in _desktopList.Items)
            {
                if (string.Equals(item.Text, name, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
        finally
        {
            _suppressListSwitch = false;
        }
    }

    private static Button MakeButton(string text, int left, int top, int width) =>
        new()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 30
        };

    private bool IsMouseOverMirror()
    {
        Point p = _mirror.PointToClient(Cursor.Position);
        return _mirror.ClientRectangle.Contains(p);
    }

    private void TryBeginInputRedirectFromCtrl()
    {
        if (!_inputRedirectActive && (ModifierKeys & Keys.Control) == Keys.Control && IsMouseOverMirror())
            BeginInputRedirect();
    }

    private void BeginInputRedirect()
    {
        if (_inputRedirectActive) return;
        string? name = _manager.SelectedName;
        if (string.IsNullOrEmpty(name))
        {
            AppendLog("Cannot redirect input: no desktop selected.");
            return;
        }
        if (DesktopAccessor.IsProtectedSystemDesktop(name)
            && name.Equals("Winlogon", StringComparison.OrdinalIgnoreCase))
        {
            AppendLog("Refusing input redirect to Winlogon.");
            return;
        }

        _inputRedirectActive = true;
        _grabBanner.Visible = true;
        _mirror.BackColor = Color.FromArgb(40, 0, 0);
        _mirror.Focus();
        _mirror.Capture = true; // keep receiving mouse while cursor leaves the pane
        SetStatus($"INPUT REDIRECT → [{name}]. Press Ctrl+End to release.");
        AppendLog($"Input redirect ACTIVE → [{name}]. Ctrl+End to stop.");
    }

    private void EndInputRedirect()
    {
        if (!_inputRedirectActive) return;
        _inputRedirectActive = false;
        _grabBanner.Visible = false;
        _mirror.BackColor = Color.Black;
        if (_mirror.Capture)
            _mirror.Capture = false;
        AppendLog("Input redirect OFF — mouse/keyboard back to main desktop.");
    }

    private void Mirror_MouseMove(object? sender, MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control && !_inputRedirectActive)
            BeginInputRedirect();

        if (!_inputRedirectActive) return;
        if (!TryMapMirrorPoint(e.Location, out int sx, out int sy)) return;

        // Soft move via PostMessage path (ClickAt does full click; use move-only through DesktopMessageInput via ClickAt skip)
        // For move we re-use ClickAt is heavy — just store and click on MouseDown.
        _ = sx;
        _ = sy;
    }

    private void Mirror_MouseDown(object? sender, MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control && !_inputRedirectActive)
            BeginInputRedirect();

        if (!_inputRedirectActive) return;
        if (string.IsNullOrEmpty(_manager.SelectedName)) return;
        if (!TryMapMirrorPoint(e.Location, out int sx, out int sy)) return;

        try
        {
            _manager.ClickSelected(sx, sy, rightButton: e.Button == MouseButtons.Right);
        }
        catch (Exception ex)
        {
            AppendLog("Redirect click failed: " + ex.Message);
        }
    }

    private void Mirror_MouseClick(object? sender, MouseEventArgs e)
    {
        // Non-redirect mode: single PostMessage click for convenience
        if (_inputRedirectActive) return;
        if (string.IsNullOrEmpty(_manager.SelectedName) || _currentFrame == null) return;
        if (!TryMapMirrorPoint(e.Location, out int sx, out int sy)) return;

        SafeUi(() =>
        {
            string detail = _manager.ClickSelected(sx, sy, rightButton: e.Button == MouseButtons.Right);
            SetStatus(detail);
        });
    }

    private void ForwardKeyToVirtualDesktop(Keys keyData)
    {
        if (string.IsNullOrEmpty(_manager.SelectedName)) return;

        Keys code = keyData & Keys.KeyCode;
        char? ch = KeyToChar(keyData);
        try
        {
            if (ch.HasValue)
                _manager.TypeSelected(ch.Value.ToString());
            else
                _manager.KeyPressSelected((ushort)code);
        }
        catch (Exception ex)
        {
            AppendLog("Redirect key failed: " + ex.Message);
        }
    }

    private static char? KeyToChar(Keys keyData)
    {
        Keys code = keyData & Keys.KeyCode;
        bool shift = (keyData & Keys.Shift) == Keys.Shift;
        if (code >= Keys.A && code <= Keys.Z)
        {
            char c = (char)('a' + (code - Keys.A));
            return shift ? char.ToUpperInvariant(c) : c;
        }
        if (code >= Keys.D0 && code <= Keys.D9 && !shift)
            return (char)('0' + (code - Keys.D0));
        if (code == Keys.Space) return ' ';
        if (code == Keys.OemPeriod) return shift ? '>' : '.';
        if (code == Keys.Oemcomma) return shift ? '<' : ',';
        if (code == Keys.OemMinus) return shift ? '_' : '-';
        return null;
    }

    private bool TryMapMirrorPoint(Point clientPt, out int screenX, out int screenY)
    {
        screenX = screenY = 0;
        if (_currentFrame == null || _mirror.ClientSize.Width <= 0 || _mirror.ClientSize.Height <= 0)
            return false;

        float imgAspect = (float)_currentFrame.Width / _currentFrame.Height;
        float boxAspect = (float)_mirror.ClientSize.Width / _mirror.ClientSize.Height;

        float drawW, drawH, offsetX, offsetY;
        if (imgAspect > boxAspect)
        {
            drawW = _mirror.ClientSize.Width;
            drawH = drawW / imgAspect;
            offsetX = 0;
            offsetY = (_mirror.ClientSize.Height - drawH) / 2f;
        }
        else
        {
            drawH = _mirror.ClientSize.Height;
            drawW = drawH * imgAspect;
            offsetY = 0;
            offsetX = (_mirror.ClientSize.Width - drawW) / 2f;
        }

        float lx = clientPt.X - offsetX;
        float ly = clientPt.Y - offsetY;
        if (lx < 0 || ly < 0 || lx > drawW || ly > drawH)
            return false;

        screenX = (int)(lx / drawW * _currentFrame.Width);
        screenY = (int)(ly / drawH * _currentFrame.Height);
        return true;
    }

    private void RefreshMirrorFrame()
    {
        if (!_mirroring) return;
        if (string.IsNullOrEmpty(_manager.SelectedName) && string.IsNullOrEmpty(_viewDesktopName))
            return;
        try
        {
            // Prefer selected name; keep viewing even if ownership is external/orphan
            if (!string.IsNullOrEmpty(_viewDesktopName)
                && !string.Equals(_manager.SelectedName, _viewDesktopName, StringComparison.OrdinalIgnoreCase))
            {
                _manager.Select(_viewDesktopName);
            }
            var bmp = _manager.CaptureSelectedBitBlt();
            ShowFrame(bmp);
        }
        catch (Exception ex)
        {
            AppendLog("BitBlt mirror error: " + ex.Message);
        }
    }

    private void ShowFrame(Bitmap bmp)
    {
        var old = _currentFrame;
        _currentFrame = bmp;
        _mirror.Image = _currentFrame;
        old?.Dispose();
    }

    private void SafeUi(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text)
    {
        _status.Text = text;
        AppendLog(text);
    }

    private void AppendLog(string msg)
    {
        if (IsDisposed) return;
        void Write()
        {
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        if (InvokeRequired) BeginInvoke(Write);
        else Write();
    }
}
