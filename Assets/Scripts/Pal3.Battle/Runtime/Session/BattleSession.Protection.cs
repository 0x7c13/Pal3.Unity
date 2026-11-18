// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private string ValidatePhysicalProtection(string controller, string target)
        {
            if (Status != BattleSessionStatus.Running || _resolvingOutcome || controller == null || !_byId.TryGetValue(controller, out Actor guardian) ||
                !guardian.Ready || guardian.Run != null || !PhysicalBodyIdle(guardian) || IsUncontrollable(guardian))
                return "The protection controller is not ready.";
            if (NativeActorIndex(guardian) != 0 || guardian.Initial.Side != BattleSide.Ally || !guardian.Alive || guardian.Slot != BattleSlot.Center)
                return "Original protection requires the living player in native slot 0 at the center.";
            BattleFormationRules rules = FormationRules();
            if (target == null)
            {
                // The command wheel checks occupied party slots, including fallen allies.
                return _actors.Any(actor => rules.IsProtectionTargetSlot(NativeActorIndex(actor))) ? null : "队伍中没有可设置援护的角色。";
            }
            if (!_byId.TryGetValue(target, out Actor protectedActor))
                return "The protection target is missing.";
            BattleFormationDecision decision = rules.CanSetProtect(FormationActor(guardian), FormationActor(protectedActor));
            return decision.FailureReason;
        }

        // The caller records a SetProtection input only when this changes the
        // selection. Native confirmation of an unchanged selection returns to UI.
        private bool ApplyPhysicalProtection(string controller, string target)
        {
            if (ValidatePhysicalProtection(controller, target) != null)
                return false;
            int selection = target == null ? -1 : NativeActorIndex(_byId[target]);
            if (selection == _physicalProtectionTarget)
                return false;
            _physicalProtectionTarget = selection;
            Actor actor = _byId[controller];
            AdvanceFiveSpiritFieldTurn(actor);
            actor.Gauge = actor.CastGauge = 0;
            actor.Ready = false;
            return true;
        }

        private void TryStartProtection(PhysicalStageRun attack)
        {
            if (!attack.CanStartProtection || attack.Source.Initial.Side != BattleSide.Enemy || _physicalProtectionTarget < 0)
                return;
            Actor guardian = ActorAtNativeIndex(0);
            Actor target = ActorAtNativeIndex(_physicalProtectionTarget);
            if (guardian == null || target == null || !attack.Run.Targets.Contains(target.Initial.InstanceId) || _catalog is not IBattlePhysicalTimings)
                return;
            double distance = BattleMovement.Distance(target.Position, attack.Source.Position);
            if (distance >= 120f || distance <= 50f)
                return;
            BattleFormationActor guardianSnapshot = FormationActor(guardian);
            var idleGuardian = new BattleFormationActor(guardianSnapshot.Index, guardianSnapshot.ResourceId, guardianSnapshot.Slot,
                guardianSnapshot.WeaponType, valid: guardianSnapshot.Valid, alive: guardianSnapshot.Alive,
                hp: guardianSnapshot.Hp, maxHp: guardianSnapshot.MaxHp, states: guardian.States.Keys,
                idle: PhysicalBodyIdle(guardian), atHome: IsAtHome(guardian), fled: guardian.Escaped);
            if (!FormationRules().CanProtect(_physicalProtectionTarget, idleGuardian, FormationActor(target)).Allowed)
                return;
            var stage = new PhysicalStageRun
            {
                Session = this,
                Source = guardian,
                Protected = target,
                Attacker = attack.Source,
                Profile = PhysicalBody(guardian).Profile,
                Protection = true
            };
            if (!_stage.TryAdd(guardian.Initial.InstanceId, "protection", stage,
                BattleStageScheduler.TimeAt(SimulationTick), out BattleStageHandle handle))
                return;
            stage.Handle = handle;
            _protectionStages.Add(stage);
            attack.CanStartProtection = false;
        }

        private bool AdvanceProtectionSubtask(PhysicalStageRun stage, int index, bool require)
        {
            Actor guardian = stage.Source;
            if (!stage.Executed[index])
            {
                if (PhysicalBodyIdle(guardian))
                {
                    if (index == 1)
                    {
                        BattleMovementPoint destination = BattlePhysicalGeometry.ProtectRunTarget(guardian.Position, stage.Protected.Home, stage.Attacker.Home);
                        QueuePhysicalTask(guardian, new PhysicalBodyTask
                        {
                            Kind = BattlePhysicalBodyKind.FastRun,
                            Destination = destination,
                            Animation = stage.Profile.Animation(BattlePose.Moving)
                        });
                        QueuePhysicalTask(guardian, new PhysicalBodyTask
                        {
                            Kind = BattlePhysicalBodyKind.Turn,
                            Destination = new BattleMovementPoint(BattleFacingRules.Initial(guardian.Initial.Side), 0, 0)
                        });
                    }
                    else if (index == 2)
                        QueuePhysicalTask(guardian, new PhysicalBodyTask
                        {
                            Kind = BattlePhysicalBodyKind.Protect,
                            Target = stage.Protected,
                            Animation = stage.Profile.Animation(BattlePose.Guard)
                        });
                    else
                        QueuePhysicalReturn(stage, new[] { BattlePhysicalBodyKind.Run, BattlePhysicalBodyKind.Turn });
                    stage.Executed[index] = true;
                }
                else if (index == 1)
                    return true;
            }
            return require || PhysicalBodyIdle(guardian);
        }

        private bool HasEnemyPhysicalAttack(Actor target) => _physicalStages.Values.Any(stage => !stage.Finished &&
            stage.Source.Initial.Side == BattleSide.Enemy && stage.Run.Targets.Contains(target.Initial.InstanceId));

        private void AdvancePhysicalGuardBody(Actor actor, PhysicalBodyState body, PhysicalBodyTask task)
        {
            if (!task.Executed)
            {
                task.Executed = true;
                body.Pose = BattlePose.Guard;
                TryPlayBodyAnimation(actor, task.Animation, true, BattlePose.Guard, body.Profile.NativeAction(BattlePose.Guard));
                body.BladeSeconds = .25f;
                return;
            }
            BattlePhysicalMarkerKind? marker = GetBodyBinding(actor).LastPoppedMarker;
            if ((!actor.Alive || marker == BattlePhysicalMarkerKind.Over) &&
                (!HasEnemyPhysicalAttack(task.Target) || !IsAtHome(task.Target) || !task.Target.Alive))
            {
                task.Finished = true;
                body.BladeSeconds = .01f;
            }
        }

        private bool TryGetPhysicalProtector(Actor attacker, Actor target, out Actor guardian)
        {
            guardian = ActorAtNativeIndex(0);
            return target.Initial.Side == BattleSide.Ally && guardian != null && _physicalProtectionTarget != 0 &&
                NativeActorIndex(target) == _physicalProtectionTarget && guardian.Alive && guardian.Slot == BattleSlot.Center &&
                !Has(guardian, BattleState.Paralysis) && IsAtHome(target) &&
                BattlePhysicalGeometry.AtProtectPosition(NativeActorIndex(guardian), guardian.Position, target.Home, attacker.Home) &&
                _protectionStages.Any(stage => !stage.Finished && _stage.IsActive(stage.Handle));
        }
    }
}
