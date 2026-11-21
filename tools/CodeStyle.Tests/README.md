# Code style regression tests

Run the formatter and its tests with the SDK pinned by the repository:

```sh
dotnet test tools/CodeStyle.Tests
```

The suite uses NUnit and the real .NET CLI; Python and Unity are not required.
CLI cases create isolated Git repositories, invoke the built formatter, and
compile and execute literal fixtures for both `PAL3` and `PAL3A`. Unit cases
cover selective whitespace boundaries and the final file-write preflight.

By default fixtures live in the operating system temporary directory. Set
`PAL3_CODE_STYLE_TEST_ROOT` to place them in a chosen QA directory, and set
`PAL3_CODE_STYLE_KEEP_FIXTURES=1` to retain them for diagnosis. Child processes
receive the fixture as their temporary directory and have a three-minute limit.
