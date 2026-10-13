using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;

namespace AiOPDF;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--package-check")
        {
            PackageCheck.Run(args[1]);
            return;
        }
        AppBuilder.Configure<App>().UsePlatformDetect()
            .With(new Avalonia.Media.FontManagerOptions { DefaultFamilyName = "avares://AiOPDF/Assets#Lato" })
            .StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
    public override void Initialize()
    {
        L10n.Initialize();
        LocalizedUI.Install();
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
