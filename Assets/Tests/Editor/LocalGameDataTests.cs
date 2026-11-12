// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Command;
    using DataReader;
    using DataReader.Cpk;
    using DataReader.Cvd;
    using DataReader.Nav;
    using DataReader.Sce;
    using DataReader.Scn;
    using FileSystem;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using Utilities;

    // Optional compatibility tests: original data stays outside the repository.
    [TestFixture, Category("LocalGameData")]
    public sealed class LocalGameDataTests
    {
        #if PAL3
        private const string Variant = "PAL3";
        #else
        private const string Variant = "PAL3A";
        #endif

        private static string DataRoot()
        {
            string root = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Game Data", Variant));
            if (!Directory.Exists(root)) Assert.Ignore($"Set PAL3_TEST_DATA_ROOT to the {Variant} data directory");
            return root;
        }

        [Test]
        public void EveryActiveCpkFileEntryCanBeReadAndDecompressed()
        {
            string root = DataRoot(); var crc = new Crc32Hash(); crc.Init();
            var failures = new List<string>();
            int archives = 0, files = 0, compressed = 0;
            long packedBytes = 0, unpackedBytes = 0;
            foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                         .Where(p => p.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p))
            {
                archives++;
                var archive = new CpkArchive(path, crc, 936); archive.Init();
                using var stream = File.OpenRead(path);
                var header = CoreUtility.ReadStruct<CpkHeader>(stream);
                stream.Position = header.TableStart;
                var seen = new HashSet<uint>();
                int archiveFiles = 0, archiveCompressed = 0;
                // The table has MaxFileCount capacity; only NumberOfFiles live records are part of the archive.
                // Spare slots in shipped archives can retain stale records. Match CpkArchive.Init's live-record count,
                // while reading the raw table so unusual filenames or tree structure cannot hide live files.
                uint liveRecords = 0;
                for (uint index = 0; index < header.MaxFileCount && liveRecords < header.NumberOfFiles; index++)
                {
                    var entry = CoreUtility.ReadStruct<CpkTableEntity>(stream);
                    if (entry.IsEmpty() || entry.IsDeleted()) continue;
                    liveRecords++;
                    if (entry.IsDirectory()) continue;
                    files++; archiveFiles++;
                    packedBytes += entry.PackedSize;
                    if (entry.IsCompressed()) { compressed++; archiveCompressed++; }
                    try
                    {
                        if (!seen.Add(entry.CRC)) throw new InvalidDataException("Duplicate live CRC would hide a file behind another table entry");
                        byte[] bytes = archive.ReadAllBytes(entry.CRC);
                        unpackedBytes += bytes.LongLength;
                        if (bytes.LongLength != (entry.IsCompressed() ? entry.OriginSize : entry.PackedSize))
                            throw new InvalidDataException("ReadAllBytes returned an unexpected length");
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{Path.GetFileName(path)} table[{index}] CRC={entry.CRC:X8} " +
                            $"flags={entry.Flag:X8} packed={entry.PackedSize} original={entry.OriginSize}: {ex}");
                    }
                }
                Assert.That(liveRecords, Is.EqualTo(header.NumberOfFiles), Path.GetFileName(path));
                TestContext.WriteLine($"{Variant} {Path.GetFileName(path)}: read {archiveFiles} files ({archiveCompressed} compressed)");
            }
            TestContext.WriteLine($"{Variant}: {archives} CPKs, {files} file entries, {compressed} compressed; " +
                $"packed bytes={packedBytes}, decoded bytes={unpackedBytes}; failures={failures.Count}");
            Assert.That(files, Is.GreaterThan(0));
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }

        [Test]
        public void MountedRealArchivesResolveNormalizedAndSegmentedPaths()
        {
            string root = DataRoot(); var crc = new Crc32Hash(); crc.Init();
            var fs = new CpkFileSystem(root, crc, 936);
            string[] archives = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (string path in archives) fs.Mount(path[(root.TrimEnd(Path.DirectorySeparatorChar).Length + 1)..], 936);
            int checkedPaths = 0, segmented = 0;
            foreach (string path in archives)
            {
                var archive = new CpkArchive(path, crc, 936); archive.Init();
                string name = Files(archive.GetRootEntries()).FirstOrDefault(n =>
                    n.EndsWith(".nav", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".pol", StringComparison.OrdinalIgnoreCase));
                if (name == null) continue;
                string archiveName = Path.GetFileName(path);
                archive.FileExists(name, out uint hash); byte[] expected = archive.ReadAllBytes(hash);
                Assert.That(fs.ReadAllBytes((archiveName + "/" + name.Replace('\\', '/')).ToUpperInvariant()), Is.EqualTo(expected));
                if (archiveName.StartsWith("q02_", StringComparison.OrdinalIgnoreCase))
                {
                    string logical = "q02.cpk\\" + name.Replace(archiveName, "q02.cpk");
                    Assert.That(fs.FileExists(logical, out string physical), Is.True);
                    Assert.That(physical, Is.EqualTo(archiveName.ToLowerInvariant()));
                    Assert.That(fs.ReadAllBytes(logical), Is.EqualTo(expected)); segmented++;
                }
                checkedPaths++;
            }
            TestContext.WriteLine($"{Variant}: {checkedPaths} real archive paths, {segmented} segmented paths verified");
            Assert.That(checkedPaths, Is.GreaterThan(0));
            #if PAL3A
            Assert.That(segmented, Is.EqualTo(3));
            #endif
        }

        [TestCase(".sce")] [TestCase(".scn")] [TestCase(".nav")] [TestCase(".cvd")]
        public void AllLocalResourcesAgreeAcrossReadersAndPreserveProtocolStructure(string extension)
        {
            string root = DataRoot();
            var crc = new Crc32Hash(); crc.Init();
            var resolver = new SceCommandTypeResolver(); resolver.Init();
            var parser = new SceCommandParser(resolver);
            int files = 0, commands = 0, nonzeroTimes = 0, legacyRejections = 0;
            var failures = new List<string>();
            foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                         .Where(p => p.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p))
            {
                var archive = new CpkArchive(path, crc, 936); archive.Init();
                foreach (string name in Files(archive.GetRootEntries()).Where(p => p.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    files++;
                    try
                    {
                        Assert.That(archive.FileExists(name, out uint hash), Is.True, name);
                        byte[] data = archive.ReadAllBytes(hash);
                        string key = (Path.GetFileName(path) + "\\" + name).ToLowerInvariant();
                        if (Variant == "PAL3" && key == "basedata.cpk\\subgame\\hockshop\\data\\invesths.sce")
                        {
                            // This shipped legacy file has 8-byte indexes, rather than the current 72-byte layout.
                            Assert.That(Sha256(data), Is.EqualTo("b0dfd3467ff956046beb278e407b686f52638678efe6cb560fb6f83a12089510"));
                            using IBinaryReader oldSafe = new SafeBinaryReader(data);
                            using IBinaryReader oldFast = new UnsafeBinaryReader(data);
                            Assert.Throws<EndOfStreamException>(() => new SceFileReader().Read(oldSafe, 936));
                            Assert.Throws<EndOfStreamException>(() => new SceFileReader().Read(oldFast, 936));
                            legacyRejections++;
                            continue;
                        }
                        using IBinaryReader safe = new SafeBinaryReader(data);
                        using IBinaryReader fast = new UnsafeBinaryReader(data);
                        object a, b;
                        switch (extension)
                        {
                            case ".sce":
                                var sce = new SceFileReader().Read(safe, 936); a = sce; b = new SceFileReader().Read(fast, 936);
                                Assert.That(sce.Indexes.Length, Is.EqualTo(BitConverter.ToUInt16(data, 5)));
                                foreach (var block in sce.ScriptBlocks.Values)
                                {
                                    using IBinaryReader code = new SafeBinaryReader(block.ScriptData);
                                    if (Variant == "PAL3" && (key == "t01.cpk\\t01.sce" || key == "t02.cpk\\t02.sce") && block.Id == 1404)
                                    {
                                        // Old test scenes contain opcode 119 with one string; the released protocol has four parameters.
                                        Assert.That(Sha256(block.ScriptData), Is.EqualTo("2cf10e2d9f49edef9a9c2d80d0e97fa9310e59fee5fbc0555afdae0e71401890"));
                                        Assert.Throws<EndOfStreamException>(() => parser.ParseNextCommand(code, 936, out _));
                                        legacyRejections++;
                                        continue;
                                    }
                                    while (code.Position < code.Length)
                                    {
                                        long offset = code.Position;
                                        try { parser.ParseNextCommand(code, 936, out _); commands++; }
                                        catch (Exception ex) { throw new InvalidDataException($"Block {block.Id} command at {offset}/{code.Length}, opcode {BitConverter.ToUInt16(block.ScriptData, (int)offset)}", ex); }
                                    }
                                    Assert.That(code.Position, Is.EqualTo(code.Length));
                                }
                                break;
                            case ".scn":
                                var scn = new ScnFileReader().Read(safe, 936); a = scn; b = new ScnFileReader().Read(fast, 936);
                                Assert.That(scn.NpcInfos.Length, Is.EqualTo(BitConverter.ToUInt16(data, 6)));
                                Assert.That(scn.ObjectInfos.Length, Is.EqualTo(BitConverter.ToUInt16(data, 12)));
                                break;
                            case ".nav":
                                var nav = new NavFileReader().Read(safe, 936); a = nav; b = new NavFileReader().Read(fast, 936);
                                Assert.That(nav.Layers.Length, Is.EqualTo(data[5]));
                                foreach (var layer in nav.Layers) Assert.That(layer.Tiles.Length, Is.EqualTo(layer.Width * layer.Height));
                                break;
                            default:
                                var cvd = new CvdFileReader().Read(safe, 936); a = cvd; b = new CvdFileReader().Read(fast, 936);
                                Assert.That(safe.Position, Is.EqualTo(data.Length));
                                foreach (var node in Nodes(cvd.RootNodes).Where(n => n.IsGeometryNode))
                                {
                                    Assert.That(node.Mesh.AnimationTimeKeys[0], Is.Zero);
                                    if (node.Mesh.AnimationTimeKeys.Length > 1) nonzeroTimes++;
                                }
                                break;
                        }
                        Assert.That(fast.Position, Is.EqualTo(safe.Position));
                        Assert.That(JsonConvert.SerializeObject(b), Is.EqualTo(JsonConvert.SerializeObject(a)));
                    }
                    catch (Exception ex) { failures.Add($"{Path.GetFileName(path)}\\{name}: {ex}"); }
                }
            }
            TestContext.WriteLine($"{Variant}: {files} {extension} files, {commands} commands, {nonzeroTimes} animated CVD nodes; known legacy rejections: {legacyRejections}; failures: {failures.Count}");
            Assert.That(files, Is.GreaterThan(0));
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }

        private static string Sha256(byte[] data)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        internal static IEnumerable<string> Files(IEnumerable<CpkEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsDirectory) { foreach (var name in Files(entry.Children)) yield return name; }
                else yield return entry.VirtualPath;
            }
        }
        private static IEnumerable<CvdGeometryNode> Nodes(IEnumerable<CvdGeometryNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in Nodes(node.Children)) yield return child;
            }
        }
    }
}
