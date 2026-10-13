using AiOPDF;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

static class BehaviorAuditChecks
{
    public static void Run()
    {
        AppFontResolver.Initialize();
        var root = Path.GetFullPath("artifacts/behavior-audit"); Directory.CreateDirectory(root);
        var work = Path.Combine(root, "run-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(work);
        var rows = new List<string> { "# Medidas de auditoría funcional", "", "Datos sintéticos. Estas medidas describen el comportamiento real; no son una certificación de conformidad.", "" };
        void Finding(string value) { rows.Add("- " + value); Console.WriteLine(value); }
        var source = Path.Combine(work, "source.pdf");
        using (var pdf = new PdfDocument())
        {
            for (var i = 0; i < 3; i++)
            {
                var page = pdf.AddPage(); page.Width = XUnit.FromPoint(400); page.Height = XUnit.FromPoint(300);
                using var g = XGraphics.FromPdfPage(page);
                g.DrawString("HIDDEN_EDGE_SECRET", new XFont("AiOPDF Sans", 7), XBrushes.Black, 5, 10);
                for (var line = 0; line < 12; line++) g.DrawString($"PAGE {i + 1} LINE {line + 1} CONTENT SECRET", new XFont("AiOPDF Sans", 11), XBrushes.Black, 35, 45 + line * 16);
            }
            pdf.Info.Title = "SYNTHETIC_PRIVATE_TITLE"; pdf.Save(source);
        }
        var original = File.ReadAllBytes(source);
        string Run(ToolKind kind, string? input = null, string pages = "", int start = 1)
        {
            var output = Path.Combine(work, kind + "-" + Guid.NewGuid().ToString("N") + ".pdf");
            PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == kind), [input ?? source], output, Pages: pages, StartNumber: start, Margin: 10, Dpi: 144), default); return output;
        }
        string Text(string file, int page) { using var pdf = UglyToad.PdfPig.PdfDocument.Open(file); return UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(pdf.GetPage(page)); }
        var compressed = Run(ToolKind.Compress);
        if (new FileInfo(compressed).Length > original.Length || !Text(compressed, 1).Contains("SECRET")) throw new Exception("Compresión pierde texto o aumenta tamaño");
        var uncompressed = Path.Combine(work, "uncompressed.pdf");
        using (var pdf = new PdfDocument())
        {
            pdf.Options.CompressContentStreams = false;
            var page = pdf.AddPage();
            using var graphics = XGraphics.FromPdfPage(page);
            for (var i = 0; i < 200; i++) graphics.DrawString("REPEATED TEXT TO COMPRESS", new XFont("AiOPDF Sans", 10), XBrushes.Black, 20, 30 + i % 60 * 12);
            pdf.Save(uncompressed);
        }
        var optimized = Run(ToolKind.Compress, uncompressed);
        if (new FileInfo(optimized).Length >= new FileInfo(uncompressed).Length || !Text(optimized, 1).Contains("REPEATED"))
            throw new Exception("Optimización no comprime streams sin comprimir");
        Finding($"PASS: optimización real de streams: {new FileInfo(uncompressed).Length} → {new FileInfo(optimized).Length} bytes, conservando texto.");
        Finding($"Compresión: {new FileInfo(source).Length} bytes → {new FileInfo(compressed).Length} bytes; texto seleccionable conservado: {Text(compressed, 1).Contains("SECRET")}.");
        var flattened = Run(ToolKind.Flatten); Finding($"Aplanar: texto seleccionable conservado: {Text(flattened, 1).Contains("SECRET")}.");
        var numbered = Run(ToolKind.Number, pages: "3", start: 7);
        var lastLine = Text(numbered, 3).Split('\n', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        if (lastLine != "7") throw new Exception("La numeración no comienza en el valor elegido");
        Finding($"Numerar solo página 3, inicio 7: el último fragmento extraído es «{lastLine}».");
        var redacted = Run(ToolKind.Redact, pages: "1");
        if (PdfEngine.PageCount(redacted) != 3 || !Text(redacted, 2).Contains("SECRET")) throw new Exception("La censura eliminó páginas no seleccionadas");
        var partial = Path.Combine(work, "partial-redaction.pdf");
        PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Redact), [source], partial,
            Pages: "1", RedactX: 10, RedactY: 10, RedactWidth: 40, RedactHeight: 30, Dpi: 144), default);
        using (var image = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(partial, 0, "", 800)))
        {
            var inside = image.GetPixel(image.Width / 4, image.Height / 4);
            var outside = image.GetPixel(image.Width * 9 / 10, image.Height * 9 / 10);
            if (inside.Red != 0 || inside.Green != 0 || inside.Blue != 0 || outside.Red < 240) throw new Exception("Censura parcial aplicada fuera de la región");
        }
        if (Text(partial, 1).Contains("SECRET")) throw new Exception("Censura conserva texto oculto de la página procesada");
        Finding($"Censurar solo página 1 de un documento de 3 páginas: salida de {PdfEngine.PageCount(redacted)} página(s).");
        var cropped = Run(ToolKind.Crop);
        double visibleWidth, visibleHeight;
        using (var pdf = PdfEngine.Open(cropped)) { visibleWidth = pdf.Pages[0].CropBox.Width; visibleHeight = pdf.Pages[0].CropBox.Height; }
        Finding($"Recorte: caja visible {visibleWidth:F2} × {visibleHeight:F2} pt; el marcador fuera de la caja visible sigue extraíble: {Text(cropped, 1).Contains("HIDDEN_EDGE_SECRET")}.");
        var preview = PdfEngine.Thumbnail(cropped, 0, "", 720); File.WriteAllBytes(Path.Combine(work, "cropped-preview.png"), preview);
        using (var image = SkiaSharp.SKBitmap.Decode(preview)) Finding($"Render real del recorte: {image.Width} × {image.Height} píxeles; relación {image.Width / (double)image.Height:F4} frente a {visibleWidth / visibleHeight:F4} de la caja visible.");
        foreach (var kind in new[] { ToolKind.Flatten, ToolKind.Compress, ToolKind.Grayscale, ToolKind.Redact, ToolKind.Ocr })
        {
            var output = Run(kind, cropped, "1"); using var pdf = PdfEngine.Open(output);
            var visible = PdfGeometry.VisibleSize(pdf.Pages[0]);
            if (Math.Abs(visible.Width - visibleWidth) > .1 || Math.Abs(visible.Height - visibleHeight) > .1) throw new Exception(kind + " distorsiona la región visible");
            Finding($"{kind} después de recortar: página de salida {pdf.Pages[0].Width.Point:F2} × {pdf.Pages[0].Height.Point:F2} pt; caja visible de entrada {visibleWidth:F2} × {visibleHeight:F2} pt.");
        }
        if (!File.ReadAllBytes(source).SequenceEqual(original)) throw new Exception("El original cambió");
        var offset = Path.Combine(work, "offset-rotated.pdf");
        using (var pdf = PdfEngine.Open(source, mode: PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify))
        {
            pdf.Pages[0].CropBox = new PdfRectangle(new XPoint(40, 30), new XPoint(350, 250));
            pdf.Pages[0].Rotate = 90;
            pdf.Save(offset);
        }
        foreach (var kind in new[] { ToolKind.Flatten, ToolKind.Grayscale, ToolKind.Redact, ToolKind.Ocr })
        {
            using var pdf = PdfEngine.Open(Run(kind, offset, "1"));
            var size = PdfGeometry.VisibleSize(pdf.Pages[0]);
            if (Math.Abs(size.Width - 220) > .1 || Math.Abs(size.Height - 310) > .1 || pdf.PageCount != 3)
                throw new Exception(kind + " altera recorte desplazado, rotación o páginas restantes");
        }
        Finding("PASS: censura parcial, recorte desplazado y rotación de 90° conservan el alcance y la geometría esperados.");
        Finding("Los originales permanecen idénticos byte por byte.");
        Finding("PASS: regresiones de compresión, numeración, conservación de páginas y geometría corregidas.");
        File.WriteAllLines(Path.Combine(root, "measurements.md"), rows, new UTF8Encoding(false));
        Console.WriteLine("Evidencia y documentos: " + work);
    }
}
