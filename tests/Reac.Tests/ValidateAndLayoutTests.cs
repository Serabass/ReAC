using Reac.Ir;
using Reac.Layout;
using Reac.Validate;

namespace Reac.Tests;

public class ValidateAndLayoutTests
{
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
    Assert.Equal("CObjectObjectFlags1", f1.BitfieldTypeName);
    var layout = LayoutEngine.BuildLayouts(ir, 4)["CObject"];
    var flat = layout.Flattened.First(x =>
      x.Name == "objectFlags1" && x.DeclaringTypeName == "CObject"
    );
    Assert.NotNull(flat.FlagBits);
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
