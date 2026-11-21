// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using Game.Rendering.Renderer;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Color32 = UnityEngine.Color32;

    public sealed class BattleWeaponPresentationTests
    {
        [TestCase("PAL3", 0, true, true)]
        [TestCase("PAL3A", 0, false, false)]
        [TestCase("PAL3A", -1, false, true)]
        [TestCase("PAL3A", 1, true, true)]
        public void ExternalFlagsUseEachPublishedGamesZeroConvention(string variant, float flag, bool left, bool right)
        {
            BattleWeaponVisibility actual = BattleWeaponPresentationRules.Visible(variant, 0, 29, 1, external: true, leftFlag: flag, rightFlag: flag);
            Assert.That((actual.Left, actual.Right), Is.EqualTo((left, right)));
        }

        [Test]
        [Category("LocalNativeEvidence")]
        public void EveryPublishedAttachVisibilityCombinationMatches()
        {
            JObject document = JObject.Parse(File.ReadAllText(Path.Combine(NativeDirectory(), "weapon-visibility-native-vectors.json")));
            int count = 0;
            foreach (JProperty variant in document.Properties())
                foreach (JToken row in variant.Value["rows"])
                {
                    BattleWeaponVisibility actual = BattleWeaponPresentationRules.Visible(variant.Name, (int)row["identity"], (int)row["action"],
                        (int)row["weapon"], (bool)row["form"], true, (bool)row["external"], (float)row["leftFlag"], (float)row["rightFlag"]);
                    Assert.That((actual.Left, actual.Right), Is.EqualTo(((bool)row["left"], (bool)row["right"])), variant.Name + ": " + count);
                    count++;
                }
            Assert.That(count, Is.EqualTo(37600));
        }

        [Test]
        [Category("LocalNativeEvidence")]
        public void WeaponTrailSamplingAndColorsMatchTheInstalledEngine()
        {
            JObject document = JObject.Parse(File.ReadAllText(Path.Combine(NativeDirectory(), "weapon-blade-native-vectors.json")));
            Type type = typeof(WeaponBlade);
            const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var owner = new GameObject("original weapon blade fixture");
            try
            {
                foreach (JToken row in document["rows"])
                {
                    TagNode[] tags = {
                        new() { GameBoxTransformMatrix = new GameBoxMatrix4x4(), TintColor = 0xff336699 },
                        new() { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -40, Tz = 140 }, TintColor = 0xff336699 }
                    };
                    object blade = Activator.CreateInstance(type, members, null, new object[] { tags }, null);
                    try
                    {
                        foreach (JToken sample in row["samples"])
                        {
                            JToken origin = sample["vertices"][0];
                            owner.transform.position = new Vector3((float)origin[0], (float)origin[1], (float)origin[2]);
                            type.GetMethod("Sample", members).Invoke(blade, new object[] { owner.transform, (bool)sample["visible"],
                                (int)sample["tick"] == 0 ? 0f : 0.02f, (float)row["rate"], (float)row["lifetime"] });
                            int count = (int)type.GetField("_count", members).GetValue(blade);
                            Assert.That(count, Is.EqualTo((int)sample["count"]), row["mode"] + " " + sample["tick"]);
                            if (count < 3)
                                continue;
                            Mesh mesh = (Mesh)type.GetField("_mesh", members).GetValue(blade);
                            Vector3[] vertices = mesh.vertices;
                            Color32[] colors = mesh.colors32;
                            for (int frame = 0; frame < count; frame++)
                                for (int index = 0; index < 2; index++)
                                {
                                    JToken expected = sample["frames"][frame]["vertices"][index];
                                    JToken position = expected["position"];
                                    Assert.That(Vector3.Distance(vertices[frame * 2 + index], new Vector3((float)position[0], (float)position[1], (float)position[2])), Is.LessThan(0.00001f));
                                    uint color = (uint)expected["color"];
                                    Assert.That(colors[frame * 2 + index], Is.EqualTo(new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color, (byte)(color >> 24))));
                                }
                        }
                    }
                    finally { type.GetMethod("Dispose", members).Invoke(blade, null); }
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PublishedSnapshotsRetainCompatibilityGeometryWithoutRebuildingTheUnusedMesh()
        {
            using var preview = new ResourceModelPreview(null, false);
            GameObject owner = preview.CreateObject("Blade buffer regression");
            Camera camera = preview.Root.scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).First(value => value != null);
            var tags = new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Ty = 10 }, TintColor = 0xff336699 },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -40, Tz = 140 }, TintColor = 0xccee8855 }
            };
            using var compatibility = new WeaponBlade(tags);
            using var published = new WeaponBlade(tags);
            var material = WeaponBlade.CreateMaterial();
            const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(WeaponBlade);
            object vertices = type.GetField("_vertices", members).GetValue(published);
            object colors = type.GetField("_colors", members).GetValue(published);
            object triangles = type.GetField("_triangles", members).GetValue(published);
            try
            {
                for (int frame = 0; frame < 160; frame++)
                {
                    owner.transform.SetPositionAndRotation(new Vector3(frame * .01f, Mathf.Sin(frame * .13f), Mathf.Cos(frame * .11f)), Quaternion.Euler(frame, frame * 3, 0));
                    bool visible = frame % 19 < 14;
                    float lifetime = frame < 60 ? .12f : .25f;
                    float delta = frame < 30 ? .02f : frame < 80 ? 1f / 60 : frame < 110 ? 0 : .01f;
                    float rate = frame < 60 || frame > 120 ? 1 : frame < 100 ? 2 : 0;
                    if (frame == 130)
                        delta = 30;
                    compatibility.SamplePose(owner.transform, visible, lifetime);
                    published.SamplePose(owner.transform, visible, lifetime, rebuildMesh: false);
                    for (int flush = 0; flush < 2; flush++)
                    {
                        compatibility.Advance(delta, rate);
                        published.Advance(delta, rate, rebuildMesh: false);
                        compatibility.RenderFlush(camera, material, 0, flush, true);
                        published.RenderFlush(camera, material, 0, flush, true);
                        var expected = (Mesh[])type.GetField("_flushMeshes", members).GetValue(compatibility);
                        var actual = (Mesh[])type.GetField("_flushMeshes", members).GetValue(published);
                        Assert.That(actual[flush].vertices, Is.EqualTo(expected[flush].vertices), $"Frame {frame}, flush {flush}");
                        Assert.That(actual[flush].colors32, Is.EqualTo(expected[flush].colors32), $"Frame {frame}, flush {flush}");
                        Assert.That(actual[flush].triangles, Is.EqualTo(expected[flush].triangles), $"Frame {frame}, flush {flush}");
                        Assert.That(actual[0], Is.Not.SameAs(actual[1]));
                    }
                    Assert.That(published.TriangleCount, Is.EqualTo(compatibility.TriangleCount));
                    Assert.That(((Mesh)type.GetField("_mesh", members).GetValue(published)).vertexCount, Is.Zero);
                }
                Assert.That(type.GetField("_vertices", members).GetValue(published), Is.SameAs(vertices));
                Assert.That(type.GetField("_colors", members).GetValue(published), Is.SameAs(colors));
                Assert.That(type.GetField("_triangles", members).GetValue(published), Is.SameAs(triangles));
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void PublishedFlushKeepsAnExplicitPreviewSceneOverride()
        {
            using var preview = new ResourceModelPreview(null, false);
            var owner = new GameObject("Explicit preview blade camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = owner.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = preview.Root.scene;
            Assert.That(camera.scene.IsValid(), Is.True);
            Assert.That(owner.scene, Is.Not.EqualTo(camera.scene), "The fixture must distinguish the override from the camera object's scene.");
            var tags = new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4(), TintColor = 0xffffffff },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Ty = 20 }, TintColor = 0xffffffff },
            };
            using var blade = new WeaponBlade(tags);
            Material material = WeaponBlade.CreateMaterial();
            try
            {
                blade.SamplePose(owner.transform, true, .12f, false);
                blade.Advance(.02f, 1, false);
                owner.transform.position = Vector3.right;
                blade.SamplePose(owner.transform, true, .12f, false);
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                for (int flush = 0; flush < 2; flush++)
                    blade.RenderFlush(camera, material, 0, flush, true);
                var renderers = (MeshRenderer[])typeof(WeaponBlade).GetField("_flushRenderers", fields).GetValue(blade);
                foreach (MeshRenderer renderer in renderers)
                {
                    Assert.That(renderer.gameObject.scene, Is.EqualTo(preview.Root.scene));
                    Assert.That(renderer.enabled, Is.True);
                }
                blade.Dispose();
                foreach (MeshRenderer renderer in renderers)
                    Assert.That(renderer == null, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(owner);
            }
        }

        private static string NativeDirectory()
        {
            string directory = Environment.GetEnvironmentVariable("PAL3_NATIVE_VECTORS_ROOT");
            if (directory == null)
                Assert.Ignore("Published native vectors are local analysis artifacts.");
            return directory;
        }
    }
}
