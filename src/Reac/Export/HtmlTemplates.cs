using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Scriban;
using Scriban.Runtime;
using Reac.Ir;

namespace Reac.Export;

/// <summary>Embedded <see href="https://github.com/scriban/scriban">Scriban</see> templates for static HTML export.</summary>
internal static class HtmlTemplates
{
  private const string ResourcePrefix = "Reac.Export.Templates.";

  private static readonly ConcurrentDictionary<string, Template> Cache = new(StringComparer.Ordinal);

  private static Template Load(string fileName)
  {
    return Cache.GetOrAdd(
      fileName,
      fn =>
      {
        var asm = typeof(HtmlTemplates).Assembly;
        var fullName = ResourcePrefix + fn;
        using var stream =
          asm.GetManifestResourceStream(fullName)
          ?? throw new InvalidOperationException(
            $"Missing embedded template '{fullName}'. Known: {string.Join(", ", asm.GetManifestResourceNames())}"
          );
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Template.Parse(reader.ReadToEnd(), fn);
      }
    );
  }

  /// <summary>
  /// Scriban does not read nested CLR objects in <c>for x in Rows</c> — rows/cells render empty unless we pass
  /// nested <see cref="Dictionary{TKey,TValue}"/> / lists of dictionaries (same fix as sidebar type tree).
  /// </summary>
  private static void PushModelGlobalsDeep(TemplateContext ctx, object model)
  {
    var so = ToScriptObject(model);
    ctx.PushGlobal(so);
  }

  private static ScriptObject ToScriptObject(object model)
  {
    var root = ToScribanValue(model);
    if (root is not Dictionary<string, object> dict)
      throw new InvalidOperationException($"Expected root model to be a mapping, got {root?.GetType().FullName}");
    var so = new ScriptObject();
    foreach (var kv in dict)
      so[kv.Key] = kv.Value;
    return so;
  }

  /// <summary>Recursively convert POCOs/records to nested dictionaries and sequences to lists Scriban can index.</summary>
  private static object? ToScribanValue(object? v)
  {
    if (v == null)
      return "";

    switch (v)
    {
      case string s:
        return s;
      case bool:
      case byte:
      case sbyte:
      case short:
      case ushort:
      case int:
      case uint:
      case long:
      case ulong:
      case float:
      case double:
      case decimal:
        return v;
    }

    if (v.GetType().IsEnum)
      return Convert.ToInt32(v);

    if (v is Dictionary<string, object> existing)
      return existing;

    if (v is IDictionary<string, object> idict)
    {
      var map = new Dictionary<string, object>(StringComparer.Ordinal);
      foreach (DictionaryEntry e in (IDictionary)idict)
        map[(string)e.Key!] = ToScribanValue(e.Value)!;
      return map;
    }

    if (v is IEnumerable && v is not string)
    {
      var list = new List<object?>();
      foreach (var item in (IEnumerable)v)
        list.Add(ToScribanValue(item));
      return list;
    }

    return ToScribanDict(v);
  }

  private static Dictionary<string, object> ToScribanDict(object o)
  {
    var d = new Dictionary<string, object>(StringComparer.Ordinal);
    foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (!p.CanRead)
        continue;
      d[p.Name] = ToScribanValue(p.GetValue(o))!;
    }

    return d;
  }

  public static string RenderCommonStyles() => Load("styles.scriban").Render(new TemplateContext());

  public static string RenderLayout(
    string pageTitle,
    string stylesBlock,
    string sidebarHtml,
    string mainHtml,
    bool liveReload = false
  )
  {
    var t = Load("layout.scriban");
    var ctx = new TemplateContext();
    var globals = new ScriptObject
    {
      ["title"] = pageTitle,
      ["styles"] = stylesBlock,
      ["sidebar"] = sidebarHtml,
      ["main"] = mainHtml,
      ["live_reload"] = liveReload,
    };
    ctx.PushGlobal(globals);
    return t.Render(ctx);
  }

  public static string RenderSidebar(SidebarNavVm m)
  {
    var t = Load("sidebar.scriban");
    var ctx = new TemplateContext();
    PushModelGlobalsDeep(ctx, m);
    return t.Render(ctx);
  }

  public static string RenderIndexMain() => Load("index_main.scriban").Render(new TemplateContext());

  public static string RenderTypeMain(TypePageMainVm m)
  {
    var t = Load("type_page.scriban");
    var ctx = new TemplateContext();
    PushModelGlobalsDeep(ctx, m);
    return t.Render(ctx);
  }

  public static string RenderFieldNote(FieldNoteVm m)
  {
    var t = Load("field_note.scriban");
    var ctx = new TemplateContext();
    PushModelGlobalsDeep(ctx, m);
    return t.Render(ctx);
  }

  public static string RenderProvenance(string filePath, IReadOnlyList<string> sourceUrls)
  {
    var urls = sourceUrls
      .Select(u => new ProvenanceUrlVm(
        u,
        u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
          || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
      ))
      .ToList();
    var vm = new ProvenanceTemplateVm(filePath, urls.Count > 0, urls);
    var t = Load("provenance.scriban");
    var ctx = new TemplateContext();
    PushModelGlobalsDeep(ctx, vm);
    return t.Render(ctx);
  }

  public static string RenderBitfieldMain(BitfieldTypeDecl b, string provenanceHtml)
  {
    var t = Load("bitfield_page.scriban");
    var ctx = new TemplateContext();
    var bits = b
      .Bits.OrderBy(x => x.Bit)
      .Select(x => new BitRow(x.Bit, x.Name))
      .ToList();
    PushModelGlobalsDeep(
      ctx,
      new
      {
        name = b.Name,
        storage = b.StorageName,
        summary = b.Summary ?? "",
        note = b.Note ?? "",
        bits,
        provenance = provenanceHtml,
      }
    );
    return t.Render(ctx);
  }

  public static string RenderEnumMain(EnumTypeDecl e, string provenanceHtml)
  {
    var t = Load("enum_page.scriban");
    var ctx = new TemplateContext();
    var values = e
      .Values.OrderBy(x => x.Value)
      .Select(x => new EnumRow(x.Value, x.Name, x.Description ?? ""))
      .ToList();
    PushModelGlobalsDeep(
      ctx,
      new
      {
        name = e.Name,
        storage = e.StorageName,
        summary = e.Summary ?? "",
        note = e.Note ?? "",
        values,
        provenance = provenanceHtml,
      }
    );
    return t.Render(ctx);
  }

  public static string RenderDocMain(
    DocumentDecl d,
    IReadOnlyList<DocRefRow> references,
    IReadOnlyList<DocSectionRow> sections
  )
  {
    var t = Load("doc_page.scriban");
    var ctx = new TemplateContext();
    PushModelGlobalsDeep(
      ctx,
      new
      {
        title = d.Title,
        summary = d.Summary ?? "",
        references,
        sections,
      }
    );
    return t.Render(ctx);
  }

  private sealed record BitRow(int bit, string name);

  private sealed record EnumRow(ulong value, string name, string description);
}
