// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private sealed class SpiritOrbRun
        {
            internal long Id;
            internal string Actor;
            internal int Element;
            internal float Acceleration;
            internal BattleSpiritRect Destination;
            internal BattleSpiritOrb Orb;
        }

        private long _nextSpiritOrb;
        private readonly List<SpiritOrbRun> _spiritOrbs = new();
        private readonly HashSet<string> _spiritCapturedActors = new(StringComparer.Ordinal);

        public IReadOnlyList<BattleSpiritOrbSnapshot> SpiritOrbs => Array.AsReadOnly(_spiritOrbs.Select(orb =>
            new BattleSpiritOrbSnapshot(orb.Id, orb.Actor, orb.Element, orb.Orb.Position, orb.Destination, orb.Acceleration, orb.Orb.Origin)).ToArray());

        public bool WasSpiritCaptured(string actorId) => actorId != null && _spiritCapturedActors.Contains(actorId);

        private bool ResolvePal3AAbilityMarker(Actor source, ActionRun run)
        {
            if (_scenario.Variant != "PAL3A" || run.Ability?.Id != BattlePal3ARules.SpiritCaptureSkill)
                return false;
            if (run.AbilityResolved)
                return true;
            if (_effects is not IBattleSpiritCaptureLayout layout)
                throw new NotSupportedException("Spirit capture needs the original screen-space wheel layout.");
            run.AbilityResolved = true;
            Actor primary = ActorAtNativeIndex(0) ?? throw new InvalidOperationException("Spirit capture requires native slot zero.");
            foreach (Actor enemy in _actors.Where(actor => NativeActorIndex(actor) is >= 5 and < 11).OrderBy(NativeActorIndex))
            {
                if (enemy.Escaped || enemy.EnemyFleeing || !enemy.Alive)
                    continue;
                // GetSpiritCount calls CanCapture a second time. Both forecasts
                // consume their own original RNG even when the second gate fails.
                bool eligible = CanPal3ASpiritCapture(enemy, primary);
                int count = eligible && CanPal3ASpiritCapture(enemy, primary)
                    ? BattlePal3ARules.CaptureCount(enemy.Definition.PhysicalAttackElementValues) : 0;
                if (!eligible)
                    continue;
                if (!IsAtHome(enemy) || count <= 0)
                {
                    QueueNumber(enemy, 0, 0, 1f, false, miss: true);
                    continue;
                }
                int emitted = 0;
                for (int element = 0; element < 5; element++)
                    for (int unit = 0; unit < enemy.Definition.PhysicalAttackElementValues.ElementAtOrDefault(element); unit++)
                    {
                        float acceleration = 3.06f;
                        if (emitted++ > 0)
                            acceleration = (float)(3.06f + (_spiritVisualRandom.Next() % 20 - 10) * (double).1f);
                        BattleSpiritRect target = layout.SpiritWheelTarget(element);
                        BattleSpiritPoint origin = layout.CaptureSpiritOrigin(enemy.Initial.InstanceId,
                            new BattleMovementPoint(enemy.Position.X, enemy.Position.Y + 80f, enemy.Position.Z));
                        _spiritOrbs.Add(new SpiritOrbRun
                        {
                            Id = ++_nextSpiritOrb,
                            Actor = enemy.Initial.InstanceId,
                            Element = element,
                            Acceleration = acceleration,
                            Destination = target,
                            Orb = new BattleSpiritOrb(origin, target, acceleration)
                        });
                        if (emitted == 1)
                        {
                            // PAL3A requests the current HP through ChangeHP. Guard can halve
                            // that request without suppressing the native flag or spirit orbs.
                            ChangeHp(enemy, -enemy.Hp, source, run.Id, "original spirit capture", ignoreInvisible: true);
                            _spiritCapturedActors.Add(enemy.Initial.InstanceId);
                        }
                    }
            }
            PaySkillCost(source, run.Ability, run.Id, 0);
            CheckResult();
            return true;
        }

        private bool CanPal3ASpiritCapture(Actor enemy, Actor primary)
        {
            if (!enemy.Alive || enemy.Escaped || enemy.Definition.Boss)
                return false;
            int forecast = BattlePal3ARules.CaptureForecast(primary.Level, EffectiveAttributes(primary)[3], EffectiveAttributes(enemy)[4],
                BattleEffectRules.PhysicalElementDifference(primary.Definition, enemy.Definition),
                RuntimeNormalAttackRange(primary) is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn, _random);
            return BattlePal3ARules.CanCapture(enemy.Hp, true, true, false, forecast);
        }

        private void AdvancePal3ASpiritOrbs()
        {
            foreach (SpiritOrbRun orb in _spiritOrbs.ToArray())
                if (orb.Orb.Advance((float)StepSeconds))
                {
                    _spiritWheel = BattlePal3ARules.Gain(_spiritWheel, orb.Element, 1);
                    _spiritOrbs.Remove(orb);
                }
        }

        private void FlushPal3ASpiritOrbs()
        {
            foreach (SpiritOrbRun orb in _spiritOrbs)
                _spiritWheel = BattlePal3ARules.Gain(_spiritWheel, orb.Element, 1);
            _spiritOrbs.Clear();
        }
    }
}
