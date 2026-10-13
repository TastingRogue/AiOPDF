using AiOPDF;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

// Separate native test session: exercises the same MainWindow at a compact width.
public sealed class NativeButtonApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://AiOPDF/")) { Source = new Uri("avares://AiOPDF/StudioTheme.axaml") });
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow { Width = 860, Height = 620 };
            window.SelectTool(ToolCatalog.All.Single(t => t.Kind == ToolKind.Split));
            window.Opened += async (_, _) => await window.AddPaths([Path.GetFullPath("output/pdf/AiOPDF-prueba-A.pdf")]);
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
    public static void Run() => AppBuilder.Configure<NativeButtonApp>().UsePlatformDetect().StartWithClassicDesktopLifetime([]);
}
