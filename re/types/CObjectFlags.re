
bitfield CObjectFlags1 : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject"
  summary "First object flags byte (VC); names from GTAMods wiki."
  0 bIsPickupObject
  1 bDoCircleEffect
  2 bRenderPickupQuantity
  3 bRenderPickupAvailability
  4 bWindowMinorCollisionDamage
  5 bHasWindowBeenBrokenByMelee
  6 bHasObjectExplosionTriggered
  7 bIsVehicleComponent
}

bitfield CObjectFlags2 : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject"
  summary "Second object flags byte (VC); names from GTAMods wiki."
  0 bSpecialLighting
  1 bNoVehicleCollisionWhenDetached
}
