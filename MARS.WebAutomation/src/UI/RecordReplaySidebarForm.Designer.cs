using System.Drawing;
using System.Windows.Forms;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class RecordReplaySidebarForm
    {
        private void InitializeComponent()
        {
            _web = new WebBrowser();
            SuspendLayout();
            //
            // _web
            //
            _web.AllowWebBrowserDrop = false;
            _web.Dock = DockStyle.Fill;
            _web.IsWebBrowserContextMenuEnabled = false;
            _web.Location = new Point(0, 0);
            _web.MinimumSize = new Size(20, 20);
            _web.Name = "_web";
            _web.ScriptErrorsSuppressed = true;
            _web.Size = new Size(300, 600);
            //
            // RecordReplaySidebarForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(300, 600);
            Controls.Add(_web);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "RecordReplaySidebarForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Record-Replay Events";
            TopMost = true;
            ResumeLayout(false);
        }
    }
}
