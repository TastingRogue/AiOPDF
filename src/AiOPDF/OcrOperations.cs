using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PDFtoImage;
using RapidOcrNet;
using SkiaSharp;

namespace AiOPDF;

public static class OcrOperations
{
    public static void Run(PdfJob job, string output, CancellationToken token, IProgress<string>? progress)
    {
        using var ocr = new RapidOcr();
        var models = Path.Combine(AppContext.BaseDirectory, "models", "v5");
        progress?.Report("Cargando modelos OCR locales…"); token.ThrowIfCancellationRequested();
        ocr.InitModels(detPath: Path.Combine(models, "ch_PP-OCRv5_mobile_det.onnx"), clsPath: Path.Combine(models, "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx"),
            recPath: Path.Combine(models, "latin_PP-OCRv5_rec_mobile_infer.onnx"), keysPath: Path.Combine(models, "ppocrv5_latin_dict.txt"));
        using var source = PdfEngine.Open(job.Inputs[0], job.Password);
        using var stream = File.OpenRead(job.Inputs[0]);
        using var document = new PdfDocument();
        using var writer = job.Tool.Kind == ToolKind.OcrText ? new StreamWriter(output, false, new System.Text.UTF8Encoding(false)) : null;
        var selected = PdfOperations.ParsePages(job.Pages, source.PageCount).ToHashSet();
        foreach (var number in writer == null ? Enumerable.Range(1, source.PageCount) : selected.Order())
        {
            if (!selected.Contains(number)) { document.AddPage(source.Pages[number - 1]); continue; }
            token.ThrowIfCancellationRequested(); progress?.Report($"Reconociendo página {number} de {source.PageCount}…");
            ProcessingLimits.Page(source.Pages[number - 1], job.Dpi);
            using var bitmap = Conversion.ToImage(stream, leaveOpen: true, page: number - 1, password: job.Password, options: new RenderOptions(Dpi: job.Dpi));
            var result = ocr.DetectAsync(bitmap, RapidOcrOptions.Default, null, token).GetAwaiter().GetResult();
            if (writer != null) { writer.WriteLine($"--- Página {number} ---"); writer.WriteLine(result.StrRes); writer.WriteLine(); continue; }
            var original = source.Pages[number - 1]; var page = document.AddPage();
            var visible = PdfGeometry.VisibleSize(original);
            page.Width = XUnit.FromPoint(visible.Width);
            page.Height = XUnit.FromPoint(visible.Height);
            using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100); using var imageStream = new MemoryStream(encoded.ToArray()); using var image = XImage.FromStream(imageStream);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
            var scaleX = page.Width.Point / bitmap.Width; var scaleY = page.Height.Point / bitmap.Height;
            foreach (var block in result.TextBlocks)
            {
                token.ThrowIfCancellationRequested(); if (string.IsNullOrWhiteSpace(block.Text)) continue;
                var points = block.BoxPoints;
                var x = points.Min(p => p.X) * scaleX; var y = points.Min(p => p.Y) * scaleY;
                var width = (points.Max(p => p.X) - points.Min(p => p.X)) * scaleX;
                var height = (points.Max(p => p.Y) - points.Min(p => p.Y)) * scaleY;
                var font = new XFont("AiOPDF Sans", Math.Max(4, height * .8));
                var measured = graphics.MeasureString(block.Text, font).Width;
                var state = graphics.Save(); graphics.TranslateTransform(x, y + height * .85);
                graphics.ScaleTransform(width / Math.Max(1, measured), 1);
                graphics.DrawString(block.Text, font, XBrushes.Transparent, 0, 0); graphics.Restore(state);
            }
        }
        token.ThrowIfCancellationRequested();
        if (writer == null) document.Save(output);
    }
}

