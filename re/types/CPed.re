class CPed : CPhysical {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed"

  fn 0x004FF780 SetAmmo(eWeaponType, uint) : void
  fn 0x004FF840 GrantAmmo(eWeaponType, uint) : void
  note fn SetAmmo "Sets ammo for a weapon slot (sample)."

  static 0x94AD28 Player : CPed* "Singleton / global; points to the active player (instance of CPed)."

  0x141 fastShoot : bool
  0x14C shootingAnim : byte 
  0x354 health : float "Current health; game scripts or natives may write this field."

  0x358 armor : float "Armor value; document script bindings in your own notes if needed."

  // Last vehicle the ped was in
  0x3A8 lastVehicle : CVehicle*
  // Ped type
  0x3D4 pedType: EPedType
  // Weapons
  0x408 weapons : CWeapon[10]
  // Targeted ped
  0x508 targetedPed : CPed*
  // Nearest peds
  0x56C nearestPed : CPed*[10]
  // Wanted
  0x5F4 wanted : CWanted*
  // Drunkenness
  0x638 drunkenness : bool
  // Drunkenness countdown
  0x639 drunkennessCountdown : byte
  // Can be damaged
  0x63D canBeDamaged : bool
}

enum ePedType : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed"
  summary "Ped type enum"
  0 PLAYER1 "Primary player definition"
  1 PLAYER2 "Secondary player definition"
  6 COP "Cop"
  // ...
}
