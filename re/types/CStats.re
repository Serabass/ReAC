@source("https://gtamods.com/wiki/List_of_statistics_(VC)")
@source("https://gtamods.com/wiki/Function_Memory_Addresses_(VC)#CStats")
class CStats {
  module Core.Main
  0x004CE3FB Init() : int
  static 0x00978794 peopleKilledByPlayer : uint32
  static 0x009753AC peopleKilledByOthers : uint32
  static 0x00A0D224 missionsPassed : uint32
  static 0x00A0D388 carsExploded : uint32
  static 0x00974B04 boatsExploded : uint32
  static 0x0094DB58 tyresPopped : uint32
  static 0x009751F0 helisDestroyed : uint32
  static 0x0097F1F4 daysPassed : uint32
  static 0x0094ADD0 hiddenPackages : HiddenPackagesStat
  static 0x009B5F8C cheatedCount : uint32
}

@source("https://gtamods.com/wiki/List_of_statistics_(VC)")
class HiddenPackagesStat {
  module Core.Main
  0x000 found : uint32 "hidden packages found"
  0x004 total : uint32 "total number of packages"
}
