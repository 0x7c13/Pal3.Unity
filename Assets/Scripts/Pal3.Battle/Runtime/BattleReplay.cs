// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class BattleReplay
    {
        public static BattleSession Recreate(BattleSetup setup, IBattleCatalog catalog, BattleSession original,
            IBattleEffectDirector effects = null, Action<BattleSession> onCreated = null)
        {
            return RecreateFrames(setup, catalog, original, effects, onCreated).Last();
        }

        // Every element is the same session advanced to the next replay boundary.
        // Enumeration must finish to verify the final timeline.
        public static IEnumerable<BattleSession> RecreateFrames(BattleSetup setup, IBattleCatalog catalog, BattleSession original,
            IBattleEffectDirector effects = null, Action<BattleSession> onCreated = null)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            BattleSession replay = null;
            foreach (BattleSession frame in PlayFrames(setup, catalog, original.Inputs, original.Tick,
                original.ReplayRules, original.Definitions, effects, onCreated))
            {
                replay = frame;
                yield return frame;
            }
            if (!original.MatchesModalTimeline(replay) || original.SimulationTick != replay.SimulationTick ||
                original.Status != replay.Status || original.Result?.Reason != replay.Result?.Reason)
                throw new InvalidOperationException("Replay diverged from the original battle. Check that the same battle movies are available.");
        }

        public static BattleSession Play(BattleSetup setup, IBattleCatalog catalog, IEnumerable<BattleInput> inputs, long endTick, IBattleRules rules = null, BattleDefinitions definitions = null, IBattleEffectDirector effects = null, Action<BattleSession> onCreated = null)
            => PlayFrames(setup, catalog, inputs, endTick, rules, definitions, effects, onCreated).Last();

        private static IEnumerable<BattleSession> PlayFrames(BattleSetup setup, IBattleCatalog catalog, IEnumerable<BattleInput> inputs, long endTick,
            IBattleRules rules, BattleDefinitions definitions, IBattleEffectDirector effects, Action<BattleSession> onCreated)
        {
            if (endTick < 0 || endTick > 10000000)
                throw new ArgumentOutOfRangeException(nameof(endTick));
            BattleInput[] recorded = inputs?.Select(input => input?.Copy() ?? throw new ArgumentException("Missing replay input.")).ToArray()
                ?? throw new ArgumentNullException(nameof(inputs));
            if (recorded.Any(input => input.Tick < 0 || input.Tick > endTick) || !recorded.Select(input => input.Tick).SequenceEqual(recorded.Select(input => input.Tick).OrderBy(tick => tick)))
                throw new ArgumentException("Replay inputs must be in their recorded order and within the requested tick range.");
            var session = new BattleSession(setup, catalog, rules, definitions, effects);
            onCreated?.Invoke(session);
            int cursor = 0;
            while (true)
            {
                while (cursor < recorded.Length && recorded[cursor].Tick == session.Tick)
                {
                    BattleInput input = recorded[cursor++];
                    if (!Enum.IsDefined(typeof(BattleInputKind), input.Kind))
                        throw new ArgumentException("Unknown replay input kind.");
                    if (input.FromTestAi)
                    {
                        // Automatic commands are regenerated; they must still agree with their recorded payload.
                        if (!session.MatchesRecordedInput(cursor - 1, input))
                            throw new InvalidOperationException("Replay automatic input diverged at " + session.Tick);
                        continue;
                    }
                    switch (input.Kind)
                    {
                        case BattleInputKind.ControlChange:
                            session.SetAutomatic(input.Side, input.Automatic);
                            break;
                        case BattleInputKind.Abort:
                            session.Abort(input.Reason);
                            break;
                        case BattleInputKind.Fault:
                            session.Fault(input.Reason);
                            break;
                        case BattleInputKind.CancelAction:
                            if (!session.CancelAction(input.Actor))
                                throw new InvalidOperationException("Replay cancellation rejected at " + session.Tick);
                            break;
                        case BattleInputKind.Command:
                            if (!session.Submit(new BattleCommand(input.Actor, input.Action, input.Target, input.AbilityId)))
                                throw new InvalidOperationException("Replay command rejected at " + session.Tick);
                            break;
                        case BattleInputKind.ElementWheel:
                            if (!session.SubmitElementWheel(input.Actor, input.AbilityId))
                                throw new InvalidOperationException("Replay spirit wheel command rejected at " + session.Tick);
                            break;
                        case BattleInputKind.SetProtection:
                            if (!session.SetProtection(input.Actor, input.Target))
                                throw new InvalidOperationException("Replay protection change rejected at " + session.Tick);
                            break;
                        case BattleInputKind.BeginFormation:
                            if (!session.BeginFormation(input.Actor))
                                throw new InvalidOperationException("Replay formation opening rejected at " + session.Tick);
                            break;
                        case BattleInputKind.SwapFormation:
                            if (!session.SwapFormation(input.Actor, input.FormationSlot))
                                throw new InvalidOperationException("Replay formation swap rejected at " + session.Tick);
                            break;
                        case BattleInputKind.AiNormal:
                            session.RequestAiNormal(input.Actor);
                            break;
                        case BattleInputKind.AiRepeat:
                            session.RequestAiRepeat(input.Actor);
                            break;
                        case BattleInputKind.MagicMenuFieldDenial:
                            if (!session.InspectMagicMenuAbility(input.Actor, input.AbilityId))
                                throw new InvalidOperationException("Replay could not invalidate a forbidden magic menu command at " + session.Tick);
                            break;
                        case BattleInputKind.AiProfile:
                            session.SetAiProfile(input.Actor, input.AiFlags);
                            break;
                        case BattleInputKind.AiControl:
                            if (!session.CommitAiControl(input.Actor, input.AiFlags))
                                throw new InvalidOperationException("Replay AI control rejected at " + session.Tick);
                            break;
                        case BattleInputKind.AiAutoOnly:
                            session.SetAiAutoOnly(input.Automatic);
                            break;
                        case BattleInputKind.CoAttackPresentationSkip:
                            session.SkipCoAttackPresentation();
                            break;
                        case BattleInputKind.ModalPresentationSkip:
                            if (!session.SkipModalPresentation())
                                throw new InvalidOperationException("Replay cannot skip a missing battle movie at " + session.Tick);
                            break;
                        case BattleInputKind.EndFormation:
                            if (!session.EndFormation(input.Actor))
                                throw new InvalidOperationException("Replay formation closing rejected at " + session.Tick);
                            break;
                        default:
                            throw new ArgumentException("Unsupported replay input kind: " + input.Kind);
                    }
                    if (!session.MatchesRecordedInput(cursor - 1, input))
                        throw new InvalidOperationException("Replay input diverged at " + session.Tick);
                }
                if (session.Tick == endTick)
                {
                    if (session.RecordedInputCount != recorded.Length)
                        throw new InvalidOperationException("Replay generated a different number of inputs.");
                    yield return session;
                    yield break;
                }
                if (session.Status != BattleSessionStatus.Running)
                    throw new InvalidOperationException("Replay ended before its recorded final tick.");
                long before = session.Tick;
                session.Step();
                if (session.Tick == before)
                    throw new InvalidOperationException("Replay needs a battle movie that has not finished preparing.");
                yield return session;
            }
        }
    }
}
