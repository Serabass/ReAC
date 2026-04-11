namespace Reac.Ir;

public abstract record TypeExpr
{
  public sealed record Scalar(string Name) : TypeExpr; // pointer, uint32, float, byte, ...

  public sealed record Named(string Name) : TypeExpr;

  public sealed record Pointer(TypeExpr Inner) : TypeExpr;

  public sealed record Array(TypeExpr Element, int Length) : TypeExpr;

  /// <summary>Constructed type, e.g. <c>FuncPtr&lt;int&gt;</c> (not C function-pointer syntax with parentheses).</summary>
  public sealed record Generic(string Name, IReadOnlyList<TypeExpr> TypeArguments) : TypeExpr;
}
