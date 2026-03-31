using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Ghostty.Tests.Visual.Infrastructure;

public sealed class ImageCompareResult
{
    public double DiffScore { get; init; }
    public bool IsMatch { get; init; }
    public string? DiffImagePath { get; init; }
}

public static class ImageComparer
{
    /// <summary>
    /// Compare two images and return a diff score (0.0 = identical, 1.0 = completely different).
    /// Optionally saves a diff image highlighting changed pixels.
    /// </summary>
    public static ImageCompareResult Compare(
        string expectedPath,
        string actualPath,
        double tolerance,
        string? diffOutputPath = null)
    {
        using var expected = Image.Load<Rgba32>(expectedPath);
        using var actual = Image.Load<Rgba32>(actualPath);

        // Resize actual to match expected if dimensions differ
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            actual.Mutate(ctx => ctx.Resize(expected.Width, expected.Height));
        }

        long totalDiff = 0;
        long totalPixels = expected.Width * expected.Height;
        using var diff = new Image<Rgba32>(expected.Width, expected.Height);

        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                var ep = expected[x, y];
                var ap = actual[x, y];

                int dr = Math.Abs(ep.R - ap.R);
                int dg = Math.Abs(ep.G - ap.G);
                int db = Math.Abs(ep.B - ap.B);
                int pixelDiff = dr + dg + db;

                if (pixelDiff > 0)
                {
                    // Highlight differences in red, intensity proportional to diff
                    byte intensity = (byte)Math.Min(255, pixelDiff);
                    diff[x, y] = new Rgba32(intensity, 0, 0, 255);
                }
                else
                {
                    // Dim copy of original for context
                    diff[x, y] = new Rgba32((byte)(ep.R / 3), (byte)(ep.G / 3), (byte)(ep.B / 3), 255);
                }

                totalDiff += pixelDiff;
            }
        }

        double score = totalDiff / (totalPixels * 765.0); // 765 = 255*3 max diff per pixel
        bool isMatch = score <= tolerance;

        string? savedDiffPath = null;
        if (!isMatch && diffOutputPath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(diffOutputPath)!);
            diff.SaveAsPng(diffOutputPath);
            savedDiffPath = diffOutputPath;
        }

        return new ImageCompareResult
        {
            DiffScore = score,
            IsMatch = isMatch,
            DiffImagePath = savedDiffPath
        };
    }

    /// <summary>
    /// Check if two screenshots are visually identical (for render stability checks).
    /// Uses a very tight tolerance since we're comparing consecutive captures.
    /// </summary>
    public static bool AreIdentical(string path1, string path2) =>
        Compare(path1, path2, tolerance: 0.001).IsMatch;

    /// <summary>
    /// Check if an image is blank (all pixels the same color or nearly so).
    /// </summary>
    public static bool IsBlank(string imagePath, double threshold = 0.01)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        var firstPixel = image[0, 0];
        long totalDiff = 0;
        long totalPixels = image.Width * image.Height;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var p = image[x, y];
                totalDiff += Math.Abs(p.R - firstPixel.R)
                           + Math.Abs(p.G - firstPixel.G)
                           + Math.Abs(p.B - firstPixel.B);
            }
        }

        return (totalDiff / (totalPixels * 765.0)) < threshold;
    }
}
