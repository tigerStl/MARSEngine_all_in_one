using OpenCvSharp;

namespace ImageTextDiff;

public sealed class TextFocusedImageComparer
{
    public sealed class CompareOptions
    {
        public int MaxMarkers { get; set; } = 30;
        public int DiffThreshold { get; set; } = 35;
        public int MinRegionPixels { get; set; } = 28;
    }

    public sealed record DifferenceMarker(int X, int Y, int Radius, int Area);

    public sealed class CompareResult
    {
        public required string OutputPath { get; init; }
        public required IReadOnlyList<DifferenceMarker> Markers { get; init; }
    }

    /// <summary>
    /// Compares imageA and imageB using a text/edge focused strategy:
    /// 1) Normalize to a common canvas (resolution-independent)
    /// 2) Compare edge maps to emphasize text strokes
    /// 3) Group connected components and draw circles on output
    /// </summary>
    public CompareResult CompareAndMark(
        string imageAPath,
        string imageBPath,
        string outputPath,
        CompareOptions? options = null)
    {
        options ??= new CompareOptions();

        using var aSrc = Cv2.ImRead(imageAPath, ImreadModes.Color);
        using var bSrc = Cv2.ImRead(imageBPath, ImreadModes.Color);
        if (aSrc.Empty() || bSrc.Empty())
            throw new InvalidOperationException("One or both input images cannot be loaded.");

        // Fixed compare canvas (resolution-independent).
        const int compareW = 1280;
        const int compareH = 720;
        using var aNorm = FitContain(aSrc, compareW, compareH);
        using var bNorm = FitContain(bSrc, compareW, compareH);

        using var aGray = new Mat();
        using var bGray = new Mat();
        Cv2.CvtColor(aNorm, aGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(bNorm, bGray, ColorConversionCodes.BGR2GRAY);

        // Mild blur to reduce antialiasing noise across environments.
        Cv2.GaussianBlur(aGray, aGray, new Size(3, 3), 0);
        Cv2.GaussianBlur(bGray, bGray, new Size(3, 3), 0);

        using var aEdges = new Mat();
        using var bEdges = new Mat();
        Cv2.Canny(aGray, aEdges, 50, 150);
        Cv2.Canny(bGray, bEdges, 50, 150);

        // Edge difference highlights text glyph/stroke changes better than raw RGB diff.
        using var edgeDiff = new Mat();
        Cv2.Absdiff(aEdges, bEdges, edgeDiff);
        Cv2.Threshold(edgeDiff, edgeDiff, options.DiffThreshold, 255, ThresholdTypes.Binary);

        // Bridge nearby text stroke fragments into coherent regions.
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edgeDiff, edgeDiff, kernel, iterations: 1);
        Cv2.MorphologyEx(edgeDiff, edgeDiff, MorphTypes.Close, kernel, iterations: 1);

        var markers = FindMarkers(edgeDiff, options)
            .OrderByDescending(m => m.Area)
            .Take(Math.Max(1, options.MaxMarkers))
            .ToList();

        using var output = aNorm.Clone();
        foreach (var m in markers)
        {
            Cv2.Circle(output, new Point(m.X, m.Y), m.Radius, new Scalar(0, 0, 255), 2, LineTypes.AntiAlias);
            Cv2.Circle(output, new Point(m.X, m.Y), Math.Max(2, m.Radius - 2), new Scalar(0, 80, 255), 1, LineTypes.AntiAlias);
        }

        Cv2.ImWrite(outputPath, output);
        return new CompareResult
        {
            OutputPath = outputPath,
            Markers = markers
        };
    }

    private static Mat FitContain(Mat src, int width, int height)
    {
        var canvas = new Mat(new Size(width, height), MatType.CV_8UC3, new Scalar(255, 255, 255));
        var scale = Math.Min(width / (double)src.Width, height / (double)src.Height);
        var drawW = Math.Max(1, (int)Math.Round(src.Width * scale));
        var drawH = Math.Max(1, (int)Math.Round(src.Height * scale));
        var resized = new Mat();
        Cv2.Resize(src, resized, new Size(drawW, drawH), interpolation: InterpolationFlags.Linear);

        var x = (width - drawW) / 2;
        var y = (height - drawH) / 2;
        var roi = new Rect(x, y, drawW, drawH);
        resized.CopyTo(new Mat(canvas, roi));
        resized.Dispose();
        return canvas;
    }

    private static IEnumerable<DifferenceMarker> FindMarkers(Mat binaryMask, CompareOptions options)
    {
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();
        Cv2.ConnectedComponentsWithStats(
            binaryMask,
            labels,
            stats,
            centroids,
            PixelConnectivity.Connectivity8,
            MatType.CV_32S);

        var count = stats.Rows;
        for (var i = 1; i < count; i++) // 0 is background
        {
            var area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
            if (area < Math.Max(1, options.MinRegionPixels))
                continue;

            var x = stats.Get<int>(i, (int)ConnectedComponentsTypes.Left);
            var y = stats.Get<int>(i, (int)ConnectedComponentsTypes.Top);
            var w = stats.Get<int>(i, (int)ConnectedComponentsTypes.Width);
            var h = stats.Get<int>(i, (int)ConnectedComponentsTypes.Height);

            // Reject giant layout-level diffs; keep text-ish regions.
            if (w > binaryMask.Width * 0.75 || h > binaryMask.Height * 0.6)
                continue;

            var cx = (int)Math.Round(centroids.Get<double>(i, 0));
            var cy = (int)Math.Round(centroids.Get<double>(i, 1));
            var radius = Math.Max(8, (int)Math.Round(Math.Max(w, h) * 0.65));
            yield return new DifferenceMarker(cx, cy, radius, area);
        }
    }
}
