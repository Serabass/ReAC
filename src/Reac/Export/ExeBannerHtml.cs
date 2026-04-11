using System.Linq;
using System.Net;
using Reac.Ir;

namespace Reac.Export;

internal static class ExeBannerHtml
{
  public static string Build(ProjectIr project)
  {
    var m = project.Modules.FirstOrDefault(x => x.Name == "Core.Main");
    if (m?.ExePath == null || m.ExeSha256Hex == null)
      return "";

    var lines = new List<string>
    {
      "<p><strong>Game module (exe)</strong></p>",
      $"<p class=\"exe-line\">Configured path: <code>{WebUtility.HtmlEncode(m.ExePath)}</code></p>",
    };

    if (m.ExeResolvedFullPath != null)
      lines.Add(
        $"<p class=\"exe-line\">Resolved: <code>{WebUtility.HtmlEncode(m.ExeResolvedFullPath)}</code></p>"
      );

    if (m.ExeFilePresent)
    {
      lines.Add("<p class=\"exe-line exe-ok\">Status: file present, SHA-256 verified.</p>");
      if (m.ExeActualSha256Hex != null)
        lines.Add(
          $"<p class=\"exe-line\"><span class=\"prov\">SHA-256:</span> <code>{WebUtility.HtmlEncode(m.ExeActualSha256Hex)}</code></p>"
        );
    }
    else
    {
      lines.Add(
        "<p class=\"exe-line exe-warn\">Status: file not found (gitignored or not copied yet). Static field values from the binary are not available in export.</p>"
      );
      lines.Add(
        $"<p class=\"exe-line\"><span class=\"prov\">Expected SHA-256 (when file is present):</span> <code>{WebUtility.HtmlEncode(m.ExeSha256Hex)}</code></p>"
      );
    }

    return string.Join("\n", lines);
  }
}
