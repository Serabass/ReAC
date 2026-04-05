struct CWeapon size 0x18 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x005D45E0 Fire(CEntity*, CVector*) : void // sample: main fire dispatch
  note fn Fire "Illustrative native entry; replace address/signature for your build."
  0x000 weaponType : uint32
  0x004 status : uint32
  0x008 clip : uint32
  0x00C ammo : uint32
}
