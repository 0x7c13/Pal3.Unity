// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Core.Contract.Enums;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleHitFeedbackRulesTests
    {
        [Test]
        public void SimpleAtlasStartsAfterTheCreationPassAndHasOneSixteenFrameLoop()
        {
            Assert.That(BattleHitFlashSnapshot.FrameAt(-1), Is.EqualTo(-1));
            Assert.That(BattleHitFlashSnapshot.FrameAt(0), Is.EqualTo(1));
            Assert.That(BattleHitFlashSnapshot.FrameAt(14), Is.EqualTo(15));
            Assert.That(BattleHitFlashSnapshot.FrameAt(15), Is.EqualTo(16));
        }

        [Test]
        public void PublishedRedSparkIntegratesGravityBeforeMovementAndExpiresOnLuminosity()
        {
            var random = new BattleRandom(1);
            var spark = new BattleWeaponSpark(new BattleMovementPoint(12, 40, -7), random);
            // MSVC rand seed 1: 41%10 = 1, (18467%5760)/16 = 74 degrees.
            Assert.That(spark.Velocity.X, Is.EqualTo(MathF.Sin(74 * MathF.PI / 180)).Within(.000001));
            Assert.That(spark.Velocity.Z, Is.EqualTo(MathF.Cos(74 * MathF.PI / 180)).Within(.000001));
            spark.Advance(.02f);
            Assert.That(spark.Velocity.Y, Is.EqualTo(-10.4f).Within(.00001));
            Assert.That(spark.Position.Y, Is.EqualTo(39.792f).Within(.00001));
            Assert.That(spark.Alpha, Is.EqualTo(195));
            for (int tick = 1; tick < 40; tick++)
                spark.Advance(.02f);
            Assert.That(spark.Alive, Is.False);
            Assert.That(random.State, Is.EqualTo(3357800067u), "Sparks use exactly two presentation random draws.");
        }
    }

    [Category("LocalGameData")]
    public sealed class BattleHitFeedbackTests
    {
        private static object Actor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))[id];

        private static void Block(BattleSession session, string id, bool dodge = false) =>
            typeof(BattleSession).GetMethod("PushBodyBlock", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new[] { Actor(session, id), (object)dodge });

        private static BattleEditorSession Editor(bool ziXuan = false, bool toon = false)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Original local data unavailable.");
            var editor = new BattleEditorSession();
            try
            {
                editor.Load(root, 936);
                var scenario = new BattleScenario
                {
                    Variant = BattleData.Variant,
                    Scene = editor.Data.Scenes.First(),
                    DataFingerprint = editor.Data.Fingerprint,
                    Encounter = BattleEncounter.EnemyAmbush,
                    MotionBlur = false,
                    ToonRendering = toon
                };
                BattleParticipant ally = editor.Data.CreateParticipant(ziXuan ? 107 : 101, BattleSide.Ally, BattleSlot.Water);
#if PAL3
                if (ziXuan)
                    ally.Equipment[0] = (int)editor.Data.Database.GameItemInfos.Values.First(item => item.Type == ItemType.Weapon &&
                        (int)item.WeaponType == 3 && item.ApplicableActors.Contains(PlayerActorId.ZiXuan)).Id;
#endif
                int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
                scenario.Participants.Add(ally);
                scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
                editor.StartBattle(scenario);
                editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
                return editor;
            }
            catch
            {
                editor.Dispose();
                throw;
            }
        }

        private static void HideOrdinaryMeshes(BattlePreview preview)
        {
            foreach (Renderer renderer in preview.View.Root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static Texture2D Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                var image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ModalMovieHidesFrozenPostCompositeHitFlashesAndRestoresThemAfterward(bool toon, bool trail)
        {
            using BattleEditorSession editor = Editor(toon: toon);
            BattleActorSnapshot enemy = editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Enemy);
            Block(editor.Battle, enemy.Id);
            editor.StepBattle();
            BattleHitFlashSnapshot flash = editor.Battle.HitFlashes.Single();
            Vector3 center = BattleAnimationCatalog.UnityPosition(flash.Position);
            editor.Preview.View.SetCamera(center + Vector3.up * 20, center, 35);
            editor.Preview.Trail.Enabled = trail;
            HideOrdinaryMeshes(editor.Preview);
            const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var movies = (BattleMoviePlayer)typeof(BattleEffectDirector).GetField("_movies", members).GetValue(editor.Preview.Effects);
            string directory = Path.Combine(Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT") ?? Application.temporaryCachePath,
                "ModalHitFeedback-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            object movie = null;
            try
            {
                // A tiny decoded black movie exercises the actual movie mesh,
                // modal owner and post-composite pass without an external codec.
                File.WriteAllBytes(Path.Combine(directory, "video.rgba"), new byte[2 * 2 * 4 * 30]);
                Type decodedType = typeof(BattleMoviePlayer).GetNestedType("DecodedMovie", BindingFlags.NonPublic);
                object decoded = Activator.CreateInstance(decodedType, true);
                foreach (var pair in new[] { ("Width", 2), ("Height", 2), ("Frames", 30) })
                    decodedType.GetField(pair.Item1, members).SetValue(decoded, pair.Item2);
                decodedType.GetField("Directory", members).SetValue(decoded, directory);
                decodedType.GetField("Rate", members).SetValue(decoded, 30u);
                decodedType.GetField("Divisor", members).SetValue(decoded, 1u);
                movie = Activator.CreateInstance(typeof(BattleMoviePlayer).GetNestedType("Movie", BindingFlags.NonPublic), members, null,
                    new[] { (object)movies, decoded }, null);
                ((IList)typeof(BattleMoviePlayer).GetField("_movies", members).GetValue(movies)).Add(movie);
                long simulationTick = editor.Battle.SimulationTick;
                editor.StepBattle();
                Assert.That(editor.Battle.IsModalPresentation, Is.True);
                Assert.That(editor.Battle.SimulationTick, Is.EqualTo(simulationTick));
                Assert.That(editor.Battle.HitFlashes.Single().Frame, Is.EqualTo(flash.Frame));
                HideOrdinaryMeshes(editor.Preview);
                Texture2D during = Read(editor.Preview.View.Render(256, 256));
                try
                {
                    Assert.That(during.GetPixels().Any(pixel => pixel.r > .1f || pixel.g > .1f || pixel.b > .1f), Is.False,
                        "The black modal movie must cover the frozen world's post-composite hit flash.");
                }
                finally
                {
                    Object.DestroyImmediate(during);
                }
                movies.Advance(2);
                editor.SyncBattle();
                Assert.That(editor.Battle.IsModalPresentation, Is.False);
                Assert.That(editor.Battle.HitFlashes.Single().Frame, Is.EqualTo(flash.Frame));
                HideOrdinaryMeshes(editor.Preview);
                Texture2D resumed = Read(editor.Preview.View.Render(256, 256));
                try
                {
                    Assert.That(resumed.GetPixels().Any(pixel => pixel.r > .35f && pixel.g > .35f), Is.True,
                        "Modal drawing must not consume or permanently discard the frozen hit flash.");
                }
                finally
                {
                    Object.DestroyImmediate(resumed);
                }
            }
            finally
            {
                (movie as IDisposable)?.Dispose();
                Directory.Delete(directory, true);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealEnemyBlockDrawsItsOriginalAtlasOnTheFlatNativeQuadAndExpires(bool trail)
        {
            using BattleEditorSession editor = Editor();
            BattleActorSnapshot enemy = editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Enemy);
            Block(editor.Battle, enemy.Id);
            editor.StepBattle();
            BattleHitFlashSnapshot flash = editor.Battle.HitFlashes.Single();
            Assert.That(flash.Position.X, Is.EqualTo(enemy.Position.X));
            Assert.That(flash.Position.Y, Is.EqualTo(20));
            Assert.That(flash.Position.Z, Is.EqualTo(enemy.Position.Z));
            Assert.That(flash.Frame, Is.EqualTo(1));
            Vector3 center = BattleAnimationCatalog.UnityPosition(flash.Position);
            editor.Preview.View.SetCamera(center + Vector3.up * 20, center, 35);
            editor.Preview.Trail.Enabled = trail;
            HideOrdinaryMeshes(editor.Preview);
            Texture2D image = Read(editor.Preview.View.Render(256, 256));
            try
            {
                MeshFilter mesh = editor.Preview.View.Root.GetComponentsInChildren<MeshFilter>().Single(filter => filter.name == "Original monster hit flashes");
                Assert.That(mesh.sharedMesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.sharedMesh.vertices.All(point => Math.Abs(point.y - 1) < .00001), Is.True, "Native Simple is an XZ plane, not a camera billboard.");
                Assert.That(mesh.sharedMesh.bounds.size.x, Is.EqualTo(10).Within(.00001));
                Assert.That(mesh.sharedMesh.bounds.size.z, Is.EqualTo(10).Within(.00001));
                Assert.That(mesh.sharedMesh.uv, Is.EqualTo(new[]
                {
                    new Vector2(.25f, 0), new Vector2(.5f, 0), new Vector2(.5f, .25f), new Vector2(.25f, .25f)
                }), "Native frame 1 uses Set256UV's first raw row and preserves vec0(-X,+Z) through vec3(-X,-Z).");
                Assert.That(image.GetPixels().Any(pixel => pixel.r > .35f && pixel.g > .35f), Is.True, "The real atlas must reach the rendered image.");
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-hit-flash-trail-" + trail + ".png"), image.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
            for (int index = 0; index < 15; index++)
                editor.StepBattle();
            Assert.That(editor.Battle.HitFlashes, Is.Empty);
            HideOrdinaryMeshes(editor.Preview);
            Texture2D expired = Read(editor.Preview.View.Render(256, 256));
            try
            {
                Assert.That(expired.GetPixels().Any(pixel => pixel.r > .35f && pixel.g > .35f), Is.False,
                    "The post-trail flash must leave no stale atlas cell in scene history.");
            }
            finally
            {
                Object.DestroyImmediate(expired);
            }
            Assert.That(editor.Preview.View.Root.GetComponentsInChildren<MeshRenderer>().Single(renderer => renderer.name == "Original monster hit flashes").enabled, Is.False);
        }

        [Test]
        public void PartyAndDodgeBlocksDoNotProduceTheEnemySimpleEffect()
        {
            using BattleEditorSession editor = Editor();
            Block(editor.Battle, editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Ally).Id);
            editor.StepBattle();
            Assert.That(editor.Battle.HitFlashes, Is.Empty);
            Block(editor.Battle, editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Enemy).Id, true);
            editor.StepBattle();
            Assert.That(editor.Battle.HitFlashes, Is.Empty);
        }

        [Test]
        public void AllSixteenSimpleCellsAndTheirAsymmetricCornersReachTheGpuWithoutRowOrZFlips()
        {
            using BattleEditorSession editor = Editor();
            BattleActorSnapshot enemy = editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Enemy);
            Block(editor.Battle, enemy.Id);
            editor.StepBattle();
            BattleMovementPoint origin = editor.Battle.HitFlashes.Single().Position;
            Vector3 center = BattleAnimationCatalog.UnityPosition(origin);
            editor.Preview.View.SetCamera(center + Vector3.up * 20, center, 35);
            editor.Preview.Trail.Enabled = false;
            HideOrdinaryMeshes(editor.Preview);
            editor.Preview.View.Render(256, 256);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo frames = typeof(BattlePreview).GetField("_hitFeedbackFrames", flags);
            var material = (Material)typeof(BattlePreview).GetField("_hitFlashMaterial", flags).GetValue(editor.Preview);
            Texture original = material.mainTexture;
            var atlas = new Texture2D(32, 32, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            Color Ink(int frame, int corner) => new((frame + 1) / 20f, (corner + 1) / 10f, .05f, 1);
            for (int frame = 0; frame < 16; frame++)
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        atlas.SetPixel(frame % 4 * 8 + x, frame / 4 * 8 + y, Ink(frame, (x >= 4 ? 1 : 0) + (y >= 4 ? 2 : 0)));
            atlas.Apply();
            material.mainTexture = atlas;
            try
            {
                frames.SetValue(editor.Preview, Array.Empty<BattleHitFlashSnapshot>());
                Texture2D background = Read(editor.Preview.View.Render(256, 256));
                try
                {
                    ConstructorInfo constructor = typeof(BattleHitFlashSnapshot).GetConstructors(flags).Single();
                    for (int frame = 0; frame < 16; frame++)
                    {
                        var snapshot = (BattleHitFlashSnapshot)constructor.Invoke(new object[] { 1L, enemy.Id, origin, frame });
                        frames.SetValue(editor.Preview, new[] { snapshot });
                        Texture2D actual = Read(editor.Preview.View.Render(256, 256));
                        try
                        {
                            for (int corner = 0; corner < 4; corner++)
                            {
                                var point = new BattleMovementPoint(origin.X + (corner % 2 == 0 ? -50 : 50), origin.Y,
                                    origin.Z + (corner < 2 ? 50 : -50));
                                Assert.That(editor.Preview.View.Project(BattleAnimationCatalog.UnityPosition(point), new Rect(0, 0, 256, 256), out Vector2 screen), Is.True);
                                int x = Mathf.Clamp((int)screen.x, 0, 255), y = Mathf.Clamp(255 - (int)screen.y, 0, 255);
                                Color clear = background.GetPixel(x, y);
                                Color expected = QualitySettings.activeColorSpace == ColorSpace.Linear ? (clear.linear + Ink(frame, corner)).gamma : clear + Ink(frame, corner);
                                Color found = actual.GetPixel(x, y);
                                Assert.That(found.r, Is.EqualTo(Mathf.Clamp01(expected.r)).Within(.035f), $"cell {frame}, native XZ corner {corner}, R");
                                Assert.That(found.g, Is.EqualTo(Mathf.Clamp01(expected.g)).Within(.035f), $"cell {frame}, native XZ corner {corner}, G");
                                Assert.That(found.b, Is.EqualTo(Mathf.Clamp01(expected.b)).Within(.035f), $"cell {frame}, native XZ corner {corner}, B");
                            }
                        }
                        finally
                        {
                            Object.DestroyImmediate(actual);
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(background);
                }
            }
            finally
            {
                material.mainTexture = original;
                Object.DestroyImmediate(atlas);
            }
        }

#if PAL3
        [Test]
        public void RenderWeaponSparksAfterAnEnemyJoinsAnEmptySlot()
        {
            using BattleEditorSession editor = Editor();
            int monster = editor.Battle.Actors.Single(actor => actor.Side == BattleSide.Enemy).ActorId;
            typeof(BattleSession).GetMethod("CreateOriginalMonster", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(editor.Battle, new object[] { 6, monster, 0L });
            editor.SyncBattle();
            Assert.That(editor.Battle.Actors.Count(actor => actor.Side == BattleSide.Enemy), Is.EqualTo(2));
            editor.Preview.View.LimitToDisplayRefreshRate = false;
            Assert.That(editor.Preview.View.Render(256, 256), Is.Not.Null);
        }

        [Test]
        public void ZiXuanNativeWeaponEmitsRedSparksOncePerDisplayedFrameAndRepeatedRepaintIsStable()
        {
            using BattleEditorSession editor = Editor(true);
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor.Battle);
            uint randomState = random.State;
            for (int frame = 0; frame < 8; frame++)
            {
                editor.StepBattle();
                editor.Preview.View.Render(256, 256);
            }
            Assert.That(editor.Preview.WeaponSparkCount, Is.GreaterThan(0));
            int count = editor.Preview.WeaponSparkCount;
            var sparkMesh = editor.Preview.View.Root.GetComponentsInChildren<MeshFilter>().Single(filter => filter.name == "Original weapon red sparks");
            Vector3[] vertices = sparkMesh.sharedMesh.vertices;
            editor.Preview.View.Render(256, 256);
            Assert.That(editor.Preview.WeaponSparkCount, Is.EqualTo(count));
            Assert.That(sparkMesh.sharedMesh.vertices, Is.EqualTo(vertices));
            Assert.That(random.State, Is.EqualTo(randomState), "The visual stream must not consume combat CUtil RNG.");
        }
#endif
    }
}
