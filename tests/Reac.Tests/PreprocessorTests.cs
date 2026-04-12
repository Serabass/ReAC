using Reac.Dsl;

namespace Reac.Tests;

public class PreprocessorTests
{
  [Fact]
  public void ProcessAllFiles_expands_define_and_ifdef()
  {
    var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["a.re"] = """
        #define ADDR 0x42

        #ifdef VC
        class T {
          module M
          ADDR x : uint32
        }
        #endif
        """,
    };
    var pre = new Dictionary<string, string>(StringComparer.Ordinal) { ["VC"] = "1" };
    var proc = RePreprocessor.ProcessAllFiles(raw, pre);
    var text = proc["a.re"];
    var doc = ReDocumentParser.ParseDocument(text);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fl = Assert.Single(td.Body.OfType<ReBodyLine.FieldLine>());
    Assert.Equal(0x42, fl.Offset);
  }

  [Fact]
  public void ProcessAllFiles_ifdef_false_strips_block()
  {
    var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["x.re"] = """
        #ifdef MISSING
        class Gone { module M 0x0 a : uint32 }
        #endif
        class Here { module M 0x0 b : uint32 }
        """,
    };
    var proc = RePreprocessor.ProcessAllFiles(raw, new Dictionary<string, string>());
    var doc = ReDocumentParser.ParseDocument(proc["x.re"]);
    Assert.Single(doc);
    var td = Assert.IsType<ReTopLevel.TypeDef>(doc[0]);
    Assert.Equal("Here", td.Name);
  }
}
