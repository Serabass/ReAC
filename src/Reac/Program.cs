using Reac.Export;
using Reac.Ir;
using Reac.Validate;
using System.CommandLine;

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

        var projectOpt = new Option<DirectoryInfo?>("--project", "Project root (default: search for project.toml)");
        projectOpt.AddAlias("-p");

        var validate = new Command("validate", "Validate knowledge base");
        validate.AddOption(projectOpt);
        validate.SetHandler(ValidateHandler, projectOpt);

        var exportHtml = new Command("export-html", "Export static HTML");
        exportHtml.AddOption(projectOpt);
        var outDir = new Option<DirectoryInfo?>("--out", "Output directory (default: generated/html)");
        outDir.AddAlias("-o");
        exportHtml.AddOption(outDir);
        exportHtml.SetHandler(ExportHtmlHandler, projectOpt, outDir);

        var build = new Command("build", "validate + export-html");
        build.AddOption(projectOpt);
        build.AddOption(outDir);
        build.SetHandler(BuildHandler, projectOpt, outDir);

        root.AddCommand(init);
        root.AddCommand(validate);
        root.AddCommand(exportHtml);
        root.AddCommand(build);

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

    private static void ExportHtmlHandler(DirectoryInfo? projectOpt, DirectoryInfo? outOpt)
    {
        var root = ResolveRoot(projectOpt);
        var ir = ProjectLoader.Load(root);
        var ps = ResolvePointerSize(ir);
        var outPath = outOpt?.FullName ?? Path.Combine(root, ir.Config.GeneratedDir, "html");
        HtmlExporter.Export(ir, outPath, ps);
        Console.WriteLine($"HTML written to {outPath}");
    }

    private static async Task BuildHandler(DirectoryInfo? projectOpt, DirectoryInfo? outOpt)
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
        HtmlExporter.Export(ir, outPath, ps);
        Console.WriteLine($"HTML written to {outPath}");
        await Task.CompletedTask;
    }

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
            string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase));
        if (t != null)
            return t.PointerSizeBytes;
        Console.Error.WriteLine("warning: active target not found; pointer size default 4");
        return 4;
    }
}
