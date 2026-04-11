using System.Globalization;
using System.Text.RegularExpressions;
using Reac.Ir;

namespace Reac.Dsl;

/// <summary>Parses field type text: generics <c>Name&lt;T,U&gt;</c>, suffix <c>*</c>, <c>[n]</c>.</summary>
public static class TypeExprParser
{
  private static readonly HashSet<string> ScalarKeywords = new(StringComparer.OrdinalIgnoreCase)
  {
    "pointer",
    "byte",
    "uint8",
    "uint16",
    "uint32",
    "uint64",
    "int",
    "int32",
    "int64",
    "void",
    "float",
    "double",
    "dword",
    "word",
    "char",
    "bool",
  };

  public static TypeExpr Parse(string input)
  {
    input = input.Trim();
    if (string.IsNullOrEmpty(input))
      throw new ArgumentException("Empty type expression", nameof(input));

    if (TryMatchTrailingArray(input, out var arrayInner, out var arrayLen))
      return new TypeExpr.Array(Parse(arrayInner), arrayLen);

    if (input.EndsWith('*'))
    {
      var inner = input[..^1].TrimEnd();
      return new TypeExpr.Pointer(Parse(inner));
    }

    if (TryParseGeneric(input, out var genName, out var argStrings))
    {
      var args = new TypeExpr[argStrings.Count];
      for (var i = 0; i < argStrings.Count; i++)
        args[i] = Parse(argStrings[i]);
      return new TypeExpr.Generic(genName, args);
    }

    var key = input.ToLowerInvariant();
    if (ScalarKeywords.Contains(key))
      return new TypeExpr.Scalar(key);

    return new TypeExpr.Named(input);
  }

  /// <summary>Trailing <c>[n]</c> array length (not inside <c>&lt;&gt;</c>).</summary>
  private static bool TryMatchTrailingArray(string s, out string inner, out int length)
  {
    inner = "";
    length = 0;
    var m = Regex.Match(s, @"\[(\d+)\]$");
    if (!m.Success)
      return false;
    length = int.Parse(
      m.Groups[1].Value,
      NumberStyles.Integer,
      CultureInfo.InvariantCulture
    );
    inner = s[..m.Index];
    return true;
  }

  /// <summary>Top-level <c>Name&lt;...&gt;</c> with balanced angle brackets; whole string must be one generic.</summary>
  private static bool TryParseGeneric(string s, out string name, out List<string> argStrings)
  {
    name = "";
    argStrings = new List<string>();
    var lt = s.IndexOf('<');
    if (lt < 0)
      return false;

    var head = s[..lt].Trim();
    if (head.Length == 0 || !(char.IsLetter(head[0]) || head[0] == '_'))
      return false;
    for (var k = 1; k < head.Length; k++)
    {
      var c = head[k];
      if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.'))
        return false;
    }

    var depth = 0;
    for (var i = lt; i < s.Length; i++)
    {
      var c = s[i];
      if (c == '<')
        depth++;
      else if (c == '>')
      {
        depth--;
        if (depth == 0)
        {
          if (i + 1 != s.Length)
            return false;
          name = head;
          var inside = s.Substring(lt + 1, i - lt - 1);
          argStrings = SplitTypeArgumentList(inside);
          return true;
        }
      }
    }

    return false;
  }

  private static List<string> SplitTypeArgumentList(string inside)
  {
    var parts = new List<string>();
    var start = 0;
    var depthAngle = 0;
    var depthParen = 0;
    for (var i = 0; i <= inside.Length; i++)
    {
      if (i == inside.Length
        || (inside[i] == ',' && depthAngle == 0 && depthParen == 0))
      {
        var chunk = inside.Substring(start, i - start).Trim();
        if (chunk.Length > 0)
          parts.Add(chunk);
        start = i + 1;
        continue;
      }

      var c = inside[i];
      if (c == '<')
        depthAngle++;
      else if (c == '>')
        depthAngle--;
      else if (c == '(')
        depthParen++;
      else if (c == ')')
        depthParen--;
    }

    return parts;
  }
}
