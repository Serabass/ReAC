using System.Globalization;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Reac.Importers.GtaModsVc;

public sealed class GtaModsImporterOptions
{
    public string? FixturePath { get; init; }
    public string Url { get; init; } = "https://gtamods.com/wiki/Memory_Addresses_%28VC%29";
    public bool Force { get; init; }
}

public static class GtaModsImporter
{
    private static readonly string[] Sections = ["CEntity", "CPhysical", "CPed", "CMatrix", "CVector"];

    public static async Task RunAsync(string projectRoot, GtaModsImporterOptions opt, CancellationToken ct)
    {
        string html;
        if (!string.IsNullOrEmpty(opt.FixturePath))
            html = await File.ReadAllTextAsync(opt.FixturePath, ct);
        else
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("reac/0.1");
            html = await client.GetStringAsync(opt.Url, ct);
        }

        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var typesDir = Path.Combine(projectRoot, "types");
        Directory.CreateDirectory(typesDir);

        foreach (var section in Sections)
        {
            var table = FindSectionTable(doc, section);
            if (table == null)
                continue;

            var (kind, parent, totalSize) = InferKindParentSize(section);
            var lines = ParseRows(table);
            var sb = new StringBuilder();
            sb.AppendLine($"{kind} {section}{(parent != null ? " : " + parent : "")} size 0x{totalSize:X} {{");
            sb.AppendLine("  module GTA.Core");
            sb.AppendLine($"  source \"{opt.Url}\"");
            sb.AppendLine($"  summary \"Imported from GTAMods Wiki ({section})\"");
            foreach (var row in lines)
            {
                var typ = MapWikiType(row.Type);
                sb.AppendLine($"  0x{row.Offset:X3} {row.Name} : {typ}");
            }

            sb.AppendLine("}");
            var path = Path.Combine(typesDir, section + ".re");
            if (File.Exists(path) && !opt.Force)
                throw new InvalidOperationException($"File exists (use --force): {path}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }

    private static (string kind, string? parent, int size) InferKindParentSize(string section) =>
        section switch
        {
            "CMatrix" => ("struct", null, 0x40),
            "CVector" => ("struct", null, 0x0C),
            "CEntity" => ("class", null, 0x64),
            "CPhysical" => ("class", "CEntity", 0x120),
            "CPed" => ("class", "CPhysical", 0x6D8),
            _ => ("class", null, 0x100)
        };

    private static string MapWikiType(string wikiType)
    {
        var t = wikiType.Trim();
        var tl = t.ToLowerInvariant();
        if (tl.Contains("pointer", StringComparison.Ordinal) && tl.Contains("cped", StringComparison.Ordinal))
            return "CPed*";
        if (tl.Contains("pointer", StringComparison.Ordinal) && tl.Contains("cvehicle", StringComparison.Ordinal))
            return "CVehicle*";
        if (tl.Contains("cwanted", StringComparison.OrdinalIgnoreCase))
            return "CWanted*";
        if (tl.Contains("cweapon", StringComparison.OrdinalIgnoreCase) && tl.Contains("[10]", StringComparison.Ordinal))
            return "CWeapon[10]";
        if (tl == "pointer" || (tl.Contains("pointer") && !tl.Contains('[')))
            return "pointer";
        if (tl is "dword" or "uint32")
            return "uint32";
        if (tl is "word" or "uint16")
            return "uint16";
        if (tl is "byte")
            return "byte";
        if (tl is "float")
            return "float";
        if (tl.Contains("cvector", StringComparison.OrdinalIgnoreCase))
            return "CVector";
        if (tl.Contains("cmatrix", StringComparison.OrdinalIgnoreCase))
            return "CMatrix";
        return t.Replace(' ', '_');
    }

    private sealed record Row(int Offset, string Name, string Type);

    private static IReadOnlyList<Row> ParseRows(IElement table)
    {
        var rows = new List<Row>();
        foreach (var tr in table.QuerySelectorAll("tr").Skip(1))
        {
            var cells = tr.QuerySelectorAll("th,td");
            if (cells.Length < 2)
                continue;
            var offText = cells[0].TextContent.Trim();
            var typeText = cells[1].TextContent.Trim();
            if (!TryParseOffset(offText, out var offset))
                continue;
            var name = "f_" + offset.ToString("X");
            rows.Add(new Row(offset, name, typeText));
        }

        return rows;
    }

    private static bool TryParseOffset(string text, out int offset)
    {
        offset = 0;
        text = text.Trim().Replace("0X", "0x", StringComparison.Ordinal);
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = text[2..];
            if (hex.Length == 0)
                return false;
            offset = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset);
    }

    private static IElement? FindSectionTable(IDocument doc, string section)
    {
        foreach (var span in doc.QuerySelectorAll("span.mw-headline"))
        {
            var id = span.Id ?? "";
            var tx = span.TextContent.Trim();
            if (!string.Equals(id, section, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tx, section, StringComparison.OrdinalIgnoreCase))
                continue;

            IElement? h = span;
            while (h != null && !h.LocalName.Equals("h2", StringComparison.OrdinalIgnoreCase))
                h = h.ParentElement;
            if (h == null)
                continue;
            var sib = h.NextElementSibling;
            while (sib != null)
            {
                if (sib.LocalName.Equals("table", StringComparison.OrdinalIgnoreCase) &&
                    sib.ClassList.Contains("wikitable"))
                    return sib;
                if (sib.LocalName.StartsWith('h') && sib.LocalName.Length == 2 && char.IsDigit(sib.LocalName[1]))
                    break;
                sib = sib.NextElementSibling;
            }
        }

        return null;
    }
}
