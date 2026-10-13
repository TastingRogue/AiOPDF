using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AiOPDF;

public sealed record TextChange(string Kind, string Text);
public sealed record ComparedWord(string Text, ComparisonUnderline Underline);
public sealed record PageComparison(int Page, string State, TextChange[] Changes)
{
    [System.Text.Json.Serialization.JsonIgnore] public ComparedWord[] OriginalWords { get; init; } = [];
    [System.Text.Json.Serialization.JsonIgnore] public ComparedWord[] UpdatedWords { get; init; } = [];
}
public sealed record TextHunk(string Before, string Removed, string Added, string After);
public sealed record ContentComparisonResult(string Original, string Updated, PageComparison[] Pages)
{
    public string Summary { get { var changed = Pages.Count(p => p.State == "changed"); var pending = Pages.Count(p => p.State == "unreadable"); return $"{changed} {(changed == 1 ? "página con cambios" : "páginas con cambios")} · {Pages.Count(p => p.State == "equal")} sin cambios · {pending} {(pending == 1 ? "pendiente" : "pendientes")} de OCR"; } }
    public string Report()
    {
        var report = new StringBuilder(L10n.Format("comparison.reportHeader", Original, Updated, L10n.Translate(Summary)));
        foreach (var page in Pages)
        {
            report.AppendLine(L10n.Format("comparison.reportPage", page.Page, L10n.Translate(page.State == "equal" ? "Sin cambios en el texto" : page.State == "unreadable" ? "Sin texto legible: aplicar OCR antes de comparar" : "Texto modificado")));
            foreach (var change in ContentComparison.Hunks(page.Changes)) { report.AppendLine(L10n.Format("comparison.reportContext", change.Before, change.After)); if (change.Removed.Length > 0) report.AppendLine(L10n.Text("comparison.reportRemoved") + change.Removed); if (change.Added.Length > 0) report.AppendLine(L10n.Text("comparison.reportAdded") + change.Added); }
        }
        return report.ToString();
    }
}

public static class ContentComparison
{
    public static PageComparison ComparePage(UglyToad.PdfPig.Content.Page? left, UglyToad.PdfPig.Content.Page? right, int number, CancellationToken token)
    {
        ComparedWord[] Extract(UglyToad.PdfPig.Content.Page? page) => page?.GetWords().SelectMany(w => Regex.Matches(w.Text.Normalize(), @"\S+").Select(m => new ComparedWord(m.Value, new ComparisonUnderline(w.BoundingBox.BottomLeft.X / page.Width, 1 - w.BoundingBox.BottomLeft.Y / page.Height, w.BoundingBox.BottomRight.X / page.Width, 1 - w.BoundingBox.BottomRight.Y / page.Height)))).ToArray() ?? [];
        var a = Extract(left); var b = Extract(right);
        if ((left != null && a.Length == 0) || (right != null && b.Length == 0)) return new(number, "unreadable", []);
        var changes = Diff(string.Join(" ", a.Select(w => w.Text)), string.Join(" ", b.Select(w => w.Text)), token);
        return new PageComparison(number, changes.Any(c => c.Kind != "equal") || left == null || right == null ? "changed" : "equal", changes) { OriginalWords = a, UpdatedWords = b };
    }
    public static IEnumerable<TextHunk> Hunks(TextChange[] changes)
    {
        for (var i = 0; i < changes.Length; i++)
        {
            if (changes[i].Kind == "equal") continue;
            var before = i > 0 ? string.Join(" ", changes[i - 1].Text.Split(' ').TakeLast(8)) : "";
            var removed = new List<string>(); var added = new List<string>();
            while (i < changes.Length && changes[i].Kind != "equal") { (changes[i].Kind == "removed" ? removed : added).Add(changes[i].Text); i++; }
            var after = i < changes.Length ? string.Join(" ", changes[i].Text.Split(' ').Take(8)) : "";
            yield return new(before, string.Join(" ", removed), string.Join(" ", added), after);
        }
    }
    public static ContentComparisonResult Run(string original, string updated, string password, CancellationToken token, IProgress<string>? progress = null)
    {
        ProcessingLimits.Input(original); ProcessingLimits.Input(updated);
        using var a = UglyToad.PdfPig.PdfDocument.Open(original, new UglyToad.PdfPig.ParsingOptions { Password = password });
        using var b = UglyToad.PdfPig.PdfDocument.Open(updated, new UglyToad.PdfPig.ParsingOptions { Password = password });
        if (a.NumberOfPages > ProcessingLimits.MaximumPages || b.NumberOfPages > ProcessingLimits.MaximumPages) throw new ArgumentException("El documento supera el límite de 10 000 páginas.");
        var pages = new List<PageComparison>();
        for (var number = 1; number <= Math.Max(a.NumberOfPages, b.NumberOfPages); number++)
        {
            token.ThrowIfCancellationRequested(); progress?.Report($"Comparando texto de la página {number}…");
            pages.Add(ComparePage(number <= a.NumberOfPages ? a.GetPage(number) : null, number <= b.NumberOfPages ? b.GetPage(number) : null, number, token));
        }
        return new(Path.GetFileName(original), Path.GetFileName(updated), pages.ToArray());
    }

    // Exact word LCS for normal pages. Large changed blocks use a bounded replacement
    // rather than allocating a quadratic matrix for arbitrarily large documents.
    public static TextChange[] Diff(string left, string right, CancellationToken token)
    {
        var a = Regex.Matches(left.Normalize(), @"\S+").Select(m => m.Value).ToArray();
        var b = Regex.Matches(right.Normalize(), @"\S+").Select(m => m.Value).ToArray();
        var prefix = 0; while (prefix < Math.Min(a.Length, b.Length) && a[prefix] == b[prefix]) { token.ThrowIfCancellationRequested(); prefix++; }
        var suffix = 0; while (suffix < Math.Min(a.Length, b.Length) - prefix && a[^(suffix + 1)] == b[^(suffix + 1)]) { token.ThrowIfCancellationRequested(); suffix++; }
        var n = a.Length - prefix - suffix; var m = b.Length - prefix - suffix;
        var parts = new List<(string Kind, string Word)>();
        for (var i = 0; i < prefix; i++) parts.Add(("equal", a[i]));
        if ((long)(n + 1) * (m + 1) > 2_000_000)
        {
            for (var i = prefix; i < a.Length - suffix; i++) parts.Add(("removed", a[i]));
            for (var i = prefix; i < b.Length - suffix; i++) parts.Add(("added", b[i]));
        }
        else
        {
            var lengths = new int[n + 1, m + 1];
            for (var i = n - 1; i >= 0; i--) { token.ThrowIfCancellationRequested(); for (var j = m - 1; j >= 0; j--) lengths[i, j] = a[prefix + i] == b[prefix + j] ? 1 + lengths[i + 1, j + 1] : Math.Max(lengths[i + 1, j], lengths[i, j + 1]); }
            var x = 0; var y = 0;
            while (x < n || y < m)
            {
                token.ThrowIfCancellationRequested();
                if (x < n && y < m && a[prefix + x] == b[prefix + y]) { parts.Add(("equal", a[prefix + x++])); y++; }
                else if (x < n && (y == m || lengths[x + 1, y] >= lengths[x, y + 1])) parts.Add(("removed", a[prefix + x++]));
                else parts.Add(("added", b[prefix + y++]));
            }
        }
        for (var i = a.Length - suffix; i < a.Length; i++) parts.Add(("equal", a[i]));
        var result = new List<TextChange>(); var group = new StringBuilder(); string? kind = null;
        foreach (var part in parts) { token.ThrowIfCancellationRequested(); if (kind != part.Kind) { if (kind != null) result.Add(new(kind, group.ToString())); group.Clear(); kind = part.Kind; } if (group.Length > 0) group.Append(' '); group.Append(part.Word); }
        if (kind != null) result.Add(new(kind, group.ToString()));
        return result.ToArray();
    }

    public static int Write(ContentComparisonResult result, string folder, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); File.WriteAllText(Path.Combine(folder, "comparacion.txt"), result.Report());
        token.ThrowIfCancellationRequested(); File.WriteAllText(Path.Combine(folder, "comparacion.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 2;
    }
}
