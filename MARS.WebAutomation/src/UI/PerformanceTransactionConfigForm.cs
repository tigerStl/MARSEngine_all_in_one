using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceTransactionConfigForm : Form
    {
        private readonly BindingList<TransactionConfigRow> _rows;
        private readonly DataGridView _grid;
        private readonly NumericUpDown _numDurationSeconds;
        private readonly NumericUpDown _numUsers;

        public PerformanceTransactionConfigForm(
            IReadOnlyCollection<TransactionConfigRow> rows,
            int defaultUsers,
            int durationSeconds)
        {
            InitializeComponent();

            _rows = new BindingList<TransactionConfigRow>((rows ?? Array.Empty<TransactionConfigRow>()).Select(Clone).ToList());

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            top.Controls.Add(new Label { Text = "Default Users", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            _numUsers = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100000,
                Value = Math.Max(1, defaultUsers),
                Width = 96
            };
            top.Controls.Add(_numUsers);
            top.Controls.Add(new Label { Text = "Duration (sec)", AutoSize = true, Margin = new Padding(14, 8, 6, 0) });
            _numDurationSeconds = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 36000,
                Value = Math.Max(1, durationSeconds),
                Width = 96
            };
            top.Controls.Add(_numDurationSeconds);
            root.Controls.Add(top, 0, 0);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                DataSource = _rows
            };
            _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Enabled", DataPropertyName = "Enabled", Width = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Transaction", DataPropertyName = "Name", ReadOnly = true, Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Users Override", DataPropertyName = "UsersOverride", Width = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DurationSec Override", DataPropertyName = "DurationSecondsOverride", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Weight", DataPropertyName = "Weight", Width = 90 });
            root.Controls.Add(_grid, 0, 1);

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 96 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 96 };
            bottom.Controls.Add(btnOk);
            bottom.Controls.Add(btnCancel);
            root.Controls.Add(bottom, 0, 2);

            Controls.Add(root);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        public int SelectedDefaultUsers => (int)_numUsers.Value;
        public int SelectedDurationSeconds => (int)_numDurationSeconds.Value;
        public IReadOnlyList<TransactionConfigRow> Rows => _rows.ToList();

        private static TransactionConfigRow Clone(TransactionConfigRow input)
        {
            return new TransactionConfigRow
            {
                Name = input?.Name ?? string.Empty,
                Enabled = input == null || input.Enabled,
                UsersOverride = input?.UsersOverride,
                DurationSecondsOverride = input?.DurationSecondsOverride,
                Weight = Math.Max(1, input?.Weight ?? 1)
            };
        }
    }

    internal sealed class TransactionConfigRow
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public int? UsersOverride { get; set; }
        public int? DurationSecondsOverride { get; set; }
        public int Weight { get; set; } = 1;
    }
}
