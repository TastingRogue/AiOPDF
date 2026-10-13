using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AiOPDF;

// Original vector artwork. A folded page is the common motif, with a unique action glyph.
public sealed class BrandIcon : Control
{
    public ToolKind? Kind { get; init; }
    public string Accent { get; init; } = "#26756A";
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = Math.Min(Bounds.Width, Bounds.Height) / 48;
        using var transform = context.PushTransform(Matrix.CreateScale(scale, scale));
        var pen = new Pen(Brush.Parse(Accent), 2.4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var faint = Brush.Parse("#FFFFFF");
        context.DrawGeometry(faint, pen, Geometry.Parse(Frame(Kind)));
        context.DrawGeometry(null, pen, Geometry.Parse(Glyph(Kind)));
    }
    public static string Frame(ToolKind? kind) => kind switch
    {
        ToolKind.Merge => "M 5,8 H 18 V 30 H 5 Z M 30,8 H 43 V 30 H 30 Z M 17,32 H 31 V 44 H 17 Z",
        ToolKind.Split => "M 17,4 H 31 V 19 H 17 Z M 4,30 H 18 V 44 H 4 Z M 30,30 H 44 V 44 H 30 Z",
        ToolKind.Organize => "M 5,7 H 19 V 22 H 5 Z M 28,7 H 42 V 22 H 28 Z M 5,31 H 19 V 46 H 5 Z",
        ToolKind.Compare => "M 3,8 H 19 V 40 H 3 Z M 29,8 H 45 V 40 H 29 Z",
        _ => "M 12,5 L 29,5 L 37,13 L 37,41 Q 37,43 35,43 L 12,43 Q 10,43 10,41 L 10,7 Q 10,5 12,5 Z M 29,5 L 29,13 L 37,13"
    };
    public static string Glyph(ToolKind? kind) => kind switch
        {
            ToolKind.Merge => "M 12,17 V 24 L 24,29 L 36,24 V 17 M 24,20 V 29 M 20,25 L 24,29 L 28,25",
            ToolKind.Organize => "M 10,12 H 14 M 10,17 H 14 M 33,12 H 37 M 33,17 H 37 M 10,36 H 14 M 10,41 H 14 M 27,35 H 43 M 39,31 L 43,35 L 39,39",
            ToolKind.Split => "M 24,20 V 25 M 24,25 L 11,29 M 24,25 L 37,29 M 8,25 L 11,29 L 15,25 M 33,25 L 37,29 L 40,25",
            ToolKind.Delete => "M 18,22 L 30,34 M 30,22 L 18,34",
            ToolKind.Rotate => "M 30,29 A 7,7 0 1 1 25,21 M 25,17 L 25,22 L 30,22",
            ToolKind.Images => "M 3,23 H 21 V 39 H 3 Z M 4,36 L 9,29 L 14,34 L 18,31 L 21,36 M 25,29 H 43 M 39,25 L 43,29 L 39,33",
            ToolKind.Png => "M 16,20 H 32 V 33 H 16 Z M 16,31 L 21,25 L 26,30 L 29,27 L 32,31 M 24,36 V 46 M 20,42 L 24,46 L 28,42",
            ToolKind.Jpeg => "M 16,20 H 32 V 33 H 16 Z M 16,31 L 21,25 L 26,30 L 29,27 L 32,31 M 17,38 L 21,42 L 17,46 M 31,38 L 27,42 L 31,46",
            ToolKind.Text => "M 16,21 H 31 M 16,27 H 28 M 16,33 H 24 M 28,38 H 44 M 40,34 L 44,38 L 40,42",
            ToolKind.Word => "M 14,22 L 18,35 L 24,25 L 29,35 L 33,22",
            ToolKind.OcrText => "M 14,22 V 18 H 19 M 29,18 H 34 V 22 M 14,33 V 37 H 19 M 29,37 H 34 V 33 M 20,23 H 28 M 24,23 V 32",
            ToolKind.Excel => "M 16,20 L 32,20 L 32,35 L 16,35 Z M 16,25 L 32,25 M 16,30 L 32,30 M 23,20 L 23,35",
            ToolKind.Slides => "M 15,20 L 33,20 L 33,32 L 15,32 Z M 24,32 L 24,38 M 20,38 L 28,38",
            ToolKind.Ocr => "M 15,24 L 15,19 L 20,19 M 28,19 L 33,19 L 33,24 M 15,31 L 15,36 L 20,36 M 28,36 L 33,36 L 33,31 M 20,27 L 28,27",
            ToolKind.Redact => "M 16,22 L 32,22 L 32,27 L 16,27 Z M 16,32 L 32,32",
            ToolKind.Watermark => "M 24,18 C 22,23 17,27 17,30 A 7,7 0 0 0 31,30 C 31,27 26,23 24,18 Z",
            ToolKind.Number => "M 21,19 L 19,36 M 29,19 L 27,36 M 16,25 L 33,25 M 15,31 L 32,31",
            ToolKind.Crop => "M 18,17 L 18,33 L 34,33 M 14,22 L 29,22 L 29,37",
            ToolKind.Protect => "M 17,27 L 31,27 L 31,37 L 17,37 Z M 20,27 L 20,22 A 4,4 0 0 1 28,22 L 28,27 M 24,31 L 24,33",
            ToolKind.Unlock => "M 17,27 H 31 V 37 H 17 Z M 20,27 V 22 A 4,4 0 0 1 28,22 M 24,31 V 33",
            ToolKind.Metadata => "M 24,20 L 24,21 M 24,26 L 24,35 M 21,35 L 27,35",
            ToolKind.Flatten => "M 16,21 L 32,21 M 16,28 L 32,28 M 16,35 L 32,35 M 24,21 L 24,35",
            ToolKind.Compress => "M 15,19 L 20,24 M 15,24 L 20,24 L 20,19 M 33,37 L 28,32 M 28,37 L 28,32 L 33,32",
            ToolKind.Grayscale => "M 24,18 A 9,9 0 1 0 24,36 A 9,9 0 1 0 24,18 M 24,18 L 24,36",
            ToolKind.Compare => "M 7,17 H 15 M 7,24 H 15 M 7,31 H 12 M 33,17 H 41 M 33,24 H 37 M 33,31 H 41 M 21,21 L 24,18 L 27,21 M 24,18 V 30 M 21,27 L 24,30 L 27,27",
            _ => "M 16,26 L 22,32 L 32,21"
        };
}
