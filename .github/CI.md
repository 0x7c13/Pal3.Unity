# Continuous integration

GitHub Actions builds PAL3 and PAL3A with the Unity version in
`ProjectSettings/ProjectVersion.txt`. Each variant covers Android, iOS,
macOS, Linux, and 32-bit/64-bit Windows. The workflows run on pushes and pull
requests, and successful desktop/Android builds upload artifacts.

Codacy is not part of this repository's CI. Its repository webhook is disabled;
there is no Codacy workflow or Codacy-required branch protection check. Re-enabling
that external integration would resume its checks independently of these workflows.

Keep the committed project configured for PAL3. PAL3A workflows switch their own
checkout before building. Public builds reject imported private VFX and legacy Toon assets;
do not add them or original game data to CI inputs.

When investigating a failure, inspect the failed job's Unity/build logs first.
Retry an isolated runner or network failure after checking the log; fix reproducible
source/build problems without bypassing the build checks. A successful CI build
checks compilation and packaging, and does not replace device or game-data runtime
validation.
