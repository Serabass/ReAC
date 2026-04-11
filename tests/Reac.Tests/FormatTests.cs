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
  public void SortFields_ByName_ReordersLines()
  {
    var tops = ReDocumentParser.ParseDocument(
      """
      class T {
        0x020 bb : byte
        0x010 aa : byte
      }
      """
    );
    var o = FormatOptions.Default with { SortFields = LineSortMode.ByName };
    var formatted = ReDocumentFormatter.FormatDocument(tops, o);
    Assert.True(formatted.IndexOf("aa :", StringComparison.Ordinal) < formatted.IndexOf("bb :", StringComparison.Ordinal));
  }

  [Fact]
  public void SortStaticFields_ByAddress_ReordersLines()
  {
    var tops = ReDocumentParser.ParseDocument(
      """
      class T {
        static 0x20 b : byte
        static 0x10 a : byte
      }
      """
    );
    var o = FormatOptions.Default with { SortStaticFields = LineSortMode.ByNumeric };
    var formatted = ReDocumentFormatter.FormatDocument(tops, o);
    Assert.True(formatted.IndexOf("a :", StringComparison.Ordinal) < formatted.IndexOf("b :", StringComparison.Ordinal));
  }

  [Fact]
  public void SortBitfieldBits_ByValue_Reorders()
  {
    var tops = ReDocumentParser.ParseDocument(
      """
      bitfield B : byte {
        0x001 x
        0x000 y
      }
      """
    );
    var o = FormatOptions.Default with { SortBitfieldBits = LineSortMode.ByNumeric };
    var formatted = ReDocumentFormatter.FormatDocument(tops, o);
    Assert.True(
      formatted.IndexOf("0x000", StringComparison.Ordinal) < formatted.IndexOf("0x001", StringComparison.Ordinal)
    );
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

        [sort]
        fields = "name"
        static_fields = "address"
        functions = "name"
        bitfield_bits = "value"
        enum_values = "name"
        """
      );
      var o = FormatConfigLoader.Load(path);
      Assert.Equal("    ", o.IndentUnit);
      Assert.True(o.DigitsUpperCase);
      Assert.Equal(4, o.PadOffsets);
      Assert.Equal(2, o.PadSizes);
      Assert.Equal(6, o.PadAddresses);
      Assert.True(o.AlignFieldTypes);
      Assert.Equal(LineSortMode.ByName, o.SortFields);
      Assert.Equal(LineSortMode.ByNumeric, o.SortStaticFields);
      Assert.Equal(LineSortMode.ByName, o.SortFunctions);
      Assert.Equal(LineSortMode.ByNumeric, o.SortBitfieldBits);
      Assert.Equal(LineSortMode.ByName, o.SortEnumValues);
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
