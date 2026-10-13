using AiOPDF;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.VisualTree;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

static class ImprovementChecks
{
    static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static void Await(Task task)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted && timer.Elapsed.TotalSeconds < 90) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
        task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
    }
    public static void Run(string language = "es")
    {
        AppFontResolver.Initialize(); var root = Path.GetFullPath("artifacts/improvement-checks"); Directory.CreateDirectory(root);
        var work = Path.Combine(root, "run-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(work);
        L10n.Initialize(Path.Combine(work, "settings.json")); L10n.SetLanguage(language);
        var samples = SampleDocuments.Create(); var bytes = File.ReadAllBytes(samples[0]);
        PdfTool Tool(ToolKind kind) => ToolCatalog.All.Single(t => t.Kind == kind);
        string Text(string path, int page) { using var pdf = UglyToad.PdfPig.PdfDocument.Open(path); return pdf.GetPage(page).Text; }
        var merged = Path.Combine(work, "merged.pdf"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Merge), samples, merged), default);
        var split = Path.Combine(work, "ranges"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Split), [merged], split, Pages: "1-2,3-6", SplitMode: 1), default);
        var files = Directory.GetFiles(split).Order().ToArray();
        Check(files.Length == 2 && PdfEngine.PageCount(files[0]) == 2 && PdfEngine.PageCount(files[1]) == 4, "ranges 1–2 / 3–6 produce two PDFs of 2 and 4 pages");
        Check(Text(files[1], 2).Contains("140") && Text(files[0], 1).Contains("125"), "range division preserves A/B text and order");
        var fixedGroups = Path.Combine(work, "fixed"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Split), [merged], fixedGroups, SplitMode: 2, GroupSize: 2), default);
        Check(Directory.GetFiles(fixedGroups).Length == 3 && Directory.GetFiles(fixedGroups).All(f => PdfEngine.PageCount(f) == 2), "fixed groups create three two-page PDFs");
        try { PdfEngine.Run(new PdfJob(Tool(ToolKind.Split), [merged], Path.Combine(work, "invalid"), Pages: "1-2,7", SplitMode: 1), default); throw new Exception("Invalid range accepted"); }
        catch (ArgumentException) { Check(!Directory.Exists(Path.Combine(work, "invalid")), "invalid group leaves no partial destination"); }
        var numbers = Path.Combine(work, "numbers.pdf"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Number), [samples[0]], numbers, Pages: "3", StartNumber: 7, NumberFormat: "Página n", StampPosition: "Arriba · derecha"), default);
        Check(Text(numbers, 3).Contains("Página 7") && !Text(numbers, 1).Contains("Página 7"), "number format and selected-page scope reach output");
        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(numbers)) Check(pdf.GetPage(3).GetWords().Any(w => w.Text == "7" && w.BoundingBox.Bottom > 750), "top numbering is positioned at the top");
        var geometry = Path.Combine(work, "geometry.pdf");
        using (var pdf = new PdfDocument())
        {
            foreach (var rotation in new[] { 0, 90, 180, 270 }) { var p = pdf.AddPage(); p.Width = XUnit.FromPoint(400); p.Height = XUnit.FromPoint(300); p.CropBox = new PdfRectangle(new XPoint(50, 30), new XPoint(350, 270)); p.Rotate = rotation; }
            pdf.Save(geometry);
        }
        var cropped = Path.Combine(work, "crop.pdf"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Crop), [geometry], cropped, Pages: "1,2,3,4", RedactX: 10, RedactY: 20, RedactWidth: 50, RedactHeight: 40, CropRegion: true), default);
        using (var before = PdfEngine.Open(geometry)) using (var after = PdfEngine.Open(cropped))
            for (var i = 0; i < 4; i++) { var a = PdfGeometry.VisibleSize(before.Pages[i]); var b = PdfGeometry.VisibleSize(after.Pages[i]); Check(Math.Abs(b.Width - a.Width * .5) < .01 && Math.Abs(b.Height - a.Height * .4) < .01, "crop handles visible box and rotation " + before.Pages[i].Rotate); }
        var png = Path.Combine(work, "image.png"); File.WriteAllBytes(png, PdfEngine.Thumbnail(samples[0], 1, "", 800));
        var imagePdf = Path.Combine(work, "a4.pdf"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Images), [png], imagePdf, ImagePaper: "A4", Landscape: true, ImageMargin: 15), default);
        using (var pdf = PdfEngine.Open(imagePdf)) Check(Math.Abs(pdf.Pages[0].Width.Point - 841.89) < .01 && Math.Abs(pdf.Pages[0].Height.Point - 595.276) < .01, "image paper and orientation change actual PDF geometry");
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow(); window.Show(); Dispatcher.UIThread.RunJobs();
        T Field<T>(string name) where T : Control => window.GetLogicalDescendants().OfType<T>().Single(c => AutomationProperties.GetName(c) == L10n.Translate(name));
        foreach (var kind in Enum.GetValues<ToolKind>().Where(k => k != ToolKind.Compare))
        {
            window.SelectTool(Tool(kind));
            var inputList = window.GetLogicalDescendants().OfType<ListBox>().Single();
            while (inputList.ItemCount > 0) { inputList.SelectedIndex = 0; window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == L10n.Translate("Quitar")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
            var input = kind == ToolKind.Images ? png : samples[0];
            if (kind == ToolKind.Unlock) { input = Path.Combine(work, "protected.pdf"); PdfEngine.Run(new PdfJob(Tool(ToolKind.Protect), [samples[0]], input, NewPassword: "Sample-123456"), default); }
            Await(window.AddPaths(kind == ToolKind.Merge ? samples : [input]));
            if (kind == ToolKind.Unlock) Field<TextBox>("Contraseña del PDF").Text = "Sample-123456";
            if (kind == ToolKind.Protect) { Field<TextBox>("Nueva contraseña").Text = "Sample-123456"; Field<TextBox>("Repite la contraseña").Text = "Sample-123456"; }
            if (kind == ToolKind.Delete) Field<TextBox>("Páginas que quieres eliminar").Text = "2";
            if (kind == ToolKind.Watermark) Field<TextBox>("Texto de la marca").Text = "MUESTRA";
            if (kind == ToolKind.Crop) { Field<Avalonia.Controls.NumericUpDown>("Ancho (%)").Value = 70; }
            foreach (var dpi in window.GetLogicalDescendants().OfType<NumericUpDown>().Where(c => (AutomationProperties.GetName(c) ?? "").Contains("DPI"))) dpi.Value = 96;
            var prepare = window.PrepareResult(); Await(prepare); Check(prepare.Result, "prepare and review " + kind);
            var destination = Path.Combine(work, "reviewed-" + kind + (Tool(kind).Folder ? "" : kind is ToolKind.Text or ToolKind.OcrText ? ".txt" : kind == ToolKind.Word ? ".docx" : kind == ToolKind.Excel ? ".xlsx" : kind == ToolKind.Slides ? ".pptx" : ".pdf"));
            Check(!File.Exists(destination) && !Directory.Exists(destination), "prepare does not save to final destination " + kind);
            var savedCopy = window.SavePreparedToPathAsync(destination); Await(savedCopy); if (!savedCopy.Result) throw new Exception("save reviewed copy " + kind + " · " + string.Join(" | ", window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text)));
            Check(savedCopy.Result, "save reviewed copy " + kind);
            if (kind == ToolKind.Split) { window.Width = 1440; window.Height = 1120; Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame(); frame!.Save(work + "/reviewed-split.png", PngBitmapEncoderOptions.Default); }
            Check(!window.SavePreparedToPath(destination), "review save rejects overwrite " + kind);
        }
        window.SelectTool(Tool(ToolKind.Split)); Await(window.AddPaths([merged])); Field<ComboBox>("Cómo dividir").SelectedIndex = 1; Field<TextBox>("Páginas").Text = "1-2,3-6";
        var mode = Field<ComboBox>("Cómo dividir"); mode.IsDropDownOpen = true; Dispatcher.UIThread.RunJobs();
        Check(mode.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single().IsOpen, "custom selector opens its options"); mode.IsDropDownOpen = false;
        var ranges = window.PrepareResult(); Await(ranges); Check(ranges.Result, "UI range preparation");
        var rangeDest = Path.Combine(work, "ui-ranges"); Check(window.SavePreparedToPath(rangeDest) && Directory.GetFiles(rangeDest).Length == 2, "UI ranges save two reviewed files");
        window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == L10n.Translate("Usar este PDF en otra herramienta")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (var i = 0; i < 60; i++) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(20); }
        var reused = window.GetLogicalDescendants().OfType<ListBox>().Single().Items.Cast<string>().Single();
        Check(PdfEngine.PageCount(reused) == 2 && reused.Contains("AiOPDF-review-"), "continue uses selected result rather than original");
        var reusedRoot = Path.GetDirectoryName(Path.GetDirectoryName(reused))!;
        window.SelectTool(Tool(ToolKind.Compare)); Await(window.AddPaths(samples)); var compare = window.CompareDocuments(); Await(compare); Check(compare.Result, "parallel comparison reader");
        var change = window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == L10n.Translate("Siguiente cambio")); change.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (var i = 0; i < 30; i++) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(20); }
        Check(window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text?.StartsWith(L10n.Code == "en" ? "Change 1 of" : "Cambio 1 de") == true), "change navigation selects first difference");
        var originalWidth = Field<Avalonia.Controls.Image>("Página del PDF original").Width;
        Field<NumericUpDown>("Zoom de comparación (%)").Value = 150; Dispatcher.UIThread.RunJobs();
        Check(Field<Avalonia.Controls.Image>("Página del PDF original").Width > originalWidth, "zoom enlarges actual page within scrollable viewer");
        Field<NumericUpDown>("Zoom de comparación (%)").Value = 100;
        foreach (var size in new[] { (640, 540), (1280, 800), (1920, 1080), (2560, 1380) })
        {
            window.Width = size.Item1; window.Height = size.Item2; Dispatcher.UIThread.RunJobs();
            foreach (var name in new[] { "Página del PDF original", "Página del PDF actualizado" })
            {
                var pageImage = Field<Avalonia.Controls.Image>(name);
                var viewer = pageImage.GetVisualAncestors().OfType<ScrollViewer>().First();
                Check(viewer.Extent.Width <= viewer.Viewport.Width + 1 && viewer.Extent.Height <= viewer.Viewport.Height + 1, $"complete comparison page fits at 100% {name} {size.Item1}");
            }
            using var fullPageFrame = window.CaptureRenderedFrame(); fullPageFrame!.Save($"{work}/comparison-full-page-{size.Item1}.png", PngBitmapEncoderOptions.Default);
        }
        window.Width = 1440; window.Height = 1120; Dispatcher.UIThread.RunJobs(); using (var frame = window.CaptureRenderedFrame()) frame!.Save(work + "/comparison-reader.png", PngBitmapEncoderOptions.Default);
        window.SelectTool(Tool(ToolKind.Crop)); Await(window.AddPaths(samples.Take(1))); Field<NumericUpDown>("Ancho (%)").Value = 70; Field<NumericUpDown>("Alto (%)").Value = 60;
        Dispatcher.UIThread.RunJobs();
        var editor = window.GetVisualDescendants().OfType<RegionEditor>().Single();
        var initial = editor.Selection;
        var from = editor.TranslatePoint(new Point(editor.Bounds.Width / 2 - 70, 100), window)!.Value;
        var to = editor.TranslatePoint(new Point(editor.Bounds.Width / 2 + 70, 220), window)!.Value;
        window.MouseMove(from); window.MouseDown(from, MouseButton.Left); window.MouseMove(to); window.MouseUp(to, MouseButton.Left); Dispatcher.UIThread.RunJobs();
        Check(editor.Selection != initial && Math.Abs((double)Field<NumericUpDown>("Ancho (%)").Value! - editor.Selection.Width) < .01, "pointer drag updates actual region values");
        window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == L10n.Translate("Deshacer región")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(editor.Selection == initial, "undo restores drawn region");
        Dispatcher.UIThread.RunJobs(); using (var frame = window.CaptureRenderedFrame()) frame!.Save(work + "/visual-crop.png", PngBitmapEncoderOptions.Default);
        window.Close(); Check(File.ReadAllBytes(samples[0]).SequenceEqual(bytes), "new workflows preserve original bytes");
        Check(!Directory.Exists(reusedRoot), "closing cleans owned review temporaries");
        Check(!Directory.EnumerateFileSystemEntries(work, ".aiopdf-*").Any(), "reviewed save cleans staging files");
        Console.WriteLine("Evidence: " + work);
    }
}

