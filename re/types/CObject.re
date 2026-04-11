@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject")
@note("Layout follows GTAMods Memory_Addresses_(VC) CObject; matDummyInitial is 72 B in that game.")
class CObject : CPhysical {
  module Core.Main
  0x120 matDummyInitial           : byte[72]
  0x168 fAttachForce              : float
  0x16C byteObjectType            : byte
  0x16D objectFlags1              : CObjectFlags1
  0x16E objectFlags2              : CObjectFlags2
  0x16F bytePickupObjectBonusType : byte
  0x170 wPickupObjectQuantity     : uint16
  0x174 fDamageMultiplier         : float
  0x178 byteCollisionDamageType   : byte
  0x179 byteSpecialCollisionType  : byte
  0x17A byteCameraAvoids          : byte
  0x17B byteBounceScore           : byte
  0x180 dwObjectTimer             : uint32
  0x184 wRefModelId               : uint16
  0x188 pInitialSurface           : CEntity*
  0x18C pContactPhysical          : CPhysical*
  0x190 byteVehicleMainColor      : byte
  0x191 byteVehicleExtraColor     : byte
}

bitfield CObjectFlags1 : byte {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject")
  @summary("First object flags byte (VC); names from GTAMods wiki.")
  0x000 bIsPickupObject
  0x001 bDoCircleEffect
  0x002 bRenderPickupQuantity
  0x003 bRenderPickupAvailability
  0x004 bWindowMinorCollisionDamage
  0x005 bHasWindowBeenBrokenByMelee
  0x006 bHasObjectExplosionTriggered
  0x007 bIsVehicleComponent
}

bitfield CObjectFlags2 : byte {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject")
  @summary("Second object flags byte (VC); names from GTAMods wiki.")
  0x000 bSpecialLighting
  0x001 bNoVehicleCollisionWhenDetached
}
