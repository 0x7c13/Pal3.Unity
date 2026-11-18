// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleSceneAppearanceRenderingTests
    {
        [TestCase(0f)]
        [TestCase(.44f)]
        [TestCase(1f)]
        [Category("LocalGameData")]
        public void PaintedBackgroundsIgnoreDynamicLightsAcrossFadesAndDoNotLeakThroughMaterialReuse(float opacity)
        {
            using var data = new BattleData(Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT"), 936);
#if PAL3
            const string scene = "Z01";
#else
            const string scene = "M06A";
#endif
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = scene,
                DataFingerprint = data.Fingerprint,
                ToonRendering = true,
                Music = "NONE"
            };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(data.CreateParticipant(data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id)), BattleSide.Enemy, BattleSlot.Water));
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            var properties = new MaterialPropertyBlock();
            bool Background(MeshRenderer renderer)
            {
                renderer.GetPropertyBlock(properties);
                return properties.GetFloat("_UnlitBackground") > .5f;
            }
            MeshRenderer[] all = preview.View.Root.GetComponentsInChildren<MeshRenderer>();
            MeshRenderer[] backgrounds = all.Where(Background).ToArray();
            Assert.That(backgrounds, Is.Not.Empty);
            Assert.That(all.Except(backgrounds), Is.Not.Empty, "The selection must not encompass the ground or bodies.");
            foreach (MeshRenderer renderer in backgrounds)
            {
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
                Assert.That(renderer.receiveShadows, Is.False);
            }
            var backgroundMaterials = backgrounds.SelectMany(renderer => renderer.sharedMaterials).ToHashSet();
            var environment = backgrounds[0].GetComponentInParent<Game.Rendering.Renderer.PolyModelRenderer>();
            environment.SetOpacity(opacity);
            backgroundMaterials.UnionWith(backgrounds.SelectMany(renderer => renderer.sharedMaterials));
            foreach (MeshRenderer renderer in all)
                renderer.enabled = Background(renderer);
            FrameSelectedTriangle(preview.View, backgrounds[0]);
            var camera = (Camera)typeof(ResourceModelPreview).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview.View);
            camera.backgroundColor = UnityEngine.Color.clear;
            Light light = preview.View.Root.GetComponentInChildren<Light>();
            light.intensity = 0;
            Texture2D unlit = Read(preview.View.Render(400, 300));
            Texture2D bright = null;
            try
            {
                light.intensity = 3;
                light.transform.rotation = Quaternion.Euler(20, 130, 0);
                light.color = UnityEngine.Color.red;
                bright = Read(preview.View.Render(400, 300));
                Assert.That(bright.GetPixels32(), Is.EqualTo(unlit.GetPixels32()), "Painted backgrounds must not acquire light bands, AO shading or received shadows.");
                if (opacity > 0)
                    Assert.That(unlit.GetPixels32().Count(pixel => pixel.a > 0), Is.GreaterThan(100), "The image comparison must actually contain background pixels.");
                else
                    Assert.That(unlit.GetPixels32().All(pixel => pixel.a == 0), Is.True, "Zero opacity must remove the painted layer.");
            }
            finally
            {
                Object.DestroyImmediate(unlit);
                Object.DestroyImmediate(bright);
            }
            string path = Core.Contract.Constants.FileConstants.CombatSceneFolderVirtualPath + scene + "\\" + scene + ".pol";
            var model = data.Read(path, new Core.DataReader.Pol.PolFileReader());
            // Replacement constructs its candidate before releasing the old environment.
            // The second replacement can rent the original background materials.
            preview.View.ReplaceEnvironment(data.Resources.Find(path), model);
            preview.View.ReplaceEnvironment(data.Resources.Find(path), model);
            MeshRenderer[] reused = preview.View.Root.transform.Find("Battle environment " + data.Resources.Find(path).Name).GetComponentsInChildren<MeshRenderer>();
            Assert.That(reused.SelectMany(renderer => renderer.sharedMaterials).Any(backgroundMaterials.Contains), Is.True,
                "This assertion must exercise materials actually returned by the background environment.");
            foreach (MeshRenderer renderer in reused)
            {
                Assert.That(Background(renderer), Is.False);
                Assert.That(renderer.sharedMaterials.All(material => material.GetFloat("_UnlitBackground") == 0), Is.True);
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On));
            }
        }

#if PAL3A
        [Test]
        [Category("LocalGameData")]
        public void PaintedFarWaterKeepsItsOpaquePolBlendAndCannotMixTheCameraClearColor()
        {
            using var data = new BattleData(Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT"), 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = "m14a",
                DataFingerprint = data.Fingerprint,
                ToonRendering = true,
                Music = "NONE"
            };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(data.CreateParticipant(data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id)), BattleSide.Enemy, BattleSlot.Water));
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            var group = preview.View.Root.GetComponentsInChildren<Game.Dev.Presenters.PolMeshBatchInfoPresenter>().Single(presenter =>
                presenter.sources.Any(source => source.nodeName == "Plane02" && source.material.TextureFileNames.Contains("w0001.tga", StringComparer.OrdinalIgnoreCase)));
            MeshRenderer water = group.GetComponent<MeshRenderer>();
            Assert.That(water.sharedMaterial.shader.name, Is.EqualTo("Pal3/ToonOpaque"), "The source POL group is Opaque, despite its water-texture filename.");
            foreach (MeshRenderer renderer in preview.View.Root.GetComponentsInChildren<MeshRenderer>())
                renderer.enabled = renderer == water;
            FrameSelectedTriangle(preview.View, water);
            var camera = (Camera)typeof(ResourceModelPreview).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview.View);
            camera.backgroundColor = UnityEngine.Color.red;
            Texture2D red = Read(preview.View.Render(128, 128));
            Texture2D blue = null;
            try
            {
                camera.backgroundColor = UnityEngine.Color.blue;
                blue = Read(preview.View.Render(128, 128));
                UnityEngine.Color pixel = red.GetPixel(64, 64);
                Assert.That(blue.GetPixel(64, 64), Is.EqualTo(pixel), "The painted opaque layer cannot leak the camera's bottom color.");
                Assert.That(pixel.a, Is.EqualTo(1));
                Assert.That(pixel, Is.Not.EqualTo(UnityEngine.Color.red).And.Not.EqualTo(UnityEngine.Color.blue));
            }
            finally
            {
                Object.DestroyImmediate(red);
                Object.DestroyImmediate(blue);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("LocalGameData")]
        public void ExplicitFloorNormalsKeepTheStandingSurfaceLitAndPreserveNativeAndAdjacentGroupNormals(bool toon)
        {
            using var data = new BattleData(Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT"), 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = "M06B",
                DataFingerprint = data.Fingerprint,
                ToonRendering = toon,
                Music = "NONE"
            };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(data.CreateParticipant(data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id)), BattleSide.Enemy, BattleSlot.Water));
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            string geometry = Core.Contract.Constants.FileConstants.CombatSceneFolderVirtualPath + "M06B\\M06B.pol";
            var sourceMesh = data.Read(geometry, new Core.DataReader.Pol.PolFileReader()).Meshes[25];
            Vector3[] authoredNormals = sourceMesh.VertexInfo.GameBoxNormals.Select(normal => new Vector3(-normal.X, normal.Y, normal.Z)).ToArray();
            var selected = new List<Vector3>();
            var adjacent = new List<Vector3>();
            foreach (var presenter in preview.View.Root.GetComponentsInChildren<Game.Dev.Presenters.PolMeshBatchInfoPresenter>())
                foreach (var source in presenter.sources.Where(source => source.nodeName == "Cylinder02"))
                {
                    Vector3[] normals = presenter.GetComponent<MeshFilter>().sharedMesh.normals.Skip(source.vertexStart).Take(source.vertexCount).ToArray();
                    if (source.materialGroupIndex is 0 or 1 && (!toon || source.materialGroupIndex == 1))
                    {
                        var map = new Core.DataReader.Pol.PolMeshIndexMap(authoredNormals.Length, sourceMesh.Textures[source.materialGroupIndex].GameBoxTriangles);
                        Assert.That(normals.Length, Is.EqualTo(map.SourceIndices.Length));
                        for (int index = 0; index < normals.Length; index++)
                            Assert.That(Vector3.Distance(normals[index], authoredNormals[map.SourceIndices[index]]), Is.LessThan(1e-6f),
                                "Native and unselected adjacent groups retain each original source normal.");
                    }
                    if (source.materialGroupIndex == 0 && source.material.TextureFileNames.Contains("di.tga", StringComparer.OrdinalIgnoreCase))
                    {
                        selected.AddRange(normals);
                        var renderer = presenter.GetComponent<MeshRenderer>();
                        Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On));
                        Assert.That(renderer.receiveShadows, Is.True);
                        var properties = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(properties);
                        Assert.That(properties.GetFloat("_UnlitBackground"), Is.Zero);
                    }
                    else if (source.materialGroupIndex == 1 && source.material.TextureFileNames.Contains("bj.tga", StringComparer.OrdinalIgnoreCase))
                        adjacent.AddRange(normals);
                }
            Assert.That(selected.Count, Is.GreaterThan(3));
            Assert.That(adjacent.Count, Is.GreaterThan(3));
            Assert.That(selected.All(normal => Vector3.Dot(normal, Vector3.up) > .999f), Is.EqualTo(toon),
                "Only Toon reconstructs this verified horizontal floor; Native retains the released vertex normals.");
            Assert.That(adjacent.Any(normal => Vector3.Dot(normal, Vector3.up) < .9f), Is.True,
                "The adjacent painted horizon must not inherit the floor's normal reconstruction.");
        }
#endif

        [TestCase(.25f, .99f, 1f, 1f, false, true)]
        [TestCase(.6f, .99f, 1f, 1f, false, true)]
        [TestCase(.8f, .35f, 1f, 1f, false, true)]
        [TestCase(.8f, .99f, .44f, .6f, false, true)]
        [TestCase(.95f, 1f, 1f, 1f, false, true)]
        [TestCase(.6f, 1f, .75f, 1f, true, true)]
        [TestCase(.6f, 1f, .75f, 1f, true, false)]
        [TestCase(.2f, .2f, 1f, .5f, true, true)]
        public void PaintedTransparentStagesMatchNativeRgbAlphaAndPostStageClassificationOnGpu(
            float textureAlpha, float diffuseAlpha, float tintAlpha, float bakedAlpha, bool vertexColors, bool baked)
        {
            using var preview = new ResourceModelPreview(null, false);
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var lightmap = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, new Color(.25f, .5f, .75f, textureAlpha));
            texture.Apply();
            lightmap.SetPixel(0, 0, new Color(.6f, .7f, .8f, bakedAlpha));
            lightmap.Apply();
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-2, -2, 0), new Vector3(2, -2, 0), new Vector3(2, 2, 0), new Vector3(-2, 2, 0) },
                normals = Enumerable.Repeat(Vector3.forward, 4).ToArray(),
                colors = Enumerable.Repeat(new Color(.7f, .8f, .9f, .35f), 4).ToArray(),
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                uv2 = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var materials = new List<Material>();
            try
            {
                GameObject quad = preview.CreateObject("Painted original-stage comparison");
                quad.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("_GbVertexColor", vertexColors ? 1 : 0);
                properties.SetFloat("_GbLightCount", 0);
                renderer.SetPropertyBlock(properties);
                preview.SetCamera(new Vector3(0, 0, 5), Vector3.zero, 40);
                renderer.enabled = false;
                Texture2D clear = Read(preview.Render(128, 128));
                Color clearPixel;
                try
                {
                    clearPixel = clear.GetPixel(64, 64);
                }
                finally { Object.DestroyImmediate(clear); }
                renderer.enabled = true;
                for (int part = -1; part < 2; part++)
                {
                    Color native = Render(false, part);
                    Color toon = Render(true, part);
                    for (int channel = 0; channel < 4; channel++)
                        Assert.That(Math.Abs(toon[channel] - native[channel]), Is.LessThanOrEqualTo(2f / 255),
                            "Native and Painted must agree for the combined image and each classified pass independently.");
                    if (part == -1)
                        Assert.That(native, Is.Not.EqualTo(clearPixel), "The comparison must contain rendered surface pixels.");
                }

                Color Render(bool painted, int part)
                {
                    var pair = new Material[2];
                    string[] shaders = painted ? new[] { "Pal3/ToonCutout", "Pal3/ToonTransparent" } :
                        new[] { "Pal3/TransparentOpaquePart", "Pal3/Transparent" };
                    for (int index = 0; index < pair.Length; index++)
                    {
                        var material = new Material(Shader.Find(shaders[index]));
                        materials.Add(material);
                        material.mainTexture = texture;
                        material.SetTexture("_ShadowTex", lightmap);
                        material.SetFloat("_HasShadowTex", baked ? 1 : 0);
                        material.SetFloat("_LightmapScale", 2);
                        material.SetFloat("_Threshold", .9f);
                        material.SetColor("_TintColor", new Color(1, 1, 1, tintAlpha));
                        if (painted)
                        {
                            material.SetFloat("_UnlitBackground", 1);
                            material.SetFloat("_BackgroundBrightness", 1);
                            material.SetFloat("_SourceDiffuseAlpha", diffuseAlpha);
                        }
                        else
                        {
                            material.SetFloat("_GbNativeLighting", 1);
                            material.SetVector("_GbMaterialDiffuse", new Vector4(1, 1, 1, diffuseAlpha));
                            material.SetVector("_GbMaterialAmbient", Vector4.one);
                        }
                        pair[index] = material;
                    }
                    renderer.sharedMaterials = part < 0 ? pair : new[] { pair[part] };
                    Texture2D image = Read(preview.Render(128, 128));
                    try
                    {
                        return image.GetPixel(64, 64);
                    }
                    finally { Object.DestroyImmediate(image); }
                }
            }
            finally
            {
                foreach (Material material in materials)
                    Object.DestroyImmediate(material);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(lightmap);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MixedVertexColorChannelsStayCompleteAndUseIndependentBatchBindings(bool coloredFirst)
        {
            using var preview = new ResourceModelPreview(null, true);
            Core.DataReader.Pol.PolMesh MakeMesh(bool colored, float offset)
            {
                return new Core.DataReader.Pol.PolMesh
                {
                    VertexFvfFlag = (uint)(Core.Primitives.GameBoxVertexType.XYZ | Core.Primitives.GameBoxVertexType.Normal |
                        Core.Primitives.GameBoxVertexType.UV0 | (colored ? Core.Primitives.GameBoxVertexType.Diffuse : 0)),
                    VertexInfo = new Core.DataReader.Pol.PolVertexInfo
                    {
                        GameBoxPositions = new[] { new Core.Primitives.GameBoxVector3(offset, 0, 0),
                            new Core.Primitives.GameBoxVector3(offset + 10, 0, 0), new Core.Primitives.GameBoxVector3(offset, 10, 0) },
                        GameBoxNormals = Enumerable.Repeat(new Core.Primitives.GameBoxVector3(0, 0, 1), 3).ToArray(),
                        DiffuseColors = colored ? Enumerable.Repeat(new Core.Primitives.Color32(255, 128, 64, 90), 3).ToArray() : Array.Empty<Core.Primitives.Color32>(),
                        Uvs = new[] { new[] { new Core.Primitives.GameBoxVector2(0, 0), new Core.Primitives.GameBoxVector2(1, 0), new Core.Primitives.GameBoxVector2(0, 1) } }
                    },
                    Textures = new[] { new Core.DataReader.Pol.PolTexture
                    {
                        BlendFlag = Core.Primitives.GameBoxBlendFlag.Opaque,
                        Material = new Core.Primitives.GameBoxMaterial { Diffuse = Core.Primitives.Color.White,
                            Ambient = Core.Primitives.Color.White, TextureFileNames = new[] { string.Empty } },
                        GameBoxTriangles = new[] { 0, 1, 2 }
                    } }
                };
            }
            var model = new Core.DataReader.Pol.PolFile(100,
                new[] { new Core.DataReader.Pol.PolGeometryNode { Name = "first" }, new Core.DataReader.Pol.PolGeometryNode { Name = "second" } },
                new[] { MakeMesh(coloredFirst, 1), MakeMesh(!coloredFirst, 12) }, Array.Empty<Core.DataReader.Pol.TagNode>());
            preview.ReplaceEnvironment(new ResourceEntry("basedata.cpk\\mixed.pol"), model, (_, _) => null);
            MeshFilter[] meshes = preview.Root.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes.Length, Is.EqualTo(2), "A renderer-wide vertex-color flag requires separate bindings for unlike channels.");
            foreach (MeshFilter filter in meshes)
            {
                Mesh mesh = filter.sharedMesh;
                Assert.That(mesh.colors32.Length, Is.EqualTo(0).Or.EqualTo(mesh.vertexCount), "Every enabled channel must cover every batched vertex.");
                var properties = new MaterialPropertyBlock();
                filter.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_GbVertexColor"), Is.EqualTo(mesh.colors32.Length > 0 ? 1 : 0));
            }
        }

        private static void FrameSelectedTriangle(ResourceModelPreview preview, MeshRenderer renderer)
        {
            // The authored battle camera may contain no sky at all. Frame an
            // actual selected triangle so the pixel comparison cannot pass empty.
            MeshFilter surface = renderer.GetComponent<MeshFilter>();
            Vector3[] vertices = surface.sharedMesh.vertices;
            int[] triangles = surface.sharedMesh.triangles;
            Vector3 a = surface.transform.TransformPoint(vertices[triangles[0]]);
            Vector3 b = surface.transform.TransformPoint(vertices[triangles[1]]);
            Vector3 c = surface.transform.TransformPoint(vertices[triangles[2]]);
            Vector3 center = (a + b + c) / 3;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Assert.That(normal.sqrMagnitude, Is.GreaterThan(.9f));
            float distance = Math.Max(.1f, Math.Min(Vector3.Distance(a, b), Vector3.Distance(a, c)) * .5f);
            preview.SetCamera(center + normal * distance, center, 50);
        }

        private static Texture2D Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }
    }
}
