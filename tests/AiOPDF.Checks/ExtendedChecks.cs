using AiOPDF;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

static class ExtendedChecks
{
    public static void Run(string folder, string input)
    {
        AppFontResolver.Initialize();
        var textPdf = Path.Combine(folder, "text.pdf");
        using (var pdf = new PdfDocument())
        {
            var page = pdf.AddPage(); page.Width = XUnit.FromPoint(400); page.Height = XUnit.FromPoint(300);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("AiOPDF PRIVATE DOCUMENT 12345", new XFont("AiOPDF Sans", 18), XBrushes.Black, 35, 90);
            pdf.Info.Title = "Sensitive title"; pdf.Info.Author = "Sensitive author"; pdf.Save(textPdf);
        }
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
        PdfJob Job(ToolKind kind, string? path = null) => new(ToolCatalog.All.Single(t => t.Kind == kind), [path ?? textPdf], Path.Combine(folder, Guid.NewGuid().ToString("N")), Dpi: 96);
        string Run(PdfJob job) { PdfEngine.Run(job, CancellationToken.None); return job.Output; }
        var original = File.ReadAllBytes(textPdf);
        var merged = Run(Job(ToolKind.Merge, input) with { Inputs = [input, input] });
        using (var pdf = PdfReader.Open(merged, PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 6, "integrated engine merges PDFs");
        var ordered = Run(Job(ToolKind.Organize, input) with { Pages = "3,1,1" });
        using (var pdf = PdfReader.Open(ordered, PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 3 && pdf.Pages[0].Width.Point == 203 && pdf.Pages[1].Width.Point == 201, "integrated engine orders and duplicates");
        var removed = Run(Job(ToolKind.Delete, input) with { Pages = "2" });
        using (var pdf = PdfReader.Open(removed, PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 2 && pdf.Pages[1].Width.Point == 203, "integrated engine deletes pages");
        var rotated = Run(Job(ToolKind.Rotate, input) with { Pages = "2", Rotation = 90 });
        using (var pdf = PdfReader.Open(rotated, PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 3 && pdf.Pages[0].Rotate == 0 && pdf.Pages[1].Rotate == 90, "integrated engine rotates selected pages");
        using (var bitmap = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(textPdf, 0, ""))) Check(bitmap.Width > 0 && bitmap.Height > 0, "native PDF preview renders");
        Check(File.ReadAllText(Run(Job(ToolKind.Text))).Contains("PRIVATE DOCUMENT"), "text extracted");
        var split = Run(Job(ToolKind.Split, input)); Check(Directory.GetFiles(split, "*.pdf").Length == 3, "split writes separate PDFs");
        foreach (var kind in new[] { ToolKind.Png, ToolKind.Jpeg })
        {
            var output = Run(Job(kind)); var exported = Directory.GetFiles(output).Single();
            using var bitmap = SkiaSharp.SKBitmap.Decode(exported); Check(bitmap.Width > 0, kind + " export decodes");
        }
        foreach (var kind in new[] { ToolKind.Flatten, ToolKind.Grayscale })
        {
            var output = Run(Job(kind)); using var pdf = UglyToad.PdfPig.PdfDocument.Open(output);
            Check(pdf.NumberOfPages == 1 && string.IsNullOrWhiteSpace(pdf.GetPage(1).Text), kind + " contains image without original text");
        }
        var compressed = Run(Job(ToolKind.Compress));
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(compressed)) Check(pdf.GetPage(1).Text.Contains("PRIVATE"), "compression preserves text");
        Check(new FileInfo(compressed).Length <= original.Length, "compression never increases size");
        var watermark = Run(Job(ToolKind.Watermark) with { Text = "CONFIDENCIAL" });
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(watermark)) Check(pdf.GetPage(1).Text.Contains("CONFIDENCIAL") && pdf.GetPage(1).Text.Contains("PRIVATE"), "watermark preserves original text");
        var numbered = Run(Job(ToolKind.Number) with { StartNumber = 7 });
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(numbered)) Check(pdf.GetPage(1).Text.Contains("7"), "page number added");
        var cropped = Run(Job(ToolKind.Crop) with { Margin = 10 });
        using (var pdf = PdfReader.Open(cropped, PdfDocumentOpenMode.Import)) Check(pdf.Pages[0].CropBox.Width < pdf.Pages[0].MediaBox.Width, "crop changes visible box");
        var cleaned = Run(Job(ToolKind.Metadata) with { ClearMetadata = true });
        using (var pdf = PdfReader.Open(cleaned, PdfDocumentOpenMode.Import)) Check(pdf.Info.Title.Length == 0 && pdf.Info.Author.Length == 0, "metadata cleared");
        var edited = Run(Job(ToolKind.Metadata) with { Text = "New title", Author = "New author" });
        using (var pdf = PdfReader.Open(edited, PdfDocumentOpenMode.Import)) Check(pdf.Info.Title == "New title" && pdf.Info.Author == "New author", "metadata edited");
        var encrypted = Run(Job(ToolKind.Protect) with { NewPassword = "AiOPDF-test-123" });
        try { using var pdf = PdfReader.Open(encrypted, PdfDocumentOpenMode.Import); throw new Exception("encryption not applied"); }
        catch (PdfReaderException) { Check(true, "encrypted PDF requires password"); }
        using (var pdf = PdfReader.Open(encrypted, "AiOPDF-test-123", PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 1, "password opens encrypted PDF");
        var unlocked = Run(Job(ToolKind.Unlock, encrypted) with { Password = "AiOPDF-test-123" });
        using (var pdf = PdfReader.Open(unlocked, PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 1, "unlock removes password");
        var comparison = Run(Job(ToolKind.Compare) with { Inputs = [textPdf, textPdf] }); Check(File.ReadAllText(Path.Combine(comparison, "comparacion.txt")).Contains("0 páginas con cambios"), "equal PDF text compares equal");
        var different = Run(Job(ToolKind.Compare) with { Inputs = [textPdf, watermark] });
        Check(File.ReadAllText(Path.Combine(different, "comparacion.txt")).Contains("Añadido:"), "comparison detects added watermark text");
        var coloredImage = Path.Combine(folder, "colored.png");
        using (var bitmap = new SkiaSharp.SKBitmap(100, 100)) { bitmap.Erase(SkiaSharp.SKColors.Coral); using var encoded = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100); File.WriteAllBytes(coloredImage, encoded.ToArray()); }
        var coloredPdf = Run(Job(ToolKind.Images) with { Inputs = [coloredImage] });
        var grayPdf = Run(Job(ToolKind.Grayscale, coloredPdf));
        using (var bitmap = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(grayPdf, 0, "")))
        { var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2); Check(Math.Abs(pixel.Red - pixel.Green) < 3 && Math.Abs(pixel.Green - pixel.Blue) < 3, "grayscale removes source color"); }
        var redacted = Run(Job(ToolKind.Redact) with { RedactX = 0, RedactY = 0, RedactWidth = 100, RedactHeight = 100 });
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(redacted)) Check(string.IsNullOrWhiteSpace(pdf.GetPage(1).Text), "redaction contains no source text");
        using (var bmp = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(redacted, 0, ""))) Check(bmp.GetPixel(bmp.Width / 2, bmp.Height / 2).Red < 10, "redaction renders black");
        foreach (var kind in new[] { ToolKind.Word, ToolKind.Excel, ToolKind.Slides })
        {
            var output = Run(Job(kind));
            using OpenXmlPackage package = kind switch { ToolKind.Word => WordprocessingDocument.Open(output, false), ToolKind.Excel => SpreadsheetDocument.Open(output, false), _ => PresentationDocument.Open(output, false) };
            var errors = new OpenXmlValidator().Validate(package).Take(5).ToArray();
            Check(errors.Length == 0, kind + " Open XML valid: " + string.Join("; ", errors.Select(e => e.Description)));
        }
        var ocrText = Run(Job(ToolKind.OcrText) with { Dpi = 144 }); Check(File.ReadAllText(ocrText).Contains("PRIVATE", StringComparison.OrdinalIgnoreCase), "offline OCR recognizes text");
        var ocrPdf = Run(Job(ToolKind.Ocr) with { Dpi = 144 });
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(ocrPdf)) Check(pdf.GetPage(1).Text.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase), "OCR PDF has searchable text");
        var abandoned = Job(ToolKind.Split, input); using var canceled = new CancellationTokenSource(); canceled.Cancel();
        try { PdfEngine.Run(abandoned, canceled.Token); throw new Exception("ignored cancellation"); } catch (OperationCanceledException) { Check(!Directory.Exists(abandoned.Output), "folder cancellation leaves no result"); }
        var interrupted = Job(ToolKind.Png, input); using var duringWork = new CancellationTokenSource(); var reports = 0;
        try { PdfEngine.Run(interrupted, duringWork.Token, new CallbackProgress(_ => { if (++reports == 2) duringWork.Cancel(); })); throw new Exception("ignored cancellation during work"); }
        catch (OperationCanceledException) { Check(reports >= 2 && !Directory.Exists(interrupted.Output), "mid-operation cancellation removes partial exports"); }
        Check(File.ReadAllBytes(textPdf).SequenceEqual(original), "extended operations preserve input");
        Check(!Directory.EnumerateFileSystemEntries(folder, ".aiopdf-*").Any(), "extended operations clean staging files");
    }
}

sealed class CallbackProgress(Action<string> callback) : IProgress<string>
{
    public void Report(string value) => callback(value);
}

