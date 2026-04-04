using Reac.Ir;

namespace Reac.Dsl;

public static class RdocDocumentParser
{
    public static DocumentDecl ParseDocument(string text, string filePath)
    {
        var i = 0;
        StringLiterals.SkipNoise(text, ref i);
        ExpectWord(text, ref i, "document");
        var id = ReadIdent(text, ref i);
        StringLiterals.SkipNoise(text, ref i);
        var body = ReadBraced(text, ref i);

        string? title = null;
        string? summary = null;
        var refs = new List<string>();
        var sections = new List<DocSection>();

        var j = 0;
        while (j < body.Length)
        {
            StringLiterals.SkipNoise(body, ref j);
            if (j >= body.Length) break;
            if (!TryPeekWord(body, j, out var kw, out var next))
                break;
            j = next;

            if (kw.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                StringLiterals.SkipNoise(body, ref j);
                if (!StringLiterals.TryParse(body, ref j, out title))
                    throw new ParseException("title");
                continue;
            }

            if (kw.Equals("summary", StringComparison.OrdinalIgnoreCase))
            {
                StringLiterals.SkipNoise(body, ref j);
                if (!StringLiterals.TryParse(body, ref j, out summary!))
                    throw new ParseException("summary");
                continue;
            }

            if (kw.Equals("references", StringComparison.OrdinalIgnoreCase))
            {
                StringLiterals.SkipNoise(body, ref j);
                if (j >= body.Length || body[j] != '{')
                    throw new ParseException("references {");
                var inner = ReadBraced(body, ref j);
                ParseRefs(inner, refs);
                continue;
            }

            if (kw.Equals("section", StringComparison.OrdinalIgnoreCase))
            {
                StringLiterals.SkipNoise(body, ref j);
                var secName = ReadIdent(body, ref j);
                var secInner = ReadBraced(body, ref j);
                var st = ParseSectionText(secInner);
                sections.Add(new DocSection { Name = secName, Text = st });
                continue;
            }

            throw new ParseException("Unexpected in rdoc: " + kw);
        }

        return new DocumentDecl
        {
            Id = id,
            Title = string.IsNullOrEmpty(title) ? id : title!,
            Summary = summary,
            References = refs,
            Sections = sections,
            FilePath = filePath
        };
    }

    private static void ParseRefs(string inner, List<string> refs)
    {
        var j = 0;
        while (j < inner.Length)
        {
            StringLiterals.SkipNoise(inner, ref j);
            if (j >= inner.Length) break;
            var w = ReadWord(inner, ref j);
            if (!w.Equals("ref", StringComparison.OrdinalIgnoreCase))
                throw new ParseException("expected ref");
            StringLiterals.SkipNoise(inner, ref j);
            refs.Add(ReadIdent(inner, ref j));
        }
    }

    private static string ParseSectionText(string inner)
    {
        var j = 0;
        StringLiterals.SkipNoise(inner, ref j);
        ExpectWord(inner, ref j, "text");
        StringLiterals.SkipNoise(inner, ref j);
        if (!StringLiterals.TryParse(inner, ref j, out var t))
            throw new ParseException("section text");
        return t;
    }

    private static bool TryPeekWord(string s, int i, out string word, out int afterWord)
    {
        word = "";
        afterWord = i;
        var j = i;
        StringLiterals.SkipNoise(s, ref j);
        if (j >= s.Length || !char.IsLetter(s[j]))
            return false;
        var start = j;
        while (j < s.Length && char.IsLetter(s[j]))
            j++;
        word = s.Substring(start, j - start);
        afterWord = j;
        return true;
    }

    private static void ExpectWord(string s, ref int i, string word)
    {
        StringLiterals.SkipNoise(s, ref i);
        for (var k = 0; k < word.Length; k++)
        {
            if (i + k >= s.Length || char.ToLowerInvariant(s[i + k]) != word[k])
                throw new ParseException("expected " + word);
        }

        i += word.Length;
        if (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_'))
            throw new ParseException("word boundary");
    }

    private static string ReadIdent(string s, ref int i)
    {
        StringLiterals.SkipNoise(s, ref i);
        var start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_'))
            i++;
        return s.Substring(start, i - start);
    }

    private static string ReadWord(string s, ref int i)
    {
        var start = i;
        while (i < s.Length && char.IsLetter(s[i]))
            i++;
        return s.Substring(start, i - start);
    }

    /// <summary>Read content of {...}, i points after opening {.</summary>
    private static string ReadBraced(string s, ref int i)
    {
        StringLiterals.SkipNoise(s, ref i);
        if (i >= s.Length || s[i] != '{')
            throw new ParseException("expected {");
        i++;
        var depth = 1;
        var start = i;
        while (i < s.Length && depth > 0)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}') depth--;
            if (depth == 0) break;
            i++;
        }

        var inner = s.Substring(start, i - start);
        if (i < s.Length && s[i] == '}') i++;
        return inner;
    }
}
