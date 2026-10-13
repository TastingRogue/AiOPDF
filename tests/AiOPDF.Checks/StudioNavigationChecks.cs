using AiOPDF;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

static class StudioNavigationChecks
{
    static void Await(Task task)
    {
        var time = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted && time.Elapsed.TotalSeconds < 25) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
        if (!task.IsCompleted) throw new TimeoutException("Navigation preview timeout"); task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
    }
    public static void Run(MainWindow window, string sample)
    {
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
        T Field<T>(string name) where T : Control => window.GetLogicalDescendants().OfType<T>().Single(c => AutomationProperties.GetName(c) == name);
        var include = Field<CheckBox>("Incluir página 2"); include.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null); window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null); Dispatcher.UIThread.RunJobs();
        Check(include.IsChecked == false, "custom checkbox toggles with Space");
        var expander = window.GetLogicalDescendants().OfType<Expander>().Single();
        var disclosure = expander.GetVisualDescendants().OfType<ToggleButton>().Single(); disclosure.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null); window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null); Dispatcher.UIThread.RunJobs();
        Check(expander.IsExpanded && Field<TextBox>("Contraseña del PDF").IsEffectivelyVisible, "custom disclosure reveals password field");
        window.ShowHome(); window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Organize)); Dispatcher.UIThread.RunJobs();
        Check(window.GetLogicalDescendants().OfType<ListBox>().Single().ItemCount == 1, "back to same tool retains document");
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Png)); Dispatcher.UIThread.RunJobs();
        Check(window.GetLogicalDescendants().OfType<ListBox>().Single().ItemCount == 1, "switching PDF tool retains compatible input");
        Await(window.AddPaths([sample]));
        window.GetLogicalDescendants().OfType<Expander>().Single(e => e.Header as string == "Calidad y resolución").IsExpanded = true; Dispatcher.UIThread.RunJobs();
        var dpi = Field<NumericUpDown>("Resolución (DPI)"); dpi.Value = 96; Dispatcher.UIThread.RunJobs();
        var plus = dpi.GetVisualDescendants().OfType<RepeatButton>().Single(b => AutomationProperties.GetName(b) == "Aumentar valor"); plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
        Check(dpi.Value == 108, "custom number stepper increments value");
        var entry = dpi.GetVisualDescendants().OfType<TextBox>().Single(); entry.Focus(NavigationMethod.Tab); entry.SelectAll(); window.KeyTextInput("120"); window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); Dispatcher.UIThread.RunJobs();
        Check(dpi.Value == 120, "custom number field accepts typed value");
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Rotate)); Dispatcher.UIThread.RunJobs();
        Field<Button>("Girar 180 grados").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var output = Path.Combine(Path.GetFullPath("artifacts"), "rotation-navigation-" + Guid.NewGuid().ToString("N") + ".pdf");
        try { var save = window.SaveToPath(output); Await(save); using var pdf = PdfEngine.Open(output); Check(save.Result && pdf.Pages[0].Rotate == 180, "custom rotation choices drive actual output"); }
        finally { if (File.Exists(output)) File.Delete(output); }
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<TextBox>().Any(c => AutomationProperties.GetName(c) == "Buscar herramientas"), "Escape returns to tools");
        var category = window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Abrir categoría Convertir");
        var point = category.TranslatePoint(new Point(category.Bounds.Width / 2, category.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None); window.MouseUp(point, MouseButton.Left, RawInputModifiers.None); Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<Button>().Any(b => AutomationProperties.GetName(b) == "Abrir categoría Convertir, seleccionada"), "sidebar pointer navigation selects category");
        window.KeyPress(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f"); window.KeyRelease(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f"); Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<TextBox>().Single().IsKeyboardFocusWithin, "Ctrl F focuses tool search");
    }
}

