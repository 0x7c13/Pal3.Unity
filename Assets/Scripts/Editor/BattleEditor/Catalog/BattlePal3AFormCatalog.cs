// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Engine.Extensions;

    public sealed partial class BattleAnimationCatalog : IBattlePal3AFormCatalog
    {
        private readonly Dictionary<string, string> _pal3AOriginalModels = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Actor, bool Transformed), BattlePhysicalAnimation> _pal3AFormClips = new();

        public BattlePhysicalAnimation FormChangeAnimation(string actorId, bool transformed)
        {
            if (BattleData.Variant != "PAL3A" || !_participants.TryGetValue(actorId, out BattleParticipant actor) ||
                actor.ActorId is not (101 or 103))
                throw new ArgumentException("This actor has no original PAL3A hero form.", nameof(actorId));
            if (_pal3AFormClips.TryGetValue((actorId, transformed), out BattlePhysicalAnimation captured))
                return captured;
            if (!_pal3AOriginalModels.ContainsKey(actorId))
                _pal3AOriginalModels.Add(actorId, _data.Model(actor));
            BattleParticipant candidate = actor.Copy();
            candidate.Model = transformed ? actor.ActorId == 101 ? "107" : "106" : _pal3AOriginalModels[actorId];
            captured = CompilePhysicalAnimation(candidate, "z20");
            _pal3AFormClips.Add((actorId, transformed), captured);
            return captured;
        }

        public void ActivatePal3AForm(string actorId, BattlePal3AFormKind kind)
        {
            if (BattleData.Variant != "PAL3A" || !_participants.TryGetValue(actorId, out BattleParticipant actor))
                throw new ArgumentException("The original PAL3A form actor is missing.", nameof(actorId));
            if (!_pal3AOriginalModels.ContainsKey(actorId))
                _pal3AOriginalModels.Add(actorId, _data.Model(actor));
            // Only this session's selected model changes. Every compiled clip and
            // physical profile remains immutable and no live Unity object enters it.
            BattleParticipant candidate = actor.Copy();
            candidate.Model = kind switch
            {
                BattlePal3AFormKind.NangongFireBeast when actor.ActorId == 101 => "107",
                BattlePal3AFormKind.PengxuFairyBeast when actor.ActorId == 103 => "106",
                BattlePal3AFormKind.Human => _pal3AOriginalModels[actorId],
                _ => throw new ArgumentException("The selected form belongs to another hero.", nameof(kind))
            };
            ActorDefinition definition = _actors[actorId];
            var durations = new Dictionary<BattlePose, int>();
            foreach (var pair in definition.Clips)
                if (_data.TryActionPath(candidate, pair.Value, out string path))
                {
                    BattleAnimationMetadata clip = _data.GetAnimationMetadata(path);
                    double rate = pair.Key == BattlePose.Idle ? .65f : ActivePlaybackRate;
                    durations[pair.Key] = ToTicks(clip.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / rate);
                }
            actor.Model = candidate.Model;
            definition.Durations = durations;
            _physicalProfiles.Remove(actorId);
            InvalidateInitialAttacks(actorId);
            foreach (var key in _abilities.Keys.Where(key => key.Item1 == actorId).ToArray())
                _abilities.Remove(key);
            foreach (var key in _abilityStages.Keys.Where(key => key.Item1 == actorId).ToArray())
                _abilityStages.Remove(key);
        }
    }
}
