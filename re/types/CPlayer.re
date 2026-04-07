class CPlayer : CPed {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed"

  // Player's money (in dollars)
  static 0x94ADC8 playerMoney     : uint32

  // Player can sprint infinitely
  static 0x94AE68 infiniteSprint  : bool
}
