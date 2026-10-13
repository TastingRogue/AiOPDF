using AiOPDF;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

static class ComparisonChecks
{
    public static void Run()
    {
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS content comparison " + name); }
        var root = Path.Combine(Path.GetFullPath("artifacts"), "comparison-checks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        string Pdf(string name, string[] pages, bool differentLayout = false)
        {
            var path = Path.Combine(root, name + ".pdf"); using var pdf = new PdfDocument();
            foreach (var content in pages) { var page = pdf.AddPage(); using var graphics = XGraphics.FromPdfPage(page); if (content.Length > 0) graphics.DrawString(content, new XFont("AiOPDF Sans", differentLayout ? 22 : 14), differentLayout ? XBrushes.Purple : XBrushes.Black, differentLayout ? 90 : 40, differentLayout ? 180 : 80); }
            pdf.Save(path); return path;
        }
        try
        {
            AppFontResolver.Initialize(); var a = Pdf("original", ["Contrato Total 100 EUR"]); var layout = Pdf("layout", ["Contrato Total 100 EUR"], true);
            Check(ContentComparison.Run(a, layout, "", default).Pages.Single().State == "equal", "font position and color changes do not alter text");
            var b = Pdf("updated", ["Contrato Total 150 EUR"]); var result = ContentComparison.Run(a, b, "", default);
            Check(ComparisonMarkup.Locate(a, layout, "", 1, true).Length == 0, "layout changes do not receive underlines");
            Check(ComparisonMarkup.Locate(a, b, "", 1, true).Length == 1 && ComparisonMarkup.Locate(a, b, "", 1, false).Length == 1, "only changed amount is underlined on both pages");
            var repeatedA = Pdf("repeated-a", ["Total 100 Total 100"]); var repeatedB = Pdf("repeated-b", ["Total 100 Total 150"]);
            var repeatedMarks = ComparisonMarkup.Locate(repeatedA, repeatedB, "", 1, true);
            Check(repeatedMarks.Length == 1 && repeatedMarks[0].Left > ComparisonMarkup.Locate(a, b, "", 1, true)[0].Left, "repeated words mark only changed occurrence");
            foreach (var originalSide in new[] { true, false })
            {
                var marked = ComparisonMarkup.Render(a, b, "", 1, originalSide);
                using var image = SkiaSharp.SKBitmap.Decode(marked);
                var color = SkiaSharp.SKColor.Parse(originalSide ? "#D02962" : "#008573"); var colored = 0;
                for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) if (image.GetPixel(x, y) == color) colored++;
                Check(colored > 10, "render contains visible underline " + originalSide);
            }
            foreach (var rotation in new[] { 90, 180, 270 })
            {
                var rotated = new List<string>();
                foreach (var source in new[] { a, b })
                {
                    var destination = Path.Combine(root, Path.GetFileNameWithoutExtension(source) + "-" + rotation + ".pdf");
                    using var document = PdfSharp.Pdf.IO.PdfReader.Open(source, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify); document.Pages[0].Rotate = rotation; document.Save(destination); rotated.Add(destination);
                }
                var marks = ComparisonMarkup.Locate(rotated[0], rotated[1], "", 1, true);
                Check(marks.Length == 1 && new[] { marks[0].Left, marks[0].Right, marks[0].Bottom, marks[0].EndBottom }.All(v => v >= 0 && v <= 1), "rotated underline remains on page " + rotation);
            }
            Check(result.Pages[0].Changes.Any(c => c.Kind == "removed" && c.Text == "100") && result.Pages[0].Changes.Any(c => c.Kind == "added" && c.Text == "150"), "amount change identifies old and new content");
            var hunk = ContentComparison.Hunks(result.Pages[0].Changes).Single(); Check(hunk.Before.Contains("Total") && hunk.After.Contains("EUR"), "changed amount retains surrounding context");
            Check(ContentComparison.Diff("a\n b   c", "a b\tc", default).All(c => c.Kind == "equal"), "line reflow and spaces are ignored");
            foreach (var pair in new[] { ("a b a", "a a b"), ("", "nuevo texto"), ("texto eliminado", ""), ("Total 100", "Total 100,00"), ("Sí", "Si") })
            {
                var changes = ContentComparison.Diff(pair.Item1, pair.Item2, default);
                Check(string.Join(" ", changes.Where(c => c.Kind != "added").Select(c => c.Text)) == pair.Item1 && string.Join(" ", changes.Where(c => c.Kind != "removed").Select(c => c.Text)) == pair.Item2, "diff reconstructs both versions " + pair.Item1);
            }
            var extra = Pdf("extra-page", ["Contrato Total 100 EUR", "Nueva cláusula"]);
            Check(ContentComparison.Run(a, extra, "", default).Pages[1].Changes.Any(c => c.Kind == "added" && c.Text.Contains("Nueva")), "added page content is reported");
            Check(ContentComparison.Run(extra, a, "", default).Pages[1].Changes.Any(c => c.Kind == "removed"), "deleted page content is reported");
            var blank = Pdf("scan-placeholder", [""]); Check(ContentComparison.Run(blank, blank, "", default).Pages[0].State == "unreadable", "pages without text are never declared equal");
            Check(ContentComparison.Run(a, blank, "", default).Pages[0].State == "unreadable", "one unreadable version makes comparison incomplete");
            var longDiff = ContentComparison.Diff(string.Join(" ", Enumerable.Range(0, 3000).Select(i => "old" + i)), string.Join(" ", Enumerable.Range(0, 3000).Select(i => "new" + i)), default);
            Check(longDiff.Length == 2 && longDiff[0].Kind == "removed" && longDiff[1].Kind == "added", "large changed page uses bounded memory");
            using var canceled = new CancellationTokenSource(); canceled.Cancel(); try { ContentComparison.Run(a, b, "", canceled.Token); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { Check(true, "cancellation stops comparison"); }
            var output = Path.Combine(root, "export"); PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Compare), [a, b], output), default, comparisonResult: result);
            Check(Directory.GetFiles(output).Length == 2 && File.ReadAllText(Path.Combine(output, "comparacion.txt")).Contains("Eliminado: 100"), "export saves actual content report");
        }
        finally { Directory.Delete(root, true); }
    }
}
