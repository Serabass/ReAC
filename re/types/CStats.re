class CStats {
  module Core.Main
  source "https://gtamods.com/wiki/List_of_statistics_(VC)"

  static 0x978794 peopleKilledByPlayer    : uint32
  static 0x9753AC peopleKilledByOthers    : uint32
  static 0xA0D224 missionsPassed          : uint32
  static 0xA0D388 carsExploded            : uint32
  static 0x974B04 boatsExploded           : uint32
  static 0x94DB58 tyresPopped             : uint32
  static 0x9751F0 helisDestroyed          : uint32
  static 0x97F1F4 daysPassed              : uint32
  static 0x94ADD0 hiddenPackages          : HiddenPackagesStat
}

class HiddenPackagesStat {
  module Core.Main
  source "https://gtamods.com/wiki/List_of_statistics_(VC)"

  0x00 found : uint32
  0x04 total : uint32
}
