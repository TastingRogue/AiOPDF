using AiOPDF;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using DocumentFormat.OpenXml.Packaging;

static class UiFlowChecks
{
    static void Await(Task task)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted && timer.Elapsed < TimeSpan.FromSeconds(90)) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
        if (!task.IsCompleted) throw new TimeoutException("UI operation timed out");
        task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
    }

    public static void Run(MainWindow window, string sample, string? partner = null)
    {
        var folder = Path.Combine(Path.GetFullPath("artifacts"), "ui-flows-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var original = File.ReadAllBytes(sample); var completed = new HashSet<ToolKind>();
        void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
        T Field<T>(string name) where T : Control => window.GetLogicalDescendants().OfType<T>().Single(c => AutomationProperties.GetName(c) == name);
        try
        {
            var imageFiles = new List<string>();
            foreach (var format in new[] { SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Webp })
            {
                var path = Path.Combine(folder, "input." + format.ToString().ToLowerInvariant()); using var bitmap = SKBitmap.Decode(PdfEngine.Thumbnail(format == SKEncodedImageFormat.Jpeg ? partner ?? sample : sample, 0, "", 800));
                using var bytes = bitmap.Encode(format, 90); File.WriteAllBytes(path, bytes.ToArray()); imageFiles.Add(path);
            }
            var protectedPdf = Path.Combine(folder, "protected.pdf");
            PdfEngine.Run(new PdfJob(ToolCatalog.All.Single(t => t.Kind == ToolKind.Protect), [sample], protectedPdf, NewPassword: "Secret-123456"), CancellationToken.None);
            foreach (var tool in ToolCatalog.All)
            {
                window.SelectTool(tool); Dispatcher.UIThread.RunJobs();
                var inputList = window.GetLogicalDescendants().OfType<ListBox>().Single();
                while (inputList.ItemCount > 0)
                {
                    inputList.SelectedIndex = 0;
                    window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Quitar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Dispatcher.UIThread.RunJobs();
                }
                Await(window.AddPaths(tool.Kind == ToolKind.Images ? imageFiles : tool.Kind == ToolKind.Unlock ? [protectedPdf] : tool.Multiple ? [sample, partner ?? protectedPdf] : [sample]));
                if ((tool.Multiple && tool.Kind != ToolKind.Images) || tool.Kind == ToolKind.Unlock) Field<TextBox>("Contraseña del PDF").Text = "Secret-123456";
                if (tool.Kind == ToolKind.Delete) Field<TextBox>("Páginas que quieres eliminar").Text = "2";
                if (tool.Kind == ToolKind.Organize) Field<TextBox>("Páginas").Text = "3,1,1";
                if (tool.Kind == ToolKind.Protect) Field<TextBox>("Nueva contraseña").Text = "Secret-123456";
                if (tool.Kind == ToolKind.Watermark) Field<TextBox>("Texto de la marca").Text = "REVISADO";
                if (tool.Kind == ToolKind.Metadata) { Field<TextBox>("Título").Text = "Prueba desde la interfaz"; Field<TextBox>("Autor").Text = "AiOPDF"; }
                foreach (var control in window.GetLogicalDescendants().OfType<NumericUpDown>().Where(c => (AutomationProperties.GetName(c) ?? "").Contains("DPI"))) control.Value = 96;
                if (tool.Kind == ToolKind.Compare)
                {
                    var comparison = window.CompareDocuments(); Await(comparison); Check(comparison.Result, "UI comparison runs before export");
                    Check(Field<TextBlock>("Resumen de comparación").Text!.Contains("de OCR"), "UI reports unreadable pages");
                    Check(!Directory.Exists(Path.Combine(folder, tool.Kind.ToString())), "UI comparison creates no output before export");
                    window.Width = 1445; window.Height = 1120; Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame(); frame!.Save("docs/screenshots/content-comparison.png", PngBitmapEncoderOptions.Default);
                }
                var output = Path.Combine(folder, tool.Kind.ToString()); var saving = window.SaveToPath(output); Await(saving); Check(saving.Result, "UI save " + tool.Kind + (saving.Result ? "" : ": " + Field<TextBlock>("Estado del procesamiento").Text));
                Check(Field<TextBlock>("Ubicación del resultado").Text == output, "UI exposes saved location " + tool.Kind);
                if (tool.Kind == ToolKind.Split) { window.Width = 1220; window.Height = 880; Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame(); frame!.Save("docs/screenshots/result-panel.png", PngBitmapEncoderOptions.Default); }
                if (tool.Folder)
                {
                    var results = Directory.GetFiles(output); Check(results.Length >= (tool.Kind == ToolKind.Compare ? 2 : 3), "UI folder output " + tool.Kind);
                    if (tool.Kind == ToolKind.Compare) Check(File.ReadAllText(Path.Combine(output, "comparacion.txt")).Contains("Añadido:"), "UI comparison report contains text changes");
                    foreach (var png in results.Where(p => p.EndsWith(".png") || p.EndsWith(".jpg"))) { using var bitmap = SKBitmap.Decode(png); Check(bitmap != null && bitmap.Width > 0, "UI image readable " + tool.Kind); }
                }
                else if (tool.Kind is ToolKind.Text or ToolKind.OcrText) Check(File.ReadAllText(output).Contains("AiOPDF", StringComparison.OrdinalIgnoreCase), "UI text content " + tool.Kind);
                else if (tool.Kind == ToolKind.Word) { using var doc = WordprocessingDocument.Open(output, false); Check(doc.MainDocumentPart!.Document!.InnerText.Contains("AiOPDF"), "UI Word content"); }
                else if (tool.Kind == ToolKind.Excel) { using var doc = SpreadsheetDocument.Open(output, false); Check(doc.WorkbookPart!.WorksheetParts.Single().Worksheet!.InnerText.Contains("AiOPDF"), "UI Excel content"); }
                else if (tool.Kind == ToolKind.Slides) { using var doc = PresentationDocument.Open(output, false); Check(doc.PresentationPart!.SlideParts.Count() == 3 && doc.PresentationPart.SlideParts.All(p => p.ImageParts.Any()), "UI slides contain pages"); }
                else
                {
                    using var pdf = PdfEngine.Open(output, tool.Kind == ToolKind.Protect ? "Secret-123456" : "");
                    var expected = tool.Kind == ToolKind.Merge ? 6 : tool.Kind == ToolKind.Delete ? 2 : 3;
                    Check(pdf.PageCount == expected, "UI PDF page count " + tool.Kind);
                    using var thumbnail = SKBitmap.Decode(PdfEngine.Thumbnail(output, 0, tool.Kind == ToolKind.Protect ? "Secret-123456" : "")); Check(thumbnail.Width > 0, "UI PDF rendered " + tool.Kind);
                }
                completed.Add(tool.Kind);
            }
            Check(completed.Count == ToolCatalog.All.Length, "all 25 tools covered through UI save flow");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Organize)); Await(window.AddPaths([sample]));
            Field<TextBox>("Páginas").Text = "3,1"; Dispatcher.UIThread.RunJobs();
            Check(Field<CheckBox>("Incluir página 2").IsChecked == false, "manual page order updates preview selection");
            Field<Button>("Mover página 3 después").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(Field<TextBox>("Páginas").Text == "1,3", "preview move updates page order");
            Field<CheckBox>("Incluir página 1").IsChecked = false; Field<CheckBox>("Incluir página 3").IsChecked = false;
            var noPages = Path.Combine(folder, "no-pages.pdf"); var emptySelection = window.SaveToPath(noPages); Await(emptySelection); Check(!emptySelection.Result && !File.Exists(noPages), "empty visual selection is rejected");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Delete)); Await(window.AddPaths([sample]));
            Field<TextBox>("Páginas que quieres eliminar").Text = "1-3"; var invalid = Path.Combine(folder, "invalid.pdf"); var failure = window.SaveToPath(invalid); Await(failure); Check(!failure.Result && !File.Exists(invalid), "UI rejects deleting every page");
            Field<TextBox>("Páginas que quieres eliminar").Text = "999"; failure = window.SaveToPath(invalid); Await(failure); Check(!failure.Result && !File.Exists(invalid), "UI rejects invalid page range");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Unlock)); Await(window.AddPaths([protectedPdf])); Field<TextBox>("Contraseña del PDF").Text = "wrong-password";
            failure = window.SaveToPath(invalid); Await(failure); Check(!failure.Result && !File.Exists(invalid), "UI rejects incorrect password");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Compress)); Await(window.AddPaths([sample]));
            failure = window.SaveToPath(sample); Await(failure); Check(!failure.Result, "UI rejects overwriting original");
            var canceledPath = Path.Combine(folder, "canceled.pdf"); var canceled = window.SaveToPath(canceledPath);
            window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Cancelar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Await(canceled);
            Check(!canceled.Result && !File.Exists(canceledPath), "UI cancel leaves no output");
            Check(File.ReadAllBytes(sample).SequenceEqual(original), "UI flows preserve original bytes");
            Check(!Directory.EnumerateFileSystemEntries(folder, ".aiopdf-*").Any(), "UI flows clean staging files");
        }
        finally { Directory.Delete(folder, true); }
    }
}


