using AiOPDF;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;

static class UiChecks
{
    public static void Run(bool validateFlows = true)
    {
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        Directory.CreateDirectory("docs/screenshots");
        var window = new MainWindow(); window.Show(); Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/home.png", PngBitmapEncoderOptions.Default);
        if (window.GetVisualDescendants().OfType<Button>().Count() < ToolCatalog.All.Length) throw new Exception("Missing tool cards");
        var search = window.GetVisualDescendants().OfType<TextBox>().Single();
        if (AutomationProperties.GetName(search) != "Buscar herramientas") throw new Exception("Search accessible name missing");
        search.Focus(NavigationMethod.Tab); window.KeyTextInput("OCR"); Dispatcher.UIThread.RunJobs();
        if (search.Text != "OCR") throw new Exception("Custom search does not accept keyboard text");
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Ocr)); window.ShowHome(); Dispatcher.UIThread.RunJobs();
        search = window.GetVisualDescendants().OfType<TextBox>().Single();
        if (search.Text != "OCR") throw new Exception("Back navigation lost search");
        search.Text = ""; Console.WriteLine("PASS custom text input and retained search");
        search.Text = "ninguna-herramienta"; Dispatcher.UIThread.RunJobs();
        var reset = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Limpiar búsqueda y filtros");
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/search-empty.png", PngBitmapEncoderOptions.Default);
        reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
        if (search.Text != "") throw new Exception("Reset search failed");
        var selected = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Organizar"); selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
        if (selected.Content as string != "✓ Organizar") throw new Exception("Category selected cue missing");
        window.ShowHome(); Dispatcher.UIThread.RunJobs();
        var firstCard = window.GetVisualDescendants().OfType<Button>().First(b => AutomationProperties.GetName(b) == "Unir PDF");
        firstCard.Focus(NavigationMethod.Tab); Dispatcher.UIThread.RunJobs();
        if (!firstCard.IsKeyboardFocusWithin) throw new Exception("Tool card cannot receive keyboard focus");
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/keyboard-focus.png", PngBitmapEncoderOptions.Default);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); Dispatcher.UIThread.RunJobs();
        if (!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == "Seleccionar archivos")) throw new Exception("Custom card does not activate from keyboard");
        Console.WriteLine("PASS custom tool card activates with Enter");
        foreach (var tool in ToolCatalog.All) { window.SelectTool(tool); Dispatcher.UIThread.RunJobs(); }
        window.SelectTool(ToolCatalog.All[0]); Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/merge.png", PngBitmapEncoderOptions.Default);
        window.ShowHome(); window.Width = 860; window.Height = 620; Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/home-small.png", PngBitmapEncoderOptions.Default);
        window.Width = 640; window.Height = 540; Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/home-minimum.png", PngBitmapEncoderOptions.Default);
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Redact)); Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/tool-minimum.png", PngBitmapEncoderOptions.Default);
        window.Width = 1220; window.Height = 880;
        var sample = Path.GetFullPath("docs/demo.pdf");
        AppFontResolver.Initialize();
        using (var pdf = new PdfSharp.Pdf.PdfDocument())
        {
            for (var i = 1; i <= 3; i++)
            {
                var page = pdf.AddPage();
                using var graphics = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                graphics.DrawString($"AiOPDF — documento de prueba {i}", new PdfSharp.Drawing.XFont("AiOPDF Sans", 22), PdfSharp.Drawing.XBrushes.DarkSlateGray, 45, 90);
                graphics.DrawString("Procesado en tu equipo, sin subir documentos.", new PdfSharp.Drawing.XFont("AiOPDF Sans", 14), PdfSharp.Drawing.XBrushes.DarkSlateGray, 45, 125);
            }
            pdf.Save(sample);
        }
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Organize));
        var loaded = window.AddPaths([sample]); var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!loaded.IsCompleted && timer.Elapsed < TimeSpan.FromSeconds(20)) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(20); }
        if (!loaded.IsCompleted) throw new TimeoutException("Preview load timed out");
        loaded.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
        if (window.GetVisualDescendants().OfType<Image>().Count() < 3) throw new Exception("Preview images missing");
        using (var frame = window.CaptureRenderedFrame()) frame!.Save("docs/screenshots/organize.png", PngBitmapEncoderOptions.Default);
        StudioNavigationChecks.Run(window, sample);
        ButtonChecks.Run(window, sample);
        ResponsiveChecks.Run(window, sample);
        if (validateFlows)
        {
            var samples = SampleDocuments.Create();
            UiFlowChecks.Run(window, samples[0], samples[1]);
            UiFlowChecks.Run(window, samples[1], samples[0]);
        }
        window.Close(); Console.WriteLine("PASS all tool screens and navigation render; screenshots saved");
    }
}

