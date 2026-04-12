@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed")
class CPolice {
  module Core.Main

  static 0x00A10ADB heliState : bool

  // Cars
  static 0x00426A21 firstCar    : IDE_CAR
  static 0x00426987 secondCar   : IDE_CAR
  static 0x0042697E viceChee1   : IDE_CAR
  static 0x004268B8 viceChee2   : IDE_CAR
  static 0x004268EE viceChee3   : IDE_CAR
  static 0x00426909 viceChee4   : IDE_CAR
  static 0x004269BA fbiRancher  : IDE_CAR
  static 0x00426A0A barracksOl  : IDE_CAR
  static 0x00426A14 rhino       : IDE_CAR

  // Ped Models
  static 0x004ED762 policeManSkin   : IDE_PED
  static 0x004ED76B policeManSkin2  : IDE_PED
  static 0x004ED7C3 swatSkin        : IDE_PED
  static 0x004ED812 fbiSkin         : IDE_PED
  static 0x004ED834 armySkin        : IDE_PED
}
