namespace Reac;

internal static class InitFiles
{
    internal const string ProjectToml = """
name = "reknow-dda-vc"
version = "0.1.0"
active_target = "gta_vc_1_0_win32"

[paths]
targets_dir = "targets"
modules_dir = "modules"
types_dir = "types"
docs_dir = "docs"
generated_dir = "generated"
""";

    internal static readonly (string Path, string Content)[] AllFiles =
    [
        ("targets/gta_vc_1_0_win32.re", TargetRe),
        ("modules/GTA.Core.re", ModuleGta),
        ("modules/RenderWare.Core.re", ModuleRw),
        ("types/CVector.re", CVector),
        ("types/CMatrix.re", CMatrix),
        ("types/CWeapon.re", CWeapon),
        ("types/CWanted.re", CWanted),
        ("types/CEntity.re", CEntity),
        ("types/CPhysical.re", CPhysical),
        ("types/CPed.re", CPed),
        ("docs/Overview.rdoc", OverviewRdoc),
        ("docs/GTA_VC_Memory_Model.rdoc", MemoryModelRdoc)
    ];

    private const string TargetRe = """
target gta_vc_1_0_win32 {
  pointer_size_bytes 4
  game "GTA Vice City"
  version "1.0"
  platform "win32"
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
}
""";

    private const string ModuleGta = """
module GTA.Core {
  summary "GTA Vice City core runtime types (MVP sample)."
}
""";

    private const string ModuleRw = """
module RenderWare.Core {
  summary "RenderWare-related placeholders (MVP)."
}
""";

    private const string CVector = """
struct CVector size 0x0C {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 x : float
  0x004 y : float
  0x008 z : float
}
""";

    private const string CMatrix = """
struct CMatrix size 0x40 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 right : CVector
  0x010 up : CVector
  0x020 at : CVector
  0x030 pos : CVector
}
""";

    private const string CWeapon = """
struct CWeapon size 0x18 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 weaponType : uint32
  0x004 status : uint32
  0x008 clip : uint32
  0x00C ammo : uint32
}
""";

    private const string CWanted = """
struct CWanted size 0x24 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 chaos : uint32
  0x01E activity : byte
  0x020 hudLevel : uint32
}
""";

    private const string CEntity = """
class CEntity size 0x64 {
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x000 vtable : pointer
  note vtable "GTAMods Wiki (CEntity): vtable dispatches virtuals — typical slots include destructor (~CEntity), Remove, Add, Render (names/indices as on the wiki table)."
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
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
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
  module GTA.Core
  source "https://gtamods.com/wiki/Memory_Addresses_%28VC%29"
  0x354 health : float
  note health "Wiki table: current health; script/native SET_CHAR_HEALTH ultimately writes this field."
  0x358 armor : float
  note armor "Armor float; related script entry points (e.g. SET_CHAR_ARMOUR) are documented separately on the wiki."
  0x3A8 lastVehicle : pointer
  0x408 weapons : CWeapon[10]
  0x508 targetedPed : CPed*
  0x5F4 wanted : CWanted*
}
""";

    private const string OverviewRdoc = """
document Overview {
  title "Overview"
  summary "MVP documentation for REaC GTA VC sample."
  references {
    ref CEntity
    ref CPed
  }
  section Intro {
    text "This project stores reverse-engineering knowledge as text files."
  }
}
""";

    private const string MemoryModelRdoc = """
document GTA_VC_Memory_Model {
  title "GTA Vice City memory model overview"
  references {
    ref CEntity
    ref CPhysical
    ref CPed
    ref CVector
    ref CMatrix
  }
  summary "Imported layout entities align with GTAMods Wiki tables."
  section Inheritance {
    text "CPed extends CPhysical; CPhysical extends CEntity."
  }
  section Wiki_functions {
    text "Two documentation patterns from GTAMods Wiki: (1) Virtual methods on CEntity are reached via the vtable pointer at +0x00 (e.g. Remove, Add — see the CEntity section on Memory_Addresses_(VC)). (2) Script-facing operations such as SET_CHAR_HEALTH / SET_CHAR_ARMOUR correspond to the health and armor floats on CPed in the wiki table, even though the opcode names are not struct fields."
  }
}
""";
}
