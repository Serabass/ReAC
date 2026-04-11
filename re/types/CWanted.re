bitfield WantedActivityFlags : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted"
  summary "CWanted Activity fields"
  0x000 bCopsIgnorePlayer
  0x001 bEveryoneIgnorePlayer
  0x002 bSWATRequired
  0x003 bFBIRequired
  0x004 bArmyRequired
}

struct CWanted {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted"
  0x004D1E90 SetMaximumWantedLevel(int) : void "Sets the maximum wanted level"
  0x004D1E90 AreArmyRequired() : bool "Checks if Army is required"
  0x004D1E40 AreFbiRequired() : bool "Checks if FBI is required"
  0x004D1E60 AreSwatRequired() : bool "Checks if SWAT is required"
  0x000 chaos    : uint32
  0x01E activity : WantedActivityFlags
  0x020 hudLevel : uint32
}
