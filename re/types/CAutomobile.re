@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CAutomobile")
class CAutomobile : CVehicle {
  module Core.Main
  0x2A4 engineState          : byte
  0x2A5 wheelsState          : CWheelsState
  0x501 specialProps         : SpecialVehicleProps
  0x5C5 numWheelsOnGround    : byte
  0x5CC burnout              : byte
}

@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CAutomobile")
@summary("Wheel State")
enum eWheelState : byte {
  0x000 normal
  0x001 popped
  0x002 none
}

bitfield SpecialVehicleProps : byte {
  0x000 taxiLight
  0x001 notSprayable
  0x003 watertight
  0x004 upsideNotDamaged
  0x005 bitMoreResistantToPhysicalDamage
  0x006 tankDetonateCars
}

class CWheelsState {
  0x000 leftFrontWheelState  : eWheelState
  0x001 leftRearWheelState   : eWheelState
  0x002 rightFrontWheelState : eWheelState
  0x003 rightRearWheelState  : eWheelState
}
