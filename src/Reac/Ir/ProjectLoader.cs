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

    var parsedFiles = new List<(string Path, IReadOnlyList<ReTopLevel> Tops)>();
    foreach (var f in EnumerateReFilesSorted(targetsDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(File.ReadAllText(f))));
    foreach (var f in EnumerateReFilesSorted(modulesDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(File.ReadAllText(f))));
    foreach (var f in EnumerateReFilesSorted(typesDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(File.ReadAllText(f))));

    var targets = new List<TargetDecl>();
    foreach (var (path, tops) in parsedFiles)
    {
      foreach (var t in tops)
      {
        if (t is ReTopLevel.Target tg)
          targets.Add(ToTarget(tg, path));
      }
    }

    var pointerSizeForBitfields =
      targets
        .FirstOrDefault(x =>
          string.Equals(x.Id, cfg.ActiveTarget, StringComparison.OrdinalIgnoreCase)
        )
        ?.PointerSizeBytes
      ?? 4;

    var bitfieldMap = new Dictionary<string, BitfieldTypeDecl>(StringComparer.Ordinal);
    foreach (var (path, tops) in parsedFiles)
    {
      foreach (var t in tops)
      {
        if (t is not ReTopLevel.BitfieldDef bf)
          continue;
        var decl = ToBitfieldDecl(bf, path, pointerSizeForBitfields);
        if (!bitfieldMap.TryAdd(decl.Name, decl))
          throw new InvalidOperationException(
            $"Duplicate bitfield type '{decl.Name}' (see {path} and existing)"
          );
      }
    }

    var modules = new List<ModuleDecl>();
    var types = new List<TypeDecl>();
    foreach (var (path, tops) in parsedFiles)
    {
      foreach (var t in tops)
      {
        switch (t)
        {
          case ReTopLevel.Target:
            break;
          case ReTopLevel.Module m:
            modules.Add(ToModule(m, path));
            break;
          case ReTopLevel.TypeDef td:
            types.Add(ToType(td, path, bitfieldMap));
            break;
          case ReTopLevel.BitfieldDef:
            break;
        }
      }
    }

    var documents = new List<DocumentDecl>();
    foreach (
      var f in Directory.Exists(docsDir)
        ? Directory
          .EnumerateFiles(docsDir, "*.rdoc")
          .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
          .ToArray()
        : Array.Empty<string>()
    )
    {
      var text = File.ReadAllText(f);
      documents.Add(RdocDocumentParser.ParseDocument(text, f));
    }

    var pointerSize = pointerSizeForBitfields;
    var typesWithSizes = TypeSizeInference.Apply(types, pointerSize);

    var bitfieldList = bitfieldMap
      .Values.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
      .ToList();

    return new ProjectIr
    {
      Config = cfg,
      ProjectRoot = Path.GetFullPath(projectRoot),
      Targets = targets,
      Modules = modules,
      Types = typesWithSizes,
      BitfieldTypes = bitfieldList,
      Documents = documents,
    };
  }

  private static IEnumerable<string> EnumerateReFilesSorted(string dir)
  {
    if (!Directory.Exists(dir))
      yield break;
    foreach (
      var f in Directory
        .EnumerateFiles(dir, "*.re", SearchOption.AllDirectories)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
    )
      yield return f;
  }

  private static BitfieldTypeDecl ToBitfieldDecl(
    ReTopLevel.BitfieldDef d,
    string file,
    int pointerSizeBytes
  )
  {
    var maxBit = FieldSizer.MaxBitIndexForScalarStorage(d.StorageName, pointerSizeBytes);
    if (maxBit is null)
      throw new InvalidOperationException(
        $"bitfield '{d.Name}': unsupported storage '{d.StorageName}' (use a fixed-size scalar: byte..uint64, float, double, pointer, etc.)"
      );

    var seenBits = new HashSet<int>();
    foreach (var (bit, _) in d.Bits)
    {
      if (bit < 0 || bit > maxBit.Value)
        throw new InvalidOperationException(
          $"bitfield '{d.Name}': bit index {bit} out of range for storage '{d.StorageName}'"
        );
      if (!seenBits.Add(bit))
        throw new InvalidOperationException($"bitfield '{d.Name}': duplicate bit index {bit}");
    }

    return new BitfieldTypeDecl
    {
      Name = d.Name,
      StorageName = d.StorageName,
      Bits = d.Bits.Select(b => new FlagBitDecl { Bit = b.Bit, Name = b.Name }).ToList(),
      SourceUrls = d.SourceUrls.ToList(),
      Summary = d.Summary,
      Note = d.Note,
      FilePath = file,
    };
  }

  private static TargetDecl ToTarget(ReTopLevel.Target t, string file) =>
    new()
    {
      Id = t.Id,
      PointerSizeBytes = t.PointerSizeBytes,
      Game = t.Game,
      Version = t.Version,
      Platform = t.Platform,
      SourceUrls = t.SourceUrls,
      FilePath = file,
    };

  private static ModuleDecl ToModule(ReTopLevel.Module m, string file) =>
    new()
    {
      Name = m.Name,
      Summary = m.Summary,
      Note = m.Note,
      FilePath = file,
    };

  private static TypeDecl ToType(
    ReTopLevel.TypeDef td,
    string file,
    IReadOnlyDictionary<string, BitfieldTypeDecl> bitfieldMap
  )
  {
    var fieldNotes = new Dictionary<string, string>(StringComparer.Ordinal);
    var functionNotes = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var line in td.Body)
    {
      if (line is ReBodyLine.NoteFieldLine nf)
        fieldNotes[nf.FieldName] = nf.Text;
      if (line is ReBodyLine.NoteFunctionLine nfn)
        functionNotes[nfn.FunctionName] = nfn.Text;
    }

    var fields = new List<FieldDecl>();
    foreach (var line in td.Body)
    {
      if (line is not ReBodyLine.FieldLine fl)
        continue;

      var mergedNote = fl.Note ?? (fieldNotes.TryGetValue(fl.Name, out var nt) ? nt : null);
      TypeExpr resolvedType = fl.Type;
      IReadOnlyList<FlagBitDecl>? flagBits = null;
      string? bitfieldTypeName = null;
      if (fl.Type is TypeExpr.Named nn && bitfieldMap.TryGetValue(nn.Name, out var bfDecl))
      {
        resolvedType = new TypeExpr.Scalar(CanonicalScalarForBitfieldStorage(bfDecl.StorageName));
        flagBits = bfDecl.Bits;
        bitfieldTypeName = nn.Name;
      }

      fields.Add(
        new FieldDecl
        {
          Offset = fl.Offset,
          Name = fl.Name,
          Type = resolvedType,
          Note = mergedNote,
          Provenance = null,
          FlagBits = flagBits,
          BitfieldTypeName = bitfieldTypeName,
        }
      );
    }

    var functions = new List<FunctionDecl>();
    foreach (var line in td.Body)
    {
      if (line is ReBodyLine.FunctionLine fn)
      {
        var mergedFnNote = fn.Note ?? (functionNotes.TryGetValue(fn.Name, out var n) ? n : null);
        functions.Add(
          new FunctionDecl
          {
            Address = fn.Address,
            Name = fn.Name,
            Parameters = fn.Parameters,
            ReturnType = fn.ReturnType,
            Note = mergedFnNote,
            Provenance = null,
          }
        );
      }
    }

    var sources = new List<string>();
    string? sum = null,
      note = null,
      mod = null;
    foreach (var line in td.Body)
    {
      if (line is ReBodyLine.SourceLine s)
        sources.Add(s.Url);
      if (line is ReBodyLine.SummaryLine sm)
        sum = sm.Text;
      if (line is ReBodyLine.NoteEntityLine n)
        note = n.Text;
      if (line is ReBodyLine.ModuleLine ml)
        mod = ml.ModuleName;
    }

    return new TypeDecl
    {
      Name = td.Name,
      Kind = td.Kind,
      ParentName = td.Parent,
      DeclaredSize = td.DeclaredSize,
      Size = td.DeclaredSize ?? 0,
      ModuleName = mod,
      SourceUrls = sources,
      Summary = sum,
      Note = note,
      Provenance = null,
      OwnFields = fields,
      OwnFunctions = functions,
      FilePath = file,
    };
  }

  private static string CanonicalScalarForBitfieldStorage(string storageName)
  {
    var n = storageName.ToLowerInvariant();
    if (n is "uint8" or "bool" or "char")
      return "byte";
    if (n is "word")
      return "uint16";
    return storageName;
  }
}
