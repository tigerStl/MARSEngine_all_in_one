using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MARS.WebAutomation.Models;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceRequestDetailForm : Form
    {
        private readonly PerformanceRequestRecord _record;
        private readonly TextBox _txtUrl;
        private readonly TextBox _txtPayload;
        private readonly TextBox _txtHeaders;
        private readonly TextBox _txtResponse;
        private readonly ListBox _lstCandidates;
        private readonly TextBox _txtTemplate;
        private readonly Button _btnApplyPlaceholder;
        private readonly CheckBox _chkAnchorSelected;
        private readonly TextBox _txtAnchorGroup;
        private readonly CheckBox _chkCorrelationNeeded;
        private readonly TextBox _txtNotes;
        private readonly Button _btnSave;
        private readonly DataGridView _gridHeaders;
        private readonly DataGridView _gridQuery;
        private readonly DataGridView _gridForm;
        private readonly TextBox _txtParamsRaw;

        public PerformanceRequestDetailForm(PerformanceRequestRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            InitializeComponent();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5 };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            top.Controls.Add(NewMetaLabel("Method"), 0, 0);
            top.Controls.Add(NewValueLabel(_record.Method), 1, 0);
            top.Controls.Add(NewMetaLabel("Type / Status"), 2, 0);
            top.Controls.Add(NewValueLabel((_record.ResourceType ?? string.Empty) + " / " + (_record.Status?.ToString() ?? string.Empty)), 3, 0);
            top.Controls.Add(NewMetaLabel("Anchor"), 0, 1);
            top.Controls.Add(NewValueLabel((_record.IsAnchorSelected ? "Selected" : "Not selected") + $" (candidate={_record.AnchorCandidate}, score={_record.AnchorScore})"), 1, 1);
            top.Controls.Add(NewMetaLabel("AnchorGroup"), 2, 1);
            top.Controls.Add(NewValueLabel(_record.AnchorGroup), 3, 1);
            top.Controls.Add(NewMetaLabel("ReplayPolicy"), 0, 2);
            top.Controls.Add(NewValueLabel(_record.ReplayPolicy), 1, 2);
            top.Controls.Add(NewMetaLabel("ValidationHint"), 2, 2);
            top.Controls.Add(NewValueLabel(_record.ValidationHint), 3, 2);
            top.Controls.Add(NewMetaLabel("Correlation"), 0, 3);
            top.Controls.Add(NewValueLabel(_record.CorrelationHint), 1, 3);
            top.Controls.Add(NewMetaLabel("Notes"), 2, 3);
            top.Controls.Add(NewValueLabel(_record.Notes), 3, 3);
            top.Controls.Add(NewMetaLabel("URL"), 0, 4);
            _txtUrl = NewText(_record.Url);
            _txtUrl.Multiline = false;
            _txtUrl.Height = _txtUrl.PreferredHeight;
            top.SetColumnSpan(_txtUrl, 3);
            top.Controls.Add(_txtUrl, 1, 4);
            root.Controls.Add(top, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabHeaders = new TabPage("Headers");
            var tabParams = new TabPage("Params");
            var tabBody = new TabPage("Body");
            var tabResponse = new TabPage("Response");
            var tabParam = new TabPage("Parameterization");
            var tabPreview = new TabPage("Preview");

            _txtHeaders = NewText(_record.Headers);
            _txtPayload = NewText(_record.Payload);
            _txtResponse = NewText(_record.Response);
            _txtParamsRaw = NewText(_record.Parameter);

            _gridHeaders = NewKvGrid("Header", "Value");
            _gridQuery = NewKvGrid("Query", "Value");
            _gridForm = NewKvGrid("Field", "Value");

            tabHeaders.Controls.Add(WrapStandard("Headers Table", _gridHeaders, "Headers Raw", _txtHeaders));
            tabParams.Controls.Add(WrapStandard("Query/Parameter Table", _gridQuery, "Parameter Raw", _txtParamsRaw));
            tabBody.Controls.Add(WrapStandard("Form/Data Table", _gridForm, "Body Raw", _txtPayload));
            tabResponse.Controls.Add(_txtResponse);

            var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            p.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            _lstCandidates = new ListBox { Dock = DockStyle.Fill };
            _txtTemplate = NewText(_record.Url);
            _btnApplyPlaceholder = new Button { Text = "Apply placeholder to URL template", Dock = DockStyle.Left, Width = 230 };
            _btnApplyPlaceholder.Click += (_, __) => ApplySelectedPlaceholder();
            p.Controls.Add(_lstCandidates, 0, 0);
            p.SetRowSpan(_lstCandidates, 3);
            p.Controls.Add(WrapBasic("Template URL", _txtTemplate), 1, 0);
            p.Controls.Add(_btnApplyPlaceholder, 1, 1);
            p.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Recommendation: parameterize volatile fields (timestamp/token/id). Use placeholders like ${userId}, ${token}, ${ts}.",
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 2);
            tabParam.Controls.Add(p);

            var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            BuildPreview(previewHost);
            tabPreview.Controls.Add(previewHost);

            tabs.TabPages.Add(tabHeaders);
            tabs.TabPages.Add(tabParams);
            tabs.TabPages.Add(tabBody);
            tabs.TabPages.Add(tabResponse);
            tabs.TabPages.Add(tabParam);
            tabs.TabPages.Add(tabPreview);
            root.Controls.Add(tabs, 0, 1);

            var editPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _chkAnchorSelected = new CheckBox { Text = "Anchor selected", AutoSize = true, Checked = _record.IsAnchorSelected };
            _chkCorrelationNeeded = new CheckBox { Text = "Correlation needed", AutoSize = true, Checked = _record.CorrelationNeeded };
            _txtAnchorGroup = new TextBox { Width = 140, Text = _record.AnchorGroup ?? "General" };
            _txtNotes = new TextBox { Width = 280, Text = _record.Notes ?? string.Empty };
            _btnSave = new Button { Text = "Save to record", Width = 130, Height = 26 };
            _btnSave.Click += (_, __) => SaveToRecord();
            editPanel.Controls.Add(_chkAnchorSelected);
            editPanel.Controls.Add(_chkCorrelationNeeded);
            editPanel.Controls.Add(new Label { Text = "Group", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            editPanel.Controls.Add(_txtAnchorGroup);
            editPanel.Controls.Add(new Label { Text = "Notes", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            editPanel.Controls.Add(_txtNotes);
            editPanel.Controls.Add(_btnSave);
            root.Controls.Add(editPanel, 0, 2);

            Controls.Add(root);
            RefreshStandardTables();
            LoadCandidates();
        }

        private static TextBox NewText(string value)
        {
            return new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = false,
                WordWrap = false,
                Text = value ?? string.Empty,
                Font = new Font("Consolas", 9f),
                Dock = DockStyle.Fill
            };
        }

        private static DataGridView NewKvGrid(string col1, string col2)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = col1, DataPropertyName = "Key", Width = 260 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = col2, DataPropertyName = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            return g;
        }

        private static Label NewMetaLabel(string text) => new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private static Label NewValueLabel(string text) => new Label { Text = text ?? string.Empty, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

        private static Control WrapBasic(string title, Control body)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            panel.Controls.Add(body, 0, 1);
            return panel;
        }

        private static Control WrapStandard(string tableTitle, Control tableBody, string rawTitle, Control rawBody)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            panel.Controls.Add(new Label { Text = tableTitle, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            panel.Controls.Add(tableBody, 0, 1);
            panel.Controls.Add(new Label { Text = rawTitle, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            panel.Controls.Add(rawBody, 0, 3);
            return panel;
        }

        private static List<KvRow> ParsePairs(string raw, params char[] separators)
        {
            var rows = new List<KvRow>();
            if (string.IsNullOrWhiteSpace(raw))
                return rows;
            var splits = separators == null || separators.Length == 0 ? new[] { '&' } : separators;
            foreach (var piece in raw.Split(splits, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = piece.Trim();
                if (part.Length == 0)
                    continue;
                var idx = part.IndexOf('=');
                if (idx <= 0)
                {
                    rows.Add(new KvRow { Key = part, Value = string.Empty });
                    continue;
                }
                rows.Add(new KvRow
                {
                    Key = part.Substring(0, idx).Trim(),
                    Value = idx + 1 < part.Length ? part.Substring(idx + 1).Trim() : string.Empty
                });
            }
            return rows;
        }

        private void RefreshStandardTables()
        {
            _gridHeaders.DataSource = ParsePairs(_txtHeaders.Text, ';', '\n', '\r');

            var query = string.Empty;
            if (Uri.TryCreate(_txtUrl.Text ?? string.Empty, UriKind.Absolute, out var uri))
                query = uri.Query?.TrimStart('?') ?? string.Empty;
            _gridQuery.DataSource = ParsePairs(string.IsNullOrWhiteSpace(_txtParamsRaw.Text) ? query : _txtParamsRaw.Text, '&', ';', '\n', '\r');

            _gridForm.DataSource = ParsePairs(_txtPayload.Text, '&', ';', '\n', '\r');
        }

        private void LoadCandidates()
        {
            var cands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddKeyCandidates(_record.Parameter, cands);
            AddKeyCandidates(_record.Url, cands);
            AddKeyCandidates(_record.Payload, cands);
            foreach (var c in cands.OrderBy(x => x))
                _lstCandidates.Items.Add(c);
        }

        private static void AddKeyCandidates(string source, HashSet<string> output)
        {
            if (string.IsNullOrWhiteSpace(source))
                return;
            var parts = source.Split(new[] { '&', '?', ';', '\n', '\r', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var idx = p.IndexOf('=');
                if (idx <= 0) continue;
                var key = p.Substring(0, idx).Trim().Trim('"', '\'');
                if (key.Length < 2) continue;
                if (char.IsLetter(key[0]))
                    output.Add(key);
            }
        }

        private void ApplySelectedPlaceholder()
        {
            if (!(_lstCandidates.SelectedItem is string key) || string.IsNullOrWhiteSpace(key))
                return;
            var marker = "${" + key + "}";
            var text = _txtTemplate.Text ?? string.Empty;
            var token = key + "=";
            var i = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return;
            var start = i + token.Length;
            var end = text.IndexOf('&', start);
            if (end < 0) end = text.Length;
            _txtTemplate.Text = text.Substring(0, start) + marker + text.Substring(end);
        }

        private void SaveToRecord()
        {
            _record.Url = _txtUrl.Text ?? string.Empty;
            _record.Headers = _txtHeaders.Text ?? string.Empty;
            _record.Parameter = _txtParamsRaw.Text ?? string.Empty;
            _record.Payload = _txtPayload.Text ?? string.Empty;
            _record.Response = _txtResponse.Text ?? string.Empty;
            _record.IsAnchorSelected = _chkAnchorSelected.Checked;
            _record.CorrelationNeeded = _chkCorrelationNeeded.Checked;
            _record.AnchorGroup = string.IsNullOrWhiteSpace(_txtAnchorGroup.Text) ? "General" : _txtAnchorGroup.Text.Trim();
            _record.Notes = _txtNotes.Text ?? string.Empty;
            _record.CorrelationHint = _record.CorrelationNeeded ? "extract token/cookie and bind" : "none";
            RefreshStandardTables();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BuildPreview(Panel host)
        {
            if (!string.Equals(_record.ResourceType ?? string.Empty, "image", StringComparison.OrdinalIgnoreCase))
            {
                host.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Preview is available for image resources.",
                    TextAlign = ContentAlignment.MiddleCenter
                });
                return;
            }

            var pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
            host.Controls.Add(pic);
            try
            {
                if (!string.IsNullOrWhiteSpace(_record.Url))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    pic.LoadAsync(_record.Url);
                }
            }
            catch
            {
                host.Controls.Clear();
                host.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Image preview failed. URL may require auth cookie.",
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }
        }

        private sealed class KvRow
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
