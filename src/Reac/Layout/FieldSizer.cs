using Reac.Ir;

namespace Reac.Layout;

public static class FieldSizer
{
  /// <summary>Uses only <paramref name="effectiveSizes"/> for named types (inference); missing or 0 = indeterminate.</summary>
  public static (bool Indeterminate, int? Size) TryGetSpanWithEffectiveSizes(
    TypeExpr expr,
    IReadOnlyDictionary<string, TypeDecl> types,
    IReadOnlyDictionary<string, int> effectiveSizes,
    int pointerSize
  )
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
        var (innerI, innerS) = TryGetSpanWithEffectiveSizes(
          ar.Element,
          types,
          effectiveSizes,
          pointerSize
        );
        if (innerI || innerS is null)
          return (true, null);
        return (false, innerS * ar.Length);
      }
      default:
        return (true, null);
    }
  }

  public static (bool Indeterminate, int? Size) TryGetSpan(
    TypeExpr expr,
    IReadOnlyDictionary<string, TypeDecl> types,
    int pointerSize
  )
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
      _ => null,
    };
  }

  /// <summary>Max bit index (0-based) for a scalar of this name, or null if unknown or zero-sized.</summary>
  public static int? MaxBitIndexForScalarStorage(string scalarName, int pointerSizeBytes)
  {
    var sz = SizeOfScalar(scalarName, pointerSizeBytes);
    return sz is > 0 ? sz.Value * 8 - 1 : null;
  }

  /// <summary>Maximum unsigned integer representable in this scalar width (for enum value checks).</summary>
  public static ulong? MaxUnsignedValueForScalarStorage(string scalarName, int pointerSizeBytes)
  {
    var sz = SizeOfScalar(scalarName, pointerSizeBytes);
    return sz switch
    {
      1 => byte.MaxValue,
      2 => ushort.MaxValue,
      4 => uint.MaxValue,
      8 => ulong.MaxValue,
      _ => null,
    };
  }
}
