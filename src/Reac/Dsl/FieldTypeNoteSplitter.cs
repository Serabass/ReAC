namespace Reac.Dsl;

/// <summary>
/// Splits the text after <c>name :</c> into a type expression and an optional trailing
/// string literal (field note). Without this, <c>CPed* "a; b"</c> is parsed as a single
/// <see cref="Ir.TypeExpr.Named"/> because the line does not end with <c>*</c>.
/// </summary>
internal static class FieldTypeNoteSplitter
{
  /// <param name="restAfterColon">Trimmed fragment after <c>:</c>; line <c>//</c> comments should already be removed.</param>
  public static (string TypeText, string? QuotedNote) Split(string restAfterColon)
  {
    var rest = restAfterColon.Trim();
    if (rest.Length == 0)
      return (rest, null);

    var i = rest.IndexOf("//", StringComparison.Ordinal);
    if (i >= 0)
      rest = rest[..i].TrimEnd();

    var q = rest.IndexOf('"');
    if (q < 0)
      return (rest, null);

    var typeCandidate = rest[..q].TrimEnd();
    var litPart = rest[q..];
    var li = 0;
    if (!StringLiterals.TryParse(litPart, ref li, out var noteText))
      return (restAfterColon.Trim(), null);

    if (li != litPart.Length)
      return (restAfterColon.Trim(), null);

    if (typeCandidate.Length == 0)
      return (restAfterColon.Trim(), null);

    return (typeCandidate, noteText);
  }
}
