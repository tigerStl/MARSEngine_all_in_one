using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MARS.WebAutomation.UI
{
    /// <summary>Semi-transparent top-most countdown shown before single-step replay.</summary>
    internal sealed class ReplayCountdownOverlayForm : Form
    {
        private readonly Label _label;

        public ReplayCountdownOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 150);
            BackColor = Color.FromArgb(230, 30, 41, 59);
            Opacity = 0.9;
            TopMost = true;
            ShowInTaskbar = false;

            _label = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Point)
            };
            Controls.Add(_label);
        }

        public static async Task ShowCountdownAsync(Form owner, string title, string countLinePrefix, int seconds)
        {
            if (seconds <= 0)
                return;
            using (var f = new ReplayCountdownOverlayForm { Owner = owner })
            {
                f.Show(owner);
                try
                {
                    for (var t = seconds; t >= 1; t--)
                    {
                        f._label.Text = title + "\r\n\r\n" + countLinePrefix + " " + t;
                        await Task.Delay(1000).ConfigureAwait(true);
                    }
                }
                finally
                {
                    f.Close();
                }
            }
        }
    }
}
