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

  public string? Description { get; init; }
}

public sealed class FieldDecl
{
  /// <summary>When false, byte offset in the instance. When true, <see cref="Offset"/> is unused (0).</summary>
  public bool IsStatic { get; init; }

  /// <summary>Absolute address in the module image (static/global); set when <see cref="IsStatic"/>.</summary>
  public ulong? StaticAddress { get; init; }

  public required int Offset { get; init; }
  public required string Name { get; init; }
  public required TypeExpr Type { get; init; }
  public string? Note { get; init; }
  public Provenance? Provenance { get; init; }

  /// <summary>Optional bit names for a scalar flag word (e.g. byte).</summary>
  public IReadOnlyList<FlagBitDecl>? FlagBits { get; init; }

  /// <summary>When field type was a named <c>bitfield</c> definition, its name (for docs).</summary>
  public string? BitfieldTypeName { get; init; }

  /// <summary>When field type was a named <c>enum</c> definition, its name (for docs).</summary>
  public string? EnumTypeName { get; init; }

  /// <summary>Enum members when field type resolved from a named <c>enum</c>.</summary>
  public IReadOnlyList<EnumValueDecl>? EnumValues { get; init; }
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

/// <summary>One enumerator with optional human-readable description.</summary>
public sealed class EnumValueDecl
{
  public required ulong Value { get; init; }
  public required string Name { get; init; }
  public string? Description { get; init; }
}

/// <summary>Top-level named enumeration (storage is one scalar; numeric values must fit).</summary>
public sealed class EnumTypeDecl
{
  public required string Name { get; init; }

  /// <summary>Underlying scalar name, e.g. byte or uint32.</summary>
  public required string StorageName { get; init; }
  public required IReadOnlyList<EnumValueDecl> Values { get; init; }
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

  /// <summary>Decorators from source lines <c>@name</c> immediately above this function.</summary>
  public IReadOnlyList<string> Decorators { get; init; } = Array.Empty<string>();
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

  /// <summary>Optional: verified at load time against <see cref="ExeSha256Hex"/>.</summary>
  public string? ExePath { get; init; }

  /// <summary>Optional: lowercase 64-char SHA-256 hex of the file at <see cref="ExePath"/>.</summary>
  public string? ExeSha256Hex { get; init; }

  /// <summary>Absolute path resolved from <see cref="ExePath"/> (set when @exe was declared).</summary>
  public string? ExeResolvedFullPath { get; init; }

  /// <summary>True when the exe file existed at load time (hash was verified in that case).</summary>
  public bool ExeFilePresent { get; init; }

  /// <summary>SHA-256 of the file on disk after a successful hash check; null if <see cref="ExeFilePresent"/> is false.</summary>
  public string? ExeActualSha256Hex { get; init; }

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
  public required IReadOnlyList<EnumTypeDecl> EnumTypes { get; init; }
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

  /// <summary>Predefined preprocessor names (for <c>#ifdef</c>); values used only for macro expansion.</summary>
  public IReadOnlyDictionary<string, string> PredefinedMacros { get; init; } =
    new Dictionary<string, string>(StringComparer.Ordinal);
}
