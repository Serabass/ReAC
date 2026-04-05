namespace Reac.Tests;

internal static class TestPaths
{
  /// <summary>Repository root (directory containing Reac.sln), for integration tests.</summary>
  internal static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
      if (
        File.Exists(Path.Combine(dir.FullName, "Reac.sln"))
        && File.Exists(Path.Combine(dir.FullName, "project.toml"))
      )
        return dir.FullName;
      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root (Reac.sln + project.toml).");
  }
}
