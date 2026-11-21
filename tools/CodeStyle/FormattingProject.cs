// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.Text;
using System.Xml.Linq;

internal sealed class FormattingProject : IDisposable
{
    private static readonly string[] Rules = ["SA1107", "SA1502", "SA1507", "SA1516"];
    private readonly string _directory;
    private readonly string _sources;
    private readonly string _project;
    private readonly string _root;

    private static string EscapeItem(string path)
    {
        var escaped = new StringBuilder();
        foreach (char character in path)
            if (character is '%' or '$' or '@' or '(' or ')' or ';' or '\'' or '?' or '*')
                escaped.Append('%').Append(((int)character).ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            else
                escaped.Append(character);
        return escaped.ToString();
    }

    internal FormattingProject(string root, IReadOnlyDictionary<string, byte[]> originals)
    {
        _root = root;
        string? workspace = Directory.GetParent(root) is { } parent ? Path.Combine(parent.FullName, "workspace") : null;
        string temporaryRoot = Directory.Exists(workspace) ? Path.Combine(workspace, "Tooling", "CodeStyle") : Path.GetTempPath();
        _directory = Path.Combine(temporaryRoot, "pal3-format-" + Guid.NewGuid().ToString("N"));
        _sources = Path.Combine(_directory, "sources");
        _project = Path.Combine(_directory, "Formatting.csproj");
        try
        {
            Directory.CreateDirectory(_sources);
            File.Copy(Path.Combine(root, ".editorconfig"), Path.Combine(_sources, ".editorconfig"));
            foreach (var entry in originals)
            {
                string path = Path.Combine(_sources, entry.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, entry.Value);
            }
            var project = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("TargetFramework", "net10.0"),
                    new XElement("EnableDefaultCompileItems", "false"),
                    new XElement("GenerateAssemblyInfo", "false"),
                    new XElement("NuGetAudit", "false"),
                    new XElement("DefineConstants", "$(PAL3_FORMAT_VARIANT);UNITY_EDITOR")),
                new XElement("ItemGroup",
                    new XElement("PackageReference", new XAttribute("Include", "StyleCop.Analyzers"),
                        new XAttribute("Version", "1.3.180"), new XAttribute("PrivateAssets", "all")),
                    originals.Keys.Select(name => new XElement("Compile", new XAttribute("Include", EscapeItem(Path.Combine(_sources, name)))))));
            new XDocument(project).Save(_project);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal byte[] Read(string name) => File.ReadAllBytes(Path.Combine(_sources, name));

    private async Task<bool> Run(IEnumerable<string> arguments, CancellationToken cancellation, string? variant = null)
    {
        ProcessResult result = await Processes.Run("dotnet", arguments, _root, cancellation,
            variant == null ? null : new Dictionary<string, string> { ["PAL3_FORMAT_VARIANT"] = variant });
        Console.Write(Encoding.UTF8.GetString(result.Output));
        Console.Error.Write(result.Error);
        return result.ExitCode == 0;
    }

    internal async Task Format(bool verbose, CancellationToken cancellation)
    {
        if (!await Run(["restore", _project, "--verbosity", "quiet"], cancellation))
            throw new IOException("Unable to restore the temporary formatting project.");
        bool succeeded = true;
        foreach (string variant in new[] { "PAL3", "PAL3A" })
        {
            foreach (string command in new[] { "analyzers", "whitespace" })
            {
                var arguments = new List<string> { "format", command, _project, "--no-restore", "--verbosity", verbose ? "diagnostic" : "minimal" };
                if (command == "analyzers")
                {
                    arguments.Add("--diagnostics");
                    arguments.AddRange(Rules);
                }
                succeeded &= await Run(arguments, cancellation, variant);
            }
        }
        if (!succeeded)
            throw new IOException("The SDK formatter failed; source files were not changed.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
