namespace Reac;

internal static class InitFiles
{
  internal const string ProjectToml = """
name = "reknow-dda"
version = "0.1.0"
active_target = "example_win32"

[paths]
targets_dir = "targets"
modules_dir = "modules"
types_dir = "types"
docs_dir = "docs"
# Single .rdoc entry; body may use #include "relative.rdoc" (fragments only, not a nested document block).
docs_entry = "docs/Overview.rdoc"
generated_dir = "generated"
""";

  internal static readonly (string Path, string Content)[] AllFiles =
  [
    ("targets/example_win32.re", TargetRe),
    ("modules/Sample.Core.re", ModuleSample),
    ("modules/RenderWare.Core.re", ModuleRw),
    ("types/CVector.re", CVector),
    ("types/CMatrix.re", CMatrix),
    ("types/CWeapon.re", CWeapon),
    ("types/CWanted.re", CWanted),
    ("types/CEntity.re", CEntity),
    ("types/CPhysical.re", CPhysical),
    ("types/CObjectFlags.re", CObjectFlags),
    ("types/EWeaponType.re", EWeaponType),
    ("types/CObject.re", CObject),
    ("types/CPed.re", CPed),
    ("docs/Overview.rdoc", OverviewRdoc),
    ("docs/parts/Sample_Memory_Model.rdoc", MemoryModelFragmentRdoc),
  ];

  private const string TargetRe = """
target example_win32 {
  pointer_size_bytes 4
  game "Sample"
  version "1.0"
  platform "win32"
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
}
""";

  private const string ModuleSample = """
module Sample.Core {
  @summary("Illustrative module for REaC samples (game-agnostic tooling; data is only an example).")
}
""";

  private const string ModuleRw = """
module RenderWare.Core {
  @summary("Third-party engine placeholders (optional; rename or remove in your KB).")
}
""";

  private const string CVector = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
struct CVector size 0x0C {
  module Sample.Core
  0x000 x : float
  0x004 y : float
  0x008 z : float
}
""";

  private const string CMatrix = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
struct CMatrix size 0x40 {
  module Sample.Core
  0x000 right : CVector
  0x010 up : CVector
  0x020 at : CVector
  0x030 pos : CVector
}
""";

  private const string CWeapon = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
struct CWeapon size 0x18 {
  module Sample.Core
  @note("Illustrative native entry; replace address/signature for your build.")
  fn 0x005D45E0 Fire(CEntity*, CVector*) : void // sample: main fire dispatch
  0x000 weaponType : eWeaponType
  0x004 status : uint32
  0x008 clip : uint32
  0x00C ammo : uint32
}
""";

  private const string CWanted = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
struct CWanted size 0x24 {
  module Sample.Core
  @note("Illustrative; tie to your script/runtime docs if applicable.")
  fn 0x004D1E90 SetMaximumWantedLevel(int) : void
  0x000 chaos : uint32
  0x01E activity : byte
  0x020 hudLevel : uint32
}
""";

  private const string CEntity = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
class CEntity size 0x64 {
  module Sample.Core
  @note("Sets render/collision model index (sample).")
  fn 0x004898B0 SetModelIndex(uint) : void
  fn 0x00487D10 GetDistanceFromCentreOfMassToBaseOfModel() : float
  0x000 vtable : pointer
  @note(vtable "Virtual dispatch table; typical slots include destructor, Remove, Add, Render (names depend on your binary).")
  0x004 matrix : CMatrix
  0x044 rwObject : pointer
  0x048 rpAtomic : pointer
  0x04C rpClump : pointer
  0x050 typeFlags : byte
  0x051 solidFlags : byte
  0x052 miscFlags1 : byte
  0x053 miscFlags2 : byte
  0x058 scanCode : uint16
  0x05A randomSeed : uint16
  0x05C modelIndex : uint16
  0x05E level : byte
  0x05F interior : byte
}
""";

  private const string CPhysical = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
class CPhysical : CEntity size 0x120 {
  module Sample.Core
  @note("Returns whether this physical is colliding with the given entity.")
  fn 0x004B9010 GetHasCollidedWith(CEntity*) : bool
  0x064 audioEntity : uint32
  0x06C lastCollisionTime : uint32
  0x070 moveSpeed : CVector
  0x07C turnSpeed : CVector
  0x088 moveForce : CVector
  0x094 turnForce : CVector
  0x0B8 mass : float
  0x0BC turnResistance : float
  0x0C0 accelerationResistance : float
  0x0E6 collisionObjectCount : byte
  0x100 speed : float
  0x104 collisionPower : float
  0x11A inWater : byte
  0x11C collisionValue : byte
  0x11D originLevel : byte
}
""";

  private const string CObjectFlags = """
bitfield CObjectFlags1 : byte {
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject")
  @summary("First object flags byte (VC); names from GTAMods wiki.")
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
  @source("https://gtamods.com/wiki/Memory_Addresses_(VC)#CObject")
  @summary("Second object flags byte (VC); names from GTAMods wiki.")
  0 bSpecialLighting
  1 bNoVehicleCollisionWhenDetached
}

""";

  private const string EWeaponType = """
enum eWeaponType : uint32 {
  @source("https://example.com/reverse/sample-memory")
  @summary("Illustrative weapon type ids (sample; replace with your game's table).")
  0 WEAPON_UNARMED "No weapon equipped."
  1 WEAPON_BASEBALLBAT
  22 WEAPON_PISTOL "Standard sidearm."
}

""";

  private const string CObject = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
@note("Layout follows GTAMods Memory_Addresses_(VC) CObject; matDummyInitial is 72 B in that game.")
class CObject : CPhysical size 0x1A0 {
  module Sample.Core
  0x120 matDummyInitial : byte[72]
  0x168 fAttachForce : float
  0x16C byteObjectType : byte
  0x16D objectFlags1 : CObjectFlags1
  0x16E objectFlags2 : CObjectFlags2
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
""";

  private const string CPed = """
@source("https://example.com/reverse/sample-memory")
@source("https://example.com/reverse/sample-functions")
class CPed : CPhysical {
  module Sample.Core
  @note("Sets ammo for a weapon slot (sample).")
  fn 0x004FF780 SetAmmo(eWeaponType, uint) : void
  fn 0x004FF840 GrantAmmo(eWeaponType, uint) : void
  static 0x94AD28 Player : CPed*
  @note(Player "Singleton / global; points to the active player (instance of CPed).")
  0x354 health : float
  @note(health "Current health; game scripts or natives may write this field.")
  0x358 armor : float
  @note(armor "Armor value; document script bindings in your own notes if needed.")
  0x3A8 lastVehicle : pointer
  0x408 weapons : CWeapon[10]
  0x508 targetedPed : CPed*
  0x5F4 wanted : CWanted*
}
""";

  private const string OverviewRdoc = """
document Overview {
  title "Overview"
  summary "REaC stores reverse-engineering knowledge as text: types, targets, modules, and documents. This repo includes an illustrative sample KB."
  references {
    ref CEntity
    ref CPed
  }
  section Intro {
    text "Author types in .re, validate layout, export HTML. No specific game or importer is required."
  }
  #include "parts/Sample_Memory_Model.rdoc"
}
""";

  private const string MemoryModelFragmentRdoc = """
  references {
    ref CEntity
    ref CPhysical
    ref CPed
    ref CPlayer
    ref CVector
    ref CMatrix
    ref CObject
    ref CObjectFlags1
    ref CObjectFlags2
    ref eWeaponType
  }
  section Memory_model {
    text "Example .re types showing inheritance, field offsets, optional native function entry points, and bitfield annotations. Replace sources and names with your binary's provenance."
  }
  section Inheritance {
    text "Example chain: CPed extends CPhysical; CPhysical extends CEntity; CObject extends CPhysical — illustrative only."
  }
  section Fields_and_functions {
    text "Struct fields use hex offsets; native calls are documented separately with fn lines (addresses and signatures are opaque text for your target ABI)."
  }
  section Bitfields {
    text "Define named flag layouts as top-level bitfield Name : StorageScalar { N bitName; ... } where StorageScalar is any fixed-size scalar (byte, uint16, uint32, uint64, float, double, pointer, …). Bit indices are 0 .. 8*size-1. Optionally use a separate .re file; then use that name as the field type. Examples: CObjectFlags1, CObjectFlags2."
  }
  section Enums {
    text "Top-level enum Name : StorageScalar { N EnumeratorName optional-quoted-description; ... }. Values are unsigned and must fit the storage width. Use the enum name as a field type (layout uses the underlying scalar). Example: eWeaponType on CWeapon.weaponType."
  }
  section Function_entry_points {
    text "When you document exports or addresses, list them as fn entries on the relevant type. Cross-link external write-ups in source lines or notes."
  }
""";
}
