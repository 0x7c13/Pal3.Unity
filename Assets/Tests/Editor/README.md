# Core protocol and source checks

Run the EditMode suite in Unity Test Runner. Run it once for each variant using
`PAL3 / Switch Variant / PAL3A` and `PAL3A / Switch Variant / PAL3`; restore PAL3
after validation. The two variants compile different command and SCN layouts.

`Assets/Tests/Game/Editor/ScriptRuntimeTests.cs` additionally runs the actual
parser, command dispatcher, script manager and variable manager without loading
a game scene. It checks per-instance local initialization and isolation, shared
globals, parallel waits (including timers behind dialogue waits), variant-specific
child calls, exception cleanup, and resets during dispatch and completion. Local
variables are never exposed by the global store's save enumeration.

`CopyrightHeaderTests` scans project-owned C# and shader sources under
`Assets/Scripts`, `Assets/Tests`, `Assets/Rendering` and `Assets/Resources/Shaders`.
The required header has 72 hyphens on each separator, the exact copyright owner
and LICENSE sentence, and a blank line before the code. UTF-8 BOM and LF/CRLF
are accepted. The test rejects malformed, reversed, redundant or future year
ranges; valid historical dates remain valid until the file is changed.

For this 2026 update, pre-existing older files use `2021-2026`; files introduced
in 2026 use `2026` alone. File creation years were checked against Git addition records without
`--follow`, which can misidentify unrelated files with similar headers as copies. Keep the original starting year when updating a file later.
The explicit upstream catalog preserves MiniLZO, the BMP loader and the bundled
[priority queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp).
Unity's generated InputActions keeps its tool-owned header; project-generated
console commands receive the standard header from their generator. Other
third-party plugin/package trees and Unity build outputs are outside these roots.

The portable tests use synthetic bytes and fixed expected values:

- SCE: all command definitions, opcode/mask uniqueness, constructor/property
  correspondence, the 16-parameter limit, deterministic parameter order, and
  separate reviewed wire-format snapshots in `Baselines/`. A snapshot change
  requires protocol review; do not regenerate it merely to make a test pass.
- SCE bytes: reversed parameters and lists, variable widths, both Chinese
  encodings, variant-specific camera parameters, block offsets and truncation.
- Binary readers: endian values, bulk reads, positions, fixed strings, short
  streams, EOF, empty input and disposal. Tests explicitly construct both readers;
  Editor's default `IFileReader` path alone exercises only the unsafe reader.
- CVD: nonzero initial mesh/material times retain their original intervals after
  rebasing, for both `cvdf` and `cvds`.
- CPK: original-game CRC vectors, sparse/deleted entries, non-default table offset,
  compressed/uncompressed payloads, preload lifecycle, case/separator/culture
  normalization, PAL3A-style segmented lookup, and short/incorrect-size LZO input.
- SCN: two consecutive NPC/object records with independently counted record sizes
  and sentinel fields, including PAL3A's extra fields and floating-point parameters.
- NAV: v1/v2, two layers, explicit offsets, portals, tile padding, winding and
  invalid versions/dimensions.

`LocalGameData` is an optional compatibility category. It searches for the current
variant under `../Game Data/PAL3` or `../Game Data/PAL3A`, relative to the Unity
project. Set `PAL3_TEST_DATA_ROOT` to override this location. With no local data,
these cases are skipped; portable regressions still run. The tests enumerate all
CPKs and read every SCE, SCN, NAV and CVD through both binary readers, compare
decoded objects and positions, and decode all supported script blocks. These are
protocol/data checks, not gameplay or device rendering validation.
They also check normalized file-system lookups against raw archive bytes, including
all three PAL3A Q02 archive segments. Of PAL3's command definitions, 147 have
parameter layouts in the available original `DecoderHelp.h`; these were compared
with the initial snapshot, accounting for variable masks and 32-bit integer width.

`EveryActiveCpkFileEntryCanBeReadAndDecompressed` additionally reads every live
file entry in every CPK, without extension or directory-tree filtering. It scans
the raw index until the header's `NumberOfFiles` live records have been counted
(including directories), skipping empty/deleted slots. `MaxFileCount` is capacity;
some PAL3A archives retain stale records in unused slots beyond the live count.
Each live file must succeed through `ReadAllBytes`, and failures fail the test.
The supplied PAL3 corpus has 43 CPKs / 154,352 files (154,308 compressed); PAL3A
has 39 CPKs / 113,195 files (113,138 compressed). All passed exact LZO output-size
validation, so the stricter check is retained for these validated datasets.

The supplied PAL3 corpus has three legacy incompatibilities, recorded explicitly
with SHA-256 checks and assertions of controlled EOF rejection:

- `basedata.cpk\SubGame\HockShop\Data\InvestHS.sce` contains 8-byte indexes;
  the current SCE layout uses 72-byte indexes.
- `T01.cpk\T01.sce` and `T02.cpk\T02.sce`, block 1404, contain opcode 119
  with only a string. The current protocol and original released command table
  require a string and three integers.

Other blocks in T01/T02 are still checked. Changed bytes or unexpected failures
fail the suite. Game files, extracted resources and test logs are never fixtures
in this repository. For PAL3, the owning original sources are `CPKCore.cpp`,
`scasmdecoder.cpp`, `DecoderHelp.h`, `scenedata.h` and `navigationinfo.cpp` in the
workspace's local `PAL3 Source` directory. PAL3A layout differences are also
checked against the supplied local data.
