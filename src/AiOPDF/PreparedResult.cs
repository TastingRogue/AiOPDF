using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace AiOPDF;

public sealed partial class MainWindow
{
    readonly List<string> temporaryResults = new();
    readonly PrivateReviewSession reviewSession = new();
    string? preparedOutput;
    Border? preparedPanel;
    int resultRequest;

    void HidePrepared()
    {
        resultRequest++; preparedOutput = null;
        if (preparedPanel != null) workspace.Children.Remove(preparedPanel);
        preparedPanel = null;
    }

    void CleanupPrepared()
    {
        HidePrepared(); comparisonResult = null; regionRequest++; regionEditor = null;
        // Only directories created by this window are eligible for cleanup.
        foreach (var directory in temporaryResults)
            try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        reviewSession.Dispose();
    }

    public async Task<bool> PrepareResult()
    {
        if (tool.Kind == ToolKind.Compare) return await CompareDocuments();
        if (!CanRun || cancellation != null) return false;
        HidePrepared();
        var directory = Path.Combine(reviewSession.DirectoryPath, "result-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory); temporaryResults.Add(directory);
        var extension = tool.Kind switch { ToolKind.Text or ToolKind.OcrText => "txt", ToolKind.Word => "docx", ToolKind.Excel => "xlsx", ToolKind.Slides => "pptx", _ => "pdf" };
        var target = Path.Combine(directory, tool.Folder ? "resultados" : $"AiOPDF-{tool.Kind}.{extension}");
        var secret = tool.Kind == ToolKind.Protect ? newPassword.Text ?? "" : tool.Kind == ToolKind.Unlock ? "" : password.Text ?? "";
        if (!await SaveToPath(target)) return false;
        var processingSummary = status.Text?.Replace("Guardado:", "Tamaño:").Replace("Se guardó una copia", "Se preparó una copia");
        preparedOutput = target; lastOutput = null; reveal.IsVisible = false; if (successArt != null) successArt.IsVisible = false;
        status.Text = (tool.Kind == ToolKind.Compress ? processingSummary + "\n" : "") + "Resultado preparado. Revísalo antes de guardar una copia.";
        var outputs = tool.Folder ? Directory.GetFiles(target).Order().ToArray() : [target];
        var review = new StackPanel { Spacing = 14 };
        review.Children.Add(Label("Revisa tu resultado", 22, true));
        review.Children.Add(Label("Esta copia es temporal. Si cambias opciones, vuelve a procesar. Se elimina al cerrar la aplicación normalmente.", 13));
        var choices = new ComboBox { ItemsSource = outputs.Select(p => $"{Path.GetFileName(p)} · {new FileInfo(p).Length:N0} bytes").ToArray(), SelectedIndex = 0, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        AutomationProperties.SetName(choices, "Archivo del resultado"); review.Children.Add(choices);
        var display = new StackPanel { Spacing = 10 }; review.Children.Add(display);
        var save = Primary("Guardar copia"); save.Click += async (_, _) => await SavePreparedDialog();
        var continueWith = Secondary("Usar este PDF en otra herramienta");
        continueWith.Click += async (_, _) =>
        {
            var chosen = outputs[choices.SelectedIndex];
            SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Merge));
            files.Clear(); await AddPaths([chosen]);
            status.Text = "Resultado cargado. Elige la siguiente herramienta en el menú.";
        };
        var actions = Toolbar(save, continueWith); review.Children.Insert(3, actions);
        async Task Display()
        {
            var owner = ++resultRequest; var chosen = outputs[choices.SelectedIndex]; display.Children.Clear();
            var isPdf = Path.GetExtension(chosen).Equals(".pdf", StringComparison.OrdinalIgnoreCase); continueWith.IsVisible = isPdf;
            try
            {
                if (isPdf)
                {
                    var count = await Task.Run(() => PdfEngine.PageCount(chosen, secret));
                    if (owner != resultRequest) return;
                    var image = new Image { Height = 450, Stretch = Stretch.Uniform };
                    var number = 1; var pageLabel = Label($"Página 1 de {count}", 13, true);
                    Button previous = Secondary("Página anterior"), next = Secondary("Página siguiente");
                    async Task LoadPage()
                    {
                        var pageRequest = ++resultRequest;
                        try
                        {
                            var png = await Task.Run(() => PdfEngine.Thumbnail(chosen, number - 1, secret, 900));
                            if (pageRequest != resultRequest) return;
                            var old = image.Source as Bitmap; image.Source = null; ReleasePreview(old);
                            var bitmap = new Bitmap(new MemoryStream(png)); previewImages.Add(bitmap); image.Source = bitmap;
                            pageLabel.Text = $"Página {number} de {count}"; previous.IsEnabled = number > 1; next.IsEnabled = number < count;
                        }
                        catch (Exception error) { if (pageRequest == resultRequest) pageLabel.Text = "No se pudo mostrar esta página. " + error.Message; }
                    }
                    previous.Click += async (_, _) => { number--; await LoadPage(); }; next.Click += async (_, _) => { number++; await LoadPage(); };
                    display.Children.Add(pageLabel); display.Children.Add(image);
                    if (count > 1) display.Children.Add(Toolbar(previous, next));
                    await LoadPage();
                }
                else if (Path.GetExtension(chosen) is ".png" or ".jpg")
                {
                    var bitmap = new Bitmap(chosen); previewImages.Add(bitmap); display.Children.Add(new Image { Source = bitmap, Height = 400, Stretch = Stretch.Uniform });
                }
                else if (Path.GetExtension(chosen) == ".txt") display.Children.Add(Label(File.ReadAllText(chosen) is var value && value.Length > 6000 ? value[..6000] + "…" : value, 14));
                else display.Children.Add(Label("Guarda la copia y ábrela en tu aplicación de Office para revisar el contenido editable.", 13));
            }
            catch (Exception error) { if (owner == resultRequest) display.Children.Add(Label("No se pudo mostrar la vista previa: " + error.Message + " Puedes guardar la copia y revisarla en otra aplicación.", 13)); }
        }
        choices.SelectionChanged += async (_, _) => { if (choices.SelectedIndex >= 0) await Display(); };
        preparedPanel = Panel(review, 20); preparedPanel.Background = Brush.Parse("#F4F9F5"); AutomationProperties.SetName(preparedPanel, "Revisión del resultado"); workspace.Children.Insert(0, preparedPanel);
        await Display(); return true;
    }

    async Task SavePreparedDialog()
    {
        if (preparedOutput == null || cancellation != null) return;
        string? destination;
        if (Directory.Exists(preparedOutput))
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Guardar copia de los resultados", AllowMultiple = false });
            var parent = folders.FirstOrDefault()?.TryGetLocalPath();
            destination = parent == null ? null : Path.Combine(parent, $"AiOPDF-{tool.Kind}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..4]}");
        }
        else
        {
            var extension = Path.GetExtension(preparedOutput).TrimStart('.');
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Guardar copia revisada", SuggestedFileName = Path.GetFileName(preparedOutput), DefaultExtension = extension, FileTypeChoices = [new FilePickerFileType(extension.ToUpperInvariant()) { Patterns = [$"*.{extension}"] }] });
            destination = file?.TryGetLocalPath();
        }
        if (destination != null) await SavePreparedToPathAsync(destination);
    }

    public async Task<bool> SavePreparedToPathAsync(string destination)
    {
        if (preparedOutput == null || cancellation != null) return false;
        var source = preparedOutput; cancellation = new CancellationTokenSource(); var owner = cancellation;
        workspace.IsEnabled = sideNavigation.IsEnabled = homeButton.IsEnabled = execute.IsEnabled = saveShortcut.IsEnabled = false;
        cancel.IsVisible = progress.IsVisible = true;
        try
        {
            var notifications = new Progress<long>(bytes => { if (ReferenceEquals(cancellation, owner)) status.Text = $"Guardando copia… {bytes / 1048576.0:F1} MB"; });
            await Task.Run(() => ReviewedCopy.CopyAsync(source, destination, owner.Token, notifications));
            lastOutput = Path.GetFullPath(destination); status.Text = "Copia guardada. El resultado revisado sigue disponible.";
            if (resultLocation != null) resultLocation.Text = lastOutput;
            reveal.IsVisible = true; if (successArt != null) successArt.IsVisible = true; return true;
        }
        catch (OperationCanceledException) { status.Text = "Guardado cancelado. El resultado revisado sigue disponible."; return false; }
        catch (Exception error) { status.Text = "No se pudo guardar la copia. " + error.Message; return false; }
        finally { owner.Dispose(); cancellation = null; workspace.IsEnabled = sideNavigation.IsEnabled = homeButton.IsEnabled = true; execute.IsEnabled = saveShortcut.IsEnabled = CanRun; cancel.IsVisible = progress.IsVisible = false; }
    }

    public bool SavePreparedToPath(string destination)
    {
        if (preparedOutput == null || cancellation != null) return false;
        string? staging = null;
        try
        {
            var target = Path.GetFullPath(destination); var parent = Path.GetDirectoryName(target)!;
            if (!Directory.Exists(parent) || File.Exists(target) || Directory.Exists(target)) throw new ArgumentException("Elige una carpeta existente y un nombre nuevo para conservar los originales.");
            ReviewedCopy.CopyAsync(preparedOutput, target, CancellationToken.None).GetAwaiter().GetResult();
            lastOutput = target; status.Text = "Copia guardada. El resultado revisado sigue disponible.";
            if (resultLocation != null) resultLocation.Text = target;
            reveal.IsVisible = true; if (successArt != null) successArt.IsVisible = true; return true;
        }
        catch (Exception error) { status.Text = "No se pudo guardar la copia. " + error.Message; return false; }
        finally { if (staging != null) { if (File.Exists(staging)) File.Delete(staging); if (Directory.Exists(staging)) Directory.Delete(staging, true); } }
    }
}
