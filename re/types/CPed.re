class CPed : CPhysical {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPed"

  fn 0x004FF780 SetAmmo(eWeaponType, uint) : void
  fn 0x004FF840 GrantAmmo(eWeaponType, uint) : void
  note fn SetAmmo "Sets ammo for a weapon slot (sample)."

  static 0x94AD28 Player : CPed*
  note Player "Singleton / global; points to the active player (instance of CPed)."

  0x141 fastShoot : bool
  0x14C shootingAnim : byte
  0x354 health : float
  note health "Current health; game scripts or natives may write this field."

  0x358 armor : float
  note armor "Armor value; document script bindings in your own notes if needed."

  0x3A8 lastVehicle : pointer
  0x408 weapons : CWeapon[10]
  0x508 targetedPed : CPed*
  0x5F4 wanted : CWanted*
}
