class CObject : CPhysical size 0x1A0 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"

  note "Layout follows GTAMods Memory_Addresses_(VC) CObject; matDummyInitial is 72 B in that game."

  0x120 matDummyInitial : byte[72]
  0x168 fAttachForce : float
  0x16C byteObjectType : byte
  0x16D objectFlags1 : CObjectObjectFlags1
  0x16E objectFlags2 : CObjectObjectFlags2
  0x16F bytePickupObjectBonusType : byte
  0x170 wPickupObjectQuantity : uint16
  0x172 _padding172 : byte[2]
  0x174 fDamageMultiplier : float
  0x178 byteCollisionDamageType : byte
  0x179 byteSpecialCollisionType : byte
  0x17A byteCameraAvoids : byte
  0x17B byteBounceScore : byte
  0x17C _padding17C : byte[4]
  0x180 dwObjectTimer : uint32
  0x184 wRefModelId : uint16
  0x186 _padding186 : byte[2]
  0x188 pInitialSurface : CEntity*
  0x18C pContactPhysical : CPhysical*
  0x190 byteVehicleMainColor : byte
  0x191 byteVehicleExtraColor : byte
  0x192 _padding192 : byte[2]
  0x194 _padTo1A0 : byte[12]
}
