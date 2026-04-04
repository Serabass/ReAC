struct CWeapon size 0x18 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  fn 0x005D45E0 Fire(CEntity*, CVector*) : void // wiki: CWeapon::Fire
  note fn Fire "Address and signature from Function_Memory_Addresses_(VC); thiscall on CWeapon instance."
  0x000 weaponType : uint32
  0x004 status : uint32
  0x008 clip : uint32
  0x00C ammo : uint32
}