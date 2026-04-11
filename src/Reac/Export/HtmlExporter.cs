using System.Globalization;
using System.Text;
using Reac.Ir;
using Reac.Layout;

namespace Reac.Export;

public static class HtmlExporter
{
  public static void Export(ProjectIr project, string outDir, int pointerSizeBytes, bool liveReload = false)
  {
    Directory.CreateDirectory(outDir);
    var layouts = LayoutEngine.BuildLayouts(project, pointerSizeBytes);
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    var styles = HtmlTemplates.RenderCommonStyles();

    var indexHtml = HtmlTemplates.RenderLayout(
      "REaC — Knowledge base",
      styles,
      BuildSidebarNav(project, "", null, null),
      HtmlTemplates.RenderIndexMain(),
      liveReload
    );
    File.WriteAllText(Path.Combine(outDir, "index.html"), indexHtml, Encoding.UTF8);

    var typeDir = Path.Combine(outDir, "type");
    Directory.CreateDirectory(typeDir);
    foreach (var t in project.Types)
    {
      var layout = layouts[t.Name];
      var unresolved = new List<string>();
      foreach (var f in layout.Flattened)
        CollectUnresolvedNames(f.Type, typeMap, unresolved);

      var html = RenderTypePage(
        t,
        layout,
        unresolved.Distinct().ToList(),
        typeMap,
        BuildSidebarNav(project, "../", "type", t.Name),
        styles,
        liveReload
      );
      File.WriteAllText(Path.Combine(typeDir, EscapeFile(t.Name) + ".html"), html, Encoding.UTF8);
    }

    var docDir = Path.Combine(outDir, "doc");
    Directory.CreateDirectory(docDir);
    foreach (var d in project.Documents)
    {
      var html = RenderDocPage(
        d,
        project,
        BuildSidebarNav(project, "../", "doc", d.Id),
        styles,
        liveReload
      );
      File.WriteAllText(Path.Combine(docDir, EscapeFile(d.Id) + ".html"), html, Encoding.UTF8);
    }

    var bitfieldDir = Path.Combine(outDir, "bitfield");
    Directory.CreateDirectory(bitfieldDir);
    foreach (var b in project.BitfieldTypes)
    {
      var html = RenderBitfieldPage(
        b,
        BuildSidebarNav(project, "../", "bitfield", b.Name),
        styles,
        liveReload
      );
      File.WriteAllText(
        Path.Combine(bitfieldDir, EscapeFile(b.Name) + ".html"),
        html,
        Encoding.UTF8
      );
    }

    var enumDir = Path.Combine(outDir, "enum");
    Directory.CreateDirectory(enumDir);
    foreach (var e in project.EnumTypes)
    {
      var html = RenderEnumPage(
        e,
        BuildSidebarNav(project, "../", "enum", e.Name),
        styles,
        liveReload
      );
      File.WriteAllText(Path.Combine(enumDir, EscapeFile(e.Name) + ".html"), html, Encoding.UTF8);
    }

    var stampPath = Path.Combine(outDir, "buildstamp.txt");
    if (liveReload)
      File.WriteAllText(
        stampPath,
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        Encoding.UTF8
      );
    else if (File.Exists(stampPath))
      File.Delete(stampPath);
  }

  private static string EscapeFile(string name) =>
    name.Replace('<', '_').Replace('>', '_').Replace(':', '_').Replace('*', '_');

  private static string BuildSidebarNav(
    ProjectIr project,
    string hrefPrefix,
    string? highlightKind,
    string? highlightId
  )
  {
    bool IsCurrent(string kind, string id) =>
      highlightKind != null
      && string.Equals(highlightKind, kind, StringComparison.OrdinalIgnoreCase)
      && string.Equals(highlightId, id, StringComparison.Ordinal);

    var typeRoots = BuildTypeRoots(project, hrefPrefix, highlightKind, highlightId);
    var bitfields = project
      .BitfieldTypes.OrderBy(x => x.Name)
      .Select(b => new NavSidebarItemVm(
        $"{hrefPrefix}bitfield/{EscapeFile(b.Name)}.html",
        b.Name,
        IsCurrent("bitfield", b.Name)
      ))
      .ToList();
    var enums = project
      .EnumTypes.OrderBy(x => x.Name)
      .Select(e => new NavSidebarItemVm(
        $"{hrefPrefix}enum/{EscapeFile(e.Name)}.html",
        e.Name,
        IsCurrent("enum", e.Name)
      ))
      .ToList();
    var docs = project
      .Documents.OrderBy(x => x.Id)
      .Select(d => new NavSidebarItemVm(
        $"{hrefPrefix}doc/{EscapeFile(d.Id)}.html",
        d.Title,
        IsCurrent("doc", d.Id)
      ))
      .ToList();

    var typeRootDicts = typeRoots.Select(NavTypeNodeToDict).ToList();
    var vm = new SidebarNavVm($"{hrefPrefix}index.html", typeRootDicts, bitfields, enums, docs);
    return HtmlTemplates.RenderSidebar(vm);
  }

  private static Dictionary<string, object> NavTypeNodeToDict(NavTypeNode n)
  {
    var children = n.Children.Select(NavTypeNodeToDict).ToList<object>();
    return new Dictionary<string, object>(StringComparer.Ordinal)
    {
      ["Name"] = n.Name,
      ["Href"] = n.Href,
      ["Current"] = n.Current,
      ["HasChildren"] = n.HasChildren,
      ["Children"] = children,
    };
  }

  private static IReadOnlyList<NavTypeNode> BuildTypeRoots(
    ProjectIr project,
    string hrefPrefix,
    string? highlightKind,
    string? highlightId
  )
  {
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    var inProject = typeMap.Keys.ToHashSet(StringComparer.Ordinal);
    var childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var t in project.Types)
    {
      var p = t.ParentName;
      if (string.IsNullOrEmpty(p) || !inProject.Contains(p))
        continue;
      if (!childrenByParent.TryGetValue(p, out var list))
      {
        list = new List<string>();
        childrenByParent[p] = list;
      }

      list.Add(t.Name);
    }

    foreach (var kv in childrenByParent)
      kv.Value.Sort(StringComparer.OrdinalIgnoreCase);

    var roots = project
      .Types.Where(t => string.IsNullOrEmpty(t.ParentName) || !inProject.Contains(t.ParentName!))
      .Select(t => t.Name)
      .Distinct()
      .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
      .ToList();

    var result = new List<NavTypeNode>();
    foreach (var root in roots)
    {
      var visiting = new HashSet<string>(StringComparer.Ordinal);
      var node = BuildNavTypeNode(
        root,
        hrefPrefix,
        highlightKind,
        highlightId,
        childrenByParent,
        typeMap,
        visiting
      );
      if (node != null)
        result.Add(node);
    }

    return result;
  }

  private static NavTypeNode? BuildNavTypeNode(
    string typeName,
    string hrefPrefix,
    string? highlightKind,
    string? highlightId,
    Dictionary<string, List<string>> childrenByParent,
    IReadOnlyDictionary<string, TypeDecl> typeMap,
    HashSet<string> visiting
  )
  {
    var href = $"{hrefPrefix}type/{EscapeFile(typeName)}.html";
    var cur =
      highlightKind != null
      && string.Equals(highlightKind, "type", StringComparison.OrdinalIgnoreCase)
      && string.Equals(highlightId, typeName, StringComparison.Ordinal);

    if (!visiting.Add(typeName))
      return new NavTypeNode
      {
        Name = typeName,
        Href = href,
        Current = cur,
        Children = new List<NavTypeNode>(),
      };

    try
    {
      if (!typeMap.ContainsKey(typeName))
        return null;

      var kids = childrenByParent.TryGetValue(typeName, out var list) ? list : null;
      var node = new NavTypeNode
      {
        Name = typeName,
        Href = href,
        Current = cur,
        Children = new List<NavTypeNode>(),
      };
      if (kids == null)
        return node;

      foreach (var child in kids)
      {
        var ch = BuildNavTypeNode(
          child,
          hrefPrefix,
          highlightKind,
          highlightId,
          childrenByParent,
          typeMap,
          visiting
        );
        if (ch != null)
          node.Children.Add(ch);
      }

      return node;
    }
    finally
    {
      visiting.Remove(typeName);
    }
  }

  private static void CollectUnresolvedNames(
    TypeExpr expr,
    IReadOnlyDictionary<string, TypeDecl> map,
    List<string> list
  )
  {
    switch (expr)
    {
      case TypeExpr.Named n:
        if (!map.ContainsKey(n.Name))
          list.Add(n.Name);
        break;
      case TypeExpr.Pointer p:
        CollectUnresolvedNames(p.Inner, map, list);
        break;
      case TypeExpr.Array a:
        CollectUnresolvedNames(a.Element, map, list);
        break;
      case TypeExpr.Generic g:
        foreach (var a in g.TypeArguments)
          CollectUnresolvedNames(a, map, list);
        break;
    }
  }

  private static string RenderTypePage(
    TypeDecl t,
    TypeLayout layout,
    IReadOnlyList<string> unresolved,
    IReadOnlyDictionary<string, TypeDecl> typeMap,
    string sidebarNavHtml,
    string styles,
    bool liveReload
  )
  {
    var vm = BuildTypePageMainVm(t, layout, unresolved, typeMap);
    var main = HtmlTemplates.RenderTypeMain(vm);
    return HtmlTemplates.RenderLayout(t.Name + " — REaC", styles, sidebarNavHtml, main, liveReload);
  }

  private static TypePageMainVm BuildTypePageMainVm(
    TypeDecl t,
    TypeLayout layout,
    IReadOnlyList<string> unresolved,
    IReadOnlyDictionary<string, TypeDecl> typeMap
  )
  {
    var chain = layout.InheritanceChain;
    var ancestors = new List<TypeAncestorVm>();
    if (chain.Count > 1)
    {
      for (var i = 0; i < chain.Count - 1; i++)
      {
        var ancName = chain[i];
        if (!typeMap.TryGetValue(ancName, out var anc))
          continue;
        var instRows = new List<TableRow4Vm>();
        foreach (var f in anc.OwnFields.Where(x => !x.IsStatic))
        {
          instRows.Add(
            new TableRow4Vm(
              $"0x{f.Offset:X}",
              f.Name,
              FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1),
              FieldNoteHtml(f.Note, f.FlagBits, f.EnumValues)
            )
          );
        }

        var staticRowsA = new List<TableRow4Vm>();
        foreach (var f in anc.OwnFields.Where(x => x.IsStatic))
        {
          var addr = f.StaticAddress ?? 0;
          staticRowsA.Add(
            new TableRow4Vm(
              $"0x{addr:X}",
              f.Name,
              FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1),
              FieldNoteHtml(f.Note, f.FlagBits, f.EnumValues)
            )
          );
        }

        ancestors.Add(
          new TypeAncestorVm(
            ancName,
            $"{EscapeFile(ancName)}.html",
            instRows,
            staticRowsA,
            staticRowsA.Count > 0
          )
        );
      }
    }

    var ownFieldRows = new List<TableRow4Vm>();
    foreach (var f in layout.OwnFields.Where(x => !x.IsStatic))
    {
      ownFieldRows.Add(
        new TableRow4Vm(
          $"0x{f.Offset:X}",
          f.Name,
          FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1),
          FieldNoteHtml(f.Note, f.FlagBits, f.EnumValues)
        )
      );
    }

    var staticInHierarchy = chain
      .Select(tn => typeMap.TryGetValue(tn, out var d) ? d : null)
      .Where(d => d != null)
      .SelectMany(d => d!.OwnFields.Where(f => f.IsStatic))
      .ToList();

    var staticHierarchyRows = new List<TableRow5Vm>();
    if (staticInHierarchy.Count > 0)
    {
      foreach (var typeName in chain)
      {
        if (!typeMap.TryGetValue(typeName, out var decl))
          continue;
        foreach (var f in decl.OwnFields.Where(x => x.IsStatic))
        {
          var addr = f.StaticAddress ?? 0;
          staticHierarchyRows.Add(
            new TableRow5Vm(
              $"0x{addr:X}",
              typeName,
              f.Name,
              FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1),
              FieldNoteHtml(f.Note, f.FlagBits, f.EnumValues)
            )
          );
        }
      }
    }

    var provenanceHtml = HtmlTemplates.RenderProvenance(t.FilePath, t.SourceUrls);

    var nativeSections = new List<NativeFnSectionVm>();
    foreach (var typeName in chain)
    {
      if (!typeMap.TryGetValue(typeName, out var declForFn))
        continue;
      if (declForFn.OwnFunctions.Count == 0)
        continue;
      var rows = declForFn
        .OwnFunctions.Select(fn =>
        {
          var retPlain = string.IsNullOrEmpty(fn.ReturnType) ? "" : fn.ReturnType;
          var notePlain = string.IsNullOrEmpty(fn.Note) ? "" : fn.Note;
          var decPlain = fn.Decorators.Count == 0
            ? ""
            : string.Join(" ", fn.Decorators.Select(d => "@" + d));
          return new NativeFnRowVm(
            $"0x{fn.Address:X}",
            fn.Name,
            fn.Parameters,
            retPlain,
            notePlain,
            decPlain
          );
        })
        .ToList();
      nativeSections.Add(new NativeFnSectionVm(typeName, rows));
    }

    var hasAnyNativeFn = nativeSections.Count > 0;

    var flatRows = new List<FlatRowVm>();
    foreach (var ff in layout.Flattened)
    {
      flatRows.Add(
        new FlatRowVm(
          false,
          $"0x{ff.Offset:X}",
          ff.Name,
          FieldTypeHtml(ff.Type, ff.BitfieldTypeName, ff.EnumTypeName, depth: 1),
          ff.DeclaringTypeName,
          ff.Indeterminate ? "indeterminate" : ""
        )
      );
    }

    foreach (var typeName in chain)
    {
      if (!typeMap.TryGetValue(typeName, out var declSt))
        continue;
      foreach (var sf in declSt.OwnFields.Where(x => x.IsStatic))
      {
        var addr = sf.StaticAddress ?? 0;
        flatRows.Add(
          new FlatRowVm(
            true,
            $"<code>static 0x{addr:X}</code>",
            sf.Name,
            FieldTypeHtml(sf.Type, sf.BitfieldTypeName, sf.EnumTypeName, depth: 1),
            typeName,
            "module global"
          )
        );
      }
    }

    var groupedSections = new List<GroupedSectionVm>();
    foreach (var g in layout.Flattened.GroupBy(x => x.DeclaringTypeName))
    {
      var instRows = new List<TableRow4Vm>();
      foreach (var ff in g)
      {
        instRows.Add(
          new TableRow4Vm(
            $"0x{ff.Offset:X}",
            ff.Name,
            FieldTypeHtml(ff.Type, ff.BitfieldTypeName, ff.EnumTypeName, depth: 1),
            FieldNoteHtml(ff.Note, ff.FlagBits, ff.EnumValues)
          )
        );
      }

      var staticRowsG = new List<GroupStaticRowVm>();
      if (typeMap.TryGetValue(g.Key, out var declForGroup))
      {
        foreach (var sf in declForGroup.OwnFields.Where(x => x.IsStatic))
        {
          var addr = sf.StaticAddress ?? 0;
          staticRowsG.Add(
            new GroupStaticRowVm(
              $"static 0x{addr:X}",
              sf.Name,
              FieldTypeHtml(sf.Type, sf.BitfieldTypeName, sf.EnumTypeName, depth: 1),
              FieldNoteHtml(sf.Note, sf.FlagBits, sf.EnumValues)
            )
          );
        }
      }

      groupedSections.Add(new GroupedSectionVm(g.Key, instRows, staticRowsG));
    }

    return new TypePageMainVm(
      t.Name,
      t.Kind.ToString().ToLowerInvariant(),
      t.Size > 0 ? $"0x{t.Size:X}" : "(unknown)",
      !t.DeclaredSize.HasValue,
      t.ParentName ?? "(none)",
      string.Join(" \u2192 ", layout.InheritanceChain),
      ancestors.Count > 0,
      ancestors,
      ownFieldRows,
      staticInHierarchy.Count > 0,
      staticHierarchyRows,
      provenanceHtml,
      hasAnyNativeFn,
      nativeSections,
      flatRows,
      groupedSections,
      unresolved.Count == 0,
      unresolved
    );
  }

  private static string FieldNoteHtml(
    string? note,
    IReadOnlyList<FlagBitDecl>? bits,
    IReadOnlyList<EnumValueDecl>? enumVals
  ) => HtmlTemplates.RenderFieldNote(BuildFieldNoteVm(note, bits, enumVals));

  private static FieldNoteVm BuildFieldNoteVm(
    string? note,
    IReadOnlyList<FlagBitDecl>? bits,
    IReadOnlyList<EnumValueDecl>? enumVals
  )
  {
    var hasNote = !string.IsNullOrEmpty(note);
    var fb = bits is { Count: > 0 }
      ? bits
        .OrderBy(x => x.Bit)
        .Select(x => new FlagBitVm(x.Bit, x.Name, x.Description))
        .ToList()
      : (IReadOnlyList<FlagBitVm>)Array.Empty<FlagBitVm>();
    var ev = enumVals is { Count: > 0 }
      ? enumVals
        .OrderBy(x => x.Value)
        .Select(x => new EnumValVm(x.Value, x.Name, x.Description))
        .ToList()
      : (IReadOnlyList<EnumValVm>)Array.Empty<EnumValVm>();
    return new FieldNoteVm(hasNote, note, fb.Count > 0, fb, ev.Count > 0, ev);
  }

  private static string TypeString(TypeExpr e) =>
    e switch
    {
      TypeExpr.Scalar s => s.Name,
      TypeExpr.Named n => n.Name,
      TypeExpr.Pointer p => TypeString(p.Inner) + "*",
      TypeExpr.Array a => $"{TypeString(a.Element)}[{a.Length}]",
      TypeExpr.Generic g =>
        $"{g.Name}<{string.Join(", ", g.TypeArguments.Select(TypeString))}>",
      _ => "?",
    };

  private static string FieldTypeHtml(
    TypeExpr t,
    string? bitfieldTypeName,
    string? enumTypeName,
    int depth
  )
  {
    if (bitfieldTypeName != null && t is TypeExpr.Scalar s)
    {
      var href =
        depth == 1
          ? $"../bitfield/{EscapeFile(bitfieldTypeName)}.html"
          : $"bitfield/{EscapeFile(bitfieldTypeName)}.html";
      var encBf = System.Net.WebUtility.HtmlEncode(bitfieldTypeName);
      var encSc = System.Net.WebUtility.HtmlEncode(s.Name);
      return $"<a href=\"{href}\">{encBf}</a> <span class=\"prov\">({encSc})</span>";
    }

    if (enumTypeName != null && t is TypeExpr.Scalar sEn)
    {
      var href =
        depth == 1
          ? $"../enum/{EscapeFile(enumTypeName)}.html"
          : $"enum/{EscapeFile(enumTypeName)}.html";
      var encEn = System.Net.WebUtility.HtmlEncode(enumTypeName);
      var encSc = System.Net.WebUtility.HtmlEncode(sEn.Name);
      return $"<a href=\"{href}\">{encEn}</a> <span class=\"prov\">({encSc})</span>";
    }

    return System.Net.WebUtility.HtmlEncode(TypeString(t));
  }

  private static string RenderBitfieldPage(
    BitfieldTypeDecl b,
    string sidebarNavHtml,
    string styles,
    bool liveReload
  )
  {
    var main = HtmlTemplates.RenderBitfieldMain(
      b,
      HtmlTemplates.RenderProvenance(b.FilePath, b.SourceUrls)
    );
    return HtmlTemplates.RenderLayout(b.Name + " — REaC", styles, sidebarNavHtml, main, liveReload);
  }

  private static string RenderEnumPage(EnumTypeDecl e, string sidebarNavHtml, string styles, bool liveReload)
  {
    var main = HtmlTemplates.RenderEnumMain(
      e,
      HtmlTemplates.RenderProvenance(e.FilePath, e.SourceUrls)
    );
    return HtmlTemplates.RenderLayout(e.Name + " — REaC", styles, sidebarNavHtml, main, liveReload);
  }

  private static string RenderDocPage(
    DocumentDecl d,
    ProjectIr project,
    string sidebarNavHtml,
    string styles,
    bool liveReload
  )
  {
    var typeNames = project.Types.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
    var bitfieldNames = project.BitfieldTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
    var enumNames = project.EnumTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
    var refs = new List<DocRefRow>();
    foreach (var r in d.References)
    {
      string href;
      if (typeNames.Contains(r))
        href = $"../type/{EscapeFile(r)}.html";
      else if (bitfieldNames.Contains(r))
        href = $"../bitfield/{EscapeFile(r)}.html";
      else if (enumNames.Contains(r))
        href = $"../enum/{EscapeFile(r)}.html";
      else
        href = "#";
      refs.Add(new DocRefRow(href, r));
    }

    var sections = d.Sections.Select(s => new DocSectionRow(s.Name, s.Text)).ToList();
    var main = HtmlTemplates.RenderDocMain(d, refs, sections);
    return HtmlTemplates.RenderLayout(d.Title + " — REaC", styles, sidebarNavHtml, main, liveReload);
  }
}
