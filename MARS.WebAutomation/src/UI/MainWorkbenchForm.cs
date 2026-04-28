using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Playwright;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MARS.WebAutomation;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace MARS.WebAutomation.UI
{
    public partial class MainWorkbenchForm : Form
    {
        private static readonly Logger FormLog = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".UI.MainWorkbenchForm");

        private static readonly object SingletonSync = new object();
        private static MainWorkbenchForm _instance;

        private readonly PlaywrightHostService _host = new PlaywrightHostService();
        private readonly RecordingService _recording = new RecordingService();
        private readonly ReplayService _replay = new ReplayService();
        private readonly NetworkCaptureService _network = new NetworkCaptureService();
        private readonly JsonDataStore _store = new JsonDataStore();
        private readonly WorkbenchSettingsStore _settingsStore = new WorkbenchSettingsStore();
        private WorkbenchSettings _settings;
        private readonly BindingList<SemanticStepRecord> _steps = new BindingList<SemanticStepRecord>();
        private RecordReplaySidebarForm _recordReplaySidebar;
        private ImageList _objectTreeImageList;
        private bool _syncTreeFromPickInProgress;
        private SplitContainer _recordSplit;
        private WebView2 _recordWebView;
        private bool _recordWebViewReady;
        private bool _recordWorkflowUsesBundle;
        private bool _pendingWorkflowStepsPush;
        private const string RecordWorkflowVirtualHost = "mars.workflow";
        private const string RecordWorkflowStartUrl = "https://mars.workflow/index.html";
        private bool _recordSplitDistanceInitialized;
        private ContextMenuStrip _stepsGridMenu;
        private bool _suppressStepsListEvents;
        private ToolStrip _recordCanvasToolStrip;
        private ToolStripButton _btnCanvasZoomOut;
        private ToolStripButton _btnCanvasZoomIn;
        private ToolStripButton _btnCanvasCenter;
        private ToolStripLabel _lblCanvasZoom;
        private int _recordCanvasZoomPercent = 100;
        private bool _recordCanvasDebugEnabled = true;
        private Label _lblHotkey;
        private CheckBox _chkHotkeyCtrl;
        private CheckBox _chkHotkeyAlt;
        private CheckBox _chkHotkeyShift;
        private ComboBox _cmbHotkeyKey;
        private Label _lblIgnoredPagePrefixes;
        private TextBox _txtIgnoredPagePrefixes;
        private Label _lblRecorderTabDepth;
        private NumericUpDown _numRecorderTabDepth;
        private ToolStripSeparator _tsbSepReload;
        private ToolStripButton _tsbReloadEngine;
        private ToolStripSeparator _tsbSepSync;
        private CheckBox _chkSyncFocus;
        private ToolStripControlHost _tsbSyncHost;
        private ToolStrip _gridToolStrip;
        private ToolStripButton _btnGridInsert;
        private ToolStripButton _btnGridDelete;
        private ToolStripButton _btnGridReplay;
        private bool _hotkeyRegistered;
        private bool _updatingRecorderModeFromUi;
        private const int WmHotkey = 0x0312;
        private const int RecordReplayHotkeyId = 0x2277;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;

        public MainWorkbenchForm()
        {
            InitializeComponent();
            ApplyWorkbenchChrome();
            Load += MainWorkbenchForm_Load;
            FormClosed += MainWorkbenchForm_FormClosed;
            gridSteps.DataSource = _steps;
            gridObjectProps.Columns.Clear();
            gridObjectProps.Columns.Add("name", "Property");
            gridObjectProps.Columns.Add("value", "Value");
            gridObjectProps.Columns[0].Width = 140;

            _recording.RecordedStep += Recording_RecordedStep;
            _recording.Picked += Recording_Picked;
            _host.ActiveDocumentUrlChanged += Host_ActiveDocumentUrlChanged;
            SetupObjectPreviewAndToolbar();
            SetupReloadEngineToolbar();
            SetupSyncToolbarCheckbox();
            treeObjects.NodeMouseClick += treeObjects_NodeMouseClick;
            InitRecordReplayTabUi();
            ConfigureStepsGridColumns();
            _steps.ListChanged += Steps_ListChanged;
            gridSteps.CellContentClick += gridSteps_CellContentClick;
            gridSteps.CellDoubleClick += gridSteps_CellDoubleClick;
            gridSteps.CellEndEdit += gridSteps_CellEndEdit;
            tabRecord.SizeChanged += TabRecord_SizeChanged;
            InitHotkeySettingsUi();
        }

        private void Host_ActiveDocumentUrlChanged(object sender, string url)
        {
            void Apply()
            {
                if (string.IsNullOrWhiteSpace(url))
                    return;
                txtUrl.Text = url.Trim();
                UpdateUriLabels();
            }

            if (InvokeRequired)
                BeginInvoke((Action)Apply);
            else
                Apply();
        }

        private void InitRecordReplayTabUi()
        {
            _recordSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 6
            };
            _gridToolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.System
            };
            _btnGridInsert = new ToolStripButton("+");
            _btnGridDelete = new ToolStripButton("−");
            _btnGridReplay = new ToolStripButton("\u25b6");
            _btnGridInsert.Click += (_, __) => InsertStepAfterSelection();
            _btnGridDelete.Click += (_, __) => DeleteSelectedStep();
            _btnGridReplay.Click += (_, __) => _ = TestSelectedStepAsync();
            _gridToolStrip.Items.Add(_btnGridInsert);
            _gridToolStrip.Items.Add(_btnGridDelete);
            _gridToolStrip.Items.Add(new ToolStripSeparator());
            _gridToolStrip.Items.Add(_btnGridReplay);

            var panelGridHost = new Panel { Dock = DockStyle.Fill };
            gridSteps.Dock = DockStyle.Fill;
            panelGridHost.Controls.Add(gridSteps);
            panelGridHost.Controls.Add(_gridToolStrip);
            _recordSplit.Panel1.Controls.Add(panelGridHost);

            _recordWebView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            _recordWebView.CoreWebView2InitializationCompleted += RecordWebView_CoreWebView2InitializationCompleted;

            _recordCanvasToolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.System
            };
            _btnCanvasZoomOut = new ToolStripButton("−");
            _btnCanvasZoomIn = new ToolStripButton("+");
            _btnCanvasCenter = new ToolStripButton("Center");
            _lblCanvasZoom = new ToolStripLabel("100%");
            _btnCanvasZoomOut.Click += (_, __) => SetCanvasZoom(_recordCanvasZoomPercent - 10, centerAfter: false);
            _btnCanvasZoomIn.Click += (_, __) => SetCanvasZoom(_recordCanvasZoomPercent + 10, centerAfter: false);
            _btnCanvasCenter.Click += (_, __) => CenterCanvasViewport();
            _recordCanvasToolStrip.Items.Add(_btnCanvasZoomOut);
            _recordCanvasToolStrip.Items.Add(_btnCanvasZoomIn);
            _recordCanvasToolStrip.Items.Add(new ToolStripSeparator());
            _recordCanvasToolStrip.Items.Add(_btnCanvasCenter);
            _recordCanvasToolStrip.Items.Add(new ToolStripSeparator());
            _recordCanvasToolStrip.Items.Add(_lblCanvasZoom);

            var panelCanvasHost = new Panel { Dock = DockStyle.Fill };
            panelCanvasHost.Controls.Add(_recordWebView);
            // Keep zoom actions callable via shortcuts/messages, but hide the extra top toolbar row in visual canvas.
            _recordCanvasToolStrip.Visible = false;
            _recordSplit.Panel2.Controls.Add(panelCanvasHost);
            ApplyRecordCanvasToolbarLocalization();

            tabRecord.Controls.Clear();
            lblRecordHint.Dock = DockStyle.Top;
            // Dock layout resolves last-in-collection first. Add Fill first, then Top, so the hint
            // reserves the top band and the split fills the remaining height (avoids label painting over grid headers).
            tabRecord.Controls.Add(_recordSplit);
            tabRecord.Controls.Add(lblRecordHint);

            tabMain.SelectedIndexChanged += (_, __) =>
            {
                if (tabMain.SelectedTab == tabRecord)
                    EnsureRecordReplaySidebar();
                UpdateRecorderModeFromUiStateAsync();
            };
        }

        private void InitHotkeySettingsUi()
        {
            _lblHotkey = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Record-Replay hotkey",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };
            _chkHotkeyCtrl = new CheckBox { Text = "Ctrl", AutoSize = true };
            _chkHotkeyAlt = new CheckBox { Text = "Alt", AutoSize = true };
            _chkHotkeyShift = new CheckBox { Text = "Shift", AutoSize = true };
            _cmbHotkeyKey = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbHotkeyKey.Items.AddRange(new object[] { "F8", "F9", "F10", "F11", "F12" });
            panel.Controls.Add(_chkHotkeyCtrl);
            panel.Controls.Add(_chkHotkeyAlt);
            panel.Controls.Add(_chkHotkeyShift);
            panel.Controls.Add(_cmbHotkeyKey);

            layoutSettings.RowCount += 1;
            layoutSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutSettings.Controls.Add(_lblHotkey, 0, layoutSettings.RowCount - 1);
            layoutSettings.Controls.Add(panel, 1, layoutSettings.RowCount - 1);

            _lblIgnoredPagePrefixes = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Skip inject URL prefixes",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _txtIgnoredPagePrefixes = new TextBox
            {
                Dock = DockStyle.Fill
            };
            layoutSettings.RowCount += 1;
            layoutSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutSettings.Controls.Add(_lblIgnoredPagePrefixes, 0, layoutSettings.RowCount - 1);
            layoutSettings.Controls.Add(_txtIgnoredPagePrefixes, 1, layoutSettings.RowCount - 1);

            _lblRecorderTabDepth = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Tab context depth",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _numRecorderTabDepth = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 12,
                Value = 5,
                Width = 72,
                Dock = DockStyle.Left
            };
            layoutSettings.RowCount += 1;
            layoutSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutSettings.Controls.Add(_lblRecorderTabDepth, 0, layoutSettings.RowCount - 1);
            layoutSettings.Controls.Add(_numRecorderTabDepth, 1, layoutSettings.RowCount - 1);
        }

        private void ConfigureStepsGridColumns()
        {
            gridSteps.AutoGenerateColumns = false;
            gridSteps.Columns.Clear();
            gridSteps.ColumnHeadersVisible = true;
            gridSteps.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            gridSteps.ColumnHeadersHeight = 30;
            var del = new DataGridViewButtonColumn
            {
                Name = "colDel",
                UseColumnTextForButtonValue = true,
                Text = "\u2715",
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            del.DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
            gridSteps.Columns.Add(del);
            var hi = new DataGridViewButtonColumn
            {
                Name = "colHi",
                UseColumnTextForButtonValue = true,
                Text = "\U0001f3af",
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            hi.DefaultCellStyle.ForeColor = Color.FromArgb(14, 116, 144);
            gridSteps.Columns.Add(hi);
            var test = new DataGridViewButtonColumn
            {
                Name = "colTest",
                UseColumnTextForButtonValue = true,
                Text = "\u25b6",
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            test.DefaultCellStyle.ForeColor = Color.FromArgb(5, 150, 105);
            gridSteps.Columns.Add(test);
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSeq", DataPropertyName = "RunOrder", ReadOnly = true, Width = 20 });
            var colMs = new DataGridViewTextBoxColumn { Name = "colElapsed", DataPropertyName = "ElapsedMsSincePrev", ReadOnly = true, Width = 100 };
            colMs.DefaultCellStyle.Format = "N0";
            gridSteps.Columns.Add(colMs);
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colKw", DataPropertyName = "Keyword", ReadOnly = false, MinimumWidth = 72, FillWeight = 80 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEvt", DataPropertyName = "SourceEvent", ReadOnly = false, MinimumWidth = 56, FillWeight = 60 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colData", DataPropertyName = "Data", ReadOnly = false, MinimumWidth = 60, FillWeight = 100 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBounds", DataPropertyName = "BoundsDisplay", ReadOnly = false, MinimumWidth = 120, FillWeight = 110 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLogical", DataPropertyName = "LogicalKind", ReadOnly = false, MinimumWidth = 72, FillWeight = 70 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLoc", DataPropertyName = "Locator", ReadOnly = false, MinimumWidth = 100, FillWeight = 160 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colXp", DataPropertyName = "ElementXpath", ReadOnly = false, MinimumWidth = 72, FillWeight = 90 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLocAlt", DataPropertyName = "LocatorAlternates", ReadOnly = false, MinimumWidth = 72, FillWeight = 100 });
            gridSteps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colParam", DataPropertyName = "Parameter", ReadOnly = false, MinimumWidth = 40, FillWeight = 60 });
            gridSteps.ReadOnly = false;
            foreach (DataGridViewColumn c in gridSteps.Columns)
            {
                if (string.Equals(c.Name, "colDel", StringComparison.Ordinal)
                    || string.Equals(c.Name, "colHi", StringComparison.Ordinal)
                    || string.Equals(c.Name, "colTest", StringComparison.Ordinal)
                    || string.Equals(c.Name, "colSeq", StringComparison.Ordinal)
                    || string.Equals(c.Name, "colElapsed", StringComparison.Ordinal))
                    c.ReadOnly = true;
            }

            ApplyStepsGridColumnHeaders();
            _stepsGridMenu = new ContextMenuStrip();
            _stepsGridMenu.Items.Add(new ToolStripMenuItem("Run", null, async (_, __) => await TestSelectedStepAsync().ConfigureAwait(true)));
            _stepsGridMenu.Items.Add(new ToolStripMenuItem("Delete", null, (_, __) => DeleteSelectedStep()));
            _stepsGridMenu.Items.Add(new ToolStripMenuItem("Highlight", null, async (_, __) =>
            {
                var s = GetSelectedStepOrNull();
                if (s != null) await HighlightStepOnPageAsync(s).ConfigureAwait(true);
            }));
            _stepsGridMenu.Items.Add(new ToolStripSeparator());
            _stepsGridMenu.Items.Add(new ToolStripMenuItem("Export", null, (_, __) => ExportStepsFromGrid()));
            _stepsGridMenu.Items.Add(new ToolStripMenuItem("Insert row", null, (_, __) => InsertStepAfterSelection()));
            gridSteps.ContextMenuStrip = _stepsGridMenu;
        }

        private void ApplyStepsGridColumnHeaders()
        {
            void Set(string colName, string headerKey)
            {
                if (!gridSteps.Columns.Contains(colName))
                    return;
                gridSteps.Columns[colName].HeaderText = L(headerKey);
            }

            Set("colDel", "StepsColAction");
            Set("colHi", "StepsColAction");
            Set("colTest", "StepsColAction");
            Set("colSeq", "StepsColOrder");
            Set("colElapsed", "StepsColElapsed");
            Set("colKw", "StepsColKeyword");
            Set("colEvt", "StepsColEvent");
            Set("colData", "StepsColData");
            Set("colBounds", "StepsColBounds");
            Set("colLogical", "StepsColLogicalKind");
            Set("colLoc", "StepsColLocator");
            Set("colXp", "StepsColXPath");
            Set("colLocAlt", "StepsColLocatorAlt");
            Set("colParam", "StepsColParameter");
            if (_stepsGridMenu != null && _stepsGridMenu.Items.Count >= 6)
            {
                _stepsGridMenu.Items[0].Text = L("GridRun");
                _stepsGridMenu.Items[1].Text = L("GridDelete");
                _stepsGridMenu.Items[2].Text = L("GridHighlight");
                _stepsGridMenu.Items[4].Text = L("GridExport");
                _stepsGridMenu.Items[5].Text = L("GridInsertRow");
            }
            if (gridSteps.Columns.Contains("colHi"))
                gridSteps.Columns["colHi"].HeaderText = string.Empty;
            if (gridSteps.Columns.Contains("colTest"))
                gridSteps.Columns["colTest"].HeaderText = string.Empty;
        }

        private void MoveSelectedStep(int delta)
        {
            if (gridSteps.CurrentRow == null)
                return;
            var i = gridSteps.CurrentRow.Index;
            if (i < 0 || i >= _steps.Count)
                return;
            var j = i + delta;
            if (j < 0 || j >= _steps.Count)
                return;
            var item = _steps[i];
            _suppressStepsListEvents = true;
            try
            {
                _steps.RemoveAt(i);
                _steps.Insert(j, item);
            }
            finally
            {
                _suppressStepsListEvents = false;
            }

            RenumberStepMetadata();
            RefreshRecordReplayCanvas();
            if (j >= 0 && j < gridSteps.Rows.Count)
            {
                gridSteps.ClearSelection();
                gridSteps.Rows[j].Selected = true;
            }
        }

        private void Steps_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (_suppressStepsListEvents)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Steps_ListChanged(sender, e)));
                return;
            }

            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                case ListChangedType.ItemDeleted:
                case ListChangedType.Reset:
                    RenumberStepMetadata();
                    RefreshRecordReplayCanvas();
                    break;
            }
        }

        private void RenumberStepMetadata()
        {
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i].RunOrder = i + 1;
                _steps[i].ElapsedMsSincePrev = i == 0 ? 0 : (_steps[i].TimestampUtc - _steps[i - 1].TimestampUtc).TotalMilliseconds;
            }

            if (gridSteps.IsHandleCreated)
                gridSteps.Refresh();
        }

        private void gridSteps_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var col = gridSteps.Columns[e.ColumnIndex].Name;
            if (string.Equals(col, "colDel", StringComparison.Ordinal))
            {
                if (e.RowIndex >= _steps.Count)
                    return;
                _steps.RemoveAt(e.RowIndex);
                return;
            }
            if (string.Equals(col, "colHi", StringComparison.Ordinal))
            {
                if (e.RowIndex < _steps.Count)
                    _ = HighlightStepOnPageAsync(_steps[e.RowIndex]);
                return;
            }
            if (string.Equals(col, "colTest", StringComparison.Ordinal))
            {
                if (e.RowIndex < _steps.Count)
                    _ = TestStepByIndexAsync(e.RowIndex);
                return;
            }
        }

        private void gridSteps_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var col = gridSteps.Columns[e.ColumnIndex].Name;
            if (string.Equals(col, "colDel", StringComparison.Ordinal)
                || string.Equals(col, "colHi", StringComparison.Ordinal)
                || string.Equals(col, "colTest", StringComparison.Ordinal)
                || string.Equals(col, "colSeq", StringComparison.Ordinal)
                || string.Equals(col, "colElapsed", StringComparison.Ordinal))
                return;
            gridSteps.BeginEdit(true);
        }

        private void gridSteps_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _steps.Count || e.ColumnIndex < 0)
                return;
            var col = gridSteps.Columns[e.ColumnIndex].Name;
            if (!string.Equals(col, "colBounds", StringComparison.Ordinal))
                return;
            var raw = Convert.ToString(gridSteps.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
            var b = ParseBoundsDisplay(raw);
            _steps[e.RowIndex].BoundingRect = b;
            gridSteps.Refresh();
        }

        private static BoundingRectDto ParseBoundsDisplay(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            try
            {
                // expected: x=1, y=2, w=3, h=4
                var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                double Read(string key)
                {
                    foreach (var p in parts)
                    {
                        var t = p.Trim();
                        if (!t.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                            continue;
                        var s = t.Substring(key.Length + 1).Trim();
                        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                            return v;
                    }
                    return 0;
                }
                return new BoundingRectDto { X = Read("x"), Y = Read("y"), Width = Read("w"), Height = Read("h") };
            }
            catch
            {
                return null;
            }
        }

        private SemanticStepRecord GetSelectedStepOrNull()
        {
            var idx = gridSteps.CurrentRow?.Index ?? -1;
            if (idx < 0 || idx >= _steps.Count)
                return null;
            return _steps[idx];
        }

        private int GetSelectedStepIndex()
        {
            var idx = gridSteps.CurrentRow?.Index ?? -1;
            return idx >= 0 && idx < _steps.Count ? idx : -1;
        }

        private void InsertStepAfterSelection()
        {
            var idx = GetSelectedStepIndex();
            var insertAt = idx < 0 ? _steps.Count : idx + 1;
            var s = new SemanticStepRecord
            {
                TimestampUtc = DateTime.UtcNow,
                Keyword = string.Empty,
                SourceEvent = string.Empty,
                Data = string.Empty,
                Locator = string.Empty,
                ElementXpath = string.Empty,
                LocatorAlternates = string.Empty,
                LogicalKind = string.Empty,
                Parameter = string.Empty
            };
            _steps.Insert(insertAt, s);
            RenumberStepMetadata();
            gridSteps.ClearSelection();
            if (insertAt >= 0 && insertAt < gridSteps.Rows.Count)
                gridSteps.Rows[insertAt].Selected = true;
        }

        private void DeleteSelectedStep()
        {
            var idx = GetSelectedStepIndex();
            if (idx < 0)
                return;
            _steps.RemoveAt(idx);
        }

        private async Task TestSelectedStepAsync()
        {
            var idx = GetSelectedStepIndex();
            if (idx < 0)
                return;
            await TestStepByIndexAsync(idx).ConfigureAwait(true);
        }

        private async Task TestStepByIndexAsync(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _steps.Count)
                return;
            // Testing a step should stop recorder mode to avoid creating extra test steps.
            if (tsbRecord.Checked)
            {
                tsbRecord.Checked = false;
                await SetRecorderModeAsync("off").ConfigureAwait(true);
                SetStatus("Record stopped. Running step test…");
            }
            await TestStepKeywordAsync(_steps[rowIndex], rowIndex).ConfigureAwait(true);
        }

        private void ExportStepsFromGrid()
        {
            if (_steps.Any(s => string.IsNullOrWhiteSpace(s?.Keyword)))
            {
                MessageBox.Show(this, L("GridExportHasEmptyKeyword"), L("GridExport"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var defaultDir = Path.Combine(_settings?.DataRootFolder ?? DataPathHelper.GetAssemblyBaseDirectory(), "export", "testcase");
            Directory.CreateDirectory(defaultDir);
            using (var dlg = new SaveFileDialog
            {
                Filter = "JSON|*.json",
                InitialDirectory = defaultDir,
                FileName = "steps-export.json"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    var json = JsonConvert.SerializeObject(_steps.ToList(), Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json, Encoding.UTF8);
                    SetStatus("Exported: " + dlg.FileName);
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "ExportStepsFromGrid failed.");
                    MessageBox.Show(this, ex.Message, L("GridExport"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void TabRecord_SizeChanged(object sender, EventArgs e)
        {
            if (_recordSplitDistanceInitialized || _recordSplit == null || tabRecord.ClientSize.Width < 160)
                return;
            try
            {
                _recordSplit.Panel1MinSize = 140;
                _recordSplit.Panel2MinSize = 160;
                var available = Math.Max(0, _recordSplit.ClientSize.Width - _recordSplit.SplitterWidth);
                var min = _recordSplit.Panel1MinSize;
                var max = available - _recordSplit.Panel2MinSize;
                if (max < min)
                    return;

                var target = (int)Math.Round(available * 0.55d);
                if (target < min) target = min;
                if (target > max) target = max;
                _recordSplit.SplitterDistance = target;
                _recordSplitDistanceInitialized = true;
            }
            catch
            {
                // ignore invalid splitter distance during early layout
            }
        }

        public void ApplyCanvasNodeMoved(int index, int x, int y)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplyCanvasNodeMoved(index, x, y)));
                return;
            }

            if (index < 0 || index >= _steps.Count)
                return;
            _steps[index].CanvasX = x;
            _steps[index].CanvasY = y;
        }

        private void RefreshRecordReplayCanvas()
        {
            if (_recordWebView == null)
                return;
            try
            {
                if (!_recordWebViewReady || _recordWebView.CoreWebView2 == null)
                {
                    _pendingWorkflowStepsPush = true;
                    return;
                }

                if (_recordWorkflowUsesBundle)
                    PostWorkflowStepsToReact();
                else
                    _recordWebView.NavigateToString(BuildRecordReplayCanvasFallbackHtml(_steps, _recordCanvasDebugEnabled));

                if (_recordCanvasDebugEnabled)
                {
                    var latest = _steps.Count > 0 ? (_steps[_steps.Count - 1].Keyword ?? string.Empty) : "(none)";
                    SetStatus($"Canvas refresh: steps={_steps.Count}, latest={latest}");
                }
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "Failed to refresh record canvas WebView2.");
            }
        }

        private void SetCanvasZoom(int percent, bool centerAfter)
        {
            _recordCanvasZoomPercent = Math.Max(40, Math.Min(240, percent));
            if (_lblCanvasZoom != null)
                _lblCanvasZoom.Text = _recordCanvasZoomPercent + "%";
            ApplyCanvasZoom();
            if (centerAfter)
                CenterCanvasViewport();
        }

        private async Task InitializeRecordCanvasWebViewAsync()
        {
            if (_recordWebView == null || _recordWebViewReady)
                return;
            try
            {
                await _recordWebView.EnsureCoreWebView2Async(null).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "WebView2 initialization failed.");
            }
        }

        private void RecordWebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess || _recordWebView?.CoreWebView2 == null)
            {
                FormLog.Warn(e.InitializationException, "WebView2 Core initialization failed.");
                return;
            }

            _recordWebViewReady = true;
            _recordWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _recordWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _recordWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            _recordWebView.CoreWebView2.WebMessageReceived -= RecordWebView_WebMessageReceived;
            _recordWebView.CoreWebView2.WebMessageReceived += RecordWebView_WebMessageReceived;
            _recordWebView.NavigationCompleted -= RecordWebView_NavigationCompleted;
            _recordWebView.NavigationCompleted += RecordWebView_NavigationCompleted;

            _recordWorkflowUsesBundle = TryMapWorkflowVirtualHost();
            if (_recordWorkflowUsesBundle)
                _recordWebView.CoreWebView2.Navigate(RecordWorkflowStartUrl);
            else
                _recordWebView.NavigateToString(BuildRecordReplayCanvasFallbackHtml(_steps, _recordCanvasDebugEnabled));
        }

        private void RecordWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                return;
            if (_recordWorkflowUsesBundle)
            {
                if (_pendingWorkflowStepsPush)
                    _pendingWorkflowStepsPush = false;
                PostWorkflowStepsToReact();
                ApplyCanvasZoom();
                return;
            }

            ApplyCanvasZoom();
            if (_steps.Count > 0)
                CenterCanvasViewport();
        }

        private void RecordWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(msg))
                    return;
                var jo = JsonConvert.DeserializeObject<JObject>(msg);
                if (jo == null)
                    return;
                var type = (string)jo["type"] ?? string.Empty;
                if (string.Equals(type, "nodeMoved", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyCanvasNodeMoved((int?)jo["index"] ?? -1, (int?)jo["x"] ?? 0, (int?)jo["y"] ?? 0);
                    return;
                }

                if (string.Equals(type, "wheelZoom", StringComparison.OrdinalIgnoreCase))
                    ApplyCanvasWheelZoom((int?)jo["delta"] ?? 0);
                else if (string.Equals(type, "requestRefresh", StringComparison.OrdinalIgnoreCase))
                    PostWorkflowStepsToReact();
                else if (string.Equals(type, "testStep", StringComparison.OrdinalIgnoreCase))
                    _ = TestStepByIndexAsync((int?)jo["index"] ?? -1);
                else if (string.Equals(type, "editStep", StringComparison.OrdinalIgnoreCase))
                    ApplyCanvasStepEdit((int?)jo["index"] ?? -1, (string)jo["keyword"], (string)jo["data"]);
            }
            catch (Exception ex)
            {
                FormLog.Debug(ex, "Ignored invalid WebView2 message from record canvas.");
            }
        }

        private void ApplyCanvasStepEdit(int index, string keyword, string data)
        {
            if (index < 0 || index >= _steps.Count)
                return;
            if (!string.IsNullOrWhiteSpace(keyword))
                _steps[index].Keyword = keyword.Trim();
            if (data != null)
                _steps[index].Data = data;
            gridSteps.Refresh();
            RefreshRecordReplayCanvas();
        }

        private void ApplyCanvasZoom()
        {
            if (!_recordWebViewReady || _recordWebView?.CoreWebView2 == null)
                return;
            try
            {
                if (_recordWorkflowUsesBundle)
                {
                    var o = new JObject
                    {
                        ["type"] = "setZoom",
                        ["percent"] = _recordCanvasZoomPercent
                    };
                    _recordWebView.CoreWebView2.PostWebMessageAsString(o.ToString(Formatting.None));
                    return;
                }

                _recordWebView.ExecuteScriptAsync("setCanvasZoom(" + _recordCanvasZoomPercent.ToString(CultureInfo.InvariantCulture) + ");");
            }
            catch (Exception ex)
            {
                FormLog.Debug(ex, "ApplyCanvasZoom ignored.");
            }
        }

        private void CenterCanvasViewport()
        {
            if (!_recordWebViewReady || _recordWebView?.CoreWebView2 == null)
                return;
            try
            {
                if (_recordWorkflowUsesBundle)
                {
                    var o = new JObject { ["type"] = "centerView" };
                    _recordWebView.CoreWebView2.PostWebMessageAsString(o.ToString(Formatting.None));
                    return;
                }

                _recordWebView.ExecuteScriptAsync("centerCanvas();");
            }
            catch (Exception ex)
            {
                FormLog.Debug(ex, "CenterCanvasViewport ignored.");
            }
        }

        private static string GetWorkflowAppDistFolder()
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            var dir = string.IsNullOrEmpty(loc)
                ? AppDomain.CurrentDomain.BaseDirectory
                : Path.GetDirectoryName(loc);
            if (string.IsNullOrEmpty(dir))
                dir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                "script", "workflow-app", "dist");
        }

        private bool TryMapWorkflowVirtualHost()
        {
            try
            {
                var dist = GetWorkflowAppDistFolder();
                var index = Path.Combine(dist, "index.html");
                if (!Directory.Exists(dist) || !File.Exists(index))
                {
                    FormLog.Warn("Workflow bundle not found at {Dist}. Run npm install && npm run build in script/workflow-app.", dist);
                    return false;
                }

                _recordWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    RecordWorkflowVirtualHost,
                    dist,
                    CoreWebView2HostResourceAccessKind.Allow);
                return true;
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "SetVirtualHostNameToFolderMapping failed; falling back to inline canvas.");
                return false;
            }
        }

        private void PostWorkflowStepsToReact()
        {
            if (!_recordWebViewReady || _recordWebView?.CoreWebView2 == null || !_recordWorkflowUsesBundle)
                return;
            try
            {
                var arr = new JArray();
                for (var i = 0; i < _steps.Count; i++)
                {
                    var s = _steps[i];
                    var loc = s.Locator ?? string.Empty;
                    if (loc.Length > 120)
                        loc = loc.Substring(0, 117) + "...";
                    var o = new JObject
                    {
                        ["index"] = i,
                        ["keyword"] = s.Keyword ?? string.Empty,
                        ["logicalKind"] = s.LogicalKind ?? string.Empty,
                        ["sourceEvent"] = s.SourceEvent ?? string.Empty,
                        ["data"] = s.Data ?? string.Empty,
                        ["locatorShort"] = loc
                    };
                    if (s.CanvasX.HasValue)
                        o["x"] = s.CanvasX.Value;
                    if (s.CanvasY.HasValue)
                        o["y"] = s.CanvasY.Value;
                    arr.Add(o);
                }

                var payload = new JObject
                {
                    ["type"] = "setSteps",
                    ["uiLanguage"] = _settings?.UiLanguage ?? "en",
                    ["steps"] = arr
                };
                _recordWebView.CoreWebView2.PostWebMessageAsString(payload.ToString(Formatting.None));
            }
            catch (Exception ex)
            {
                FormLog.Debug(ex, "PostWorkflowStepsToReact failed.");
            }
        }

        public void ApplyCanvasWheelZoom(int wheelDelta)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplyCanvasWheelZoom(wheelDelta)));
                return;
            }

            var delta = wheelDelta >= 0 ? 10 : -10;
            SetCanvasZoom(_recordCanvasZoomPercent + delta, centerAfter: false);
        }

        private async Task HighlightStepOnPageAsync(SemanticStepRecord step)
        {
            if (step == null || !_host.IsRunning || _host.Page == null || string.IsNullOrWhiteSpace(step.Locator))
                return;
            try
            {
                var targetPage = await _replay.ResolvePageForStepAsync(_host.Page, step).ConfigureAwait(true);
                if (targetPage == null || targetPage.IsClosed)
                    return;
                var payload = new Dictionary<string, object> { ["hint"] = step.Locator };
                if (!string.IsNullOrWhiteSpace(step.TargetXpath))
                    payload["xpath"] = step.TargetXpath;
                if (step.BoundingRect != null)
                {
                    payload["x"] = step.BoundingRect.X;
                    payload["y"] = step.BoundingRect.Y;
                    payload["w"] = step.BoundingRect.Width;
                    payload["h"] = step.BoundingRect.Height;
                }
                payload["kind"] = "interactive";
                await targetPage.EvaluateAsync<bool>(PageInspectionScripts.ApplyObjectHighlight, payload).ConfigureAwait(true);
                SetStatus("Highlighted: " + (step.Keyword ?? string.Empty));
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "HighlightStepOnPageAsync failed.");
                SetStatus("Highlight failed.");
            }
        }

        private async Task TestStepKeywordAsync(SemanticStepRecord step, int rowIndex)
        {
            if (step == null || !_host.IsRunning || _host.Page == null)
                return;
            try
            {
                var result = await _replay.ExecuteKeywordAsync(_host.Page, step).ConfigureAwait(true);
                if (result.Success)
                {
                    SetStatus($"Test ok: #{rowIndex + 1} {step.Keyword}");
                    if (!string.IsNullOrWhiteSpace(result.DataReturned))
                        step.Data = result.DataReturned;
                    if (gridSteps.IsHandleCreated)
                        gridSteps.Refresh();
                }
                else
                {
                    var err = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Keyword execution failed." : result.ErrorMessage;
                    SetStatus("Test failed: " + err);
                    MessageBox.Show(this, err, "Keyword test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "TestStepKeywordAsync failed.");
                MessageBox.Show(this, ex.Message, "Keyword test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyRecordCanvasToolbarLocalization()
        {
            if (_btnCanvasZoomOut != null)
                _btnCanvasZoomOut.ToolTipText = L("CanvasZoomOut");
            if (_btnCanvasZoomIn != null)
                _btnCanvasZoomIn.ToolTipText = L("CanvasZoomIn");
            if (_btnCanvasCenter != null)
            {
                _btnCanvasCenter.Text = L("CanvasCenter");
                _btnCanvasCenter.ToolTipText = L("CanvasCenter");
            }
            if (_lblCanvasZoom != null)
                _lblCanvasZoom.ToolTipText = L("CanvasZoomLevel");
        }

        private static string KeywordWorkflowCssClass(string kw)
        {
            if (string.IsNullOrEmpty(kw))
                return "kwDef";
            switch (kw)
            {
                case "FillEdit":
                    return "kwFill";
                case "ClickButton":
                    return "kwClick";
                case "SelectDropDown":
                    return "kwSel";
                case "SetBox":
                    return "kwCheck";
                case "FillTable":
                    return "kwTable";
                case "Pegwindow":
                case "WindowGeometry":
                    return "kwDef";
                case "SearchAndClick":
                case "SearchAndUpdate":
                    return "kwSearch";
                default:
                    return "kwDef";
            }
        }

        private static string BuildRecordReplayCanvasFallbackHtml(BindingList<SemanticStepRecord> steps, bool debugEnabled)
        {
            var sb = new StringBuilder(4096);
            sb.Append("<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"/>");
            sb.Append("<meta charset=\"utf-8\"/><style>");
            sb.Append("html,body{height:100%;margin:0;font-family:'Segoe UI',Tahoma,sans-serif;background:#f1f5f9;overflow:auto;}");
            sb.Append("#viewport{position:relative;min-height:520px;padding:18px;transform-origin:0 0;}");
            sb.Append("#cv{position:relative;min-height:520px;}");
            sb.Append(".card{position:absolute;min-width:188px;max-width:300px;border-radius:14px;padding:10px 12px 12px;");
            sb.Append("box-shadow:0 6px 18px rgba(15,23,42,.12);border:1px solid rgba(15,23,42,.1);cursor:grab;}");
            sb.Append(".kw{font-weight:600;font-size:13px;color:#0f172a;}");
            sb.Append(".meta{font-size:11px;color:#475569;margin-top:6px;line-height:1.35;word-break:break-word;}");
            sb.Append(".loc{font-size:10px;color:#64748b;margin-top:4px;max-height:3.2em;overflow:hidden;}");
            sb.Append(".kwFill{background:linear-gradient(155deg,#e8f4fd,#dbeafe);border-color:#1d4ed8;}");
            sb.Append(".kwClick{background:linear-gradient(155deg,#fff7ed,#ffedd5);border-color:#c2410c;}");
            sb.Append(".kwSel{background:linear-gradient(155deg,#f5f3ff,#ede9fe);border-color:#6d28d9;}");
            sb.Append(".kwCheck{background:linear-gradient(155deg,#ecfdf5,#d1fae5);border-color:#047857;}");
            sb.Append(".kwTable{background:linear-gradient(155deg,#ecfeff,#cffafe);border-color:#0e7490;}");
            sb.Append(".kwSearch{background:linear-gradient(155deg,#fef9c3,#fef08a);border-color:#a16207;}");
            sb.Append(".kwDef{background:linear-gradient(155deg,#f8fafc,#e2e8f0);border-color:#475569;}");
            sb.Append(".empty{padding:28px;color:#64748b;font-size:13px;}</style></head><body><div id=\"viewport\"><div id=\"cv\">");
            if (debugEnabled)
            {
                var latest = steps != null && steps.Count > 0 ? (steps[steps.Count - 1].Keyword ?? string.Empty) : "(none)";
                var debug = $"debug steps={steps?.Count ?? 0}; latest={latest}; at={DateTime.Now:HH:mm:ss}";
                sb.Append("<div style=\"position:sticky;top:0;z-index:9999;margin-bottom:8px;padding:6px 10px;font-size:12px;");
                sb.Append("background:#fff7ed;color:#9a3412;border:1px solid #fed7aa;border-radius:8px;\">");
                sb.Append(WebUtility.HtmlEncode(debug));
                sb.Append("</div>");
            }
            if (steps == null || steps.Count == 0)
            {
                sb.Append("<div class=\"empty\">No steps yet. Start recording to see a workflow map.</div>");
            }
            else
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var s = steps[i];
                    var defaultLeft = 28d + (i % 3) * 228d;
                    var defaultTop = 28d + (i / 3) * 120d;
                    var left = s.CanvasX ?? defaultLeft;
                    var top = s.CanvasY ?? defaultTop;
                    if (double.IsNaN(left) || double.IsInfinity(left) || left < 0d || left > 5000d)
                        left = defaultLeft;
                    if (double.IsNaN(top) || double.IsInfinity(top) || top < 0d || top > 5000d)
                        top = defaultTop;
                    var kwClass = KeywordWorkflowCssClass(s.Keyword);
                    var locShort = s.Locator ?? string.Empty;
                    if (locShort.Length > 120)
                        locShort = locShort.Substring(0, 117) + "...";
                    sb.Append("<div class=\"card ").Append(kwClass).Append("\" data-idx=\"").Append(i)
                        .Append("\" style=\"left:").Append(left.ToString("0", CultureInfo.InvariantCulture))
                        .Append("px;top:").Append(top.ToString("0", CultureInfo.InvariantCulture)).Append("px;\">");
                    sb.Append("<div class=\"kw\">").Append(WebUtility.HtmlEncode(s.Keyword ?? string.Empty)).Append("</div>");
                    sb.Append("<div class=\"meta\">").Append(WebUtility.HtmlEncode(s.LogicalKind ?? string.Empty));
                    sb.Append(" · ").Append(WebUtility.HtmlEncode(s.SourceEvent ?? string.Empty)).Append("</div>");
                    sb.Append("<div class=\"meta\">").Append(WebUtility.HtmlEncode(s.Data ?? string.Empty)).Append("</div>");
                    sb.Append("<div class=\"loc\">").Append(WebUtility.HtmlEncode(locShort)).Append("</div>");
                    sb.Append("</div>");
                }
            }

            sb.Append("</div></div><script type=\"text/javascript\">");
            sb.Append("var d={t:null,ox:0,oy:0};");
            sb.Append("function dn(ev){var c=ev.target;while(c&&c.id!=='cv'){var cn=c.className||'';");
            sb.Append("if(typeof cn!=='string')cn='';if(cn.indexOf('card')>=0)break;c=c.parentElement;}");
            sb.Append("if(!c||c.id==='cv')return;d.t=c;d.ox=ev.clientX-c.offsetLeft;d.oy=ev.clientY-c.offsetTop;");
            sb.Append("document.onmousemove=mv;document.onmouseup=up;}");
            sb.Append("function mv(ev){if(!d.t)return;d.t.style.left=(ev.clientX-d.ox)+'px';d.t.style.top=(ev.clientY-d.oy)+'px';}");
            sb.Append("function up(){if(d.t){try{var x=parseInt(d.t.style.left||'0',10)||0;var y=parseInt(d.t.style.top||'0',10)||0;");
            sb.Append("chrome.webview.postMessage(JSON.stringify({type:'nodeMoved',index:parseInt(d.t.getAttribute('data-idx'),10),x:x,y:y}));}catch(e){}}d.t=null;document.onmousemove=null;document.onmouseup=null;}");
            sb.Append("function setCanvasZoom(p){var v=document.getElementById('viewport');if(!v)return;var z=(parseInt(p,10)||100)/100;v.style.transform='scale('+z+')';");
            sb.Append("v.style.width=(100/z)+'%';}");
            sb.Append("function centerCanvas(){var cards=document.getElementsByClassName('card');if(!cards||cards.length===0)return;");
            sb.Append("var minX=999999,minY=999999,maxX=0,maxY=0;for(var i=0;i<cards.length;i++){var c=cards[i];");
            sb.Append("var x=c.offsetLeft,y=c.offsetTop,w=c.offsetWidth,h=c.offsetHeight;if(x<minX)minX=x;if(y<minY)minY=y;");
            sb.Append("if(x+w>maxX)maxX=x+w;if(y+h>maxY)maxY=y+h;}var cx=(minX+maxX)/2,cy=(minY+maxY)/2;");
            sb.Append("if(!isFinite(cx)||!isFinite(cy))return;");
            sb.Append("window.scrollTo(Math.max(0,cx-window.innerWidth/2),Math.max(0,cy-window.innerHeight/2));}");
            sb.Append("document.getElementById('cv').addEventListener('mousedown',dn,true);");
            sb.Append("document.addEventListener('wheel',function(ev){if(!ev.ctrlKey)return;ev.preventDefault();");
            sb.Append("try{chrome.webview.postMessage(JSON.stringify({type:'wheelZoom',delta:(ev.deltaY<0?120:-120)}));}catch(e){}},{passive:false});");
            sb.Append("</script></body></html>");
            return sb.ToString();
        }

        private void SetupReloadEngineToolbar()
        {
            _tsbSepReload = new ToolStripSeparator();
            _tsbReloadEngine = new ToolStripButton
            {
                Name = "tsbReloadEngine",
                Text = "Reload engine",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Image = FormsIconHelper.ToBitmap(IconChar.Rotate, Color.FromArgb(51, 65, 85), 20, 0d, FlipOrientation.Normal),
                ImageScaling = ToolStripItemImageScaling.None
            };
            _tsbReloadEngine.Click += tsbReloadEngine_Click;
            toolMain.Items.Add(_tsbSepReload);
            toolMain.Items.Add(_tsbReloadEngine);
        }

        private void SetupSyncToolbarCheckbox()
        {
            _tsbSepSync = new ToolStripSeparator();
            _chkSyncFocus = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Text = "Sync",
                Margin = new Padding(6, 0, 0, 0)
            };
            _chkSyncFocus.CheckedChanged += (_, __) => UpdateRecorderModeFromUiStateAsync();
            _tsbSyncHost = new ToolStripControlHost(_chkSyncFocus)
            {
                Name = "tsbSyncHost",
                Margin = new Padding(2, 0, 0, 0),
                AutoSize = false,
                Width = 120
            };
            toolMain.Items.Add(_tsbSepSync);
            toolMain.Items.Add(_tsbSyncHost);
        }

        /// <summary>
        /// Layout, typography, command bar icons (Font Awesome via NuGet), and control chrome.
        /// </summary>
        private void ApplyWorkbenchChrome()
        {
            using (WebAutomationMethodTrace.Begin(FormLog, nameof(ApplyWorkbenchChrome)))
            {
                ApplyWorkbenchChromeCore();
            }
        }

        private void ApplyWorkbenchChromeCore()
        {
            var family = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
                ? "Microsoft YaHei UI"
                : "Segoe UI";
            Font uiFont;
            try
            {
                uiFont = new Font(family, 9F, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (ArgumentException ex)
            {
                FormLog.Warn(ex, "Failed to use preferred UI font family: {Family}", family);
                uiFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
            }

            var surface = Color.FromArgb(248, 250, 252);
            var workspace = Color.FromArgb(241, 245, 249);
            var ink = Color.FromArgb(51, 65, 85);
            var primary = Color.FromArgb(0, 122, 204);
            var primaryHover = Color.FromArgb(0, 105, 181);

            Font = uiFont;
            menuMain.Font = uiFont;
            toolMain.Font = uiFont;
            statusMain.Font = uiFont;
            tabMain.Font = uiFont;
            BackColor = workspace;
            menuMain.BackColor = Color.White;
            menuMain.RenderMode = ToolStripRenderMode.Professional;
            toolMain.BackColor = Color.FromArgb(252, 252, 254);
            toolMain.ForeColor = ink;
            toolMain.GripStyle = ToolStripGripStyle.Hidden;
            toolMain.ImageScalingSize = new Size(22, 22);
            toolMain.Padding = new Padding(6, 4, 8, 4);
            toolMain.Stretch = false;
            toolMain.AutoSize = false;
            toolMain.Height = Math.Max(32, toolMain.ImageScalingSize.Height + toolMain.Padding.Vertical + 6);
            statusMain.BackColor = surface;
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = ink;
            tabMain.Padding = new Point(10, 6);
            tabMain.BackColor = workspace;
            tabTarget.BackColor = workspace;
            tabObjects.BackColor = workspace;
            tabRecord.BackColor = workspace;
            tabSettings.BackColor = workspace;
            panelTargetCard.BackColor = Color.White;

            tslBrand.Font = new Font(uiFont.FontFamily, 10f, FontStyle.Bold, GraphicsUnit.Point);
            tslBrand.ForeColor = Color.FromArgb(15, 23, 42);
            lblSectionUrl.Font = new Font(uiFont.FontFamily, 9.75f, FontStyle.Bold, GraphicsUnit.Point);

            ToolStripManager.RenderMode = ToolStripManagerRenderMode.Professional;

            void StyleToolbarButton(ToolStripButton b, IconChar icon)
            {
                b.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                b.TextImageRelation = TextImageRelation.ImageBeforeText;
                b.Image = FormsIconHelper.ToBitmap(icon, ink, 20, 0d, FlipOrientation.Normal);
                b.ImageScaling = ToolStripItemImageScaling.None;
            }

            StyleToolbarButton(tsbTarget, IconChar.Globe);
            StyleToolbarButton(tsbRecord, IconChar.CircleDot);
            StyleToolbarButton(tsbReplay, IconChar.Play);
            StyleToolbarButton(tsbExport, IconChar.FileExport);
            StyleToolbarButton(tsbImport, IconChar.FileImport);
            StyleToolbarButton(tsbSave, IconChar.FloppyDisk);
            if (_btnGridInsert != null)
            {
                _btnGridInsert.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _btnGridInsert.Image = FormsIconHelper.ToBitmap(IconChar.Plus, ink, 16, 0d, FlipOrientation.Normal);
                _btnGridInsert.ToolTipText = L("GridInsertRow");
            }
            if (_btnGridDelete != null)
            {
                _btnGridDelete.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _btnGridDelete.Image = FormsIconHelper.ToBitmap(IconChar.Trash, Color.FromArgb(220, 38, 38), 16, 0d, FlipOrientation.Normal);
                _btnGridDelete.ToolTipText = L("GridDelete");
            }
            if (_btnGridReplay != null)
            {
                _btnGridReplay.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _btnGridReplay.Image = FormsIconHelper.ToBitmap(IconChar.Play, Color.FromArgb(5, 150, 105), 16, 0d, FlipOrientation.Normal);
                _btnGridReplay.ToolTipText = L("GridRun");
            }

            void StyleGrid(DataGridView g)
            {
                g.EnableHeadersVisualStyles = false;
                g.BorderStyle = BorderStyle.None;
                g.BackgroundColor = Color.FromArgb(250, 250, 252);
                g.GridColor = Color.FromArgb(220, 223, 230);
                g.RowHeadersVisible = false;
                g.AllowUserToAddRows = false;
                g.AllowUserToDeleteRows = false;
                g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                g.MultiSelect = false;
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 245, 249);
                g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 44, 52);
                g.ColumnHeadersDefaultCellStyle.Font = new Font(uiFont, FontStyle.Bold);
                g.ColumnHeadersHeight = 28;
                g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                g.DefaultCellStyle.Font = uiFont;
                g.DefaultCellStyle.SelectionBackColor = primary;
                g.DefaultCellStyle.SelectionForeColor = Color.White;
                g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 254);
            }

            StyleGrid(gridObjectProps);
            StyleGrid(gridSteps);

            treeObjects.BorderStyle = BorderStyle.FixedSingle;
            treeObjects.LineColor = Color.FromArgb(200, 203, 210);
            treeObjects.HideSelection = false;
            treeObjects.BackColor = Color.White;
            treeObjects.Font = uiFont;
            EnsureObjectTreeImageList();

            txtUrl.Font = uiFont;
            txtUrl.Multiline = false;
            txtUrl.Height = txtUrl.PreferredHeight;
            const int urlRowHeight = 40;
            var vPad = Math.Max(2, (urlRowHeight - txtUrl.Height) / 2);
            txtUrl.Margin = new Padding(4, vPad, 10, vPad);

            void StyleOutlineButton(Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.UseVisualStyleBackColor = false;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                b.BackColor = Color.White;
                b.ForeColor = ink;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
                b.Font = uiFont;
            }

            void StylePrimaryButton(Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.UseVisualStyleBackColor = false;
                b.FlatAppearance.BorderSize = 0;
                b.BackColor = primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.MouseOverBackColor = primaryHover;
                b.Font = uiFont;
            }

            StyleOutlineButton(btnStartBrowser);
            StylePrimaryButton(btnNavigate);
            StyleOutlineButton(btnRefreshTree);
            StyleOutlineButton(btnSaveSettings);

            try
            {
                var textBarH = txtUrl.PreferredHeight + txtUrl.Margin.Vertical;
                var startH = btnStartBrowser.PreferredSize.Height + btnStartBrowser.Margin.Vertical;
                var navH = btnNavigate.PreferredSize.Height + btnNavigate.Margin.Vertical;
                var urlRowNeed = Math.Max(58, Math.Max(textBarH, Math.Max(startH, navH)) + 6);
                if (layoutTarget.RowStyles.Count > 0)
                    layoutTarget.RowStyles[0] = new RowStyle(SizeType.Absolute, urlRowNeed);
                layoutTarget.PerformLayout();
            }
            catch (Exception ex)
            {
                // ignore if layout not ready
                FormLog.Debug(ex, "Layout not ready when applying target row style.");
            }

            lblRecordHint.Font = uiFont;
            lblRecordHint.ForeColor = ink;
            lblRecordHint.BackColor = Color.FromArgb(241, 245, 249);
            lblRecordHint.Padding = new Padding(10, 8, 10, 8);
            lblRecordHint.AutoSize = false;
            lblRecordHint.Dock = DockStyle.Top;
            lblRecordHint.Height = 36;

            if (_lblRecorderTabDepth != null)
                _lblRecorderTabDepth.Font = uiFont;
            if (_numRecorderTabDepth != null)
                _numRecorderTabDepth.Font = uiFont;

            foreach (Control c in layoutSettings.Controls)
            {
                if (c is TextBox || c is NumericUpDown || c is CheckBox || c is Label)
                    c.Font = uiFont;
            }

            foreach (Control c in layoutTarget.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.Font = uiFont;
                    lbl.ForeColor = ink;
                }
            }

            StyleOutlineButton(btnTreeSearchGo);
            StyleOutlineButton(btnTreeSearchPrev);
            StyleOutlineButton(btnTreeSearchNext);
            btnTreeSearchGo.Width = 34;
            btnTreeSearchGo.Text = string.Empty;
            btnTreeSearchGo.Image = FormsIconHelper.ToBitmap(IconChar.MagnifyingGlass, ink, 18, 0d, FlipOrientation.Normal);
            btnTreeSearchPrev.Width = 34;
            btnTreeSearchPrev.Text = string.Empty;
            btnTreeSearchPrev.Image = FormsIconHelper.ToBitmap(IconChar.ChevronLeft, ink, 18, 0d, FlipOrientation.Normal);
            btnTreeSearchNext.Width = 34;
            btnTreeSearchNext.Text = string.Empty;
            btnTreeSearchNext.Image = FormsIconHelper.ToBitmap(IconChar.ChevronRight, ink, 18, 0d, FlipOrientation.Normal);
            chkTreeRegex.FlatStyle = FlatStyle.Flat;
            chkTreeRegex.UseVisualStyleBackColor = false;
            chkTreeRegex.BackColor = Color.White;
            chkTreeRegex.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        }

        public static MainWorkbenchForm GetOrCreateSingleton()
        {
            lock (SingletonSync)
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new MainWorkbenchForm();
                return _instance;
            }
        }

        internal static void ResetSingleton(MainWorkbenchForm form)
        {
            lock (SingletonSync)
            {
                if (ReferenceEquals(_instance, form))
                    _instance = null;
            }
        }

        private void MainWorkbenchForm_Load(object sender, EventArgs e)
        {
            _settings = _settingsStore.Load();
            if (string.IsNullOrWhiteSpace(_settings.UiLanguage))
                _settings.UiLanguage = "en";
            ApplySettingsToUi();
            ApplyLocalizedUi();
            _ = InitializeRecordCanvasWebViewAsync();
            txtUrl.TextChanged += (_, __) => UpdateUriLabels();
            UpdateUriLabels();
            ClearObjectInspectUi(removePageOverlay: false);
            RegisterRecordReplayHotkey();
            UpdateRecorderModeFromUiStateAsync();
            RenumberStepMetadata();
            RefreshRecordReplayCanvas();
        }

        private void ApplySettingsToUi()
        {
            txtDataRoot.Text = _settings.DataRootFolder;
            chkHeadless.Checked = _settings.Headless;
            numTimeout.Value = Math.Min(numTimeout.Maximum, Math.Max(numTimeout.Minimum, _settings.DefaultTimeoutMs));
            chkPersistHeaders.Checked = _settings.PersistSensitiveHeaders;
            txtBrowserChannel.Text = _settings.BrowserChannel ?? string.Empty;
            numViewportW.Value = Math.Min(numViewportW.Maximum, Math.Max(numViewportW.Minimum, _settings.ViewportWidth));
            numViewportH.Value = Math.Min(numViewportH.Maximum, Math.Max(numViewportH.Minimum, _settings.ViewportHeight));
            if (_chkHotkeyCtrl != null) _chkHotkeyCtrl.Checked = _settings.RecordReplayHotkeyCtrl;
            if (_chkHotkeyAlt != null) _chkHotkeyAlt.Checked = _settings.RecordReplayHotkeyAlt;
            if (_chkHotkeyShift != null) _chkHotkeyShift.Checked = _settings.RecordReplayHotkeyShift;
            if (_cmbHotkeyKey != null)
            {
                var keyText = string.IsNullOrWhiteSpace(_settings.RecordReplayHotkeyKey) ? "F12" : _settings.RecordReplayHotkeyKey.Trim().ToUpperInvariant();
                var idx = _cmbHotkeyKey.Items.IndexOf(keyText);
                _cmbHotkeyKey.SelectedIndex = idx >= 0 ? idx : _cmbHotkeyKey.Items.IndexOf("F12");
            }
            if (_txtIgnoredPagePrefixes != null)
                _txtIgnoredPagePrefixes.Text = _settings.RecorderIgnoredPageUrlPrefixes ?? string.Empty;
            if (_numRecorderTabDepth != null)
                _numRecorderTabDepth.Value = Math.Min(_numRecorderTabDepth.Maximum,
                    Math.Max(_numRecorderTabDepth.Minimum, _settings.RecorderTabContextAncestorDepth));
        }

        private void ReadSettingsFromUi()
        {
            _settings.DataRootFolder = txtDataRoot.Text.Trim();
            _settings.Headless = chkHeadless.Checked;
            _settings.DefaultTimeoutMs = (int)numTimeout.Value;
            _settings.PersistSensitiveHeaders = chkPersistHeaders.Checked;
            _settings.BrowserChannel = txtBrowserChannel.Text.Trim();
            _settings.ViewportWidth = (int)numViewportW.Value;
            _settings.ViewportHeight = (int)numViewportH.Value;
            _settings.RecordReplayHotkeyCtrl = _chkHotkeyCtrl != null && _chkHotkeyCtrl.Checked;
            _settings.RecordReplayHotkeyAlt = _chkHotkeyAlt != null && _chkHotkeyAlt.Checked;
            _settings.RecordReplayHotkeyShift = _chkHotkeyShift != null && _chkHotkeyShift.Checked;
            _settings.RecordReplayHotkeyKey = _cmbHotkeyKey?.SelectedItem?.ToString() ?? "F12";
            _settings.RecorderIgnoredPageUrlPrefixes = _txtIgnoredPagePrefixes?.Text?.Trim() ?? string.Empty;
            _settings.RecorderTabContextAncestorDepth = _numRecorderTabDepth != null
                ? (int)_numRecorderTabDepth.Value
                : 5;
        }

        private void UpdateUriLabels()
        {
            if (!Uri.TryCreate(txtUrl.Text?.Trim(), UriKind.Absolute, out var uri))
            {
                lblScheme.Text = L("UriScheme") + L("UriInvalid");
                lblHost.Text = L("UriHost");
                lblPort.Text = L("UriPort");
                lblPath.Text = L("UriPath");
                lblQuery.Text = L("UriQuery");
                return;
            }

            lblScheme.Text = L("UriScheme") + uri.Scheme;
            lblHost.Text = L("UriHost") + uri.Host;
            lblPort.Text = uri.IsDefaultPort ? L("UriPortDefault") : L("UriPort") + uri.Port;
            lblPath.Text = L("UriPath") + uri.AbsolutePath;
            lblQuery.Text = L("UriQuery") + (string.IsNullOrEmpty(uri.Query) ? L("UriQueryNone") : uri.Query);
        }

        private void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => statusLabel.Text = text));
                return;
            }
            statusLabel.Text = text;
        }

        private void RegisterRecordReplayHotkey()
        {
            UnregisterRecordReplayHotkey();
            if (_settings == null) return;
            uint mod = 0;
            if (_settings.RecordReplayHotkeyCtrl) mod |= ModControl;
            if (_settings.RecordReplayHotkeyAlt) mod |= ModAlt;
            if (_settings.RecordReplayHotkeyShift) mod |= ModShift;
            var key = ResolveVirtualKey(_settings.RecordReplayHotkeyKey);
            _hotkeyRegistered = RegisterHotKey(Handle, RecordReplayHotkeyId, mod, key);
        }

        private void UnregisterRecordReplayHotkey()
        {
            if (!_hotkeyRegistered) return;
            UnregisterHotKey(Handle, RecordReplayHotkeyId);
            _hotkeyRegistered = false;
        }

        private static uint ResolveVirtualKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return (uint)Keys.F12;
            if (Enum.TryParse(key, true, out Keys k))
                return (uint)k;
            return (uint)Keys.F12;
        }

        private void Recording_RecordedStep(object sender, RecorderEventArgs e)
        {
            if (e?.Step == null)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, RecorderEventArgs>(Recording_RecordedStep), sender, e);
                return;
            }
            if (!tsbRecord.Checked)
                return;

            void Add()
            {
                _steps.Add(e.Step);
                SetStatus("Recorded: " + e.Step.Keyword);
            }

            Add();
            RefreshRecordReplayCanvas();
            PushStepToSidebar(e.Step);
            if (ShouldSyncFocusedElement())
                SyncTreeSelectionFromPick(e.Step, highlightPage: false);
        }

        private void Recording_Picked(object sender, PickEventArgs e)
        {
            if (e?.Snapshot == null)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, PickEventArgs>(Recording_Picked), sender, e);
                return;
            }

            void ShowPick()
            {
                if (ShouldSyncFocusedElement())
                    SyncTreeSelectionFromPick(e.Snapshot, highlightPage: !e.IsSyncRequest);
                SetStatus("Picked: " + e.Snapshot.Locator);
            }
            ShowPick();
        }

        private async void SyncTreeSelectionFromPick(SemanticStepRecord snapshot, bool highlightPage = true)
        {
            if (snapshot == null) return;
            if (_syncTreeFromPickInProgress) return;
            if (!ShouldSyncFocusedElement()) return;
            _syncTreeFromPickInProgress = true;
            try
            {
                // Always refresh before matching to avoid stale tree selection.
                await PopulateObjectTreeFromPageAsync(showErrorDialog: false).ConfigureAwait(true);
                var best = FindBestNodeBySnapshot(snapshot);
                if (best != null)
                {
                    treeObjects.SelectedNode = best;
                    best.EnsureVisible();
                    await ActivateObjectTreeNodeAsync(best, highlightPage).ConfigureAwait(true);
                    return;
                }
                ShowNodeDetails(snapshot);
            }
            finally
            {
                _syncTreeFromPickInProgress = false;
            }
        }

        private bool ShouldSyncFocusedElement()
        {
            return _chkSyncFocus != null
                && _chkSyncFocus.Checked
                && tabMain.SelectedTab == tabObjects;
        }

        private async void UpdateRecorderModeFromUiStateAsync()
        {
            if (_updatingRecorderModeFromUi || _host.Page == null)
                return;
            _updatingRecorderModeFromUi = true;
            try
            {
                if (tsbRecord.Checked)
                {
                    await SetRecorderModeAsync("record").ConfigureAwait(true);
                    return;
                }
                if (tsbTarget.Checked)
                {
                    await SetRecorderModeAsync("pick").ConfigureAwait(true);
                    return;
                }
                if (ShouldSyncFocusedElement())
                {
                    await EnsureRecordingInstalledAsync().ConfigureAwait(true);
                    await SetRecorderModeAsync("sync").ConfigureAwait(true);
                    SetStatus("Sync mode active.");
                }
                else
                {
                    await SetRecorderModeAsync("off").ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "Failed to update recorder mode from UI state.");
            }
            finally
            {
                _updatingRecorderModeFromUi = false;
            }
        }

        private TreeNode FindBestNodeBySnapshot(SemanticStepRecord snapshot)
        {
            if (snapshot == null) return null;
            var locator = snapshot.Locator ?? string.Empty;
            TreeNode best = null;
            var score = -1;
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is ObjectTreeNodeDto dto)
                    {
                        var s = 0;
                        if (!string.IsNullOrWhiteSpace(dto.PlaywrightLocator) &&
                            dto.PlaywrightLocator.IndexOf(locator, StringComparison.OrdinalIgnoreCase) >= 0) s += 10;
                        if (!string.IsNullOrWhiteSpace(dto.LocatorHint) &&
                            (dto.LocatorHint.IndexOf(locator, StringComparison.OrdinalIgnoreCase) >= 0 ||
                             locator.IndexOf(dto.LocatorHint, StringComparison.OrdinalIgnoreCase) >= 0)) s += 8;
                        if (!string.IsNullOrWhiteSpace(dto.Xpath) &&
                            locator.IndexOf(dto.Xpath, StringComparison.OrdinalIgnoreCase) >= 0) s += 5;
                        if (snapshot.BoundingRect != null && dto.Bounds != null)
                        {
                            var sx = snapshot.BoundingRect.X + snapshot.BoundingRect.Width / 2.0;
                            var sy = snapshot.BoundingRect.Y + snapshot.BoundingRect.Height / 2.0;
                            var dx = dto.Bounds.X + dto.Bounds.Width / 2.0;
                            var dy = dto.Bounds.Y + dto.Bounds.Height / 2.0;
                            var dist = Math.Abs(sx - dx) + Math.Abs(sy - dy);
                            if (dist < 40) s += 12;
                            else if (dist < 120) s += 8;
                            else if (dist < 240) s += 4;
                        }
                        if (s > score) { score = s; best = node; }
                    }
                    if (node.Nodes.Count > 0) Walk(node.Nodes);
                }
            }
            Walk(treeObjects.Nodes);
            return score > 0 ? best : null;
        }

        private async void btnStartBrowser_Click(object sender, EventArgs e)
        {
            try
            {
                SetStatus("Starting browser…");
                ReadSettingsFromUi();
                _recording.ResetForNewContext();
                await _host.StartAsync(_settings).ConfigureAwait(true);
                await _recording.InstallAsync(_host.Page).ConfigureAwait(true);
                _network.Detach();
                _network.Clear();
                _network.Attach(_host.Page, _settings.PersistSensitiveHeaders);
                ClearObjectInspectUi(removePageOverlay: false);
                SetStatus("Browser ready.");
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "Start browser failed.");
                MessageBox.Show(this, ex.Message, "Start browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Error.");
            }
        }

        private async void btnNavigate_Click(object sender, EventArgs e)
        {
            if (!_host.IsRunning)
            {
                MessageBox.Show(this, "Start the browser first.", "Navigate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                SetStatus("Navigating…");
                await _host.NavigateAsync(txtUrl.Text.Trim()).ConfigureAwait(true);
                if (_host.Page != null)
                    txtUrl.Text = _host.Page.Url;
                UpdateUriLabels();
                ClearObjectInspectUi(removePageOverlay: true);
                SetStatus("Loaded.");
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "Navigate failed.");
                MessageBox.Show(this, ex.Message, "Navigate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRefreshTree_Click(object sender, EventArgs e)
        {
            await PopulateObjectTreeFromPageAsync(showErrorDialog: true).ConfigureAwait(true);
        }

        /// <summary>True for built-in browser UI tabs (not normal web documents).</summary>
        private static bool IsInternalBrowserSchemePage(IPage page)
        {
            if (page == null)
                return true;
            string url;
            try
            {
                url = page.Url ?? string.Empty;
            }
            catch
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (url.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase))
                return true;
            if (url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase))
                return true;
            if (url.StartsWith("chrome-devtools://", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>Active page first, then other open pages in the same browser context (e.g. <c>window.open</c> popups). Skips <c>chrome://</c> / <c>devtools://</c> internal pages.</summary>
        private List<IPage> GetOrderedPagesForObjectInspect()
        {
            var list = new List<IPage>();
            var main = _host.Page;
            if (main != null && !main.IsClosed && !IsInternalBrowserSchemePage(main))
                list.Add(main);
            var ctx = _host.Context;
            if (ctx?.Pages == null)
                return list;
            foreach (var p in ctx.Pages)
            {
                if (p == null || p.IsClosed || IsInternalBrowserSchemePage(p))
                    continue;
                if (list.Any(x => ReferenceEquals(x, p)))
                    continue;
                list.Add(p);
            }
            return list;
        }

        private static string TruncateTreeLabel(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 1) + "…";
        }

        private async Task<List<ObjectTreeNodeDto>> BuildMergedObjectTreeRootsAsync()
        {
            var merged = new List<ObjectTreeNodeDto>();
            var pages = GetOrderedPagesForObjectInspect();
            if (pages.Count == 0)
                return merged;

            for (var i = 0; i < pages.Count; i++)
            {
                var p = pages[i];
                try
                {
                    await p.EvaluateAsync<string>(PageInspectionScripts.PrepareObjectTreeCapture, i).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    FormLog.Debug(ex, "PrepareObjectTreeCapture skipped for a page.");
                    continue;
                }

                string json;
                try
                {
                    json = await p.EvaluateAsync<string>(PageInspectionScripts.BuildObjectTreeJson).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    FormLog.Warn(ex, "BuildObjectTreeJson failed for page {Index}", i);
                    try
                    {
                        await p.EvaluateAsync(PageInspectionScripts.ClearObjectTreeCaptureMarkers).ConfigureAwait(true);
                    }
                    catch
                    {
                        /* ignore */
                    }
                    continue;
                }

                try
                {
                    await p.EvaluateAsync(PageInspectionScripts.ClearObjectTreeCaptureMarkers).ConfigureAwait(true);
                }
                catch
                {
                    /* ignore */
                }

                var pageRoots = JsonConvert.DeserializeObject<List<ObjectTreeNodeDto>>(json) ?? new List<ObjectTreeNodeDto>();
                if (pageRoots.Count == 0)
                    continue;

                var bodyRoot = pageRoots[0];
                if (pages.Count == 1)
                {
                    merged.Add(bodyRoot);
                    continue;
                }

                string title;
                try
                {
                    title = await p.TitleAsync().ConfigureAwait(true) ?? string.Empty;
                }
                catch
                {
                    title = string.Empty;
                }

                var url = p.Url ?? string.Empty;
                var label = $"Window ({i + 1}/{pages.Count}): {TruncateTreeLabel(title, 80)} — {TruncateTreeLabel(url, 120)}";
                var wrap = new ObjectTreeNodeDto
                {
                    Id = "w" + i + "_page",
                    ParentId = null,
                    DisplayName = label,
                    Tag = "BROWSER_PAGE",
                    Role = string.Empty,
                    LocatorHint = string.Empty,
                    InteractiveKind = "container",
                    PageInstanceId = string.Empty,
                    Children = new List<ObjectTreeNodeDto> { bodyRoot }
                };
                bodyRoot.ParentId = wrap.Id;
                merged.Add(wrap);
            }

            return merged;
        }

        private async Task<IPage> ResolvePageForObjectTreeAsync(ObjectTreeNodeDto dto)
        {
            if (dto == null || !_host.IsRunning)
                return _host.Page;
            var stamp = dto.PageInstanceId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(stamp))
                return _host.Page ?? null;

            foreach (var p in GetOrderedPagesForObjectInspect())
            {
                try
                {
                    var v = await p.EvaluateAsync<string>("() => (typeof window.__marsPageInstanceId === 'string' ? window.__marsPageInstanceId : '')").ConfigureAwait(true);
                    if (string.Equals(v, stamp, StringComparison.Ordinal))
                        return p;
                }
                catch
                {
                    /* page may be closing */
                }
            }

            return _host.Page;
        }

        /// <summary>
        /// Rebuilds the object <see cref="TreeView"/> from the current Playwright page DOM snapshot.
        /// </summary>
        private async Task PopulateObjectTreeFromPageAsync(bool showErrorDialog)
        {
            using (WebAutomationMethodTrace.Begin(FormLog, nameof(PopulateObjectTreeFromPageAsync),
                (nameof(showErrorDialog), showErrorDialog)))
            {
                if (!_host.IsRunning)
                {
                    if (showErrorDialog)
                        MessageBox.Show(this, "Start the browser and navigate first.", "Objects", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    SetStatus("Building object tree…");
                    await TryRemovePageHighlightAsync().ConfigureAwait(true);
                    var roots = await BuildMergedObjectTreeRootsAsync().ConfigureAwait(true);
                    treeObjects.BeginUpdate();
                    treeObjects.Nodes.Clear();
                    foreach (var r in roots)
                        AddTreeNodeRecursive(treeObjects.Nodes, null, r);
                    treeObjects.ExpandAll();
                    treeObjects.EndUpdate();
                    _treeSearchMatches.Clear();
                    _treeSearchIndex = -1;
                    SetStatus(roots.Count > 1 ? $"Object tree updated ({roots.Count} windows/pages)." : "Object tree updated.");
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "Populate object tree failed. showErrorDialog={ShowErrorDialog}", showErrorDialog);
                    if (showErrorDialog)
                        MessageBox.Show(this, ex.Message, "Object tree", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        SetStatus("Object tree failed: " + ex.Message);
                }
            }
        }

        private void EnsureObjectTreeImageList()
        {
            if (_objectTreeImageList != null)
                return;
            _objectTreeImageList = new ImageList();
            ObjectTreeIconHelper.PopulateImageList(_objectTreeImageList);
            treeObjects.ImageList = _objectTreeImageList;
        }

        private void AddTreeNodeRecursive(TreeNodeCollection roots, TreeNode parent, ObjectTreeNodeDto node)
        {
            var tn = new TreeNode(node.DisplayName ?? node.Tag) { Tag = node };
            var ix = ObjectTreeIconHelper.GetImageIndex(node);
            tn.ImageIndex = ix;
            tn.SelectedImageIndex = ix;
            if (parent == null)
                roots.Add(tn);
            else
                parent.Nodes.Add(tn);
            if (node.Children == null)
                return;
            foreach (var c in node.Children)
                AddTreeNodeRecursive(roots, tn, c);
        }

        private async void treeObjects_AfterSelect(object sender, TreeViewEventArgs e)
        {
            await ActivateObjectTreeNodeAsync(e.Node).ConfigureAwait(true);
        }

        /// <summary>Re-runs highlight when the same node is clicked again (WinForms does not fire <see cref="TreeView.AfterSelect"/> if selection is unchanged).</summary>
        private async void treeObjects_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.Node == null)
                return;
            if (!ReferenceEquals(treeObjects.SelectedNode, e.Node))
                return;
            await ActivateObjectTreeNodeAsync(e.Node).ConfigureAwait(true);
        }

        /// <summary>Clears the object tree, property grid, and search matches. Optionally removes the in-page highlight overlay.</summary>
        private void ClearObjectInspectUi(bool removePageOverlay)
        {
            treeObjects.BeginUpdate();
            treeObjects.Nodes.Clear();
            treeObjects.EndUpdate();
            gridObjectProps.Rows.Clear();
            _treeSearchMatches.Clear();
            _treeSearchIndex = -1;
            _objectPreview?.ClearImage();
            if (removePageOverlay)
                _ = TryRemovePageHighlightAsync();
        }

        private async Task TryRemovePageHighlightAsync()
        {
            if (!_host.IsRunning)
                return;
            foreach (var p in GetOrderedPagesForObjectInspect())
            {
                try
                {
                    await p.EvaluateAsync(PageInspectionScripts.RemoveObjectHighlight).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    FormLog.Debug(ex, "Remove page highlight failed on one page");
                }
            }
        }

        private async Task ActivateObjectTreeNodeAsync(TreeNode node, bool highlightPage = true)
        {
            if (node == null || !(node.Tag is ObjectTreeNodeDto dto))
                return;
            _objectPreview?.ShowAfterTreeSelection();
            PositionObjectPreview();
            ShowObjectNode(dto);
            try
            {
                if (highlightPage)
                    await TryHighlightObjectOnPageAsync(dto).ConfigureAwait(true);
                if (_objectPreview != null)
                {
                    var capPage = await ResolvePageForObjectTreeAsync(dto).ConfigureAwait(true);
                    await _objectPreview.TryCaptureFromPageAsync(capPage ?? _host.Page, dto).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                FormLog.Debug(ex, "Highlight on page failed for {Locator}", dto.LocatorHint);
            }
        }

        /// <summary>
        /// Scrolls to the object in the live page and draws a transient overlay so testers can correlate the object tree with the DOM (locator preview).
        /// </summary>
        private async Task TryHighlightObjectOnPageAsync(ObjectTreeNodeDto dto)
        {
            if (!_host.IsRunning || _host.Page == null || dto == null)
                return;

            var payload = new Dictionary<string, object> { ["hint"] = dto.LocatorHint ?? string.Empty };
            if (!string.IsNullOrWhiteSpace(dto.Xpath))
                payload["xpath"] = dto.Xpath.Trim();
            if (dto.Bounds != null)
            {
                payload["x"] = dto.Bounds.X;
                payload["y"] = dto.Bounds.Y;
                payload["w"] = dto.Bounds.Width;
                payload["h"] = dto.Bounds.Height;
            }

            var kind = string.Equals(dto.InteractiveKind, "container", StringComparison.OrdinalIgnoreCase)
                ? "container"
                : "interactive";
            payload["kind"] = kind;

            var targetPage = await ResolvePageForObjectTreeAsync(dto).ConfigureAwait(true);
            if (targetPage == null)
                return;
            await targetPage.EvaluateAsync<bool>(PageInspectionScripts.ApplyObjectHighlight, payload).ConfigureAwait(true);
        }

        private void ShowObjectNode(ObjectTreeNodeDto dto)
        {
            gridObjectProps.Rows.Clear();
            var locator = string.IsNullOrWhiteSpace(dto.PlaywrightLocator)
                ? (dto.LocatorHint ?? string.Empty)
                : dto.PlaywrightLocator;

            // User-requested automation attributes (English alphabetical order).
            AddPropRow("Prop.AriaChecked", dto.AriaChecked);
            AddPropRow("Prop.AriaControls", dto.AriaControls);
            AddPropRow("Prop.AriaDescribedby", dto.AriaDescribedby);
            AddPropRow("Prop.AriaExpanded", dto.AriaExpanded);
            AddPropRow("Prop.AriaLabel", dto.AriaLabel);
            AddPropRow("Prop.AriaLabelledby", dto.AriaLabelledby);
            AddPropRow("Prop.AriaSelected", dto.AriaSelected);
            AddPropRow("Prop.Autocomplete", dto.Autocomplete);
            AddPropRow("Prop.DataAttrs", dto.DataAttributes);
            AddPropRow("Prop.Disabled", dto.Disabled);
            AddPropRow("Prop.ForAttr", dto.ForAttr);
            AddPropRow("Prop.Hidden", dto.Hidden);
            AddPropRow("Prop.Href", dto.Href);
            AddPropRow("Prop.HtmlId", dto.HtmlId);
            AddPropRow("Prop.Locator", locator);
            AddPropRow("Prop.Pattern", dto.Pattern);
            AddPropRow("Prop.Placeholder", dto.Placeholder);
            AddPropRow("Prop.Readonly", dto.Readonly);
            AddPropRow("Prop.Required", dto.Required);
            AddPropRow("Prop.TabIndex", dto.TabIndexStr);
            AddPropRow("Prop.Type", dto.InputType);
            AddPropRow("Prop.Value", dto.Value);
            AddPropRow("Prop.Xpath", dto.Xpath);

            AddPropRow("Prop.Tag", dto.Tag);
            AddPropRow("Prop.Id", dto.Id);
            AddPropRow("Prop.Role", dto.Role);
            AddPropRow("Prop.InteractiveKind", dto.InteractiveKind);
            AddPropRow("Prop.ClassName", dto.ClassName);
            AddPropRow("Prop.NameAttr", dto.NameAttr);
            AddPropRow("Prop.Title", dto.Title);
            AddPropRow("Prop.LocatorHint", dto.LocatorHint);
            if (!string.IsNullOrWhiteSpace(dto.PageInstanceId))
                AddPropRow("Prop.PageInstanceId", dto.PageInstanceId);
            AddPropRow("Prop.TextPreview", dto.TextPreview);
            AddPropRow("Prop.ContentEditable", dto.ContentEditable);
            if (dto.Bounds != null)
            {
                AddPropRow("Prop.X", dto.Bounds.X.ToString("0.##"));
                AddPropRow("Prop.Y", dto.Bounds.Y.ToString("0.##"));
                AddPropRow("Prop.W", dto.Bounds.Width.ToString("0.##"));
                AddPropRow("Prop.H", dto.Bounds.Height.ToString("0.##"));
            }

            AddPropRowOuterHtmlLast("Prop.OuterHtml", dto.OuterHtml);
        }

        private void AddPropRow(string nameKey, string value)
        {
            var label = nameKey.StartsWith("Prop.", StringComparison.Ordinal) ? L(nameKey) : nameKey;
            gridObjectProps.Rows.Add(label, value ?? string.Empty);
        }

        private void AddPropRowOuterHtmlLast(string nameKey, string value)
        {
            var label = nameKey.StartsWith("Prop.", StringComparison.Ordinal) ? L(nameKey) : nameKey;
            var rowIndex = gridObjectProps.Rows.Add(label, value ?? string.Empty);
            var row = gridObjectProps.Rows[rowIndex];
            var bg = Color.FromArgb(234, 252, 255);
            row.DefaultCellStyle.BackColor = bg;
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(198, 234, 246);
            row.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            try
            {
                var w = Math.Max(120, gridObjectProps.ClientSize.Width - gridObjectProps.RowHeadersWidth - 40);
                var sz = TextRenderer.MeasureText(
                    value ?? string.Empty,
                    gridObjectProps.Font,
                    new Size(w, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                row.Height = Math.Min(400, Math.Max(56, sz.Height + 14));
            }
            catch
            {
                row.Height = 72;
            }
        }

        private void ShowNodeDetails(SemanticStepRecord step)
        {
            gridObjectProps.Rows.Clear();
            AddRow("Keyword", step.Keyword);
            AddRow("Logical kind", step.LogicalKind);
            if (!string.IsNullOrWhiteSpace(step.RecordedPageUrl))
                AddRow("Recorded page URL", step.RecordedPageUrl);
            if (!string.IsNullOrWhiteSpace(step.RecordedPageTitle))
                AddRow("Recorded page title", step.RecordedPageTitle);
            AddRow("Locator", step.Locator);
            if (!string.IsNullOrWhiteSpace(step.ElementXpath))
                AddRow("XPath", step.ElementXpath);
            if (!string.IsNullOrWhiteSpace(step.LocatorAlternates))
                AddRow("Alt locators", step.LocatorAlternates);
            AddRow("Parameter", step.Parameter);
            AddRow("Data", step.Data);
            if (!string.IsNullOrWhiteSpace(step.TargetTag))
                AddRow("Target tag", step.TargetTag);
            if (!string.IsNullOrWhiteSpace(step.TargetRole))
                AddRow("Target role", step.TargetRole);
            if (!string.IsNullOrWhiteSpace(step.TargetLocator))
                AddRow("Target locator", step.TargetLocator);
            if (!string.IsNullOrWhiteSpace(step.TargetXpath))
                AddRow("Target xpath", step.TargetXpath);
            AddRow("Source event", step.SourceEvent);
            AddRow("Bounds", step.BoundsDisplay);
            if (step.BoundingRect != null)
            {
                AddRow("X", step.BoundingRect.X.ToString("0.##"));
                AddRow("Y", step.BoundingRect.Y.ToString("0.##"));
                AddRow("W", step.BoundingRect.Width.ToString("0.##"));
                AddRow("H", step.BoundingRect.Height.ToString("0.##"));
            }
        }

        private void AddRow(string name, string value)
        {
            gridObjectProps.Rows.Add(name, value ?? string.Empty);
        }

        private async void tsbTarget_Click(object sender, EventArgs e)
        {
            if (!_host.IsRunning)
            {
                tsbTarget.Checked = false;
                MessageBox.Show(this, "Start the browser first.", "Target", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (tsbTarget.Checked)
            {
                tsbRecord.Checked = false;
                await EnsureRecordingInstalledAsync().ConfigureAwait(true);
                await SetRecorderModeAsync("pick").ConfigureAwait(true);
                SetStatus("Target pick: click an element in the page.");
            }
            else
                await SetRecorderModeAsync("off").ConfigureAwait(true);
            UpdateRecorderModeFromUiStateAsync();
        }

        private async void tsbRecord_Click(object sender, EventArgs e)
        {
            if (!_host.IsRunning)
            {
                tsbRecord.Checked = false;
                MessageBox.Show(this, "Start the browser first.", "Record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (tsbRecord.Checked)
            {
                tsbTarget.Checked = false;
                await EnsureRecordingInstalledAsync().ConfigureAwait(true);
                await SetRecorderModeAsync("record").ConfigureAwait(true);
                EnsureRecordReplaySidebar();
                SetStatus("Recording…");
            }
            else
            {
                await SetRecorderModeAsync("off").ConfigureAwait(true);
                SetStatus("Record stopped.");
            }
            UpdateRecorderModeFromUiStateAsync();
        }

        private async Task SetRecorderModeAsync(string mode)
        {
            if (!TryEnsureRecorderPageAlive())
                return;
            try
            {
                await _recording.SetModeAsync(_host.Page, mode, _settings).ConfigureAwait(true);
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                HandleRecorderTargetClosed(ex);
            }
        }

        private async Task EnsureRecordingInstalledAsync()
        {
            if (!TryEnsureRecorderPageAlive())
                return;
            try
            {
                await _recording.InstallAsync(_host.Page, _settings).ConfigureAwait(true);
                ShowEngineLoadStatus();
            }
            catch (Exception ex) when (IsTargetClosed(ex))
            {
                HandleRecorderTargetClosed(ex);
            }
        }

        private bool TryEnsureRecorderPageAlive()
        {
            if (!_host.IsRunning || _host.Page == null || _host.Page.IsClosed)
            {
                SetStatus("Recorder page is closed. Please start browser again.");
                tsbRecord.Checked = false;
                tsbTarget.Checked = false;
                return false;
            }
            return true;
        }

        private void HandleRecorderTargetClosed(Exception ex)
        {
            FormLog.Info(ex, "Recorder action ignored because target page/context/browser is closed.");
            SetStatus("Recorder page was closed due to inactivity. Please start browser again.");
            tsbRecord.Checked = false;
            tsbTarget.Checked = false;
        }

        private static bool IsTargetClosed(Exception ex)
        {
            if (ex == null)
                return false;
            var msg = ex.Message ?? string.Empty;
            return msg.IndexOf("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("Target closed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async void tsbReloadEngine_Click(object sender, EventArgs e)
        {
            if (_host.Page == null)
            {
                SetStatus("No page to reload engine.");
                return;
            }
            try
            {
                await _recording.ReloadEngineAsync(_host.Page, _settings).ConfigureAwait(true);
                ShowEngineLoadStatus(prefix: "Recorder engine reloaded");
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "Reload recorder engine failed.");
                MessageBox.Show(this, ex.Message, "Reload recorder engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Recorder engine reload failed.");
            }
        }

        private void ShowEngineLoadStatus(string prefix = "Recorder engine loaded")
        {
            var path = _recording.CurrentEngineScriptPath;
            var t = _recording.LastEngineInjectedUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
            if (string.IsNullOrWhiteSpace(path))
            {
                SetStatus(prefix + ".");
                return;
            }
            SetStatus($"{prefix}: {path} @ {t}");
        }

        private void EnsureRecordReplaySidebar()
        {
            if (_recordReplaySidebar == null || _recordReplaySidebar.IsDisposed)
            {
                _recordReplaySidebar = new RecordReplaySidebarForm();
                _recordReplaySidebar.Show(this);
                _recordReplaySidebar.StartCapture();
            }
            else if (!_recordReplaySidebar.Visible)
            {
                _recordReplaySidebar.Show(this);
            }
            _recordReplaySidebar.Left = 0;
            _recordReplaySidebar.Top = 0;
            _recordReplaySidebar.Width = 300;
            _recordReplaySidebar.Height = Screen.PrimaryScreen.Bounds.Height;
            _recordReplaySidebar.BringToFront();
        }

        private void PushStepToSidebar(SemanticStepRecord step)
        {
            if (_recordReplaySidebar == null || _recordReplaySidebar.IsDisposed || step == null) return;
            var latestReq = _network.Entries.LastOrDefault();
            var reqHeaders = latestReq?.RequestHeaders == null
                ? string.Empty
                : string.Join("; ", latestReq.RequestHeaders.Select(kv => kv.Key + "=" + kv.Value));
            var response = latestReq?.Status?.ToString() ?? string.Empty;
            var position = step.BoundingRect == null
                ? string.Empty
                : $"{step.BoundingRect.X:0.#},{step.BoundingRect.Y:0.#}";

            _recordReplaySidebar.AddRecordCard(new RecordReplayEventCard
            {
                EventName = string.IsNullOrWhiteSpace(step.SourceEvent) ? step.Keyword : step.SourceEvent,
                Position = position,
                ObjectType = step.Keyword,
                Tag = string.Empty,
                DataAttributes = string.Empty,
                Xpath = string.Empty,
                Value = step.Data,
                Id = string.Empty,
                AriaAttributes = string.Empty,
                Data = step.Data,
                ListenedRequestUrl = latestReq?.Url,
                ListenedRequestHeaders = reqHeaders,
                ExpectedResponse = response
            });
        }

        private async void tsbReplay_Click(object sender, EventArgs e)
        {
            if (!_host.IsRunning)
            {
                MessageBox.Show(this, "Start the browser first.", "Replay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_steps.Count == 0)
            {
                MessageBox.Show(this, "No steps to replay.", "Replay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                SetStatus("Replaying…");
                await _replay.ReplayAsync(_host.Page, _steps.ToList(), 200).ConfigureAwait(true);
                SetStatus("Replay finished.");
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "Replay failed.");
                MessageBox.Show(this, ex.Message, "Replay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveInternalAsync()
        {
            using (WebAutomationMethodTrace.Begin(FormLog, nameof(SaveInternalAsync),
                ("url", txtUrl.Text?.Trim()),
                ("steps", _steps.Count)))
            {
                ReadSettingsFromUi();
                if (!Uri.TryCreate(txtUrl.Text.Trim(), UriKind.Absolute, out var uri))
                {
                    MessageBox.Show(this, "Enter a valid absolute URL before saving.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var path = DataPathHelper.BuildJsonPath(_settings.DataRootFolder, uri);
                var title = _host.IsRunning && _host.Page != null
                    ? await _host.Page.TitleAsync().ConfigureAwait(true)
                    : string.Empty;
                var norm = DataPathHelper.SanitizeUrlToFileKey(uri);
                var doc = new WebTestDocument
                {
                    PageInfo = PageInfoDto.FromUri(uri, title, norm),
                    Steps = _steps.ToList(),
                    NetworkCaptures = _network.Entries.ToList()
                };

                var cookieSummary = _host.Context != null
                    ? await NetworkCaptureService.BuildCookiesSummaryAsync(_host.Context).ConfigureAwait(true)
                    : string.Empty;
                if (!string.IsNullOrEmpty(cookieSummary) && doc.NetworkCaptures.Count > 0)
                    doc.NetworkCaptures[doc.NetworkCaptures.Count - 1].CookiesSummary = cookieSummary;

                doc.SettingsSnapshot["Headless"] = _settings.Headless.ToString();
                doc.SettingsSnapshot["DataRoot"] = _settings.DataRootFolder;

                _store.Save(path, doc);
                SetStatus("Saved: " + path);
            }
        }

        private async void tsbSave_Click(object sender, EventArgs e)
        {
            try
            {
                await SaveInternalAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "Save failed.");
                MessageBox.Show(this, ex.Message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuFileSave_Click(object sender, EventArgs e) => tsbSave_Click(sender, e);
        private void menuFileExport_Click(object sender, EventArgs e) => tsbExport_Click(sender, e);
        private void menuFileImport_Click(object sender, EventArgs e) => tsbImport_Click(sender, e);

        private async void tsbExport_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "webtest.json" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    ReadSettingsFromUi();
                    if (!Uri.TryCreate(txtUrl.Text.Trim(), UriKind.Absolute, out var uri))
                        uri = new Uri("https://example.com/");
                    var title = _host.IsRunning && _host.Page != null
                        ? await _host.Page.TitleAsync().ConfigureAwait(true)
                        : string.Empty;
                    var doc = new WebTestDocument
                    {
                        PageInfo = PageInfoDto.FromUri(uri, title, DataPathHelper.SanitizeUrlToFileKey(uri)),
                        Steps = _steps.ToList(),
                        NetworkCaptures = _network.Entries.ToList()
                    };
                    _store.Save(dlg.FileName, doc);
                    SetStatus("Exported.");
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "Export failed.");
                    MessageBox.Show(this, ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void tsbImport_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "JSON|*.json" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    var doc = _store.Load(dlg.FileName);
                    _suppressStepsListEvents = true;
                    try
                    {
                        while (_steps.Count > 0)
                            _steps.RemoveAt(_steps.Count - 1);
                        foreach (var s in doc.Steps ?? Enumerable.Empty<SemanticStepRecord>())
                            _steps.Add(s);
                    }
                    finally
                    {
                        _suppressStepsListEvents = false;
                    }

                    RenumberStepMetadata();
                    RefreshRecordReplayCanvas();
                    if (doc.PageInfo != null && !string.IsNullOrEmpty(doc.PageInfo.OriginalUrl))
                        txtUrl.Text = doc.PageInfo.OriginalUrl;
                    UpdateUriLabels();
                    SetStatus("Imported " + _steps.Count + " steps.");
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "Import failed.");
                    MessageBox.Show(this, ex.Message, "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void menuFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuHelpAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "MARS.WebAutomation — Playwright record/replay workbench.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            ReadSettingsFromUi();
            _settingsStore.Save(_settings);
            RegisterRecordReplayHotkey();
            SetStatus("Settings saved.");
        }

        private void MainWorkbenchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterRecordReplayHotkey();
            splitObjects.Panel2.Resize -= SplitObjects_Panel2_Resize;
            _host.ActiveDocumentUrlChanged -= Host_ActiveDocumentUrlChanged;
            _network.Dispose();
            _steps.ListChanged -= Steps_ListChanged;
            gridSteps.CellContentClick -= gridSteps_CellContentClick;
            gridSteps.CellDoubleClick -= gridSteps_CellDoubleClick;
            gridSteps.CellEndEdit -= gridSteps_CellEndEdit;
            tabRecord.SizeChanged -= TabRecord_SizeChanged;
            _recording.RecordedStep -= Recording_RecordedStep;
            _recording.Picked -= Recording_Picked;
            try
            {
                if (_host.IsRunning && _host.Page != null)
                    _host.Page.EvaluateAsync(PageInspectionScripts.RemoveObjectHighlight).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // ignore
                FormLog.Warn(ex, "Failed to remove page highlight during form close.");
            }

            try
            {
                _host.ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // ignore shutdown errors
                FormLog.Warn(ex, "Host shutdown failed during form close.");
            }

            if (_recordReplaySidebar != null)
            {
                try { _recordReplaySidebar.Close(); } catch (Exception ex) { FormLog.Warn(ex, "Failed to close record-replay sidebar window."); }
                _recordReplaySidebar = null;
            }

            ResetSingleton(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) == Keys.Control)
            {
                var key = keyData & Keys.KeyCode;
                if (key == Keys.D0 || key == Keys.NumPad0)
                {
                    SetCanvasZoom(100, centerAfter: true);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == RecordReplayHotkeyId)
            {
                if (!tsbRecord.Checked)
                {
                    tsbRecord.Checked = true;
                    tsbRecord_Click(tsbRecord, EventArgs.Empty);
                    tabMain.SelectedTab = tabRecord;
                }
                else
                {
                    tsbRecord.Checked = false;
                    tsbRecord_Click(tsbRecord, EventArgs.Empty);
                }
            }
            base.WndProc(ref m);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
