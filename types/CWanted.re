struct CWanted size 0x24 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x004D1E90 SetMaximumWantedLevel(int) : void
  note fn SetMaximumWantedLevel "Illustrative; tie to your script/runtime docs if applicable."
  0x000 chaos : uint32
  0x01E activity : byte
  0x020 hudLevel : uint32
}
