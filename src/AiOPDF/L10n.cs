using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiOPDF;

public sealed record LanguagePack(string Code, string Name, string Culture, Dictionary<string, string> Messages)
{
    public override string ToString() => Name;
}

/// <summary>Offline catalogs, stable message IDs, fallback and per-user language preference.</summary>
public static class L10n
{
    static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    static readonly Dictionary<string, LanguagePack> packs = LoadPacks();
    static readonly LanguagePack baseline = packs["es"];
    static readonly Dictionary<string, string> sourceKeys = baseline.Messages.GroupBy(x => x.Value).ToDictionary(g => g.Key, g => g.First().Key);
    static readonly (string Key, Regex Pattern)[] templates = baseline.Messages.Where(x => Regex.IsMatch(x.Value, @"\{\d+\}"))
        .Select(x => (x.Key, Pattern(x.Value))).OrderByDescending(x => baseline.Messages[x.Key].Length).ToArray();
    static string code = "es";
    static bool initialized;
    public static event Action? Changed;
    public static IReadOnlyList<LanguagePack> Languages => packs.Values.OrderBy(p => p.Code == "es" ? 0 : p.Code == "en" ? 1 : 2).ThenBy(p => p.Name).ToArray();
    public static string Code => code;
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(packs[code].Culture);
    public static string PreferencePath { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiOPDF", "settings.json");

    public static void Initialize(string? preferencePath = null)
    {
        if (initialized && preferencePath == null) return;
        initialized = true;
        if (preferencePath != null) PreferencePath = preferencePath;
        var selected = "es";
        try { if (File.Exists(PreferencePath)) selected = JsonSerializer.Deserialize<Preference>(File.ReadAllText(PreferencePath))?.Language ?? selected; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        SetLanguage(packs.ContainsKey(selected) ? selected : "es", false);
    }

    public static bool SetLanguage(string language, bool persist = true)
    {
        if (!packs.ContainsKey(language)) return false;
        if (persist)
        {
            // Preserve other preferences when a future version adds settings.
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
                Dictionary<string, JsonElement> settings = [];
                if (File.Exists(PreferencePath))
                    try { settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(PreferencePath)) ?? []; }
                    catch (JsonException) { } // A malformed preference must not prevent selecting a new language.
                settings["Language"] = JsonSerializer.SerializeToElement(language);
                var staging = PreferencePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try { File.WriteAllText(staging, JsonSerializer.Serialize(settings)); File.Move(staging, PreferencePath, true); }
                finally { if (File.Exists(staging)) File.Delete(staging); }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return false; }
        }
        if (code != language) { code = language; Changed?.Invoke(); }
        return true;
    }

    public static string Text(string key) => packs[code].Messages.GetValueOrDefault(key) ?? baseline.Messages.GetValueOrDefault(key) ?? key;
    public static string Format(string key, params object?[] args) => string.Format(Culture, Text(key), args);

    // Compatibility adapter for existing Spanish UI copy. New UI should use Text/Format IDs.
    // Translation applies to presentation only; paths, document contents and job values stay untouched.
    public static string Translate(string source)
    {
        if (code == "es" || source.Length == 0) return source;
        if (sourceKeys.TryGetValue(source, out var key)) return Text(key);
        foreach (var item in templates)
        {
            var match = item.Pattern.Match(source);
            if (!match.Success) continue;
            return Regex.Replace(Text(item.Key), @"\{(\d+)\}", m =>
            {
                var captured = match.Groups["v" + m.Groups[1].Value].Value;
                if (item.Key == "split.fullSummary" && m.Groups[1].Value == "1")
                    return string.Join(" / ", captured.Split(" / ").Select(Translate));
                return sourceKeys.TryGetValue(captured, out var capturedKey) ? Text(capturedKey) : captured;
            });
        }
        if (source.StartsWith("✓ ", StringComparison.Ordinal)) return "✓ " + Translate(source[2..]);
        // Some legacy statuses concatenate a prefix, backend error and recovery instruction.
        foreach (var entry in sourceKeys.OrderByDescending(x => x.Key.Length))
            if (entry.Key.Length > 5 && entry.Key.EndsWith(' ') && source.StartsWith(entry.Key, StringComparison.Ordinal))
                return Text(entry.Value) + Translate(source[entry.Key.Length..]);
        foreach (var entry in sourceKeys.Where(x => x.Key.StartsWith(' ')).OrderByDescending(x => x.Key.Length))
            if (source.EndsWith(entry.Key, StringComparison.Ordinal)) return Translate(source[..^entry.Key.Length]) + Text(entry.Value);
        return source;
    }

    static Regex Pattern(string text)
    {
        var escaped = Regex.Escape(text);
        escaped = Regex.Replace(escaped, @"\\\{(\d+)\}", m => "(?<v" + m.Groups[1].Value + ">.*?)");
        return new Regex("\\A" + escaped + "\\z", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    static Dictionary<string, LanguagePack> LoadPacks()
    {
        var result = new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(L10n).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.Contains(".Localization.") && n.EndsWith(".json")))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            var pack = JsonSerializer.Deserialize<LanguagePack>(stream, Options)!;
            Validate(pack); result.Add(pack.Code, pack);
        }
        // Drop a new JSON module here without modifying application code.
        var directory = Path.Combine(AppContext.BaseDirectory, "Localization");
        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    var pack = JsonSerializer.Deserialize<LanguagePack>(File.ReadAllText(file), Options)!;
                    Validate(pack);
                    if (pack.Messages.Any(entry => result["es"].Messages.TryGetValue(entry.Key, out var source) && Slots(source) != Slots(entry.Value))) continue;
                    if (!result.ContainsKey(pack.Code)) result.Add(pack.Code, pack);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { }
        var spanish = result["es"];
        foreach (var pack in result.Values)
            foreach (var entry in pack.Messages)
                if (spanish.Messages.TryGetValue(entry.Key, out var source) && Slots(source) != Slots(entry.Value))
                    throw new InvalidDataException($"Invalid placeholders: {pack.Code}/{entry.Key}");
        return result;
    }

    public static string Slots(string text) => string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Order());
    static void Validate(LanguagePack pack)
    {
        if (pack == null || !Regex.IsMatch(pack.Code ?? "", @"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$") || string.IsNullOrWhiteSpace(pack.Name) || pack.Messages == null)
            throw new ArgumentException("Invalid language module.");
        if (string.IsNullOrWhiteSpace(pack.Culture) || pack.Messages.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value == null || Regex.IsMatch(Regex.Replace(x.Value, @"\{\d+\}", ""), "[{}]"))) throw new ArgumentException("Invalid language messages.");
        _ = CultureInfo.GetCultureInfo(pack.Culture);
    }
    sealed record Preference(string Language);
}
