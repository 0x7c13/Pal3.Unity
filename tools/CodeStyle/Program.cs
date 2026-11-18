// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.ComponentModel;

internal static class Program
{
    private static async Task<int> Main(string[] arguments)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, input) =>
        {
            input.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            Options options = Options.Parse(arguments);
            if (options.Help)
            {
                Console.WriteLine(Options.Usage);
                return 0;
            }
            Repository repository = await Repository.Open(cancellation.Token);
            string? reference = options.All || options.Paths.Length > 0 ? null : await repository.ComparisonBase(options.Base);
            SourceFile[] files = await repository.Select(options.Paths, reference);
            if (files.Length == 0)
            {
                Console.WriteLine("No owned C# changes to format.");
                return 0;
            }
            var originals = files.ToDictionary(file => file.Name, file => File.ReadAllBytes(repository.FullPath(file.Name)), StringComparer.Ordinal);
            using var project = new FormattingProject(repository.Root, originals);
            Console.WriteLine($"{(options.Check ? "Checking" : "Formatting")} {files.Length} C# files (PAL3 and PAL3A).");
            await project.Format(options.Verbose, cancellation.Token);
            var changes = new List<FileChange>();
            foreach ((string name, string? baselineName) in files)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                byte[]? baseline = reference == null || baselineName == null ? null : await repository.Baseline(reference, baselineName);
                byte[] selected = SelectiveFormatting.Select(baseline, originals[name], project.Read(name), cancellation.Token);
                changes.Add(new(repository.FullPath(name), originals[name], selected));
            }
            cancellation.Token.ThrowIfCancellationRequested();
            ChangeWriter.Verify(changes);
            bool changed = changes.Any(change => !change.Original.AsSpan().SequenceEqual(change.Formatted));
            if (!options.Check)
                ChangeWriter.Apply(changes, cancellation.Token);
            else
                foreach (FileChange change in changes.Where(change => !change.Original.AsSpan().SequenceEqual(change.Formatted)))
                    Console.WriteLine("Formatting required: " + Path.GetRelativePath(repository.Root, change.Path).Replace(Path.DirectorySeparatorChar, '/'));
            return options.Check && changed ? 1 : 0;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Formatting cancelled.");
            return 130;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or Win32Exception or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }
}
