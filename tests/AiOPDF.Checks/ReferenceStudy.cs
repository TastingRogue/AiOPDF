using AiOPDF;
using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

static class ReferenceStudy
{
    public static void Run()
    {
        var root = Path.GetFullPath("artifacts/ilovepdf-study");
        var rows = new List<string> { "# Inspección de resultados descargados de iLovePDF", "", "Muestras sintéticas A/B, 12 de octubre de 2026. Observaciones de archivos; no certifican cualquier documento.", "" };
        void Note(string text) { rows.Add("- " + text); Console.WriteLine(text); }
        foreach (var archive in Directory.GetFiles(root, "*.zip"))
        {
            using var zip = ZipFile.OpenRead(archive);
            var destination = Path.Combine(root, Path.GetFileNameWithoutExtension(archive)); Directory.CreateDirectory(destination);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                var path = Path.Combine(destination, Path.GetFileName(entry.Name));
                entry.ExtractToFile(path, overwrite: true);
                Note($"{Path.GetFileName(archive)}: {entry.Name}, {entry.Length} bytes.");
            }
        }
        foreach (var file in Directory.GetFiles(root, "*.pdf", SearchOption.AllDirectories).Where(p => !Path.GetRelativePath(root, p).StartsWith("aiopdf-", StringComparison.Ordinal)))
        {
            using var document = PdfEngine.Open(file);
            using var textDocument = UglyToad.PdfPig.PdfDocument.Open(file);
            Note($"{Path.GetRelativePath(root, file)}: {document.PageCount} páginas, {new FileInfo(file).Length} bytes.");
            for (var i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i]; var size = PdfGeometry.VisibleSize(page);
                var text = textDocument.GetPage(i + 1).Text;
                Note($"  Página {i + 1}: visible {size.Width:F2} × {size.Height:F2} pt, rotación {page.Rotate}°, {text.Length} caracteres; marcador TEST-000A: {text.Contains("TEST-000A")}; texto de informe: {text.Contains("Informe de prueba")}; final: «{text[^Math.Min(45, text.Length)..]}».");
            }
            File.WriteAllBytes(Path.Combine(root, Path.GetFileNameWithoutExtension(file) + "-preview.png"), PdfEngine.Thumbnail(file, 0, "", 600));
        }
        using (var document = WordprocessingDocument.Open(Path.Combine(root, "word.docx"), false))
        {
            var part = document.MainDocumentPart!;
            var body = part.Document!.Body!;
            Note($"Word: {body.Descendants<Paragraph>().Count()} párrafos, {body.Descendants<Table>().Count()} tablas, {body.Descendants<Drawing>().Count()} dibujos, {part.ImageParts.Count()} partes de imagen; texto de informe: {(body.InnerText ?? "").Contains("Informe de prueba")}.");
        }
        using (var document = SpreadsheetDocument.Open(Path.Combine(root, "excel.xlsx"), false))
        {
            var part = document.WorkbookPart!;
            Note($"Excel: {part.WorksheetParts.Count()} hojas, {part.WorksheetParts.Sum(p => p.Worksheet!.Descendants<DocumentFormat.OpenXml.Spreadsheet.Cell>().Count())} celdas. Tipos de celda: {string.Join(", ", part.WorksheetParts.SelectMany(p => p.Worksheet!.Descendants<DocumentFormat.OpenXml.Spreadsheet.Cell>()).GroupBy(c => c.DataType?.InnerText ?? "sin tipo").Select(g => g.Key + ": " + g.Count()))}.");
        }
        using (var document = PresentationDocument.Open(Path.Combine(root, "slides.pptx"), false))
        {
            var part = document.PresentationPart!;
            Note($"PowerPoint: {part.SlideParts.Count()} diapositivas, {part.SlideParts.Sum(p => p.Slide!.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Count())} fragmentos de texto editables, {part.SlideParts.Sum(p => p.ImageParts.Count())} partes de imagen.");
        }
        var local = Path.Combine(root, "aiopdf-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(local);
        long inputBytes = 0, localBytes = 0, referenceBytes = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(root, "split"), "*.pdf"))
        {
            var output = Path.Combine(local, Path.GetFileName(file));
            PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Compress), [file], output), default);
            inputBytes += new FileInfo(file).Length; localBytes += new FileInfo(output).Length;
        }
        referenceBytes = Directory.GetFiles(Path.Combine(root, "compressed"), "*.pdf").Sum(p => new FileInfo(p).Length);
        Note($"Mismos dos PDFs de rangos: entrada {inputBytes} bytes; iLovePDF recomendado {referenceBytes} bytes; AiOPDF sin pérdida {localBytes} bytes. Los mecanismos tienen distintos compromisos de calidad; no se infiere equivalencia de calidad solo por tamaño.");
        File.WriteAllLines(Path.Combine(root, "result-inspection.md"), rows);
    }
}


