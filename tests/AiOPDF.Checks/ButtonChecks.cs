using Avalonia.VisualTree;
using AiOPDF;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PdfSharp.Pdf;

static class ButtonChecks
{
    static void Pump(Func<bool> ready)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!ready() && clock.Elapsed.TotalSeconds < 25) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
        if (!ready()) throw new Exception("Button operation did not finish"); Dispatcher.UIThread.RunJobs();
    }
    public static void Run(MainWindow window, string sample)
    {
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS button " + name); }
        Button Button(string name) => window.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == name && b.IsEffectivelyVisible);
        void Click(string name) { Button(name).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
        ListBox Files() => window.GetLogicalDescendants().OfType<ListBox>().Single();
        void Load(params string[] paths) { var task = window.AddPaths(paths); Pump(() => task.IsCompleted); task.GetAwaiter().GetResult(); }
        bool Preview(string value) => window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains(value) == true);
        foreach (var category in new[] { "Todas" }.Concat(ToolCatalog.All.Select(t => t.Category).Distinct()))
        {
            window.ShowHome();
            window.GetLogicalDescendants().OfType<Button>().Single(b => (AutomationProperties.GetName(b) ?? "").StartsWith("Abrir categoría " + category)).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            var expected = category == "Todas" ? 25 : ToolCatalog.All.Count(t => t.Category == category);
            Check(Preview(expected == 1 ? "1 herramienta disponible" : $"{expected} herramientas disponibles"), "sidebar category " + category);
        }
        foreach (var tool in ToolCatalog.All)
        {
            window.ShowHome();
            window.GetLogicalDescendants().OfType<Button>().Single(b => (AutomationProperties.GetName(b) ?? "").StartsWith("Abrir categoría Todas")).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            // Activate every real tool card, rather than calling SelectTool directly.
            window.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == tool.Title).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            Check(Preview(tool.Title), "tool card " + tool.Kind);
            foreach (var disclosure in window.GetLogicalDescendants().OfType<Expander>()) disclosure.IsExpanded = true; Dispatcher.UIThread.RunJobs();
            foreach (var number in window.GetLogicalDescendants().OfType<NumericUpDown>())
            {
                number.ApplyTemplate(); Dispatcher.UIThread.RunJobs();
                var before = number.Value;
                var stepper = number.GetVisualDescendants().OfType<Avalonia.Controls.RepeatButton>().ToArray();
                stepper.Single(b => AutomationProperties.GetName(b) == "Aumentar valor").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                Check(number.Value == Math.Min(number.Maximum, before!.Value + number.Increment), "number increase " + AutomationProperties.GetName(number));
                stepper.Single(b => AutomationProperties.GetName(b) == "Disminuir valor").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                Check(number.Value == Math.Max(number.Minimum, Math.Min(number.Maximum, before!.Value + number.Increment) - number.Increment), "number decrease with bounds " + AutomationProperties.GetName(number));
                number.Value = before;
            }
            Check(!window.GetLogicalDescendants().OfType<Button>().Any(b => b.Content as string == "Volver a herramientas" && b.IsEffectivelyVisible), "desktop hides redundant back " + tool.Kind);
            window.Width = 860; Dispatcher.UIThread.RunJobs(); Click("Volver a herramientas"); Check(window.GetLogicalDescendants().OfType<TextBox>().Any(t => AutomationProperties.GetName(t) == "Buscar herramientas"), "compact back " + tool.Kind); window.Width = 1220; Dispatcher.UIThread.RunJobs();
        }
        var temp = Path.Combine(Path.GetFullPath("artifacts"), "button-checks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        try
        {
            var other = Path.Combine(temp, "second.pdf"); File.Copy(sample, other);
            var longPdf = Path.Combine(temp, "eight-pages.pdf"); using (var pdf = new PdfDocument()) { for (var i = 0; i < 8; i++) pdf.AddPage(); pdf.Save(longPdf); }
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Merge));
            while (Files().ItemCount > 0) { Files().SelectedIndex = 0; Click("Quitar"); }
            Load(sample, other); Files().SelectedIndex = 1; Click("Subir"); Check((string)Files().Items[0]! == other, "move file up");
            Click("Bajar"); Check((string)Files().Items[1]! == other, "move file down");
            Click("Quitar"); Check(Files().ItemCount == 1, "remove selected file"); Files().SelectedIndex = 0; Click("Quitar");
            Check(Files().ItemCount == 0 && !Button(ToolCatalog.All.Single(t => t.Kind == ToolKind.Merge).ActionLabel).IsEnabled, "last removal disables save");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Compare)); Load(sample);
            Check(!Button("Comparar contenido").IsEnabled, "compare requires two versions"); Load(other);
            Click("Comparar contenido"); Pump(() => window.GetLogicalDescendants().OfType<Button>().Any(b => b.Content as string == "Guardar informe" && b.IsEffectivelyVisible) && Button("Comparar contenido").IsEnabled);
            Check(window.GetLogicalDescendants().OfType<TextBlock>().Single(b => AutomationProperties.GetName(b) == "Resumen de comparación").Text!.Contains("0 páginas con cambios"), "compare button shows result without save dialog");
            Files().SelectedIndex = 1; Dispatcher.UIThread.RunJobs(); Check(Button("Guardar informe").IsEffectivelyVisible, "selecting a version preserves comparison");
            Click("Intercambiar versiones"); Check(!window.GetLogicalDescendants().OfType<Button>().Any(b => b.Content as string == "Guardar informe" && b.IsEffectivelyVisible), "swapping versions invalidates old report");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Organize)); Load(longPdf);
            Check(Preview("Vista 1–6") && !Button("Anterior").IsEnabled && Button("Siguiente").IsEnabled, "first preview group and boundary buttons"); Click("Siguiente"); Pump(() => Preview("Vista 7–8") && window.GetLogicalDescendants().OfType<Button>().Any(b => b.Content as string == "Anterior" && b.IsEffectivelyVisible && b.IsEnabled)); Check(!Button("Siguiente").IsEnabled, "last preview group disables next");
            Click("Anterior"); Pump(() => Preview("Vista 1–6") && window.GetLogicalDescendants().OfType<Button>().Any(b => b.Content as string == "Siguiente" && b.IsEffectivelyVisible && b.IsEnabled)); Check(Preview("Vista 1–6"), "previous preview group");
            var pages = window.GetLogicalDescendants().OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Páginas"); pages.Text = "3,1";
            Click("Actualizar vista previa"); Pump(() => window.GetLogicalDescendants().OfType<CheckBox>().Any(c => AutomationProperties.GetName(c) == "Incluir página 3"));
            Check(window.GetLogicalDescendants().OfType<CheckBox>().Single(c => AutomationProperties.GetName(c) == "Incluir página 2").IsChecked == false, "refresh preserves selected pages");
            window.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Mover página 3 después").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            window.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Mover página 3 antes").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            Check(pages.Text == "3,1", "move page in both directions");
            foreach (var box in window.GetLogicalDescendants().OfType<CheckBox>().Where(c => (AutomationProperties.GetName(c) ?? "").StartsWith("Incluir página"))) box.IsChecked = false;
            Click("Actualizar vista previa"); Pump(() => window.GetLogicalDescendants().OfType<CheckBox>().Any(c => AutomationProperties.GetName(c) == "Incluir página 3"));
            Check(window.GetLogicalDescendants().OfType<CheckBox>().Where(c => (AutomationProperties.GetName(c) ?? "").StartsWith("Incluir página")).All(c => c.IsChecked == false), "refresh preserves empty selection");
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Rotate)); Load(sample);
            Check(!window.GetLogicalDescendants().OfType<Button>().Any(b => (b.Content as string is "Anterior" or "Siguiente") && b.IsEffectivelyVisible), "short PDF hides preview navigation");
            foreach (var angle in new[] { 90, 180, 270 })
            {
                window.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == $"Girar {angle} grados").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                var output = Path.Combine(temp, $"rotate-{angle}.pdf"); var task = window.SaveToPath(output); Pump(() => task.IsCompleted);
                using var pdf = PdfEngine.Open(output); Check(task.Result && pdf.Pages[0].Rotate == angle, "rotation " + angle);
            }
        }
        finally { window.ShowHome(); Directory.Delete(temp, true); }
    }
}




