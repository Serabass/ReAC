
@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted")
@summary("CWanted Activity fields")
bitfield WantedActivityFlags : byte {
  0x000 bCopsIgnorePlayer
  0x001 bEveryoneIgnorePlayer
  0x002 bSWATRequired
  0x003 bFBIRequired
  0x004 bArmyRequired
}

@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted")
struct CWanted {
  module Core.Main

  static 0x004D1E20 AreArmyRequired() : bool "Checks if Army is required"
  static 0x004D1E40 AreFbiRequired() : bool "Checks if FBI is required"
  static 0x004D1E60 AreSwatRequired() : bool "Checks if SWAT is required"
  static 0x004D1E90 SetMaximumWantedLevel(int) : void "Sets the maximum wanted level"

  0x000 chaos    : uint32
  0x01E activity : WantedActivityFlags
  0x020 hudLevel : uint32
}
