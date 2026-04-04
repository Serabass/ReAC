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
        ("types/CPed.re", CPed),
        ("docs/Overview.rdoc", OverviewRdoc),
        ("docs/Sample_Memory_Model.rdoc", MemoryModelRdoc)
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
  summary "Illustrative module for REaC samples (game-agnostic tooling; data is only an example)."
}
""";

    private const string ModuleRw = """
module RenderWare.Core {
  summary "Third-party engine placeholders (optional; rename or remove in your KB)."
}
""";

    private const string CVector = """
struct CVector size 0x0C {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  0x000 x : float
  0x004 y : float
  0x008 z : float
}
""";

    private const string CMatrix = """
struct CMatrix size 0x40 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  0x000 right : CVector
  0x010 up : CVector
  0x020 at : CVector
  0x030 pos : CVector
}
""";

    private const string CWeapon = """
struct CWeapon size 0x18 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x005D45E0 Fire(CEntity*, CVector*) : void // sample: main fire dispatch
  note fn Fire "Illustrative native entry; replace address/signature for your build."
  0x000 weaponType : uint32
  0x004 status : uint32
  0x008 clip : uint32
  0x00C ammo : uint32
}
""";

    private const string CWanted = """
struct CWanted size 0x24 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x004D1E90 SetMaximumWantedLevel(int) : void
  note fn SetMaximumWantedLevel "Illustrative; tie to your script/runtime docs if applicable."
  0x000 chaos : uint32
  0x01E activity : byte
  0x020 hudLevel : uint32
}
""";

    private const string CEntity = """
class CEntity size 0x64 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x004898B0 SetModelIndex(uint) : void
  note fn SetModelIndex "Sets render/collision model index (sample)."
  fn 0x00487D10 GetDistanceFromCentreOfMassToBaseOfModel() : float
  0x000 vtable : pointer
  note vtable "Virtual dispatch table; typical slots include destructor, Remove, Add, Render (names depend on your binary)."
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
class CPhysical : CEntity size 0x120 {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x004B9010 GetHasCollidedWith(CEntity*) : bool
  note fn GetHasCollidedWith "Returns whether this physical is colliding with the given entity."
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

    private const string CPed = """
class CPed : CPhysical {
  module Sample.Core
  source "https://example.com/reverse/sample-memory"
  source "https://example.com/reverse/sample-functions"
  fn 0x004FF780 SetAmmo(eWeaponType, uint) : void
  fn 0x004FF840 GrantAmmo(eWeaponType, uint) : void
  note fn SetAmmo "Sets ammo for a weapon slot (sample)."
  0x354 health : float
  note health "Current health; game scripts or natives may write this field."
  0x358 armor : float
  note armor "Armor value; document script bindings in your own notes if needed."
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
}
""";

    private const string MemoryModelRdoc = """
document Sample_Memory_Model {
  title "Sample memory model (illustrative)"
  references {
    ref CEntity
    ref CPhysical
    ref CPed
    ref CVector
    ref CMatrix
  }
  summary "Example .re types showing inheritance, field offsets, and optional native function entry points. Replace sources and names with your binary's provenance."
  section Inheritance {
    text "Example chain: CPed extends CPhysical; CPhysical extends CEntity — common in game engines, not mandatory for REaC."
  }
  section Fields_and_functions {
    text "Struct fields use hex offsets; native calls are documented separately with fn lines (addresses and signatures are opaque text for your target ABI)."
  }
  section Function_entry_points {
    text "When you document exports or addresses, list them as fn entries on the relevant type. Cross-link external write-ups in source lines or notes."
  }
}
""";
}
