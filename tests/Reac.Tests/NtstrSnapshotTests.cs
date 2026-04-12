using Reac.Export;
using Reac.Ir;

namespace Reac.Tests;

public class NtstrSnapshotTests
{
  [Fact]
  public void Sandbox_static_ntstr_reads_particle_string_from_configured_exe()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var ps = ir
      .Targets.First(x =>
        string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase)
      )
      .PointerSizeBytes;

    var exePath = Path.Combine(root, "re", "modules", "gta-vc.exe");
    Assert.True(File.Exists(exePath), "fixture gta-vc.exe missing");
    var image = File.ReadAllBytes(exePath);
    var map = StaticFieldSnapshotReader.BuildSnapshotByFieldKey(image, ir, ps);
    var key = StaticFieldSnapshotReader.FieldKey("Sandbox", "particle");
    Assert.True(map.TryGetValue(key, out var v), "snapshot missing for Sandbox::particle");
    Assert.Contains("particle", v, StringComparison.OrdinalIgnoreCase);
    Assert.StartsWith("\"", v, StringComparison.Ordinal);
  }
}
