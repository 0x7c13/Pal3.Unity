// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle.Tests;

using NUnit.Framework;

public sealed class CliTests
{
    private const string Source = "Assets/Scripts/效果 layout.cs";
    private const string Good = "internal sealed class Layout\r\n{\r\n#if PAL3\r\n    public int AOne() => 1;\r\n\r\n    public int ATwo() => 2;\r\n#elif PAL3A\r\n    public int BOne() => 1;\r\n\r\n    public int BTwo() => 2;\r\n#endif\r\n}\r\n";

    [Test]
    public void CheckIsReadOnlyAndBothVariantsAndBaseModeRepairMemberGaps()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Good);
        string baseline = fixture.Commit();
        fixture.Write(Source, Good.Replace(";\r\n\r\n", ";\r\n"));
        byte[] original = fixture.Read(Source);
        Assert.That(fixture.Format(false, "--check").Output, Does.Contain("Formatting required:"));
        Assert.That(fixture.Read(Source), Is.EqualTo(original));
        fixture.Format();
        Assert.That(fixture.ReadText(Source), Is.EqualTo(Good));
        fixture.Format(true, "--check", "--all");
        fixture.Write(Source, Good.Replace("BOne() => 1;\r\n\r\n", "BOne() => 1;\r\n"));
        fixture.Format(false, "--check", "--base", baseline);
        fixture.Format();
        fixture.Format(true, "--check", "--base", baseline);
    }

    [Test]
    public void SelectiveFormattingLeavesInheritedViolationsAndExplicitPathsAuditWholeFile()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/Legacy.cs";
        const string baseline = "class Legacy\r\n{\r\n    int  Untouched;\r\n\r\n    int Changed;\r\n}\r\n";
        fixture.Write(source, baseline);
        fixture.Commit();
        fixture.Write(source, baseline.Replace("int Changed", "int   Changed"));
        byte[] original = fixture.Read(source);
        fixture.Format(false, "--check");
        Assert.That(fixture.Read(source), Is.EqualTo(original));
        fixture.Format();
        Assert.That(fixture.ReadText(source), Is.EqualTo(baseline));
        fixture.Format(true, "--check");
        fixture.Format(false, "--check", source);
        fixture.Format(true, source);
        fixture.Format(true, "--check", source);
        Assert.That(fixture.ReadText(source), Does.Contain("int Untouched;"));
    }

    [Test]
    public void IncrementalFormattingAcceptsUnicodeSpacesAndPreservesUntouchedText()
    {
        using var fixture = new RepositoryFixture();
        const string text = "class Layout\r\n{\r\n    int\u00a0First = 1;\r\n    int\u3000Second = 2; // 注\u3000释\r\n}\r\n";
        fixture.Write(Source, text);
        fixture.Commit();
        fixture.Write(Source, text.Replace("2;", "3;"));
        fixture.Format(false, "--check");
        fixture.Format();
        Assert.That(fixture.ReadText(Source), Is.EqualTo(text.Replace("int\u3000Second = 2", "int Second = 3")));
        fixture.Format(true, "--check");
    }

    [Test]
    public void NewOwnedFilesAreFormattedWhileThirdPartyGeneratedAndOutsidePathsRemainUntouched()
    {
        using var fixture = new RepositoryFixture();
        string[] excluded = { "Assets/Plugins/Keep.cs", "Assets/Packages/Keep.cs", "Library/Keep.cs", "Logs/Keep.cs", "obj/Keep.cs", "tools/Keep.cs", "Elsewhere.cs" };
        const string bad = "class Keep{void One(){}void Two(){}}";
        foreach (string path in excluded)
            fixture.Write(path, bad);
        fixture.Write("Assets/Scripts/Tracked.cs", Good);
        fixture.Commit();
        fixture.Write("Assets/Tests/New test.cs", "class NewTest{int  A;}\n");
        const string staged = "Assets/Scripts/Staged.cs";
        fixture.Write(staged, "class Staged{int  B;}\n");
        fixture.Run("git", "add", "--", staged);
        fixture.Format();
        fixture.Format(true, "--check", "--all");
        Assert.That(fixture.ReadText("Assets/Tests/New test.cs"), Does.Contain("int A;"));
        Assert.That(fixture.ReadText(staged), Does.Contain("int B;"));
        foreach (string path in excluded)
            Assert.That(fixture.ReadText(path), Is.EqualTo(bad), path);
    }

    [Test]
    public void InvalidArgumentCombinationsAndEscapingPathsDoNotWriteSources()
    {
        using var fixture = new RepositoryFixture();
        const string original = "class Bad{int  A;}\n";
        fixture.Write(Source, original);
        fixture.Commit();
        foreach (string[] arguments in new[]
        {
            new[] { "--all", "--base", "HEAD" },
            new[] { "--all", Source },
            new[] { "--base", "HEAD", Source },
            new[] { "../Outside.cs" },
            new[] { "--unknown" }
        })
        {
            fixture.Format(false, arguments);
            Assert.That(fixture.ReadText(Source), Is.EqualTo(original));
        }
    }

    [TestCase("")]
    [TestCase("  ")]
    [TestCase("-b")]
    [TestCase("--check")]
    public void InvalidBaseValuesFailAsUsageErrors(string value)
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Good);
        fixture.Commit();
        var result = fixture.Format(false, "--base", value);
        Assert.That(result.ExitCode, Is.EqualTo(2));
        Assert.That(result.Output, Does.Contain("--base requires one Git commit."));
    }

    [Test]
    public void DivergedBaseUsesTheBranchPointForFileAndLineSelection()
    {
        using var fixture = new RepositoryFixture();
        const string upstreamOnly = "Assets/Tests/UpstreamOnly.cs";
        string legacy = Good.Replace("int ATwo", "int   ATwo");
        fixture.Write(Source, legacy);
        fixture.Write(upstreamOnly, "class UpstreamOnly{int  A;}\n");
        string fork = fixture.Commit();

        fixture.Run("git", "checkout", "-q", "-b", "upstream");
        fixture.Write(Source, Good);
        fixture.Write(upstreamOnly, "class UpstreamOnly{int A;}\n");
        fixture.Commit();

        fixture.Run("git", "checkout", "-q", "-b", "topic", fork);
        fixture.Write(Source, legacy.Replace("int BTwo", "int   BTwo"));
        fixture.Commit();
        var result = fixture.Format(true, "--base", "upstream");
        Assert.That(result.Output, Does.Contain("Formatting 1 C# files"));
        Assert.That(fixture.ReadText(Source), Is.EqualTo(legacy));
        Assert.That(fixture.ReadText(upstreamOnly), Is.EqualTo("class UpstreamOnly{int  A;}\n"));
        fixture.Format(true, "--check", "--base", "upstream");
    }

    [Test]
    public void ExplicitPathCasingFollowsTheFilesystem()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/Case/One.cs";
        const string alias = "assets/scripts/case/one.cs";
        fixture.Write(source, "class One{int  A;}\n");
        fixture.Commit();

        if (File.Exists(fixture.PathFor(alias)))
        {
            fixture.Format(false, "--check", alias);
            fixture.Format(true, "assets/SCRIPTS/CASE");
            Assert.That(fixture.ReadText(source), Does.Contain("int A;"));
        }
        else
        {
            fixture.Write("Assets/Scripts/Case/one.cs", "class Lower{int  B;}\n");
            fixture.Format(true, "Assets/Scripts/Case/one.cs");
            Assert.That(fixture.ReadText(source), Is.EqualTo("class One{int  A;}\n"));
            Assert.That(fixture.ReadText("Assets/Scripts/Case/one.cs"), Does.Contain("int B;"));
        }
    }

    [Test]
    public void EmptyOwnedSelectionExitsSuccessfullyWithoutFormattingExcludedFiles()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write("Assets/Plugins/Only.cs", "class Only{int  A;}");
        fixture.Commit();
        fixture.Format(true, "--check");
        Assert.That(fixture.ReadText("Assets/Plugins/Only.cs"), Is.EqualTo("class Only{int  A;}"));
    }
    [Test]
    public void BaseModeIncludesCommittedEditsThatDefaultModeLeavesOutsideSelection()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Good);
        string baseline = fixture.Commit();
        fixture.Write(Source, Good.Replace("int ATwo", "int   ATwo"));
        fixture.Commit();
        fixture.Format(true, "--check");
        fixture.Format(false, "--check", "--base", baseline);
        fixture.Format(true, "--base", baseline);
        fixture.Format(true, "--check", "--base", baseline);
        Assert.That(fixture.ReadText(Source), Is.EqualTo(Good));
    }

    [Test]
    public void RenamedFilesPreserveInheritedFormattingAndDeletedFilesStayExcluded()
    {
        using var fixture = new RepositoryFixture();
        const string original = "Assets/Scripts/Before.cs", renamed = "Assets/Scripts/After [α] #1.cs", deleted = "Assets/Tests/Deleted.cs";
        const string content = "class Renamed\n{\n    int  Untouched;\n\n    int Changed;\n}\n";
        fixture.Write(original, content);
        fixture.Write(deleted, "class Deleted{int  B;}\n");
        string baseline = fixture.Commit();
        fixture.Run("git", "mv", original, renamed);
        fixture.Run("git", "rm", "--quiet", deleted);
        fixture.Format(true, "--check");
        fixture.Format();
        Assert.That(fixture.ReadText(renamed), Is.EqualTo(content));

        fixture.Write(renamed, content.Replace("int Changed", "int   Edited"));
        fixture.Commit();
        fixture.Format(false, "--check", "--base", baseline);
        fixture.Format(true, "--base", baseline);
        Assert.That(fixture.ReadText(renamed), Is.EqualTo(content.Replace("int Changed", "int Edited")));
        fixture.Format(true, "--check", "--base", baseline);
        Assert.That(File.Exists(fixture.PathFor(original)), Is.False);
        Assert.That(File.Exists(fixture.PathFor(deleted)), Is.False);
    }

    [Test]
    public void DirectorySelectionAcceptsTheTrailingSeparatorAndChecksItsFiles()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Good);
        fixture.Commit();
        fixture.Write(Source, Good.Replace(";\r\n\r\n", ";\r\n"));
        byte[] original = fixture.Read(Source);
        fixture.Format(false, "--check", "Assets/Scripts");
        fixture.Format(false, "--check", "Assets/Scripts/");
        Assert.That(fixture.Read(Source), Is.EqualTo(original));
        fixture.Format(true, "Assets/Scripts/");
        Assert.That(fixture.ReadText(Source), Is.EqualTo(Good));
    }

    [Test]
    public void MsBuildReservedCharactersInFileNamesRemainLiteral()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write("README.txt", "Fixture repository");
        fixture.Commit();
        string[] names = { "A;B.cs", "A$(Missing).cs", "A@(Compile).cs", "A%25.cs", "A'中文.cs" };
        for (int index = 0; index < names.Length; index++)
            fixture.Write("Assets/Scripts/" + names[index], "class Punctuation" + index + "{int  A;}\n");
        fixture.Format(false, "--check");
        fixture.Format();
        fixture.Format(true, "--check");
        foreach (string name in names)
            Assert.That(fixture.ReadText("Assets/Scripts/" + name), Does.Contain("int A;"), name);
    }

    [Test]
    public void ExplicitPathsResolveFromTheInvokingDirectoryAndAcceptAncestorAliases()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Good.Replace("=> 1;", "=>  1;"));
        fixture.Commit();
        fixture.FormatFrom(fixture.PathFor("Assets/Scripts"), true, Path.GetFileName(Source));
        Assert.That(fixture.ReadText(Source), Is.EqualTo(Good));
        fixture.Write(Source, Good.Replace("=> 1;", "=>  1;"));
        string alias = fixture.Root + "-parent-alias";
        fixture.Link(alias, Path.GetDirectoryName(fixture.Root)!, directory: true);
        try
        {
            string throughParentAlias = Path.Combine(alias, Path.GetFileName(fixture.Root));
            fixture.Format(true, Path.Combine(throughParentAlias, Source));
            Assert.That(fixture.ReadText(Source), Is.EqualTo(Good));
            fixture.Link("back-to-parent", "..", directory: true);
            try
            {
                var rejected = fixture.Format(false, Path.Combine(throughParentAlias, "back-to-parent", Path.GetFileName(fixture.Root), Source));
                Assert.That(rejected.Output, Does.Contain("Symbolic-link paths are not formatted"));
            }
            finally
            {
                Directory.Delete(fixture.PathFor("back-to-parent"));
            }
        }
        finally
        {
            Directory.Delete(alias);
        }
    }

    [Test]
    public void WholeFileFormattingNormalizesUnicodeLineTerminators()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, "internal class Layout { int A;\u0085int B;\u2028int C;\u2029int D; }\n");
        fixture.Commit();
        fixture.Format(true, "--all");
        Assert.That(fixture.ReadText(Source), Does.Not.Contain("\u0085").And.Not.Contain("\u2028").And.Not.Contain("\u2029"));
        fixture.Format(true, "--all", "--check");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ParentTraversalCannotHideADirectoryLink(bool absolute)
    {
        using var fixture = new RepositoryFixture();
        const string alias = "Assets/Scripts/Link";
        const string lexicalFile = "Assets/Scripts/Victim.cs";
        const string physicalFile = "Assets/Outside/Victim.cs";
        const string original = "class Victim{int  A;}\n";
        fixture.Write(lexicalFile, original);
        fixture.Write(physicalFile, original);
        Directory.CreateDirectory(fixture.PathFor("Assets/Outside/Nested"));
        fixture.Link(alias, "Assets/Outside/Nested", directory: true);
        fixture.Commit();

        string path = alias + "/../Victim.cs";
        var result = fixture.Format(false, absolute ? fixture.PathFor(path) : path);
        Assert.That(result.Output, Does.Contain("Symbolic-link paths"));
        Assert.That(fixture.ReadText(lexicalFile), Is.EqualTo(original));
        Assert.That(fixture.ReadText(physicalFile), Is.EqualTo(original));

        fixture.Format(true, "Assets/Scripts/../Scripts/Victim.cs");
        Assert.That(fixture.ReadText(lexicalFile), Is.Not.EqualTo(original));
    }

    [Test]
    public void OwnedFileLinksAreSkippedByDefaultAndRejectedExplicitlyWithoutChangingTheirTarget()
    {
        using var fixture = new RepositoryFixture();
        const string target = "Shared/Target.cs", alias = "Assets/Scripts/Alias.cs";
        const string original = "class SharedTarget{int  A;}\n";
        fixture.Write(target, original);
        fixture.Link(alias, target);
        fixture.Commit();
        string? link = new FileInfo(fixture.PathFor(alias)).LinkTarget;
        Assert.That(link, Is.Not.Null);
        fixture.Format(true, "--check");
        fixture.Format(true, "--all");
        fixture.Format(false, alias);
        Assert.That(fixture.ReadText(target), Is.EqualTo(original));
        Assert.That(new FileInfo(fixture.PathFor(alias)).LinkTarget, Is.EqualTo(link));
        Assert.That(fixture.Run("git", "status", "--porcelain", "--", alias, target).Output, Is.Empty);
    }

}
