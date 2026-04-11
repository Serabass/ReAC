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
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  private static readonly Regex StaticFieldLineRegex = new(
    @"^static\s+(0x[0-9a-fA-F]+)\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*(.+)$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
  );

  private static readonly Regex DecoratorLineRegex = new(
    @"^\s*@([A-Za-z_][A-Za-z0-9_]*)\s*$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  private static readonly Regex InlineBitfieldHeadRegex = new(
    @"^0x([0-9a-fA-F]+)\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*bitfield\s*:\s*([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
  );

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

      var typePrefix = new List<ReBodyLine>();
      while (TryConsumeLeadingTypeDecoratorLine(text, ref i, out var prefixLine))
        typePrefix.Add(prefixLine);

      StringLiterals.SkipNoise(text, ref i);
      if (i >= text.Length)
        break;

      var kw = PeekKeyword(text, i);
      if (kw == "target")
        list.Add(ParseTarget(text, ref i));
      else if (kw == "module")
        list.Add(ParseModule(text, ref i));
      else if (kw == "class")
        list.Add(ParseType(text, ref i, TypeKind.Class, typePrefix));
      else if (kw == "struct")
        list.Add(ParseType(text, ref i, TypeKind.Struct, typePrefix));
      else if (kw == "bitfield")
        list.Add(ParseBitfieldTopLevel(text, ref i));
      else if (kw == "enum")
        list.Add(ParseEnumTopLevel(text, ref i));
      else
        throw new ParseException(
          $"Unexpected at {i}: expected target, module, class, struct, bitfield, enum"
        );
    }

    return list;
  }

  /// <summary>Reads one line of <c>@source</c>/<c>@summary</c>/<c>@note("...")</c> before <c>class</c>/<c>struct</c>.</summary>
  private static bool TryConsumeLeadingTypeDecoratorLine(string text, ref int i, out ReBodyLine line)
  {
    line = null!;
    var saved = i;
    StringLiterals.SkipNoise(text, ref i);
    if (i >= text.Length || text[i] != '@')
    {
      i = saved;
      return false;
    }

    var lineBegin = i;
    var lineEnd = lineBegin;
    while (lineEnd < text.Length && text[lineEnd] != '\n' && text[lineEnd] != '\r')
      lineEnd++;
    var trimmed = text.Substring(lineBegin, lineEnd - lineBegin).Trim();
    var ni = lineEnd;
    if (ni < text.Length && text[ni] == '\r')
      ni++;
    if (ni < text.Length && text[ni] == '\n')
      ni++;

    if (!TryParseLeadingTypeMetadataLine(trimmed, out line))
    {
      i = saved;
      return false;
    }

    i = ni;
    return true;
  }

  /// <summary>Only metadata decorators valid before <c>class</c>/<c>struct</c> (not bare <c>@stdcall</c>).</summary>
  private static bool TryParseLeadingTypeMetadataLine(string trimmed, out ReBodyLine line)
  {
    line = null!;
    if (trimmed.Length < 2 || trimmed[0] != '@')
      return false;
    var j = 1;
    if (j >= trimmed.Length || !(char.IsLetter(trimmed[j]) || trimmed[j] == '_'))
      return false;
    var nameStart = j;
    while (j < trimmed.Length && (char.IsLetterOrDigit(trimmed[j]) || trimmed[j] == '_'))
      j++;
    var decName = trimmed.Substring(nameStart, j - nameStart);
    StringLiterals.SkipNoise(trimmed, ref j);
    if (j >= trimmed.Length || trimmed[j] != '(')
      return false;
    j++;
    StringLiterals.SkipNoise(trimmed, ref j);

    if (decName.Equals("source", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(trimmed, ref j, out var url))
        throw new ParseException("leading @source: bad string");
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j >= trimmed.Length || trimmed[j] != ')')
        throw new ParseException("leading @source: expected ')'");
      j++;
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j < trimmed.Length)
        throw new ParseException("leading @source: trailing content");
      line = new ReBodyLine.SourceLine(url);
      return true;
    }

    if (decName.Equals("summary", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(trimmed, ref j, out var sm))
        throw new ParseException("leading @summary: bad string");
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j >= trimmed.Length || trimmed[j] != ')')
        throw new ParseException("leading @summary: expected ')'");
      j++;
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j < trimmed.Length)
        throw new ParseException("leading @summary: trailing content");
      line = new ReBodyLine.SummaryLine(sm);
      return true;
    }

    if (decName.Equals("note", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(trimmed, ref j, out var nt))
        throw new ParseException("leading @note: bad string");
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j >= trimmed.Length || trimmed[j] != ')')
        throw new ParseException("leading @note: expected ')'");
      j++;
      StringLiterals.SkipNoise(trimmed, ref j);
      if (j < trimmed.Length)
        throw new ParseException("leading @note: trailing content");
      line = new ReBodyLine.NoteEntityLine(nt);
      return true;
    }

    return false;
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
      return int.Parse(
        text.AsSpan(start, i - start),
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture
      );
    }

    var sb = new StringBuilder();
    while (i < text.Length && char.IsDigit(text[i]))
    {
      sb.Append(text[i]);
      i++;
    }

    return int.Parse(sb.ToString(), CultureInfo.InvariantCulture);
  }

  /// <summary>Unsigned decimal or hex literal for enum values.</summary>
  private static ulong ParseULong(string text, ref int i)
  {
    StringLiterals.SkipNoise(text, ref i);
    if (i + 1 < text.Length && text[i] == '0' && (text[i + 1] == 'x' || text[i + 1] == 'X'))
    {
      i += 2;
      var start = i;
      while (i < text.Length && Uri.IsHexDigit(text[i]))
        i++;
      if (start == i)
        throw new ParseException("enum: expected hex digits after 0x");
      return ulong.Parse(
        text.AsSpan(start, i - start),
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture
      );
    }

    var sb = new StringBuilder();
    while (i < text.Length && char.IsDigit(text[i]))
    {
      sb.Append(text[i]);
      i++;
    }

    if (sb.Length == 0)
      throw new ParseException("enum: expected numeric value");
    return ulong.Parse(sb.ToString(), CultureInfo.InvariantCulture);
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

  private static (Dictionary<string, string> Values, List<string> SourceUrls) ParseTargetKeyValues(
    string body
  )
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
    string? summary = null,
      note = null;
    foreach (var l in lines)
    {
      if (l is ReBodyLine.SummaryLine s)
        summary = s.Text;
      if (l is ReBodyLine.NoteEntityLine n)
        note = n.Text;
    }

    return new ReTopLevel.Module(name, summary, note, lines);
  }

  private static ReTopLevel ParseType(
    string text,
    ref int i,
    TypeKind kind,
    IReadOnlyList<ReBodyLine>? leadingDecorators = null
  )
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
    if (leadingDecorators is { Count: > 0 })
      lines = leadingDecorators.Concat(lines).ToList();
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

  private static (
    IReadOnlyList<(int Bit, string Name, string? Description)> Bits,
    IReadOnlyList<string> SourceUrls,
    string? Summary,
    string? Note
  ) ParseBitfieldInnerLines(string body)
  {
    var bits = new List<(int Bit, string Name, string?)>();
    var sources = new List<string>();
    string? summary = null,
      note = null;
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
      if (ni < body.Length && body[ni] == '\r')
        ni++;
      if (ni < body.Length && body[ni] == '\n')
        ni++;
      lineStart = ni;

      if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
        continue;

      var bitfieldLine = line.Trim();
      if (bitfieldLine.Length > 0 && bitfieldLine[0] == '@')
      {
        if (!TryParseAtDecoratorInBody(bitfieldLine, out var meta, out var bare))
          throw new ParseException($"bitfield: bad decorator: {bitfieldLine}");
        if (bare != null)
          throw new ParseException($"bitfield: bare @{bare} not allowed in block");
        if (meta is ReBodyLine.SourceLine sl)
        {
          sources.Add(sl.Url);
          continue;
        }

        if (meta is ReBodyLine.SummaryLine sm)
        {
          summary = sm.Text;
          continue;
        }

        if (meta is ReBodyLine.NoteEntityLine ne)
        {
          note = ne.Text;
          continue;
        }

        if (meta is ReBodyLine.ExePathLine or ReBodyLine.Sha256ExpectedLine)
          throw new ParseException("bitfield: @exe and @sha256 are not allowed in block");

        throw new ParseException("bitfield: only @source, @summary, @note allowed in block");
      }

      var li = 0;
      var bitNum = ParseULong(line, ref li);
      if (bitNum > int.MaxValue)
        throw new ParseException($"bitfield: bit index too large: {line}");
      var bit = (int)bitNum;
      StringLiterals.SkipNoise(line, ref li);
      var bitName = ParseIdent(line, ref li);
      StringLiterals.SkipNoise(line, ref li);
      string? bitDesc = null;
      if (li < line.Length)
      {
        if (!StringLiterals.TryParse(line, ref li, out bitDesc))
          throw new ParseException(
            $"bitfield: expected optional quoted description after '{bitName}', got: {line}"
          );
        StringLiterals.SkipNoise(line, ref li);
        if (li < line.Length)
          throw new ParseException($"bitfield: trailing content after bit line: {line}");
      }

      if (!seen.Add(bit))
        throw new ParseException($"bitfield: duplicate bit index {bit}");
      bits.Add((bit, bitName, bitDesc));
    }

    if (bits.Count == 0)
      throw new ParseException("bitfield: at least one bit line required");

    return (bits, sources, summary, note);
  }

  private static ReTopLevel ParseEnumTopLevel(string text, ref int i)
  {
    ExpectKeyword(text, ref i, "enum");
    var name = ParseIdent(text, ref i);
    StringLiterals.SkipNoise(text, ref i);
    if (i >= text.Length || text[i] != ':')
      throw new ParseException("enum: expected ':' after name");
    i++;
    var storage = ParseIdent(text, ref i);
    var body = ParseBlock(text, ref i);
    var (values, sources, summary, note) = ParseEnumInnerLines(body);
    return new ReTopLevel.EnumDef(name, storage, values, sources, summary, note);
  }

  private static (
    IReadOnlyList<(ulong Value, string Name, string? Description)> Values,
    IReadOnlyList<string> SourceUrls,
    string? Summary,
    string? Note
  ) ParseEnumInnerLines(string body)
  {
    var values = new List<(ulong Value, string Name, string? Description)>();
    var sources = new List<string>();
    string? summary = null,
      note = null;
    var seenVals = new HashSet<ulong>();
    var seenNames = new HashSet<string>(StringComparer.Ordinal);
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
      if (ni < body.Length && body[ni] == '\r')
        ni++;
      if (ni < body.Length && body[ni] == '\n')
        ni++;
      lineStart = ni;

      if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
        continue;

      var enumLine = line.Trim();
      if (enumLine.Length > 0 && enumLine[0] == '@')
      {
        if (!TryParseAtDecoratorInBody(enumLine, out var meta, out var bare))
          throw new ParseException($"enum: bad decorator: {enumLine}");
        if (bare != null)
          throw new ParseException($"enum: bare @{bare} not allowed in block");
        if (meta is ReBodyLine.SourceLine sl)
        {
          sources.Add(sl.Url);
          continue;
        }

        if (meta is ReBodyLine.SummaryLine sm)
        {
          summary = sm.Text;
          continue;
        }

        if (meta is ReBodyLine.NoteEntityLine ne)
        {
          note = ne.Text;
          continue;
        }

        if (meta is ReBodyLine.ExePathLine or ReBodyLine.Sha256ExpectedLine)
          throw new ParseException("enum: @exe and @sha256 are not allowed in block");

        throw new ParseException("enum: only @source, @summary, @note allowed in block");
      }

      var li = 0;
      var val = ParseULong(line, ref li);
      StringLiterals.SkipNoise(line, ref li);
      var memberName = ParseIdent(line, ref li);
      StringLiterals.SkipNoise(line, ref li);
      string? desc = null;
      if (li < line.Length)
      {
        if (!StringLiterals.TryParse(line, ref li, out desc))
          throw new ParseException(
            $"enum: expected optional string description after '{memberName}', got: {line}"
          );
        StringLiterals.SkipNoise(line, ref li);
        if (li < line.Length)
          throw new ParseException($"enum: trailing content after description: {line}");
      }

      if (!seenVals.Add(val))
        throw new ParseException($"enum: duplicate value {val}");
      if (!seenNames.Add(memberName))
        throw new ParseException($"enum: duplicate member name '{memberName}'");

      values.Add((val, memberName, desc));
    }

    if (values.Count == 0)
      throw new ParseException("enum: at least one enumerator line required");

    return (values, sources, summary, note);
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
      if (text[i] == '{')
        depth++;
      else if (text[i] == '}')
        depth--;
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

  /// <summary>Finds <c>//</c> starting a line comment; ignores <c>://</c> in URLs and <c>//</c> inside strings (caller trims outside strings).</summary>
  private static int IndexOfDoubleSlashLineComment(string line)
  {
    for (var k = 0; k + 1 < line.Length; k++)
    {
      if (line[k] != '/' || line[k + 1] != '/')
        continue;
      if (k >= 1 && line[k - 1] == ':')
        continue;
      if (k == 0 || char.IsWhiteSpace(line[k - 1]))
        return k;
    }

    return -1;
  }

  /// <summary>Merges <c>"quoted"</c> field note with optional trailing <c>// slash</c> note.</summary>
  private static string? MergeFieldNotes(string? quotedNote, string? slashNote)
  {
    if (quotedNote == null && slashNote == null)
      return null;
    if (quotedNote == null)
      return slashNote;
    if (slashNote == null)
      return quotedNote;
    return $"{quotedNote} {slashNote}";
  }

  private static bool TryPeekNextWorkLine(string body, int fromIndex, out string workLine, out int afterLine)
  {
    var i = fromIndex;
    while (true)
    {
      StringLiterals.SkipNoise(body, ref i);
      if (i >= body.Length)
      {
        workLine = "";
        afterLine = i;
        return false;
      }

      var lineBegin = i;
      var lineEnd = i;
      while (lineEnd < body.Length && body[lineEnd] != '\n' && body[lineEnd] != '\r')
        lineEnd++;
      var raw = body.Substring(lineBegin, lineEnd - lineBegin).Trim();
      var ni = lineEnd;
      if (ni < body.Length && body[ni] == '\r')
        ni++;
      if (ni < body.Length && body[ni] == '\n')
        ni++;
      afterLine = ni;
      if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("//", StringComparison.Ordinal))
      {
        i = ni;
        continue;
      }

      var cmt = IndexOfDoubleSlashLineComment(raw);
      workLine = cmt >= 0 ? raw[..cmt].Trim() : raw;
      return true;
    }
  }

  private static bool NextLineLooksLikeNativeFunction(string body, int afterCurrentLine)
  {
    var pos = afterCurrentLine;
    while (TryPeekNextWorkLine(body, pos, out var wl, out var nextPos))
    {
      var fw = FirstWord(wl);
      if (fw.Equals("module", StringComparison.OrdinalIgnoreCase))
      {
        pos = nextPos;
        continue;
      }

      return TryParseFunctionLine(wl, Array.Empty<string>(), null, out var fn) && fn != null;
    }

    return false;
  }

  /// <summary>Parses <c>@name</c> or <c>@name(...)</c> in type/module bodies (not leading prefix).</summary>
  private static bool TryParseAtDecoratorInBody(
    string workLine,
    out ReBodyLine? meta,
    out string? bareDecoratorName
  )
  {
    meta = null;
    bareDecoratorName = null;
    if (string.IsNullOrWhiteSpace(workLine) || workLine[0] != '@')
      return false;

    if (DecoratorLineRegex.IsMatch(workLine))
    {
      bareDecoratorName = DecoratorLineRegex.Match(workLine).Groups[1].Value;
      return true;
    }

    var w = workLine;
    var j = 1;
    if (j >= w.Length || !(char.IsLetter(w[j]) || w[j] == '_'))
      return false;
    var nameStart = j;
    while (j < w.Length && (char.IsLetterOrDigit(w[j]) || w[j] == '_'))
      j++;
    var decName = w.Substring(nameStart, j - nameStart);
    StringLiterals.SkipNoise(w, ref j);
    if (j >= w.Length || w[j] != '(')
      throw new ParseException($"@{decName}: expected '(' or bare @name");

    j++;
    StringLiterals.SkipNoise(w, ref j);

    if (decName.Equals("source", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(w, ref j, out var url))
        throw new ParseException("@source: bad string");
      StringLiterals.SkipNoise(w, ref j);
      if (j >= w.Length || w[j] != ')')
        throw new ParseException("@source: expected ')'");
      j++;
      StringLiterals.SkipNoise(w, ref j);
      if (j < w.Length)
        throw new ParseException("@source: trailing content");
      meta = new ReBodyLine.SourceLine(url);
      return true;
    }

    if (decName.Equals("summary", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(w, ref j, out var sm))
        throw new ParseException("@summary: bad string");
      StringLiterals.SkipNoise(w, ref j);
      if (j >= w.Length || w[j] != ')')
        throw new ParseException("@summary: expected ')'");
      j++;
      StringLiterals.SkipNoise(w, ref j);
      if (j < w.Length)
        throw new ParseException("@summary: trailing content");
      meta = new ReBodyLine.SummaryLine(sm);
      return true;
    }

    if (decName.Equals("note", StringComparison.OrdinalIgnoreCase))
    {
      if (j < w.Length && w[j] == '"')
      {
        if (!StringLiterals.TryParse(w, ref j, out var nt))
          throw new ParseException("@note: bad string");
        StringLiterals.SkipNoise(w, ref j);
        if (j >= w.Length || w[j] != ')')
          throw new ParseException("@note: expected ')'");
        j++;
        StringLiterals.SkipNoise(w, ref j);
        if (j < w.Length)
          throw new ParseException("@note: trailing content");
        meta = new ReBodyLine.NoteEntityLine(nt);
        return true;
      }

      var fieldNm = ParseIdent(w, ref j);
      StringLiterals.SkipNoise(w, ref j);
      if (!StringLiterals.TryParse(w, ref j, out var fnt))
        throw new ParseException("@note(field): bad string");
      StringLiterals.SkipNoise(w, ref j);
      if (j >= w.Length || w[j] != ')')
        throw new ParseException("@note(field): expected ')'");
      j++;
      StringLiterals.SkipNoise(w, ref j);
      if (j < w.Length)
        throw new ParseException("@note(field): trailing content");
      meta = new ReBodyLine.NoteFieldLine(fieldNm, fnt);
      return true;
    }

    if (decName.Equals("exe", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(w, ref j, out var exePath))
        throw new ParseException("@exe: bad string");
      StringLiterals.SkipNoise(w, ref j);
      if (j >= w.Length || w[j] != ')')
        throw new ParseException("@exe: expected ')'");
      j++;
      StringLiterals.SkipNoise(w, ref j);
      if (j < w.Length)
        throw new ParseException("@exe: trailing content");
      meta = new ReBodyLine.ExePathLine(exePath);
      return true;
    }

    if (decName.Equals("sha256", StringComparison.OrdinalIgnoreCase))
    {
      if (!StringLiterals.TryParse(w, ref j, out var hexRaw))
        throw new ParseException("@sha256: bad string");
      StringLiterals.SkipNoise(w, ref j);
      if (j >= w.Length || w[j] != ')')
        throw new ParseException("@sha256: expected ')'");
      j++;
      StringLiterals.SkipNoise(w, ref j);
      if (j < w.Length)
        throw new ParseException("@sha256: trailing content");
      meta = new ReBodyLine.Sha256ExpectedLine(NormalizeSha256HexOrThrow(hexRaw));
      return true;
    }

    throw new ParseException($"Unknown decorator @{decName}(...)");
  }

  private static string NormalizeSha256HexOrThrow(string raw)
  {
    var s = raw.Trim();
    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
      s = s[2..];
    s = s.Replace(" ", "").Replace("-", "");
    if (s.Length != 64)
      throw new ParseException("@sha256: expected 64 hex characters after normalization");
    foreach (var c in s)
    {
      if (!Uri.IsHexDigit(c))
        throw new ParseException("@sha256: non-hex character in hash");
    }

    return s.ToLowerInvariant();
  }

  private static List<ReBodyLine> ParseTypeBodyLines(string body)
  {
    var lines = new List<ReBodyLine>();
    var lineStart = 0;
    var pendingDecorators = new List<string>();
    string? pendingFnNote = null;
    var moduleLineJustEmitted = false;
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
      if (ni < body.Length && body[ni] == '\r')
        ni++;
      if (ni < body.Length && body[ni] == '\n')
        ni++;

      if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
      {
        lineStart = ni;
        continue;
      }

      var workLine = line;
      string? inlineSlashNote = null;
      {
        var cmt = IndexOfDoubleSlashLineComment(workLine);
        if (cmt >= 0)
        {
          inlineSlashNote = workLine[(cmt + 2)..].Trim();
          workLine = workLine[..cmt].Trim();
        }
      }

      if (workLine.Length > 0 && workLine[0] == '@')
      {
        if (TryParseAtDecoratorInBody(workLine, out var meta, out var bareName))
        {
          if (bareName != null)
          {
            pendingDecorators.Add(bareName);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }

          if (meta is ReBodyLine.SourceLine sl)
          {
            if (!moduleLineJustEmitted && NextLineLooksLikeNativeFunction(body, ni))
              pendingDecorators.Add($"source({ReQuotedString.DoubleQuote(sl.Url)})");
            else
              lines.Add(sl);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }

          if (meta is ReBodyLine.SummaryLine sum)
          {
            if (!moduleLineJustEmitted && NextLineLooksLikeNativeFunction(body, ni))
              pendingDecorators.Add($"summary({ReQuotedString.DoubleQuote(sum.Text)})");
            else
              lines.Add(sum);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }

          if (meta is ReBodyLine.NoteEntityLine ne)
          {
            if (NextLineLooksLikeNativeFunction(body, ni))
              pendingFnNote = ne.Text;
            else
              lines.Add(ne);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }

          if (meta is ReBodyLine.NoteFieldLine nf)
          {
            lines.Add(nf);
            lineStart = ni;
            pendingDecorators.Clear();
            moduleLineJustEmitted = false;
            continue;
          }

          if (meta is ReBodyLine.ExePathLine ex)
          {
            lines.Add(ex);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }

          if (meta is ReBodyLine.Sha256ExpectedLine sh)
          {
            lines.Add(sh);
            moduleLineJustEmitted = false;
            lineStart = ni;
            continue;
          }
        }

        throw new ParseException($"Unrecognized decorator: {workLine}");
      }

      if (
        InlineBitfieldHeadRegex.IsMatch(workLine)
        && TryParseInlineBitfieldField(
          body,
          lineBegin,
          ni,
          workLine,
          inlineSlashNote,
          out var inl,
          out var inlineEnd
        )
      )
      {
        lines.Add(inl!);
        lineStart = inlineEnd;
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = false;
        continue;
      }

      lineStart = ni;

      try
      {
        var sp = ReLineParsers.StaticFieldLine.End().Parse(workLine);
        lines.Add(
          new ReBodyLine.StaticFieldLine(
            sp.Address,
            sp.Name,
            sp.Type,
            MergeFieldNotes(sp.QuotedNote, inlineSlashNote)
          )
        );
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = false;
        continue;
      }
      catch (Exception)
      {
        // try instance field
      }

      if (
        TryParseStaticFieldLineRegex(
          workLine,
          out var stAddr,
          out var stName,
          out var stTyp
        )
      )
      {
        var stSplit = FieldTypeNoteSplitter.Split(StripLineComment(stTyp));
        lines.Add(
          new ReBodyLine.StaticFieldLine(
            stAddr,
            stName,
            TypeExprParser.Parse(stSplit.TypeText),
            MergeFieldNotes(stSplit.QuotedNote, inlineSlashNote)
          )
        );
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = false;
        continue;
      }

      // Address + name + '(' immediately after the name => native function (optional "fn"; "static" + addr).
      if (
        TryParseFunctionLine(line, pendingDecorators, pendingFnNote, out var fnLineEarly)
        && fnLineEarly != null
      )
      {
        lines.Add(fnLineEarly);
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = false;
        continue;
      }

      try
      {
        var parsed = ReLineParsers.FieldLine.End().Parse(workLine);
        lines.Add(
          new ReBodyLine.FieldLine(
            parsed.Offset,
            parsed.Name,
            parsed.Type,
            MergeFieldNotes(parsed.QuotedNote, inlineSlashNote)
          )
        );
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = false;
        continue;
      }
      catch (Exception)
      {
        if (TryParseFieldLineRegex(workLine, out var off, out var nm, out var typ))
        {
          var fldSplit = FieldTypeNoteSplitter.Split(StripLineComment(typ));
          lines.Add(
            new ReBodyLine.FieldLine(
              off,
              nm,
              TypeExprParser.Parse(fldSplit.TypeText),
              MergeFieldNotes(fldSplit.QuotedNote, inlineSlashNote)
            )
          );
          pendingDecorators.Clear();
          pendingFnNote = null;
          moduleLineJustEmitted = false;
          continue;
        }
      }

      var fw = FirstWord(line);
      if (fw.Equals("module", StringComparison.OrdinalIgnoreCase))
      {
        var rest = line.Substring(6).Trim();
        lines.Add(new ReBodyLine.ModuleLine(rest));
        pendingDecorators.Clear();
        pendingFnNote = null;
        moduleLineJustEmitted = true;
        continue;
      }

      throw new ParseException($"Unrecognized body line: {line}");
    }

    return lines;
  }

  private static bool TryParseInlineBitfieldField(
    string body,
    int rowStart,
    int lineEndExclusive,
    string workLine,
    string? inlineSlashNote,
    out ReBodyLine.InlineBitfieldFieldLine? line,
    out int newPos
  )
  {
    line = null;
    newPos = rowStart;
    if (!InlineBitfieldHeadRegex.IsMatch(workLine))
      return false;
    var m = InlineBitfieldHeadRegex.Match(workLine);
    var braceIdx = body.IndexOf('{', rowStart, lineEndExclusive - rowStart);
    if (braceIdx < 0)
      return false;
    var i = braceIdx;
    var inner = ParseBlock(body, ref i);
    var (bits, sources, summary, blockNote) = ParseBitfieldInnerLines(inner);
    var offset = int.Parse(
      m.Groups[1].Value,
      NumberStyles.HexNumber,
      CultureInfo.InvariantCulture
    );
    var fieldName = m.Groups[2].Value;
    var storage = m.Groups[3].Value;
    line = new ReBodyLine.InlineBitfieldFieldLine(
      offset,
      fieldName,
      storage,
      bits,
      sources,
      summary,
      blockNote,
      MergeFieldNotes(null, inlineSlashNote)
    );
    newPos = i;
    return true;
  }

  /// <summary>
  /// Native function if <c>0xADDR</c> is followed by an identifier and <c>(</c> — not by <c>:</c> (field).
  /// Optional prefix <c>fn</c> or <c>static</c> before the address. Optional trailing <c>// note</c>.
  /// </summary>
  private static bool TryParseFunctionLine(
    string line,
    IReadOnlyList<string> decorators,
    string? pendingDecoratorNote,
    out ReBodyLine.FunctionLine? fl
  )
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

    int i;
    if (
      work.Length >= 4
      && work.StartsWith("fn", StringComparison.OrdinalIgnoreCase)
      && char.IsWhiteSpace(work[2])
    )
    {
      i = 3;
      while (i < work.Length && char.IsWhiteSpace(work[i]))
        i++;
    }
    else if (
      work.Length >= 8
      && work.StartsWith("static", StringComparison.OrdinalIgnoreCase)
      && char.IsWhiteSpace(work[6])
    )
    {
      i = 7;
      while (i < work.Length && char.IsWhiteSpace(work[i]))
        i++;
    }
    else if (
      work.Length >= 3
      && work[0] == '0'
      && (work[1] == 'x' || work[1] == 'X')
      && work.Length > 2
      && Uri.IsHexDigit(work[2])
    )
    {
      i = 0;
    }
    else
      return false;

    if (i + 1 >= work.Length || work[i] != '0' || (work[i + 1] != 'x' && work[i + 1] != 'X'))
      return false;
    i += 2;
    var hs = i;
    while (i < work.Length && Uri.IsHexDigit(work[i]))
      i++;
    if (hs == i)
      return false;
    var address = int.Parse(
      work.AsSpan(hs, i - hs),
      NumberStyles.HexNumber,
      CultureInfo.InvariantCulture
    );

    while (i < work.Length && char.IsWhiteSpace(work[i]))
      i++;

    var nameStart = i;
    if (i >= work.Length || !(char.IsLetter(work[i]) || work[i] == '_'))
      return false;
    i++;
    while (
      i < work.Length
      && (char.IsLetterOrDigit(work[i]) || work[i] == '_' || work[i] == '.')
    )
      i++;
    var name = work.Substring(nameStart, i - nameStart);
    if (name.Length == 0)
      return false;

    while (i < work.Length && char.IsWhiteSpace(work[i]))
      i++;
    if (i >= work.Length || work[i] != '(')
      return false;

    var openP = i;
    i++;
    var depth = 1;
    while (i < work.Length && depth > 0)
    {
      var c = work[i];
      if (c == '(')
        depth++;
      else if (c == ')')
        depth--;
      if (depth == 0)
        break;
      i++;
    }

    if (depth != 0)
      return false;
    var closeP = i;
    var parameters = work.Substring(openP + 1, closeP - openP - 1).Trim();

    i++;
    while (i < work.Length && char.IsWhiteSpace(work[i]))
      i++;
    var after = work.Substring(i).Trim();
    string? retType = null;
    if (after.Length > 0)
    {
      if (!after.StartsWith(':'))
        return false;
      retType = after[1..].Trim();
      if (retType.Length == 0)
        retType = null;
    }

    var mergedNote = pendingDecoratorNote ?? inlineNote;
    fl = new ReBodyLine.FunctionLine(
      address,
      name,
      parameters,
      retType,
      mergedNote,
      decorators.Count == 0 ? Array.Empty<string>() : new List<string>(decorators)
    );
    return true;
  }

  private static bool TryParseStaticFieldLineRegex(
    string line,
    out ulong address,
    out string name,
    out string typeRest
  )
  {
    address = 0;
    name = "";
    typeRest = "";
    var m = StaticFieldLineRegex.Match(line.Trim());
    if (!m.Success)
      return false;
    address = ulong.Parse(
      m.Groups[1].Value.AsSpan(2),
      NumberStyles.HexNumber,
      CultureInfo.InvariantCulture
    );
    name = m.Groups[2].Value;
    typeRest = m.Groups[3].Value.Trim();
    return true;
  }

  private static bool TryParseFieldLineRegex(
    string line,
    out int offset,
    out string name,
    out string typeRest
  )
  {
    offset = 0;
    name = "";
    typeRest = "";
    var m = FieldLineRegex.Match(line.Trim());
    if (!m.Success)
      return false;
    offset = int.Parse(
      m.Groups[1].Value.AsSpan(2),
      NumberStyles.HexNumber,
      CultureInfo.InvariantCulture
    );
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
    while (i < line.Length && char.IsWhiteSpace(line[i]))
      i++;
    var j = i;
    while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_'))
      j++;
    return line.Substring(i, j - i);
  }
}
