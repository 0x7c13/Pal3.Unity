// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Core.DataReader.Lgt;
    using Core.DataReader.Mv3;
    using Core.Primitives;
    using Core.Utilities;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Game.Rendering;
    using Game.Rendering.Material;
    using Game.Rendering.Native;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Color = UnityEngine.Color;
    using NativeColor = Core.Primitives.Color;

    public sealed class NativeLightingTests
    {
        private static LightNode Light(float strength, bool diffuse = true) => new()
        {
            LightColor = new NativeColor(strength, strength, strength),
            UseDiffuse = diffuse,
            GameBoxWorldMatrix = new GameBoxMatrix4x4 { Xx = 1, Yy = 1, Zz = 1, Tw = 1, Tz = -100000 },
        };

        [TestCase(3, 1, 2)]
        [TestCase(9, 4, 1)]
        [TestCase(38, 19, 1)]
        public void EquallyStrongLightsFollowReleasedPartitionAndShortSortOrder(int count, int first, int second)
        {
            var order = new int[count];
            GameBoxLightOrder.Sort(order, Enumerable.Repeat(.1f, count).ToArray());
            Assert.That(order.Take(2), Is.EqualTo(new[] { first, second }));
        }

        [TestCase("Pal3/ToonOpaque")]
        [TestCase("Pal3/ToonCutout")]
        [TestCase("Pal3/ToonTransparent")]
        [TestCase("Pal3/ToonWater")]
        [TestCase("Pal3/Opaque")]
        [TestCase("Pal3/Transparent")]
        [TestCase("Pal3/TransparentOpaquePart")]
        public void OriginalShadersRetainSrpBatcherCompatibility(string name)
        {
            // ShaderUtil's compatibility code needs both an active URP and compiled pass metadata.
            using var preview = new ResourceModelPreview(null, false);
            preview.Render(32, 32);
            Shader shader = Shader.Find(name);
            Assert.That(shader, Is.Not.Null);
            const BindingFlags methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo compilePass = typeof(UnityEditor.ShaderUtil).GetMethod("CompilePass", methods);
            MethodInfo check = typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", methods);
            Assert.That(compilePass, Is.Not.Null, "The installed Unity editor must expose synchronous pass compilation.");
            Assert.That(check, Is.Not.Null, "The installed Unity editor must expose its shader compatibility inspection.");
            var material = new Material(shader);
            try
            {
                compilePass.Invoke(null, new object[] { material, 0, true });
                Assert.That((int)check.Invoke(null, new object[] { shader, 0 }), Is.Zero, name);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeSelectionRejectsZeroDiffuseAndExcludesLightmappedGeometry(string variant)
        {
            using var lighting = new GameBoxNativeLighting(variant);
            lighting.SetLights(new LgtFile(new[] { Light(.1f), Light(.8f), Light(.3f), Light(1, false) }));
            var root = new GameObject("Native light selection");
            var camera = new GameObject("Native light camera");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                using IDisposable binding = lighting.Bind(renderer, Vector3.zero, false, false, false, 20);
                lighting.Apply(camera.AddComponent<Camera>());
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_GbLightCount"), Is.EqualTo(2));
                Assert.That(block.GetVector("_GbLightDiffuse0").x, Is.EqualTo(.8f));
                Assert.That(block.GetVector("_GbLightDiffuse1").x, Is.EqualTo(.3f));
                binding.Dispose();
                using IDisposable mapped = lighting.Bind(renderer, Vector3.zero, true, false, true, 20);
                lighting.Apply(camera.GetComponent<Camera>());
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_GbLightCount"), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }

        [TestCase("PAL3", 1, .01f)]
        [TestCase("PAL3A", 2, .8f)]
        public void SceneAttachmentUsesThePublishedVariantOrder(string variant, int count, float first)
        {
            using var lighting = new GameBoxNativeLighting(variant);
            lighting.SetLights(new LgtFile(new[] { Light(.8f), Light(.3f) }));
            var root = new GameObject("Original scene attachment");
            var camera = new GameObject("Original scene camera");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                using IDisposable binding = lighting.Bind(renderer, Vector3.zero, false, false, true, 20);
                lighting.Apply(camera.AddComponent<Camera>());
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_GbLightCount"), Is.EqualTo(count));
                Assert.That(block.GetVector("_GbLightDiffuse0").x, Is.EqualTo(first));
                if (variant == "PAL3")
                    Assert.That(block.GetVector("_GbLightAmbient0").x, Is.EqualTo(-.25f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void Pal3AUsesFallbackOnlyForAMissingLightFile(bool exists, int expected)
        {
            using var lighting = new GameBoxNativeLighting("PAL3A");
            lighting.SetLights(exists ? new LgtFile(Array.Empty<LightNode>()) : null);
            var root = new GameObject("Empty versus missing LGT");
            var camera = new GameObject("Empty-LGT camera");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                using IDisposable binding = lighting.Bind(renderer, Vector3.zero, false, false, false, 20);
                lighting.Apply(camera.AddComponent<Camera>());
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_GbLightCount"), Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }

        [Test]
        public void NativeNormalsAccumulateAcrossMaterialAttributesAndUseOriginalQuantization()
        {
            Mv3Vert[] vertices = { new(), new() { X = 64 }, new() { Y = 64 }, new() { Z = 640 } };
            Mv3Attribute Part(params ushort[] indices) => new()
            {
                IndexBuffers = new[] { new Mv3IndexBuffer { TriangleIndex = indices } },
            };
            ushort[] actual = Mv3NativeNormals.Calculate(new[] { new Mv3VertFrame { Vertices = vertices } },
                new[] { Part(0, 1, 2), Part(0, 3, 1) })[0];
            // Released PAL3 ComputeNormal oracle, with a ten-times-larger second face.
            Assert.That(actual, Is.EqualTo(new ushort[] { 16191, 16191, 0, 16255 }));
            GameBoxVector3 a = Mv3NativeNormals.Decode(actual[0]);
            GameBoxVector3 b = Mv3NativeNormals.Decode(actual[2]);
            GameBoxVector3 blend = (a + b) * .5f;
            Assert.That(GameBoxVector3.Magnitude(blend), Is.LessThan(.98f), "Morph normal interpolation must retain its length.");
        }

        [Test]
        public void AuthoredDirectionalLightRemainsACameraRelativePointWithMatrixScale()
        {
            using var lighting = new GameBoxNativeLighting("PAL3A");
            LightNode light = Light(1);
            light.LightType = GameBoxLightType.Directional;
            light.GameBoxWorldMatrix.Yy = 2;
            light.GameBoxWorldMatrix.Ty = 12345;
            lighting.SetLights(new LgtFile(new[] { light }));
            var root = new GameObject("Scaled directional-light object");
            var camera = new GameObject("Directional-light camera");
            try
            {
                root.transform.position = new Vector3(10, 20, 30);
                camera.transform.position = new Vector3(1, 2, 3);
                var renderer = root.AddComponent<MeshRenderer>();
                using IDisposable binding = lighting.Bind(renderer, Vector3.zero, false, false, false, 20);
                var view = camera.AddComponent<Camera>();
                lighting.Apply(view);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Vector4 initial = block.GetVector("_GbLightPosition0");
                Assert.That(initial.x, Is.EqualTo(-180).Within(.01));
                Assert.That(initial.y, Is.EqualTo(199640).Within(.01));
                Assert.That(initial.z, Is.EqualTo(-540).Within(.01));
                camera.transform.position += new Vector3(3, 4, 5);
                lighting.Apply(view);
                renderer.GetPropertyBlock(block);
                Vector4 moved = block.GetVector("_GbLightPosition0");
                Assert.That(moved - initial, Is.EqualTo(new Vector4(60, 80, 100, 0)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OriginalShaderGpuUsesBothMaterialTermsAndBothSelectedLights(string variant)
        {
            LightNode first = Light(.2f);
            first.LightColor = new NativeColor(.2f, .3f, .4f);
            LightNode second = Light(.3f);
            second.LightColor = new NativeColor(.4f, .3f, .2f);
            Color actual = Draw(variant, new[] { first, second }, false, false);
            AssertColor(actual, new Color(.38f, .48f, .58f, .8f));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OriginalShaderGpuKeepsVertexColorsOutsideLighting(string variant)
        {
            Color actual = Draw(variant, new[] { Light(1) }, true, false);
            AssertColor(actual, new Color(.25f, .5f, .75f, .6f));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OriginalShaderGpuMultipliesBothLightmapTexturesAndNativeAlpha(string variant)
        {
            Color actual = Draw(variant, new[] { Light(1) }, false, true);
            AssertColor(actual, new Color(.4f, .36f, .28f, .32f));
        }

        [Test]
        public void OriginalShaderGpuUsesAmbientWhenNoDiffuseLightIsEligible()
        {
            AssertColor(Draw("PAL3A", new[] { Light(1, false) }, false, false), new Color(.04f, .06f, .08f, .8f));
        }

        [Test]
        public void OriginalShaderGpuDoesNotCapVertexAttenuationAtSelectionLimit()
        {
            LightNode light = Light(.001f);
            light.GameBoxWorldMatrix.Tz = -.2f;
            light.LightDecayType = GameBoxLightDecayType.Distance;
            light.DecayRadius = 40;
            // A zero vertex-unit fixture gives every shader input the same native
            // point while retaining a large, visible rasterized test surface.
            AssertColor(Draw("PAL3A", new[] { light }, false, false, 0), new Color(.073333f, .1f, .126667f, .8f));
        }

        [Test]
        public void OriginalLightmapAlphaAppliesObjectBlendBeforeTwoTimesSaturation()
        {
            AssertColor(Draw("PAL3A", new[] { Light(1) }, false, true, saturatedLightmap: true, opacity: .5f),
                new Color(.4f, .36f, .28f, .8f));
        }

        [Test]
        [Category("ResourceBrowserLocalData")]
        public void RealDataOriginalLightingDrawsBothAdditionalMaterialGroupsAndNativeWeaponColors()
        {
            string variant = ResourceBrowserWindow.Variant;
            string dataRoot = Environment.GetEnvironmentVariable(variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(dataRoot))
                Assert.Ignore("Local game data is unavailable.");
            using var catalog = ResourceCatalog.OpenBaseData(dataRoot, 936);
            string[] models = variant == "PAL3A" ?
                new[] { "role\\101\\j21.mv3", "role\\526\\z1.mv3", "weapon\\WPB\\WPB.pol", "weapon\\JQN\\jqn.pol", "weapon\\WXA\\WXA.pol", "cbscene\\M15C\\m15c.pol" } :
                new[] { "role\\101\\z01.mv3", "weapon\\JT13\\JT13.pol", "cbscene\\Z04A\\Z04A.pol" };
            ResourceEntry lightEntry = catalog.Entries.First(entry => entry.Extension == ".lgt" && entry.Path.IndexOf("cbscene", StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (string path in models)
            {
                ResourceEntry entry = catalog.Entries.Single(value => value.Path.EndsWith(path, StringComparison.OrdinalIgnoreCase));
                using var lighting = new GameBoxNativeLighting(variant);
                lighting.SetLights((LgtFile)ResourceFormats.Read(lightEntry, catalog.Read(lightEntry), catalog.Codepage));
                using var preview = new ResourceModelPreview(catalog, false, lighting);
                if (entry.Extension == ".pol")
                {
                    preview.Load(entry, ResourceFormats.Read(entry, catalog.Read(entry), catalog.Codepage));
                    if (path.StartsWith("cbscene", StringComparison.OrdinalIgnoreCase))
                        preview.SetCamera(new Vector3(15, 18, 30), new Vector3(0, 2, 0), 50);
                    SaveRealCapture(preview, variant + "-" + path.Replace('\\', '-'));
                    continue;
                }
                byte[] bytes = catalog.Read(entry);
                var plain = (Mv3File)ResourceFormats.Read(entry, bytes, catalog.Codepage);
                var model = (Mv3File)ResourceFormats.Read(entry, bytes, catalog.Codepage, includeMv3NativeNormals: true);
                Assert.That(plain.Meshes.SelectMany(mesh => mesh.KeyFrames).All(frame => frame.NativeNormals == null), Is.True);
                Assert.That(model.Meshes.SelectMany(mesh => mesh.KeyFrames).All(frame => frame.NativeNormals?.Length == frame.GameBoxVertices.Length), Is.True);
                Assert.That(plain.Meshes.Select(mesh => mesh.GameBoxTriangles), Is.EqualTo(model.Meshes.Select(mesh => mesh.GameBoxTriangles)));
                GameObject target = preview.CreateObject("Real original-lit actor");
                var renderer = preview.SetVertexModel(target, entry.Path, model);
                renderer.SampleAnimation((float)model.TotalGameBoxTicks / 4800 * .37f);
                Assert.That(target.GetComponentsInChildren<MeshFilter>().Sum(filter => filter.sharedMesh.triangles.Length),
                    Is.EqualTo(model.Meshes.Sum(mesh => mesh.GameBoxTriangles.Length)));
                if (path.EndsWith("j21.mv3", StringComparison.OrdinalIgnoreCase))
                    Assert.That(model.Meshes.Sum(mesh => mesh.GameBoxTriangles.Length) / 3, Is.EqualTo(1470));
                if (path.Contains("526"))
                    Assert.That(model.Meshes.Sum(mesh => mesh.GameBoxTriangles.Length) / 3, Is.EqualTo(555));
                preview.Frame();
                SaveRealCapture(preview, variant + "-" + path.Replace('\\', '-'));
            }
        }

        private static void SaveRealCapture(ResourceModelPreview preview, string name)
        {
            RenderTexture rendered = preview.Render(512, 512);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = rendered;
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(30));
                string output = Environment.GetEnvironmentVariable("PAL3_NATIVE_LIGHTING_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void BakedNodeScalePreservesOriginalSourceSpaceLightingOnGpu()
        {
            LightNode light = Light(.8f);
            light.GameBoxWorldMatrix.Tx = -20;
            light.GameBoxWorldMatrix.Tz = -40;
            Color original = Draw("PAL3A", new[] { light }, false, false);
            Color baked = Draw("PAL3A", new[] { light }, false, false, bakedNodeScale: new Vector3(3, .5f, 2));
            AssertColor(baked, original);
        }

        private static Color Draw(string variant, LightNode[] lights, bool vertexColor, bool lightmapped,
            float vertexUnits = 20, bool saturatedLightmap = false, float opacity = 1, Vector3? bakedNodeScale = null)
        {
            using var lighting = new GameBoxNativeLighting(variant);
            lighting.SetLights(new LgtFile(lights));
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), false, lighting);
            using var preview = new ResourceModelPreview(null, false, lighting);
            var main = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var map = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var image = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            main.SetPixel(0, 0, lightmapped ? new Color(.5f, .6f, .7f, saturatedLightmap ? 1 : .8f) : Color.white);
            map.SetPixel(0, 0, new Color(.4f, .3f, .2f, saturatedLightmap ? 1 : .25f));
            main.Apply();
            map.Apply();
            IMaterial[] materials = manager.CreateStandardMaterials(RendererType.Pol,
                ("main", new UnityTexture2D(main)), lightmapped ? ("lightmap", new UnityTexture2D(map)) : default,
                new NativeColor(1, 1, 1, opacity), GameBoxBlendFlag.Opaque,
                new GameBoxMaterial { Diffuse = new NativeColor(.5f, .6f, .7f, .8f), Ambient = new NativeColor(.2f, .3f, .4f) });
            try
            {
                GameObject geometry = preview.CreateObject("Known native-light triangle");
                var mesh = new Mesh
                {
                    vertices = new[] { new Vector3(-1, -1), new Vector3(1, -1), new Vector3(1, 1), new Vector3(-1, 1) },
                    triangles = new[] { 0, 2, 1, 0, 3, 2 },
                    normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                    uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                    uv2 = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                    colors = new[] { new Color(.25f, .5f, .75f, .6f), new Color(.25f, .5f, .75f, .6f), new Color(.25f, .5f, .75f, .6f), new Color(.25f, .5f, .75f, .6f) },
                };
                geometry.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = geometry.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[0].NativeObject as Material;
                Matrix4x4 sourceToRenderer = Matrix4x4.Scale(bakedNodeScale ?? Vector3.one);
                if (bakedNodeScale.HasValue)
                {
                    mesh.vertices = mesh.vertices.Select(sourceToRenderer.MultiplyPoint3x4).ToArray();
                    Vector3 scale = bakedNodeScale.Value;
                    geometry.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                }
                using IDisposable binding = lighting.Bind(renderer, Vector3.zero, lightmapped, vertexColor, false, vertexUnits,
                    bakedNodeScale.HasValue ? () => sourceToRenderer : null);
                preview.SetCamera(new Vector3(0, 0, -5), Vector3.zero, 40);
                RenderTexture rendered = preview.Render(128, 128);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = rendered;
                    image.ReadPixels(new Rect(64, 64, 1, 1), 0, 0);
                    image.Apply();
                    Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
                    return image.GetPixel(0, 0);
                }
                finally { RenderTexture.active = previous; }
            }
            finally
            {
                manager.ReturnToPool(materials);
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(map);
                Object.DestroyImmediate(image);
            }
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.015f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.015f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.015f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.015f));
        }
    }
}
