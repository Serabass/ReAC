using Reac;
using Reac.Dsl;
using Reac.Layout;

namespace Reac.Ir;

public static class ProjectLoader
{
    public static ProjectIr Load(string projectRoot)
    {
        var projectToml = Path.Combine(projectRoot, "project.toml");
        if (!File.Exists(projectToml))
            throw new FileNotFoundException("project.toml not found", projectToml);

        var cfg = ProjectMeta.Load(projectToml);
        var targetsDir = Path.Combine(projectRoot, cfg.TargetsDir);
        var modulesDir = Path.Combine(projectRoot, cfg.ModulesDir);
        var typesDir = Path.Combine(projectRoot, cfg.TypesDir);
        var docsDir = Path.Combine(projectRoot, cfg.DocsDir);

        var targets = new List<TargetDecl>();
        var modules = new List<ModuleDecl>();
        var types = new List<TypeDecl>();
        var documents = new List<DocumentDecl>();

        foreach (var f in EnumerateReFiles(targetsDir))
            Merge(targets, modules, types, f);
        foreach (var f in EnumerateReFiles(modulesDir))
            Merge(targets, modules, types, f);
        foreach (var f in EnumerateReFiles(typesDir))
            Merge(targets, modules, types, f);

        foreach (var f in Directory.Exists(docsDir) ? Directory.EnumerateFiles(docsDir, "*.rdoc") : Array.Empty<string>())
        {
            var text = File.ReadAllText(f);
            var doc = RdocDocumentParser.ParseDocument(text, f);
            documents.Add(doc);
        }

        var pointerSize = targets.FirstOrDefault(x =>
                string.Equals(x.Id, cfg.ActiveTarget, StringComparison.OrdinalIgnoreCase))
            ?.PointerSizeBytes ?? 4;
        var typesWithSizes = TypeSizeInference.Apply(types, pointerSize);

        return new ProjectIr
        {
            Config = cfg,
            ProjectRoot = Path.GetFullPath(projectRoot),
            Targets = targets,
            Modules = modules,
            Types = typesWithSizes,
            Documents = documents
        };
    }

    private static IEnumerable<string> EnumerateReFiles(string dir)
    {
        if (!Directory.Exists(dir))
            yield break;
        foreach (var f in Directory.EnumerateFiles(dir, "*.re", SearchOption.AllDirectories))
            yield return f;
    }

    private static void Merge(List<TargetDecl> targets, List<ModuleDecl> modules, List<TypeDecl> types, string file)
    {
        var text = File.ReadAllText(file);
        var tops = ReDocumentParser.ParseDocument(text);
        foreach (var t in tops)
        {
            switch (t)
            {
                case ReTopLevel.Target tg:
                    targets.Add(ToTarget(tg, file));
                    break;
                case ReTopLevel.Module m:
                    modules.Add(ToModule(m, file));
                    break;
                case ReTopLevel.TypeDef td:
                    types.Add(ToType(td, file));
                    break;
            }
        }
    }

    private static TargetDecl ToTarget(ReTopLevel.Target t, string file) =>
        new()
        {
            Id = t.Id,
            PointerSizeBytes = t.PointerSizeBytes,
            Game = t.Game,
            Version = t.Version,
            Platform = t.Platform,
            SourceUrl = t.SourceUrl,
            FilePath = file
        };

    private static ModuleDecl ToModule(ReTopLevel.Module m, string file) =>
        new()
        {
            Name = m.Name,
            Summary = m.Summary,
            Note = m.Note,
            FilePath = file
        };

    private static TypeDecl ToType(ReTopLevel.TypeDef td, string file)
    {
        var fieldNotes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in td.Body)
        {
            if (line is ReBodyLine.NoteFieldLine nf)
                fieldNotes[nf.FieldName] = nf.Text;
        }

        var fields = new List<FieldDecl>();
        foreach (var line in td.Body)
        {
            if (line is ReBodyLine.FieldLine fl)
            {
                var mergedNote = fl.Note ?? (fieldNotes.TryGetValue(fl.Name, out var nt) ? nt : null);
                fields.Add(new FieldDecl
                {
                    Offset = fl.Offset,
                    Name = fl.Name,
                    Type = fl.Type,
                    Note = mergedNote,
                    Provenance = null
                });
            }
        }

        string? src = null, sum = null, note = null, mod = null;
        foreach (var line in td.Body)
        {
            if (line is ReBodyLine.SourceLine s) src = s.Url;
            if (line is ReBodyLine.SummaryLine sm) sum = sm.Text;
            if (line is ReBodyLine.NoteEntityLine n) note = n.Text;
            if (line is ReBodyLine.ModuleLine ml) mod = ml.ModuleName;
        }

        return new TypeDecl
        {
            Name = td.Name,
            Kind = td.Kind,
            ParentName = td.Parent,
            DeclaredSize = td.DeclaredSize,
            Size = td.DeclaredSize ?? 0,
            ModuleName = mod,
            SourceUrl = src,
            Summary = sum,
            Note = note,
            Provenance = null,
            OwnFields = fields,
            FilePath = file
        };
    }
}
