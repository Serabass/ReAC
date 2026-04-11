using Reac.Dsl;
using Xunit;

namespace Reac.Tests;

public class FormatTests
{
  private const string SampleStruct = """
    class C {
      0x354 health : float
      0x408 weapons : CWeapon[10]
      0x5F4 wanted : CWanted*
    }
    """;

  [Fact]
  public void FormatDocument_DefaultIndent_RoundTrip()
  {
    var tops = ReDocumentParser.ParseDocument(SampleStruct);
    var opts = FormatOptions.Default;
    var formatted = ReDocumentFormatter.FormatDocument(tops, opts);
    var again = ReDocumentParser.ParseDocument(formatted);
    Assert.Equal(tops.Count, again.Count);
  }

  [Fact]
  public void Indent_UsesFourSpaces_WhenConfigured()
  {
    var tops = ReDocumentParser.ParseDocument("struct S {\n  0x0 a : byte\n}\n");
    var opts = FormatOptions.Default with { IndentUnit = "    " };
    var formatted = ReDocumentFormatter.FormatDocument(tops, opts);
    Assert.Contains("    0x000 a : byte", formatted);
  }

  [Fact]
  public void Indent_UsesTab_WhenConfigured()
  {
    var tops = ReDocumentParser.ParseDocument("struct S {\n  0x0 a : byte\n}\n");
    var opts = FormatOptions.Default with { IndentUnit = "\t" };
    var formatted = ReDocumentFormatter.FormatDocument(tops, opts);
    Assert.Contains("\t0x000 a : byte", formatted);
  }

  [Fact]
  public void Hex_UpperCase_And_PadAddresses()
  {
    var tops = ReDocumentParser.ParseDocument(
      "class X {\n  static 0x1234ab p : byte\n}\n"
    );
    var o = FormatOptions.Default with
    {
      DigitsUpperCase = true,
      PadAddresses = 8,
    };
    var s = ReDocumentFormatter.FormatDocument(tops, o);
    Assert.Contains("0x001234AB", s);
  }

  [Fact]
  public void AlignFieldTypes_PadsNames_BeforeColon()
  {
    var tops = ReDocumentParser.ParseDocument(SampleStruct);
    var o = FormatOptions.Default with { AlignFieldTypes = true };
    var formatted = ReDocumentFormatter.FormatDocument(tops, o);
    Assert.Contains("0x354 health  : float", formatted);
    Assert.Contains("0x408 weapons : CWeapon[10]", formatted);
    Assert.Contains("0x5f4 wanted  : CWanted*", formatted);
  }

  [Fact]
  public void FormatConfigLoader_ParsesTomlSections()
  {
    var path = Path.Combine(Path.GetTempPath(), $"reac-fmt-{Guid.NewGuid():N}.toml");
    try
    {
      File.WriteAllText(
        path,
        """
        [indent]
        style = "space"
        width = 4

        [hex]
        digits_case = "upper"
        pad_offsets = 4
        pad_sizes = 2
        pad_addresses = 6

        [fields]
        align_types = true
        """
      );
      var o = FormatConfigLoader.Load(path);
      Assert.Equal("    ", o.IndentUnit);
      Assert.True(o.DigitsUpperCase);
      Assert.Equal(4, o.PadOffsets);
      Assert.Equal(2, o.PadSizes);
      Assert.Equal(6, o.PadAddresses);
      Assert.True(o.AlignFieldTypes);
    }
    finally
    {
      try
      {
        File.Delete(path);
      }
      catch
      {
        /* ignore */
      }
    }
  }
}
