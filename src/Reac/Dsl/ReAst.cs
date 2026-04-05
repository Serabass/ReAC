using Reac.Ir;

namespace Reac.Dsl;

public abstract record ReTopLevel
{
  public sealed record Target(
    string Id,
    int PointerSizeBytes,
    string? Game,
    string? Version,
    string? Platform,
    IReadOnlyList<string> SourceUrls
  ) : ReTopLevel;

  public sealed record Module(
    string Name,
    string? Summary,
    string? Note,
    IReadOnlyList<ReBodyLine> Body
  ) : ReTopLevel;

  /// <param name="DeclaredSize">Explicit size in source, or null if omitted (inferred later).</param>
  public sealed record TypeDef(
    TypeKind Kind,
    string Name,
    string? Parent,
    int? DeclaredSize,
    IReadOnlyList<ReBodyLine> Body
  ) : ReTopLevel;

  /// <summary>Named flag layout; storage is any fixed-size scalar (bit count = 8 * scalar size). Use as field type, e.g. <c>0x10 flags : MyFlags</c>.</summary>
  public sealed record BitfieldDef(
    string Name,
    string StorageName,
    IReadOnlyList<(int Bit, string Name)> Bits,
    IReadOnlyList<string> SourceUrls,
    string? Summary,
    string? Note
  ) : ReTopLevel;

  /// <summary>Named enumeration; storage is a fixed-size scalar. Values are unsigned; optional quoted description per line.</summary>
  public sealed record EnumDef(
    string Name,
    string StorageName,
    IReadOnlyList<(ulong Value, string Name, string? Description)> Values,
    IReadOnlyList<string> SourceUrls,
    string? Summary,
    string? Note
  ) : ReTopLevel;
}

public abstract record ReBodyLine
{
  public sealed record ModuleLine(string ModuleName) : ReBodyLine;

  public sealed record SourceLine(string Url) : ReBodyLine;

  public sealed record SummaryLine(string Text) : ReBodyLine;

  public sealed record NoteEntityLine(string Text) : ReBodyLine;

  public sealed record FieldLine(int Offset, string Name, TypeExpr Type, string? Note) : ReBodyLine;

  public sealed record NoteFieldLine(string FieldName, string Text) : ReBodyLine;

  /// <param name="Parameters">Comma-separated parameter types as in C++ (opaque text).</param>
  public sealed record FunctionLine(
    int Address,
    string Name,
    string Parameters,
    string? ReturnType,
    string? Note
  ) : ReBodyLine;

  public sealed record NoteFunctionLine(string FunctionName, string Text) : ReBodyLine;
}
