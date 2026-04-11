namespace Reac.Binary;

/// <summary>Minimal PE32/PE32+ helpers: map a virtual address (as in the loaded module) to an on-disk file offset.</summary>
internal static class PeImage
{
  /// <returns>False if the file is not a valid PE, RVA is outside any section, or the RVA maps to uninitialized virtual-only range (no raw bytes).</returns>
  public static bool TryVaToFileOffset(ReadOnlySpan<byte> image, ulong va, out int fileOffset)
  {
    fileOffset = 0;
    if (image.Length < 0x40)
      return false;

    var eLfanew = BitConverter.ToUInt32(image.Slice(0x3C, 4));
    if (eLfanew == 0 || eLfanew + 24 > image.Length)
      return false;

    var pe = (int)eLfanew;
    if (pe + 4 > image.Length || image[pe] != (byte)'P' || image[pe + 1] != (byte)'E')
      return false;

    var coff = pe + 4;
    if (coff + 20 > image.Length)
      return false;

    var numberOfSections = BitConverter.ToUInt16(image.Slice(coff + 2, 2));
    var sizeOfOptionalHeader = BitConverter.ToUInt16(image.Slice(coff + 16, 2));
    var optionalStart = coff + 20;
    if (optionalStart + sizeOfOptionalHeader > image.Length)
      return false;

    var magic = BitConverter.ToUInt16(image.Slice(optionalStart, 2));
    ulong imageBase;
    if (magic == 0x10B)
    {
      if (optionalStart + 0x1C + 4 > image.Length)
        return false;
      imageBase = BitConverter.ToUInt32(image.Slice(optionalStart + 0x1C, 4));
    }
    else if (magic == 0x20B)
    {
      if (optionalStart + 0x18 + 8 > image.Length)
        return false;
      imageBase = BitConverter.ToUInt64(image.Slice(optionalStart + 0x18, 8));
    }
    else
      return false;

    if (va < imageBase)
      return false;

    var rva = va - imageBase;
    if (rva > uint.MaxValue)
      return false;
    var rva32 = (uint)rva;

    var sectionTable = optionalStart + sizeOfOptionalHeader;
    if (sectionTable + numberOfSections * 40 > image.Length)
      return false;

    for (var s = 0; s < numberOfSections; s++)
    {
      var sh = sectionTable + s * 40;
      var virtualSize = BitConverter.ToUInt32(image.Slice(sh + 8, 4));
      var virtualAddressField = BitConverter.ToUInt32(image.Slice(sh + 12, 4));
      var sizeOfRawData = BitConverter.ToUInt32(image.Slice(sh + 16, 4));
      var pointerToRaw = BitConverter.ToUInt32(image.Slice(sh + 20, 4));

      if (virtualSize == 0)
        virtualSize = sizeOfRawData;

      if (rva32 < virtualAddressField || rva32 >= virtualAddressField + virtualSize)
        continue;

      var offsetInSection = rva32 - virtualAddressField;
      if (offsetInSection >= sizeOfRawData)
        continue;

      var off = (ulong)pointerToRaw + offsetInSection;
      if (off > int.MaxValue || (long)off >= image.Length)
        return false;
      fileOffset = (int)off;
      return true;
    }

    return false;
  }
}
