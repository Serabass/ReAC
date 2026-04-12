using System.Text;
using System.Text.RegularExpressions;

namespace Reac.Dsl;

/// <summary>
/// Expands <c>#include "relative.rdoc"</c> lines (paths relative to the including file). Included fragments must be
/// valid inside the parent <c>document { ... }</c> body — not a nested <c>document</c> block.
/// </summary>
public static class RdocIncludeExpander
{
  private static readonly Regex IncludeLineRegex = new(
    @"^\s*#include\s+""([^""]+)""\s*$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  /// <param name="entryFullPath">Absolute path to the entry <c>.rdoc</c> file.</param>
  /// <returns>Expanded text and ordered list of included file paths (each path when first included via <c>#include</c>).</returns>
  public static (string ExpandedText, IReadOnlyList<string> IncludedSourcePaths) Expand(
    string entryFullPath
  )
  {
    var full = Path.GetFullPath(entryFullPath);
    if (!File.Exists(full))
      throw new FileNotFoundException("rdoc entry not found", full);

    var includedOrder = new List<string>();
    var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var expanded = ExpandFile(full, visiting, includedOrder);
    return (expanded, includedOrder);
  }

  private static string ExpandFile(
    string fileFullPath,
    HashSet<string> visiting,
    List<string> includedOrder
  )
  {
    var norm = Path.GetFullPath(fileFullPath);
    if (!visiting.Add(norm))
      throw new InvalidOperationException($"rdoc #include cycle involving '{norm}'");

    try
    {
      var text = File.ReadAllText(norm);
      var sb = new StringBuilder();
      foreach (var line in SplitLines(text))
      {
        var m = IncludeLineRegex.Match(line);
        if (m.Success)
        {
          var rel = m.Groups[1].Value.Trim();
          var dir = Path.GetDirectoryName(norm);
          if (string.IsNullOrEmpty(dir))
            dir = Directory.GetCurrentDirectory();
          var sep = rel.Replace('/', Path.DirectorySeparatorChar);
          var incPath = Path.GetFullPath(Path.Combine(dir, sep));
          if (!File.Exists(incPath))
            throw new FileNotFoundException($"rdoc #include target not found: {incPath}", incPath);

          includedOrder.Add(incPath);
          var inner = ExpandFile(incPath, visiting, includedOrder);
          sb.Append(inner);
          if (inner.Length == 0 || !inner.EndsWith('\n'))
            sb.Append('\n');
        }
        else
        {
          sb.Append(line);
          sb.Append('\n');
        }
      }

      return sb.ToString();
    }
    finally
    {
      visiting.Remove(norm);
    }
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

