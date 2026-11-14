// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Game.Effect.Original;

    public sealed class ResourceEntry
    {
        public string Path
        {
            get;
        }

        public string PhysicalPath
        {
            get;
        }

        public string Extension
        {
            get;
        }

        public string Source
        {
            get;
        }

        public string Name
        {
            get;
        }

        public string Category
        {
            get;
        }

        public bool IsArchived => PhysicalPath == null;

        public ResourceEntry(string path, string physicalPath = null)
        {
            Path = path.Replace('/', '\\');
            PhysicalPath = physicalPath;
            Name = Path[(Path.LastIndexOf('\\') + 1)..];
            Extension = System.IO.Path.GetExtension(Name).ToLowerInvariant();
            Source = physicalPath == null ? Path.Split('\\')[0] : "Loose files";
            Category = ResourceFormats.Category(Extension);
        }
    }

    // No runtime service registrations, Unity objects, or archive preloads. Safe to index off-thread.
    public sealed class ResourceCatalog : IDisposable
    {
        public string Root
        {
            get;
        }

        public int Codepage
        {
            get;
        }

        public CpkFileSystem FileSystem
        {
            get;
        }

        public List<ResourceEntry> Entries { get; } = new();
        public List<string> Diagnostics { get; } = new();

        private readonly Dictionary<string, ResourceEntry> _lookup = new(StringComparer.OrdinalIgnoreCase);

        private ResourceCatalog(string root, int codepage)
        {
            Root = System.IO.Path.GetFullPath(root);
            CheckNoLinks(Root, "The source data folder contains a symbolic link. Select its real target folder to preserve original-data export protection");
            Codepage = codepage;
            var hash = new Crc32Hash();
            hash.Init();
            FileSystem = new CpkFileSystem(Root, hash, codepage);
        }

        public static ResourceCatalog Load(string root, int codepage, string variant,
            CancellationToken cancellation = default, Action<string> progress = null)
        {
            var catalog = new ResourceCatalog(root, codepage);
            try
            {
                var archives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in Directory.EnumerateFiles(catalog.Root, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false }))
                {
                    cancellation.ThrowIfCancellationRequested();
                    string relative = System.IO.Path.GetRelativePath(catalog.Root, file);
                    if (string.Equals(System.IO.Path.GetExtension(file), ".cpk", StringComparison.OrdinalIgnoreCase))
                    {
                        progress?.Invoke(relative);
                        if (!archives.Add(System.IO.Path.GetFileName(file)))
                            throw new InvalidDataException($"Duplicate archive name: {relative}. Select a single game's data directory.");
                        try
                        {
                            catalog.FileSystem.Mount(relative, codepage);
                        }
                        catch (Exception ex) { catalog.Diagnostics.Add($"{relative}: {ex.Message}"); }
                    }
                    else
                        catalog.Entries.Add(new ResourceEntry(relative, file));
                }
                using (var effects = new OriginalEffectResources(catalog.FileSystem, codepage))
                {
                    if (effects.GameVariant != variant)
                        throw new InvalidDataException($"This folder contains {effects.GameVariant}. Use {variant} data, or Switch Variant in the Unity menu.");
                }
                progress?.Invoke("Reading archive directory indexes…");
                foreach (string path in catalog.FileSystem.Search())
                {
                    cancellation.ThrowIfCancellationRequested();
                    catalog.Entries.Add(new ResourceEntry(path));
                }
                catalog.Entries.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Path, b.Path));
                foreach (ResourceEntry entry in catalog.Entries)
                    catalog._lookup.TryAdd(entry.Path, entry);
                return catalog;
            }
            catch
            {
                catalog.Dispose();
                throw;
            }
        }

        public ResourceEntry Find(string path)
        {
            path = path.Replace('/', '\\');
            if (_lookup.TryGetValue(path, out ResourceEntry entry))
                return entry;
            // PAL3A logical archive names (q02.cpk) resolve through q02_01/02/03.cpk.
            int separator = path.IndexOf('\\');
            return separator > 0 && path[..separator].EndsWith(".cpk", StringComparison.OrdinalIgnoreCase) && FileSystem.FileExists(path)
                ? new ResourceEntry(path) : null;
        }

        public const long PreviewByteLimit = 64 * 1024 * 1024;

        public (long length, long packedLength) GetSizes(ResourceEntry entry)
        {
            if (entry.IsArchived)
                return FileSystem.GetFileSizes(entry.Path);
            long length = new FileInfo(entry.PhysicalPath).Length;
            return (length, 0);
        }

        public byte[] Read(ResourceEntry entry, long byteLimit = PreviewByteLimit)
        {
            var sizes = GetSizes(entry);
            if (sizes.length > byteLimit || sizes.packedLength > byteLimit)
                throw new InvalidDataException($"This resource exceeds the {byteLimit / 1024 / 1024} MiB preview limit. Export the original file instead.");
            return entry.IsArchived ? FileSystem.ReadAllBytes(entry.Path) : File.ReadAllBytes(entry.PhysicalPath);
        }

        public void CheckExportDestination(string destination) => CheckOutsideRoot(Root, destination);

        public static void CheckOutsideRoot(string root, string destination)
        {
            string path = System.IO.Path.GetFullPath(destination);
            string source = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            if (path.Equals(source, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(source + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Choose an output outside the original game data directory.");
        }

        public void ExportOriginal(ResourceEntry entry, string destination)
        {
            CheckExportDestination(destination);
            // Check symlink ancestors even for a user-selected single-file destination.
            destination = ExportPath(System.IO.Path.GetDirectoryName(destination), System.IO.Path.GetFileName(destination));
            WriteNewFile(destination, stream =>
            {
                if (entry.IsArchived)
                {
                    byte[] bytes = Read(entry, int.MaxValue);
                    stream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    using var input = File.OpenRead(entry.PhysicalPath);
                    input.CopyTo(stream);
                }
            });
        }

        public static void WriteNewFile(string destination, Action<Stream> write)
        {
            CheckNoLinks(destination);
            if (File.Exists(destination))
                throw new IOException("Destination already exists: " + destination);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                    write(stream);
                File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        public static bool Matches(ResourceEntry entry, string query, string category = "All", string source = "All")
        {
            if (category != "All" && entry.Category != category || source != "All" && entry.Source != source)
                return false;
            return Matches(entry, SplitQuery(query), category, source);
        }

        public static string[] SplitQuery(string query) => (query ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        public static bool Matches(ResourceEntry entry, string[] terms, string category, string source)
        {
            if (category != "All" && entry.Category != category || source != "All" && entry.Source != source)
                return false;
            foreach (string word in terms)
            {
                bool exclude = word.StartsWith("-", StringComparison.Ordinal);
                string term = exclude ? word[1..] : word;
                bool match = term.StartsWith("ext:", StringComparison.OrdinalIgnoreCase)
                    ? entry.Extension.Equals("." + term[4..].TrimStart('.'), StringComparison.OrdinalIgnoreCase)
                    : entry.Path.IndexOf(term.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase) >= 0;
                if (match == exclude)
                    return false;
            }
            return true;
        }

        public static string ExportPath(string root, string virtualPath)
        {
            string[] parts = virtualPath.Replace('\\', '/').Split('/');
            if (parts.Any(p => string.IsNullOrWhiteSpace(p) || p is "." or ".." || p.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 || p.Contains(':')))
                throw new InvalidDataException($"Unsafe resource path: {virtualPath}");
            string destination = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, System.IO.Path.Combine(parts)));
            string prefix = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            if (!destination.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException("Export path escapes its destination.");
            CheckNoLinks(destination);
            return destination;
        }

        public static void CheckNoLinks(string fullPath, string message = "Use a path without symbolic links")
        {
            for (string path = System.IO.Path.GetFullPath(fullPath); path != null; path = System.IO.Path.GetDirectoryName(path))
            {
                if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"{message}: {path}");
                if (path == System.IO.Path.GetPathRoot(path))
                    break;
            }
        }

        public void Dispose() => FileSystem.DisposeAllInMemoryArchives();
    }
}
