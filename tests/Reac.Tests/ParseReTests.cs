using Reac.Dsl;
using Reac.Ir;

namespace Reac.Tests;

public class ParseReTests
{
    [Fact]
    public void ParseFieldLine_pointer_and_inheritance()
    {
        const string src = """
            class CPed : CPhysical size 0x6D8 {
              module GTA.Core
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
}
