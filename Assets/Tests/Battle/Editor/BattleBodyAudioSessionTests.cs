// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleBodyAudioSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattlePhysicalTimings, IBattleStageGeometry, IBattleBodyAudioCatalog
        {
            private readonly string _variant;

            public Catalog(string variant) => _variant = variant;

            public BattlePhysicalStageProfile Physical(string actor)
            {
                var animations = Enum.GetValues(typeof(BattlePose)).Cast<BattlePose>().ToDictionary(pose => pose,
                    pose => new BattlePhysicalAnimation(pose.ToString(), 5, 10,
                        pose == BattlePose.Attacking ? new[] { new BattlePhysicalMarker(2, BattlePhysicalMarkerKind.Work) } : null, 480));
                return new BattlePhysicalStageProfile(_variant, actor == "A" ? 101 : 701, actor == "A" ? 0 : -1, 1, 1, animations);
            }

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 5;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 5;

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => actor == "A" ? default : new(100, 0, 0);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => InitialHome(actor);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) => new(Attack(source, target), sourceHome, sourceHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from, BattleMovementPoint to, bool midpoint) => new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };

            public BattleBodySoundProfile BodySounds(string actor) => new(121, 38);
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-10, false, false);
        }

        private sealed class Audio : IBattleEffectDirector, IBattleAudio, IBattleMovementAudio
        {
            public readonly List<string> Cues = new();

            public long Start(BattleEffectRequest request) => throw new InvalidOperationException("No effect is needed by this body fixture.");

            public bool IsRunning(long handle) => false;

            public void Stop(long handle)
            {
            }

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void PlaySound(string sound) => Cues.Add(sound);

            public void PlayMovementSound(string actor, string sound, bool loop) => Cues.Add("start:" + actor + ":" + sound + ":" + loop);

            public void StopMovementSound(string actor) => Cues.Add("stop:" + actor);

            public void StopBodySounds() => Cues.Add("clear");
        }

        private static BattleSession Session(string variant, Audio audio)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "audio", Seed = 10 };
            foreach (var value in new[] { ("A", 101, BattleSide.Ally), ("E", 701, BattleSide.Enemy) })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = value.Item1,
                    ActorId = value.Item2,
                    Side = value.Item3,
                    Slot = BattleSlot.Water,
                    Attributes = new[] { 500, 100, 200, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 500, 10, 0 }
                });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    NormalAttackMode = 924
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(variant), new Rules(), definitions, effects: audio);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.Step();
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            return session;
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeBodyAudioEmitsOnceAndLeavesCombatResultsDeterministic(string variant)
        {
            var audio = new Audio();
            BattleSession audible = Session(variant, audio);
            BattleSession silent = Session(variant, null);
            for (int step = 0; step < 100; step++)
            {
                audible.Step();
                silent.Step();
            }
            Assert.That(audible.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(audio.Cues.Count(cue => cue == "wd025"), Is.EqualTo(1));
            Assert.That(audio.Cues.Count(cue => cue == "we038"), Is.EqualTo(1));
            Assert.That(audio.Cues, Does.Contain("start:A:we011:True"));
            Assert.That(audio.Cues, Does.Contain("stop:A"));
            Assert.That(audible.Actors.Select(actor => actor.Hp), Is.EqualTo(silent.Actors.Select(actor => actor.Hp)));
            Assert.That(audible.Events.Select(entry => entry.Kind), Is.EqualTo(silent.Events.Select(entry => entry.Kind)));
            audible.Abort();
            Assert.That(audio.Cues.Last(), Is.EqualTo("clear"));
        }
    }
}
