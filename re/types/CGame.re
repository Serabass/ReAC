class CGame {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)"

  static 0x68F5F0 gravity : float
  static 0x695680 moonSize : uint32

  // https://gtamods.com/wiki/015D
  static 0x97F264 timeScale : float

  static 0x68723B chanceOfTrafficAccidents : byte
}
