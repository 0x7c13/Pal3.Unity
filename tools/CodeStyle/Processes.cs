// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.Diagnostics;
using System.Text;

internal readonly record struct ProcessResult(int ExitCode, byte[] Output, string Error);

internal static class Processes
{
    internal static async Task<ProcessResult> Run(string executable, IEnumerable<string> arguments, string directory,
        CancellationToken cancellation, IReadOnlyDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (string argument in arguments)
            info.ArgumentList.Add(argument);
        if (environment != null)
            foreach (var entry in environment)
                info.Environment[entry.Key] = entry.Value;
        using var process = new Process { StartInfo = info };
        process.Start();
        using var output = new MemoryStream();
        Task copy = process.StandardOutput.BaseStream.CopyToAsync(output, cancellation);
        Task<string> error = process.StandardError.ReadToEndAsync(cancellation);
        try
        {
            await process.WaitForExitAsync(cancellation);
            await copy;
            return new(process.ExitCode, output.ToArray(), await error);
        }
        catch
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            try
            {
                await Task.WhenAll(copy, error);
            }
            catch (OperationCanceledException)
            {
                // Preserve the original cancellation after reaping children.
            }
            throw;
        }
    }

    internal static async Task<byte[]> Git(string directory, CancellationToken cancellation, params string[] arguments)
    {
        ProcessResult result = await Run("git", arguments, directory, cancellation);
        if (result.ExitCode != 0)
            throw new IOException("Git failed: " + result.Error.Trim());
        return result.Output;
    }
}
