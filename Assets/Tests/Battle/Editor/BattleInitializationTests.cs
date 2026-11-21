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

    public sealed class BattleInitializationTests
    {
        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 2, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => int.Parse(actor.Substring(1));

            public int DamageDivisor(string actor) => 1;
        }

        // Full published InitSequen functions: PAL3 0x4dbb80, PAL3A 0x4cf38b.
        // Independently executed by Battle-Stage23-InitializationReview/probe-initialization.py.
        // Native slot 0 is dead whenever present; missing slots have bValid=false.
        private static IEnumerable<TestCaseData> NativeInitialGauges()
        {
            var vectors = new[]
            {
                (BattleEncounter.Normal, new[] { 0, 1, 5 }, new[] { 1955, 2517, 2506 }, 415139642u),
                (BattleEncounter.Normal, new[] { 1, 5 }, new[] { 1955, 2517 }, 3357800067u),
                (BattleEncounter.Normal, new[] { 0, 4, 5, 10 }, new[] { 1955, 2517, 2506, 2256 }, 3884216597u),
                (BattleEncounter.EnemyAmbush, new[] { 0, 1, 5 }, new[] { 0, 0, 11500 }, 1u),
                (BattleEncounter.EnemyAmbush, new[] { 1, 5 }, new[] { 0, 11500 }, 1u),
                (BattleEncounter.EnemyAmbush, new[] { 0, 4, 5, 10 }, new[] { 0, 0, 11500, 11500 }, 1u),
                (BattleEncounter.AllyAmbush, new[] { 0, 1, 5 }, new[] { 11500, 11500, 0 }, 1u),
                (BattleEncounter.AllyAmbush, new[] { 1, 5 }, new[] { 11500, 0 }, 1u),
                (BattleEncounter.AllyAmbush, new[] { 0, 4, 5, 10 }, new[] { 11500, 11500, 0, 0 }, 1u)
            };
            foreach (string variant in new[] { "PAL3", "PAL3A" })
                foreach (bool captured in new[] { false, true })
                    foreach (var vector in vectors)
                        yield return new TestCaseData(variant, captured, vector.Item1, vector.Item2, vector.Item3, vector.Item4);
        }

        [TestCaseSource(nameof(NativeInitialGauges))]
        public void DeadParticipantsReceiveInitialGaugesAndMissingSlotsDoNotConsumeRandomness(string variant, bool captured,
            BattleEncounter encounter, int[] nativeSlots, int[] expectedGauges, uint finalSeed)
        {
            var scenario = new BattleScenario
            {
                Variant = variant,
                Scene = "native-initialization",
                DataFingerprint = "native-initialization-fixture",
                Seed = 1,
                Encounter = encounter
            };
            // Authoring order differs from the original identity / enemy-slot order.
            foreach (int slot in nativeSlots.Reverse())
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = "N" + slot,
                    ActorId = slot < 5 ? 101 + slot : 701,
                    Side = slot < 5 ? BattleSide.Ally : BattleSide.Enemy,
                    Slot = (BattleSlot)(slot < 5 ? slot : slot - 5),
                    Attributes = new[] { 100, 50, 30, 40, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { slot == 0 ? 0 : 100, 0, 30 }
                });
            BattleDefinitions definitions = captured ? new BattleDefinitions(variant, scenario.DataFingerprint,
                Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? int.Parse(actor.InstanceId.Substring(1)) : -1
                }))) : null;
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(session.Actors.Count, Is.EqualTo(nativeSlots.Length));
            for (int index = 0; index < nativeSlots.Length; index++)
            {
                int slot = nativeSlots[index];
                BattleActorSnapshot actor = session.Actors.Single(actor => actor.Id == "N" + slot);
                Assert.That(actor.Gauge, Is.EqualTo(expectedGauges[index]), "native slot " + slot);
                Assert.That(actor.Ready, Is.EqualTo(slot != 0 && expectedGauges[index] >= BattleSession.FullGauge));
                Assert.That(actor.Rounds, Is.Zero);
            }
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Started).RandomState, Is.EqualTo(finalSeed));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Ready && entry.Source == "N0"), Is.False);
        }
    }
}
