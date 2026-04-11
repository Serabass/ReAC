using System.Text;

namespace Reac.Dsl;

internal static class ReQuotedString
{
  /// <summary>Double-quoted literal with backslash escapes (matches common .re usage).</summary>
  public static string DoubleQuote(string s)
  {
    var sb = new StringBuilder(s.Length + 2);
    sb.Append('"');
    foreach (var c in s)
    {
      switch (c)
      {
        case '\\':
          sb.Append("\\\\");
          break;
        case '"':
          sb.Append("\\\"");
          break;
        case '\n':
          sb.Append("\\n");
          break;
        case '\r':
          sb.Append("\\r");
          break;
        case '\t':
          sb.Append("\\t");
          break;
        default:
          sb.Append(c);
          break;
      }
    }

    sb.Append('"');
    return sb.ToString();
  }
}
