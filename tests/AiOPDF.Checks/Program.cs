using AiOPDF;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
if (args.Contains("--localization")) { LocalizationChecks.Run(); return; }
if (args.Length > 1 && args[0] == "--readiness-child")
{
    using var session = new PrivateReviewSession(args[1]);
    File.WriteAllText(Path.Combine(session.DirectoryPath, "private.txt"), "synthetic private document");
    Console.WriteLine(session.DirectoryPath); Console.Out.Flush(); Console.In.ReadLine(); return;
}

if (args.Contains("--native-results"))
{
    var outputRoot = Path.GetFullPath("output/pdf");
    using (var merged = PdfReader.Open(Path.Combine(outputRoot, "botones-unir-validado.pdf"), PdfDocumentOpenMode.Import))
        if (merged.PageCount != 6) throw new Exception("Native save did not create six pages");
    var splitFolder = Directory.GetDirectories(outputRoot, "AiOPDF-Split-*").OrderByDescending(Directory.GetCreationTimeUtc).First();
    var splitFiles = Directory.GetFiles(splitFolder, "*.pdf"); if (splitFiles.Length != 3) throw new Exception("Native folder save did not create three PDFs");
    foreach (var file in splitFiles)
    {
        using var pdf = PdfReader.Open(file, PdfDocumentOpenMode.Import); if (pdf.PageCount != 1) throw new Exception("Split PDF must have one page");
        using var image = SkiaSharp.SKBitmap.Decode(PdfEngine.Thumbnail(file, 0, "")); if (image.Width < 1) throw new Exception("Native result cannot render");
    }
    Console.WriteLine("PASS native file dialog saved six-page PDF"); Console.WriteLine("PASS native folder dialog saved three renderable PDFs"); return;
}
if (args.Contains("--native-dialogs")) { NativeButtonApp.Run(); return; }
if (args.Contains("--ui")) { UiChecks.Run(); return; }
if (args.Contains("--ui-preview")) { UiChecks.Run(validateFlows: false); return; }
if (args.Contains("--assets")) { AssetChecks.Run(); return; }
if (args.Contains("--comparison")) { ComparisonChecks.Run(); return; }
if (args.Contains("--audit")) { BehaviorAuditChecks.Run(); return; }
if (args.Contains("--reference-study")) { ReferenceStudy.Run(); return; }
if (args.Contains("--improvements-en")) { ImprovementChecks.Run("en"); return; }
if (args.Contains("--improvements")) { ImprovementChecks.Run(); return; }
if (args.Contains("--readiness")) { ReleaseReadinessChecks.Run(); return; }
if (args.Contains("--stability")) { StabilityChecks.Run(); return; }
if (args.Contains("--corpus")) { CorpusChecks.Run(); return; }

var folder = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try
{
    var input = Path.Combine(folder, "source.pdf");
    using (var source = new PdfDocument())
    {
        for (var i = 1; i <= 3; i++) source.AddPage().Width = PdfSharp.Drawing.XUnit.FromPoint(200 + i);
        source.Save(input);
    }
    var original = File.ReadAllBytes(input);
    void Check(bool success, string label) { if (!success) throw new Exception(label); Console.WriteLine("PASS " + label); }
    string Run(string tool, string pages, int rotation = 90, string[]? inputs = null)
    {
        var output = Path.Combine(folder, Guid.NewGuid() + ".pdf");
        PdfOperations.Run(tool, inputs ?? new[] { input }, output, pages, rotation, CancellationToken.None);
        return output;
    }
    using (var result = PdfReader.Open(Run("Unir PDF", "", inputs: new[] { input, input }), PdfDocumentOpenMode.Import))
        Check(result.PageCount == 6, "merge preserves all pages");
    using (var result = PdfReader.Open(Run("Extraer / ordenar", "3,1"), PdfDocumentOpenMode.Import))
        Check(result.PageCount == 2 && result.Pages[0].Width.Point == 203 && result.Pages[1].Width.Point == 201, "selection preserves requested order");
    using (var result = PdfReader.Open(Run("Eliminar páginas", "2"), PdfDocumentOpenMode.Import))
        Check(result.PageCount == 2 && result.Pages[1].Width.Point == 203, "delete preserves remaining pages");
    using (var result = PdfReader.Open(Run("Rotar PDF", "2"), PdfDocumentOpenMode.Import))
        Check(result.PageCount == 3 && result.Pages[0].Rotate == 0 && result.Pages[1].Rotate == 90, "rotation preserves unselected pages");
    var image = Path.Combine(folder, "image.png");
    using (var bitmap = new SkiaSharp.SKBitmap(20, 20))
    {
        bitmap.Erase(SkiaSharp.SKColors.Red);
        using var encoded = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(image, encoded.ToArray());
    }
    using (var result = PdfReader.Open(Run("Imágenes a PDF", "", inputs: new[] { image }), PdfDocumentOpenMode.Import))
        Check(result.PageCount == 1, "image creates PDF page");
    var invalid = Path.Combine(folder, "invalid.pdf");
    try { PdfOperations.Run("Extraer / ordenar", new[] { input }, invalid, "4", 90, CancellationToken.None); throw new Exception("accepted invalid range"); }
    catch (ArgumentException) { Check(!File.Exists(invalid), "invalid range leaves no output"); }
    try { PdfOperations.Run("Unir PDF", new[] { input }, input, "", 90, CancellationToken.None); throw new Exception("overwrote original"); }
    catch (ArgumentException) { Check(File.ReadAllBytes(input).SequenceEqual(original), "original protected"); }
    using var canceled = new CancellationTokenSource(); canceled.Cancel();
    try { PdfOperations.Run("Unir PDF", new[] { input }, invalid, "", 90, canceled.Token); throw new Exception("ignored cancellation"); }
    catch (OperationCanceledException) { Check(!File.Exists(invalid), "cancellation leaves no output"); }
    Check(!Directory.GetFiles(folder, "*.tmp").Any(), "temporary files cleaned");
    ExtendedChecks.Run(folder, input);
}
finally { Directory.Delete(folder, recursive: true); }
