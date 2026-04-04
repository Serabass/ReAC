using Sprache;

namespace Reac.Dsl;

internal static class CommonParse
{
    internal static readonly Parser<string> LineComment =
        from _ in Parse.String("//")
        from _2 in Parse.CharExcept('\n').Many()
        select "";

    internal static readonly Parser<Unit> Noise =
        from w in Parse.WhiteSpace.Many()
        from c in LineComment.Many()
        select Unit.Value;

    internal static Parser<T> Tok<T>(Parser<T> inner) =>
        from _ in Noise
        from x in inner
        select x;

    internal static Parser<string> Identifier { get; } = Tok(
        Parse.Regex(@"[A-Za-z_][A-Za-z0-9_]*")
    );

    internal static Parser<int> HexInt { get; } = Tok(
        from _ in Parse.IgnoreCase("0x")
        from digits in Parse.Regex(@"[0-9a-fA-F]+")
        select Convert.ToInt32(digits, 16)
    );
}

internal readonly struct Unit
{
    public static Unit Value => default;
}
