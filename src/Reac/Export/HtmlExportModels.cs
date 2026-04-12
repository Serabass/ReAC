namespace Reac.Export;

internal sealed class NavTypeNode
{
  public string Name { get; init; } = "";
  public string Href { get; init; } = "";
  public bool Current { get; init; }
  public List<NavTypeNode> Children { get; init; } = new();
  public bool HasChildren => Children.Count > 0;
}

internal sealed record NavSidebarItemVm(string Href, string Label, bool Current);

/// <summary>Type tree as nested dictionaries so Scriban can read fields in <c>nav_type_node</c> (plain CLR graphs render empty).</summary>
internal sealed record SidebarNavVm(
  string HomeHref,
  IReadOnlyList<Dictionary<string, object>> TypeRoots,
  IReadOnlyList<NavSidebarItemVm> Bitfields,
  IReadOnlyList<NavSidebarItemVm> Enums,
  IReadOnlyList<NavSidebarItemVm> Docs
);

internal sealed record ProvenanceUrlVm(string Url, bool IsHttp);

internal sealed record ProvenanceTemplateVm(
  string FilePath,
  bool HasUrls,
  IReadOnlyList<ProvenanceUrlVm> SourceUrls
);

internal sealed record FieldNoteVm(
  bool HasNote,
  string? Note,
  bool HasBits,
  IReadOnlyList<FlagBitVm> FlagBits,
  bool HasEnums,
  IReadOnlyList<EnumValVm> EnumValues,
  bool UseNoteSpoiler,
  int NoteLineCount,
  bool UseBitsSpoiler,
  int BitsCount,
  bool UseEnumsSpoiler,
  int EnumsCount
);

internal sealed record FlagBitVm(int Bit, string Name, string? Description);

internal sealed record EnumValVm(ulong Value, string Name, string? Description);

internal sealed record TableRow4Vm(
  string C1,
  string NamePlain,
  string TypeHtml,
  string NoteHtml,
  string SnapshotText = ""
);

internal sealed record TypeAncestorVm(
  string Name,
  string PageHref,
  IReadOnlyList<TableRow4Vm> InstanceRows,
  IReadOnlyList<TableRow4Vm> StaticRows,
  bool HasStaticRows
);

internal sealed record TableRow5Vm(
  string Addr,
  string DeclaringType,
  string NamePlain,
  string TypeHtml,
  string NoteHtml,
  string SnapshotText = ""
);

internal sealed record NativeFnRowVm(
  string AddrHex,
  string NamePlain,
  string ParamsPlain,
  string ReturnPlain,
  string NotePlain,
  string DecoratorsPlain,
  /// <summary>HTML for the Note column (may include <c>details.note-spoiler</c>).</summary>
  string NoteCellHtml
);

internal sealed record NativeFnSectionVm(string TypeName, IReadOnlyList<NativeFnRowVm> Rows);

internal sealed record FlatRowVm(
  bool IsStaticRow,
  string Cell1Html,
  string NamePlain,
  string TypeHtml,
  string DeclaringPlain,
  string LayoutText,
  string SnapshotText = ""
);

internal sealed record GroupStaticRowVm(
  string AddrPlain,
  string NamePlain,
  string TypeHtml,
  string NoteHtml,
  string SnapshotText = ""
);

internal sealed record GroupedSectionVm(
  string TypeName,
  IReadOnlyList<TableRow4Vm> InstanceRows,
  IReadOnlyList<GroupStaticRowVm> StaticRows
);

internal sealed record DocRefRow(string Href, string Label);

internal sealed record DocSectionRow(string Name, string Text);

internal sealed record TypePageMainVm(
  string Name,
  string KindLabel,
  string SizeText,
  bool SizeInferred,
  string ParentName,
  string InheritanceChainText,
  bool ShowAncestorSection,
  IReadOnlyList<TypeAncestorVm> Ancestors,
  IReadOnlyList<TableRow4Vm> OwnFieldRows,
  bool ShowStaticHierarchy,
  IReadOnlyList<TableRow5Vm> StaticHierarchyRows,
  string ProvenanceHtml,
  bool HasAnyNativeFn,
  IReadOnlyList<NativeFnSectionVm> NativeFnSections,
  IReadOnlyList<FlatRowVm> FlatRows,
  IReadOnlyList<GroupedSectionVm> GroupedSections,
  bool UnresolvedEmpty,
  IReadOnlyList<string> Unresolved
);
