using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

namespace Reac.Dsl;

public static class FormatConfigLoader
{
  public const string DefaultFileName = "reac.format.toml";

  /// <summary>Load options from TOML file; throws if invalid.</summary>
  public static FormatOptions Load(string path)
  {
    var text = File.ReadAllText(path);
    var model = Toml.ToModel(text);
    if (model is not TomlTable root)
      throw new InvalidOperationException("Invalid reac.format.toml root");

    var o = FormatOptions.Default;

    if (root.TryGetValue("indent", out var indObj) && indObj is TomlTable ind)
    {
      var style = GetString(ind, "style")?.ToLowerInvariant() ?? "space";
      if (style == "tab")
        o = o with { IndentUnit = "\t" };
      else if (style == "space")
      {
        var w = GetInt(ind, "width") ?? 2;
        if (w != 2 && w != 4)
          throw new InvalidOperationException("indent.width must be 2 or 4 when style is space");
        o = o with { IndentUnit = new string(' ', w) };
      }
      else
        throw new InvalidOperationException("indent.style must be \"space\" or \"tab\"");
    }

    if (root.TryGetValue("hex", out var hexObj) && hexObj is TomlTable hex)
    {
      var dc = GetString(hex, "digits_case")?.ToLowerInvariant() ?? "lower";
      if (dc != "upper" && dc != "lower")
        throw new InvalidOperationException("hex.digits_case must be \"upper\" or \"lower\"");
      o = o with { DigitsUpperCase = dc == "upper" };

      if (hex.TryGetValue("pad_offsets", out var po))
        o = o with { PadOffsets = ExpectPad(po, "hex.pad_offsets") };
      if (hex.TryGetValue("pad_sizes", out var ps))
        o = o with { PadSizes = ExpectPad(ps, "hex.pad_sizes") };
      if (hex.TryGetValue("pad_addresses", out var pa))
        o = o with { PadAddresses = ExpectPad(pa, "hex.pad_addresses") };
    }

    if (root.TryGetValue("fields", out var fldObj) && fldObj is TomlTable fld)
    {
      if (fld.TryGetValue("align_types", out var at))
        o = o with { AlignFieldTypes = ExpectBool(at, "fields.align_types") };
    }

    if (root.TryGetValue("sort", out var sortObj) && sortObj is TomlTable sort)
    {
      if (sort.TryGetValue("fields", out var sf))
        o = o with { SortFields = ParseSortFields(sf?.ToString(), "sort.fields") };
      if (sort.TryGetValue("static_fields", out var ssf))
        o = o with
        {
          SortStaticFields = ParseSortAddressName(ssf?.ToString(), "sort.static_fields"),
        };
      if (sort.TryGetValue("functions", out var sfn))
        o = o with { SortFunctions = ParseSortAddressName(sfn?.ToString(), "sort.functions") };
      if (sort.TryGetValue("bitfield_bits", out var sbb))
        o = o with
        {
          SortBitfieldBits = ParseSortValueName(sbb?.ToString(), "sort.bitfield_bits"),
        };
      if (sort.TryGetValue("enum_values", out var sev))
        o = o with { SortEnumValues = ParseSortValueName(sev?.ToString(), "sort.enum_values") };
    }

    return o;
  }

  private static LineSortMode ParseSortFields(string? s, string key) =>
    s?.Trim().ToLowerInvariant() switch
    {
      null or "" or "preserve" => LineSortMode.Preserve,
      "offset" => LineSortMode.ByNumeric,
      "name" => LineSortMode.ByName,
      _ =>
        throw new InvalidOperationException(
          $"{key} must be \"preserve\", \"offset\", or \"name\""
        ),
    };

  private static LineSortMode ParseSortAddressName(string? s, string key) =>
    s?.Trim().ToLowerInvariant() switch
    {
      null or "" or "preserve" => LineSortMode.Preserve,
      "address" => LineSortMode.ByNumeric,
      "name" => LineSortMode.ByName,
      _ =>
        throw new InvalidOperationException(
          $"{key} must be \"preserve\", \"address\", or \"name\""
        ),
    };

  private static LineSortMode ParseSortValueName(string? s, string key) =>
    s?.Trim().ToLowerInvariant() switch
    {
      null or "" or "preserve" => LineSortMode.Preserve,
      "value" => LineSortMode.ByNumeric,
      "name" => LineSortMode.ByName,
      _ =>
        throw new InvalidOperationException(
          $"{key} must be \"preserve\", \"value\", or \"name\""
        ),
    };

  /// <param name="explicitPath">If set, this file must exist.</param>
  public static FormatOptions LoadForProject(string projectRoot, string? explicitPath)
  {
    if (explicitPath != null)
      return Load(explicitPath);
    var p = Path.Combine(projectRoot, DefaultFileName);
    if (File.Exists(p))
      return Load(p);
    return FormatOptions.Default;
  }

  private static int ExpectPad(object? v, string key)
  {
    var n = ConvertToInt(v, key);
    if (n < 1 || n > 32)
      throw new InvalidOperationException($"{key} must be between 1 and 32");
    return n;
  }

  private static bool ExpectBool(object? v, string key)
  {
    if (v is bool b)
      return b;
    throw new InvalidOperationException($"{key} must be a boolean");
  }

  private static int ConvertToInt(object? v, string key)
  {
    switch (v)
    {
      case int i:
        return i;
      case long l:
        return checked((int)l);
      case string s:
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x))
          return x;
        break;
    }

    throw new InvalidOperationException($"{key} must be an integer");
  }

  private static int? GetInt(TomlTable t, string key)
  {
    if (!t.TryGetValue(key, out var v) || v == null)
      return null;
    return ConvertToInt(v, key);
  }

  private static string? GetString(TomlTable t, string key)
  {
    if (!t.TryGetValue(key, out var v) || v == null)
      return null;
    return v.ToString();
  }
}
