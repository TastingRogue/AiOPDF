using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using S = DocumentFormat.OpenXml.Spreadsheet;
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using PDFtoImage;
using SkiaSharp;

namespace AiOPDF;

// These exports intentionally state their fidelity limits in the UI: text-only DOCX/XLSX, image slides.
public static class OfficeExports
{
    public static void Run(PdfJob job, string output, CancellationToken token, IProgress<string>? progress)
    {
        if (job.Tool.Kind == ToolKind.Slides) { Slides(job, output, token, progress); return; }
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(job.Inputs[0], new UglyToad.PdfPig.ParsingOptions { Password = job.Password });
        var selected = PdfOperations.ParsePages(job.Pages, pdf.NumberOfPages);
        string PageText(int number) => UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(pdf.GetPage(number));
        if (job.Tool.Kind == ToolKind.Word)
        {
            using var document = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document);
            var part = document.AddMainDocumentPart(); var body = new W.Body(); part.Document = new W.Document(body);
            for (var i = 0; i < selected.Length; i++)
            {
                token.ThrowIfCancellationRequested(); progress?.Report($"Exportando página {selected[i]}…");
                if (i > 0) body.Append(new W.Paragraph(new W.Run(new W.Break { Type = W.BreakValues.Page })));
                foreach (var line in PageText(selected[i]).Split('\n')) body.Append(new W.Paragraph(new W.Run(new W.Text(line.TrimEnd('\r')) { Space = SpaceProcessingModeValues.Preserve })));
            }
            body.Append(new W.SectionProperties(new W.PageSize { Width = 11906, Height = 16838 }, new W.PageMargin { Top = 1000, Bottom = 1000, Left = 1000, Right = 1000 })); part.Document.Save();
        }
        else
        {
            using var workbook = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook);
            var main = workbook.AddWorkbookPart(); main.Workbook = new S.Workbook(); var sheets = main.Workbook.AppendChild(new S.Sheets());
            var part = main.AddNewPart<WorksheetPart>(); var data = new S.SheetData(); part.Worksheet = new S.Worksheet(data);
            sheets.Append(new S.Sheet { Id = main.GetIdOfPart(part), SheetId = 1, Name = "Texto PDF" });
            uint row = 1;
            void Add(string page, string value) => data.Append(new S.Row(new S.Cell { DataType = S.CellValues.InlineString, InlineString = new S.InlineString(new S.Text(page)) }, new S.Cell { DataType = S.CellValues.InlineString, InlineString = new S.InlineString(new S.Text(value) { Space = SpaceProcessingModeValues.Preserve }) }) { RowIndex = row++ });
            Add("Página", "Texto");
            foreach (var number in selected) { token.ThrowIfCancellationRequested(); foreach (var line in PageText(number).Split('\n')) Add(number.ToString(), line.TrimEnd('\r')); }
            part.Worksheet.Save(); main.Workbook.Save();
        }
    }

    private static void Slides(PdfJob job, string output, CancellationToken token, IProgress<string>? progress)
    {
        using var source = PdfEngine.Open(job.Inputs[0], job.Password);
        using var pdf = File.OpenRead(job.Inputs[0]);
        using var document = PresentationDocument.Create(output, PresentationDocumentType.Presentation);
        var main = document.AddPresentationPart(); var ids = new P.SlideIdList();
        var master = main.AddNewPart<SlideMasterPart>(); var layout = master.AddNewPart<SlideLayoutPart>(); layout.AddPart(master);
        layout.SlideLayout = new P.SlideLayout(new P.CommonSlideData(EmptyTree()) { Name = "Página PDF" }, new P.ColorMapOverride(new A.MasterColorMapping())) { Type = P.SlideLayoutValues.Blank, Preserve = true };
        master.SlideMaster = new P.SlideMaster(new P.CommonSlideData(EmptyTree()),
            new P.ColorMap { Background1 = A.ColorSchemeIndexValues.Light1, Text1 = A.ColorSchemeIndexValues.Dark1, Background2 = A.ColorSchemeIndexValues.Light2, Text2 = A.ColorSchemeIndexValues.Dark2,
                Accent1 = A.ColorSchemeIndexValues.Accent1, Accent2 = A.ColorSchemeIndexValues.Accent2, Accent3 = A.ColorSchemeIndexValues.Accent3, Accent4 = A.ColorSchemeIndexValues.Accent4, Accent5 = A.ColorSchemeIndexValues.Accent5, Accent6 = A.ColorSchemeIndexValues.Accent6, Hyperlink = A.ColorSchemeIndexValues.Hyperlink, FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink },
            new P.SlideLayoutIdList(new P.SlideLayoutId { Id = 2147483649U, RelationshipId = master.GetIdOfPart(layout) }), new P.TextStyles(new P.TitleStyle(), new P.BodyStyle(), new P.OtherStyle()));
        var theme = master.AddNewPart<ThemePart>();
        const string ns = "http://schemas.openxmlformats.org/drawingml/2006/main";
        var colors = new[] { ("dk1", "203D38"), ("lt1", "FFFFFF"), ("dk2", "6F7F7B"), ("lt2", "F6F8F6"), ("accent1", "26756A"), ("accent2", "A66225"), ("accent3", "7760A5"), ("accent4", "496B9A"), ("accent5", "AA586B"), ("accent6", "EAF3EE"), ("hlink", "26756A"), ("folHlink", "7760A5") };
        var colorXml = string.Concat(colors.Select(c => $"<a:{c.Item1}><a:srgbClr val=\"{c.Item2}\"/></a:{c.Item1}>"));
        const string fill = "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>";
        const string font = "<a:latin typeface=\"Lato\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/>";
        var styles = $"<a:fillStyleLst>{fill}{fill}{fill}</a:fillStyleLst><a:lnStyleLst>{string.Concat(Enumerable.Repeat($"<a:ln w=\"9525\">{fill}<a:prstDash val=\"solid\"/></a:ln>", 3))}</a:lnStyleLst><a:effectStyleLst>{string.Concat(Enumerable.Repeat("<a:effectStyle><a:effectLst/></a:effectStyle>", 3))}</a:effectStyleLst><a:bgFillStyleLst>{fill}{fill}{fill}</a:bgFillStyleLst>";
        var themeXml = $"<a:theme xmlns:a=\"{ns}\" name=\"AiOPDF\"><a:themeElements><a:clrScheme name=\"AiOPDF\">{colorXml}</a:clrScheme><a:fontScheme name=\"AiOPDF\"><a:majorFont>{font}</a:majorFont><a:minorFont>{font}</a:minorFont></a:fontScheme><a:fmtScheme name=\"AiOPDF\">{styles}</a:fmtScheme></a:themeElements></a:theme>";
        using (var themeStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(themeXml))) theme.FeedData(themeStream);
        main.Presentation = new P.Presentation(new P.SlideMasterIdList(new P.SlideMasterId { Id = 2147483648U, RelationshipId = main.GetIdOfPart(master) }), ids, new P.SlideSize { Cx = 9144000, Cy = 6858000 }, new P.NotesSize { Cx = 6858000, Cy = 9144000 });
        uint index = 256;
        foreach (var number in PdfOperations.ParsePages(job.Pages, source.PageCount))
        {
            token.ThrowIfCancellationRequested(); progress?.Report($"Creando diapositiva {number}…");
            ProcessingLimits.Page(source.Pages[number - 1], job.Dpi);
            using var bitmap = Conversion.ToImage(pdf, leaveOpen: true, page: number - 1, password: job.Password, options: new RenderOptions(Dpi: job.Dpi));
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); using var stream = new MemoryStream(data.ToArray());
            var slide = main.AddNewPart<SlidePart>(); slide.AddPart(layout); var image = slide.AddImagePart(ImagePartType.Png); image.FeedData(stream);
            var ratio = Math.Min(9144000.0 / bitmap.Width, 6858000.0 / bitmap.Height); var width = (long)(bitmap.Width * ratio); var height = (long)(bitmap.Height * ratio);
            var picture = new P.Picture(new P.NonVisualPictureProperties(new P.NonVisualDrawingProperties { Id = 2, Name = $"Página {number}" }, new P.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true }), new P.ApplicationNonVisualDrawingProperties()),
                new P.BlipFill(new A.Blip { Embed = slide.GetIdOfPart(image) }, new A.Stretch(new A.FillRectangle())),
                new P.ShapeProperties(new A.Transform2D(new A.Offset { X = (9144000 - width) / 2, Y = (6858000 - height) / 2 }, new A.Extents { Cx = width, Cy = height }), new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));
            var tree = new P.ShapeTree(new P.NonVisualGroupShapeProperties(new P.NonVisualDrawingProperties { Id = 1, Name = "" }, new P.NonVisualGroupShapeDrawingProperties(), new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new A.TransformGroup(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = 0, Cy = 0 }, new A.ChildOffset { X = 0, Y = 0 }, new A.ChildExtents { Cx = 0, Cy = 0 })), picture);
            slide.Slide = new P.Slide(new P.CommonSlideData(tree), new P.ColorMapOverride(new A.MasterColorMapping())); slide.Slide.Save(); ids.Append(new P.SlideId { Id = index++, RelationshipId = main.GetIdOfPart(slide) });
        }
        main.Presentation.Save();
    }

    private static P.ShapeTree EmptyTree() => new(new P.NonVisualGroupShapeProperties(new P.NonVisualDrawingProperties { Id = 1, Name = "" }, new P.NonVisualGroupShapeDrawingProperties(), new P.ApplicationNonVisualDrawingProperties()),
        new P.GroupShapeProperties(new A.TransformGroup(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = 0, Cy = 0 }, new A.ChildOffset { X = 0, Y = 0 }, new A.ChildExtents { Cx = 0, Cy = 0 })));
}

