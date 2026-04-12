using System.Globalization;
using System.Linq;
using System.Text;
using Reac.Binary;
using Reac.Ir;
using Reac.Layout;

namespace Reac.Export;

internal static class StaticFieldSnapshotReader
{
  public static Dictionary<string, string> BuildSnapshotByFieldKey(
    byte[] image,
    ProjectIr project,
    int pointerSize
  )
  {
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    var typeMap = project.Types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    var enumMap = project.EnumTypes.ToDictionary(e => e.Name, StringComparer.Ordinal);
    var bitfieldMap = project.BitfieldTypes.ToDictionary(b => b.Name, StringComparer.Ordinal);

    foreach (var td in project.Types)
    {
      foreach (var f in td.OwnFields.Where(x => x.IsStatic))
      {
        var addr = f.StaticAddress;
        if (addr is null)
          continue;
        var key = FieldKey(td.Name, f.Name);
        if (!PeImage.TryVaToFileOffset(image, addr.Value, out var fileOff))
        {
          map[key] = "(VA not mapped in PE)";
          continue;
        }

        var formatted = TryFormat(image, fileOff, f, pointerSize, typeMap, enumMap, bitfieldMap);
        map[key] = formatted ?? "—";
      }
    }

    return map;
  }

  public static string FieldKey(string declaringType, string fieldName) =>
    $"{declaringType}::{fieldName}";

  private static string? TryFormat(
    byte[] image,
    int fileOff,
    FieldDecl f,
    int pointerSize,
    IReadOnlyDictionary<string, TypeDecl> typeMap,
    IReadOnlyDictionary<string, EnumTypeDecl> enumMap,
    IReadOnlyDictionary<string, BitfieldTypeDecl> bitfieldMap
  )
  {
    var (indet, sz) = FieldSizer.TryGetSpan(f.Type, typeMap, pointerSize);
    if (indet || sz is null || sz <= 0)
      return null;

    if (fileOff < 0 || fileOff + sz > image.Length)
      return "(out of file range)";

    var span = image.AsSpan(fileOff, sz.Value);

    if (
      f.EnumTypeName != null
      && f.EnumValues is { Count: > 0 }
      && enumMap.TryGetValue(f.EnumTypeName, out var enDecl)
    )
      return FormatEnumStorage(span, enDecl.StorageName, f.EnumValues);

    if (f.BitfieldTypeName != null && bitfieldMap.TryGetValue(f.BitfieldTypeName, out var bfDecl))
      return FormatScalar(span, FieldSizerCanonicalScalar(bfDecl.StorageName), pointerSize);

    if (f.Type is TypeExpr.Scalar sNt && sNt.Name.Equals("ntstr", StringComparison.OrdinalIgnoreCase))
      return FormatNtstrField(image, fileOff, pointerSize);

    return f.Type switch
    {
      TypeExpr.Scalar s => FormatScalar(span, s.Name, pointerSize),
      TypeExpr.Pointer => FormatPointer(span, pointerSize),
      _ => null,
    };
  }

  private static string FieldSizerCanonicalScalar(string storageName)
  {
    var n = storageName.ToLowerInvariant();
    return n is "uint8" or "bool" or "char" ? "byte"
      : n is "word" ? "uint16"
      : storageName;
  }

  private static string FormatPointer(ReadOnlySpan<byte> span, int pointerSize)
  {
    return pointerSize switch
    {
      4 => "0x" + BitConverter.ToUInt32(span[..4]).ToString("X8", CultureInfo.InvariantCulture),
      8 => "0x" + BitConverter.ToUInt64(span[..8]).ToString("X16", CultureInfo.InvariantCulture),
      _ => "—",
    };
  }

  private static string FormatEnumStorage(
    ReadOnlySpan<byte> span,
    string storageName,
    IReadOnlyList<EnumValueDecl> values
  )
  {
    var n = storageName.ToLowerInvariant();
    ulong raw = n switch
    {
      "byte" or "uint8" or "char" or "bool" => span[0],
      "uint16" or "word" => BitConverter.ToUInt16(span[..2]),
      "uint32" or "dword" or "int32" or "int" => BitConverter.ToUInt32(span[..4]),
      "uint64" or "int64" => BitConverter.ToUInt64(span[..8]),
      _ => 0,
    };

    var match = values.FirstOrDefault(v => v.Value == raw);
    return match != null ? $"{match.Name} (0x{raw:X})" : $"0x{raw:X} (unknown)";
  }

  private static string FormatScalar(ReadOnlySpan<byte> span, string scalarName, int pointerSize)
  {
    var n = scalarName.ToLowerInvariant();
    return n switch
    {
      "float" => BitConverter.ToSingle(span[..4]).ToString("G9", CultureInfo.InvariantCulture),
      "double" => BitConverter.ToDouble(span[..8]).ToString("G17", CultureInfo.InvariantCulture),
      "byte" or "uint8" or "char" => span[0].ToString(CultureInfo.InvariantCulture),
      "bool" => (span[0] != 0).ToString(CultureInfo.InvariantCulture),
      "uint16" or "word" => BitConverter.ToUInt16(span[..2]).ToString(CultureInfo.InvariantCulture),
      "int16" => BitConverter.ToInt16(span[..2]).ToString(CultureInfo.InvariantCulture),
      "uint32" or "dword" => BitConverter
        .ToUInt32(span[..4])
        .ToString(CultureInfo.InvariantCulture),
      "int32" or "int" => BitConverter.ToInt32(span[..4]).ToString(CultureInfo.InvariantCulture),
      "uint64" => BitConverter.ToUInt64(span[..8]).ToString(CultureInfo.InvariantCulture),
      "int64" => BitConverter.ToInt64(span[..8]).ToString(CultureInfo.InvariantCulture),
      "pointer" => FormatPointer(span, pointerSize),
      _ => "—",
    };
  }

  /// <summary>
  /// Prefer <c>char*</c> stored at the field: read pointer-sized VA, map to file, read NTBS.
  /// If that fails or yields empty, treat the field offset as the start of an <strong>inline</strong> NTBS (e.g. static <c>char[]</c> / literal storage).
  /// </summary>
  private static string FormatNtstrField(byte[] image, int fileOff, int pointerSize)
  {
    if (fileOff < 0 || fileOff + pointerSize > image.Length)
      return "(out of file range)";

    if (pointerSize == 4)
    {
      var stringVa = BitConverter.ToUInt32(image.AsSpan(fileOff, 4));
      if (stringVa != 0 && PeImage.TryVaToFileOffset(image, stringVa, out var strOff))
      {
        var viaPtr = ReadNullTerminatedStringForDisplay(image, strOff);
        if (viaPtr != "\"\"")
          return viaPtr;
      }
    }
    else if (pointerSize == 8)
    {
      var stringVa = BitConverter.ToUInt64(image.AsSpan(fileOff, 8));
      if (stringVa != 0 && PeImage.TryVaToFileOffset(image, stringVa, out var strOff))
      {
        var viaPtr = ReadNullTerminatedStringForDisplay(image, strOff);
        if (viaPtr != "\"\"")
          return viaPtr;
      }
    }

    return ReadNullTerminatedStringForDisplay(image, fileOff);
  }

  /// <summary>ASCII / Latin-1 up to first 0 byte; shown in quotes for HTML snapshot column.</summary>
  private static string ReadNullTerminatedStringForDisplay(byte[] image, int start)
  {
    const int maxLen = 4096;
    var end = Math.Min(image.Length, start + maxLen);
    var len = 0;
    for (var i = start; i < end; i++)
    {
      if (image[i] == 0)
        break;
      len++;
    }

    if (len == 0)
      return "\"\"";

    var text = Encoding.ASCII.GetString(image, start, len);
    return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
  }
}

