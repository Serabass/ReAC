using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Reac.Ir;
using Sprache;

namespace Reac.Dsl;

/// <summary>Parses .re files; Sprache used in <see cref="ReLineParsers"/> for field lines.</summary>
public static class ReDocumentParser
{
    private static readonly Regex FieldLineRegex = new(
        @"^(0x[0-9a-fA-F]+)\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<ReTopLevel> ParseDocument(string text)
    {
        var i = 0;
        StringLiterals.SkipNoise(text, ref i);
        var list = new List<ReTopLevel>();
        while (i < text.Length)
        {
            StringLiterals.SkipNoise(text, ref i);
            if (i >= text.Length)
                break;

            var kw = PeekKeyword(text, i);
            if (kw == "target")
                list.Add(ParseTarget(text, ref i));
            else if (kw == "module")
                list.Add(ParseModule(text, ref i));
            else if (kw == "class")
                list.Add(ParseType(text, ref i, TypeKind.Class));
            else if (kw == "struct")
                list.Add(ParseType(text, ref i, TypeKind.Struct));
            else if (kw == "bitfield")
                list.Add(ParseBitfieldTopLevel(text, ref i));
            else
                throw new ParseException($"Unexpected at {i}: expected target, module, class, struct, bitfield");
        }

        return list;
    }

    private static string PeekKeyword(string s, int i)
    {
        var j = i;
        StringLiterals.SkipNoise(s, ref j);
        var sb = new StringBuilder();
        while (j < s.Length && char.IsLetter(s[j]))
        {
            sb.Append(s[j]);
            j++;
        }

        return sb.ToString();
    }

    private static void ExpectKeyword(string text, ref int i, string keyword)
    {
        StringLiterals.SkipNoise(text, ref i);
        for (var k = 0; k < keyword.Length; k++)
        {
            if (i + k >= text.Length || char.ToLowerInvariant(text[i + k]) != keyword[k])
                throw new ParseException($"Expected '{keyword}' at {i}");
        }

        i += keyword.Length;
        if (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
            throw new ParseException($"Expected word boundary after '{keyword}'");
    }

    private static string ParseIdent(string text, ref int i)
    {
        StringLiterals.SkipNoise(text, ref i);
        if (i >= text.Length || !(char.IsLetter(text[i]) || text[i] == '_'))
            throw new ParseException("Expected identifier");
        var start = i;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.'))
            i++;
        return text.Substring(start, i - start);
    }

    private static int ParseHexOrDecimal(string text, ref int i)
    {
        StringLiterals.SkipNoise(text, ref i);
        if (i + 1 < text.Length && text[i] == '0' && (text[i + 1] == 'x' || text[i + 1] == 'X'))
        {
            i += 2;
            var start = i;
            while (i < text.Length && Uri.IsHexDigit(text[i]))
                i++;
            return int.Parse(text.AsSpan(start, i - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        var sb = new StringBuilder();
        while (i < text.Length && char.IsDigit(text[i]))
        {
            sb.Append(text[i]);
            i++;
        }

        return int.Parse(sb.ToString(), CultureInfo.InvariantCulture);
    }

    private static ReTopLevel ParseTarget(string text, ref int i)
    {
        ExpectKeyword(text, ref i, "target");
        var id = ParseIdent(text, ref i);
        var body = ParseBlock(text, ref i);
        var (d, sourceUrls) = ParseTargetKeyValues(body);
        var ps = 4;
        if (d.TryGetValue("pointer_size_bytes", out var psv) && int.TryParse(psv, out var p))
            ps = p;
        d.TryGetValue("game", out var game);
        d.TryGetValue("version", out var version);
        d.TryGetValue("platform", out var platform);
        return new ReTopLevel.Target(id, ps, game, version, platform, sourceUrls);
    }

    private static (Dictionary<string, string> Values, List<string> SourceUrls) ParseTargetKeyValues(string body)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceUrls = new List<string>();
        var i = 0;
        while (i < body.Length)
        {
            StringLiterals.SkipNoise(body, ref i);
            if (i >= body.Length)
                break;
            var key = ReadWord(body, ref i);
            StringLiterals.SkipNoise(body, ref i);
            if (key.Equals("pointer_size_bytes", StringComparison.OrdinalIgnoreCase))
            {
                var n = ParseHexOrDecimal(body, ref i);
                d["pointer_size_bytes"] = n.ToString(CultureInfo.InvariantCulture);
                continue;
            }

            if (!StringLiterals.TryParse(body, ref i, out var str))
                throw new ParseException($"Expected string value for '{key}' in target");

            if (key.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                sourceUrls.Add(str);
                continue;
            }

            d[key] = str;
        }

        return (d, sourceUrls);
    }

    private static string ReadWord(string s, ref int i)
    {
        var start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_'))
            i++;
        return s.Substring(start, i - start);
    }

    private static ReTopLevel ParseModule(string text, ref int i)
    {
        ExpectKeyword(text, ref i, "module");
        var name = ParseIdent(text, ref i);
        var body = ParseBlock(text, ref i);
        var lines = ParseTypeBodyLines(body);
        string? summary = null, note = null;
        foreach (var l in lines)
        {
            if (l is ReBodyLine.SummaryLine s)
                summary = s.Text;
            if (l is ReBodyLine.NoteEntityLine n)
                note = n.Text;
        }

        return new ReTopLevel.Module(name, summary, note, lines);
    }

    private static ReTopLevel ParseType(string text, ref int i, TypeKind kind)
    {
        ExpectKeyword(text, ref i, kind == TypeKind.Class ? "class" : "struct");
        var name = ParseIdent(text, ref i);
        StringLiterals.SkipNoise(text, ref i);
        string? parent = null;
        if (i < text.Length && text[i] == ':')
        {
            i++;
            parent = ParseIdent(text, ref i);
        }

        StringLiterals.SkipNoise(text, ref i);
        int? declaredSize = null;
        if (i < text.Length && char.IsLetter(text[i]))
        {
            var maybeSize = PeekKeyword(text, i);
            if (string.Equals(maybeSize, "size", StringComparison.OrdinalIgnoreCase))
            {
                ExpectKeyword(text, ref i, "size");
                declaredSize = ParseHexOrDecimal(text, ref i);
            }
        }

        var body = ParseBlock(text, ref i);
        var lines = ParseTypeBodyLines(body);
        return new ReTopLevel.TypeDef(kind, name, parent, declaredSize, lines);
    }

    private static ReTopLevel ParseBitfieldTopLevel(string text, ref int i)
    {
        ExpectKeyword(text, ref i, "bitfield");
        var name = ParseIdent(text, ref i);
        StringLiterals.SkipNoise(text, ref i);
        if (i >= text.Length || text[i] != ':')
            throw new ParseException("bitfield: expected ':' after name");
        i++;
        var storage = ParseIdent(text, ref i);
        var body = ParseBlock(text, ref i);
        var (bits, sources, summary, note) = ParseBitfieldInnerLines(body);
        return new ReTopLevel.BitfieldDef(name, storage, bits, sources, summary, note);
    }

    private static (IReadOnlyList<(int Bit, string Name)> Bits, IReadOnlyList<string> SourceUrls, string? Summary,
        string? Note) ParseBitfieldInnerLines(string body)
    {
        var bits = new List<(int Bit, string Name)>();
        var sources = new List<string>();
        string? summary = null, note = null;
        var seen = new HashSet<int>();
        var lineStart = 0;
        while (lineStart < body.Length)
        {
            StringLiterals.SkipNoise(body, ref lineStart);
            if (lineStart >= body.Length)
                break;

            var lineBegin = lineStart;
            var lineEnd = lineStart;
            while (lineEnd < body.Length && body[lineEnd] != '\n' && body[lineEnd] != '\r')
                lineEnd++;
            var line = body.Substring(lineBegin, lineEnd - lineBegin).Trim();
            var ni = lineEnd;
            if (ni < body.Length && body[ni] == '\r') ni++;
            if (ni < body.Length && body[ni] == '\n') ni++;
            lineStart = ni;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            var fw = FirstWord(line);
            if (fw.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                var r = line.AsSpan(6).Trim();
                var si = 0;
                var s = r.ToString();
                if (!StringLiterals.TryParse(s, ref si, out var url))
                    throw new ParseException("bitfield source: bad string");
                sources.Add(url);
                continue;
            }

            if (fw.Equals("summary", StringComparison.OrdinalIgnoreCase))
            {
                var s = line.Substring(7).Trim();
                var si = 0;
                if (!StringLiterals.TryParse(s, ref si, out var sm))
                    throw new ParseException("bitfield summary: bad string");
                summary = sm;
                continue;
            }

            if (fw.Equals("note", StringComparison.OrdinalIgnoreCase))
            {
                var rest = line.Substring(4).Trim();
                var si = 0;
                if (!StringLiterals.TryParse(rest, ref si, out var nt))
                    throw new ParseException("bitfield note: bad string");
                note = nt;
                continue;
            }

            var m = Regex.Match(line, @"^(\d+)\s+([A-Za-z_][A-Za-z0-9_]*)$");
            if (!m.Success)
                throw new ParseException($"bitfield: expected source/summary/note or 'N name', got: {line}");
            var bit = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var bitName = m.Groups[2].Value;
            if (!seen.Add(bit))
                throw new ParseException($"bitfield: duplicate bit index {bit}");
            bits.Add((bit, bitName));
        }

        if (bits.Count == 0)
            throw new ParseException("bitfield: at least one bit line required");

        return (bits, sources, summary, note);
    }

    private static string ParseBlock(string text, ref int i)
    {
        StringLiterals.SkipNoise(text, ref i);
        if (i >= text.Length || text[i] != '{')
            throw new ParseException("Expected {");
        i++;
        var depth = 1;
        var start = i;
        while (i < text.Length && depth > 0)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;
            if (depth == 0)
                break;
            i++;
        }

        if (depth != 0)
            throw new ParseException("Unclosed {");
        var inner = text.Substring(start, i - start);
        i++;
        return inner;
    }

    private static List<ReBodyLine> ParseTypeBodyLines(string body)
    {
        var lines = new List<ReBodyLine>();
        var lineStart = 0;
        while (lineStart < body.Length)
        {
            StringLiterals.SkipNoise(body, ref lineStart);
            if (lineStart >= body.Length)
                break;

            var lineBegin = lineStart;
            var lineEnd = lineStart;
            while (lineEnd < body.Length && body[lineEnd] != '\n' && body[lineEnd] != '\r')
                lineEnd++;
            var line = body.Substring(lineBegin, lineEnd - lineBegin).Trim();
            var ni = lineEnd;
            if (ni < body.Length && body[ni] == '\r') ni++;
            if (ni < body.Length && body[ni] == '\n') ni++;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
            {
                lineStart = ni;
                continue;
            }

            lineStart = ni;

            try
            {
                var parsed = ReLineParsers.FieldLine.End().Parse(line);
                lines.Add(new ReBodyLine.FieldLine(parsed.Offset, parsed.Name, parsed.Type, null));
                continue;
            }
            catch (Exception)
            {
                if (TryParseFieldLineRegex(line, out var off, out var nm, out var typ))
                {
                    lines.Add(new ReBodyLine.FieldLine(off, nm, TypeExprParser.Parse(StripLineComment(typ)), null));
                    continue;
                }
            }

            if (TryParseFunctionLine(line, out var fnLine) && fnLine != null)
            {
                lines.Add(fnLine);
                continue;
            }

            var fw = FirstWord(line);
            if (fw.Equals("module", StringComparison.OrdinalIgnoreCase))
            {
                var rest = line.Substring(6).Trim();
                lines.Add(new ReBodyLine.ModuleLine(rest));
                continue;
            }

            if (fw.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                var r = line.AsSpan(6).Trim();
                var si = 0;
                var s = r.ToString();
                if (!StringLiterals.TryParse(s, ref si, out var url))
                    throw new ParseException("source: bad string");
                lines.Add(new ReBodyLine.SourceLine(url));
                continue;
            }

            if (fw.Equals("summary", StringComparison.OrdinalIgnoreCase))
            {
                var s = line.Substring(7).Trim();
                var si = 0;
                if (!StringLiterals.TryParse(s, ref si, out var sm))
                    throw new ParseException("summary: bad string");
                lines.Add(new ReBodyLine.SummaryLine(sm));
                continue;
            }

            if (fw.Equals("note", StringComparison.OrdinalIgnoreCase))
            {
                var rest = line.Substring(4).Trim();
                if (rest.Length >= 2 && rest.StartsWith("fn", StringComparison.OrdinalIgnoreCase) &&
                    (rest.Length == 2 || char.IsWhiteSpace(rest[2])))
                {
                    var afterFnKw = rest.Substring(2).TrimStart();
                    var qPos = afterFnKw.IndexOf('"');
                    if (qPos < 0)
                        throw new ParseException("note fn: expected string literal");
                    var fnName = afterFnKw[..qPos].TrimEnd();
                    if (fnName.Length == 0)
                        throw new ParseException("note fn: missing function name");
                    var litPart = afterFnKw[qPos..];
                    var li = 0;
                    if (!StringLiterals.TryParse(litPart, ref li, out var fnNoteText))
                        throw new ParseException("note fn: bad string");
                    lines.Add(new ReBodyLine.NoteFunctionLine(fnName, fnNoteText));
                    continue;
                }

                var si = 0;
                if (rest.Length > 0 && (char.IsLetter(rest[0]) || rest[0] == '_'))
                {
                    var w = FirstWord(rest);
                    var after = rest.Substring(w.Length).Trim();
                    if (after.Length > 0 && (after[0] == '"' || after.StartsWith("\"\"\"", StringComparison.Ordinal)))
                    {
                        var ai = 0;
                        if (!StringLiterals.TryParse(after, ref ai, out var nt))
                            throw new ParseException("note field: bad string");
                        lines.Add(new ReBodyLine.NoteFieldLine(w, nt));
                        continue;
                    }
                }

                si = 0;
                if (!StringLiterals.TryParse(rest, ref si, out var nte))
                    throw new ParseException("note: bad string");
                lines.Add(new ReBodyLine.NoteEntityLine(nte));
                continue;
            }

            throw new ParseException($"Unrecognized body line: {line}");
        }

        return lines;
    }

    /// <summary>Parses <c>fn 0xADDR Name(params) [: ReturnType]</c> and optional trailing <c>// note</c>.</summary>
    private static bool TryParseFunctionLine(string line, out ReBodyLine.FunctionLine? fl)
    {
        fl = null;
        var work = line.Trim();
        string? inlineNote = null;
        var cmt = work.IndexOf("//", StringComparison.Ordinal);
        if (cmt >= 0)
        {
            inlineNote = work[(cmt + 2)..].Trim();
            work = work[..cmt].Trim();
        }

        if (work.Length < 4 || !work.StartsWith("fn", StringComparison.OrdinalIgnoreCase) ||
            !char.IsWhiteSpace(work[2]))
            return false;

        var i = 3;
        while (i < work.Length && char.IsWhiteSpace(work[i])) i++;

        if (i + 1 >= work.Length || work[i] != '0' || (work[i + 1] != 'x' && work[i + 1] != 'X'))
            return false;
        i += 2;
        var hs = i;
        while (i < work.Length && Uri.IsHexDigit(work[i])) i++;
        if (hs == i)
            return false;
        var address = int.Parse(work.AsSpan(hs, i - hs), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        while (i < work.Length && char.IsWhiteSpace(work[i])) i++;
        var openP = work.IndexOf('(', i);
        if (openP < 0)
            return false;
        var name = work.Substring(i, openP - i).Trim();
        if (name.Length == 0)
            return false;

        var closeP = work.LastIndexOf(')');
        if (closeP <= openP)
            return false;
        var parameters = work.Substring(openP + 1, closeP - openP - 1).Trim();

        var after = work.Substring(closeP + 1).Trim();
        string? retType = null;
        if (after.Length > 0)
        {
            if (!after.StartsWith(':'))
                return false;
            retType = after[1..].Trim();
            if (retType.Length == 0)
                retType = null;
        }

        fl = new ReBodyLine.FunctionLine(address, name, parameters, retType, inlineNote);
        return true;
    }

    private static bool TryParseFieldLineRegex(string line, out int offset, out string name, out string typeRest)
    {
        offset = 0;
        name = "";
        typeRest = "";
        var m = FieldLineRegex.Match(line.Trim());
        if (!m.Success)
            return false;
        offset = int.Parse(m.Groups[1].Value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        name = m.Groups[2].Value;
        typeRest = m.Groups[3].Value.Trim();
        return true;
    }

    private static string StripLineComment(string s)
    {
        var i = s.IndexOf("//", StringComparison.Ordinal);
        if (i >= 0)
            s = s[..i];
        return s.Trim();
    }

    private static string FirstWord(string line)
    {
        var i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        var j = i;
        while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_'))
            j++;
        return line.Substring(i, j - i);
    }
}
