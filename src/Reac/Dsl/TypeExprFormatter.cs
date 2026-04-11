using System.Text;
using Reac.Ir;

namespace Reac.Dsl;

public static class TypeExprFormatter
{
  public static string Format(TypeExpr e)
  {
    var sb = new StringBuilder();
    Append(e, sb);
    return sb.ToString();
  }

  public static void Append(TypeExpr e, StringBuilder sb)
  {
    switch (e)
    {
      case TypeExpr.Scalar s:
        sb.Append(s.Name);
        break;
      case TypeExpr.Named n:
        sb.Append(n.Name);
        break;
      case TypeExpr.Pointer p:
        Append(p.Inner, sb);
        sb.Append('*');
        break;
      case TypeExpr.Array a:
        Append(a.Element, sb);
        sb.Append('[').Append(a.Length).Append(']');
        break;
      case TypeExpr.Generic g:
        sb.Append(g.Name).Append('<');
        for (var i = 0; i < g.TypeArguments.Count; i++)
        {
          if (i > 0)
            sb.Append(", ");
          Append(g.TypeArguments[i], sb);
        }

        sb.Append('>');
        break;
      default:
        sb.Append('?');
        break;
    }
  }
}
