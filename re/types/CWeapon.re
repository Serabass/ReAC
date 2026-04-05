enum eWeaponType : uint32 {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon"
  summary "Illustrative weapon type ids"
  0 unarmed "No weapon equipped."
  1 baseballBat
  22 pistol "Standard sidearm."
}

enum eWeaponStatus : uint32 {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon"
  summary "Illustrative weapon status ids"
  0 normal
  1 firing1stpersonOrFlamethrower "firing in 1st person/using flamethrower"
  2 reloading
  3 noAmmo
}

struct CWeapon size 0x18 {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon"

  fn 0x005D45E0 Fire(CEntity*, CVector*) : void // sample: main fire dispatch
  note fn Fire "Illustrative native entry; replace address/signature for your build."

  0x000 weaponType : eWeaponType
  0x004 status : eWeaponStatus
  0x008 clip : uint32
  0x00C ammo : uint32
}
