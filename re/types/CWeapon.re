enum eWeaponType : uint32 {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon")
  @summary("Illustrative weapon type ids")
  0x000 unarmed "No weapon equipped."
  0x001 baseballBat "Baseball bat"
  0x016 pistol "Standard sidearm."
}

enum eWeaponStatus : uint32 {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon")
  @summary("Illustrative weapon status ids")
  0x000 normal
  0x001 firing1stpersonOrFlamethrower "firing in 1st person/using flamethrower"
  0x002 reloading
  0x003 noAmmo
}

@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CWeapon")
struct CWeapon {
  module Core.Main
  @note("Illustrative native entry; replace address/signature for your build.")
  0x005D45E0 Fire(CEntity*, CVector*) : void
  0x000 weaponType : eWeaponType
  0x004 status     : eWeaponStatus
  0x008 clip       : uint32
  0x00C ammo       : uint32
}
