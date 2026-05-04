using System;
using System.Drawing;
using System.Windows.Forms;

namespace MARS.WebAutomation.UI
{
    /// <summary>Options shown before starting an NBomber performance run.</summary>
    public sealed class PerformanceRunUiOptions
    {
        public string ConcurrencyMode { get; set; } = "constant";
        public int SimulatedUsers { get; set; } = 5;
        public int InitialUsers { get; set; } = 5;
        public int UsersStep { get; set; } = 5;
        public int DurationSeconds { get; set; } = 60;
        /// <summary>Chart / bucket sampling interval in seconds (e.g. 3).</summary>
        public int ChartSampleIntervalSeconds { get; set; } = 3;
        public bool SaveResponseBodies { get; set; }
        public string ResponseBodyMustContain { get; set; }
    }

    internal sealed class PerformanceRunDialog : Form
    {
        private readonly NumericUpDown _numUsers;
        private readonly NumericUpDown _numInitialUsers;
        private readonly NumericUpDown _numUsersStep;
        private readonly NumericUpDown _numDuration;
        private readonly NumericUpDown _numChartInterval;
        private readonly CheckBox _chkSaveResponses;
        private readonly TextBox _txtBodyContains;
        private readonly RadioButton _rbConstantUsers;
        private readonly RadioButton _rbSteppedUsers;

        public PerformanceRunUiOptions ResultOptions { get; private set; }

        public PerformanceRunDialog(PerformanceRunUiOptions seed)
        {
            var s = seed ?? new PerformanceRunUiOptions();

            Text = "Performance run options";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(480, 340);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12),
                RowCount = 0
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            void AddRow(string labelText, Control value, int height = 32)
            {
                var idx = root.RowCount;
                root.RowCount++;
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                root.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, idx);
                root.Controls.Add(value, 1, idx);
            }

            _rbConstantUsers = new RadioButton { Text = "Fixed users", AutoSize = true };
            _rbSteppedUsers = new RadioButton { Text = "Stepped users (initial + step until total)", AutoSize = true, Margin = new Padding(10, 3, 0, 3) };
            var modePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0)
            };
            modePanel.Controls.Add(_rbConstantUsers);
            modePanel.Controls.Add(_rbSteppedUsers);

            _numUsers = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = Math.Max(1, s.SimulatedUsers), Dock = DockStyle.Left, Width = 96 };
            var ramp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0)
            };
            ramp.Controls.Add(_numUsers);
            ramp.Controls.Add(MiniRamp("+5", () => AddUsers(5)));
            ramp.Controls.Add(MiniRamp("+10", () => AddUsers(10)));
            ramp.Controls.Add(MiniRamp("+25", () => AddUsers(25)));
            ramp.Controls.Add(MiniRamp("\u00d72", () =>
            {
                var v = (decimal)_numUsers.Value * 2;
                v = Math.Max(_numUsers.Minimum, Math.Min(_numUsers.Maximum, v));
                _numUsers.Value = v;
            }));
            _numInitialUsers = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = Math.Max(1, s.InitialUsers > 0 ? s.InitialUsers : s.SimulatedUsers), Dock = DockStyle.Left, Width = 96 };
            _numUsersStep = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = Math.Max(1, s.UsersStep > 0 ? s.UsersStep : 5), Dock = DockStyle.Left, Width = 96 };
            var steppedPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0)
            };
            steppedPanel.Controls.Add(new Label { Text = "Initial", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
            steppedPanel.Controls.Add(_numInitialUsers);
            steppedPanel.Controls.Add(new Label { Text = "Step", AutoSize = true, Margin = new Padding(12, 7, 6, 0) });
            steppedPanel.Controls.Add(_numUsersStep);

            _numDuration = new NumericUpDown { Minimum = 1, Maximum = 86400, Value = Math.Max(1, s.DurationSeconds), Dock = DockStyle.Fill };
            _numChartInterval = new NumericUpDown { Minimum = 1, Maximum = 600, Value = Math.Max(1, s.ChartSampleIntervalSeconds), Dock = DockStyle.Fill };
            _chkSaveResponses = new CheckBox { Text = "Save each response under data\\test\\log", Checked = s.SaveResponseBodies, AutoSize = true, Dock = DockStyle.Left };
            _txtBodyContains = new TextBox { Dock = DockStyle.Fill, Text = s.ResponseBodyMustContain ?? string.Empty };

            AddRow("Concurrency mode", modePanel, 34);
            AddRow("Total users", ramp, 36);
            AddRow("Stepped config", steppedPanel, 36);
            AddRow("Duration (seconds)", _numDuration);
            AddRow("Chart sample interval (sec)", _numChartInterval);
            AddRow("", _chkSaveResponses, 28);
            AddRow("Body must contain (optional)", _txtBodyContains);
            var hintIdx = root.RowCount;
            root.RowCount++;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            var hint = new Label
            {
                Text = "Every performance run opens this dialog. Values default to your last run (persisted) so you can raise concurrency step by step to find peak load." + Environment.NewLine
                     + "Validation: if set, responses with status 200–299 must contain this substring (case-insensitive).",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            root.Controls.Add(hint, 0, hintIdx);
            root.SetColumnSpan(hint, 2);
            if (string.Equals(s.ConcurrencyMode, "stepped", StringComparison.OrdinalIgnoreCase))
                _rbSteppedUsers.Checked = true;
            else
                _rbConstantUsers.Checked = true;

            _rbConstantUsers.CheckedChanged += (_, __) => UpdateModeState();
            _rbSteppedUsers.CheckedChanged += (_, __) => UpdateModeState();
            UpdateModeState();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 8)
            };
            var ok = new Button { Text = "OK", Width = 88 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 88 };
            ok.Click += Ok_Click;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            Controls.Add(root);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private Button MiniRamp(string text, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(6, 4, 0, 0),
                Padding = new Padding(8, 2, 8, 2)
            };
            b.Click += (_, __) => onClick();
            return b;
        }

        private void AddUsers(int delta)
        {
            var v = (decimal)_numUsers.Value + delta;
            v = Math.Max(_numUsers.Minimum, Math.Min(_numUsers.Maximum, v));
            _numUsers.Value = v;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            ResultOptions = new PerformanceRunUiOptions
            {
                ConcurrencyMode = _rbSteppedUsers.Checked ? "stepped" : "constant",
                SimulatedUsers = (int)_numUsers.Value,
                InitialUsers = (int)_numInitialUsers.Value,
                UsersStep = (int)_numUsersStep.Value,
                DurationSeconds = (int)_numDuration.Value,
                ChartSampleIntervalSeconds = (int)_numChartInterval.Value,
                SaveResponseBodies = _chkSaveResponses.Checked,
                ResponseBodyMustContain = _txtBodyContains.Text?.Trim() ?? string.Empty
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateModeState()
        {
            var stepped = _rbSteppedUsers.Checked;
            _numInitialUsers.Enabled = stepped;
            _numUsersStep.Enabled = stepped;
            if (_numInitialUsers.Value > _numUsers.Value)
                _numInitialUsers.Value = _numUsers.Value;
        }
    }
}
