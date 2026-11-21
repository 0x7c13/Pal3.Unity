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
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;
    using NUnit.Framework;

    [Category("LocalGameData")]
    public sealed class BattleEquipmentIntegrationTests
    {
        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private static BattleData Load()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            return new BattleData(root, 936);
        }

        [Test]
        public void CombatRosterRejectsOppositeSidesAndNonstandardMainFormsWithoutRestrictingPreviewCapture()
        {
            using BattleData data = Load();
            foreach (var entry in data.Database.CombatActorInfos.Where(entry => entry.Key > 0))
            {
                bool main = entry.Value.Type == CombatActorType.MainActor;
                bool standard = BattleData.PlayerIdentity(entry.Key) >= 0;
                Assert.That(data.CombatRosterDenial(entry.Key, BattleSide.Ally) == null, Is.EqualTo(main && standard), "ally " + entry.Key);
                Assert.That(data.CombatRosterDenial(entry.Key, BattleSide.Enemy) == null, Is.EqualTo(!main), "enemy " + entry.Key);
            }
            BattleScenario scenario = Setup(data, 0, 0, 0).CreateInstance();
            Assert.That(data.Validate(scenario, forBattle: true), Is.Empty);
            scenario.Participants = scenario.Participants.Select(actor => data.CreateParticipant(actor.ActorId,
                actor.Side == BattleSide.Ally ? BattleSide.Enemy : BattleSide.Ally, actor.Slot)).ToList();
            Assert.That(data.Validate(scenario), Is.Empty, "Action previews retain their existing roster freedom.");
            Assert.DoesNotThrow(() => data.Capture(scenario));
            string[] reasons = data.Validate(scenario, forBattle: true).ToArray();
            foreach (BattleParticipant actor in scenario.Participants)
                Assert.That(reasons.Any(reason => reason.Contains(actor.ActorId.ToString()) && reason.Contains(data.Database.CombatActorInfos[actor.ActorId].Name)), Is.True);
            Assert.Throws<NotSupportedException>(() => new BattleAnimationCatalog(data, data.Capture(scenario)));
        }

        private static BattleSetup Setup(BattleData data, int identity, int item, int slot)
        {
            int actorId = ActorConstants.MainActorCombatActorIdMap.Single(pair => (int)pair.Key == identity).Value;
            BattleParticipant player = data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            player.AttributeSource = BattleAttributeSource.BaseWithEquipment;
            player.Attributes = new[] { 100, 20, 30, 40, 50, 6, 7, 8, 9, 10, 11, 12 };
            player.Vitals = new[] { 100, 20, 30 };
            player.Equipment = new int[6];
            player.Equipment[0] = BattleLoadoutRules.DefaultWeapon(BattleData.Variant, identity);
            player.UsesNativeDefaultWeapon = true;
            player.MagicSword = BattleData.Variant == "PAL3" && identity == 0 ? new BattleMagicSword() : null;
            if (item != 0)
            {
                player.Equipment[slot] = item;
                if (slot == 0)
                {
                    player.UsesNativeDefaultWeapon = false;
                    player.MagicSword = BattleData.Variant == "PAL3" && item == 6603 ? new BattleMagicSword() : null;
                }
            }
            return new BattleSetup(new BattleScenario
            {
                Variant = BattleData.Variant,
                DataFingerprint = data.Fingerprint,
                Scene = data.Scenes.First(),
                Seed = 12345,
                Participants = { player, data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water) }
            });
        }

        private static BattleSession Start(BattleData data, BattleSetup setup) =>
            new(setup, new Catalog(), definitions: BattleDefinitionCompiler.Build(data, setup));

        [Test]
        public void EveryLegalStockEquipmentAndPlayerCombinationCompilesInBothOrnamentSlots()
        {
            using BattleData data = Load();
            int canonicalPairs = 0;
            foreach (GameItemInfo item in data.Database.GameItemInfos.Values.Where(item => (int)item.Type is >= 7 and <= 11))
            {
                int slot = (int)item.Type switch
                {
                    11 => 0,
                    8 => 1,
                    7 => 2,
                    9 => 3,
                    _ => 4
                };
                foreach (PlayerActorId identity in item.ApplicableActors)
                {
                    canonicalPairs++;
                    foreach (int position in slot == 4 ? new[] { 4, 5 } : new[] { slot })
                    {
                        BattleSetup setup = Setup(data, (int)identity, (int)item.Id, position);
                        BattleActorDefinition actor = BattleDefinitionCompiler.Build(data, setup).Actors.Values.Single(actor => actor.Identity == (int)identity);
                        string context = $"item={item.Id}, identity={identity}, slot={position}";
                        Assert.That(actor.UnsupportedReason, Is.Empty, context);
                        Assert.That(actor.Equipment.Select(equipment => equipment.UnsupportedReason), Is.All.Empty, context);
                    }
                }
            }
            Assert.That(canonicalPairs, Is.EqualTo(BattleData.Variant == "PAL3" ? 438 : 442));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void NonweaponPoisonResistanceUsesEveryNonzeroSignedWordAndIgnoresFlags(int slot)
        {
            using BattleData data = Load();
            int type = slot switch
            {
                1 => 8,
                2 => 7,
                3 => 9,
                _ => 10
            };
            GameItemInfo original = data.Database.GameItemInfos.Values.First(item => (int)item.Type == type && item.ApplicableActors.Contains((PlayerActorId)0));
            GameItemInfo changed = original;
            changed.CombatStates = new Dictionary<ActorCombatStateType, ItemCombatStateValue>
            {
                [ActorCombatStateType.Invisible] = new() { Flags = 1, Value = 15 }
            };
            BattleSetup setup = Setup(data, 0, (int)original.Id, slot);
            uint? seed = null;
            try
            {
                foreach (byte flags in new byte[] { 0, 1, 255 })
                    foreach (short value in new short[] { 0, 1, 2, 15, -1, short.MinValue, short.MaxValue })
                    {
                        changed.CombatStates[ActorCombatStateType.PoisonResist] = new ItemCombatStateValue { Flags = flags, Value = value };
                        data.Database.GameItemInfos[(int)original.Id] = changed;
                        BattleSession session = Start(data, setup);
                        BattleActorSnapshot player = session.Actors.Single(actor => actor.Side == BattleSide.Ally);
                        Assert.That(player.States.Select(state => state.State),
                            Is.EquivalentTo(value == 0 ? Array.Empty<BattleState>() : new[] { BattleState.PoisonResist }), $"flags={flags}, word={value}");
                        if (value != 0)
                            Assert.That(player.States.Single().Permanent, Is.True);
                        uint current = session.Events.Single(entry => entry.Kind == BattleEventKind.Started).RandomState;
                        seed ??= current;
                        Assert.That(current, Is.EqualTo(seed), $"flags={flags}, word={value}");
                        Assert.That(data.Database.GameItemInfos[(int)original.Id].CombatStates[ActorCombatStateType.PoisonResist].Value, Is.EqualTo(value));
                    }
            }
            finally
            {
                data.Database.GameItemInfos[(int)original.Id] = original;
            }
        }

#if PAL3A
        [TestCase(8144, 1, 4)]
        [TestCase(8144, 1, 5)]
        [TestCase(8144, 2, 4)]
        [TestCase(8144, 2, 5)]
        [TestCase(8146, 0, 4)]
        [TestCase(8146, 0, 5)]
        [TestCase(8146, 3, 4)]
        [TestCase(8146, 3, 5)]
        [TestCase(8146, 4, 4)]
        [TestCase(8146, 4, 5)]
        public void StockOrnamentsPreserveAttributesWithoutInventingPassiveStates(int item, int identity, int slot)
        {
            using BattleData data = Load();
            BattleSession baseline = Start(data, Setup(data, identity, 0, slot));
            BattleSession session = Start(data, Setup(data, identity, item, slot));
            BattleActorSnapshot player = session.Actors.Single(actor => actor.Side == BattleSide.Ally);
            BattleActorSnapshot before = baseline.Actors.Single(actor => actor.Side == BattleSide.Ally);
            int[] expected = before.Attributes.ToArray();
            expected[4] += item == 8144 ? 8 : 5;
            expected[item == 8144 ? 11 : 10]++;
            Assert.That(player.Attributes, Is.EqualTo(expected));
            Assert.That(player.States, Is.Empty);
            Assert.That(session.Definitions.Actors[player.Id].Equipment.Single(equipment => equipment.Slot == slot).States, Is.Empty);
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(baseline.Events.Last().RandomState));
            Assert.That(data.Database.GameItemInfos[item].CombatStates.Values.Any(state => state.Value != 0), Is.True);
        }
#endif
    }
}
