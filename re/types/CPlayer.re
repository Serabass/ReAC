@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed")
class CPlayer : CPed {
  module Core.Main
  static 0x0094AD28 Player : CPed* "Singleton / global; points to the active player (instance of CPed)."

  static 0x0094ADC8 playerMoney : uint32
  static 0x0094AE68 infiniteSprint : bool
}
