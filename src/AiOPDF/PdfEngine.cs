using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFtoImage;
using SkiaSharp;

namespace AiOPDF;

public static class PdfEngine
{
    public static PdfDocument Open(string path, string password = "", PdfDocumentOpenMode mode = PdfDocumentOpenMode.Import)
    {
        ProcessingLimits.Input(path);
        var document = string.IsNullOrEmpty(password) ? PdfReader.Open(path, mode) : PdfReader.Open(path, password, mode);
        if (document.PageCount > ProcessingLimits.MaximumPages) { document.Dispose(); throw new ArgumentException("El documento supera el límite de 10 000 páginas."); }
        return document;
    }

    public static int PageCount(string path, string password = "")
    {
        using var document = Open(path, password); return document.PageCount;
    }

    public static byte[] Thumbnail(string path, int page, string password, int width = 180)
    {
        using var document = Open(path, password); var size = PdfGeometry.VisibleSize(document.Pages[page]); ProcessingLimits.Raster(width, width * size.Height / size.Width);
        using var stream = File.OpenRead(path);
        using var bitmap = Conversion.ToImage(stream, leaveOpen: true, page: page, password: password, options: new RenderOptions(Width: width, WithAspectRatio: true));
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
    }

    public static JobResult Run(PdfJob job, CancellationToken token, IProgress<string>? progress = null, ContentComparisonResult? comparisonResult = null)
    {
        AppFontResolver.Initialize();
        Validate(job);
        token.ThrowIfCancellationRequested();
        var parent = Path.GetDirectoryName(Path.GetFullPath(job.Output))!;
        if (!Directory.Exists(parent)) throw new ArgumentException("La carpeta de destino no existe.");
        var staging = Path.Combine(parent, $".aiopdf-{Guid.NewGuid():N}");
        try
        {
            if (job.Tool.Folder)
            {
                Directory.CreateDirectory(staging);
                var count = WriteFolder(job, staging, token, progress, comparisonResult);
                token.ThrowIfCancellationRequested();
                Directory.Move(staging, job.Output);
                return new(job.Output, count, $"Listo. Se guardaron {count} archivos.");
            }
            if (job.Tool.Kind is ToolKind.Ocr or ToolKind.OcrText) OcrOperations.Run(job, staging, token, progress);
            else if (job.Tool.Kind is ToolKind.Word or ToolKind.Excel or ToolKind.Slides) OfficeExports.Run(job, staging, token, progress);
            else if (job.Tool.Kind == ToolKind.Text) WriteText(job, staging, token);
            else WritePdf(job, staging, token, progress);
            token.ThrowIfCancellationRequested();
            File.Move(staging, job.Output, overwrite: false);
            if (job.Tool.Kind == ToolKind.Compress)
            {
                var before = new FileInfo(job.Inputs[0]).Length;
                var after = new FileInfo(job.Output).Length;
                return new(job.Output, 1, after < before
                    ? $"Guardado: {before:N0} → {after:N0} bytes. Reducción del {100.0 * (before - after) / before:F1}%."
                    : "No se pudo reducir sin perder contenido. Se guardó una copia del original.");
            }
            return new(job.Output, 1, "Listo. Tu archivo está guardado.");
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private static void Validate(PdfJob job)
    {
        if (job.Inputs.Length == 0 || job.Inputs.Any(p => !File.Exists(p))) throw new ArgumentException("Selecciona archivos locales existentes.");
        if (!job.Tool.Multiple && job.Inputs.Length != 1) throw new ArgumentException("Selecciona un solo documento.");
        if (job.Tool.Kind == ToolKind.Compare && job.Inputs.Length != 2) throw new ArgumentException("Selecciona exactamente dos PDFs.");
        if (File.Exists(job.Output) || Directory.Exists(job.Output)) throw new ArgumentException("Elige un destino nuevo para conservar los archivos existentes.");
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (job.Inputs.Any(p => Path.GetFullPath(p).Equals(Path.GetFullPath(job.Output), comparison))) throw new ArgumentException("El destino no puede ser un original.");
        if (job.Dpi is < 36 or > 300 || job.Quality is < 10 or > 100) throw new ArgumentException("La resolución o calidad está fuera del rango permitido.");
        if (job.Tool.Kind == ToolKind.Protect && job.NewPassword.Length < 8) throw new ArgumentException("Usa una contraseña de al menos 8 caracteres.");
        if (job.Tool.Kind == ToolKind.Watermark && string.IsNullOrWhiteSpace(job.Text)) throw new ArgumentException("Escribe el texto de la marca de agua.");
        if (job.Tool.Kind == ToolKind.Delete && string.IsNullOrWhiteSpace(job.Pages)) throw new ArgumentException("Indica las páginas que quieres eliminar.");
        if (job.Rotation is not (90 or 180 or 270)) throw new ArgumentException("Selecciona un giro de 90, 180 o 270 grados.");
        if (job.WatermarkOpacity is < 1 or > 100 || job.ImageMargin is < 0 or > 50) throw new ArgumentException("La opacidad o el margen no es válido.");
    }

    private static void WritePdf(PdfJob job, string target, CancellationToken token, IProgress<string>? progress)
    {
        if (job.Tool.Kind == ToolKind.Compress)
        {
            using var source = Open(job.Inputs[0], job.Password, PdfDocumentOpenMode.Modify);
            source.Options.CompressContentStreams = true;
            source.Options.FlateEncodeMode = PdfSharp.Pdf.PdfFlateEncodeMode.BestCompression;
            foreach (var item in source.Internals.GetAllObjects())
            {
                token.ThrowIfCancellationRequested();
                if (item is PdfDictionary dictionary && dictionary.Stream != null && !dictionary.Elements.ContainsKey("/Filter"))
                {
                    var bytes = dictionary.Stream.Value;
                    using var buffer = new MemoryStream();
                    using (var compressor = new System.IO.Compression.ZLibStream(buffer, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
                        compressor.Write(bytes);
                    if (buffer.Length + 30 < bytes.Length)
                    {
                        dictionary.Stream.Value = buffer.ToArray();
                        dictionary.Elements.SetName("/Filter", "/FlateDecode");
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            source.Save(target);
            if (new FileInfo(target).Length >= new FileInfo(job.Inputs[0]).Length)
                File.Copy(job.Inputs[0], target, overwrite: true);
            return;
        }
        if (job.Tool.Kind is ToolKind.Protect or ToolKind.Unlock or ToolKind.Metadata or ToolKind.Watermark or ToolKind.Number or ToolKind.Crop)
        {
            using var source = Open(job.Inputs[0], job.Password, PdfDocumentOpenMode.Modify);
            var selection = PdfOperations.ParsePages(job.Pages, source.PageCount).ToHashSet();
            if (job.Tool.Kind == ToolKind.Protect)
            {
                source.SecuritySettings.UserPassword = job.NewPassword;
                source.SecuritySettings.OwnerPassword = job.NewPassword;
                source.SecurityHandler.SetEncryptionToV5();
            }
            if (job.Tool.Kind == ToolKind.Unlock) source.SecurityHandler.SetEncryptionToNoneAndResetPasswords();
            if (job.Tool.Kind == ToolKind.Metadata)
            {
                if (job.ClearMetadata)
                {
                    source.Info.Elements.Clear(); source.Internals.Catalog.Elements.Remove("/Metadata");
                }
                else { source.Info.Title = job.Text; source.Info.Author = job.Author; }
            }
            var nextNumber = job.StartNumber;
            for (var i = 0; i < source.PageCount; i++)
            {
                token.ThrowIfCancellationRequested();
                if (!selection.Contains(i + 1)) continue;
                var page = source.Pages[i];
                if (job.Tool.Kind == ToolKind.Crop)
                {
                    if (job.CropRegion) { page.CropBox = PdfGeometry.CropRegion(page, job.RedactX, job.RedactY, job.RedactWidth, job.RedactHeight); continue; }
                    var box = page.CropBox;
                    if (box.Width <= 0 || box.Height <= 0) box = page.MediaBox;
                    var margin = job.Margin * 72 / 25.4;
                    if (margin < 0 || 2 * margin >= Math.Min(box.Width, box.Height)) throw new ArgumentException("El margen es demasiado grande para esta página.");
                    page.CropBox = new PdfRectangle(new XPoint(box.X1 + margin, box.Y1 + margin), new XPoint(box.X2 - margin, box.Y2 - margin));
                }
                if (job.Tool.Kind is ToolKind.Watermark or ToolKind.Number)
                {
                    using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                    if (job.Tool.Kind == ToolKind.Watermark)
                    {
                        var font = new XFont("AiOPDF Sans", 32);
                        var size = graphics.MeasureString(job.Text, font);
                        var available = Math.Max(1, page.Width.Point - 24);
                        if (size.Width > available)
                        {
                            font = new XFont("AiOPDF Sans", 32 * available / size.Width);
                            size = graphics.MeasureString(job.Text, font);
                        }
                        DrawStamp(graphics, page, job.Text, font, new XSolidBrush(XColor.FromArgb((int)(255 * job.WatermarkOpacity / 100.0), 38, 117, 106)), job.StampPosition);
                    }
                    else
                    {
                        var number = nextNumber++;
                        var value = job.NumberFormat switch { "Página n" => $"Página {number}", "Página n de N" => $"Página {number} de {source.PageCount}", _ => number.ToString() };
                        DrawStamp(graphics, page, value, new XFont("AiOPDF Sans", 11), XBrushes.DarkSlateGray, job.StampPosition);
                    }
                }
            }
            token.ThrowIfCancellationRequested(); source.Save(target); return;
        }

        using var document = new PdfDocument();
        if (job.Tool.Kind == ToolKind.Images)
        {
            foreach (var file in job.Inputs)
            {
                token.ThrowIfCancellationRequested();
                ProcessingLimits.Input(file);
                using (var codec = SKCodec.Create(file) ?? throw new ArgumentException("Imagen no compatible.")) ProcessingLimits.Raster(codec.Info.Width, codec.Info.Height);
                using var bitmap = SKBitmap.Decode(file) ?? throw new ArgumentException("Imagen no compatible.");
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(encoded.ToArray());
                using var image = XImage.FromStream(stream);
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(bitmap.Width * 72.0 / 96); page.Height = XUnit.FromPoint(bitmap.Height * 72.0 / 96);
                if (job.ImagePaper != "Ajustado a la imagen")
                {
                    if (job.ImagePaper is not ("A4" or "Letter")) throw new ArgumentException("Selecciona A4, Letter o ajustado a la imagen.");
                    var width = job.ImagePaper == "A4" ? 595.276 : 612;
                    var height = job.ImagePaper == "A4" ? 841.89 : 792;
                    page.Width = XUnit.FromPoint(job.Landscape ? height : width); page.Height = XUnit.FromPoint(job.Landscape ? width : height);
                }
                using var graphics = XGraphics.FromPdfPage(page);
                var inset = job.ImagePaper == "Ajustado a la imagen" ? 0 : job.ImageMargin * 72 / 25.4;
                var scale = Math.Min((page.Width.Point - 2 * inset) / bitmap.Width, (page.Height.Point - 2 * inset) / bitmap.Height);
                if (scale <= 0) throw new ArgumentException("El margen no deja espacio para la imagen.");
                var drawnWidth = bitmap.Width * scale; var drawnHeight = bitmap.Height * scale;
                graphics.DrawImage(image, (page.Width.Point - drawnWidth) / 2, (page.Height.Point - drawnHeight) / 2, drawnWidth, drawnHeight);
            }
        }
        else if (job.Tool.Kind is ToolKind.Flatten or ToolKind.Grayscale or ToolKind.Redact)
        {
            using var source = Open(job.Inputs[0], job.Password);
            var selected = PdfOperations.ParsePages(job.Pages, source.PageCount).ToHashSet();
            using var pdfStream = File.OpenRead(job.Inputs[0]);
            foreach (var number in Enumerable.Range(1, source.PageCount))
            {
                if (!selected.Contains(number)) { document.AddPage(source.Pages[number - 1]); continue; }
                token.ThrowIfCancellationRequested(); progress?.Report($"Procesando página {number} de {source.PageCount}…");
                ProcessingLimits.Page(source.Pages[number - 1], job.Dpi);
                using var bitmap = Conversion.ToImage(pdfStream, leaveOpen: true, page: number - 1, password: job.Password,
                    options: new RenderOptions(Dpi: job.Dpi, Grayscale: job.Tool.Kind == ToolKind.Grayscale));
                if (job.Tool.Kind == ToolKind.Redact)
                {
                    if (job.RedactX < 0 || job.RedactY < 0 || job.RedactWidth <= 0 || job.RedactHeight <= 0 || job.RedactX + job.RedactWidth > 100 || job.RedactY + job.RedactHeight > 100)
                        throw new ArgumentException("La región de censura debe estar dentro de la página (0–100%).");
                    using var canvas = new SKCanvas(bitmap);
                    using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = false };
                    canvas.DrawRect(new SKRect((float)Math.Floor(bitmap.Width * job.RedactX / 100), (float)Math.Floor(bitmap.Height * job.RedactY / 100),
                        (float)Math.Ceiling(bitmap.Width * (job.RedactX + job.RedactWidth) / 100), (float)Math.Ceiling(bitmap.Height * (job.RedactY + job.RedactHeight) / 100)), paint);
                }
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var data = new MemoryStream(encoded.ToArray()); using var image = XImage.FromStream(data);
                var original = source.Pages[number - 1];
                var page = document.AddPage();
                var visible = PdfGeometry.VisibleSize(original);
                page.Width = XUnit.FromPoint(visible.Width);
                page.Height = XUnit.FromPoint(visible.Height);
                using var graphics = XGraphics.FromPdfPage(page); graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
            }
        }
        else foreach (var path in job.Inputs)
        {
            using var source = Open(path, job.Password);
            var chosen = job.Tool.Kind == ToolKind.Merge ? Enumerable.Range(1, source.PageCount).ToArray() : PdfOperations.ParsePages(job.Pages, source.PageCount);
            var rotating = chosen.ToHashSet();
            if (job.Tool.Kind == ToolKind.Delete) chosen = Enumerable.Range(1, source.PageCount).Except(chosen).ToArray();
            if (job.Tool.Kind == ToolKind.Rotate) chosen = Enumerable.Range(1, source.PageCount).ToArray();
            foreach (var number in chosen)
            {
                token.ThrowIfCancellationRequested();
                var page = document.AddPage(source.Pages[number - 1]);
                if (job.Tool.Kind == ToolKind.Rotate && rotating.Contains(number)) page.Rotate = (page.Rotate + job.Rotation) % 360;
            }
        }
        if (document.PageCount == 0) throw new ArgumentException("El resultado debe conservar al menos una página.");
        token.ThrowIfCancellationRequested(); document.Save(target);
    }

    static void DrawStamp(XGraphics graphics, PdfPage page, string value, XFont font, XBrush brush, string position)
    {
        var size = graphics.MeasureString(value, font); var parts = position.Split(" · ");
        var x = parts.Last() switch { "izquierda" => 18, "derecha" => page.Width.Point - size.Width - 18, _ => (page.Width.Point - size.Width) / 2 };
        var y = parts.First() switch { "Arriba" => 18 + size.Height, "Centro" => (page.Height.Point + size.Height) / 2, _ => page.Height.Point - 18 };
        graphics.DrawString(value, font, brush, Math.Max(0, x), y);
    }

    private static int WriteFolder(PdfJob job, string folder, CancellationToken token, IProgress<string>? progress, ContentComparisonResult? comparisonResult)
    {
        if (job.Tool.Kind == ToolKind.Compare) return ContentComparison.Write(comparisonResult ?? ContentComparison.Run(job.Inputs[0], job.Inputs[1], job.Password, token, progress), folder, token);
        using var source = Open(job.Inputs[0], job.Password);
        var selected = PdfOperations.ParsePages(job.Pages, source.PageCount);
        if (job.Tool.Kind == ToolKind.Split)
        {
            var groups = PdfOperations.SplitGroups(job.Pages, source.PageCount, job.SplitMode, job.GroupSize);
            for (var group = 0; group < groups.Length; group++)
            {
                using var output = new PdfDocument();
                foreach (var number in groups[group]) { token.ThrowIfCancellationRequested(); output.AddPage(source.Pages[number - 1]); }
                progress?.Report($"Archivo {group + 1} de {groups.Length}…");
                output.Save(Path.Combine(folder, $"{group + 1:D4}-paginas-{groups[group][0]}-{groups[group][^1]}.pdf"));
            }
            return groups.Length;
        }
        var index = 0;
        using var pdfStream = File.OpenRead(job.Inputs[0]);
        foreach (var number in selected)
        {
            token.ThrowIfCancellationRequested(); progress?.Report($"Página {++index} de {selected.Length}…");
            var stem = Path.Combine(folder, $"{index:D4}-pagina-{number}");
            if (job.Tool.Kind == ToolKind.Split)
            {
                using var output = new PdfDocument(); output.AddPage(source.Pages[number - 1]); output.Save(stem + ".pdf");
            }
            else
            {
                ProcessingLimits.Page(source.Pages[number - 1], job.Dpi);
                using var image = Conversion.ToImage(pdfStream, leaveOpen: true, page: number - 1, password: job.Password, options: new RenderOptions(Dpi: job.Dpi));
                using var data = image.Encode(job.Tool.Kind == ToolKind.Png ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg, job.Quality);
                File.WriteAllBytes(stem + (job.Tool.Kind == ToolKind.Png ? ".png" : ".jpg"), data.ToArray());
            }
        }
        return index;
    }

    private static void WriteText(PdfJob job, string path, CancellationToken token)
    {
        using var source = UglyToad.PdfPig.PdfDocument.Open(job.Inputs[0], new UglyToad.PdfPig.ParsingOptions { Password = job.Password });
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        foreach (var number in PdfOperations.ParsePages(job.Pages, source.NumberOfPages))
        {
            token.ThrowIfCancellationRequested();
            writer.WriteLine($"--- Página {number} ---");
            var page = source.GetPage(number);
            writer.WriteLine(UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page));
            writer.WriteLine();
        }
    }

}
