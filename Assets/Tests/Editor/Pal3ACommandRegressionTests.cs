// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using Command;
    using Command.SceCommands;
    using Contract;
    using Contract.Enums;
    using DataReader;
    using NUnit.Framework;

    public sealed class Pal3ACommandRegressionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CorrectedDefinitionsPreservePartyConditionCameraAndCombatInstructions(bool unsafeReader)
        {
            // Independent original opcode signatures; adjoining instructions used to be consumed as parameters.
            byte[] data = ProtocolTestData.Build(w =>
            {
                void Op(ushort id, ushort mask = 0)
                {
                    w.Write(id);
                    w.Write(mask);
                }
                Op(145);
                w.Write(2);
                Op(202);
                w.Write(1);
                w.Write(2);
                Op(176);
                w.Write(1);
                Op(2);
                w.Write(2);
                Op(198);
                Op(250);
                w.Write(0);
                Op(192);
                Op(195);
                w.Write((ushort)4);
                w.Write(new byte[] { 80, 53, 53, 0 });
                w.Write(705);
                for (int i = 0; i < 5; i++)
                    w.Write(0);
                Op(185);
                w.Write(3);
                w.Write(5);
                Op(186, 1);
                w.Write((ushort)0);
                Op(5);
                w.Write(0);
                Op(8, 1);
                w.Write(1);
                w.Write((ushort)0);
                Op(12);
                w.Write(1234);
                Op(33);
                w.Write(0);
                w.Write(1);
                w.Write(2.5f);
                w.Write(-30f);
                w.Write(90f);
            });
            var resolver = new SceCommandTypeResolver();
            resolver.Init();
            var parser = new SceCommandParser(resolver);
            using IBinaryReader reader = ProtocolTestData.Reader(data, unsafeReader);
            var commands = new List<ICommand>();
            var ids = new List<ushort>();
            while (reader.Position < reader.Length)
            {
                commands.Add(parser.ParseNextCommand(reader, 936, out ushort id));
                ids.Add(id);
            }
            Assert.That(ids, Is.EqualTo(new ushort[] { 145, 202, 176, 2, 198, 250, 192, 195, 185, 186, 5, 8, 12, 33 }));
            Assert.That(((PlayerSetActorLevelCommand)commands[0]).ActorId, Is.EqualTo(2));
            Assert.That(((TeamAddOrRemoveActorCommand)commands[1]).IsIn, Is.EqualTo(1));
            Assert.That(((ScriptRunnerChangeExecutionModeCommand)commands[3]).Mode, Is.EqualTo(2));
            Assert.That(((CameraFollowPlayerCommand)commands[5]).Follow, Is.Zero);
            var fight = (CombatEnterBossFightUsingMusicCommand)commands[7];
            Assert.That(fight.Monster6Id, Is.EqualTo(705));
            Assert.That(fight.CombatMusicName, Is.EqualTo("P55"));
            Assert.That(((MiniGameStartFortuneTellingCommand)commands[8]).Symbol1, Is.EqualTo(5));
            Assert.That(((MiniGameStartFortuneTellingCommand)commands[8]).Symbol2, Is.EqualTo(3));
            Assert.That(((ScriptVarSetFortuneTellingResultCommand)commands[9]).Variable, Is.Zero);
            Assert.That(((ScriptEvaluateVarIsEqualToCommand)commands[11]).Value, Is.EqualTo(1));
            var orbit = (CameraOrbitCommand)commands[13];
            Assert.That(orbit.Yaw, Is.EqualTo(90));
            Assert.That(orbit.Pitch, Is.EqualTo(-30));
            Assert.That(orbit.Duration, Is.EqualTo(2.5));
            Assert.That(orbit.CurveType, Is.EqualTo(1));
            Assert.That(orbit.Synchronous, Is.Zero);
            Assert.That(reader.Position, Is.EqualTo(data.Length));
        }

        [Test]
        public void NextBattleTransformationsAreConsumedOnceAndPersistentAbilitiesSurvive()
        {
            var state = new Pal3ACombatScriptState
            {
                NanGongHuangAbilitiesEnabled = true,
                WangPengXuTransformEnabled = true,
                NextNanGongHuangTransform = true,
                NextWangPengXuTransform = true
            };
            var first = state.BeginBattle(false);
            var second = state.BeginBattle(false);
            Assert.That(first.wolf && first.pengXu, Is.True);
            Assert.That(second.wolf || second.pengXu, Is.False);
            Assert.That(state.NanGongHuangAbilitiesEnabled && state.WangPengXuTransformEnabled, Is.True);
            state.Reset();
            Assert.That(state.Flags, Is.Zero);
        }

        [Test]
        public void LiaoRiRetainsOnlyPlayerLossesAndAppliesThemOnceToTheSecondForm()
        {
            var state = new Pal3ACombatScriptState { PreserveLosses = true };
            state.BeginBattle(false);
            state.RecordAttributeChange(0, ActorAttributeType.Hp, -30);
            state.RecordAttributeChange(4, ActorAttributeType.Mp, -12);
            state.RecordAttributeChange(0, ActorAttributeType.Hp, 20); // Healing does not undo cumulative loss.
            state.RecordAttributeChange(5, ActorAttributeType.Hp, -999); // Enemies are excluded.
            var restored = new Pal3ACombatScriptState();
            restored.Restore(state.Flags, state.LostHp, state.LostMp);
            Assert.That(restored.BeginBattle(true).hpBonus, Is.EqualTo(30));
            Assert.That(restored.LostMp, Is.EqualTo(12));
            Assert.That(restored.BeginBattle(true).hpBonus, Is.Zero);
            Assert.That(restored.LostHp, Is.Zero);
        }
        [Test]
        public void ExtremeLossesSaturateAndRestoredCountersCannotBecomeNegative()
        {
            var state = new Pal3ACombatScriptState { PreserveLosses = true };
            state.RecordAttributeChange(0, ActorAttributeType.Hp, int.MinValue);
            state.RecordAttributeChange(0, ActorAttributeType.Hp, -1);
            state.RecordAttributeChange(4, ActorAttributeType.Mp, int.MinValue);
            var bonuses = state.BeginBattle(true);
            Assert.That(bonuses.hpBonus, Is.EqualTo(int.MaxValue));
            Assert.That(bonuses.mpBonus, Is.EqualTo(int.MaxValue));
            state.Restore(16, -1, int.MinValue);
            Assert.That(state.BeginBattle(true).hpBonus, Is.Zero);
            Assert.That(state.LostMp, Is.Zero);
        }

    }
}
#endif
