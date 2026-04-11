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
  public void TypeExprParser_generics_pointer_and_array()
  {
    var g = TypeExprParser.Parse("FuncPtr<int>");
    var gen = Assert.IsType<TypeExpr.Generic>(g);
    Assert.Equal("FuncPtr", gen.Name);
    Assert.IsType<TypeExpr.Scalar>(Assert.Single(gen.TypeArguments));

    var p = TypeExprParser.Parse("FuncPtr<uint32>*");
    var ptr = Assert.IsType<TypeExpr.Pointer>(p);
    Assert.IsType<TypeExpr.Generic>(ptr.Inner);

    var a = TypeExprParser.Parse("FuncPtr<int>[4]");
    var arr = Assert.IsType<TypeExpr.Array>(a);
    Assert.Equal(4, arr.Length);
    Assert.IsType<TypeExpr.Generic>(arr.Element);
  }

  [Fact]
  public void TypeExprParser_double_and_triple_pointer_CPed()
  {
    var t = TypeExprParser.Parse("CPed**");
    var p1 = Assert.IsType<TypeExpr.Pointer>(t);
    var p2 = Assert.IsType<TypeExpr.Pointer>(p1.Inner);
    var n = Assert.IsType<TypeExpr.Named>(p2.Inner);
    Assert.Equal("CPed", n.Name);

    var t3 = TypeExprParser.Parse("CPed***");
    var a = Assert.IsType<TypeExpr.Pointer>(t3);
    var b = Assert.IsType<TypeExpr.Pointer>(a.Inner);
    var c = Assert.IsType<TypeExpr.Pointer>(b.Inner);
    var ped3 = Assert.IsType<TypeExpr.Named>(c.Inner);
    Assert.Equal("CPed", ped3.Name);
  }

  [Fact]
  public void TypeExprParser_pointer_scalar_double_star()
  {
    var t = TypeExprParser.Parse("uint32**");
    var p1 = Assert.IsType<TypeExpr.Pointer>(t);
    var p2 = Assert.IsType<TypeExpr.Pointer>(p1.Inner);
    Assert.IsType<TypeExpr.Scalar>(p2.Inner);
  }

  [Fact]
  public void TypeExprParser_nested_generics_triple_FuncPtr()
  {
    const string s = "FuncPtr<FuncPtr<FuncPtr<int, float>>, int, float>";
    var root = TypeExprParser.Parse(s);
    var g0 = Assert.IsType<TypeExpr.Generic>(root);
    Assert.Equal("FuncPtr", g0.Name);
    Assert.Equal(3, g0.TypeArguments.Count);

    var g1 = Assert.IsType<TypeExpr.Generic>(g0.TypeArguments[0]);
    Assert.Equal("FuncPtr", g1.Name);
    var mid = Assert.Single(g1.TypeArguments);
    var g2 = Assert.IsType<TypeExpr.Generic>(mid);
    Assert.Equal("FuncPtr", g2.Name);
    Assert.Equal(2, g2.TypeArguments.Count);
    Assert.IsType<TypeExpr.Scalar>(g2.TypeArguments[0]);
    Assert.IsType<TypeExpr.Scalar>(g2.TypeArguments[1]);

    Assert.IsType<TypeExpr.Scalar>(g0.TypeArguments[1]);
    Assert.IsType<TypeExpr.Scalar>(g0.TypeArguments[2]);
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
    Assert.Equal(
      new (int, string, string?)[] { (0, "a", null), (1, "b", null) },
      bf.Bits
    );
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

  [Fact]
  public void Parse_static_fn_native_same_as_fn()
  {
    const string src = """
      class C {
        module M
        static 0x0041C2F0 CreateMapCouldMoveInThisArea(float, float) : int
        static 0x0041D350 IsThisVehicleInteresting (CVehicle* vehicle) : bool // note
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fns = td.Body.OfType<ReBodyLine.FunctionLine>().ToList();
    Assert.Equal(2, fns.Count);
    Assert.Equal(0x0041C2F0, fns[0].Address);
    Assert.Equal("CreateMapCouldMoveInThisArea", fns[0].Name);
    Assert.Equal("float, float", fns[0].Parameters);
    Assert.Equal("int", fns[0].ReturnType);
    Assert.Null(fns[0].Note);
    Assert.Equal(0x0041D350, fns[1].Address);
    Assert.Equal("IsThisVehicleInteresting", fns[1].Name);
    Assert.Equal("CVehicle* vehicle", fns[1].Parameters);
    Assert.Equal("bool", fns[1].ReturnType);
    Assert.Equal("note", fns[1].Note);
  }

  [Fact]
  public void Parse_bare_hex_fn_native_without_fn_keyword()
  {
    const string src = """
      class G {
        module M
        0x0042E900 Close() : uint8
        0x00451550 Process(): int
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fns = td.Body.OfType<ReBodyLine.FunctionLine>().ToList();
    Assert.Equal(2, fns.Count);
    Assert.Equal(0x0042E900, fns[0].Address);
    Assert.Equal("Close", fns[0].Name);
    Assert.Equal("", fns[0].Parameters);
    Assert.Equal("uint8", fns[0].ReturnType);
    Assert.Equal(0x00451550, fns[1].Address);
    Assert.Equal("Process", fns[1].Name);
    Assert.Equal("", fns[1].Parameters);
    Assert.Equal("int", fns[1].ReturnType);
  }

  [Fact]
  public void Parse_hex_fn_no_keyword_SetModelIndex_shape()
  {
    const string src = """
      class E {
        module M
        0x004898B0 SetModelIndex(uint) : void
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fn = Assert.Single(td.Body.OfType<ReBodyLine.FunctionLine>());
    Assert.Equal(0x004898B0, fn.Address);
    Assert.Equal("SetModelIndex", fn.Name);
    Assert.Equal("uint", fn.Parameters);
    Assert.Equal("void", fn.ReturnType);
  }

  [Fact]
  public void Parse_field_generic_type_not_function()
  {
    const string src = """
      struct T {
        module M
        0x20 x : FuncPtr<int>
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    Assert.Empty(td.Body.OfType<ReBodyLine.FunctionLine>());
    var fl = Assert.Single(td.Body.OfType<ReBodyLine.FieldLine>());
    Assert.Equal(0x20, fl.Offset);
    Assert.Equal("x", fl.Name);
    var gen = Assert.IsType<TypeExpr.Generic>(fl.Type);
    Assert.Equal("FuncPtr", gen.Name);
    Assert.IsType<TypeExpr.Scalar>(Assert.Single(gen.TypeArguments));
  }

  [Fact]
  public void Parse_fn_generic_in_parameters_string()
  {
    const string src = """
      struct T {
        module M
        0x0 F(int, FuncPtr<void, void>) : void
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fn = Assert.Single(td.Body.OfType<ReBodyLine.FunctionLine>());
    Assert.Equal("int, FuncPtr<void, void>", fn.Parameters);
  }

  [Fact]
  public void Parse_inline_bitfield_block()
  {
    const string src = """
      class Obj {
        module M
        0x10 flags : bitfield : byte {
          summary "byte of flags"
          0 bA "first"
          1 bB
        }
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var inl = Assert.Single(td.Body.OfType<ReBodyLine.InlineBitfieldFieldLine>());
    Assert.Equal(0x10, inl.Offset);
    Assert.Equal("flags", inl.FieldName);
    Assert.Equal("byte", inl.StorageName);
    Assert.Equal("byte of flags", inl.Summary);
    Assert.Equal(2, inl.Bits.Count);
    Assert.Equal((0, "bA", "first"), inl.Bits[0]);
    Assert.Equal((1, "bB", null), inl.Bits[1]);
  }

  [Fact]
  public void Parse_native_function_decorators_stack()
  {
    const string src = """
      struct S {
        module M
        @stdcall
        @nothrow
        0x00451550 Process(): int
      }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var td = Assert.IsType<ReTopLevel.TypeDef>(Assert.Single(doc));
    var fn = Assert.Single(td.Body.OfType<ReBodyLine.FunctionLine>());
    Assert.Equal(new[] { "stdcall", "nothrow" }, fn.Decorators);
  }

  [Fact]
  public void Parse_bitfield_top_level_bit_descriptions()
  {
    const string src = """
      bitfield BF : byte {
        0 a "alpha"
      }
      struct T size 0x4 { module M 0x0 f : BF }
      """;
    var doc = ReDocumentParser.ParseDocument(src);
    var bf = Assert.IsType<ReTopLevel.BitfieldDef>(doc[0]);
    Assert.Single(bf.Bits);
    Assert.Equal((0, "a", "alpha"), bf.Bits[0]);
  }
}
