using Reac.Ir;
using Tomlyn;
using Tomlyn.Model;

namespace Reac;

public static class ProjectMeta
{
  public static ProjectConfig Load(string projectTomlPath)
  {
    var text = File.ReadAllText(projectTomlPath);
    var model = Toml.ToModel(text);
    if (model is not TomlTable root)
      throw new InvalidOperationException("Invalid project.toml root");

    var name = root.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
    var version = root.TryGetValue("version", out var v) ? v?.ToString() ?? "" : "";
    var active = root.TryGetValue("active_target", out var a) ? a?.ToString() ?? "" : "";

    var targetsDir = "targets";
    var modulesDir = "modules";
    var typesDir = "types";
    var docsDir = "docs";
    var generatedDir = "generated";
    if (root.TryGetValue("paths", out var pObj) && pObj is TomlTable paths)
    {
      if (paths.TryGetValue("targets_dir", out var td))
        targetsDir = td?.ToString() ?? targetsDir;
      if (paths.TryGetValue("modules_dir", out var md))
        modulesDir = md?.ToString() ?? modulesDir;
      if (paths.TryGetValue("types_dir", out var tyd))
        typesDir = tyd?.ToString() ?? typesDir;
      if (paths.TryGetValue("docs_dir", out var dd))
        docsDir = dd?.ToString() ?? docsDir;
      if (paths.TryGetValue("generated_dir", out var gd))
        generatedDir = gd?.ToString() ?? generatedDir;
    }

    var predefined = new Dictionary<string, string>(StringComparer.Ordinal);
    if (root.TryGetValue("preprocessor", out var preObj) && preObj is TomlTable pre)
    {
      if (pre.TryGetValue("defines", out var defObj) && defObj is TomlTable defTable)
      {
        foreach (var kv in defTable)
        {
          var key = kv.Key ?? "";
          if (key.Length == 0)
            continue;
          predefined[key] = kv.Value?.ToString() ?? "";
        }
      }
    }

    return new ProjectConfig
    {
      Name = name,
      Version = version,
      ActiveTarget = active,
      TargetsDir = targetsDir,
      ModulesDir = modulesDir,
      TypesDir = typesDir,
      DocsDir = docsDir,
      GeneratedDir = generatedDir,
      PredefinedMacros = predefined,
    };
  }
}
