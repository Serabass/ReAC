namespace Reac.Ir;

public enum TypeKind
{
  Class,
  Struct,
}

public sealed class FlagBitDecl
{
  public required int Bit { get; init; }
  public required string Name { get; init; }
}

public sealed class FieldDecl
{
  public required int Offset { get; init; }
  public required string Name { get; init; }
  public required TypeExpr Type { get; init; }
  public string? Note { get; init; }
  public Provenance? Provenance { get; init; }

  /// <summary>Optional bit names for a scalar flag word (e.g. byte).</summary>
  public IReadOnlyList<FlagBitDecl>? FlagBits { get; init; }

  /// <summary>When field type was a named <c>bitfield</c> definition, its name (for docs).</summary>
  public string? BitfieldTypeName { get; init; }
}

/// <summary>Top-level named bit layout (storage is one scalar).</summary>
public sealed class BitfieldTypeDecl
{
  public required string Name { get; init; }

  /// <summary>Underlying scalar name, e.g. byte or uint16.</summary>
  public required string StorageName { get; init; }
  public required IReadOnlyList<FlagBitDecl> Bits { get; init; }
  public required IReadOnlyList<string> SourceUrls { get; init; }
  public string? Summary { get; init; }
  public string? Note { get; init; }
  public required string FilePath { get; init; }
}

/// <summary>Native function entry point associated with a type (thiscall/free function address from game exe).</summary>
public sealed class FunctionDecl
{
  public required int Address { get; init; }
  public required string Name { get; init; }
  public required string Parameters { get; init; }
  public string? ReturnType { get; init; }
  public string? Note { get; init; }
  public Provenance? Provenance { get; init; }
}

public sealed class TypeDecl
{
  public required string Name { get; init; }
  public required TypeKind Kind { get; init; }
  public string? ParentName { get; init; }

  /// <summary>Size from DSL when present; null means inferred into <see cref="Size"/>.</summary>
  public int? DeclaredSize { get; init; }

  /// <summary>Effective object size: explicit or inferred; 0 means unknown/indeterminate.</summary>
  public required int Size { get; init; }
  public string? ModuleName { get; init; }
  public required IReadOnlyList<string> SourceUrls { get; init; }
  public string? Summary { get; init; }
  public string? Note { get; init; }
  public Provenance? Provenance { get; init; }
  public required IReadOnlyList<FieldDecl> OwnFields { get; init; }
  public required IReadOnlyList<FunctionDecl> OwnFunctions { get; init; }
  public required string FilePath { get; init; }
}

public sealed class ModuleDecl
{
  public required string Name { get; init; }
  public string? Summary { get; init; }
  public string? Note { get; init; }
  public required string FilePath { get; init; }
}

public sealed class TargetDecl
{
  public required string Id { get; init; }
  public int PointerSizeBytes { get; init; }
  public string? Game { get; init; }
  public string? Version { get; init; }
  public string? Platform { get; init; }
  public required IReadOnlyList<string> SourceUrls { get; init; }
  public required string FilePath { get; init; }
}

public sealed class DocumentDecl
{
  public required string Id { get; init; }
  public required string Title { get; init; }
  public string? Summary { get; init; }
  public required IReadOnlyList<string> References { get; init; }
  public required IReadOnlyList<DocSection> Sections { get; init; }
  public required string FilePath { get; init; }
}

public sealed class DocSection
{
  public required string Name { get; init; }
  public required string Text { get; init; }
}

public sealed class ProjectIr
{
  public required ProjectConfig Config { get; init; }
  public required string ProjectRoot { get; init; }
  public required IReadOnlyList<TargetDecl> Targets { get; init; }
  public required IReadOnlyList<ModuleDecl> Modules { get; init; }
  public required IReadOnlyList<TypeDecl> Types { get; init; }
  public required IReadOnlyList<BitfieldTypeDecl> BitfieldTypes { get; init; }
  public required IReadOnlyList<DocumentDecl> Documents { get; init; }
}

public sealed class ProjectConfig
{
  public required string Name { get; init; }
  public required string Version { get; init; }
  public required string ActiveTarget { get; init; }
  public required string TargetsDir { get; init; }
  public required string ModulesDir { get; init; }
  public required string TypesDir { get; init; }
  public required string DocsDir { get; init; }
  public required string GeneratedDir { get; init; }
}
