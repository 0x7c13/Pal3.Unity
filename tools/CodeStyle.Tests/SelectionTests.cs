// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle.Tests;

using System.Text;
using NUnit.Framework;

public sealed class SelectionTests
{
    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    private static string Select(string? baseline, string original, string formatted) =>
        Encoding.UTF8.GetString(SelectiveFormatting.Select(baseline == null ? null : Bytes(baseline), Bytes(original), Bytes(formatted)));

    [TestCase("\r\n")]
    [TestCase("\n")]
    public void ChangedLinePreservesInheritedSpacingAndEols(string eol)
    {
        string baseline = "int  A;" + eol + eol + "int B;" + eol;
        string original = baseline.Replace("int B", "int   B");
        string formatted = "int A;\r\n\r\nint B;\r\n";
        Assert.That(Select(baseline, original, formatted), Is.EqualTo(baseline));
        Assert.That(Select(baseline, baseline, formatted), Is.EqualTo(baseline));
    }

    [Test]
    public void EditingOneStatementLeavesAdjacentLegacyWhitespaceUntouched()
    {
        const string baseline = "int before = 0;  \nint changed = 1;\n  int next = 2;\n  int second = 3;\n  int third = 4;\n";
        string original = baseline.Replace("changed = 1", "changed = 42");
        const string formatted = "int before = 0;\nint changed = 42;\nint next = 2;\nint second = 3;\nint third = 4;\n";
        Assert.That(Select(baseline, original, formatted), Is.EqualTo(original));
    }

    [Test]
    public void RestoringMemberGapPreservesAdjacentIndentationAndFormatsTheEditedMember()
    {
        const string baseline = "class C\n{\n    int A() => 1;\n\n  int B() => 2;\n}\n";
        const string formatted = "class C\n{\n    int A() => 1;\n\n    int B() => 2;\n}\n";
        Assert.That(Select(baseline, baseline.Replace(";\n\n", ";\n"), formatted), Is.EqualTo(baseline));
        string original = baseline.Replace("  int B() => 2", "int B() => 42");
        Assert.That(Select(baseline, original, formatted.Replace("=> 2", "=> 42")),
            Is.EqualTo(formatted.Replace("=> 2", "=> 42")));
    }

    [Test]
    public void NewFileUsesCompleteFormattingResult()
    {
        const string formatted = "int A;\r\n\r\nint B;\r\n";
        Assert.That(Select(null, "int  A;\nint   B;", formatted), Is.EqualTo(formatted));
    }

    [Test]
    public void RemovedFinalNewlineAndMemberGapAreRepaired()
    {
        const string baseline = "int  A;\r\n\r\nint B;\r\n";
        const string formatted = "int A;\r\n\r\nint B;\r\n";
        Assert.That(Select(baseline, baseline[..^2], formatted), Is.EqualTo(baseline));
        Assert.That(Select(baseline, baseline.Replace(";\r\n\r\n", ";\r\n"), formatted), Is.EqualTo(baseline));
        Assert.That(Select("class D {}\n\n\n", "class D {}\n\n", "class D {}\n"), Is.EqualTo("class D {}\n"));
    }

    [Test]
    public void MemberExpansionConvergesWithoutDuplicatingInheritedMembers()
    {
        const string baseline = "class D\n{\n    public int A { get; }\n    public int B { get; }\n    public int C { get; }\n}\n";
        string original = baseline[..^2] + "    public int D { get; }\n}\n";
        string formatted = original.Replace("{ get; }", "\n    {\n        get;\n    }");
        string selected = Select(baseline, original, formatted);
        Assert.That(selected.Split("public int B").Length - 1, Is.EqualTo(1));
        Assert.That(new string(selected.Where(character => !char.IsWhiteSpace(character)).ToArray()),
            Is.EqualTo(new string(original.Where(character => !char.IsWhiteSpace(character)).ToArray())));
        Assert.That(Select(baseline, selected, formatted), Is.EqualTo(selected));
        string removed = baseline.Replace("    public int A { get; }\n", "");
        string deletion = Select(baseline, removed, removed.Replace("{ get; }", "\n    {\n        get;\n    }"));
        Assert.That(deletion, Does.Contain("public int B { get; }"));
    }

    [Test]
    public void NonWhitespaceRewriteFailsWithoutReturningPartialOutput()
    {
        Assert.Throws<InvalidOperationException>(() => Select("int A;\n", "int  A;\n", "int B;\n"));
    }

    [Test]
    public void MixedEolsAndLiteralOffsetsRemainStableAfterBlankLineDeletion()
    {
        const string prefix = "class C\r\n{\r\n    int A;";
        const string suffix = "    string Text = @\"hello\nworld\";\n    int B;\n    int C;\n    int D;\n}\n";
        string baseline = prefix + string.Concat(Enumerable.Repeat("\r\n", 6)) + suffix;
        string original = prefix + string.Concat(Enumerable.Repeat("\r\n", 5)) + suffix;
        string formatted = prefix + "\r\n\r\n" + suffix.Replace("\n", "\r\n");
        string selected = Select(baseline, original, formatted);
        Assert.That(selected, Does.Contain("@\"hello\nworld\""));
        Assert.That(Select(baseline, selected, formatted), Is.EqualTo(selected));
    }

    [Test]
    public void LongLfLiteralDoesNotChangeCrLfCodeConvention()
    {
        string payload = "@\"" + string.Concat(Enumerable.Repeat("world\n", 40)) + "world\"";
        string original = "class D\r\n{\r\n    string Text = " + payload + ";\r\n    int   A;\r\n}\r\n";
        string baseline = original.Replace("int   A;", "int A;");
        Assert.That(Select(baseline, original, baseline), Is.EqualTo(baseline));
    }
    [Test]
    public void PreCancelledSelectionDoesNotProduceOutputOrMutateInputs()
    {
        byte[] baseline = Bytes("class C { int A; }\n"), original = Bytes("class C { int  A; }\n"), formatted = Bytes("class C\n{\n    int A;\n}\n");
        byte[] before = original.ToArray();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => SelectiveFormatting.Select(baseline, original, formatted, cancellation.Token));
        Assert.That(original, Is.EqualTo(before));
    }

}
