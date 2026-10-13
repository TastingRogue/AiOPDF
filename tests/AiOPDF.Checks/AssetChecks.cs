using AiOPDF;
using SkiaSharp;

static class AssetChecks
{
    public static void Run()
    {
        var folder = "src/AiOPDF/Assets"; Directory.CreateDirectory(folder + "/icons");
        foreach (var tool in ToolCatalog.All)
            File.WriteAllText($"{folder}/icons/{tool.Kind.ToString().ToLowerInvariant()}.svg", $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><g fill=\"none\" stroke=\"{tool.Color}\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path fill=\"#FFFFFF\" d=\"{BrandIcon.Frame(tool.Kind)}\"/><path d=\"{BrandIcon.Glyph(tool.Kind)}\"/></g></svg>");
        AppIconAssets.Generate(folder);
        Console.WriteLine("PASS original SVG icons and application icon generated");
    }
}

