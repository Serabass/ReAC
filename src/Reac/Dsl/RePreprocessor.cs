using System.Text;
using System.Text.RegularExpressions;

namespace Reac.Dsl;

/// <summary>Minimal C-like preprocessor: <c>#define</c>, <c>#ifdef</c>, <c>#ifndef</c>, <c>#else</c>, <c>#endif</c>.</summary>
public static class RePreprocessor
{
  private static readonly Regex DefineLineRegex = new(
    @"^\s*#define\s+([A-Za-z_][A-Za-z0-9_]*)\s*(.*?)\s*$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  /// <summary>
  /// Expands defines from all files (fixpoint), then strips conditionals and macro-expands each file.
  /// Duplicate <c>#define</c> with different values throws <see cref="InvalidOperationException"/>.
  /// </summary>
  public static Dictionary<string, string> ProcessAllFiles(
    IReadOnlyDictionary<string, string> pathToRaw,
    IReadOnlyDictionary<string, string> predefinedMacros
  )
  {
    var defines = new Dictionary<string, string>(predefinedMacros, StringComparer.Ordinal);
    var ordered = pathToRaw.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();

    for (var iter = 0; iter < 64; iter++)
    {
      var added = false;
      foreach (var kv in ordered)
      {
        var stripped = StripConditionals(kv.Value, defines);
        foreach (var (name, value) in ExtractDefines(stripped))
        {
          if (defines.TryGetValue(name, out var existing))
          {
            if (!string.Equals(existing, value, StringComparison.Ordinal))
              throw new InvalidOperationException(
                $"Preprocessor: conflicting #define for '{name}' in {kv.Key}"
              );
          }
          else
          {
            defines[name] = value;
            added = true;
          }
        }
      }

      if (!added)
        break;
    }

    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in ordered)
    {
      var s = StripConditionals(kv.Value, defines);
      s = RemoveDefineLines(s);
      s = ExpandMacros(s, defines);
      result[kv.Key] = s;
    }

    return result;
  }

  private static IEnumerable<(string Name, string Value)> ExtractDefines(string text)
  {
    foreach (var line in SplitLines(text))
    {
      var m = DefineLineRegex.Match(line);
      if (!m.Success)
        continue;
      yield return (m.Groups[1].Value, m.Groups[2].Value.Trim());
    }
  }

  private static string RemoveDefineLines(string text)
  {
    var sb = new StringBuilder();
    foreach (var line in SplitLines(text))
    {
      if (DefineLineRegex.IsMatch(line))
        continue;
      sb.Append(line);
      sb.Append('\n');
    }

    return sb.ToString();
  }

  private static string ExpandMacros(string text, IReadOnlyDictionary<string, string> defines)
  {
    if (defines.Count == 0)
      return text;
    var names = defines.Keys.OrderByDescending(k => k.Length).ToList();
    var sb = new StringBuilder();
    var inString = false;
    for (var i = 0; i < text.Length; )
    {
      var c = text[i];
      if (c == '"')
      {
        inString = !inString;
        sb.Append(c);
        i++;
        continue;
      }

      if (inString)
      {
        sb.Append(c);
        i++;
        continue;
      }

      var matched = false;
      foreach (var name in names)
      {
        if (i + name.Length > text.Length)
          continue;
        if (!text.AsSpan(i, name.Length).Equals(name.AsSpan(), StringComparison.Ordinal))
          continue;
        if (i > 0 && IsIdChar(text[i - 1]))
          continue;
        if (i + name.Length < text.Length && IsIdChar(text[i + name.Length]))
          continue;
        sb.Append(defines[name]);
        i += name.Length;
        matched = true;
        break;
      }

      if (!matched)
      {
        sb.Append(c);
        i++;
      }
    }

    return sb.ToString();
  }

  private static bool IsIdChar(char c) => char.IsLetterOrDigit(c) || c == '_';

  /// <summary>Recursive: drop lines in false <c>#ifdef</c> branches.</summary>
  private static string StripConditionals(string text, IReadOnlyDictionary<string, string> defines)
  {
    bool Def(string n) => defines.ContainsKey(n);
    var lines = SplitLines(text);
    var i = 0;
    var sb = new StringBuilder();

    void Parse(bool emit)
    {
      while (i < lines.Count)
      {
        var raw = lines[i];
        var t = raw.TrimStart();
        if (t.StartsWith("#ifdef ", StringComparison.OrdinalIgnoreCase))
        {
          var name = t.Substring(7).Trim();
          i++;
          var take = emit && Def(name);
          if (take)
          {
            Parse(true);
            if (i >= lines.Count)
              return;
            var t2 = lines[i].TrimStart();
            if (t2.Equals("#else", StringComparison.OrdinalIgnoreCase))
            {
              i++;
              Parse(false);
              ExpectEndif();
            }
            else if (t2.Equals("#endif", StringComparison.OrdinalIgnoreCase))
            {
              i++;
            }
            else
            {
              throw new InvalidOperationException(
                $"Preprocessor: expected #else or #endif after #ifdef {name}"
              );
            }
          }
          else
          {
            Parse(false);
            if (i >= lines.Count)
              return;
            var t2 = lines[i].TrimStart();
            if (t2.Equals("#else", StringComparison.OrdinalIgnoreCase))
            {
              i++;
              Parse(emit);
              ExpectEndif();
            }
            else if (t2.Equals("#endif", StringComparison.OrdinalIgnoreCase))
            {
              i++;
            }
            else
            {
              throw new InvalidOperationException("Preprocessor: expected #else or #endif");
            }
          }

          continue;
        }

        if (t.StartsWith("#ifndef ", StringComparison.OrdinalIgnoreCase))
        {
          var name = t.Substring(8).Trim();
          i++;
          var take = emit && !Def(name);
          if (take)
          {
            Parse(true);
            if (i >= lines.Count)
              return;
            var t2 = lines[i].TrimStart();
            if (t2.Equals("#else", StringComparison.OrdinalIgnoreCase))
            {
              i++;
              Parse(false);
              ExpectEndif();
            }
            else if (t2.Equals("#endif", StringComparison.OrdinalIgnoreCase))
            {
              i++;
            }
            else
            {
              throw new InvalidOperationException(
                $"Preprocessor: expected #else or #endif after #ifndef {name}"
              );
            }
          }
          else
          {
            Parse(false);
            if (i >= lines.Count)
              return;
            var t2 = lines[i].TrimStart();
            if (t2.Equals("#else", StringComparison.OrdinalIgnoreCase))
            {
              i++;
              Parse(emit);
              ExpectEndif();
            }
            else if (t2.Equals("#endif", StringComparison.OrdinalIgnoreCase))
            {
              i++;
            }
            else
            {
              throw new InvalidOperationException("Preprocessor: expected #else or #endif");
            }
          }

          continue;
        }

        if (t.Equals("#else", StringComparison.OrdinalIgnoreCase))
        {
          return;
        }

        if (t.Equals("#endif", StringComparison.OrdinalIgnoreCase))
        {
          return;
        }

        if (emit)
          sb.Append(raw).Append('\n');
        i++;
      }
    }

    void ExpectEndif()
    {
      if (i >= lines.Count)
        throw new InvalidOperationException("Preprocessor: missing #endif");
      var t = lines[i].TrimStart();
      if (!t.Equals("#endif", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Preprocessor: expected #endif, got: {t}");
      i++;
    }

    Parse(true);
    return sb.ToString();
  }

  private static List<string> SplitLines(string text)
  {
    var lines = new List<string>();
    var start = 0;
    for (var i = 0; i <= text.Length; i++)
    {
      if (i == text.Length || text[i] == '\n' || text[i] == '\r')
      {
        lines.Add(text.Substring(start, i - start));
        if (i < text.Length && text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
          i++;
        start = i + 1;
      }
    }

    return lines;
  }
}
