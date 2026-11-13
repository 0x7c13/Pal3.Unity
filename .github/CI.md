# Continuous integration

GitHub Actions builds PAL3 and PAL3A with the Unity version in
`ProjectSettings/ProjectVersion.txt`. Each variant covers Android, iOS,
macOS, Linux, and 32-bit/64-bit Windows. The workflows run only on pull requests.
New commits cancel superseded runs for the same pull request and workflow.
Successful desktop/Android builds upload artifacts.

The separate `Code style` workflow verifies changed owned C# files with the SDK's
`dotnet format`, using `.editorconfig` and the SDK pinned in `global.json`.
It combines whitespace formatting with pinned StyleCop layout rules SA1107, SA1502,
SA1507 and SA1516 to enforce separate statements, multiline blocks and blank lines
between members. StyleCop is restored only into the temporary formatting project.
It parses both PAL3 and PAL3A conditional branches without Unity, game data or private
assets. Run `python3 tools/format-csharp.py` locally to format current changes, then
`python3 tools/format-csharp.py --check` to verify them. CI selects PR changes with
`--check --base <base-sha>`; `--all` is an explicit full-source audit, not the default.
This checks whitespace and layout, not semantic correctness or Unity behavior.

Codacy is not part of this repository's CI. Its repository webhook is disabled;
there is no Codacy workflow or Codacy-required branch protection check. Re-enabling
that external integration would resume its checks independently of these workflows.

The workflows use checkout v7.0.1, cache v6.1.0, and unity-builder v6.0.0.
The builder CLI is pinned to v0.1.73 so its behavior does not change independently
of the workflow. Update and validate this pin together with builder upgrades.

Keep the committed project configured for PAL3. PAL3A workflows switch their own
checkout before building. Public builds reject imported private VFX and legacy Toon assets;
do not add them or original game data to CI inputs.

When investigating a failure, inspect the failed job's Unity/build logs first.
Retry an isolated runner or network failure after checking the log; fix reproducible
source/build problems without bypassing the build checks. A successful CI build
checks compilation and packaging, and does not replace device or game-data runtime
validation.
