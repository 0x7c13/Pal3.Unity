// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Core.DataReader;
    using Core.DataReader.Cvd;
    using Core.DataReader.Data;
    using Core.DataReader.Lgt;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Game.Effect.Runtime;
    using Game.Rendering.Material;
    using Game.Rendering.Native;
    using Game.Rendering.Renderer;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class NativeModelEffectTests
    {
        private sealed class WhiteTextures : ITextureResourceProvider
        {
            public string GetTexturePath(string name) => name;

            public ITexture2D GetTexture(string name) => GetTexture(name, out _);

            public ITexture2D GetTexture(string name, out bool hasAlphaChannel)
            {
                hasAlphaChannel = false;
                return new UnityTexture2D(Texture2D.whiteTexture);
            }
        }

        private static PolFile ColoredPol(string texture, bool unreferencedVertex = false)
        {
            GameBoxVector3[] positions = unreferencedVertex ?
                new[] { new GameBoxVector3(-20, -20, 0), new GameBoxVector3(20, -20, 0), new GameBoxVector3(20, 20, 0), new GameBoxVector3(-20, 20, 0), new GameBoxVector3(100, 80, 40) } :
                new[] { new GameBoxVector3(-20, -20, 0), new GameBoxVector3(20, -20, 0), new GameBoxVector3(20, 20, 0), new GameBoxVector3(-20, 20, 0) };
            return new PolFile(1, new[] { new PolGeometryNode { Name = "Authored quad" } },
                new[]
                {
                    new PolMesh
                    {
                        VertexFvfFlag = GameBoxVertexType.XYZ | GameBoxVertexType.Diffuse | GameBoxVertexType.UV0,
                        VertexInfo = new PolVertexInfo
                        {
                            GameBoxPositions = positions,
                            DiffuseColors = Enumerable.Repeat(new Core.Primitives.Color32(64, 128, 192, 128), positions.Length).ToArray(),
                            Uvs = new[] { new GameBoxVector2[positions.Length] },
                        },
                        Textures = new[]
                        {
                            new PolTexture
                            {
                                BlendFlag = GameBoxBlendFlag.AlphaBlend,
                                Material = new GameBoxMaterial { TextureFileNames = new[] { texture } },
                                GameBoxTriangles = new[] { 0, 2, 1, 0, 3, 2 },
                            },
                        },
                    },
                }, Array.Empty<TagNode>());
        }

        [Test]
        public void AuthoredPolKeepsBlendAndCompleteSourceBoundsWithoutWaterSubstitution()
        {
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), false);
            var root = new GameObject("Authored model policy");
            try
            {
                PolFile model = ColoredPol("w0001.dds", true);
                var renderer = root.AddComponent<PolyModelRenderer>();
                renderer.Render(model, new WhiteTextures(), manager, false, textureFactory: new UnityTextureFactory(), animateWater: false,
                    modelMode: PolModelMode.Authored);
                Material[] materials = root.GetComponentInChildren<MeshRenderer>().sharedMaterials;
                Assert.That(materials.Select(material => material.shader.name), Is.EqualTo(new[] { "Pal3/TransparentOpaquePart", "Pal3/Transparent" }));
                Mesh mesh = root.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertexCount, Is.EqualTo(4), "Unused geometry can be omitted without shrinking the authored selection/framing volume.");
                var sourceBounds = new Bounds(model.Meshes[0].VertexInfo.GameBoxPositions[0].ToUnityPosition(), Vector3.zero);
                foreach (GameBoxVector3 position in model.Meshes[0].VertexInfo.GameBoxPositions)
                    sourceBounds.Encapsulate(position.ToUnityPosition());
                Assert.That(mesh.bounds, Is.EqualTo(sourceBounds));
                PolMesh source = model.Meshes[0];
                PolTexture section = source.Textures[0];
                section.Material.TextureFileNames = new[] { "white.tga", "" };
                source.Textures[0] = section;
                model.Meshes[0] = source;
                renderer.Render(model, new WhiteTextures(), manager, false, textureFactory: new UnityTextureFactory(), animateWater: false,
                    modelMode: PolModelMode.Authored);
                Assert.That(root.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetFloat("_HasShadowTex"), Is.Zero,
                    "An empty second texture slot must preserve the model's single-texture material.");
                model = ColoredPol("w0001.dds", true);
                renderer.Render(model, new WhiteTextures(), manager, false, textureFactory: new UnityTextureFactory(), animateWater: false);
                Assert.That(root.GetComponentInChildren<MeshRenderer>().sharedMaterial.shader.name, Is.EqualTo("Pal3/Water"),
                    "The default game's water convention must remain intact.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AuthoredPolVertexRgbaAndFadeMatchAUniformColorOnGpuWithoutSceneLights()
        {
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), false);
            using var preview = new ResourceModelPreview(null, false);
            var renderer = preview.CreateObject("Authored colors").AddComponent<PolyModelRenderer>();
            renderer.Render(ColoredPol("white.tga"), new WhiteTextures(), manager, false, textureFactory: new UnityTextureFactory(), animateWater: false,
                modelMode: PolModelMode.Authored);
            preview.SetCamera(new Vector3(0, 0, -5), Vector3.zero, 40);
            MeshRenderer meshRenderer = renderer.GetComponentInChildren<MeshRenderer>();
            Material[] materials = meshRenderer.sharedMaterials;
            meshRenderer.enabled = false;
            UnityEngine.Color background = Center(preview);
            meshRenderer.enabled = true;
            var reference = new Material(Shader.Find("Pal3/Transparent")) { mainTexture = Texture2D.whiteTexture };
            reference.SetFloat("_Threshold", 2);
            var properties = new MaterialPropertyBlock();
            try
            {
                foreach (float opacity in new[] { 1f, .5f })
                {
                    meshRenderer.sharedMaterials = materials;
                    renderer.SetOpacity(opacity);
                    meshRenderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_GbVertexColor"), Is.EqualTo(1));
                    Assert.That(meshRenderer.sharedMaterial.GetFloat("_GbNativeLighting"), Is.Zero);
                    UnityEngine.Color actual = Center(preview);
                    properties.SetFloat("_GbVertexColor", 0);
                    meshRenderer.SetPropertyBlock(properties);
                    reference.SetColor("_TintColor", new UnityEngine.Color(64f / 255, 128f / 255, 192f / 255, 128f / 255 * opacity));
                    meshRenderer.sharedMaterials = new[] { reference };
                    UnityEngine.Color expected = Center(preview);
                    Assert.That(Math.Abs(expected.r - background.r) + Math.Abs(expected.g - background.g) + Math.Abs(expected.b - background.b),
                        Is.GreaterThan(.05f), "The GPU comparison must contain visible geometry.");
                    Assert.That(actual.r, Is.EqualTo(expected.r).Within(.015f));
                    Assert.That(actual.g, Is.EqualTo(expected.g).Within(.015f));
                    Assert.That(actual.b, Is.EqualTo(expected.b).Within(.015f));
                    Assert.That(actual.a, Is.EqualTo(expected.a).Within(.015f));
                    properties.SetFloat("_GbVertexColor", 1);
                    meshRenderer.SetPropertyBlock(properties);
                }
            }
            finally
            {
                meshRenderer.sharedMaterials = materials;
                Object.DestroyImmediate(reference);
                renderer.Dispose();
            }
        }

        private static UnityEngine.Color Center(ResourceModelPreview preview)
        {
            RenderTexture target = preview.Render(64, 64);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(32, 32, 1, 1), 0, 0);
                image.Apply();
                return image.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void NativeCvdKeepsAuthoredCurrentFrameNormalsAndMaterialTerms()
        {
            CvdVertex[] Frame(GameBoxVector3 normal) => new[]
            {
                new CvdVertex { GameBoxPosition = new GameBoxVector3(-20, 0, -20), GameBoxNormal = normal },
                new CvdVertex { GameBoxPosition = new GameBoxVector3(20, 0, -20), GameBoxNormal = normal },
                new CvdVertex { GameBoxPosition = new GameBoxVector3(0, 0, 20), GameBoxNormal = normal },
            };
            var source = new GameBoxMaterial
            {
                Diffuse = new Core.Primitives.Color(.4f, .5f, .6f, .7f),
                Ambient = new Core.Primitives.Color(.1f, .2f, .3f),
                TextureFileNames = new[] { "white.tga" },
            };
            var node = new CvdGeometryNode
            {
                IsGeometryNode = true,
                Scale = 1,
                Children = Array.Empty<CvdGeometryNode>(),
                PositionKeyInfos = new[] { new CvdAnimationPositionKeyFrame { KeyType = CvdAnimationKeyType.Linear } },
                RotationKeyInfos = new[] { new CvdAnimationRotationKeyFrame { KeyType = CvdAnimationKeyType.Linear, GameBoxRotation = new GameBoxQuaternion(0, 0, 0, 1) } },
                ScaleKeyInfos = new[] { new CvdAnimationScaleKeyFrame { KeyType = CvdAnimationKeyType.Linear, GameBoxScale = new GameBoxVector3(3, 2, .5f), GameBoxRotation = new GameBoxQuaternion(0, 0, 0, 1) } },
                Mesh = new CvdMesh
                {
                    AnimationTimeKeys = new[] { 0f, 1f },
                    MeshSections = new[]
                    {
                        new CvdMeshSection
                        {
                            FrameVertices = new[] { Frame(new GameBoxVector3(0, 1, 0)), Frame(new GameBoxVector3(1, 0, 0)) },
                            GameBoxTriangles = new[] { 0, 1, 2 }, Material = source,
                        },
                    },
                },
            };
            using var lighting = new GameBoxNativeLighting("PAL3A");
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), false, lighting);
            var root = new GameObject("Native CVD regression");
            try
            {
                var renderer = root.AddComponent<CvdModelRenderer>();
                renderer.Init(new CvdFile(1, new[] { node }), new WhiteTextures(), manager);
                renderer.SetCurrentTime(.5f);
                Mesh mesh = root.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(mesh.normals, Is.EqualTo(Enumerable.Repeat(Vector3.back, 3).ToArray()), "The native IPOL draw does not interpolate authored normals.");
                Material material = root.GetComponentInChildren<MeshRenderer>().sharedMaterial;
                Assert.That(material.GetFloat("_GbNativeLighting"), Is.EqualTo(1));
                Assert.That(material.GetColor("_GbMaterialDiffuse").a, Is.EqualTo(.7f));
                renderer.SetCurrentTime(1);
                Assert.That(mesh.normals, Is.EqualTo(Enumerable.Repeat(Vector3.left, 3).ToArray()));
                var animation = renderer.PlayAnimationAsync(1, 1, 1, true);
                Assert.That(animation.MoveNext(), Is.True);
                var sourceField = typeof(CvdModelRenderer).GetField("_animationCts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var cancellation = (System.Threading.CancellationTokenSource)sourceField.GetValue(renderer);
                renderer.Dispose();
                Assert.That(root.transform.childCount, Is.Zero);
                Assert.That(mesh == null, Is.True);
                Assert.Throws<ObjectDisposedException>(() => cancellation.Token.ToString());
                Assert.That(sourceField.GetValue(renderer), Is.Null);
                var cache = (System.Collections.IDictionary)typeof(CvdModelRenderer).GetField("_textureCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(renderer);
                Assert.That(cache.Count, Is.Zero);
                renderer.Dispose();
                renderer.Init(new CvdFile(1, new[] { node }), new WhiteTextures(), manager);
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                renderer.Dispose();
                Assert.That(root.transform.childCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("POL", false)]
        [TestCase("POL", true)]
        [TestCase("CVD", true)]
        [Category("ResourceBrowserLocalData")]
        public void ActualModelEffectsRetainAuthoredVertexDataWithOptionalSceneLighting(string kind, bool native)
        {
            string variant = ResourceBrowserWindow.Variant;
            string dataRoot = Environment.GetEnvironmentVariable(variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(dataRoot))
                Assert.Ignore("Local game data is unavailable.");
            using var catalog = ResourceCatalog.OpenBaseData(dataRoot, 936);
            using var resources = new EffectResources(catalog.FileSystem, 936);
            using var lighting = native ? new GameBoxNativeLighting(variant) : null;
            ResourceEntry lightEntry = catalog.Entries.First(entry => entry.Extension == ".lgt" && entry.Path.IndexOf("cbscene", StringComparison.OrdinalIgnoreCase) >= 0);
            lighting?.SetLights((LgtFile)ResourceFormats.Read(lightEntry, catalog.Read(lightEntry), 936));
            using var preview = new ResourceModelPreview(catalog, false, lighting);
            EffectDefinition definition = resources.Definitions.EffectDefinitions.First(value => value.IsValid &&
                EffectClass.Name(value.ClassNameCrc) == "model" && value.TextureName.EndsWith("." + kind, StringComparison.OrdinalIgnoreCase));
            resources.Definitions.Groups[0] = new EffectGroup
            {
                Objects = new[]
                {
                    new EffectGroupObject { TypeCrc = definition.TypeNameCrc, Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10, SpeedAlpha = -.5f } } },
                },
            };
            var player = preview.CreateObject("Actual model effect").AddComponent<EffectPlayer>();
            player.AutoAdvance = false;
            player.Initialize(resources, 0, nativeLighting: preview.NativeLighting);
            Assert.That(player.Error, Is.Null, definition.TextureName);
            preview.Rendering += player.Render;
            player.AdvanceFrame(.02f);
            preview.Render(64, 64);
            MeshRenderer[] renderers = preview.Root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers, Is.Not.Empty, definition.TextureName);
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials).All(material => material.GetFloat("_GbNativeLighting") == (native ? 1 : 0)), Is.True);
            if (kind == "POL")
            {
                PolFile model = resources.PolModel(definition.TextureName);
                if (model.Meshes.Any(mesh => (mesh.VertexFvfFlag & GameBoxVertexType.Diffuse) != 0))
                {
                    Assert.That(preview.Root.GetComponentsInChildren<MeshFilter>().All(filter => filter.sharedMesh.colors32.Length == filter.sharedMesh.vertexCount), Is.True,
                        "The real model effect's vertex colors must reach its mesh.");
                    var block = new MaterialPropertyBlock();
                    foreach (MeshRenderer meshRenderer in renderers)
                    {
                        meshRenderer.GetPropertyBlock(block);
                        Assert.That(block.GetFloat("_GbVertexColor"), Is.EqualTo(1), "The model fade must preserve its vertex-color property.");
                    }
                }
            }
            else
                Assert.That(preview.Root.GetComponentsInChildren<MeshFilter>().All(filter => filter.sharedMesh.normals.Length == filter.sharedMesh.vertexCount), Is.True);
            preview.Frame();
            RenderTexture rendered = preview.Render(128, 128);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rendered;
                image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(20).Count(), Is.GreaterThan(10), definition.TextureName);
                string output = Environment.GetEnvironmentVariable("PAL3_NATIVE_LIGHTING_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                    File.WriteAllBytes(Path.Combine(output, variant + "-effect-" + kind + (native ? "" : "-standalone") + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
            Assert.That(player.Error, Is.Null);
        }
    }
}
