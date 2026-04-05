using Reac.Ir;

namespace Reac.Layout;

public sealed class TypeLayout
{
  public required string Name { get; init; }
  public required IReadOnlyList<string> InheritanceChain { get; init; }
  public required IReadOnlyList<FlattenedField> Flattened { get; init; }
  public required IReadOnlyList<FieldDecl> OwnFields { get; init; }
}
