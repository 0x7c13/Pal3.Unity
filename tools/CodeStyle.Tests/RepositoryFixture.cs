// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle.Tests;

using System.Diagnostics;
using System.Text;
using NUnit.Framework;

internal sealed class RepositoryFixture : IDisposable
{
    internal static readonly string SourceRoot = FindSourceRoot();
    internal string Root
    {
        get;
    }

    internal RepositoryFixture()
    {
        string parent = Environment.GetEnvironmentVariable("PAL3_CODE_STYLE_TEST_ROOT") ??
            Path.Combine(Path.GetTempPath(), "Pal3.CodeStyle.Tests");
        Root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        foreach (string name in new[] { ".editorconfig", "global.json" })
            File.Copy(Path.Combine(SourceRoot, name), Path.Combine(Root, name));
        Write(".gitignore", "bin/\nobj/\ncompiled/\n");
        Run("git", "init", "--quiet");
        Run("git", "config", "core.autocrlf", "false");
        Run("git", "config", "core.symlinks", "true");
        Run("git", "config", "core.hooksPath", PathFor(".git/no-hooks"));
        // Git canonicalizes system temporary aliases such as /var on macOS.
        // Use that stable root before any source snapshots/plans are created.
        Root = Run("git", "rev-parse", "--show-toplevel").Output.TrimEnd('\r', '\n');
    }

    internal string PathFor(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    internal void Write(string relative, string content) => Write(relative, Encoding.UTF8.GetBytes(content));

    internal void Write(string relative, byte[] content)
    {
        string path = PathFor(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    internal byte[] Read(string relative) => File.ReadAllBytes(PathFor(relative));

    internal string ReadText(string relative) => Encoding.UTF8.GetString(Read(relative));

    internal ProcessResult Format(bool success = true, params string[] arguments) => FormatFrom(Root, success, arguments);

    internal ProcessResult FormatFrom(string directory, bool success, params string[] arguments)
    {
        string formatter = Path.Combine(AppContext.BaseDirectory, "formatter");
        string configuration = Directory.GetFiles(formatter, "*.runtimeconfig.json").Single();
        string assembly = configuration[..^".runtimeconfig.json".Length] + ".dll";
        return Run("dotnet", new[] { assembly }.Concat(arguments).ToArray(), success, directory);
    }

    internal void Link(string relative, string target, bool directory = false)
    {
        string path = PathFor(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            string destination = Path.GetRelativePath(Path.GetDirectoryName(path)!, PathFor(target));
            if (directory)
                Directory.CreateSymbolicLink(path, destination);
            else
                File.CreateSymbolicLink(path, destination);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or PlatformNotSupportedException ||
            OperatingSystem.IsWindows() && exception is IOException && (exception.HResult & 0xffff) == 1314)
        {
            Assert.Ignore("The current filesystem/user cannot create symbolic links: " + exception.Message);
        }
    }

    internal string Commit(params string[] paths)
    {
        Run("git", new[] { "add", "--" }.Concat(paths.Length == 0 ? new[] { "." } : paths).ToArray());
        Run("git", "-c", "user.name=Code style fixture", "-c", "user.email=fixture@localhost",
            "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "Fixture baseline");
        return Run("git", "rev-parse", "HEAD").Output.Trim();
    }

    internal ProcessResult Run(string executable, params string[] arguments) => Run(executable, arguments, true);

    internal ProcessResult Run(string executable, string[] arguments, bool success, string? directory = null)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory ?? Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);
        // A caller's worktree/index overrides must never redirect fixture Git
        // commands into an unrelated repository.
        foreach (string variable in start.Environment.Keys.Where(name => name.StartsWith("GIT_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(variable);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        // Both Windows and Unix subprocesses keep their temporary files inside
        // this fixture, which makes cleanup and failed-test inspection local.
        foreach (string variable in new[] { "TMPDIR", "TMP", "TEMP" })
            start.Environment[variable] = Root;
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(180000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10000);
            Assert.Fail("Command exceeded 180 seconds: " + executable + " " + string.Join(" ", arguments));
        }
        string text = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        Assert.That(process.ExitCode == 0, Is.EqualTo(success),
            executable + " " + string.Join(" ", arguments) + Environment.NewLine + text);
        return new ProcessResult(process.ExitCode, text);
    }

    public void Dispose()
    {
        // Failed fixtures can be retained for local diagnosis without changing
        // the behavior of ordinary dotnet test runs on Windows/macOS/Linux.
        if (Environment.GetEnvironmentVariable("PAL3_CODE_STYLE_KEEP_FIXTURES") == "1")
            return;
        foreach (string file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            FileAttributes attributes = File.GetAttributes(file);
            if ((attributes & FileAttributes.ReparsePoint) == 0)
                File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
        }
        Directory.Delete(Root, recursive: true);
    }

    private static string FindSourceRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tools", "CodeStyle.Tests")))
                return directory.FullName;
        throw new DirectoryNotFoundException("The code style tests must run from a repository build output.");
    }

    internal readonly record struct ProcessResult(int ExitCode, string Output);
}
