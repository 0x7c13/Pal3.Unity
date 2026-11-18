// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleHudPresenter
    {
        private readonly Dictionary<string, (int Generation, int Mask, int Flags)> _aiDraft = new(StringComparer.Ordinal);

        private void BeginAiDraft()
        {
            _aiDraft.Clear();
            AiActor = Controller;
        }

        public bool AiControlConsumesGauges => Controller != null && _session.Actors.First(actor => actor.Id == Controller).Side == BattleSide.Ally &&
            BattleAiControlRules.ConsumesGauges(_session.GetAiProfile(Controller), AiFlags(Controller));

        public int AiFlags(string actor)
        {
            int live = _session.GetAiProfile(actor);
            return _aiDraft.TryGetValue(actor, out var edit) && edit.Generation == _session.MonsterGeneration(actor)
                ? (live & ~edit.Mask) | (edit.Flags & edit.Mask)
                : live;
        }

        public static int ToggleAiFlag(int flags, int bit)
        {
            if (flags < 0 || flags > 63 || bit < 0 || bit > 5)
                throw new ArgumentOutOfRangeException(nameof(flags));
            int changed = flags ^ 1 << bit;
            if (bit == 5)
                return (changed & 32) != 0 ? changed | 1 : changed & ~15;
            if (bit == 0)
                return (changed & 1) == 0 ? changed & ~47 : (flags & 32) != 0 ? changed : flags & ~1;
            if ((flags & 32) == 0)
                return bit == 4 ? flags : flags & ~(1 << bit);
            return changed;
        }

        public bool ToggleAiDraft(int bit)
        {
            if (Page != BattleHudPage.Ai || Controller == null)
                return false;
            string actor = AiActor ?? Controller;
            int before = AiFlags(actor);
            int next = ToggleAiFlag(before, bit);
            int generation = _session.MonsterGeneration(actor);
            int previousMask = _aiDraft.TryGetValue(actor, out var edit) && edit.Generation == generation ? edit.Mask : 0;
            int live = _session.GetAiProfile(actor);
            // Preserve changes made outside this panel to flags the draft leaves alone.
            int mask = (previousMask | (before ^ next)) & (live ^ next);
            if (mask != 0)
                _aiDraft[actor] = (generation, mask, next);
            else
                _aiDraft.Remove(actor);
            bool denied = (before & 32) == 0 && (bit is >= 1 and <= 4 || bit == 0 && (before & 1) == 0);
            Message = denied ? "先启用 AI，再设置此项。" : null;
            return next != before;
        }

        public bool CommitAiDraft()
        {
            if (Page != BattleHudPage.Ai || Controller == null || _session.WaitingActor != Controller || _session.IsPal3ATransformed(Controller))
                return false;
            string controller = Controller;
            int flags = AiFlags(controller);
            // The original controller pays its original menu cost atomically.
            if (_session.GetAiProfile(controller) != flags)
            {
                bool playerController = _session.Actors.First(actor => actor.Id == controller).Side == BattleSide.Ally;
                if (playerController)
                {
                    if (!_session.CommitAiControl(controller, flags))
                        return false;
                }
                else
                    _session.SetAiProfile(controller, flags);
            }
            // Editing a different participant is an explicit tool extension.
            // It retains the existing recorded debug profile command.
            foreach (BattleActorSnapshot actor in _session.Actors)
                if (actor.Id != controller && _aiDraft.ContainsKey(actor.Id))
                    _session.SetAiProfile(actor.Id, AiFlags(actor.Id));
            _aiDraft.Clear();
            Page = BattleHudPage.Commands;
            ResetFocus();
            Synchronize(_session);
            return true;
        }
    }
}
