// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattlePal3AFormsIntegrationTests
    {
        private BattleEditorSession _session;

        [SetUp]
        public void LoadData()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original PAL3A data is required for the form presentation tests.");
            _session = new BattleEditorSession();
            _session.Load(root, 936);
        }

        [TearDown]
        public void Dispose() => _session?.Dispose();

        private BattleScenario Scenario(int actorId, int ability = 0)
        {
            BattleData data = _session.Data;
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water));
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Attributes = new[] { 50000, 100, 1000, 5000, 1, 5, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 50000, 100, 1000 };
                actor.Skills.Clear();
            }
            if (ability != 0)
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = ability, Level = 4, Mastered = true });
            return scenario;
        }

        private void FinishAction(string source)
        {
            for (int tick = 0; tick < 1500 && _session.Battle.Status == BattleSessionStatus.Running &&
                (_session.Battle.Actors.Single(actor => actor.Id == source).ActionId != 0 || _session.Battle.Pal3AForm(source).Pending); tick++)
                _session.StepBattle();
            Assert.That(_session.Battle.Status, Is.EqualTo(BattleSessionStatus.Running), _session.Battle.Result?.Reason);
            Assert.That(_session.Battle.Actors.Single(actor => actor.Id == source).ActionId, Is.Zero);
            Assert.That(_session.Battle.Pal3AForm(source).Pending, Is.False);
        }

        [Test]
        public void NangongTransformationLoadsBothActualZ20BodiesAndTheBeastGeometry()
        {
            BattleScenario scenario = Scenario(101);
            scenario.Pal3A.SpiritWheel.Charges = new[] { 3, 3, 3, 3, 3 };
            _session.StartBattle(scenario);
            string source = scenario.Participants[0].InstanceId;
            Assert.That(_session.Battle.Submit(source, BattleAction.Transform), Is.True);
            FinishAction(source);
            Assert.That(_session.Battle.Pal3AForm(source).Kind, Is.EqualTo(BattlePal3AFormKind.NangongFireBeast));
            Assert.That(_session.Battle.Pal3AState.SpiritWheel.Charges, Is.All.Zero);
            Assert.That(_session.Preview.View.Dependencies.Keys.Any(path => path.StartsWith(FileConstants.GetActorFolderVirtualPath("107"), StringComparison.OrdinalIgnoreCase)), Is.True);
            Capture("nangong-107");
            CaptureForm(source, "107", "nangong-107-inspection");
        }

        [Test]
        public void PengxuActual4308SwapsTo106AndPaysItsCapturedMaximumSp()
        {
            BattleScenario scenario = Scenario(103, 4308);
            _session.StartBattle(scenario);
            string source = scenario.Participants[0].InstanceId;
            string target = _session.Battle.GetTargetCandidates(new BattleCommand(source, BattleAction.Skill, null, 4308)).First().Id;
            var command = new BattleCommand(source, BattleAction.Skill, target, 4308);
            Assert.That(_session.Battle.Submit(command), Is.True, _session.Battle.CanSubmit(command));
            FinishAction(source);
            var catalog = new BattleAnimationCatalog(_session.Data, new BattleSetup(scenario), _session.Battle.Definitions);
            Assert.That(catalog.FormChangeAnimation(source, true), Is.Null, "The released 106 model has no z20 action.");
            catalog.ActivatePal3AForm(source, BattlePal3AFormKind.PengxuFairyBeast);
            Assert.That(catalog.Physical(source).Animation(BattlePose.Idle).Clip, Is.EqualTo("z03"));
            foreach ((string attacker, string defender) in new[] { (source, target), (target, source) })
            {
                BattleAttackPlan plan = catalog.PlanAttack(attacker, defender, catalog.InitialHome(attacker), catalog.InitialHome(defender));
                Assert.That(catalog.Approach(attacker, defender), Is.EqualTo(BattleAnimationCatalog.UnityPosition(plan.Approach)));
                AttackTiming timing = catalog.Attack(attacker, defender);
                Assert.That(timing.TravelTicks, Is.EqualTo(plan.Timing.TravelTicks));
                Assert.That(timing.HitTicks, Is.EqualTo(plan.Timing.HitTicks));
            }
            Assert.That(_session.Battle.Pal3AForm(source).Kind, Is.EqualTo(BattlePal3AFormKind.PengxuFairyBeast));
            BattleActorSnapshot actor = _session.Battle.Actors.Single(actor => actor.Id == source);
            Assert.That((actor.Hp, actor.MaxHp, actor.Sp), Is.EqualTo((400, 400, 0)));
            Assert.That(_session.Preview.View.Dependencies.Keys.Any(path => path.StartsWith(FileConstants.GetActorFolderVirtualPath("106"), StringComparison.OrdinalIgnoreCase)), Is.True);
            Capture("pengxu-106");
            CaptureForm(source, "106", "pengxu-106-inspection");
        }

        [Test]
        public void ActualCaptureProjectsOrbsAndChargesTheWheelWithoutEndingTheBattle()
        {
            BattleScenario scenario = Scenario(101, 4102);
            BattleParticipant survivor = scenario.Participants[1].Copy();
            survivor.InstanceId = Guid.NewGuid().ToString("N");
            survivor.Slot = BattleSlot.Fire;
            scenario.Participants.Add(survivor);
            scenario.Participants[1].Vitals[0] = 1;
            _session.StartBattle(scenario);
            string source = scenario.Participants[0].InstanceId;
            string target = _session.Battle.GetTargetCandidates(new BattleCommand(source, BattleAction.Skill, null, 4102)).First().Id;
            var command = new BattleCommand(source, BattleAction.Skill, target, 4102);
            Assert.That(_session.Battle.Submit(command), Is.True, _session.Battle.CanSubmit(command));
            for (int tick = 0; tick < 600 && _session.Battle.SpiritOrbs.Count == 0; tick++)
                _session.StepBattle();
            Assert.That(_session.Battle.SpiritOrbs.Count, Is.EqualTo(4));
            Assert.That(_session.Battle.WasSpiritCaptured(scenario.Participants[1].InstanceId), Is.True);
            Assert.That(_session.Battle.SpiritOrbs.All(orb => float.IsFinite(orb.Position.X) && float.IsFinite(orb.Position.Y)), Is.True);
            foreach (BattleSpiritOrbSnapshot orb in _session.Battle.SpiritOrbs)
                Assert.That(orb.Destination.Right, Is.GreaterThan(700));
            for (int tick = 0; tick < 600 && _session.Battle.SpiritOrbs.Count > 0; tick++)
                _session.StepBattle();
            Assert.That(_session.Battle.SpiritOrbs, Is.Empty);
            Assert.That(_session.Battle.Pal3AState.SpiritWheel.Charges, Is.EqualTo(new[] { 1, 2, 0, 0, 1 }));
            Assert.That(_session.Battle.Status, Is.EqualTo(BattleSessionStatus.Running), _session.Battle.Result?.Reason);
            for (int element = 0; element < 5; element++)
                Assert.That(_session.Preview.View.GetTexture($"basedata.cpk\\ui\\combat\\control\\ImbibeNimbus{element}.tga").width, Is.GreaterThan(2));
            Capture("capture-wheel");
        }

        private void CaptureForm(string source, string model, string name)
        {
            Transform actor = _session.Preview.View.Root.transform.Find(source);
            Assert.That(actor, Is.Not.Null);
            MeshFilter[] meshes = actor.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes.Length, Is.GreaterThan(0));
            Assert.That(meshes.All(mesh => mesh.sharedMesh != null && mesh.sharedMesh.vertexCount > 0), Is.True);
            Renderer[] renderers = actor.GetComponentsInChildren<Renderer>();
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
                .Any(material => material.mainTexture != null && material.mainTexture.name.Split('\\').Last().StartsWith(model + ".", StringComparison.OrdinalIgnoreCase)), Is.True,
                "The transformed actor must bind its own model texture, not merely preload the dependency.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            float distance = Mathf.Max(1, bounds.size.magnitude * 1.5f);
            try
            {
                _session.Preview.View.SetCamera(bounds.center + new Vector3(distance, distance * .3f, -distance), bounds.center, 35);
                Capture(name);
            }
            finally
            {
                _session.Preview.Camera.Publish();
            }
        }

        private void Capture(string name)
        {
            RenderTexture target = _session.Preview.View.Render(800, 600);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(20));
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (output != null)
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllBytes(Path.Combine(output, "PAL3A-" + name + ".png"), image.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
