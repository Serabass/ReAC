class CVehicle : CPhysical size 0x0C {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle"

  static 0x69A61A carFriction : uint8

  0x23C currentRadioStation : eRadioStation
  0x240 horn : bool
  0x245 siren : bool
  0x29C vehicleType : eVehicleType
}

enum eVehicleType : dword {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle"
  summary "Vehicle Type"

  0 general
  1 boat
  2 train
  3 npc_police_heli
  4 npc_plane
  5 bike
}

enum eRadioStation : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#CVehicle"
  summary "Radio Station"
  0 wildStyle
  1 flashFm
  2 kChat
  3 fever105
  4 vRock
  5 vcpr
  6 espantoso
  7 emotion983
  8 wave103
  9 mp3
}
