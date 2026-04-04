using Reac.Ir;
using Reac.Validate;

namespace Reac.Tests;

public class ValidateAndLayoutTests
{
    [Fact]
    public void Validate_repo_kb_has_no_errors()
    {
        var root = TestPaths.RepoRoot();
        var ir = ProjectLoader.Load(root);
        var ps = ir.Targets.First(x => string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase))
            .PointerSizeBytes;
        var issues = ProjectValidator.Validate(ir, ps);
        var errors = issues.Where(i => i.IsError).ToList();
        Assert.Empty(errors);
    }
}
