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

public sealed class TypeLayout
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> InheritanceChain { get; init; }
    public required IReadOnlyList<FlattenedField> Flattened { get; init; }
    public required IReadOnlyList<FieldDecl> OwnFields { get; init; }
}

public static class LayoutEngine
{
    public static IReadOnlyList<string> GetInheritanceChain(TypeDecl t, IReadOnlyDictionary<string, TypeDecl> typeMap)
    {
        var chain = new List<string>();
        TypeDecl? cur = t;
        while (cur != null)
        {
            chain.Insert(0, cur.Name);
            cur = cur.ParentName != null && typeMap.TryGetValue(cur.ParentName, out var p) ? p : null;
        }

        return chain;
    }

    public static IReadOnlyDictionary<string, TypeLayout> BuildLayouts(ProjectIr project, int pointerSizeBytes)
    {
        var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var result = new Dictionary<string, TypeLayout>(StringComparer.Ordinal);
        foreach (var t in project.Types)
            result[t.Name] = BuildOne(t, typeMap, pointerSizeBytes);
        return result;
    }

    private static TypeLayout BuildOne(TypeDecl t, Dictionary<string, TypeDecl> typeMap, int pointerSize)
    {
        var chain = GetInheritanceChain(t, typeMap).ToList();

        var flat = new List<FlattenedField>();
        foreach (var name in chain)
        {
            var decl = typeMap[name];
            foreach (var f in decl.OwnFields)
            {
                var (indet, _) = FieldSizer.TryGetSpan(f.Type, typeMap, pointerSize);
                flat.Add(new FlattenedField
                {
                    Offset = f.Offset,
                    Name = f.Name,
                    Type = f.Type,
                    DeclaringTypeName = name,
                    Indeterminate = indet,
                    Note = f.Note,
                    FlagBits = f.FlagBits,
                    BitfieldTypeName = f.BitfieldTypeName
                });
            }
        }

        flat.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        return new TypeLayout
        {
            Name = t.Name,
            InheritanceChain = chain,
            Flattened = flat,
            OwnFields = t.OwnFields.ToList()
        };
    }
}

public static class FieldSizer
{
    /// <summary>Uses only <paramref name="effectiveSizes"/> for named types (inference); missing or 0 = indeterminate.</summary>
    public static (bool Indeterminate, int? Size) TryGetSpanWithEffectiveSizes(
        TypeExpr expr,
        IReadOnlyDictionary<string, TypeDecl> types,
        IReadOnlyDictionary<string, int> effectiveSizes,
        int pointerSize)
    {
        switch (expr)
        {
            case TypeExpr.Scalar s:
            {
                var sc = SizeOfScalar(s.Name, pointerSize);
                return sc is null ? (true, null) : (false, sc);
            }
            case TypeExpr.Pointer:
                return (false, pointerSize);
            case TypeExpr.Named n:
                if (effectiveSizes.TryGetValue(n.Name, out var es) && es > 0)
                    return (false, es);
                return (true, null);
            case TypeExpr.Array ar:
            {
                var (innerI, innerS) = TryGetSpanWithEffectiveSizes(ar.Element, types, effectiveSizes, pointerSize);
                if (innerI || innerS is null)
                    return (true, null);
                return (false, innerS * ar.Length);
            }
            default:
                return (true, null);
        }
    }

    public static (bool Indeterminate, int? Size) TryGetSpan(TypeExpr expr, IReadOnlyDictionary<string, TypeDecl> types, int pointerSize)
    {
        switch (expr)
        {
            case TypeExpr.Scalar s:
                return (false, SizeOfScalar(s.Name, pointerSize));
            case TypeExpr.Pointer:
                return (false, pointerSize);
            case TypeExpr.Named n:
                if (!types.TryGetValue(n.Name, out var td) || td.Size <= 0)
                    return (true, null);
                return (false, td.Size);
            case TypeExpr.Array ar:
            {
                var (innerI, innerS) = TryGetSpan(ar.Element, types, pointerSize);
                if (innerI || innerS is null)
                    return (true, null);
                return (false, innerS * ar.Length);
            }
            default:
                return (true, null);
        }
    }

    private static int? SizeOfScalar(string name, int pointerSize)
    {
        var n = name.ToLowerInvariant();
        return n switch
        {
            "pointer" => pointerSize,
            "byte" or "uint8" or "char" or "bool" => 1,
            "uint16" or "word" => 2,
            "uint32" or "dword" or "int32" => 4,
            "uint64" or "int64" => 8,
            "float" => 4,
            "double" => 8,
            _ => null
        };
    }

    /// <summary>Max bit index (0-based) for a scalar of this name, or null if unknown or zero-sized.</summary>
    public static int? MaxBitIndexForScalarStorage(string scalarName, int pointerSizeBytes)
    {
        var sz = SizeOfScalar(scalarName, pointerSizeBytes);
        return sz is > 0 ? sz.Value * 8 - 1 : null;
    }
}
