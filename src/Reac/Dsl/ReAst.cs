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
        string? SourceUrl) : ReTopLevel;

    public sealed record Module(string Name, string? Summary, string? Note, IReadOnlyList<ReBodyLine> Body) : ReTopLevel;

    /// <param name="DeclaredSize">Explicit size in source, or null if omitted (inferred later).</param>
    public sealed record TypeDef(
        TypeKind Kind,
        string Name,
        string? Parent,
        int? DeclaredSize,
        IReadOnlyList<ReBodyLine> Body) : ReTopLevel;
}

public abstract record ReBodyLine
{
    public sealed record ModuleLine(string ModuleName) : ReBodyLine;
    public sealed record SourceLine(string Url) : ReBodyLine;
    public sealed record SummaryLine(string Text) : ReBodyLine;
    public sealed record NoteEntityLine(string Text) : ReBodyLine;
    public sealed record FieldLine(int Offset, string Name, TypeExpr Type, string? Note) : ReBodyLine;
    public sealed record NoteFieldLine(string FieldName, string Text) : ReBodyLine;
}
