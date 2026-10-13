using PdfSharp.Pdf;

namespace AiOPDF;

public static class PdfGeometry
{
    public static PdfRectangle CropRegion(PdfPage page, double x, double y, double width, double height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || x + width > 100.001 || y + height > 100.001)
            throw new ArgumentException("Dibuja una región dentro de la página.");
        var crop = page.CropBox; if (crop.Width <= 0 || crop.Height <= 0) crop = page.MediaBox;
        var media = page.MediaBox;
        var left = Math.Max(crop.X1, media.X1); var bottom = Math.Max(crop.Y1, media.Y1);
        var w = Math.Min(crop.X2, media.X2) - left; var h = Math.Min(crop.Y2, media.Y2) - bottom;
        if (w <= 0 || h <= 0) throw new ArgumentException("La página no tiene una región visible válida.");
        var rotation = (page.Rotate % 360 + 360) % 360;
        var corners = new[] { (x, y), (x + width, y), (x, y + height), (x + width, y + height) }.Select(p =>
        {
            var u = p.Item1 / 100; var v = p.Item2 / 100;
            return rotation switch { 90 => (U: v, V: u), 180 => (U: 1 - u, V: v), 270 => (U: 1 - v, V: 1 - u), _ => (U: u, V: 1 - v) };
        }).ToArray();
        return new PdfRectangle(new PdfSharp.Drawing.XPoint(left + corners.Min(p => p.U) * w, bottom + corners.Min(p => p.V) * h),
            new PdfSharp.Drawing.XPoint(left + corners.Max(p => p.U) * w, bottom + corners.Max(p => p.V) * h));
    }

    public static (double Width, double Height) VisibleSize(PdfPage page)
    {
        var box = page.CropBox;
        if (box.Width <= 0 || box.Height <= 0) box = page.MediaBox;
        var media = page.MediaBox;
        var width = Math.Min(box.X2, media.X2) - Math.Max(box.X1, media.X1);
        var height = Math.Min(box.Y2, media.Y2) - Math.Max(box.Y1, media.Y1);
        if (width <= 0 || height <= 0) throw new ArgumentException("La región visible de la página está vacía.");
        var unit = page.Elements.GetReal("/UserUnit");
        if (unit <= 0) unit = 1;
        return page.Rotate % 180 != 0 ? (height * unit, width * unit) : (width * unit, height * unit);
    }
}
