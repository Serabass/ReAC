@source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle")
class CVehicle : CPhysical size 0x00C {
  module Core.Main
  static 0x0069A61A carFriction : uint8
  0x23C currentRadioStation : eRadioStation
  0x240 horn                : bool
  0x245 siren               : bool
  0x29C vehicleType         : eVehicleType
}

enum eVehicleType : dword {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle")
  @summary("Vehicle Type")
  0x000 general
  0x001 boat
  0x002 train
  0x003 npc_police_heli
  0x004 npc_plane
  0x005 bike
}

enum eRadioStation : byte {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle")
  @summary("Radio Station")
  0x000 wildStyle
  0x001 flashFm
  0x002 kChat
  0x003 fever105
  0x004 vRock
  0x005 vcpr
  0x006 espantoso
  0x007 emotion983
  0x008 wave103
  0x009 mp3
}
