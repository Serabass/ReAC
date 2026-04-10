class CGarage {
  module Core.Main
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#Garages"
 
  static 0x7D74B8 elSwankoCasa : CGarage* "El Swanko Casa; 1 car garage."
  static 0x7D74E0 elSwankoCasa2 : CGarage*
  static 0x7D7508 elSwankoCasa3 : CGarage*
  static 0x7D7530 elSwankoCasa4 : CGarage*

  static 0x7D7558 hymanCondoLeft : CGarage* "Hyman Condo left; 4 car garage."
  static 0x7D7580 hymanCondoLeft2 : CGarage*
  static 0x7D75A8 hymanCondoLeft3 : CGarage*
  static 0x7D75D0 hymanCondoLeft4 : CGarage*

  static 0x7D75F8 hymanCondoMiddle : CGarage* "Hyman Condo middle; 2 car garage."
  static 0x7D7620 hymanCondoMiddle2 : CGarage*
  static 0x7D7648 hymanCondoMiddle3 : CGarage*
  static 0x7D7670 hymanCondoMiddle4 : CGarage*

  static 0x7D7698 hymanCondoRight : CGarage* "Hyman Condo right; 2 car garage."
  static 0x7D76C0 hymanCondoRight2 : CGarage*
  static 0x7D76E8 hymanCondoRight3 : CGarage*
  static 0x7D7710 hymanCondoRight4 : CGarage*

  static 0x7D7738 oceanHeights : CGarage* "Ocean Heights; 1 car garage."
  static 0x7D7760 oceanHeights2 : CGarage*
  static 0x7D7788 oceanHeights3 : CGarage*
  static 0x7D77B0 oceanHeights4 : CGarage*

  static 0x7D77D8 linksViewApartment : CGarage* "Links View apartment; 1 car garage."
  static 0x7D7800 linksViewApartment2 : CGarage*
  static 0x7D7828 linksViewApartment3 : CGarage*
  static 0x7D7850 linksViewApartment4 : CGarage*

  static 0x7D7878 sunshineAutosFarRight : CGarage* "Sunshine Autos far right; 2 car garage."
  static 0x7D78A0 sunshineAutosFarRight2 : CGarage*
  static 0x7D78C8 sunshineAutosFarRight3 : CGarage*
  static 0x7D78F0 sunshineAutosFarRight4 : CGarage*

  static 0x7D7918 sunshineAutosMidRight : CGarage* "Sunshine Autos mid right; 2 car garage."
  static 0x7D7940 sunshineAutosMidRight2 : CGarage*
  static 0x7D7968 sunshineAutosMidRight3 : CGarage*
  static 0x7D7990 sunshineAutosMidRight4 : CGarage*

  static 0x7D79B8 sunshineAutosMidLeft : CGarage* "Sunshine Autos mid left; 2 car garage."
  static 0x7D79E0 sunshineAutosMidLeft2 : CGarage*
  static 0x7D7A08 sunshineAutosMidLeft3 : CGarage*
  static 0x7D7A30 sunshineAutosMidLeft4 : CGarage*

  static 0x7D7A58 sunshineAutosFarLeft : CGarage* "Sunshine Autos far left; 2 car garage."
  static 0x7D7A80 sunshineAutosFarLeft2 : CGarage*
  static 0x7D7AA8 sunshineAutosFarLeft3 : CGarage*
  static 0x7D7AD0 sunshineAutosFarLeft4 : CGarage*

  static 0x7D7AF8 vercettiEstate : CGarage* "Vercetti Estate; 2 car garage."
  static 0x7D7B20 vercettiEstate2 : CGarage*
  static 0x7D7B48 vercettiEstate3 : CGarage*
  static 0x7D7B70 vercettiEstate4 : CGarage*

  0x000 ideModel : dword
  0x004 pos : CVector
  0x010 angle : CVector
  0x01C immunities : dword
  0x020 primaryColor : byte
  0x021 secondaryColor : byte
  0x022 radioStation : eRadioStation
  0x023 variation1 : byte
  0x024 variation2 : byte
  0x025 bombType : eBombType
}

enum eBombType : byte {
  source "https://gtamods.com/wiki/Memory_Addresses_(VC)#Garages"
  summary "Bomb Type"

  0 none
  1 timed
  2 engineIgnition
  3 remote
}
