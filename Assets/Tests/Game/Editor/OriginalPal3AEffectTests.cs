// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Core.DataReader.Data;
    using Effect.Original;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class OriginalPal3AEffectTests
    {
        public static readonly int[] NativeGroups = { 9, 27, 29, 34, 40, 50, 91, 157, 160, 172, 173, 175, 176, 177, 178, 179, 181, 182, 186, 187, 189, 194, 198, 201, 202, 205, 206, 209, 217, 230, 231, 254, 257, 258, 259, 261, 281, 282, 283, 285, 287, 290, 292, 297, 299, 314, 316, 320, 322, 330, 331, 333, 335, 351, 355, 356, 359, 362, 365, 373, 377, 381, 385, 388, 389, 400, 424 };
        public static readonly int[] SparGroups = { 45, 164, 170, 171, 174, 211, 289, 315, 318, 323, 325, 328, 344, 345, 358, 360, 361, 367, 397, 401, 404, 407, 408, 409, 411, 570 };

        private static OriginalEffectResources Open()
        {
            string path = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT") ?? Path.GetFullPath("../Game Data/PAL3A");
            if (!Directory.Exists(path))
                Assert.Ignore("Set PAL3A_TEST_DATA_ROOT to the local PAL3A data directory");
            var crc = new Crc32Hash();
            crc.Init();
            var fs = new CpkFileSystem(path, crc, 936);
            fs.Mount("basedata/basedata.cpk", 936);
            return new OriginalEffectResources(fs);
        }

        [Test]
        public void AllNonSparScriptAndSceneGroupsSimulateWithOriginalData()
        {
            using var resources = Open();
            Assert.That(resources.GameVariant, Is.EqualTo("PAL3A"));
            Assert.That(resources.OverridesPath, Is.EqualTo("Effects/PAL3AOverrides"));
            var errors = new List<string>();
            foreach (int id in NativeGroups)
            {
                var root = new GameObject($"PAL3A effect {id}");
                try
                {
                    Assert.That(resources.Diagnose(id), Is.Empty, $"Group {id}");
                    var player = root.AddComponent<OriginalEffectPlayer>();
                    player.AutoAdvance = false;
                    player.Initialize(resources, id, 1234);
                    bool rendered = false;
                    for (int i = 0; i < 650; i++)
                    {
                        player.Advance(0.02f);
                        if (i % 10 == 0)
                        {
                            player.Render(null);
                            rendered |= player.QuadCount > 0 || root.GetComponentsInChildren<Renderer>().Any(r => r.enabled);
                        }
                        Assert.That(player.Error, Is.Null, $"Group {id}");
                    }
                    Assert.That(rendered, Is.True, $"Group {id}: no geometry");
                    player.Seek(1);
                    player.Render(null);
                    Assert.That(player.Error, Is.Null);
                }
                catch (Exception ex)
                {
                    errors.Add($"{id}: {ex.Message}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void SparBillboardUsesNativeViewRotationAndUvCorners()
        {
            using var resources = Open();
            var source = resources.Definitions.Groups[170].Objects[0];
            resources.Definitions.TryGetType(source.TypeCrc, out EffectDefinition definition);
            definition.InitXPosition = definition.InitYPosition = definition.InitZPosition = 0;
            definition.InitXRotation = definition.InitYRotation = definition.InitZRotation = 0;
            definition.InitSize = 100;
            var file = resources.Spar(definition.ParticleFileName);
            var group = file.Groups[0];
            var key = OriginalSparTests.Key();
            key.RotationMinimum = key.RotationMaximum = Mathf.PI / 2;
            group.Keyframes = new[] { key };
            group.TextureFrames = new[] { new[] { 0.2f, 0.3f, 0.7f, 0.8f } };
            file.Groups = new[] { group };
            var root = new GameObject("SPar native billboard");
            var task = new EffectTask { StartTime = -1, Duration = 10, SpeedX = 10, RotAxisZ = 900, XStretchSpeed = 100, SpeedAlpha = -255 };
            var node = new OriginalSparNode(root, definition, new[] { task }, Vector3.zero, 0, resources, 1);
            try
            {
                node.Step(0.02f, 0.02f);
                node.Step(0.04f, 0.02f);
                node.Render(null);
                Mesh mesh = root.GetComponentInChildren<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                // Native 0x4c8eb6..0x4c8ff6 uses positive view-space radians.
                // Its top-left UV is (su,sv); Unity's bottom-left is (su,1-ev).
                Assert.That(vertices[0].x, Is.EqualTo(0.08f).Within(0.000001));
                Assert.That(vertices[0].y, Is.EqualTo(-0.1f).Within(0.000001));
                Assert.That(Vector3.Distance(vertices[0], vertices[1]), Is.EqualTo(0.2f).Within(0.000001));
                Assert.That(mesh.uv[0].x, Is.EqualTo(0.2f).Within(0.000001));
                Assert.That(mesh.uv[0].y, Is.EqualTo(0.2f).Within(0.000001));
                Assert.That(mesh.uv[2].y, Is.EqualTo(0.7f).Within(0.000001));
                int[] indices = mesh.triangles;
                Assert.That(Vector3.Cross(vertices[indices[1]] - vertices[indices[0]], vertices[indices[2]] - vertices[indices[0]]).z, Is.LessThan(0));
            }
            finally
            {
                node.Dispose();
            }
        }

        [TestCase(0, "trans.tga", 245, 188, 28)]
        [TestCase(1, "trans_s.tga", 0, 238, 255)]
        [TestCase(2, "trans_m.tga", 246, 0, 208)]
        [TestCase(3, "trans_l.tga", 108, 0, 255)]
        [TestCase(4, "trans_f.tga", 0, 255, 112)]
        [TestCase(5, "trans_t.tga", 255, 218, 0)]
        [TestCase(6, "trans_p.tga", 255, 0, 0)]
        public void TransportUsesOriginalStyleWithoutLoadingFlexeffZero(int type, string texture, int red, int green, int blue)
        {
            using var resources = Open();
            var root = new GameObject("PAL3A transport");
            try
            {
                var portal = root.AddComponent<OriginalPortalEffect>();
                portal.AutoAdvance = false;
                portal.Initialize(resources, (uint)type);
                portal.Sample(2);
                MeshFilter[] meshes = root.GetComponentsInChildren<MeshFilter>();
                Assert.That(meshes.Select(m => m.sharedMesh.vertexCount), Is.EqualTo(new[] { 4, 24, 24 }));
                Assert.That(meshes[0].GetComponent<MeshRenderer>().sharedMaterial.mainTexture.name, Does.EndWith(texture));
                Assert.That(meshes[1].sharedMesh.colors32[0], Is.EqualTo(new Color32((byte)red, (byte)green, (byte)blue, 255)));
                Assert.That(root.GetComponentsInChildren<OriginalEffectPlayer>(), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(1, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.DstAlpha)]
        [TestCase(2, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcColor)]
        [TestCase(3, UnityEngine.Rendering.BlendMode.SrcColor, UnityEngine.Rendering.BlendMode.OneMinusSrcColor)]
        [TestCase(0, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero)]
        public void SparMaterialMatchesOriginalGbfAndSurvivesLiveCacheTrim(int shader, UnityEngine.Rendering.BlendMode source, UnityEngine.Rendering.BlendMode destination)
        {
            using var resources = Open();
            Material material = resources.SparMaterial("effect\\trans.tga", shader);
            Assert.That(material.GetInt("_SrcBlend"), Is.EqualTo((int)source));
            Assert.That(material.GetInt("_DstBlend"), Is.EqualTo((int)destination));
            Assert.That(material.GetInt("_AlphaMode"), Is.Zero);
            var root = new GameObject("SPar cache lifetime");
            try
            {
                root.AddComponent<MeshRenderer>().sharedMaterial = material;
                resources.TrimUnusedRenderAssets(new[] { root });
                Assert.That(material != null && material.mainTexture != null, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            resources.TrimUnusedRenderAssets(Array.Empty<GameObject>());
            Assert.That(material == null, Is.True);
        }

        [TestCaseSource(nameof(SparGroups))]
        public void RemainingScriptAndSceneGroupSimulatesRendersAndSeeks(int id)
        {
            using var resources = Open();
            Assert.That(resources.Diagnose(id), Is.Empty, $"Group {id}");
            var root = new GameObject($"SPar group {id}");
            try
            {
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, id, 1234);
                int maximum = 0;
                float sampleTime = 0;
                bool sparRendered = false;
                for (int i = 0; i < 650; i++)
                {
                    player.Advance(0.02f);
                    if (i % 10 == 0)
                    {
                        player.Render(null);
                        maximum = Math.Max(maximum, player.QuadCount);
                        bool hasSpar = root.GetComponentsInChildren<MeshRenderer>().Any(r => r.enabled && r.sharedMaterial.shader.name == "Pal3/OriginalEffect" && r.sharedMaterial.GetInt("_Cull") == 2);
                        if (sampleTime == 0 && hasSpar)
                            sampleTime = player.Time;
                        sparRendered |= hasSpar;
                        foreach (MeshFilter mesh in root.GetComponentsInChildren<MeshFilter>())
                            Assert.That(mesh.sharedMesh.vertices.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), Is.True);
                    }
                    Assert.That(player.Error, Is.Null);
                }
                Assert.That(maximum, Is.GreaterThan(0), $"Group {id}: no geometry");
                Assert.That(sparRendered, Is.True, $"Group {id}: SPar did not submit visible particles");
                TestContext.WriteLine($"GPU sample: {id} {sampleTime:R}");
                player.Seek(sampleTime);
                player.Render(null);
                Vector3[] first = root.GetComponentsInChildren<MeshFilter>().SelectMany(m => m.sharedMesh.vertices).ToArray();
                Assert.That(first, Is.Not.Empty);
                player.Seek(sampleTime + 2);
                player.Seek(sampleTime);
                player.Render(null);
                Assert.That(root.GetComponentsInChildren<MeshFilter>().SelectMany(m => m.sharedMesh.vertices).ToArray(), Is.EqualTo(first));
                Assert.That(player.Error, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
