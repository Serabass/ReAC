using System.CommandLine;
using System.Text;
using Reac.Dsl;
using Reac.Export;
using Reac.Ir;
using Reac.Validate;

namespace Reac;

internal static class Program
{
  public static async Task<int> Main(string[] args)
  {
    var root = new RootCommand("reac — Reverse Engineering as Code");

    var init = new Command("init", "Create KB scaffold");
    var initPath = new Argument<DirectoryInfo?>("path", "Directory (default: cwd)");
    init.AddArgument(initPath);
    init.SetHandler(InitHandler, initPath);

    var projectOpt = new Option<DirectoryInfo?>(
      "--project",
      "Project root (default: search for project.toml)"
    );
    projectOpt.AddAlias("-p");

    var validate = new Command("validate", "Validate knowledge base");
    validate.AddOption(projectOpt);
    validate.SetHandler(ValidateHandler, projectOpt);

    var exportHtml = new Command("export-html", "Export static HTML");
    exportHtml.AddOption(projectOpt);
    var outDir = new Option<DirectoryInfo?>("--out", "Output directory (default: generated/html)");
    outDir.AddAlias("-o");
    exportHtml.AddOption(outDir);
    var liveReload = new Option<bool>(
      "--live-reload",
      () => false,
      "Poll buildstamp and reload the page (use with watch + static server)"
    );
    exportHtml.AddOption(liveReload);
    exportHtml.SetHandler(ExportHtmlHandler, projectOpt, outDir, liveReload);

    var build = new Command("build", "validate + export-html");
    build.AddOption(projectOpt);
    build.AddOption(outDir);
    build.AddOption(liveReload);
    build.SetHandler(BuildHandler, projectOpt, outDir, liveReload);

    var formatConfigOpt = new Option<FileInfo?>(
      "--format-config",
      "Path to reac.format.toml (default: <project>/reac.format.toml if present)"
    );
    formatConfigOpt.AddAlias("-F");
    var formatCheck = new Option<bool>(
      "--check",
      () => false,
      "Exit with error if any .re would change (does not write)"
    );
    var format = new Command(
      "format",
      "Rewrite .re sources from AST (drops // line comments). Uses reac.format.toml when present."
    );
    format.AddOption(projectOpt);
    format.AddOption(formatConfigOpt);
    format.AddOption(formatCheck);
    format.SetHandler(FormatHandler, projectOpt, formatConfigOpt, formatCheck);

    root.AddCommand(init);
    root.AddCommand(validate);
    root.AddCommand(exportHtml);
    root.AddCommand(build);
    root.AddCommand(format);

    return await root.InvokeAsync(args);
  }

  private static void InitHandler(DirectoryInfo? path)
  {
    var dir = path?.FullName ?? Directory.GetCurrentDirectory();
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "project.toml"), InitFiles.ProjectToml);
    foreach (var (rel, content) in InitFiles.AllFiles)
    {
      var p = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
      Directory.CreateDirectory(Path.GetDirectoryName(p)!);
      if (!File.Exists(p))
        File.WriteAllText(p, content);
    }

    Console.WriteLine($"Initialized KB scaffold in {dir}");
  }

  private static void ValidateHandler(DirectoryInfo? projectOpt)
  {
    var root = ResolveRoot(projectOpt);
    var ir = ProjectLoader.Load(root);
    var ps = ResolvePointerSize(ir);
    var issues = ProjectValidator.Validate(ir, ps);
    foreach (var i in issues)
      Console.Error.WriteLine((i.IsError ? "error: " : "warning: ") + i.Message);
    var errors = issues.Count(x => x.IsError);
    if (errors > 0)
      Environment.Exit(1);
  }

  private static void ExportHtmlHandler(DirectoryInfo? projectOpt, DirectoryInfo? outOpt, bool liveReload)
  {
    var root = ResolveRoot(projectOpt);
    var ir = ProjectLoader.Load(root);
    var ps = ResolvePointerSize(ir);
    var outPath = outOpt?.FullName ?? Path.Combine(root, ir.Config.GeneratedDir, "html");
    HtmlExporter.Export(ir, outPath, ps, liveReload);
    Console.WriteLine($"HTML written to {outPath}");
  }

  private static async Task BuildHandler(DirectoryInfo? projectOpt, DirectoryInfo? outOpt, bool liveReload)
  {
    var root = ResolveRoot(projectOpt);
    var ir = ProjectLoader.Load(root);
    var ps = ResolvePointerSize(ir);
    var issues = ProjectValidator.Validate(ir, ps);
    foreach (var i in issues)
      Console.Error.WriteLine((i.IsError ? "error: " : "warning: ") + i.Message);
    if (issues.Any(x => x.IsError))
      Environment.Exit(1);
    var outPath = outOpt?.FullName ?? Path.Combine(root, ir.Config.GeneratedDir, "html");
    HtmlExporter.Export(ir, outPath, ps, liveReload);
    Console.WriteLine($"HTML written to {outPath}");
    await Task.CompletedTask;
  }

  private static async Task FormatHandler(
    DirectoryInfo? projectOpt,
    FileInfo? formatConfigOpt,
    bool check
  )
  {
    var root = ResolveRoot(projectOpt);
    var cfg = ProjectMeta.Load(Path.Combine(root, "project.toml"));
    var opts = FormatConfigLoader.LoadForProject(root, formatConfigOpt?.FullName);

    var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var path in ProjectLoader.EnumerateReSourceFiles(root, cfg))
      raw[path] = File.ReadAllText(path);
    var processed = RePreprocessor.ProcessAllFiles(raw, cfg.PredefinedMacros);

    var utf8NoBom = new UTF8Encoding(false);
    var changed = 0;
    foreach (var path in ProjectLoader.EnumerateReSourceFiles(root, cfg))
    {
      var tops = ReDocumentParser.ParseDocument(processed[path]);
      var formatted = ReDocumentFormatter.FormatDocument(tops, opts);
      var normalizedOut = NormalizeNewlines(formatted);
      if (check)
      {
        var disk = NormalizeNewlines(File.ReadAllText(path));
        if (normalizedOut != disk)
        {
          Console.Error.WriteLine(path);
          changed++;
        }
      }
      else
      {
        File.WriteAllText(path, normalizedOut, utf8NoBom);
      }
    }

    if (check && changed > 0)
      Environment.Exit(1);
    await Task.CompletedTask;
  }

  private static string NormalizeNewlines(string s) =>
    s.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

  private static string ResolveRoot(DirectoryInfo? projectOpt)
  {
    if (projectOpt != null)
      return Path.GetFullPath(projectOpt.FullName);
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "project.toml")))
        return dir.FullName;
      dir = dir.Parent;
    }

    throw new InvalidOperationException("project.toml not found; pass --project");
  }

  private static int ResolvePointerSize(ProjectIr ir)
  {
    var t = ir.Targets.FirstOrDefault(x =>
      string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase)
    );
    if (t != null)
      return t.PointerSizeBytes;
    Console.Error.WriteLine("warning: active target not found; pointer size default 4");
    return 4;
  }
}
