using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using MARS.WebAutomation;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using Newtonsoft.Json;
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
            catch (ArgumentException)
            {
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

            lblRecordHint.Font = uiFont;
            lblRecordHint.ForeColor = ink;
            lblRecordHint.BackColor = Color.FromArgb(241, 245, 249);
            lblRecordHint.Padding = new Padding(10, 8, 10, 8);
            lblRecordHint.AutoSize = false;
            lblRecordHint.Dock = DockStyle.Top;
            lblRecordHint.Height = 36;

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

            WindowState = FormWindowState.Maximized;
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
            ApplySettingsToUi();
            txtUrl.TextChanged += (_, __) => UpdateUriLabels();
            UpdateUriLabels();
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
        }

        private void UpdateUriLabels()
        {
            if (!Uri.TryCreate(txtUrl.Text?.Trim(), UriKind.Absolute, out var uri))
            {
                lblScheme.Text = "Scheme: (invalid URL)";
                lblHost.Text = "Host:";
                lblPort.Text = "Port:";
                lblPath.Text = "Path:";
                lblQuery.Text = "Query:";
                return;
            }

            lblScheme.Text = "Scheme: " + uri.Scheme;
            lblHost.Text = "Host: " + uri.Host;
            lblPort.Text = uri.IsDefaultPort ? "Port: (default)" : "Port: " + uri.Port;
            lblPath.Text = "Path: " + uri.AbsolutePath;
            lblQuery.Text = "Query: " + (string.IsNullOrEmpty(uri.Query) ? "(none)" : uri.Query);
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

        private void Recording_RecordedStep(object sender, RecorderEventArgs e)
        {
            if (e?.Step == null)
                return;
            if (!tsbRecord.Checked)
                return;

            void Add()
            {
                _steps.Add(e.Step);
                SetStatus("Recorded: " + e.Step.Keyword);
            }

            if (InvokeRequired)
                BeginInvoke((Action)Add);
            else
                Add();
        }

        private void Recording_Picked(object sender, PickEventArgs e)
        {
            if (e?.Snapshot == null)
                return;

            void ShowPick()
            {
                ShowNodeDetails(e.Snapshot);
                SetStatus("Picked: " + e.Snapshot.Locator);
            }

            if (InvokeRequired)
                BeginInvoke((Action)ShowPick);
            else
                ShowPick();
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
                SetStatus("Browser ready.");
            }
            catch (Exception ex)
            {
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
                SetStatus("Loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Navigate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRefreshTree_Click(object sender, EventArgs e)
        {
            await PopulateObjectTreeFromPageAsync(showErrorDialog: true).ConfigureAwait(true);
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
                    var json = await _host.Page.EvaluateAsync<string>(PageInspectionScripts.BuildObjectTreeJson).ConfigureAwait(true);
                    var roots = JsonConvert.DeserializeObject<List<ObjectTreeNodeDto>>(json) ?? new List<ObjectTreeNodeDto>();
                    treeObjects.BeginUpdate();
                    treeObjects.Nodes.Clear();
                    foreach (var r in roots)
                        AddTreeNodeRecursive(treeObjects.Nodes, null, r);
                    treeObjects.ExpandAll();
                    treeObjects.EndUpdate();
                    SetStatus("Object tree updated.");
                }
                catch (Exception ex)
                {
                    if (showErrorDialog)
                        MessageBox.Show(this, ex.Message, "Object tree", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        SetStatus("Object tree failed: " + ex.Message);
                }
            }
        }

        private void AddTreeNodeRecursive(TreeNodeCollection roots, TreeNode parent, ObjectTreeNodeDto node)
        {
            var tn = new TreeNode(node.DisplayName ?? node.Tag) { Tag = node };
            if (parent == null)
                roots.Add(tn);
            else
                parent.Nodes.Add(tn);
            if (node.Children == null)
                return;
            foreach (var c in node.Children)
                AddTreeNodeRecursive(roots, tn, c);
        }

        private void treeObjects_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is ObjectTreeNodeDto dto)
                ShowObjectNode(dto);
        }

        private void ShowObjectNode(ObjectTreeNodeDto dto)
        {
            gridObjectProps.Rows.Clear();
            AddRow("Id", dto.Id);
            AddRow("Tag", dto.Tag);
            AddRow("Role", dto.Role);
            AddRow("Locator hint", dto.LocatorHint);
            if (dto.Bounds != null)
            {
                AddRow("X", dto.Bounds.X.ToString("0.##"));
                AddRow("Y", dto.Bounds.Y.ToString("0.##"));
                AddRow("W", dto.Bounds.Width.ToString("0.##"));
                AddRow("H", dto.Bounds.Height.ToString("0.##"));
            }
        }

        private void ShowNodeDetails(SemanticStepRecord step)
        {
            gridObjectProps.Rows.Clear();
            AddRow("Keyword", step.Keyword);
            AddRow("Locator", step.Locator);
            AddRow("Parameter", step.Parameter);
            AddRow("Data", step.Data);
            AddRow("Source event", step.SourceEvent);
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
                await SetRecorderModeAsync("pick").ConfigureAwait(true);
                SetStatus("Target pick: click an element in the page.");
            }
            else
                await SetRecorderModeAsync("off").ConfigureAwait(true);
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
                await SetRecorderModeAsync("record").ConfigureAwait(true);
                SetStatus("Recording…");
            }
            else
            {
                await SetRecorderModeAsync("off").ConfigureAwait(true);
                SetStatus("Record stopped.");
            }
        }

        private Task SetRecorderModeAsync(string mode)
        {
            return _recording.SetModeAsync(_host.Page, mode);
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
                    while (_steps.Count > 0)
                        _steps.RemoveAt(_steps.Count - 1);
                    foreach (var s in doc.Steps ?? Enumerable.Empty<SemanticStepRecord>())
                        _steps.Add(s);
                    if (doc.PageInfo != null && !string.IsNullOrEmpty(doc.PageInfo.OriginalUrl))
                        txtUrl.Text = doc.PageInfo.OriginalUrl;
                    UpdateUriLabels();
                    SetStatus("Imported " + _steps.Count + " steps.");
                }
                catch (Exception ex)
                {
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
            SetStatus("Settings saved.");
        }

        private void MainWorkbenchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _network.Dispose();
            _recording.RecordedStep -= Recording_RecordedStep;
            _recording.Picked -= Recording_Picked;
            try
            {
                _host.ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
                // ignore shutdown errors
            }

            ResetSingleton(this);
        }
    }
}
