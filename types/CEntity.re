class CEntity size 0x64 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 vtable : pointer
  note vtable "GTAMods Wiki (CEntity): vtable dispatches virtuals — typical slots include destructor (~CEntity), Remove, Add, Render (names/indices as on the wiki table)."
  0x004 matrix : CMatrix
  0x044 rwObject : pointer
  0x048 rpAtomic : pointer
  0x04C rpClump : pointer
  0x050 typeFlags : byte
  0x051 solidFlags : byte
  0x052 miscFlags1 : byte
  0x053 miscFlags2 : byte
  0x058 scanCode : uint16
  0x05A randomSeed : uint16
  0x05C modelIndex : uint16
  0x05E level : byte
  0x05F interior : byte
}