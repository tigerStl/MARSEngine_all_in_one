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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Playwright;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MARS.WebAutomation;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface;
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
        private readonly PerformancePackStore _perfPackStore = new PerformancePackStore();
        private readonly WorkbenchSettingsStore _settingsStore = new WorkbenchSettingsStore();
        private WorkbenchSettings _settings;
        private readonly BindingList<SemanticStepRecord> _steps = new BindingList<SemanticStepRecord>();
        private readonly BindingList<PerformanceRequestRecord> _performanceRequests = new BindingList<PerformanceRequestRecord>();
        private RecordReplaySidebarForm _recordReplaySidebar;
        private ImageList _objectTreeImageList;
        private Bitmap _gridActDeleteIcon;
        private Bitmap _gridActHighlightIcon;
        private Bitmap _gridActTestIcon;
        private bool _syncTreeFromPickInProgress;
        private SplitContainer _recordSplit;
        private WebView2 _recordWebView;
        private bool _recordWebViewReady;
        private bool _recordWorkflowUsesBundle;
        private bool _pendingWorkflowStepsPush;
        private const string RecordWorkflowVirtualHost = "mars.workflow";
        private const string RecordWorkflowStartUrl = "https://mars.workflow/index.html";
        private bool _recordSplitDistanceInitialized;
        private bool _stepsMasterDetailSplitDistanceInitialized;
        private SplitContainer _perfAnchorRuntimeSplit;
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
        private Label _lblPerformanceFilterTokens;
        private TextBox _txtPerformanceFilterTokens;
        private ToolStripLabel _tslPerfUsers;
        private ToolStripComboBox _tscbPerfUsers;
        private ToolStripMenuItem _menuBrandTitle;
        private ToolStripMenuItem _menuPerformance;
        private ToolStripMenuItem _menuWithPerformanceTest;
        private ToolStripMenuItem _menuPerfUsers;
        private ToolStripMenuItem _menuPerfUsers5;
        private ToolStripMenuItem _menuPerfUsers10;
        private ToolStripMenuItem _menuPerfUsers100;
        private ToolStripMenuItem _menuPerfConfigTransactions;
        private ToolStripMenuItem _menuPerfRunNow;
        private ToolStripMenuItem _menuPerfRunSelectedAnchor;
        private ToolStripMenuItem _menuPerfExportPack;
        private ToolStripMenuItem _menuPerfImportPack;
        private ContextMenuStrip _perfGridMenu;
        private bool _syncingPerfMenuState;
        private ToolStrip _gridToolStrip;
        private ToolStripButton _btnGridInsert;
        private ToolStripButton _btnGridDelete;
        private ToolStripButton _btnGridReplay;
        private SplitContainer _stepsMasterDetailSplit;
        private DataGridView _gridPerformance;
        private DataGridView _gridPerfRuntime;
        private Label _lblPerformanceAnchors;
        private Label _lblPerformanceAnchorSummary;
        private Label _lblPerformanceRuntime;
        private readonly BindingList<PerfTransactionRuntimeRow> _perfRuntimeRows = new BindingList<PerfTransactionRuntimeRow>();
        private readonly HashSet<string> _performanceFilterTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _performanceFilterTokensFromSettings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _performanceIgnoreExactUrlsFromSettings = new List<string>();
        private readonly List<string> _performanceIgnoreWildcardUrlsFromSettings = new List<string>();
        private readonly Color _stepPerfLightBrown = Color.FromArgb(237, 224, 206);
        private readonly INBomberExecuteAdapter _perfExecuteAdapter = new NBomberExecuteAdapter();
        private readonly Dictionary<string, TransactionConfigRow> _transactionConfigByName = new Dictionary<string, TransactionConfigRow>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PerfTransactionRuntimeRow> _runtimeByTransaction = new Dictionary<string, PerfTransactionRuntimeRow>(StringComparer.OrdinalIgnoreCase);
        private DateTime _perfRunStartedUtc;
        private int _perfDurationSeconds = 60;
        private bool _performanceRunInProgress;
        private CancellationTokenSource _perfRunCts;
        private int _perfExpectedRounds;
        private int _perfCompletedRounds;
        private int _perfCurrentRound;
        private PerformanceMetricsCollector _latestPerformanceMetrics;
        private SemanticStepRecord _lastRecordedUiStep;
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
            gridPerfAnchorPreview.Dock = DockStyle.Fill;
            lblPerfDesignAnchorSummary.Dock = DockStyle.Top;
            gridPerfAnchorPreview.BringToFront();
            splitRecordPerfPreview.SplitterDistance = 160;
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
            _network.EntryCompleted += Network_EntryCompleted;
            _host.ActiveDocumentUrlChanged += Host_ActiveDocumentUrlChanged;
            SetupObjectPreviewAndToolbar();
            SetupReloadEngineToolbar();
            SetupPerformanceToolbarCheckbox();
            SetupMenuBrandTitle();
            SetupMenuPerformanceOptions();
            treeObjects.NodeMouseClick += treeObjects_NodeMouseClick;
            InitRecordReplayTabUi();
            ConfigureStepsGridColumns();
            _steps.ListChanged += Steps_ListChanged;
            gridSteps.CellContentClick += gridSteps_CellContentClick;
            gridSteps.CellMouseClick += gridSteps_CellMouseClick;
            gridSteps.CellPainting += gridSteps_CellPainting;
            gridSteps.CellDoubleClick += gridSteps_CellDoubleClick;
            gridSteps.CellEndEdit += gridSteps_CellEndEdit;
            gridSteps.CellFormatting += gridSteps_CellFormatting;
            gridSteps.SelectionChanged += gridSteps_SelectionChanged;
            gridSteps.RowPrePaint += gridSteps_RowPrePaint;
            gridSteps.DataError += Grid_DataError;
            tabRecord.SizeChanged += TabRecord_SizeChanged;
            InitHotkeySettingsUi();
        }

        private static bool IsLikelyUiActionStep(SemanticStepRecord step)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.Keyword))
                return false;
            return !string.Equals(step.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase);
        }

        private static string InferPerformanceFilterTag(PerformanceRequestRecord req)
        {
            var url = req?.Url ?? string.Empty;
            if (url.IndexOf("heartbeat", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("/ping", StringComparison.OrdinalIgnoreCase) >= 0)
                return "heartbeat";
            if (url.IndexOf("handshake", StringComparison.OrdinalIgnoreCase) >= 0)
                return "handshake";
            return "normal";
        }

        private static string BuildPerformanceReplayPolicy(PerformanceRequestRecord req)
        {
            var tag = req?.FilterTag ?? string.Empty;
            var type = req?.ResourceType ?? string.Empty;
            if (string.Equals(tag, "heartbeat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tag, "handshake", StringComparison.OrdinalIgnoreCase))
                return "Ignore/Filter";
            if (string.Equals(type, "xhr", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "fetch", StringComparison.OrdinalIgnoreCase))
                return "Keep+Replay";
            if (string.Equals(type, "document", StringComparison.OrdinalIgnoreCase))
                return "Keep+Flow";
            return "Keep+Validate";
        }

        private static string BuildPerformanceValidationHint(PerformanceRequestRecord req)
        {
            var type = req?.ResourceType ?? string.Empty;
            if (string.Equals(type, "document", StringComparison.OrdinalIgnoreCase))
                return "validate status/url/title";
            if (string.Equals(type, "xhr", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "fetch", StringComparison.OrdinalIgnoreCase))
                return "validate status/body-key";
            return "validate status";
        }

        private static int ComputeAnchorScore(PerformanceRequestRecord req)
        {
            if (req == null)
                return 0;
            var score = 0;
            var method = req.Method ?? string.Empty;
            var type = req.ResourceType ?? string.Empty;
            var url = req.Url ?? string.Empty;
            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
                score += 5;
            if (string.Equals(type, "xhr", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "fetch", StringComparison.OrdinalIgnoreCase))
                score += 4;
            if (string.Equals(type, "document", StringComparison.OrdinalIgnoreCase))
                score += 3;
            if (url.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("create", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("submit", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("/api/", StringComparison.OrdinalIgnoreCase) >= 0)
                score += 3;
            if (req.Status.HasValue && req.Status.Value >= 200 && req.Status.Value < 400)
                score += 1;
            if (string.Equals(req.FilterTag, "heartbeat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(req.FilterTag, "handshake", StringComparison.OrdinalIgnoreCase))
                score -= 6;
            if (string.Equals(type, "stylesheet", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "image", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "font", StringComparison.OrdinalIgnoreCase))
                score -= 10;
            return score;
        }

        private static string InferAnchorGroup(PerformanceRequestRecord req)
        {
            var url = req?.Url ?? string.Empty;
            if (url.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("auth", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Login";
            if (url.IndexOf("create", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("application", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CreateApplication";
            return "General";
        }

        private static bool NeedsCorrelation(PerformanceRequestRecord req)
        {
            var h = req?.Headers ?? string.Empty;
            var p = req?.Payload ?? string.Empty;
            var u = req?.Url ?? string.Empty;
            return h.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0
                   || h.IndexOf("cookie", StringComparison.OrdinalIgnoreCase) >= 0
                   || p.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0
                   || p.IndexOf("__RequestVerificationToken", StringComparison.OrdinalIgnoreCase) >= 0
                   || u.IndexOf("state=", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildPerformanceParameterFromUrl(string rawUrl)
        {
            if (!Uri.TryCreate(rawUrl ?? string.Empty, UriKind.Absolute, out var uri))
                return string.Empty;
            var q = uri.Query?.TrimStart('?') ?? string.Empty;
            return string.IsNullOrWhiteSpace(q) ? "(none)" : q;
        }

        private static string NormalizePerformanceFilterTokens(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "heartbeat;handshake" : raw.Trim();
        }

        private static string NormalizePerformanceIgnoreUrlPatterns(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
        }

        private static bool IsWildcardUrlPatternMatch(string url, string pattern)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pattern))
                return false;
            if (string.Equals(url, pattern, StringComparison.OrdinalIgnoreCase))
                return true;
            var rx = "^" + Regex.Escape(pattern.Trim())
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            return Regex.IsMatch(url, rx, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
            // Reuse real designer controls so layout is visible/editable in WinForms designer.
            _recordSplit = splitRecordMainPreview;
            _stepsMasterDetailSplit = splitRecordWorkPreview;
            _perfAnchorRuntimeSplit = splitRecordPerfPreview;
            _gridPerformance = gridPerfAnchorPreview;
            _gridPerfRuntime = gridPerfRuntimePreview;
            _lblPerformanceAnchors = lblPerfDesignTitle;
            _lblPerformanceAnchorSummary = lblPerfDesignAnchorSummary;
            _lblPerformanceRuntime = lblPerfDesignRuntime;

            _recordSplit.Dock = DockStyle.Fill;
            _recordSplit.Orientation = Orientation.Vertical;
            _recordSplit.BorderStyle = BorderStyle.FixedSingle;
            _recordSplit.SplitterWidth = 6;
            _recordSplit.Panel1MinSize = 260;
            _recordSplit.Panel2MinSize = 160;

            _stepsMasterDetailSplit.Dock = DockStyle.Fill;
            _stepsMasterDetailSplit.Orientation = Orientation.Horizontal;
            _stepsMasterDetailSplit.BorderStyle = BorderStyle.None;
            _stepsMasterDetailSplit.SplitterWidth = 6;
            _stepsMasterDetailSplit.Panel1MinSize = 120;
            _stepsMasterDetailSplit.Panel2MinSize = 90;

            _perfAnchorRuntimeSplit.Dock = DockStyle.Fill;
            _perfAnchorRuntimeSplit.Orientation = Orientation.Horizontal;
            _perfAnchorRuntimeSplit.SplitterWidth = 6;
            _perfAnchorRuntimeSplit.Panel1MinSize = 100;
            _perfAnchorRuntimeSplit.Panel2MinSize = 168;
            _perfAnchorRuntimeSplit.FixedPanel = FixedPanel.Panel2;

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

            var lblVisualization = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Visualization",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            gridSteps.Dock = DockStyle.Fill;
            panel1.Controls.Clear();
            panel1.Controls.Add(gridSteps);
            panel1.Controls.Add(_gridToolStrip);
            panel1.Controls.Add(lblVisualization);

            _gridPerformance.AllowUserToAddRows = false;
            _gridPerformance.AllowUserToDeleteRows = false;
            _gridPerformance.ReadOnly = true;
            _gridPerformance.AutoGenerateColumns = false;
            _gridPerformance.RowHeadersVisible = false;
            _gridPerformance.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ConfigurePerformanceGridColumns();
            _gridPerformance.CellContentClick += gridPerformance_CellContentClick;
            _gridPerformance.CellDoubleClick += gridPerformance_CellDoubleClick;
            _gridPerformance.RowPrePaint += gridPerformance_RowPrePaint;
            _gridPerformance.DataError += Grid_DataError;
            _gridPerformance.MouseDown += gridPerformance_MouseDown;
            _perfGridMenu = new ContextMenuStrip();
            _perfGridMenu.Items.Add("Ignore…", null, (_, __) => IgnoreSelectedPerformanceRows());
            _perfGridMenu.Items.Add(new ToolStripSeparator());
            _perfGridMenu.Items.Add("Export Performance Pack…", null, (_, __) => ExportPerformancePack());
            _perfGridMenu.Items.Add("Import Performance Pack…", null, (_, __) => ImportPerformancePack());
            _gridPerformance.ContextMenuStrip = _perfGridMenu;

            _lblPerformanceAnchors.Dock = DockStyle.Top;
            _lblPerformanceAnchors.Height = 24;
            _lblPerformanceAnchors.Text = "Perform Test anchors";
            _lblPerformanceAnchors.TextAlign = ContentAlignment.MiddleLeft;
            _lblPerformanceAnchors.Padding = new Padding(6, 0, 0, 0);
            _lblPerformanceAnchors.BackColor = Color.FromArgb(248, 250, 252);

            _lblPerformanceAnchorSummary.Dock = DockStyle.Top;
            _lblPerformanceAnchorSummary.Height = 22;
            _lblPerformanceAnchorSummary.Text = "Anchor groups: (none)";
            _lblPerformanceAnchorSummary.TextAlign = ContentAlignment.MiddleLeft;
            _lblPerformanceAnchorSummary.Padding = new Padding(10, 0, 0, 0);
            _lblPerformanceAnchorSummary.BackColor = Color.FromArgb(241, 245, 249);
            _lblPerformanceAnchorSummary.ForeColor = Color.FromArgb(71, 85, 105);

            _lblPerformanceRuntime.Dock = DockStyle.Top;
            _lblPerformanceRuntime.Height = 20;
            _lblPerformanceRuntime.Text = "Runtime progress (throughput/error rate)";
            _lblPerformanceRuntime.TextAlign = ContentAlignment.MiddleLeft;
            _lblPerformanceRuntime.Padding = new Padding(10, 0, 0, 0);
            _lblPerformanceRuntime.BackColor = Color.FromArgb(241, 245, 249);
            _lblPerformanceRuntime.ForeColor = Color.FromArgb(71, 85, 105);

            _gridPerfRuntime.AllowUserToAddRows = false;
            _gridPerfRuntime.AllowUserToDeleteRows = false;
            _gridPerfRuntime.ReadOnly = true;
            _gridPerfRuntime.AutoGenerateColumns = false;
            _gridPerfRuntime.RowHeadersVisible = false;
            _gridPerfRuntime.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridPerfRuntime.DataSource = _perfRuntimeRows;
            _gridPerfRuntime.DataError += Grid_DataError;
            _gridPerfRuntime.CellDoubleClick += gridPerfRuntime_CellDoubleClick;

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

            var panelCanvasHost = panelRecordCanvasPreview;
            panelCanvasHost.Controls.Clear();
            panelCanvasHost.Controls.Add(_recordWebView);
            // Keep zoom actions callable via shortcuts/messages, but hide the extra top toolbar row in visual canvas.
            _recordCanvasToolStrip.Visible = false;
            _recordSplit.Panel2.Controls.Add(panelCanvasHost);
            ApplyRecordCanvasToolbarLocalization();

            lblRecordHint.Dock = DockStyle.Top;

            tabMain.SelectedIndexChanged += (_, __) =>
            {
                if (tabMain.SelectedTab == tabRecord)
                    EnsureRecordReplaySidebar();
                UpdateRecorderModeFromUiStateAsync();
            };
            ApplyPerformancePanelVisibility();
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

            _lblPerformanceFilterTokens = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Performance filter tokens",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _txtPerformanceFilterTokens = new TextBox
            {
                Dock = DockStyle.Fill
            };
            layoutSettings.RowCount += 1;
            layoutSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutSettings.Controls.Add(_lblPerformanceFilterTokens, 0, layoutSettings.RowCount - 1);
            layoutSettings.Controls.Add(_txtPerformanceFilterTokens, 1, layoutSettings.RowCount - 1);
        }

        private void ConfigureStepsGridColumns()
        {
            gridSteps.AutoGenerateColumns = false;
            if (gridSteps.Columns.Count == 0)
                return;
            gridSteps.ColumnHeadersVisible = true;
            gridSteps.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            gridSteps.ColumnHeadersHeight = 30;
            if (gridSteps.Columns.Contains("colAct"))
                gridSteps.Columns["colAct"].DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            if (gridSteps.Columns.Contains("colElapsed"))
                gridSteps.Columns["colElapsed"].DefaultCellStyle.Format = "N0";
            gridSteps.ReadOnly = false;
            foreach (DataGridViewColumn c in gridSteps.Columns)
            {
                if (string.Equals(c.Name, "colAct", StringComparison.Ordinal)
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

        private void ConfigurePerformanceGridColumns()
        {
            if (_gridPerformance == null)
                return;
            if (_gridPerformance.Columns.Count == 0)
                FormLog.Warn("Performance grid has no designer columns; using fallback behavior.");
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            var gridName = sender == _gridPerformance ? "performanceGrid" : "stepsGrid";
            FormLog.Warn("DataGridView DataError suppressed. grid={Grid} row={Row} col={Col} ctx={Ctx}", gridName, e.RowIndex, e.ColumnIndex, e.Context);
            SetStatus("Grid value format warning (suppressed).");
        }

        private void gridSteps_SelectionChanged(object sender, EventArgs e)
        {
            BindPerformanceForSelectedStep();
        }

        private void gridSteps_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _steps.Count)
                return;
            var step = _steps[e.RowIndex];
            if (step?.PerformanceRequestRefs == null || step.PerformanceRequestRefs.Count == 0)
            {
                gridSteps.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                return;
            }
            gridSteps.Rows[e.RowIndex].DefaultCellStyle.BackColor = _stepPerfLightBrown;
        }

        private void BindPerformanceForSelectedStep()
        {
            if (_gridPerformance == null)
                return;
            if (!IsPerformanceTestEnabled())
            {
                _gridPerformance.DataSource = new BindingList<PerformanceRequestRecord>();
                UpdatePerformanceAnchorSummary(new List<PerformanceRequestRecord>());
                return;
            }
            BindingList<PerformanceRequestRecord> BuildAllVisible()
            {
                var allRows = _performanceRequests
                    .Where(p => p != null && !p.IsFiltered && !ShouldHidePerformanceBySettings(p))
                    .ToList();
                return new BindingList<PerformanceRequestRecord>(allRows);
            }
            var step = GetSelectedStepOrNull();
            if (step == null)
            {
                _gridPerformance.DataSource = BuildAllVisible();
                return;
            }
            if (step.PerformanceRequestRefs == null || step.PerformanceRequestRefs.Count == 0)
            {
                // If selected step has no links yet, still show visible captures for troubleshooting.
                _gridPerformance.DataSource = BuildAllVisible();
                return;
            }

            var candidates = _performanceRequests
                .Where(p => p != null
                            && step.PerformanceRequestRefs.Contains(p.Id))
                .ToList();
            var rows = candidates
                .Where(p => !p.IsFiltered
                            && !_performanceFilterTags.Contains(p.FilterTag ?? string.Empty)
                            && !ShouldHidePerformanceBySettings(p))
                .ToList();
            _gridPerformance.DataSource = new BindingList<PerformanceRequestRecord>(rows);
            UpdatePerformanceAnchorSummary(rows);
            if (rows.Count == 0 && candidates.Count > 0)
                SetStatus("Performance rows captured but filtered by current settings/tags.");
        }

        private void UpdatePerformanceAnchorSummary(IReadOnlyCollection<PerformanceRequestRecord> rows)
        {
            if (_lblPerformanceAnchorSummary == null)
                return;
            if (rows == null || rows.Count == 0)
            {
                _lblPerformanceAnchorSummary.Text = "Anchor groups: (none)";
                return;
            }
            var selected = rows.Where(r => r != null && r.IsAnchorSelected).ToList();
            if (selected.Count == 0)
            {
                _lblPerformanceAnchorSummary.Text = "Anchor groups: 0 selected | Sim users: " + GetSelectedPerfUsersCount();
                return;
            }
            var groups = selected
                .GroupBy(r => string.IsNullOrWhiteSpace(r.AnchorGroup) ? "General" : r.AnchorGroup)
                .Select(g => $"{g.Key}({g.Count()})")
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);
            _lblPerformanceAnchorSummary.Text = "Anchor groups: " + string.Join(", ", groups) + " | Sim users: " + GetSelectedPerfUsersCount();
        }

        private bool ShouldHidePerformanceBySettings(PerformanceRequestRecord p)
        {
            if (p == null)
                return false;
            var tag = p.FilterTag ?? string.Empty;
            var type = p.ResourceType ?? string.Empty;
            if (_performanceFilterTokensFromSettings.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(tag) && _performanceFilterTokensFromSettings.Contains(tag))
                    return true;
                if (!string.IsNullOrWhiteSpace(type) && _performanceFilterTokensFromSettings.Contains(type))
                    return true;
            }
            if (_performanceIgnoreExactUrlsFromSettings.Count > 0 || _performanceIgnoreWildcardUrlsFromSettings.Count > 0)
            {
                var url = p.Url ?? string.Empty;
                if (_performanceIgnoreExactUrlsFromSettings.Any(exact => string.Equals(url, exact, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (_performanceIgnoreWildcardUrlsFromSettings.Any(pattern => IsWildcardUrlPatternMatch(url, pattern)))
                    return true;
            }
            return false;
        }

        private void RefreshPerformanceFilterTokensFromSettings()
        {
            _performanceFilterTokensFromSettings.Clear();
            var raw = NormalizePerformanceFilterTokens(_settings?.PerformanceFilterTokens);
            var parts = raw.Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var t = p.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(t))
                    _performanceFilterTokensFromSettings.Add(t);
            }
            _performanceIgnoreExactUrlsFromSettings.Clear();
            _performanceIgnoreWildcardUrlsFromSettings.Clear();

            var exactRaw = NormalizePerformanceIgnoreUrlPatterns(_settings?.PerformanceIgnoreExactUrls);
            var wildcardRaw = NormalizePerformanceIgnoreUrlPatterns(_settings?.PerformanceIgnoreWildcardUrls);
            // Backward compatibility: old combined list treated as wildcard patterns.
            var legacyRaw = NormalizePerformanceIgnoreUrlPatterns(_settings?.PerformanceIgnoreUrlPatterns);

            var exactParts = exactRaw.Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in exactParts)
            {
                var item = part.Trim();
                if (!string.IsNullOrWhiteSpace(item) && !_performanceIgnoreExactUrlsFromSettings.Contains(item, StringComparer.OrdinalIgnoreCase))
                    _performanceIgnoreExactUrlsFromSettings.Add(item);
            }

            var wildcardParts = (wildcardRaw + ";" + legacyRaw).Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in wildcardParts)
            {
                var item = part.Trim();
                if (!string.IsNullOrWhiteSpace(item) && !_performanceIgnoreWildcardUrlsFromSettings.Contains(item, StringComparer.OrdinalIgnoreCase))
                    _performanceIgnoreWildcardUrlsFromSettings.Add(item);
            }
        }

        private void gridPerformance_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || _gridPerformance == null)
                return;
            var hit = _gridPerformance.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= _gridPerformance.Rows.Count)
                return;
            var row = _gridPerformance.Rows[hit.RowIndex];
            if (!row.Selected)
            {
                _gridPerformance.ClearSelection();
                row.Selected = true;
                if (hit.RowIndex >= 0)
                    _gridPerformance.CurrentCell = _gridPerformance.Rows[hit.RowIndex].Cells[0];
            }
        }

        private List<PerformanceRequestRecord> GetSelectedPerformanceRows()
        {
            if (_gridPerformance == null || _gridPerformance.SelectedRows == null || _gridPerformance.SelectedRows.Count == 0)
                return new List<PerformanceRequestRecord>();
            var rows = new List<PerformanceRequestRecord>();
            foreach (DataGridViewRow selected in _gridPerformance.SelectedRows)
            {
                if (selected?.DataBoundItem is PerformanceRequestRecord perf && perf != null)
                    rows.Add(perf);
            }
            return rows;
        }

        private void IgnoreSelectedPerformanceRows()
        {
            var selectedRows = GetSelectedPerformanceRows();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one performance row first.", "Ignore",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var firstUrl = selectedRows.Select(r => r?.Url ?? string.Empty).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)) ?? string.Empty;
            using (var dlg = new Form())
            {
                dlg.Text = "Ignore setting";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.ClientSize = new Size(680, 300);

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 9 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

                var lblUrl = new Label { Dock = DockStyle.Fill, Text = "Selected URL:", TextAlign = ContentAlignment.MiddleLeft };
                var txtUrl = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = firstUrl };
                var modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
                var rbExact = new RadioButton { Text = "Complete URL", AutoSize = true, Checked = true };
                var rbWildcard = new RadioButton { Text = "Wildcard pattern (*, ?)", AutoSize = true, Margin = new Padding(12, 3, 0, 3) };
                modePanel.Controls.Add(rbExact);
                modePanel.Controls.Add(rbWildcard);
                var txtPattern = new TextBox { Dock = DockStyle.Fill, Text = firstUrl };
                var lblExisting = new Label { Dock = DockStyle.Fill, Text = "Existing ignore templates:", TextAlign = ContentAlignment.MiddleLeft };
                var txtExistingExact = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = _settings?.PerformanceIgnoreExactUrls ?? string.Empty };
                var txtExistingWildcard = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = _settings?.PerformanceIgnoreWildcardUrls ?? _settings?.PerformanceIgnoreUrlPatterns ?? string.Empty };
                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 88 };
                var btnOk = new Button { Text = "OK", Width = 88 };
                btnOk.Click += (_, __) =>
                {
                    var pattern = (txtPattern.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        MessageBox.Show(dlg, "Ignore value cannot be empty.", "Ignore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!SavePerformanceIgnoreUrlPattern(pattern, rbExact.Checked))
                        return;
                    foreach (var row in selectedRows)
                    {
                        if (row == null)
                            continue;
                        row.IsFiltered = true;
                        row.Action = "Ignore";
                        row.Notes = rbExact.Checked ? "Ignored by exact URL: " + pattern : "Ignored by wildcard URL: " + pattern;
                    }
                    BindPerformanceForSelectedStep();
                    _gridPerformance?.Refresh();
                    SetStatus($"Ignored {selectedRows.Count} row(s): {pattern}");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                btnPanel.Controls.Add(btnOk);
                btnPanel.Controls.Add(btnCancel);

                layout.Controls.Add(lblUrl, 0, 0);
                layout.Controls.Add(txtUrl, 0, 1);
                layout.Controls.Add(modePanel, 0, 2);
                layout.Controls.Add(txtPattern, 0, 3);
                layout.Controls.Add(lblExisting, 0, 4);
                layout.Controls.Add(new Label { Text = "Exact URLs", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
                layout.Controls.Add(txtExistingExact, 0, 6);
                layout.Controls.Add(txtExistingWildcard, 0, 7);
                layout.Controls.Add(btnPanel, 0, 8);
                dlg.Controls.Add(layout);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                dlg.ShowDialog(this);
            }
        }

        private bool SavePerformanceIgnoreUrlPattern(string pattern, bool isExact)
        {
            if (_settings == null || string.IsNullOrWhiteSpace(pattern))
                return false;

            string AppendUnique(string raw, string value)
            {
                var items = NormalizePerformanceIgnoreUrlPatterns(raw)
                    .Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                if (!items.Contains(value, StringComparer.OrdinalIgnoreCase))
                    items.Add(value);
                return string.Join(";", items);
            }

            if (isExact)
                _settings.PerformanceIgnoreExactUrls = AppendUnique(_settings.PerformanceIgnoreExactUrls, pattern);
            else
                _settings.PerformanceIgnoreWildcardUrls = AppendUnique(_settings.PerformanceIgnoreWildcardUrls, pattern);

            RefreshPerformanceFilterTokensFromSettings();
            try
            {
                _settingsStore.Save(_settings);
                return true;
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "Could not save performance ignore URL pattern.");
                return false;
            }
        }

        private void gridPerformance_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _gridPerformance == null)
                return;
            if (!string.Equals(_gridPerformance.Columns[e.ColumnIndex].Name, "colPerfAction", StringComparison.Ordinal))
                return;
            if (!(_gridPerformance.Rows[e.RowIndex].DataBoundItem is PerformanceRequestRecord perf))
                return;

            var action = (perf.Action ?? string.Empty).Trim();
            if (string.Equals(action, "Promote", StringComparison.OrdinalIgnoreCase))
            {
                perf.IsAnchorSelected = true;
                perf.Action = "Unlink";
                perf.Notes = "Promoted as transaction anchor";
                BindPerformanceForSelectedStep();
                _gridPerformance.Refresh();
                return;
            }
            if (string.Equals(action, "Unlink", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var s in _steps)
                    s?.PerformanceRequestRefs?.Remove(perf.Id);
                BindPerformanceForSelectedStep();
                gridSteps.Refresh();
                return;
            }

            if (string.Equals(action, "Ignore", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(perf.FilterTag))
            {
                if (string.IsNullOrWhiteSpace(perf.FilterTag))
                    perf.FilterTag = "noise";
                _performanceFilterTags.Add(perf.FilterTag);
                perf.IsFiltered = true;
                perf.Notes = "Ignored as noise";
                BindPerformanceForSelectedStep();
            }
        }

        private void gridPerformance_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridPerformance == null)
                return;
            if (!(_gridPerformance.Rows[e.RowIndex].DataBoundItem is PerformanceRequestRecord perf))
                return;
            using (var dlg = new PerformanceRequestDetailForm(perf))
                dlg.ShowDialog(this);
            _gridPerformance.Refresh();
            BindPerformanceForSelectedStep();
            RefreshRecordReplayCanvas();
        }

        private void gridPerfRuntime_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _latestPerformanceMetrics == null)
                return;
            var report = _latestPerformanceMetrics.BuildReportSnapshot();
            var env = $"Machine: {Environment.MachineName}\nOS: {Environment.OSVersion}\nCLR: {Environment.Version}\nURL: {txtUrl?.Text?.Trim() ?? string.Empty}\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var dlg = new PerformanceRuntimeReportForm(report, env, GetSelectedPerfUsersCount(), Math.Max(1, _perfDurationSeconds), _settings?.UiLanguage ?? "en");
            dlg.Show(this);
        }

        private void gridPerformance_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (_gridPerformance == null || e.RowIndex < 0 || e.RowIndex >= _gridPerformance.Rows.Count)
                return;
            if (!(_gridPerformance.Rows[e.RowIndex].DataBoundItem is PerformanceRequestRecord row))
                return;
            if (row.IsAnchorSelected)
            {
                _gridPerformance.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(219, 234, 254);
                return;
            }
            if (row.AnchorCandidate)
                _gridPerformance.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 249, 195);
        }

        private void ApplyStepsGridColumnHeaders()
        {
            void Set(string colName, string headerKey)
            {
                if (!gridSteps.Columns.Contains(colName))
                    return;
                gridSteps.Columns[colName].HeaderText = L(headerKey);
            }

            Set("colAct", "StepsColAction");
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
            if (gridSteps.Columns.Contains("colPerfRef"))
                gridSteps.Columns["colPerfRef"].HeaderText = "Perf#";
            if (_stepsGridMenu != null && _stepsGridMenu.Items.Count >= 6)
            {
                _stepsGridMenu.Items[0].Text = L("GridRun");
                _stepsGridMenu.Items[1].Text = L("GridDelete");
                _stepsGridMenu.Items[2].Text = L("GridHighlight");
                _stepsGridMenu.Items[4].Text = L("GridExport");
                _stepsGridMenu.Items[5].Text = L("GridInsertRow");
            }
            if (gridSteps.Columns.Contains("colAct"))
                gridSteps.Columns["colAct"].HeaderText = string.Empty;
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
        }

        private void EnsureGridActionIcons()
        {
            if (_gridActDeleteIcon != null && _gridActHighlightIcon != null && _gridActTestIcon != null)
                return;
            _gridActDeleteIcon = FormsIconHelper.ToBitmap(IconChar.Trash, Color.FromArgb(220, 38, 38), 10, 0d, FlipOrientation.Normal);
            _gridActHighlightIcon = FormsIconHelper.ToBitmap(IconChar.Bullseye, Color.FromArgb(14, 116, 144), 10, 0d, FlipOrientation.Normal);
            _gridActTestIcon = FormsIconHelper.ToBitmap(IconChar.Play, Color.FromArgb(5, 150, 105), 10, 0d, FlipOrientation.Normal);
        }

        private void gridSteps_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (!string.Equals(gridSteps.Columns[e.ColumnIndex].Name, "colAct", StringComparison.Ordinal))
                return;

            EnsureGridActionIcons();
            e.PaintBackground(e.CellBounds, true);
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            var seg = Math.Max(1, e.CellBounds.Width / 3);
            var y = e.CellBounds.Top + Math.Max(0, (e.CellBounds.Height - 10) / 2);
            var x0 = e.CellBounds.Left + Math.Max(0, (seg - 10) / 2);
            var x1 = e.CellBounds.Left + seg + Math.Max(0, (seg - 10) / 2);
            var x2 = e.CellBounds.Left + (seg * 2) + Math.Max(0, (seg - 10) / 2);
            if (_gridActDeleteIcon != null) e.Graphics.DrawImage(_gridActDeleteIcon, x0, y, 10, 10);
            if (_gridActHighlightIcon != null) e.Graphics.DrawImage(_gridActHighlightIcon, x1, y, 10, 10);
            if (_gridActTestIcon != null) e.Graphics.DrawImage(_gridActTestIcon, x2, y, 10, 10);
            e.Handled = true;
        }

        private void gridSteps_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var col = gridSteps.Columns[e.ColumnIndex].Name;
            if (!string.Equals(col, "colAct", StringComparison.Ordinal))
                return;
            if (e.RowIndex >= _steps.Count)
                return;
            var rect = gridSteps.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            var relX = e.X;
            var one = Math.Max(1, rect.Width / 3);
            if (relX < one)
            {
                _steps.RemoveAt(e.RowIndex);
                return;
            }
            if (relX < one * 2)
            {
                _ = HighlightStepOnPageAsync(_steps[e.RowIndex]);
                return;
            }
            _ = TestStepByIndexAsync(e.RowIndex);
        }

        private void gridSteps_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var col = gridSteps.Columns[e.ColumnIndex].Name;
            if (string.Equals(col, "colAct", StringComparison.Ordinal)
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

        private void gridSteps_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _steps.Count || e.ColumnIndex < 0)
                return;
            if (!string.Equals(gridSteps.Columns[e.ColumnIndex].Name, "colPerfRef", StringComparison.Ordinal))
                return;
            e.Value = _steps[e.RowIndex]?.PerformanceRequestRefs?.Count ?? 0;
            e.FormattingApplied = true;
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
            try
            {
                if (!_recordSplitDistanceInitialized && _recordSplit != null && tabRecord.ClientSize.Width >= 160)
                {
                    var available = Math.Max(0, _recordSplit.ClientSize.Width - _recordSplit.SplitterWidth);
                    const int desiredMinLeft = 260;
                    const int desiredMinRight = 160;
                    if (available >= desiredMinLeft + desiredMinRight)
                    {
                        _recordSplit.Panel1MinSize = desiredMinLeft;
                        _recordSplit.Panel2MinSize = desiredMinRight;
                    }
                    else
                    {
                        // Keep split functional on narrow startup widths; avoid impossible min-size constraints.
                        _recordSplit.Panel1MinSize = 0;
                        _recordSplit.Panel2MinSize = 0;
                    }
                    var min = _recordSplit.Panel1MinSize;
                    var max = available - _recordSplit.Panel2MinSize;
                    if (max >= min)
                    {
                        var target = (int)Math.Round(available * 0.62d);
                        if (target < min) target = min;
                        if (target > max) target = max;
                        _recordSplit.SplitterDistance = target;
                        _recordSplitDistanceInitialized = true;
                    }
                }

                if (!_stepsMasterDetailSplitDistanceInitialized && _stepsMasterDetailSplit != null && tabRecord.ClientSize.Height >= 200)
                {
                    var availableH = Math.Max(0, _stepsMasterDetailSplit.ClientSize.Height - _stepsMasterDetailSplit.SplitterWidth);
                    var minTop = _stepsMasterDetailSplit.Panel1MinSize;
                    var maxTop = availableH - _stepsMasterDetailSplit.Panel2MinSize;
                    if (maxTop >= minTop)
                    {
                        // Keep performance detail table at ~1/4 height by default.
                        var targetTop = (int)Math.Round(availableH * 0.75d);
                        if (targetTop < minTop) targetTop = minTop;
                        if (targetTop > maxTop) targetTop = maxTop;
                        _stepsMasterDetailSplit.SplitterDistance = targetTop;
                        _stepsMasterDetailSplitDistanceInitialized = true;
                    }
                }

                if (_perfAnchorRuntimeSplit != null
                    && tabRecord.ClientSize.Height >= 200
                    && IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            ApplyPerfAnchorRuntimeSplitDistance();
                        }
                        catch
                        {
                            // ignore invalid splitter distance during early layout
                        }
                    }));
                }
            }
            catch
            {
                // ignore invalid splitter distance during early layout
            }
        }

        /// <summary>
        /// Reserves a stable band for the runtime throughput grid; anchor requests grid takes remaining height.
        /// Called after outer splits have non-zero height (see <see cref="TabRecord_SizeChanged"/>).
        /// </summary>
        private void ApplyPerfAnchorRuntimeSplitDistance()
        {
            if (_perfAnchorRuntimeSplit == null)
                return;
            var h = _perfAnchorRuntimeSplit.Height;
            if (h < 80)
                return;
            var sw = _perfAnchorRuntimeSplit.SplitterWidth;
            var avail = Math.Max(0, h - sw);
            var minTop = _perfAnchorRuntimeSplit.Panel1MinSize;
            var maxTop = avail - _perfAnchorRuntimeSplit.Panel2MinSize;
            if (maxTop < minTop)
                return;
            var runtimeBand = Math.Max(_perfAnchorRuntimeSplit.Panel2MinSize, (int)Math.Round(avail * 0.42d));
            runtimeBand = Math.Min(runtimeBand, 320);
            var bottomKeep = Math.Min(avail - minTop, Math.Max(_perfAnchorRuntimeSplit.Panel2MinSize, runtimeBand));
            var top = avail - bottomKeep;
            if (top < minTop) top = minTop;
            if (top > maxTop) top = maxTop;
            _perfAnchorRuntimeSplit.SplitterDistance = top;
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
                    _recordWebView.NavigateToString(BuildRecordReplayCanvasFallbackHtml(_steps, _performanceRequests, _recordCanvasDebugEnabled));

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
                _recordWebView.NavigateToString(BuildRecordReplayCanvasFallbackHtml(_steps, _performanceRequests, _recordCanvasDebugEnabled));
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
                        ["locatorShort"] = loc,
                        ["hasPerformance"] = s.PerformanceRequestRefs != null && s.PerformanceRequestRefs.Count > 0,
                        ["performanceCount"] = s.PerformanceRequestRefs?.Count ?? 0
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

        private static string BuildRecordReplayCanvasFallbackHtml(BindingList<SemanticStepRecord> steps, BindingList<PerformanceRequestRecord> perfRows, bool debugEnabled)
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
            sb.Append(".perf{margin-top:8px;border-radius:10px;padding:5px 8px;font-size:10px;font-weight:600;display:inline-block;}");
            sb.Append(".perfGet{background:#dcfce7;border:1px solid #22c55e;color:#166534;}");
            sb.Append(".perfPost{background:#fee2e2;border:1px solid #ef4444;color:#991b1b;}");
            sb.Append(".perfOther{background:#ede9fe;border:1px solid #8b5cf6;color:#5b21b6;}");
            sb.Append(".kwFill{background:linear-gradient(155deg,#e8f4fd,#dbeafe);border-color:#1d4ed8;}");
            sb.Append(".kwClick{background:linear-gradient(155deg,#fff7ed,#ffedd5);border-color:#c2410c;}");
            sb.Append(".kwSel{background:linear-gradient(155deg,#f5f3ff,#ede9fe);border-color:#6d28d9;}");
            sb.Append(".kwCheck{background:linear-gradient(155deg,#ecfdf5,#d1fae5);border-color:#047857;}");
            sb.Append(".kwTable{background:linear-gradient(155deg,#ecfeff,#cffafe);border-color:#0e7490;}");
            sb.Append(".kwSearch{background:linear-gradient(155deg,#fef9c3,#fef08a);border-color:#a16207;}");
            sb.Append(".kwDef{background:linear-gradient(155deg,#f8fafc,#e2e8f0);border-color:#475569;}");
            sb.Append(".anchorLane{position:absolute;left:16px;right:16px;min-height:130px;border:1px dashed #94a3b8;border-radius:12px;background:#f8fafc;padding:12px;}");
            sb.Append(".anchorTitle{font-size:12px;font-weight:700;color:#334155;margin-bottom:8px;}");
            sb.Append(".anchorNode{display:inline-block;transform:rotate(45deg);width:82px;height:82px;margin:8px 14px;background:#dbeafe;border:1px solid #2563eb;vertical-align:top;}");
            sb.Append(".anchorNode > span{display:block;transform:rotate(-45deg);font-size:10px;color:#1e3a8a;padding:26px 8px 0 8px;text-align:center;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}");
            sb.Append(".anchorLogin{background:#fee2e2;border-color:#dc2626;}");
            sb.Append(".anchorCreate{background:#dcfce7;border-color:#16a34a;}");
            sb.Append(".anchorCandidate{background:#fef9c3;border-color:#ca8a04;}");
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
                    if (s.PerformanceRequestRefs != null && s.PerformanceRequestRefs.Count > 0)
                    {
                        var perfClass = "perfOther";
                        if (string.Equals(s.SourceEvent, "fetch", StringComparison.OrdinalIgnoreCase))
                            perfClass = "perfGet";
                        else if (string.Equals(s.SourceEvent, "xhr", StringComparison.OrdinalIgnoreCase))
                            perfClass = "perfPost";
                        sb.Append("<div class=\"perf ").Append(perfClass).Append("\">");
                        sb.Append(WebUtility.HtmlEncode("Requests: " + s.PerformanceRequestRefs.Count));
                        sb.Append("</div>");
                    }
                    sb.Append("</div>");
                }
            }

            var anchorRows = (perfRows ?? new BindingList<PerformanceRequestRecord>())
                .Where(p => p != null && (p.IsAnchorSelected || p.AnchorCandidate))
                .ToList();
            var groups = anchorRows
                .GroupBy(p => string.IsNullOrWhiteSpace(p.AnchorGroup) ? "General" : p.AnchorGroup)
                .ToList();
            var maxStepBottom = 0d;
            if (steps != null)
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var s = steps[i];
                    var defaultTop = 28d + (i / 3) * 120d;
                    var top = s.CanvasY ?? defaultTop;
                    if (double.IsNaN(top) || double.IsInfinity(top))
                        top = defaultTop;
                    var bottom = top + 130d;
                    if (bottom > maxStepBottom)
                        maxStepBottom = bottom;
                }
            }
            var laneTop = Math.Max(430, (int)Math.Round(maxStepBottom + 28d));
            var laneIndex = 0;
            foreach (var g in groups)
            {
                var topPx = laneTop + laneIndex * 150;
                sb.Append("<div class=\"anchorLane\" style=\"top:").Append(topPx.ToString(CultureInfo.InvariantCulture)).Append("px;\">");
                sb.Append("<div class=\"anchorTitle\">Group: ").Append(WebUtility.HtmlEncode(g.Key)).Append("</div>");
                foreach (var item in g.Take(8))
                {
                    var cls = "anchorNode";
                    if (string.Equals(g.Key, "Login", StringComparison.OrdinalIgnoreCase)) cls += " anchorLogin";
                    if (g.Key.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0) cls += " anchorCreate";
                    if (!item.IsAnchorSelected) cls += " anchorCandidate";
                    var text = (item.Method ?? string.Empty) + " " + (item.ResourceType ?? string.Empty);
                    sb.Append("<div class=\"").Append(cls).Append("\"><span>");
                    sb.Append(WebUtility.HtmlEncode(text));
                    sb.Append("</span></div>");
                }
                sb.Append("</div>");
                laneIndex++;
            }

            var canvasMinHeight = Math.Max(520, laneTop + (Math.Max(1, groups.Count) * 150) + 50);
            sb.Append("<script type=\"text/javascript\">try{var cv=document.getElementById('cv');if(cv)cv.style.minHeight='")
                .Append(canvasMinHeight.ToString(CultureInfo.InvariantCulture))
                .Append("px';}catch(e){}</script>");

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
            tsbReloadEngine.Image = FormsIconHelper.ToBitmap(IconChar.Rotate, Color.FromArgb(51, 65, 85), 20, 0d, FlipOrientation.Normal);
            tsbReloadEngine.ImageScaling = ToolStripItemImageScaling.None;
        }

        private void chkSyncFocus_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRecorderModeFromUiStateAsync();
        }

        private void chkWithPerformanceTest_CheckedChanged(object sender, EventArgs e)
        {
            ApplyPerformancePanelVisibility();
            UpdatePerformanceMenuState();
        }

        private async void tsbRunPerf_Click(object sender, EventArgs e)
        {
            await RunPerformanceTestAsync().ConfigureAwait(true);
        }

        private void tsbStopPerf_Click(object sender, EventArgs e)
        {
            StopCurrentPerformanceRun();
        }

        private async void tsbRunPerfSelected_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedAnchorGroupsFromGrid();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select rows in performance grid first.", "Performance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await RunPerformanceTestAsync(selected).ConfigureAwait(true);
        }

        private void SetupPerformanceToolbarCheckbox()
        {
            tsbRunPerf.Image = FormsIconHelper.ToBitmap(IconChar.BoltLightning, Color.FromArgb(3, 105, 161), 18, 0d, FlipOrientation.Normal);
            tsbRunPerf.ImageScaling = ToolStripItemImageScaling.None;
            tsbStopPerf.Image = FormsIconHelper.ToBitmap(IconChar.Stop, Color.FromArgb(220, 38, 38), 16, 0d, FlipOrientation.Normal);
            tsbStopPerf.ImageScaling = ToolStripItemImageScaling.None;
        }

        private void SetupMenuBrandTitle()
        {
            _menuBrandTitle = new ToolStripMenuItem("MARS WEB automation")
            {
                Enabled = false
            };
            menuMain.Items.Insert(0, _menuBrandTitle);
            tslBrand.Visible = false;
            tsbSepBrand.Visible = false;
        }

        private bool IsPerformanceTestEnabled()
        {
            return chkWithPerformanceTest.Checked;
        }

        private void SetupMenuPerformanceOptions()
        {
            _menuPerformance = new ToolStripMenuItem("Performance");
            _menuWithPerformanceTest = new ToolStripMenuItem("With Performance Test") { CheckOnClick = true };
            _menuWithPerformanceTest.CheckedChanged += (_, __) =>
            {
                if (_syncingPerfMenuState)
                    return;
                chkWithPerformanceTest.Checked = _menuWithPerformanceTest.Checked;
            };

            _menuPerfUsers = new ToolStripMenuItem("Sim Users");
            _menuPerfUsers5 = NewPerfUsersMenu("5");
            _menuPerfUsers10 = NewPerfUsersMenu("10");
            _menuPerfUsers100 = NewPerfUsersMenu("100");
            _menuPerfUsers.DropDownItems.AddRange(new ToolStripItem[] { _menuPerfUsers5, _menuPerfUsers10, _menuPerfUsers100 });

            _menuPerfConfigTransactions = new ToolStripMenuItem("Configure Transactions...");
            _menuPerfConfigTransactions.Click += (_, __) => ConfigurePerformanceTransactions();
            _menuPerfRunNow = new ToolStripMenuItem("Run NBomber");
            _menuPerfRunNow.Click += async (_, __) => await RunPerformanceTestAsync().ConfigureAwait(true);
            _menuPerfRunSelectedAnchor = new ToolStripMenuItem("Run Selected AnchorGroup");
            _menuPerfRunSelectedAnchor.Click += async (_, __) =>
            {
                var selected = GetSelectedAnchorGroupsFromGrid();
                if (selected.Count == 0)
                {
                    MessageBox.Show(this, "Select rows in performance grid first.", "Performance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                await RunPerformanceTestAsync(selected).ConfigureAwait(true);
            };
            _menuPerfExportPack = new ToolStripMenuItem("Export Performance Pack…");
            _menuPerfExportPack.Click += (_, __) => ExportPerformancePack();
            _menuPerfImportPack = new ToolStripMenuItem("Import Performance Pack…");
            _menuPerfImportPack.Click += (_, __) => ImportPerformancePack();

            _menuPerformance.DropDownItems.Add(_menuWithPerformanceTest);
            _menuPerformance.DropDownItems.Add(new ToolStripSeparator());
            _menuPerformance.DropDownItems.Add(_menuPerfUsers);
            _menuPerformance.DropDownItems.Add(_menuPerfExportPack);
            _menuPerformance.DropDownItems.Add(_menuPerfImportPack);
            _menuPerformance.DropDownItems.Add(new ToolStripSeparator());
            _menuPerformance.DropDownItems.Add(_menuPerfConfigTransactions);
            _menuPerformance.DropDownItems.Add(_menuPerfRunNow);
            _menuPerformance.DropDownItems.Add(_menuPerfRunSelectedAnchor);
            menuMain.Items.Add(_menuPerformance);
            UpdatePerformanceMenuState();
        }

        private ToolStripMenuItem NewPerfUsersMenu(string users)
        {
            var item = new ToolStripMenuItem(users) { CheckOnClick = true };
            item.Click += (_, __) =>
            {
                if (_syncingPerfMenuState)
                    return;
                if (_tscbPerfUsers != null)
                    _tscbPerfUsers.SelectedItem = users;
                else if (_settings != null && int.TryParse(users, out var n) && n > 0)
                    _settings.PerformanceSimUserCount = n;
                UpdatePerformanceMenuState();
            };
            return item;
        }

        private void UpdatePerformanceMenuState()
        {
            if (_menuPerformance == null)
                return;

            _syncingPerfMenuState = true;
            try
            {
                var users = GetSelectedPerfUsersCount().ToString(CultureInfo.InvariantCulture);
                if (_menuWithPerformanceTest != null)
                    _menuWithPerformanceTest.Checked = IsPerformanceTestEnabled();
                if (_menuPerfUsers5 != null) _menuPerfUsers5.Checked = string.Equals(users, "5", StringComparison.Ordinal);
                if (_menuPerfUsers10 != null) _menuPerfUsers10.Checked = string.Equals(users, "10", StringComparison.Ordinal);
                if (_menuPerfUsers100 != null) _menuPerfUsers100.Checked = string.Equals(users, "100", StringComparison.Ordinal);
            }
            finally
            {
                _syncingPerfMenuState = false;
            }
        }

        private int GetSelectedPerfUsersCount()
        {
            if (_tscbPerfUsers == null)
                return Math.Max(1, _settings?.PerformanceSimUserCount ?? 5);
            if (int.TryParse(_tscbPerfUsers.Text, out var n) && n > 0)
                return n;
            return Math.Max(1, _settings?.PerformanceSimUserCount ?? 5);
        }

        private void ApplyPerformancePanelVisibility()
        {
            if (_stepsMasterDetailSplit == null)
                return;
            var enabled = IsPerformanceTestEnabled();
            _stepsMasterDetailSplit.Panel2Collapsed = !enabled;
            if (!enabled && _gridPerformance != null)
                _gridPerformance.DataSource = new BindingList<PerformanceRequestRecord>();
            if (enabled)
            {
                BindPerformanceForSelectedStep();
                if (_perfAnchorRuntimeSplit != null)
                    ApplyPerfAnchorRuntimeSplitDistance();
            }
            tsbRunPerf.Enabled = enabled && !_performanceRunInProgress;
            if (_menuPerfRunNow != null)
                _menuPerfRunNow.Enabled = enabled && !_performanceRunInProgress;
            if (_menuPerfRunSelectedAnchor != null)
                _menuPerfRunSelectedAnchor.Enabled = enabled && !_performanceRunInProgress;
            if (_menuPerfConfigTransactions != null)
                _menuPerfConfigTransactions.Enabled = enabled && !_performanceRunInProgress;
            if (_menuPerfExportPack != null)
                _menuPerfExportPack.Enabled = !_performanceRunInProgress && _performanceRequests.Count > 0;
            if (_menuPerfImportPack != null)
                _menuPerfImportPack.Enabled = !_performanceRunInProgress;
        }

        private void ConfigurePerformanceTransactions()
        {
            var names = CollectTransactionNames();
            if (names.Count == 0)
            {
                MessageBox.Show(this, "No transaction groups available. Record performance requests first.", "Performance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rows = names.Select(name =>
            {
                if (_transactionConfigByName.TryGetValue(name, out var existing))
                    return new TransactionConfigRow
                    {
                        Name = name,
                        Enabled = existing.Enabled,
                        UsersOverride = existing.UsersOverride,
                        DurationSecondsOverride = existing.DurationSecondsOverride,
                        Weight = Math.Max(1, existing.Weight)
                    };
                return new TransactionConfigRow { Name = name, Enabled = true, Weight = 1 };
            }).ToList();

            using (var dlg = new PerformanceTransactionConfigForm(rows, GetSelectedPerfUsersCount(), _perfDurationSeconds))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _transactionConfigByName.Clear();
                foreach (var row in dlg.Rows.Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name)))
                {
                    _transactionConfigByName[row.Name] = new TransactionConfigRow
                    {
                        Name = row.Name,
                        Enabled = row.Enabled,
                        UsersOverride = row.UsersOverride > 0 ? row.UsersOverride : null,
                        DurationSecondsOverride = row.DurationSecondsOverride > 0 ? row.DurationSecondsOverride : null,
                        Weight = Math.Max(1, row.Weight)
                    };
                }

                _perfDurationSeconds = Math.Max(1, dlg.SelectedDurationSeconds);
                if (_tscbPerfUsers != null)
                    _tscbPerfUsers.SelectedItem = dlg.SelectedDefaultUsers.ToString(CultureInfo.InvariantCulture);
                UpdatePerformanceAnchorSummary(GetCurrentPerformanceRowsForDisplay());
            }
        }

        private async Task RunPerformanceTestAsync(HashSet<string> targetGroups = null)
        {
            if (!IsPerformanceTestEnabled())
            {
                MessageBox.Show(this, "Please enable 'With Performance Test' first.", "Performance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_performanceRunInProgress)
                return;

            var sourceRequests = GetCurrentPerformanceRowsForDisplay()
                .Where(r => r != null && !r.IsFiltered && !string.Equals(r.Action, "Ignore", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (targetGroups != null && targetGroups.Count > 0)
            {
                sourceRequests = sourceRequests
                    .Where(r =>
                    {
                        var g = string.IsNullOrWhiteSpace(r.AnchorGroup) ? "General" : r.AnchorGroup.Trim();
                        return targetGroups.Contains(g);
                    })
                    .ToList();
            }
            if (sourceRequests.Count == 0)
            {
                MessageBox.Show(this, "No performance rows available to run.", "Performance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var usersPre = GetSelectedPerfUsersCount();
            var durationPre = TimeSpan.FromSeconds(Math.Max(1, _perfDurationSeconds));
            var plan = _perfExecuteAdapter.BuildExecutionPlan(sourceRequests, usersPre, durationPre);
            ApplyTransactionConfig(plan);
            if (targetGroups != null && targetGroups.Count > 0 && plan.Transactions != null)
            {
                foreach (var tx in plan.Transactions)
                {
                    if (tx == null)
                        continue;
                    tx.Enabled = tx.Enabled && targetGroups.Contains(tx.Name ?? string.Empty);
                }
            }

            if (plan.Transactions == null || plan.Transactions.Count == 0 || plan.Transactions.All(t => t == null || !t.Enabled))
            {
                MessageBox.Show(this, "No enabled transactions in current plan.", "Performance",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PerformanceRunUiOptions runOpt;
            using (var runDialog = new PerformanceRunDialog(BuildPerformanceRunDialogSeed(usersPre)))
            {
                if (runDialog.ShowDialog(this) != DialogResult.OK || runDialog.ResultOptions == null)
                    return;
                runOpt = runDialog.ResultOptions;
            }

            var users = Math.Max(1, runOpt.SimulatedUsers);
            _perfDurationSeconds = Math.Max(1, runOpt.DurationSeconds);
            if (_tscbPerfUsers != null)
            {
                var s = users.ToString(CultureInfo.InvariantCulture);
                if (_tscbPerfUsers.Items.IndexOf(s) < 0)
                    _tscbPerfUsers.Items.Add(s);
                _tscbPerfUsers.SelectedItem = s;
                if (_settings != null)
                    _settings.PerformanceSimUserCount = users;
            }

            PersistLastPerformanceRunFromDialog(runOpt);

            var scheduleUsers = BuildPerfUsersSchedule(runOpt);
            if (scheduleUsers.Count == 0)
                scheduleUsers.Add(users);
            const int nbomberDefaultWarmupSeconds = 30;
            if (_perfDurationSeconds < nbomberDefaultWarmupSeconds)
            {
                MessageBox.Show(this,
                    $"Duration ({_perfDurationSeconds}s) is smaller than NBomber warm-up ({nbomberDefaultWarmupSeconds}s)." + Environment.NewLine
                    + "Please increase duration to at least 30 seconds before running.",
                    "Performance validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            _perfExpectedRounds = scheduleUsers.Count;
            _perfCompletedRounds = 0;
            _perfCurrentRound = 0;
            plan.SimulatedUsers = scheduleUsers[0];
            plan.Duration = TimeSpan.FromSeconds(_perfDurationSeconds);

            var metrics = new PerformanceMetricsCollector();
            metrics.Reset();
            _latestPerformanceMetrics = metrics;
            plan.Telemetry = new NBomberTelemetryContext
            {
                SaveResponseBodies = runOpt.SaveResponseBodies,
                ResponseLogDirectory = BuildDataTestLogDirectory(),
                ResponseBodyMustContain = runOpt.ResponseBodyMustContain,
                Metrics = metrics
            };

            try
            {
                var liveChart = new PerformanceLiveChartForm(metrics, Math.Max(1, runOpt.ChartSampleIntervalSeconds));
                liveChart.Show(this);
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "Could not open live chart window.");
            }

            SetPerformanceRunningState(true);
            ResetRuntimeProgressRows(plan);
            SetStatus("NBomber performance run started...");
            UpdatePerformanceAnchorSummary(sourceRequests);

            try
            {
                _perfRunStartedUtc = DateTime.UtcNow;
                _perfRunCts = new CancellationTokenSource();
                var all = new List<NBomberExecutionResult>();
                for (var i = 0; i < scheduleUsers.Count; i++)
                {
                    _perfRunCts.Token.ThrowIfCancellationRequested();
                    var usersNow = scheduleUsers[i];
                    _perfCurrentRound = i + 1;
                    plan.SimulatedUsers = usersNow;
                    ResetRuntimeProgressRows(plan);
                    SetStatus($"NBomber running stage {i + 1}/{scheduleUsers.Count}, users={usersNow} ...");
                    var result = await Task.Run(async () =>
                        await _perfExecuteAdapter.ExecuteAsync(plan, snapshot =>
                        {
                            if (snapshot == null || IsDisposed || !IsHandleCreated)
                                return;
                            BeginInvoke(new Action(() =>
                            {
                                UpdateRuntimeProgress(snapshot);
                                var txt = "Perf users=" + usersNow + " " + (snapshot.Stage ?? "running")
                                          + " | OK=" + snapshot.TotalOk
                                          + " FAIL=" + snapshot.TotalFail
                                          + (string.IsNullOrWhiteSpace(snapshot.Transaction) ? string.Empty : " | Tx=" + snapshot.Transaction);
                                SetStatus(txt);
                            }));
                        }, _perfRunCts.Token).ConfigureAwait(false),
                        _perfRunCts.Token).ConfigureAwait(true);
                    all.Add(result);
                    _perfCompletedRounds = i + 1;
                    UpdateRuntimeRoundProgressForAllRows();
                    SavePerformanceResult(plan, result, sourceRequests, targetGroups);
                }

                var totalOk = all.Sum(r => r?.TotalOk ?? 0);
                var totalFail = all.Sum(r => r?.TotalFail ?? 0);
                var allSuccess = all.All(r => r != null && r.Success);
                var isZh = (_settings?.UiLanguage ?? "en").StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                var msgTitle = isZh ? "性能测试完毕。" : "Performance test completed.";
                var msg = msgTitle + Environment.NewLine + Environment.NewLine
                          + string.Format("{0,-12}: {1}", "Stages", all.Count + " (" + string.Join(", ", scheduleUsers) + ")") + Environment.NewLine
                          + string.Format("{0,-12}: {1}", "OK", totalOk) + Environment.NewLine
                          + string.Format("{0,-12}: {1}", "Fail", totalFail);
                MessageBox.Show(this, msg, "Performance", MessageBoxButtons.OK,
                    allSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                SetStatus(allSuccess ? "NBomber performance run completed." : "NBomber completed with failures.");
                _perfCurrentRound = _perfExpectedRounds;
                UpdateRuntimeRoundProgressForAllRows();
            }
            catch (OperationCanceledException)
            {
                SetStatus("NBomber performance run stopped.");
            }
            catch (Exception ex)
            {
                FormLog.Error(ex, "RunPerformanceTestAsync failed.");
                MessageBox.Show(this, ex.Message, "Performance Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("NBomber performance run failed.");
            }
            finally
            {
                if (_perfRunCts != null)
                {
                    _perfRunCts.Dispose();
                    _perfRunCts = null;
                }
                _perfCurrentRound = 0;
                SetPerformanceRunningState(false);
            }
        }

        private string BuildDataTestLogDirectory()
        {
            var configuredRoot = _settings?.DataRootFolder;
            var dataRoot = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(DataPathHelper.GetAssemblyBaseDirectory(), "data")
                : configuredRoot.Trim();
            var tail = dataRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(Path.GetFileName(tail), "data", StringComparison.OrdinalIgnoreCase))
                dataRoot = Path.Combine(dataRoot, "data");
            return Path.Combine(dataRoot, "test", "log");
        }

        private void SetPerformanceRunningState(bool running)
        {
            _performanceRunInProgress = running;
            tsbRunPerf.Enabled = !running && IsPerformanceTestEnabled();
            tsbStopPerf.Enabled = running;
            toolMain.Enabled = true;
            if (_menuPerfRunNow != null)
                _menuPerfRunNow.Enabled = !running && IsPerformanceTestEnabled();
            if (_menuPerfRunSelectedAnchor != null)
                _menuPerfRunSelectedAnchor.Enabled = !running && IsPerformanceTestEnabled();
            if (_menuPerfConfigTransactions != null)
                _menuPerfConfigTransactions.Enabled = !running && IsPerformanceTestEnabled();
            if (_menuPerfExportPack != null)
                _menuPerfExportPack.Enabled = !running && _performanceRequests.Count > 0;
            if (_menuPerfImportPack != null)
                _menuPerfImportPack.Enabled = !running;
        }

        private void StopCurrentPerformanceRun()
        {
            if (!_performanceRunInProgress || _perfRunCts == null)
                return;
            try
            {
                _perfRunCts.Cancel();
                SetStatus("Stopping performance run...");
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "StopCurrentPerformanceRun ignored.");
            }
        }

        private void ExportPerformancePack()
        {
            if (_performanceRequests == null || _performanceRequests.Count == 0)
            {
                MessageBox.Show(this, "No performance requests to export.", "Performance Pack", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Filter = "Performance pack JSON|*.json|All files|*.*",
                FileName = "mars-performance-pack.json"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    var tc = new Dictionary<string, PerformanceTransactionConfigEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in _transactionConfigByName)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value == null)
                            continue;
                        tc[kv.Key] = new PerformanceTransactionConfigEntry
                        {
                            Enabled = kv.Value.Enabled,
                            UsersOverride = kv.Value.UsersOverride,
                            DurationSecondsOverride = kv.Value.DurationSecondsOverride,
                            Weight = Math.Max(1, kv.Value.Weight)
                        };
                    }

                    var exportDirPath = BuildPerformancePackAssetDirectory(dlg.FileName);
                    Directory.CreateDirectory(exportDirPath);
                    var exportDirName = Path.GetFileName(exportDirPath);
                    var requestsForExport = CloneRequestsForExport(_performanceRequests.Where(r => r != null));
                    var exportedImages = 0;
                    foreach (var req in requestsForExport)
                    {
                        if (req == null)
                            continue;
                        if (!TryExportResponseImageToFile(req, exportDirPath, exportDirName))
                            continue;
                        exportedImages++;
                    }

                    var doc = new PerformancePackDocument
                    {
                        SourcePageUrl = txtUrl?.Text?.Trim(),
                        DefaultSimUsers = GetSelectedPerfUsersCount(),
                        DefaultDurationSeconds = _perfDurationSeconds,
                        Requests = requestsForExport,
                        TransactionConfig = tc
                    };
                    _perfPackStore.Save(dlg.FileName, doc);
                    SetStatus("Performance pack exported: " + dlg.FileName + (exportedImages > 0 ? $" (images: {exportedImages})" : string.Empty));
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "ExportPerformancePack failed.");
                    MessageBox.Show(this, ex.Message, "Performance Pack", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string BuildPerformancePackAssetDirectory(string exportJsonPath)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(exportJsonPath);
            var parentDir = Path.GetDirectoryName(exportJsonPath);
            if (string.IsNullOrWhiteSpace(parentDir))
                parentDir = DataPathHelper.GetAssemblyBaseDirectory();
            return Path.Combine(parentDir, fileNameWithoutExt ?? "performance-pack");
        }

        private static List<PerformanceRequestRecord> CloneRequestsForExport(IEnumerable<PerformanceRequestRecord> source)
        {
            var list = new List<PerformanceRequestRecord>();
            if (source == null)
                return list;
            foreach (var r in source)
            {
                if (r == null)
                    continue;
                list.Add(new PerformanceRequestRecord
                {
                    Id = r.Id,
                    TimestampUtc = r.TimestampUtc,
                    Action = r.Action,
                    Method = r.Method,
                    ResourceType = r.ResourceType,
                    Url = r.Url,
                    Parameter = r.Parameter,
                    Headers = r.Headers,
                    Cookies = r.Cookies,
                    Payload = r.Payload,
                    Response = r.Response,
                    Status = r.Status,
                    ReplayPolicy = r.ReplayPolicy,
                    ValidationHint = r.ValidationHint,
                    AnchorScore = r.AnchorScore,
                    AnchorCandidate = r.AnchorCandidate,
                    IsAnchorSelected = r.IsAnchorSelected,
                    AnchorGroup = r.AnchorGroup,
                    CorrelationNeeded = r.CorrelationNeeded,
                    CorrelationHint = r.CorrelationHint,
                    Notes = r.Notes,
                    FilterTag = r.FilterTag,
                    IsFiltered = r.IsFiltered
                });
            }
            return list;
        }

        private static bool TryExportResponseImageToFile(PerformanceRequestRecord req, string exportDirPath, string exportDirName)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Response))
                return false;

            if (!TryExtractResponseImageBytes(req, out var imageBytes, out var extension))
                return false;
            if (imageBytes == null || imageBytes.Length == 0)
                return false;

            var id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString("N") : req.Id.Trim();
            var safeId = string.Concat(id.Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_'));
            if (string.IsNullOrWhiteSpace(safeId))
                safeId = Guid.NewGuid().ToString("N");
            var fileName = safeId + extension;
            var fullPath = Path.Combine(exportDirPath, fileName);
            File.WriteAllBytes(fullPath, imageBytes);
            req.Response = $"@file:{exportDirName}/{fileName}";
            return true;
        }

        private static bool TryExtractResponseImageBytes(PerformanceRequestRecord req, out byte[] bytes, out string extension)
        {
            bytes = null;
            extension = ".bin";
            var raw = (req?.Response ?? string.Empty).Trim();
            if (raw.Length == 0)
                return false;

            var dataUri = Regex.Match(raw, @"^data:image/(?<mime>[\w\+\-\.]+);base64,(?<data>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            if (dataUri.Success)
            {
                var base64 = dataUri.Groups["data"].Value.Trim();
                if (TryDecodeBase64(base64, out bytes))
                {
                    extension = GuessImageExtension(dataUri.Groups["mime"].Value, req?.Url);
                    return true;
                }
                return false;
            }

            if (!LooksLikeImageRequest(req))
                return false;
            if (!TryDecodeBase64(raw, out bytes))
                return false;

            extension = GuessImageExtension(req?.ResourceType, req?.Url);
            return true;
        }

        private static bool TryDecodeBase64(string raw, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var compact = raw.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
            try
            {
                bytes = Convert.FromBase64String(compact);
                return bytes != null && bytes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeImageRequest(PerformanceRequestRecord req)
        {
            if (req == null)
                return false;
            if (string.Equals(req.ResourceType, "image", StringComparison.OrdinalIgnoreCase))
                return true;
            var url = req.Url ?? string.Empty;
            return url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                   || url.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
        }

        private static string GuessImageExtension(string mimeOrType, string url)
        {
            var token = (mimeOrType ?? string.Empty).Trim().ToLowerInvariant();
            if (token.Contains("png")) return ".png";
            if (token.Contains("jpeg") || token.Contains("jpg")) return ".jpg";
            if (token.Contains("gif")) return ".gif";
            if (token.Contains("webp")) return ".webp";
            if (token.Contains("bmp")) return ".bmp";
            if (token.Contains("svg")) return ".svg";
            if (token.Contains("icon") || token.Contains("ico")) return ".ico";

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var ext = Path.GetExtension(url);
                    if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8)
                        return ext.ToLowerInvariant();
                }
                catch
                {
                    // ignore and fallback
                }
            }
            return ".img";
        }

        private void ImportPerformancePack()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "Performance pack JSON|*.json|All files|*.*"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    var doc = _perfPackStore.Load(dlg.FileName);
                    var list = doc?.Requests?.Where(r => r != null).ToList() ?? new List<PerformanceRequestRecord>();
                    if (list.Count == 0)
                    {
                        MessageBox.Show(this, "No requests found in file (expect schema mars.perf-pack/1.0, or { \"requests\": [...] }, or a JSON array).", "Performance Pack",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    foreach (var r in list)
                    {
                        if (string.IsNullOrWhiteSpace(r.Id))
                            r.Id = Guid.NewGuid().ToString("N");
                    }

                    var validIds = new HashSet<string>(list.Select(r => r.Id), StringComparer.Ordinal);

                    _performanceRequests.RaiseListChangedEvents = false;
                    try
                    {
                        _performanceRequests.Clear();
                        foreach (var r in list)
                            _performanceRequests.Add(r);
                    }
                    finally
                    {
                        _performanceRequests.RaiseListChangedEvents = true;
                    }

                    _transactionConfigByName.Clear();
                    if (doc.TransactionConfig != null)
                    {
                        foreach (var kv in doc.TransactionConfig)
                        {
                            if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value == null)
                                continue;
                            _transactionConfigByName[kv.Key] = new TransactionConfigRow
                            {
                                Name = kv.Key,
                                Enabled = kv.Value.Enabled,
                                UsersOverride = kv.Value.UsersOverride > 0 ? kv.Value.UsersOverride : null,
                                DurationSecondsOverride = kv.Value.DurationSecondsOverride > 0 ? kv.Value.DurationSecondsOverride : null,
                                Weight = Math.Max(1, kv.Value.Weight)
                            };
                        }
                    }

                    if (doc.DefaultDurationSeconds.HasValue && doc.DefaultDurationSeconds.Value > 0)
                        _perfDurationSeconds = doc.DefaultDurationSeconds.Value;
                    if (doc.DefaultSimUsers.HasValue && doc.DefaultSimUsers.Value > 0 && _tscbPerfUsers != null)
                    {
                        var s = doc.DefaultSimUsers.Value.ToString(CultureInfo.InvariantCulture);
                        if (_tscbPerfUsers.Items.IndexOf(s) < 0)
                            _tscbPerfUsers.Items.Add(s);
                        _tscbPerfUsers.SelectedItem = s;
                        if (_settings != null)
                            _settings.PerformanceSimUserCount = doc.DefaultSimUsers.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(doc.SourcePageUrl) && Uri.TryCreate(doc.SourcePageUrl.Trim(), UriKind.Absolute, out _))
                        txtUrl.Text = doc.SourcePageUrl.Trim();

                    SanitizeStepPerformanceRefs(validIds);
                    gridSteps.Refresh();
                    BindPerformanceForSelectedStep();
                    RefreshRecordReplayCanvas();
                    UpdatePerformanceAnchorSummary(GetCurrentPerformanceRowsForDisplay());
                    ApplyPerformancePanelVisibility();
                    SetStatus("Performance pack imported: " + list.Count + " request(s).");
                }
                catch (Exception ex)
                {
                    FormLog.Error(ex, "ImportPerformancePack failed.");
                    MessageBox.Show(this, ex.Message, "Performance Pack", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SanitizeStepPerformanceRefs(ISet<string> validIds)
        {
            if (validIds == null || validIds.Count == 0)
            {
                foreach (var step in _steps)
                {
                    step.PerformanceRequestRefs?.Clear();
                }
                return;
            }

            foreach (var step in _steps)
            {
                if (step?.PerformanceRequestRefs == null || step.PerformanceRequestRefs.Count == 0)
                    continue;
                step.PerformanceRequestRefs.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !validIds.Contains(id));
            }
        }

        private List<string> CollectTransactionNames()
        {
            return _performanceRequests
                .Where(p => p != null && !p.IsFiltered && !ShouldHidePerformanceBySettings(p))
                .Select(p => string.IsNullOrWhiteSpace(p.AnchorGroup) ? "General" : p.AnchorGroup.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<PerformanceRequestRecord> GetCurrentPerformanceRowsForDisplay()
        {
            return _performanceRequests
                .Where(p => p != null && !p.IsFiltered && !ShouldHidePerformanceBySettings(p))
                .ToList();
        }

        private HashSet<string> GetSelectedAnchorGroupsFromGrid()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_gridPerformance == null)
                return set;

            foreach (DataGridViewRow row in _gridPerformance.SelectedRows)
            {
                if (!(row?.DataBoundItem is PerformanceRequestRecord rec))
                    continue;
                set.Add(string.IsNullOrWhiteSpace(rec.AnchorGroup) ? "General" : rec.AnchorGroup.Trim());
            }

            return set;
        }

        private void ApplyTransactionConfig(NBomberExecutionPlan plan)
        {
            if (plan?.Transactions == null || plan.Transactions.Count == 0)
                return;

            foreach (var tx in plan.Transactions)
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.Name))
                    continue;
                if (!_transactionConfigByName.TryGetValue(tx.Name, out var cfg) || cfg == null)
                    continue;
                tx.Enabled = cfg.Enabled;
                tx.Weight = Math.Max(1, cfg.Weight);
                tx.SimulatedUsersOverride = cfg.UsersOverride > 0 ? cfg.UsersOverride : null;
                tx.DurationOverride = cfg.DurationSecondsOverride > 0
                    ? TimeSpan.FromSeconds(cfg.DurationSecondsOverride.Value)
                    : (TimeSpan?)null;
            }
        }

        private void ResetRuntimeProgressRows(NBomberExecutionPlan plan)
        {
            _runtimeByTransaction.Clear();
            _perfRuntimeRows.Clear();
            if (plan?.Transactions == null)
                return;

            foreach (var tx in plan.Transactions.Where(t => t != null && t.Enabled))
            {
                var row = new PerfTransactionRuntimeRow
                {
                    Transaction = tx.Name,
                    Ok = 0,
                    Fail = 0,
                    TotalRequest = 0,
                    FinishedRequest = 0,
                    RoundProgress = BuildRoundProgressText(),
                    ThroughputPerSecond = "0.00",
                    ErrorRate = "0.00%",
                    LastDetail = "pending"
                };
                _runtimeByTransaction[tx.Name ?? string.Empty] = row;
                _perfRuntimeRows.Add(row);
            }
        }

        private void UpdateRuntimeProgress(NBomberProgressSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Transaction))
                return;
            if (!_runtimeByTransaction.TryGetValue(snapshot.Transaction, out var row))
                return;

            if (string.Equals(snapshot.Stage, "ok", StringComparison.OrdinalIgnoreCase))
                row.Ok += 1;
            else if (string.Equals(snapshot.Stage, "fail", StringComparison.OrdinalIgnoreCase))
                row.Fail += 1;
            row.TotalRequest = Math.Max(row.TotalRequest, snapshot.TotalStarted);
            row.FinishedRequest = snapshot.TotalOk + snapshot.TotalFail;
            row.RoundProgress = BuildRoundProgressText();
            var elapsedSec = Math.Max(1.0, (DateTime.UtcNow - _perfRunStartedUtc).TotalSeconds);
            var total = row.Ok + row.Fail;
            row.ThroughputPerSecond = (total / elapsedSec).ToString("0.00", CultureInfo.InvariantCulture);
            row.ErrorRate = total <= 0
                ? "0.00%"
                : ((100.0 * row.Fail) / total).ToString("0.00", CultureInfo.InvariantCulture) + "%";
            row.LastDetail = snapshot.Detail ?? snapshot.Stage ?? string.Empty;

            if (_gridPerfRuntime != null)
                _gridPerfRuntime.Refresh();
        }

        private string BuildRoundProgressText()
        {
            var total = Math.Max(1, _perfExpectedRounds);
            var current = Math.Max(_perfCompletedRounds, _perfCurrentRound);
            if (current <= 0)
                current = _perfCompletedRounds;
            current = Math.Max(0, Math.Min(total, current));
            return $"{current}/{total}";
        }

        private void UpdateRuntimeRoundProgressForAllRows()
        {
            var text = BuildRoundProgressText();
            foreach (var row in _perfRuntimeRows)
            {
                if (row != null)
                    row.RoundProgress = text;
            }
            _gridPerfRuntime?.Refresh();
        }

        private void SavePerformanceResult(
            NBomberExecutionPlan plan,
            NBomberExecutionResult result,
            IReadOnlyCollection<PerformanceRequestRecord> requests,
            HashSet<string> targetGroups)
        {
            try
            {
                var configuredRoot = _settings?.DataRootFolder;
                var dataRoot = string.IsNullOrWhiteSpace(configuredRoot)
                    ? Path.Combine(DataPathHelper.GetAssemblyBaseDirectory(), "data")
                    : configuredRoot.Trim();
                if (!string.Equals(Path.GetFileName(dataRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "data", StringComparison.OrdinalIgnoreCase))
                    dataRoot = Path.Combine(dataRoot, "data");
                var folder = Path.Combine(dataRoot, "performance");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, "result.json");
                var transactionsConfigured = plan?.Transactions == null
                    ? new List<object>()
                    : plan.Transactions
                        .Select(t => (object)new
                        {
                            name = t.Name,
                            enabled = t.Enabled,
                            usersOverride = t.SimulatedUsersOverride,
                            durationOverrideSec = t.DurationOverride.HasValue ? (int?)Math.Max(1, (int)t.DurationOverride.Value.TotalSeconds) : null
                        })
                        .ToList();

                var payload = new
                {
                    generatedAtUtc = DateTime.UtcNow,
                    success = result?.Success ?? false,
                    message = result?.Message ?? string.Empty,
                    startedUtc = result?.StartedUtc,
                    completedUtc = result?.CompletedUtc,
                    totalOk = result?.TotalOk ?? 0,
                    totalFail = result?.TotalFail ?? 0,
                    simUsers = result?.SimulatedUsers ?? GetSelectedPerfUsersCount(),
                    durationSec = Math.Max(1, _perfDurationSeconds),
                    selectedAnchorGroups = targetGroups == null ? new string[0] : targetGroups.OrderBy(x => x).ToArray(),
                    executedTransactions = result?.ExecutedTransactions ?? new List<string>(),
                    runtime = _perfRuntimeRows.Select(r => new
                    {
                        transaction = r.Transaction,
                        ok = r.Ok,
                        fail = r.Fail,
                        throughputPerSecond = r.ThroughputPerSecond,
                        errorRate = r.ErrorRate,
                        lastDetail = r.LastDetail
                    }).ToList(),
                    requestCount = requests?.Count ?? 0,
                    transactionsConfigured = transactionsConfigured
                };

                File.WriteAllText(path, JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8);
                SetStatus((result?.Message ?? "NBomber performance run completed.") + " Result saved: " + path);
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "SavePerformanceResult failed.");
            }
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
            toolPerf.Font = uiFont;
            statusMain.Font = uiFont;
            tabMain.Font = uiFont;
            BackColor = workspace;
            menuMain.BackColor = Color.White;
            menuMain.RenderMode = ToolStripRenderMode.Professional;
            toolMain.BackColor = Color.FromArgb(252, 252, 254);
            toolPerf.BackColor = Color.FromArgb(252, 252, 254);
            toolMain.ForeColor = ink;
            toolPerf.ForeColor = ink;
            toolMain.GripStyle = ToolStripGripStyle.Hidden;
            toolPerf.GripStyle = ToolStripGripStyle.Hidden;
            toolMain.ImageScalingSize = new Size(22, 22);
            toolPerf.ImageScalingSize = new Size(22, 22);
            toolMain.Padding = new Padding(6, 4, 8, 4);
            toolPerf.Padding = new Padding(6, 4, 8, 4);
            toolMain.Stretch = false;
            toolPerf.Stretch = false;
            toolMain.AutoSize = false;
            toolPerf.AutoSize = false;
            toolMain.Height = Math.Max(32, toolMain.ImageScalingSize.Height + toolMain.Padding.Vertical + 6);
            toolPerf.Height = Math.Max(32, toolPerf.ImageScalingSize.Height + toolPerf.Padding.Vertical + 6);
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
                g.ScrollBars = ScrollBars.Both;
                g.AutoSizeColumnsMode = ReferenceEquals(g, gridSteps)
                    ? DataGridViewAutoSizeColumnsMode.None
                    : DataGridViewAutoSizeColumnsMode.Fill;
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
            if (_txtPerformanceFilterTokens != null)
                _txtPerformanceFilterTokens.Text = NormalizePerformanceFilterTokens(_settings.PerformanceFilterTokens);
            chkWithPerformanceTest.Checked = _settings.PerformancePanelEnabled;
            if (_tscbPerfUsers != null)
            {
                var target = Math.Max(1, _settings.PerformanceSimUserCount);
                var s = target.ToString(CultureInfo.InvariantCulture);
                if (_tscbPerfUsers.Items.IndexOf(s) < 0)
                    _tscbPerfUsers.Items.Add(s);
                _tscbPerfUsers.SelectedItem = s;
            }
            if (_settings.LastPerformanceRunDurationSeconds > 0)
                _perfDurationSeconds = _settings.LastPerformanceRunDurationSeconds;
            RefreshPerformanceFilterTokensFromSettings();
            UpdatePerformanceMenuState();
            ApplyPerformancePanelVisibility();
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
            _settings.PerformanceFilterTokens = NormalizePerformanceFilterTokens(_txtPerformanceFilterTokens?.Text);
            _settings.PerformancePanelEnabled = chkWithPerformanceTest.Checked;
            _settings.PerformanceSimUserCount = GetSelectedPerfUsersCount();
            RefreshPerformanceFilterTokensFromSettings();
        }

        private PerformanceRunUiOptions BuildPerformanceRunDialogSeed(int usersPre)
        {
            var dur = _settings.LastPerformanceRunDurationSeconds > 0
                ? _settings.LastPerformanceRunDurationSeconds
                : Math.Max(1, _perfDurationSeconds);
            var chart = _settings.LastPerformanceRunChartIntervalSeconds > 0
                ? _settings.LastPerformanceRunChartIntervalSeconds
                : 3;
            return new PerformanceRunUiOptions
            {
                ConcurrencyMode = string.IsNullOrWhiteSpace(_settings.LastPerformanceRunMode) ? "constant" : _settings.LastPerformanceRunMode,
                SimulatedUsers = _settings.LastPerformanceRunUsers > 0 ? _settings.LastPerformanceRunUsers : usersPre,
                InitialUsers = _settings.LastPerformanceRunInitialUsers > 0 ? _settings.LastPerformanceRunInitialUsers : usersPre,
                UsersStep = _settings.LastPerformanceRunUsersStep > 0 ? _settings.LastPerformanceRunUsersStep : 5,
                DurationSeconds = dur,
                ChartSampleIntervalSeconds = chart,
                SaveResponseBodies = _settings.LastPerformanceRunSaveResponses,
                ResponseBodyMustContain = _settings.LastPerformanceRunBodyMustContain ?? string.Empty
            };
        }

        private void PersistLastPerformanceRunFromDialog(PerformanceRunUiOptions runOpt)
        {
            if (_settings == null || runOpt == null)
                return;
            _settings.LastPerformanceRunUsers = Math.Max(1, runOpt.SimulatedUsers);
            _settings.LastPerformanceRunMode = string.IsNullOrWhiteSpace(runOpt.ConcurrencyMode) ? "constant" : runOpt.ConcurrencyMode.Trim().ToLowerInvariant();
            _settings.LastPerformanceRunInitialUsers = Math.Max(1, runOpt.InitialUsers);
            _settings.LastPerformanceRunUsersStep = Math.Max(1, runOpt.UsersStep);
            _settings.LastPerformanceRunDurationSeconds = Math.Max(1, runOpt.DurationSeconds);
            _settings.LastPerformanceRunChartIntervalSeconds = Math.Max(1, runOpt.ChartSampleIntervalSeconds);
            _settings.LastPerformanceRunSaveResponses = runOpt.SaveResponseBodies;
            _settings.LastPerformanceRunBodyMustContain = runOpt.ResponseBodyMustContain ?? string.Empty;
            try
            {
                _settingsStore.Save(_settings);
            }
            catch (Exception ex)
            {
                FormLog.Warn(ex, "Could not persist last performance run options.");
            }
        }

        private static List<int> BuildPerfUsersSchedule(PerformanceRunUiOptions opt)
        {
            var list = new List<int>();
            if (opt == null)
                return list;
            var total = Math.Max(1, opt.SimulatedUsers);
            if (!string.Equals(opt.ConcurrencyMode, "stepped", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(total);
                return list;
            }

            var current = Math.Max(1, opt.InitialUsers);
            var step = Math.Max(1, opt.UsersStep);
            while (current < total)
            {
                list.Add(current);
                checked { current += step; }
            }
            if (list.Count == 0 || list[list.Count - 1] != total)
                list.Add(total);
            return list;
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
                if (string.Equals(e.Step.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.Step.SourceEvent, "update", StringComparison.OrdinalIgnoreCase))
                {
                    for (var i = _steps.Count - 1; i >= 0; i--)
                    {
                        var s = _steps[i];
                        if (!string.Equals(s.Keyword, "PegwindowMove", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (!string.Equals(s.RecordedPageUrl ?? string.Empty, e.Step.RecordedPageUrl ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                            continue;
                        s.TimestampUtc = DateTime.UtcNow;
                        s.Data = e.Step.Data;
                        s.Parameter = e.Step.Parameter;
                        s.RecordedPageTitle = e.Step.RecordedPageTitle;
                        if (gridSteps.IsHandleCreated)
                            gridSteps.Refresh();
                        SetStatus("Recorded update: " + e.Step.Keyword);
                        return;
                    }
                }
                _steps.Add(e.Step);
                if (IsLikelyUiActionStep(e.Step))
                    _lastRecordedUiStep = e.Step;
                SetStatus("Recorded: " + e.Step.Keyword);
            }

            Add();
            RefreshRecordReplayCanvas();
            PushStepToSidebar(e.Step);
            if (ShouldSyncFocusedElement())
                SyncTreeSelectionFromPick(e.Step, highlightPage: false);
        }

        private void Network_EntryCompleted(object sender, NetworkCaptureService.NetworkCaptureEntryEventArgs e)
        {
            if (e?.Entry == null)
                return;
            if (!IsPerformanceTestEnabled())
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, NetworkCaptureService.NetworkCaptureEntryEventArgs>(Network_EntryCompleted), sender, e);
                return;
            }

            var id = e.Entry.Id ?? Guid.NewGuid().ToString("N");
            var perf = _performanceRequests.FirstOrDefault(x => string.Equals(x?.Id, id, StringComparison.Ordinal));
            var isNew = perf == null;
            if (isNew)
            {
                perf = new PerformanceRequestRecord { Id = id };
                _performanceRequests.Add(perf);
            }

            perf.TimestampUtc = e.Entry.TimestampUtc;
            perf.Method = e.Entry.Method ?? string.Empty;
            perf.ResourceType = e.Entry.ResourceType ?? string.Empty;
            perf.Url = e.Entry.Url ?? string.Empty;
            perf.Parameter = BuildPerformanceParameterFromUrl(e.Entry.Url);
            perf.Headers = e.Entry.RequestHeaders == null ? string.Empty : string.Join("; ", e.Entry.RequestHeaders.Select(kv => kv.Key + "=" + kv.Value));
            perf.Cookies = e.Entry.CookiesSummary ?? string.Empty;
            perf.Payload = e.Entry.RequestBody ?? string.Empty;
            perf.Response = e.Entry.ResponseBody ?? string.Empty;
            perf.Status = e.Entry.Status;
            perf.FilterTag = InferPerformanceFilterTag(perf);
            perf.ReplayPolicy = BuildPerformanceReplayPolicy(perf);
            perf.ValidationHint = BuildPerformanceValidationHint(perf);
            perf.AnchorScore = ComputeAnchorScore(perf);
            perf.AnchorCandidate = perf.AnchorScore >= 5;
            if (isNew)
                perf.IsAnchorSelected = false;
            perf.AnchorGroup = InferAnchorGroup(perf);
            perf.CorrelationNeeded = NeedsCorrelation(perf);
            perf.CorrelationHint = perf.CorrelationNeeded ? "extract token/cookie and bind" : "none";
            if (isNew)
            {
                perf.Action = perf.AnchorCandidate
                    ? "Promote"
                    : (string.Equals(perf.FilterTag, "normal", StringComparison.OrdinalIgnoreCase) ? "Unlink" : "Ignore");
            }

            var linkStep = _lastRecordedUiStep ?? _steps.LastOrDefault();
            if (isNew && linkStep != null)
            {
                if (linkStep.PerformanceRequestRefs == null)
                    linkStep.PerformanceRequestRefs = new List<string>();
                linkStep.PerformanceRequestRefs.Add(perf.Id);
            }

            gridSteps.Refresh();
            BindPerformanceForSelectedStep();
            RefreshRecordReplayCanvas();
            SetStatus("Captured protocol: " + perf.Method + " " + perf.Url);
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
            return chkSyncFocus.Checked
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
                _performanceRequests.Clear();
                _performanceFilterTags.Clear();
                _lastRecordedUiStep = null;
                _network.Attach(_host.Page, _settings.PersistSensitiveHeaders);
                BindPerformanceForSelectedStep();
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
                _network.Attach(_host.Page, _settings.PersistSensitiveHeaders);
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

        private static ObjectTreeNodeDto CloneTreeNode(ObjectTreeNodeDto n)
        {
            if (n == null)
                return null;
            var c = new ObjectTreeNodeDto
            {
                Id = n.Id,
                ParentId = n.ParentId,
                DisplayName = n.DisplayName,
                Tag = n.Tag,
                Role = n.Role,
                LocatorHint = n.LocatorHint,
                Bounds = n.Bounds,
                InteractiveKind = n.InteractiveKind,
                ClassName = n.ClassName,
                NameAttr = n.NameAttr,
                Title = n.Title,
                Href = n.Href,
                InputType = n.InputType,
                Placeholder = n.Placeholder,
                AriaLabel = n.AriaLabel,
                AriaRole = n.AriaRole,
                TabIndexStr = n.TabIndexStr,
                Disabled = n.Disabled,
                ContentEditable = n.ContentEditable,
                TextPreview = n.TextPreview,
                PlaywrightLocator = n.PlaywrightLocator,
                HtmlId = n.HtmlId,
                AriaChecked = n.AriaChecked,
                AriaControls = n.AriaControls,
                AriaDescribedby = n.AriaDescribedby,
                AriaExpanded = n.AriaExpanded,
                AriaLabelledby = n.AriaLabelledby,
                AriaSelected = n.AriaSelected,
                Autocomplete = n.Autocomplete,
                Value = n.Value,
                Required = n.Required,
                Pattern = n.Pattern,
                ForAttr = n.ForAttr,
                Readonly = n.Readonly,
                Hidden = n.Hidden,
                DataAttributes = n.DataAttributes,
                Xpath = n.Xpath,
                OuterHtml = n.OuterHtml,
                PageInstanceId = n.PageInstanceId,
                FramePath = n.FramePath,
                RolePath = n.RolePath,
                HtmlTagPath = n.HtmlTagPath,
                IdPath = n.IdPath,
                NamePath = n.NamePath,
                TextPath = n.TextPath,
                Children = new List<ObjectTreeNodeDto>()
            };
            if (n.Children != null)
            {
                foreach (var ch in n.Children)
                    c.Children.Add(CloneTreeNode(ch));
            }
            return c;
        }

        private static void CollectIframeRoots(ObjectTreeNodeDto node, List<(ObjectTreeNodeDto FrameNode, ObjectTreeNodeDto FrameDocRoot)> acc)
        {
            if (node == null)
                return;
            var tag = (node.Tag ?? string.Empty).ToLowerInvariant();
            if ((tag == "iframe" || tag == "frame") && node.Children != null && node.Children.Count > 0)
            {
                var docRoot = node.Children[0];
                if (docRoot != null)
                    acc.Add((node, docRoot));
            }
            if (node.Children == null)
                return;
            foreach (var ch in node.Children)
                CollectIframeRoots(ch, acc);
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
                var frameRoots = new List<(ObjectTreeNodeDto FrameNode, ObjectTreeNodeDto FrameDocRoot)>();
                CollectIframeRoots(bodyRoot, frameRoots);
                if (pages.Count == 1)
                {
                    merged.Add(bodyRoot);
                    for (var fi = 0; fi < frameRoots.Count; fi++)
                    {
                        var fr = frameRoots[fi];
                        var frameUrl = fr.FrameNode?.Href ?? string.Empty;
                        var frameName = fr.FrameNode?.NameAttr ?? string.Empty;
                        var flabel = $"IFrame ({fi + 1}/{frameRoots.Count}): {TruncateTreeLabel(frameName, 40)} — {TruncateTreeLabel(frameUrl, 120)}";
                        var docClone = CloneTreeNode(fr.FrameDocRoot);
                        var wrapFrame = new ObjectTreeNodeDto
                        {
                            Id = "f" + i + "_" + fi + "_root",
                            ParentId = null,
                            DisplayName = flabel,
                            Tag = "BROWSER_IFRAME",
                            Role = string.Empty,
                            LocatorHint = string.Empty,
                            InteractiveKind = "container",
                            PageInstanceId = fr.FrameDocRoot?.PageInstanceId ?? string.Empty,
                            Children = new List<ObjectTreeNodeDto> { docClone }
                        };
                        if (docClone != null)
                            docClone.ParentId = wrapFrame.Id;
                        merged.Add(wrapFrame);
                    }
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
                for (var fi = 0; fi < frameRoots.Count; fi++)
                {
                    var fr = frameRoots[fi];
                    var frameUrl = fr.FrameNode?.Href ?? string.Empty;
                    var frameName = fr.FrameNode?.NameAttr ?? string.Empty;
                    var flabel = $"IFrame ({fi + 1}/{frameRoots.Count}): {TruncateTreeLabel(frameName, 40)} — {TruncateTreeLabel(frameUrl, 120)}";
                    var docClone = CloneTreeNode(fr.FrameDocRoot);
                    var frameWrap = new ObjectTreeNodeDto
                    {
                        Id = "w" + i + "_frame_" + fi,
                        ParentId = wrap.Id,
                        DisplayName = flabel,
                        Tag = "BROWSER_IFRAME",
                        Role = string.Empty,
                        LocatorHint = string.Empty,
                        InteractiveKind = "container",
                        PageInstanceId = fr.FrameDocRoot?.PageInstanceId ?? string.Empty,
                        Children = new List<ObjectTreeNodeDto> { docClone }
                    };
                    if (docClone != null)
                        docClone.ParentId = frameWrap.Id;
                    wrap.Children.Add(frameWrap);
                }
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
            var tag = (node?.Tag ?? string.Empty).ToUpperInvariant();
            if (tag == "BROWSER_PAGE")
                tn.ForeColor = Color.FromArgb(29, 78, 216);
            else if (tag == "BROWSER_IFRAME")
                tn.ForeColor = Color.FromArgb(109, 40, 217);
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
            if (!string.IsNullOrWhiteSpace(dto.FramePath))
                payload["framePath"] = dto.FramePath.Trim();
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

            // Outer HTML must stay at first row.
            AddPropRowOuterHtmlLast("Prop.OuterHtml", dto.OuterHtml);

            var props = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Prop.AriaChecked", dto.AriaChecked),
                new KeyValuePair<string, string>("Prop.AriaControls", dto.AriaControls),
                new KeyValuePair<string, string>("Prop.AriaDescribedby", dto.AriaDescribedby),
                new KeyValuePair<string, string>("Prop.AriaExpanded", dto.AriaExpanded),
                new KeyValuePair<string, string>("Prop.AriaLabel", dto.AriaLabel),
                new KeyValuePair<string, string>("Prop.AriaLabelledby", dto.AriaLabelledby),
                new KeyValuePair<string, string>("Prop.AriaSelected", dto.AriaSelected),
                new KeyValuePair<string, string>("Prop.Autocomplete", dto.Autocomplete),
                new KeyValuePair<string, string>("Prop.ClassName", dto.ClassName),
                new KeyValuePair<string, string>("Prop.ContentEditable", dto.ContentEditable),
                new KeyValuePair<string, string>("Prop.DataAttrs", dto.DataAttributes),
                new KeyValuePair<string, string>("Prop.Disabled", dto.Disabled),
                new KeyValuePair<string, string>("Prop.ForAttr", dto.ForAttr),
                new KeyValuePair<string, string>("Prop.Hidden", dto.Hidden),
                new KeyValuePair<string, string>("Prop.Href", dto.Href),
                new KeyValuePair<string, string>("Prop.HtmlId", dto.HtmlId),
                new KeyValuePair<string, string>("Prop.Id", dto.Id),
                new KeyValuePair<string, string>("Id Path", dto.IdPath),
                new KeyValuePair<string, string>("Prop.InteractiveKind", dto.InteractiveKind),
                new KeyValuePair<string, string>("Prop.Locator", locator),
                new KeyValuePair<string, string>("Prop.LocatorHint", dto.LocatorHint),
                new KeyValuePair<string, string>("HTML Tag Path", dto.HtmlTagPath),
                new KeyValuePair<string, string>("Name Path", dto.NamePath),
                new KeyValuePair<string, string>("Prop.NameAttr", dto.NameAttr),
                new KeyValuePair<string, string>("Prop.PageInstanceId", dto.PageInstanceId),
                new KeyValuePair<string, string>("Prop.Pattern", dto.Pattern),
                new KeyValuePair<string, string>("Prop.Placeholder", dto.Placeholder),
                new KeyValuePair<string, string>("Prop.Readonly", dto.Readonly),
                new KeyValuePair<string, string>("Role Path", dto.RolePath),
                new KeyValuePair<string, string>("Prop.Required", dto.Required),
                new KeyValuePair<string, string>("Prop.Role", dto.Role),
                new KeyValuePair<string, string>("Prop.TabIndex", dto.TabIndexStr),
                new KeyValuePair<string, string>("Prop.Tag", dto.Tag),
                new KeyValuePair<string, string>("Text Path", dto.TextPath),
                new KeyValuePair<string, string>("Prop.TextPreview", dto.TextPreview),
                new KeyValuePair<string, string>("Prop.Title", dto.Title),
                new KeyValuePair<string, string>("Prop.Type", dto.InputType),
                new KeyValuePair<string, string>("Prop.Value", dto.Value),
                new KeyValuePair<string, string>("Prop.Xpath", dto.Xpath)
            };
            if (dto.Bounds != null)
            {
                props.Add(new KeyValuePair<string, string>("Prop.X", dto.Bounds.X.ToString("0.##")));
                props.Add(new KeyValuePair<string, string>("Prop.Y", dto.Bounds.Y.ToString("0.##")));
                props.Add(new KeyValuePair<string, string>("Prop.W", dto.Bounds.Width.ToString("0.##")));
                props.Add(new KeyValuePair<string, string>("Prop.H", dto.Bounds.Height.ToString("0.##")));
            }

            foreach (var kv in props
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .OrderBy(kv =>
                {
                    var label = kv.Key.StartsWith("Prop.", StringComparison.Ordinal) ? L(kv.Key) : kv.Key;
                    return label ?? string.Empty;
                }, StringComparer.OrdinalIgnoreCase))
            {
                AddPropRow(kv.Key, kv.Value);
            }
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
                _network.Attach(_host.Page, _settings?.PersistSensitiveHeaders ?? false);
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
                _network.Attach(_host.Page, _settings?.PersistSensitiveHeaders ?? false);
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
            var response = latestReq?.ResponseBody ?? latestReq?.Status?.ToString() ?? string.Empty;
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
                ListenedRequestHeaders = reqHeaders + (string.IsNullOrWhiteSpace(latestReq?.RequestBody) ? string.Empty : " | payload=" + latestReq.RequestBody),
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
                _network.Attach(_host.Page, _settings.PersistSensitiveHeaders);
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
            BindPerformanceForSelectedStep();
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
            gridSteps.CellMouseClick -= gridSteps_CellMouseClick;
            gridSteps.CellPainting -= gridSteps_CellPainting;
            gridSteps.CellDoubleClick -= gridSteps_CellDoubleClick;
            gridSteps.CellEndEdit -= gridSteps_CellEndEdit;
            gridSteps.CellFormatting -= gridSteps_CellFormatting;
            gridSteps.SelectionChanged -= gridSteps_SelectionChanged;
            gridSteps.RowPrePaint -= gridSteps_RowPrePaint;
            gridSteps.DataError -= Grid_DataError;
            tabRecord.SizeChanged -= TabRecord_SizeChanged;
            _recording.RecordedStep -= Recording_RecordedStep;
            _recording.Picked -= Recording_Picked;
            _network.EntryCompleted -= Network_EntryCompleted;
            if (_gridPerformance != null)
            {
                _gridPerformance.CellContentClick -= gridPerformance_CellContentClick;
                _gridPerformance.CellDoubleClick -= gridPerformance_CellDoubleClick;
                _gridPerformance.RowPrePaint -= gridPerformance_RowPrePaint;
                _gridPerformance.DataError -= Grid_DataError;
            }
            if (_gridPerfRuntime != null)
            {
                _gridPerfRuntime.DataError -= Grid_DataError;
                _gridPerfRuntime.CellDoubleClick -= gridPerfRuntime_CellDoubleClick;
            }
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
            try { _gridActDeleteIcon?.Dispose(); } catch { }
            try { _gridActHighlightIcon?.Dispose(); } catch { }
            try { _gridActTestIcon?.Dispose(); } catch { }
            _gridActDeleteIcon = null;
            _gridActHighlightIcon = null;
            _gridActTestIcon = null;

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

        private sealed class PerfTransactionRuntimeRow
        {
            public string Transaction { get; set; }
            public long Ok { get; set; }
            public long Fail { get; set; }
            public long TotalRequest { get; set; }
            public long FinishedRequest { get; set; }
            public string RoundProgress { get; set; }
            public string ThroughputPerSecond { get; set; }
            public string ErrorRate { get; set; }
            public string LastDetail { get; set; }
        }

    }
}
