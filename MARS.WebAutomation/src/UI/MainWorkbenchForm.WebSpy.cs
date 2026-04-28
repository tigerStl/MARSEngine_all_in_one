using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using MARS.WebAutomation;
using MARS.WebAutomation.Models;
using NLog;

namespace MARS.WebAutomation.UI
{
    public partial class MainWorkbenchForm
    {
        private static readonly Logger WebSpyLog = LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".UI.MainWorkbenchForm.WebSpy");
        private WebSpyPickContext _queuedWebSpy;

        /// <summary>
        /// Called before the form is shown (e.g. from <see cref="WebAutomationApp.ShowWorkbenchForWebSpy"/>).
        /// </summary>
        public void QueueWebSpyIntegration(WebSpyPickContext context)
        {
            var ctx = context ?? throw new ArgumentNullException(nameof(context));
            _queuedWebSpy = ctx;
            if (IsHandleCreated && Visible)
            {
                TriggerWebSpyIntegration(ctx);
                return;
            }
            Shown -= MainWorkbenchForm_WebSpyOnShown;
            Shown += MainWorkbenchForm_WebSpyOnShown;
        }

        private void MainWorkbenchForm_WebSpyOnShown(object sender, EventArgs e)
        {
            Shown -= MainWorkbenchForm_WebSpyOnShown;
            var ctx = _queuedWebSpy;
            _queuedWebSpy = null;
            if (ctx == null)
                return;
            TriggerWebSpyIntegration(ctx);
        }

        private async void TriggerWebSpyIntegration(WebSpyPickContext ctx)
        {
            try
            {
                await RunWebSpyIntegrationAsync(ctx).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                WebSpyLog.Error(ex, "TriggerWebSpyIntegration failed.");
                MessageBox.Show(this, ex.Message, "Web spy integration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task RunWebSpyIntegrationAsync(WebSpyPickContext ctx)
        {
            using (WebAutomationMethodTrace.Begin(WebSpyLog, nameof(RunWebSpyIntegrationAsync),
                (nameof(ctx.AutomationId), ctx?.AutomationId),
                (nameof(ctx.UiName), ctx?.UiName),
                (nameof(ctx.ClassName), ctx?.ClassName)))
            {
                _settings = _settingsStore.Load();
                ApplySettingsToUi();

                SetStatus("Connecting to browser (CDP)…");
                var endpoints = new[]
                {
                    "http://127.0.0.1:9222",
                    "http://127.0.0.1:9223"
                };
                var attachResult = await _host.TryConnectChromiumOverCdpAsync(_settings, endpoints, ctx).ConfigureAwait(true);
                if (!attachResult.Success)
                {
                    var detail = string.IsNullOrWhiteSpace(attachResult.ErrorMessage)
                        ? GetText("WebSpy.CdpFallback.Message")
                        : GetText("WebSpy.CdpFallback.Message") + "\r\n\r\n—\r\n\r\n" + attachResult.ErrorMessage.Trim();
                    SetStatus("CDP attach failed.");
                    WebSpyLog.Warn("CDP attach failed: {Error}", attachResult.ErrorMessage);
                    MessageBox.Show(
                        this,
                        detail,
                        GetText("WebSpy.CdpFallback.Title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Close();
                    return;
                }
                else
                {
                    SetStatus("Attached via CDP.");
                }

                if (_host.Page != null)
                {
                    await _recording.InstallAsync(_host.Page, _settings).ConfigureAwait(true);
                    txtUrl.Text = _host.Page.Url ?? string.Empty;
                    UpdateUriLabels();
                }

                tabMain.SelectedIndex = 0;
                Application.DoEvents();

                tabMain.SelectedIndex = 1;
                await PopulateObjectTreeFromPageAsync(showErrorDialog: false).ConfigureAwait(true);
                SelectBestTreeNodeForWebSpy(ctx.AutomationId, ctx.UiName, ctx.ClassName);
                SetStatus("Web spy: object tree ready.");
            }
        }

        private void SelectBestTreeNodeForWebSpy(string automationId, string uiName, string className)
        {
            using (WebAutomationMethodTrace.Begin(WebSpyLog, nameof(SelectBestTreeNodeForWebSpy),
                (nameof(automationId), automationId),
                (nameof(uiName), uiName),
                (nameof(className), className)))
            {
                TreeNode best = null;
                var bestScore = 0;
                Walk(treeObjects.Nodes);
                if (best != null)
                {
                    treeObjects.SelectedNode = best;
                    best.EnsureVisible();
                    if (best.Tag is ObjectTreeNodeDto dto)
                        ShowObjectNode(dto);
                }

                void Walk(TreeNodeCollection nodes)
                {
                    foreach (TreeNode n in nodes)
                    {
                        if (n.Tag is ObjectTreeNodeDto dto)
                        {
                            var sc = ScoreMatch(dto, automationId, uiName, className);
                            if (sc > bestScore)
                            {
                                bestScore = sc;
                                best = n;
                            }
                        }
                        if (n.Nodes.Count > 0)
                            Walk(n.Nodes);
                    }
                }
            }
        }

        private static int ScoreMatch(ObjectTreeNodeDto dto, string automationId, string uiName, string className)
        {
            using (WebAutomationMethodTrace.Begin(WebSpyLog, nameof(ScoreMatch),
                ("dto.DisplayName", dto?.DisplayName),
                ("dto.Tag", dto?.Tag),
                (nameof(automationId), automationId),
                (nameof(uiName), uiName),
                (nameof(className), className)))
            {
                var score = 0;
                var loc = dto.LocatorHint ?? string.Empty;
                var disp = dto.DisplayName ?? string.Empty;
                var tag = dto.Tag ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(automationId))
                {
                    if (loc.IndexOf(automationId, StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 12;
                    if (disp.IndexOf(automationId, StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 8;
                }

                if (!string.IsNullOrWhiteSpace(uiName) && uiName.Length > 1)
                {
                    if (disp.IndexOf(uiName, StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 6;
                }

                if (!string.IsNullOrWhiteSpace(className)
                    && tag.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 3;
                }

                return score;
            }
        }

        private string GetText(string key)
        {
            using (WebAutomationMethodTrace.Begin(WebSpyLog, nameof(GetText), (nameof(key), key)))
            {
                var lang = (System.Configuration.ConfigurationManager.AppSettings["UiLanguage"] ?? "en").Trim();
                var isZh = lang.IndexOf("zh", StringComparison.OrdinalIgnoreCase) == 0;

                if (isZh)
                {
                    switch (key)
                    {
                        case "WebSpy.CdpFallback.Title":
                            return "Web 调试端口";
                        case "WebSpy.CdpFallback.Message":
                            return "无法通过 CDP 连接到已打开的浏览器（未检测到 9222/9223 调试端口）。\r\n\r\n请用远程调试参数启动 Chrome/Edge 后重试；SpyTool 将回退到桌面 UIA 拾取。";
                    }
                }

                switch (key)
                {
                    case "WebSpy.CdpFallback.Title":
                        return "Web debug port";
                    case "WebSpy.CdpFallback.Message":
                        return "Could not attach to your existing browser via CDP (no response on 9222/9223).\r\n\r\nStart Chrome/Edge with remote debugging, then try again; SpyTool falls back to desktop UIA picking.";
                    default:
                        return key;
                }
            }
        }
    }
}
