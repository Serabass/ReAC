struct CWanted size 0x24 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  fn 0x004D1E90 SetMaximumWantedLevel(int) : void
  note fn SetMaximumWantedLevel "Script-facing cap for max wanted (wiki ties to opcode 01F0)."
  0x000 chaos : uint32
  0x01E activity : byte
  0x020 hudLevel : uint32
}