using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text.Json;

namespace AiOPDF;

// Explicit diagnostic for installed binaries; never opens or reads user documents.
internal static class PackageCheck
{
    public static void Run(string reportPath)
    {
        var folder = Path.Combine(Path.GetTempPath(), "AiOPDF-package-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var checks = new List<string>();
        try
        {
            AppFontResolver.Initialize();
            var input = Path.Combine(folder, "synthetic.pdf");
            using (var pdf = new PdfDocument())
            {
                using var graphics = XGraphics.FromPdfPage(pdf.AddPage());
                graphics.DrawString("PACKAGE TEST 12345", new XFont("AiOPDF Sans", 24), XBrushes.Black, 40, 80);
                pdf.Save(input);
            }
            if (PdfEngine.PageCount(input) != 1) throw new InvalidOperationException("PDF read failed.");
            checks.Add("PDFsharp / embedded font");
            using (var image = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(input, 0, "", 600)))
                if (image is null || image.Width < 1) throw new InvalidOperationException("Native render failed.");
            checks.Add("PDFium / Skia native rendering");
            using (var pdf = UglyToad.PdfPig.PdfDocument.Open(input))
                if (!pdf.GetPage(1).Text.Contains("PACKAGE TEST")) throw new InvalidOperationException("Text extraction failed.");
            checks.Add("PdfPig text extraction");
            var output = Path.Combine(folder, "ocr.txt");
            OcrOperations.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.OcrText), [input], output, Dpi: 144), output, CancellationToken.None, null);
            if (!File.ReadAllText(output).Contains("12345")) throw new InvalidOperationException("Installed OCR failed.");
            checks.Add("ONNX native runtime / local OCR models");
            L10n.SetLanguage("es", false);
            if (L10n.Text("common.language") == "common.language") throw new InvalidOperationException("Spanish catalog missing.");
            L10n.SetLanguage("en", false);
            checks.Add("Embedded es/en catalogs");
            File.WriteAllText(Path.GetFullPath(reportPath), JsonSerializer.Serialize(new { success = true, version = typeof(App).Assembly.GetName().Version?.ToString(), runtime = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, checks }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}
