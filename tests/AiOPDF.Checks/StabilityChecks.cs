using AiOPDF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Automation;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Interactivity;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
static class StabilityChecks
{
    public static void Run()
    {
        AppFontResolver.Initialize(); var root = Path.Combine(Path.GetFullPath("artifacts"), "stability-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var a = Path.Combine(root, "500-pages.pdf"); var b = Path.Combine(root, "500-pages-copy.pdf");
        using (var pdf = new PdfDocument()) { for (var i = 0; i < 500; i++) { var p = pdf.AddPage(); using var g = XGraphics.FromPdfPage(p); g.DrawString("Synthetic page " + i, new XFont("AiOPDF Sans", 14), XBrushes.Black, 40, 80); } pdf.Save(a); }
        File.Copy(a, b);
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow(); window.Show(); window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Compare));
        void Await(Task task) { var timer = System.Diagnostics.Stopwatch.StartNew(); while (!task.IsCompleted && timer.Elapsed.TotalSeconds < 90) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); } task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs(); }
        Await(window.AddPaths([a, b])); Await(window.CompareDocuments());
        var pageImage = window.GetVisualDescendants().OfType<Image>().Single(i => AutomationProperties.GetName(i) == "Página del PDF original");
        var next = window.GetVisualDescendants().OfType<Button>().Single(c => c.Content as string == "Página siguiente");
        var field = typeof(MainWindow).GetField("previewImages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        int maximum = 0;
        for (var i = 0; i < 40; i++)
        {
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var timer = System.Diagnostics.Stopwatch.StartNew();
            while (pageImage.Source == null && timer.Elapsed.TotalSeconds < 30) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
            if (pageImage.Source == null) throw new Exception("Page render timeout");
            var count = ((System.Collections.ICollection)field.GetValue(window)!).Count; maximum = Math.Max(maximum, count);
            if (count > 2) throw new Exception("Bitmap ownership grows during comparison navigation");
        }
        if (window.GetVisualDescendants().OfType<Expander>().Any(e => e.IsExpanded)) throw new Exception("Comparison details loaded eagerly");
        Console.WriteLine($"PASS stability 500-page comparison, 40 page transitions, at most {maximum} live page bitmaps"); window.Close();
        Console.WriteLine("Evidence: " + root);
    }
}
