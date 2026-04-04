using Reac.Ir;
using Reac.Layout;

namespace Reac.Tests;

public class TypeSizeInferenceTests
{
    [Fact]
    public void Infers_child_extent_from_parent_and_own_fields()
    {
        var a = new TypeDecl
        {
            Name = "A",
            Kind = TypeKind.Class,
            ParentName = null,
            DeclaredSize = 16,
            Size = 16,
            OwnFields = Array.Empty<FieldDecl>(),
            FilePath = "a.re"
        };
        var b = new TypeDecl
        {
            Name = "B",
            Kind = TypeKind.Class,
            ParentName = "A",
            DeclaredSize = null,
            Size = 0,
            OwnFields =
            [
                new FieldDecl { Offset = 20, Name = "z", Type = new TypeExpr.Scalar("uint32") }
            ],
            FilePath = "b.re"
        };
        var list = new List<TypeDecl> { a, b };
        var result = TypeSizeInference.Apply(list, 4);
        var bb = result.First(t => t.Name == "B");
        Assert.Equal(24, bb.Size);
    }
}
