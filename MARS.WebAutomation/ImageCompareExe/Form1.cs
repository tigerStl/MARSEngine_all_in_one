namespace ImageCompareExe;

public partial class Form1 : Form
{
    private Bitmap? _imageA;
    private Bitmap? _imageB;
    private readonly List<DiffMarker> _markers = new();
    private bool _draggingSplit;

    public Form1()
    {
        InitializeComponent();
        DoubleBuffered = true;
        canvasPanel.DoubleBuffered(true);
    }

    private sealed record DiffMarker(float X, float Y, float Radius, int Area);

    private void btnLoadA_Click(object sender, EventArgs e)
    {
        var bmp = LoadBitmapFromDialog();
        if (bmp == null) return;
        _imageA?.Dispose();
        _imageA = bmp;
        _markers.Clear();
        canvasPanel.Invalidate();
    }

    private void btnLoadB_Click(object sender, EventArgs e)
    {
        var bmp = LoadBitmapFromDialog();
        if (bmp == null) return;
        _imageB?.Dispose();
        _imageB = bmp;
        _markers.Clear();
        canvasPanel.Invalidate();
    }

    private void btnSwap_Click(object sender, EventArgs e)
    {
        (_imageA, _imageB) = (_imageB, _imageA);
        _markers.Clear();
        canvasPanel.Invalidate();
    }

    private void btnMark_Click(object sender, EventArgs e)
    {
        if (_imageA == null || _imageB == null)
        {
            MessageBox.Show(this, "Load both Image A and Image B first.", "Image Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            _markers.Clear();
            _markers.AddRange(DetectTextFocusedDiffs(_imageA, _imageB, 30));
            canvasPanel.Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Mark Diff Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void trackSplit_Scroll(object sender, EventArgs e)
    {
        lblSplit.Text = $"Split: {trackSplit.Value}%";
        canvasPanel.Invalidate();
    }

    private void canvasPanel_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _draggingSplit = true;
        SetSplitFromMouseX(e.X);
    }

    private void canvasPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_draggingSplit) return;
        SetSplitFromMouseX(e.X);
    }

    private void canvasPanel_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _draggingSplit = false;
    }

    private void SetSplitFromMouseX(int x)
    {
        if (canvasPanel.ClientSize.Width <= 1) return;
        var value = (int)Math.Round(100.0 * x / canvasPanel.ClientSize.Width);
        value = Math.Max(0, Math.Min(100, value));
        trackSplit.Value = value;
        lblSplit.Text = $"Split: {value}%";
        canvasPanel.Invalidate();
    }

    private void canvasPanel_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.Black);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = canvasPanel.ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var splitX = (int)Math.Round(rect.Width * (trackSplit.Value / 100.0));
        splitX = Math.Max(rect.Left, Math.Min(rect.Right, splitX));

        if (_imageB != null)
            DrawImageContain(g, _imageB, rect);

        if (_imageA != null)
        {
            var state = g.Save();
            g.SetClip(new Rectangle(rect.Left, rect.Top, Math.Max(0, splitX - rect.Left), rect.Height));
            DrawImageContain(g, _imageA, rect);
            g.Restore(state);
        }

        using var dividerPen = new Pen(Color.Cyan, 2f);
        g.DrawLine(dividerPen, splitX, rect.Top, splitX, rect.Bottom);

        DrawMarkers(g, rect);
    }

    private void DrawMarkers(Graphics g, Rectangle rect)
    {
        if (_markers.Count == 0) return;
        using var stroke = new Pen(Color.Red, 2f);
        using var fill = new SolidBrush(Color.FromArgb(36, 255, 59, 48));
        foreach (var m in _markers)
        {
            var cx = rect.Left + m.X * rect.Width;
            var cy = rect.Top + m.Y * rect.Height;
            var r = Math.Max(10f, m.Radius * rect.Width);
            var rr = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            g.FillEllipse(fill, rr);
            g.DrawEllipse(stroke, rr);
        }
    }

    private static void DrawImageContain(Graphics g, Bitmap image, Rectangle rect)
    {
        var scale = Math.Min(rect.Width / (float)image.Width, rect.Height / (float)image.Height);
        var w = image.Width * scale;
        var h = image.Height * scale;
        var x = rect.Left + (rect.Width - w) / 2f;
        var y = rect.Top + (rect.Height - h) / 2f;
        g.DrawImage(image, x, y, w, h);
    }

    private static Bitmap? LoadBitmapFromDialog()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif|All Files|*.*",
            Multiselect = false
        };
        if (ofd.ShowDialog() != DialogResult.OK)
            return null;
        using var src = new Bitmap(ofd.FileName);
        return new Bitmap(src);
    }

    private static IEnumerable<DiffMarker> DetectTextFocusedDiffs(Bitmap a, Bitmap b, int maxMarkers)
    {
        using var aMatRaw = OpenCvSharp.Extensions.BitmapConverter.ToMat(a);
        using var bMatRaw = OpenCvSharp.Extensions.BitmapConverter.ToMat(b);

        const int compareW = 1280;
        const int compareH = 720;
        using var aMat = FitContain(aMatRaw, compareW, compareH);
        using var bMat = FitContain(bMatRaw, compareW, compareH);

        using var aGray = new OpenCvSharp.Mat();
        using var bGray = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.CvtColor(aMat, aGray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
        OpenCvSharp.Cv2.CvtColor(bMat, bGray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
        OpenCvSharp.Cv2.GaussianBlur(aGray, aGray, new OpenCvSharp.Size(3, 3), 0);
        OpenCvSharp.Cv2.GaussianBlur(bGray, bGray, new OpenCvSharp.Size(3, 3), 0);

        using var aEdges = new OpenCvSharp.Mat();
        using var bEdges = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Canny(aGray, aEdges, 50, 150);
        OpenCvSharp.Cv2.Canny(bGray, bEdges, 50, 150);

        using var diff = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Absdiff(aEdges, bEdges, diff);
        OpenCvSharp.Cv2.Threshold(diff, diff, 35, 255, OpenCvSharp.ThresholdTypes.Binary);
        using var kernel = OpenCvSharp.Cv2.GetStructuringElement(OpenCvSharp.MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
        OpenCvSharp.Cv2.Dilate(diff, diff, kernel, iterations: 1);
        OpenCvSharp.Cv2.MorphologyEx(diff, diff, OpenCvSharp.MorphTypes.Close, kernel, iterations: 1);

        using var labels = new OpenCvSharp.Mat();
        using var stats = new OpenCvSharp.Mat();
        using var centroids = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.ConnectedComponentsWithStats(
            diff,
            labels,
            stats,
            centroids,
            OpenCvSharp.PixelConnectivity.Connectivity8,
            OpenCvSharp.MatType.CV_32S);

        var list = new List<DiffMarker>();
        for (var i = 1; i < stats.Rows; i++)
        {
            var area = stats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Area);
            if (area < 28)
                continue;
            var w = stats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Width);
            var h = stats.Get<int>(i, (int)OpenCvSharp.ConnectedComponentsTypes.Height);
            if (w > diff.Width * 0.75 || h > diff.Height * 0.6)
                continue;

            var cx = (float)centroids.Get<double>(i, 0);
            var cy = (float)centroids.Get<double>(i, 1);
            var radiusPx = Math.Max(8f, (float)(Math.Max(w, h) * 0.65));
            list.Add(new DiffMarker(
                Math.Clamp(cx / compareW, 0f, 1f),
                Math.Clamp(cy / compareH, 0f, 1f),
                Math.Clamp(radiusPx / compareW, 0.002f, 0.5f),
                area));
        }

        return list
            .OrderByDescending(m => m.Area)
            .Take(Math.Max(1, maxMarkers))
            .ToList();
    }

    private static OpenCvSharp.Mat FitContain(OpenCvSharp.Mat src, int width, int height)
    {
        var canvas = new OpenCvSharp.Mat(new OpenCvSharp.Size(width, height), OpenCvSharp.MatType.CV_8UC3, new OpenCvSharp.Scalar(255, 255, 255));
        var scale = Math.Min(width / (double)src.Width, height / (double)src.Height);
        var drawW = Math.Max(1, (int)Math.Round(src.Width * scale));
        var drawH = Math.Max(1, (int)Math.Round(src.Height * scale));
        using var resized = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Resize(src, resized, new OpenCvSharp.Size(drawW, drawH), interpolation: OpenCvSharp.InterpolationFlags.Linear);
        var x = (width - drawW) / 2;
        var y = (height - drawH) / 2;
        var roi = new OpenCvSharp.Rect(x, y, drawW, drawH);
        resized.CopyTo(new OpenCvSharp.Mat(canvas, roi));
        return canvas;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _imageA?.Dispose();
        _imageB?.Dispose();
        base.OnFormClosed(e);
    }
}

internal static class ControlExtensions
{
    public static void DoubleBuffered(this Control control, bool enabled)
    {
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, enabled, null);
    }
}
