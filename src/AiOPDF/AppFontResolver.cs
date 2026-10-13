using PdfSharp.Fonts;

namespace AiOPDF;

public sealed class AppFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        using var stream = typeof(AppFontResolver).Assembly.GetManifestResourceStream("AiOPDF.Assets.Lato-Regular.ttf")
            ?? throw new InvalidOperationException("La fuente integrada no está disponible.");
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new("AiOPDF Sans", bold, italic);
    public static void Initialize()
    {
        if (GlobalFontSettings.FontResolver == null) GlobalFontSettings.FontResolver = new AppFontResolver();
    }
}
