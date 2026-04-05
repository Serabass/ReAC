using Reac.Export;
using Reac.Ir;

namespace Reac.Tests;

public class HtmlExportSmokeTests
{
  [Fact]
  public void Export_writes_index_and_entity_page()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var ps = ir
      .Targets.First(x =>
        string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase)
      )
      .PointerSizeBytes;
    var outDir = Path.Combine(Path.GetTempPath(), "reac-html-" + Guid.NewGuid().ToString("N"));
    try
    {
      HtmlExporter.Export(ir, outDir, ps);
      var index = Path.Combine(outDir, "index.html");
      Assert.True(File.Exists(index));
      var html = File.ReadAllText(index);
      Assert.Contains("CEntity", html, StringComparison.Ordinal);

      var cped = Path.Combine(outDir, "type", "CPed.html");
      Assert.True(File.Exists(cped));
      var pedHtml = File.ReadAllText(cped);
      Assert.Contains("class=\"sidebar\"", pedHtml, StringComparison.Ordinal);
      Assert.Contains("nav-types-root", pedHtml, StringComparison.Ordinal);
      Assert.Contains("nav-tree", pedHtml, StringComparison.Ordinal);
      Assert.Contains("sidebar-home", pedHtml, StringComparison.Ordinal);
      Assert.Contains("nav-current", pedHtml, StringComparison.Ordinal);
      Assert.Contains("Static fields", pedHtml, StringComparison.Ordinal);
      Assert.Contains("0x94AD28", pedHtml, StringComparison.Ordinal);
      Assert.Contains("Player", pedHtml, StringComparison.Ordinal);
      Assert.Contains("Inheritance", pedHtml, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("Provenance", pedHtml, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("Ancestor types", pedHtml, StringComparison.Ordinal);
      Assert.Contains("details", pedHtml, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("CPhysical", pedHtml, StringComparison.Ordinal);

      var cobj = Path.Combine(outDir, "type", "CObject.html");
      Assert.True(File.Exists(cobj));
      var cobjHtml = File.ReadAllText(cobj);
      Assert.Contains("CObjectObjectFlags1", cobjHtml, StringComparison.Ordinal);
      Assert.Contains("../bitfield/CObjectObjectFlags1.html", cobjHtml, StringComparison.Ordinal);

      var bf1 = Path.Combine(outDir, "bitfield", "CObjectObjectFlags1.html");
      Assert.True(File.Exists(bf1));
      Assert.Contains("bIsPickupObject", File.ReadAllText(bf1), StringComparison.Ordinal);

      var cweapon = Path.Combine(outDir, "type", "CWeapon.html");
      Assert.True(File.Exists(cweapon));
      var weaponHtml = File.ReadAllText(cweapon);
      Assert.Contains("eWeaponType", weaponHtml, StringComparison.Ordinal);
      Assert.Contains("../enum/eWeaponType.html", weaponHtml, StringComparison.Ordinal);

      var en = Path.Combine(outDir, "enum", "eWeaponType.html");
      Assert.True(File.Exists(en));
      var enumHtml = File.ReadAllText(en);
      Assert.Contains("pistol", enumHtml, StringComparison.Ordinal);
      Assert.Contains("Standard sidearm", enumHtml, StringComparison.Ordinal);
    }
    finally
    {
      try
      {
        Directory.Delete(outDir, true);
      }
      catch
      { /* ignore */
      }
    }
  }
}
