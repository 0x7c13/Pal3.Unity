// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle.Tests;

using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

public sealed class PayloadTests
{
    private const string Source = "Assets/Scripts/文字 payload.cs";

    private const string LiteralSource = """""
using System;
using System.Text;
internal static class LiteralFixture
{
    static int Factor = 2;
#if PAL3
    static string Branch() => $$"""
        中文 😀 PAL3
        #if NATIVE
        #endif
        {{Factor}}
        """;
#elif PAL3A
    static string Branch() => $$"""
        中文 😀 PAL3A
        {{Factor + 1}}
        """;
#endif
    static string Verbatim() => @"one
# two ""quoted""";
    static string Interpolated() => $@"first
       {string.Join(" ", new[] { "one", "two" })}";
    static string Raw() => """
        zero
          two
        """;
    public static void Main() => Console.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(Branch() + Verbatim() + Interpolated() + Raw())));
}
""""";

    private const string InterpolationSource = """""
using System;
using System.Text;
internal static class InterpolationFixture
{
    static int A = 2, B = 3;
    static string Plain() => $"sum={A + B} {{ braces }}";
    static string Verbatim() => $@"first
# second {A + B,4:N2}";
    static string Raw() => $$"""
          raw text<tail>
          {{A + B}}
          {{string.Join(",", new[]
          {
              A, B
          })}}
          """;
    static string Formatted() => $"{A + B:D2}";
    static string Nested() => $"outer {$"inner {A + B}"}";
    static string MultilineHole() => $"{A +
B}";
    static string CommentHole() => $"{A + // 中文 😀
B}";
    static string RawHole() => $$"""
{{A +
B}}
""";
    static string LongDelimiter() => $$$""""
        literal {{braces}} and """ quotes
        {{{A + B}}}
        """";
    public static void Main() => Console.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(Plain() + Verbatim() + Raw() + Formatted() + Nested() + MultilineHole() + CommentHole() + RawHole() + LongDelimiter())));
}
""""";

    [Test]
    public void BomUnicodeAndLiteralSpellingSurviveSelectiveEditsAndRealVariantExecution()
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, Bom(LiteralSource.ReplaceLineEndings("\n") + "\n"));
        fixture.Commit();
        fixture.Write(Source, fixture.ReadText(Source).Replace("Factor = 2;", "Factor  =  2;")
            .Replace("          two\n        \"\"\";", "          two\n      \"\"\";"));
        byte[] before = fixture.Read(Source);
        Dictionary<string, string> payloads = Outputs(fixture);
        fixture.Format(false, "--check");
        Assert.That(fixture.Read(Source), Is.EqualTo(before));
        fixture.Format();
        byte[] after = fixture.Read(Source);
        Assert.That(after.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
        Assert.That(fixture.ReadText(Source), Does.Contain("Factor = 2;").And.Not.Contain("\r\n"));
        foreach (string method in new[] { "Branch", "Verbatim", "Interpolated", "Raw" })
            Assert.That(LiteralBodies(fixture.ReadText(Source), method), Is.EqualTo(LiteralBodies(Encoding.UTF8.GetString(before), method)), method);
        fixture.Format(true, "--check");
        Assert.That(Outputs(fixture), Is.EqualTo(payloads));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InterpolationExpressionsFormatWhileRawPayloadAndLegacyIndentationRemainExecutable(bool legacyIndentation)
    {
        using var fixture = new RepositoryFixture();
        fixture.Write(Source, InterpolationSource.ReplaceLineEndings("\n")
            .Replace("<tail>", "  ", StringComparison.Ordinal) + "\n");
        fixture.Format(true, Source);
        if (legacyIndentation)
        {
            string text = fixture.ReadText(Source);
            int start = text.IndexOf("$$\"\"\"", StringComparison.Ordinal);
            int end = text.IndexOf("\"\"\"", start + 5, StringComparison.Ordinal) + 3;
            string[] lines = text[start..end].Split('\n');
            string shifted = lines[0] + "\n" + string.Join("\n", lines.Skip(1).Select(line => "      " + line));
            fixture.Write(Source, text[..start] + shifted + text[end..]);
        }
        string baseline = fixture.Commit();
        fixture.Write(Source, fixture.ReadText(Source).Replace("A + B", "A+B").Replace("A, B", "A,  B"));
        byte[] original = fixture.Read(Source);
        Dictionary<string, string> payloads = Outputs(fixture);
        fixture.Format(false, "--check");
        Assert.That(fixture.Read(Source), Is.EqualTo(original));
        fixture.Format();
        Assert.That(fixture.ReadText(Source), Does.Not.Contain("A+B"));
        fixture.Format(true, "--check");
        fixture.Format(true, "--check", "--base", baseline);
        if (!legacyIndentation)
            fixture.Format(true, "--check", Source);
        Assert.That(Outputs(fixture), Is.EqualTo(payloads));
    }

    [Test]
    public void CommonInactivePlayerLiteralKeepsLfPayloadInsideBomCrLfCode()
    {
        using var fixture = new RepositoryFixture();
        const string text = "using System;\nusing System.Text;\ninternal static class InactiveFixture\n{\n    static int N = 0;\n#if !UNITY_EDITOR\n    static string Value = @\"one\nold\";\n#else\n    static string Value = \"editor\";\n#endif\n    static void Main() => Console.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(Value + N)));\n}\n";
        fixture.Write(Source, Bom(text.Replace("\n", "\r\n").Replace("@\"one\r\nold\"", "@\"one\nold\"")));
        fixture.Commit();
        fixture.Write(Source, fixture.ReadText(Source).Replace("old", "new").Replace("int N = 0;", "int N  =  0;"));
        byte[] original = fixture.Read(Source);
        Dictionary<string, string> payloads = Outputs(fixture);
        fixture.Format();
        Assert.That(fixture.Read(Source), Is.EqualTo(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original).Replace("int N  =  0;", "int N = 0;"))));
        fixture.Format(true, "--check");
        Assert.That(Outputs(fixture), Is.EqualTo(payloads));
    }

    [TestCase("new")]
    [TestCase("--all")]
    [TestCase(Source)]
    public void WholeFileFormattingPreservesInactiveLiteralBytes(string mode)
    {
        using var fixture = new RepositoryFixture();
        fixture.Write("README.txt", "Fixture repository");
        fixture.Commit();
        const string payload = "@\"one  \ntwo\"";
        fixture.Write(Source, "internal class Fixture\r\n{\r\n    int  n;\r\n#if !UNITY_EDITOR\r\n    string Value = " + payload + ";\r\n#endif\r\n}\r\n");
        if (mode == "new")
            fixture.Format();
        else
            fixture.Format(true, mode);
        Assert.That(fixture.ReadText(Source), Does.Contain("int n;").And.Contain(payload));
    }

    private static byte[] Bom(string source) => new byte[] { 0xef, 0xbb, 0xbf }.Concat(Encoding.UTF8.GetBytes(source)).ToArray();

    private static string[] LiteralBodies(string source, string method) => source.Split(method + "() => ", StringSplitOptions.None)
        .Skip(1).Select(part => part.Split(";\n", StringSplitOptions.None)[0]).ToArray();

    private static Dictionary<string, string> Outputs(RepositoryFixture fixture)
    {
        var document = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup", new XElement("TargetFramework", "net10.0"), new XElement("OutputType", "Exe"),
                new XElement("EnableDefaultCompileItems", "false"), new XElement("NuGetAudit", "false")),
            new XElement("ItemGroup", new XElement("Compile", new XAttribute("Include", Source)))));
        fixture.Write("Payload.csproj", document.ToString());
        var result = new Dictionary<string, string>();
        foreach (string variant in new[] { "PAL3", "PAL3A" })
        {
            string destination = fixture.PathFor("compiled/" + variant);
            fixture.Run("dotnet", "build", "Payload.csproj", "--verbosity", "quiet", "-p:DefineConstants=" + variant, "-o", destination);
            result.Add(variant, fixture.Run("dotnet", Path.Combine(destination, "Payload.dll")).Output);
        }
        return result;
    }
}
