using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AiOPDF;

// Original vector illustration: loose paper, tabs and a pencil on a small workbench.
public sealed class DeskArt : Control
{
    public override void Render(DrawingContext context)
    {
        var scale = Math.Min(Bounds.Width / 320, Bounds.Height / 190);
        using var sizing = context.PushTransform(Matrix.CreateScale(scale, scale));
        context.DrawEllipse(Brush.Parse("#F8D977"), null, new Point(254, 44), 30, 30);
        Paper(context, -12, 126, 91, "#EEE7FF", "#AD96ED");
        Paper(context, 9, 177, 87, "#FFFFFF", "#6650B5");
        context.DrawGeometry(Brush.Parse("#FF946D"), null, Geometry.Parse("M 74,130 L 91,113 L 151,170 L 134,187 Z"));
        context.DrawGeometry(Brush.Parse("#29243D"), null, Geometry.Parse("M 134,187 L 153,190 L 151,170 Z"));
        context.DrawGeometry(null, new Pen(Brush.Parse("#5A3FC0"), 3, lineCap: PenLineCap.Round), Geometry.Parse("M 263,114 L 271,122 L 287,101 M 38,40 L 53,40 M 45,33 L 45,47"));
    }
    static void Paper(DrawingContext context, double angle, double x, double y, string color, string line)
    {
        using var rotation = context.PushTransform(Matrix.CreateTranslation(-x, -y) * Matrix.CreateRotation(angle * Math.PI / 180) * Matrix.CreateTranslation(x, y));
        var rect = new Rect(x - 48, y - 70, 96, 140);
        context.DrawRectangle(Brush.Parse(color), new Pen(Brush.Parse(line), 1.3), rect, 12, 12);
        context.DrawRectangle(Brush.Parse(line), null, new Rect(x - 32, y - 47, 30, 8), 4, 4);
        var pen = new Pen(Brush.Parse(line), 3, lineCap: PenLineCap.Round);
        foreach (var offset in new[] { -20, -6, 8, 22 }) context.DrawLine(pen, new Point(x - 32, y + offset), new Point(x + (offset == 22 ? 8 : 29), y + offset));
        context.DrawRectangle(Brush.Parse("#FFE394"), null, new Rect(x + 23, y + 42, 13, 35), 2, 2);
    }
}
