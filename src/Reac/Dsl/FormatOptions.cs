using System.Globalization;

namespace Reac.Dsl;

/// <summary>How to order lines when <see cref="ReDocumentFormatter"/> runs (see <c>[sort]</c> in <c>reac.format.toml</c>).</summary>
public enum LineSortMode
{
  /// <summary>Keep parser order.</summary>
  Preserve,

  /// <summary>Field offset, static/function address, bit index, or enum value.</summary>
  ByNumeric,

  /// <summary>Identifier name (case-insensitive).</summary>
  ByName,
}

/// <summary>Output style for <see cref="ReDocumentFormatter"/>; usually loaded from <c>reac.format.toml</c>.</summary>
public sealed record FormatOptions
{
  /// <summary>One indent level: two/four spaces or a single tab.</summary>
  public string IndentUnit { get; init; } = "  ";

  public bool DigitsUpperCase { get; init; }

  public int PadOffsets { get; init; } = 3;

  public int PadSizes { get; init; } = 3;

  public int PadAddresses { get; init; } = 8;

  public bool AlignFieldTypes { get; init; }

  /// <summary>Instance fields and inline bitfield fields (by offset or name).</summary>
  public LineSortMode SortFields { get; init; } = LineSortMode.Preserve;

  /// <summary>Static globals (by address or name).</summary>
  public LineSortMode SortStaticFields { get; init; } = LineSortMode.Preserve;

  /// <summary>Native functions (by address or name).</summary>
  public LineSortMode SortFunctions { get; init; } = LineSortMode.Preserve;

  /// <summary>Lines inside top-level <c>bitfield</c> / inline bitfield blocks.</summary>
  public LineSortMode SortBitfieldBits { get; init; } = LineSortMode.Preserve;

  /// <summary>Lines inside <c>enum</c> blocks.</summary>
  public LineSortMode SortEnumValues { get; init; } = LineSortMode.Preserve;

  public static FormatOptions Default { get; } = new();

  public string Indent(int levels)
  {
    if (levels <= 0)
      return "";
    return string.Concat(Enumerable.Repeat(IndentUnit, levels));
  }
}

public enum HexKind
{
  Offset,
  Size,
  Address,
  EnumOrBitIndex,
}

public static class ReHexFormat
{
  public static string Format(ulong value, HexKind kind, FormatOptions o)
  {
    var pad = kind switch
    {
      HexKind.Offset => o.PadOffsets,
      HexKind.Size => o.PadSizes,
      HexKind.Address => o.PadAddresses,
      HexKind.EnumOrBitIndex => o.PadOffsets,
      _ => o.PadOffsets,
    };
    if (pad < 1 || pad > 32)
      throw new ArgumentOutOfRangeException(nameof(o), "pad must be between 1 and 32");
    var fmt = o.DigitsUpperCase ? "X" : "x";
    var digits = value.ToString(fmt, CultureInfo.InvariantCulture);
    while (digits.Length < pad)
      digits = "0" + digits;
    return "0x" + digits;
  }

  public static string FormatInt(int value, HexKind kind, FormatOptions o)
  {
    if (value < 0)
      throw new ArgumentOutOfRangeException(nameof(value));
    return Format((ulong)value, kind, o);
  }
}
