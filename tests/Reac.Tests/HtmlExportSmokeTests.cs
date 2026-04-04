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
        var ps = ir.Targets.First(x => string.Equals(x.Id, ir.Config.ActiveTarget, StringComparison.OrdinalIgnoreCase))
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
            Assert.Contains("Inheritance", pedHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provenance", pedHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Ancestor types", pedHtml, StringComparison.Ordinal);
            Assert.Contains("details", pedHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CPhysical", pedHtml, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { /* ignore */ }
        }
    }
}
