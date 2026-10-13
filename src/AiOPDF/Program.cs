using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;

namespace AiOPDF;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}

public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://AiOPDF/")) { Source = new Uri("avares://AiOPDF/StudioTheme.axaml") });
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
