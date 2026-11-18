// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using static BattleAiAttemptKind;
    using D = BattleAiDemandKind;

    public sealed partial class BattleAi
    {
        private readonly bool _pal3A;
        private readonly BattleRandom _random;
        private readonly BattleAiPort _port;

        public BattleAiRole[] Roles { get; } = new BattleAiRole[11];
        public BattleAiDemand[,] Demand { get; } = new BattleAiDemand[11, 10];
        public BattleAiDemand[] FriendDemand { get; } = new BattleAiDemand[10];
        public BattleAiDemand[] EnemyDemand { get; } = new BattleAiDemand[10];
        public bool[] NewRole { get; } = new bool[11];
        public IReadOnlyList<BattleAiSkill> Items { get; set; } = Array.Empty<BattleAiSkill>();

        public bool Automatic
        {
            get; private set;
        }

        public BattleAi(bool pal3A, BattleRandom random, BattleAiPort port)
        {
            _pal3A = pal3A;
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (random.Variant != (pal3A ? "PAL3A" : "PAL3"))
                throw new ArgumentException("The AI and random stream variants differ.", nameof(random));
            _port = port ?? throw new ArgumentNullException(nameof(port));
            for (int role = 0; role < 11; role++)
            {
                Roles[role] = new BattleAiRole();
                NewRole[role] = true;
                for (int kind = 0; kind < 10; kind++)
                    Demand[role, kind] = new BattleAiDemand();
            }
            for (int kind = 0; kind < 10; kind++)
            {
                FriendDemand[kind] = new BattleAiDemand();
                EnemyDemand[kind] = new BattleAiDemand();
            }
        }

        public static int HpState(int current, int maximum)
        {
            if (current == maximum)
                return 1;
            if (current >= maximum / 2 && current < maximum)
                return 2;
            if (current >= maximum / 4)
                return 3;
            return current > 0 ? 4 : 5;
        }

        public void SetAutomatic(bool enabled, bool only = false)
        {
            Automatic = enabled;
            if (!only)
            {
                for (int role = 0; role < 5; role++)
                    Roles[role].Profile.SetEnabled(enabled);
            }
        }

        public void UpdateDemands()
        {
            CheckDemands();
            TraceDemands();
        }

        public void CheckDemands()
        {
            for (int role = 0; role < 11; role++)
            {
                BattleAiRole actor = Roles[role];
                if (!actor.Valid)
                    continue;
                SetDemand(role, D.Revive, actor.Alive ? 100 : 0, actor.Alive);
                int state = HpState(actor.Hp, actor.MaxHp);
                SetDemand(role, D.Recover, state >= 3 ? 40 : 100, state < 3);
                SetDemand(role, D.BeAttack, state >= 3 ? 40 : 80, state < 3);
                if (state == 4)
                {
                    SetDemand(role, D.Recover, 10);
                    SetDemand(role, D.BeAttack, 10);
                }
                BattleAiDemand bad = Demand[role, (int)D.BadSpecStateRecover];
                if (!actor.BadState)
                    bad.Satisfy = 100;
                else if (!bad.Handled)
                {
                    bad.State = actor.WorstBadState;
                    bad.Satisfy = 50;
                }
            }
            CheckGroupDemand(false);
            CheckGroupDemand(true);
        }

        public void TraceDemands()
        {
            for (int role = 0; role < 11; role++)
            {
                if (!Roles[role].Valid)
                    continue;
                for (int kind = 1; kind < 10; kind++)
                    TraceDemand(Demand[role, kind]);
            }
            for (int kind = 1; kind < 10; kind++)
            {
                TraceDemand(FriendDemand[kind]);
                TraceDemand(EnemyDemand[kind]);
            }
        }

        private void TraceDemand(BattleAiDemand demand)
        {
            if (demand.ActionId < 0)
                return;
            if (_port.IsActionActive(demand.ActionId))
            {
                demand.Handled = true;
                demand.Satisfy = 100;
            }
            else
            {
                demand.ActionId = -1;
                demand.Handled = false;
            }
        }

        private void SetDemand(int role, D kind, int satisfy, bool force = false)
        {
            BattleAiDemand demand = Demand[role, (int)kind];
            if (force || !demand.Handled)
                demand.Satisfy = satisfy;
        }

        private void CheckGroupDemand(bool enemy)
        {
            int count = 0;
            for (int role = enemy ? 5 : 0; role < (enemy ? 11 : 5); role++)
            {
                if (Roles[role].Valid && HpState(Roles[role].Hp, Roles[role].MaxHp) >= 3)
                    count++;
            }
            int satisfy = count < 2 ? 100 : count == 2 ? 60 : count == 3 ? 40 : 20;
            BattleAiDemand[] group = enemy ? EnemyDemand : FriendDemand;
            if (!group[(int)D.Recover].Handled)
                group[(int)D.Recover].Satisfy = satisfy + 10;
            if (!group[(int)D.BeAttack].Handled)
                group[(int)D.BeAttack].Satisfy = enemy ? 100 - satisfy : satisfy;
        }

        private bool Try(BattleAiAttemptKind kind, int source, int target = -1, BattleAiDemand demand = null, int ability = 0)
        {
            long action = _port.TryAttempt(new BattleAiAttempt(kind, source, target, demand, ability));
            // The original BossPhysical entry always returns false, even if a future adapter is permissive.
            if (kind == BossPhysical || action < 0)
                return false;
            if (demand != null && kind is not (AttackBigMagic or FiveSpiritMagic or FiveSpiritBigMagic))
                demand.ActionId = action;
            return true;
        }

        private BattleAiDemand At(int target, D kind) => Demand[target, (int)kind];

        private static bool Needs(BattleAiDemand demand) => demand.Satisfy < 100;

        private bool MadTarget(int source) => Roles[source].Madness && _random.Next() % 10 > 7;

        private bool ChaosTarget(int source) => Roles[source].Chaos && _random.Next() % 10 > 7;

        public static bool IsSpecialBossReady(BattleAiRole role)
            => role.Identity == 739 && role.Mp == role.MaxMp;

        public bool HandleSpecialBoss(int source)
            => IsSpecialBossReady(Roles[source]) && Try(SpecAI, source, ability: 7709);

        // Use this inside the real CastNormalAttack adapter, after observing the attempt.
        // Formation and initial-position predicates come from the same session snapshot as commands.
        public bool CanNormalAttack(int source, int target, bool invisible, bool wall, bool mirror, bool atInitialPosition)
        {
            BattleAiRole actor = Roles[source];
            BattleAiRole destination = Roles[target];
            if (source == target || !actor.Profile.Attack && !actor.Chaos && !actor.Madness)
                return false;
            if (!actor.Valid || !destination.Valid || !actor.Alive || !destination.Alive || !_pal3A && invisible)
                return false;
            if (wall && mirror && (source < 5 || _random.Probability(70)))
                return false;
            return atInitialPosition && _port.CanAttackBack(source, target);
        }

        public bool CapabilityEnabled(int source, BattleAiAttemptKind kind)
        {
            BattleAiRole actor = Roles[source];
            if (kind == Normal)
                return actor.Profile.Attack || actor.Chaos || actor.Madness;
            if (kind == BossPhysical)
                return false;
            if (kind == SpecAI)
                return IsSpecialBossReady(actor);
            if (_pal3A && kind is SelectedMiscMagic or FiveSpiritMagic or FiveSpiritBigMagic)
                return !actor.Madness;
            bool enabled = kind is RecoverSkill or RecoverBigSkill or AttackSkill or AttackBigSkill or ReviveSkill or CureSkill ? actor.Profile.Skill
                : kind is RecoverItem or RecoverBigItem or AttackItem or AttackBigItem or ReviveItem or CureItem ? actor.Profile.Item : actor.Profile.Magic;
            return (enabled || actor.Chaos) && !actor.Madness;
        }

        public bool HandleEnemy(int source)
        {
            if (!Roles[source].Valid || !Roles[source].Alive)
                return false;
            int state = HpState(Roles[source].Hp, Roles[source].MaxHp);
            if (state == 5)
                return false;
            return Roles[source].Chaos ? HandleEnemyChaos(source) : HandleEnemyState(source, state <= 2 ? 0 : state == 3 ? 1 : 2);
        }

        // Exposed for native-function oracle tests, which invoke these three entry points directly.
        public bool HandleEnemyState(int source, int condition)
        {
            if (condition < 0 || condition > 2)
                throw new ArgumentOutOfRangeException(nameof(condition));
            if (condition == 0 && Try(SpecAI, source, ability: IsSpecialBossReady(Roles[source]) ? 7709 : 0))
                return true;
            int start = 0;
            for (; start < 11; start++)
            {
                if (_random.Next() % 3 == 1 && Roles[start].Valid)
                    break;
            }
            if (start == 10)
                start = 0;
            for (int target = start; target < 11; target++)
            {
                BattleAiRole actor = Roles[target];
                if (!actor.Valid || !actor.Alive || actor.EnemyFriend)
                    continue;
                if (_pal3A && condition == 2 && Roles[source].Identity == 7325)
                {
                    if (Try(RecoverMagic, source, source, At(target, D.Recover)) || Try(RecoverBigMagic, source, demand: At(target, D.Recover)))
                        return true;
                }
                bool firstAttack = condition == 2
                    ? target < 5 || MadTarget(source) && Needs(At(target, D.OneHit))
                    : (target < 5 || MadTarget(source) || ChaosTarget(source)) && Needs(At(target, D.OneHit));
                if (firstAttack && Try(Normal, source, target, At(target, D.OneHit)))
                    return true;
                if (Needs(EnemyDemand[(int)D.Recover]) && Try(RecoverBigMagic, source, demand: EnemyDemand[(int)D.Recover]))
                    return true;
                if (condition > 0 && target == source && Needs(At(target, D.Recover)))
                {
                    if (Try(RecoverMagic, source, target, At(target, D.Recover)))
                        return true;
                    if (condition == 2 && Try(RecoverBigMagic, source, demand: At(target, D.Recover)))
                        return true;
                }
                if (target >= 5 && Needs(At(target, D.Recover)) && Try(RecoverMagic, source, target, At(target, D.Recover)))
                    return true;
                if (target >= 5 && Needs(At(target, D.BadSpecStateRecover)) && _random.Probability(50) && Try(CureMagic, source, target, At(target, D.BadSpecStateRecover)))
                    return true;
                if (_pal3A ? HandlePal3AAdditional(source, target, condition) : HandlePal3Remainder(source, target, condition))
                    return true;
            }
            return false;
        }

        private bool HandlePal3Remainder(int source, int target, int condition)
        {
            BattleAiDemand group = FriendDemand[(int)D.BeAttack];
            if (Needs(group))
            {
                if (_random.Probability(condition == 0 ? 37 : 25) && Try(AttackBigMagic, source, demand: group))
                    return true;
                if (Roles[source].Boss && _random.Probability(30) && Try(BossPhysical, source, demand: group))
                    return true;
            }
            if (condition > 0 && HandleElementDemands(source, target, 70, -1, 25))
                return true;
            if (target < 5 && _random.Probability(condition == 0 ? 15 : 10))
            {
                if (Try(AttackBigMagic, source) || Try(AttackMagic, source, target))
                    return true;
            }
            if (condition == 0 && target >= 5 && _random.Probability(30))
            {
                if (Needs(At(target, D.Recover)) && Try(RecoverMagic, source, target))
                    return true;
                if (_random.Probability(30) && Try(RandomMiscMagic, source, target))
                    return true;
            }
            return (target < 5 || MadTarget(source) || condition == 0 && ChaosTarget(source)) && Try(Normal, source, target);
        }

        private bool HandleElementDemands(int source, int target, int wallProbability, int bossPhysicalProbability, int fiveProbability)
        {
            if (target >= 5)
                return false;
            if (Needs(At(target, D.WallMirrorAttack)))
            {
                if (_random.Probability(wallProbability) && Try(AttackMagic, source, target, At(target, D.WallMirrorAttack)))
                    return true;
                if (bossPhysicalProbability >= 0 && _random.Probability(bossPhysicalProbability) && Try(BossPhysical, source, demand: At(target, D.WallMirrorAttack)))
                    return true;
            }
            if (Needs(At(target, D.BoundAttack)) && _random.Probability(70) && Try(Normal, source, target, At(target, D.BoundAttack)))
                return true;
            if (Needs(At(target, D.FiveMagicAttack)))
            {
                if (Try(AttackMagic, source, target, At(target, D.FiveMagicAttack)))
                    return true;
                if (_random.Probability(fiveProbability) && Try(AttackMagic, source, target, At(target, D.FiveMagicAttack)))
                    return true;
            }
            return false;
        }

        private bool HandlePal3AAdditional(int source, int target, int condition)
        {
            BattleAiRole actor = Roles[source];
            bool boss = actor.Boss;
            if (actor.Seal && target < 5 && Try(Normal, source, target))
                return true;
            if (source >= 5)
            {
                int skill = QueryStatusMagic(source, BattleAiStatusQuery.Debuff);
                if (skill > 0 && target < 5 && _random.Probability(30 + condition * 10) && HealthyFriendCount() > (boss ? 1 : 2)
                    && Try(SelectedMiscMagic, source, target, ability: skill))
                    return true;
                if (condition == 1 || condition == 0 && !boss || condition == 2 && boss)
                {
                    skill = QueryStatusMagic(source, BattleAiStatusQuery.Buff);
                    if (skill > 0 && target >= 5 && _random.Probability(condition == 1 && boss ? 30 : 50) && Try(SelectedMiscMagic, source, target, ability: skill))
                        return true;
                }
                skill = QueryStatusMagic(source, BattleAiStatusQuery.Poison);
                if (skill > 0 && target < 5 && _random.Probability(50) && Try(SelectedMiscMagic, source, target, ability: skill))
                    return true;
            }
            BattleAiDemand group = FriendDemand[(int)D.BeAttack];
            if (Needs(group) && _random.Probability(boss ? 60 - condition * 10 : 50) && Try(boss ? BossPhysical : AttackBigMagic, source, demand: group))
                return true;
            if (HasSkillBetween(source, 264, 268))
            {
                bool single = _port.SceneFiveSpiritMode == 0 && source >= 5;
                int chance = single ? (boss ? 70 : 90) - condition * 10 : boss ? 60 - condition * 10 : 50;
                if (_random.Probability(chance) && Try(single ? FiveSpiritMagic : FiveSpiritBigMagic, source, single ? target : -1, At(target, D.WallMirrorAttack)))
                    return true;
            }
            if (boss && HasSkillBetween(source, 274, 274) && AliveEnemyCount() == 1 && _random.Probability(60 - condition * 10)
                && Try(SelectedMiscMagic, source, target, ability: 274))
                return true;
            int wall = condition == 0 || boss ? 80 : condition == 1 ? 60 : 40;
            int physical = condition == 0 ? 50 : condition == 1 ? (boss ? 50 : 40) : boss ? 40 : 30;
            if (HandleElementDemands(source, target, wall, physical, boss ? 80 : 70 - condition * 10))
                return true;
            if (target < 5 && _random.Probability(condition == 2 ? 50 : boss ? 80 - condition * 20 : 40))
            {
                if (Try(AttackBigMagic, source) || Try(AttackMagic, source, target))
                    return true;
            }
            if (target >= 5 && _random.Probability(10 + condition * 10) && Needs(At(target, D.Recover)) && Try(RecoverMagic, source, target))
                return true;
            return (target < 5 || MadTarget(source) || condition == 0 && ChaosTarget(source)) && Try(Normal, source, target);
        }

        private int HealthyFriendCount()
        {
            int count = 0;
            for (int role = 0; role < 5; role++)
            {
                if (Roles[role].Valid && !Roles[role].Fled && !Roles[role].BadState)
                    count++;
            }
            return count;
        }

        private int AliveEnemyCount()
        {
            int count = 0;
            for (int role = 5; role < 11; role++)
            {
                if (Roles[role].Valid && Roles[role].Alive && !Roles[role].Fled)
                    count++;
            }
            return count;
        }

        private bool HasSkillBetween(int source, int first, int last)
        {
            foreach (BattleAiSkill skill in Roles[source].Skills)
            {
                if (skill.Id >= first && skill.Id <= last)
                    return true;
            }
            return false;
        }

        public bool HandleEnemyChaos(int source)
        {
            int target = 0;
            for (int count = 0; count < 512; count++)
            {
                target = _random.Next() % 11;
                if (Roles[target].Valid && target != source && (!_pal3A || !Roles[target].Fled && !Roles[target].Fleeing))
                    break;
            }
            if (!Roles[target].Valid || _pal3A && (Roles[target].Fled || Roles[target].Fleeing))
                return false;
            int choice = _random.Next() % 8;
            BattleAiAttemptKind kind = choice == 1 ? RecoverBigMagic : choice == 2 ? RecoverMagic : choice == 4 ? AttackBigMagic : choice == 5 ? AttackMagic : Normal;
            return Try(kind, source, choice == 1 || choice == 4 ? -1 : target);
        }

        public bool HandleFriend(int source)
        {
            BattleAiRole actor = Roles[source];
            if (!actor.Valid || !actor.Alive || actor.Hp == 0)
                return false;
            return actor.Profile.Negative && HandleFriendNegative(source) || HandleFriendAffirmative(source);
        }

        public bool HandleFriendNegative(int source)
        {
            BattleAiAttemptKind[] chaos = { ReviveSkill, ReviveMagic, ReviveItem, RecoverBigMagic, RecoverBigSkill, RecoverBigItem, RecoverMagic, RecoverSkill, RecoverItem };
            for (int target = 0; target < 11; target++)
            {
                BattleAiRole actor = Roles[target];
                if (!actor.Valid)
                    continue;
                if (Roles[source].Chaos)
                {
                    int choice = _random.Next() % 64;
                    if (choice < chaos.Length && Try(chaos[choice], source, choice >= 3 && choice <= 5 ? -1 : target))
                        return true;
                    continue;
                }
                if (target < 5 && Needs(At(target, D.Revive)) && TryThree(source, target, At(target, D.Revive), ReviveSkill, ReviveMagic, ReviveItem))
                    return true;
                if (!actor.Alive || actor.Fled)
                    continue;
                if (Needs(FriendDemand[(int)D.Recover]) && TryThree(source, -1, FriendDemand[(int)D.Recover], RecoverBigMagic, RecoverBigSkill, RecoverBigItem))
                    return true;
                if (target < 5 && At(target, D.Recover).Satisfy < 50 && TryThree(source, target, At(target, D.Recover), RecoverMagic, RecoverSkill, RecoverItem))
                    return true;
                if (target < 5 && Needs(At(target, D.BadSpecStateRecover)) && _random.Probability(50) && TryThree(source, target, At(target, D.BadSpecStateRecover), CureMagic, CureSkill, CureItem))
                    return true;
            }
            return false;
        }

        public bool HandleFriendAffirmative(int source)
        {
            if (Roles[source].Chaos)
            {
                int target = 0;
                for (int count = 0; count < 1024; count++)
                {
                    target = _random.Next() % 11;
                    if (Roles[target].Valid && Roles[target].Fled && target != source)
                        break;
                }
                if (!Roles[target].Valid || Roles[target].Fled)
                    return false;
                BattleAiAttemptKind[] choices = { AttackBigMagic, AttackBigSkill, AttackBigItem, Normal, AttackMagic, AttackSkill, AttackItem, AttackMagic, AttackSkill, AttackItem, Normal };
                int choice = _random.Next() % 11;
                return Try(choices[choice], source, choice < 3 ? -1 : target);
            }
            for (int target = 0; target < 11; target++)
            {
                BattleAiRole actor = Roles[target];
                if (!actor.Valid || !actor.Alive || actor.Fled)
                    continue;
                if (Needs(EnemyDemand[(int)D.BeAttack]) && TryThree(source, -1, EnemyDemand[(int)D.BeAttack], AttackBigMagic, AttackBigSkill, AttackBigItem))
                    return true;
                if ((target >= 5 && Needs(At(target, D.OneHit)) || MadTarget(source)) && TryAttackSet(source, target, At(target, D.OneHit)))
                    return true;
                if ((target >= 5 || MadTarget(source)) && TryAttackSet(source, target, null))
                    return true;
            }
            return false;
        }

        private bool TryThree(int source, int target, BattleAiDemand demand, BattleAiAttemptKind first, BattleAiAttemptKind second, BattleAiAttemptKind third)
            => Try(first, source, target, demand) || Try(second, source, target, demand) || Try(third, source, target, demand);

        private bool TryAttackSet(int source, int target, BattleAiDemand demand)
            => Try(Normal, source, target, demand) || TryThree(source, target, demand, AttackMagic, AttackSkill, AttackItem);
    }
}
