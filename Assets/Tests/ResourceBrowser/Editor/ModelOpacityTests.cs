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
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Game.Rendering.Material;
    using Game.Rendering.Renderer;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Color = Core.Primitives.Color;

    public sealed class ModelOpacityTests
    {
        private sealed class CountingFactory : IMaterialFactory
        {
            public readonly List<IMaterial> Materials = new();
            private readonly UnityMaterialFactory _factory = new();

            public IMaterial CreateMaterial(string shaderName)
            {
                IMaterial material = _factory.CreateMaterial(shaderName);
                Materials.Add(material);
                return material;
            }

            public IMaterial CreateMaterialFrom(IMaterial template)
            {
                IMaterial material = _factory.CreateMaterialFrom(template);
                Materials.Add(material);
                return material;
            }
        }

        private sealed class Textures : ITextureResourceProvider, IDisposable
        {
            private readonly UnityTexture2D _texture;

            public Textures()
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, UnityEngine.Color.white);
                texture.Apply();
                _texture = new UnityTexture2D(texture);
            }

            public string GetTexturePath(string name) => "opacity/" + name;

            public ITexture2D GetTexture(string name) => GetTexture(name, out _);

            public ITexture2D GetTexture(string name, out bool alpha)
            {
                alpha = name.StartsWith("alpha", StringComparison.Ordinal);
                return _texture;
            }

            public void Dispose() => _texture.Destroy();
        }

        private static readonly GameBoxVector3[] Vertices =
        {
            new(-10, -10, 0), new(0, 10, 0), new(10, -10, 0)
        };

        private static Mv3File Model() => new(4800, Array.Empty<Mv3AnimationEvent>(),
            new[] { new Mv3TagNode { Name = "tag_weapon", TagFrames = new[] { new Mv3TagFrame { GameBoxRotation = new GameBoxQuaternion(0, 0, 0, 1) } } } },
            new[]
            {
                new Mv3Mesh
                {
                    Name = "body", Attributes = new[] { new Mv3Attribute { MaterialId = 0 } },
                    GameBoxTriangles = new[] { 0, 1, 2 }, NormalIndices = new[] { 0, 1, 2 }, NormalCount = 3,
                    Uvs = new[] { new GameBoxVector2(0, 0), new GameBoxVector2(0, 1), new GameBoxVector2(1, 0) },
                    KeyFrames = new[] { new Mv3AnimationKeyFrame { GameBoxVertices = Vertices } }
                }
            }, new[] { new GameBoxMaterial { TextureFileNames = new[] { "opaque.tga" } } });

        private static PolFile Weapon(GameBoxBlendFlag blend = GameBoxBlendFlag.Opaque, string texture = "opaque.tga") => new(1,
            new[] { new PolGeometryNode { Name = "weapon" } },
            new[]
            {
                new PolMesh
                {
                    VertexInfo = new PolVertexInfo
                    {
                        GameBoxPositions = Vertices, GameBoxNormals = new[] { new GameBoxVector3(0, 0, 1), new GameBoxVector3(0, 0, 1), new GameBoxVector3(0, 0, 1) },
                        Uvs = new[] { new[] { new GameBoxVector2(0, 0), new GameBoxVector2(0, 1), new GameBoxVector2(1, 0) } }
                    },
                    Textures = new[] { new PolTexture { BlendFlag = blend, Material = new GameBoxMaterial { TextureFileNames = new[] { texture } }, GameBoxTriangles = new[] { 0, 1, 2 } } }
                }
            }, Array.Empty<TagNode>());

        [TestCase(false)]
        [TestCase(true)]
        public void ContinuousFadeKeepsMaterialsPooledAndWeaponOpacityAcrossRebuilds(bool toon)
        {
            var factory = new CountingFactory();
            using var manager = new GameMaterialManager(factory, toon);
            using var textures = new Textures();
            var root = new GameObject("Opacity lifecycle");
            try
            {
                var renderer = root.AddComponent<Mv3ModelRenderer>();
                renderer.SetOpacity(0.44f);
                renderer.Init(Model(), manager, textures, tagNodePolFile: Weapon(), tagNodeTextureProvider: textures,
                    textureFactory: new UnityTextureFactory(), animateTagWater: false);
                AssertFade(root, 0.44f, 2);
                int allocated = factory.Materials.Count;
                for (int cycle = 0; cycle < 4; cycle++)
                {
                    for (int step = 0; step <= 50; step++)
                        renderer.SetOpacity(step / 50f);
                    renderer.SetOpacity(0.44f);
                    AssertFade(root, 0.44f, 2);
                }
                Assert.That(factory.Materials.Count, Is.EqualTo(allocated), "A fade must reuse its material pair.");
                renderer.ChangeTexture("alpha.tga");
                AssertFade(root, 0.44f, 2);
                renderer.ChangeTexture("opaque2.tga");
                AssertFade(root, 0.44f, 2);
                renderer.Init(Model(), manager, textures, tagNodePolFile: Weapon(), tagNodeTextureProvider: textures,
                    textureFactory: new UnityTextureFactory(), animateTagWater: false);
                AssertFade(root, 0.44f, 2);
                renderer.SetOpacity(1f);
                Assert.That(root.GetComponentsInChildren<MeshRenderer>().All(mesh => mesh.sharedMaterials.Length == 1), Is.True);
                renderer.Dispose();
                Assert.That(root.GetComponentsInChildren<MeshFilter>(), Is.Empty);
                renderer.Dispose();
                manager.Dispose();
                Assert.That(factory.Materials.All(material => material.IsNativeObjectDisposed), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AlphaTextureCutoutIsDisabledDuringFadeAndRestoredAtFullOpacity(bool toon)
        {
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), toon);
            using var textures = new Textures();
            var root = new GameObject("Alpha opacity");
            try
            {
                var renderer = root.AddComponent<PolyModelRenderer>();
                renderer.Render(Weapon(GameBoxBlendFlag.AlphaBlend), textures, manager, false,
                    tintColor: new Color(0.5f, 0.6f, 0.7f, 0.8f), textureFactory: new UnityTextureFactory(), animateWater: false);
                Material[] materials = root.GetComponentInChildren<MeshRenderer>().sharedMaterials;
                float[] thresholds = materials.Select(material => material.GetFloat("_Threshold")).ToArray();
                renderer.SetOpacity(0.44f);
                Assert.That(materials.All(material => material.GetFloat("_Threshold") == 2f), Is.True);
                Assert.That(materials.All(material => Math.Abs(material.GetColor("_TintColor").a - 0.352f) < 0.0001f), Is.True);
                renderer.SetOpacity(1f);
                Assert.That(materials.Select(material => material.GetFloat("_Threshold")), Is.EqualTo(thresholds));
                Assert.That(materials[0].GetColor("_TintColor").r, Is.EqualTo(0.5f));
                Assert.That(materials[0].GetColor("_TintColor").a, Is.EqualTo(0.8f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SelectionReleasesItsOwnPropertyBlockAndPreservesExistingLighting(bool existing)
        {
            using var manager = new GameMaterialManager(new UnityMaterialFactory(), true);
            using var textures = new Textures();
            var root = new GameObject("Selection batching") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var model = root.AddComponent<Mv3ModelRenderer>();
                model.Init(Model(), manager, textures);
                MeshRenderer renderer = root.GetComponentInChildren<MeshRenderer>();
                if (existing)
                {
                    var lighting = new MaterialPropertyBlock();
                    lighting.SetFloat("_GbLightCount", 2);
                    renderer.SetPropertyBlock(lighting);
                }
                model.SetSelectionLuminance(-1);
                Assert.That(renderer.HasPropertyBlock(), Is.EqualTo(existing));
                model.SetSelectionLuminance(.8f);
                Assert.That(renderer.HasPropertyBlock(), Is.True);
                model.SetOpacity(.44f);
                model.SetSelectionLuminance(-1);
                Assert.That(renderer.HasPropertyBlock(), Is.EqualTo(existing));
                var restored = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(restored);
                if (existing)
                    Assert.That(restored.GetFloat("_GbLightCount"), Is.EqualTo(2));
                model.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OpacityRejectsNonFiniteValuesAndClampsFiniteValues()
        {
            var root = new GameObject("Opacity bounds");
            try
            {
                var renderer = root.AddComponent<Mv3ModelRenderer>();
                Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetOpacity(float.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetOpacity(float.PositiveInfinity));
                Assert.DoesNotThrow(() => renderer.SetOpacity(-1f));
                Assert.DoesNotThrow(() => renderer.SetOpacity(2f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WaterFadeSurvivesPolRerenderAndReleasesItsGeometry(bool toon)
        {
            var factory = new CountingFactory();
            using var manager = new GameMaterialManager(factory, toon);
            using var textures = new Textures();
            var root = new GameObject("Water opacity");
            try
            {
                var renderer = root.AddComponent<PolyModelRenderer>();
                renderer.SetOpacity(0.44f);
                for (int i = 0; i < 3; i++)
                {
                    renderer.Render(Weapon(texture: "w0001.dds"), textures, manager, false,
                        textureFactory: new UnityTextureFactory(), animateWater: false);
                    Assert.That(root.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(1));
                    Material material = root.GetComponentInChildren<MeshRenderer>().sharedMaterial;
                    Assert.That(material.GetFloat("_Alpha"), Is.EqualTo(0.44f).Within(0.00001f));
                }
                Assert.That(factory.Materials.Count, Is.EqualTo(5), "Re-render must reuse the returned water material.");
                renderer.Dispose();
                Assert.That(root.transform.childCount, Is.Zero);
                manager.Dispose();
                Assert.That(factory.Materials.All(material => material.IsNativeObjectDisposed), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AssertFade(GameObject root, float opacity, int count)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers, Has.Length.EqualTo(count));
            foreach (MeshRenderer renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2));
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.That(material.GetColor("_TintColor").a, Is.EqualTo(opacity).Within(0.00001f));
                    Assert.That(material.GetFloat("_Threshold"), Is.EqualTo(2f));
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("ResourceBrowserLocalData")]
        public void RealActorAndWeaponFadeToBackgroundInBothMaterialStyles(bool toon)
        {
            string variant = ResourceBrowserWindow.Variant;
            string dataRoot = Environment.GetEnvironmentVariable(variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", variant));
            if (!Directory.Exists(dataRoot))
                Assert.Ignore("Local game data is unavailable: " + dataRoot);
            using var catalog = ResourceCatalog.Load(dataRoot, 936, variant);
            var actorModel = catalog.Entries.Where(entry => entry.Extension == ".mv3")
                .Select(entry => (entry, model: (Mv3File)ResourceFormats.Read(entry, catalog.Read(entry), catalog.Codepage)))
                .First(value => value.model.TagNodes.Any(tag => !tag.Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase)));
            ResourceEntry actor = actorModel.entry;
            Mv3File model = actorModel.model;
            ResourceEntry weaponEntry = catalog.Entries.First(entry => entry.Extension == ".pol" && entry.Path.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) >= 0);
            var weapon = (PolFile)ResourceFormats.Read(weaponEntry, catalog.Read(weaponEntry), catalog.Codepage);
            using var preview = new ResourceModelPreview(catalog, toon);
            GameObject target = preview.CreateObject("Opacity actor");
            Mv3ModelRenderer renderer = preview.SetVertexModel(target, actor.Path, model, weaponEntry.Path, weapon);
            Assert.That(target.GetComponentsInChildren<PolyModelRenderer>(), Is.Not.Empty, "The real tag hierarchy must include its weapon.");
            TestContext.WriteLine(variant + ": actor=" + actor.Path + "; weapon=" + weaponEntry.Path + "; toon=" + toon);
            preview.Frame();
            renderer.SetOpacity(1f);
            UnityEngine.Color32[] opaque = Read(preview);
            renderer.SetOpacity(0f);
            UnityEngine.Color32[] background = Read(preview);
            MeshRenderer[] meshes = target.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mesh in meshes)
                mesh.enabled = false;
            UnityEngine.Color32[] empty = Read(preview);
            Assert.That(background.Zip(empty, (a, b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)).Sum(),
                Is.LessThan(64), "Zero opacity must match the empty background.");
            foreach (MeshRenderer mesh in meshes)
                mesh.enabled = true;
            renderer.SetOpacity(0.44f);
            UnityEngine.Color32[] faded = Read(preview);
            Material[] fadedMaterials = target.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(mesh => mesh.sharedMaterials).ToArray();
            for (int step = 0; step < 204; step++)
                renderer.SetOpacity(step % 51 / 50f);
            renderer.SetOpacity(0.44f);
            Assert.That(target.GetComponentsInChildren<MeshRenderer>().SelectMany(mesh => mesh.sharedMaterials),
                Is.EqualTo(fadedMaterials), "Continuous real-model fading must reuse the same materials.");
            Assert.That(Read(preview), Is.EqualTo(faded), "Repeated fading must produce the same pixels.");
            long Distance(UnityEngine.Color32[] pixels) => pixels.Zip(background, (a, b) =>
                (long)Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)).Sum();
            Assert.That(Distance(opaque), Is.GreaterThan(1000), actor.Path);
            Assert.That(Distance(faded), Is.GreaterThan(100).And.LessThan(Distance(opaque)), actor.Path);
            TestContext.WriteLine("opaqueDiff=" + Distance(opaque) + "; faded044Diff=" + Distance(faded));
            string output = Environment.GetEnvironmentVariable("PAL3_OPACITY_QA_ROOT");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                foreach (var sample in new[] { ("opaque", opaque), ("invisible044", faded), ("zero", background) })
                {
                    var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                    try
                    {
                        image.SetPixels32(sample.Item2);
                        image.Apply();
                        File.WriteAllBytes(Path.Combine(output, variant + (toon ? "-toon-" : "-original-") + sample.Item1 + ".png"), image.EncodeToPNG());
                    }
                    finally { Object.DestroyImmediate(image); }
                }
            }
        }

        private static UnityEngine.Color32[] Read(ResourceModelPreview preview)
        {
            RenderTexture target = preview.Render(256, 256);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                image.Apply();
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
