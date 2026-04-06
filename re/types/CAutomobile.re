class CAutomobile : CVehicle {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CAutomobile"

  0x2A4 engineState : byte

  0x2A5 leftFrontWheelState : eWheelState
  0x2A6 leftRearWheelState : eWheelState
  0x2A7 rightFrontWheelState : eWheelState
  0x2A8 rightRearWheelState : eWheelState

  0x501 specialProps : SpecialVehicleProps
  0x5C5 numWheelsOnGround : byte
  0x5CC burnout : byte
}

enum eWheelState : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CAutomobile"
  summary "Wheel State"
  0 normal
  1 popped
  2 none
}

bitfield SpecialVehicleProps : byte {
  0 taxiLight
  1 notSprayable
  3 watertight 
  4 upsideNotDamaged
  5 bitMoreResistantToPhysicalDamage 
  6 tankDetonateCars 
}
