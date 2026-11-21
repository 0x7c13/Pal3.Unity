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
    using Core.Contract.Enums;
    using Core.DataReader.Mv3;
    using Core.DataReader.Data;
    using Game.Effect.Runtime;
    using Core.Primitives;
    using Game.Rendering.Native;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleShadowTests
    {
        [Test]
        public void NativeRadiusUsesAuthoredActionUnionAndCompleteWidths()
        {
            var model = new Mv3File(0, Array.Empty<Mv3AnimationEvent>(), Array.Empty<Mv3TagNode>(), new[]
            {
                new Mv3Mesh { GameBoxBoundsMin = new GameBoxVector3(-10, -5, -20), GameBoxBoundsMax = new GameBoxVector3(15, 50, 10) },
                new Mv3Mesh { GameBoxBoundsMin = new GameBoxVector3(-20, -10, -30), GameBoxBoundsMax = new GameBoxVector3(10, 70, 30) }
            }, Array.Empty<GameBoxMaterial>());
            Assert.That(GameBoxBattleShadow.CaptureRadius(model), Is.EqualTo(35));
            var shadow = new GameBoxBattleShadow(new Vector3(-2, 4, 3), 35);
            Assert.That(shadow.Corner(0), Is.EqualTo(new Vector3(-.25f, 4.025f, 4.75f)));
            Assert.That(shadow.Corner(2), Is.EqualTo(new Vector3(-3.75f, 4.025f, 1.25f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReleasedVisibilityMatrixRetainsAllyHpExceptionAndPal3ABlendThreshold(bool pal3A)
        {
            // Released Shadow.Render oracle: 160 combinations per executable,
            // including native slots 0, 4, 5, 10 and values adjacent to .5.
            int visible = 0;
            foreach (int index in new[] { 0, 4, 5, 10 })
                foreach (bool valid in new[] { false, true })
                    foreach (bool modelAlive in new[] { false, true })
                        foreach (int hp in new[] { 0, 1 })
                            foreach (float blend in new[] { 0, .44f, .49999997f, .5f, 1 })
                                if (GameBoxBattleShadow.IsVisible(pal3A, valid, modelAlive, index < 5, hp, blend))
                                    visible++;
            Assert.That(visible, Is.EqualTo(pal3A ? 20 : 50));
            Assert.That(GameBoxBattleShadow.IsVisible(pal3A, true, false, true, 1, 1), Is.True);
            Assert.That(GameBoxBattleShadow.IsVisible(pal3A, true, false, false, 1, 1), Is.False);
            Assert.That(GameBoxBattleShadow.IsVisible(pal3A, true, true, false, 0, .44f), Is.EqualTo(!pal3A));
            Assert.That(GameBoxBattleShadow.IsVisible(pal3A, true, true, false, 0, .5f), Is.True);
        }

        [TestCase(false, false, 7310, -1)]
        [TestCase(false, true, 101, -1)]
        [TestCase(true, false, 7310, 1)]
        [TestCase(true, false, 7311, -1)]
        [TestCase(true, true, 101, 1)]
        public void PublishedActorFlushGroupsBracketShadows(bool pal3A, bool ally, int actorId, int expected)
        {
            Assert.That(GameBoxBattleRenderOrder.Actor(pal3A, ally, actorId), Is.EqualTo(expected));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ShippedBlendEquationsRenderOnGpu(string variant)
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            using var preview = new ResourceModelPreview(null, false);
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, new UnityEngine.Color(.25f, .5f, .75f, .25f));
            texture.Apply();
            try
            {
                using var shadow = new GameBoxBattleShadowRenderer(variant, texture);
                shadow.Begin();
                shadow.Add(new GameBoxBattleShadow(Vector3.zero, 60));
                shadow.End();
                preview.Rendering += camera => shadow.Render(camera, preview.Root.layer);
                preview.SetCamera(new Vector3(0, 5, -1), Vector3.zero, 40);
                Texture2D image = Read(preview.Render(128, 128));
                try
                {
                    UnityEngine.Color actual = image.GetPixel(64, 64);
                    UnityEngine.Color background = new(.12f, .13f, .15f);
                    UnityEngine.Color sample = texture.GetPixel(0, 0);
                    UnityEngine.Color expected = variant == "PAL3" ? background * sample : sample * (1 - sample.a) + background * sample.a;
                    Assert.That(actual.r, Is.EqualTo(expected.r).Within(.015f));
                    Assert.That(actual.g, Is.EqualTo(expected.g).Within(.015f));
                    Assert.That(actual.b, Is.EqualTo(expected.b).Within(.015f));
                }
                finally { Object.DestroyImmediate(image); }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [TestCase(-1, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void TransparentSortingCanBracketTheNativeShadow(int order, bool after)
        {
            using var preview = new ResourceModelPreview(null, false);
            var shadowTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var bodyTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            shadowTexture.SetPixel(0, 0, new UnityEngine.Color(.5f, .5f, .5f, 1));
            bodyTexture.SetPixel(0, 0, new UnityEngine.Color(1, 0, 0, .5f));
            shadowTexture.Apply();
            bodyTexture.Apply();
            var bodyMaterial = new Material(Shader.Find("Pal3/Effect")) { mainTexture = bodyTexture };
            bodyMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            bodyMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-3, .1f, -3), new Vector3(3, .1f, -3), new Vector3(3, .1f, 3), new Vector3(-3, .1f, 3) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                colors = Enumerable.Repeat(UnityEngine.Color.white, 4).ToArray(),
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            try
            {
                GameObject body = preview.CreateObject("Transparent role-order fixture");
                body.AddComponent<MeshFilter>().sharedMesh = mesh;
                var bodyRenderer = body.AddComponent<MeshRenderer>();
                bodyRenderer.sharedMaterial = bodyMaterial;
                bodyRenderer.sortingOrder = order;
                using var shadow = new GameBoxBattleShadowRenderer("PAL3", shadowTexture);
                shadow.Begin();
                shadow.Add(new GameBoxBattleShadow(Vector3.zero, 60));
                shadow.End();
                preview.Rendering += camera => shadow.Render(camera, preview.Root.layer);
                preview.SetCamera(new Vector3(0, 5, -1), Vector3.zero, 40);
                Texture2D image = Read(preview.Render(128, 128));
                try
                {
                    float background = .12f, bodyAlpha = bodyTexture.GetPixel(0, 0).a, shade = shadowTexture.GetPixel(0, 0).r;
                    float expected = after ? background * shade * (1 - bodyAlpha) + bodyAlpha :
                        (background * (1 - bodyAlpha) + bodyAlpha) * shade;
                    Assert.That(image.GetPixel(64, 64).r, Is.EqualTo(expected).Within(.015f));
                }
                finally { Object.DestroyImmediate(image); }
            }
            finally
            {
                Object.DestroyImmediate(bodyMaterial);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(bodyTexture);
                Object.DestroyImmediate(shadowTexture);
            }
        }

        [TestCase(false)]
        [Category("LocalGameData")]
        public void RealBattleShadowKeepsCapturedRadiusAcrossActionsAndFollowsCurrentHeight(bool toon)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var data = new BattleData(root, 936);
            int ally = data.Database.CombatActorInfos.Keys.First(id => id > 0 && data.Database.CombatActorInfos[id].Type == CombatActorType.MainActor);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint, ToonRendering = toon };
            BattleParticipant actor = data.CreateParticipant(ally, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants.Add(actor);
            int enemy = data.Database.CombatActorInfos.Keys.First(id => id > 0 &&
                data.Database.CombatActorInfos[id].Type != CombatActorType.MainActor &&
                !string.IsNullOrEmpty(data.Database.CombatActorInfos[id].ModelId) &&
                data.Resources.Find(Core.Contract.Constants.FileConstants.GetActorFolderVirtualPath(data.Database.CombatActorInfos[id].ModelId) +
                    data.Database.CombatActorInfos[id].ModelId + ".ini") != null);
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
#if PAL3A
            scenario.Participants.Add(data.CreateParticipant(7310, BattleSide.Enemy, BattleSlot.Fire));
#endif
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            var radii = (Dictionary<string, float>)typeof(BattlePreview).GetField("_shadowRadii", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
            float initial = radii[actor.InstanceId];
            string attack = data.AttackAction(actor);
            preview.PlayAction(actor.InstanceId, attack);
            Assert.That(radii[actor.InstanceId], Is.EqualTo(initial));
            var renderer = (GameBoxBattleShadowRenderer)typeof(BattlePreview).GetField("_shadows", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
            Assert.That(renderer.Count, Is.EqualTo(scenario.Participants.Count));
            Transform body = preview.View.Root.transform.Find(actor.InstanceId);
            body.position += new Vector3(1, 2, 3);
            typeof(BattlePreview).GetMethod("SampleShadows", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(preview, null);
            var mesh = (Mesh)typeof(GameBoxBattleShadowRenderer).GetField("_mesh", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
            Assert.That(mesh.vertices.Take(4).All(vertex => Mathf.Abs(vertex.y - (body.position.y + .025f)) < .00001f), Is.True);
            Assert.That(mesh.vertices.Take(4).Average(vertex => vertex.x), Is.EqualTo(body.position.x).Within(.00001f));
            Assert.That(preview.View.Dependencies["basedata.cpk\\shadow.tga"], Is.True);
            preview.Sample(.2f);
            preview.View.SetCamera(body.position + new Vector3(3, 7, -7), body.position, 40);
            Texture2D image = Read(preview.View.Render(800, 600));
            try
            {
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(30));
                foreach (BattleParticipant participant in scenario.Participants)
                {
                    int expected = BattleData.Variant == "PAL3A" && (participant.Side == BattleSide.Ally || participant.ActorId == 7310) ? 1 : -1;
                    Transform actorRoot = preview.View.Root.transform.Find(participant.InstanceId);
                    Assert.That(actorRoot.GetComponentsInChildren<UnityEngine.Renderer>(true).All(part => part.sortingOrder == expected), Is.True,
                        $"Original body/equipment flush group for {participant.ActorId}");
                }
                renderer.Begin();
                renderer.End();
                Texture2D without = Read(preview.View.Render(800, 600));
                try
                {
                    UnityEngine.Color32[] withPixels = image.GetPixels32();
                    UnityEngine.Color32[] withoutPixels = without.GetPixels32();
                    int changed = withPixels.Where((pixel, index) => Math.Abs(pixel.r - withoutPixels[index].r) > 1 ||
                        Math.Abs(pixel.g - withoutPixels[index].g) > 1 || Math.Abs(pixel.b - withoutPixels[index].b) > 1).Count();
                    Assert.That(changed, Is.GreaterThan(20), "The original texture shadow must visibly affect the actual battle floor.");
                }
                finally { Object.DestroyImmediate(without); }
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_SHADOW_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + (toon ? "-toon" : "-original") + "-shadows.png"), image.EncodeToPNG());
                }
            }
            finally { Object.DestroyImmediate(image); }
        }

        [TestCase(null)]
#if PAL3
        [TestCase("Z06")]
#endif
#if PAL3A
        [TestCase("M06C")]
        [TestCase("m06d")]
        [TestCase("M10A")]
        [TestCase("M10B")]
        [TestCase("M15A")]
#endif
        [Category("LocalGameData")]
        public void ToonBattleUsesModelShadowCastersAndKeepsNativeBlobAndQueuesOut(string scene)
        {
            using var data = new BattleData(Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT"), 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = scene ?? data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                ToonRendering = true,
                Music = "NONE"
            };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id));
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            preview.Audio.Muted = preview.Effects.Muted = true;
            Assert.That(typeof(BattlePreview).GetField("_shadows", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview), Is.Null);
            Assert.That(preview.View.Dependencies.ContainsKey("basedata.cpk\\shadow.tga"), Is.False);
            MeshRenderer[] bodies = scenario.Participants.SelectMany(actor => preview.View.Root.transform.Find(actor.InstanceId).GetComponentsInChildren<MeshRenderer>()).ToArray();
            Assert.That(bodies, Is.Not.Empty);
            Assert.That(bodies.All(body => body.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On), Is.True);
            Assert.That(bodies.SelectMany(body => body.sharedMaterials).Where(material => material.shader.name is "Pal3/ToonOpaque" or "Pal3/ToonCutout")
                .All(material => material.renderQueue <= 2500), Is.True, "Native post-shadow queues must not remove Toon bodies from opaque render passes.");
            Light light = preview.View.Root.GetComponentInChildren<Light>();
            Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft));
            Bounds allBodies = bodies[0].bounds;
            foreach (MeshRenderer body in bodies.Skip(1))
                allBodies.Encapsulate(body.bounds);
            float distance = Math.Max(12, allBodies.extents.magnitude * 2.8f);
            // Look across the key's ground projection so received shadows are
            // visible beside bodies rather than hidden behind their bounds.
            preview.View.SetCamera(allBodies.center + new Vector3(14, 12, 8).normalized * distance, allBodies.center, 50);
            Texture2D with = Read(preview.View.Render(800, 600));
            var camera = (Camera)typeof(ResourceModelPreview).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview.View);
            Rect[] bodyPixels = bodies.Select(body =>
            {
                Bounds bounds = body.bounds;
                var points = Enumerable.Range(0, 8).Select(corner => camera.WorldToViewportPoint(new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z))).ToArray();
                Assert.That(points.All(point => point.z > camera.nearClipPlane), Is.True, "The caster exclusion bounds must remain in front of the test camera.");
                return Rect.MinMaxRect(points.Min(point => point.x) * with.width - 2, points.Min(point => point.y) * with.height - 2,
                    points.Max(point => point.x) * with.width + 2, points.Max(point => point.y) * with.height + 2);
            }).ToArray();
            foreach (MeshRenderer body in bodies)
                body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Texture2D without = Read(preview.View.Render(800, 600));
            Renderer[] environment = preview.View.Root.GetComponentsInChildren<Renderer>().Where(renderer => !bodies.Contains(renderer)).ToArray();
            bool[] visible = environment.Select(renderer => renderer.enabled).ToArray();
            UnityEngine.Color background = camera.backgroundColor;
            Texture2D silhouette;
            try
            {
                foreach (Renderer renderer in environment)
                    renderer.enabled = false;
                camera.backgroundColor = UnityEngine.Color.clear;
                silhouette = Read(preview.View.Render(800, 600));
            }
            finally
            {
                for (int index = 0; index < environment.Length; index++)
                    environment[index].enabled = visible[index];
                camera.backgroundColor = background;
            }
            try
            {
                UnityEngine.Color32[] first = with.GetPixels32(), second = without.GetPixels32();
                UnityEngine.Color32[] maskPixels = silhouette.GetPixels32();
                Assert.That(maskPixels.Count(pixel => pixel.a == 0), Is.GreaterThan(maskPixels.Length / 2), "A body-only render must preserve its transparent background.");
                Assert.That(maskPixels.Count(pixel => pixel.a > 16), Is.GreaterThan(100));
                var bodyMask = new bool[maskPixels.Length];
                for (int index = 0; index < maskPixels.Length; index++)
                    if (maskPixels[index].a > 16)
                        for (int y = Math.Max(0, index / with.width - 1); y <= Math.Min(with.height - 1, index / with.width + 1); y++)
                            for (int x = Math.Max(0, index % with.width - 1); x <= Math.Min(with.width - 1, index % with.width + 1); x++)
                                bodyMask[y * with.width + x] = true;
                int changed = first.Where((pixel, index) => !bodyMask[index] &&
                    pixel.r + pixel.g + pixel.b + 5 < second[index].r + second[index].g + second[index].b).Count();
                SaveLightingImage(with, scenario.Scene + "-toon-real-shadows");
                SaveLightingImage(without, scenario.Scene + "-toon-no-body-shadows");
                SaveLightingImage(silhouette, scenario.Scene + "-body-mask");
                string diagnostics = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(diagnostics))
                    File.WriteAllText(Path.Combine(diagnostics, "ToonLighting", BattleData.Variant + "-" + scenario.Scene + "-shadow-mask.json"),
                        Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            receivedShadowPixels = changed,
                            excludedBodyRects = bodyPixels.Select(rect => new[] { rect.x, rect.y, rect.width, rect.height }).ToArray()
                        }));
                Assert.That(changed, Is.GreaterThan(20), "Disabling model casters must remove received shadows outside the rendered body silhouette, not just self-shadowing.");
                if (scene == "Z06")
                {
                    // Reproduce the old preview's missing camera profile and
                    // unshadowed 1.2 key on the same snow, geometry and camera.
                    camera.GetComponent<Game.Rendering.Toon.ToonCameraLighting>().Detach();
                    light.intensity = 1.2f;
                    light.color = UnityEngine.Color.white;
                    light.shadows = LightShadows.None;
                    light.transform.rotation = Quaternion.Euler(45, -35, 0);
                    Texture2D legacy = Read(preview.View.Render(800, 600));
                    try
                    {
                        int Clipped(UnityEngine.Color32[] pixels) => pixels.Count(pixel => pixel.r >= 248 && pixel.g >= 248 && pixel.b >= 248);
                        int previousClipping = Clipped(legacy.GetPixels32()), currentClipping = Clipped(first);
                        Assert.That(previousClipping, Is.GreaterThan(1000), "The snow fixture must expose the old excessive lighting.");
                        Assert.That(currentClipping, Is.LessThan(previousClipping / 4), "The corrected snow must retain substantially more highlight detail.");
                        SaveLightingImage(legacy, scenario.Scene + "-legacy-lighting-config");
                        string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                        if (!string.IsNullOrEmpty(output))
                            File.WriteAllText(Path.Combine(output, "ToonLighting", BattleData.Variant + "-" + scenario.Scene + "-comparison.json"),
                                Newtonsoft.Json.JsonConvert.SerializeObject(new
                                {
                                    previousClipping,
                                    currentClipping,
                                    totalPixels = first.Length
                                }));
                    }
                    finally
                    {
                        Object.DestroyImmediate(legacy);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(with);
                Object.DestroyImmediate(without);
                Object.DestroyImmediate(silhouette);
            }
        }

        [Test]
        public void ToonPreviewAppliesItsOwnCameraProfileAndPreservesBrightSurfaceDetail()
        {
            using var view = new ResourceModelPreview(null, true);
            var camera = (Camera)typeof(ResourceModelPreview).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(plane, view.Root.scene);
            plane.transform.SetParent(view.Root.transform, false);
            MeshFilter planeMesh = plane.GetComponent<MeshFilter>();
            planeMesh.sharedMesh = Object.Instantiate(planeMesh.sharedMesh);
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new UnityEngine.Color(.8f, .8f, .8f, 1));
            texture.Apply();
            var material = new Material(Shader.Find("Pal3/ToonOpaque"));
            material.SetTexture("_MainTex", texture);
            plane.GetComponent<MeshRenderer>().sharedMaterial = material;
            Vector4 sceneParameters = default, effects = default;
            float character = 0;
            void CaptureParameters(UnityEngine.Rendering.ScriptableRenderContext context, Camera rendering)
            {
                if (rendering != camera)
                    return;
                sceneParameters = Shader.GetGlobalVector("_Pal3ToonSceneLighting");
                effects = Shader.GetGlobalVector("_Pal3ToonEffectStrengths");
                character = Shader.GetGlobalFloat("_Pal3ToonCharacterLighting");
            }
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += CaptureParameters;
            try
            {
                view.SetCamera(new Vector3(0, 5, -6), Vector3.zero, 50);
                Texture2D image = Read(view.Render(800, 600));
                try
                {
                    Assert.That(sceneParameters.z, Is.EqualTo(1));
                    Assert.That(effects.y, Is.EqualTo(.65f));
                    Assert.That(character, Is.EqualTo(.9f));
                    Assert.That(Shader.GetGlobalVector("_Pal3ToonSceneLighting"), Is.EqualTo(Vector4.zero));
                    Assert.That(Shader.GetGlobalVector("_Pal3ToonEffectStrengths"), Is.EqualTo(Vector4.zero));
                    UnityEngine.Color32[] pixels = image.GetPixels32();
                    Assert.That(pixels.Count(pixel => pixel.r > 140 && pixel.g > 140 && pixel.b > 140), Is.GreaterThan(10000));
                    Assert.That(pixels.Count(pixel => pixel.r >= 248 && pixel.g >= 248 && pixel.b >= 248), Is.LessThan(100),
                        "A textured bright surface must not be blown out by an unconfigured preview key light.");
                    SaveLightingImage(image, "bright-surface-profile");
                }
                finally
                {
                    Object.DestroyImmediate(image);
                }
            }
            finally
            {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= CaptureParameters;
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        private static void SaveLightingImage(Texture2D image, string name)
        {
            string root = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
            if (string.IsNullOrEmpty(root))
                return;
            string folder = Path.Combine(root, "ToonLighting");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, BattleData.Variant + "-" + name + ".png"), image.EncodeToPNG());
        }

        [TestCase("model")]
        [TestCase("quad")]
        [Category("LocalGameData")]
        public void ActualCombatEffectRenderersUseTheirNativeFlushGroupAndViewersKeepDefault(string kind)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var data = new BattleData(root, 936);
            using var view = new ResourceModelPreview(data.Resources, false);
            using var director = new BattleEffectDirector(data, view);
            var resources = (EffectResources)typeof(BattleEffectDirector).GetField("_resources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
            EffectDefinitionFile definitions = resources.Definitions;
            string Kind(EffectGroupObject obj) => definitions.TryGetType(obj.TypeCrc, out EffectDefinition definition) ? EffectClass.Name(definition.ClassNameCrc) : "missing";
            string[] viewerKinds = { "model", "quad", "particle", "S-Particle", "cylinder", "grid", "meteor", "shock", "explosive", "audio", "callback" };
            int group = Enumerable.Range(0, definitions.Groups.Length).First(index => definitions.Groups[index].Objects.Any(obj => Kind(obj) == kind) &&
                definitions.Groups[index].Objects.All(obj => viewerKinds.Contains(Kind(obj))) && resources.Diagnose(index).Length == 0);
            long handle = director.Start(new BattleEffectRequest(group, "source", Array.Empty<string>(), default, 1));
            EffectPlayer combat = view.Root.GetComponentInChildren<EffectPlayer>();
            UnityEngine.Renderer[] renderers = Enumerable.Range(0, definitions.Groups[group].Objects.Length)
                .Where(index => Kind(definitions.Groups[group].Objects[index]) == kind)
                .SelectMany(index => combat.transform.GetChild(index).GetComponentsInChildren<UnityEngine.Renderer>(true)).ToArray();
            Assert.That(renderers, Is.Not.Empty);
            int expected = kind == "model" ? BattleData.Variant == "PAL3A" ? 1 : -1 : 2;
            Assert.That(renderers.All(renderer => renderer.sortingOrder == expected), Is.True);
            director.Stop(handle);
            GameObject viewerRoot = view.CreateObject("Ordinary effect viewer");
            var viewer = viewerRoot.AddComponent<EffectPlayer>();
            viewer.AutoAdvance = false;
            viewer.Initialize(resources, group);
            Assert.That(viewer.Error, Is.Null);
            renderers = viewer.GetComponentsInChildren<UnityEngine.Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Assert.That(renderers.All(renderer => renderer.sortingOrder == 0), Is.True);
            Object.DestroyImmediate(viewerRoot);
        }

#if PAL3A
        [TestCase(101, "107")]
        [TestCase(103, "106")]
        [Category("LocalGameData")]
        public void InitialBeastFormCapturesItsOwnIdleBounds(int actorId, string beast)
        {
            string root = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var data = new BattleData(root, 936);
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint, ToonRendering = false };
            BattleParticipant actor = data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants.Add(actor);
            scenario.Participants.Add(data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water));
            scenario.Pal3A.NangongStartsTransformed = actorId == 101;
            scenario.Pal3A.PengxuStartsTransformed = actorId == 103;
            BattleParticipant transformed = actor.Copy();
            transformed.Model = beast;
            Mv3File idle = data.Read(data.ActionPath(transformed, data.IdleAction(transformed)), new Mv3FileReader());
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            var radii = (Dictionary<string, float>)typeof(BattlePreview).GetField("_shadowRadii", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview);
            Assert.That(radii[actor.InstanceId], Is.EqualTo(GameBoxBattleShadow.CaptureRadius(idle)));
        }
#endif

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
            finally { RenderTexture.active = previous; }
        }
    }
}
