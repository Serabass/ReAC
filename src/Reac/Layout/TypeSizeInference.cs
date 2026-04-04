using Reac.Ir;

namespace Reac.Layout;

/// <summary>Fills <see cref="TypeDecl.Size"/> when <see cref="TypeDecl.DeclaredSize"/> is omitted.</summary>
public static class TypeSizeInference
{
    public static IReadOnlyList<TypeDecl> Apply(IReadOnlyList<TypeDecl> types, int pointerSizeBytes)
    {
        var typeMap = types.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var effective = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var t in types)
        {
            if (t.DeclaredSize is { } ds && ds > 0)
                effective[t.Name] = ds;
        }

        var maxRounds = Math.Max(types.Count + 2, 8);
        for (var round = 0; round < maxRounds; round++)
        {
            var changed = false;
            foreach (var t in types)
            {
                if (effective.ContainsKey(t.Name))
                    continue;
                if (!TryComputeExtent(t, typeMap, effective, pointerSizeBytes, out var extent) || extent <= 0)
                    continue;
                effective[t.Name] = extent;
                changed = true;
            }

            if (!changed)
                break;
        }

        return types.Select(t =>
        {
            var sz = effective.TryGetValue(t.Name, out var e) ? e : 0;
            if (sz <= 0 && t.DeclaredSize is { } d && d > 0)
                sz = d;
            return CloneWithSize(t, sz);
        }).ToList();
    }

    private static bool TryComputeExtent(
        TypeDecl t,
        IReadOnlyDictionary<string, TypeDecl> typeMap,
        Dictionary<string, int> effectiveSizes,
        int pointerSizeBytes,
        out int extent)
    {
        extent = 0;
        if (!typeMap.TryGetValue(t.Name, out _))
            return false;

        var chain = LayoutEngine.GetInheritanceChain(t, typeMap);
        var maxEnd = 0;
        foreach (var typeName in chain)
        {
            var decl = typeMap[typeName];
            foreach (var f in decl.OwnFields)
            {
                var (indet, sp) =
                    FieldSizer.TryGetSpanWithEffectiveSizes(f.Type, typeMap, effectiveSizes, pointerSizeBytes);
                if (indet || sp is null)
                    return false;
                maxEnd = Math.Max(maxEnd, f.Offset + sp.Value);
            }
        }

        extent = maxEnd;
        return true;
    }

    private static TypeDecl CloneWithSize(TypeDecl t, int size) =>
        new()
        {
            Name = t.Name,
            Kind = t.Kind,
            ParentName = t.ParentName,
            DeclaredSize = t.DeclaredSize,
            Size = size,
            ModuleName = t.ModuleName,
            SourceUrl = t.SourceUrl,
            Summary = t.Summary,
            Note = t.Note,
            Provenance = t.Provenance,
            OwnFields = t.OwnFields,
            OwnFunctions = t.OwnFunctions,
            FilePath = t.FilePath
        };
}
