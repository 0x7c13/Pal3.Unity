# Continuous integration

GitHub Actions builds PAL3 and PAL3A with the Unity version in
`ProjectSettings/ProjectVersion.txt`. Each variant covers Android, iOS,
macOS, Linux, and 32-bit/64-bit Windows. The workflows run only on pull requests.
New commits cancel superseded runs for the same pull request and workflow.
Successful desktop/Android builds upload artifacts.

The README badges show each workflow's latest pull request build using
`event=pull_request`, with no branch filter. Without the event filter, GitHub's
badges prefer the default branch and can keep showing old push builds after a
workflow switches to PR-only triggers. The badge links filter the build history
to pull requests as well.

The separate `Code style` workflow runs the .NET CLI in `tools/CodeStyle`, using
`.editorconfig` and the SDK pinned in `global.json`.
It calls the SDK's `dotnet format` for whitespace and StyleCop 1.3.180 layout rules
SA1107, SA1502, SA1507 and SA1516 to enforce separate statements, multiline blocks
and blank lines between members. StyleCop is restored only into the temporary
formatting project.
It parses both PAL3 and PAL3A conditional branches without Unity or game data.
Run `dotnet run --project tools/CodeStyle --` locally to format current changes,
then `dotnet run --project tools/CodeStyle -- --check` to verify them.
The formatter and its tests require no Python, Unity installation or game data.
CI runs `dotnet test tools/CodeStyle.Tests`, then uses the SDK's
`dotnet format whitespace --verify-no-changes` on both `tools/CodeStyle` and
`tools/CodeStyle.Tests` to check the tool's own sources. Finally, it selects PR changes with
`--check --base <base-sha>`; `--all` is an explicit full-source audit, not the default.
Default and `--base` runs apply formatting only to changed lines and deletion
boundaries, preserving existing formatting elsewhere. Explicit file/directory
paths and `--all` format whole files. Check mode operates on temporary copies.
This checks whitespace and layout, not semantic correctness or Unity behavior.
See the [tool README](../tools/CodeStyle/README.md) for commands and scope.

Codacy is not part of this repository's CI. Its repository webhook is disabled;
there is no Codacy workflow or Codacy-required branch protection check. Re-enabling
that external integration would resume its checks independently of these workflows.

The workflows use checkout v7.0.1, cache v6.1.0, and unity-builder v6.0.0.
The builder CLI is pinned to v0.1.73 so its behavior does not change independently
of the workflow. Update and validate this pin together with builder upgrades.

Keep the committed project configured for PAL3. PAL3A workflows switch their own
checkout before building. Do not add original game data to CI inputs.

When investigating a failure, inspect the failed job's Unity/build logs first.
Retry an isolated runner or network failure after checking the log; fix reproducible
source/build problems without bypassing the build checks. A successful CI build
checks compilation and packaging, and does not replace device or game-data runtime
validation.
