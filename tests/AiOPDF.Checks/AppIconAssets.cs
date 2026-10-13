using SkiaSharp;

static class AppIconAssets
{
    public static void Generate(string folder)
    {
        using var source = SKBitmap.Decode("branding/aiopdf-icon-master.png") ?? throw new InvalidDataException("El máster del icono no se pudo leer.");
        using var sourceImage = SKImage.FromBitmap(source);
        var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256, 512, 1024 };
        var icons = new List<(int Size, byte[] Data)>();
        foreach (var size in sizes)
        {
            using var bitmap = new SKBitmap(size, size); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent);
            canvas.DrawImage(sourceImage, new SKRect(0, 0, size, size), new SKSamplingOptions(SKFilterMode.Linear));
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100); var bytes = png.ToArray();
            File.WriteAllBytes($"{folder}/app-icon-{size}.png", bytes);
            if (size == 1024) File.WriteAllBytes(folder + "/app-icon.png", bytes);
            if (size <= 256) icons.Add((size, bytes));
        }
        using (var writer = new BinaryWriter(File.Create(folder + "/app-icon.ico")))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)icons.Count);
            var offset = 6 + icons.Count * 16;
            foreach (var icon in icons) { writer.Write((byte)(icon.Size == 256 ? 0 : icon.Size)); writer.Write((byte)(icon.Size == 256 ? 0 : icon.Size)); writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)32); writer.Write((uint)icon.Data.Length); writer.Write((uint)offset); offset += icon.Data.Length; }
            foreach (var icon in icons) writer.Write(icon.Data);
        }
        // Preview at actual desktop sizes on both light and dark backgrounds.
        Directory.CreateDirectory("docs/screenshots");
        using var preview = new SKBitmap(640, 240); using var sheet = new SKCanvas(preview); sheet.Clear(SKColor.Parse("#FCF9F5"));
        using var background = new SKPaint { Color = SKColor.Parse("#211C34") }; sheet.DrawRect(0, 120, 640, 120, background);
        foreach (var size in new[] { 16, 24, 32, 48, 64, 96 })
        {
            using var image = SKBitmap.Decode(File.ReadAllBytes($"{folder}/app-icon-{(size == 96 ? 128 : size)}.png"));
            using var previewImage = SKImage.FromBitmap(image);
            var x = Array.IndexOf(new[] { 16, 24, 32, 48, 64, 96 }, size) * 104 + (104 - size) / 2;
            sheet.DrawImage(previewImage, new SKRect(x, (120 - size) / 2, x + size, (120 + size) / 2), new SKSamplingOptions(SKFilterMode.Linear)); sheet.DrawImage(previewImage, new SKRect(x, 120 + (120 - size) / 2, x + size, 120 + (120 + size) / 2), new SKSamplingOptions(SKFilterMode.Linear));
        }
        using var previewPng = preview.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes("docs/screenshots/app-icon-sizes.png", previewPng.ToArray());
    }
}
