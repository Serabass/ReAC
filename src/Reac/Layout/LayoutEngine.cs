using Reac.Ir;

namespace Reac.Layout;

public static class LayoutEngine
{
  public static IReadOnlyList<string> GetInheritanceChain(
    TypeDecl t,
    IReadOnlyDictionary<string, TypeDecl> typeMap
  )
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

  public static IReadOnlyDictionary<string, TypeLayout> BuildLayouts(
    ProjectIr project,
    int pointerSizeBytes
  )
  {
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    var result = new Dictionary<string, TypeLayout>(StringComparer.Ordinal);
    foreach (var t in project.Types)
      result[t.Name] = BuildOne(t, typeMap, pointerSizeBytes);
    return result;
  }

  private static TypeLayout BuildOne(
    TypeDecl t,
    Dictionary<string, TypeDecl> typeMap,
    int pointerSize
  )
  {
    var chain = GetInheritanceChain(t, typeMap).ToList();

    var flat = new List<FlattenedField>();
    foreach (var name in chain)
    {
      var decl = typeMap[name];
      foreach (var f in decl.OwnFields)
      {
        if (f.IsStatic)
          continue;
        var (indet, _) = FieldSizer.TryGetSpan(f.Type, typeMap, pointerSize);
        flat.Add(
          new FlattenedField
          {
            Offset = f.Offset,
            Name = f.Name,
            Type = f.Type,
            DeclaringTypeName = name,
            Indeterminate = indet,
            Note = f.Note,
            FlagBits = f.FlagBits,
            BitfieldTypeName = f.BitfieldTypeName,
            EnumTypeName = f.EnumTypeName,
            EnumValues = f.EnumValues,
          }
        );
      }
    }

    flat.Sort((a, b) => a.Offset.CompareTo(b.Offset));

    return new TypeLayout
    {
      Name = t.Name,
      InheritanceChain = chain,
      Flattened = flat,
      OwnFields = t.OwnFields.ToList(),
    };
  }
}
