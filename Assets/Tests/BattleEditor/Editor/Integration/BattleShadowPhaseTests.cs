// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using Battle;
    using Core.Contract.Enums;
    using System.Reflection;
    using Core.DataReader.Pol;
    using Core.DataReader.Mv3;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Game.Effect.Runtime;
    using Core.Primitives;
    using Game.Rendering.Native;
    using Game.Rendering.Renderer;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Color = UnityEngine.Color;

    public sealed class BattleShadowPhaseTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(.25f)]
        [TestCase(2f)]
        public void NativePostShadowOpaqueDrawExecutesAfterShadowAtEitherDepth(float nativeHeight)
        {
            using var preview = new ResourceModelPreview(null, false);
            var material = new Material(Shader.Find("Pal3/Opaque")) { mainTexture = Texture2D.whiteTexture };
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0, 0, 0, .5f));
            texture.Apply();
            Mesh mesh = Plane(nativeHeight / 20);
            try
            {
                GameObject root = preview.CreateObject("Native opaque post-shadow boundary");
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                root.AddComponent<MeshRenderer>().sharedMaterial = material;
                GameBoxBattleRenderPhase.Apply(root, 1);
                using var shadow = new GameBoxBattleShadowRenderer("PAL3A", texture);
                shadow.Begin();
                shadow.Add(new GameBoxBattleShadow(Vector3.zero, 60));
                shadow.End();
                preview.Rendering += camera => shadow.Render(camera, preview.Root.layer);
                preview.SetCamera(new Vector3(0, 5, -1), Vector3.zero, 40);
                Assert.That(Center(preview).r, Is.EqualTo(1).Within(.015f));
                Assert.That(material.renderQueue, Is.EqualTo(2000), "The pooled source material must retain its original phase.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(101)]
        [TestCase(102)]
        [TestCase(104)]
#if PAL3A
        [TestCase(105)]
#endif
        [Category("LocalGameData")]
        public void ActualIdleBodyMatchesExplicitOpaqueThenAlphaPostShadowReference(int id)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var data = new BattleData(root, 936);
            BattleParticipant participant = data.CreateParticipant(id, BattleSide.Ally, BattleSlot.Water);
            string path = data.ActionPath(participant, data.IdleAction(participant));
            Mv3File model = data.Read(path, new Mv3FileReader());
            using var preview = new ResourceModelPreview(data.Resources, false);
            GameObject body = preview.CreateObject("Actual post-shadow body " + id);
            preview.SetVertexModel(body, path, model);
            var renderers = body.GetComponentsInChildren<Renderer>(true);
            var originals = renderers.Select(renderer => renderer.sharedMaterials).ToArray();
            using var shadow = new GameBoxBattleShadowRenderer("PAL3A", preview.GetTexture("basedata.cpk\\shadow.tga"));
            shadow.Begin();
            shadow.Add(new GameBoxBattleShadow(Vector3.zero, GameBoxBattleShadow.CaptureRadius(model)));
            shadow.End();
            preview.Rendering += camera => shadow.Render(camera, preview.Root.layer);
            preview.SetCamera(new Vector3(3, 2, -5), new Vector3(0, .5f, 0), 40);
            GameBoxBattleRenderPhase.Apply(body, 1);
            Texture2D Capture()
            {
                RenderTexture target = preview.Render(256, 256);
                RenderTexture previous = RenderTexture.active;
                var image = new Texture2D(256, 256);
                try
                {
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    image.Apply();
                    return image;
                }
                finally { RenderTexture.active = previous; }
            }
            Texture2D actual = Capture();
            var copies = new List<Material>();
            Texture2D reference = null;
            try
            {
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterials = originals[i].Select(source =>
                    {
                        var copy = new Material(source) { renderQueue = source.renderQueue <= 2500 ? 3002 : 3003 };
                        copies.Add(copy);
                        return copy;
                    }).ToArray();
                reference = Capture();
                var actualPixels = actual.GetPixels32();
                var expectedPixels = reference.GetPixels32();
                int changed = actualPixels.Where((pixel, index) => Math.Abs(pixel.r - expectedPixels[index].r) > 1 ||
                    Math.Abs(pixel.g - expectedPixels[index].g) > 1 || Math.Abs(pixel.b - expectedPixels[index].b) > 1).Count();
                Assert.That(changed, Is.Zero, "The actual body must keep the independently specified opaque-before-alpha post-shadow pass.");
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_SHADOW_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-body-" + id + "-phase.png"), actual.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-body-" + id + "-reference.png"), reference.EncodeToPNG());
                }
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterials = originals[i];
                GameBoxBattleRenderPhase.Apply(body, 1);
                foreach (Material copy in copies)
                    Object.DestroyImmediate(copy);
                Object.DestroyImmediate(actual);
                Object.DestroyImmediate(reference);
            }
        }

        [Test]
        public void PhaseCopiesTrackUniformsAndMaterialReplacementWithoutChangingTheSharedViewer()
        {
            var root = new GameObject("Post-shadow source replacement");
            var viewer = new GameObject("Shared ordinary viewer");
            var source = new Material(Shader.Find("Pal3/Opaque"));
            var alpha = new Material(Shader.Find("Pal3/Transparent"));
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                viewer.AddComponent<MeshRenderer>().sharedMaterial = source;
                renderer.sharedMaterial = source;
                GameBoxBattleRenderPhase.Apply(root, 1);
                Material first = renderer.sharedMaterial;
                Assert.That(first, Is.Not.SameAs(source));
                Assert.That(first.renderQueue, Is.EqualTo(3002));
                source.SetColor("_TintColor", new Color(.2f, .3f, .4f, .6f));
                GameBoxBattleRenderPhase.Apply(root, 1);
                Assert.That(renderer.sharedMaterial.GetColor("_TintColor"), Is.EqualTo(source.GetColor("_TintColor")));
                Assert.That(viewer.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(source));
                Assert.That(source.renderQueue, Is.EqualTo(2000));
                renderer.sharedMaterial = alpha;
                GameBoxBattleRenderPhase.Apply(root, 1);
                Material second = renderer.sharedMaterial;
                Assert.That(first == null, Is.True, "A replaced material's private phase copy must be released.");
                Assert.That(second.renderQueue, Is.EqualTo(3003));
                Assert.That(second.shader, Is.SameAs(alpha.shader));
                GameBoxBattleRenderPhase.Apply(root, -1);
                Assert.That(second == null, Is.True);
                Assert.That(renderer.sharedMaterial, Is.SameAs(alpha));
                GameBoxBattleRenderPhase.Apply(root, 1);
                Material last = renderer.sharedMaterial;
                Object.DestroyImmediate(root);
                Assert.That(last == null, Is.True, "The owning object must release its private material copies.");
                Assert.That(source != null && alpha != null, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(viewer);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(alpha);
            }
        }

        [Test]
        public void PhaseSourcesAndTexturesSurviveEffectCacheTrimUntilTheirOwnerIsReleased()
        {
            var hash = new Crc32Hash();
            hash.Init();
            using var resources = new EffectResources(new CpkFileSystem(Application.temporaryCachePath, hash, 936));
            GameObject root = new("Combat phase cache lease");
            try
            {
                Material source = resources.Material("", false, false);
                root.AddComponent<MeshRenderer>().sharedMaterial = source;
                GameBoxBattleRenderPhase.Apply(root, 2);
                Material copy = root.GetComponent<MeshRenderer>().sharedMaterial;
                resources.TrimUnusedRenderAssets(new[] { root });
                Assert.That(source != null, Is.True);
                Assert.That(resources.Material("", false, false), Is.SameAs(source));
                GameBoxBattleRenderPhase.Apply(root, 2);
                Assert.That(root.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(copy));
                Object.DestroyImmediate(root);
                Assert.That(copy == null, Is.True);
                resources.TrimUnusedRenderAssets(Array.Empty<GameObject>());
                Assert.That(source == null, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NativeBladeUpdatesAgeTwiceWithoutAddingPoseSamplesAndKeepDistinctFlushSnapshots()
        {
            using var preview = new ResourceModelPreview(null, false);
            GameObject owner = preview.CreateObject("Blade pose fixture");
            using var blade = new WeaponBlade(new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = 20 }, TintColor = 0xff101010 },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -20 }, TintColor = 0xff101010 }
            });
            var material = WeaponBlade.CreateMaterial();
            try
            {
                blade.SamplePose(owner.transform, true, .12f);
                blade.Advance(.02f, 1);
                blade.Advance(.02f, 1);
                owner.transform.position = Vector3.forward;
                blade.SamplePose(owner.transform, true, .12f);
                int count = (int)typeof(WeaponBlade).GetField("_count", Members).GetValue(blade);
                Camera camera = preview.Root.scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).First(value => value != null);
                blade.Advance(.02f, 1);
                blade.RenderFlush(camera, material, owner.layer, 0, true);
                blade.Advance(.02f, 1);
                blade.RenderFlush(camera, material, owner.layer, 1, true);
                Assert.That((float)typeof(WeaponBlade).GetField("_time", Members).GetValue(blade), Is.EqualTo(.08f).Within(.000001f));
                Assert.That((int)typeof(WeaponBlade).GetField("_count", Members).GetValue(blade), Is.EqualTo(count));
                Mesh[] meshes = (Mesh[])typeof(WeaponBlade).GetField("_flushMeshes", Members).GetValue(blade);
                Assert.That(meshes[0], Is.Not.SameAs(meshes[1]));
                Assert.That(meshes[0].vertices, Is.EqualTo(meshes[1].vertices));
                Assert.That(meshes[0].colors32[0].a, Is.GreaterThan(meshes[1].colors32[0].a));
                var renderers = (MeshRenderer[])typeof(WeaponBlade).GetField("_flushRenderers", Members).GetValue(blade);
                Assert.That(renderers.Select(renderer => renderer.sortingOrder), Is.EqualTo(new[] { 0, 3 }));
                UnityEngine.Color32[] saved = meshes[0].colors32;
                blade.RenderFlush(camera, material, owner.layer, 0, false);
                Assert.That(meshes[0].colors32, Is.EqualTo(saved));
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void SecondBladeFlushCompositesAfterDirectEffectsAndFirstFlushBeforeShadow()
        {
            using var preview = new ResourceModelPreview(null, false);
            GameObject owner = preview.CreateObject("Blade GPU fixture");
            using var blade = new WeaponBlade(new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = 20 }, TintColor = 0xffcc0000 },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -20 }, TintColor = 0xffcc0000 }
            });
            var bladeMaterial = WeaponBlade.CreateMaterial();
            var shadowTexture = new Texture2D(1, 1);
            shadowTexture.SetPixel(0, 0, new Color(.5f, .5f, .5f, 1));
            shadowTexture.Apply();
            var directTexture = new Texture2D(1, 1);
            directTexture.SetPixel(0, 0, new Color(0, 0, 1, .5f));
            directTexture.Apply();
            var directMaterial = new Material(Shader.Find("Pal3/Effect")) { mainTexture = directTexture };
            directMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            directMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            Mesh plane = Plane(-.01f);
            try
            {
                GameObject effect = preview.CreateObject("Direct effect order fixture");
                effect.AddComponent<MeshFilter>().sharedMesh = plane;
                effect.AddComponent<MeshRenderer>().sharedMaterial = directMaterial;
                GameBoxBattleRenderPhase.Apply(effect, 2);
                using var shadow = new GameBoxBattleShadowRenderer("PAL3", shadowTexture);
                shadow.Begin();
                shadow.Add(new GameBoxBattleShadow(Vector3.zero, 60));
                shadow.End();
                preview.Rendering += camera => shadow.Render(camera, preview.Root.layer);
                owner.transform.position = new Vector3(0, 0, -1);
                blade.SamplePose(owner.transform, true, .12f);
                blade.Advance(.02f, 1);
                blade.Advance(.02f, 1);
                owner.transform.position = new Vector3(0, 0, 1);
                blade.SamplePose(owner.transform, true, .12f);
                Camera camera = preview.Root.scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).First(value => value != null);
                blade.Advance(.02f, 1);
                blade.RenderFlush(camera, bladeMaterial, owner.layer, 0, true);
                blade.Advance(.02f, 1);
                blade.RenderFlush(camera, bladeMaterial, owner.layer, 1, true);
                Mesh[] meshes = (Mesh[])typeof(WeaponBlade).GetField("_flushMeshes", Members).GetValue(blade);
                float first = (meshes[0].colors32[2].a + meshes[0].colors32[4].a) / 510f;
                float second = (meshes[1].colors32[2].a + meshes[1].colors32[4].a) / 510f;
                float alpha = directTexture.GetPixel(0, 0).a;
                float expected = (.12f + .8f * first) * shadowTexture.GetPixel(0, 0).r * (1 - alpha) + .8f * second;
                preview.SetCamera(new Vector3(0, 5, -.001f), Vector3.zero, 40);
                Color correct = Center(preview);
                Assert.That(correct.r, Is.EqualTo(expected).Within(.02f));
                var renderers = (MeshRenderer[])typeof(WeaponBlade).GetField("_flushRenderers", Members).GetValue(blade);
                renderers[1].sortingOrder = 1;
                Color wrong = Center(preview);
                Assert.That(correct.r - wrong.r, Is.GreaterThan(.05f), "Moving the second Flush ahead of the direct effect must change its composition.");
            }
            finally
            {
                Object.DestroyImmediate(bladeMaterial);
                Object.DestroyImmediate(shadowTexture);
                Object.DestroyImmediate(directTexture);
                Object.DestroyImmediate(directMaterial);
                Object.DestroyImmediate(plane);
            }
        }

#if PAL3
        [Test]
        [Category("LocalGameData")]
        public void PausedPresentationRepaintsReuseBothBladeSnapshotsWithoutAgingAgain()
        {
            string root = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var data = new BattleData(root, 936);
            var scenario = new BattleScenario { Variant = "PAL3", Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint };
            BattleParticipant ally = data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants.Add(ally);
            int enemy = data.Database.CombatActorInfos.First(pair => pair.Key > 0 && pair.Value.Type != CombatActorType.MainActor &&
                !string.IsNullOrEmpty(pair.Value.ModelId) && data.Resources.Find(Core.Contract.Constants.FileConstants.GetActorFolderVirtualPath(pair.Value.ModelId) + pair.Value.ModelId + ".ini") != null).Key;
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            using var preview = new BattlePreview(data, new BattleSetup(scenario));
            var actor = preview.View.Root.transform.Find(ally.InstanceId).GetComponentInChildren<Mv3ModelRenderer>();
            var blade = new WeaponBlade(new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = 20 }, TintColor = 0xff101010 },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -20 }, TintColor = 0xff101010 }
            });
            GameObject pose = preview.View.CreateObject("Cached blade pose fixture");
            blade.SamplePose(pose.transform, true, .12f);
            blade.Advance(.02f, 1);
            blade.Advance(.02f, 1);
            pose.transform.position = Vector3.forward;
            blade.SamplePose(pose.transform, true, .12f);
            var blades = (Dictionary<string, WeaponBlade>)typeof(Mv3ModelRenderer).GetField("_weaponBlades", Members).GetValue(actor);
            blades.Add("fixture", blade);
            actor.SampleWeaponBlades(0, 1, .12f);
            preview.View.Render(128, 128);
            float clock = (float)typeof(WeaponBlade).GetField("_time", Members).GetValue(blade);
            int count = (int)typeof(WeaponBlade).GetField("_count", Members).GetValue(blade);
            Mesh[] meshes = (Mesh[])typeof(WeaponBlade).GetField("_flushMeshes", Members).GetValue(blade);
            var first = meshes[0].colors32;
            var second = meshes[1].colors32;
            for (int repaint = 0; repaint < 3; repaint++)
                preview.View.Render(128, 128);
            Assert.That((float)typeof(WeaponBlade).GetField("_time", Members).GetValue(blade), Is.EqualTo(clock));
            Assert.That((int)typeof(WeaponBlade).GetField("_count", Members).GetValue(blade), Is.EqualTo(count));
            Assert.That(meshes[0].colors32, Is.EqualTo(first));
            Assert.That(meshes[1].colors32, Is.EqualTo(second));
            // Sample advances both the authoring clock and its actors. A second
            // actor sample at an earlier time would seek and dispose this trail.
            preview.Sample(.02f);
            preview.View.Render(128, 128);
            Assert.That(Blades(actor)["fixture"], Is.SameAs(blade));
            Assert.That(BladeClock(blade), Is.EqualTo(clock + .04f).Within(.000001f));
            preview.Sample(.02f);
            preview.View.Render(128, 128);
            Assert.That(BladeClock(blade), Is.EqualTo(clock + .04f).Within(.000001f));
        }
#endif

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void PendingBladeAdvanceSurvivesDuplicatePreparationAndTransferThenResetsForFreeze(bool duplicate, bool transfer)
        {
            using var preview = new ResourceModelPreview(null, false);
            GameObject owner = preview.CreateObject("Pending blade clock fixture");
            var renderer = owner.AddComponent<Mv3ModelRenderer>();
            renderer.UseRenderBladeClock = true;
            var blade = SeedBlade(owner.transform);
            Blades(renderer).Add("fixture", blade);
            try
            {
                Camera camera = preview.Root.scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).First(value => value != null);
                renderer.SampleWeaponBlades(0, 1, .12f);
                renderer.RenderPublishedWeaponBlades(camera, .02f, true, false);
                float before = BladeClock(blade);
                renderer.SampleWeaponBlades(.02, 1, .12f);
                if (duplicate)
                    renderer.SampleWeaponBlades(.02, 1, .12f);
                if (transfer)
                {
                    var replacement = preview.CreateObject("Replacement pending blade owner").AddComponent<Mv3ModelRenderer>();
                    replacement.UseRenderBladeClock = true;
                    replacement.MoveWeaponPresentationFrom(renderer);
                    renderer.Dispose();
                    renderer = replacement;
                    renderer.SampleWeaponBlades(.02, 1, .12f);
                }
                renderer.RenderPublishedWeaponBlades(camera, .02f, true, false);
                Assert.That(BladeClock(blade), Is.EqualTo(before + .04f).Within(.000001f));
                float advanced = BladeClock(blade);
                renderer.SampleWeaponBlades(.02, 1, .12f);
                renderer.SampleWeaponBlades(.02, 1, .12f);
                renderer.RenderPublishedWeaponBlades(camera, .02f, true, false);
                Assert.That(BladeClock(blade), Is.EqualTo(advanced), "The next genuinely frozen frame must consume no aging.");
                renderer.RenderPublishedWeaponBlades(camera, .02f, false, false);
                Assert.That(BladeClock(blade), Is.EqualTo(advanced));
                renderer.SampleWeaponBlades(.04, 1, .12f);
                renderer.SampleWeaponBlades(.06, 1, .12f);
                renderer.SampleWeaponBlades(.06, 1, .12f);
                renderer.RenderPublishedWeaponBlades(camera, .04f, true, false);
                Assert.That(BladeClock(blade), Is.EqualTo(advanced + .08f).Within(.000001f), "Coalesced preparation must retain its unconsumed advance.");
            }
            finally { renderer.Dispose(); }
        }

#if PAL3
        [TestCase(false)]
        [TestCase(true)]
        [Category("LocalGameData")]
        public void NormalSessionStepAndTickKeepBothNativeAgingUpdatesAcrossTheirDoubleApply(bool tick)
        {
            string root = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var session = new BattleEditorSession();
            session.Load(root, 936);
            BattleData data = session.Data;
            var scenario = new BattleScenario { Variant = "PAL3", Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint };
            BattleParticipant ally = data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants.Add(ally);
            int enemy = data.Database.CombatActorInfos.First(pair => pair.Key > 0 && pair.Value.Type != CombatActorType.MainActor &&
                !string.IsNullOrEmpty(pair.Value.ModelId) && data.Resources.Find(Core.Contract.Constants.FileConstants.GetActorFolderVirtualPath(pair.Value.ModelId) + pair.Value.ModelId + ".ini") != null).Key;
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            session.StartBattle(scenario);
            var renderer = session.Preview.View.Root.transform.Find(ally.InstanceId).GetComponentInChildren<Mv3ModelRenderer>();
            var blade = SeedBlade(renderer.transform);
            Blades(renderer).Add("fixture", blade);
            session.Preview.View.Render(128, 128);
            float before = BladeClock(blade);
            long initial = session.Battle.Actors.Single(actor => actor.Id == ally.InstanceId).NativePresentationTick;
            if (tick)
            {
                session.IsPlaying = true;
                session.Tick(.020001f);
            }
            else
                session.StepBattle();
            BattleActorSnapshot state = session.Battle.Actors.Single(actor => actor.Id == ally.InstanceId);
            Assert.That(state.NativePresentationTick, Is.GreaterThan(initial));
            session.Preview.View.Render(128, 128);
            float expected = before + .04f * state.NativePlaybackRate;
            Assert.That(BladeClock(blade), Is.EqualTo(expected).Within(.000001f));
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", Members).GetValue(session.Battle);
            uint randomState = random.State;
            long frame = session.Battle.Tick;
            for (int repaint = 0; repaint < 3; repaint++)
            {
                session.SyncBattle();
                session.Preview.View.Render(128, 128);
            }
            Assert.That(BladeClock(blade), Is.EqualTo(expected).Within(.000001f));
            Assert.That(session.Battle.Tick, Is.EqualTo(frame));
            Assert.That(random.State, Is.EqualTo(randomState));
            Assert.That(session.Battle.Actors.Single(actor => actor.Id == ally.InstanceId).NativePresentationTick, Is.EqualTo(state.NativePresentationTick));
        }
#endif

        private static Dictionary<string, WeaponBlade> Blades(Mv3ModelRenderer renderer) =>
            (Dictionary<string, WeaponBlade>)typeof(Mv3ModelRenderer).GetField("_weaponBlades", Members).GetValue(renderer);

        private static float BladeClock(WeaponBlade blade) =>
            (float)typeof(WeaponBlade).GetField("_time", Members).GetValue(blade);

        private static WeaponBlade SeedBlade(Transform pose)
        {
            var blade = new WeaponBlade(new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = 20 }, TintColor = 0xff101010 },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Tx = -20 }, TintColor = 0xff101010 }
            });
            blade.SamplePose(pose, true, .12f);
            blade.Advance(.02f, 1);
            pose.position += Vector3.forward;
            blade.SamplePose(pose, true, .12f);
            return blade;
        }

        private static Mesh Plane(float y) => new()
        {
            vertices = new[] { new Vector3(-3, y, -3), new Vector3(3, y, -3), new Vector3(3, y, 3), new Vector3(-3, y, 3) },
            triangles = new[] { 0, 2, 1, 0, 3, 2 },
            uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
            colors = Enumerable.Repeat(Color.white, 4).ToArray(),
            normals = Enumerable.Repeat(Vector3.up, 4).ToArray()
        };

        private static Color Center(ResourceModelPreview preview)
        {
            RenderTexture target = preview.Render(128, 128);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(1, 1);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(64, 64, 1, 1), 0, 0);
                image.Apply();
                return image.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
