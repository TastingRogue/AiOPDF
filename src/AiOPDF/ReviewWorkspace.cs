using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace AiOPDF;

public sealed partial class MainWindow
{
    RegionEditor? regionEditor;
    NumericUpDown? regionPage;
    ComboBox? regionScope;
    bool useCropRegion, updatingRegion;
    int regionRequest;
    StackPanel BuildRegionWorkspace()
    {
        useCropRegion = tool.Kind == ToolKind.Crop;
        if (useCropRegion) { updatingRegion = true; redactX.Value = 0; redactY.Value = 0; redactW.Maximum = redactH.Maximum = 100; redactW.Value = 100; redactH.Value = 100; updatingRegion = false; }
        regionEditor = new RegionEditor { IsRedaction = tool.Kind == ToolKind.Redact };
        AutomationProperties.SetName(regionEditor, tool.Kind == ToolKind.Crop ? "Dibujar área que conservar" : "Dibujar área que censurar");
        regionEditor.SetSelection(tool.Kind == ToolKind.Crop ? new Rect(0, 0, 100, 100) : new Rect((double)(redactX.Value ?? 10), (double)(redactY.Value ?? 10), (double)(redactW.Value ?? 30), (double)(redactH.Value ?? 10)));
        regionPage = new NumericUpDown { Minimum = 1, Maximum = 1, Value = 1, Width = 160 }; AutomationProperties.SetName(regionPage, "Página para dibujar la región");
        var scope = regionScope = new ComboBox { ItemsSource = new[] { "Todas las páginas", "Solo la página mostrada", "Páginas indicadas en ajustes" }, SelectedIndex = 0 };
        AutomationProperties.SetName(scope, "Aplicar región a");
        scope.SelectionChanged += (_, _) => { if (scope.SelectedIndex == 0) pages.Text = ""; if (scope.SelectedIndex == 1) pages.Text = ((int)(regionPage.Value ?? 1)).ToString(); };
        regionPage.ValueChanged += async (_, _) => { if (scope.SelectedIndex == 1) pages.Text = ((int)(regionPage.Value ?? 1)).ToString(); await RefreshRegion(); };
        var undo = Secondary("Deshacer región"); undo.Click += (_, _) => regionEditor?.Undo();
        var reset = Secondary("Restablecer región"); reset.Click += (_, _) => regionEditor?.Reset();
        var measure = Label("", 13);
        regionEditor.SelectionChanged += rect =>
        {
            updatingRegion = true; redactX.Value = (decimal)rect.X; redactY.Value = (decimal)rect.Y; redactW.Maximum = 100 - (decimal)rect.X; redactH.Maximum = 100 - (decimal)rect.Y; redactW.Value = (decimal)rect.Width; redactH.Value = (decimal)rect.Height; updatingRegion = false;
            measure.Text = $"Región: {rect.Width:F1}% × {rect.Height:F1}% de la página";
        };
        return new StackPanel { Spacing = 12, Children = { Label(tool.Kind == ToolKind.Crop ? "Dibuja el área que quieres conservar" : "Dibuja el área que quieres censurar", 20, true), Label("Arrastra sobre la página. También puedes ajustar la región con los campos numéricos.", 13), Toolbar(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { new TextBlock { Text = "Página", VerticalAlignment = VerticalAlignment.Center }, regionPage } }, scope), regionEditor, measure, Toolbar(undo, reset) } };
    }
    async Task RefreshRegion()
    {
        var editor = regionEditor; if (editor == null || regionPage == null || files.Count == 0 || tool.Kind is not (ToolKind.Redact or ToolKind.Crop)) return;
        var owner = ++regionRequest; var file = files[0]; var secret = password.Text ?? ""; var page = (int)(regionPage.Value ?? 1);
        try
        {
            var png = await Task.Run(() => PdfEngine.Thumbnail(file, page - 1, secret, 1000));
            if (owner != regionRequest || !ReferenceEquals(editor, regionEditor)) return;
            var old = editor.Page; editor.Page = null; ReleasePreview(old);
            var bitmap = new Bitmap(new MemoryStream(png)); previewImages.Add(bitmap); editor.Page = bitmap; editor.InvalidateVisual();
        }
        catch (Exception error) { if (owner == regionRequest) status.Text = "No se pudo cargar la página para dibujar. " + error.Message; }
    }
    void UpdateSplitSummary()
    {
        if (tool.Kind != ToolKind.Split) return;
        groupSize.IsEnabled = splitMode.SelectedIndex == 2;
        if (previewCount == 0) { splitSummary.Text = "Añade un PDF para calcular las salidas."; return; }
        try
        {
            var groups = PdfOperations.SplitGroups(pages.Text ?? "", previewCount, splitMode.SelectedIndex, (int)(groupSize.Value ?? 2));
            splitSummary.Text = $"Se crearán {groups.Length} PDFs · " + string.Join(" / ", groups.Take(8).Select(g => g.Length == 1 ? $"página {g[0]}" : $"{g.Length} páginas ({g[0]}–{g[^1]})")) + (groups.Length > 8 ? " / …" : "");
        }
        catch (ArgumentException error) { splitSummary.Text = error.Message; }
    }

    void AddComparisonReader(string[] inputs, string secret)
    {
        var result = comparisonResult!;
        var reader = new StackPanel { Spacing = 12 };
        var count = result.Pages.Length; var current = 1; var request = 0;
        var pageLabel = Label("", 14, true);
        Image before = new Image { Stretch = Stretch.Uniform, Width = 320, VerticalAlignment = VerticalAlignment.Top }, after = new Image { Stretch = Stretch.Uniform, Width = 320, VerticalAlignment = VerticalAlignment.Top };
        AutomationProperties.SetName(before, "Página del PDF original"); AutomationProperties.SetName(after, "Página del PDF actualizado");
        var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
        foreach (var original in new[] { true, false })
        {
            var block = new StackPanel { Spacing = 8, Children = { Label(original ? "Original" : "Actualizado", 13, true), new ScrollViewer { Content = original ? before : after, Height = Math.Clamp(Bounds.Height - 460, 320, 700), HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto } } };
            var surface = Panel(block, 12); Grid.SetColumn(surface, original ? 0 : 2); pair.Children.Add(surface);
        }
        Button previous = Secondary("Página anterior"), next = Secondary("Página siguiente");
        var zoom = new NumericUpDown { Minimum = 50, Maximum = 200, Value = 100, Increment = 25, Width = 160 };
        AutomationProperties.SetName(zoom, "Zoom de comparación (%)");
        var bar = Toolbar(previous, next, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { new TextBlock { Text = "Zoom (%)", VerticalAlignment = VerticalAlignment.Center }, zoom } });
        var excerpt = new StackPanel { Spacing = 8 };
        async Task ShowPage(int number)
        {
            current = number; var owner = ++request; pageLabel.Text = $"Página {current} de {count}";
            previous.IsEnabled = current > 1; next.IsEnabled = current < count;
            var oldBefore = before.Source as Bitmap; var oldAfter = after.Source as Bitmap;
            before.Source = after.Source = null; ReleasePreview(oldBefore); ReleasePreview(oldAfter);
            try
            {
                var bytes = await Task.Run(() => inputs.Select((p, index) => number <= PdfEngine.PageCount(p, secret) ? result.Pages[number - 1].State == "changed" ? ComparisonMarkup.Render(inputs[0], inputs[1], secret, number, index == 0, comparison: result.Pages[number - 1]) : PdfEngine.Thumbnail(p, number - 1, secret, 1200) : null).ToArray());
                if (owner != request || tool.Kind != ToolKind.Compare || !ReferenceEquals(result, comparisonResult)) return;
                foreach (var original in new[] { true, false }) if (bytes[original ? 0 : 1] is byte[] png)
                { var bitmap = new Bitmap(new MemoryStream(png)); previewImages.Add(bitmap); (original ? before : after).Source = bitmap; }
            }
            catch (Exception error) { if (owner == request) pageLabel.Text = "No se pudo mostrar la página: " + error.Message; }
        }
        previous.Click += async (_, _) => await ShowPage(Math.Max(1, current - 1)); next.Click += async (_, _) => await ShowPage(Math.Min(count, current + 1));
        void ResizePages()
        {
            var factor = (double)(zoom.Value ?? 100) / 100;
            var top = pair.TranslatePoint(default, this)?.Y ?? 440;
            var height = Math.Clamp(Bounds.Height - Math.Max(0, top) - 90, 180, 700);
            foreach (var viewer in pair.GetVisualDescendants().OfType<ScrollViewer>()) { viewer.Height = height; if (factor <= 1) viewer.Offset = default; }
            before.Width = after.Width = Math.Max(100, (pair.Bounds.Width - 64) / 2) * factor;
            before.Height = after.Height = (height - 12) * factor;
        }
        zoom.ValueChanged += (_, _) => ResizePages(); pair.SizeChanged += (_, _) => ResizePages();
        SizeChanged += OnReaderResize;
        void OnReaderResize(object? sender, SizeChangedEventArgs e)
        {
            if (!ReferenceEquals(result, comparisonResult)) { SizeChanged -= OnReaderResize; return; }
            ResizePages();
        }
        reader.Children.Add(pageLabel); reader.Children.Add(bar); reader.Children.Add(pair);
        var changes = result.Pages.SelectMany(p => ContentComparison.Hunks(p.Changes).Select(h => (Page: p.Page, Hunk: h))).ToArray();
        var changeIndex = -1; var changeLabel = Label($"{changes.Length} cambios de texto", 14, true);
        Button backChange = Secondary("Cambio anterior"), nextChange = Secondary("Siguiente cambio"); backChange.IsEnabled = false; nextChange.IsEnabled = changes.Length > 0;
        async Task SelectChange(int index)
        {
            changeIndex = index; var change = changes[index];
            changeLabel.Text = $"Cambio {index + 1} de {changes.Length} · página {change.Page}";
            backChange.IsEnabled = index > 0; nextChange.IsEnabled = index + 1 < changes.Length;
            excerpt.Children.Clear();
            var fragments = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") }; excerpt.Children.Add(fragments);
            foreach (var removed in new[] { true, false })
            {
                var content = removed ? change.Hunk.Removed : change.Hunk.Added;
                var shown = content.Length > 1500 ? content[..1500] + "… (fragmento completo en el informe)" : content;
                var card = Panel(new StackPanel { Spacing = 6, Children = { Label(removed ? "Original · eliminado" : "Actualizado · añadido", 13, true), Label(content.Length == 0 ? "Sin este fragmento" : shown, 15, true, localize: content.Length == 0) } }, 14);
                card.Background = Brush.Parse(removed ? "#FCE8F0" : "#E7F5EF"); Grid.SetColumn(card, removed ? 0 : 2); fragments.Children.Add(card);
            }
            await ShowPage(change.Page);
        }
        backChange.Click += async (_, _) => await SelectChange(changeIndex - 1); nextChange.Click += async (_, _) => await SelectChange(changeIndex + 1);
        reader.Children.Add(changeLabel); reader.Children.Add(Toolbar(backChange, nextChange)); reader.Children.Add(excerpt);
        reader.Children.Add(Label("100% ajusta la página completa. Eliminado: subrayado rosa. Añadido: subrayado verde. Aumenta el zoom para leer los detalles. Las imágenes y el diseño no se comparan.", 12));
        comparisonView!.Children.Insert(2, reader);
        if (comparisonView.Parent is Border panel) { workspace.Children.Remove(panel); workspace.Children.Insert(0, panel); }
        _ = ShowPage(1);
    }
}

