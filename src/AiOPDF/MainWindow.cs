using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AiOPDF;

public sealed partial class MainWindow : Window
{
    static readonly IBrush Ink = Brush.Parse("#29243D"), Muted = Brush.Parse("#675F77"), Teal = Brush.Parse("#5A3FC0"), ControlBorder = Brush.Parse("#8D869F");
    readonly ContentControl sideNavigation = new();
    readonly Button homeButton = Secondary("Volver a herramientas");
    readonly Button saveShortcut = Secondary("Guardar resultado");
    string homeCategory = "Todas", homeQuery = "";
    TextBox? homeSearch;
    bool showingHome;
    Action? adaptLayout;
    Action? updateFileState;
    readonly ContentControl body = new();
    readonly ObservableCollection<string> files = new();
    readonly ListBox list = new() { MinHeight = 80, MaxHeight = 180 };
    readonly TextBlock status = Label(""), previewStatus = Label("Selecciona un documento para ver sus páginas.", 13);
    readonly TextBox pages = new() { PlaceholderText = "Todas, o escribe 1,3,5-8" }, password = new() { PasswordChar = '●', PlaceholderText = "Contraseña del PDF" }, newPassword = new() { PasswordChar = '●', PlaceholderText = "Al menos 8 caracteres" }, text = new(), author = new();
    readonly CheckBox clearMetadata = new() { Content = "Limpiar metadatos del documento" };
    readonly TextBox confirmPassword = new() { PasswordChar = '●', PlaceholderText = "Repite la misma contraseña" };
    readonly ComboBox splitMode = new() { ItemsSource = new[] { "Un PDF por página", "Un PDF por rango", "Cada N páginas" }, SelectedIndex = 0 };
    readonly NumericUpDown groupSize = new() { Minimum = 1, Maximum = 100000, Value = 2 };
    readonly TextBlock splitSummary = Label("Añade un PDF para calcular las salidas.", 13);
    readonly ComboBox numberFormat = new() { ItemsSource = new[] { "Número", "Página n", "Página n de N" }, SelectedIndex = 0 }, stampPosition = new() { ItemsSource = new[] { "Arriba · izquierda", "Arriba · centro", "Arriba · derecha", "Centro · izquierda", "Centro · centro", "Centro · derecha", "Abajo · izquierda", "Abajo · centro", "Abajo · derecha" }, SelectedIndex = 7 };
    readonly NumericUpDown opacity = new() { Minimum = 1, Maximum = 100, Value = 25 };
    readonly ComboBox imagePaper = new() { ItemsSource = new[] { "Ajustado a la imagen", "A4", "Letter" }, SelectedIndex = 0 };
    readonly CheckBox landscape = new() { Content = "Papel horizontal" };
    readonly NumericUpDown imageMargin = new() { Minimum = 0, Maximum = 50, Value = 0 };
    readonly ComboBox angle = new() { ItemsSource = new[] { "90°", "180°", "270°" }, SelectedIndex = 0 };
    readonly NumericUpDown dpi = new() { Minimum = 36, Maximum = 300, Value = 144, Increment = 12 }, quality = new() { Minimum = 10, Maximum = 100, Value = 75 }, margin = new() { Minimum = 0, Maximum = 100, Value = 10 }, start = new() { Minimum = 1, Maximum = 100000, Value = 1 };
    readonly NumericUpDown redactX = new() { Minimum = 0, Maximum = 99, Value = 10 }, redactY = new() { Minimum = 0, Maximum = 99, Value = 10 }, redactW = new() { Minimum = 1, Maximum = 100, Value = 30 }, redactH = new() { Minimum = 1, Maximum = 100, Value = 10 };
    readonly StackPanel workspace = new() { Spacing = 20 };
    readonly WrapPanel thumbnails = new() { Orientation = Orientation.Horizontal };
    readonly Button execute = Primary("Guardar resultado"), cancel = Secondary("Cancelar"), reveal = Secondary("Abrir carpeta del resultado");
    readonly ProgressBar progress = new() { IsIndeterminate = true, Height = 4, IsVisible = false };
    readonly List<Bitmap> previewImages = new();
    readonly List<int> pageOrder = new();
    readonly Dictionary<int, CheckBox> previewSelection = new();
    bool updatingPageSelection;
    string? synchronizedPagesText;
    CancellationTokenSource? cancellation, previewCancellation;
    PdfTool tool = ToolCatalog.All[0];
    int previewOffset, previewCount;
    Action<bool>? updatePreviewNavigation;
    string? lastOutput;
    bool adding;
    Control? successArt;
    TextBlock? resultLocation;
    ContentComparisonResult? comparisonResult;
    StackPanel? comparisonView;
    readonly Button exportComparison = Secondary("Guardar informe");
    bool CanRun => files.Count > 0 && (tool.Kind switch { ToolKind.Compare => files.Count == 2, ToolKind.Crop => !useCropRegion || redactW.Value < 100 || redactH.Value < 100, ToolKind.Delete => !string.IsNullOrWhiteSpace(pages.Text), ToolKind.Watermark => !string.IsNullOrWhiteSpace(text.Text), ToolKind.Protect => (newPassword.Text?.Length ?? 0) >= 8 && newPassword.Text == confirmPassword.Text, _ => true });

    public MainWindow()
    {
        Title = "AiOPDF — tus documentos contigo"; Width = 1220; Height = 880; MinWidth = 640; MinHeight = 540;
        FontFamily = new FontFamily("avares://AiOPDF/Assets#Lato");
        SizeChanged += (_, _) => adaptLayout?.Invoke();
        foreach (var type in new[] { typeof(TextBox), typeof(NumericUpDown), typeof(ComboBox) })
        {
            var style = new Style(x => x.OfType(type));
            style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, ControlBorder));
            style.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, 14d));
            Styles.Add(style);
        }
        if (AssetLoader.Exists(new Uri("avares://AiOPDF/Assets/app-icon.ico"))) Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AiOPDF/Assets/app-icon.ico")));
        Background = Brush.Parse("#FCFAF6"); Foreground = Ink; list.ItemsSource = files;
        AutomationProperties.SetName(list, "Archivos seleccionados"); AutomationProperties.SetName(password, "Contraseña del PDF"); AutomationProperties.SetName(status, "Estado del procesamiento");
        pages.TextChanged += (_, _) =>
        {
            if (updatingPageSelection || pages.Text == synchronizedPagesText || tool.Kind != ToolKind.Organize || previewCount == 0) return;
            synchronizedPagesText = null;
            try
            {
                var selected = PdfOperations.ParsePages(pages.Text ?? "", previewCount); pageOrder.Clear(); pageOrder.AddRange(selected);
                updatingPageSelection = true; foreach (var item in previewSelection) item.Value.IsChecked = pageOrder.Contains(item.Key);
            }
            catch (ArgumentException) { }
            finally { updatingPageSelection = false; }
        };
        list.ItemTemplate = new FuncDataTemplate<string>((path, _) => { var label = Label(Path.GetFileName(path ?? "")); label.Margin = new Thickness(8); label.TextWrapping = TextWrapping.NoWrap; label.TextTrimming = TextTrimming.CharacterEllipsis; ToolTip.SetTip(label, path); AutomationProperties.SetName(label, path ?? ""); return label; });
        list.SelectionChanged += async (_, _) => { if (!adding && cancellation == null && list.SelectedItem is string) { previewOffset = 0; await RefreshPreview(); } };
        var shell = new Grid { ColumnDefinitions = new ColumnDefinitions("190,*") };
        var frame = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") }; Grid.SetColumn(frame, 1); shell.Children.Add(frame);
        var rail = new Border { Background = Brush.Parse("#EEE7FF"), Child = new ScrollViewer { Content = sideNavigation }, Padding = new Thickness(18, 26) }; shell.Children.Add(rail);
        SizeChanged += (_, _) => { var compact = Bounds.Width < 1000; shell.ColumnDefinitions = new ColumnDefinitions(compact ? "0,*" : "190,*"); rail.IsVisible = !compact; };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(32, 18) };
        var brand = Row(); brand.VerticalAlignment = VerticalAlignment.Center;
        var appMark = new Image { Source = new Bitmap(AssetLoader.Open(new Uri("avares://AiOPDF/Assets/app-icon-64.png"))), Width = 36, Height = 36 };
        AutomationProperties.SetName(appMark, "Logotipo de AiOPDF"); brand.Children.Add(appMark); brand.Children.Add(Label("AiOPDF", 25, true));
        homeButton.Click += (_, _) => { if (cancellation == null) ShowHome(); }; brand.Children.Add(homeButton); saveShortcut.Click += async (_, _) => await Execute(); brand.Children.Add(saveShortcut); header.Children.Add(brand);
        SizeChanged += (_, _) => { saveShortcut.IsVisible = !showingHome && Bounds.Width < 1150; homeButton.IsVisible = !showingHome && Bounds.Width < 1000; };
        var badge = Panel(Label("✓  Solo en tu equipo", 13, true), 10); badge.Background = Brush.Parse("#FFF0C3"); badge.BorderThickness = new Thickness(0); Grid.SetColumn(badge, 1); header.Children.Add(badge);
        SizeChanged += (_, _) => badge.IsVisible = Bounds.Width >= 860;
        frame.Children.Add(header); Grid.SetRow(body, 1); frame.Children.Add(body);
        var version = (Attribute.GetCustomAttribute(typeof(App).Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute)) as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion.Split('+')[0] ?? typeof(App).Assembly.GetName().Version?.ToString(3) ?? "";
        var bottom = Label($"Hecho para tus documentos. Sin subirlos a ningún sitio.   ·   AiOPDF {version}", 12); bottom.Foreground = Muted; bottom.Margin = new Thickness(32, 12); Grid.SetRow(bottom, 2); frame.Children.Add(bottom); Content = shell;
        KeyDown += (_, e) => { if (cancellation != null) return; if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { ShowHome(); Avalonia.Threading.Dispatcher.UIThread.Post(() => { homeSearch?.Focus(); homeSearch?.SelectAll(); }, Avalonia.Threading.DispatcherPriority.Loaded); e.Handled = true; } else if (e.Key == Key.Escape && !showingHome) { ShowHome(); e.Handled = true; } };
        execute.Click += async (_, _) => await Execute(); cancel.Click += (_, _) => cancellation?.Cancel(); reveal.Click += (_, _) => { if (lastOutput != null) OpenFolder(lastOutput); };
        exportComparison.Click += async (_, _) => { if (comparisonResult == null || cancellation != null) return; var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Guardar informe de comparación", AllowMultiple = false }); var parent = folders.FirstOrDefault()?.TryGetLocalPath(); if (parent != null) await SaveToPath(Path.Combine(parent, $"AiOPDF-Compare-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}")); };
        files.CollectionChanged += (_, _) => InvalidateComparison();
        void RefreshAction() { execute.IsEnabled = cancellation == null && CanRun; saveShortcut.IsEnabled = execute.IsEnabled; }
        pages.TextChanged += (_, _) => RefreshAction(); text.TextChanged += (_, _) => RefreshAction(); newPassword.TextChanged += (_, _) => RefreshAction();
        confirmPassword.TextChanged += (_, _) => RefreshAction();
        pages.TextChanged += (_, _) => UpdateSplitSummary(); splitMode.SelectionChanged += (_, _) => UpdateSplitSummary(); groupSize.ValueChanged += (_, _) => UpdateSplitSummary();
        pages.TextChanged += (_, _) => { if (regionScope != null) { if (string.IsNullOrWhiteSpace(pages.Text)) regionScope.SelectedIndex = 0; else if (regionScope.SelectedIndex != 1 || pages.Text != ((int)(regionPage?.Value ?? 1)).ToString()) regionScope.SelectedIndex = 2; } };
        foreach (var value in new[] { redactX, redactY, redactW, redactH }) value.ValueChanged += (_, _) =>
        {
            if (!updatingRegion)
            {
                updatingRegion = true; redactW.Maximum = 100 - (redactX.Value ?? 0); redactH.Maximum = 100 - (redactY.Value ?? 0); updatingRegion = false;
                regionEditor?.SetSelection(new Rect((double)(redactX.Value ?? 0), (double)(redactY.Value ?? 0), (double)(redactW.Value ?? 1), (double)(redactH.Value ?? 1)));
            }
            RefreshAction();
        };
        clearMetadata.IsCheckedChanged += (_, _) => { if (tool.Kind == ToolKind.Metadata) { text.IsEnabled = clearMetadata.IsChecked != true; author.IsEnabled = clearMetadata.IsChecked != true; } };
        Closing += (_, e) => { if (cancellation != null) { cancellation.Cancel(); e.Cancel = true; status.Text = "Cancelando… espera antes de cerrar."; } else { previewCancellation?.Cancel(); DisposePreviews(); CleanupPrepared(); } };
        ShowHome();
    }

    static TextBlock Label(string value, double size = 14, bool bold = false) => new() { Text = value, FontSize = size, LineHeight = size * (size >= 24 ? 1.2 : 1.5), FontWeight = bold ? FontWeight.Bold : FontWeight.Normal, TextWrapping = TextWrapping.Wrap, Foreground = Ink };
    static Button Primary(string value) => new() { Content = value, Background = Teal, Foreground = Brushes.White, Padding = new Thickness(22, 13), CornerRadius = new CornerRadius(10) };
    static Button Secondary(string value) => new() { Content = value, Padding = new Thickness(14, 10), CornerRadius = new CornerRadius(12), Background = Brushes.White, Foreground = Ink, BorderBrush = Brush.Parse("#C9C1D6"), BorderThickness = new Thickness(1) };
    static StackPanel Row() => new() { Orientation = Orientation.Horizontal, Spacing = 10 };
    static WrapPanel Toolbar(params Control[] controls)
    {
        var panel = new WrapPanel();
        foreach (var control in controls) { control.VerticalAlignment = VerticalAlignment.Center; control.Margin = new Thickness(0, 0, 12, 10); panel.Children.Add(control); }
        return panel;
    }
    static Border Panel(Control child, double padding = 24) => new() { Child = child, Background = Brushes.White, BorderBrush = Brush.Parse("#E2DDEC"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(20), Padding = new Thickness(padding) };
    static Image Artwork(string file, double width) => new() { Source = new Bitmap(AssetLoader.Open(new Uri($"avares://AiOPDF/Assets/{file}"))), Width = width, Stretch = Stretch.UniformToFill };

    public void ShowHome()
    {
        showingHome = true; homeButton.IsVisible = false; saveShortcut.IsVisible = false; RefreshNavigation();
        previewCancellation?.Cancel(); DisposePreviews();
        var root = new StackPanel { Margin = new Thickness(32, 8, 32, 20), Spacing = 20 };
        var intro = new Grid { ColumnDefinitions = new ColumnDefinitions("*,280") };
        var copy = new StackPanel { Spacing = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 4) };
        var headline = Label("Dale otra vuelta\na tus PDF.", 38, true); copy.Children.Add(headline);
        var sub = Label("Tu caja de herramientas para unir, recortar,\nconvertir y poner cada página en su sitio.", 15); sub.Foreground = Muted; copy.Children.Add(sub); intro.Children.Add(copy);
        var art = new DeskArt { Width = 300, Height = 190 }; Grid.SetColumn(art, 1); intro.Children.Add(art);
        var hero = Panel(intro, 22); hero.Background = Brush.Parse("#FFF0C3"); hero.BorderThickness = new Thickness(0); hero.CornerRadius = new CornerRadius(26); root.Children.Add(hero);
        var search = new TextBox { PlaceholderText = "Unir, OCR, contraseña…", Width = 250, Text = homeQuery }; homeSearch = search; AutomationProperties.SetName(search, "Buscar herramientas");
        var title = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") }; title.Children.Add(Label("¿Qué necesitas hacer?", 24, true)); var searchField = new StackPanel { Spacing = 4, Children = { Label("Buscar herramientas", 13, true), search } }; Grid.SetColumn(searchField, 1); title.Children.Add(searchField); root.Children.Add(title);
        var category = homeCategory; var categories = new WrapPanel(); var cards = new WrapPanel(); var categoryButtons = new Dictionary<string, Button>();
        var resultCount = Label("", 13); resultCount.Foreground = Muted;
        void RenderCards()
        {
            cards.Children.Clear();
            foreach (var item in ToolCatalog.All.Where(t => (category == "Todas" || t.Category == category) && ($"{t.Title} {t.Description}".Contains(search.Text ?? "", StringComparison.CurrentCultureIgnoreCase))))
            {
                var stack = new StackPanel { Spacing = 12 };
                var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
                var icon = Panel(new BrandIcon { Kind = item.Kind, Accent = item.Color, Width = 44, Height = 44 }, 10); icon.Background = Brush.Parse(CategoryTint(item.Category)); icon.BorderThickness = new Thickness(0); icon.CornerRadius = new CornerRadius(14); top.Children.Add(icon);
                stack.Children.Add(top);
                stack.Children.Add(Label(item.Title, 18, true)); var description = Label(item.Description, 13); description.Foreground = Muted; stack.Children.Add(description);
                var button = new Button { Content = stack, Width = 262, MinHeight = 186, Margin = new Thickness(0, 0, 14, 14), Padding = new Thickness(18), Background = Brushes.White, BorderBrush = Brush.Parse("#DDD5E8"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(20), HorizontalContentAlignment = HorizontalAlignment.Stretch };
                button.PointerEntered += (_, _) => { button.Background = Brush.Parse(CategoryTint(item.Category)); button.BorderBrush = Brush.Parse(item.Color); };
                button.PointerExited += (_, _) => { button.Background = Brushes.White; button.BorderBrush = Brush.Parse("#E1E5EB"); };
                AutomationProperties.SetName(button, item.Title); button.Click += (_, _) => SelectTool(item); cards.Children.Add(button);
            }
            var count = cards.Children.Count; resultCount.Text = count == 1 ? "1 herramienta disponible" : $"{count} herramientas disponibles";
            foreach (var entry in categoryButtons) { var active = entry.Key == category; entry.Value.Content = active ? $"✓ {entry.Key}" : entry.Key; entry.Value.Background = active ? Brush.Parse("#EAF3EE") : Brushes.White; entry.Value.BorderBrush = active ? Teal : Brush.Parse("#D6E3DA"); AutomationProperties.SetName(entry.Value, active ? $"{entry.Key}, categoría seleccionada" : entry.Key); }
            if (count == 0) { var reset = Secondary("Limpiar búsqueda y filtros"); reset.Click += (_, _) => { category = "Todas"; homeCategory = category; search.Text = ""; RenderCards(); RefreshNavigation(); search.Focus(); }; cards.Children.Add(new StackPanel { Spacing = 12, Children = { Label($"No hay herramientas para «{search.Text}» en {category}.", 16, true), reset } }); }
            adaptLayout?.Invoke();
        }
        foreach (var name in new[] { "Todas" }.Concat(ToolCatalog.All.Select(t => t.Category).Distinct())) { var button = Secondary(name); button.Margin = new Thickness(0, 0, 8, 8); button.Click += (_, _) => { category = name; homeCategory = name; RenderCards(); RefreshNavigation(); }; categoryButtons[name] = button; categories.Children.Add(button); }
        adaptLayout = () => { var available = Math.Max(500, Bounds.Width - (Bounds.Width >= 1000 ? 270 : 80)); var narrow = available < 900; var compact = available < 650; var artWidth = available >= 1400 ? 340 : 240; intro.ColumnDefinitions = new ColumnDefinitions(compact ? "*,0" : $"*,{artWidth}"); headline.FontSize = compact ? 28 : narrow ? 32 : 38; headline.LineHeight = headline.FontSize * 1.15; art.IsVisible = !compact; art.Width = artWidth; art.Height = 170; headline.Text = available >= 1400 ? "Dale otra vuelta a tus PDF." : "Dale otra vuelta\na tus PDF."; categories.IsVisible = Bounds.Width < 1000; var columns = Math.Clamp((int)(available / 280), 2, 7); foreach (var card in cards.Children.OfType<Button>()) card.Width = (available / columns) - 14; title.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "*,Auto"); title.RowDefinitions = new RowDefinitions(compact ? "Auto,12,Auto" : "Auto"); Grid.SetColumn(searchField, compact ? 0 : 1); Grid.SetRow(searchField, compact ? 2 : 0); search.Width = compact ? double.NaN : 250; };
        search.TextChanged += (_, _) => { homeQuery = search.Text ?? ""; RenderCards(); }; root.Children.Add(categories); root.Children.Add(resultCount); root.Children.Add(cards); RenderCards();
        body.Content = new ScrollViewer { Content = root };
    }

    void RefreshNavigation()
    {
        var rail = new StackPanel { Spacing = 8 }; rail.Children.Add(Label("Tu espacio", 22, true)); var caption = Label("Herramientas PDF", 13); caption.Foreground = Muted; caption.Margin = new Thickness(0, 0, 0, 18); rail.Children.Add(caption);
        foreach (var category in new[] { "Todas" }.Concat(ToolCatalog.All.Select(t => t.Category).Distinct()))
        {
            var active = showingHome ? homeCategory == category : tool.Category == category;
            var content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") }; content.Children.Add(Label(active ? "✓ " + category : category, 14, active)); var count = Label((category == "Todas" ? ToolCatalog.All.Length : ToolCatalog.All.Count(t => t.Category == category)).ToString(), 12); Grid.SetColumn(count, 1); content.Children.Add(count);
            var button = Secondary(category); button.Content = content; button.HorizontalContentAlignment = HorizontalAlignment.Stretch; button.Background = active ? Brushes.White : Brushes.Transparent; button.BorderThickness = new Thickness(active ? 1 : 0); button.BorderBrush = Teal; AutomationProperties.SetName(button, $"Abrir categoría {category}" + (active ? ", seleccionada" : ""));
            button.Click += (_, _) => { if (cancellation != null) return; homeCategory = category; homeQuery = ""; ShowHome(); }; rail.Children.Add(button);
        }
        rail.Children.Add(new Border { Height = 24 }); var privacy = Label("Sin cuentas.\nSin enviar archivos.\nTodo aquí.", 13); privacy.Foreground = Muted; rail.Children.Add(privacy); sideNavigation.Content = rail;
    }

    static string CategoryTint(string category) => category switch { "Organizar" => "#E7F5EF", "Convertir" => "#FFF0DC", "Editar" => "#EEE9FC", "Seguridad" or "Revisar" => "#E7F0FF", _ => "#FCE8F0" };

    public void SelectTool(PdfTool selected)
    {
        HidePrepared();
        regionEditor = null; regionPage = null; regionScope = null; regionRequest++; useCropRegion = false;
        var sameTool = selected.Kind == tool.Kind && files.Count > 0;
        var compatibleFiles = selected.Kind != ToolKind.Images && tool.Kind != ToolKind.Images ? files.ToArray() : Array.Empty<string>();
        foreach (var control in new Control[] { workspace, list, pages, password, newPassword, confirmPassword, text, author, clearMetadata, angle, dpi, quality, margin, start, redactX, redactY, redactW, redactH, splitMode, groupSize, splitSummary, numberFormat, stampPosition, opacity, imagePaper, landscape, imageMargin, thumbnails, previewStatus, execute, exportComparison, cancel, progress, status, reveal })
            if (control.Parent is Avalonia.Controls.Panel parent) parent.Children.Remove(control);
        tool = selected; showingHome = false; homeButton.IsVisible = Bounds.Width < 1000; saveShortcut.IsVisible = Bounds.Width < 1150; RefreshNavigation(); previewCancellation?.Cancel(); DisposePreviews();
        text.IsEnabled = author.IsEnabled = tool.Kind != ToolKind.Metadata || clearMetadata.IsChecked != true;
        if (!sameTool) { splitMode.SelectedIndex = 0; groupSize.Value = 2; stampPosition.SelectedIndex = 7; numberFormat.SelectedIndex = 0; opacity.Value = 25; imagePaper.SelectedIndex = 0; imageMargin.Value = 0; landscape.IsChecked = false; redactX.Value = 10; redactY.Value = 10; redactW.Value = 30; redactH.Value = 10; }
        if (!sameTool) { synchronizedPagesText = null; adding = true; files.Clear(); foreach (var path in selected.Multiple ? compatibleFiles : compatibleFiles.Take(1)) files.Add(path); adding = false; pageOrder.Clear(); pages.Text = ""; password.Text = ""; newPassword.Text = ""; confirmPassword.Text = ""; newPassword.PasswordChar = confirmPassword.PasswordChar = '●'; text.Text = ""; author.Text = ""; clearMetadata.IsChecked = false; }
        status.Text = ""; reveal.IsVisible = false; cancel.IsVisible = false; execute.IsEnabled = false; previewCount = 0; previewOffset = 0; previewStatus.Text = "Selecciona un documento para ver sus páginas."; workspace.Children.Clear();
        var root = new StackPanel { Margin = new Thickness(32, 8, 32, 20), Spacing = 20 }; var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,16,*") };
        var toolIcon = Panel(new BrandIcon { Kind = tool.Kind, Accent = tool.Color, Width = 40, Height = 40 }, 12); toolIcon.Background = Brush.Parse(CategoryTint(tool.Category)); toolIcon.BorderThickness = new Thickness(0); heading.Children.Add(toolIcon); var headingText = new StackPanel { Spacing = 5, Children = { Label(tool.Title, 29, true), Label(tool.Description, 15) } }; Grid.SetColumn(headingText, 2); heading.Children.Add(headingText); root.Children.Add(heading);
        comparisonResult = null; comparisonView = null; exportComparison.IsVisible = false;
        var steps = new WrapPanel(); foreach (var value in tool.Kind == ToolKind.Compare ? new[] { "1  Elige dos versiones", "2  Compara el contenido", "3  Guarda el informe si lo necesitas" } : new[] { "1  Añade archivos", "2  Ajusta el resultado", "3  Revisa y guarda una copia" }) { var step = Label(value, 13, true); step.Foreground = Muted; step.Margin = new Thickness(0, 0, 24, 4); steps.Children.Add(step); } root.Children.Add(steps);
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("5*,24,4*") };
        var dropContent = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
        var dropIcon = new BrandIcon { Kind = tool.Kind, Accent = tool.Color, Width = 52, Height = 52, HorizontalAlignment = HorizontalAlignment.Center }; var dropTitle = Label("Arrastra tus archivos aquí", 19, true); dropContent.Children.Add(dropIcon); dropContent.Children.Add(dropTitle);
        var add = Secondary("Seleccionar archivos"); add.HorizontalAlignment = HorizontalAlignment.Center; add.Click += async (_, _) => await AddFiles(); dropContent.Children.Add(add);
        var hint = Label(tool.Kind == ToolKind.Images ? "JPG · PNG · WebP" : tool.Multiple ? "Varios PDFs locales" : "Un PDF local", 12); hint.Foreground = Muted; hint.HorizontalAlignment = HorizontalAlignment.Center; dropContent.Children.Add(hint);
        var drop = Panel(dropContent, 18); drop.Background = Brush.Parse(CategoryTint(tool.Category)); drop.BorderBrush = Brush.Parse(tool.Color); DragDrop.SetAllowDrop(drop, true);
        drop.AddHandler(DragDrop.DragOverEvent, (_, e) => { e.DragEffects = cancellation == null && e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; });
        drop.AddHandler(DragDrop.DropEvent, async (_, e) => { if (cancellation != null) return; await AddPaths((e.DataTransfer.TryGetFiles() ?? []).Select(f => f.TryGetLocalPath()).OfType<string>()); e.Handled = true; });
        workspace.Children.Add(drop); var actions = Row(); var remove = Secondary("Quitar"); remove.Click += async (_, _) => { adding = true; if (list.SelectedIndex >= 0) files.RemoveAt(list.SelectedIndex); adding = false; execute.IsEnabled = CanRun; saveShortcut.IsEnabled = CanRun; updateFileState?.Invoke(); await RefreshPreview(); }; actions.Children.Add(remove);
        if (tool.Kind == ToolKind.Compare) { var swap = Secondary("Intercambiar versiones"); swap.Click += (_, _) => { if (files.Count == 2) files.Move(0, 1); }; actions.Children.Add(swap); }
        else if (tool.Multiple) { var up = Secondary("Subir"); up.Click += (_, _) => MoveFile(-1); var down = Secondary("Bajar"); down.Click += (_, _) => MoveFile(1); actions.Children.Add(up); actions.Children.Add(down); }
        var fileSection = new StackPanel { Spacing = 8, Children = { Label("Archivos", 16, true), list, actions } }; workspace.Children.Add(fileSection);
        updateFileState = () => { var loaded = files.Count > 0; dropIcon.IsVisible = !loaded; dropTitle.IsVisible = !loaded; hint.IsVisible = !loaded; fileSection.IsVisible = loaded; add.Content = loaded ? tool.Multiple ? "Añadir archivos" : "Cambiar archivo" : "Seleccionar archivos"; drop.Padding = new Thickness(loaded ? 12 : 18); }; updateFileState();
        if (tool.Kind != ToolKind.Images) { var reload = Secondary("Actualizar vista previa"); reload.Click += async (_, _) => { previewOffset = 0; await RefreshPreview(resetOrder: previewCount == 0); }; workspace.Children.Add(new Expander { Header = "¿Tu PDF tiene contraseña?", Content = new StackPanel { Spacing = 10, Children = { Label("Contraseña del PDF", 13, true), password, reload } }, HorizontalAlignment = HorizontalAlignment.Stretch }); }
        if (tool.Kind is ToolKind.Crop or ToolKind.Redact) workspace.Children.Add(Panel(BuildRegionWorkspace(), 18));
        var preview = new StackPanel { Spacing = 12, Children = { Label("Vista previa", 18, true), previewStatus, thumbnails } }; var navigation = Row(); var prev = Secondary("Anterior"); var next = Secondary("Siguiente");
        prev.Click += async (_, _) => { if (cancellation == null) { previewOffset = Math.Max(0, previewOffset - 6); await RefreshPreview(false); } }; next.Click += async (_, _) => { if (cancellation == null && previewOffset + 6 < previewCount) { previewOffset += 6; await RefreshPreview(false); } }; navigation.Children.Add(prev); navigation.Children.Add(next); preview.Children.Add(navigation); workspace.Children.Add(Panel(preview, 18));
        updatePreviewNavigation = ready => { navigation.IsVisible = ready && previewCount > 6; prev.IsEnabled = ready && previewOffset > 0; next.IsEnabled = ready && previewOffset + 6 < previewCount; }; updatePreviewNavigation(false);
        if (tool.Kind == ToolKind.Compare) { workspace.Children.RemoveAt(workspace.Children.Count - 1); comparisonView = new StackPanel { Spacing = 14 }; AutomationProperties.SetName(comparisonView, "Resultado de comparación"); workspace.Children.Add(Panel(comparisonView, 20)); InvalidateComparison(); }
        columns.Children.Add(workspace); var options = Panel(BuildOptions()); AutomationProperties.SetName(options, "Opciones del resultado"); options.VerticalAlignment = VerticalAlignment.Top; Grid.SetColumn(options, 2); columns.Children.Add(options); root.Children.Add(columns);
        adaptLayout = () => { var available = Bounds.Width - (Bounds.Width >= 1000 ? 190 : 0) - 64; var narrow = available < 900; columns.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "*,24,420"); columns.RowDefinitions = new RowDefinitions(narrow ? "Auto,24,Auto" : "Auto"); Grid.SetColumn(options, narrow ? 0 : 2); Grid.SetRow(options, narrow ? 2 : 0); var previewWidth = narrow ? available : available - 444; var pageColumns = Math.Min(3, Math.Max(1, thumbnails.Children.Count)); var maxPreviewWidth = Math.Clamp((Bounds.Height - 480) / 1.3, 220, 520); var imageWidth = Math.Clamp((previewWidth - 40) / pageColumns - 34, 126, maxPreviewWidth); foreach (var tile in thumbnails.Children.OfType<Border>()) if (tile.Child is StackPanel page && page.Children.FirstOrDefault() is Image image) { image.Width = imageWidth; image.Height = imageWidth * 1.3; } }; adaptLayout(); body.Content = new ScrollViewer { Content = root }; add.Focus();
        execute.IsEnabled = CanRun;
        saveShortcut.IsEnabled = CanRun;
        if (files.Count > 0) _ = RefreshPreview(resetOrder: !sameTool);
    }

    StackPanel BuildOptions()
    {
        var options = new StackPanel { Spacing = 16 }; var advanced = new StackPanel { Spacing = 15 };
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("48,12,*") };
        heading.Children.Add(new BrandIcon { Kind = tool.Kind, Accent = "#5A3FC0", Width = 42, Height = 42, VerticalAlignment = VerticalAlignment.Center });
        var headingCopy = new StackPanel { Spacing = 4, Children = { Label(tool.Kind == ToolKind.Compare ? "Compara el contenido" : "Prepara tu resultado", 21, true), Label(tool.Kind == ToolKind.Compare ? "Primero revisa. Después exporta." : "Una copia, a tu manera.", 13) } }; Grid.SetColumn(headingCopy, 2); heading.Children.Add(headingCopy);
        options.Children.Add(new Border { Background = Brush.Parse("#EEE7FF"), CornerRadius = new CornerRadius(16), Padding = new Thickness(16), Child = heading });
        void Field(string title, Control value) { AutomationProperties.SetName(value, title); var field = new StackPanel { Spacing = 6, Children = { Label(title, 13, true), value } }; (value == dpi || value == quality ? advanced : options).Children.Add(field); }
        pages.PlaceholderText = tool.Kind == ToolKind.Delete ? "Ejemplo: 2,5-8" : "Todas, o escribe 1,3,5-8";
        if (tool.Kind is not (ToolKind.Merge or ToolKind.Images or ToolKind.Protect or ToolKind.Unlock or ToolKind.Metadata or ToolKind.Compare or ToolKind.Compress)) { Field(tool.Kind == ToolKind.Delete ? "Páginas que quieres eliminar" : "Páginas", pages); options.Children.Add(Label(tool.Kind == ToolKind.Delete ? "Indica las páginas que quieres quitar. Debe quedar al menos una." : tool.Kind == ToolKind.Organize ? "El orden se conserva: 3,1,2. Puedes repetir páginas." : "Vacío incluye todas. Ejemplo: 1,3,5-8.", 12)); }
        if (tool.Kind == ToolKind.Rotate)
        {
            var choices = Row(); var buttons = new List<Button>();
            for (var index = 0; index < 3; index++) { var value = index; var button = Secondary($"{(index + 1) * 90}°"); AutomationProperties.SetName(button, $"Girar {(index + 1) * 90} grados"); button.Click += (_, _) => { angle.SelectedIndex = value; foreach (var entry in buttons) { entry.Background = buttons.IndexOf(entry) == value ? Brush.Parse("#EEE7FF") : Brushes.White; entry.BorderBrush = buttons.IndexOf(entry) == value ? Teal : Brush.Parse("#C9C1D6"); } }; buttons.Add(button); choices.Children.Add(button); }
            buttons[angle.SelectedIndex].Background = Brush.Parse("#EEE7FF"); buttons[angle.SelectedIndex].BorderBrush = Teal; options.Children.Add(new StackPanel { Spacing = 6, Children = { Label("Giro", 13, true), choices } });
        }
        if (tool.Kind == ToolKind.Protect)
        {
            Field("Nueva contraseña", newPassword); Field("Repite la contraseña", confirmPassword);
            var show = new CheckBox { Content = "Mostrar contraseñas" };
            show.IsCheckedChanged += (_, _) => newPassword.PasswordChar = confirmPassword.PasswordChar = show.IsChecked == true ? '\0' : '●';
            options.Children.Add(show); options.Children.Add(Label("Usa al menos 8 caracteres y repite la contraseña para evitar errores al abrir el PDF.", 12));
        }
        if (tool.Kind == ToolKind.Watermark) { text.PlaceholderText = "Ejemplo: CONFIDENCIAL"; Field("Texto de la marca", text); Field("Posición", stampPosition); Field("Opacidad (%)", opacity); }
        if (tool.Kind == ToolKind.Metadata) { text.PlaceholderText = "Título del documento"; Field("Título", text); Field("Autor", author); options.Children.Add(clearMetadata); }
        if (tool.Kind == ToolKind.Number) { Field("Número inicial", start); Field("Formato", numberFormat); Field("Posición", stampPosition); options.Children.Add(Label("La primera página seleccionada recibe este número; las siguientes continúan la secuencia. N indica la cantidad de páginas del documento.", 12)); }
        if (tool.Kind == ToolKind.Images) { Field("Tamaño de papel", imagePaper); options.Children.Add(landscape); Field("Margen del papel (mm)", imageMargin); options.Children.Add(Label("La imagen se centra y conserva su proporción. El margen no se aplica en el tamaño ajustado.", 12)); }
        if (tool.Kind == ToolKind.Crop)
        {
            var uniform = new CheckBox { Content = "Usar margen uniforme", IsChecked = false };
            uniform.IsCheckedChanged += (_, _) => { useCropRegion = uniform.IsChecked != true; margin.IsEnabled = !useCropRegion; if (regionEditor != null) regionEditor.IsEnabled = useCropRegion; execute.IsEnabled = cancellation == null && CanRun; saveShortcut.IsEnabled = execute.IsEnabled; };
            options.Children.Add(uniform); margin.IsEnabled = false; Field("Margen en cada lado (mm)", margin);
            Field("Desde la izquierda (%)", redactX); Field("Desde arriba (%)", redactY); Field("Ancho (%)", redactW); Field("Alto (%)", redactH);
            options.Children.Add(Label("El recorte oculta contenido; no lo elimina. No lo uses para censurar información.", 12));
        }
        if (tool.Kind is ToolKind.Png or ToolKind.Jpeg or ToolKind.Flatten or ToolKind.Grayscale) Field("Resolución (DPI)", dpi);
        if (tool.Kind == ToolKind.Jpeg) Field("Calidad de imagen (10–100)", quality);
        if (tool.Kind is ToolKind.Flatten or ToolKind.Grayscale) options.Children.Add(Panel(Label("Las páginas se convierten en imágenes. Se pierde el texto seleccionable, formularios y enlaces. Las páginas no seleccionadas se conservan.", 13), 14));
        if (tool.Kind == ToolKind.Compress) options.Children.Add(Label("Optimización sin pérdida de texto ni imágenes. Procesa el documento completo. Si no disminuye el tamaño, guarda una copia idéntica y te lo indica.", 13));
        if (tool.Kind == ToolKind.Compress) options.Children.Add(Label("Optimización sin pérdida de texto ni imágenes. Procesa el documento completo. Si no disminuye el tamaño, guarda una copia idéntica y te lo indica.", 13));
        if (tool.Kind == ToolKind.Metadata) options.Children.Add(Label("Limpiar borra propiedades del documento y XMP principal. No elimina adjuntos, anotaciones ni información personal dentro de las páginas.", 13));
        if (tool.Kind == ToolKind.Split) { Field("Cómo dividir", splitMode); Field("Páginas por grupo", groupSize); options.Children.Add(splitSummary); UpdateSplitSummary(); }
        if (tool.Kind == ToolKind.Text) options.Children.Add(Label("Extrae texto existente. Los escaneos sin OCR pueden producir páginas sin texto.", 13));
        if (tool.Kind == ToolKind.Compare) options.Children.Add(Label("El primer PDF es el original; el segundo, la versión actualizada. Compara texto por número de página e ignora espacios y saltos de línea. Páginas movidas o texto trasladado entre páginas aparecerán como cambios. No compara imágenes ni interpreta el significado. Si falta texto legible, aplica OCR antes de comparar.", 13));
        if (tool.Kind is ToolKind.Ocr or ToolKind.OcrText) { Field("Resolución para OCR (DPI)", dpi); options.Children.Add(Label("Modelos de alfabeto latino incluidos, sin descargas durante el uso. Revisa el texto reconocido. OCR a PDF genera páginas rasterizadas con texto invisible para búsqueda.", 13)); }
        if (tool.Kind is ToolKind.Word or ToolKind.Excel or ToolKind.Slides) options.Children.Add(Panel(Label(tool.Kind == ToolKind.Word ? "Exporta texto editable. No conserva columnas, imágenes ni el diseño del PDF." : tool.Kind == ToolKind.Excel ? "Una línea de texto por fila. No detecta tablas ni calcula fórmulas." : "Cada página será una imagen en una diapositiva. No será editable como texto.", 13), 14));
        if (tool.Kind == ToolKind.Redact) { Field("Desde la izquierda (%)", redactX); Field("Desde arriba (%)", redactY); Field("Ancho (%)", redactW); Field("Alto (%)", redactH); Field("Resolución (DPI)", dpi); options.Children.Add(Label("Cubre esta región con negro. Rasteriza las páginas indicadas y conserva las demás: elimina texto, enlaces y formularios. Revisa el resultado antes de compartir.", 13)); }
        options.Children.Add(Label(tool.Kind == ToolKind.Compare ? "Los cambios aparecerán aquí, sin crear archivos. Guardar el informe es opcional." : "Primero procesa y revisa. Después elige dónde guardar una copia.", 13));
        if (advanced.Children.Count > 0) options.Children.Add(new Expander { Header = "Calidad y resolución", Content = advanced, HorizontalAlignment = HorizontalAlignment.Stretch });
        execute.Content = tool.ActionLabel; saveShortcut.Content = execute.Content; options.Children.Add(execute); if (tool.Kind == ToolKind.Compare) options.Children.Add(exportComparison); options.Children.Add(cancel); options.Children.Add(progress); options.Children.Add(status);
        var receiptHeading = new Grid { ColumnDefinitions = new ColumnDefinitions("38,10,*") };
        receiptHeading.Children.Add(new BrandIcon { Width = 36, Height = 36, Accent = "#5A3FC0" });
        var receiptTitle = Label("Tu resultado está listo", 17, true); receiptTitle.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(receiptTitle, 2); receiptHeading.Children.Add(receiptTitle);
        resultLocation = Label("", 12); resultLocation.Foreground = Muted; AutomationProperties.SetName(resultLocation, "Ubicación del resultado");
        reveal.HorizontalAlignment = HorizontalAlignment.Stretch;
        successArt = new Border { Background = Brush.Parse("#FFF2BB"), CornerRadius = new CornerRadius(16), Padding = new Thickness(16), IsVisible = false, Child = new StackPanel { Spacing = 12, Children = { receiptHeading, resultLocation, reveal } } };
        options.Children.Add(successArt);
        var privacy = new Grid { ColumnDefinitions = new ColumnDefinitions("24,8,*") }; privacy.Children.Add(new BrandIcon { Kind = ToolKind.Protect, Accent = "#675F77", Width = 22, Height = 22 });
        var privacyCopy = Label("Tus originales siguen intactos. Todo ocurre en tu equipo.", 12); privacyCopy.Foreground = Muted; Grid.SetColumn(privacyCopy, 2); privacy.Children.Add(privacyCopy); options.Children.Add(privacy); return options;
    }

    async Task AddFiles()
    {
        var images = tool.Kind == ToolKind.Images;
        var selected = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = tool.Multiple, Title = images ? "Seleccionar imágenes" : "Seleccionar PDFs", FileTypeFilter = [new FilePickerFileType(images ? "Imágenes" : "PDF") { Patterns = images ? ["*.jpg", "*.jpeg", "*.png", "*.webp"] : ["*.pdf"] }] });
        await AddPaths(selected.Select(f => f.TryGetLocalPath()).OfType<string>());
    }
    public async Task AddPaths(IEnumerable<string> paths)
    {
        var allowed = tool.Kind == ToolKind.Images ? new[] { ".jpg", ".jpeg", ".png", ".webp" } : [".pdf"];
        var accepted = paths.Where(p => File.Exists(p) && allowed.Contains(Path.GetExtension(p).ToLowerInvariant())).ToArray(); if (accepted.Length == 0) return;
        HidePrepared();
        if (tool.Kind == ToolKind.Compare)
        {
            if (accepted.Length > 2) { status.Text = "Selecciona exactamente dos versiones del PDF."; return; }
            if (accepted.Length == 2) { adding = true; files.Clear(); adding = false; }
            else if (files.Count == 2) { adding = true; files.RemoveAt(list.SelectedIndex >= 0 ? list.SelectedIndex : 1); adding = false; }
        }
        synchronizedPagesText = null; adding = true; if (!tool.Multiple) { files.Clear(); pages.Text = ""; } foreach (var path in tool.Multiple ? accepted : accepted.Take(1)) if (!files.Contains(path)) files.Add(path); if (list.SelectedIndex < 0) list.SelectedIndex = 0; adding = false;
        execute.IsEnabled = CanRun; saveShortcut.IsEnabled = CanRun; updateFileState?.Invoke(); previewOffset = 0; previewCount = 0; await RefreshPreview();
    }
    void MoveFile(int offset) { var current = list.SelectedIndex; var next = current + offset; if (current >= 0 && next >= 0 && next < files.Count) { files.Move(current, next); list.SelectedIndex = next; } }
    void DisposePreviews()
    {
        if (regionEditor != null) regionEditor.Page = null;
        foreach (var image in this.GetVisualDescendants().OfType<Image>()) if (image.Source is Bitmap bitmap && previewImages.Contains(bitmap)) image.Source = null;
        thumbnails.Children.Clear(); previewSelection.Clear(); foreach (var image in previewImages) image.Dispose(); previewImages.Clear();
    }
    void ReleasePreview(Bitmap? image) { if (image != null && previewImages.Remove(image)) image.Dispose(); }
    async Task RefreshPreview(bool resetOrder = true)
    {
        previewCancellation?.Cancel(); var owner = new CancellationTokenSource(); previewCancellation = owner; var token = owner.Token; DisposePreviews(); updatePreviewNavigation?.Invoke(false);
        var file = list.SelectedItem as string ?? files.FirstOrDefault(); if (file == null) { previewStatus.Text = "Selecciona un documento para ver sus páginas."; if (ReferenceEquals(previewCancellation, owner)) previewCancellation = null; owner.Dispose(); return; }
        var secret = password.Text ?? ""; previewStatus.Text = "Preparando vista previa…";
        try
        {
            if (tool.Kind == ToolKind.Compare) return;
            if (tool.Kind == ToolKind.Images) { previewStatus.Text = $"{files.Count} imágenes seleccionadas"; return; }
            var count = await Task.Run(() => PdfEngine.PageCount(file, secret), token); if (token.IsCancellationRequested) return; previewCount = count; UpdateSplitSummary(); if (regionPage != null) { regionPage.Maximum = count; await RefreshRegion(); }
            if (resetOrder) { var selection = PdfOperations.ParsePages(tool.Kind == ToolKind.Organize ? pages.Text ?? "" : "", count); pageOrder.Clear(); pageOrder.AddRange(selection); }
            previewStatus.Text = $"{Path.GetFileName(file)} · {count} páginas\nVista {previewOffset + 1}–{Math.Min(count, previewOffset + 6)}";
            for (var i = previewOffset; i < Math.Min(count, previewOffset + 6); i++)
            {
                var number = i + 1; var bytes = await Task.Run(() => PdfEngine.Thumbnail(file, number - 1, secret, 720), token); if (token.IsCancellationRequested) return;
                var bitmap = new Bitmap(new MemoryStream(bytes)); previewImages.Add(bitmap);
                var page = new StackPanel { Spacing = 6, Children = { new Image { Source = bitmap, Width = 126, Height = 156, Stretch = Stretch.Uniform }, Label($"Página {number}", 12) } };
                if (tool.Kind == ToolKind.Organize)
                {
                    var choose = new CheckBox { Content = "Incluir", IsChecked = pageOrder.Contains(number) }; AutomationProperties.SetName(choose, $"Incluir página {number}"); previewSelection[number] = choose;
                    choose.IsCheckedChanged += (_, _) => { if (updatingPageSelection) return; if (choose.IsChecked == true && !pageOrder.Contains(number)) pageOrder.Add(number); else if (choose.IsChecked != true) pageOrder.RemoveAll(p => p == number); updatingPageSelection = true; pages.Text = synchronizedPagesText = string.Join(",", pageOrder); updatingPageSelection = false; }; page.Children.Add(choose);
                    var moves = Row(); foreach (var direction in new[] { -1, 1 }) { var move = Secondary(direction < 0 ? "←" : "→"); move.Padding = new Thickness(10, 4); AutomationProperties.SetName(move, $"Mover página {number} " + (direction < 0 ? "antes" : "después")); move.Click += (_, _) => { var pos = pageOrder.IndexOf(number); var next = pos + direction; if (pos >= 0 && next >= 0 && next < pageOrder.Count) { (pageOrder[pos], pageOrder[next]) = (pageOrder[next], pageOrder[pos]); updatingPageSelection = true; pages.Text = synchronizedPagesText = string.Join(",", pageOrder); updatingPageSelection = false; } }; moves.Children.Add(move); } page.Children.Add(moves);
                }
                var tile = Panel(page, 10); tile.Margin = new Thickness(0, 0, 10, 10); thumbnails.Children.Add(tile);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!token.IsCancellationRequested) previewStatus.Text = "No se pudo mostrar la vista previa. Comprueba el archivo y su contraseña."; }
        finally { if (ReferenceEquals(previewCancellation, owner)) { previewCancellation = null; adaptLayout?.Invoke(); updatePreviewNavigation?.Invoke(!token.IsCancellationRequested && previewImages.Count > 0); } owner.Dispose(); }
    }

    void InvalidateComparison()
    {
        if (comparisonResult != null) status.Text = "Las versiones cambiaron. Vuelve a comparar el contenido.";
        comparisonResult = null; exportComparison.IsVisible = false;
        if (tool.Kind != ToolKind.Compare || comparisonView == null) return;
        DisposePreviews();
        comparisonView.Children.Clear(); comparisonView.Children.Add(Label("Dos versiones, un vistazo a los cambios", 20, true));
        comparisonView.Children.Add(Label(files.Count > 0 ? $"Original: {Path.GetFileName(files[0])}" : "Añade el PDF original.", 14));
        comparisonView.Children.Add(Label(files.Count > 1 ? $"Versión actualizada: {Path.GetFileName(files[1])}" : "Añade la versión actualizada.", 14));
        comparisonView.Children.Add(Label("Pulsa Comparar contenido para ver qué texto se añadió o eliminó. Los documentos permanecen en tu equipo.", 13));
        execute.IsEnabled = cancellation == null && CanRun; saveShortcut.IsEnabled = execute.IsEnabled;
        reveal.IsVisible = false; if (successArt != null) successArt.IsVisible = false;
    }

    public async Task<bool> CompareDocuments()
    {
        if (tool.Kind != ToolKind.Compare || files.Count != 2 || cancellation != null) return false;
        InvalidateComparison(); cancellation = new CancellationTokenSource(); var owner = cancellation; var token = owner.Token;
        workspace.IsEnabled = false; sideNavigation.IsEnabled = false; homeButton.IsEnabled = false; execute.IsEnabled = false; saveShortcut.IsEnabled = false; cancel.IsVisible = true; progress.IsVisible = true;
        status.Text = "Comparando el texto en tu equipo…"; var inputs = files.ToArray(); var secret = password.Text ?? "";
        try
        {
            var notifications = new Progress<string>(value => { if (ReferenceEquals(cancellation, owner)) status.Text = value; });
            comparisonResult = await Task.Run(() => ContentComparison.Run(inputs[0], inputs[1], secret, token, notifications));
            comparisonView!.Children.Clear(); var summary = Label(comparisonResult.Summary, 18, true); AutomationProperties.SetName(summary, "Resumen de comparación"); comparisonView.Children.Add(summary);
            comparisonView.Children.Add(Label($"Original: {comparisonResult.Original}\nVersión actualizada: {comparisonResult.Updated}", 13));
            foreach (var page in comparisonResult.Pages)
            {
                var state = page.State == "equal" ? "Sin cambios en el texto" : page.State == "unreadable" ? "Pendiente de OCR" : "Texto modificado";
                StackPanel BuildDetails()
                {
                var details = new StackPanel { Spacing = 10 };
                if (page.State == "unreadable") details.Children.Add(Label("No se pudo comparar el contenido de esta página. Aplica OCR a las dos versiones y vuelve a compararlas.", 13));
                foreach (var change in ContentComparison.Hunks(page.Changes))
                {
                    var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
                    foreach (var removed in new[] { true, false })
                    {
                        var value = removed ? change.Removed : change.Added;
                        var excerpt = value.Length > 5000 ? value[..5000] + "… (el informe contiene el fragmento completo)" : value;
                        var copy = new StackPanel { Spacing = 6, Children = { Label(removed ? "Original · eliminado" : "Actualizado · añadido", 13, true) } };
                        if (change.Before.Length > 0) { var context = Label("… " + change.Before, 12); context.Foreground = Muted; copy.Children.Add(context); }
                        copy.Children.Add(Label(value.Length == 0 ? "(sin este fragmento)" : excerpt, 15, true));
                        if (change.After.Length > 0) { var context = Label(change.After + " …", 12); context.Foreground = Muted; copy.Children.Add(context); }
                        var block = new Border { Padding = new Thickness(14), CornerRadius = new CornerRadius(12), Background = Brush.Parse(removed ? "#FCE8F0" : "#E7F5EF"), Child = copy }; Grid.SetColumn(block, removed ? 0 : 2); pair.Children.Add(block);
                    }
                    details.Children.Add(pair);
                }
                if (details.Children.Count == 0) details.Children.Add(Label("El texto coincide. Las imágenes y el diseño no se comparan.", 13));
                    return details;
                }
                var disclosure = new Expander { Header = $"Página {page.Page} · {state}", IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch };
                disclosure.PropertyChanged += (_, e) => { if (e.Property == Expander.IsExpandedProperty && disclosure.IsExpanded && disclosure.Content == null) disclosure.Content = BuildDetails(); };
                comparisonView.Children.Add(disclosure);
            }
            AddComparisonReader(inputs, secret);
            exportComparison.IsVisible = true; exportComparison.IsEnabled = true; status.Text = comparisonResult.Pages.Any(p => p.State == "unreadable") ? "Comparación parcial: hay páginas que requieren OCR." : "Comparación lista. Revisa los cambios; puedes guardar el informe.";
            return true;
        }
        catch (OperationCanceledException) { status.Text = "Comparación cancelada. No se crearon archivos."; return false; }
        catch (Exception error) { comparisonResult = null; status.Text = "No se pudo comparar. " + error.Message + " Comprueba los documentos y su contraseña."; return false; }
        finally { owner.Dispose(); cancellation = null; workspace.IsEnabled = true; sideNavigation.IsEnabled = true; homeButton.IsEnabled = true; execute.IsEnabled = CanRun; saveShortcut.IsEnabled = CanRun; cancel.IsVisible = false; progress.IsVisible = false; password.Text = ""; }
    }

    async Task Execute()
    {
        if (tool.Kind == ToolKind.Compare) { await CompareDocuments(); return; }
        if (files.Count == 0 || cancellation != null) return;
        await PrepareResult();
    }

    // Shared by the save dialog and integration checks; no alternate processing path.
    public async Task<bool> SaveToPath(string output)
    {
        if (files.Count == 0 || cancellation != null) return false;
        if (tool.Kind == ToolKind.Organize && previewCount > 0 && pageOrder.Count == 0 && string.IsNullOrWhiteSpace(pages.Text)) { status.Text = "Incluye al menos una página."; return false; }
        var job = new PdfJob(tool, files.ToArray(), output, pages.Text ?? "", (angle.SelectedIndex + 1) * 90, password.Text ?? "", newPassword.Text ?? "", text.Text ?? "", author.Text ?? "", clearMetadata.IsChecked == true, (int)(dpi.Value ?? 144), (int)(quality.Value ?? 75), (double)(margin.Value ?? 10), (int)(start.Value ?? 1), (double)(redactX.Value ?? 10), (double)(redactY.Value ?? 10), (double)(redactW.Value ?? 30), (double)(redactH.Value ?? 10), splitMode.SelectedIndex, (int)(groupSize.Value ?? 2), numberFormat.SelectedItem as string ?? "Número", stampPosition.SelectedItem as string ?? "Abajo · centro", (int)(opacity.Value ?? 25), imagePaper.SelectedItem as string ?? "Ajustado a la imagen", landscape.IsChecked == true, (double)(imageMargin.Value ?? 0), useCropRegion);
        cancellation = new CancellationTokenSource(); previewCancellation?.Cancel(); workspace.IsEnabled = false; sideNavigation.IsEnabled = false; homeButton.IsEnabled = false; saveShortcut.IsEnabled = false; execute.IsEnabled = false; cancel.IsVisible = true; progress.IsVisible = true; reveal.IsVisible = false; if (successArt != null) successArt.IsVisible = false; status.Text = "Procesando en tu equipo…";
        try { var token = cancellation.Token; var owner = cancellation; var notifications = new Progress<string>(value => { if (ReferenceEquals(cancellation, owner)) status.Text = value; }); var result = await Task.Run(() => PdfEngine.Run(job, token, notifications, tool.Kind == ToolKind.Compare ? comparisonResult : null)); lastOutput = result.Output; status.Text = result.Message; if (resultLocation != null) { resultLocation.Text = result.Output; ToolTip.SetTip(resultLocation, result.Output); } reveal.IsVisible = true; if (successArt != null) successArt.IsVisible = true; return true; }
        catch (OperationCanceledException) { status.Text = "Cancelado. Los originales se conservaron."; return false; }
        catch (Exception error) { status.Text = "No se pudo completar. " + error.Message + " Revisa los archivos y las opciones, elige un nombre nuevo y vuelve a guardar."; return false; }
        finally { cancellation.Dispose(); cancellation = null; workspace.IsEnabled = true; sideNavigation.IsEnabled = true; homeButton.IsEnabled = true; saveShortcut.IsEnabled = CanRun; execute.IsEnabled = CanRun; cancel.IsVisible = false; progress.IsVisible = false; password.Text = ""; newPassword.Text = ""; confirmPassword.Text = ""; newPassword.PasswordChar = confirmPassword.PasswordChar = '●'; }
    }
    void OpenFolder(string output)
    {
        try { var folder = Directory.Exists(output) ? output : Path.GetDirectoryName(output)!; var info = new ProcessStartInfo { UseShellExecute = true, FileName = folder }; if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) { info = new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open") { UseShellExecute = false }; info.ArgumentList.Add(folder); } Process.Start(info); }
        catch { status.Text = $"El resultado está guardado en {output}"; }
    }
}











