class CPed : CPhysical {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x354 health : float
  note health "Wiki table: current health; script/native SET_CHAR_HEALTH ultimately writes this field."
  0x358 armor : float
  note armor "Armor float; related script entry points (e.g. SET_CHAR_ARMOUR) are documented separately on the wiki."
  0x3A8 lastVehicle : pointer
  0x408 weapons : CWeapon[10]
  0x508 targetedPed : CPed*
  0x5F4 wanted : CWanted*
}
