using AiOPDF;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Reflection;
using System.Text.Json;

static class LocalizationChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS localization " + message); }
    static T Field<T>(MainWindow window, string name) => (T)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
    public static void Run()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "localization-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var moduleDirectory = Path.Combine(AppContext.BaseDirectory, "Localization"); Directory.CreateDirectory(moduleDirectory);
        var module = Path.Combine(moduleDirectory, "test-" + Guid.NewGuid().ToString("N") + ".json");
        var invalidModule = Path.Combine(moduleDirectory, "test-invalid-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(module, JsonSerializer.Serialize(new { code = "fr", name = "Français", culture = "fr", messages = new Dictionary<string,string> { ["common.language"] = "Langue" } }));
        File.WriteAllText(invalidModule, JsonSerializer.Serialize(new { code = "de", name = "Deutsch", culture = "de", messages = new Dictionary<string,string> { ["ui.115"] = "Seite {3}" } }));
        try { L10n.Initialize(Path.Combine(root, "settings.json")); }
        finally { File.Delete(module); File.Delete(invalidModule); }
        Check(L10n.Languages.Any(p => p.Code == "fr") && !L10n.Languages.Any(p => p.Code == "de"), "external module discovered and malformed placeholders rejected");
        L10n.SetLanguage("fr", false); Check(L10n.Text("common.language") == "Langue" && L10n.Text("common.files") == "Archivos", "new module translates and missing messages fall back to Spanish"); L10n.SetLanguage("es", false);
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        Check(L10n.Languages.Select(p => p.Code).Contains("es") && L10n.Languages.Select(p => p.Code).Contains("en"), "both catalogs discovered");
        var spanish = L10n.Languages.Single(p => p.Code == "es"); var english = L10n.Languages.Single(p => p.Code == "en");
        Check(spanish.Messages.Keys.Order().SequenceEqual(english.Messages.Keys.Order()), "English covers every Spanish message");
        Check(spanish.Messages.All(x => L10n.Slots(x.Value) == L10n.Slots(english.Messages[x.Key])), "all template placeholders agree");
        var window = new MainWindow(); window.Show(); Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Tu espacio"), "Spanish default renders");
        var selector = window.GetVisualDescendants().OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Idioma");
        selector.SelectedItem = english; Dispatcher.UIThread.RunJobs();
        Check(L10n.Code == "en" && File.ReadAllText(L10n.PreferencePath).Contains("en"), "selector changes and persists English");
        var keyedLabel = new TextBlock(); LocalizedUI.Bind(keyedLabel, TextBlock.TextProperty, "ui.115", 8);
        Check(keyedLabel.Text == "Page 8", "explicit key binding formats arguments");
        Check(window.Title?.Contains("your documents") == true && window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Your space"), "window and navigation change live");
        var report = new ContentComparisonResult("Original.pdf", "Updated.pdf", [new PageComparison(1, "changed", [new TextChange("removed", "Guardar copia"), new TextChange("added", "Texto modificado")])]).Report();
        Check(report.StartsWith("Text comparison") && report.Contains("Removed: Guardar copia") && report.Contains("Added: Texto modificado"), "report translates instructions without translating document passages");
        Check(L10n.Translate("Abrir categoría Organizar, seleccionada") == "Open category Organize, selected", "selected navigation accessibility name translated");
        Check(L10n.Translate("Página 7 de 12") == "Page 7 of 12", "dynamic page counter translated");
        Check(L10n.Translate("AiOPDF-prueba-A.pdf · 3 páginas\nVista 1–3") == "AiOPDF-prueba-A.pdf · 3 pages\nView 1–3", "preview template preserves filename");
        Check(L10n.Translate("No se pudo guardar la copia. La carpeta de destino no existe.") == "Unable to save the copy. The destination folder does not exist.", "compound error translated");
        var search = window.GetVisualDescendants().OfType<TextBox>().Single(); search.Text = "Merge"; Dispatcher.UIThread.RunJobs();
        Check(window.GetVisualDescendants().OfType<Button>().Any(b => AutomationProperties.GetName(b) == "Merge PDFs"), "search uses displayed English titles");
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Watermark)); Dispatcher.UIThread.RunJobs();
        var userText = Field<TextBox>(window, "text"); userText.Text = "Guardar copia";
        var pages = Field<TextBox>(window, "pages"); pages.Text = "1,3";
        var opacity = Field<NumericUpDown>(window, "opacity"); opacity.Value = 43;
        var position = Field<ComboBox>(window, "stampPosition"); position.SelectedIndex = 2;
        var selectedTool = Field<PdfTool>(window, "tool");
        var region = new TextBlock(); region.SetValue(LocalizedUI.EnabledProperty, false); region.Text = "Guardar copia";
        var status = Field<TextBlock>(window, "status"); status.Text = "Procesando página 2 de 5…";
        Check(status.Text == "Processing page 2 of 5…", "processing status translated");
        Check((string)position.SelectedItem! == "Arriba · derecha", "job option value remains invariant");
        L10n.SetLanguage("es"); Dispatcher.UIThread.RunJobs();
        Check(keyedLabel.Text == "Página 8", "explicit key binding changes language without rebuilding");
        Check(userText.Text == "Guardar copia" && pages.Text == "1,3" && opacity.Value == 43 && position.SelectedIndex == 2 && Field<PdfTool>(window,"tool") == selectedTool, "switch preserves user input and active tool");
        Check(region.Text == "Guardar copia" && status.Text == "Procesando página 2 de 5…", "document content stays untouched and UI returns to Spanish");
        L10n.SetLanguage("en"); Dispatcher.UIThread.RunJobs();
        Check(L10n.Translate("1 página con cambios · 2 sin cambios · 0 pendientes de OCR") == "1 page with changes · 2 unchanged · 0 awaiting OCR", "comparison singular summary translated");
        Check(L10n.Translate("Página 1 · Sin cambios en el texto") == "Page 1 · No text changes", "nested comparison state translated");
        Check(L10n.Translate("Se crearán 2 PDFs · página 1 / 2 páginas (2–3)") == "2 PDFs will be created · page 1 / 2 pages (2–3)", "split summary and ranges translated");
        Check(position.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Top · right"), "option presentation translates without changing values");
        foreach (var pack in new[] { spanish, english })
        {
            L10n.SetLanguage(pack.Code, false);
            foreach (var tool in ToolCatalog.All)
            {
                window.SelectTool(tool); Dispatcher.UIThread.RunJobs();
                Check(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == L10n.Translate(tool.Title)), $"{pack.Code} {tool.Kind} heading renders");
                foreach (var size in new[] { (Width: 640d, Height: 720d), (Width: 1440d, Height: 900d) })
                {
                    window.Width = size.Width; window.Height = size.Height; Dispatcher.UIThread.RunJobs();
                    var languageControl = window.GetVisualDescendants().OfType<ComboBox>().Single(c => c.ItemsSource is IReadOnlyList<LanguagePack>);
                    var location = languageControl.TranslatePoint(new Point(0,0), window)!.Value;
                    Check(location.X >= 0 && location.X + languageControl.Bounds.Width <= window.Bounds.Width && languageControl.Bounds.Width >= 100, $"{pack.Code} {tool.Kind} selector visible at {size.Width}");
                }
            }
        }
        L10n.SetLanguage("en"); L10n.SetLanguage("es", false); L10n.Initialize(L10n.PreferencePath);
        Check(L10n.Code == "en", "saved choice restored on next initialization");
        Check(!L10n.SetLanguage("zz") && L10n.Code == "en", "unsupported language does not change state");
        File.WriteAllText(L10n.PreferencePath, "not-json"); L10n.Initialize(L10n.PreferencePath); Check(L10n.Code == "es", "corrupt preferences safely fall back to Spanish");
        Check(L10n.SetLanguage("en") && File.ReadAllText(L10n.PreferencePath).Contains("en"), "new selection repairs corrupt preferences");
        L10n.SetLanguage("en", false); window.ShowHome(); Dispatcher.UIThread.RunJobs(); window.GetVisualDescendants().OfType<TextBox>().Single().Text = ""; window.Width = 1280; window.Height = 900; Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save(Path.Combine(root, "home-en.png"), PngBitmapEncoderOptions.Default);
        window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Watermark)); window.Width = 640; window.Height = 820; Dispatcher.UIThread.RunJobs();
        using (var frame = window.CaptureRenderedFrame()) frame!.Save(Path.Combine(root, "watermark-en-640.png"), PngBitmapEncoderOptions.Default);
        L10n.SetLanguage("es", false); window.Close(); Console.WriteLine("Evidence: " + root);
    }
}


