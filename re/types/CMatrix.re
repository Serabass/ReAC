@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CMatrix")
struct CMatrix size 0x040 {
  module Core.Main
  0x000 right : CVector
  0x010 up    : CVector
  0x020 at    : CVector
  0x030 pos   : CVector
}
