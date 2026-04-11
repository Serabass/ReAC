class CPhysical : CEntity {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPhysical"
  0x004B9010 GetHasCollidedWith(CEntity*) : bool
  note fn GetHasCollidedWith "Returns whether this physical is colliding with the given entity."
  0x064 audioEntity            : uint32
  0x06C lastCollisionTime      : uint32
  0x070 moveSpeed              : CVector
  0x07C turnSpeed              : CVector
  0x088 moveForce              : CVector
  0x094 turnForce              : CVector
  0x0B8 mass                   : float
  0x0BC turnResistance         : float
  0x0C0 accelerationResistance : float
  0x0E6 collisionObjectCount   : byte
  0x100 speed                  : float
  0x104 collisionPower         : float
  0x11A flags1                 : CPhysicalFlags1
  0x11C collisionValue         : byte
  0x11D originLevel            : byte
}

bitfield CPhysicalFlags1 : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CPhysical"
  summary "First physical flags byte (VC); names from GTAMods wiki."
  0x004 bInWater
}
