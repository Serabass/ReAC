using System.Globalization;
using System.Text;
using Reac.Ir;

namespace Reac.Dsl;

file static class ReBodySortHelpers
{
  public static string FieldBindingName(ReBodyLine line) =>
    line switch
    {
      ReBodyLine.FieldLine fl => fl.Name,
      ReBodyLine.InlineBitfieldFieldLine ib => ib.FieldName,
      _ => throw new InvalidOperationException("Expected field or inline bitfield line"),
    };

  public static int FieldOffset(ReBodyLine line) =>
    line switch
    {
      ReBodyLine.FieldLine fl => fl.Offset,
      ReBodyLine.InlineBitfieldFieldLine ib => ib.Offset,
      _ => 0,
    };
}

/// <summary>Parsable .re text from AST. Round-trip may drop // comments (see parser). Uses LF line endings.</summary>
public static class ReDocumentFormatter
{
  public static string FormatDocument(IReadOnlyList<ReTopLevel> tops, FormatOptions o)
  {
    var sb = new StringBuilder();
    for (var i = 0; i < tops.Count; i++)
    {
      if (i > 0)
        sb.Append('\n');
      AppendTopLevel(sb, tops[i], o, 0);
    }

    var s = sb.ToString();
    if (s.Length == 0)
      return "";
    return s.TrimEnd() + "\n";
  }

  private static void AppendTopLevel(StringBuilder sb, ReTopLevel top, FormatOptions o, int level)
  {
    switch (top)
    {
      case ReTopLevel.Target t:
        AppendTarget(sb, t, o, level);
        break;
      case ReTopLevel.Module m:
        AppendModule(sb, m, o, level);
        break;
      case ReTopLevel.TypeDef td:
        AppendTypeDef(sb, td, o, level);
        break;
      case ReTopLevel.BitfieldDef bf:
        AppendBitfieldDef(sb, bf, o, level);
        break;
      case ReTopLevel.EnumDef en:
        AppendEnumDef(sb, en, o, level);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(top));
    }
  }

  private static void AppendTarget(StringBuilder sb, ReTopLevel.Target t, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    var ind1 = o.Indent(level + 1);
    sb.Append(ind).Append("target ").Append(t.Id).Append(" {\n");
    sb.Append(ind1)
      .Append("pointer_size_bytes ")
      .Append(t.PointerSizeBytes.ToString(CultureInfo.InvariantCulture))
      .Append('\n');
    if (t.Game != null)
      sb.Append(ind1).Append("game ").Append(ReQuotedString.DoubleQuote(t.Game)).Append('\n');
    if (t.Version != null)
      sb.Append(ind1).Append("version ").Append(ReQuotedString.DoubleQuote(t.Version)).Append('\n');
    if (t.Platform != null)
      sb.Append(ind1).Append("platform ").Append(ReQuotedString.DoubleQuote(t.Platform)).Append('\n');
    foreach (var url in t.SourceUrls)
      sb.Append(ind1).Append("source ").Append(ReQuotedString.DoubleQuote(url)).Append('\n');
    sb.Append(ind).Append("}\n");
  }

  private static void AppendModule(StringBuilder sb, ReTopLevel.Module m, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    var ind1 = o.Indent(level + 1);
    sb.Append(ind).Append("module ").Append(m.Name).Append(" {\n");
    AppendBodyLines(sb, m.Body, o, level + 1);
    sb.Append(ind).Append("}\n");
  }

  /// <summary>All <c>@source</c>/<c>@summary</c>/<c>@note</c> lines go before <c>class</c>; remaining body lines keep order.</summary>
  private static (List<ReBodyLine> PrefixLines, List<ReBodyLine> BodyRest) SplitLeadingTypeMetadata(
    IReadOnlyList<ReBodyLine> body
  )
  {
    var prefix = new List<ReBodyLine>();
    var rest = new List<ReBodyLine>();
    foreach (var l in body)
    {
      if (
        l is ReBodyLine.SourceLine
          or ReBodyLine.SummaryLine
          or ReBodyLine.NoteEntityLine
          or ReBodyLine.ExePathLine
          or ReBodyLine.Sha256ExpectedLine
      )
        prefix.Add(l);
      else
        rest.Add(l);
    }

    return (prefix, rest);
  }

  private static void AppendTypeMetadataDecorator(StringBuilder sb, ReBodyLine line, string ind)
  {
    switch (line)
    {
      case ReBodyLine.SourceLine sl:
        sb.Append(ind).Append("@source(").Append(ReQuotedString.DoubleQuote(sl.Url)).Append(")\n");
        break;
      case ReBodyLine.SummaryLine sm:
        sb.Append(ind).Append("@summary(").Append(ReQuotedString.DoubleQuote(sm.Text)).Append(")\n");
        break;
      case ReBodyLine.NoteEntityLine ne:
        sb.Append(ind).Append("@note(").Append(ReQuotedString.DoubleQuote(ne.Text)).Append(")\n");
        break;
      case ReBodyLine.ExePathLine xp:
        sb.Append(ind).Append("@exe(").Append(ReQuotedString.DoubleQuote(xp.Path)).Append(")\n");
        break;
      case ReBodyLine.Sha256ExpectedLine sh:
        sb.Append(ind).Append("@sha256(").Append(ReQuotedString.DoubleQuote(sh.Hex)).Append(")\n");
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(line));
    }
  }

  private static void AppendTypeDef(StringBuilder sb, ReTopLevel.TypeDef td, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    var (prefix, innerBody) = SplitLeadingTypeMetadata(td.Body);
    foreach (var p in prefix)
      AppendTypeMetadataDecorator(sb, p, ind);

    sb.Append(ind).Append(td.Kind == TypeKind.Class ? "class " : "struct ").Append(td.Name);
    if (td.Parent != null)
      sb.Append(" : ").Append(td.Parent);
    if (td.DeclaredSize.HasValue)
    {
      sb.Append(" size ");
      sb.Append(ReHexFormat.Format((ulong)td.DeclaredSize.Value, HexKind.Size, o));
    }

    sb.Append(" {\n");
    AppendBodyLines(sb, innerBody, o, level + 1);
    sb.Append(ind).Append("}\n");
  }

  private static void AppendBitfieldDef(StringBuilder sb, ReTopLevel.BitfieldDef bf, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    sb.Append(ind).Append("bitfield ").Append(bf.Name).Append(" : ").Append(bf.StorageName).Append(" {\n");
    AppendBitfieldEnumInner(
      sb,
      bf.Bits.Select(b => ((ulong)b.Bit, b.Name, b.Description)),
      bf.SourceUrls,
      bf.Summary,
      bf.Note,
      o,
      level + 1,
      o.SortBitfieldBits
    );
    sb.Append(ind).Append("}\n");
  }

  private static void AppendEnumDef(StringBuilder sb, ReTopLevel.EnumDef ed, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    sb.Append(ind).Append("enum ").Append(ed.Name).Append(" : ").Append(ed.StorageName).Append(" {\n");
    AppendBitfieldEnumInner(
      sb,
      ed.Values,
      ed.SourceUrls,
      ed.Summary,
      ed.Note,
      o,
      level + 1,
      o.SortEnumValues
    );
    sb.Append(ind).Append("}\n");
  }

  private static void AppendBitfieldEnumInner(
    StringBuilder sb,
    IEnumerable<(ulong Value, string Name, string? Description)> values,
    IReadOnlyList<string> sourceUrls,
    string? summary,
    string? note,
    FormatOptions o,
    int level,
    LineSortMode lineSort
  )
  {
    var ind = o.Indent(level);
    foreach (var url in sourceUrls)
      sb.Append(ind).Append("@source(").Append(ReQuotedString.DoubleQuote(url)).Append(")\n");
    if (summary != null)
      sb.Append(ind).Append("@summary(").Append(ReQuotedString.DoubleQuote(summary)).Append(")\n");
    if (note != null)
      sb.Append(ind).Append("@note(").Append(ReQuotedString.DoubleQuote(note)).Append(")\n");

    foreach (var (value, name, desc) in OrderNamedNumericTuples(values, lineSort))
    {
      sb.Append(ind)
        .Append(ReHexFormat.Format(value, HexKind.EnumOrBitIndex, o))
        .Append(' ')
        .Append(name);
      if (desc != null)
        sb.Append(' ').Append(ReQuotedString.DoubleQuote(desc));
      sb.Append('\n');
    }
  }

  private static int MaxFieldNameWidth(IReadOnlyList<ReBodyLine> body)
  {
    var m = 0;
    foreach (var line in body)
    {
      if (line is ReBodyLine.FieldLine f)
        m = Math.Max(m, f.Name.Length);
    }

    return m;
  }

  private static List<(ulong Value, string Name, string? Description)> OrderNamedNumericTuples(
    IEnumerable<(ulong Value, string Name, string? Description)> values,
    LineSortMode mode
  )
  {
    var list = values.ToList();
    if (mode == LineSortMode.Preserve || list.Count <= 1)
      return list;
    if (mode == LineSortMode.ByNumeric)
    {
      list.Sort(
        (a, b) =>
        {
          var c = a.Value.CompareTo(b.Value);
          return c != 0
            ? c
            : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }
      );
    }
    else
    {
      list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    return list;
  }

  /// <summary>
  /// When any sort mode is active: header lines (module, <c>@source</c>/<c>@summary</c>/<c>@note</c> as body lines) first in file order,
  /// then fields (sorted) with <c>@note(field ...)</c> lines after each name, then statics, then functions (with preceding <c>@note</c> on <see cref="ReBodyLine.FunctionLine"/>).
  /// </summary>
  private static List<ReBodyLine> ApplyBodySort(IReadOnlyList<ReBodyLine> body, FormatOptions o)
  {
    if (
      o.SortFields == LineSortMode.Preserve
      && o.SortStaticFields == LineSortMode.Preserve
      && o.SortFunctions == LineSortMode.Preserve
    )
      return body.ToList();

    var header = new List<ReBodyLine>();
    var notesByField = new Dictionary<string, List<ReBodyLine.NoteFieldLine>>(StringComparer.OrdinalIgnoreCase);
    var fields = new List<ReBodyLine>();
    var statics = new List<ReBodyLine.StaticFieldLine>();
    var funcs = new List<ReBodyLine.FunctionLine>();

    foreach (var line in body)
    {
      switch (line)
      {
        case ReBodyLine.NoteFieldLine nf:
          if (!notesByField.TryGetValue(nf.FieldName, out var lf))
          {
            lf = new List<ReBodyLine.NoteFieldLine>();
            notesByField[nf.FieldName] = lf;
          }

          lf.Add(nf);
          break;
        case ReBodyLine.FieldLine:
        case ReBodyLine.InlineBitfieldFieldLine:
          fields.Add(line);
          break;
        case ReBodyLine.StaticFieldLine sf:
          statics.Add(sf);
          break;
        case ReBodyLine.FunctionLine fn:
          funcs.Add(fn);
          break;
        case ReBodyLine.ModuleLine:
        case ReBodyLine.SourceLine:
        case ReBodyLine.SummaryLine:
        case ReBodyLine.NoteEntityLine:
        case ReBodyLine.ExePathLine:
        case ReBodyLine.Sha256ExpectedLine:
          header.Add(line);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(line));
      }
    }

    if (o.SortFields != LineSortMode.Preserve)
    {
      fields.Sort(
        (a, b) =>
        {
          if (o.SortFields == LineSortMode.ByNumeric)
          {
            var c = ReBodySortHelpers.FieldOffset(a).CompareTo(ReBodySortHelpers.FieldOffset(b));
            return c != 0
              ? c
              : string.Compare(
                ReBodySortHelpers.FieldBindingName(a),
                ReBodySortHelpers.FieldBindingName(b),
                StringComparison.OrdinalIgnoreCase
              );
          }

          return string.Compare(
            ReBodySortHelpers.FieldBindingName(a),
            ReBodySortHelpers.FieldBindingName(b),
            StringComparison.OrdinalIgnoreCase
          );
        }
      );
    }

    if (o.SortStaticFields != LineSortMode.Preserve)
    {
      statics.Sort(
        (a, b) =>
        {
          if (o.SortStaticFields == LineSortMode.ByNumeric)
          {
            var c = a.Address.CompareTo(b.Address);
            return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
          }

          return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }
      );
    }

    if (o.SortFunctions != LineSortMode.Preserve)
    {
      funcs.Sort(
        (a, b) =>
        {
          if (o.SortFunctions == LineSortMode.ByNumeric)
          {
            var c = a.Address.CompareTo(b.Address);
            return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
          }

          return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }
      );
    }

    var result = new List<ReBodyLine>();
    result.AddRange(header);

    foreach (var f in fields)
    {
      result.Add(f);
      var name = ReBodySortHelpers.FieldBindingName(f);
      if (notesByField.TryGetValue(name, out var ns))
      {
        result.AddRange(ns);
        notesByField.Remove(name);
      }
    }

    foreach (var kv in notesByField)
      result.AddRange(kv.Value);

    result.AddRange(statics);

    foreach (var fn in funcs)
      result.Add(fn);

    return result;
  }

  /// <summary>Merges legacy <c>note fn Name \"...\"</c> lines into <see cref="ReBodyLine.FunctionLine.Note"/> so output uses <c>@note</c> before the address line.</summary>
  private static List<ReBodyLine> MergeLegacyNoteFunctionLines(IReadOnlyList<ReBodyLine> body)
  {
    var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var line in body)
    {
      if (line is ReBodyLine.NoteFunctionLine nfn)
        byName[nfn.FunctionName] = nfn.Text;
    }

    if (byName.Count == 0)
      return new List<ReBodyLine>(body);

    var result = new List<ReBodyLine>(body.Count);
    foreach (var line in body)
    {
      if (line is ReBodyLine.NoteFunctionLine)
        continue;
      if (line is ReBodyLine.FunctionLine fn && byName.TryGetValue(fn.Name, out var nt))
        result.Add(fn with { Note = fn.Note ?? nt });
      else
        result.Add(line);
    }

    return result;
  }

  private static void AppendBodyLines(StringBuilder sb, IReadOnlyList<ReBodyLine> body, FormatOptions o, int level)
  {
    var merged = MergeLegacyNoteFunctionLines(body);
    var ordered = ApplyBodySort(merged, o);
    var maxW = o.AlignFieldTypes ? MaxFieldNameWidth(ordered) : 0;
    foreach (var line in ordered)
      AppendBodyLine(sb, line, o, level, maxW);
  }

  private static void AppendBodyLine(
    StringBuilder sb,
    ReBodyLine line,
    FormatOptions o,
    int level,
    int maxFieldNameWidth
  )
  {
    var ind = o.Indent(level);
    switch (line)
    {
      case ReBodyLine.ModuleLine ml:
        sb.Append(ind).Append("module ").Append(ml.ModuleName).Append('\n');
        break;
      case ReBodyLine.SourceLine sl:
        sb.Append(ind).Append("@source(").Append(ReQuotedString.DoubleQuote(sl.Url)).Append(")\n");
        break;
      case ReBodyLine.SummaryLine sm:
        sb.Append(ind).Append("@summary(").Append(ReQuotedString.DoubleQuote(sm.Text)).Append(")\n");
        break;
      case ReBodyLine.NoteEntityLine ne:
        sb.Append(ind).Append("@note(").Append(ReQuotedString.DoubleQuote(ne.Text)).Append(")\n");
        break;
      case ReBodyLine.ExePathLine xp:
        sb.Append(ind).Append("@exe(").Append(ReQuotedString.DoubleQuote(xp.Path)).Append(")\n");
        break;
      case ReBodyLine.Sha256ExpectedLine sh:
        sb.Append(ind).Append("@sha256(").Append(ReQuotedString.DoubleQuote(sh.Hex)).Append(")\n");
        break;
      case ReBodyLine.FieldLine fl:
        sb.Append(ind).Append(ReHexFormat.FormatInt(fl.Offset, HexKind.Offset, o)).Append(' ');
        if (o.AlignFieldTypes)
          sb.Append(fl.Name.PadRight(maxFieldNameWidth));
        else
          sb.Append(fl.Name);
        sb.Append(" : ");
        TypeExprFormatter.Append(fl.Type, sb);
        if (fl.Note != null)
          sb.Append(' ').Append(ReQuotedString.DoubleQuote(fl.Note));
        sb.Append('\n');
        break;
      case ReBodyLine.StaticFieldLine sf:
        sb.Append(ind)
          .Append("static ")
          .Append(ReHexFormat.Format(sf.Address, HexKind.Address, o))
          .Append(' ')
          .Append(sf.Name)
          .Append(" : ");
        TypeExprFormatter.Append(sf.Type, sb);
        if (sf.Note != null)
          sb.Append(' ').Append(ReQuotedString.DoubleQuote(sf.Note));
        sb.Append('\n');
        break;
      case ReBodyLine.NoteFieldLine nf:
        sb.Append(ind)
          .Append("@note(")
          .Append(nf.FieldName)
          .Append(' ')
          .Append(ReQuotedString.DoubleQuote(nf.Text))
          .Append(")\n");
        break;
      case ReBodyLine.NoteFunctionLine:
        throw new InvalidOperationException("NoteFunctionLine must be merged before formatting.");
      case ReBodyLine.FunctionLine fn:
        foreach (var d in fn.Decorators)
          sb.Append(ind).Append('@').Append(d).Append('\n');
        if (fn.Note != null)
          sb.Append(ind).Append("@note(").Append(ReQuotedString.DoubleQuote(fn.Note)).Append(")\n");
        sb.Append(ind)
          .Append(ReHexFormat.FormatInt(fn.Address, HexKind.Address, o))
          .Append(' ')
          .Append(fn.Name)
          .Append('(')
          .Append(fn.Parameters)
          .Append(')');
        if (fn.ReturnType != null)
          sb.Append(" : ").Append(fn.ReturnType);
        sb.Append('\n');
        break;
      case ReBodyLine.InlineBitfieldFieldLine ib:
        AppendInlineBitfield(sb, ib, o, level, maxFieldNameWidth);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(line));
    }
  }

  private static void AppendInlineBitfield(
    StringBuilder sb,
    ReBodyLine.InlineBitfieldFieldLine ib,
    FormatOptions o,
    int level,
    int maxFieldNameWidth
  )
  {
    var ind = o.Indent(level);
    sb.Append(ind).Append(ReHexFormat.FormatInt(ib.Offset, HexKind.Offset, o)).Append(' ');
    if (o.AlignFieldTypes)
      sb.Append(ib.FieldName.PadRight(maxFieldNameWidth));
    else
      sb.Append(ib.FieldName);
    sb.Append(" : bitfield : ").Append(ib.StorageName).Append(" {\n");
    AppendBitfieldEnumInner(
      sb,
      ib.Bits.Select(b => ((ulong)b.Bit, b.Name, b.Description)),
      ib.SourceUrls,
      ib.Summary,
      ib.BlockNote,
      o,
      level + 1,
      o.SortBitfieldBits
    );
    sb.Append(ind).Append('}');
    if (ib.Note != null)
      sb.Append(" // ").Append(ib.Note);
    sb.Append('\n');
  }
}
