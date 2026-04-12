using Reac.Ir;
using Reac.Layout;
using Reac.Validate;

namespace Reac.Tests;

public class ValidateAndLayoutTests
{
  [Fact]
  public void CPed_has_static_Player_field()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var ped = ir.Types.First(t => t.Name == "CPed");
    var player = ped.OwnFields.First(x => x.Name == "Player");
    Assert.True(player.IsStatic);
    Assert.Equal(0x94AD28UL, player.StaticAddress);
    Assert.IsType<TypeExpr.Pointer>(player.Type);
    var layout = LayoutEngine.BuildLayouts(ir, 4)["CPed"];
    Assert.DoesNotContain(layout.Flattened, x => x.Name == "Player");
  }

  [Fact]
  public void CObject_flag_fields_have_bitfield_metadata()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var cobj = ir.Types.First(t => t.Name == "CObject");
    var f1 = cobj.OwnFields.First(x => x.Name == "objectFlags1");
    Assert.NotNull(f1.FlagBits);
    Assert.Equal(8, f1.FlagBits!.Count);
    Assert.Contains(f1.FlagBits, b => b.Bit == 0 && b.Name == "bIsPickupObject");
    Assert.Equal("CObjectFlags1", f1.BitfieldTypeName);
    var layout = LayoutEngine.BuildLayouts(ir, 4)["CObject"];
    var flat = layout.Flattened.First(x =>
      x.Name == "objectFlags1" && x.DeclaringTypeName == "CObject"
    );
    Assert.NotNull(flat.FlagBits);
  }

  [Fact]
  public void Core_Main_module_resolves_gta_vc_exe_metadata()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var m = ir.Modules.First(x => x.Name == "Core.Main");
    Assert.Equal("re/modules/gta-vc.exe", m.ExePath);
    Assert.NotNull(m.ExeSha256Hex);
    if (m.ExeFilePresent)
    {
      Assert.NotNull(m.ExeActualSha256Hex);
      Assert.Equal(m.ExeSha256Hex, m.ExeActualSha256Hex);
    }
    else
    {
      Assert.Null(m.ExeActualSha256Hex);
    }
  }

  [Fact]
  public void Validate_repo_kb_has_no_errors()
  {
    var root = TestPaths.RepoRoot();
    var ir = ProjectLoader.Load(root);
    var ps = ir
      .Targets.First(x =>
        string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase)
      )
      .PointerSizeBytes;
    var issues = ProjectValidator.Validate(ir, ps);
    var errors = issues.Where(i => i.IsError).ToList();
    Assert.Empty(errors);
  }
}

