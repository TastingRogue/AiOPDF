using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AiOPDF;

// Own native drawing surface: no embedded web view or browser component.
public sealed class RegionEditor : Control
{
    public Bitmap? Page { get; set; }
    public bool IsRedaction { get; set; }
    public Rect Selection { get; private set; } = new(10, 10, 30, 10);
    public event Action<Rect>? SelectionChanged;
    readonly Stack<Rect> history = new();
    Point? anchor;
    Rect PageBounds
    {
        get
        {
            if (Page == null) return default;
            var scale = Math.Min(Bounds.Width / Page.Size.Width, Bounds.Height / Page.Size.Height);
            var width = Page.Size.Width * scale; var height = Page.Size.Height * scale;
            return new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
        }
    }
    public void SetSelection(Rect selection) { Selection = selection; InvalidateVisual(); }
    public void Reset() { history.Push(Selection); Selection = IsRedaction ? new Rect(10, 10, 30, 10) : new Rect(0, 0, 100, 100); SelectionChanged?.Invoke(Selection); InvalidateVisual(); }
    public void Undo() { if (history.TryPop(out var selection)) { Selection = selection; SelectionChanged?.Invoke(selection); InvalidateVisual(); } }
    public RegionEditor()
    {
        Height = 470; Focusable = true; Cursor = new Cursor(StandardCursorType.Cross);
        PointerPressed += (_, e) =>
        {
            var p = e.GetPosition(this);
            if (Page == null || !PageBounds.Contains(p) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            history.Push(Selection); anchor = p; e.Pointer.Capture(this); Focus(); e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (anchor is not Point start) return;
            var box = PageBounds; var p = e.GetPosition(this);
            p = new Point(Math.Clamp(p.X, box.Left, box.Right), Math.Clamp(p.Y, box.Top, box.Bottom));
            var left = Math.Min(start.X, p.X); var top = Math.Min(start.Y, p.Y);
            var x = Math.Clamp((left - box.Left) / box.Width * 100, 0, 99); var y = Math.Clamp((top - box.Top) / box.Height * 100, 0, 99);
            Selection = new Rect(x, y, Math.Min(100 - x, Math.Max(1, Math.Abs(p.X - start.X) / box.Width * 100)), Math.Min(100 - y, Math.Max(1, Math.Abs(p.Y - start.Y) / box.Height * 100)));
            InvalidateVisual();
        };
        PointerReleased += (_, e) => { if (anchor != null) { anchor = null; e.Pointer.Capture(null); SelectionChanged?.Invoke(Selection); e.Handled = true; } };
        KeyDown += (_, e) => { if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { Undo(); e.Handled = true; } };
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context); context.FillRectangle(Brush.Parse("#F3EFF8"), new Rect(Bounds.Size));
        if (Page == null) return;
        var box = PageBounds; context.DrawImage(Page, box);
        var selected = new Rect(box.Left + Selection.X / 100 * box.Width, box.Top + Selection.Y / 100 * box.Height, Selection.Width / 100 * box.Width, Selection.Height / 100 * box.Height);
        context.FillRectangle(IsRedaction ? Brush.Parse("#BB29243D") : Brush.Parse("#335A3FC0"), selected);
        context.DrawRectangle(null, new Pen(Brush.Parse("#5A3FC0"), 2), selected);
    }
}
