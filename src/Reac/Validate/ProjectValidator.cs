using Reac.Ir;
using Reac.Layout;

namespace Reac.Validate;

public sealed class ValidationIssue
{
  public required bool IsError { get; init; }
  public required string Message { get; init; }
}

public static class ProjectValidator
{
  public static IReadOnlyList<ValidationIssue> Validate(ProjectIr project, int pointerSizeBytes)
  {
    var issues = new List<ValidationIssue>();
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    var bitfieldNames = project.BitfieldTypes.Select(b => b.Name).ToHashSet(StringComparer.Ordinal);

    foreach (var g in project.Types.GroupBy(t => t.Name).Where(g => g.Count() > 1))
      issues.Add(new ValidationIssue { IsError = true, Message = $"Duplicate type name: {g.Key}" });

    foreach (var bn in bitfieldNames)
    {
      if (typeMap.ContainsKey(bn))
        issues.Add(
          new ValidationIssue
          {
            IsError = true,
            Message = $"Name '{bn}' is used both as a class/struct type and as a bitfield type",
          }
        );
    }

    foreach (var t in project.Types)
    {
      if (HasInheritanceCycle(t, typeMap))
        issues.Add(
          new ValidationIssue
          {
            IsError = true,
            Message = $"Inheritance cycle involving '{t.Name}'",
          }
        );

      if (t.DeclaredSize == null && t.Size <= 0)
        issues.Add(
          new ValidationIssue
          {
            IsError = false,
            Message =
              $"Type '{t.Name}': size could not be inferred (add explicit size or resolve field types)",
          }
        );

      if (t.ParentName != null)
      {
        if (!typeMap.TryGetValue(t.ParentName, out var parent))
        {
          issues.Add(
            new ValidationIssue
            {
              IsError = false,
              Message =
                $"Type '{t.Name}': parent '{t.ParentName}' not found — parent-boundary check indeterminate",
            }
          );
        }
        else if (parent.Size <= 0)
        {
          issues.Add(
            new ValidationIssue
            {
              IsError = false,
              Message =
                $"Type '{t.Name}': parent '{t.ParentName}' has unknown size — boundary indeterminate",
            }
          );
        }
        else
        {
          foreach (var f in t.OwnFields)
          {
            if (f.Offset < parent.Size)
              issues.Add(
                new ValidationIssue
                {
                  IsError = true,
                  Message =
                    $"Type '{t.Name}': field '{f.Name}' at 0x{f.Offset:X} is before parent size 0x{parent.Size:X}",
                }
              );
          }
        }
      }

      var ownOffsets = new HashSet<int>();
      foreach (var f in t.OwnFields)
      {
        if (!ownOffsets.Add(f.Offset))
          issues.Add(
            new ValidationIssue
            {
              IsError = true,
              Message = $"Type '{t.Name}': duplicate offset 0x{f.Offset:X} in own fields",
            }
          );
        CollectUnresolved(f.Type, t.Name, f.Name, typeMap, issues);

        if (f.FlagBits is { Count: > 0 } flagBits)
        {
          var maxBit = MaxBitIndexForFlagField(f.Type, pointerSizeBytes);
          if (maxBit is null)
          {
            issues.Add(
              new ValidationIssue
              {
                IsError = true,
                Message =
                  $"Type '{t.Name}': field '{f.Name}' has FlagBits but resolved scalar type has no fixed size for bit range",
              }
            );
          }
          else
          {
            var seenBits = new HashSet<int>();
            foreach (var fb in flagBits)
            {
              if (fb.Bit < 0 || fb.Bit > maxBit.Value)
                issues.Add(
                  new ValidationIssue
                  {
                    IsError = true,
                    Message =
                      $"Type '{t.Name}': field '{f.Name}' FlagBits bit {fb.Bit} out of range (0..{maxBit})",
                  }
                );
              if (!seenBits.Add(fb.Bit))
                issues.Add(
                  new ValidationIssue
                  {
                    IsError = true,
                    Message =
                      $"Type '{t.Name}': field '{f.Name}' FlagBits duplicate bit index {fb.Bit}",
                  }
                );
            }
          }
        }
      }

      var ownFnAddr = new HashSet<int>();
      foreach (var fn in t.OwnFunctions)
      {
        if (!ownFnAddr.Add(fn.Address))
          issues.Add(
            new ValidationIssue
            {
              IsError = false,
              Message =
                $"Type '{t.Name}': duplicate function address 0x{fn.Address:X} in native function list",
            }
          );
      }
    }

    var layouts = LayoutEngine.BuildLayouts(project, pointerSizeBytes);
    foreach (var t in project.Types)
    {
      var layout = layouts[t.Name];
      var intervals = new List<(int start, int end, string name)>();
      foreach (var ff in layout.Flattened)
      {
        var (indet, sz) = FieldSizer.TryGetSpan(ff.Type, typeMap, pointerSizeBytes);
        if (indet || sz is null)
        {
          issues.Add(
            new ValidationIssue
            {
              IsError = false,
              Message =
                $"Layout indeterminate for field '{ff.Name}' in '{ff.DeclaringTypeName}' (type span unknown)",
            }
          );
          continue;
        }

        var end = ff.Offset + sz.Value;
        intervals.Add((ff.Offset, end, ff.Name));
      }

      intervals.Sort((a, b) => a.start.CompareTo(b.start));
      for (var i = 1; i < intervals.Count; i++)
      {
        var prev = intervals[i - 1];
        var cur = intervals[i];
        if (cur.start < prev.end)
          issues.Add(
            new ValidationIssue
            {
              IsError = true,
              Message =
                $"Overlap in '{t.Name}': field '{cur.name}' at 0x{cur.start:X} overlaps with previous span ending 0x{prev.end:X}",
            }
          );
      }
    }

    foreach (var d in project.Documents)
    {
      foreach (var r in d.References)
      {
        if (!typeMap.ContainsKey(r) && !bitfieldNames.Contains(r))
          issues.Add(
            new ValidationIssue
            {
              IsError = true,
              Message =
                $"Document '{d.Id}': ref '{r}' not found (no type or bitfield with that name)",
            }
          );
      }
    }

    return issues;
  }

  private static bool HasInheritanceCycle(TypeDecl start, IReadOnlyDictionary<string, TypeDecl> map)
  {
    var visited = new HashSet<string>(StringComparer.Ordinal);
    var cur = start.Name;
    while (true)
    {
      if (!visited.Add(cur))
        return true;
      if (!map.TryGetValue(cur, out var decl) || decl.ParentName == null)
        return false;
      cur = decl.ParentName;
    }
  }

  /// <returns>Max valid bit index for the scalar storage, or null if unknown.</returns>
  private static int? MaxBitIndexForFlagField(TypeExpr type, int pointerSizeBytes)
  {
    if (type is not TypeExpr.Scalar s)
      return null;
    return FieldSizer.MaxBitIndexForScalarStorage(s.Name, pointerSizeBytes);
  }

  private static void CollectUnresolved(
    TypeExpr expr,
    string typeName,
    string fieldName,
    IReadOnlyDictionary<string, TypeDecl> map,
    List<ValidationIssue> issues
  )
  {
    switch (expr)
    {
      case TypeExpr.Named n:
        if (!map.ContainsKey(n.Name))
          issues.Add(
            new ValidationIssue
            {
              IsError = false,
              Message = $"Unresolved type '{n.Name}' referenced from '{typeName}.{fieldName}'",
            }
          );
        break;
      case TypeExpr.Pointer p:
        CollectUnresolved(p.Inner, typeName, fieldName, map, issues);
        break;
      case TypeExpr.Array a:
        CollectUnresolved(a.Element, typeName, fieldName, map, issues);
        break;
      case TypeExpr.Scalar:
        break;
    }
  }
}
