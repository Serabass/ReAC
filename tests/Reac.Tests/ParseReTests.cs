using Reac.Dsl;
using Reac.Ir;

namespace Reac.Tests;

public class ParseReTests
{
  [Fact]
  public void Parse_field_quoted_note_with_semicolon_is_not_part_of_type()
  {
    const string src = """
      class T {
        module M
        static 0x7D74B8 g : CGarage* "El Swanko Casa; 1 car garage."
        0x10 armor : float "Armor; see wiki."
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var lines = td.Body.ToList();
    var st = Assert.IsType<ReBodyLine.StaticFieldLine>(lines[1]);
    Assert.IsType<TypeExpr.Pointer>(st.Type);
    Assert.Equal("El Swanko Casa; 1 car garage.", st.Note);
    var fl = Assert.IsType<ReBodyLine.FieldLine>(lines[2]);
    Assert.IsType<TypeExpr.Scalar>(fl.Type);
    Assert.Equal("Armor; see wiki.", fl.Note);
  }

  [Fact]
  public void Parse_static_field_absolute_address()
  {
    const string src = """
      class CPed : CPhysical {
        module Sample.Core
        static 0x94AD28 Player : CPed* // global
        note Player "doc note wins over slash comment"
        0x10 health : float
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var lines = td.Body.ToList();
    var st = Assert.IsType<ReBodyLine.StaticFieldLine>(lines[1]);
    Assert.Equal(0x94AD28UL, st.Address);
    Assert.Equal("Player", st.Name);
    Assert.IsType<TypeExpr.Pointer>(st.Type);
    Assert.Equal("global", st.Note);
    var notePlayer = Assert.IsType<ReBodyLine.NoteFieldLine>(lines[2]);
    Assert.Equal("Player", notePlayer.FieldName);
    var fl = Assert.IsType<ReBodyLine.FieldLine>(lines[3]);
    Assert.Equal(0x10, fl.Offset);
  }

  [Fact]
  public void ParseFieldLine_pointer_and_inheritance()
  {
    const string src = """
      class CPed : CPhysical size 0x6D8 {
        module Sample.Core
        0x354 health : float
        0x508 targetedPed : CPed*
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var t = Assert.Single(doc);
    var type = Assert.IsType<ReTopLevel.TypeDef>(t);
    Assert.Equal("CPed", type.Name);
    Assert.Equal("CPhysical", type.Parent);
    var fields = type.Body.OfType<ReBodyLine.FieldLine>().ToList();
    Assert.Equal(2, fields.Count);
    var ptr = Assert.IsType<TypeExpr.Pointer>(fields[1].Type);
    Assert.IsType<TypeExpr.Named>(ptr.Inner);
  }

  [Fact]
  public void ParseType_optional_size_omitted()
  {
    const string src = """
      class A : B {
        module M
        0x10 x : uint32
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    Assert.Null(td.DeclaredSize);
  }

  [Fact]
  public void TypeExprParser_array_and_named()
  {
    var e = TypeExprParser.Parse("CWeapon[10]");
    var arr = Assert.IsType<TypeExpr.Array>(e);
    Assert.Equal(10, arr.Length);
    Assert.IsType<TypeExpr.Named>(arr.Element);
  }

  [Fact]
  public void Parse_enum_top_level_optional_descriptions_and_field_reference()
  {
    const string src = """
      enum MyKind : byte {
        summary "Kinds of thing."
        0 Alpha "First."
        10 Beta
      }
      struct T size 0x10 {
        module M
        0x0 kind : MyKind
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    Assert.Equal(2, doc.Count);
    var en = Assert.IsType<ReTopLevel.EnumDef>(doc[0]);
    Assert.Equal("MyKind", en.Name);
    Assert.Equal("byte", en.StorageName);
    Assert.Equal(2, en.Values.Count);
    Assert.Equal((0UL, "Alpha", "First."), en.Values[0]);
    Assert.Equal((10UL, "Beta", null), en.Values[1]);
    var td = Assert.IsType<ReTopLevel.TypeDef>(doc[1]);
    var fl = Assert.Single(td.Body.OfType<ReBodyLine.FieldLine>());
    Assert.IsType<TypeExpr.Named>(fl.Type);
    Assert.Equal("MyKind", ((TypeExpr.Named)fl.Type).Name);
  }

  [Fact]
  public void Parse_bitfield_top_level_and_field_reference()
  {
    const string src = """
      bitfield MyFlags : byte {
        0 a
        1 b
      }
      struct T size 0x10 {
        module M
        0x0 flags : MyFlags
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    Assert.Equal(2, doc.Count);
    var bf = Assert.IsType<ReTopLevel.BitfieldDef>(doc[0]);
    Assert.Equal("MyFlags", bf.Name);
    Assert.Equal("byte", bf.StorageName);
    Assert.Equal(new[] { (0, "a"), (1, "b") }, bf.Bits);
    var td = Assert.IsType<ReTopLevel.TypeDef>(doc[1]);
    var fl = Assert.Single(td.Body.OfType<ReBodyLine.FieldLine>());
    Assert.IsType<TypeExpr.Named>(fl.Type);
    Assert.Equal("MyFlags", ((TypeExpr.Named)fl.Type).Name);
  }

  [Fact]
  public void ParseType_preserves_multiple_source_lines_order()
  {
    const string src = """
      struct T size 0x4 {
        module M
        source "https://a.example/mem"
        source "https://b.example/fn"
        0x0 x : uint32
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var urls = td.Body.OfType<ReBodyLine.SourceLine>().Select(s => s.Url).ToList();
    Assert.Equal(new[] { "https://a.example/mem", "https://b.example/fn" }, urls);
  }

  [Fact]
  public void Parse_fn_native_and_note_fn()
  {
    const string src = """
      struct S {
        module M
        fn 0x005D45E0 Fire(CEntity*, CVector*) : void // inline
        note fn Fire "long note"
        fn 0x004FF780 SetAmmo(eWeaponType, uint) : void
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fns = td.Body.OfType<ReBodyLine.FunctionLine>().ToList();
    Assert.Equal(2, fns.Count);
    Assert.Equal(0x005D45E0, fns[0].Address);
    Assert.Equal("Fire", fns[0].Name);
    Assert.Equal("CEntity*, CVector*", fns[0].Parameters);
    Assert.Equal("void", fns[0].ReturnType);
    Assert.Equal("inline", fns[0].Note);
    var nfn = Assert.Single(td.Body.OfType<ReBodyLine.NoteFunctionLine>());
    Assert.Equal("Fire", nfn.FunctionName);
    Assert.Equal("long note", nfn.Text);
  }
}
