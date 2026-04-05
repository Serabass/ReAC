using Reac.Dsl;

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
}
