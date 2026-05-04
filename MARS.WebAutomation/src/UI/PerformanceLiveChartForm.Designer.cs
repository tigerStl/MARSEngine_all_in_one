using System.Drawing;
using System.Windows.Forms;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceLiveChartForm
    {
        private Panel _chartScrollHost;
        private Panel _chartPanel;

        private void InitializeComponent()
        {
            _chartScrollHost = new Panel();
            _chartPanel = new Panel();
            SuspendLayout();
            //
            // _chartScrollHost
            //
            _chartScrollHost.AutoScroll = true;
            _chartScrollHost.BackColor = Color.White;
            _chartScrollHost.Dock = DockStyle.Fill;
            _chartScrollHost.Controls.Add(_chartPanel);
            //
            // _chartPanel
            //
            _chartPanel.BackColor = Color.White;
            _chartPanel.Location = new Point(0, 0);
            _chartPanel.Size = new Size(900, 480);
            _chartPanel.Paint += ChartPanel_Paint;
            //
            // PerformanceLiveChartForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 480);
            Controls.Add(_chartScrollHost);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "PerformanceLiveChartForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Performance live chart";
            ResumeLayout(false);
        }
    }
}
