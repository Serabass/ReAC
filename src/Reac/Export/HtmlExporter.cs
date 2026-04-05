using System.Text;
using Reac.Ir;
using Reac.Layout;

namespace Reac.Export;

public static class HtmlExporter
{
  /// <summary>Shared CSS for exported static site (readable tables, sidebar index, responsive).</summary>
  private const string StylesCommon = """
<style>
:root { color-scheme: light dark; }
*, *::before, *::after { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  line-height: 1.55; color: #1a1a1a; background: #e9ecef; }
a { color: #0b57d0; text-decoration: none; }
a:hover { text-decoration: underline; }
code { font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace;
  font-size: 0.88em; background: #f1f3f5; padding: 0.12em 0.45em; border-radius: 4px; }
table { width: 100%; border-collapse: collapse; font-size: 0.92rem; margin: 0.5rem 0 1.35rem; }
th, td { border: 1px solid #cfd6dd; padding: 0.45rem 0.65rem; vertical-align: top; }
th { background: #e9eef4; font-weight: 600; text-align: left; }
tbody tr:nth-child(even) { background: #fafbfc; }
tr.static-field td { background: #eef2f8; }
.prov { font-size: 0.88rem; color: #5c6570; }
h1 { font-size: 1.65rem; font-weight: 600; margin: 0 0 1rem; line-height: 1.25; }
h2 { font-size: 1.15rem; margin: 1.75rem 0 0.75rem; padding-bottom: 0.35rem; border-bottom: 1px solid #dee3e9; }
h3 { font-size: 1.05rem; margin: 1.25rem 0 0.5rem; }
.breadcrumb { margin: 0 0 1.25rem; font-size: 0.95rem; }
.breadcrumb a { color: #0b57d0; }
/* Index: sidebar + main */
.layout { display: flex; max-width: 1280px; margin: 0 auto; min-height: 100vh; background: #fff;
  box-shadow: 0 0 0 1px #cfd6dd; }
nav.sidebar { width: 260px; flex-shrink: 0; padding: 1.25rem 1rem; background: #f8f9fa;
  border-right: 1px solid #dee3e9; position: sticky; top: 0; align-self: flex-start; max-height: 100vh; overflow: auto; }
nav.sidebar h3 { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: #6c757d;
  margin: 1.25rem 0 0.5rem; font-weight: 700; }
nav.sidebar h3:first-child { margin-top: 0; }
nav.sidebar ul { list-style: none; padding: 0; margin: 0 0 1rem; }
nav.sidebar ul.nav-types-root { padding-left: 0; margin-bottom: 1rem; }
nav.sidebar li { margin: 0.2rem 0; word-break: break-word; }
nav.sidebar ul.nav-tree { list-style: none; margin: 0.3rem 0 0.4rem 0; padding: 0 0 0 0.85rem;
  border-left: 1px solid #dee3e9; }
nav.sidebar ul.nav-tree > li { margin: 0.18rem 0; }
main.index-main { flex: 1; padding: 1.75rem 2rem; min-width: 0; }
main.index-main > p.lead { color: #5c6570; margin-top: 0; max-width: 52ch; }
main.page-main { flex: 1; min-width: 0; background: #fff; }
main.page-main > .page { box-shadow: none; min-height: 0; }
nav.sidebar p.sidebar-home { margin: 0 0 1rem; font-size: 0.95rem; font-weight: 600; }
nav.sidebar li.nav-current a { font-weight: 600; color: #063a91; }
/* Inner pages */
.page { max-width: 920px; margin: 0 auto; padding: 1.5rem 1.35rem 2.75rem; background: #fff;
  min-height: 100vh; box-shadow: 0 0 0 1px #cfd6dd; }
details.ancestor { margin: 0.75rem 0; border: 1px solid #dee3e9; border-radius: 8px; padding: 0.5rem 0.85rem; background: #fafbfc; }
details.ancestor summary { cursor: pointer; font-weight: 600; }
@media (max-width: 800px) {
  .layout { flex-direction: column; }
  nav.sidebar { width: 100%; position: relative; max-height: none; border-right: none; border-bottom: 1px solid #dee3e9; }
}
</style>
""";

  public static void Export(ProjectIr project, string outDir, int pointerSizeBytes)
  {
    Directory.CreateDirectory(outDir);
    var layouts = LayoutEngine.BuildLayouts(project, pointerSizeBytes);
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);

    var indexSb = new StringBuilder();
    indexSb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
    indexSb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    indexSb.AppendLine("<title>REaC — Knowledge base</title>");
    indexSb.AppendLine(StylesCommon);
    indexSb.AppendLine("</head><body>");
    indexSb.AppendLine("<div class=\"layout\">");
    indexSb.AppendLine(BuildSidebarNav(project, "", null, null));
    indexSb.AppendLine("<main class=\"index-main\"><h1>REaC</h1>");
    indexSb.AppendLine(
      "<p class=\"lead\">Reverse-engineering knowledge base — types, bitfields, enums, and documents exported as static HTML.</p></main></div></body></html>"
    );
    File.WriteAllText(Path.Combine(outDir, "index.html"), indexSb.ToString(), Encoding.UTF8);

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
        BuildSidebarNav(project, "../", "type", t.Name)
      );
      File.WriteAllText(Path.Combine(typeDir, EscapeFile(t.Name) + ".html"), html, Encoding.UTF8);
    }

    var docDir = Path.Combine(outDir, "doc");
    Directory.CreateDirectory(docDir);
    foreach (var d in project.Documents)
    {
      var html = RenderDocPage(d, project, BuildSidebarNav(project, "../", "doc", d.Id));
      File.WriteAllText(Path.Combine(docDir, EscapeFile(d.Id) + ".html"), html, Encoding.UTF8);
    }

    var bitfieldDir = Path.Combine(outDir, "bitfield");
    Directory.CreateDirectory(bitfieldDir);
    foreach (var b in project.BitfieldTypes)
    {
      var html = RenderBitfieldPage(b, BuildSidebarNav(project, "../", "bitfield", b.Name));
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
      var html = RenderEnumPage(e, BuildSidebarNav(project, "../", "enum", e.Name));
      File.WriteAllText(Path.Combine(enumDir, EscapeFile(e.Name) + ".html"), html, Encoding.UTF8);
    }
  }

  private static string EscapeFile(string name) =>
    name.Replace('<', '_').Replace('>', '_').Replace(':', '_').Replace('*', '_');

  /// <summary>Left navigation: types, bitfields, enums, docs. Use <paramref name="hrefPrefix"/> <c>""</c> for index, <c>"../"</c> for pages in subfolders.</summary>
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

    string Li(string kind, string id, string label, string href)
    {
      var cur = IsCurrent(kind, id);
      var cls = cur ? " class=\"nav-current\"" : "";
      var aria = cur ? " aria-current=\"page\"" : "";
      var enc = System.Net.WebUtility.HtmlEncode(label);
      return $"<li{cls}><a href=\"{System.Net.WebUtility.HtmlEncode(href)}\"{aria}>{enc}</a></li>";
    }

    var sb = new StringBuilder();
    sb.AppendLine("<nav class=\"sidebar\" aria-label=\"Site\">");
    sb.AppendLine(
      $"<p class=\"sidebar-home\"><a href=\"{System.Net.WebUtility.HtmlEncode($"{hrefPrefix}index.html")}\">REaC</a></p>"
    );
    sb.AppendLine("<h3>Types</h3><ul class=\"nav-types-root\">");
    AppendTypeInheritanceTree(sb, project, hrefPrefix, highlightKind, highlightId);
    sb.AppendLine("</ul><h3>Bitfield types</h3><ul>");
    foreach (var b in project.BitfieldTypes.OrderBy(x => x.Name))
      sb.AppendLine(
        Li("bitfield", b.Name, b.Name, $"{hrefPrefix}bitfield/{EscapeFile(b.Name)}.html")
      );
    sb.AppendLine("</ul><h3>Enum types</h3><ul>");
    foreach (var e in project.EnumTypes.OrderBy(x => x.Name))
      sb.AppendLine(Li("enum", e.Name, e.Name, $"{hrefPrefix}enum/{EscapeFile(e.Name)}.html"));
    sb.AppendLine("</ul><h3>Docs</h3><ul>");
    foreach (var d in project.Documents.OrderBy(x => x.Id))
      sb.AppendLine(Li("doc", d.Id, d.Title, $"{hrefPrefix}doc/{EscapeFile(d.Id)}.html"));
    sb.AppendLine("</ul></nav>");
    return sb.ToString();
  }

  /// <summary>Nested <c>ul/li</c> by <see cref="TypeDecl.ParentName"/>; roots = no parent or parent not in this project.</summary>
  private static void AppendTypeInheritanceTree(
    StringBuilder sb,
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

    foreach (var root in roots)
    {
      var visiting = new HashSet<string>(StringComparer.Ordinal);
      AppendTypeTreeNode(
        sb,
        hrefPrefix,
        highlightKind,
        highlightId,
        childrenByParent,
        typeMap,
        root,
        visiting
      );
    }
  }

  private static void AppendTypeTreeNode(
    StringBuilder sb,
    string hrefPrefix,
    string? highlightKind,
    string? highlightId,
    Dictionary<string, List<string>> childrenByParent,
    IReadOnlyDictionary<string, TypeDecl> typeMap,
    string typeName,
    HashSet<string> visiting
  )
  {
    if (!visiting.Add(typeName))
    {
      AppendTypeTreeLinkLine(sb, hrefPrefix, highlightKind, highlightId, typeName);
      return;
    }

    try
    {
      if (!typeMap.ContainsKey(typeName))
        return;

      var kids = childrenByParent.TryGetValue(typeName, out var list) ? list : null;
      var href = $"{hrefPrefix}type/{EscapeFile(typeName)}.html";
      var cur =
        highlightKind != null
        && string.Equals(highlightKind, "type", StringComparison.OrdinalIgnoreCase)
        && string.Equals(highlightId, typeName, StringComparison.Ordinal);
      var cls = cur ? " class=\"nav-current\"" : "";
      var aria = cur ? " aria-current=\"page\"" : "";
      var encName = System.Net.WebUtility.HtmlEncode(typeName);
      var encHref = System.Net.WebUtility.HtmlEncode(href);

      if (kids == null || kids.Count == 0)
      {
        sb.AppendLine($"<li{cls}><a href=\"{encHref}\"{aria}>{encName}</a></li>");
        return;
      }

      sb.AppendLine($"<li{cls}><a href=\"{encHref}\"{aria}>{encName}</a>");
      sb.AppendLine("<ul class=\"nav-tree\">");
      foreach (var child in kids)
      {
        AppendTypeTreeNode(
          sb,
          hrefPrefix,
          highlightKind,
          highlightId,
          childrenByParent,
          typeMap,
          child,
          visiting
        );
      }

      sb.AppendLine("</ul></li>");
    }
    finally
    {
      visiting.Remove(typeName);
    }
  }

  private static void AppendTypeTreeLinkLine(
    StringBuilder sb,
    string hrefPrefix,
    string? highlightKind,
    string? highlightId,
    string typeName
  )
  {
    var href = $"{hrefPrefix}type/{EscapeFile(typeName)}.html";
    var cur =
      highlightKind != null
      && string.Equals(highlightKind, "type", StringComparison.OrdinalIgnoreCase)
      && string.Equals(highlightId, typeName, StringComparison.Ordinal);
    var cls = cur ? " class=\"nav-current\"" : "";
    var aria = cur ? " aria-current=\"page\"" : "";
    sb.AppendLine(
      $"<li{cls}><a href=\"{System.Net.WebUtility.HtmlEncode(href)}\"{aria}>{System.Net.WebUtility.HtmlEncode(typeName)}</a></li>"
    );
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
    }
  }

  private static string RenderTypePage(
    TypeDecl t,
    TypeLayout layout,
    IReadOnlyList<string> unresolved,
    IReadOnlyDictionary<string, TypeDecl> typeMap,
    string sidebarNavHtml
  )
  {
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
    sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    sb.AppendLine("<title>" + System.Net.WebUtility.HtmlEncode(t.Name) + " — REaC</title>");
    sb.AppendLine(StylesCommon);
    sb.AppendLine("</head><body>");
    sb.AppendLine("<div class=\"layout\">");
    sb.AppendLine(sidebarNavHtml);
    sb.AppendLine("<main class=\"page-main\"><div class=\"page\">");
    sb.AppendLine("<p class=\"breadcrumb\"><a href=\"../index.html\">Index</a></p>");
    sb.AppendLine(
      "<h1>"
        + System.Net.WebUtility.HtmlEncode(t.Name)
        + " <small>("
        + System.Net.WebUtility.HtmlEncode(t.Kind.ToString().ToLowerInvariant())
        + ")</small></h1>"
    );
    var sizeText = t.Size > 0 ? $"0x{t.Size:X}" : "(unknown)";
    var declaredNote = t.DeclaredSize.HasValue ? "" : " <span class=\"prov\">(inferred)</span>";
    sb.AppendLine(
      $"<p>Total size: {sizeText}{declaredNote} &nbsp; Parent: {System.Net.WebUtility.HtmlEncode(t.ParentName ?? "(none)")}</p>"
    );
    sb.AppendLine(
      "<h2>Inheritance chain</h2><p>"
        + System.Net.WebUtility.HtmlEncode(string.Join(" \u2192 ", layout.InheritanceChain))
        + "</p>"
    );

    var chain = layout.InheritanceChain;
    if (chain.Count > 1)
    {
      sb.AppendLine("<h2>Ancestor types</h2>");
      for (var i = 0; i < chain.Count - 1; i++)
      {
        var ancName = chain[i];
        if (!typeMap.TryGetValue(ancName, out var anc))
          continue;
        sb.AppendLine(
          $"<details class=\"ancestor\"><summary>Base: {System.Net.WebUtility.HtmlEncode(ancName)} "
            + $"(<a href=\"{EscapeFile(ancName)}.html\">page</a>)</summary>"
        );
        sb.AppendLine(
          "<table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>"
        );
        foreach (var f in anc.OwnFields.Where(x => !x.IsStatic))
        {
          var n = FormatFieldNoteCell(f.Note, f.FlagBits, f.EnumValues);
          var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1);
          sb.AppendLine(
            $"<tr><td>0x{f.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{n}</td></tr>"
          );
        }

        sb.AppendLine("</tbody></table>");
        var ancStatics = anc.OwnFields.Where(x => x.IsStatic).ToList();
        if (ancStatics.Count > 0)
        {
          sb.AppendLine("<p class=\"prov\"><strong>Static</strong> (module address)</p>");
          sb.AppendLine(
            "<table><thead><tr><th>Address</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>"
          );
          foreach (var f in ancStatics)
          {
            var n = FormatFieldNoteCell(f.Note, f.FlagBits, f.EnumValues);
            var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1);
            var addr = f.StaticAddress ?? 0;
            sb.AppendLine(
              $"<tr><td>0x{addr:X}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{n}</td></tr>"
            );
          }

          sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</details>");
      }
    }

    sb.AppendLine(
      "<h2>Own fields</h2><table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>"
    );
    foreach (var f in layout.OwnFields.Where(x => !x.IsStatic))
    {
      var noteCell = FormatFieldNoteCell(f.Note, f.FlagBits, f.EnumValues);
      var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1);
      sb.AppendLine(
        $"<tr><td>0x{f.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteCell}</td></tr>"
      );
    }

    sb.AppendLine("</tbody></table>");

    var staticInHierarchy = chain
      .Select(tn => typeMap.TryGetValue(tn, out var d) ? d : null)
      .Where(d => d != null)
      .SelectMany(d => d!.OwnFields.Where(f => f.IsStatic))
      .ToList();
    if (staticInHierarchy.Count > 0)
    {
      sb.AppendLine("<h2 id=\"static-fields\">Static fields</h2>");
      sb.AppendLine(
        "<p class=\"prov\">Absolute addresses in the module image (globals / singletons), not offsets inside an instance.</p>"
      );
      sb.AppendLine(
        "<table><thead><tr><th>Address</th><th>Declaring type</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>"
      );
      foreach (var typeName in chain)
      {
        if (!typeMap.TryGetValue(typeName, out var decl))
          continue;
        foreach (var f in decl.OwnFields.Where(x => x.IsStatic))
        {
          var noteCell = FormatFieldNoteCell(f.Note, f.FlagBits, f.EnumValues);
          var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, f.EnumTypeName, depth: 1);
          var addr = f.StaticAddress ?? 0;
          sb.AppendLine(
            $"<tr><td>0x{addr:X}</td><td>{System.Net.WebUtility.HtmlEncode(typeName)}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteCell}</td></tr>"
          );
        }
      }

      sb.AppendLine("</tbody></table>");
    }

    sb.AppendLine("<h2>Provenance</h2><div class=\"prov\">");
    sb.AppendLine("<div>File: " + System.Net.WebUtility.HtmlEncode(t.FilePath) + "</div>");
    sb.AppendLine("<div>Sources:</div><ul class=\"prov\">");
    if (t.SourceUrls.Count == 0)
      sb.AppendLine("<li>(none)</li>");
    else
      foreach (var u in t.SourceUrls)
      {
        var enc = System.Net.WebUtility.HtmlEncode(u);
        if (
          u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
          || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
          sb.AppendLine($"<li><a href=\"{enc}\" target=\"_blank\">{enc}</a></li>");
        else
          sb.AppendLine("<li>" + enc + "</li>");
      }

    sb.AppendLine("</ul>");
    sb.AppendLine("</div>");

    sb.AppendLine("<h2>Native functions</h2>");
    var hasAnyFn = false;
    foreach (var typeName in chain)
    {
      if (!typeMap.TryGetValue(typeName, out var declForFn))
        continue;
      if (declForFn.OwnFunctions.Count == 0)
        continue;
      hasAnyFn = true;
      sb.AppendLine("<h3>" + System.Net.WebUtility.HtmlEncode(typeName) + "</h3>");
      sb.AppendLine(
        "<table><thead><tr><th>Address</th><th>Name</th><th>Parameters</th><th>Returns</th><th>Note</th></tr></thead><tbody>"
      );
      foreach (var fn in declForFn.OwnFunctions)
      {
        var retCell = string.IsNullOrEmpty(fn.ReturnType)
          ? ""
          : System.Net.WebUtility.HtmlEncode(fn.ReturnType);
        var noteFn = string.IsNullOrEmpty(fn.Note) ? "" : System.Net.WebUtility.HtmlEncode(fn.Note);
        sb.AppendLine(
          $"<tr><td>0x{fn.Address:X}</td><td>{System.Net.WebUtility.HtmlEncode(fn.Name)}</td><td>{System.Net.WebUtility.HtmlEncode(fn.Parameters)}</td><td>{retCell}</td><td class=\"prov\">{noteFn}</td></tr>"
        );
      }

      sb.AppendLine("</tbody></table>");
    }

    if (!hasAnyFn)
      sb.AppendLine("<p class=\"prov\">(none declared)</p>");

    sb.AppendLine(
      "<h2>Flattened layout</h2><p class=\"prov\">Instance members by offset; static globals from the same inheritance chain are appended below.</p>"
    );
    sb.AppendLine(
      "<table><thead><tr><th>Off / address</th><th>Name</th><th>Type</th><th>Declaring</th><th>Layout</th></tr></thead><tbody>"
    );
    foreach (var ff in layout.Flattened)
    {
      var typeCell = FieldTypeHtml(ff.Type, ff.BitfieldTypeName, ff.EnumTypeName, depth: 1);
      sb.AppendLine(
        $"<tr><td>0x{ff.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(ff.Name)}</td><td>{typeCell}</td><td>{System.Net.WebUtility.HtmlEncode(ff.DeclaringTypeName)}</td><td>{(ff.Indeterminate ? "indeterminate" : "")}</td></tr>"
      );
    }

    foreach (var typeName in chain)
    {
      if (!typeMap.TryGetValue(typeName, out var declSt))
        continue;
      foreach (var sf in declSt.OwnFields.Where(x => x.IsStatic))
      {
        var typeCell = FieldTypeHtml(sf.Type, sf.BitfieldTypeName, sf.EnumTypeName, depth: 1);
        var addr = sf.StaticAddress ?? 0;
        sb.AppendLine(
          $"<tr class=\"static-field\"><td><code>static 0x{addr:X}</code></td><td>{System.Net.WebUtility.HtmlEncode(sf.Name)}</td><td>{typeCell}</td><td>{System.Net.WebUtility.HtmlEncode(typeName)}</td><td class=\"prov\">module global</td></tr>"
        );
      }
    }

    sb.AppendLine("</tbody></table>");

    sb.AppendLine("<h2>Grouped by declaring type</h2>");
    foreach (var g in layout.Flattened.GroupBy(x => x.DeclaringTypeName))
    {
      sb.AppendLine("<h3>" + System.Net.WebUtility.HtmlEncode(g.Key) + "</h3>");
      sb.AppendLine(
        "<table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note / bits</th></tr></thead><tbody>"
      );
      foreach (var ff in g)
      {
        var noteBits = FormatFieldNoteCell(ff.Note, ff.FlagBits, ff.EnumValues);
        var typeCell = FieldTypeHtml(ff.Type, ff.BitfieldTypeName, ff.EnumTypeName, depth: 1);
        sb.AppendLine(
          $"<tr><td>0x{ff.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(ff.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteBits}</td></tr>"
        );
      }

      if (typeMap.TryGetValue(g.Key, out var declForGroup))
      {
        foreach (var sf in declForGroup.OwnFields.Where(x => x.IsStatic))
        {
          var noteBits = FormatFieldNoteCell(sf.Note, sf.FlagBits, sf.EnumValues);
          var typeCell = FieldTypeHtml(sf.Type, sf.BitfieldTypeName, sf.EnumTypeName, depth: 1);
          var addr = sf.StaticAddress ?? 0;
          sb.AppendLine(
            $"<tr><td class=\"prov\">static 0x{addr:X}</td><td>{System.Net.WebUtility.HtmlEncode(sf.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteBits}</td></tr>"
          );
        }
      }

      sb.AppendLine("</tbody></table>");
    }

    sb.AppendLine("<h2>Unresolved references</h2><ul>");
    if (unresolved.Count == 0)
      sb.AppendLine("<li>(none)</li>");
    else
      foreach (var u in unresolved)
        sb.AppendLine("<li>" + System.Net.WebUtility.HtmlEncode(u) + "</li>");
    sb.AppendLine("</ul></div></main></div></body></html>");
    return sb.ToString();
  }

  private static string FormatFieldNoteCell(
    string? note,
    IReadOnlyList<FlagBitDecl>? bits,
    IReadOnlyList<EnumValueDecl>? enumVals
  )
  {
    var parts = new List<string>();
    if (!string.IsNullOrEmpty(note))
      parts.Add(System.Net.WebUtility.HtmlEncode(note));
    if (bits is { Count: > 0 })
    {
      var ul = new StringBuilder("<ul class=\"prov\">");
      foreach (var b in bits.OrderBy(x => x.Bit))
        ul.Append("<li>bit ")
          .Append(b.Bit)
          .Append(": ")
          .Append(System.Net.WebUtility.HtmlEncode(b.Name))
          .Append("</li>");
      ul.Append("</ul>");
      parts.Add(ul.ToString());
    }

    if (enumVals is { Count: > 0 })
    {
      var ul = new StringBuilder("<ul class=\"prov\">");
      foreach (var ev in enumVals.OrderBy(x => x.Value))
      {
        ul.Append("<li>")
          .Append(ev.Value)
          .Append(": ")
          .Append(System.Net.WebUtility.HtmlEncode(ev.Name));
        if (!string.IsNullOrEmpty(ev.Description))
          ul.Append(" — ").Append(System.Net.WebUtility.HtmlEncode(ev.Description));
        ul.Append("</li>");
      }

      ul.Append("</ul>");
      parts.Add(ul.ToString());
    }

    return string.Join("", parts);
  }

  private static string TypeString(TypeExpr e) =>
    e switch
    {
      TypeExpr.Scalar s => s.Name,
      TypeExpr.Named n => n.Name,
      TypeExpr.Pointer p => TypeString(p.Inner) + "*",
      TypeExpr.Array a => $"{TypeString(a.Element)}[{a.Length}]",
      _ => "?",
    };

  /// <param name="depth">1 = page under type/ (link to ../bitfield/… or ../enum/…)</param>
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

  private static string RenderBitfieldPage(BitfieldTypeDecl b, string sidebarNavHtml)
  {
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
    sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    sb.AppendLine("<title>" + System.Net.WebUtility.HtmlEncode(b.Name) + " — REaC</title>");
    sb.AppendLine(StylesCommon);
    sb.AppendLine("</head><body>");
    sb.AppendLine("<div class=\"layout\">");
    sb.AppendLine(sidebarNavHtml);
    sb.AppendLine("<main class=\"page-main\"><div class=\"page\">");
    sb.AppendLine("<p class=\"breadcrumb\"><a href=\"../index.html\">Index</a></p>");
    sb.AppendLine(
      "<h1>" + System.Net.WebUtility.HtmlEncode(b.Name) + " <small>(bitfield)</small></h1>"
    );
    sb.AppendLine(
      "<p>Storage: <code>" + System.Net.WebUtility.HtmlEncode(b.StorageName) + "</code></p>"
    );
    if (!string.IsNullOrEmpty(b.Summary))
      sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(b.Summary) + "</p>");
    if (!string.IsNullOrEmpty(b.Note))
      sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(b.Note) + "</p>");
    sb.AppendLine("<h2>Bits</h2><table><thead><tr><th>Bit</th><th>Name</th></tr></thead><tbody>");
    foreach (var x in b.Bits.OrderBy(x => x.Bit))
      sb.AppendLine(
        $"<tr><td>{x.Bit}</td><td>{System.Net.WebUtility.HtmlEncode(x.Name)}</td></tr>"
      );
    sb.AppendLine("</tbody></table>");
    sb.AppendLine("<h2>Provenance</h2><div class=\"prov\">");
    sb.AppendLine("<div>File: " + System.Net.WebUtility.HtmlEncode(b.FilePath) + "</div>");
    sb.AppendLine("<div>Sources:</div><ul class=\"prov\">");
    if (b.SourceUrls.Count == 0)
      sb.AppendLine("<li>(none)</li>");
    else
      foreach (var u in b.SourceUrls)
      {
        var enc = System.Net.WebUtility.HtmlEncode(u);
        if (
          u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
          || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
          sb.AppendLine($"<li><a href=\"{enc}\">{enc}</a></li>");
        else
          sb.AppendLine("<li>" + enc + "</li>");
      }

    sb.AppendLine("</ul></div></div></main></div></body></html>");
    return sb.ToString();
  }

  private static string RenderEnumPage(EnumTypeDecl e, string sidebarNavHtml)
  {
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
    sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    sb.AppendLine("<title>" + System.Net.WebUtility.HtmlEncode(e.Name) + " — REaC</title>");
    sb.AppendLine(StylesCommon);
    sb.AppendLine("</head><body>");
    sb.AppendLine("<div class=\"layout\">");
    sb.AppendLine(sidebarNavHtml);
    sb.AppendLine("<main class=\"page-main\"><div class=\"page\">");
    sb.AppendLine("<p class=\"breadcrumb\"><a href=\"../index.html\">Index</a></p>");
    sb.AppendLine(
      "<h1>" + System.Net.WebUtility.HtmlEncode(e.Name) + " <small>(enum)</small></h1>"
    );
    sb.AppendLine(
      "<p>Storage: <code>" + System.Net.WebUtility.HtmlEncode(e.StorageName) + "</code></p>"
    );
    if (!string.IsNullOrEmpty(e.Summary))
      sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(e.Summary) + "</p>");
    if (!string.IsNullOrEmpty(e.Note))
      sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(e.Note) + "</p>");
    sb.AppendLine(
      "<h2>Values</h2><table><thead><tr><th>Value</th><th>Name</th><th>Description</th></tr></thead><tbody>"
    );
    foreach (var x in e.Values.OrderBy(x => x.Value))
    {
      var desc = string.IsNullOrEmpty(x.Description)
        ? ""
        : System.Net.WebUtility.HtmlEncode(x.Description);
      sb.AppendLine(
        $"<tr><td>{x.Value}</td><td>{System.Net.WebUtility.HtmlEncode(x.Name)}</td><td class=\"prov\">{desc}</td></tr>"
      );
    }

    sb.AppendLine("</tbody></table>");
    sb.AppendLine("<h2>Provenance</h2><div class=\"prov\">");
    sb.AppendLine("<div>File: " + System.Net.WebUtility.HtmlEncode(e.FilePath) + "</div>");
    sb.AppendLine("<div>Sources:</div><ul class=\"prov\">");
    if (e.SourceUrls.Count == 0)
      sb.AppendLine("<li>(none)</li>");
    else
      foreach (var u in e.SourceUrls)
      {
        var enc = System.Net.WebUtility.HtmlEncode(u);
        if (
          u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
          || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
          sb.AppendLine($"<li><a href=\"{enc}\">{enc}</a></li>");
        else
          sb.AppendLine("<li>" + enc + "</li>");
      }

    sb.AppendLine("</ul></div></div></main></div></body></html>");
    return sb.ToString();
  }

  private static string RenderDocPage(DocumentDecl d, ProjectIr project, string sidebarNavHtml)
  {
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
    sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    sb.AppendLine("<title>" + System.Net.WebUtility.HtmlEncode(d.Title) + " — REaC</title>");
    sb.AppendLine(StylesCommon);
    sb.AppendLine("</head><body>");
    sb.AppendLine("<div class=\"layout\">");
    sb.AppendLine(sidebarNavHtml);
    sb.AppendLine("<main class=\"page-main\"><div class=\"page\">");
    sb.AppendLine("<p class=\"breadcrumb\"><a href=\"../index.html\">Index</a></p>");
    sb.AppendLine("<h1>" + System.Net.WebUtility.HtmlEncode(d.Title) + "</h1>");
    if (!string.IsNullOrEmpty(d.Summary))
      sb.AppendLine("<p>" + System.Net.WebUtility.HtmlEncode(d.Summary) + "</p>");
    var typeNames = project.Types.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
    var bitfieldNames = project.BitfieldTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
    var enumNames = project.EnumTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
    sb.AppendLine("<h2>References</h2><ul>");
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
      sb.AppendLine($"<li><a href=\"{href}\">{System.Net.WebUtility.HtmlEncode(r)}</a></li>");
    }

    sb.AppendLine("</ul>");
    foreach (var s in d.Sections)
    {
      sb.AppendLine("<h2>" + System.Net.WebUtility.HtmlEncode(s.Name) + "</h2>");
      sb.AppendLine("<p>" + System.Net.WebUtility.HtmlEncode(s.Text) + "</p>");
    }

    sb.AppendLine("</div></main></div></body></html>");
    return sb.ToString();
  }
}
