@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed")
class CPed : CPhysical {
  module Core.Main
  @note("Sets ammo for a weapon slot (sample).")
  0x004FF780 SetAmmo(eWeaponType, uint) : void
  0x004FF840 GrantAmmo(eWeaponType, uint) : void
  static 0x0094AD28 Player : CPed* "Singleton / global; points to the active player (instance of CPed)."
  0x141 fastShoot            : bool
  0x14C shootingAnim         : byte
  0x354 health               : float "Current health; game scripts or natives may write this field."
  0x358 armor                : float "Armor value; document script bindings in your own notes if needed."
  0x3A8 lastVehicle          : CVehicle*
  0x3D4 pedType              : EPedType
  0x408 weapons              : CWeapon[10]
  0x508 targetedPed          : CPed*
  0x56C nearestPed           : CPed*[10]
  0x5F4 wanted               : CWanted*
  0x638 drunkenness          : bool
  0x639 drunkennessCountdown : byte
  0x63D canBeDamaged         : bool
}

enum ePedType : byte {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed")
  @summary("Ped type enum")
  0x000 PLAYER1 "Primary player definition"
  0x001 PLAYER2 "Secondary player definition"
  0x006 COP "Cop"
}
