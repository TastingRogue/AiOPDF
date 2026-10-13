using AiOPDF;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

static class SampleDocuments
{
    public static string[] Create()
    {
        AppFontResolver.Initialize();
        var folder = Path.GetFullPath("output/pdf"); Directory.CreateDirectory(folder);
        var outputs = new List<string>();
        foreach (var variant in new[] { "A", "B" })
        {
            var output = Path.Combine(folder, $"AiOPDF-prueba-{variant}.pdf");
            var raw = Path.Combine(folder, ".source-" + Guid.NewGuid() + ".pdf");
            var scan = Path.Combine(folder, ".scan-" + Guid.NewGuid() + ".pdf");
            try
            {
                using (var pdf = new PdfDocument())
                {
                    pdf.Info.Title = $"AiOPDF documento de prueba {variant}"; pdf.Info.Author = "Datos ficticios de AiOPDF";
                    for (var i = 1; i <= 3; i++)
                    {
                        var page = pdf.AddPage(); if (i == 2) page.Orientation = PdfSharp.PageOrientation.Landscape;
                        using var g = XGraphics.FromPdfPage(page); var width = page.Width.Point;
                        g.DrawRectangle(new XSolidBrush(XColor.FromArgb(90, 63, 192)), 0, 0, width, 115);
                        g.DrawString($"AiOPDF TEST DOCUMENT {variant}", new XFont("AiOPDF Sans", 25), XBrushes.White, 40, 55);
                        g.DrawString($"Page {i} - fictional data only", new XFont("AiOPDF Sans", 14), XBrushes.White, 40, 85);
                        var font = new XFont("AiOPDF Sans", 16);
                        g.DrawString(i == 3 ? "SCANNED PAGE - OCR CHECK 12345" : "Informe de prueba: texto, cifras y colores.", font, XBrushes.Black, 40, 155);
                        for (var row = 0; row < 6; row++)
                        {
                            var y = 185 + row * 36;
                            g.DrawRectangle(new XSolidBrush(row % 2 == 0 ? XColor.FromArgb(238, 231, 255) : XColors.White), 40, y, width - 80, 36);
                            g.DrawString($"Item {row + 1}    {variant}    Cantidad {row + 2}    Total {(row + 1) * (variant == "A" ? 125 : 140)} EUR", font, XBrushes.Black, 52, y + 24);
                        }
                        g.DrawRectangle(XBrushes.Coral, 40, 430, 100, 60); g.DrawRectangle(XBrushes.Teal, 155, 430, 100, 60); g.DrawRectangle(XBrushes.Gold, 270, 430, 100, 60);
                        g.DrawString("PRIVATE DEMO ID: TEST-000" + variant, font, XBrushes.Black, 40, 535);
                    }
                    pdf.Save(raw);
                }
                PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Flatten), [raw], scan, Pages: "3", Dpi: 144), CancellationToken.None);
                using var source = PdfReader.Open(raw, PdfDocumentOpenMode.Import); using var scanned = PdfReader.Open(scan, PdfDocumentOpenMode.Import); using var result = new PdfDocument();
                result.Info.Title = source.Info.Title; result.Info.Author = source.Info.Author;
                result.AddPage(source.Pages[0]); result.AddPage(source.Pages[1]); result.AddPage(scanned.Pages[0]); result.Save(output);
                for (var p = 0; p < 3; p++)
                {
                    var bytes = PdfEngine.Thumbnail(output, p, "", 800);
                    using var bitmap = SkiaSharp.SKBitmap.Decode(bytes);
                    if ((p == 1 && bitmap.Width <= bitmap.Height) || (p != 1 && bitmap.Height <= bitmap.Width)) throw new Exception("Thumbnail aspect ratio incorrect");
                    File.WriteAllBytes(Path.Combine(folder, $"prueba-{variant}-pagina-{p + 1}.png"), bytes);
                }
                outputs.Add(output); Console.WriteLine("PASS created and rendered " + output);
            }
            finally { if (File.Exists(raw)) File.Delete(raw); if (File.Exists(scan)) File.Delete(scan); }
        }
        return outputs.ToArray();
    }
}
