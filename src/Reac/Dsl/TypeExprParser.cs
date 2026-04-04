using System.Text.RegularExpressions;
using Reac.Ir;

namespace Reac.Dsl;

/// <summary>Parses field type text into TypeExpr (suffix * and [n]).</summary>
public static class TypeExprParser
{
    private static readonly HashSet<string> ScalarKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "pointer", "byte", "uint8", "uint16", "uint32", "uint64", "int32", "int64",
        "float", "double", "dword", "word", "char", "bool"
    };

    public static TypeExpr Parse(string input)
    {
        input = input.Trim();
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Empty type expression", nameof(input));

        // CWeapon[10]
        var m = Regex.Match(input, @"^(.+)\[(\d+)\]$");
        if (m.Success)
        {
            var inner = Parse(m.Groups[1].Value.Trim());
            return new TypeExpr.Array(inner, int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        if (input.EndsWith('*'))
        {
            var inner = Parse(input[..^1].TrimEnd());
            return new TypeExpr.Pointer(inner);
        }

        var key = input.ToLowerInvariant();
        if (ScalarKeywords.Contains(key))
            return new TypeExpr.Scalar(key);

        return new TypeExpr.Named(input);
    }
}
