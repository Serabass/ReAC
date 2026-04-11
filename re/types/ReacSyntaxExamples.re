class ReacSyntaxExamples {
  module Core.Main
  source "https://example.com/reac/syntax-examples"
  note "Synthetic layout for documenting language features; not tied to a real binary."
  0x000 whenPredefined  : uint32
  0x004 whenDefinedInRe : uint16
  0x006 elseBranchDemo  : byte
  0x008 inlineFlags     : bitfield : byte {
    source "https://example.com/reac/inline-bitfield"
    summary "Same block rules as top-level bitfield (source / summary / note / bit lines)."
    0x000 bOne "description in quotes on a bit line"
    0x001 bTwo
  }

  @stdcall
  @nothrow
  0x01000000 ExampleDecoratedNative() : void
}
