class CGame {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)"

  static 0x00441F70 DeactivateSlowMotion()  : void
  static 0x00A10B98 slowMotion              : bool

  // Gravity (in m/s^2)
  static 0x68F5F0 gravity         : float

  // Moon size
  static 0x695680 moonSize        : uint32

  // Current interior (0 = exterior)
  static 0x978810 currentInterior : uint32

  // Time scale (https://gtamods.com/wiki/015D)
  static 0x97F264 timeScale : float

  // Tick rate (in Hz)
  static 0x69102E tickRate : byte

  // Chance of traffic accidents
  static 0x68723B chanceOfTrafficAccidents : byte
}
