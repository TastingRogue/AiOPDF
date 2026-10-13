namespace AiOPDF;

public enum ToolKind { Merge, Organize, Delete, Rotate, Images, Split, Png, Jpeg, Text, Watermark, Number, Crop, Protect, Unlock, Metadata, Flatten, Compress, Grayscale, Compare, Ocr, OcrText, Word, Excel, Slides, Redact }
public sealed record PdfTool(ToolKind Kind, string Title, string Description, string Category, string Color, bool Folder = false, bool Multiple = false)
{
    public string ActionLabel => Kind switch
    {
        ToolKind.Compare => "Comparar contenido",
        ToolKind.Merge => "Unir", ToolKind.Organize => "Organizar", ToolKind.Split => "Dividir",
        ToolKind.Delete => "Eliminar páginas", ToolKind.Rotate => "Rotar",
        ToolKind.Images => "Crear PDF", ToolKind.Png => "Exportar imágenes PNG", ToolKind.Jpeg => "Exportar imágenes JPG",
        ToolKind.Text => "Extraer texto", ToolKind.Watermark => "Añadir marca", ToolKind.Number => "Numerar",
        ToolKind.Crop => "Recortar", ToolKind.Metadata => "Aplicar datos", ToolKind.Protect => "Proteger",
        ToolKind.Unlock => "Quitar contraseña", ToolKind.Flatten => "Rasterizar", ToolKind.Compress => "Comprimir",
        ToolKind.Grayscale => "Convertir a grises", ToolKind.Ocr => "Reconocer texto", ToolKind.OcrText => "Reconocer texto",
        ToolKind.Word => "Exportar a Word", ToolKind.Excel => "Exportar a Excel", ToolKind.Slides => "Exportar diapositivas",
        ToolKind.Redact => "Censurar", _ => "Procesar"
    };
}

public static class ToolCatalog
{
    public static readonly PdfTool[] All =
    [
        new(ToolKind.Merge, "Unir PDF", "Junta documentos en el orden que elijas.", "Organizar", "#26756A", Multiple: true),
        new(ToolKind.Organize, "Organizar páginas", "Extrae, duplica y cambia el orden de páginas.", "Organizar", "#26756A"),
        new(ToolKind.Split, "Dividir PDF", "Separa por páginas, rangos o grupos del mismo tamaño.", "Organizar", "#26756A", Folder: true),
        new(ToolKind.Delete, "Eliminar páginas", "Quita las páginas que ya no necesitas.", "Organizar", "#26756A"),
        new(ToolKind.Rotate, "Rotar PDF", "Gira páginas y conserva el resto del documento.", "Organizar", "#26756A"),
        new(ToolKind.Images, "Imágenes a PDF", "Convierte JPG, PNG y WebP en un documento.", "Convertir", "#A66225", Multiple: true),
        new(ToolKind.Png, "PDF a PNG", "Exporta páginas como imágenes de alta calidad.", "Convertir", "#A66225", Folder: true),
        new(ToolKind.Jpeg, "PDF a JPG", "Crea imágenes fáciles de compartir.", "Convertir", "#A66225", Folder: true),
        new(ToolKind.Text, "Extraer texto", "Guarda el texto seleccionable en un archivo TXT.", "Convertir", "#A66225"),
        new(ToolKind.Watermark, "Marca de agua", "Añade un texto discreto a tus páginas.", "Editar", "#7760A5"),
        new(ToolKind.Number, "Numerar páginas", "Añade números al pie del documento.", "Editar", "#7760A5"),
        new(ToolKind.Crop, "Recortar PDF", "Oculta márgenes sin cambiar el tamaño del contenido.", "Editar", "#7760A5"),
        new(ToolKind.Metadata, "Metadatos", "Cambia el título y autor o limpia esos datos.", "Editar", "#7760A5"),
        new(ToolKind.Protect, "Proteger PDF", "Cifra tu documento con una contraseña.", "Seguridad", "#496B9A"),
        new(ToolKind.Unlock, "Quitar contraseña", "Crea una copia abierta usando tu contraseña.", "Seguridad", "#496B9A"),
        new(ToolKind.Flatten, "Rasterizar PDF", "Convierte las páginas a imágenes dentro de un PDF.", "Optimizar", "#AA586B"),
        new(ToolKind.Compress, "Comprimir PDF", "Optimiza sin pérdida y muestra la reducción real.", "Optimizar", "#AA586B"),
        new(ToolKind.Grayscale, "Escala de grises", "Crea un PDF de imágenes en blanco y negro.", "Optimizar", "#AA586B"),
        new(ToolKind.Compare, "Comparar PDFs", "Revisa texto añadido y eliminado entre dos versiones.", "Revisar", "#496B9A", Folder: true, Multiple: true),
        new(ToolKind.Ocr, "OCR a PDF", "Reconoce texto y crea páginas buscables.", "Convertir", "#A66225"),
        new(ToolKind.OcrText, "OCR a texto", "Lee escaneos con modelos locales de alfabeto latino.", "Convertir", "#A66225"),
        new(ToolKind.Word, "Texto a Word", "Exporta texto editable; no conserva el diseño.", "Convertir", "#A66225"),
        new(ToolKind.Excel, "Texto a Excel", "Exporta una línea por fila, sin reconstruir tablas.", "Convertir", "#A66225"),
        new(ToolKind.Slides, "Páginas a diapositivas", "Una imagen por diapositiva; contenido no editable.", "Convertir", "#A66225"),
        new(ToolKind.Redact, "Censurar región", "Elimina una región y crea páginas rasterizadas.", "Seguridad", "#496B9A")
    ];
}

public sealed record PdfJob(PdfTool Tool, string[] Inputs, string Output, string Pages = "", int Rotation = 90,
    string Password = "", string NewPassword = "", string Text = "", string Author = "", bool ClearMetadata = false,
    int Dpi = 144, int Quality = 75, double Margin = 18, int StartNumber = 1,
    double RedactX = 10, double RedactY = 10, double RedactWidth = 30, double RedactHeight = 10,
    int SplitMode = 0, int GroupSize = 2, string NumberFormat = "Número", string StampPosition = "Abajo · centro",
    int WatermarkOpacity = 25, string ImagePaper = "Ajustado a la imagen", bool Landscape = false, double ImageMargin = 0,
    bool CropRegion = false);
public sealed record JobResult(string Output, int Files, string Message);


