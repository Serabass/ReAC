// REaC DSL examples (preprocessor, inline bitfield, @decorators on native functions).
// Predefined symbols: see [preprocessor.defines] in project.toml at repo root.

#define REAC_DEFINE_FROM_RE 1

class ReacSyntaxExamples {
  module Core.Main
  source "https://example.com/reac/syntax-examples"
  note "Synthetic layout for documenting language features; not tied to a real binary."

#ifdef REAC_SYNTAX_EXAMPLES
  // Included when REAC_SYNTAX_EXAMPLES is set in project.toml [preprocessor.defines]
  0x00 whenPredefined : uint32
#endif

#ifdef REAC_DEFINE_FROM_RE
  // Branch kept after global #define REAC_DEFINE_FROM_RE 1 (collected across .re files)
  0x04 whenDefinedInRe : uint16
#endif

#ifndef REAC_UNDEF_FOR_ELSE_DEMO
  0x06 elseBranchDemo : byte
#else
  0x06 elseBranchDemo : uint32
#endif

  // Inline bitfield: generates a synthetic bitfield type ReacSyntaxExamples_inlineFlags (HTML + IR)
  0x08 inlineFlags : bitfield : byte {
    summary "Same block rules as top-level bitfield (source / summary / note / bit lines)."
    source "https://example.com/reac/inline-bitfield"
    0 bOne "description in quotes on a bit line"
    1 bTwo
  }

  @stdcall
  @nothrow
  0x01000000 ExampleDecoratedNative() : void
}
