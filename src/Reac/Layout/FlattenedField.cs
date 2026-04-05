using Reac.Ir;

namespace Reac.Layout;

public sealed class FlattenedField
{
  public required int Offset { get; init; }
  public required string Name { get; init; }
  public required TypeExpr Type { get; init; }
  public required string DeclaringTypeName { get; init; }
  public bool Indeterminate { get; init; }
  public string? Note { get; init; }
  public IReadOnlyList<FlagBitDecl>? FlagBits { get; init; }
  public string? BitfieldTypeName { get; init; }
}
