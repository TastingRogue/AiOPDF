using AiOPDF;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
static class CorpusChecks
{
    public static void Run()
    {
        AppFontResolver.Initialize(); var root = Path.Combine(Path.GetFullPath("artifacts"), "corpus-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS corpus " + name); }
        var source = Path.Combine(root, "accented-columns.pdf");
        using (var pdf = new PdfDocument())
        {
            var page = pdf.AddPage(); using var g = XGraphics.FromPdfPage(page);
            g.DrawString("Español: acción, año, privacidad.", new XFont("AiOPDF Sans", 14), XBrushes.Black, 40, 80);
            g.DrawString("Columna izquierda 125 EUR", new XFont("AiOPDF Sans", 12), XBrushes.Black, 40, 130);
            g.DrawString("Columna derecha 150 EUR", new XFont("AiOPDF Sans", 12), XBrushes.Black, 300, 130);
            pdf.Save(source);
        }
        var same = ContentComparison.Run(source, source, "", default).Pages.Single(); Check(same.State == "equal" && ComparisonMarkup.Locate(same, true).Length == 0, "accented text and columns agree between report and markup");
        var docx = Path.Combine(root, "text.docx"); PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Word), [source], docx), default);
        using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(docx, false)) Check(document.MainDocumentPart!.Document!.InnerText.Contains("acción"), "accented text survives Office export");
        var corrupt = Path.Combine(root, "truncated.pdf"); File.WriteAllText(corrupt, "%PDF-1.7\ntruncated"); var output = Path.Combine(root, "bad-output.pdf");
        try { PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Rotate), [corrupt], output), default); throw new Exception("Corrupt PDF accepted"); } catch (Exception) when (!File.Exists(output)) { Check(Directory.GetFileSystemEntries(root, ".aiopdf-*").Length == 0, "corrupt PDF leaves no final or partial output"); }
        var changed = Path.Combine(root, "changed.pdf");
        using (var pdf = new PdfDocument()) { var page = pdf.AddPage(); using var g = XGraphics.FromPdfPage(page); g.DrawString("Texto cambiado", new XFont("AiOPDF Sans", 14), XBrushes.Black, 40, 80); pdf.Save(changed); }
        var comparison = ContentComparison.Run(source, changed, "", default).Pages.Single();
        Check(ComparisonMarkup.Locate(comparison, true).Length > 0 && ComparisonMarkup.Locate(comparison, false).Length > 0, "shared diff produces both sides of changed content");
        File.WriteAllText(Path.Combine(root, "README.txt"), "Generated synthetic fixtures; no third-party documents. Covers columns, Spanish accents and truncated PDF. Signed documents, forms and broad scan quality still require an expanded acceptance corpus.");
        Console.WriteLine("Evidence: " + root);
    }
}
