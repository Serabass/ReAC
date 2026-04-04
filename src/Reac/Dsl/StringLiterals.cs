using System.Text.RegularExpressions;

namespace Reac.Dsl;

internal static class StringLiterals
{
    /// <summary>Parses "..." or """...""" starting at i; advances i past the literal.</summary>
    public static bool TryParse(string s, ref int i, out string value)
    {
        value = "";
        if (i >= s.Length)
            return false;

        if (i + 2 < s.Length && s[i] == '"' && s[i + 1] == '"' && s[i + 2] == '"')
        {
            var m = Regex.Match(s.Substring(i), "^\"\"\"(.*?)\"\"\"", RegexOptions.Singleline);
            if (!m.Success)
                throw new ParseException("Unclosed triple-quoted string");
            value = m.Groups[1].Value;
            i += m.Length;
            return true;
        }

        if (s[i] != '"')
            return false;

        i++;
        var sb = new System.Text.StringBuilder();
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '"')
            {
                value = sb.ToString();
                i++;
                return true;
            }

            if (c == '\\' && i + 1 < s.Length)
            {
                sb.Append(s[i + 1]);
                i += 2;
                continue;
            }

            sb.Append(c);
            i++;
        }

        throw new ParseException("Unclosed string literal");
    }

    /// <summary>Skip whitespace and comments //; advance i.</summary>
    public static void SkipNoise(string s, ref int i)
    {
        while (i < s.Length)
        {
            var c = s[i];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                i++;
                continue;
            }

            if (i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue;
            }

            break;
        }
    }
}

public sealed class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
