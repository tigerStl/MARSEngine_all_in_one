using System.Drawing;
using System.Windows.Forms;

// Usage: HighlightOverlay.exe <x> <y> <width> <height>
// All values in pixels. x, y = screen position (absolute); width, height = size.
// Draws a red border at that position and flashes 3 times.

namespace HighlightOverlay;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length < 4) return;
        if (!int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y) ||
            !int.TryParse(args[2], out int w) || !int.TryParse(args[3], out int h))
            return;
        if (w <= 0 || h <= 0) return;

        // Use physical pixels (1:1) so size matches Java AWT getLocationOnScreen/getSize.
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(x, y),
            Size = new Size(w, h),
            TopMost = true,
            ShowInTaskbar = false,
            BackColor = Color.Magenta,
            TransparencyKey = Color.Magenta
        };

        form.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.Red, 3);
            e.Graphics.DrawRectangle(pen, 1, 1, form.ClientSize.Width - 2, form.ClientSize.Height - 2);
        };

        int flashCount = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += (s, e) =>
        {
            if (flashCount < 6)
            {
                form.Visible = (flashCount % 2) == 0;
                flashCount++;
                timer.Interval = form.Visible ? 200 : 150;
            }
            else
            {
                timer.Stop();
                form.Visible = true;
                var closeTimer = new System.Windows.Forms.Timer { Interval = 300 };
                closeTimer.Tick += (_, _) => { closeTimer.Stop(); form.Close(); };
                closeTimer.Start();
            }
        };
        form.Shown += (s, e) => { flashCount = 0; timer.Start(); };

        Application.Run(form);
    }
}
