using Reac.Ir;
using Sprache;

namespace Reac.Dsl;

/// <summary>Sprache parsers for single-line field declarations.</summary>
public static class ReLineParsers
{
  public static readonly Parser<(int Offset, string Name, TypeExpr Type)> FieldLine =
    from off in CommonParse.HexInt
    from name in CommonParse.Identifier
    from _ in Parse.Char(':')
    from rest in Parse.Regex(@"[^\r\n]+")
    select (off, name, TypeExprParser.Parse(StripComment(rest)));

  /// <summary><c>static 0xADDR name : type</c> — global pointer in the module image.</summary>
  public static readonly Parser<(ulong Address, string Name, TypeExpr Type)> StaticFieldLine =
    from _ in CommonParse.Tok(Parse.IgnoreCase("static"))
    from addr in CommonParse.HexULong
    from name in CommonParse.Identifier
    from _sp in Parse.WhiteSpace.Many()
    from _c in Parse.Char(':')
    from rest in Parse.Regex(@"[^\r\n]+")
    select (addr, name, TypeExprParser.Parse(StripComment(rest)));

  private static string StripComment(string rest)
  {
    var i = rest.IndexOf("//", StringComparison.Ordinal);
    if (i >= 0)
      rest = rest[..i];
    return rest.Trim();
  }
}
