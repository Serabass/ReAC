using Reac;
using Reac.Dsl;
using Reac.Layout;

namespace Reac.Ir;

public static class ProjectLoader
{
  /// <summary>All <c>*.re</c> under targets, modules, types dirs (in that order), sorted per directory tree.</summary>
  public static IEnumerable<string> EnumerateReSourceFiles(string projectRoot, ProjectConfig cfg)
  {
    foreach (var rel in new[] { cfg.TargetsDir, cfg.ModulesDir, cfg.TypesDir })
    {
      var dir = Path.Combine(projectRoot, rel);
      if (!Directory.Exists(dir))
        continue;
      foreach (
        var f in Directory
          .EnumerateFiles(dir, "*.re", SearchOption.AllDirectories)
          .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
      )
        yield return f;
    }
  }

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

    var rawReFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var f in EnumerateReFilesSorted(targetsDir))
      rawReFiles[f] = File.ReadAllText(f);
    foreach (var f in EnumerateReFilesSorted(modulesDir))
      rawReFiles[f] = File.ReadAllText(f);
    foreach (var f in EnumerateReFilesSorted(typesDir))
      rawReFiles[f] = File.ReadAllText(f);
    var processedRe = RePreprocessor.ProcessAllFiles(rawReFiles, cfg.PredefinedMacros);
    var parsedFiles = new List<(string Path, IReadOnlyList<ReTopLevel> Tops)>();
    foreach (var f in EnumerateReFilesSorted(targetsDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(processedRe[f])));
    foreach (var f in EnumerateReFilesSorted(modulesDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(processedRe[f])));
    foreach (var f in EnumerateReFilesSorted(typesDir))
      parsedFiles.Add((f, ReDocumentParser.ParseDocument(processedRe[f])));

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

    var enumMap = new Dictionary<string, EnumTypeDecl>(StringComparer.Ordinal);
    foreach (var (path, tops) in parsedFiles)
    {
      foreach (var t in tops)
      {
        if (t is not ReTopLevel.EnumDef ed)
          continue;
        if (bitfieldMap.ContainsKey(ed.Name))
          throw new InvalidOperationException(
            $"Name '{ed.Name}' is used both as a bitfield and as an enum (see {path})"
          );
        var decl = ToEnumDecl(ed, path, pointerSizeForBitfields);
        if (!enumMap.TryAdd(decl.Name, decl))
          throw new InvalidOperationException(
            $"Duplicate enum type '{decl.Name}' (see {path} and existing)"
          );
      }
    }

    RegisterSyntheticInlineBitfields(
      parsedFiles,
      bitfieldMap,
      enumMap,
      pointerSizeForBitfields
    );

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
            types.Add(ToType(td, path, bitfieldMap, enumMap));
            break;
          case ReTopLevel.BitfieldDef:
            break;
          case ReTopLevel.EnumDef:
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

    var enumList = enumMap.Values.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();

    return new ProjectIr
    {
      Config = cfg,
      ProjectRoot = Path.GetFullPath(projectRoot),
      Targets = targets,
      Modules = modules,
      Types = typesWithSizes,
      BitfieldTypes = bitfieldList,
      EnumTypes = enumList,
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

  private static void RegisterSyntheticInlineBitfields(
    IReadOnlyList<(string Path, IReadOnlyList<ReTopLevel> Tops)> parsedFiles,
    Dictionary<string, BitfieldTypeDecl> bitfieldMap,
    IReadOnlyDictionary<string, EnumTypeDecl> enumMap,
    int pointerSizeBytes
  )
  {
    foreach (var (path, tops) in parsedFiles)
    {
      foreach (var t in tops)
      {
        if (t is not ReTopLevel.TypeDef td)
          continue;
        foreach (var line in td.Body)
        {
          if (line is not ReBodyLine.InlineBitfieldFieldLine ib)
            continue;
          var synth = $"{td.Name}_{ib.FieldName}";
          if (enumMap.ContainsKey(synth))
            throw new InvalidOperationException(
              $"Inline bitfield '{synth}' conflicts with an enum name (see {path})"
            );
          if (bitfieldMap.ContainsKey(synth))
            throw new InvalidOperationException(
              $"Inline bitfield '{synth}' conflicts with an existing bitfield type (see {path})"
            );
          var pseudo = new ReTopLevel.BitfieldDef(
            synth,
            ib.StorageName,
            ib.Bits,
            ib.SourceUrls,
            ib.Summary,
            ib.BlockNote
          );
          bitfieldMap[synth] = ToBitfieldDecl(pseudo, path, pointerSizeBytes);
        }
      }
    }
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
    foreach (var (bit, _, _) in d.Bits)
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
      Bits = d
        .Bits.Select(b => new FlagBitDecl
        {
          Bit = b.Bit,
          Name = b.Name,
          Description = b.Description,
        })
        .ToList(),
      SourceUrls = d.SourceUrls.ToList(),
      Summary = d.Summary,
      Note = d.Note,
      FilePath = file,
    };
  }

  private static EnumTypeDecl ToEnumDecl(
    ReTopLevel.EnumDef d,
    string file,
    int pointerSizeBytes
  )
  {
    var maxVal = FieldSizer.MaxUnsignedValueForScalarStorage(d.StorageName, pointerSizeBytes);
    if (maxVal is null)
      throw new InvalidOperationException(
        $"enum '{d.Name}': unsupported storage '{d.StorageName}' (use a fixed-size scalar: byte..uint64, float, double, pointer, etc.)"
      );

    var seenVals = new HashSet<ulong>();
    var seenNames = new HashSet<string>(StringComparer.Ordinal);
    var list = new List<EnumValueDecl>();
    foreach (var (val, name, desc) in d.Values)
    {
      if (val > maxVal.Value)
        throw new InvalidOperationException(
          $"enum '{d.Name}': value {val} does not fit in storage '{d.StorageName}'"
        );
      if (!seenVals.Add(val))
        throw new InvalidOperationException($"enum '{d.Name}': duplicate value {val}");
      if (!seenNames.Add(name))
        throw new InvalidOperationException($"enum '{d.Name}': duplicate member name '{name}'");

      list.Add(
        new EnumValueDecl
        {
          Value = val,
          Name = name,
          Description = desc,
        }
      );
    }

    return new EnumTypeDecl
    {
      Name = d.Name,
      StorageName = d.StorageName,
      Values = list,
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
    IReadOnlyDictionary<string, BitfieldTypeDecl> bitfieldMap,
    IReadOnlyDictionary<string, EnumTypeDecl> enumMap
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
      if (line is ReBodyLine.InlineBitfieldFieldLine ibf)
      {
        var synth = $"{td.Name}_{ibf.FieldName}";
        if (!bitfieldMap.TryGetValue(synth, out var bfInline))
          throw new InvalidOperationException($"Internal: missing synthetic bitfield '{synth}'");
        var mergedInline =
          (fieldNotes.TryGetValue(ibf.FieldName, out var nti) ? nti : null) ?? ibf.Note;
        fields.Add(
          new FieldDecl
          {
            IsStatic = false,
            StaticAddress = null,
            Offset = ibf.Offset,
            Name = ibf.FieldName,
            Type = new TypeExpr.Scalar(CanonicalScalarForBitfieldStorage(bfInline.StorageName)),
            Note = mergedInline,
            Provenance = null,
            FlagBits = bfInline.Bits,
            BitfieldTypeName = synth,
            EnumTypeName = null,
            EnumValues = null,
          }
        );
        continue;
      }

      if (line is not ReBodyLine.FieldLine && line is not ReBodyLine.StaticFieldLine)
        continue;

      var isStatic = line is ReBodyLine.StaticFieldLine;
      var fl = line as ReBodyLine.FieldLine;
      var sf = line as ReBodyLine.StaticFieldLine;
      var fieldName = fl?.Name ?? sf!.Name;
      var rawType = fl?.Type ?? sf!.Type;

      var mergedNote =
        (fieldNotes.TryGetValue(fieldName, out var nt) ? nt : null) ?? (fl?.Note ?? sf?.Note);
      TypeExpr resolvedType = rawType;
      IReadOnlyList<FlagBitDecl>? flagBits = null;
      string? bitfieldTypeName = null;
      string? enumTypeName = null;
      IReadOnlyList<EnumValueDecl>? enumValues = null;
      if (rawType is TypeExpr.Named nn)
      {
        if (bitfieldMap.TryGetValue(nn.Name, out var bfDecl))
        {
          resolvedType = new TypeExpr.Scalar(CanonicalScalarForBitfieldStorage(bfDecl.StorageName));
          flagBits = bfDecl.Bits;
          bitfieldTypeName = nn.Name;
        }
        else if (enumMap.TryGetValue(nn.Name, out var enDecl))
        {
          resolvedType = new TypeExpr.Scalar(CanonicalScalarForBitfieldStorage(enDecl.StorageName));
          enumValues = enDecl.Values;
          enumTypeName = nn.Name;
        }
      }

      fields.Add(
        new FieldDecl
        {
          IsStatic = isStatic,
          StaticAddress = isStatic ? sf!.Address : null,
          Offset = fl?.Offset ?? 0,
          Name = fieldName,
          Type = resolvedType,
          Note = mergedNote,
          Provenance = null,
          FlagBits = flagBits,
          BitfieldTypeName = bitfieldTypeName,
          EnumTypeName = enumTypeName,
          EnumValues = enumValues,
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
            Decorators = fn.Decorators,
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
