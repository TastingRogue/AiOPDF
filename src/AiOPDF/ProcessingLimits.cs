namespace AiOPDF;
public static class ProcessingLimits
{
    public const long MaximumInputBytes = 1024L * 1024 * 1024;
    public const int MaximumPages = 10000;
    public const long MaximumRasterPixels = 32_000_000;
    public static void Input(string path)
    {
        if (new FileInfo(path).Length > MaximumInputBytes) throw new ArgumentException("El archivo supera el límite de 1 GB. Divídelo antes de procesarlo.");
    }
    public static void Raster(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0 || width > 16384 || height > 16384 || Math.Ceiling(width) * Math.Ceiling(height) > MaximumRasterPixels)
            throw new ArgumentException("La página o imagen supera el límite de renderizado. Reduce la resolución o el tamaño.");
    }
    public static void Page(PdfSharp.Pdf.PdfPage page, int dpi)
    {
        var size = PdfGeometry.VisibleSize(page); Raster(size.Width * dpi / 72, size.Height * dpi / 72);
    }
}
