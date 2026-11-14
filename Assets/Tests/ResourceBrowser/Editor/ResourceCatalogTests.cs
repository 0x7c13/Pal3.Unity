// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class ResourceCatalogTests
    {
        [TestCase("角色 ext:dds -missing", true)]
        [TestCase("角色 ext:tga", false)]
        [TestCase("-角色", false)]
        [TestCase("BASEDATA.CPK/角色", true)]
        public void QueryCombinesPathTypeAndExclusions(string query, bool expected)
        {
            Assert.That(ResourceCatalog.Matches(new ResourceEntry("basedata.cpk\\角色\\A.DDS"), query), Is.EqualTo(expected));
        }

        [TestCase("../escape.bin")]
        [TestCase("archive.cpk/../../escape.bin")]
        [TestCase("/absolute.bin")]
        [TestCase("C:\\escape.bin")]
        public void ExportRejectsUnsafePaths(string path)
        {
            Assert.Throws<InvalidDataException>(() => ResourceCatalog.ExportPath(Application.dataPath, path));
        }

        [Test]
        public void ImageHeadersAreCheckedBeforeUnsafeTgaDecoding()
        {
            Assert.Throws<InvalidDataException>(() => ResourceImageData.Prepare(".tga", new byte[17]));
            var tga = new byte[18];
            tga[2] = 2;
            tga[12] = 2;
            tga[14] = 2;
            tga[16] = 24;
            Assert.Throws<InvalidDataException>(() => ResourceImageData.Prepare(".tga", tga));
            tga[13] = 255;
            Assert.Throws<InvalidDataException>(() => ResourceImageData.Prepare(".tga", tga));
        }

        [Test]
        public void TgaImageIdAndTopOriginAreNormalizedWithoutChangingOriginalBytes()
        {
            var tga = new byte[25];
            tga[0] = 1;
            tga[2] = 2;
            tga[12] = 1;
            tga[14] = 2;
            tga[16] = 24;
            tga[17] = 32;
            tga[19] = 10;
            tga[22] = 20;
            byte[] normalized = ResourceImageData.Prepare(".tga", tga);
            Assert.That(normalized[18], Is.EqualTo(20));
            Assert.That(normalized[21], Is.EqualTo(10));
            Assert.That(tga[19], Is.EqualTo(10));
            Assert.That(normalized.Length, Is.EqualTo(24));
        }

        [Test]
        public void NativeTexturePreviewOwnsAndReleasesItsTexture()
        {
            var original = new Texture2D(2, 2);
            original.SetPixels(new[] { Color.clear, Color.red, Color.green, Color.white });
            original.Apply();
            byte[] png = original.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(original);
            var preview = new ResourceTexturePreview(new ResourceEntry("image.png"), png);
            Texture2D texture = preview.Texture;
            Assert.That(texture.width, Is.EqualTo(2));
            Assert.That(texture.GetPixel(0, 0).a, Is.Zero);
            preview.Dispose();
            Assert.That(texture == null, Is.True);
        }
    }

    [TestFixture, Category("ResourceBrowserLocalData")]
    public sealed class ResourceCatalogLocalTests
    {
        private ResourceCatalog _catalog;
        private string _output;

        [OneTimeSetUp]
        public void Load()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", ResourceBrowserWindow.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is unavailable.");
            _catalog = ResourceCatalog.Load(root, 936, ResourceBrowserWindow.Variant);
            _output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(_output);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _catalog?.Dispose();
            if (_output != null && Directory.Exists(_output))
                Directory.Delete(_output, true);
        }

        [Test]
        public void CatalogIndexesRealDataAndExportsExactBytesWithoutOverwriting()
        {
            Assert.That(_catalog.Entries.Count, Is.GreaterThan(100000));
            Assert.That(_catalog.Diagnostics, Is.Empty);
            ResourceEntry entry = _catalog.Entries.First(e => e.Extension == ".dds");
            var sizes = _catalog.GetSizes(entry);
            byte[] expected = _catalog.Read(entry);
            Assert.That(sizes.length, Is.EqualTo(expected.Length));
            string output = ResourceCatalog.ExportPath(_output, entry.Path);
            _catalog.ExportOriginal(entry, output);
            Assert.That(File.ReadAllBytes(output), Is.EqualTo(expected));
            Assert.Throws<IOException>(() => _catalog.ExportOriginal(entry, output));
            Assert.Throws<IOException>(() => _catalog.CheckExportDestination(Path.Combine(_catalog.Root, "output")));
            Assert.Throws<InvalidDataException>(() => _catalog.Read(entry, 1));
        }

        [Test]
        public void MshJsonExportOmitsParentCyclesAndRetainsBoneRelationships()
        {
            ResourceEntry entry = _catalog.Entries.First(e => e.Extension == ".msh");
            object model = ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage);
            string output = Path.Combine(_output, "skeleton.json");
            ResourceBrowserWindow.WriteJson(model, output);
            string json = File.ReadAllText(output);
            Assert.That(json, Does.Contain("\"ParentId\""));
            Assert.That(json, Does.Contain("\"Children\""));
        }

        [TestCase("obj/missing.dds")]
        [TestCase("a/missing.dds")]
        [TestCase("/missing.dds")]
        [TestCase("missing.dds")]
        [TestCase("missing.cpk/missing.dds")]
        public void MissingLooseAndArchiveDependenciesReturnNull(string path)
        {
            Assert.That(_catalog.Find(path), Is.Null);
        }

        [Test]
        public void FailedExportLeavesNoFinalOrPartialFile()
        {
            string destination = Path.Combine(_output, "failed.bin");
            Assert.Throws<IOException>(() => ResourceCatalog.WriteNewFile(destination, stream =>
            {
                stream.WriteByte(1);
                throw new IOException("Simulated write failure");
            }));
            Assert.That(File.Exists(destination), Is.False);
            Assert.That(Directory.GetFiles(_output, "*.partial"), Is.Empty);
        }

        [Test]
        public void SymbolicLinkInputCannotBypassOriginalDirectoryProtection()
        {
            if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.LinuxEditor)
                Assert.Ignore("Symlink regression uses the Unix ln utility.");
            string alias = Path.Combine(_output, "data-alias");
            var start = new System.Diagnostics.ProcessStartInfo("/bin/ln") { UseShellExecute = false };
            start.ArgumentList.Add("-s");
            start.ArgumentList.Add(_catalog.Root);
            start.ArgumentList.Add(alias);
            using var process = System.Diagnostics.Process.Start(start);
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.Zero);
            try
            {
                var error = Assert.Throws<IOException>(() => ResourceCatalog.Load(alias, 936, ResourceBrowserWindow.Variant));
                Assert.That(error.Message, Does.Contain("source data").And.Contain("real target"));
            }
            finally { Directory.Delete(alias); }
        }

        [Test]
        public void IndexCancellationStopsWithoutReturningPartialCatalog()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => ResourceCatalog.Load(_catalog.Root, 936, ResourceBrowserWindow.Variant, cancellation.Token));
        }

        [TestCase(".gdb")]
        [TestCase(".sce")]
        [TestCase(".csf")]
        [TestCase(".par")]
        [TestCase(".data")]
        [TestCase(".scn")]
        [TestCase(".nav")]
        [TestCase(".lgt")]
        public void RealSceneNavigationAndLightRecordsDecode(string extension)
        {
            ResourceEntry entry = _catalog.Entries.FirstOrDefault(e => e.Extension == extension &&
                (extension != ".data" || e.Name.Equals("flexeff.data", StringComparison.OrdinalIgnoreCase)));
            if (entry == null)
                Assert.Ignore("No local sample for " + extension);
            object decoded = ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage);
            Assert.That(decoded, Is.Not.Null);
            if (decoded is Core.DataReader.Scn.ScnFile scene)
            {
                Assert.That(_catalog.Find(ResourceSceneData.BackgroundPath(scene.SceneInfo)), Is.Not.Null);
                Assert.That(_catalog.Find(Core.Contract.Constants.FileConstants.GetNavFileVirtualPath(scene.SceneInfo.CityName, scene.SceneInfo.Model)), Is.Not.Null);
            }
        }

        [TestCase(".dds")]
        [TestCase(".tga")]
        [TestCase(".bmp")]
        public void RealTexturesCanBePreviewedAndExported(string extension)
        {
            ResourceEntry entry = _catalog.Entries.FirstOrDefault(e => e.Extension == extension);
            if (entry == null)
                Assert.Ignore("No sample for " + extension);
            using var preview = new ResourceTexturePreview(entry, _catalog.Read(entry));
            Assert.That(preview.Texture.width, Is.GreaterThan(0));
            string output = Path.Combine(_output, "texture-" + extension[1..] + ".png");
            preview.SavePng(output);
            Assert.That(new FileInfo(output).Length, Is.GreaterThan(0));
        }
    }
}
