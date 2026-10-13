using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace AiOPDF;

public static class PdfOperations
{
    public static int[][] SplitGroups(string pages, int count, int mode, int size)
    {
        if (mode is < 0 or > 2 || size < 1) throw new ArgumentException("Selecciona un modo de división y un tamaño de grupo válido.");
        if (mode == 1)
        {
            if (string.IsNullOrWhiteSpace(pages)) throw new ArgumentException("Escribe los rangos separados por comas. Ejemplo: 1-2,3-6.");
            return pages.Split(',').Select(range => ParsePages(range, count)).ToArray();
        }
        var selected = ParsePages(pages, count);
        return selected.Chunk(mode == 0 ? 1 : size).ToArray();
    }

    public static int[] ParsePages(string value, int count)
    {
        if (count < 1) throw new ArgumentException("El documento no tiene páginas.");
        if (string.IsNullOrWhiteSpace(value)) return Enumerable.Range(1, count).ToArray();
        var result = new List<int>();
        foreach (var item in value.Split(','))
        {
            var range = item.Trim().Split('-');
            if (range.Length > 2 || !int.TryParse(range[0], out var first))
                throw new ArgumentException("Usa páginas como 1,3,5-8.");
            var last = first;
            if (range.Length == 2 && !int.TryParse(range[1], out last))
                throw new ArgumentException("El rango de páginas no es válido.");
            if (first < 1 || last > count || first > count || last < first)
                throw new ArgumentException($"Las páginas deben estar entre 1 y {count}, con rangos ascendentes.");
            for (var page = first; page <= last; page++) result.Add(page);
        }
        return result.ToArray();
    }

    public static void Run(string operation, string[] inputs, string output, string pages, int rotation, CancellationToken token)
    {
        if (inputs.Length == 0) throw new ArgumentException("Selecciona al menos un archivo.");
        if (inputs.Any(path => Path.GetFullPath(path).Equals(Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Guarda el resultado con un nombre distinto al original.");
        // Create the temporary result beside the destination so the final rename is on the same volume.
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, $".aiopdf-{Guid.NewGuid():N}.tmp");
        try
        {
            using var document = new PdfDocument();
            if (operation == "Imágenes a PDF")
            {
                foreach (var path in inputs)
                {
                    token.ThrowIfCancellationRequested();
                    using var image = XImage.FromFile(path);
                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(image.PointWidth);
                    page.Height = XUnit.FromPoint(image.PointHeight);
                    using var graphics = XGraphics.FromPdfPage(page);
                    graphics.DrawImage(image, 0, 0, image.PointWidth, image.PointHeight);
                }
            }
            else
            {
                if (operation != "Unir PDF" && inputs.Length != 1)
                    throw new ArgumentException("Esta herramienta trabaja con un PDF a la vez.");
                foreach (var path in inputs)
                {
                    token.ThrowIfCancellationRequested();
                    using var source = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                    var selection = operation == "Unir PDF" ? Enumerable.Range(1, source.PageCount).ToArray() : ParsePages(pages, source.PageCount);
                    if (operation == "Eliminar páginas")
                    {
                        if (string.IsNullOrWhiteSpace(pages)) throw new ArgumentException("Indica las páginas que quieres eliminar.");
                        selection = Enumerable.Range(1, source.PageCount).Except(selection).ToArray();
                    }
                    var rotateSelection = selection.ToHashSet();
                    if (operation == "Rotar PDF") selection = Enumerable.Range(1, source.PageCount).ToArray();
                    foreach (var number in selection)
                    {
                        token.ThrowIfCancellationRequested();
                        var page = document.AddPage(source.Pages[number - 1]);
                        if (operation == "Rotar PDF" && rotateSelection.Contains(number)) page.Rotate = (page.Rotate + rotation) % 360;
                    }
                }
            }
            if (document.PageCount == 0) throw new ArgumentException("El resultado debe conservar al menos una página.");
            token.ThrowIfCancellationRequested();
            document.Save(temporary);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, output, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
