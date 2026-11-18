// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle.Tests;

using System.Text;
using NUnit.Framework;

public sealed class ChangeWriterTests
{
    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    [TestCase(false)]
    [TestCase(true)]
    public void ChangedSourceAbortsEntireBatchIncludingUnchangedPlannedFiles(bool secondNeedsFormatting)
    {
        using var fixture = new RepositoryFixture();
        const string first = "Assets/Scripts/First.cs", second = "Assets/Scripts/Second.cs";
        fixture.Write(first, "class First{int  A;}\n");
        fixture.Write(second, "class Second{int  A;}\n");
        byte[] firstOriginal = fixture.Read(first), secondOriginal = fixture.Read(second);
        FileChange[] changes =
        {
            new(fixture.PathFor(first), firstOriginal, Bytes("class First\n{\n    int A;\n}\n")),
            new(fixture.PathFor(second), secondOriginal, secondNeedsFormatting ? Bytes("class Second\n{\n    int A;\n}\n") : secondOriginal)
        };
        fixture.Write(second, "class Second { int ExternallyChanged; }\n");
        Assert.Throws<IOException>(() => ChangeWriter.Apply(changes));
        Assert.That(fixture.Read(first), Is.EqualTo(firstOriginal));
        Assert.That(fixture.ReadText(second), Is.EqualTo("class Second { int ExternallyChanged; }\n"));
    }

    [Test]
    public void MissingSourceFailsBeforeAnyOtherOutputIsWritten()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/First.cs";
        fixture.Write(source, "class First{int  A;}\n");
        byte[] original = fixture.Read(source);
        FileChange[] changes =
        {
            new(fixture.PathFor(source), original, Bytes("class First { int A; }\n")),
            new(fixture.PathFor("Assets/Scripts/Missing.cs"), Bytes("class Missing {}"), Bytes("class Missing { }"))
        };
        Assert.Catch<IOException>(() => ChangeWriter.Apply(changes));
        Assert.That(fixture.Read(source), Is.EqualTo(original));
    }

    [Test]
    public void SuccessfulBatchWritesExactPlannedBytesIncludingBomAndMixedEols()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/Changed.cs", untouched = "Assets/Scripts/Untouched.cs";
        fixture.Write(source, "class Changed{int  A;}\n");
        fixture.Write(untouched, "class Untouched {}\n");
        byte[] formatted = new byte[] { 0xef, 0xbb, 0xbf }.Concat(Bytes("class Changed\r\n{\r\n    string S = @\"one\ntwo\";\r\n}\r\n")).ToArray();
        byte[] originalUntouched = fixture.Read(untouched);
        ChangeWriter.Apply(new FileChange[]
        {
            new(fixture.PathFor(source), fixture.Read(source), formatted),
            new(fixture.PathFor(untouched), originalUntouched, originalUntouched)
        });
        Assert.That(fixture.Read(source), Is.EqualTo(formatted));
        Assert.That(fixture.Read(untouched), Is.EqualTo(originalUntouched));
    }
    [Test]
    public void SourceReplacedByLinkAfterPlanningAbortsAllWritesEvenWhenItsBytesMatch()
    {
        using var fixture = new RepositoryFixture();
        const string first = "Assets/Scripts/First.cs", second = "Assets/Scripts/Second.cs", target = "Shared/Target.cs";
        fixture.Write(first, "class First{int  A;}\n");
        fixture.Write(second, "class Second{int  B;}\n");
        byte[] firstOriginal = fixture.Read(first), secondOriginal = fixture.Read(second);
        fixture.Write(target, secondOriginal);
        FileChange[] changes =
        {
            new(fixture.PathFor(first), firstOriginal, Bytes("class First { int A; }\n")),
            new(fixture.PathFor(second), secondOriginal, secondOriginal)
        };
        File.Delete(fixture.PathFor(second));
        fixture.Link(second, target);
        string? link = new FileInfo(fixture.PathFor(second)).LinkTarget;
        Assert.Throws<IOException>(() => ChangeWriter.Apply(changes));
        Assert.That(fixture.Read(first), Is.EqualTo(firstOriginal));
        Assert.That(fixture.Read(target), Is.EqualTo(secondOriginal));
        Assert.That(new FileInfo(fixture.PathFor(second)).LinkTarget, Is.EqualTo(link));
    }

    [Test]
    public void ReadOnlyInputLeavesTheBatchAndFileAttributesIntact()
    {
        using var fixture = new RepositoryFixture();
        const string first = "Assets/Scripts/A.cs", second = "Assets/Scripts/B.cs";
        foreach (string name in new[] { first, second })
            fixture.Write(name, "class A{ }");
        string path = fixture.PathFor(second);
        FileAttributes attributes = File.GetAttributes(path);
        File.SetAttributes(path, attributes | FileAttributes.ReadOnly);
        try
        {
            FileChange[] changes = new[] { first, second }
                .Select(name => new FileChange(fixture.PathFor(name), fixture.Read(name), Bytes("class A { }"))).ToArray();
            Assert.Throws<UnauthorizedAccessException>(() => ChangeWriter.Apply(changes));
            Assert.That(fixture.ReadText(first), Is.EqualTo("class A{ }"));
            Assert.That(fixture.ReadText(second), Is.EqualTo("class A{ }"));
            Assert.That(File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly), Is.True);
            Assert.That(Directory.GetFiles(fixture.PathFor("Assets/Scripts")), Has.Length.EqualTo(2));
        }
        finally
        {
            File.SetAttributes(path, attributes);
        }
    }

    [Test]
    public void PreCancelledApplyWritesNoSelectedFile()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/First.cs";
        fixture.Write(source, "class First{int  A;}\n");
        byte[] original = fixture.Read(source);
        FileChange[] changes = { new(fixture.PathFor(source), original, Bytes("class First { int A; }\n")) };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => ChangeWriter.Apply(changes, cancellation.Token));
        Assert.That(fixture.Read(source), Is.EqualTo(original));
        Assert.That(Directory.GetFiles(fixture.PathFor("Assets/Scripts")), Has.Length.EqualTo(1));
    }

    [Test]
    public void ParentReplacedByLinkBeforeChangesAreMaterializedCannotRedirectWrites()
    {
        using var fixture = new RepositoryFixture();
        const string source = "Assets/Scripts/A.cs", target = "Shared/A.cs", moved = "Assets/Moved/A.cs";
        fixture.Write(source, "class A{int  Value;}\n");
        byte[] original = fixture.Read(source);
        fixture.Write(target, original);
        // Program captures originals before awaiting the SDK. Materializing the
        // write plan afterward must retain this lexical path and see the link.
        Directory.Move(fixture.PathFor("Assets/Scripts"), fixture.PathFor("Assets/Moved"));
        fixture.Link("Assets/Scripts", "Shared", directory: true);
        Assert.Throws<IOException>(() =>
        {
            FileChange[] changes = { new(fixture.PathFor(source), original, Bytes("class A { int Value; }\n")) };
            ChangeWriter.Apply(changes);
        });
        Assert.That(fixture.Read(target), Is.EqualTo(original));
        Assert.That(fixture.Read(moved), Is.EqualTo(original));
    }

}
