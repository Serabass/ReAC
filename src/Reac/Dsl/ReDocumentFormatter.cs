using System.Globalization;
using System.Text;
using Reac.Ir;

namespace Reac.Dsl;

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

  private static void AppendTypeDef(StringBuilder sb, ReTopLevel.TypeDef td, FormatOptions o, int level)
  {
    var ind = o.Indent(level);
    sb.Append(ind).Append(td.Kind == TypeKind.Class ? "class " : "struct ").Append(td.Name);
    if (td.Parent != null)
      sb.Append(" : ").Append(td.Parent);
    if (td.DeclaredSize.HasValue)
    {
      sb.Append(" size ");
      sb.Append(ReHexFormat.Format((ulong)td.DeclaredSize.Value, HexKind.Size, o));
    }

    sb.Append(" {\n");
    AppendBodyLines(sb, td.Body, o, level + 1);
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
      level + 1
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
      level + 1
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
    int level
  )
  {
    var ind = o.Indent(level);
    foreach (var url in sourceUrls)
      sb.Append(ind).Append("source ").Append(ReQuotedString.DoubleQuote(url)).Append('\n');
    if (summary != null)
      sb.Append(ind).Append("summary ").Append(ReQuotedString.DoubleQuote(summary)).Append('\n');
    if (note != null)
      sb.Append(ind).Append("note ").Append(ReQuotedString.DoubleQuote(note)).Append('\n');

    foreach (var (value, name, desc) in values)
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

  private static void AppendBodyLines(StringBuilder sb, IReadOnlyList<ReBodyLine> body, FormatOptions o, int level)
  {
    var maxW = o.AlignFieldTypes ? MaxFieldNameWidth(body) : 0;
    foreach (var line in body)
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
        sb.Append(ind).Append("source ").Append(ReQuotedString.DoubleQuote(sl.Url)).Append('\n');
        break;
      case ReBodyLine.SummaryLine sm:
        sb.Append(ind).Append("summary ").Append(ReQuotedString.DoubleQuote(sm.Text)).Append('\n');
        break;
      case ReBodyLine.NoteEntityLine ne:
        sb.Append(ind).Append("note ").Append(ReQuotedString.DoubleQuote(ne.Text)).Append('\n');
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
          .Append("note ")
          .Append(nf.FieldName)
          .Append(' ')
          .Append(ReQuotedString.DoubleQuote(nf.Text))
          .Append('\n');
        break;
      case ReBodyLine.NoteFunctionLine nff:
        sb.Append(ind)
          .Append("note fn ")
          .Append(nff.FunctionName)
          .Append(' ')
          .Append(ReQuotedString.DoubleQuote(nff.Text))
          .Append('\n');
        break;
      case ReBodyLine.FunctionLine fn:
        foreach (var d in fn.Decorators)
          sb.Append(ind).Append('@').Append(d).Append('\n');
        sb.Append(ind)
          .Append(ReHexFormat.FormatInt(fn.Address, HexKind.Address, o))
          .Append(' ')
          .Append(fn.Name)
          .Append('(')
          .Append(fn.Parameters)
          .Append(')');
        if (fn.ReturnType != null)
          sb.Append(" : ").Append(fn.ReturnType);
        if (fn.Note != null)
          sb.Append(" // ").Append(fn.Note);
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
      level + 1
    );
    sb.Append(ind).Append('}');
    if (ib.Note != null)
      sb.Append(" // ").Append(ib.Note);
    sb.Append('\n');
  }
}
