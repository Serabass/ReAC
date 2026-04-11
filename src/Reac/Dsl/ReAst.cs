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
    IReadOnlyList<(int Bit, string Name, string? Description)> Bits,
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

  /// <summary>Instance field with inline bit layout (synthetic bitfield type name derived as <c>TypeName_fieldName</c>).</summary>
  public sealed record InlineBitfieldFieldLine(
    int Offset,
    string FieldName,
    string StorageName,
    IReadOnlyList<(int Bit, string Name, string? Description)> Bits,
    IReadOnlyList<string> SourceUrls,
    string? Summary,
    string? BlockNote,
    string? Note
  ) : ReBodyLine;

  /// <summary>Global/static at absolute address (not an instance offset).</summary>
  public sealed record StaticFieldLine(ulong Address, string Name, TypeExpr Type, string? Note)
    : ReBodyLine;

  public sealed record NoteFieldLine(string FieldName, string Text) : ReBodyLine;

  /// <param name="Parameters">Comma-separated parameter types as in C++ (opaque text).</param>
  public sealed record FunctionLine(
    int Address,
    string Name,
    string Parameters,
    string? ReturnType,
    string? Note,
    IReadOnlyList<string> Decorators
  ) : ReBodyLine;

  public sealed record NoteFunctionLine(string FunctionName, string Text) : ReBodyLine;

  /// <summary>Module metadata: path to game exe (or fingerprint file), relative to project root or absolute.</summary>
  public sealed record ExePathLine(string Path) : ReBodyLine;

  /// <summary>Expected SHA-256 of the exe file (64 hex digits; optional 0x prefix; case-insensitive).</summary>
  public sealed record Sha256ExpectedLine(string Hex) : ReBodyLine;
}
