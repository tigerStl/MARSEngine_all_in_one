using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;

namespace MARS.WebAutomation.UI
{
    public partial class MainWorkbenchForm
    {
        private ObjectInspectPreviewPanel _objectPreview;
        private readonly List<TreeNode> _treeSearchMatches = new List<TreeNode>();
        private int _treeSearchIndex = -1;

        private string L(string key) => UiStrings.T(key, _settings?.UiLanguage ?? "en");

        private void SetupObjectPreviewAndToolbar()
        {
            _objectPreview = new ObjectInspectPreviewPanel
            {
                Name = "objectInspectPreview",
                Visible = true
            };
            _objectPreview.SetTitle(L("PreviewTitle"));
            splitObjects.Panel2.Controls.Add(_objectPreview);
            _objectPreview.BringToFront();
            splitObjects.Panel2.Resize += SplitObjects_Panel2_Resize;
            txtTreeSearch.KeyDown += TxtTreeSearch_KeyDown;
            PositionObjectPreview();
        }

        private void SplitObjects_Panel2_Resize(object sender, EventArgs e) => PositionObjectPreview();

        private void PositionObjectPreview()
        {
            if (_objectPreview == null || splitObjects.Panel2.IsDisposed || !_objectPreview.Visible)
                return;
            if (!_objectPreview.IsFloatingLayout)
                return;
            _objectPreview.AlignBottomRight(splitObjects.Panel2);
        }

        private void TxtTreeSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RunTreeSearch(resetIndex: true);
            }
        }

        private void menuHelpLangEnglish_Click(object sender, EventArgs e)
        {
            _settings.UiLanguage = "en";
            _settingsStore.Save(_settings);
            ApplyLocalizedUi();
        }

        private void menuHelpLangChinese_Click(object sender, EventArgs e)
        {
            _settings.UiLanguage = "zh";
            _settingsStore.Save(_settings);
            ApplyLocalizedUi();
        }

        private void ApplyLocalizedUi()
        {
            Text = L("AppTitle");
            menuFile.Text = L("File");
            menuFileSave.Text = L("Save");
            menuFileExport.Text = L("Export");
            menuFileImport.Text = L("Import");
            menuFileExit.Text = L("Exit");
            menuHelp.Text = L("Help");
            menuHelpLanguage.Text = L("Language");
            menuHelpLangEnglish.Text = L("English");
            menuHelpLangChinese.Text = L("Chinese");
            menuHelpAbout.Text = L("About");
            tabTarget.Text = L("Target");
            tabObjects.Text = L("Objects");
            tabRecord.Text = L("RecordReplay");
            tabSettings.Text = L("Settings");
            lblSectionUrl.Text = L("PageUrl");
            btnStartBrowser.Text = L("StartBrowser");
            btnNavigate.Text = L("Navigate");
            btnRefreshTree.Text = L("RefreshTree");
            lblRecordHint.Text = L("RecordHint");
            lblDataRoot.Text = L("DataRoot");
            chkHeadless.Text = L("Headless");
            lblTimeout.Text = L("Timeout");
            chkPersistHeaders.Text = L("PersistHeaders");
            lblChannel.Text = L("Channel");
            lblViewport.Text = L("Viewport");
            btnSaveSettings.Text = L("SaveSettings");
            tslBrand.Text = L("Brand");
            tsbTarget.Text = L("ToolbarTarget");
            tsbRecord.Text = L("ToolbarRecord");
            tsbReplay.Text = L("ToolbarReplay");
            tsbExport.Text = L("ToolbarExport");
            tsbImport.Text = L("ToolbarImport");
            tsbSave.Text = L("ToolbarSave");
            if (_tsbReloadEngine != null)
                _tsbReloadEngine.Text = L("ToolbarReloadEngine");
            if (_chkSyncFocus != null)
                _chkSyncFocus.Text = L("ToolbarSync");
            if (_lblIgnoredPagePrefixes != null)
                _lblIgnoredPagePrefixes.Text = L("SkipInjectPrefixes");
            if (_lblRecorderTabDepth != null)
                _lblRecorderTabDepth.Text = L("RecorderTabDepth");
            statusLabel.Text = L("Ready");
            if (gridObjectProps.Columns.Count >= 2)
            {
                gridObjectProps.Columns[0].HeaderText = L("PropColumn");
                gridObjectProps.Columns[1].HeaderText = L("ValueColumn");
            }

            ApplyStepsGridColumnHeaders();

            chkTreeRegex.AccessibleDescription = L("RegexMode");
            if (_objectPreview != null)
                _objectPreview.SetTitle(L("PreviewTitle"));
            ApplyRecordCanvasToolbarLocalization();
            UpdateUriLabels();
            ApplyWorkbenchChrome();
        }

        private void btnTreeSearchGo_Click(object sender, EventArgs e) => RunTreeSearch(resetIndex: true);

        private void btnTreeSearchPrev_Click(object sender, EventArgs e)
        {
            if (_treeSearchMatches.Count == 0)
                return;
            _treeSearchIndex--;
            if (_treeSearchIndex < 0)
                _treeSearchIndex = _treeSearchMatches.Count - 1;
            SelectTreeSearchMatch();
        }

        private void btnTreeSearchNext_Click(object sender, EventArgs e)
        {
            if (_treeSearchMatches.Count == 0)
                return;
            _treeSearchIndex++;
            if (_treeSearchIndex >= _treeSearchMatches.Count)
                _treeSearchIndex = 0;
            SelectTreeSearchMatch();
        }

        private void RunTreeSearch(bool resetIndex)
        {
            _treeSearchMatches.Clear();
            _treeSearchIndex = -1;
            var q = txtTreeSearch.Text?.Trim() ?? string.Empty;
            if (q.Length == 0)
            {
                SetStatus(L("Ready"));
                return;
            }

            Regex rx = null;
            if (chkTreeRegex.Checked)
            {
                try
                {
                    rx = new Regex(q, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                catch (ArgumentException)
                {
                    FormLog.Warn("Invalid regex input for tree search: {Query}", q);
                    MessageBox.Show(this, L("BadRegex"), L("Objects"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    if (TreeNodeMatchesSearch(n, q, rx))
                        _treeSearchMatches.Add(n);
                    if (n.Nodes.Count > 0)
                        Walk(n.Nodes);
                }
            }

            Walk(treeObjects.Nodes);
            if (_treeSearchMatches.Count == 0)
            {
                SetStatus(L("SearchNoMatch"));
                return;
            }

            _treeSearchIndex = resetIndex ? 0 : Math.Min(Math.Max(0, _treeSearchIndex), _treeSearchMatches.Count - 1);
            SelectTreeSearchMatch();
            SetStatus(string.Format(L("SearchMatchCount"), _treeSearchMatches.Count));
        }

        private static bool TreeNodeMatchesSearch(TreeNode n, string q, Regex rx)
        {
            var dto = n.Tag as ObjectTreeNodeDto;
            var blob = (n.Text ?? string.Empty) + "\n";
            if (dto != null)
            {
                blob += (dto.DisplayName ?? "") + "\n" + (dto.LocatorHint ?? "") + "\n" + (dto.Tag ?? "") + "\n"
                    + (dto.ClassName ?? "") + "\n" + (dto.Role ?? "") + "\n" + (dto.TextPreview ?? "") + "\n"
                    + (dto.AriaLabel ?? "") + "\n" + (dto.Title ?? "") + "\n" + (dto.Href ?? "") + "\n"
                    + (dto.NameAttr ?? "") + "\n" + (dto.PlaywrightLocator ?? "") + "\n" + (dto.HtmlId ?? "") + "\n"
                    + (dto.Xpath ?? "") + "\n" + (dto.Value ?? "") + "\n" + (dto.DataAttributes ?? "") + "\n"
                    + (dto.AriaChecked ?? "") + "\n" + (dto.AriaControls ?? "") + "\n" + (dto.AriaDescribedby ?? "")
                    + "\n" + (dto.AriaExpanded ?? "") + "\n" + (dto.AriaLabelledby ?? "") + "\n" + (dto.AriaSelected ?? "")
                    + "\n" + (dto.ForAttr ?? "") + "\n" + (dto.Placeholder ?? "");
            }

            if (rx != null)
                return rx.IsMatch(blob);
            return blob.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectTreeSearchMatch()
        {
            if (_treeSearchIndex < 0 || _treeSearchIndex >= _treeSearchMatches.Count)
                return;
            var n = _treeSearchMatches[_treeSearchIndex];
            treeObjects.SelectedNode = n;
            n.EnsureVisible();
        }
    }
}
