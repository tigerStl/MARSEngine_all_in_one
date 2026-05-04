using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceRuntimeReportForm
    {
        private void InitializeComponent()
        {
            _topBar = new Panel();
            _btnExportHtml = new Button();
            _btnExportJson = new Button();
            _webView = new WebView2();
            _topBar.SuspendLayout();
            SuspendLayout();
            //
            // _topBar
            //
            _topBar.BackColor = Color.FromArgb(248, 250, 252);
            _topBar.Controls.Add(_btnExportHtml);
            _topBar.Controls.Add(_btnExportJson);
            _topBar.Dock = DockStyle.Top;
            _topBar.Location = new Point(0, 0);
            _topBar.Name = "_topBar";
            _topBar.Padding = new Padding(8, 6, 8, 6);
            _topBar.Size = new Size(1280, 40);
            //
            // _btnExportHtml
            //
            _btnExportHtml.Dock = DockStyle.Right;
            _btnExportHtml.Location = new Point(1162, 6);
            _btnExportHtml.Name = "_btnExportHtml";
            _btnExportHtml.Size = new Size(110, 28);
            _btnExportHtml.Text = "Export HTML";
            _btnExportHtml.UseVisualStyleBackColor = true;
            //
            // _btnExportJson
            //
            _btnExportJson.Dock = DockStyle.Right;
            _btnExportJson.Location = new Point(1052, 6);
            _btnExportJson.Name = "_btnExportJson";
            _btnExportJson.Size = new Size(110, 28);
            _btnExportJson.Text = "Export JSON";
            _btnExportJson.UseVisualStyleBackColor = true;
            //
            // _webView
            //
            _webView.CreationProperties = null;
            _webView.DefaultBackgroundColor = Color.White;
            _webView.Dock = DockStyle.Fill;
            _webView.Location = new Point(0, 40);
            _webView.Name = "_webView";
            _webView.Size = new Size(1280, 780);
            _webView.TabIndex = 1;
            _webView.ZoomFactor = 1D;
            //
            // PerformanceRuntimeReportForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 820);
            Controls.Add(_webView);
            Controls.Add(_topBar);
            Name = "PerformanceRuntimeReportForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Performance runtime report";
            _topBar.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
