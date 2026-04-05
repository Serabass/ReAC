namespace Reac.Ir;

public sealed class Provenance
{
  public string? SourceUrl { get; init; }
  public string? SourceSection { get; init; }
  public string? ImportedBy { get; init; }
  public DateTimeOffset? ImportedAt { get; init; }
}
