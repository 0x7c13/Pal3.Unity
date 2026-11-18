// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using Battle;
    using Core.Contract.Enums;
    using Core.DataReader.Mv3;
    using Core.DataReader.Data;
    using Engine.Extensions;
    using Game.Rendering.Renderer;
    using Game.Effect.Runtime;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattlePreviewTests
    {
        private BattleEditorSession _session;

        [SetUp]
        public void LoadData()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            _session = new BattleEditorSession();
            _session.Load(root, 936);
        }

        [TearDown]
        public void Dispose() => _session?.Dispose();

        private BattleScenario CreateScenario()
        {
            BattleData data = _session.Data;
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint };
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
            {
                var info = data.Database.CombatActorInfos.OrderBy(pair => pair.Key).First(pair => pair.Key > 0 &&
                    (pair.Value.Type == CombatActorType.MainActor) == (side == BattleSide.Ally) &&
                    data.Resources.Find(Core.Contract.Constants.FileConstants.GetActorFolderVirtualPath(pair.Value.Type == CombatActorType.MainActor ? pair.Key.ToString() : pair.Value.ModelId) +
                        (pair.Value.Type == CombatActorType.MainActor ? pair.Key.ToString() : pair.Value.ModelId) + ".ini") != null);
                scenario.Participants.Add(data.CreateParticipant(info.Key, side, BattleSlot.Water));
            }
            BattleParticipant duplicate = scenario.Participants[1].Copy();
            duplicate.InstanceId = Guid.NewGuid().ToString("N");
            duplicate.Slot = BattleSlot.Fire;
            scenario.Participants.Add(duplicate);
            return scenario;
        }

        [TestCase(800, 800, false)]
        [TestCase(800, 800, true)]
        [TestCase(800, 600, false)]
        [TestCase(800, 600, true)]
        [TestCase(1600, 900, false)]
        [TestCase(1600, 900, true)]
        public void CustomCameraKeepsItsProjectionThroughBattleBindingAndCaptureUntilNativeListTakesOver(int width, int height, bool expand)
        {
            BattleScenario scenario = CreateScenario();
            scenario.Camera = new BattleCamera
            {
                Override = true,
                Position = new BattlePoint { X = 0, Y = 10, Z = -20 },
                Target = new BattlePoint { X = 0, Y = 0, Z = 0 },
                FieldOfView = 60
            };
            var viewport = new Rect(0, 0, width, height);
            var world = new Vector3(2, 1, 1);
            void ConfigureViewport()
            {
                _session.Preview.View.ExpandReferenceFraming = expand;
                _session.Preview.View.SetViewportContext(viewport);
            }
            Vector2 Project()
            {
                ConfigureViewport();
                Assert.That(_session.Preview.View.Project(world, viewport, out Vector2 point), Is.True);
                return point;
            }
            void AssertCustomProjection(Vector2 expected)
            {
                Assert.That(Vector2.Distance(Project(), expected), Is.LessThan(.003));
                _session.Preview.View.GetCamera(out _, out _, out float vertical);
                Assert.That(vertical, Is.EqualTo(60).Within(.0001));
            }

            _session.Start(scenario);
            Vector2 authored = Project();
            _session.StartBattle(scenario);
            AssertCustomProjection(authored);
            Assert.That(_session.Preview.Camera.StartList(-1), Is.False);
            _session.Preview.Camera.Advance(BattleSession.StepSeconds, BattleSession.StepSeconds);
            AssertCustomProjection(authored);

            // Exercise the same GetCamera -> BattleCamera fields used by the
            // authoring Save Current View action, without opening its window.
            _session.Preview.View.GetCamera(out Vector3 position, out Vector3 target, out float fieldOfView);
            scenario.Camera = new BattleCamera
            {
                Override = true,
                Position = new BattlePoint { X = position.x, Y = position.y, Z = position.z },
                Target = new BattlePoint { X = target.x, Y = target.y, Z = target.z },
                FieldOfView = fieldOfView
            };
            _session.StartBattle(scenario);
            AssertCustomProjection(authored);

            Assert.That(_session.Preview.Camera.StartList(0), Is.True);
            _session.Preview.Camera.Advance(BattleSession.StepSeconds, BattleSession.StepSeconds);
            ConfigureViewport();
            float nativeHorizontal = _session.Preview.Camera.Controller.FieldOfView;
            float aspect = width / (float)height;
            float nativeVertical = Camera.HorizontalToVerticalFieldOfView(nativeHorizontal, expand ? Math.Min(aspect, 4f / 3) : aspect);
            _session.Preview.View.GetCamera(out _, out _, out float actualVertical);
            Assert.That(_session.Preview.View.HorizontalFieldOfView, Is.EqualTo(nativeHorizontal));
            Assert.That(actualVertical, Is.EqualTo(nativeVertical).Within(.0001));

            _session.Preview.Camera.StopList();
            _session.Preview.Camera.Advance(BattleSession.StepSeconds, BattleSession.StepSeconds);
            ConfigureViewport();
            _session.Preview.View.GetCamera(out _, out _, out actualVertical);
            Assert.That(actualVertical, Is.EqualTo(nativeVertical).Within(.0001),
                "Stopping a native list must not restore the old authoring projection.");
        }

        [Test]
        public void LoadsOnlyBaseDataAndRejectsInvalidStartWithoutLosingCurrentPreview()
        {
            BattleScenario scenario = CreateScenario();
            Assert.That(_session.Data.Resources.Entries.All(entry => entry.Path.StartsWith("basedata.cpk\\", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(_session.Data.Validate(scenario), Is.Empty);
            _session.Start(scenario);
            BattlePreview previous = _session.Preview;
            scenario.Participants[0].ActorId = int.MaxValue;
            Assert.Throws<ArgumentException>(() => _session.Start(scenario));
            Assert.That(_session.Preview, Is.SameAs(previous));
            Assert.Throws<DirectoryNotFoundException>(() => _session.Load(Path.Combine(_session.Data.Resources.Root, "missing-" + Guid.NewGuid().ToString("N")), 936));
            Assert.That(_session.Preview, Is.SameAs(previous));
            scenario = CreateScenario();
            scenario.DataFingerprint = "other revision";
            Assert.That(_session.Data.Validate(scenario).Any(error => error.Contains("fingerprint")), Is.True);
        }

        [Test]
        public void EffectInitializationFailureReleasesPartialOwnersAndKeepsTheCurrentPreview()
        {
            BattleScenario scenario = CreateScenario();
            _session.Start(scenario);
            BattlePreview previous = _session.Preview;
            int scenes = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            int audioOwners = Resources.FindObjectsOfTypeAll<GameObject>().Count(value => value.name == "Battle audio preview");
            var archives = (IDictionary)_session.Data.Resources.FileSystem.GetType()
                .GetField("_cpkArchives", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_session.Data.Resources.FileSystem);
            var mounted = new System.Collections.Generic.List<DictionaryEntry>();
            foreach (DictionaryEntry archive in archives)
                mounted.Add(archive);
            archives.Clear(); // Simulate a dependency disappearing after database capture, without editing original files.
            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    Assert.Throws<FileNotFoundException>(() => _session.Start(scenario));
                    Assert.That(_session.Preview, Is.SameAs(previous));
                    Assert.That(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount, Is.EqualTo(scenes));
                    Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Count(value => value.name == "Battle audio preview"), Is.EqualTo(audioOwners));
                }
            }
            finally
            {
                foreach (DictionaryEntry archive in mounted)
                    archives.Add(archive.Key, archive.Value);
            }
            previous.Sample(.5f);
        }

        [TestCase("POL")]
        [TestCase("CVD")]
        public void BattleCompositionSuppliesSceneLightingToModelEffects(string kind)
        {
            BattleScenario scenario = CreateScenario();
            scenario.ToonRendering = false;
            _session.Start(scenario);
            BattlePreview preview = _session.Preview;
            var resources = (EffectResources)typeof(BattleEffectDirector)
                .GetField("_resources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview.Effects);
            EffectDefinition definition = resources.Definitions.EffectDefinitions.First(value => value.IsValid &&
                EffectClass.Name(value.ClassNameCrc) == "model" && value.TextureName.EndsWith("." + kind, StringComparison.OrdinalIgnoreCase));
            resources.Definitions.Groups[0] = new EffectGroup
            {
                Objects = new[]
                {
                    new EffectGroupObject { TypeCrc = definition.TypeNameCrc, Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10 } } }
                }
            };
            preview.Effects.Start(new BattleEffectRequest(0, "model probe", Array.Empty<string>(), default, 1));
            preview.Effects.Advance(1, .02f, .02f);
            preview.View.Render(64, 64);
            MeshRenderer[] renderers = preview.View.Root.transform.Find("Battle effect 0").GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers, Is.Not.Empty, definition.TextureName);
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
                .All(material => material.GetFloat("_GbNativeLighting") == 1), Is.True,
                "The BattlePreview composition root must pass its native scene-lighting owner to original model effects.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedStartSampleAndDisposeAreIsolatedAndRender(bool toon)
        {
            BattleScenario scenario = CreateScenario();
            scenario.ToonRendering = toon;
            string original = JsonUtility.ToJson(scenario);
            int actorId = scenario.Participants[0].ActorId;
            int originalHp = _session.Data.Database.CombatActorInfos[actorId].AttributeValues[ActorAttributeType.Hp];
            int scenes = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            for (int i = 0; i < 20; i++)
            {
                _session.Start(scenario);
                _session.Preview.Sample(0.7f);
                Assert.That(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount, Is.EqualTo(scenes + 1));
                BattleScenario copy = _session.CapturedScenario;
                copy.Participants[0].Attributes[0] = 999999;
                Assert.That(_session.CapturedScenario.Participants[0].Attributes[0], Is.Not.EqualTo(999999));
                if (i == 0)
                    Capture(_session.Preview, toon);
                _session.Stop();
                Assert.That(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount, Is.EqualTo(scenes));
            }
            Assert.That(JsonUtility.ToJson(scenario), Is.EqualTo(original));
            Assert.That(_session.Data.Database.CombatActorInfos[actorId].AttributeValues[ActorAttributeType.Hp], Is.EqualTo(originalHp));
        }

        [Test]
        public void RejectsOtherGameDataInBothDirections()
        {
            string current = _session.Data.Resources.Root;
            string other = Path.Combine(Directory.GetParent(current).FullName, BattleData.Variant == "PAL3" ? "PAL3A" : "PAL3");
            if (!Directory.Exists(other))
                Assert.Ignore("Other game's data is not available.");
            BattleData original = _session.Data;
            Assert.Throws<InvalidDataException>(() => _session.Load(other, 936));
            Assert.That(_session.Data, Is.SameAs(original));
        }

        [Test]
        public void PartialModelFailureKeepsPreviousPoseAndDoesNotGrowNativeResources()
        {
            BattleScenario scenario = CreateScenario();
            _session.Start(scenario);
            BattleParticipant actor = scenario.Participants[0];
            string path = _session.Data.ActionPath(actor, _session.Data.IdleAction(actor));
            Mv3File good = _session.Data.Read(path, new Mv3FileReader());
            Mv3Mesh invalid = good.Meshes[0];
            invalid.Attributes = invalid.Attributes.ToArray();
            invalid.Attributes[0].MaterialId = int.MaxValue;
            var bad = new Mv3File(good.TotalGameBoxTicks, good.AnimationEvents, Array.Empty<Mv3TagNode>(), new[] { good.Meshes[0], invalid }, good.Materials);
            var view = _session.Preview.View;
            GameObject target = view.CreateObject("Failure recovery probe");
            var renderer = view.SetVertexModel(target, path, good);
            int children = target.GetComponentsInChildren<Transform>().Length;
            Assert.Throws<IndexOutOfRangeException>(() => view.SetVertexModel(target, path, bad));
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            int materials = Resources.FindObjectsOfTypeAll<Material>().Length;
            int textures = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            for (int i = 0; i < 20; i++)
            {
                Assert.Throws<IndexOutOfRangeException>(() => view.SetVertexModel(target, path, bad));
                Assert.That(target.GetComponentsInChildren<Transform>().Length, Is.EqualTo(children));
                Assert.That(target.GetComponentInChildren<Game.Rendering.Renderer.Mv3ModelRenderer>(), Is.SameAs(renderer));
                renderer.SampleAnimation(0.1f);
            }
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.EqualTo(meshes));
            Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length, Is.EqualTo(materials));
            Assert.That(Resources.FindObjectsOfTypeAll<Texture2D>().Length, Is.EqualTo(textures));
            view.SetVertexModel(target, path, good).SampleAnimation(0.2f);
            Assert.That(target.GetComponentsInChildren<Transform>().Length, Is.EqualTo(children));
        }

#if PAL3A
        [TestCase(false)]
        [TestCase(true)]
        public void Combo507ReleasesOnlyItsOwnActorsOnCompletionOrStop(bool stopEarly)
        {
            BattleScenario scenario = CreateScenario();
            scenario.Participants.RemoveAll(actor => actor.Side == BattleSide.Ally);
            BattleParticipant first = _session.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            BattleParticipant second = _session.Data.CreateParticipant(103, BattleSide.Ally, BattleSlot.Fire);
            BattleParticipant third = _session.Data.CreateParticipant(104, BattleSide.Ally, BattleSlot.Wind);
            scenario.Participants.AddRange(new[] { first, second, third });
            _session.StartBattle(scenario);
            BattleSession battle = _session.Battle;
            BattleEffectDirector effects = _session.Preview.Effects;
            var request = new BattleEffectRequest(957, first.InstanceId, Array.Empty<string>(), default, 1);
            int frame = 0;
            void Advance() => effects.Advance(++frame, (float)BattleSession.StepSeconds, (float)BattleSession.StepSeconds);

            // Combo 507 invokes group 957 through group 1014. One action node
            // controls slots 3 and 0, and two other nodes control slot 2.
            long handle = effects.Start(request);
            for (int i = 0; i < 8; i++)
            {
                Advance();
            }
            Assert.That(battle.IsActorExternallyControlled(first.InstanceId), Is.True);
            Assert.That(battle.IsActorExternallyControlled(second.InstanceId), Is.True);
            Assert.That(battle.IsActorExternallyControlled(third.InstanceId), Is.True);

            long overlapping = effects.Start(request);
            for (int i = 0; i < 8; i++)
            {
                Advance();
            }
            battle.SetEffectActorControlled(first.InstanceId, true);
            battle.SetEffectActorControlled(first.InstanceId, false);
            Assert.That(battle.IsActorExternallyControlled(first.InstanceId), Is.True);
            battle.SetEffectActorControlled(second.InstanceId, true);

            if (stopEarly)
            {
                effects.Stop(handle);
            }
            else
            {
                while (effects.IsRunning(handle) && frame < 1000)
                {
                    Advance();
                }
            }
            Assert.That(effects.IsRunning(handle), Is.False);
            Assert.That(effects.IsRunning(overlapping), Is.True);
            Assert.That(battle.IsActorExternallyControlled(third.InstanceId), Is.True);
            effects.Stop(overlapping);
            Assert.That(battle.IsActorExternallyControlled(first.InstanceId), Is.False);
            Assert.That(battle.IsActorExternallyControlled(third.InstanceId), Is.False);
            Assert.That(battle.IsActorExternallyControlled(second.InstanceId), Is.True);
            battle.SetEffectActorControlled(second.InstanceId, false);
            Assert.That(battle.IsActorExternallyControlled(second.InstanceId), Is.False);
        }
#endif

#if PAL3
        [Test]
        public void ResultSparksUseTheActiveZiXuanWeaponVisibility()
        {
            BattleData data = _session.Data;
            int actorId = Core.Contract.Constants.ActorConstants.MainActorCombatActorIdMap.Single(pair => (int)pair.Key == 3).Value;
            BattleParticipant ziXuan = data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            ziXuan.Equipment[0] = data.Database.GameItemInfos.First(pair => (int)pair.Value.WeaponType == 3 && pair.Value.ApplicableActors.Contains((PlayerActorId)3)).Key;
            BattleScenario scenario = CreateScenario();
            scenario.Participants.Clear();
            scenario.Participants.Add(ziXuan);
            BattleParticipant enemy = data.CreateParticipant(data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id)), BattleSide.Enemy, BattleSlot.Water);
            enemy.Vitals[0] = 0;
            scenario.Participants.Add(enemy);
            _session.StartBattle(scenario);
            _session.Preview.Audio.Muted = _session.Preview.Effects.Muted = true;
            for (int tick = 0; tick < 20 && _session.ResultPresentation == null; tick++)
                _session.StepBattle();
            Assert.That(_session.ResultPresentation, Is.Not.Null);
            // An ended battle retains its external attachment state. Result
            // bodies own their new action and do not inherit that override.
            _session.Battle.SetEffectActorControlled(ziXuan.InstanceId, true);
            _session.Battle.SetEffectActorWeapons(ziXuan.InstanceId, -1, -1);
            Assert.That(_session.Battle.WeaponVisibility(ziXuan.InstanceId).Right, Is.False);
            var renderer = _session.Preview.View.Root.transform.Find(ziXuan.InstanceId).GetComponentInChildren<Game.Rendering.Renderer.Mv3ModelRenderer>();
            Assert.That(renderer.RightWeaponVisible, Is.True);
            for (int frame = 0; frame < 12; frame++)
            {
                _session.StepBattle();
                _session.Preview.View.Render(256, 192);
            }
            var sparks = (IList)typeof(BattlePreview).GetField("_weaponSparks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_session.Preview);
            Assert.That(sparks.Count, Is.GreaterThan(0), "The visible result weapon must emit despite the frozen combat override.");
        }
#endif

        private BattleScenario TaggedAuthoringScenario()
        {
            BattleScenario scenario = CreateScenario();
            scenario.Participants.Clear();
            var equipped = _session.Data.Database.GameItemInfos.Where(pair => pair.Value.Type == ItemType.Weapon)
                .SelectMany(pair => pair.Value.ApplicableActors.Where(player => Core.Contract.Constants.ActorConstants.MainActorCombatActorIdMap.ContainsKey(player))
                    .Select(player => (Item: pair.Key, Player: player)));
            var tagged = equipped.First(entry =>
            {
                string path = Core.Contract.Constants.FileConstants.GetWeaponModelFileVirtualPath(_session.Data.Database.GameItemInfos[entry.Item].ModelName);
                if (_session.Data.Resources.Find(path) == null || _session.Data.Read(path, new Core.DataReader.Pol.PolFileReader()).TagNodes.Length < 2)
                    return false;
                var actor = _session.Data.CreateParticipant(Core.Contract.Constants.ActorConstants.MainActorCombatActorIdMap[entry.Player], BattleSide.Ally, BattleSlot.Water);
                return _session.Data.Read(_session.Data.ActionPath(actor, _session.Data.IdleAction(actor)), new Mv3FileReader()).TagNodes.Any(tag => tag.Name.Equals("tag_weapon1", StringComparison.OrdinalIgnoreCase));
            });
            BattleParticipant source = _session.Data.CreateParticipant(Core.Contract.Constants.ActorConstants.MainActorCombatActorIdMap[tagged.Player], BattleSide.Ally, BattleSlot.Water);
            source.Equipment[0] = tagged.Item;
            TestContext.WriteLine($"Tagged authoring actor={source.ActorId}, weapon={source.Equipment[0]}");
            scenario.Participants.Add(source);
            scenario.Participants.Add(_session.Data.CreateParticipant(_session.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_session.Data, id)), BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

        private static Dictionary<string, Vector3[]> WeaponPose(Mv3ModelRenderer renderer)
        {
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(Mv3ModelRenderer);
            var tags = (Mv3TagNode[])type.GetField("_tagNodesInfo", fields).GetValue(renderer);
            var entities = (Engine.Core.Abstraction.IGameEntity[])type.GetField("_tagNodes", fields).GetValue(renderer);
            var weapon = (Core.DataReader.Pol.PolFile)type.GetField("_tagNodePolFile", fields).GetValue(renderer);
            var result = new Dictionary<string, Vector3[]>(StringComparer.OrdinalIgnoreCase);
            if (weapon == null)
                return result;
            for (int index = 0; index < tags.Length; index++)
            {
                string name = tags[index].Name;
                if (!name.Equals("tag_weapon1", StringComparison.OrdinalIgnoreCase) && !name.Equals("tag_weapon2", StringComparison.OrdinalIgnoreCase) ||
                    entities[index]?.NativeObject is not GameObject owner)
                    continue;
                result.Add(name, weapon.TagNodes.Select(tag =>
                {
                    var matrix = tag.GameBoxTransformMatrix;
                    return owner.transform.TransformPoint(new Core.Primitives.GameBoxVector3(matrix.Tx, matrix.Ty, matrix.Tz).ToUnityPosition());
                }).ToArray());
            }
            return result;
        }

        private static bool SameWeaponPoints(Vector3[] first, Vector3[] second) => first.Length == second.Length &&
            first.Zip(second, (left, right) => Vector3.Distance(left, right) < .00001f).All(equal => equal);

        private static void AssertRetainedWeaponPoses(Mv3ModelRenderer renderer, params Dictionary<string, Vector3[]>[] allowed)
        {
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var blades = (IDictionary)typeof(Mv3ModelRenderer).GetField("_weaponBlades", fields).GetValue(renderer);
            Assert.That(blades.Keys.Cast<string>(), Is.EquivalentTo(allowed[0].Keys));
            foreach (DictionaryEntry pair in blades)
            {
                var blade = (WeaponBlade)pair.Value;
                int count = (int)typeof(WeaponBlade).GetField("_count", fields).GetValue(blade);
                var frames = (Vector3[][])typeof(WeaponBlade).GetField("_frames", fields).GetValue(blade);
                Assert.That(count, Is.GreaterThanOrEqualTo(2));
                for (int frame = 0; frame < count; frame++)
                    Assert.That(allowed.Any(pose => pose.TryGetValue((string)pair.Key, out Vector3[] expected) && SameWeaponPoints(frames[frame], expected)),
                        Is.True, $"{pair.Key} retained frame {frame} includes a pose from the previous action.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AuthoringRestartsKeepOnlyTheFinalActionInEveryRetainedBladeFrame(bool attackPreview)
        {
            BattleScenario scenario = TaggedAuthoringScenario();
            BattleParticipant source = scenario.Participants[0];
            Vector3 home = _session.Data.Position(source, scenario.SceneElement);
            scenario.Participants[1].OverridePosition = true;
            scenario.Participants[1].Position = new BattlePoint { X = home.x + 10, Y = home.y, Z = home.z + 7 };
            _session.Start(scenario);
            BattlePreview preview = _session.Preview;
            Mv3ModelRenderer Renderer() => preview.View.Root.transform.Find(source.InstanceId).GetComponentInChildren<Mv3ModelRenderer>();
            var idle = WeaponPose(Renderer());
            Assert.That(idle.ContainsKey("tag_weapon1"), Is.True);
            Dictionary<string, Vector3[]> start = null;
            if (attackPreview)
            {
                preview.PlayAttack(source.InstanceId, scenario.Participants[1].InstanceId);
                start = WeaponPose(Renderer());
            }
            else
            {
                string idleAction = _session.Data.IdleAction(source);
                foreach (var action in _session.Data.Actions(source))
                {
                    if (string.IsNullOrEmpty(action.ActionName) || action.ActionName.Equals(idleAction, StringComparison.OrdinalIgnoreCase) ||
                        !_session.Data.TryActionPath(source, action.ActionName, out _))
                        continue;
                    preview.PlayAction(source.InstanceId, action.ActionName);
                    var pose = WeaponPose(Renderer());
                    if (!pose.TryGetValue("tag_weapon1", out Vector3[] points) || SameWeaponPoints(idle["tag_weapon1"], points))
                        continue;
                    start = pose;
                    TestContext.WriteLine("Restart action: " + action.ActionName);
                    break;
                }
            }
            Assert.That(start, Is.Not.Null, "The fixture needs a genuinely different tagged action.");
            Assert.That(start.ContainsKey("tag_weapon1"), Is.True);
            Assert.That(SameWeaponPoints(idle["tag_weapon1"], start["tag_weapon1"]), Is.False,
                "The final action's t=0 endpoints must differ from the idle endpoints.");
            preview.View.Render(256, 192);
            AssertRetainedWeaponPoses(Renderer(), start);
            preview.Sample(.02f);
            var next = WeaponPose(Renderer());
            preview.View.Render(256, 192);
            AssertRetainedWeaponPoses(Renderer(), start, next);
        }

        [Test]
        public void AuthoringBladeClocksRewindAndRefreshClipChangesAtZero()
        {
            BattleScenario scenario = TaggedAuthoringScenario();
            _session.Start(scenario);
            BattlePreview preview = _session.Preview;
            string id = scenario.Participants[0].InstanceId;
            var type = typeof(Game.Rendering.Renderer.Mv3ModelRenderer);
            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
            Game.Rendering.Renderer.Mv3ModelRenderer Renderer() => preview.View.Root.transform.Find(id).GetComponentInChildren<Game.Rendering.Renderer.Mv3ModelRenderer>();
            double? Clock() => (double?)type.GetField("_lastWeaponClock", fields).GetValue(Renderer());
            preview.PlayAttack(id, scenario.Participants[1].InstanceId);
            for (int frame = 1; frame <= 10; frame++)
            {
                preview.Sample(frame * .02f);
                preview.View.Render(256, 192);
            }
            Assert.That(Clock(), Is.EqualTo((double)preview.Time));
            preview.Sample(0);
            Assert.That(Clock(), Is.Null.Or.Zero);
            var blades = (IDictionary)type.GetField("_weaponBlades", fields).GetValue(Renderer());
            foreach (Game.Rendering.Renderer.WeaponBlade blade in blades.Values)
                Assert.That((int)typeof(Game.Rendering.Renderer.WeaponBlade).GetField("_count", fields).GetValue(blade), Is.LessThanOrEqualTo(2));
            preview.View.Render(256, 192);
            // Swapping back to the equipped idle clip at the same zero time
            // must publish its current attachment pose.
            preview.PlayAction(id, _session.Data.IdleAction(scenario.Participants[0]));
            Assert.That(Clock(), Is.Zero);
            preview.View.Render(256, 192);
            Assert.That(Renderer().TryGetRightWeaponBladeTip(out Vector3 actual), Is.True);
            var tags = (Mv3TagNode[])type.GetField("_tagNodesInfo", fields).GetValue(Renderer());
            var entities = (Engine.Core.Abstraction.IGameEntity[])type.GetField("_tagNodes", fields).GetValue(Renderer());
            var weapon = (Core.DataReader.Pol.PolFile)type.GetField("_tagNodePolFile", fields).GetValue(Renderer());
            int right = Array.FindIndex(tags, tag => tag.Name.Equals("tag_weapon1", StringComparison.OrdinalIgnoreCase));
            var matrix = weapon.TagNodes[0].GameBoxTransformMatrix;
            Vector3 point = new Vector3(-matrix.Tx, matrix.Ty, matrix.Tz) / 20;
            Vector3 expected = ((GameObject)entities[right].NativeObject).transform.TransformPoint(point);
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.00001), "A t=0 clip swap must publish its new weapon pose.");
        }

        [Test]
        public void WeaponDrawingFlagsKeepTheLoadedGeometryAvailableForRestoration()
        {
            _session.Start(CreateScenario());
            var renderer = _session.Preview.View.Root.GetComponentsInChildren<Game.Rendering.Renderer.Mv3ModelRenderer>()
                .First(actor => actor.GetComponentsInChildren<Game.Rendering.Renderer.PolyModelRenderer>().Length > 0);
            MeshFilter[] meshes = renderer.GetComponentsInChildren<Game.Rendering.Renderer.PolyModelRenderer>()
                .SelectMany(weapon => weapon.GetComponentsInChildren<MeshFilter>()).ToArray();
            Assert.That(meshes, Is.Not.Empty);
            Mesh[] originals = meshes.Select(mesh => mesh.sharedMesh).ToArray();
            for (int iteration = 0; iteration < 8; iteration++)
            {
                renderer.SetWeaponVisibility(false, false);
                Assert.That(meshes.All(mesh => mesh != null && mesh.sharedMesh != null && mesh.gameObject.activeInHierarchy), Is.True);
                Assert.That(meshes.All(mesh => !mesh.GetComponent<UnityEngine.Renderer>().enabled), Is.True);
                renderer.SetWeaponVisibility(true, true);
                Assert.That(meshes.Select(mesh => mesh.sharedMesh), Is.EqualTo(originals));
                Assert.That(meshes.All(mesh => mesh.GetComponent<UnityEngine.Renderer>().enabled), Is.True);
            }
        }

#if PAL3A
        [TestCase(101, 2)]
        [TestCase(104, 10)]
        public void ThrownWeaponRendersItsOwnModelAndRetainsGeometryAfterPickup(int actorId, int weaponType)
        {
            BattleData data = _session.Data;
            BattleScenario scenario = CreateScenario();
            scenario.Encounter = BattleEncounter.AllyAmbush;
            scenario.Participants.RemoveAt(2);
            BattleParticipant source = data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            source.Equipment[0] = data.Database.GameItemInfos.First(pair => pair.Value.Type == Core.Contract.Enums.ItemType.Weapon &&
                (int)pair.Value.WeaponType == weaponType && pair.Value.ApplicableActors.Contains((PlayerActorId)(actorId - 101))).Key;
            scenario.Participants[0] = source;
            foreach (BattleParticipant participant in scenario.Participants)
            {
                participant.Attributes = new[] { 20000, 100, 100, 20, 1, 5, 0, 0, 0, 0, 0, 0 };
                participant.Vitals = new[] { 20000, 100, 100 };
                participant.Skills.Clear();
            }
            _session.StartBattle(scenario);
            for (int tick = 0; tick < 2000 && _session.Battle.WaitingActor != source.InstanceId; tick++)
                _session.StepBattle();
            Assert.That(_session.Battle.WaitingActor, Is.EqualTo(source.InstanceId));
            Assert.That(_session.Battle.Submit(source.InstanceId, BattleAction.Attack, scenario.Participants[1].InstanceId), Is.True);
            for (int tick = 0; tick < 400 && !_session.Battle.ThrownWeapons.Any(weapon => weapon.Active); tick++)
                _session.StepBattle();
            BattleThrownWeaponSnapshot projectile = _session.Battle.ThrownWeapons.Single();
            Assert.That(projectile.Active, Is.True);
            Transform model = _session.Preview.View.Root.transform.Find("Thrown weapon " + source.InstanceId);
            Assert.That(model, Is.Not.Null);
            MeshFilter[] meshes = model.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes, Is.Not.Empty);
            Assert.That(meshes.All(mesh => mesh.sharedMesh != null && mesh.GetComponent<UnityEngine.Renderer>().enabled), Is.True);
            Assert.That(Vector3.Distance(model.position, BattleAnimationCatalog.UnityPosition(projectile.Position)), Is.LessThan(.00001));
            Capture(_session.Preview, false, "-thrown-" + weaponType);
            for (int tick = 0; tick < 500 && _session.Battle.ThrownWeapons.Any(weapon => weapon.Active); tick++)
                _session.StepBattle();
            Assert.That(_session.Battle.ThrownWeapons.Any(weapon => weapon.Active), Is.False);
            Assert.That(meshes.All(mesh => mesh != null && mesh.sharedMesh != null && !mesh.GetComponent<UnityEngine.Renderer>().enabled), Is.True);
            Assert.That(_session.Battle.Status, Is.EqualTo(BattleSessionStatus.Running));
        }
#endif

        [Test]
        public void RejectingCoincidentAttackPreviewKeepsTheCurrentActionAndTimeline()
        {
            BattleScenario scenario = CreateScenario();
            BattleParticipant source = scenario.Participants[0], target = scenario.Participants[1];
            source.OverridePosition = target.OverridePosition = true;
            source.Position = target.Position = new BattlePoint();
            _session.Start(scenario);
            BattlePreview preview = _session.Preview;
            preview.PlayAction(source.InstanceId, _session.Data.AttackAction(source));
            preview.Sample(preview.Duration / 2);
            float time = preview.Time, duration = preview.Duration;

            Assert.Throws<InvalidOperationException>(() => preview.PlayAttack(source.InstanceId, target.InstanceId));
            Assert.That(preview.Time, Is.EqualTo(time));
            Assert.That(preview.Duration, Is.EqualTo(duration));
        }

        [Test]
        public void ExplicitAttackTargetsAndTimelineSamplingCanBeStoppedAndRestarted()
        {
            BattleScenario scenario = CreateScenario();
            _session.Start(scenario);
            BattlePreview preview = _session.Preview;
            string attacker = scenario.Participants[0].InstanceId;
            string target = scenario.Participants[1].InstanceId;
            Assert.Throws<InvalidOperationException>(() => preview.PlayAttack(attacker, "missing"));
            Assert.Throws<InvalidOperationException>(() => preview.PlayAttack(attacker, attacker));
            preview.PlayAttack(attacker, target);
            foreach (float fraction in new[] { 0f, 0.5f, 1f, 0.25f, 0f })
                preview.Sample(preview.Duration * fraction);
            _session.IsPlaying = true;
            _session.Tick(0.02f);
            Assert.That(preview.Time, Is.GreaterThan(0));
            scenario.Participants[0].Attributes[0] = 999999;
            _session.Restart();
            Assert.That(_session.Preview.Time, Is.Zero);
            Assert.That(_session.CapturedScenario.Participants[0].Attributes[0], Is.Not.EqualTo(999999));
            Assert.That(_session.IsPlaying, Is.False);
        }

        private static void Capture(BattlePreview preview, bool toon, string suffix = "")
        {
            RenderTexture target = preview.View.Render(800, 600);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(20), "The scene and actors must render more than a clear color.");
                string directory = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/Implementation/BattleEditor/QA"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, BattleData.Variant + (toon ? "-toon" : "-original") + suffix + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
