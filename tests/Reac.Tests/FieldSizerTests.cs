using Reac.Layout;

namespace Reac.Tests;

public class FieldSizerTests
{
  [Theory]
  [InlineData("byte", 7)]
  [InlineData("uint8", 7)]
  [InlineData("uint16", 15)]
  [InlineData("word", 15)]
  [InlineData("uint32", 31)]
  [InlineData("dword", 31)]
  [InlineData("int32", 31)]
  [InlineData("uint64", 63)]
  [InlineData("float", 31)]
  [InlineData("double", 63)]
  public void MaxBitIndex_for_scalar_matches_eight_times_size_minus_one(
    string scalar,
    int expectedMaxBit
  )
  {
    Assert.Equal(expectedMaxBit, FieldSizer.MaxBitIndexForScalarStorage(scalar, 4));
  }

  [Fact]
  public void Pointer_storage_bit_width_follows_pointer_size()
  {
    Assert.Equal(31, FieldSizer.MaxBitIndexForScalarStorage("pointer", 4));
    Assert.Equal(63, FieldSizer.MaxBitIndexForScalarStorage("pointer", 8));
  }

  [Fact]
  public void Unknown_scalar_has_no_bit_range()
  {
    Assert.Null(FieldSizer.MaxBitIndexForScalarStorage("not_a_type", 4));
  }
}
