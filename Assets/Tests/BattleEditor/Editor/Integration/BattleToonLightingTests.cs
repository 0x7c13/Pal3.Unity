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
    using System.Runtime.ExceptionServices;
    using Core.Contract.Constants;
    using Core.DataReader.Pol;
    using Game.Rendering.Renderer;
    using Game.Rendering.Toon;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class BattleToonLightingTests
    {
        [TestCase(null, true)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void GameDefaultsToToonLightingAndHonorsAnExplicitSelection(bool? stored, bool expected)
        {
            var store = new Core.Utilities.InMemoryKeyValueStore();
            if (stored.HasValue)
                store.Set(nameof(Game.Settings.GameSettings.ToonRendering), stored.Value);
            using var settings = new Game.Settings.GameSettings(store);
            settings.InitOrLoadSettings();
            Assert.That(settings.ToonRendering, Is.EqualTo(expected));
            Assert.That(new Battle.BattleScenario().ToonRendering, Is.True);
        }

        private static ToonLightingProfile Profile(Quaternion? rotation = null, Color? color = null,
            float intensity = 1, float shadow = .95f, float fill = .9f, float contrast = 1,
            float baked = .65f, float contact = 1.25f) =>
            new(rotation ?? Quaternion.identity, color ?? Color.white, intensity, shadow, fill, contrast, baked, contact, false, false);

        private static JObject Entry(string variant = "PAL3", string scene = "Z01", float intensity = 1) =>
            new()
            {
                ["variant"] = variant,
                ["scene"] = scene,
                ["label"] = "test lighting",
                ["keyEuler"] = new JArray(58, -30, 0),
                ["temperature"] = 6500,
                ["tint"] = new JArray(1, .94f, .86f),
                ["intensity"] = intensity,
                ["shadowStrength"] = .95f,
                ["characterFill"] = .9f,
                ["contrast"] = 1,
                ["bakedStrength"] = .65f,
                ["contactStrength"] = 1.25f,
                ["night"] = false,
                ["indoors"] = false
            };

        private static string Document(params JObject[] entries) =>
            new JObject { ["schemaVersion"] = 1, ["profiles"] = new JArray(entries) }.ToString(Formatting.None);

        private static BattleSurfaceOverride Painted(int node, int group, string name, string texture, float brightness = 1) =>
            new(node, group, name, texture, PolSurfaceAppearance.Painted(brightness));

        private static JObject Background(int node = 0, int group = 1, string name = "painted horizon", string texture = "distant.tga") =>
            new()
            {
                ["nodeIndex"] = node,
                ["materialGroupIndex"] = group,
                ["nodeName"] = name,
                ["texture"] = texture
            };

        private static PolFile Model() => new(1,
            new[] { new PolGeometryNode { Name = "painted horizon" }, new PolGeometryNode { Name = "real ground" } },
            new[]
            {
                new PolMesh { Textures = new[]
                {
                    new PolTexture { Material = new Core.Primitives.GameBoxMaterial { TextureFileNames = new[] { "ground.tga" } } },
                    new PolTexture { Material = new Core.Primitives.GameBoxMaterial { TextureFileNames = new[] { "^L_horizon.bmp", "distant.tga" } } }
                } },
                new PolMesh { Textures = new[]
                {
                    new PolTexture { Material = new Core.Primitives.GameBoxMaterial { TextureFileNames = new[] { "ground.tga" } } }
                } }
            }, Array.Empty<TagNode>());

        private static Dictionary<string, BattleSceneAppearance> Parse(string text)
        {
            try
            {
                return (Dictionary<string, BattleSceneAppearance>)typeof(BattleToonLightingCatalog)
                    .GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { text });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static void RejectDocument(string text)
        {
            Exception exception = Assert.Catch(() => Parse(text));
            Assert.That(exception, Is.InstanceOf<InvalidDataException>().Or.InstanceOf<ArgumentException>().Or.InstanceOf<JsonException>());
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void ProfilesRejectNonfiniteValuesInEveryStoredChannel(float value)
        {
            Func<ToonLightingProfile>[] invalid =
            {
                () => Profile(intensity: value),
                () => Profile(shadow: value),
                () => Profile(fill: value),
                () => Profile(contrast: value),
                () => Profile(baked: value),
                () => Profile(contact: value),
                () => Profile(color: new Color(value, 1, 1, 1)),
                () => Profile(color: new Color(1, value, 1, 1)),
                () => Profile(color: new Color(1, 1, value, 1)),
                () => Profile(color: new Color(1, 1, 1, value)),
                () => Profile(rotation: new Quaternion(value, 0, 0, 1))
            };
            foreach (Func<ToonLightingProfile> create in invalid)
                Assert.Catch<ArgumentException>(() => create());
        }

        [Test]
        public void ProfilesAcceptSupportedEndpointsAndRejectOutOfRangeValuesAndNonunitRotation()
        {
            Assert.DoesNotThrow(() => Profile(color: Color.black, intensity: 0, shadow: 0, fill: 0, contrast: 0, baked: 0, contact: 0));
            Assert.DoesNotThrow(() => Profile(color: Color.white, intensity: 4, shadow: 1, fill: 1, contrast: 1, baked: 1, contact: 2));
            Assert.Catch<ArgumentException>(() => Profile(rotation: new Quaternion(0, 0, 0, 0)));
            Assert.Catch<ArgumentException>(() => Profile(rotation: new Quaternion(0, 0, 0, 2)));
            Assert.Catch<ArgumentException>(() => default(ToonLightingProfile).Validate());
            foreach (float value in new[] { -.01f, 1.01f })
            {
                Assert.Catch<ArgumentException>(() => Profile(shadow: value));
                Assert.Catch<ArgumentException>(() => Profile(fill: value));
                Assert.Catch<ArgumentException>(() => Profile(contrast: value));
                Assert.Catch<ArgumentException>(() => Profile(baked: value));
                Assert.Catch<ArgumentException>(() => Profile(color: new Color(value, 1, 1)));
            }
            Assert.Catch<ArgumentException>(() => Profile(intensity: -.01f));
            Assert.Catch<ArgumentException>(() => Profile(intensity: 4.01f));
            Assert.Catch<ArgumentException>(() => Profile(contact: -.01f));
            Assert.Catch<ArgumentException>(() => Profile(contact: 2.01f));
        }

        [Test]
        public void CapturedProfilesDoNotShareMutableColorRotationOrLaterParsedConfiguration()
        {
            var color = new Color(.2f, .3f, .4f, 1);
            Quaternion rotation = Quaternion.identity;
            ToonLightingProfile captured = Profile(rotation, color);
            color.r = 1;
            rotation.x = 1;
            Color returned = captured.LightColor;
            returned.g = 1;
            Assert.That(captured.LightColor, Is.EqualTo(new Color(.2f, .3f, .4f, 1)));
            Assert.That(captured.Rotation, Is.EqualTo(Quaternion.identity));
            JObject entry = Entry(intensity: .8f);
            Dictionary<string, BattleSceneAppearance> first = Parse(Document(entry));
            BattleSceneAppearance saved = first["PAL3/Z01"];
            entry["intensity"] = 2;
            entry["tint"][0] = .1f;
            Dictionary<string, BattleSceneAppearance> second = Parse(Document(entry));
            first.Clear();
            Assert.That(saved.Lighting.Intensity, Is.EqualTo(.8f));
            Assert.That(second["PAL3/Z01"].Lighting.Intensity, Is.EqualTo(2));
            Assert.That(saved.Lighting.LightColor, Is.Not.EqualTo(second["PAL3/Z01"].Lighting.LightColor));
        }

        [Test]
        public void ParsedKeysKeepVariantsSeparateAndMatchSceneCaseInsensitively()
        {
            Dictionary<string, BattleSceneAppearance> profiles = Parse(Document(Entry("PAL3", "Z01", .8f), Entry("PAL3A", "z01", 1.8f)));
            Assert.That(profiles.Count, Is.EqualTo(2));
            Assert.That(profiles["pal3/z01"].Lighting.Intensity, Is.EqualTo(.8f));
            Assert.That(profiles["PAL3A/Z01"].Lighting.Intensity, Is.EqualTo(1.8f));
            RejectDocument(Document(Entry("PAL3", "Z01"), Entry("PAL3", "z01")));
        }

        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"schemaVersion\":2,\"profiles\":[]}")]
        [TestCase("{\"schemaVersion\":1}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[null]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[],\"unknownOption\":true}")]
        public void UnsupportedOrIncompleteDocumentsAreRejected(string text) => RejectDocument(text);

        [TestCase("keyEuler", "[0,361,0]")]
        [TestCase("keyEuler", "[0,0]")]
        [TestCase("keyEuler", "[0,\"NaN\",0]")]
        [TestCase("temperature", "999")]
        [TestCase("temperature", "12001")]
        [TestCase("tint", "[1,1]")]
        [TestCase("tint", "[1,-0.1,1]")]
        [TestCase("intensity", "\"Infinity\"")]
        [TestCase("scene", "\" \"")]
        [TestCase("variant", "\"PAL4\"")]
        public void InvalidAuthoredValuesAreRejected(string property, string value)
        {
            JObject entry = Entry();
            entry[property] = JToken.Parse(value);
            RejectDocument(Document(entry));
        }

        [Test]
        public void BackgroundSelectionValidatesIndicesNamesAndAnyTextureSlotWithoutMatchingNeighborGroups()
        {
            PolFile model = Model();
            var appearance = new BattleSceneAppearance(Profile(), new[] { Painted(0, 1, "painted horizon", "DISTANT.TGA") });
            Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(model);
            Assert.That(selected(0, 1)?.Brightness, Is.EqualTo(1));
            Assert.That(selected(0, 0), Is.Null, "A shared POL node may contain both real ground and a painted background.");
            Assert.That(selected(1, 0), Is.Null);
            Assert.That(selected(-1, 1), Is.Null);
            Assert.That(selected(0, 2), Is.Null);
            var firstTexture = new BattleSceneAppearance(Profile(), new[] { Painted(0, 1, "painted horizon", "^L_horizon.bmp") });
            Assert.That(firstTexture.ResolveSurfaces(model)(0, 1)?.Brightness, Is.EqualTo(1));
            Assert.That(new BattleSceneAppearance(Profile()).ResolveSurfaces(model), Is.Null);
        }

        [Test]
        public void BackgroundSelectionsAndResolvedBrightnessOwnTheirCapturedState()
        {
            var declaration = Painted(0, 1, "painted horizon", "distant.tga", .65f);
            BattleSurfaceOverride[] source = { declaration };
            var appearance = new BattleSceneAppearance(Profile(), source);
            source[0] = Painted(1, 0, "real ground", "ground.tga", .2f);
            Assert.That(appearance.Surfaces[0], Is.EqualTo(declaration));
            Assert.Throws<NotSupportedException>(() => ((IList<BattleSurfaceOverride>)appearance.Surfaces)[0] = source[0]);
            PolFile model = Model();
            Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(model);
            Func<int, int, PolSurfaceAppearance?> other = new BattleSceneAppearance(Profile(), source).ResolveSurfaces(model);
            PolSurfaceAppearance value = selected(0, 1).Value;
            value = PolSurfaceAppearance.Painted(2);
            Assert.That(value.Brightness, Is.EqualTo(2));
            model.NodeDescriptions[0].Name = "changed after validation";
            model.Meshes[0].Textures[1].Material.TextureFileNames[1] = "changed.tga";
            Assert.That(selected(0, 1)?.Brightness, Is.EqualTo(.65f));
            Assert.That(selected(1, 0), Is.Null);
            Assert.That(other(0, 1), Is.Null);
            Assert.That(other(1, 0)?.Brightness, Is.EqualTo(.2f));
            Assert.Throws<InvalidDataException>(() => appearance.ResolveSurfaces(model));
        }

        [Test]
        public void InvalidOrDuplicateBackgroundDeclarationsAreRejectedAtCapture()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Painted(-1, 0, "node", "texture"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Painted(0, -1, "node", "texture"));
            Assert.Throws<ArgumentException>(() => Painted(0, 0, " ", "texture"));
            Assert.Throws<ArgumentException>(() => Painted(0, 0, "node", null));
            Assert.Throws<ArgumentException>(() => new BattleSceneAppearance(Profile(), new[] { default(BattleSurfaceOverride) }));
            var one = Painted(0, 1, "node", "a.tga");
            var duplicate = Painted(0, 1, "different name", "b.tga");
            Assert.Throws<ArgumentException>(() => new BattleSceneAppearance(Profile(), new[] { one, duplicate }));
            Assert.Throws<ArgumentException>(() => new BattleSceneAppearance(default));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-.01f)]
        [TestCase(2.01f)]
        public void BackgroundBrightnessRejectsNonfiniteAndOutOfRangeValues(float value)
        {
            Assert.Catch<ArgumentException>(() => Painted(0, 1, "painted horizon", "distant.tga", value));
        }

        [TestCase(0f)]
        [TestCase(2f)]
        public void BackgroundBrightnessAcceptsEndpointsWithoutSelectingTheRealGround(float value)
        {
            var appearance = new BattleSceneAppearance(Profile(), new[] { Painted(0, 1, "painted horizon", "distant.tga", value) });
            Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(Model());
            Assert.That(selected(0, 1)?.Brightness, Is.EqualTo(value));
            Assert.That(selected(0, 0), Is.Null, "Even a zero-brightness background must not disable the real ground receiver.");
        }

        [Test]
        public void SurfaceOverridesKeepGeometricNormalGroundLitAndTheAdjacentPaintedHorizonIndependent()
        {
            BattleSurfaceOverride[] source =
            {
                new(0, 0, "painted horizon", "ground.tga", PolSurfaceAppearance.RecalculateNormals),
                Painted(0, 1, "painted horizon", "distant.tga", .7f)
            };
            var appearance = new BattleSceneAppearance(Profile(), source);
            Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(Model());
            source[0] = Painted(0, 0, "painted horizon", "ground.tga", 2);
            source[1] = new BattleSurfaceOverride(0, 1, "painted horizon", "distant.tga", PolSurfaceAppearance.RecalculateNormals);
            PolSurfaceAppearance ground = selected(0, 0).Value;
            Assert.That(ground.UnlitBackground, Is.False, "Repairing group-local normals must retain the real ground's dynamic lighting and shadow receiver.");
            Assert.That(ground.GeometricNormals, Is.True);
            Assert.That(ground.Brightness, Is.EqualTo(1));
            PolSurfaceAppearance horizon = selected(0, 1).Value;
            Assert.That(horizon.UnlitBackground, Is.True);
            Assert.That(horizon.GeometricNormals, Is.False);
            Assert.That(horizon.Brightness, Is.EqualTo(.7f));
            Assert.That(selected(1, 0), Is.Null);
            Assert.That(appearance.Surfaces[0].Appearance, Is.EqualTo(ground));
            Assert.That(appearance.Surfaces[1].Appearance, Is.EqualTo(horizon));
        }

        [Test]
        public void SurfaceOverridesRejectDuplicateCoordinatesAcrossDifferentAppearanceKinds()
        {
            var painted = Painted(0, 1, "painted horizon", "distant.tga");
            var normals = new BattleSurfaceOverride(0, 1, "painted horizon", "distant.tga", PolSurfaceAppearance.RecalculateNormals);
            Assert.Throws<ArgumentException>(() => new BattleSceneAppearance(Profile(), new[] { painted, normals }));
        }

        [TestCase("node index")]
        [TestCase("group index")]
        [TestCase("node name")]
        [TestCase("texture name")]
        [TestCase("null groups")]
        [TestCase("null filenames")]
        public void BackgroundReferencesMustMatchTheActualPolBeforeResolving(string mismatch)
        {
            PolFile model = Model();
            int node = mismatch == "node index" ? 2 : 0;
            int group = mismatch == "group index" ? 2 : 1;
            string name = mismatch == "node name" ? "Painted horizon" : "painted horizon";
            string texture = mismatch == "texture name" ? "another.tga" : "distant.tga";
            if (mismatch == "null groups")
                model.Meshes[0].Textures = null;
            if (mismatch == "null filenames")
                model.Meshes[0].Textures[1].Material.TextureFileNames = null;
            var appearance = new BattleSceneAppearance(Profile(), new[]
            {
                Painted(1, 0, "real ground", "ground.tga"),
                Painted(node, group, name, texture)
            });
            Assert.Throws<InvalidDataException>(() => appearance.ResolveSurfaces(model));
        }

        [Test]
        public void ParsedBackgroundSelectionsAreIndependentOfLaterAuthoredConfiguration()
        {
            JObject declaration = Background();
            declaration["brightness"] = .65f;
            JObject entry = Entry();
            entry["backgroundSurfaces"] = new JArray(declaration);
            BattleSceneAppearance appearance = Parse(Document(entry))["PAL3/Z01"];
            declaration["texture"] = "changed.tga";
            declaration["brightness"] = .1f;
            entry["backgroundSurfaces"] = new JArray();
            Assert.That(appearance.Surfaces.Count, Is.EqualTo(1));
            Assert.That(appearance.Surfaces[0].Appearance.Brightness, Is.EqualTo(.65f));
            Assert.That(appearance.ResolveSurfaces(Model())(0, 1)?.Brightness, Is.EqualTo(.65f));
            Assert.That(Parse(Document(entry))["PAL3/Z01"].ResolveSurfaces(Model()), Is.Null);
        }

        [Test]
        public void OmittedAuthoredBackgroundBrightnessDefaultsToOne()
        {
            JObject entry = Entry();
            entry["backgroundSurfaces"] = new JArray(Background());
            BattleSceneAppearance appearance = Parse(Document(entry))["PAL3/Z01"];
            Assert.That(appearance.Surfaces[0].Appearance.Brightness, Is.EqualTo(1));
            Assert.That(appearance.ResolveSurfaces(Model())(0, 1)?.Brightness, Is.EqualTo(1));
        }

        [TestCase("\"NaN\"")]
        [TestCase("\"Infinity\"")]
        [TestCase("\"-Infinity\"")]
        [TestCase("-0.01")]
        [TestCase("2.01")]
        public void InvalidAuthoredBackgroundBrightnessIsRejected(string value)
        {
            JObject declaration = Background();
            declaration["brightness"] = JToken.Parse(value);
            JObject entry = Entry();
            entry["backgroundSurfaces"] = new JArray(declaration);
            RejectDocument(Document(entry));
        }

        [TestCase("[null]")]
        [TestCase("[{}]")]
        [TestCase("[{\"nodeIndex\":-1,\"materialGroupIndex\":0,\"nodeName\":\"node\",\"texture\":\"x.tga\"}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":-1,\"nodeName\":\"node\",\"texture\":\"x.tga\"}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":0,\"nodeName\":\" \",\"texture\":\"x.tga\"}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":0,\"nodeName\":\"node\"}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":0,\"nodeName\":\"node\",\"texture\":\"x.tga\",\"guessByName\":true}]")]
        public void InvalidAuthoredBackgroundDeclarationsAreRejected(string json)
        {
            JObject entry = Entry();
            entry["backgroundSurfaces"] = JToken.Parse(json);
            RejectDocument(Document(entry));
        }

        [Test]
        public void DuplicateAuthoredBackgroundGroupsAreRejectedEvenWithDifferentGuards()
        {
            JObject entry = Entry();
            entry["backgroundSurfaces"] = new JArray(Background(), Background(texture: "another.tga"));
            RejectDocument(Document(entry));
        }

        [Test]
        public void ParsedGeometricNormalSurfacesPreserveTheirTypedAppearanceAndCapturedReferences()
        {
            JObject ground = Background(group: 0, texture: "ground.tga");
            JObject entry = Entry();
            entry["backgroundSurfaces"] = new JArray(Background());
            entry["geometricNormalSurfaces"] = new JArray(ground);
            BattleSceneAppearance appearance = Parse(Document(entry))["PAL3/Z01"];
            ground["nodeName"] = "changed after capture";
            entry["geometricNormalSurfaces"] = new JArray();
            Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(Model());
            Assert.That(appearance.Surfaces.Count, Is.EqualTo(2));
            Assert.That(selected(0, 0), Is.EqualTo(PolSurfaceAppearance.RecalculateNormals));
            Assert.That(selected(0, 1), Is.EqualTo(PolSurfaceAppearance.Painted()));
            Assert.That(Parse(Document(entry))["PAL3/Z01"].ResolveSurfaces(Model())(0, 0), Is.Null);
        }

        [TestCase("[null]")]
        [TestCase("[{}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":0,\"nodeName\":\"node\"}]")]
        [TestCase("[{\"nodeIndex\":0,\"materialGroupIndex\":0,\"nodeName\":\"node\",\"texture\":\"x.tga\",\"brightness\":0.5}]")]
        public void InvalidAuthoredGeometricNormalReferencesAreRejected(string value)
        {
            JObject entry = Entry();
            entry["geometricNormalSurfaces"] = JToken.Parse(value);
            RejectDocument(Document(entry));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DuplicateAuthoredSurfaceCoordinatesAreRejectedWithinAndAcrossKinds(bool paintedToo)
        {
            JObject entry = Entry();
            entry["geometricNormalSurfaces"] = paintedToo ? new JArray(Background()) : new JArray(Background(), Background());
            if (paintedToo)
                entry["backgroundSurfaces"] = new JArray(Background());
            RejectDocument(Document(entry));
        }

        private const string ProfileAssetPath = "Assets/Scripts/Editor/BattleEditor/Data/BattleToonLightingProfiles.json";

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResourceLookupMatchesSceneCaseAndUnknownScenesUseTheDefault(string variant)
        {
            TextAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ProfileAssetPath);
            Assert.That(asset, Is.Not.Null);
            Dictionary<string, BattleSceneAppearance> profiles = Parse(asset.text);
            string scene = profiles.Keys.First(key => key.StartsWith(variant + "/", StringComparison.Ordinal)).Split('/')[1];
            Assert.That(BattleToonLightingCatalog.Resolve(variant, scene.ToLowerInvariant()), Is.EqualTo(profiles[variant + "/" + scene].Lighting));
            Assert.That(BattleToonLightingCatalog.Resolve(variant, "__missing_lighting_scene__"), Is.EqualTo(ToonLightingProfile.Default));
            Assert.That(BattleToonLightingCatalog.Resolve(variant, null), Is.EqualTo(ToonLightingProfile.Default));
            Assert.That(BattleToonLightingCatalog.ResolveScene(variant, scene).Lighting, Is.EqualTo(profiles[variant + "/" + scene].Lighting));
            Assert.That(BattleToonLightingCatalog.ResolveScene(variant, "__missing_lighting_scene__").Surfaces, Is.Empty);
            RejectDocument(null);
            Assert.Catch<ArgumentException>(() => BattleToonLightingCatalog.Resolve("PAL4", scene));
        }

        [Test]
        [Category("LocalGameData")]
        public void EveryActualBattleSceneHasAnExplicitProfileAndValidatedSurfaceReferences()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            TextAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ProfileAssetPath);
            Assert.That(asset, Is.Not.Null);
            Dictionary<string, BattleSceneAppearance> profiles = Parse(asset.text);
            using var data = new BattleData(root, 936);
            foreach (string scene in data.Scenes)
            {
                Assert.That(profiles.ContainsKey(BattleData.Variant + "/" + scene), Is.True, BattleData.Variant + "/" + scene);
                BattleSceneAppearance appearance = profiles[BattleData.Variant + "/" + scene];
                PolFile model = data.Read(FileConstants.CombatSceneFolderVirtualPath + scene + "\\" + scene + ".pol", new PolFileReader());
                Func<int, int, PolSurfaceAppearance?> selected = appearance.ResolveSurfaces(model);
                for (int node = 0; node < model.Meshes.Length; node++)
                    for (int group = 0; group < model.Meshes[node].Textures.Length; group++)
                    {
                        PolSurfaceAppearance? surfaceAppearance = selected?.Invoke(node, group);
                        Assert.That(surfaceAppearance.HasValue, Is.EqualTo(appearance.Surfaces.Any(surface => surface.NodeIndex == node && surface.MaterialGroupIndex == group)),
                            $"{BattleData.Variant}/{scene} node {node}, group {group}");
                        if (surfaceAppearance.HasValue)
                            Assert.That(surfaceAppearance.Value, Is.EqualTo(appearance.Surfaces.First(surface => surface.NodeIndex == node && surface.MaterialGroupIndex == group).Appearance));
                    }
            }
            Assert.That(profiles.Keys.Count(key => key.StartsWith(BattleData.Variant + "/", StringComparison.Ordinal)), Is.EqualTo(data.Scenes.Length));
        }
    }
}
