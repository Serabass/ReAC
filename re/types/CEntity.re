@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CEntity")
class CEntity size 0x064 {
  module Core.Main
  @note("Sets render/collision model index (sample).")
  0x004898B0 SetModelIndex(uint) : void
  0x00487D10 GetDistanceFromCentreOfMassToBaseOfModel() : float
  0x000 vtable     : pointer
  @note(vtable "Virtual dispatch table; typical slots include destructor, Remove, Add, Render (names depend on your binary).")
  0x004 matrix     : CMatrix
  0x044 rwObject   : pointer
  0x048 rpAtomic   : pointer
  0x04C rpClump    : pointer
  0x050 typeFlags  : byte
  0x051 solidFlags : byte
  0x052 miscFlags1 : byte
  0x053 miscFlags2 : byte
  0x058 scanCode   : uint16
  0x05A randomSeed : uint16
  0x05C modelIndex : uint16
  0x05E level      : byte
  0x05F interior   : byte
}
