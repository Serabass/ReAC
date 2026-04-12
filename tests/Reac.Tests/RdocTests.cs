using Reac.Dsl;
using Reac.Ir;

namespace Reac.Tests;

public class RdocTests
{
  [Fact]
  public void Parse_one_line_roundtrip_title_summary()
  {
    var oneLine =
      "document X { title \"T\" summary \"S\" references { ref A } section I { text \"x\" } }";
    var d = RdocDocumentParser.ParseDocument(oneLine, "y.rdoc");
    Assert.Equal("T", d.Title);
    Assert.Equal("S", d.Summary);
  }

  [Fact]
  public void ParseOverview_like_scaffold()
  {
    var src = """
      document Overview {
        title "Overview"
        summary "Hello."
        references {
          ref CEntity
        }
        section Intro {
          text "Body."
        }
      }
      """;
    var d = RdocDocumentParser.ParseDocument(src, "x.rdoc");
    Assert.Equal("Overview", d.Id);
    Assert.Equal("Overview", d.Title);
    Assert.Contains("CEntity", d.References);
  }

  [Fact]
  public void Include_expand_then_parse_merges_fragment()
  {
    var tmp = Path.Combine(Path.GetTempPath(), "reac-rdoc-inc-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(tmp, "parts"));
    var entry = Path.Combine(tmp, "Entry.rdoc");
    var frag = Path.Combine(tmp, "parts", "Frag.rdoc");
    File.WriteAllText(
      frag,
      """
      section S1 {
        text "from frag"
      }
      """
    );
    File.WriteAllText(
      entry,
      """
      document D {
        title "T"
        #include "parts/Frag.rdoc"
      }
      """
    );
    var (expanded, included) = RdocIncludeExpander.Expand(entry);
    Assert.Single(included);
    Assert.Equal(Path.GetFullPath(frag), Path.GetFullPath(included[0]));
    var d = RdocDocumentParser.ParseDocument(expanded, entry, included);
    Assert.Equal("D", d.Id);
    Assert.Equal("T", d.Title);
    Assert.Contains(d.Sections, s => s.Name == "S1" && s.Text == "from frag");
  }

  [Fact]
  public void Include_cycle_throws()
  {
    var tmp = Path.Combine(Path.GetTempPath(), "reac-rdoc-cycle-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tmp);
    var a = Path.Combine(tmp, "a.rdoc");
    var b = Path.Combine(tmp, "b.rdoc");
    File.WriteAllText(a, "#include \"b.rdoc\"\n");
    File.WriteAllText(b, "#include \"a.rdoc\"\n");
    Assert.Throws<InvalidOperationException>(() => RdocIncludeExpander.Expand(a));
  }

  [Fact]
  public void Load_repo_kb_uses_docs_entry_and_records_includes()
  {
    var ir = ProjectLoader.Load(TestPaths.RepoRoot());
    Assert.Single(ir.Documents);
    var doc = ir.Documents[0];
    Assert.Equal("Overview", doc.Id);
    Assert.Contains("Overview.rdoc", doc.FilePath, StringComparison.OrdinalIgnoreCase);
    Assert.NotEmpty(doc.IncludedSourcePaths);
    Assert.Contains(
      doc.IncludedSourcePaths,
      p => p.EndsWith("Sample_Memory_Model.rdoc", StringComparison.OrdinalIgnoreCase)
    );
  }
}
