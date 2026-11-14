// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    [TestFixture, Category("ResourceBrowserLocalData")]
    public sealed class ResourceModelTests
    {
        private ResourceCatalog _catalog;

        [OneTimeSetUp]
        public void Load()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", ResourceBrowserWindow.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is unavailable.");
            _catalog = ResourceCatalog.Load(root, 936, ResourceBrowserWindow.Variant);
        }

        [OneTimeTearDown]
        public void Cleanup() => _catalog?.Dispose();

        [Test]
        public void Mv3SamplingBeforeFirstKeyAndBeyondClipHoldsFinitePoses()
        {
            ResourceEntry entry = _catalog.Entries.First(e => e.Extension == ".mv3");
            var source = (Core.DataReader.Mv3.Mv3File)ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage);
            var meshes = source.Meshes.Select(mesh =>
            {
                mesh.KeyFrames = mesh.KeyFrames.Select(frame =>
                {
                    frame.GameBoxTick += 4800;
                    return frame;
                }).ToArray();
                return mesh;
            }).ToArray();
            var shifted = new Core.DataReader.Mv3.Mv3File(source.TotalGameBoxTicks + 4800,
                source.AnimationEvents, source.TagNodes, meshes, source.Materials);
            using var preview = new ResourceModelPreview(_catalog, false);
            preview.Load(entry, shifted);
            preview.Sample(0);
            preview.Sample(float.MaxValue);
            Assert.That(preview.Root.GetComponentsInChildren<MeshFilter>().SelectMany(m => m.sharedMesh.vertices)
                .All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), Is.True);
        }

        [TestCase("")]
        [TestCase("obj")]
        public void LooseModelsResolveTexturesAtRootAndInShortDirectories(string directory)
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            try
            {
                const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
                using var catalog = (ResourceCatalog)Activator.CreateInstance(typeof(ResourceCatalog), privateInstance, null, new object[] { output, 936 }, null);
                string texturePath = directory.Length == 0 ? "texture.tga" : directory + "\\texture.tga";
                string filePath = Path.Combine(output, directory, "texture.tga");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var tga = new byte[21];
                tga[2] = 2;
                tga[12] = tga[14] = 1;
                tga[16] = 24;
                tga[20] = 255;
                File.WriteAllBytes(filePath, tga);
                var lookup = (Dictionary<string, ResourceEntry>)typeof(ResourceCatalog).GetField("_lookup", privateInstance).GetValue(catalog);
                lookup.Add(texturePath, new ResourceEntry(texturePath, filePath));

                ResourceEntry sourceEntry = _catalog.Entries.First(e => e.Extension == ".mv3");
                var source = (Core.DataReader.Mv3.Mv3File)ResourceFormats.Read(sourceEntry, _catalog.Read(sourceEntry), _catalog.Codepage);
                var materials = source.Materials.Select(material =>
                {
                    material.TextureFileNames = new[] { "texture.tga" };
                    return material;
                }).ToArray();
                var model = new Core.DataReader.Mv3.Mv3File(source.TotalGameBoxTicks, source.AnimationEvents, source.TagNodes, source.Meshes, materials);
                string modelPath = directory.Length == 0 ? "model.mv3" : directory + "\\model.mv3";
                using var preview = new ResourceModelPreview(catalog, false);
                preview.Load(new ResourceEntry(modelPath, Path.Combine(output, directory, "model.mv3")), model);
                Assert.That(preview.Dependencies.Count, Is.EqualTo(1));
                Assert.That(preview.Dependencies[texturePath], Is.True);
                Assert.That(preview.Root.GetComponentsInChildren<MeshFilter>(), Is.Not.Empty);
            }
            finally { Directory.Delete(output, true); }
        }

        [Test]
        public void PlayingFromTheEndRestartsAndPauseResumeKeepsTheCurrentTime()
        {
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            ResourceEntry entry = _catalog.Entries.First(e => e.Extension == ".mv3");
            using var preview = new ResourceModelPreview(_catalog, false);
            preview.Load(entry, ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage));
            Assert.That(preview.Duration, Is.GreaterThan(0));
            var window = ScriptableObject.CreateInstance<ResourceBrowserWindow>();
            Type type = typeof(ResourceBrowserWindow);
            try
            {
                type.GetMethod("CancelScan", privateInstance).Invoke(window, null);
                type.GetField("_model", privateInstance).SetValue(window, preview);
                type.GetField("_loop", privateInstance).SetValue(window, false);
                FieldInfo time = type.GetField("_time", privateInstance);
                FieldInfo playing = type.GetField("_playing", privateInstance);
                MethodInfo toggle = type.GetMethod("ToggleModelPlayback", privateInstance);
                time.SetValue(window, preview.Duration);
                toggle.Invoke(window, null);
                Assert.That(time.GetValue(window), Is.EqualTo(0f));
                Assert.That(playing.GetValue(window), Is.True);
                type.GetField("_lastTick", privateInstance).SetValue(window, EditorApplication.timeSinceStartup - 0.05);
                type.GetMethod("TickModel", privateInstance).Invoke(window, null);
                float resumedTime = (float)time.GetValue(window);
                Assert.That(resumedTime, Is.GreaterThan(0).And.LessThan(preview.Duration));
                Assert.That(playing.GetValue(window), Is.True);
                toggle.Invoke(window, null);
                Assert.That(playing.GetValue(window), Is.False);
                toggle.Invoke(window, null);
                Assert.That(time.GetValue(window), Is.EqualTo(resumedTime));
            }
            finally
            {
                type.GetField("_model", privateInstance).SetValue(window, null);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [TestCase(".pol", false)]
        [TestCase(".pol", true)]
        [TestCase(".cvd", false)]
        [TestCase(".cvd", true)]
        [TestCase(".mv3", false)]
        [TestCase(".mv3", true)]
        [TestCase(".msh", false)]
        [TestCase(".msh", true)]
        [TestCase(".mov", false)]
        [TestCase(".mov", true)]
        public void RealModelsRenderSampleDeterministicallyAndReleaseOwnedResources(string extension, bool toon)
        {
            ResourceEntry entry = _catalog.Entries.FirstOrDefault(e => e.Extension == extension);
            if (entry == null)
                Assert.Ignore("No local sample for " + extension);
            object decoded = ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage);
            Scene active = SceneManager.GetActiveScene();
            bool wasDirty = active.isDirty;
            using var preview = new ResourceModelPreview(_catalog, toon);
            preview.Load(entry, decoded);
            GameObject root = preview.Root;
            Assert.That(EditorSceneManager.IsPreviewScene(root.scene), Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Mesh[] meshes = root.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray();
            Assert.That(meshes.Length, Is.GreaterThan(0));
            Material[] materials = root.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).ToArray();
            preview.Sample(preview.Duration * 0.5f);
            Vector3[][] middle = meshes.Select(m => m.vertices).ToArray();
            preview.Sample(0);
            preview.Sample(preview.Duration);
            preview.Sample(preview.Duration * 0.5f);
            for (int i = 0; i < meshes.Length; i++)
                Assert.That(meshes[i].vertices, Is.EqualTo(middle[i]), entry.Path);
            Assert.That(meshes.SelectMany(m => m.vertices).All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), Is.True);
            RenderTexture target = preview.Render(256, 256);
            var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Distinct().Count(), Is.GreaterThan(4), "Preview is blank: " + entry.Path);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
            preview.Dispose();
            Assert.That(root == null, Is.True);
            Assert.That(meshes.All(m => m == null), Is.True);
            Assert.That(materials.All(m => m == null), Is.True);
            Assert.That(active.isDirty, Is.EqualTo(wasDirty));
        }
    }
}
