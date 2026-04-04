using System.Text;
using Reac.Ir;
using Reac.Layout;

namespace Reac.Export;

public static class HtmlExporter
{
    public static void Export(ProjectIr project, string outDir, int pointerSizeBytes)
    {
        Directory.CreateDirectory(outDir);
        var layouts = LayoutEngine.BuildLayouts(project, pointerSizeBytes);
        var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);

        var indexSb = new StringBuilder();
        indexSb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>REaC</title>");
        indexSb.AppendLine(
            "<style>body{font-family:system-ui,Segoe UI,sans-serif;margin:1rem;} nav{float:left;width:220px;} main{margin-left:240px;} table{border-collapse:collapse;} td,th{border:1px solid #ccc;padding:4px 8px;} .prov{font-size:0.85rem;color:#444;}</style>");
        indexSb.AppendLine("</head><body>");
        indexSb.AppendLine("<nav><h3>Types</h3><ul>");
        foreach (var t in project.Types.OrderBy(x => x.Name))
            indexSb.AppendLine(
                $"<li><a href=\"type/{EscapeFile(t.Name)}.html\">{System.Net.WebUtility.HtmlEncode(t.Name)}</a></li>");
        indexSb.AppendLine("</ul><h3>Bitfield types</h3><ul>");
        foreach (var b in project.BitfieldTypes.OrderBy(x => x.Name))
            indexSb.AppendLine(
                $"<li><a href=\"bitfield/{EscapeFile(b.Name)}.html\">{System.Net.WebUtility.HtmlEncode(b.Name)}</a></li>");
        indexSb.AppendLine("</ul><h3>Docs</h3><ul>");
        foreach (var d in project.Documents.OrderBy(x => x.Id))
            indexSb.AppendLine(
                $"<li><a href=\"doc/{EscapeFile(d.Id)}.html\">{System.Net.WebUtility.HtmlEncode(d.Title)}</a></li>");
        indexSb.AppendLine("</ul></nav><main><h1>REaC</h1><p>Generated static site (knowledge base export).</p></main></body></html>");
        File.WriteAllText(Path.Combine(outDir, "index.html"), indexSb.ToString(), Encoding.UTF8);

        var typeDir = Path.Combine(outDir, "type");
        Directory.CreateDirectory(typeDir);
        foreach (var t in project.Types)
        {
            var layout = layouts[t.Name];
            var unresolved = new List<string>();
            foreach (var f in layout.Flattened)
                CollectUnresolvedNames(f.Type, typeMap, unresolved);

            var html = RenderTypePage(t, layout, unresolved.Distinct().ToList(), typeMap);
            File.WriteAllText(Path.Combine(typeDir, EscapeFile(t.Name) + ".html"), html, Encoding.UTF8);
        }

        var docDir = Path.Combine(outDir, "doc");
        Directory.CreateDirectory(docDir);
        foreach (var d in project.Documents)
        {
            var html = RenderDocPage(d, project);
            File.WriteAllText(Path.Combine(docDir, EscapeFile(d.Id) + ".html"), html, Encoding.UTF8);
        }

        var bitfieldDir = Path.Combine(outDir, "bitfield");
        Directory.CreateDirectory(bitfieldDir);
        foreach (var b in project.BitfieldTypes)
        {
            var html = RenderBitfieldPage(b);
            File.WriteAllText(Path.Combine(bitfieldDir, EscapeFile(b.Name) + ".html"), html, Encoding.UTF8);
        }
    }

    private static string EscapeFile(string name) =>
        name.Replace('<', '_').Replace('>', '_').Replace(':', '_').Replace('*', '_');

    private static void CollectUnresolvedNames(TypeExpr expr, IReadOnlyDictionary<string, TypeDecl> map, List<string> list)
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

    private static string RenderTypePage(TypeDecl t, TypeLayout layout, IReadOnlyList<string> unresolved,
        IReadOnlyDictionary<string, TypeDecl> typeMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>" +
                      System.Net.WebUtility.HtmlEncode(t.Name) + "</title>");
        sb.AppendLine(
            "<style>body{font-family:system-ui;margin:1rem;} table{border-collapse:collapse;} td,th{border:1px solid #ccc;padding:4px 8px;} .prov{font-size:0.85rem;color:#444;} details.ancestor{margin:0.75rem 0;padding:0.25rem 0.5rem;border:1px solid #ddd;border-radius:4px;} details.ancestor summary{cursor:pointer;font-weight:600;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<p><a href=\"../index.html\">Index</a></p>");
        sb.AppendLine("<h1>" + System.Net.WebUtility.HtmlEncode(t.Name) + " <small>(" +
                      System.Net.WebUtility.HtmlEncode(t.Kind.ToString().ToLowerInvariant()) + ")</small></h1>");
        var sizeText = t.Size > 0 ? $"0x{t.Size:X}" : "(unknown)";
        var declaredNote = t.DeclaredSize.HasValue ? "" : " <span class=\"prov\">(inferred)</span>";
        sb.AppendLine(
            $"<p>Total size: {sizeText}{declaredNote} &nbsp; Parent: {System.Net.WebUtility.HtmlEncode(t.ParentName ?? "(none)")}</p>");
        sb.AppendLine("<h2>Inheritance chain</h2><p>" +
                      System.Net.WebUtility.HtmlEncode(string.Join(" \u2192 ", layout.InheritanceChain)) + "</p>");

        var chain = layout.InheritanceChain;
        if (chain.Count > 1)
        {
            sb.AppendLine("<h2>Ancestor types</h2>");
            for (var i = 0; i < chain.Count - 1; i++)
            {
                var ancName = chain[i];
                if (!typeMap.TryGetValue(ancName, out var anc))
                    continue;
                sb.AppendLine($"<details class=\"ancestor\"><summary>Base: {System.Net.WebUtility.HtmlEncode(ancName)} " +
                              $"(<a href=\"{EscapeFile(ancName)}.html\">page</a>)</summary>");
                sb.AppendLine(
                    "<table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>");
                foreach (var f in anc.OwnFields)
                {
                    var n = FormatFieldNoteCell(f.Note, f.FlagBits);
                    var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, depth: 1);
                    sb.AppendLine(
                        $"<tr><td>0x{f.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{n}</td></tr>");
                }

                sb.AppendLine("</tbody></table></details>");
            }
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
                if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"<li><a href=\"{enc}\">{enc}</a></li>");
                else
                    sb.AppendLine("<li>" + enc + "</li>");
            }

        sb.AppendLine("</ul>");
        sb.AppendLine("</div>");

        sb.AppendLine(
            "<h2>Own fields</h2><table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note</th></tr></thead><tbody>");
        foreach (var f in layout.OwnFields)
        {
            var noteCell = FormatFieldNoteCell(f.Note, f.FlagBits);
            var typeCell = FieldTypeHtml(f.Type, f.BitfieldTypeName, depth: 1);
            sb.AppendLine(
                $"<tr><td>0x{f.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(f.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteCell}</td></tr>");
        }

        sb.AppendLine("</tbody></table>");

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
                "<table><thead><tr><th>Address</th><th>Name</th><th>Parameters</th><th>Returns</th><th>Note</th></tr></thead><tbody>");
            foreach (var fn in declForFn.OwnFunctions)
            {
                var retCell = string.IsNullOrEmpty(fn.ReturnType)
                    ? ""
                    : System.Net.WebUtility.HtmlEncode(fn.ReturnType);
                var noteFn = string.IsNullOrEmpty(fn.Note) ? "" : System.Net.WebUtility.HtmlEncode(fn.Note);
                sb.AppendLine(
                    $"<tr><td>0x{fn.Address:X}</td><td>{System.Net.WebUtility.HtmlEncode(fn.Name)}</td><td>{System.Net.WebUtility.HtmlEncode(fn.Parameters)}</td><td>{retCell}</td><td class=\"prov\">{noteFn}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        if (!hasAnyFn)
            sb.AppendLine("<p class=\"prov\">(none declared)</p>");

        sb.AppendLine(
            "<h2>Flattened layout</h2><table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Declaring</th><th>Layout</th></tr></thead><tbody>");
        foreach (var ff in layout.Flattened)
        {
            var typeCell = FieldTypeHtml(ff.Type, ff.BitfieldTypeName, depth: 1);
            sb.AppendLine(
                $"<tr><td>0x{ff.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(ff.Name)}</td><td>{typeCell}</td><td>{System.Net.WebUtility.HtmlEncode(ff.DeclaringTypeName)}</td><td>{(ff.Indeterminate ? "indeterminate" : "")}</td></tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h2>Grouped by declaring type</h2>");
        foreach (var g in layout.Flattened.GroupBy(x => x.DeclaringTypeName))
        {
            sb.AppendLine("<h3>" + System.Net.WebUtility.HtmlEncode(g.Key) + "</h3>");
            sb.AppendLine("<table><thead><tr><th>Off</th><th>Name</th><th>Type</th><th>Note / bits</th></tr></thead><tbody>");
            foreach (var ff in g)
            {
                var noteBits = FormatFieldNoteCell(ff.Note, ff.FlagBits);
                var typeCell = FieldTypeHtml(ff.Type, ff.BitfieldTypeName, depth: 1);
                sb.AppendLine(
                    $"<tr><td>0x{ff.Offset:X}</td><td>{System.Net.WebUtility.HtmlEncode(ff.Name)}</td><td>{typeCell}</td><td class=\"prov\">{noteBits}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<h2>Unresolved references</h2><ul>");
        if (unresolved.Count == 0)
            sb.AppendLine("<li>(none)</li>");
        else
            foreach (var u in unresolved)
                sb.AppendLine("<li>" + System.Net.WebUtility.HtmlEncode(u) + "</li>");
        sb.AppendLine("</ul></body></html>");
        return sb.ToString();
    }

    private static string FormatFieldNoteCell(string? note, IReadOnlyList<FlagBitDecl>? bits)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(note))
            parts.Add(System.Net.WebUtility.HtmlEncode(note));
        if (bits is { Count: > 0 })
        {
            var ul = new StringBuilder("<ul class=\"prov\">");
            foreach (var b in bits.OrderBy(x => x.Bit))
                ul.Append("<li>bit ").Append(b.Bit).Append(": ")
                    .Append(System.Net.WebUtility.HtmlEncode(b.Name)).Append("</li>");
            ul.Append("</ul>");
            parts.Add(ul.ToString());
        }

        return string.Join("", parts);
    }

    private static string TypeString(TypeExpr e) => e switch
    {
        TypeExpr.Scalar s => s.Name,
        TypeExpr.Named n => n.Name,
        TypeExpr.Pointer p => TypeString(p.Inner) + "*",
        TypeExpr.Array a => $"{TypeString(a.Element)}[{a.Length}]",
        _ => "?"
    };

    /// <param name="depth">1 = page under type/ (link to ../bitfield/…)</param>
    private static string FieldTypeHtml(TypeExpr t, string? bitfieldTypeName, int depth)
    {
        if (bitfieldTypeName != null && t is TypeExpr.Scalar s)
        {
            var href = depth == 1
                ? $"../bitfield/{EscapeFile(bitfieldTypeName)}.html"
                : $"bitfield/{EscapeFile(bitfieldTypeName)}.html";
            var encBf = System.Net.WebUtility.HtmlEncode(bitfieldTypeName);
            var encSc = System.Net.WebUtility.HtmlEncode(s.Name);
            return $"<a href=\"{href}\">{encBf}</a> <span class=\"prov\">({encSc})</span>";
        }

        return System.Net.WebUtility.HtmlEncode(TypeString(t));
    }

    private static string RenderBitfieldPage(BitfieldTypeDecl b)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>" +
                      System.Net.WebUtility.HtmlEncode(b.Name) + "</title>");
        sb.AppendLine(
            "<style>body{font-family:system-ui;margin:1rem;} table{border-collapse:collapse;} td,th{border:1px solid #ccc;padding:4px 8px;} .prov{font-size:0.85rem;color:#444;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<p><a href=\"../index.html\">Index</a></p>");
        sb.AppendLine("<h1>" + System.Net.WebUtility.HtmlEncode(b.Name) + " <small>(bitfield)</small></h1>");
        sb.AppendLine("<p>Storage: <code>" + System.Net.WebUtility.HtmlEncode(b.StorageName) + "</code></p>");
        if (!string.IsNullOrEmpty(b.Summary))
            sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(b.Summary) + "</p>");
        if (!string.IsNullOrEmpty(b.Note))
            sb.AppendLine("<p class=\"prov\">" + System.Net.WebUtility.HtmlEncode(b.Note) + "</p>");
        sb.AppendLine("<h2>Bits</h2><table><thead><tr><th>Bit</th><th>Name</th></tr></thead><tbody>");
        foreach (var x in b.Bits.OrderBy(x => x.Bit))
            sb.AppendLine(
                $"<tr><td>{x.Bit}</td><td>{System.Net.WebUtility.HtmlEncode(x.Name)}</td></tr>");
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
                if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"<li><a href=\"{enc}\">{enc}</a></li>");
                else
                    sb.AppendLine("<li>" + enc + "</li>");
            }

        sb.AppendLine("</ul></div></body></html>");
        return sb.ToString();
    }

    private static string RenderDocPage(DocumentDecl d, ProjectIr project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>" +
                      System.Net.WebUtility.HtmlEncode(d.Title) + "</title>");
        sb.AppendLine(
            "<style>body{font-family:system-ui;margin:1rem;} a{color:#06c;}</style></head><body>");
        sb.AppendLine("<p><a href=\"../index.html\">Index</a></p>");
        sb.AppendLine("<h1>" + System.Net.WebUtility.HtmlEncode(d.Title) + "</h1>");
        if (!string.IsNullOrEmpty(d.Summary))
            sb.AppendLine("<p>" + System.Net.WebUtility.HtmlEncode(d.Summary) + "</p>");
        var typeNames = project.Types.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var bitfieldNames = project.BitfieldTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        sb.AppendLine("<h2>References</h2><ul>");
        foreach (var r in d.References)
        {
            string href;
            if (typeNames.Contains(r))
                href = $"../type/{EscapeFile(r)}.html";
            else if (bitfieldNames.Contains(r))
                href = $"../bitfield/{EscapeFile(r)}.html";
            else
                href = "#";
            sb.AppendLine(
                $"<li><a href=\"{href}\">{System.Net.WebUtility.HtmlEncode(r)}</a></li>");
        }

        sb.AppendLine("</ul>");
        foreach (var s in d.Sections)
        {
            sb.AppendLine("<h2>" + System.Net.WebUtility.HtmlEncode(s.Name) + "</h2>");
            sb.AppendLine("<p>" + System.Net.WebUtility.HtmlEncode(s.Text) + "</p>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
