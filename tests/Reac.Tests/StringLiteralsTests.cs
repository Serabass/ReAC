using Reac.Dsl;

namespace Reac.Tests;

public class StringLiteralsTests
{
  [Fact]
  public void TryParse_double_quoted()
  {
    var s = new string([' ', ' ', '"', 'H', 'e', 'l', 'l', 'o', '"', ' ']);
    var i = 0;
    StringLiterals.SkipNoise(s, ref i);
    Assert.Equal(2, i);
    Assert.Equal('"', s[i]);
    Assert.True(StringLiterals.TryParse(s, ref i, out var v));
    Assert.Equal("Hello", v);
  }
}
