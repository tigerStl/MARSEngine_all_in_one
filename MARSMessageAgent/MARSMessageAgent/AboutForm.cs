using System;
using System.Reflection;
using System.Windows.Forms;

namespace MARSMessageAgent
{
    /// <summary>
    /// About 对话框。
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            var asm = Assembly.GetExecutingAssembly();
            var title = "MARS Message Agent";
            var version = asm.GetName().Version?.ToString() ?? "1.0.0.0";
            var titleAttr = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
            var product = titleAttr?.Title ?? title;

            Text = "About...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new System.Drawing.Size(360, 160);
            ShowInTaskbar = false;

            var label = new Label
            {
                Text = product + "\r\nVersion " + version + "\r\n\r\nCOM+ Message Agent with WebSocket server.",
                AutoSize = false,
                Bounds = new System.Drawing.Rectangle(20, 20, 300, 80),
                Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9.5f)
            };
            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Bounds = new System.Drawing.Rectangle(240, 100, 80, 28)
            };
            ok.Click += (s, e) => Close();
            Controls.Add(label);
            Controls.Add(ok);
            AcceptButton = ok;
        }
    }
}
