# ImageTextDiff

Text-focused image comparison helper for C#.

## What it does

- Ignores source image resolution by normalizing both images to a fixed canvas.
- Emphasizes text changes using edge-based difference (better for glyph/stroke changes).
- Outputs a marked image with red circles around changed regions.

## Install / restore

```powershell
dotnet restore
```

## Usage

```csharp
using ImageTextDiff;

var comparer = new TextFocusedImageComparer();
var result = comparer.CompareAndMark(
    imageAPath: @"C:\temp\a.png",
    imageBPath: @"C:\temp\b.png",
    outputPath: @"C:\temp\diff-marked.png",
    options: new TextFocusedImageComparer.CompareOptions
    {
        MaxMarkers = 25,
        DiffThreshold = 35,
        MinRegionPixels = 28
    });

Console.WriteLine($"Saved: {result.OutputPath}");
Console.WriteLine($"Markers: {result.Markers.Count}");
```

## Notes

- Runtime dependency: `OpenCvSharp4.runtime.win` (Windows native binaries).
- If you get too many marks, increase `DiffThreshold` or `MinRegionPixels`.
- If marks are missing small text edits, lower `DiffThreshold`.
