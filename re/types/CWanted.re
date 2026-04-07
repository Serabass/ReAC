bitfield WantedActivityFlags : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted"
  summary "CWanted Activity fields"
  0 bCopsIgnorePlayer
  1 bEveryoneIgnorePlayer
  2 bSWATRequired
  3 bFBIRequired
  4 bArmyRequired
}

struct CWanted {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWanted"

  fn 0x004D1E90 SetMaximumWantedLevel(int) : void
  note fn SetMaximumWantedLevel "Illustrative; tie to your script/runtime docs if applicable."

  0x000 chaos     : uint32
  0x01E activity  : WantedActivityFlags
  0x020 hudLevel  : uint32
}
