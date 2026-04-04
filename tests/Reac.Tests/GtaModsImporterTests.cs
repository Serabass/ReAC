using Reac.Importers.GtaModsVc;

namespace Reac.Tests;

public class GtaModsImporterTests
{
    [Fact]
    public async Task Fixture_mode_writes_types_without_network()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "gtamods_vc_memory_addresses.html");
        Assert.True(File.Exists(fixturePath), "Copy fixture to output: " + fixturePath);

        var tmp = Path.Combine(Path.GetTempPath(), "reac-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        Directory.CreateDirectory(Path.Combine(tmp, "types"));
        try
        {
            await GtaModsImporter.RunAsync(tmp, new GtaModsImporterOptions
            {
                FixturePath = fixturePath,
                Url = "https://example.test/wiki",
                Force = true
            }, CancellationToken.None);

            var cvec = Path.Combine(tmp, "types", "CVector.re");
            Assert.True(File.Exists(cvec));
            var text = await File.ReadAllTextAsync(cvec);
            Assert.Contains("struct CVector", text, StringComparison.Ordinal);
            Assert.Contains("0x000", text, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { /* ignore */ }
        }
    }
}
