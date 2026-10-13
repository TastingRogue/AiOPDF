using AiOPDF;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

static class ResponsiveChecks
{
    public static void Run(MainWindow window, string sample)
    {
        var sizes = new[] { (640, 540), (1280, 800), (1920, 1080), (2560, 1380) };
        foreach (var size in sizes)
        {
            window.Width = size.Item1; window.Height = size.Item2; window.ShowHome(); Dispatcher.UIThread.RunJobs();
            var cards = window.GetVisualDescendants().OfType<Button>().Where(b => ToolCatalog.All.Any(t => t.Title == AutomationProperties.GetName(b))).ToArray();
            if (cards.Any(b => b.Bounds.Width > 400)) throw new Exception("Home cards stretched excessively");
            using (var frame = window.CaptureRenderedFrame()) frame!.Save($"docs/screenshots/responsive-home-{size.Item1}.png", PngBitmapEncoderOptions.Default);
            foreach (var tool in ToolCatalog.All)
            {
                window.SelectTool(tool);
                if (tool.Kind != ToolKind.Images)
                {
                    var load = window.AddPaths([sample]); var timer = System.Diagnostics.Stopwatch.StartNew();
                    while (!load.IsCompleted && timer.Elapsed.TotalSeconds < 30) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
                    load.GetAwaiter().GetResult();
                }
                Dispatcher.UIThread.RunJobs();
                foreach (var spinnerButton in window.GetVisualDescendants().OfType<Avalonia.Controls.RepeatButton>().Where(b => b.IsEffectivelyVisible && b.Content is string text && (text == "+" || text == "−" || text == "-")))
                {
                    var presenter = spinnerButton.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First();
                    if (presenter.HorizontalContentAlignment != Avalonia.Layout.HorizontalAlignment.Center || presenter.VerticalContentAlignment != Avalonia.Layout.VerticalAlignment.Center)
                        throw new Exception($"Uncentered numeric button: {tool.Kind}, {size.Item1}");
                }
                foreach (var toolbar in window.GetVisualDescendants().OfType<WrapPanel>().Where(p => p.Children.Count > 1 && p.Children.All(c => c.Margin.Right == 12)))
                {
                    var children = toolbar.Children.Where(c => c.IsVisible).ToArray();
                    for (var a = 0; a < children.Length; a++) for (var b = a + 1; b < children.Length; b++)
                        if (children[a].Bounds.Intersects(children[b].Bounds)) throw new Exception($"Overlapping toolbar controls: {tool.Kind}, {size.Item1}");
                }
                var options = window.GetVisualDescendants().OfType<Border>().Single(b => AutomationProperties.GetName(b) == "Opciones del resultado");
                if (size.Item1 >= 1280 && Math.Abs(options.Bounds.Width - 420) > 1) throw new Exception("Options width is not bounded");
                foreach (var control in window.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && c is Button or TextBox or NumericUpDown))
                {
                    var origin = control.TranslatePoint(default, window);
                    if (origin.HasValue && (origin.Value.X < -1 || origin.Value.X + control.Bounds.Width > window.Bounds.Width + 1)) throw new Exception($"Horizontal overflow: {tool.Kind}, {size.Item1}, {control}");
                }
                if (size.Item1 == 2560 && tool.Kind != ToolKind.Images && window.GetVisualDescendants().OfType<Image>().Where(i => AutomationProperties.GetName(i) != "Logotipo de AiOPDF").Any(i => i.Bounds.Width < 250)) throw new Exception("Preview did not grow on large screen");
                if (tool.Kind is ToolKind.Delete or ToolKind.Redact or ToolKind.Images)
                { using var frame = window.CaptureRenderedFrame(); frame!.Save($"docs/screenshots/responsive-{tool.Kind}-{size.Item1}.png", PngBitmapEncoderOptions.Default); }
                Console.WriteLine($"PASS responsive {tool.Kind} {size.Item1}x{size.Item2}");
            }
        }
        window.Width = 1220; window.Height = 880;
    }
}
