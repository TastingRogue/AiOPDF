using System.Text.RegularExpressions;
using SkiaSharp;
using UglyToad.PdfPig;

namespace AiOPDF;

public sealed record ComparisonUnderline(double Left, double Bottom, double Right, double EndBottom);
public static class ComparisonMarkup
{
    public static ComparisonUnderline[] Locate(string original, string updated, string password, int number, bool originalSide)
    {
        using var a = PdfDocument.Open(original, new ParsingOptions { Password = password });
        using var b = PdfDocument.Open(updated, new ParsingOptions { Password = password });
        var left = number <= a.NumberOfPages ? a.GetPage(number) : null;
        var right = number <= b.NumberOfPages ? b.GetPage(number) : null;
        return Locate(ContentComparison.ComparePage(left, right, number, default), originalSide);
    }
    public static ComparisonUnderline[] Locate(PageComparison page, bool originalSide)
    {
        var words = originalSide ? page.OriginalWords : page.UpdatedWords; var cursor = 0; var marks = new List<ComparisonUnderline>();
        foreach (var change in page.Changes)
        {
            if (change.Kind == (originalSide ? "added" : "removed")) continue;
            var count = Regex.Matches(change.Text, @"\S+").Count;
            if (change.Kind != "equal")
                foreach (var word in words.Skip(cursor).Take(count))
                    marks.Add(word.Underline);
            cursor += count;
        }
        return marks.Distinct().ToArray();
    }
    public static byte[] Render(string original, string updated, string password, int number, bool originalSide, int width = 1200, PageComparison? comparison = null)
    {
        using var bitmap = SKBitmap.Decode(PdfEngine.Thumbnail(originalSide ? original : updated, number - 1, password, width));
        using var canvas = new SKCanvas(bitmap);
        using var pen = new SKPaint { Color = SKColor.Parse(originalSide ? "#D02962" : "#008573"), IsAntialias = true, StrokeWidth = Math.Max(2, bitmap.Width / 400f), StrokeCap = SKStrokeCap.Round };
        foreach (var mark in comparison == null ? Locate(original, updated, password, number, originalSide) : Locate(comparison, originalSide))
        {
            var x1 = (float)(mark.Left * bitmap.Width); var y1 = (float)(mark.Bottom * bitmap.Height);
            var x2 = (float)(mark.Right * bitmap.Width); var y2 = (float)(mark.EndBottom * bitmap.Height);
            var length = Math.Max(1, MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)));
            var offsetX = -(y2 - y1) / length * pen.StrokeWidth; var offsetY = (x2 - x1) / length * pen.StrokeWidth;
            canvas.DrawLine(x1 + offsetX, y1 + offsetY, x2 + offsetX, y2 + offsetY, pen);
        }
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
    }
}
