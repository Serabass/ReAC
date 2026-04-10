using Reac.Ir;
using Sprache;

namespace Reac.Dsl;

/// <summary>Sprache parsers for single-line field declarations.</summary>
public static class ReLineParsers
{
  public static readonly Parser<(int Offset, string Name, TypeExpr Type, string? QuotedNote)> FieldLine =
    from off in CommonParse.HexInt
    from name in CommonParse.Identifier
    from _ in Parse.Char(':')
    from rest in Parse.Regex(@"[^\r\n]+")
    let cleaned = StripComment(rest)
    let parts = FieldTypeNoteSplitter.Split(cleaned)
    select (off, name, TypeExprParser.Parse(parts.TypeText), parts.QuotedNote);

  /// <summary><c>static 0xADDR name : type</c> — global pointer in the module image.</summary>
  public static readonly Parser<(ulong Address, string Name, TypeExpr Type, string? QuotedNote)> StaticFieldLine =
    from _ in CommonParse.Tok(Parse.IgnoreCase("static"))
    from addr in CommonParse.HexULong
    from name in CommonParse.Identifier
    from _sp in Parse.WhiteSpace.Many()
    from _c in Parse.Char(':')
    from rest in Parse.Regex(@"[^\r\n]+")
    let cleaned = StripComment(rest)
    let parts = FieldTypeNoteSplitter.Split(cleaned)
    select (addr, name, TypeExprParser.Parse(parts.TypeText), parts.QuotedNote);

  private static string StripComment(string rest)
  {
    var i = rest.IndexOf("//", StringComparison.Ordinal);
    if (i >= 0)
      rest = rest[..i];
    return rest.Trim();
  }
}
