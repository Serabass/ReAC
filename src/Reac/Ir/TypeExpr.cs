namespace Reac.Ir;

public abstract record TypeExpr
{
    public sealed record Scalar(string Name) : TypeExpr; // pointer, uint32, float, byte, ...
    public sealed record Named(string Name) : TypeExpr;
    public sealed record Pointer(TypeExpr Inner) : TypeExpr;
    public sealed record Array(TypeExpr Element, int Length) : TypeExpr;
}
